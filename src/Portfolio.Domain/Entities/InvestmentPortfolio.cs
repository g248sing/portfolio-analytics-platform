namespace Portfolio.Domain.Entities;

public class InvestmentPortfolio
{
    public Guid Id { get; set; }

    // FK to the owning ApplicationUser (Identity). No navigation property here —
    // Domain does not reference the Identity/Infrastructure layer.
    public Guid UserId { get; set; }

    public required string Name { get; set; }

    public string BaseCurrency { get; set; } = "USD";

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<Lot> Lots { get; set; } = new List<Lot>();

    public ICollection<PortfolioValueSnapshot> ValueSnapshots { get; set; } = new List<PortfolioValueSnapshot>();
}
