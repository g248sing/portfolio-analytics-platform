using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Extensions;
using Portfolio.Application.Portfolios;
using Portfolio.Application.Portfolios.Dtos;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolios")]
public class PortfoliosController(IPortfolioService portfolioService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PortfolioResponse>>> GetAll(CancellationToken cancellationToken)
    {
        var portfolios = await portfolioService.GetPortfoliosAsync(User.GetUserId(), cancellationToken);
        return Ok(portfolios);
    }

    [HttpGet("{portfolioId:guid}")]
    public async Task<ActionResult<PortfolioResponse>> GetById(Guid portfolioId, CancellationToken cancellationToken)
    {
        var portfolioResponse = await portfolioService.GetPortfolioAsync(User.GetUserId(), portfolioId, cancellationToken);
        return portfolioResponse is null ? NotFound() : Ok(portfolioResponse);
    }

    [HttpPost]
    public async Task<ActionResult<PortfolioResponse>> Create(CreatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var created = await portfolioService.CreatePortfolioAsync(User.GetUserId(), request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { portfolioId = created.Id }, created);
    }

    [HttpPut("{portfolioId:guid}")]
    public async Task<ActionResult<PortfolioResponse>> Update(Guid portfolioId, UpdatePortfolioRequest request, CancellationToken cancellationToken)
    {
        var updated = await portfolioService.UpdatePortfolioAsync(User.GetUserId(), portfolioId, request, cancellationToken);
        return updated is null ? NotFound() : Ok(updated);
    }

    [HttpDelete("{portfolioId:guid}")]
    public async Task<IActionResult> Delete(Guid portfolioId, CancellationToken cancellationToken)
    {
        var deleted = await portfolioService.DeletePortfolioAsync(User.GetUserId(), portfolioId, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
