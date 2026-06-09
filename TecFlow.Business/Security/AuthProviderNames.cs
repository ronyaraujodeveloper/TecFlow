namespace TecFlow.Business.Security;

/// <summary>Normaliza nomes de provedores de autenticação para persistência em AspNetUserLogins.</summary>
public static class AuthProviderNames
{
    public const string Google = "Google";
    public const string Facebook = "Facebook";
    public const string Apple = "Apple";
    public const string EmailPassword = "EmailPassword";

    public static string? Normalize(string? provider) => provider?.Trim() switch
    {
        Google or "google" => Google,
        Facebook or "facebook" => Facebook,
        Apple or "apple" or "ICloud" or "icloud" => Apple,
        EmailPassword or "emailpassword" => EmailPassword,
        _ => null
    };

    public static bool IsSocialProvider(string provider) =>
        provider is Google or Facebook or Apple;
}
