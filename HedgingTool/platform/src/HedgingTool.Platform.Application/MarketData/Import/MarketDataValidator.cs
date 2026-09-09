namespace HedgingTool.Platform.Application.MarketData.Import;

public sealed class MarketDataValidator
{
    public IReadOnlyList<string> Validate(IReadOnlyList<MarketDataCsvRow> rows)
    {
        var warnings = new List<string>();

        foreach (var row in rows)
        {
            if (row.OfficialClose is null && row.Nav is null)
            {
                warnings.Add($"{row.Date:yyyy-MM-dd} {row.Ticker}: row has neither OfficialClose nor NAV.");
            }

            if (row.OfficialClose <= 0)
            {
                warnings.Add($"{row.Date:yyyy-MM-dd} {row.Ticker}: OfficialClose is zero or negative.");
            }

            if (row.Nav <= 0)
            {
                warnings.Add($"{row.Date:yyyy-MM-dd} {row.Ticker}: NAV is zero or negative.");
            }
        }

        return warnings;
    }
}
