# Initialization

Initialization is the process of downloading DLLs of a specific game backend into the worker container. It does not include their loading or execution, only the download.

When a worker starts up, it is non initialized (no backend is downloaded). Before it can start handling requests it has to be initialized.

There are two ways in which a worker can be initialized:

1. Eagerly during startup
2. Lazily with the first request

Both behave in the same way, the difference is only in how and when they are triggered. Eager initialization uses the initialization recipe URL from the `INITIALIZATION_RECIPE_URL` environment variable, whereas the lazy option uses the `X-Unisave-Initialization-Recipe-Url` HTTP header on the first request that arrives when the worker is not yet initialized (and is ignored on subsequent requests).

All the instructions for what files to download and from where are part of the recipe. All of this is in more detail described in the [Initialization API](api-initialization.md) documentation page.


## File system

In the current working folder of the worker process (the same folder where the `UnisaveWorker.exe` file is located), a folder named `backend` is created and into that folder all the game backend files are downloaded. That folder serves as the root for the game backend files, including any possible assets in the future (its contents would mirror the structure of a Unity project folder in that case).

During worker startup, if this folder exists, it gets deleted. This is to make debugging from Rider easier and it should not happen in production (where the folder should not be present in the container to begin with). Therefore an appropriate warning is logged to the console.


## Unisave request handling during initialization

When the initialization did not even start yet, the worker should reject requests. But this actually never happens, as the worker has always the lazy initialization on. So when such a request arrives, it immediately starts lazy initialization.

If the incomming request does not have the header, it responds with `409 - Missing initialization recipe`. See the [Requests API documentation](./api-requests.md) for more.

Once the initialization is ongoing, requests are paused until it finishes. There is no queue or limit, the limitting is performed a step earlier in the request concurrency middleware (see [Concurrency](./concurrency.md) documentation page). This waiting should respect the OWIN request `CancellationToken`.

Once initialization finishes, requests are sent further in the processing pipeline (to be executed).


## Errors during initialization

If the initialization fails (it can, it's just an async method, it can throw or timeout due to network issues), all waiting requests are rejected with `503 - Worker initialization failed`. See the [Requests API documentation](./api-requests.md) for more. Then the worker is set back into the uninitialized state, and the very next request to come will trigger the lazy initialization process again. This is on purpose to automatically recover from network outages.


## Code structure

The lazy initialization and request waiting for successful intialization is performed in the `InitializationMiddleware`. This is where Unisave requests interact with the initialization subsystem.

However the primary service for initialization is the `Initializer` abstract class. Its lifetime is managed by the `WorkerApplication` class (like all the other services). It's abstract purely for unit testing, so that the initialization method can be overriden differently. The `Initializer` class is responsible for the state management (in what state is the worker with regards to its initialization) and for the initialization subsystem's public API.

In production, the `RecipeV1Initializer` class is used to download all the files via the `HttpClient` service.

The `Initializer` service has these public methods:

- `AttemptEagerInitialization()` called during worker startup by the `WorkerApplication` to trigger the eager initialization. If the environment variable is not set, it does nothing.
- `TriggerInitializationIfNotRunning(string recipeUrl)` this method is called by the previous one and also by the `InitializationMiddleware` to start lazy initialization.
- `WaitForFinishedInitialization(CancellationToken ct)` this async method is used by the `InitializationMiddleware` to wait for the initialization to finish. It does nothing if the initialization already completed. It throws `InitializationFailedException` if the initialization encountered an error. The error is however not part of the exception - that is logged separately directly into the console (because initialization as such does not belong to any request and runs independently).
- `Dispose()` called by the `WorkerApplication` during shutdown.

Then there is the abstract method that should be overriden to actually implement the initialization logic itself:

- `PerformInitialization(string recipeUrl, CancellationToken ct)`

The cancellation token here is triggered when the whole worker shuts down (when the `Initializer` is disposed). If the method finishes without any exception, it's considered to have succeeded. There is no time limit on its execution, although it prevents unisave requests from being handled, so it should not wait unnecessarily.

The initializer also has a property you can use to get the `backend` folder path:

- `string BackendFolderPath`

The `backend` folder is guaranteed to be created by the `Initializer` before the `PerformInitialization` method is invoked.

The `Initializer` must be thread safe.
