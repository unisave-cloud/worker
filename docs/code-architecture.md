# Code Architecture

The entrypoint is the `Program` class. It loads the `Config` from environment variables, creates the `WorkerApplication` instance, starts is, and waits for the termination unix signal.

The `WorkerApplication` is what encapsualtes the application as a whole. It manages its lifecycle via the constructor, `Start`, `Stop`, and `Dispose` methods, which must be called in this order. It creates singleton services used throughout the application, combines them together and manages their lifetime (like an IoC container would).

The primary IO goes through the HTTP server, which is started in the `WorkerApplication.StartHttpServer` method. The OWIN self-host `WebApp.Start` utility is used to start a .NET Katana OWIN server based on its definition in the `Startup` class.

The `Startup` class is THE place where the HTTP request processing (and routing) pipeline is defined. It consists of a number of OWIN middleware classes, carefully put together. Access to singleton services is provided via dependency injection through constructors (from the parent `WorkerApplication` instance).

THEREFORE, THESE ARE THE TWO CLASSES YOU WANT TO READ BEFORE READING ANY OTHER PART OF THE CODEBASE:

- `WorkerApplication`
- `Startup`


## Services

These are the services managed by the `WorkerApplication`:

- `GracefulShutdownManager` contains state for the graceful shutdown period.
- `Initializer` contains state for worker initialization (backend downloading and assembly loading).
- `MetricsManager` defines tracked prometheus metrics and manages their collection and exporting.
- `HttpClient` The .NET HTTP client, used by other services to fetch external files (mainly used by the `Initializer`).
