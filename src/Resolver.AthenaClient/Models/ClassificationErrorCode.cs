namespace Resolver.AthenaClient.Models;

/// <summary>
/// Enumeration of classification error codes.
/// </summary>
public enum ClassificationErrorCode
{
    Unspecified,
    ImageTooLarge,
    ModelError,
    AffiliateNotPermitted,
    DeploymentIdInvalid,
    CorrelationIdInvalid,
}
