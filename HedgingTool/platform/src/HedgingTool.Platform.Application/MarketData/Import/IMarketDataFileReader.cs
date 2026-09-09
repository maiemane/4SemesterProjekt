namespace HedgingTool.Platform.Application.MarketData.Import;

public interface IMarketDataFileReader
{
    Task<IReadOnlyList<MarketDataCsvRow>> ReadAsync(
        string filePath,
        CancellationToken cancellationToken = default);
}
