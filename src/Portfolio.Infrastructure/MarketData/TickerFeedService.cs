using Microsoft.EntityFrameworkCore;
using Portfolio.Application.MarketData;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.MarketData;

public class TickerFeedService(PortfolioDbContext db) : ITickerFeedService
{
    public async Task<IReadOnlyList<TickerEntryDto>> GetTickerAsync(int count, CancellationToken cancellationToken)
    {
        var topSecurityIds = await db.DailyPrices
            .GroupBy(dp => dp.SecurityId)
            .OrderByDescending(g => g.Max(dp => dp.Date))
            .Select(g => g.Key)
            .Take(count)
            .ToListAsync(cancellationToken);

        if (topSecurityIds.Count == 0)
        {
            return [];
        }

        var recentPrices = await db.DailyPrices
            .Where(dp => topSecurityIds.Contains(dp.SecurityId))
            .OrderByDescending(dp => dp.Date)
            .Select(dp => new { dp.SecurityId, dp.Close })
            .ToListAsync(cancellationToken);

        var symbolsById = await db.Securities
            .Where(s => topSecurityIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, s => s.Symbol, cancellationToken);

        return topSecurityIds
            .Select(id =>
            {
                var lastTwo = recentPrices.Where(p => p.SecurityId == id).Take(2).ToList();
                var latest = lastTwo[0].Close;
                decimal? changePercent = lastTwo.Count == 2 && lastTwo[1].Close != 0
                    ? (latest - lastTwo[1].Close) / lastTwo[1].Close * 100
                    : null;

                return new TickerEntryDto(symbolsById[id], latest, changePercent);
            })
            .ToList();
    }
}
