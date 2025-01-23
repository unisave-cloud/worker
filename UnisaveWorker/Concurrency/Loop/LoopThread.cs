using System;
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
        
        public LoopThread(
            Func<ManualResetEvent, Task?> tryPoppingTask,
            Func<Task, bool> tryExecuteTask
        )
        {
            this.tryPoppingTask = tryPoppingTask;
            this.tryExecuteTask = tryExecuteTask;
            
            thread = new Thread(TheLoop);
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
                        tryExecuteTask.Invoke(item);
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
        }
    }
}