# External APIs

These pages document the interfaces on the boundary of the Unisave Worker. It should focus on what the data and protocols look like, not on the worker's internal logic. You read these pages to understand how to interact with the worker from another service/assembly and how the worker interacts with other services/assemblies. 

- [API: Unisave Requests](api-unisave-requests.md) describes how facet calls are sent from the *Request Gateway* to the worker.
- [API: Error Codes and Meanings](api-error-codes-and-meanings.md) lists all errors that can be returned by the worker as an HTTP response.
- [API: Initialization](api-initialization.md) describes what external services does the worker send HTTP requests to, to initialize itself.
- [API: Game Backend](api-game-backend.md) describes how facet calls are passed into the game backend DLLs (to Unisave Framework).
- API: Metrics (TODO) describes individual exported prometheus metrics and their meaning and use by external services.
- [API: Health](api-health.md) describes the use of kubernetes probes, how they ping the health endpoint, and when can the worker become unhealthy.