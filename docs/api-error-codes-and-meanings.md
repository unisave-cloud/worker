# API: Error Codes and Meanings

When an error occurs during the operation of the Unisave Worker it responds with a JSON error message in this format:

```
HTTP/1.1 429 Too Many Requests
...

{
    "statusCode": 429,
    "error": true,
    "errorNumber": 2000,
    "errorMessage": "Worker queue in the RequestConcurrencyMiddleware is full."
}
```

The **status code** corresponds the HTTP status code and indicates in the HTTP-semantincs what is the problem.

The **error number** is specific to Unisave Worker and a list of numbers and their meaning is described in this document.

The **error message** is a short, human-readable description of the error.

When consuming errors, you should check agains the **error number**, not the message, as the message is not guaranteed to be permanent.

> **Note:** This design is inspired by ArangoDB's error responses.

In order to disambiguate a worker error from a regular game backend HTTP response, you can check that the response header `X-Unisave-Worker-Error: true` is present. But a more soft check for some non-main-path endpoints could be just looking for the `errorNumber` field in the output JSON together with the non-200 status code.

The error response can be sent via the `OwinExtensions` extension method on the OWIN context:

```cs
await context.SendError(
    statusCode: 429,
    errorNumber: 2000,
    "Worker queue in the RequestConcurrencyMiddleware is full."
);
```


## List of error numbers


### General errors

#### 1 - Unhandled exception
Raised when the worker crashes unexpectedly, which results in a `500 Internal Server Error` response with this error code. The exception that caused the crash will be logged into the worker's standard output.


### Ingress processing errors (1xxx)

#### 1000 - Shutting down
When the worker is gracefully shutting down, any new requests are rejected with this error and status `503 Service Unavailable`.


### Concurrency control errors (2xxx)

#### 2000 - Full worker queue
When the worker is overwhelmed and cannot accept any more requests, it responds with `429 Too Many Requests` and this error. See the `GracefulShutdownMiddleware`.


### Initialization errors (3xxx)

#### 3000 - Missing initialization recipe URL
The initialization recipe URL in the request header `X-Unisave-Initialization-Recipe-Url` is used only when the worker has not yet been initialized. Otherwise it gets ignored by the worker (and thus can be missing). However, if the initialization recipe URL is needed but is missing, a `409 Conflict` response is sent with this error. See the `InitializationMiddleware` class.

#### 3001 - Worker initialization failed
When the worker tried to initialize but the initialization process failed, requests that wait for the initialization to finish will be rejected with this error. See the `InitializationMiddleware` class. HTTP status is `503 Service Unavailable`.

#### 3002 - Worker initialization cancelled
When the worker tried to initialize but that initialization was cancelled (because the worker is shutting down). All waiting requests will be rejected with this error. See the `InitializationMiddleware` class. HTTP status is `503 Service Unavailable`.


### Execution errors (4xxx)

None so far.


### Metrics errors (5xxx)

None so far.
