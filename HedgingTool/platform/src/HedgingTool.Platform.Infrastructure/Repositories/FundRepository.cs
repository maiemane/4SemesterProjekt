using HedgingTool.Platform.Application.DTOs.Funds;
using HedgingTool.Platform.Application.Interfaces;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace HedgingTool.Platform.Infrastructure.Repositories;

public sealed class FundRepository : IFundRepository
{
    private readonly string _connectionString;

    public FundRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("HedgingDb")
            ?? throw new InvalidOperationException("Connection string 'HedgingDb' was not found.");
    }

    public async Task<IReadOnlyList<FundSummaryDto>> GetFundSummariesAsync(CancellationToken cancellationToken = default)
    {
        const string sql = """
            SELECT
                i.instrument_id,
                i.instrument_type,
                i.bloomberg_ticker,
                i.name,
                i.currency,
                i.quote_unit,
                latest_nav.nav,
                latest_nav.nav_date,
                latest_price.price,
                latest_price.trade_date
            FROM dbo.instrument AS i
            OUTER APPLY
            (
                SELECT TOP (1)
                    dn.nav,
                    dn.nav_date
                FROM dbo.daily_nav AS dn
                WHERE dn.instrument_id = i.instrument_id
                ORDER BY dn.nav_date DESC
            ) AS latest_nav
            OUTER APPLY
            (
                SELECT TOP (1)
                    dp.price,
                    dp.trade_date
                FROM dbo.daily_price AS dp
                WHERE dp.instrument_id = i.instrument_id
                ORDER BY dp.trade_date DESC
            ) AS latest_price
            WHERE i.active = 1
            ORDER BY i.name;
            """;

        var funds = new List<FundSummaryDto>();

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            funds.Add(new FundSummaryDto
            {
                InstrumentId = reader.GetInt64(0),
                InstrumentType = reader.GetString(1),
                BloombergTicker = reader.GetString(2),
                Name = reader.GetString(3),
                Currency = reader.GetString(4),
                QuoteUnit = reader.GetString(5),
                LatestNav = reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                LatestNavDate = reader.IsDBNull(7) ? null : DateOnly.FromDateTime(reader.GetDateTime(7)),
                LatestPrice = reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                LatestPriceDate = reader.IsDBNull(9) ? null : DateOnly.FromDateTime(reader.GetDateTime(9))
            });
        }

        return funds;
    }
}
