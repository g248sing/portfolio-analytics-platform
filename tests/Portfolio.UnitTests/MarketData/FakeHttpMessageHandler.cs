using System.Net;
using System.Text;

namespace Portfolio.UnitTests.MarketData;

internal class FakeHttpMessageHandler(string responseJson, HttpStatusCode statusCode = HttpStatusCode.OK) : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(responseJson, Encoding.UTF8, "application/json"),
        };
        return Task.FromResult(response);
    }
}
