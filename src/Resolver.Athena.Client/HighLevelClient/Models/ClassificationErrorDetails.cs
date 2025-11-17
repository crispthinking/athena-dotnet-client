namespace Resolver.Athena.Client.HighLevelClient.Models;

/// <summary>
/// Details of a classification error.
/// </summary>
public sealed record ClassificationErrorDetails
{
    /// <summary>
    /// The error code.
    /// </summary>
    public required ClassificationErrorCode Code { get; set; }

    /// <summary>
    /// The error message.
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Additional details about the error.
    /// </summary>
    public string? AdditionalDetails { get; set; }
}
