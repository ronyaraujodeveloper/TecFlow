using Microsoft.Extensions.Configuration;
using TecFlow.Infrastructure.Services;

namespace TecFlow.Tests.Unit;

public class DatabaseUrlConfigurationTests
{
    [Fact]
    public void ParseDatabaseUrl_ConvertsPostgresUri_ToNpgsqlConnectionString()
    {
        const string databaseUrl = "postgres://Us_Automacao:tecban321%40@db.example.com:5432/automacaosociais";

        var connectionString = DatabaseUrlConfiguration.ParseDatabaseUrl(databaseUrl);

        Assert.Contains("Host=db.example.com", connectionString);
        Assert.Contains("Port=5432", connectionString);
        Assert.Contains("Database=automacaosociais", connectionString);
        Assert.Contains("Username=Us_Automacao", connectionString);
        Assert.Contains("Password=tecban321@", connectionString);
        Assert.Contains("SSL Mode=Require", connectionString);
    }

    [Fact]
    public void ParseDatabaseUrl_AcceptsPostgresqlScheme()
    {
        const string databaseUrl = "postgresql://user:secret@127.0.0.1:5432/mydb";

        var connectionString = DatabaseUrlConfiguration.ParseDatabaseUrl(databaseUrl);

        Assert.Contains("Host=127.0.0.1", connectionString);
        Assert.Contains("Database=mydb", connectionString);
    }

    [Fact]
    public void ParseDatabaseUrl_ReturnsInput_WhenAlreadyNpgsqlFormat()
    {
        const string connectionString = "Host=127.0.0.1;Port=5432;Database=automacaosociais";

        var result = DatabaseUrlConfiguration.ParseDatabaseUrl(connectionString);

        Assert.Equal(connectionString, result);
    }

    [Fact]
    public void ApplyCloudDatabaseUrl_OverridesConfiguration_WhenDatabaseUrlIsSet()
    {
        const string databaseUrl = "postgres://Us_Automacao:tecban321%40@127.0.0.1:5432/automacaosociais";
        Environment.SetEnvironmentVariable("DATABASE_URL", databaseUrl);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=old;Database=old",
                    ["Database:Provider"] = "SqlServer"
                })
                .Build()
                .AsConfigurationManager();

            DatabaseUrlConfiguration.ApplyCloudDatabaseUrl(configuration);

            Assert.Equal("PostgreSQL", configuration["Database:Provider"]);
            Assert.Contains("127.0.0.1", configuration.GetConnectionString("DefaultConnection"));
            Assert.Contains("automacaosociais", configuration.GetConnectionString("DefaultConnection"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", null);
        }
    }

    [Fact]
    public void ConfigureAppConfiguration_AppliesDatabaseUrl_OnWorkerHost()
    {
        const string databaseUrl = "postgres://Us_Automacao:pass@postgres:5432/automacaosociais";
        Environment.SetEnvironmentVariable("DATABASE_URL", databaseUrl);

        try
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true)
                .Build();

            var configurationBuilder = new ConfigurationBuilder()
                .AddConfiguration(configuration);

            DatabaseUrlConfiguration.ConfigureAppConfiguration(configurationBuilder);

            var merged = configurationBuilder.Build();

            Assert.Equal("PostgreSQL", merged["Database:Provider"]);
            Assert.Contains("Host=postgres", merged.GetConnectionString("DefaultConnection"));
        }
        finally
        {
            Environment.SetEnvironmentVariable("DATABASE_URL", null);
        }
    }
}

internal static class ConfigurationExtensions
{
    public static IConfigurationManager AsConfigurationManager(this IConfiguration configuration)
    {
        var manager = new ConfigurationManager();
        manager.AddConfiguration(configuration);
        return manager;
    }
}
