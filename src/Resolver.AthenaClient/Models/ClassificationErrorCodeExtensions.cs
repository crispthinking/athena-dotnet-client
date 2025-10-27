using Resolver.Athena.Grpc;

namespace Resolver.AthenaClient.Models;

/// <summary>
/// Extension helpers for mapping gRPC error codes to domain codes.
/// </summary>
public static class ClassificationErrorCodeExtensions
{
    /// <summary>
    /// Maps gRPC <see cref="ErrorCode"/> values to domain codes.
    /// </summary>
    public static ClassificationErrorCode FromGrpc(ErrorCode code) => code switch
    {
        ErrorCode.ImageTooLarge => ClassificationErrorCode.ImageTooLarge,
        ErrorCode.ModelError => ClassificationErrorCode.ModelError,
        ErrorCode.AffiliateNotPermitted => ClassificationErrorCode.AffiliateNotPermitted,
        ErrorCode.DeploymentIdInvalid => ClassificationErrorCode.DeploymentIdInvalid,
        ErrorCode.CorrelationIdInvalid => ClassificationErrorCode.CorrelationIdInvalid,
        _ => ClassificationErrorCode.Unspecified,
    };
}
