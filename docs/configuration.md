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
| `WORKER_DEFAULT_THREAD_CONCURRENCY`  | Limits concurrent threads, if `null`, do not limit |
| `WORKER_DEFAULT_MAX_QUEUE_LENGTH`    | Maximum number of waiting Unisave requests |
| `WORKER_OWIN_STARTUP_ATTRIBUTE`      | Name of the `OwinStartup` attribute to look for when loading the game backend's OWIN startup class. See [the API docs](api-game-backend.md) for more info. |
| `WORKER_GRACEFUL_SHUTDOWN_SECONDS`   | Number of seconds to wait at the longest for pending requests to finish when shutting down. |
