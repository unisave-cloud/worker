# API: Health

The `/health` endpoint is probed by Kubernetes to determine worker's health state.

Here's a short overview of all the kubernetes probes:
https://kubernetes.io/docs/concepts/configuration/liveness-readiness-startup-probes/

Here's the full documentation on kubernetes probes:
https://kubernetes.io/docs/tasks/configure-pod-container/configure-liveness-readiness-startup-probes/

The worker system can use all the probes, but if the configuration string is empty, the probe is not used:

- **Liveness** Restarts the container when it fails.
- **Readiness** Redirects traffic away from the container when it fails.
- **Startup** Postpones the two check above until it succeeds. Not needed for the worker, as it starts fast.

We want to use liveness and readiness in tandem and ignore the startup probe for the worker. Therefore both probes will have the same configuration.

They are defined in the worker pool deployment resource here: https://github.com/unisave-cloud/monorepo/blob/master/WorkerSystem/Models/KubeResourcesModel.cs#L330

They are taken from a [configuration string](https://github.com/unisave-cloud/monorepo/blob/master/Monorepo/Options/WorkerSystemOptions.cs#L42), which has defaults set here: https://github.com/unisave-cloud/monorepo/blob/master/Runner/appsettings.json#L59

They can be (and are) overriden in the deployment values here:
https://github.com/unisave-cloud/deployment/blob/master/chart/unisave/templates/_worker-system-options-env.yml#L17

Here's an example of the probe:

```
http-get http://:8080/health delay=3s timeout=1s period=1s #success=1 #failure=3
```

Explainer:

- Probe via HTTP GET (instead of shell command)
- The URL to probe, 2xx response is a success, 5xx response is a failure
- It waits for 3 seconds after startup before the first probe is sent
- It waits 1s for the HTTP request to complete
- It pings every 1 second
- It takes 1 success to switch to healthy state
- It takes 3 failures to switch to unhealthy state
