namespace Resolver.Athena;

public class AthenaClientConfiguration
{
    public required string Endpoint { get; set; }
    public required string Affiliate { get; set; }
    public bool SendMd5Hash { get; set; } = false;
    public bool SendSha1Hash { get; set; } = false;
}
