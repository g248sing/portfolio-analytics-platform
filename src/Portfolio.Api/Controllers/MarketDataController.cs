using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.MarketData;

namespace Portfolio.Api.Controllers;

// Deliberately unauthenticated: this is publicly-known market data, not
// user- or portfolio-scoped, so it can back decorative UI shown even on
// the login/register pages.
[ApiController]
[Route("api/market-data")]
public class MarketDataController(ITickerFeedService tickerFeedService) : ControllerBase
{
    [HttpGet("ticker")]
    public async Task<IActionResult> GetTicker([FromQuery] int count = 24, CancellationToken cancellationToken = default)
    {
        count = Math.Clamp(count, 1, 100);
        var ticker = await tickerFeedService.GetTickerAsync(count, cancellationToken);
        return Ok(ticker);
    }
}
