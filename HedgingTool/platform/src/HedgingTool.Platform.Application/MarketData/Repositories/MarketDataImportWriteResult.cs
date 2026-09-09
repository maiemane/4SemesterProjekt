namespace HedgingTool.Platform.Application.MarketData.Repositories;

public sealed record MarketDataImportWriteResult(
    int InstrumentsCreated,
    int PricesWritten,
    int NavsWritten);
