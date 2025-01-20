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


## Error responses

If the request processing fails for some reason, a corresponding non-200 status code response will be sent. For a list of these errors and their numbers, refer to the [Error codes and meanings](api-error-codes-and-meanings.md) documentation page.
