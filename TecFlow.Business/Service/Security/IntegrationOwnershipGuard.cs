namespace TecFlow.Business.Service.Security;

public static class IntegrationOwnershipGuard
{
    public static void EnsureOwner(int integrationUserId, int currentUserId)
    {
        if (integrationUserId != currentUserId)
        {
            throw new UnauthorizedAccessException();
        }
    }
}

public static class SecretMasking
{
    public const string MaskedValue = "****************";

    public static string Mask(string? secret) =>
        string.IsNullOrEmpty(secret) ? string.Empty : MaskedValue;
}
