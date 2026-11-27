using Npgsql;

namespace RouteRevisionHub.Data;

public record ResolvedDbConnection(string ConnectionString, bool IsPostgres);

/// <summary>
/// Resolves MS SQL Server (local/cloud) or PostgreSQL (Vercel Marketplace Neon/Supabase URI or DSN) seamlessly.
/// </summary>
public static class DatabaseConnectionHelper
{
    public static ResolvedDbConnection ResolveConnection(IConfiguration config)
    {
        string raw = FirstNonEmpty(
                Environment.GetEnvironmentVariable("DATABASE_URL"),
                Environment.GetEnvironmentVariable("POSTGRES_URL"),
                Environment.GetEnvironmentVariable("POSTGRES_PRISMA_URL"),
                config.GetConnectionString("DefaultConnection"))
            ?? throw new InvalidOperationException("No database connection string configured.");

        string? explicitProvider = config["DatabaseProvider"];
        bool isPostgres =
            string.Equals(explicitProvider, "PostgreSQL", StringComparison.OrdinalIgnoreCase)
            || string.Equals(explicitProvider, "Postgres", StringComparison.OrdinalIgnoreCase)
            || string.Equals(explicitProvider, "Npgsql", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)
            || (raw.Contains("Host=", StringComparison.OrdinalIgnoreCase)
                && !raw.Contains("Server=", StringComparison.OrdinalIgnoreCase));

        if (!isPostgres)
            return new ResolvedDbConnection(raw, IsPostgres: false);

        if (raw.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            || raw.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            raw = ConvertPostgresUriToDsn(raw);
        }

        return new ResolvedDbConnection(raw, IsPostgres: true);
    }

    private static string? FirstNonEmpty(params string?[] candidates)
        => candidates.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c))?.Trim();

    private static string ConvertPostgresUriToDsn(string uriString)
    {
        var uri = new Uri(uriString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var csb = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = uri.AbsolutePath.TrimStart('/'),
            Username = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
            Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
            Pooling = true,
            SslMode = SslMode.Require
        };

        string q = uri.Query.TrimStart('?');
        foreach (var pair in q.Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = pair.Split('=', 2);
            if (kv.Length != 2) continue;
            string key = kv[0].ToLowerInvariant();
            string val = Uri.UnescapeDataString(kv[1]).ToLowerInvariant();
            if (key == "sslmode")
            {
                csb.SslMode = val switch
                {
                    "disable" => SslMode.Disable,
                    "allow" => SslMode.Allow,
                    "prefer" => SslMode.Prefer,
                    "verify-ca" => SslMode.VerifyCA,
                    "verify-full" => SslMode.VerifyFull,
                    _ => SslMode.Require
                };
            }
        }

        return csb.ConnectionString;
    }
}
