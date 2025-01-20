# TODO list

List of things to add, specify, or fix.


## Robustness

- [ ] Measure memory leakage during long running.
- [ ] Implement aging so that workers get restarted when they reach a certain age and therefore get rid of memory leaks.
- [ ] During server shutdown, wait 10 seconds for in-flight requests to finish. Implement this via a GracefulShutdownMiddleware that counts requests and responds 503 Shutting Down for any additional requests. Trigger this middleware before disposal (or rather its service). Belongs to the `Ingress` namespace.
- [ ] Formalize error codes and responses and document them like ArangoDB has. Sections could be: general, concurrency, initialization, etc...


## APIs

- [ ] Request body (upload) should be below 1MB (Nginx and NATS have the same default limit). Larger uploads have to be uploaded directly to object storage via signed URLs: https://aws.amazon.com/blogs/compute/uploading-to-amazon-s3-directly-from-a-web-or-mobile-application/
- [ ] Response body size limit... let's make it 10MB! (because then we hit serialization problems and JSON problems anyways)
