using System.Net;
using System.Text;

namespace Portfolio.UnitTests.MarketData;

/// <summary>
/// Returns a different canned JSON response depending on whether the request
/// URI contains a given substring, to simulate Alpha Vantage behaving
/// differently for outputsize=full vs outputsize=compact.
/// </summary>
internal class RoutingFakeHttpMessageHandler(params (string UrlContains, string ResponseJson)[] routes) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var url = request.RequestUri!.ToString();
        var match = routes.First(r => url.Contains(r.UrlContains, StringComparison.OrdinalIgnoreCase));

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(match.ResponseJson, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
