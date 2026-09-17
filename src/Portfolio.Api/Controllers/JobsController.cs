using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Infrastructure.Jobs;
using Quartz;

namespace Portfolio.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/jobs")]
public class JobsController(ISchedulerFactory schedulerFactory) : ControllerBase
{
    [HttpPost("price-refresh")]
    public async Task<IActionResult> TriggerPriceRefresh(CancellationToken cancellationToken)
    {
        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        await scheduler.TriggerJob(new JobKey(nameof(PriceRefreshJob)), cancellationToken: cancellationToken);
        return Accepted();
    }
}
