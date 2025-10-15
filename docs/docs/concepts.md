# Core Athena Concepts

## Outputs

### Classifications
When images are processed by the Athena API, the output may consist of several
classifications. Each classification may represent either a confidence, or a
binary yes/no decision, both represented by single-precision floating point
values.

## Streaming

### Deployments
An Athena Deployment represents a logical grouping of requests and responses. A
deployment is created by the user, and will have a unique identifier. When
using the streaming API, requests are sent to a specific deployment, and
responses are received from that deployment.

Multiple clients can connect to a single deployment, and can send and receive
requests. In a configuration where multiple clients are connected to a single
deployment, the clients will receive responses for requests sent by any of the
connected clients.

Alternatively, you can create a unique deployment for each client. In this
configuration, each client will only receive responses for requests it has
sent, but in the case that a client disconnects, any responses for requests it
has sent would require a reconnection to that deployment in order to be
received.
