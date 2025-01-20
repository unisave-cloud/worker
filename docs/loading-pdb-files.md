# Loading PDB files

When a .NET assembly is complied, you get the `.dll` with the code, and the `.pdb` with the debugging metadata (like exception stacktrace places in source code).

When the worker loads assemblies via `Assembly.LoadFile`, this automatically loads PDB files, **but only if the application is running in debug mode!**


## In Rider during development

Just start the project in the debugging mode.


## In production

You can check the `Dockerfile`, that the worker is started with the `--debug` mono flag:

```bash
mono --debug UnisaveWorker.exe
```
