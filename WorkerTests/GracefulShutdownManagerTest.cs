using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnisaveWorker.Ingress;

namespace WorkerTests
{
    [TestFixture]
    public class GracefulShutdownManagerTest
    {
        [Test]
        public void ItReturnsImmediatelyWithoutPendingRequests()
        {
            var manager = new GracefulShutdownManager();
            
            // request can enter and exist
            Assert.IsTrue(manager.OnRequestEnter());
            manager.OnRequestExit();
            
            // then the stopping is immediate
            Stopwatch sw = Stopwatch.StartNew();
            bool success = manager.PerformGracefulShutdown(
                TimeSpan.FromSeconds(10)
            );
            sw.Stop();
            
            // we did not wait at all
            Assert.IsTrue(success);
            Assert.IsTrue(sw.ElapsedMilliseconds <= 100);
            
            // now no requests can enter
            Assert.IsFalse(manager.OnRequestEnter());
        }

        [Test]
        public void ItReturnsWhenPendingRequestFinishes()
        {
            // manager with one request in
            var manager = new GracefulShutdownManager();
            manager.OnRequestEnter();
            
            // finish the request in 100ms
            Task.Run(() => {
                Thread.Sleep(100);
                manager.OnRequestExit();
            });
            
            // start waiting
            Stopwatch sw = Stopwatch.StartNew();
            bool success = manager.PerformGracefulShutdown(
                TimeSpan.FromSeconds(10)
            );
            sw.Stop();
            
            // we must have waited for the request
            Assert.IsTrue(success);
            Assert.IsTrue(sw.ElapsedMilliseconds >= 100);
        }

        [Test]
        public void ItReturnsWhenTimeoutFinishes()
        {
            // manager with one request in
            var manager = new GracefulShutdownManager();
            manager.OnRequestEnter();
            
            // start waiting
            Stopwatch sw = Stopwatch.StartNew();
            bool success = manager.PerformGracefulShutdown(
                TimeSpan.FromMilliseconds(100)
            );
            sw.Stop();
            
            // we must have waited for the timeout
            Assert.IsFalse(success);
            Assert.IsTrue(sw.ElapsedMilliseconds >= 100);
        }
    }
}