namespace TecFlow.Business.Integrations.Auth;

public static class HomologMarketplaceAuth
{
    public const string StubAuthorizationCode = "code_teste";
    public const string StubAccessToken = "homolog-test-access-token";
    public const string StubRefreshToken = "homolog-test-refresh-token";
    public const int StubAccessTokenLifetimeSeconds = 60 * 60 * 24 * 30;

    public static bool IsStubAuthorizationCode(string? code) =>
        string.Equals(code?.Trim(), StubAuthorizationCode, StringComparison.OrdinalIgnoreCase);
}
