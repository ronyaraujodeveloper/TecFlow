namespace TecFlow.Business.Service.Groups;

public static class ProductImageStorageRules
{
    public const string RelativeRoot = "/uploads/products";
    public static readonly TimeSpan Retention = TimeSpan.FromDays(15);

    public static string BuildRelativeUrl(int tenantId, DateTime utcNow, string fileName)
    {
        var stamp = utcNow.ToUniversalTime();
        var name = string.IsNullOrWhiteSpace(fileName) ? "foto.jpg" : fileName.Trim();
        return $"{RelativeRoot}/{tenantId}/{stamp:yyyy}/{stamp:MM}/{name}";
    }

    public static string BuildPhysicalFolder(string webRootPath, int tenantId, DateTime utcNow)
    {
        var stamp = utcNow.ToUniversalTime();
        return Path.Combine(
            webRootPath,
            "uploads",
            "products",
            tenantId.ToString(),
            stamp.ToString("yyyy/MM"));
    }

    public static string BuildFileName(string? messageId)
    {
        var safeMessage = string.IsNullOrWhiteSpace(messageId)
            ? "msg"
            : new string(messageId.Where(char.IsLetterOrDigit).ToArray());
        if (string.IsNullOrWhiteSpace(safeMessage))
        {
            safeMessage = "msg";
        }

        return $"{safeMessage}_{Guid.NewGuid().ToString("N")[..8]}.jpg";
    }

    public static bool IsLocalProductImage(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        var trimmed = url.Trim();
        if (trimmed.StartsWith(RelativeRoot, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
            && uri.AbsolutePath.StartsWith(RelativeRoot, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsExpired(DateTime createdAtUtc, DateTime utcNow) =>
        createdAtUtc.ToUniversalTime() <= utcNow.ToUniversalTime().Subtract(Retention);

    public static string? TryResolvePhysicalPath(string webRootPath, string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(webRootPath) || string.IsNullOrWhiteSpace(storedUrl))
        {
            return null;
        }

        var relative = storedUrl.Trim();
        if (Uri.TryCreate(relative, UriKind.Absolute, out var uri))
        {
            relative = uri.AbsolutePath;
        }

        if (!relative.StartsWith(RelativeRoot, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var combined = Path.GetFullPath(Path.Combine(
            webRootPath,
            relative.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)));
        var root = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "products"));
        if (!combined.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return combined;
    }
}
