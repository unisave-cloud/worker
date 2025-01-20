# Graceful Shutdown

When the worker shuts down (after receiving the unix signal), a graceful shutdown period is entered.

During this period, the worker waits for all in-flight requests to finish. However, there is a maximum waiting time specified by the `WORKER_GRACEFUL_SHUTDOWN_SECONDS` environment variable. When the graceful period ends, the worker is terminated forcefully by executing `Environment.Exit(0)`.

The state is managed by the `GracefulShutdownManager` service and it's embedded into the OWIN middleware stack with the `GracefulShutdownMiddleware` class.

The middleware is placed right after the global exception handler, so it applies to all HTTP requests (not only Unisave requests).

Any new requests to arrive at the server during this time are rejected with a `503 Service Unavailable` response. The OWIN server is still fully functional, it just chooses to reject these requests (in the middleware). This must be this way, because if you stop the server before waiting, it also closes the TCP socket and the pending requests would be unable to send their responses.

This is also the recommended approach for OWIN Katana servers: https://stackoverflow.com/questions/26012227/finish-current-requests-when-stopping-self-hosted-owin-server

The `host.OnAppDisposing` (startup OWIN properties dictionary) is triggered together with the HTTP server disposal, therefore it happens AFTER the pending requests finish (or are abandoned). At this point, the backend code (Unisave Framework) is disposed.

The `owin.CallCancelled` (request OWIN environment dictionary) is NOT triggered for pending requests during this period.
