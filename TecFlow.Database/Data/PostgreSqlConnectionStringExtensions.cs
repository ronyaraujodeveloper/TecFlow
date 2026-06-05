namespace TecFlow.Database.Data;

public static class PostgreSqlConnectionStringExtensions
{
    public static string EnsureUtf8Encoding(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (connectionString.Contains("Encoding=UTF8", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var separator = connectionString.TrimEnd().EndsWith(';') ? string.Empty : ";";
        return $"{connectionString}{separator}Client Encoding=UTF8;Encoding=UTF8";
    }
}
