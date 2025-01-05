# Custom OWIN Environment Keys

This is a list of custom OWIN keys set for each environment (each Unisave request) by the worker process.

- `worker.RequestIndex` Zero-based index of the Unisave request, as received and enqueued. Type `int`. Assigned by `AccessLoggingMiddleware`.


## Legacy facet API

Temporary values extracted for the legacy facet invocation API by `LegacyApiParsingMiddleware`:

- `worker.FacetClass` Name of the facet class to invoke. Type `string`. Assigned by `LegacyApiParsingMiddleware`.
- `worker.FacetMethod` Name of the facet method to invoke. Type `string`. Assigned by `LegacyApiParsingMiddleware`.
- `worker.Env` Environment variables sent with the legacy facet call. Type `Dictionary<string, string>`. Assigned by `LegacyApiParsingMiddleware`.
