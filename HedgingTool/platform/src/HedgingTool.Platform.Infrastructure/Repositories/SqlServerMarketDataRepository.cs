using System.Data;
using Microsoft.Data.SqlClient;
using HedgingTool.Platform.Application.MarketData.Import;
using HedgingTool.Platform.Application.MarketData.Repositories;

namespace HedgingTool.Platform.Infrastructure.Repositories;

public sealed class SqlServerMarketDataRepository : IMarketDataRepository
{
    private readonly string _connectionString;

    public SqlServerMarketDataRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<MarketDataImportWriteResult> ImportAsync(
        IReadOnlyList<MarketDataCsvRow> rows,
        CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0)
        {
            return new MarketDataImportWriteResult(0, 0, 0);
        }

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            await CreateStagingTableAsync(connection, transaction, cancellationToken);
            await BulkCopyToStagingAsync(connection, transaction, rows, cancellationToken);

            var instrumentsCreated = await ExecuteScalarIntAsync(connection, transaction, """
                INSERT INTO INSTRUMENT (instrument_type, bloomberg_ticker, name, currency, quote_unit, active, created_at)
                SELECT
                    'FUND',
                    staged.bloomberg_ticker,
                    MAX(staged.name),
                    MAX(staged.currency),
                    MAX(staged.quote_unit),
                    1,
                    SYSUTCDATETIME()
                FROM #MarketDataImport AS staged
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM INSTRUMENT AS instrument
                    WHERE instrument.bloomberg_ticker = staged.bloomberg_ticker
                )
                GROUP BY staged.bloomberg_ticker;

                SELECT @@ROWCOUNT;
                """, cancellationToken);

            await ExecuteNonQueryAsync(connection, transaction, """
                INSERT INTO DATA_SOURCE (source_code, source_name)
                SELECT staged.source_code, MAX(staged.source_name)
                FROM #MarketDataImport AS staged
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM DATA_SOURCE AS data_source
                    WHERE data_source.source_code = staged.source_code
                )
                GROUP BY staged.source_code;
                """, cancellationToken);

            var pricesWritten = await ExecuteScalarIntAsync(connection, transaction, """
                MERGE DAILY_PRICE WITH (HOLDLOCK) AS target
                USING (
                    SELECT DISTINCT
                        instrument.instrument_id,
                        staged.trade_date,
                        'OFFICIAL_CLOSE' AS price_type,
                        staged.official_close AS price,
                        data_source.source_id
                    FROM #MarketDataImport AS staged
                    INNER JOIN INSTRUMENT AS instrument
                        ON instrument.bloomberg_ticker = staged.bloomberg_ticker
                    INNER JOIN DATA_SOURCE AS data_source
                        ON data_source.source_code = staged.source_code
                    WHERE staged.official_close IS NOT NULL
                ) AS source
                ON target.instrument_id = source.instrument_id
                    AND target.trade_date = source.trade_date
                    AND target.price_type = source.price_type
                WHEN MATCHED THEN
                    UPDATE SET
                        price = source.price,
                        source_id = source.source_id,
                        loaded_at = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (instrument_id, trade_date, price_type, price, source_id, loaded_at)
                    VALUES (source.instrument_id, source.trade_date, source.price_type, source.price, source.source_id, SYSUTCDATETIME());

                SELECT @@ROWCOUNT;
                """, cancellationToken);

            var navsWritten = await ExecuteScalarIntAsync(connection, transaction, """
                MERGE DAILY_NAV WITH (HOLDLOCK) AS target
                USING (
                    SELECT DISTINCT
                        instrument.instrument_id,
                        staged.trade_date AS nav_date,
                        'REPORTED_NAV' AS nav_type,
                        staged.nav,
                        data_source.source_id
                    FROM #MarketDataImport AS staged
                    INNER JOIN INSTRUMENT AS instrument
                        ON instrument.bloomberg_ticker = staged.bloomberg_ticker
                    INNER JOIN DATA_SOURCE AS data_source
                        ON data_source.source_code = staged.source_code
                    WHERE staged.nav IS NOT NULL
                ) AS source
                ON target.instrument_id = source.instrument_id
                    AND target.nav_date = source.nav_date
                    AND target.nav_type = source.nav_type
                WHEN MATCHED THEN
                    UPDATE SET
                        nav = source.nav,
                        source_id = source.source_id,
                        loaded_at = SYSUTCDATETIME()
                WHEN NOT MATCHED THEN
                    INSERT (instrument_id, nav_date, nav_type, nav, source_id, loaded_at)
                    VALUES (source.instrument_id, source.nav_date, source.nav_type, source.nav, source.source_id, SYSUTCDATETIME());

                SELECT @@ROWCOUNT;
                """, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new MarketDataImportWriteResult(instrumentsCreated, pricesWritten, navsWritten);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task CreateStagingTableAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        CancellationToken cancellationToken)
    {
        await ExecuteNonQueryAsync(connection, transaction, """
            CREATE TABLE #MarketDataImport (
                trade_date date NOT NULL,
                bloomberg_ticker varchar(100) NOT NULL,
                name varchar(255) NOT NULL,
                currency varchar(20) NOT NULL,
                quote_unit varchar(50) NOT NULL,
                official_close numeric(18, 6) NULL,
                nav numeric(18, 6) NULL,
                source_code varchar(50) NOT NULL,
                source_name varchar(255) NOT NULL
            );
            """, cancellationToken);
    }

    private static async Task BulkCopyToStagingAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        IReadOnlyList<MarketDataCsvRow> rows,
        CancellationToken cancellationToken)
    {
        using var table = CreateStagingDataTable(rows);
        using var bulkCopy = new SqlBulkCopy(connection, SqlBulkCopyOptions.CheckConstraints, transaction)
        {
            DestinationTableName = "#MarketDataImport",
            BatchSize = 10_000,
            BulkCopyTimeout = 0
        };

        foreach (DataColumn column in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        await bulkCopy.WriteToServerAsync(table, cancellationToken);
    }

    private static DataTable CreateStagingDataTable(IReadOnlyList<MarketDataCsvRow> rows)
    {
        var table = new DataTable();
        table.Columns.Add("trade_date", typeof(DateTime));
        table.Columns.Add("bloomberg_ticker", typeof(string));
        table.Columns.Add("name", typeof(string));
        table.Columns.Add("currency", typeof(string));
        table.Columns.Add("quote_unit", typeof(string));
        table.Columns.Add("official_close", typeof(decimal));
        table.Columns.Add("nav", typeof(decimal));
        table.Columns.Add("source_code", typeof(string));
        table.Columns.Add("source_name", typeof(string));

        foreach (var row in rows)
        {
            table.Rows.Add(
                row.Date.ToDateTime(TimeOnly.MinValue),
                row.Ticker,
                row.Name ?? row.Ticker,
                row.Currency ?? "UNKNOWN",
                row.QuoteUnit ?? "PRICE",
                row.OfficialClose ?? (object)DBNull.Value,
                row.Nav ?? (object)DBNull.Value,
                row.SourceCode,
                row.SourceName);
        }

        return table;
    }

    private static async Task<int> ExecuteScalarIntAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 0;

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result);
    }

    private static async Task ExecuteNonQueryAsync(
        SqlConnection connection,
        SqlTransaction transaction,
        string sql,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.CommandTimeout = 0;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
