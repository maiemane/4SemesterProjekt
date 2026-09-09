using System.Globalization;
using System.Text;

namespace HedgingTool.Platform.Application.MarketData.Import;

public sealed class BloombergWideMarketDataReader : IMarketDataFileReader
{
    private const char Delimiter = ';';

    private static readonly string[] DateFormats =
    [
        "dd.MM.yyyy",
        "yyyy-MM-dd"
    ];

    private static readonly CultureInfo DecimalCulture = CultureInfo.GetCultureInfo("da-DK");

    public async Task<IReadOnlyList<MarketDataCsvRow>> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("CSV file was not found.", filePath);
        }

        var lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8, cancellationToken);

        if (lines.Length < 6)
        {
            throw new InvalidOperationException("Bloomberg wide CSV must contain metadata, ticker and field header rows.");
        }

        var tickerHeader = DelimitedTextParser.ParseLine(lines[3].TrimStart('\uFEFF'), Delimiter);
        var fieldHeader = DelimitedTextParser.ParseLine(lines[5], Delimiter);
        var columns = BuildColumns(tickerHeader, fieldHeader);
        var rows = new List<MarketDataCsvRow>();

        for (var lineIndex = 6; lineIndex < lines.Length; lineIndex++)
        {
            var line = lines[lineIndex];

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = DelimitedTextParser.ParseLine(line, Delimiter);
            var dateText = Get(fields, 0);

            if (string.IsNullOrWhiteSpace(dateText))
            {
                continue;
            }

            var date = ParseDate(dateText, lineIndex + 1);

            foreach (var instrument in columns.GroupBy(column => column.Ticker))
            {
                var officialClose = TryGetDecimal(fields, instrument.FirstOrDefault(column => column.FieldCode == "PX_OFFICIAL_CLOSE")?.Index);
                var nav = TryGetDecimal(fields, instrument.FirstOrDefault(column => column.FieldCode == "FUND_NET_ASSET_VAL")?.Index);

                if (officialClose is null && nav is null)
                {
                    continue;
                }

                rows.Add(new MarketDataCsvRow(
                    date,
                    instrument.Key,
                    instrument.Key,
                    null,
                    "PRICE",
                    officialClose,
                    nav,
                    "BLOOMBERG",
                    "Bloomberg"));
            }
        }

        return rows;
    }

    public static bool CanRead(string firstLine) =>
        firstLine.TrimStart('\uFEFF').StartsWith("Start Date;", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<BloombergColumn> BuildColumns(
        IReadOnlyList<string> tickerHeader,
        IReadOnlyList<string> fieldHeader)
    {
        var columns = new List<BloombergColumn>();
        string? currentTicker = null;
        var maxColumns = Math.Min(tickerHeader.Count, fieldHeader.Count);

        for (var index = 1; index < maxColumns; index++)
        {
            var ticker = tickerHeader[index].Trim();

            if (!string.IsNullOrWhiteSpace(ticker))
            {
                currentTicker = ticker;
            }

            var fieldCode = fieldHeader[index].Trim();

            if (string.IsNullOrWhiteSpace(currentTicker) || string.IsNullOrWhiteSpace(fieldCode))
            {
                continue;
            }

            columns.Add(new BloombergColumn(index, currentTicker, fieldCode));
        }

        if (columns.Count == 0)
        {
            throw new InvalidOperationException("Bloomberg wide CSV did not contain any instrument columns.");
        }

        return columns;
    }

    private static DateOnly ParseDate(string value, int lineNumber)
    {
        if (DateOnly.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        throw new InvalidOperationException($"Line {lineNumber}: Date '{value}' is not a supported Bloomberg date format.");
    }

    private static decimal? TryGetDecimal(IReadOnlyList<string> fields, int? index)
    {
        if (index is null || index.Value >= fields.Count)
        {
            return null;
        }

        var value = fields[index.Value].Trim();

        if (string.IsNullOrWhiteSpace(value) || value == "#N/A N/A")
        {
            return null;
        }

        if (decimal.TryParse(value, NumberStyles.Number, DecimalCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Value '{value}' is not a valid Bloomberg decimal.");
    }

    private static string Get(IReadOnlyList<string> fields, int index) =>
        index >= fields.Count ? string.Empty : fields[index];

    private sealed record BloombergColumn(int Index, string Ticker, string FieldCode);
}
