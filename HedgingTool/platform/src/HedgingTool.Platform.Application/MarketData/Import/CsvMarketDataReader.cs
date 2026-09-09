using System.Globalization;
using System.Text;

namespace HedgingTool.Platform.Application.MarketData.Import;

public sealed class CsvMarketDataReader : IMarketDataFileReader
{
    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "dd-MM-yyyy",
        "dd/MM/yyyy",
        "MM/dd/yyyy",
        "yyyy/MM/dd"
    ];

    public async Task<IReadOnlyList<MarketDataCsvRow>> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("CSV file was not found.", filePath);
        }

        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var headerLine = await reader.ReadLineAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(headerLine))
        {
            throw new InvalidOperationException("CSV file is empty.");
        }

        var headers = DelimitedTextParser.ParseLine(headerLine, ',')
            .Select((name, index) => new { Name = NormalizeHeader(name), Index = index })
            .ToDictionary(item => item.Name, item => item.Index, StringComparer.OrdinalIgnoreCase);

        var dateIndex = Required(headers, "date");
        var tickerIndex = Required(headers, "ticker");
        var nameIndex = Optional(headers, "name");
        var currencyIndex = Optional(headers, "currency");
        var quoteUnitIndex = Optional(headers, "quoteunit");
        var officialCloseIndex = Optional(headers, "officialclose");
        var navIndex = Optional(headers, "nav");
        var sourceCodeIndex = Optional(headers, "sourcecode");
        var sourceNameIndex = Optional(headers, "source");

        var rows = new List<MarketDataCsvRow>();
        var lineNumber = 1;

        while (await reader.ReadLineAsync(cancellationToken) is { } line)
        {
            lineNumber++;

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = DelimitedTextParser.ParseLine(line, ',');
            var ticker = Get(fields, tickerIndex).Trim();

            if (string.IsNullOrWhiteSpace(ticker))
            {
                throw new InvalidOperationException($"Line {lineNumber}: Ticker is required.");
            }

            rows.Add(new MarketDataCsvRow(
                ParseDate(Get(fields, dateIndex), lineNumber),
                ticker,
                NullIfWhiteSpace(Get(fields, nameIndex)),
                NullIfWhiteSpace(Get(fields, currencyIndex)),
                NullIfWhiteSpace(Get(fields, quoteUnitIndex)),
                ParseOptionalDecimal(Get(fields, officialCloseIndex), lineNumber, "OfficialClose"),
                ParseOptionalDecimal(Get(fields, navIndex), lineNumber, "NAV"),
                string.IsNullOrWhiteSpace(Get(fields, sourceCodeIndex)) ? "BLOOMBERG" : Get(fields, sourceCodeIndex).Trim(),
                string.IsNullOrWhiteSpace(Get(fields, sourceNameIndex)) ? "Bloomberg" : Get(fields, sourceNameIndex).Trim()));
        }

        return rows;
    }

    private static int Required(IReadOnlyDictionary<string, int> headers, string name)
    {
        if (!headers.TryGetValue(name, out var index))
        {
            throw new InvalidOperationException($"Missing required CSV column '{name}'.");
        }

        return index;
    }

    private static int? Optional(IReadOnlyDictionary<string, int> headers, string name) =>
        headers.TryGetValue(name, out var index) ? index : null;

    private static string NormalizeHeader(string header) =>
        header.Trim().Replace("_", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

    private static string Get(IReadOnlyList<string> fields, int? index) =>
        index is null || index.Value >= fields.Count ? string.Empty : fields[index.Value];

    private static string? NullIfWhiteSpace(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static DateOnly ParseDate(string value, int lineNumber)
    {
        if (DateOnly.TryParseExact(value.Trim(), DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            return date;
        }

        throw new InvalidOperationException($"Line {lineNumber}: Date '{value}' is not a supported date format.");
    }

    private static decimal? ParseOptionalDecimal(string value, int lineNumber, string columnName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (decimal.TryParse(value.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"Line {lineNumber}: {columnName} '{value}' is not a valid number.");
    }
}
