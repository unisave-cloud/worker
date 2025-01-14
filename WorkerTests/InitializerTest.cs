using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnisaveWorker.Initialization;

namespace WorkerTests
{
    /// <summary>
    /// Dummy initializer that can be used to test the Initializer base class
    /// </summary>
    internal class DummyInitializer : Initializer
    {
        private readonly Func<string, CancellationToken, Task> initializationLambda;

        public DummyInitializer(
            Func<string, CancellationToken, Task> initializationLambda
        )
        {
            this.initializationLambda = initializationLambda;
        }

        protected override async Task PerformInitialization(
            string recipeUrl,
            CancellationToken cancellationToken
        )
        {
            await initializationLambda.Invoke(recipeUrl, cancellationToken);
        }
    }
    
    [TestFixture]
    public class InitializerTest
    {
        private const string BackendFolderPath = "backend";
        
        [SetUp]
        public void SetUp()
        {
            // clear up any mess left over from previous tests
            if (Directory.Exists(BackendFolderPath))
                Directory.Delete(BackendFolderPath, recursive: true);
        }
        
        [Test]
        public void ItInvokesInitializationMethod()
        {
            bool wasInvoked = false;
            
            var initializer = new DummyInitializer(
                (url, ct) =>
                {
                    Assert.AreEqual("http://recipe.url", url);
                    Assert.IsFalse(ct.IsCancellationRequested);
                    wasInvoked = true;
                    return Task.CompletedTask;
                }
            );
            
            Assert.IsFalse(wasInvoked);
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            Assert.IsTrue(wasInvoked);
        }
        
