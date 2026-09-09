namespace HedgingTool.Platform.Domain.MarketData;

public sealed record DailyNav(
    long InstrumentId,
    DateOnly NavDate,
    string NavType,
    decimal Nav,
    long SourceId,
    DateTime LoadedAt);
