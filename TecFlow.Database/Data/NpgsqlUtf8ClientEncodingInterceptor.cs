using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace TecFlow.Database.Data;

/// <summary>
/// Garante CLIENT_ENCODING UTF8 em cada conexão PostgreSQL aberta pelo EF Core.
/// </summary>
public sealed class NpgsqlUtf8ClientEncodingInterceptor : DbConnectionInterceptor
{
    private const string SetClientEncodingSql = "SET CLIENT_ENCODING TO 'UTF8';";

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData)
    {
        ApplyClientEncoding(connection);
        base.ConnectionOpened(connection, eventData);
    }

    public override async Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (connection is NpgsqlConnection npgsql && npgsql.State == System.Data.ConnectionState.Open)
        {
            await using var command = npgsql.CreateCommand();
            command.CommandText = SetClientEncodingSql;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
    }

    private static void ApplyClientEncoding(DbConnection connection)
    {
        if (connection is not NpgsqlConnection npgsql || npgsql.State != System.Data.ConnectionState.Open)
        {
            return;
        }

        using var command = npgsql.CreateCommand();
        command.CommandText = SetClientEncodingSql;
        command.ExecuteNonQuery();
    }
}
