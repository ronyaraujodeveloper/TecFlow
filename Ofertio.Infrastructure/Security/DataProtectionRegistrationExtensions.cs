using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Ofertio.Util.Security;

namespace Ofertio.Infrastructure.Security;

public static class DataProtectionRegistrationExtensions
{
    /// <summary>
    /// Discriminador dos payloads <c>DP1:</c> já gravados no banco; trocar este valor torna ilegíveis os tokens de marketplace.
    /// </summary>
    public const string ApplicationName = "TecFlow";
    public const string KeysPathSetting = "DataProtection:KeysPath";
    public const string DefaultKeysFolder = "Ofertio";

    /// <summary>
    /// Chaves em <c>DataProtection:KeysPath</c> (relativo à raiz da aplicação, ex.: <c>App_Data/OfertioDataProtectionKeys</c>);
    /// o deploy preserva <c>App_Data</c>, e perder esse diretório torna ilegíveis os tokens gravados com <c>DP1:</c>.
    /// </summary>
    public static IServiceCollection AddOfertioDataProtection(this IServiceCollection services, IConfiguration configuration)
    {
        if (services.Any(descriptor => descriptor.ServiceType == typeof(ISecurityEncryptionService)))
        {
            return services;
        }

        var keysPath = ResolveKeysPath(configuration);
        Directory.CreateDirectory(keysPath);

        var builder = services.AddDataProtection()
            .SetApplicationName(ApplicationName)
            .PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        if (OperatingSystem.IsWindows())
        {
            builder.ProtectKeysWithDpapi(protectToLocalMachine: true);
        }

        services.AddSingleton<ISecurityEncryptionService, SecurityEncryptionService>();
        return services;
    }

    public static string ResolveKeysPath(IConfiguration configuration)
    {
        var configured = configuration[KeysPathSetting];
        if (string.IsNullOrWhiteSpace(configured))
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                DefaultKeysFolder,
                "DataProtectionKeys");
        }

        var path = configured.Trim();
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var contentRoot = configuration[HostDefaults.ContentRootKey];
        return Path.GetFullPath(Path.Combine(
            string.IsNullOrWhiteSpace(contentRoot) ? AppContext.BaseDirectory : contentRoot,
            path));
    }
}
