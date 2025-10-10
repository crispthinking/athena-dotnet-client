using Grpc.Core;
using Moq;
using Resolver.Athena.Grpc;

namespace Resolver.Athena.Tests.AthenaClientTests;

public class ListDeploymentsTests : AthenaClientTestsBase
{
    [Fact]
    public async Task ListDeploymentsAsync_WithEmptyResponse()
    {
        // Arrange
        _mockGrpcClient
            .Setup(client => client
                    .ListDeploymentsAsync(It.IsAny<Google.Protobuf.WellKnownTypes.Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(new ListDeploymentsResponse()));

        // Act
        var deployments = await _athenaClient.ListDeploymentsAsync(CancellationToken.None);

        // Assert
        Assert.Empty(deployments);
    }

    [Fact]
    public async Task ListDeploymentsAsync_WithSingleResponse()
    {
        // Arrange
        var deployment = new Deployment
        {
            DeploymentId = "deployment-1",
            Backlog = 10,
        };
        var response = new ListDeploymentsResponse();
        response.Deployments.Add(deployment);
        _mockGrpcClient
            .Setup(client => client
                    .ListDeploymentsAsync(It.IsAny<Google.Protobuf.WellKnownTypes.Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(response));

        // Act
        var deployments = await _athenaClient.ListDeploymentsAsync(CancellationToken.None);

        // Assert
        var singleDeployment = Assert.Single(deployments);
        Assert.Equal("deployment-1", singleDeployment.DeploymentId);
        Assert.Equal(10, singleDeployment.Backlog);
    }

    [Fact]
    public async Task ListDeploymentsAsync_WithMultiResponse()
    {
        // Arrange
        var response = new ListDeploymentsResponse();
        var expectedDeployments = 5;

        for (var i = 0; i < expectedDeployments; i++)
        {
            var deployment = new Deployment
            {
                DeploymentId = $"deployment-{i}",
                Backlog = i * 10,
            };
            response.Deployments.Add(deployment);
        }

        _mockGrpcClient
            .Setup(client => client
                    .ListDeploymentsAsync(It.IsAny<Google.Protobuf.WellKnownTypes.Empty>(), It.IsAny<Metadata>(), It.IsAny<DateTime?>(), It.IsAny<CancellationToken>()))
            .Returns(CreateAsyncUnaryCall(response));

        // Act
        var deployments = await _athenaClient.ListDeploymentsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(5, deployments.Count);
        for (var i = 0; i < expectedDeployments; i++)
        {
            var thisDeployment = Assert.Single(deployments, d => d.DeploymentId == $"deployment-{i}");
            Assert.Equal(i * 10, thisDeployment.Backlog);
        }
    }
}
