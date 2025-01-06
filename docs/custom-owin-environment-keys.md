# Custom OWIN Environment Keys

This is a list of custom OWIN keys set for each environment (each Unisave request) by the worker process.

- `worker.RequestIndex` Zero-based index of the Unisave request, as received and enqueued. Type `int`. Assigned by `AccessLoggingMiddleware`.


## Legacy facet API

Values that are present only because of the `LegacyApiTranslationMiddleware`:

- `worker.ExecutionDuration` Execution duration of the finished request in seconds. Type `double`. Assigned by `AccessLoggingMiddleware`. Later used by the translation middleware.
- `worker.Env` Environment variables sent with the legacy facet call. Type `Dictionary<string, string>`. Assigned by `LegacyApiTranslationMiddleware`.
- `worker.EnvString` Environment variables as string. Type `string`. Assigned by `LegacyApiTranslationMiddleware`.
