namespace Portfolio.Domain.Services;

/// <summary>
/// Reconstructs a portfolio's total market value and cost basis on each of a
/// set of historical dates, from its buy/sell events and each security's
/// daily closing prices. Pure and DB-free so the day-by-day sweep logic can
/// be unit tested directly.
/// </summary>
public static class PortfolioSnapshotCalculator
{
    public record BuyEvent(Guid SecurityId, DateOnly TradeDate, decimal Quantity, decimal TotalCostBasis);

    public record SellEvent(Guid SecurityId, DateOnly TradeDate, decimal Quantity, decimal CostBasisRemoved);

    public record SnapshotPoint(DateOnly Date, decimal TotalMarketValue, decimal TotalCostBasis);

    public static IReadOnlyList<SnapshotPoint> Compute(
        IReadOnlyList<BuyEvent> buys,
        IReadOnlyList<SellEvent> sells,
        IReadOnlyDictionary<Guid, IReadOnlyList<(DateOnly Date, decimal Close)>> pricesBySecurity,
        IReadOnlyList<DateOnly> snapshotDates)
    {
        var securityIds = buys.Select(b => b.SecurityId)
            .Concat(sells.Select(s => s.SecurityId))
            .Distinct()
            .ToList();

        var deltasBySecurity = securityIds.ToDictionary(
            id => id,
            id => buys.Where(b => b.SecurityId == id)
                .Select(b => (TradeDate: b.TradeDate, QuantityDelta: b.Quantity, CostBasisDelta: b.TotalCostBasis))
                .Concat(sells.Where(s => s.SecurityId == id)
                    .Select(s => (TradeDate: s.TradeDate, QuantityDelta: -s.Quantity, CostBasisDelta: -s.CostBasisRemoved)))
                .OrderBy(e => e.TradeDate)
                .ToList());

        var eventIndex = securityIds.ToDictionary(id => id, _ => 0);
        var priceIndex = securityIds.ToDictionary(id => id, _ => 0);
        var runningQuantity = securityIds.ToDictionary(id => id, _ => 0m);
        var runningCostBasis = securityIds.ToDictionary(id => id, _ => 0m);
        var lastKnownPrice = securityIds.ToDictionary(id => id, _ => (decimal?)null);

        var results = new List<SnapshotPoint>();

        foreach (var date in snapshotDates.OrderBy(d => d))
        {
            foreach (var securityId in securityIds)
            {
                var deltas = deltasBySecurity[securityId];
                var idx = eventIndex[securityId];
                while (idx < deltas.Count && deltas[idx].TradeDate <= date)
                {
                    runningQuantity[securityId] += deltas[idx].QuantityDelta;
                    runningCostBasis[securityId] += deltas[idx].CostBasisDelta;
                    idx++;
                }

                eventIndex[securityId] = idx;

                if (pricesBySecurity.TryGetValue(securityId, out var prices))
                {
                    var priceIdx = priceIndex[securityId];
                    while (priceIdx < prices.Count && prices[priceIdx].Date <= date)
                    {
                        lastKnownPrice[securityId] = prices[priceIdx].Close;
                        priceIdx++;
                    }

                    priceIndex[securityId] = priceIdx;
                }
            }

            var totalMarketValue = securityIds.Sum(id =>
                runningQuantity[id] > 0 && lastKnownPrice[id] is not null
                    ? runningQuantity[id] * lastKnownPrice[id]!.Value
                    : 0m);

            var totalCostBasis = securityIds.Sum(id => runningCostBasis[id]);

            results.Add(new SnapshotPoint(date, totalMarketValue, totalCostBasis));
        }

        return results;
    }
}
