# Development Setup

You need Rider and .NET SDK. [Install](https://learn.microsoft.com/en-us/dotnet/core/install/linux-ubuntu-install?tabs=dotnet8&pivots=os-linux-ubuntu-2204#install-the-sdk) the SDK 8.0:

```
sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-8.0
```

Optionally also docker to build containers and minikube with the development cluster.


## Setting up

- Clone the repo `git clone git@github.com:unisave-cloud/worker.git`
- Restore packages `dotnet restore`


## New feature development

- Bump the version and add the `-dev` suffix in the assembly info
- Add the feature and commit changes


## Testing in isolation

> To test a single new piece/service of the worker.

- Write unit tests and run those


## Testing in Rider

> To send real traffic to the worker and test the worker as a whole.

- Start the project in Rider (plain or with the debugger)
- Use `curl localhost:8080/foobar` to probe non-unisave request endpoints
- Use the [locust repository](https://github.com/unisave-cloud/locust) to send unisave requests towards the process (it will trigger lazy initialization), e.g. `.venv/bin/python3 -m app.common.worker.WorkerClient Echo.EchoFacet/Echo '["Hello world!"]'`
- Start the locust Engine Evolution simulation pointed at the `localhost:8080` worker endpoint to simulate realistic long-running load


## Testing in Docker

> To test the docker container itself and the worker in the memory and cpu limited environment.

- Build the container `make build`
- Start the container via `make run`
- The container runs on the `host` network, therefore you can request it just like when it runs in Rider


## Testing in Minikube

> To get worker metrics in Grafana http://grafana.unisave.minikube/.
> To test the request gateway with the worker and their interplay.

- Build the `-dev` container and push it `make build && make push`
- In the [deployment repository](https://github.com/unisave-cloud/deployment) change the worker version in `values.yaml` in `workerSystem.workerImage` and `make ingrade-dev`, wait for the minikube cluster to pull the new image (or `rollout restart` the current worker deployment).
- Port-forward traffic into the one eager container sitting in the cluster via `kubectl port-forward pod/{pod-id} 8080:8080` (test just like with Rider or Docker, you can use locust)
- Make locust requests towards the request gateway in the minikube cluster (pointed at http://unisave.minikube/)


## Testing from Unity

> To test the full range of Unisave Framework's functionality.

- Deploy to the minikube cluster
- Open the asset repository from Unity and run the UnisaveFixture tests against the minikube cluster


## Deploying new version (10 - 30 min)

- remove the `-dev` suffix from the assembly info
- build both containers `make build && make build-mono`
- push them to the registry `make push && make push-mono`
- update worker version in the deployment repository and deploy to the minikube cluster
- run UnisaveFixture tests against the local cluster
- run the locust Engine Evolution test against the local cluster
- deploy updates to the production cluster
- check that requests are handled properly
- commit the version update to github
- create a github release page
