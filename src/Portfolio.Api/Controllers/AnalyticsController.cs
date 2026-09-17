using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Extensions;
using Portfolio.Application.Analytics;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolios/{portfolioId:guid}/analytics")]
public class AnalyticsController(IAnalyticsService analyticsService) : ControllerBase
{
    [HttpGet("value-history")]
    public async Task<IActionResult> GetValueHistory(Guid portfolioId, CancellationToken cancellationToken)
    {
        var history = await analyticsService.GetValueHistoryAsync(User.GetUserId(), portfolioId, cancellationToken);
        return history is null ? NotFound() : Ok(history);
    }

    [HttpGet("allocation")]
    public async Task<IActionResult> GetAllocation(Guid portfolioId, CancellationToken cancellationToken)
    {
        var allocation = await analyticsService.GetAllocationAsync(User.GetUserId(), portfolioId, cancellationToken);
        return allocation is null ? NotFound() : Ok(allocation);
    }

    [HttpGet("performers")]
    public async Task<IActionResult> GetPerformers(Guid portfolioId, [FromQuery] int count = 5, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 50);
        var performers = await analyticsService.GetPerformersAsync(User.GetUserId(), portfolioId, count, cancellationToken);
        return performers is null ? NotFound() : Ok(performers);
    }
}
