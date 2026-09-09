namespace HedgingTool.Platform.Domain.MarketData;

public sealed record DailyPrice(
    long InstrumentId,
    DateOnly TradeDate,
    string PriceType,
    decimal Price,
    long SourceId,
    DateTime LoadedAt);
