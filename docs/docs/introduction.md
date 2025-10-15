# Introduction

The Athena API is a high performance API for interacting with the Resolver
Unknown CSAM Detection Service.

Classification can be performed in two ways:
- **Synchronous**: A single image is sent to the API and a response is returned
  immediately. This is suitable for low volume use cases, cases where latency is
  more valuable than throughput, or for testing and evaluation. Users wishing to
  perform synchronous classification should use the `ClassifySingleImageAsync` method
  on the [AthenaClient](xref:Resolver.Athena.AthenaClient) class.
- **Asynchronous**: A bidirectional stream is opened to the API and images and responses
  are sent across that. Not Yet Implemented - TODO fill this out when it is.
