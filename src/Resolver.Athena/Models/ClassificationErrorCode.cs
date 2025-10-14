using Resolver.Athena.Grpc;

namespace Resolver.Athena.Models;

public enum ClassificationErrorCode
{
    Unspecified,
    ImageTooLarge,
    ModelError,
    AffiliateNotPermitted,
    DeploymentIdInvalid,
    CorrelationIdInvalid,
}

public static class ClassificationErrorCodeExtensions
{
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
