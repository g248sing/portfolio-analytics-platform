using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Extensions;
using Portfolio.Application.Holdings;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolios/{portfolioId:guid}/holdings")]
public class HoldingsController(IHoldingsService holdingsService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid portfolioId, CancellationToken cancellationToken)
    {
        var holdings = await holdingsService.GetHoldingsAsync(User.GetUserId(), portfolioId, cancellationToken);
        return holdings is null ? NotFound() : Ok(holdings);
    }
}
