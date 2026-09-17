namespace Portfolio.Domain.Exceptions;

public class InsufficientSharesException(decimal requested, decimal available)
    : Exception($"Cannot sell {requested} shares; only {available} are available.")
{
    public decimal Requested { get; } = requested;

    public decimal Available { get; } = available;
}
