using HedgingTool.Platform.Application.DTOs.Funds;
using HedgingTool.Platform.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace HedgingTool.Platform.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class FundsController : ControllerBase
{
    private readonly IFundService _fundService;

    public FundsController(IFundService fundService)
    {
        _fundService = fundService;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<FundSummaryDto>>> GetFunds(CancellationToken cancellationToken)
    {
        var funds = await _fundService.GetFundSummariesAsync(cancellationToken);

        return Ok(funds);
    }
}
