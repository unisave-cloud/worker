# Deadlocks

This documentation details the concerns about using the `LoopMiddleware` (i.e. using only one thread for request processing).

We want to use a single thread to process multiple requests simltaneously and asynchronously to get the best of both worlds - have thread safety of the single-threaded environment and have concurrency of the asynchronous environment. The same is done in the javascript's event loop. Javascript does not need locks, it uses only one thread, yet node.js can process multiple requests in parallel with high throughput thanks to `async`-`await`.

The problem is, that javascript doesn't give the tools to control threads directly, whereas .NET does. And this can cause trouble...


## Don't use `.GetAwaiter().GetResult()` or `.Wait()` on tasks

The `.GetResult()` method of a tasks makes the current thread fall asleep, until the queried task finishes. But if we run single-threaded, that target task will be scheduled to the same task queue, that the current thread is currently occupying. Therefore it waits for itself - it deadlocks.

The tricky bit is that is does not happen always - TPL has task execution inlining optimizations that cause the worker to survive most of these calls. But once in about 800 requests, the worker freezes with a deadlock.

If you want to synchronously wait for another task from the main thread (which is done in some places in the Unisave Framework), then you have schedule that other task onto the .NET thread pool (so that it gets executed by some other thread, than the one we are currently occupying). It can be achieved like this:

```csharp
Task.Run(SomeIoOperation).GetAwaiter().GetResult();
```

The `Task.Run` makes sure the task is scheduled on the default thread pool. Although a better option would be to just use `await` and let the current thread return back to the scheduler to acually run the awaited task (though that's not always possible).


## Deadlock recovery code

The `LoopScheduler` has a built-in deadlock recovery mechanism to resolve the deadlock if it happens. It checks the time it taskes to finish a TPL task and if it exceeds the configured time, it proclaims a deadlock.

The recovery works by discarding the current loop thread and starting a new one. The old thread is left to finish its execution and terminate in case the new thread caused the old one to get unstuck.

This mechanism is meant to prevent the worker from being permanently stuck - only being stuck temporarily. But it will be stuck nonetheless and requests will get dropped. Therefore if you observe deadlocks being fired, start investigating your code looking for possible places, like the ones in this document and fix them.

If that is not an option, you can:

- use the multi-threaded, multi-request execution model if your code supports multi-threaded-ness
- or use the multi-threade, single-request execution model otherwise (though you risk worker stalls on long-running requests)
