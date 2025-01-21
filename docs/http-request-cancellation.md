# HTTP Request cancellation

Each request in OWIN comes with a cancellation token:

```cs
var context = new OwinContext(environment);

CancellationToken token = context.Request.CallCanceled;

token.IsCancelationRequested; // true / false
```

This token shold be triggered when the client closes the connetion to the server (stops listening for the response).

From the tests I've done against Rider in debugging mode, the token remained unchanged. The request finished and the response was sent without any exception (although it never arrived to the client).

I haven't tested it against the docker container, maybe it's OS-dependant.

I tried two aproaches:

1. Put a breakpoint into Unisave Request path, send a single locust echo request and kill locust while it was waiting (OS closes all of its TCP connections).
2. Do a curl request at the healthcheck endpoint with a breakpoint and killing curl while waiting.

Both had the same behaviour.

Here is an issue I found, somewhat related, but Windows specific: https://github.com/aspnet/AspNetKatana/issues/141
