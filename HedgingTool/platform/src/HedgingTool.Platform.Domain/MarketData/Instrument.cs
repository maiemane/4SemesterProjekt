namespace HedgingTool.Platform.Domain.MarketData;

public sealed record Instrument(
    long InstrumentId,
    string InstrumentType,
    string BloombergTicker,
    string Name,
    string Currency,
    string QuoteUnit,
    bool Active,
    DateTime CreatedAt);
