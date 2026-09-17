namespace Portfolio.Domain.Entities;

/// <summary>
/// Records how much of a <see cref="Lot"/> was consumed by a specific Sell
/// transaction, and the resulting realized gain/loss for that slice.
/// </summary>
public class LotConsumption
{
    public Guid Id { get; set; }

    public Guid SellTransactionId { get; set; }

    public Transaction SellTransaction { get; set; } = null!;

    public Guid LotId { get; set; }

    public Lot Lot { get; set; } = null!;

    public decimal QuantityConsumed { get; set; }

    public decimal CostBasisConsumed { get; set; }

    public decimal ProceedsAllocated { get; set; }

    public decimal RealizedGainLoss => ProceedsAllocated - CostBasisConsumed;
}
