using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Analytics;
using Portfolio.Application.Holdings;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Analytics;

public class AnalyticsService(PortfolioDbContext db, IHoldingsService holdingsService) : IAnalyticsService
{
    public async Task<IReadOnlyList<ValueHistoryPoint>?> GetValueHistoryAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        if (!await OwnsPortfolioAsync(userId, portfolioId, cancellationToken))
        {
            return null;
        }

        return await db.PortfolioValueSnapshots
            .Where(s => s.InvestmentPortfolioId == portfolioId)
            .OrderBy(s => s.Date)
            .Select(s => new ValueHistoryPoint(s.Date, s.TotalMarketValue, s.TotalCostBasis))
            .ToListAsync(cancellationToken);
    }

    public async Task<AllocationResponse?> GetAllocationAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        var holdings = await holdingsService.GetHoldingsAsync(userId, portfolioId, cancellationToken);
        if (holdings is null)
        {
            return null;
        }

        var bySector = BuildSlices(holdings, h => string.IsNullOrWhiteSpace(h.Sector) ? "Unknown" : h.Sector);
        var byAssetClass = BuildSlices(holdings, h => h.AssetClass.ToString());

        return new AllocationResponse(bySector, byAssetClass);
    }

    public async Task<PerformersResponse?> GetPerformersAsync(Guid userId, Guid portfolioId, int count, CancellationToken cancellationToken)
    {
        var holdings = await holdingsService.GetHoldingsAsync(userId, portfolioId, cancellationToken);
        if (holdings is null)
        {
            return null;
        }

        var ranked = holdings
            .Where(h => h.UnrealizedGainLossPercent is not null)
            .Select(h => new PerformerEntry(h.Symbol, h.SecurityName, h.UnrealizedGainLoss!.Value, h.UnrealizedGainLossPercent!.Value))
            .ToList();

        var top = ranked.OrderByDescending(p => p.UnrealizedGainLossPercent).Take(count).ToList();
        var bottom = ranked.OrderBy(p => p.UnrealizedGainLossPercent).Take(count).ToList();

        return new PerformersResponse(top, bottom);
    }

    private static IReadOnlyList<AllocationSlice> BuildSlices(IReadOnlyList<HoldingResponse> holdings, Func<HoldingResponse, string> labelSelector)
    {
        var totalValue = holdings.Sum(h => h.MarketValue ?? h.TotalCostBasis);

        return holdings
            .GroupBy(labelSelector)
            .Select(g =>
            {
                var value = g.Sum(h => h.MarketValue ?? h.TotalCostBasis);
                var percentage = totalValue == 0 ? 0 : value / totalValue * 100;
                return new AllocationSlice(g.Key, value, percentage);
            })
            .OrderByDescending(s => s.Value)
            .ToList();
    }

    private async Task<bool> OwnsPortfolioAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        return await db.InvestmentPortfolios.AnyAsync(p => p.Id == portfolioId && p.UserId == userId, cancellationToken);
    }
}
