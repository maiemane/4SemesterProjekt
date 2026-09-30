using HedgingTool.Platform.Application.DTOs.Funds;
using HedgingTool.Platform.Application.Interfaces;

namespace HedgingTool.Platform.Application.Services.Funds;

public sealed class FundService : IFundService
{
    private readonly IFundRepository _fundRepository;

    public FundService(IFundRepository fundRepository)
    {
        _fundRepository = fundRepository;
    }

    public Task<IReadOnlyList<FundSummaryDto>> GetFundSummariesAsync(CancellationToken cancellationToken = default)
    {
        return _fundRepository.GetFundSummariesAsync(cancellationToken);
    }
}
