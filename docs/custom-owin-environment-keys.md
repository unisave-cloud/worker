# Custom OWIN Environment Keys

This is a list of custom OWIN keys set for each environment (each Unisave request) by the worker process.

- `worker.RequestIndex` Zero-based index of the Unisave request, as received and enqueued. Type `int`. Assigned by `AccessLoggingMiddleware`.
- `worker.EnvDict` Environment variables that should apply for this request. Type `Dictionary<string, string>`. Assigned by `LegacyApiTranslationMiddleware`. [Learn more](unisave-environment-variables.md).
- `worker.EnvString` Environment variables as string that should apply for this request. Type `string`. Assigned by `LegacyApiTranslationMiddleware`. [Learn more](unisave-environment-variables.md).
- `worker.ExecutionDurationSeconds` Execution duration of the finished request in seconds. Type `double`. Assigned by `ExecutionTimingMiddleware`. Used by the `LegacyApiTranslationMiddleware` and `AccessLoggingMiddleware`.
