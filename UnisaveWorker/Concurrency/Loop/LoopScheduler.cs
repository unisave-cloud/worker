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
        // protected by lock(tasks)

        /// <summary>
        /// The thread that does task processing
        /// </summary>
        private readonly Thread loopThread;
        
        /// <summary>
        /// The loop thread starts waiting on this if there are no tasks
        /// to be processed. Signalling here wakes it up.
        /// </summary>
        private readonly ManualResetEvent waitHandle = new(initialState: false);
        // state changes should happen in lock(tasks) to keep it synced with tasks
        
        /// <summary>
        /// Tripped during disposal to tell the loop thread to terminate
        /// </summary>
        private readonly CancellationTokenSource cts = new();

        public LoopScheduler()
        {
            loopThread = new Thread(TheLoop);
            loopThread.Start();
        }

        /// <summary>
        /// Stops the loop thread
        /// </summary>
        public void Dispose()
        {
            // signal termination
            cts.Cancel();
            
            // wake up the loop thread
            waitHandle.Set();
            
            // wait for the loop thread to finish
            loopThread.Join();
        }

        /// <summary>
        /// Queues a task to the scheduler.
        /// </summary>
        /// <param name="task">The task to be queued.</param>
        protected sealed override void QueueTask(Task task)
        {
            lock (tasks)
            {
                // add the task to the list of tasks to be processed
                tasks.AddLast(task);
                
                // wake up the sleeping loop
                // (here inside the lock to keep its state in sync with the list)
                waitHandle.Set();
            }
        }

        /// <summary>
        /// Implements the single-threaded task processing itself
        /// </summary>
        private void TheLoop()
        {
            try
            {
                // process tasks from the list
                while (true)
                {
                    // check termination
                    if (cts.Token.IsCancellationRequested)
                        break;
                    
                    // try getting a task
                    Task? item = TryPoppingTask();

                    // execute the task
                    if (item != null)
                    {
                        base.TryExecuteTask(item);
                    }
                    else // or wait for tasks to be added
                    {
                        // if someone added a task and signalled before now,
                        // we just continue straight through this
                        waitHandle.WaitOne();
                    }
                }
            }
            catch (Exception e) // catch unhandled exceptions
            {
                Log.Error(
                    "Unhandled exception in the single-threaded loop: " + e
                );

                // kill the application
                int exitCode = Marshal.GetHRForException(e);
                Environment.Exit(exitCode);
            }
        }

        /// <summary>
        /// Tries to pop a task from the list and if there are none,
        /// returns null
        /// </summary>
        private Task? TryPoppingTask()
        {
            lock (tasks)
            {
                // there are none
                if (tasks.Count == 0)
                {
                    // sleep on the wait handle instead of looping for nothing
                    waitHandle.Reset();
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
            // we only consider inlining, if we are already running
            // on the loop thread
            if (Thread.CurrentThread != loopThread)
                return false;
            
            // remove the task from the queue, since we've got to it
            // via inlining, instead of popping from the queue
            if (taskWasPreviouslyQueued)
                TryDequeue(task);

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
            lock (tasks)
            {
                // NOTE: we don't need to care about the wait handle reset here,
                // since it will be reset on failed queue pop anyways.
                // It would save just a single pass through the loop.
                
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
                Monitor.TryEnter(tasks, ref lockTaken);
                if (lockTaken)
                    return tasks.ToArray();
                else
                    throw new NotSupportedException();
            }
            finally
            {
                if (lockTaken)
                    Monitor.Exit(tasks);
            }
        }
    }
}