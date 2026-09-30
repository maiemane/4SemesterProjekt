using HedgingTool.Platform.Application.DTOs.Funds;

namespace HedgingTool.Platform.Application.Interfaces;

public interface IFundRepository
{
    Task<IReadOnlyList<FundSummaryDto>> GetFundSummariesAsync(CancellationToken cancellationToken = default);
}
