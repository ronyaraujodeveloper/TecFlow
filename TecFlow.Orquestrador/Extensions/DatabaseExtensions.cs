using Microsoft.EntityFrameworkCore;
using TecFlow.Database;

namespace TecFlow.Orquestrador.Extensions;

public static class DatabaseExtensions
{
    public static async Task ApplyDatabaseMigrationsAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger("TecFlow.Database");

        try
        {
            logger.LogInformation("Aplicando migrations PostgreSQL...");
            await db.Database.MigrateAsync();
            logger.LogInformation("Migrations aplicadas com sucesso.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Falha ao aplicar migrations.");
            throw;
        }
    }
}
