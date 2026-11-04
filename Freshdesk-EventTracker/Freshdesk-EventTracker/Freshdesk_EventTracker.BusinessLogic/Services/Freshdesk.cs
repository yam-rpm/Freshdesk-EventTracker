using Freshdesk_Ticket_Viewer.Shared.Dto;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public class FreshdeskService
    {
        private const int PageSize = 100;
        private const string ApiVersion = "v2";
        private static readonly HttpClient SharedClient = new();

        private readonly HttpClient _client;
        private readonly string _baseUrl;
        private readonly string _apiKey;
        private readonly ILogger? _logger;

        public FreshdeskService(
            string apiKey,
            string baseUrl,
            ILogger? logger = null,
            HttpClient? httpClient = null)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
                throw new ArgumentException("Freshdesk API key is required.", nameof(apiKey));

            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttps &&
                 baseUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new ArgumentException(
                    "Freshdesk base URL must be an absolute HTTP or HTTPS URL.",
                    nameof(baseUrl));
            }

            _apiKey = apiKey;
            _baseUrl = baseUri.ToString().TrimEnd('/');
            _logger = logger;
            _client = httpClient ?? SharedClient;
        }

        public async Task<List<Ticket>> GetAllTickets(
            CancellationToken cancellationToken = default)
        {
            List<JsonElement> rawTickets =
                await GetAllTicketsRawAsync(cancellationToken);

            return rawTickets
                .Select(ticket => ticket.Deserialize<Ticket>())
                .Where(ticket => ticket is not null)
                .Cast<Ticket>()
                .ToList();
        }

        public async Task<List<JsonElement>> GetAllTicketsRawAsync(
            CancellationToken cancellationToken = default)
        {
            var tickets = new List<JsonElement>();

            for (int page = 1; ; page++)
            {
                string url =
                    $"{_baseUrl}/{ApiVersion}/tickets" +
                    $"?page={page}&per_page={PageSize}";

                using var request = CreateRequest(HttpMethod.Get, url);
                using HttpResponseMessage response =
                    await _client.SendAsync(request, cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError(
                        "Freshdesk ticket request failed with {StatusCode} on page {Page}.",
                        response.StatusCode,
                        page);
                }

                response.EnsureSuccessStatusCode();

                using JsonDocument document = JsonDocument.Parse(
                    await response.Content.ReadAsStringAsync(cancellationToken));

                JsonElement items = document.RootElement;
                if (items.ValueKind != JsonValueKind.Array)
                {
                    throw new JsonException(
                        "Freshdesk ticket response must be a JSON array.");
                }

                foreach (JsonElement item in items.EnumerateArray())
                    tickets.Add(item.Clone());

                if (items.GetArrayLength() < PageSize)
                    break;
            }

            _logger?.LogInformation(
                "Loaded {TicketCount} Freshdesk tickets.",
                tickets.Count);

            return tickets;
        }

        public async Task<List<JsonElement>> GetTicketConversationsRawAsync(
            int ticketId,
            CancellationToken cancellationToken = default)
        {
            if (ticketId <= 0)
                throw new ArgumentOutOfRangeException(nameof(ticketId));

            string url =
                $"{_baseUrl}/{ApiVersion}/tickets/{ticketId}/conversations";

            using var request = CreateRequest(HttpMethod.Get, url);
            using HttpResponseMessage response =
                await _client.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError(
                    "Freshdesk conversation request for ticket {TicketId} failed with {StatusCode}.",
                    ticketId,
                    response.StatusCode);
            }

            response.EnsureSuccessStatusCode();

            using JsonDocument document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync(cancellationToken));

            if (document.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new JsonException(
                    $"Freshdesk conversations for ticket {ticketId} must be a JSON array.");
            }

            var conversations = document.RootElement
                .EnumerateArray()
                .Select(item => item.Clone())
                .ToList();

            _logger?.LogInformation(
                "Loaded {ConversationCount} conversations for ticket {TicketId}.",
                conversations.Count,
                ticketId);

            return conversations;
        }

        private HttpRequestMessage CreateRequest(HttpMethod method, string url)
        {
            string credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_apiKey}:x"));

            var request = new HttpRequestMessage(method, url);
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            return request;
        }
    }
}
