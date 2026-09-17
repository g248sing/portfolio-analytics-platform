namespace Portfolio.Domain.Entities;

/// <summary>
/// Precomputed daily total value for a portfolio, populated by the background
/// job so the "value over time" chart doesn't recompute from raw joins on every request.
/// </summary>
public class PortfolioValueSnapshot
{
    public Guid Id { get; set; }

    public Guid InvestmentPortfolioId { get; set; }

    public InvestmentPortfolio InvestmentPortfolio { get; set; } = null!;

    public DateOnly Date { get; set; }

    public decimal TotalMarketValue { get; set; }

    public decimal TotalCostBasis { get; set; }
}
