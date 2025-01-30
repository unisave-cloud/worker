# Memory Leakage

When worker instances process requests, their total memory usage (for the whole process) grows, until it hits the container limit and gets killed by OOM by kubernetes. This happens only when the full unisave request execution is performed with the Unisave Framework - other worker endpoints, such as `/metrics` or `/health` do not cause the leak. Also, the leaking does not show up on the garbage-collected heap - that seems to be properly cleaned up.

It's not clear where the leak stems from, I have to debug that. But either way, the worker must be resilient against user-created memory leaks.

The worker currently solves it by tracking its memory usage and when it outgrows a threshold set by the `WORKER_UNHEALTHY_MEMORY_USAGE_BYTES` environment variable, it switches itself into the unhealthy state. This causes kubernetes to restart the container, thereby cleaning the memory.

This signalled approach is better than just killing itself, as it allows kubernetes to redirect traffic away from the worker before killing it.

The responsible class is the `MemoryLeakageMonitor`, being a part of the `HealthManager` and using `MemoryUsageGauge.GetMemoryUsageBytes` static method to get the memory usage.


## Chasing the memory leak

Stressing a worker with /heath requests (test only the OWIN-Katana part of the pipeline):

```bash
hey -z 10h -q 200 -c 1 http://localhost:8080/health
# 200 RPS, one at a time, for 10 hours
```

Getting a direct connection into a worker in minikube (accessing it, as if it runs locally):

```bash
kubectl port-forward pod/{pod-id} 8080:8080
```

Initialize a worker:

```bash
.venv/bin/python3 -m app.common.worker.WorkerClient Echo.EchoFacet/Echo '["Hello world!"]'
```

Sending 10 requests per second this way:
```bash
watch -n 0.1 ".venv/bin/python3 -m app.common.worker.WorkerClient Echo.EchoFacet/Echo '[\"hello_world\"]'"
```


## Conclusion

The leak is caused by the mono runtime, probbably by the `HttpClient` implementation, when used under multi-threaded load. (Does not occur when spinning on the `EchoFacet` with sessions disabled - no HTTP requests made) It leaks on average 24 KB per request. And it leaks the non-garbage-collected memory.

The fix was to switch to the ASP.NET Core runtime, which does not have this leak (with the identical source code and testing setup). Now I support both runtimes, though the mono runtime is kept there only as a backup.

A bonus improvement is that the average time per unisave request went down by 40%, from 100ms to 60ms (for the Engine Evolution locust simulation). Also, bechmarking the `/health` endpoint with `hey -z 10s -c 10` inside the CPU and RAM limited docker container yields 200 QPS for mono and crazy 4400 QPS for ASP.NET Core! So many nice improvements!
