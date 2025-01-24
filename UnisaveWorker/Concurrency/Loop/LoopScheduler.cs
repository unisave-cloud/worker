using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace UnisaveWorker.Concurrency.Loop
{
    /// <summary>
    /// Processes TPL tasks by a single thread only,
    /// analogous to the javascript event loop.
    ///
    /// It must be disposed so that the loop thread is gracefully terminated.
    /// 
    /// The implementation is based upon the Microsoft's
    /// LimitedConcurrencyLevelTaskScheduler taken from here:
    /// https://github.com/ChadBurggraf/parallel-extensions-extras
    /// /blob/master/TaskSchedulers/LimitedConcurrencyLevelTaskScheduler.cs
    /// </summary>
    public class LoopScheduler : TaskScheduler, IDisposable
    {
        /// <summary>
        /// Gets the maximum concurrency level supported by this scheduler.
        /// </summary>
        public sealed override int MaximumConcurrencyLevel => 1;
        
        /// <summary>
        /// The list of tasks to be executed.
        /// </summary>
        private readonly LinkedList<Task> tasks = new();

        /// <summary>
        /// The thread that does task processing
        /// </summary>
        private LoopThread? loopThread;
        
        /// <summary>
        /// Observes task execution and triggers deadlock resolution code.
        /// Is bound to the loop thread and should be discarded with it.
        /// </summary>
        private DeadlockObserver? deadlockObserver;
        
        // protects tasks and the loop thread instance
        private readonly object syncLock = new();

        /// <summary>
        /// How many seconds should the loop thread not respond for
        /// in order to treat it as a deadlock
        /// </summary>
        private readonly double deadlockTimeoutSeconds;
        
        public LoopScheduler(double deadlockTimeoutSeconds)
        {
            this.deadlockTimeoutSeconds = deadlockTimeoutSeconds;
            
            StartLoopThread();
        }

        private void StartLoopThread()
        {
            lock (syncLock)
            {
                if (loopThread != null)
                    throw new InvalidOperationException(
                        "There already is a loop thread."
                    );
                
                deadlockObserver = new DeadlockObserver(
                    timeoutSeconds: deadlockTimeoutSeconds,
                    reportDeadlockHandler: HandleReportedDeadlock
                );
                loopThread = new LoopThread(
                    tryPoppingTask: this.TryPoppingTask,
                    tryExecuteTask: base.TryExecuteTask,
                    deadlockObserver: deadlockObserver
                );
                loopThread.Start();
            }
        }

        /// <summary>
        /// Queues a task to the scheduler.
        /// </summary>
        /// <param name="task">The task to be queued.</param>
        protected sealed override void QueueTask(Task task)
        {
            lock (syncLock)
            {
                // check that we are booted up properly
                if (loopThread == null)
                    throw new InvalidOperationException(
                        "There is no loop thread."
                    );
                
                // add the task to the list of tasks to be processed
                tasks.AddLast(task);
            }
            
            // wake up the (potentially) sleeping loop
            loopThread.WakeUp();
        }

        /// <summary>
        /// Tries to pop a task from the list and if there are none,
        /// returns null
        /// </summary>
        /// <param name="threadWaitHandle">
        /// If there are no more tasks, the wait handle will be atomically
        /// reset to blocking.
        /// </param>
        private Task? TryPoppingTask(ManualResetEvent threadWaitHandle)
        {
            lock (syncLock)
            {
                // there are no tasks left
                if (tasks.Count == 0)
                {
                    // make the thread sleep instead of spinning
                    threadWaitHandle.Reset();
                    return null;
                }

                // get the next item from the queue
                Task item = tasks.First.Value;
                tasks.RemoveFirst();
                return item;
            }
        }

        /// <summary>
        /// Attempts to execute the specified task on the current thread.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        /// <param name="taskWasPreviouslyQueued"></param>
        /// <returns>
        /// Whether the task could be executed on the current thread.
        /// </returns>
        protected sealed override bool TryExecuteTaskInline(
            Task task,
            bool taskWasPreviouslyQueued
        )
        {
            // we only consider inlining, if we are running
            // in the loop thread
            if (!LoopThread.ThisThreadIsLoopThread)
                return false;
            
            // remove the task from the queue, since we've got to it
            // via inlining, instead of popping from the queue
            if (taskWasPreviouslyQueued)
                TryDequeue(task);
            
            // report movement
            // (we might be reporting to another thread here, but it's ok,
            // it just postpones the inevitable - better than a false positive)
            deadlockObserver?.ResetTimer();

            // try to run the task
            return base.TryExecuteTask(task);
        }

        /// <summary>
        /// Attempts to remove a previously scheduled task from the scheduler.
        /// </summary>
        /// <param name="task">The task to be removed.</param>
        /// <returns>Whether the task could be found and removed.</returns>
        protected sealed override bool TryDequeue(Task task)
        {
            lock (syncLock)
            {
                return tasks.Remove(task);
            }
        }

        /// <summary>
        /// Gets an enumerable of the tasks currently scheduled
        /// on this scheduler.
        /// </summary>
        protected sealed override IEnumerable<Task> GetScheduledTasks()
        {
            bool lockTaken = false;
            try
            {
                Monitor.TryEnter(syncLock, ref lockTaken);
                if (lockTaken)
                    return tasks.ToArray();
                else
                    throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(syncLock);
            }
        }

        /// <summary>
        /// Invoked by some deadlock observer, who thinks there's a task
        /// being processed suspiciously long.
        /// </summary>
        /// <param name="sender">Who invoked us</param>
        private void HandleReportedDeadlock(DeadlockObserver sender)
        {
            lock (syncLock)
            {
                // there must be a loop thread running,
                // otherwise the report is for useless
                if (loopThread == null)
                    return;
                
                // accept reports only from the current deadlock observer
                if (sender != deadlockObserver)
                    return;
                
                // terminate the existing loop thread
                // (without joining)
                loopThread.AbortDueToSuspectedDeadlock();
                loopThread = null;
                deadlockObserver = null;
                
                // start a new loop thread to handle the waiting tasks
                StartLoopThread();
            }
        }

        /// <summary>
        /// Stops the loop thread
        /// </summary>
        public void Dispose()
        {
            lock (syncLock)
            {
                // already disposed, do nothing
                if (loopThread == null)
                    return;
                
                // signal termination
                loopThread.BreakTheLoop();
            
                // wait for the loop thread to finish
                loopThread.Join();
                
                // we don't want to touch the thread anymore
                loopThread = null;
            }
        }
    }
}