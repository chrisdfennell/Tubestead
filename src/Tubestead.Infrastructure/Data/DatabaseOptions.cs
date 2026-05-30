using Microsoft.Extensions.Configuration;

namespace Tubestead.Infrastructure.Data;

public enum DatabaseProvider { Sqlite, SqlServer }

/// <summary>Resolves the database provider and connection string from configuration
/// (environment variables / appsettings), defaulting to a single SQLite file on
/// local disk so first boot needs zero setup.</summary>
public sealed record DatabaseOptions(DatabaseProvider Provider, string ConnectionString)
{
    public static DatabaseOptions FromConfiguration(IConfiguration config)
    {
        var providerRaw = config["TUBESTEAD_DB_PROVIDER"] ?? "Sqlite";
        var provider = providerRaw.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)
            ? DatabaseProvider.SqlServer
            : DatabaseProvider.Sqlite;

        var connection = config["TUBESTEAD_DB_CONNECTION"];
        if (string.IsNullOrWhiteSpace(connection))
        {
            // Default SQLite path: <data>/tubestead.db on LOCAL disk (never the NAS share).
            var dataPath = config["TUBESTEAD_DATA_PATH"];
            if (string.IsNullOrWhiteSpace(dataPath))
                dataPath = Path.Combine(AppContext.BaseDirectory, "data");
            connection = $"Data Source={Path.Combine(dataPath, "tubestead.db")}";
        }

        return new DatabaseOptions(provider, connection);
    }

    /// <summary>For SQLite default connections, the directory the .db file lives in
    /// (so startup can ensure it exists). Null for other providers/explicit strings.</summary>
    public string? SqliteDirectory()
    {
        if (Provider != DatabaseProvider.Sqlite)
            return null;
        const string marker = "Data Source=";
        var idx = ConnectionString.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;
        var path = ConnectionString[(idx + marker.Length)..].Split(';')[0].Trim();
        return Path.GetDirectoryName(Path.GetFullPath(path));
    }
}
