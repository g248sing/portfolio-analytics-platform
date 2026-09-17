using Portfolio.Domain.Services;
using Xunit;
using static Portfolio.Domain.Services.PortfolioSnapshotCalculator;

namespace Portfolio.UnitTests.Domain;

public class PortfolioSnapshotCalculatorTests
{
    private static readonly Guid AaplId = Guid.NewGuid();
    private static readonly Guid MsftId = Guid.NewGuid();

    [Fact]
    public void Single_security_buy_then_price_rises_produces_correct_value_series()
    {
        var buys = new[] { new BuyEvent(AaplId, new DateOnly(2026, 1, 1), 10, TotalCostBasis: 1000) };
        var sells = Array.Empty<SellEvent>();
        var prices = new Dictionary<Guid, IReadOnlyList<(DateOnly, decimal)>>
        {
            [AaplId] = [(new DateOnly(2026, 1, 1), 100m), (new DateOnly(2026, 1, 2), 110m)],
        };
        var dates = new[] { new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2) };

        var result = Compute(buys, sells, prices, dates);

        Assert.Equal(2, result.Count);
        Assert.Equal(1000, result[0].TotalMarketValue);
        Assert.Equal(1000, result[0].TotalCostBasis);
        Assert.Equal(1100, result[1].TotalMarketValue);
        Assert.Equal(1000, result[1].TotalCostBasis);
    }

    [Fact]
    public void Sell_reduces_quantity_and_cost_basis_from_its_trade_date_onward()
    {
        var buys = new[] { new BuyEvent(AaplId, new DateOnly(2026, 1, 1), 10, TotalCostBasis: 1000) };
        var sells = new[] { new SellEvent(AaplId, new DateOnly(2026, 1, 3), 4, CostBasisRemoved: 400) };
        var prices = new Dictionary<Guid, IReadOnlyList<(DateOnly, decimal)>>
        {
            [AaplId] = [(new DateOnly(2026, 1, 1), 100m)],
        };
        var dates = new[] { new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3) };

        var result = Compute(buys, sells, prices, dates);

        // Before the sell: still holding all 10 shares.
        Assert.Equal(1000, result[0].TotalMarketValue);
        Assert.Equal(1000, result[0].TotalCostBasis);

        // On/after the sell date: 6 shares remain at the same $100 price, cost basis drops by 400.
        Assert.Equal(600, result[1].TotalMarketValue);
        Assert.Equal(600, result[1].TotalCostBasis);
    }

    [Fact]
    public void Missing_price_on_a_date_forward_fills_from_the_last_known_price()
    {
        var buys = new[] { new BuyEvent(AaplId, new DateOnly(2026, 1, 1), 10, TotalCostBasis: 1000) };
        var sells = Array.Empty<SellEvent>();
        // No price recorded for Jan 3 (e.g. a data gap) — should carry forward Jan 2's price.
        var prices = new Dictionary<Guid, IReadOnlyList<(DateOnly, decimal)>>
        {
            [AaplId] = [(new DateOnly(2026, 1, 1), 100m), (new DateOnly(2026, 1, 2), 120m)],
        };
        var dates = new[] { new DateOnly(2026, 1, 2), new DateOnly(2026, 1, 3) };

        var result = Compute(buys, sells, prices, dates);

        Assert.Equal(1200, result[0].TotalMarketValue);
        Assert.Equal(1200, result[1].TotalMarketValue);
    }

    [Fact]
    public void Multiple_securities_are_summed_per_date()
    {
        var buys = new[]
        {
            new BuyEvent(AaplId, new DateOnly(2026, 1, 1), 10, TotalCostBasis: 1000),
            new BuyEvent(MsftId, new DateOnly(2026, 1, 1), 5, TotalCostBasis: 1500),
        };
        var sells = Array.Empty<SellEvent>();
        var prices = new Dictionary<Guid, IReadOnlyList<(DateOnly, decimal)>>
        {
            [AaplId] = [(new DateOnly(2026, 1, 1), 100m)],
            [MsftId] = [(new DateOnly(2026, 1, 1), 300m)],
        };
        var dates = new[] { new DateOnly(2026, 1, 1) };

        var result = Compute(buys, sells, prices, dates);

        Assert.Equal(1000 + 1500, result[0].TotalMarketValue);
        Assert.Equal(1000 + 1500, result[0].TotalCostBasis);
    }

    [Fact]
    public void Date_before_any_price_data_yields_zero_market_value_but_tracks_cost_basis()
    {
        var buys = new[] { new BuyEvent(AaplId, new DateOnly(2026, 1, 5), 10, TotalCostBasis: 1000) };
        var sells = Array.Empty<SellEvent>();
        var prices = new Dictionary<Guid, IReadOnlyList<(DateOnly, decimal)>>
        {
            [AaplId] = [(new DateOnly(2026, 1, 5), 100m)],
        };
        // Snapshot date before the buy even happens: no quantity, no cost basis yet.
        var dates = new[] { new DateOnly(2026, 1, 1) };

        var result = Compute(buys, sells, prices, dates);

        Assert.Equal(0, result[0].TotalMarketValue);
        Assert.Equal(0, result[0].TotalCostBasis);
    }
}
