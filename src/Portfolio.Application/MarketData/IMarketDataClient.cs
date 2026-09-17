namespace Portfolio.Application.MarketData;

public interface IMarketDataClient
{
    /// <param name="fullHistory">
    /// When true, requests the full available price history instead of just
    /// the most recent ~100 trading days (used to backfill a newly tracked symbol).
    /// </param>
    Task<IReadOnlyList<DailyPricePoint>> GetDailySeriesAsync(string symbol, bool fullHistory, CancellationToken cancellationToken);

    Task<SecurityOverview?> GetOverviewAsync(string symbol, CancellationToken cancellationToken);
}
