using Portfolio.Domain.Enums;

namespace Portfolio.Domain.Entities;

public class Security
{
    public Guid Id { get; set; }

    public required string Symbol { get; set; }

    public required string Name { get; set; }

    public string? Sector { get; set; }

    public string? Industry { get; set; }

    public AssetClass AssetClass { get; set; } = AssetClass.Equity;

    public DateTimeOffset? MetadataRefreshedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    public ICollection<Lot> Lots { get; set; } = new List<Lot>();

    public ICollection<DailyPrice> DailyPrices { get; set; } = new List<DailyPrice>();
}
