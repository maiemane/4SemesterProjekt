namespace HedgingTool.Platform.Application.MarketData.Import;

public sealed record MarketDataImportResult(
    int RowsRead,
    int InstrumentsCreated,
    int PricesWritten,
    int NavsWritten,
    IReadOnlyList<string> Warnings);
