using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Portfolio.Application.MarketData;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Exceptions;
using Portfolio.Infrastructure.MarketData;
using Portfolio.Infrastructure.Persistence;
using Quartz;

namespace Portfolio.Infrastructure.Jobs;

[DisallowConcurrentExecution]
public class PriceRefreshJob(
    PortfolioDbContext db,
    IMarketDataClient marketDataClient,
    IOptions<AlphaVantageOptions> options,
    PortfolioSnapshotService snapshotService,
    ILogger<PriceRefreshJob> logger) : IJob
{
    private const int MinRequestSpacingMs = 1200;

    private readonly AlphaVantageOptions _options = options.Value;

    public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
    {
        var log = new PriceRefreshLog
        {
            Id = Guid.NewGuid(),
            StartedAt = DateTimeOffset.UtcNow,
            Status = JobRunStatus.Running,
        };
        db.PriceRefreshLogs.Add(log);
        await db.SaveChangesAsync(cancellationToken);

        // Oldest-refreshed-first: symbols beyond MaxSymbolsPerRun roll over to the next run.
        var symbols = await db.Securities
            .Where(s => s.Transactions.Any())
            .OrderBy(s => s.LastPriceRefreshAttemptAt ?? DateTimeOffset.MinValue)
            .Take(_options.MaxSymbolsPerRun)
            .ToListAsync(cancellationToken);

        var processed = 0;
        var failed = 0;
        var errors = new List<string>();
        var quotaExhausted = false;

        foreach (var security in symbols)
        {
            try
            {
                var hasExistingPrices = await db.DailyPrices.AnyAsync(dp => dp.SecurityId == security.Id, cancellationToken);
                var points = await marketDataClient.GetDailySeriesAsync(security.Symbol, fullHistory: !hasExistingPrices, cancellationToken);

                await UpsertDailyPricesAsync(security.Id, points, cancellationToken);

                if (security.MetadataRefreshedAt is null)
                {
                    // Alpha Vantage enforces a per-second burst limit even between two
                    // calls for the same symbol, so space these out too.
                    await Task.Delay(MinRequestSpacingMs, cancellationToken);
                    var overview = await marketDataClient.GetOverviewAsync(security.Symbol, cancellationToken);
                    if (overview is not null)
                    {
                        security.Name = overview.Name;
                        security.Sector = overview.Sector;
                        security.Industry = overview.Industry;
                    }

                    security.MetadataRefreshedAt = DateTimeOffset.UtcNow;
                }

                security.LastPriceRefreshAttemptAt = DateTimeOffset.UtcNow;
                processed++;
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (MarketDataRateLimitedException ex)
            {
                failed++;
                errors.Add($"{security.Symbol}: {ex.Message}");
                logger.LogWarning(ex, "Alpha Vantage quota exhausted while refreshing {Symbol}; stopping this run early", security.Symbol);
                quotaExhausted = true;
                break;
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{security.Symbol}: {ex.Message}");
                logger.LogWarning(ex, "Failed to refresh prices for {Symbol}", security.Symbol);
            }

            if (security != symbols[^1] && !quotaExhausted)
            {
                await Task.Delay(_options.DelayBetweenRequestsMs, cancellationToken);
            }
        }

        log.CompletedAt = DateTimeOffset.UtcNow;
        log.SymbolsProcessed = processed;
        log.SymbolsFailed = failed;
        log.Status = failed == 0
            ? JobRunStatus.Succeeded
            : processed == 0 ? JobRunStatus.Failed : JobRunStatus.PartiallySucceeded;
        log.ErrorDetails = errors.Count > 0 ? string.Join("; ", errors) : null;
        await db.SaveChangesAsync(cancellationToken);

        if (processed > 0)
        {
            await snapshotService.RecomputeAllAsync(cancellationToken);
        }
    }

    private async Task UpsertDailyPricesAsync(Guid securityId, IReadOnlyList<DailyPricePoint> points, CancellationToken cancellationToken)
    {
        if (points.Count == 0)
        {
            return;
        }

        var dates = points.Select(p => p.Date).ToList();
        var existingByDate = await db.DailyPrices
            .Where(dp => dp.SecurityId == securityId && dates.Contains(dp.Date))
            .ToDictionaryAsync(dp => dp.Date, cancellationToken);

        foreach (var point in points)
        {
            if (existingByDate.TryGetValue(point.Date, out var existing))
            {
                existing.Close = point.Close;
                existing.Volume = point.Volume;
            }
            else
            {
                db.DailyPrices.Add(new DailyPrice
                {
                    Id = Guid.NewGuid(),
                    SecurityId = securityId,
                    Date = point.Date,
                    Close = point.Close,
                    Volume = point.Volume,
                });
            }
        }
    }
}
