namespace HedgingTool.Platform.Application.DTOs.Funds;

public sealed class FundSummaryDto
{
    public long InstrumentId { get; set; }
    public string InstrumentType { get; set; } = string.Empty;
    public string BloombergTicker { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public string QuoteUnit { get; set; } = string.Empty;
    public decimal? LatestNav { get; set; }
    public DateOnly? LatestNavDate { get; set; }
    public decimal? LatestPrice { get; set; }
    public DateOnly? LatestPriceDate { get; set; }
}
