using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TecFlow.Util.Security;

public static class EncryptionServiceCollectionExtensions
{
    public static IServiceCollection AddTecFlowEncryption(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var encryptionService = CreateEncryptionService(configuration);
        services.AddSingleton<IEncryptionService>(encryptionService);
        return services;
    }

    public static IEncryptionService CreateEncryptionService(IConfiguration configuration)
    {
        var key = configuration[$"{EncryptionOptions.SectionName}:Key"];
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException(
                "Encryption:Key não configurada para design-time/migrations.");
        }

        return new AesEncryptionService(key);
    }
}
