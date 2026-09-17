using Portfolio.Domain.Entities;
using Portfolio.Domain.Exceptions;
using Portfolio.Domain.Services;
using Xunit;

namespace Portfolio.UnitTests.Domain;

public class FifoLotMatcherTests
{
    private static Lot MakeLot(decimal remainingQuantity, decimal costBasisPerUnit, DateOnly acquiredDate) => new()
    {
        Id = Guid.NewGuid(),
        OriginalQuantity = remainingQuantity,
        RemainingQuantity = remainingQuantity,
        CostBasisPerUnit = costBasisPerUnit,
        AcquiredDate = acquiredDate,
    };

    [Fact]
    public void Single_lot_exact_consumption_fully_drains_the_lot()
    {
        var lot = MakeLot(10, costBasisPerUnit: 100, acquiredDate: new DateOnly(2026, 1, 1));

        var consumptions = FifoLotMatcher.Consume([lot], Guid.NewGuid(), sellQuantity: 10, totalNetProceeds: 1500);

        var consumption = Assert.Single(consumptions);
        Assert.Equal(10, consumption.QuantityConsumed);
        Assert.Equal(1000, consumption.CostBasisConsumed);
        Assert.Equal(1500, consumption.ProceedsAllocated);
        Assert.Equal(500, consumption.RealizedGainLoss);
        Assert.Equal(0, lot.RemainingQuantity);
    }

    [Fact]
    public void Single_lot_partial_consumption_leaves_remainder_open()
    {
        var lot = MakeLot(10, costBasisPerUnit: 100, acquiredDate: new DateOnly(2026, 1, 1));

        var consumptions = FifoLotMatcher.Consume([lot], Guid.NewGuid(), sellQuantity: 4, totalNetProceeds: 600);

        var consumption = Assert.Single(consumptions);
        Assert.Equal(4, consumption.QuantityConsumed);
        Assert.Equal(400, consumption.CostBasisConsumed);
        Assert.Equal(600, consumption.ProceedsAllocated);
        Assert.Equal(6, lot.RemainingQuantity);
    }

    [Fact]
    public void Sell_spanning_two_lots_consumes_oldest_first()
    {
        var oldLot = MakeLot(5, costBasisPerUnit: 100, acquiredDate: new DateOnly(2026, 1, 1));
        var newLot = MakeLot(10, costBasisPerUnit: 120, acquiredDate: new DateOnly(2026, 2, 1));

        // Sell 8: fully drains the 5-share old lot, then takes 3 from the new lot.
        var consumptions = FifoLotMatcher.Consume([oldLot, newLot], Guid.NewGuid(), sellQuantity: 8, totalNetProceeds: 8 * 150);

        Assert.Equal(2, consumptions.Count);

        var first = consumptions[0];
        Assert.Equal(oldLot.Id, first.LotId);
        Assert.Equal(5, first.QuantityConsumed);
        Assert.Equal(500, first.CostBasisConsumed);

        var second = consumptions[1];
        Assert.Equal(newLot.Id, second.LotId);
        Assert.Equal(3, second.QuantityConsumed);
        Assert.Equal(360, second.CostBasisConsumed);

        Assert.Equal(0, oldLot.RemainingQuantity);
        Assert.Equal(7, newLot.RemainingQuantity);

        // Proceeds should be allocated proportionally and sum back to the total.
        Assert.Equal(8 * 150, consumptions.Sum(c => c.ProceedsAllocated));
    }

    [Fact]
    public void Sell_spanning_three_lots_consumes_each_in_acquisition_order()
    {
        var lot1 = MakeLot(2, costBasisPerUnit: 10, acquiredDate: new DateOnly(2026, 1, 1));
        var lot2 = MakeLot(3, costBasisPerUnit: 20, acquiredDate: new DateOnly(2026, 2, 1));
        var lot3 = MakeLot(5, costBasisPerUnit: 30, acquiredDate: new DateOnly(2026, 3, 1));

        var consumptions = FifoLotMatcher.Consume([lot1, lot2, lot3], Guid.NewGuid(), sellQuantity: 7, totalNetProceeds: 700);

        Assert.Equal(3, consumptions.Count);
        Assert.Equal([lot1.Id, lot2.Id, lot3.Id], consumptions.Select(c => c.LotId));
        Assert.Equal([2m, 3m, 2m], consumptions.Select(c => c.QuantityConsumed));
        Assert.Equal(0, lot1.RemainingQuantity);
        Assert.Equal(0, lot2.RemainingQuantity);
        Assert.Equal(3, lot3.RemainingQuantity);
    }

    [Fact]
    public void Already_exhausted_lots_are_skipped()
    {
        var exhaustedLot = MakeLot(0, costBasisPerUnit: 50, acquiredDate: new DateOnly(2026, 1, 1));
        var openLot = MakeLot(5, costBasisPerUnit: 60, acquiredDate: new DateOnly(2026, 2, 1));

        var consumptions = FifoLotMatcher.Consume([exhaustedLot, openLot], Guid.NewGuid(), sellQuantity: 5, totalNetProceeds: 400);

        var consumption = Assert.Single(consumptions);
        Assert.Equal(openLot.Id, consumption.LotId);
        Assert.Equal(0, openLot.RemainingQuantity);
    }

    [Fact]
    public void Selling_more_than_available_throws_insufficient_shares()
    {
        var lot = MakeLot(3, costBasisPerUnit: 10, acquiredDate: new DateOnly(2026, 1, 1));

        var ex = Assert.Throws<InsufficientSharesException>(
            () => FifoLotMatcher.Consume([lot], Guid.NewGuid(), sellQuantity: 4, totalNetProceeds: 100));

        Assert.Equal(4, ex.Requested);
        Assert.Equal(3, ex.Available);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_sell_quantity_throws(decimal sellQuantity)
    {
        var lot = MakeLot(3, costBasisPerUnit: 10, acquiredDate: new DateOnly(2026, 1, 1));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => FifoLotMatcher.Consume([lot], Guid.NewGuid(), sellQuantity, totalNetProceeds: 100));
    }
}
