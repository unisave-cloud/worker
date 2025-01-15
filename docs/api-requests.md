# API: Requests

The *Request Gateway* forwards facet call requests to the worker instance at `POST /` endpoint.

It provides these headers:

```
Content-Type: application/json;
X-Unisave-Initialization-Recipe-Url: http://unisave.minikube/_api/sandbox-recipe/QMWf4bPBdA2ZuQai/9e429b4b3bf24b91bd233444ec3cce8a/f49b8273f27cfbbedafc1bdd170f8462c23e0bda120df927d1cdfb455ccc43d9;
```

Only JSON payload is accepted, hence the content type.

The request body has this structure:

```json
{
    "method": "facet-call",
    "env": "ENV_TYPE=development\nSESSION_DRIVER=null\n...",
    "methodParameters": {
        "facetName": "EchoFacet",
        "methodName": "Echo",
        "arguments": ["Hello world!"],
        "sessionId": "123456789",
        "deviceId": "123456789",
        "device": {
            "platform": "Locust",
            "deviceModel": null,
            "graphicsDeviceName": null,
            "graphicsDeviceID": null,
            "graphicsDeviceVendorID": null,
            "graphicsMemorySize": null,
            "graphicsDeviceType": null,
            "systemMemorySize": null,
            "processorCount": null,
            "processorFrequency": null,
            "processorType": null
        },
        "gameToken": "123456789",
        "editorKey": null,
        "client": {
            "backendHash": "123456789",
            "buildGuid": "123456789",
            "frameworkVersion": "none",
            "assetVersion": "none",
            "versionString": "none"
        }
    }
}
```


## 200: Success

When the request is successfully executed, it returns `200 OK` with this body:

```json
{
    "result": "ok",
    "returned": null,
    "special": {
        "sessionId": "DpVNxtkzCBqDx-o=",
        "logs": [],
        "executionDuration": 0.021
    }
}
```

When the request executes with an exception, it also returns `200 OK` with this body:

```json
{
    "result": "exception",
    "exception": {
        "ClassName": "System.Exception",
        "Message": "Hello world!",
        "StackTraceString": "   at UnisaveWorker"
    },
    "special": {
        "sessionId": "DpVNxtkzCBqDx-o=",
        "logs": [],
        "executionDuration": 0.021
    }
}
```


## 409: Missing initialization recipe URL

The initialization recipe URL is used only when the worker has not yet been initialized. Otherwise it gets ignored by the worker (and thus can be missing).

If the initialization recipe URL is needed but is missing, a `409 Conflict` response is sent with the error body:

```json
{
    "error": true,
    "code": 409,
    "errorMessage": "Worker is not initialized and no initialization URL was provided with the request."
}
```


## 429: Full worker queue

When the worker is overwhelmed and cannot accept any more requests, it responds with `429 Too Many Requests` and this body:

```json
{
    "error": true,
    "code": 429,
    "errorMessage": "Worker queue is full."
}
```

See the [concurrency documentation](concurrency.md) to learn more.


## 500: Uncaught exception

If there's an uncaught exception in the request processing, it should be logged and the `500 Internal Server Error` response should be returned.

See the `ExceptionLoggingMiddleware` class.


## 503: Worker initialization failed

When the worker tried to initialize but the initialization process failed, requests that wait for the initialization the finish will be rejected with the response message:

```json
{
    "error": true,
    "code": 503,
    "errorMessage": "Worker initialization failed."
}
```

See the `InitializationMiddleware` class.


## 503: Worker initialization cancelled

When the worker tried to initialize but that initialization was cancelled (because the worker is shutting down). All waiting requests will be rejected with the response message:

```json
{
    "error": true,
    "code": 503,
    "errorMessage": "Worker initialization was cancelled, the worker is probably shutting down."
}
```

See the `InitializationMiddleware` class.


## 503: Worker startup

When the worker is starting up and the HTTP server is not yet running, it rejects any incomming requests (XYZ refused to connect). The connection is refused at the TCP level.

Once the HTTP server is running, but the server cannot handle a request for some reason, it responds with `503 Service Unavailable` with the body:

```json
{
    "error": true,
    "code": 503,
    "errorMessage": "..."
}
```

**However:** If the worker is just being initialized, then the incomming requests should be queued up and wait until the initialization finishes. This is so as to not bounce the very first requests when the first worker instance is being cold-started.
