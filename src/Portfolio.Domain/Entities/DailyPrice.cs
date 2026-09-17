namespace Portfolio.Domain.Entities;

public class DailyPrice
{
    public Guid Id { get; set; }

    public Guid SecurityId { get; set; }

    public Security Security { get; set; } = null!;

    public DateOnly Date { get; set; }

    public decimal Close { get; set; }

    public long? Volume { get; set; }
}
