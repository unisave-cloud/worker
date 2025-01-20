# Configuration

The worker process can be configured via environment variables:

> **Note:** To see the default values, see the defaults in `Config.cs`.

| Variable                         | Description |
|----------------------------------|-------------|
| `WATCHDOG_SERVER_PORT`           | Port to have the HTTP server listen on |
| `MAX_QUEUE_LENGTH`               | Maximum length of the request queue |
| `REQUEST_TIMEOUT_SECONDS`        | Execution time limit for requests |
| `INITIALIZATION_RECIPE_URL`      | URL for downloading initialization recipe, empty for eager workers |
| `WATCHDOG_DUMMY_INITIALIZATION`  | `true` if the watchdog should perform dummy initialization |
| `WORKER_ENVIRONMENT_ID`          | Environment ID of the worker pool, may be empty (must be for eager pools) |
| `WORKER_BACKEND_ID`              | Backend ID of the worker pool, may be empty (must be for eager pools) |
| `VERBOSE_HTTP_SERVER`            | When `true`, the HTTP server will print additional information |
| `WORKER_DEFAULT_REQUEST_CONCURRENCY` | Limits concurrent requests, if missing, do not limit |
| `WORKER_DEFAULT_THREAD_CONCURRENCY` | Limits concurrent threads, if missing, do not limit |
| `WORKER_DEFAULT_MAX_QUEUE_LENGTH` | Maximum number of waiting Unisave requests |
| `WORKER_OWIN_STARTUP_ATTRIBUTE`  | Name of the `OwinStartup` attribute to look for when loading the game backend's OWIN startup class. See [the API docs](api-game-backend.md) for more info. |
| `WORKER_GRACEFUL_SHUTDOWN_SECONDS` | Number of seconds to wait at the longest for pending requests to finish when shutting down. |
