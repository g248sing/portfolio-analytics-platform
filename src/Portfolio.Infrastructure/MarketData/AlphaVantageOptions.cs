namespace Portfolio.Infrastructure.MarketData;

public class AlphaVantageOptions
{
    public const string SectionName = "AlphaVantage";

    public required string ApiKey { get; set; }

    public string BaseUrl { get; set; } = "https://www.alphavantage.co/";

    // Kept comfortably under the free tier's daily quota; symbols beyond this
    // count in a single run roll over to the next scheduled run, oldest-refreshed-first.
    public int MaxSymbolsPerRun { get; set; } = 20;

    // Spaces requests to stay under the free tier's per-minute quota.
    public int DelayBetweenRequestsMs { get; set; } = 15_000;
}
