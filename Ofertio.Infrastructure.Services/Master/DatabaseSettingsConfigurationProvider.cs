using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Ofertio.Infrastructure.Security;

namespace Ofertio.Infrastructure.Services.Master;

/// <summary>Carrega as chaves globais do cofre (tabela SystemSettings) por cima do appsettings.</summary>
public sealed class DatabaseSettingsConfigurationProvider : ConfigurationProvider
{
    private readonly string _connectionString;
    private readonly string _encryptionKey;

    public DatabaseSettingsConfigurationProvider(string connectionString, string encryptionKey)
    {
        _connectionString = connectionString;
        _encryptionKey = encryptionKey;
    }

    public string? LastError { get; private set; }

    public override void Load()
    {
        var data = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        try
        {
            var encryption = new DataEncryptionService(_encryptionKey);
            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText =
                "IF OBJECT_ID(N'dbo.SystemSettings', N'U') IS NOT NULL SELECT [Key], [Value] FROM dbo.SystemSettings";
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var key = reader.GetString(0);
                var raw = reader.IsDBNull(1) ? null : reader.GetString(1);
                if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(raw))
                {
                    continue;
                }

                var value = encryption.Decrypt(raw);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    data[key] = value;
                }
            }

            LastError = null;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
        }

        Data = data;
    }

    public void Reload()
    {
        Load();
        OnReload();
    }
}

public sealed class DatabaseSettingsConfigurationSource : IConfigurationSource
{
    private readonly string _connectionString;
    private readonly string _encryptionKey;

    public DatabaseSettingsConfigurationSource(string connectionString, string encryptionKey)
    {
        _connectionString = connectionString;
        _encryptionKey = encryptionKey;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new DatabaseSettingsConfigurationProvider(_connectionString, _encryptionKey);
}

public static class DatabaseSettingsConfigurationExtensions
{
    /// <summary>Adiciona o cofre do banco quando SQL Server, conexão e chave de criptografia existem.</summary>
    public static IConfigurationManager AddOfertioDatabaseSettings(this IConfigurationManager configuration)
    {
        var provider = configuration["Database:Provider"] ?? "SqlServer";
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        var encryptionKey = configuration["Encryption:Key"];
        if (!string.Equals(provider, "SqlServer", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(connectionString)
            || string.IsNullOrWhiteSpace(encryptionKey))
        {
            return configuration;
        }

        ((IConfigurationBuilder)configuration).Add(new DatabaseSettingsConfigurationSource(connectionString, encryptionKey));
        return configuration;
    }

    public static void ReloadOfertioDatabaseSettings(this IConfiguration configuration)
    {
        if (configuration is not IConfigurationRoot root)
        {
            return;
        }

        foreach (var provider in root.Providers.OfType<DatabaseSettingsConfigurationProvider>())
        {
            provider.Reload();
        }
    }
}
