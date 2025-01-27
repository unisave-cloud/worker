# Unisave Worker

Unisave worker is the process that executes game backends in Unisave cloud.


## Worker context

The worker process executes in production in the context of these services:

<img src="docs/WorkerContextDiagram.svg" alt="Worker component context" />
<!-- https://drive.google.com/file/d/18Nqn2e_ZGH5aoIqCqb4U4sW9i8Rc9sbN/view?usp=drive_link -->

The services in the context are:

- **Request Gateway:** This is the entrypoint into the worker system and it forwards backend requests to proper worker instances. This is where the primary traffic comes from.
- **Prometheus:** It collects worker instance metrics, which are used to measure worker system traffic and monitor its health. It probes the `/metrics` endpoints from the `worker-instances` prometheus job.
- **Worker System Supervisor:** The supervisor handles worker pool scaling. It probes the `/metrics` endpoint to collect CPU utilization. It side-steps prometheus to remove potential points of failure, and asks the workers directly.
- **Kubernetes:** The `/_/health` endpoint is probed by kubernetes to determine pod readiness and health.
- **Web API:** In order to initialize, the worker downloads initialization recipe from the web API. The recipe URL is provided either in an environment variable or in a request header field. Recipes are currently hosted by the web API service.
- **DigitalOcean Spaces:** The initialization recipe lists backend files and their URLs to be downloaded during initialization. These are currently downloaded directly from DigitalOcean Spaces via signed URLs.

How exactly these services interact with the worker is described in the [External APIs](docs/external-apis.md) documentation page.


## Configuration

There are no external service endpoints that need to be configured explicitly for the worker to start. The initialization recipe URL is provided at runtime.

The list of environment variables for configuration has a separate [documentation page](docs/configuration.md).


## Documentation

- [Configuration](docs/configuration.md)
- [External APIs](docs/external-apis.md)
    - [Unisave Requests](docs/api-unisave-requests.md)
    - [Error Codes and Meanings](docs/api-error-codes-and-meanings.md)
    - [Initialization](api-initialization.md)
    - [Game Backend](api-game-backend.md)
    - Metrics
- [Code Architecture](docs/code-architecture.md)
- [Concurrency](docs/concurrency.md)
    - [Deadlocks](docs/deadlocks.md)
- [Initialization](docs/initialization.md)
- [Unisave Environment Variables](docs/unisave-environment-variables.md)
- [Custom OWIN Environment Keys](docs/custom-owin-environment-keys.md)
- [Loading PDB files](docs/loading-pdb-files.md)
- [Graceful Shutdown](docs/graceful-shutdown.md)
- [Shared `Owin` and `Microsoft.Owin` DLLs](docs/shared-owin-and-ms-owin-ddls.md)
- [HTTP Request Cancellation](docs/http-request-cancellation.md)
- Metrics


## Testing

To start the worker after cloning the repo and test it before deployment, you can apply progressively more complex tests:

1. **Unit testing:** From Rider, launch unit tests to verify the functionality of internal pieces.
2. **Locust worker testing:** In [locust](https://github.com/unisave-cloud/locust?tab=readme-ov-file#test-suite-overview) repository, there are *Worker* tests, that are designed to be run against a worker process running inside Rider. Start with the [Worker Echo Test](https://github.com/unisave-cloud/locust/blob/master/docs/worker-echo.md) to see how to initialize the worker and how to send individual requests. Then use the [Worker Mixed Load Test](https://github.com/unisave-cloud/locust/blob/master/docs/worker-mixed-load.md) to see how the worker performs in a complex concurrent scenario.
3. **Locust against docker:** You can do the same setup, but run the tests against `make run` docker container with limited memory and CPU allocation to see how it breaks when overwhelmed.
4. **Locust minikube:** You can build the worker and run it in the minikube cluster and use [Engine Evolution Test](https://github.com/unisave-cloud/locust/blob/master/docs/engine-evolution.md) to stress the worker in the context of the whole cluster to see how it scales to multiple instances. You can then in addition start dropping in bombs (long timeout requests) to see how it handles those.


## Deployment

The development and production builds differ only in the version stored in `AssemblyInfo.cs`. The developments ones have the `-dev` suffix. The deployment for both is then identical:

Build the docker container:

```bash
make build
```

Push the container to the DigitalOcean registry:

```bash
make push
```

If the DigitalOcean registry authentication expires, run:

```bash
doctl registry login
```

Update the worker version in the `deployment` repository and run the `ingrade` to update the kubernetes deployment (minikube or production).
