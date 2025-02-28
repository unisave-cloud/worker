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
        ) : base("dummy")
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

        protected override void LoadBackend()
        {
            // do nothing
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
        public async Task ItInvokesInitializationMethod()
        {
            bool wasInvoked = false;
            
            var waitForStartTcs = new TaskCompletionSource<bool>();
            var initializer = new DummyInitializer(
                (url, ct) =>
                {
                    Assert.AreEqual("http://recipe.url", url);
                    Assert.IsFalse(ct.IsCancellationRequested);
                    wasInvoked = true;
                    waitForStartTcs.SetResult(true);
                    return Task.CompletedTask;
                }
            );
            
            Assert.IsFalse(wasInvoked);
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            await waitForStartTcs.Task;
            Assert.IsTrue(wasInvoked);
        }
        
        [Test]
        public async Task ItCreatesBackendFolderBeforeInitialization()
        {
            var doneTcs = new TaskCompletionSource<bool>();
            
            var initializer = new DummyInitializer(
                (url, ct) =>
                {
                    Assert.IsTrue(Directory.Exists(BackendFolderPath));
                    Assert.IsEmpty(Directory.GetDirectories(BackendFolderPath));
                    Assert.IsEmpty(Directory.GetFiles(BackendFolderPath));
                    doneTcs.SetResult(true);
                    return Task.CompletedTask;
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            
            await doneTcs.Task;
        }
        
        [Test]
        public async Task ItClearsBackendFolderBeforeInitialization()
        {
            var doneTcs = new TaskCompletionSource<bool>();
            
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
                    doneTcs.SetResult(true);
                    return Task.CompletedTask;
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            
            await doneTcs.Task;
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
            var tcs = new TaskCompletionSource<bool>();
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
            tcs.SetResult(true);
            waitingTask.Wait(1000);
            
            Assert.IsTrue(waitingTask.IsCompleted);
        }

        [Test]
        public void WaitingForInitializationCanBeCancelled()
        {
            var tcs = new TaskCompletionSource<bool>();
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
            Assert.CatchAsync<OperationCanceledException>(async () => {
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
            var tcs = new TaskCompletionSource<bool>();
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
            tcs.SetResult(true);
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
            var tcs = new TaskCompletionSource<bool>();
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
            tcs.SetResult(true);

            Assert.ThrowsAsync<InitializationFailedException>(async () => {
                await waitingTask;
            });
        }

        [Test]
        public void AfterFailedInitializationIsUninitialized()
        {
            var tcs = new TaskCompletionSource<bool>();
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
            tcs.SetResult(true);
            waitingTask.ContinueWith(_ => { }).Wait();
            // NOTE: ContinueWith here swallows the exception
            
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
            var tcs = new TaskCompletionSource<bool>();
            var waitForStartTcs = new TaskCompletionSource<bool>();
            var initializer = new DummyInitializer(
                async (url, ct) => {
                    Assert.IsFalse(ct.IsCancellationRequested);
                    waitForStartTcs.SetResult(true);
                    await tcs.Task;
                    Assert.IsTrue(ct.IsCancellationRequested);
                    ct.ThrowIfCancellationRequested();
                }
            );
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            await waitForStartTcs.Task;
            
            // cancel initialization
            initializer.Dispose();
            
            // start waiting
            Task waitingTask = initializer.WaitForFinishedInitialization(
                CancellationToken.None
            );
            
            // let the initialization action complete
            tcs.SetResult(true);

            bool cancelledException = false;
            try
            {
                await waitingTask;
            }
            catch (OperationCanceledException)
            {
                cancelledException = true;
            }
            catch (Exception)
            {
                Assert.Fail();
            }
            
            Assert.IsTrue(cancelledException);
        }

        [Test]
        public async Task InitializationStatePropertyUpdatesProperly()
        {
            var tcs = new TaskCompletionSource<bool>();
            var waitForStartTcs = new TaskCompletionSource<bool>();
            var initializer = new DummyInitializer(
                async (url, ct) =>
                {
                    waitForStartTcs.SetResult(true);
                    await tcs.Task;
                }
            );
            
            Assert.AreEqual(InitializationState.NonInitialized, initializer.State);
            
            initializer.TriggerInitializationIfNotRunning("http://recipe.url");
            await waitForStartTcs.Task;
            
            Assert.AreEqual(InitializationState.BeingInitialized, initializer.State);
            
            tcs.SetResult(true);
            await initializer.WaitForFinishedInitialization(CancellationToken.None);
            
            Assert.AreEqual(InitializationState.Initialized, initializer.State);
        }
    }
}