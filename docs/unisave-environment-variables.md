# Unisave Environment Variables

Unisave environment variables are env vars that are set by the game developer for the environment in which his backend code executes. In the legacy API, they have been sent with each facet call to the worker explicitly, but I would like to make them proper env vars for the backend process and this documentation page describes the transition.

There are two places, we need to discuss:

1. How the worker gets their value
2. How the worker passes them into the backend

Because both of these places have the legacy and the new API, I decided to define the transfer in between them **per-request** on the OWIN environment context (they can be cached for performance, but they have to be set on the dictionary). It concerns these keys:

- `worker.EnvDict` Environment variables that should apply for this request. Type `Dictionary<string, string>`. Assigned by `LegacyApiTranslationMiddleware`.
- `worker.EnvString` Environment variables as string that should apply for this request. Type `string`. Assigned by `LegacyApiTranslationMiddleware`.

I chose this design for the legacy-to-new transition period, because the legacy setup requires the ability to change env vars while request processing is ongoing, without modifying the worker pool deployment.


## Legacy env vars acquisition

In the legacy worker requests API, each unisave request comes to `POST /` with this payload:

```json
{
    "method": "facet-call",
    "env": "ENV_TYPE=development\nSESSION_DRIVER=null\n...",
    "methodParameters": {
        "facetName": "EchoFacet",
        "methodName": "Echo",
        "arguments": ["Hello world!"],
        "sessionId": "123456789",
        ...
    }
}
```

Here, the `"env"` string contains environment variables that should apply to this request. They are extracted and parsed out by the `LegacyApiTranslationMiddleware`.


## New env vars acquisition

These are going to be done in one of two ways:

1. Kubernetes deployment for the worker pool will have them set as true env vars
2. In shared workers, the master process will need to get those during runtime and pass them into the worker processes as true environment variables.

Either way, from the request processing point of view, they will become true environment variables and unisave frameworks will load them directly (except for legacy frameworks, see below for more info on that).


## Legacy env vars passing to backend

Framework versions before v0.11.0 are invoked via the static method `Unisave.Runtime.Entrypoint.Start(string execParamsJson, Type[] gameAssemblyTypes)`. The execution parameters JSON is the exact same payload that enters the worker via the legacy API, i.e.:

```json
{
    "method": "facet-call",
    "env": "ENV_TYPE=development\nSESSION_DRIVER=null\n...",
    "methodParameters": {
        "facetName": "EchoFacet",
        "methodName": "Echo",
        "arguments": ["Hello world!"],
        "sessionId": "123456789",
        ...
    }
}
```

Environment variables are parsed and processed for each request independently and the whole request processing pipeline (e.g. database connection) is built and destroyed for each request separately.


## New env vars passing to backend

Framework versions after and including v0.11.0 are invoked via the `Unisave.FrameworkStartup` class. This is done in two steps:

1. Creating the backend application instance via `FrameworkStartup.Configuration(IAppBuilder app)`.
2. Invoking the backend application with request instances as OWIN contexts.

Environment variables are loaded only during the first step - when the application is created. Then they are cached internally in the `EnvStore` service. The loading is done in `FrameworkStartup.PrepareEnvStore` and runs in two steps:

1. All *true* environment variables are loaded via `Environment.GetEnvironmentVariables()`
2. Additional vars are loaded from `unisave.EnvironmentVariables`, from the properties OWIN dictionary (the one that constructs the owin app, not the request one). These must be `IDictionary<string, string>`.

In this transitioning period we want to be still able to swap out env vars in the middle of a running worker, therefore we will use the `unisave.EnvironmentVariables` option to pass them into the backend. There will also be multiple instances of the backend app for all the hot env vars and the correct one will be chosen based on the env vars specific for a given request.

For the final setup after the transition, only the first option will be used - loading env vars from the *true* env vars. These will not change during the lifetime of the worker, instead the whole worker pool deployment will have to be re-deployed when they change.


## The final after-transition state we aim for

Environment variables are set via deployment env options for the worker pool deployment. Which makes them appear inside the worker like any other environment variable. The framework already loads these if the `unisave.EnvironmentVariables` field is not provided.

The worker process will only need to process end vars for legacy frameworks. It will load them from true environment vars (all of them, even the system ones), and put together the `"env"` string to be passed into the legacy framework entrypoint `Unisave.Runtime.Entrypoint.Start`.

> **Note:** This is not complete, it omits shared worker instances! But even those will have individual backends run as separate processes, so they need to have their true environment variables set somehow. So this really concerns whether to use kubernetes env options, or wheter to have some worker logic that modifies true env vars during runtime and restarts the downstream request processing worker processes.
