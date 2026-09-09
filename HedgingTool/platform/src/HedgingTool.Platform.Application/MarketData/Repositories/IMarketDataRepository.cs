using HedgingTool.Platform.Application.MarketData.Import;

namespace HedgingTool.Platform.Application.MarketData.Repositories;

public interface IMarketDataRepository
{
    Task<MarketDataImportWriteResult> ImportAsync(
        IReadOnlyList<MarketDataCsvRow> rows,
        CancellationToken cancellationToken = default);
}
