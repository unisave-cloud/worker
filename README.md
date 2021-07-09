# Unisave Sandbox

This repository contains the OpenFaas function image, that is used to
run game backends.

The behaviour is as follows:

1. The Unisave system asks OpenFaas to create a new function
   corresponding to a certain backend ID (say `backend-foobar` function)
2. OpenFaas creates new instance of the function and during the startup
   process the function downloads proper backend files
3. The function is now ready to receive execution requests

The initialization is performed by executing the sandbox in the `init`
mode with a signed URL from which an initialization recipe is downloaded
and executed:

    $ sandbox.exe init http://web:8000/_api/sandbox-recipe/...

The execution is then invoked by the watchdog process by calling:

    $ sandbox.exe exec

The executing uses standard input/output like any other faas function.


## Execution API

`POST /function/backend-foobar`

The request to execute a game backend contains exactly the
`executionParameters` value as defined in the Unisave Framework. So the
request body is passed to the framework as-is. Any additional values
have to be passed using HTTP headers.

Example `executionParameters` for calling a facet:

```json
{
    "method": "facet-call",
    "methodParameters": {
        "facetName": "MyFavet",
        "methodName": "MyMethod",
        "arguments": ["foo", 42],
        "sessionId": "DpVNxtkzCBqDx-o="
    },
    "env": "FOO=bar\nBAZ=asd\n"
}
```

The response is then exactly the `executionResult` returned by the
Unisave Framework.


## Execution system characteristics and decisions

- For now, we execute synchronous requests only (up to 30 seconds)
- Most requests are quick (100ms) while some may wait on external
  services and be longer (5s).
- Each sandbox has the same memory&cpu request and limit - no growth, no
  shrink. Needed for pricing. I guaratee X MBs and is has to be always 
  available.
- One sandbox has to run only one request at a time otherwise the
  requests would share memory and cpu and that breaks my promise to the
  customer.
- Scaling cannot be done by "when all are busy" because it isn't
  scalable to thousands of instances.
- Scaling will be done by Kubernetes HPA, targeting 50% utilization
  time. (one given sandbox is 50% of the time idle and 50% of the time
  working) (CPU utilization cannot be used due to e.g. waiting for HTTP
  requests)
- Load-balancing can be done by round-robin or random, because the 50%
  utilization target gives us an expected number of 2 tries to find an
  idle sandbox.
  - **Flaw!** utilization on what time window? Queue for burst
    smoothing?
  - **Solution:** Each instance gets a sticky queue. Waiting requests
    give up (all at once and future incomming) if the currently running
    requests executes for more than 2*average exec time. Requests have a
    maximum number of giveups before sticking no matter what. It is the
    number of instances - 1. Therefore for 1 instance, requests don't
    give up. TEST THE SOLUTION BEFORE USING IT!
- Cold-starts can be reduced by keeping a pool of warm but not
  initialized instances around and picking from it when scaling up from
  zero. (not a problem for further scaling, because we have other
  instances that can take the request)
- Sandboxes will execute user code in AppDomain and monitor for memory
  leakage and restart the AppDomain when needed. AppDomains will also
  provide a basic security layer.
- OpenFaas can handle requests of up to 30s due to the timeouts. So it's
  ideal for player requests. But I'll need to build an entirely separate
  system for longer running jobs (up to 15 min) that could be queued.
- Request body (upload) should be below 1MB (Nginx and NATS have the
  same default limit). Larger uploads have to uploaded directly to
  object storage via signed URLs:
  https://aws.amazon.com/blogs/compute/uploading-to-amazon-s3-directly-from-a-web-or-mobile-application/
- Response body size limit... let's make it 10MB! (because then we hit
  serialization problems and JSON problems anyways)
- Sandbox crashes have to be handled by the endpoint, not the sandbox as
  it runs in an untrusted environment. Sandboxes will crash when users
  do funky stuff. Typical crash is Exit(0), OOM or an infinite loop.
  Requests in the queue will also get killed, handle that as well.