namespace Portfolio.Domain.Exceptions;

/// <summary>
/// Thrown when the market data provider signals its call quota has been
/// exhausted (Alpha Vantage reports this as a 200 OK with a "Note"/
/// "Information" field rather than an HTTP error status).
/// </summary>
public class MarketDataRateLimitedException(string message) : Exception(message);
