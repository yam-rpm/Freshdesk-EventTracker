using System.Net;
using System.Text.Json;
using Freshdesk_EventTracker.BusinessLogic.Services;

namespace Freshdesk_EventTracker.xUnit.Tests;

public class ProffixWriteTests
{
    [Fact]
    public async Task WriteValidationResultsAsync_PostsOneRecordPerRequestToFixedTable()
    {
        var handler = new RecordingHandler();
        using var httpClient = new HttpClient(handler);
        var service = new ProffixService(
            "https://proffix.example.test",
            httpClient: httpClient);

        await service.LoginAsync("user", "password", "database", ["VOL"]);
        await service.WriteValidationResultsAsync(
        [
            new ValidationResult
            {
                TicketId = 123,
                AgentId = 17,
                InteractionType = "Antwort",
                ResultTag = ValidationResultTags.Match,
                ResultText = "Matched",
                MatchingReports = 1
            },
            new ValidationResult
            {
                TicketId = 456,
                AgentId = 18,
                InteractionType = "Notiz",
                ResultTag = ValidationResultTags.NoMatch,
                ResultText = "No match"
            }
        ]);

        Assert.Equal(2, handler.WriteRequests.Count);
        Assert.All(
            handler.WriteRequests,
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal(
                    "https://proffix.example.test/pxapi/v4/ZUS/TicketLeistVali",
                    request.Uri);
                Assert.Equal("session-id", request.SessionId);
            });

        using JsonDocument firstRecord = JsonDocument.Parse(
            handler.WriteRequests[0].Body);
        Assert.Equal(
            123,
            firstRecord.RootElement.GetProperty("TicketId").GetInt32());
    }

    [Fact]
    public async Task WriteValidationResultsAsync_DoesNotRetryRejectedWrite()
    {
        var handler = new RecordingHandler(
            _ => new HttpResponseMessage(HttpStatusCode.BadRequest));
        using var httpClient = new HttpClient(handler);
        var service = new ProffixService(
            "https://proffix.example.test",
            httpClient: httpClient);
        await service.LoginAsync("user", "password", "database", ["VOL"]);

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            service.WriteValidationResultsAsync(
            [
                new ValidationResult
                {
                    TicketId = 123,
                    ResultTag = ValidationResultTags.NoMatch
                }
            ]));

        Assert.Single(handler.WriteRequests);
    }

    [Fact]
    public async Task WriteValidationResultsAsync_RequiresSession()
    {
        using var httpClient = new HttpClient(new RecordingHandler());
        var service = new ProffixService(
            "https://proffix.example.test",
            httpClient: httpClient);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.WriteValidationResultsAsync(
                Array.Empty<ValidationResult>()));
    }

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage>? writeResponse = null)
        : HttpMessageHandler
    {
        public List<RecordedRequest> WriteRequests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (request.Method == HttpMethod.Post &&
                request.RequestUri?.AbsolutePath == "/pxapi/v4/PRO/Login")
            {
                var loginResponse =
                    new HttpResponseMessage(HttpStatusCode.OK);
                loginResponse.Headers.Add("PxSessionId", "session-id");
                return loginResponse;
            }

            if (request.Method == HttpMethod.Post)
            {
                string body = request.Content is null
                    ? ""
                    : await request.Content.ReadAsStringAsync(cancellationToken);
                WriteRequests.Add(new RecordedRequest(
                    request.Method,
                    request.RequestUri?.ToString() ?? "",
                    body,
                    request.Headers.TryGetValues(
                        "PxSessionId",
                        out IEnumerable<string>? sessionIds)
                            ? sessionIds.Single()
                            : ""));

                return writeResponse?.Invoke(request)
                    ?? new HttpResponseMessage(HttpStatusCode.OK);
            }

            return new HttpResponseMessage(HttpStatusCode.OK);
        }
    }

    private sealed record RecordedRequest(
        HttpMethod Method,
        string Uri,
        string Body,
        string SessionId);
}
