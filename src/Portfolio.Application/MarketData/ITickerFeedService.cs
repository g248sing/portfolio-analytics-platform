namespace Portfolio.Application.MarketData;

/// <summary>
/// Serves the small, public, unauthenticated feed of recent prices used for
/// the decorative ticker-tape background — not portfolio- or user-scoped,
/// since it's just publicly-known market data already fetched for the app's
/// own holdings.
/// </summary>
public interface ITickerFeedService
{
    Task<IReadOnlyList<TickerEntryDto>> GetTickerAsync(int count, CancellationToken cancellationToken);
}
