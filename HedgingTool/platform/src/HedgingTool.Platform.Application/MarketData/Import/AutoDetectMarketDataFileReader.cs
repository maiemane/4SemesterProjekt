using System.Text;

namespace HedgingTool.Platform.Application.MarketData.Import;

public sealed class AutoDetectMarketDataFileReader : IMarketDataFileReader
{
    private readonly CsvMarketDataReader _normalizedCsvReader;
    private readonly BloombergWideMarketDataReader _bloombergWideReader;

    public AutoDetectMarketDataFileReader(
        CsvMarketDataReader normalizedCsvReader,
        BloombergWideMarketDataReader bloombergWideReader)
    {
        _normalizedCsvReader = normalizedCsvReader;
        _bloombergWideReader = bloombergWideReader;
    }

    public async Task<IReadOnlyList<MarketDataCsvRow>> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("CSV file was not found.", filePath);
        }

        using var reader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var firstLine = await reader.ReadLineAsync(cancellationToken) ?? string.Empty;

        if (BloombergWideMarketDataReader.CanRead(firstLine))
        {
            return await _bloombergWideReader.ReadAsync(filePath, cancellationToken);
        }

        return await _normalizedCsvReader.ReadAsync(filePath, cancellationToken);
    }
}
