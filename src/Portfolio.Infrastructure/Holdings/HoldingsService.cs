using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Holdings;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.Holdings;

public class HoldingsService(PortfolioDbContext db) : IHoldingsService
{
    public async Task<IReadOnlyList<HoldingResponse>?> GetHoldingsAsync(Guid userId, Guid portfolioId, CancellationToken cancellationToken)
    {
        var owns = await db.InvestmentPortfolios.AnyAsync(p => p.Id == portfolioId && p.UserId == userId, cancellationToken);
        if (!owns)
        {
            return null;
        }

        var lots = await db.Lots
            .Where(l => l.InvestmentPortfolioId == portfolioId && l.RemainingQuantity > 0)
            .Select(l => new
            {
                l.SecurityId,
                l.Security.Symbol,
                l.Security.Name,
                l.Security.Sector,
                l.RemainingQuantity,
                l.CostBasisPerUnit,
            })
            .ToListAsync(cancellationToken);

        var realizedGainLossBySecurity = await db.LotConsumptions
            .Where(lc => lc.Lot.InvestmentPortfolioId == portfolioId)
            .GroupBy(lc => lc.Lot.SecurityId)
            .Select(g => new { SecurityId = g.Key, RealizedGainLoss = g.Sum(lc => lc.ProceedsAllocated - lc.CostBasisConsumed) })
            .ToDictionaryAsync(x => x.SecurityId, x => x.RealizedGainLoss, cancellationToken);

        var latestPrices = await db.DailyPrices
            .Where(dp => lots.Select(l => l.SecurityId).Distinct().Contains(dp.SecurityId))
            .GroupBy(dp => dp.SecurityId)
            .Select(g => g.OrderByDescending(dp => dp.Date).First())
            .ToDictionaryAsync(dp => dp.SecurityId, dp => dp.Close, cancellationToken);

        var holdings = lots
            .GroupBy(l => new { l.SecurityId, l.Symbol, l.Name, l.Sector })
            .Select(g =>
            {
                var quantity = g.Sum(l => l.RemainingQuantity);
                var totalCostBasis = g.Sum(l => l.RemainingQuantity * l.CostBasisPerUnit);
                var averageCostBasisPerUnit = quantity == 0 ? 0 : totalCostBasis / quantity;
                decimal? currentPrice = latestPrices.TryGetValue(g.Key.SecurityId, out var price) ? price : null;
                var marketValue = currentPrice is null ? (decimal?)null : quantity * currentPrice.Value;
                var unrealizedGainLoss = marketValue is null ? (decimal?)null : marketValue - totalCostBasis;
                var unrealizedGainLossPercent = unrealizedGainLoss is null || totalCostBasis == 0
                    ? (decimal?)null
                    : unrealizedGainLoss / totalCostBasis * 100;

                return new HoldingResponse(
                    g.Key.Symbol,
                    g.Key.Name,
                    g.Key.Sector,
                    quantity,
                    averageCostBasisPerUnit,
                    totalCostBasis,
                    currentPrice,
                    marketValue,
                    unrealizedGainLoss,
                    unrealizedGainLossPercent,
                    realizedGainLossBySecurity.GetValueOrDefault(g.Key.SecurityId));
            })
            .OrderByDescending(h => h.MarketValue ?? h.TotalCostBasis)
            .ToList();

        return holdings;
    }
}
