using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace UnisaveWorker.Concurrency.Loop
{
    /// <summary>
    /// Wrapper around a thread that runs inside the LoopScheduler
    /// </summary>
    public class LoopThread
    {
        /// <summary>
        /// Checking this value from a thread tells you, whether your thread
        /// is a loop thread or not.
        /// </summary>
        [ThreadStatic]
        public static bool ThisThreadIsLoopThread;
        
        /// <summary>
        /// The execution thread
        /// </summary>
        private readonly Thread thread;

        /// <summary>
        /// Tries atomically popping a task from the scheduler's queue
        /// </summary>
        private readonly Func<ManualResetEvent, Task?> tryPoppingTask;

        /// <summary>
        /// Executes a task via the scheduler's protected execution method
        /// </summary>
        private readonly Func<Task, bool> tryExecuteTask;
        
        /// <summary>
        /// The loop starts waiting on this if there are no tasks
        /// to be processed. Signalling here wakes it up.
        /// </summary>
        private readonly ManualResetEvent waitHandle = new(initialState: false);

        /// <summary>
        /// When tripped, it breaks the infinite loop for good and terminates
        /// the loop thread
        /// </summary>
        private readonly CancellationTokenSource breakTheLoopCts = new();

        /// <summary>
        /// Observes task execution and triggers deadlock resolution code
        /// </summary>
        private readonly DeadlockObserver deadlockObserver;

        /// <summary>
        /// Remembers whether we terminate naturally, or due to a deadlock
        /// </summary>
        private bool deadlockCausedAbort = false;
        
        public LoopThread(
            Func<ManualResetEvent, Task?> tryPoppingTask,
            Func<Task, bool> tryExecuteTask,
            DeadlockObserver deadlockObserver
        )
        {
            this.tryPoppingTask = tryPoppingTask;
            this.tryExecuteTask = tryExecuteTask;
            this.deadlockObserver = deadlockObserver;

            thread = new Thread(TheLoop) {
                Name = "Loop Thread"
            };
        }

        /// <summary>
        /// Starts the task-executing loop
        /// </summary>
        public void Start()
        {
            thread.Start();
        }

        /// <summary>
        /// Wakes up the thread if it sleeps
        /// because there were no tasks to process
        /// </summary>
        public void WakeUp()
        {
            waitHandle.Set();
        }

        /// <summary>
        /// Breaks the infinite loop and causes the processing
        /// thread to terminate
        /// </summary>
        public void BreakTheLoop()
        {
            breakTheLoopCts.Cancel();
            
            // if the thread currently sleeps at the wait handle,
            // this will wake it up
            WakeUp();
        }

        /// <summary>
        /// Waits for the completion of the processing thread
        /// </summary>
        public void Join()
        {
            if (!breakTheLoopCts.IsCancellationRequested)
                throw new InvalidOperationException(
                    "You have to break the loop before waiting for the " +
                    "thread completion. Otherwise you'd wait forever."
                );
            
            thread.Join();
        }

        /// <summary>
        /// Invoked when this thread is suspected to have deadlocked
        /// (or at least it has not responded in some time)
        /// </summary>
        public void AbortDueToSuspectedDeadlock()
        {
            int tid = thread.ManagedThreadId;
            Log.Error(
                $"Aborting the loop thread (ID {tid}) due to suspected deadlock."
            );

            // we want a log when the thread finishes
            deadlockCausedAbort = true;
            
            // should the thread ever get unstuck (or finish whatever
            // it is doing), it will not consume any more tasks and terminate
            // (because there is a replacement thread already running)
            BreakTheLoop();
            
            // Now, it would be cool to get the stack trace of the loop thread
            // somehow, but doing thread.Abort() and catching the exception
            // will not do, since the trace will point to some internal TPL
            // invocation of some task and we really want the stack trace of
            // that task. Also, aborting the thread is not good, it's better
            // to just let it do its thing and it's likely to get unstuck
            // if this is a self-waiting deadlock.
            
            // But if we run in Rider, we can inspect threads now to see
            // the stacktrace:
            if (Debugger.IsAttached)
                Debugger.Break();
        }
        
        /// <summary>
        /// Implements the single-threaded task processing loop
        /// </summary>
        private void TheLoop()
        {
            try
            {
                // remember that we are a loop thread
                ThisThreadIsLoopThread = true;

                // process tasks from the list
                while (true)
                {
                    // check termination
                    if (breakTheLoopCts.Token.IsCancellationRequested)
                        break;

                    // try getting a task
                    Task? item = tryPoppingTask.Invoke(waitHandle);

                    // execute the task
                    if (item != null)
                    {
                        deadlockObserver.StartTimer();

                        tryExecuteTask.Invoke(item);

                        deadlockObserver.StopTimer();
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
                // (because this is a severe runtime-infrastructure exception)
                int exitCode = Marshal.GetHRForException(e);
                Environment.Exit(exitCode);
            }
            finally
            {
                // should this thread be recycled, we want the flag to be reset
                ThisThreadIsLoopThread = false;
            }

            // on abort, log when the stray thread terminates
            if (deadlockCausedAbort)
            {
                int tid = Thread.CurrentThread.ManagedThreadId;
                Log.Info(
                    $"The stray loop thread (ID {tid}) has terminated."
                );
            }
        }
    }
}