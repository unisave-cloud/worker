# Concurrency

Entrypoint into the worker is the [OwinHttpListener](https://github.com/evicertia/Katana/blob/master/src/Microsoft.Owin.Host.HttpListener/OwinHttpListener.cs) from the `Microsoft.Owin.Host` package.

It defines a queue of 1000 requests and limits on concurrently handled requests (see the source code). However this should not be the mechanism by which we limit the facet call concurrency (since the HTTP server is also used for metrics and diagnostics). But keep in mind that it introduces limits that may override the facet request limits, should they be set too generously.

The rest of the worker is built with the assumption, that every incomming HTTP requests starts a new `Task` that gets placed into the default .NET thread pool. The default thread count (at least what I observed from unit tests) seems to be 10, although from stress tests it seems it gets increased if they are all utilized heavily. So we can roughly assume that each new request gets its own `Task` and likely also its own thread.

> **TL;DR;** The worker OWIN HTTP server is multi-threaded and asynchronous with one `Task` for each request.


## Unisave request concurrency limiting

The `UnisaveWorker.Concurrency` namespace contains middleware and types used to control the concurrency level for unisave requests.

The `ConcurrencyManagementMiddleware` is responsible for resolving the desired concurrency limits (and queue lengths), and applying the following two middlewares to requests. The configuration comes from the unisave request's environment variables (which are assumed to be stable across the lifetime of the worker) or falls back on the configuration of the worker itself.

The environment variables loaded from the game's environment are:

- `WORKER_REQUEST_CONCURRENCY` limits concurrent requests, if missing, fallback
- `WORKER_THREAD_CONCURRENCY` limits concurrent threads, if missing, fallback
- `WORKER_MAX_QUEUE_LENGTH` defines max queued unisave requests limit, if missing, fallback

Fallback values from the worker's environemnt variables are:

- `WORKER_DEFAULT_REQUEST_CONCURRENCY` limits concurrent requests, if missing, do not limit (skip the middleware)
- `WORKER_DEFAULT_THREAD_CONCURRENCY` limits concurrent threads, if missing, do not limit (skip the middleware)
- `WORKER_DEFAULT_MAX_QUEUE_LENGTH` defines max queued unisave requests limit, if missing, use default (see [configuration](configuration.md))


### Request concurrency

The `RequestConcurrencyMiddleware` limits how many (unisave) requests are handled at the same time (i.e. facet calls). Any additional requests are left pending inside the middleware (awaiting a dummy `TaskCompletionSource`), until some older request finishes.

Pending requests wait in a queue, which has a maximum size. Its size is controlled by the `*_MAX_QUEUE_LENGTH` environment variables - see above. When the queue overflows, requests are rejeted with the 429 status.


### Thread concurrency

The `ThreadConcurrencyMiddleware` limits how many threads are used to process requests. You typically either want to limit to 1, or not to limit at all. When limiting to 1, you essentially get the python/javascript/unity single threaded asynchronous behaviour.

This limiting is performed by running the sub-tasks on a custom scheduler (the `LimitedConcurrencyLevelTaskScheduler`), that tracks used threads.

The scheduler is takend from [this StackOverflow question](https://stackoverflow.com/questions/69222176/run-valuetasks-on-a-custom-thread-pool) which references the [scheduler code sample from MSDN](https://learn.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskscheduler).

When tasks await sub-tasks, the implicit behaviour is `.ConfigureAwait(true)`, meaning the sub-task will use the same scheduler and synchronization context as the current one, meaning the bevaiour will persist throughout the entire request processing logic (unless someone breaks it intentionally).

To learn more about schedulers, synchronization contexts, and `ConfigureAwait`, read [this Microsoft blog post](https://devblogs.microsoft.com/dotnet/configureawait-faq/).


## Unisave framework concurrency

TODO: the old one sucks, we work around (or add a middleware)? The new one works, see: https://unisave.cloud/docs/interfaces#asynchronicity--multi-threading
