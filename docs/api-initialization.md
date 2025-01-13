# API: Initialization

When the worker starts up, it's non-itialized (always), meaning it does not have the game backend DLLs downloaded and loaded.

The initialization process starts with an intialization recipe URL, something like this:

```
http://unisave.minikube/_api/sandbox-recipe/QMWf4bPBdA2ZuQai/9e429b4b3bf24b91bd233444ec3cce8a/f49b8273f27cfbbedafc1bdd170f8462c23e0bda120df927d1cdfb455ccc43d9
```

There are two ways how it can get the initialization recipe URL:

1. Lazily with each request via the `X-Unisave-Initialization-Recipe-Url` HTTP header.
2. Eagerly in the `INITIALIZATION_RECIPE_URL` environment variable.

They both perform initialization in the same way, just the first approach starts it later, while the second starts it just after the worker is started.

During initialization, all facet call requests are kept in a queue until the initialization finishes.

> **Note:** In the future it might be reasonable to bounce them back to the request gateway with `503 Service Unavailable`, in case the initialization takes too long and it would be faster to handle the request with some other running worker instance.


## Initialization recipe

The first step is to download the initialization recipe. It's a text file listing all the backend files and the URLs they can be downloaded from:

```
UNISAVE_SANDBOX_RECIPE v1
backend.dll
http://minio.unisave.minikube/unisave-dev/games/mq/mqHKTsGieTPhT0fr/backends/QMWf4bPBdA2ZuQai/backend.dll?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=73f2fcd7de36458ca357fb96ea2f8390c311beb20f1377f0b375bc27a9c2f5a1
backend.pdb
http://minio.unisave.minikube/unisave-dev/games/mq/mqHKTsGieTPhT0fr/backends/QMWf4bPBdA2ZuQai/backend.pdb?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=e06cfbd2176ca8243787a718ab75189f9c0e20c9a82fd8d6f7d7cc8433ab5730
LightJson.dll
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/LightJson.dll?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=eee60ca2bb3c27967e8353360114c309e093aeef435c5c97545918c087cb4a65
LightJson.pdb
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/LightJson.pdb?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=61dc10e113e101c810ba4210aca57a7c9665df3a6a2492af53bd18a46d7aa288
Microsoft.Owin.dll
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/Microsoft.Owin.dll?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=30e1cf09d35832338389ec41f87640a5c16cda1f5b8f6253b6e032f48dd27cb1
Microsoft.Owin.xml
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/Microsoft.Owin.xml?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=da9a7a7d997dba90e81258f5946d334ee8ae1a03cd1b08339097be052841fcc0
Owin.dll
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/Owin.dll?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=5f9c02ae6545d883fdf23ea4ba3441dc05252e7b4868f599c3a49d99611ff49a
UnisaveFramework.dll
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/UnisaveFramework.dll?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=627a1b9f81d534558b1339c29e36b5fa4e5c347f927940bd7c6c7e955ddef0d8
UnisaveFramework.pdb
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/UnisaveFramework.pdb?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=4376ebcc609904253d06ae5f34d6ca09ce5bbced1d6519f3c4dde8d803063c73
UnisaveJWT.dll
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/UnisaveJWT.dll?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=aa1f9994519fdc7172a25809ae6104b179c346c47e12de6c49694a03cc77fae1
UnityEngine.dll
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/UnityEngine.dll?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=edfe53f590e15ec730edbfaa459a79cd4b0f76d1d0dff9005452b2c0d0bcea7e
UnityEngine.pdb
http://minio.unisave.minikube/unisave-dev/unisave-framework/0.11.0/UnityEngine.pdb?X-Amz-Content-Sha256=UNSIGNED-PAYLOAD&X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=root%2F20241221%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20241221T130302Z&X-Amz-SignedHeaders=host&X-Amz-Expires=1800&X-Amz-Signature=79d290ac1fcb6e33b685343f57a38553246aaa801437b925c7c9089465a5f95a
```

It starts with a header stating the version `UNISAVE_SANDBOX_RECIPE v1` and then a list of pairs of lines, first the relative file path and second its URL for download.

These download URLs are generated when the recipe is requested and they are temporary, signed object storage URLs. They point to Digital Ocean Spaces in production.
