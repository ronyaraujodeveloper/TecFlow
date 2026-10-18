using Microsoft.EntityFrameworkCore;

namespace Ofertio.Database.Data;

public static class RelationalDatabaseOptions
{
    public const string SqlServerMigrationsAssembly = "Ofertio.Data";
    public const string PostgreSqlMigrationsAssembly = "Ofertio.Infrastructure";

    public static bool IsSqlServer(string? provider) =>
        !string.IsNullOrWhiteSpace(provider)
        && (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("SQLServer", StringComparison.OrdinalIgnoreCase)
            || provider.Equals("MSSQL", StringComparison.OrdinalIgnoreCase));

    public static void UseConfiguredProvider(
        this DbContextOptionsBuilder options,
        string connectionString,
        string? provider)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentNullException(nameof(connectionString));
        }

        if (IsSqlServer(provider))
        {
            options.UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(SqlServerMigrationsAssembly));
            return;
        }

        options.AddInterceptors(new NpgsqlUtf8ClientEncodingInterceptor());
        options.UseNpgsql(
            PostgreSqlConnectionStringExtensions.EnsureUtf8Encoding(connectionString),
            npgsql => npgsql.MigrationsAssembly(PostgreSqlMigrationsAssembly));
    }
}
