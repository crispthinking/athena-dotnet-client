using Resolver.Athena.Grpc;

namespace Resolver.Athena.Models;

/// <summary>
/// Extension methods for ClassificationErrorCode.
/// </summary>
public static class ClassificationErrorCodeExtensions
{
    /// <summary>
    /// Maps gRPC ErrorCode to ClassificationErrorCode.
    /// </summary>
    /// <param name="code">The gRPC error code.</param>
    /// <returns>The corresponding ClassificationErrorCode.</returns>
    public static ClassificationErrorCode FromGrpc(ErrorCode code) =>
        code switch
        {
            ErrorCode.ImageTooLarge => ClassificationErrorCode.ImageTooLarge,
            ErrorCode.ModelError => ClassificationErrorCode.ModelError,
            ErrorCode.AffiliateNotPermitted => ClassificationErrorCode.AffiliateNotPermitted,
            ErrorCode.DeploymentIdInvalid => ClassificationErrorCode.DeploymentIdInvalid,
            ErrorCode.CorrelationIdInvalid => ClassificationErrorCode.CorrelationIdInvalid,
            _ => ClassificationErrorCode.Unspecified,
        };
}
