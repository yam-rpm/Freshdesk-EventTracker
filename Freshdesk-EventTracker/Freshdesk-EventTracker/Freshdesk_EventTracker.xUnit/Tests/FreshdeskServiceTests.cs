using System.Net;
using System.Text;
using Freshdesk_EventTracker.BusinessLogic.Services;

namespace Freshdesk_EventTracker.xUnit.Tests;

public class FreshdeskServiceTests
{
    [Fact]
    public async Task GetAllTicketsRawAsync_PaginatesAndScopesAuthorizationToRequests()
    {
        var handler = new RecordingHandler((request, _) =>
        {
            string content = request.RequestUri?.Query.StartsWith(
                "?page=1&",
                StringComparison.Ordinal) == true
                ? "[" + string.Join(",", Enumerable.Repeat("{\"id\":1}", 100)) + "]"
                : "[{\"id\":101}]";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    content,
                    Encoding.UTF8,
                    "application/json")
            });
        });
        using var httpClient = new HttpClient(handler);
        var service = new FreshdeskService(
            "api-key",
            "https://freshdesk.example.test/api",
            httpClient: httpClient);

        List<System.Text.Json.JsonElement> tickets =
            await service.GetAllTicketsRawAsync();

        Assert.Equal(101, tickets.Count);
        Assert.Equal(2, handler.Requests.Count);
        Assert.All(
            handler.Requests,
            request =>
            {
                Assert.Equal(
                    "Basic " + Convert.ToBase64String(
                        Encoding.ASCII.GetBytes("api-key:x")),
                    request.Authorization);
                Assert.Contains("/api/v2/tickets", request.Uri);
                Assert.Contains("per_page=100", request.Uri);
            });
        Assert.Contains("page=2", handler.Requests[1].Uri);
    }

    [Fact]
    public async Task GetAllTicketsRawAsync_PropagatesHttpFailures()
    {
        var handler = new RecordingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
        using var httpClient = new HttpClient(handler);
        var service = new FreshdeskService(
            "api-key",
            "https://freshdesk.example.test/api",
            httpClient: httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.GetAllTicketsRawAsync());
    }

    [Fact]
    public async Task GetTicketConversationsRawAsync_RejectsNonArrayResponse()
    {
        var handler = new RecordingHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{}",
                    Encoding.UTF8,
                    "application/json")
            }));
        using var httpClient = new HttpClient(handler);
        var service = new FreshdeskService(
            "api-key",
            "https://freshdesk.example.test/api",
            httpClient: httpClient);

        await Assert.ThrowsAsync<System.Text.Json.JsonException>(() =>
            service.GetTicketConversationsRawAsync(123));
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> responder)
        : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.RequestUri?.ToString() ?? "",
                request.Headers.Authorization?.ToString() ?? ""));
            return responder(request, cancellationToken);
        }
    }

    private sealed record RecordedRequest(string Uri, string Authorization);
}
