using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; }

    public Guid InvestmentPortfolioId { get; set; }

    public InvestmentPortfolio InvestmentPortfolio { get; set; } = null!;

    public Guid SecurityId { get; set; }

    public Security Security { get; set; } = null!;

    public TransactionType Type { get; set; }

    public decimal Quantity { get; set; }

    public decimal PricePerUnit { get; set; }

    public decimal Fees { get; set; }

    public DateOnly TradeDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    // Populated when Type == Buy: the lot this purchase created.
    public Lot? CreatedLot { get; set; }

    // Populated when Type == Sell: which lots were drawn down to fulfil this sale.
    public ICollection<LotConsumption> LotConsumptions { get; set; } = new List<LotConsumption>();
}
