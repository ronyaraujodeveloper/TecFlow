using Microsoft.Extensions.Configuration;

namespace TecFlow.Infrastructure.Services;

/// <summary>
/// Aplica DATABASE_URL (Railway/Render) sobre a configuração local do PostgreSQL.
/// </summary>
public static class DatabaseUrlConfiguration
{
    public static void ApplyCloudDatabaseUrl(IConfigurationManager configuration)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            return;
        }

        configuration["ConnectionStrings:DefaultConnection"] = ParseDatabaseUrl(databaseUrl);
        configuration["Database:Provider"] = "PostgreSQL";
    }

    public static void ConfigureAppConfiguration(IConfigurationBuilder configurationBuilder)
    {
        var databaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
        if (string.IsNullOrWhiteSpace(databaseUrl))
        {
            return;
        }

        configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = ParseDatabaseUrl(databaseUrl),
            ["Database:Provider"] = "PostgreSQL"
        });
    }

    public static string ParseDatabaseUrl(string databaseUrl)
    {
        if (databaseUrl.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || databaseUrl.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(databaseUrl);
            var userInfo = uri.UserInfo.Split(':', 2);
            var username = Uri.UnescapeDataString(userInfo[0]);
            var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
            var database = uri.AbsolutePath.TrimStart('/');

            return $"Host={uri.Host};Port={uri.Port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true";
        }

        return databaseUrl;
    }
}
