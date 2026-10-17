using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TecFlow.Util.Security;

namespace TecFlow.Infrastructure.Security;

public static class DataProtectionRegistrationExtensions
{
    public const string ApplicationName = "TecFlow";
    public const string KeysPathSetting = "DataProtection:KeysPath";

    /// <summary>
    /// Chaves fora da pasta publicada (o deploy limpa C:\inetpub\tecflow\*) e compartilhadas por API, Worker e Orquestrador;
    /// perder esse diretório torna ilegíveis os tokens gravados com <c>DP1:</c>.
    /// </summary>
    public static IServiceCollection AddTecFlowDataProtection(this IServiceCollection services, IConfiguration configuration)
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
        return string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                ApplicationName,
                "DataProtectionKeys")
            : configured.Trim();
    }
}
