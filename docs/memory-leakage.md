# Memory Leakage

When worker instances process requests, their total memory usage (for the whole process) grows, until it hits the container limit and gets killed by OOM by kubernetes. This happens only when the full unisave request execution is performed with the Unisave Framework - other worker endpoints, such as `/metrics` or `/health` do not cause the leak. Also, the leaking does not show up on the garbage-collected heap - that seems to be properly cleaned up.

It's not clear where the leak stems from, I have to debug that. But either way, the worker must be resilient against user-created memory leaks.

The worker currently solves it by tracking its memory usage and when it outgrows a threshold set by the `WORKER_UNHEALTHY_MEMORY_USAGE_BYTES` environment variable, it switches itself into the unhealthy state. This causes kubernetes to restart the container, thereby cleaning the memory.

This signalled approach is better than just killing itself, as it allows kubernetes to redirect traffic away from the worker before killing it.

The responsible class is the `MemoryLeakageMonitor`, being a part of the `HealthManager` and using `MemoryUsageGauge.GetMemoryUsageBytes` static method to get the memory usage.
