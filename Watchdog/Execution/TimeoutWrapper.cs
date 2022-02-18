using System;
using System.Threading;

namespace Watchdog.Execution
{
    /// <summary>
    /// Handles timeout for execution
    ///
    /// It runs the action on a separate thread and if the action times out
    /// it returns and lets the action thread be (doesn't abort since it's not
    /// possible since .NET 5 and up), instead signals the abort to the called
    /// and the caller then has to kill the entire worker
    /// </summary>
    public class TimeoutWrapper : IDisposable
    {
        private readonly object syncLock = new object();
        private Thread actionThread;
        private readonly int timeoutSeconds;
        
        // these fields are protected by the lock
        private Action actionToWrap = null;
        private bool actionThreadReady = false;
        private bool actionHasTimedOut = false;
        private bool disposed = false;

        public TimeoutWrapper(int timeoutSeconds)
        {
            this.timeoutSeconds = timeoutSeconds;
        }

        public void Initialize()
        {
            actionThread = new Thread(ActionThreadLoop);
            actionThread.Start();
        }

        public void Dispose()
        {
            bool joinActionThread = false;
            
            lock (syncLock)
            {
                // already disposed -> do nothing
                if (disposed)
                    return;
                
                disposed = true;

                // if the action thread is idle, we will join on it
                if (!actionHasTimedOut && actionToWrap == null)
                {
                    joinActionThread = true;
                    
                    // wake up the action thread
                    Monitor.Pulse(syncLock);
                }
            }

            if (joinActionThread)
                actionThread?.Join();
        }

        /// <summary>
        /// Runs a task and returns true on timeout
        /// </summary>
        /// <param name="action"></param>
        /// <returns></returns>
        public bool Run(Action action)
        {
            lock (syncLock)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(TimeoutWrapper));
                
                if (actionToWrap != null)
                    throw new InvalidOperationException(
                        "Cannot use this method concurrently."
                    );
                
                // wait for the action thread to get ready
                while (!actionThreadReady)
                    Monitor.Wait(syncLock);
                
                // set the action to be executed
                actionToWrap = action;
                DateTime timeOutAt = DateTime.Now.AddSeconds(timeoutSeconds);
                
                // wake up the action thread
                Monitor.Pulse(syncLock);

                // wait for the action to be completed,
                // or the timeout to fire
                while (actionToWrap != null)
                {
                    // calculate for how long to wait until timeout
                    int millisecondsToWait = (int) (timeOutAt - DateTime.Now).TotalMilliseconds;

                    // already past timeout
                    if (millisecondsToWait <= 0)
                    {
                        actionHasTimedOut = true;
                        break;
                    }
                    
                    // wait for that amount
                    bool finishedBeforeTimeout = Monitor.Wait(
                        syncLock, millisecondsToWait
                    );

                    // we've timed out on the wait
                    if (!finishedBeforeTimeout)
                    {
                        actionHasTimedOut = true;
                        break;
                    }
                }
            }

            return actionHasTimedOut;
        }

        private void ActionThreadLoop()
        {
            while (true)
            {
                // local variable since we cannot access actionToWrap
                // outside a lock and we cannot execute it while locked
                Action act;

                lock (syncLock)
                {
                    // wait for being woken up by someone calling Handle(...)
                    actionThreadReady = true;
                    Monitor.Pulse(syncLock); // notify main thread we're ready
                    Monitor.Wait(syncLock); // wait to be woken up
                    actionThreadReady = false;
                    
                    // disposed, end the thread
                    if (disposed)
                        break;

                    // nothing to run, return to waiting
                    if (actionToWrap == null)
                        continue;
                    
                    // we have an action to run!
                    act = actionToWrap;
                }
                
                // run the action
                act?.Invoke();

                lock (syncLock)
                {
                    // action is completed
                    actionToWrap = null;
                    
                    // wake up the waiting thread
                    Monitor.Pulse(syncLock);
                }
            }
        }
    }
}