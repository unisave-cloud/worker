# API: Game backend

This documentation page describes, how requests are passed into the downloaded game backend DLLs.

There are two methods depending on the Unisave Framework version used by the game backend. They can be distinguished by checking for the presence of the newer approach (checking the existence of `OwinStartup` attribute on game's assemblies). If missing, falling back to the legacy approach.

But before the game backend can be invoked, first, its assemblies must be loaded.


## Loading game backend assemblies

TODO


## Locating the OWIN startup class

When game backend files are downloaded and the Unisave Framework assembly is loaded, you have to locate the `Owin.OwinStartup` attribute on the assembly itself.

The recommended usage of the OWIN startup attribute is described [in this microsoft documentation page](https://learn.microsoft.com/en-us/aspnet/aspnet/overview/owin-and-katana/owin-startup-class-detection).

The attribute must have an explicit name, and the Unisave Worker by default looks for the `UnisaveFramework`-named attribute. The name the worker is looking for can be overriden by specifying the `WORKER_OWIN_STARTUP_ATTRIBUTE` environment variable.

In the Unisave Framework, the startup attribute is defined [here](https://github.com/unisave-cloud/framework/blob/master/UnisaveFramework/FrameworkStartup.cs#L12).

Once the startup class is located, it is used to perform OWIN startup. If it isn't found, Unisave Worker falls back to the legacy framework invocation method via the `Unisave.Runtime.Entrypoint` static class.


## OWIN startup class

TODO:
For the new framework use: https://unisave.cloud/docs/interfaces#unisave-framework
But first built the legacy adapter that uses the old entrypoint.


## Legacy static entrypoint (before framework v0.11.0)

...
