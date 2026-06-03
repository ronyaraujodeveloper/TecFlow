using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
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

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection não configurada.");

        var encryptionService = EncryptionServiceCollectionExtensions.CreateEncryptionService(configuration);

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly("TecFlow.Infrastructure"));

        return new AppDbContext(optionsBuilder.Options, encryptionService);
    }

    private static string ResolveSettingsPath()
    {
        var candidates = new[]
        {
            Path.Combine(Directory.GetCurrentDirectory(), "../TecFlow.Orquestrador"),
            Path.Combine(Directory.GetCurrentDirectory(), "../../TecFlow.Orquestrador"),
            Path.Combine(Directory.GetCurrentDirectory(), "../TecFlow.API"),
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
