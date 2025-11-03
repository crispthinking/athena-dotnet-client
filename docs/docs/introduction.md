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
  are sent across that. Multiple clients are implemented to suit different use cases:
  - **IAsyncEnumerable Client**: A high level client that takes an `IAsyncEnumerable` and
    produces an `IAsyncEnumerable` of results. This client is suitable for most use cases
    and is the easiest to use. This client is implemented in the [AthenaClient](xref:Resolver.Athena.AthenaClient)
    class via the `ClassifyAsync` method.
  - **Low Level Channel-Based Client**: A low level client that takes in a
    channel for sending images and returns a channel for receiving results.
    This client is suitable for users who need more control over the sending
    and receiving of images and results. This client is implemented in the
    [AthenaApiClient](xref:Resolver.AthenaApiClient.Clients.AthenaApiClient) class via the
    `ClassifyAsync` method.
  - **TPL Dataflow Client**: A high-level client that uses TPL Dataflow blocks to
    manage the sending and receiving of images and results. This client is
    suitable for users who are familiar with TPL Dataflow and want to use it
    to manage their classification pipeline. This client is implemented in the
    [AthenaDataflowClient](xref:Resolver.AthenaDataflowClient.Clients.AthenaDataflowClient) class.
    This client is packaged separately in the `Resolver.Athena.DataflowClient` NuGet package.
