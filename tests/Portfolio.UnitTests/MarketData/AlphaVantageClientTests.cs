using Microsoft.Extensions.Options;
using Portfolio.Domain.Exceptions;
using Portfolio.Infrastructure.MarketData;
using Xunit;

namespace Portfolio.UnitTests.MarketData;

public class AlphaVantageClientTests
{
    private static AlphaVantageClient CreateClient(string responseJson)
    {
        var handler = new FakeHttpMessageHandler(responseJson);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://www.alphavantage.co/") };
        var options = Options.Create(new AlphaVantageOptions { ApiKey = "test-key" });
        return new AlphaVantageClient(httpClient, options);
    }

    [Fact]
    public async Task GetDailySeriesAsync_parses_close_and_volume()
    {
        const string json = """
        {
          "Meta Data": { "2. Symbol": "AAPL" },
          "Time Series (Daily)": {
            "2026-01-02": { "1. open": "149.0", "4. close": "150.25", "5. volume": "1000000" },
            "2026-01-01": { "1. open": "148.0", "4. close": "148.75", "5. volume": "900000" }
          }
        }
        """;

        var client = CreateClient(json);
        var points = await client.GetDailySeriesAsync("AAPL", fullHistory: false, CancellationToken.None);

        Assert.Equal(2, points.Count);
        var jan2 = Assert.Single(points, p => p.Date == new DateOnly(2026, 1, 2));
        Assert.Equal(150.25m, jan2.Close);
        Assert.Equal(1_000_000, jan2.Volume);
    }

    [Fact]
    public async Task GetDailySeriesAsync_throws_on_rate_limit_note()
    {
        const string json = """{ "Note": "Thank you for using Alpha Vantage! Our rate limit is..." }""";

        var client = CreateClient(json);

        await Assert.ThrowsAsync<MarketDataRateLimitedException>(
            () => client.GetDailySeriesAsync("AAPL", fullHistory: false, CancellationToken.None));
    }

    [Fact]
    public async Task GetDailySeriesAsync_throws_on_information_quota_message()
    {
        const string json = """{ "Information": "Thank you for using Alpha Vantage! ...25 requests per day..." }""";

        var client = CreateClient(json);

        await Assert.ThrowsAsync<MarketDataRateLimitedException>(
            () => client.GetDailySeriesAsync("AAPL", fullHistory: false, CancellationToken.None));
    }

    [Fact]
    public async Task GetOverviewAsync_parses_sector_and_industry()
    {
        const string json = """
        {
          "Symbol": "AAPL",
          "Name": "Apple Inc",
          "Sector": "TECHNOLOGY",
          "Industry": "ELECTRONIC COMPUTERS"
        }
        """;

        var client = CreateClient(json);
        var overview = await client.GetOverviewAsync("AAPL", CancellationToken.None);

        Assert.NotNull(overview);
        Assert.Equal("Apple Inc", overview.Name);
        Assert.Equal("TECHNOLOGY", overview.Sector);
        Assert.Equal("ELECTRONIC COMPUTERS", overview.Industry);
    }

    [Fact]
    public async Task GetOverviewAsync_returns_null_for_unknown_symbol()
    {
        const string json = "{}";

        var client = CreateClient(json);
        var overview = await client.GetOverviewAsync("BOGUS", CancellationToken.None);

        Assert.Null(overview);
    }

    [Fact]
    public async Task GetDailySeriesAsync_falls_back_to_compact_when_full_history_requires_premium()
    {
        const string premiumRequiredJson = """
        { "Information": "...outputsize=full parameter value is a premium feature..." }
        """;
        const string compactJson = """
        {
          "Time Series (Daily)": {
            "2026-01-01": { "4. close": "148.75", "5. volume": "900000" }
          }
        }
        """;

        var handler = new RoutingFakeHttpMessageHandler(
            ("outputsize=full", premiumRequiredJson),
            ("outputsize=compact", compactJson));
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://www.alphavantage.co/") };
        var client = new AlphaVantageClient(httpClient, Options.Create(new AlphaVantageOptions { ApiKey = "test-key" }));

        var points = await client.GetDailySeriesAsync("AAPL", fullHistory: true, CancellationToken.None);

        var point = Assert.Single(points);
        Assert.Equal(148.75m, point.Close);
    }
}
