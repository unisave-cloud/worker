# Concurrency

Entrypoint into the worker is the [OwinHttpListener](https://github.com/evicertia/Katana/blob/master/src/Microsoft.Owin.Host.HttpListener/OwinHttpListener.cs) from the `Microsoft.Owin.Host` package.

It defines a queue of 1000 requests and limits on concurrently handled requests (see the source code). However this should not be the mechanism by which we limit the facet call concurrency (since the HTTP server is also used for metrics and diagnostics). But keep in mind that it introduces limits that may override the facet request limits, should they be set too generously.

The rest of the worker is built with the assumption, that every incomming HTTP request starts a new `Task` that gets placed into the default .NET thread pool. The default thread count (at least what I observed from unit tests) seems to be 10, although from stress tests it seems it gets increased if they are all utilized heavily. So we can roughly assume that each new request gets its own `Task` and likely also its own thread.

> **TL;DR;** The worker OWIN HTTP server is multi-threaded and asynchronous with one `Task` for each request.


## Unisave request concurrency limiting

The `UnisaveWorker.Concurrency` namespace contains middleware and types used to control the concurrency level for unisave requests.

The `ConcurrencyManagementMiddleware` is responsible for resolving the desired concurrency limits (and queue lengths), and applying the following two middlewares to requests. The configuration comes from the unisave request's environment variables (which are assumed to be stable across the lifetime of the worker) or falls back on the configuration of the worker itself. The default worker configuration can be different for legacy Unisave Framework version (v0.11.0 and before).

The environment variables loaded from the game's environment are:

- `WORKER_REQUEST_CONCURRENCY` limits concurrent requests, `null` means unlimited, if missing, fallback to worker defaults
- `WORKER_USE_SINGLE_THREAD` limits requests execution to just a single thread, if missing, fallback to worker defaults
- `WORKER_MAX_QUEUE_LENGTH` defines max queued unisave requests limit, if missing, fallback to worker defaults

Fallback values from the worker's environemnt variables are:

- `WORKER_DEFAULT_REQUEST_CONCURRENCY` limits concurrent requests, if missing, do not limit (skip the middleware)
- `WORKER_DEFAULT_USE_SINGLE_THREAD` limits requests execution to just a single thread, if missing, do not limit (skip the middleware)
- `WORKER_DEFAULT_MAX_QUEUE_LENGTH` defines max queued unisave requests limit, if missing, use default (see [configuration](configuration.md))

Worker defaults are overriden for legacy Unisave Framework versions (v0.11.0 and older) to run only a single request at a time, since the framework was built in that way. However unisave envrionment variables can still override these.

Configuration is parsed and combined from all these places in `ConcurrencyManagementMiddleware` in the `ExtractDesiredSettings` private method.


### Request concurrency

The `RequestConcurrencyMiddleware` limits how many (unisave) requests are handled at the same time (i.e. facet calls). Any additional requests are left pending inside the middleware (awaiting a dummy `TaskCompletionSource`), until some older request finishes.

Pending requests wait in a queue, which has a maximum size. Its size is controlled by the `*_MAX_QUEUE_LENGTH` environment variables - see above. When the queue overflows, requests are rejeted with the 429 status.


### Single-threaded loop middleware

The `LoopMiddleware` defines a single thread that executes .NET tasks in an infinite loop. It is used when the `*_USE_SINGLE_THREAD` environment variables resolve to `true`. When applied, you essentially get the python/javascript/unity single threaded asynchronous behaviour.

This limiting is performed by running the sub-tasks on a custom scheduler (the `LoopScheduler`), that manages the loop thread. Because the loop thread should be gracefully stopped and disposed, the loop scheduler is a top-level `WorkerApplication` service whose lifecycle must be managed.

The `LoopScheduler` is based on the (now not-used) `LimitedConcurrencyLevelTaskScheduler` in the `Microsoft` sub-namespace. That scheduler is taken from [this StackOverflow question](https://stackoverflow.com/questions/69222176/run-valuetasks-on-a-custom-thread-pool) which references the [scheduler code sample from MSDN](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler). Its code is left in the worker reposiory as a reference for how to build custom TPL schedulers.

When tasks await sub-tasks, the implicit behaviour is `.ConfigureAwait(true)`, meaning the sub-task will use the same scheduler and synchronization context as the current one, meaning the bevaiour will persist throughout the entire request processing logic (unless someone breaks it intentionally).

To learn more about schedulers, synchronization contexts, and `ConfigureAwait`, read [this Microsoft blog post](https://devblogs.microsoft.com/dotnet/configureawait-faq/).


## Unisave framework concurrency

The Unisave Framework up until the version v0.11.0 was built to execute on the standard .NET thread pool in a one-request-at-a-time manner, with synchronous IO calls everywhere. While using waiting for asynchronous tasks (e.g. from the `HttpListener`) it would just do a blocking wait via `.GetAwaiter().GetResult()`.

The framework is (will be) updated since the version v0.11.1 to support full multi-threadedness and true asynchronous concurrency. Therefore it should support running under all concurrency settings. These new regimes are described in more details here: https://unisave.cloud/docs/interfaces#asynchronicity--multi-threading


## Deadlocks

Read the [Deadlocks](deadlocks.md) documentation page to understand the (unexpected) pitfalls of concurrency settings.


## How concurrency defaults were set

**Q:** Why not have unlimited request concurrency?
- Because a spike in requests could unnecessarily bog down the worker. The worker is not going to be able to handle all the requests anyways, so it's better to finish some and reject others, instead of starting all, then freezing, crashing, or responding late.
- Because there already is a limit of 1K built into the server and exhausting that will cause the worker system supervisor to freak out, since the metrics endpoints will stop working, prometheus metrics will stop working, etc...

**Q:** How many requests to allow simultaneously?

- We want as much as possible, so that worker does not unnecessarily slow down while processing them.
- With the 250MB RAM worker size, 10 concurrent requests seem ok based on the load tests. Almost no slow down and plenty of concurrency.

**Q:** What should the request queue length be?

- Zero is too few, we get unnecessarily bounces just from the randomness of request arrivals.
- If it is too much, requests will time out on the gateway, while still waiting in the queue under spiked load.
- With the 250MB RAM worker size, we have around 500ms per request on average, and 10s reasonable max waiting time for a player to get a response, which gives us 20 requests. It's a rough calculation, but it gives the order of magnitude we want.

**Q:** Do we run single-threaded?

- Yes, because game developers don't need to use locks and worry about race conditions.
- Yes, because the worker is CPU-limited and more threads would just steal compute time from each other (although it's not as bad as you'd think, backend processing is a lot of waiting for the database and even with 10 threads, there was not much of a slow down).
- HOWEVER: Be wary about blocking thread-waits for asynchronous tasks, it can cause deadlocks (javascript does not have this issue, FYI). See the [Deadlocks](deadlocks.md) documentation page for more.
- We allow for multi-threaded one-request execution to support legacy Unisave Frameworks.
- We allow for multi-threaded multi-request execution to "patch" synchronous HTTP requests made by the game backend code, while preventing one such request from stalling the worker.
