# Configuration

The worker process can be configured via environment variables:

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
