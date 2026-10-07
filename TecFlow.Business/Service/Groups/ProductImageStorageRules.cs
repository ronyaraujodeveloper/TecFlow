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

    public static string? ToWebRelativePath(string? storedUrl)
    {
        if (string.IsNullOrWhiteSpace(storedUrl))
        {
            return storedUrl;
        }

        var value = storedUrl.Trim().Replace('\\', '/');
        var wwwRoot = value.IndexOf("wwwroot/", StringComparison.OrdinalIgnoreCase);
        if (wwwRoot >= 0)
        {
            value = value[(wwwRoot + "wwwroot".Length)..];
        }

        var uploads = value.IndexOf("/uploads/products/", StringComparison.OrdinalIgnoreCase);
        if (uploads < 0)
        {
            uploads = value.IndexOf("uploads/products/", StringComparison.OrdinalIgnoreCase);
        }

        if (uploads >= 0)
        {
            value = value[uploads..];
            if (!value.StartsWith('/'))
            {
                value = "/" + value;
            }

            return value;
        }

        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return value.StartsWith('/') ? value : "/" + value.TrimStart('/');
    }

    public static string? TryResolveTenantProductsFolder(string webRootPath, int tenantId)
    {
        if (string.IsNullOrWhiteSpace(webRootPath) || tenantId <= 0)
        {
            return null;
        }

        var root = Path.GetFullPath(Path.Combine(webRootPath, "uploads", "products"));
        var folder = Path.GetFullPath(Path.Combine(root, tenantId.ToString()));
        if (!folder.StartsWith(root, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return folder;
    }

    public static int TryDeleteTenantProductFiles(string? tenantFolder)
    {
        if (string.IsNullOrWhiteSpace(tenantFolder) || !Directory.Exists(tenantFolder))
        {
            return 0;
        }

        string[] files;
        try
        {
            files = Directory.GetFiles(tenantFolder, "*.*", SearchOption.AllDirectories);
        }
        catch (IOException)
        {
            return 0;
        }
        catch (UnauthorizedAccessException)
        {
            return 0;
        }

        var deleted = 0;
        foreach (var file in files)
        {
            try
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
                deleted++;
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        return deleted;
    }

    public static string BuildPhysicalDirectory(string webRootPath, int tenantId, DateTime utcNow)
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
        var digits = string.IsNullOrWhiteSpace(messageId)
            ? string.Empty
            : new string(messageId.Where(char.IsDigit).ToArray());
        if (long.TryParse(digits, out var telegramId) && telegramId > 0)
        {
            return $"{telegramId}.jpg";
        }

        return "0.jpg";
    }

    public static string ResolveWebRoot(string? webRootPath, string? contentRootPath = null, string? baseDirectory = null)
    {
        if (!string.IsNullOrWhiteSpace(webRootPath))
        {
            return webRootPath;
        }

        if (!string.IsNullOrWhiteSpace(contentRootPath))
        {
            return Path.Combine(contentRootPath, "wwwroot");
        }

        var root = string.IsNullOrWhiteSpace(baseDirectory)
            ? AppDomain.CurrentDomain.BaseDirectory
            : baseDirectory;
        return Path.Combine(root, "wwwroot");
    }

    public static (string AbsoluteDir, string AbsoluteFilePath, string WebRelativeUrl) BuildSaveTarget(
        string webRootPath,
        int tenantId,
        string fileName,
        DateTime utcNow)
    {
        var stamp = utcNow.ToUniversalTime();
        var relativeDir = Path.Combine("uploads", "products", tenantId.ToString(), stamp.ToString("yyyy/MM"));
        var absoluteDir = Path.Combine(webRootPath, relativeDir);
        var absoluteFilePath = Path.Combine(absoluteDir, fileName);
        var webRelativeUrl = $"/{relativeDir}/{fileName}".Replace('\\', '/');
        return (absoluteDir, absoluteFilePath, ToWebRelativePath(webRelativeUrl) ?? webRelativeUrl);
    }

    public static string? EnsureLeadingSlash(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return url;
        }

        var value = url.Trim().Replace('\\', '/');
        if (value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        return value.StartsWith('/') ? value : "/" + value.TrimStart('/');
    }

    public static string? TryFindPhotoForMessageId(string webRootPath, string? telegramMessageId)
    {
        if (string.IsNullOrWhiteSpace(webRootPath) || string.IsNullOrWhiteSpace(telegramMessageId))
        {
            return null;
        }

        var root = Path.Combine(webRootPath, "uploads", "products");
        if (!Directory.Exists(root))
        {
            return null;
        }

        var tokens = new[]
            {
                telegramMessageId.Trim(),
                new string(telegramMessageId.Where(char.IsLetterOrDigit).ToArray())
            }
            .Where(token => token.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(token => token.IndexOfAny(Path.GetInvalidFileNameChars()) < 0);

        string? newest = null;
        var newestStamp = DateTime.MinValue;
        foreach (var token in tokens)
        {
            string[] matches;
            try
            {
                matches =
                [
                    .. Directory.GetFiles(root, $"{token}.jpg", SearchOption.AllDirectories),
                    .. Directory.GetFiles(root, $"{token}_*.jpg", SearchOption.AllDirectories)
                ];
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var match in matches)
            {
                var stamp = File.GetLastWriteTimeUtc(match);
                if (newest is null || stamp > newestStamp)
                {
                    newest = match;
                    newestStamp = stamp;
                }
            }
        }

        return newest is null ? null : EnsureLeadingSlash(ToWebRelativePath(newest));
    }

    public static bool FileExistsOnDisk(string webRootPath, string? storedUrl)
    {
        var physical = TryResolvePhysicalPath(webRootPath, storedUrl);
        return physical is not null && File.Exists(physical);
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
        relative = ToWebRelativePath(relative) ?? relative;
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

    public static bool TryParseMessageIdFromFileName(string? fileName, out long messageId)
    {
        messageId = 0;
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var name = Path.GetFileNameWithoutExtension(fileName.Trim());
        if (long.TryParse(name, out messageId) && messageId > 0)
        {
            return true;
        }

        var separator = name.IndexOf('_');
        if (separator <= 0)
        {
            return false;
        }

        return long.TryParse(name[..separator], out messageId) && messageId > 0;
    }

    public static bool TryParseTenantIdFromPhysicalPath(string webRootPath, string physicalPath, out int tenantId)
    {
        tenantId = 0;
        var relative = ToWebRelativePath(physicalPath);
        if (string.IsNullOrWhiteSpace(relative) || !relative.StartsWith(RelativeRoot, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var parts = relative.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 3 && int.TryParse(parts[2], out tenantId) && tenantId > 0;
    }

    public sealed record ExistingProductPhoto(long MessageId, int TenantId, string WebRelativeUrl, DateTime LastWriteUtc);

    public static IEnumerable<ExistingProductPhoto> EnumerateExistingPhotos(string webRootPath)
    {
        if (string.IsNullOrWhiteSpace(webRootPath))
        {
            yield break;
        }

        var root = Path.Combine(webRootPath, "uploads", "products");
        if (!Directory.Exists(root))
        {
            yield break;
        }

        foreach (var file in Directory.EnumerateFiles(root, "*.jpg", SearchOption.AllDirectories))
        {
            if (!TryParseMessageIdFromFileName(file, out var messageId)
                || !TryParseTenantIdFromPhysicalPath(webRootPath, file, out var tenantId))
            {
                continue;
            }

            var webUrl = ToWebRelativePath(file);
            if (string.IsNullOrWhiteSpace(webUrl) || !IsLocalProductImage(webUrl))
            {
                continue;
            }

            var stamp = File.GetLastWriteTimeUtc(file);
            yield return new ExistingProductPhoto(messageId, tenantId, webUrl, stamp);
        }
    }
}
