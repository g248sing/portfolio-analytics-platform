namespace Portfolio.Domain.Exceptions;

/// <summary>
/// Thrown when attempting to delete a Buy transaction whose lot has already
/// been partially or fully consumed by a later sale.
/// </summary>
public class LotAlreadyConsumedException()
    : Exception("Cannot delete this buy transaction because some of its shares have already been sold.");
