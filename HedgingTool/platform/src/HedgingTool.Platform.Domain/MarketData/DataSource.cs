namespace HedgingTool.Platform.Domain.MarketData;

public sealed record DataSource(
    long SourceId,
    string SourceCode,
    string SourceName);
