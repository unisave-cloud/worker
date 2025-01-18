# API: Game backend

This documentation page describes, how requests are passed into the downloaded game backend DLLs.

There are two methods depending on the Unisave Framework version used by the game backend. They can be distinguished by checking for the presence of the newer approach (checking the existence of `OwinStartup` attribute on game's assemblies). If missing, falling back to the legacy approach.

But before the game backend can be invoked, first, its assemblies must be loaded.


## Loading game backend assemblies

Backend assemblies are loaded after backend files are downloaded in the `BackendLoader` service. The same class is also responsible for locating backend entrypoints.

Assemblies are loaded by getting all the DLL files in the backend folder and explicitly loading them. This makes sure no additional dependencies are loaded later during worker's lifetime, which could potentially cause problems with their localization, as these assemblies are not placed in the working directory of the worker process.


## Locating the OWIN startup class

When game backend files are downloaded and the Unisave Framework assembly is loaded, you have to locate the `Owin.OwinStartup` attribute on the assembly itself.

The recommended usage of the OWIN startup attribute is described [in this microsoft documentation page](https://learn.microsoft.com/en-us/aspnet/aspnet/overview/owin-and-katana/owin-startup-class-detection).

The attribute must have an explicit name, and the Unisave Worker by default looks for the `UnisaveFramework`-named attribute. The name the worker is looking for can be overriden by specifying the `WORKER_OWIN_STARTUP_ATTRIBUTE` environment variable.

In the Unisave Framework, the startup attribute is defined [here](https://github.com/unisave-cloud/framework/blob/master/UnisaveFramework/FrameworkStartup.cs#L12).

Once the startup class is located, it is used to perform OWIN startup. If it isn't found, Unisave Worker falls back to the legacy framework invocation method via the `Unisave.Runtime.Entrypoint` static class.


## OWIN startup class

Once the OWIN startup class is located, it is instantiated (by the default constructor) and the `Configuration` method is called, with the `IAppBuilder` argument. The returned OWIN `AppFunc` can be used to directly handle the unisave request.

During `AppFunc` construction, additional OWIN startup properties must be provided. This is documented in detail [here](https://unisave.cloud/docs/interfaces#custom-properties).

The code responsible for backend invocation in this way is located in `OwinStartupExecutionMiddleware`.

Also, unless unisave environment variables are truly passed via real environment variables, the middleware must handle their mid-worker-lifetime changes and re-create the backend application when that happens.

All the specifics of this interface are described in the [interfaces unisave documentation page](https://unisave.cloud/docs/interfaces).

OWIN properties passed into the `Configuration` method are the following:

- `owin.Version` is set to `1.0.0`.
- `host.OnAppDisposing` is a cancellation token that is invoked when the backend application should be disposed.
- `unisave.GameAssemblies` is an `Assembly[]` with all the backend DLLs that can be found in the `backend` folder.
- `unisave.EnvironmentVariables` are now passed because Unisave environment variables are not yet set as true environment variables. This can be removed once that happens.


## Legacy static entrypoint (before framework v0.11.0)

The legacy entrypoint is the static class `Unisave.Runtime.Entrypoint` with the static method `Start`.

```cs
public static string Start(
    string executionParametersAsJson,
    Type[] gameAssemblyTypes
)
```

Execution parameters JSON has the following structure:

```json
{
    "method": "facet-call",
    "env": "ENV_TYPE=development\nSESSION_DRIVER=null\n...",
    "methodParameters": {
        "facetName": "EchoFacet",
        "methodName": "Echo",
        "arguments": ["Hello world!"],
        "sessionId": "123456789"
    }
}
```

The execution parameters that enter the worker in the HTTP request also contain `deviceId` and `gameToken` and other additional fields, but those have never been parsed by unisave frameworks below version v0.11.0, therefore they don't need to be passed along.

The `gameAssemblyTypes` should contain only the types defined in the `backend.dll`, anotherwords it excludes the framework DLLs. However, including them nonetheless should not cause any problems.

> **Concurrency:** The legacy framework API exects only one request to be executed at a time and checks that with a static variable. Therefore it is necessary to use a `RequestConcurrencyMiddleware` before passing requests into the framework.

The response JSON has this structure on success:

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

And this on exception:

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
