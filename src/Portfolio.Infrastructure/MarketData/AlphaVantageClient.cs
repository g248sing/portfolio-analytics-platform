using System.Globalization;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Portfolio.Application.MarketData;
using Portfolio.Domain.Exceptions;

namespace Portfolio.Infrastructure.MarketData;

public class AlphaVantageClient(HttpClient httpClient, IOptions<AlphaVantageOptions> options) : IMarketDataClient
{
    // Alpha Vantage enforces a per-second burst limit even between two calls
    // for the same symbol (e.g. the full-history retry below), independent of
    // the caller's own between-symbol spacing.
    private const int MinRequestSpacingMs = 1200;

    private readonly AlphaVantageOptions _options = options.Value;

    public async Task<IReadOnlyList<DailyPricePoint>> GetDailySeriesAsync(string symbol, bool fullHistory, CancellationToken cancellationToken)
    {
        try
        {
            return await FetchDailySeriesAsync(symbol, fullHistory, cancellationToken);
        }
        catch (MarketDataRateLimitedException ex) when (fullHistory && ex.Message.Contains("premium", StringComparison.OrdinalIgnoreCase))
        {
            // Free-tier accounts can't request outputsize=full; fall back to the
            // compact (~100 trading day) window rather than failing the refresh.
            await Task.Delay(MinRequestSpacingMs, cancellationToken);
            return await FetchDailySeriesAsync(symbol, fullHistory: false, cancellationToken);
        }
    }

    private async Task<IReadOnlyList<DailyPricePoint>> FetchDailySeriesAsync(string symbol, bool fullHistory, CancellationToken cancellationToken)
    {
        var outputSize = fullHistory ? "full" : "compact";
        var url = $"query?function=TIME_SERIES_DAILY&symbol={Uri.EscapeDataString(symbol)}&outputsize={outputSize}&apikey={_options.ApiKey}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        ThrowIfRateLimitedOrError(root);

        if (!root.TryGetProperty("Time Series (Daily)", out var series))
        {
            return [];
        }

        var points = new List<DailyPricePoint>();
        foreach (var day in series.EnumerateObject())
        {
            var date = DateOnly.Parse(day.Name, CultureInfo.InvariantCulture);
            var close = decimal.Parse(day.Value.GetProperty("4. close").GetString()!, CultureInfo.InvariantCulture);
            long? volume = day.Value.TryGetProperty("5. volume", out var volumeElement)
                && long.TryParse(volumeElement.GetString(), out var parsedVolume)
                ? parsedVolume
                : null;

            points.Add(new DailyPricePoint(date, close, volume));
        }

        return points;
    }

    public async Task<SecurityOverview?> GetOverviewAsync(string symbol, CancellationToken cancellationToken)
    {
        var url = $"query?function=OVERVIEW&symbol={Uri.EscapeDataString(symbol)}&apikey={_options.ApiKey}";

        using var response = await httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
        var root = document.RootElement;

        ThrowIfRateLimitedOrError(root);

        if (!root.TryGetProperty("Symbol", out _))
        {
            return null;
        }

        string? GetOrNull(string name) => root.TryGetProperty(name, out var element) ? element.GetString() : null;

        return new SecurityOverview(
            GetOrNull("Symbol") ?? symbol,
            GetOrNull("Name") ?? symbol,
            GetOrNull("Sector"),
            GetOrNull("Industry"));
    }

    private static void ThrowIfRateLimitedOrError(JsonElement root)
    {
        if (root.TryGetProperty("Note", out var note))
        {
            throw new MarketDataRateLimitedException(note.GetString() ?? "Alpha Vantage rate limit reached.");
        }

        if (root.TryGetProperty("Information", out var information))
        {
            throw new MarketDataRateLimitedException(information.GetString() ?? "Alpha Vantage rate limit reached.");
        }

        if (root.TryGetProperty("Error Message", out var errorMessage))
        {
            throw new InvalidOperationException(errorMessage.GetString() ?? "Alpha Vantage returned an error.");
        }
    }
}
