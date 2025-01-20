# Shared `Owin` and `Microsoft.Owin` DLLs

The Unisave Worker uses OWIN and .NET Katana to run the HTTP server. The Unisave Framework also uses OWIN for the inteface between it and the worker. Therefore both depend on these two DLLs:

- `Owin 1.0.0`
- `Microsoft.Owin 4.2.2`

When the worker starts, it loads these into the CLR. Then, when the backend is initialized and the framework is loaded, all dependencies are also loaded including these two libraries. This second time, these assemblies will not be loaded, because they have the same name and they have already been loaded ([source](https://stackoverflow.com/questions/7825587/how-can-i-avoid-loading-an-assembly-dynamically-that-i-have-already-loaded-using)).

THEREFORE, ONLY THE VERSION USED BY THE WORKER IS USED, THE ONE USED BY THE FRAMEWORK IS IGNORED AND THE FRAMEWORK IS FORCED TO USE THE WORKER'S VERSION.

In fact, the same thing happens with the whole .NET Framework, since it's just a `mscorlib` assembly dependency. Here in-fact the dependency is against the version `4.0.0.0`, even though it was compiled against .NET Framework 4.7.

You can list assembly dependencies of any assembly by using the mono disassembler:

```bash
monodis --assemblyref UnisaveFramework.dll
```

Here additional .NET Framework references are `System.Core` and `System.Net.Http`.

So in-fact there are more shared assemblies than just OWIN and they all behave in the same way - newer version of the assembly than what is specified will be loaded and used in its place. (I just don't know if the major version has to be the same though...)

What IS clear is that: YOU CANNOT LOAD TWO VERSIONS OF THE SAME ASSEMBLY INTO A SINGLE APPLICATION DOMAIN.
