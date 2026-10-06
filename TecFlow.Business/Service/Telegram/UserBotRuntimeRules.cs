namespace TecFlow.Business.Service.Telegram;

public static class UserBotRuntimeRules
{
    public const string UploadsRelative = "uploads/products";
    public const string AppDataSessionsRelative = "App_Data/telegram-sessions";

    public static string ResolveWebRoot(string? webRootPath, string? baseDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(webRootPath))
        {
            return webRootPath;
        }

        var root = string.IsNullOrWhiteSpace(baseDirectory)
            ? AppDomain.CurrentDomain.BaseDirectory
            : baseDirectory;
        return Path.Combine(root, "wwwroot");
    }

    public static string ResolveUploadsPath(string webRoot) =>
        Path.Combine(webRoot, "uploads", "products");

    public static string ResolveAppDataSessionsPath(string? baseDirectory = null)
    {
        var root = string.IsNullOrWhiteSpace(baseDirectory)
            ? AppDomain.CurrentDomain.BaseDirectory
            : baseDirectory;
        return Path.Combine(root, "App_Data", "telegram-sessions");
    }

    public static (string UploadsPath, string SessionsPath) EnsureLocalFolders(
        string? webRootPath,
        string? baseDirectory = null)
    {
        var webRoot = ResolveWebRoot(webRootPath, baseDirectory);
        var uploadsPath = ResolveUploadsPath(webRoot);
        var sessionsPath = ResolveAppDataSessionsPath(baseDirectory);
        Directory.CreateDirectory(uploadsPath);
        Directory.CreateDirectory(sessionsPath);
        return (uploadsPath, sessionsPath);
    }
}
