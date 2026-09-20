using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using TecFlow.Database.Data;
using TecFlow.Database.MultiTenancy;
using TecFlow.Util.Security;

namespace TecFlow.Database.Repositorio;

/// <summary>
/// Factory de design-time para migrations do EF Core (lê appsettings do projeto de inicialização).
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var basePath = ResolveSettingsPath();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection não configurada.");
        var provider = configuration.GetValue<string>("Database:Provider") ?? "PostgreSQL";

        var encryptionService = EncryptionServiceCollectionExtensions.CreateEncryptionService(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseConfiguredProvider(connectionString, provider);

        return new AppDbContext(optionsBuilder.Options, encryptionService, new NullCurrentTenantService());
    }

    private static string ResolveSettingsPath()
    {
        var current = Directory.GetCurrentDirectory();
        if (File.Exists(Path.Combine(current, "appsettings.json")))
        {
            return current;
        }

        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "../TecFlow.API"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../TecFlow.API"),
            Path.Combine(Directory.GetCurrentDirectory(), "../TecFlow.Orquestrador"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../TecFlow.Orquestrador"),
        };

        foreach (var path in candidates)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
            {
                return fullPath;
            }
        }

        throw new InvalidOperationException(
            "Não foi possível localizar appsettings.json do Orquestrador ou API.");
    }
}
