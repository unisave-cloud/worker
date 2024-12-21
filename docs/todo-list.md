# TODO list

List of things to add, specify, or fix.


## Robustness

- [ ] Measure memory leakage during long running.
- [ ] Implement aging so that workers get restarted when they reach a certain age and therefore get rid of memory leaks.
- [ ] Handle server shutdown (cancellation token) in concurrency middlewares.


## APIs

- [ ] Request body (upload) should be below 1MB (Nginx and NATS have the same default limit). Larger uploads have to uploaded directly to object storage via signed URLs: https://aws.amazon.com/blogs/compute/uploading-to-amazon-s3-directly-from-a-web-or-mobile-application/
- [ ] Response body size limit... let's make it 10MB! (because then we hit serialization problems and JSON problems anyways)
