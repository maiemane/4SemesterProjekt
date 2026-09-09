using Microsoft.Data.SqlClient;

namespace HedgingTool.Platform.Infrastructure.Configuration;

public static class SqlServerConnectionStringConverter
{
    public static string FromJdbcIfNeeded(string connectionString)
    {
        if (!connectionString.StartsWith("jdbc:sqlserver://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var withoutPrefix = connectionString["jdbc:sqlserver://".Length..].TrimEnd(';');
        var parts = withoutPrefix.Split(';', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 0)
        {
            throw new InvalidOperationException("JDBC SQL Server connection string is missing server information.");
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = NormalizeJdbcDataSource(parts[0])
        };

        foreach (var part in parts.Skip(1))
        {
            var separatorIndex = part.IndexOf('=', StringComparison.Ordinal);

            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = part[..separatorIndex];
            var value = part[(separatorIndex + 1)..];

            switch (key.ToLowerInvariant())
            {
                case "database":
                    builder.InitialCatalog = value;
                    break;
                case "user":
                    builder.UserID = value;
                    break;
                case "password":
                    builder.Password = value;
                    break;
                case "encrypt":
                    builder.Encrypt = bool.Parse(value);
                    break;
                case "trustservercertificate":
                    builder.TrustServerCertificate = bool.Parse(value);
                    break;
                case "hostnameincertificate":
                    builder.HostNameInCertificate = value;
                    break;
                case "logintimeout":
                    builder.ConnectTimeout = int.Parse(value);
                    break;
            }
        }

        return builder.ConnectionString;
    }

    private static string NormalizeJdbcDataSource(string dataSource)
    {
        var portSeparatorIndex = dataSource.LastIndexOf(':');

        if (portSeparatorIndex < 0)
        {
            return dataSource;
        }

        var host = dataSource[..portSeparatorIndex];
        var port = dataSource[(portSeparatorIndex + 1)..];

        return int.TryParse(port, out _) ? $"{host},{port}" : dataSource;
    }
}
