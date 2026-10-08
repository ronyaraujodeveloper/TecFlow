namespace TecFlow.Business.Integrations.Amazon;

public sealed class AmazonPaApiOptions
{
    public const string SectionName = "Integrations:AmazonPaApi";

    public string AccessKey { get; set; } = string.Empty;

    public string SecretKey { get; set; } = string.Empty;

    public string PartnerTag { get; set; } = string.Empty;

    public string Host { get; set; } = "webservices.amazon.com.br";

    public string Region { get; set; } = "us-east-1";

    public string Marketplace { get; set; } = "www.amazon.com.br";

    public bool HasCredentials =>
        !string.IsNullOrWhiteSpace(AccessKey)
        && !string.IsNullOrWhiteSpace(SecretKey)
        && !string.IsNullOrWhiteSpace(PartnerTag);
}
