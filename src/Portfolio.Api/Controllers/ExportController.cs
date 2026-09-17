using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Api.Extensions;
using Portfolio.Application.Export;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/portfolios/{portfolioId:guid}/export")]
public class ExportController(IExportService exportService) : ControllerBase
{
    [HttpGet("csv")]
    public async Task<IActionResult> ExportCsv(Guid portfolioId, CancellationToken cancellationToken)
    {
        var bytes = await exportService.ExportHoldingsToCsvAsync(User.GetUserId(), portfolioId, cancellationToken);
        return bytes is null ? NotFound() : File(bytes, "text/csv", "holdings.csv");
    }

    [HttpGet("xlsx")]
    public async Task<IActionResult> ExportXlsx(Guid portfolioId, CancellationToken cancellationToken)
    {
        var bytes = await exportService.ExportHoldingsToXlsxAsync(User.GetUserId(), portfolioId, cancellationToken);
        return bytes is null
            ? NotFound()
            : File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "holdings.xlsx");
    }
}
