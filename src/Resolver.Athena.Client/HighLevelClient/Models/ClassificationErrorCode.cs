namespace Resolver.Athena.Client.HighLevelClient.Models;

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
