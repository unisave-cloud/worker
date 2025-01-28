# Configuration

The worker process can be configured via environment variables:

> **Note:** To see the default values, see the defaults in `Config.cs`.

| Variable                             | Description |
|--------------------------------------|-------------|
| `WORKER_HTTP_URL`                    | What IP and Port to listen on with the HTTP server, e.g. `http://*:8080` |
| `INITIALIZATION_RECIPE_URL`          | URL for downloading initialization recipe, empty for eager workers |
| `WORKER_ENVIRONMENT_ID`              | Environment ID of the worker pool, may be empty (must be for eager pools). Used for prometheus metrics. |
| `WORKER_BACKEND_ID`                  | Backend ID of the worker pool, may be empty (must be for eager pools). Used for prometheus metrics. |
| `WORKER_DEFAULT_REQUEST_CONCURRENCY` | Limits concurrent requests, if `null`, do not limit |
| `WORKER_DEFAULT_USE_SINGLE_THREAD`   | Whether a single loop thread should be used by default to process requests |
| `WORKER_DEFAULT_MAX_QUEUE_LENGTH`    | Maximum number of waiting Unisave requests |
| `WORKER_OWIN_STARTUP_ATTRIBUTE`      | Name of the `OwinStartup` attribute to look for when loading the game backend's OWIN startup class. See [the API docs](api-game-backend.md) for more info. |
| `WORKER_GRACEFUL_SHUTDOWN_SECONDS`   | Number of seconds to wait at the longest for pending requests to finish when shutting down. |
| `WORKER_LOOP_DEADLOCK_TIMEOUT_SECONDS` | For how much time can the loop thread afford to work on a single task, before we consider the thread to be deadlocked. |
| `WORKER_UNHEALTHY_MEMORY_USAGE_BYTES` | How many bytes of memory usage are considered to be unhealthy and cause the worker to switch to the unhealthy state and be restarted. This prevents memory leaks from causing OOM crashes. |
