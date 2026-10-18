namespace Ofertio.Business.Service.Groups;

public static class UploadStorageRules
{
    public const string RootPathSetting = "UploadStorage:RootPath";
    public const string RequestPath = "/uploads";
    public const string FolderName = "uploads";

    /// <summary>Pasta física servida em <c>/uploads</c>: configurada (ex.: <c>C:\inetpub\ofertio\uploads</c>) ou <c>{WebRootPath}/uploads</c>.</summary>
    public static string ResolveUploadsRoot(string? configuredRoot, string? webRootPath, string? contentRootPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredRoot))
        {
            var configured = Path.GetFullPath(configuredRoot.Trim());
            if (!string.Equals(Path.GetFileName(configured.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)), FolderName, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"{RootPathSetting} deve terminar na pasta '{FolderName}' (as URLs gravadas no banco começam com {RequestPath}/). Valor: {configured}");
            }

            return configured.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return Path.GetFullPath(Path.Combine(ProductImageStorageRules.ResolveWebRoot(webRootPath, contentRootPath), FolderName));
    }

    /// <summary>
    /// Raiz onde as URLs <c>/uploads/...</c> resolvem (pai da pasta de uploads); é o valor esperado pelos parâmetros
    /// <c>webRootPath</c> de <see cref="ProductImageStorageRules"/>.
    /// </summary>
    public static string ResolveMediaRoot(string uploadsRoot) =>
        Path.GetDirectoryName(uploadsRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        ?? throw new InvalidOperationException($"Pasta de uploads sem diretório pai: {uploadsRoot}");
}
