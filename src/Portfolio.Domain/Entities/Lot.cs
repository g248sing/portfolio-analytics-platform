namespace Portfolio.Domain.Entities;

/// <summary>
/// A FIFO cost-basis lot created by a Buy transaction. Sell transactions consume
/// lots oldest-first via <see cref="LotConsumption"/> records.
/// </summary>
public class Lot
{
    public Guid Id { get; set; }

    public Guid InvestmentPortfolioId { get; set; }

    public InvestmentPortfolio InvestmentPortfolio { get; set; } = null!;

    public Guid SecurityId { get; set; }

    public Security Security { get; set; } = null!;

    public Guid BuyTransactionId { get; set; }

    public Transaction BuyTransaction { get; set; } = null!;

    public decimal OriginalQuantity { get; set; }

    public decimal RemainingQuantity { get; set; }

    // Cost basis per unit, inclusive of allocated fees.
    public decimal CostBasisPerUnit { get; set; }

    public DateOnly AcquiredDate { get; set; }

    public ICollection<LotConsumption> Consumptions { get; set; } = new List<LotConsumption>();
}
