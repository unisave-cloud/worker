# Overview

The primary objective of the watchdog is to forward execution requests (facet calls, http endpoint requests, scheduler ticks, etc.) to an initialized instance of a game backend. Therefore the primary API is the exectuion API and all other features are there to support it.

When the worker instance container is (re)started, it contains no game backend. The backend has to be downloaded from backend storage and this process is called worker initialization. The initialization downloads:

- game backend assembly
- other static backend files
- unisave framework assembly and its dependencies
