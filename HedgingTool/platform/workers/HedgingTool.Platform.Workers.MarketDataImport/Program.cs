using Microsoft.Data.SqlClient;
using HedgingTool.Platform.Application.MarketData.Import;
using HedgingTool.Platform.Infrastructure.Configuration;
using HedgingTool.Platform.Infrastructure.Repositories;

try
{
    await RunImportAsync(args);
}
catch (Exception exception) when (exception is FileNotFoundException or InvalidOperationException or SqlException)
{
    Console.Error.WriteLine($"Import failed: {exception.Message}");
    Environment.ExitCode = 1;
}

static async Task RunImportAsync(string[] args)
{
    if (args.Length < 1)
    {
        Console.Error.WriteLine("Usage: dotnet run --project workers/HedgingTool.Platform.Workers.MarketDataImport -- <csv-file> [--connection-string <sql-server-connection-string>] [--validate-only]");
        Console.Error.WriteLine("Alternatively set HEDGING_DB_CONNECTION_STRING.");
        Environment.ExitCode = 1;
        return;
    }

    var csvFilePath = args[0];
    var reader = new AutoDetectMarketDataFileReader(
        new CsvMarketDataReader(),
        new BloombergWideMarketDataReader());

    if (HasOption(args, "--validate-only"))
    {
        var rows = await reader.ReadAsync(csvFilePath);
        var warnings = new MarketDataValidator().Validate(rows);

        Console.WriteLine($"Rows parsed: {rows.Count}");
        Console.WriteLine($"Warnings: {warnings.Count}");

        foreach (var warning in warnings.Take(25))
        {
            Console.WriteLine($"Warning: {warning}");
        }

        return;
    }

    var connectionString = GetOption(args, "--connection-string")
        ?? Environment.GetEnvironmentVariable("HEDGING_DB_CONNECTION_STRING");

    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("Missing SQL Server connection string.");
        Console.Error.WriteLine("Pass --connection-string or set HEDGING_DB_CONNECTION_STRING.");
        Environment.ExitCode = 1;
        return;
    }

    var importer = new MarketDataImporter(
        reader,
        new MarketDataValidator(),
        new SqlServerMarketDataRepository(SqlServerConnectionStringConverter.FromJdbcIfNeeded(connectionString)));

    var result = await importer.ImportAsync(csvFilePath);

    Console.WriteLine($"Rows read: {result.RowsRead}");
    Console.WriteLine($"Instruments created: {result.InstrumentsCreated}");
    Console.WriteLine($"Prices written: {result.PricesWritten}");
    Console.WriteLine($"NAVs written: {result.NavsWritten}");

    foreach (var warning in result.Warnings)
    {
        Console.WriteLine($"Warning: {warning}");
    }
}

static string? GetOption(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
        {
            return args[i + 1];
        }
    }

    return null;
}

static bool HasOption(string[] args, string name) =>
    args.Any(argument => string.Equals(argument, name, StringComparison.OrdinalIgnoreCase));
