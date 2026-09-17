using Portfolio.Domain.Entities;
using Portfolio.Domain.Exceptions;

namespace Portfolio.Domain.Services;

/// <summary>
/// Consumes open lots oldest-first to satisfy a sell, mutating each lot's
/// remaining quantity and producing the <see cref="LotConsumption"/> records
/// that capture the realized gain/loss for that slice of the sale.
/// </summary>
public static class FifoLotMatcher
{
    public static IReadOnlyList<LotConsumption> Consume(
        IReadOnlyList<Lot> openLotsOldestFirst,
        Guid sellTransactionId,
        decimal sellQuantity,
        decimal totalNetProceeds)
    {
        if (sellQuantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sellQuantity), "Sell quantity must be positive.");
        }

        var totalAvailable = openLotsOldestFirst.Sum(l => l.RemainingQuantity);
        if (totalAvailable < sellQuantity)
        {
            throw new InsufficientSharesException(sellQuantity, totalAvailable);
        }

        var proceedsPerUnit = totalNetProceeds / sellQuantity;
        var consumptions = new List<LotConsumption>();
        var remaining = sellQuantity;

        foreach (var lot in openLotsOldestFirst)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (lot.RemainingQuantity <= 0)
            {
                continue;
            }

            var consumeQuantity = Math.Min(lot.RemainingQuantity, remaining);
            var costBasisConsumed = consumeQuantity * lot.CostBasisPerUnit;
            var proceedsAllocated = consumeQuantity * proceedsPerUnit;

            consumptions.Add(new LotConsumption
            {
                Id = Guid.NewGuid(),
                SellTransactionId = sellTransactionId,
                LotId = lot.Id,
                QuantityConsumed = consumeQuantity,
                CostBasisConsumed = costBasisConsumed,
                ProceedsAllocated = proceedsAllocated,
            });

            lot.RemainingQuantity -= consumeQuantity;
            remaining -= consumeQuantity;
        }

        return consumptions;
    }
}