        [Test]
        public void ItCreatesBackendFolderBeforeInitialization()
        {
            var initializer = new DummyInitializer(
                (url, ct) =>
                {
                    Assert.IsTrue(Directory.Exists(BackendFolderPath));
                    Assert.IsEmpty(Directory.GetDirectories(BackendFolderPath));
                    Assert.IsEmpty(Directory.GetFiles(BackendFolderPath));
                    return Task.CompletedTask;
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
        }
        
        [Test]
        public void ItClearsBackendFolderBeforeInitialization()
        {
            // create some mess
            Directory.CreateDirectory(BackendFolderPath);
            string fooFilePath = Path.Combine(BackendFolderPath, "foo.cs");
            File.WriteAllText(fooFilePath, "Some dummy content.");
            Assert.IsTrue(File.Exists(fooFilePath));
            
            var initializer = new DummyInitializer(
                (url, ct) =>
                {
                    Assert.IsTrue(Directory.Exists(BackendFolderPath));
                    Assert.IsFalse(File.Exists(fooFilePath));
                    return Task.CompletedTask;
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
        }

        [Test]
        public void WaitingIfUninitializedShouldThrow()
        {
            var initializer = new DummyInitializer(
                (url, ct) => Task.CompletedTask
            );

            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await initializer.WaitForFinishedInitialization(
                    CancellationToken.None
                );
            });
        }

        [Test]
        public void YouCanWaitForInitializationToComplete()
        {
            var tcs = new TaskCompletionSource<object>();
            var initializer = new DummyInitializer(
                (url, ct) => tcs.Task
            );
            
            // start initialization
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");

            // start waiting
            Task waitingTask = initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
            
            Assert.IsFalse(waitingTask.IsCompleted);
            
            // finish waiting
            tcs.SetResult(null);
            waitingTask.Wait(1000);
            
            Assert.IsTrue(waitingTask.IsCompleted);
        }

        [Test]
        public void WaitingForInitializationCanBeCancelled()
        {
            var tcs = new TaskCompletionSource<object>();
            var initializer = new DummyInitializer(
                (url, ct) => tcs.Task
            );
            
            // start initialization
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");

            // start waiting
            CancellationTokenSource cts = new CancellationTokenSource();
            Task waitingTask = initializer.WaitForFinishedInitialization(
                cts.Token
            );
            
            // cancel waiting
            cts.Cancel();
            
            // expect cancellation exception when awaited
            Assert.ThrowsAsync<OperationCanceledException>(async () => {
                await waitingTask;
            });
            
            Assert.IsTrue(waitingTask.IsCanceled);
        }

        [Test]
        public async Task WaitingWhenInitializedReturnsImmediately()
        {
            var initializer = new DummyInitializer(
                (url, ct) => Task.CompletedTask
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            await initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
            
            // now it's initialized, if we wait again, we should finish fine
            Assert.DoesNotThrowAsync(async () => {
                await initializer.WaitForFinishedInitialization(
                    CancellationToken.None
                );
            });
        }

        [Test]
        public async Task TriggeringInitializationWhenInitializingDoesNothing()
        {
            int callCount = 0;
            var tcs = new TaskCompletionSource<object>();
            var initializer = new DummyInitializer(
                (url, ct) => {
                    callCount++;
                    return tcs.Task;
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            
            // check the states
            Assert.AreEqual(InitializationState.BeingInitialized, initializer.State);
            
            // we can re-trigger and nothing crazy happens
            Assert.DoesNotThrow(() => {
                initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            });

            // and once initialization finishes, it was invoked only once
            tcs.SetResult(null);
            await initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
            Assert.AreEqual(1, callCount);
        }

        [Test]
        public async Task TriggeringInitializationWhenInitializedDoesNothing()
        {
            int callCount = 0;
            var initializer = new DummyInitializer(
                (url, ct) => {
                    callCount++;
                    return Task.CompletedTask;
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            await initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
            
            // check the state
            Assert.AreEqual(InitializationState.Initialized, initializer.State);
            
            // we can re-trigger and nothing crazy happens
            Assert.DoesNotThrow(() => {
                initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            });
            await initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
            Assert.AreEqual(1, callCount);
        }
        
        [Test]
        public void WaitingFailedInitializationShouldThrow()
        {
            var tcs = new TaskCompletionSource<object>();
            var initializer = new DummyInitializer(
                async (url, ct) => {
                    await tcs.Task;
                    throw new ArithmeticException();
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            Task waitingTask = initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
            
            // fail the initialization
            tcs.SetResult(null);

            Assert.ThrowsAsync<InitializationFailedException>(async () => {
                await waitingTask;
            });
        }

        [Test]
        public void AfterFailedInitializationIsUninitialized()
        {
            var tcs = new TaskCompletionSource<object>();
            var initializer = new DummyInitializer(
                async (url, ct) => {
                    await tcs.Task;
                    throw new ArithmeticException();
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            Task waitingTask = initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
            tcs.SetResult(null);
            waitingTask.Wait();
            
            // now, try waiting again and we should be uninitialized
            Assert.AreEqual(InitializationState.NonInitialized, initializer.State);
            Assert.ThrowsAsync<InvalidOperationException>(async () => {
                await initializer.WaitForFinishedInitialization(
                    CancellationToken.None
                );
            });
        }

        [Test]
        public async Task InitializationCanBeCancelled()
        {
            var tcs = new TaskCompletionSource<object>();
            var initializer = new DummyInitializer(
                async (url, ct) => {
                    Assert.IsFalse(ct.IsCancellationRequested);
                    await tcs.Task;
                    Assert.IsTrue(ct.IsCancellationRequested);
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            
            // cancel initialization
            initializer.Dispose();
            
            // let the initialization action complete
            tcs.SetResult(null);
            await initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
        }

        [Test]
        public async Task InitializationStatePropertyUpdatesProperly()
        {
            var tcs = new TaskCompletionSource<object>();
            var waitForStartTcs = new TaskCompletionSource<object>();
            var initializer = new DummyInitializer(
                async (url, ct) =>
                {
                    waitForStartTcs.SetResult(null);
                    await tcs.Task;
                }
            );
            
            Assert.AreEqual(InitializationState.NonInitialized, initializer.State);
            
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            await waitForStartTcs.Task;
            
            Assert.AreEqual(InitializationState.BeingInitialized, initializer.State);
            
            tcs.SetResult(null);
            await initializer.WaitForFinishedInitialization(CancellationToken.None);
            
            Assert.AreEqual(InitializationState.Initialized, initializer.State);
        }
    }
}