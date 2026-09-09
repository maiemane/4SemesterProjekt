namespace HedgingTool.Platform.Application.MarketData.Import;

public sealed record MarketDataCsvRow(
    DateOnly Date,
    string Ticker,
    string? Name,
    string? Currency,
    string? QuoteUnit,
    decimal? OfficialClose,
    decimal? Nav,
    string SourceCode,
    string SourceName);
