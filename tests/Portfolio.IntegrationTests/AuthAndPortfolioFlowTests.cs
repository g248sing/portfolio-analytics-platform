using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Portfolio.Application.Auth.Dtos;
using Portfolio.Application.Holdings;
using Portfolio.Application.Portfolios.Dtos;
using Portfolio.Application.Transactions.Dtos;
using Portfolio.Domain.Enums;
using Xunit;

namespace Portfolio.IntegrationTests;

[Collection("Integration")]
public class AuthAndPortfolioFlowTests(PostgresFixture postgres) : IAsyncLifetime
{
    // Matches the API's own JsonStringEnumConverter (Program.cs) so enum fields
    // like Transaction.Type round-trip correctly through this test client.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private CustomWebApplicationFactory _factory = null!;
    private HttpClient _client = null!;

    public Task InitializeAsync()
    {
        _factory = new CustomWebApplicationFactory(postgres.Container.GetConnectionString());
        _client = _factory.CreateClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    private async Task<string> RegisterAndGetAccessTokenAsync(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "SuperSecret123!" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        return body!.AccessToken;
    }

    [Fact]
    public async Task Register_then_login_round_trips_successfully()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await RegisterAndGetAccessTokenAsync(email);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "SuperSecret123!" });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Registering_the_same_email_twice_returns_conflict()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await RegisterAndGetAccessTokenAsync(email);

        var secondAttempt = await _client.PostAsJsonAsync("/api/auth/register", new { email, password = "SuperSecret123!" });

        Assert.Equal(HttpStatusCode.Conflict, secondAttempt.StatusCode);
    }

    [Fact]
    public async Task Login_with_wrong_password_is_unauthorized()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        await RegisterAndGetAccessTokenAsync(email);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPassword1!" });

        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Full_buy_flow_produces_correct_holdings()
    {
        var email = $"{Guid.NewGuid()}@example.com";
        var accessToken = await RegisterAndGetAccessTokenAsync(email);
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var portfolioResponse = await _client.PostAsJsonAsync("/api/portfolios", new { name = "Integration Test Portfolio", baseCurrency = "USD" });
        portfolioResponse.EnsureSuccessStatusCode();
        var portfolio = await portfolioResponse.Content.ReadFromJsonAsync<PortfolioResponse>();

        var buyResponse = await _client.PostAsJsonAsync(
            $"/api/portfolios/{portfolio!.Id}/transactions",
            new { symbol = "AAPL", type = TransactionType.Buy, quantity = 10m, pricePerUnit = 150m, fees = 1m, tradeDate = "2026-01-15" });
        buyResponse.EnsureSuccessStatusCode();
        var transaction = await buyResponse.Content.ReadFromJsonAsync<TransactionResponse>(JsonOptions);
        Assert.Equal(TransactionType.Buy, transaction!.Type);

        var holdingsResponse = await _client.GetAsync($"/api/portfolios/{portfolio.Id}/holdings");
        holdingsResponse.EnsureSuccessStatusCode();
        var holdings = await holdingsResponse.Content.ReadFromJsonAsync<List<HoldingResponse>>(JsonOptions);

        var holding = Assert.Single(holdings!);
        Assert.Equal("AAPL", holding.Symbol);
        Assert.Equal(10m, holding.Quantity);
        Assert.Equal(150.1m, holding.AverageCostBasisPerUnit);
    }

    [Fact]
    public async Task Another_users_portfolio_is_not_visible()
    {
        var ownerToken = await RegisterAndGetAccessTokenAsync($"{Guid.NewGuid()}@example.com");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ownerToken);
        var portfolioResponse = await _client.PostAsJsonAsync("/api/portfolios", new { name = "Owner's portfolio", baseCurrency = "USD" });
        var portfolio = await portfolioResponse.Content.ReadFromJsonAsync<PortfolioResponse>();

        var otherToken = await RegisterAndGetAccessTokenAsync($"{Guid.NewGuid()}@example.com");
        _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", otherToken);

        var response = await _client.GetAsync($"/api/portfolios/{portfolio!.Id}/holdings");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
