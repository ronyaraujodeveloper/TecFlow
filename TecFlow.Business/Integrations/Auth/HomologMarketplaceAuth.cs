namespace TecFlow.Business.Integrations.Auth;

public static class HomologMarketplaceAuth
{
    public const string StubAuthorizationCode = "code_teste";
    public const string StubAccessToken = "homolog-test-access-token";
    public const string StubRefreshToken = "homolog-test-refresh-token";
    public const int StubAccessTokenLifetimeSeconds = 60 * 60 * 24 * 30;
    public const string ManualLinkSuccessMessage =
        "Loja vinculada manualmente com sucesso em modo de homologação.";

    /// <summary>UserId usado em IIS/homologação quando o JWT não traz NameIdentifier.</summary>
    public const int FallbackUserId = 1;

    public const string StubCodePrefix = "code_";

    public static bool IsStubAuthorizationCode(string? code) => IsHomologStubCode(code);

    public static bool IsHomologStubCode(string? code)
    {
        var value = code?.Trim();
        return !string.IsNullOrEmpty(value)
            && value.StartsWith(StubCodePrefix, StringComparison.OrdinalIgnoreCase);
    }

    public static bool ShouldSkipRemoteOAuth(string? environmentName, string? authorizationCode)
    {
        if (IsHomologStubCode(authorizationCode))
        {
            return true;
        }

        return string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase)
            || string.Equals(environmentName, "Homologacao", StringComparison.OrdinalIgnoreCase);
    }
}
