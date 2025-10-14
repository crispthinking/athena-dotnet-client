namespace Resolver.Athena.Models;

public record ClassificationErrorDetails
{
    public required ClassificationErrorCode Code { get; set; }
    public required string Message { get; set; }

    public string? AdditionalDetails { get; set; }
}
