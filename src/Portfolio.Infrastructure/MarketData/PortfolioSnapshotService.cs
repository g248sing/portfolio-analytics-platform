using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;
using Portfolio.Domain.Services;
using Portfolio.Infrastructure.Persistence;

namespace Portfolio.Infrastructure.MarketData;

/// <summary>
/// Recomputes each portfolio's daily <see cref="PortfolioValueSnapshot"/> rows
/// from its transaction history and the latest daily prices, using the pure
/// <see cref="PortfolioSnapshotCalculator"/> sweep.
/// </summary>
public class PortfolioSnapshotService(PortfolioDbContext db)
{
    public async Task RecomputeAllAsync(CancellationToken cancellationToken)
    {
        var portfolioIds = await db.InvestmentPortfolios.Select(p => p.Id).ToListAsync(cancellationToken);
        foreach (var portfolioId in portfolioIds)
        {
            await RecomputeAsync(portfolioId, cancellationToken);
        }
    }

    public async Task RecomputeAsync(Guid portfolioId, CancellationToken cancellationToken)
    {
        var transactions = await db.Transactions
            .Where(t => t.InvestmentPortfolioId == portfolioId && t.Type != TransactionType.Dividend)
            .Select(t => new { t.Id, t.SecurityId, t.Type, t.TradeDate, t.Quantity, t.PricePerUnit, t.Fees })
            .ToListAsync(cancellationToken);

        if (transactions.Count == 0)
        {
            return;
        }

        var buys = transactions
            .Where(t => t.Type == TransactionType.Buy)
            .Select(t => new PortfolioSnapshotCalculator.BuyEvent(t.SecurityId, t.TradeDate, t.Quantity, t.Quantity * t.PricePerUnit + t.Fees))
            .ToList();

        var sellTransactionIds = transactions.Where(t => t.Type == TransactionType.Sell).Select(t => t.Id).ToList();
        var costBasisConsumedBySell = await db.LotConsumptions
            .Where(lc => sellTransactionIds.Contains(lc.SellTransactionId))
            .GroupBy(lc => lc.SellTransactionId)
            .Select(g => new { SellTransactionId = g.Key, CostBasisConsumed = g.Sum(lc => lc.CostBasisConsumed) })
            .ToDictionaryAsync(x => x.SellTransactionId, x => x.CostBasisConsumed, cancellationToken);

        var sells = transactions
            .Where(t => t.Type == TransactionType.Sell)
            .Select(t => new PortfolioSnapshotCalculator.SellEvent(
                t.SecurityId, t.TradeDate, t.Quantity, costBasisConsumedBySell.GetValueOrDefault(t.Id)))
            .ToList();

        var securityIds = transactions.Select(t => t.SecurityId).Distinct().ToList();
        var priceRows = await db.DailyPrices
            .Where(dp => securityIds.Contains(dp.SecurityId))
            .OrderBy(dp => dp.Date)
            .Select(dp => new { dp.SecurityId, dp.Date, dp.Close })
            .ToListAsync(cancellationToken);

        if (priceRows.Count == 0)
        {
            return;
        }

        var pricesBySecurity = priceRows
            .GroupBy(p => p.SecurityId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<(DateOnly Date, decimal Close)>)g.Select(p => (p.Date, p.Close)).ToList());

        var earliestTransactionDate = transactions.Min(t => t.TradeDate);
        var snapshotDates = priceRows
            .Select(p => p.Date)
            .Where(d => d >= earliestTransactionDate)
            .Distinct()
            .OrderBy(d => d)
            .ToList();

        if (snapshotDates.Count == 0)
        {
            return;
        }

        var computed = PortfolioSnapshotCalculator.Compute(buys, sells, pricesBySecurity, snapshotDates);

        var existingSnapshots = await db.PortfolioValueSnapshots
            .Where(s => s.InvestmentPortfolioId == portfolioId)
            .ToDictionaryAsync(s => s.Date, cancellationToken);

        foreach (var point in computed)
        {
            if (existingSnapshots.TryGetValue(point.Date, out var existing))
            {
                existing.TotalMarketValue = point.TotalMarketValue;
                existing.TotalCostBasis = point.TotalCostBasis;
            }
            else
            {
                db.PortfolioValueSnapshots.Add(new PortfolioValueSnapshot
                {
                    Id = Guid.NewGuid(),
                    InvestmentPortfolioId = portfolioId,
                    Date = point.Date,
                    TotalMarketValue = point.TotalMarketValue,
                    TotalCostBasis = point.TotalCostBasis,
                });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
