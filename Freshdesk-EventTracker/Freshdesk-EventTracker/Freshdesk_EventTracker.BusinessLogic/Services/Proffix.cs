
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public class ProffixService : IDisposable
    {
        private const int MaximumReportPageSize = 10000;
        private const string ValidationTableRoute =
            "/pxapi/v4/ZUS/TicketLeistVali";
        private static readonly HttpClient SharedClient = new();

        private readonly string _baseUrl;
        private readonly HttpClient _client;
        private readonly ILogger? _logger;
        private string? _pxSessionId;

        public ProffixService(
            string baseUrl,
            ILogger? logger = null,
            HttpClient? httpClient = null)
        {
            if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri) ||
                (baseUri.Scheme != Uri.UriSchemeHttps &&
                 baseUri.Scheme != Uri.UriSchemeHttp))
            {
                throw new ArgumentException(
                    "Proffix base URL must be an absolute HTTP or HTTPS URL.",
                    nameof(baseUrl));
            }

            _baseUrl = baseUri.ToString().TrimEnd('/');
            _logger = logger;
            _client = httpClient ?? SharedClient;
        }

        public async Task LoginAsync(string Benutzer, string Passwort, string Datenbank, string[] Module)
        {
            if (string.IsNullOrWhiteSpace(Benutzer))
                throw new ArgumentException("Proffix user is required.", nameof(Benutzer));
            if (string.IsNullOrWhiteSpace(Passwort))
                throw new ArgumentException("Proffix password is required.", nameof(Passwort));
            if (string.IsNullOrWhiteSpace(Datenbank))
                throw new ArgumentException("Proffix database is required.", nameof(Datenbank));
            if (Module is null || Module.Length == 0 ||
                Module.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("At least one Proffix module is required.", nameof(Module));

            string url = $"{_baseUrl}/pxapi/v4/PRO/Login";

            var loginData = new
            {
                Benutzer = Benutzer,
                Passwort = Passwort,
                Datenbank = new
                {
                    Name = Datenbank
                },
                Module = Module
            };

            string json = JsonSerializer.Serialize(loginData);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, url);

            request.Content = new StringContent(
                json, Encoding.UTF8, "application/json");

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("*/*"));

            using HttpResponseMessage response =
                await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError(
                    "Proffix login failed with {StatusCode}.",
                    response.StatusCode);
            }
            response.EnsureSuccessStatusCode();
            _logger?.LogInformation("Proffix login response: {StatusCode}", response.StatusCode);
            Console.WriteLine(response.StatusCode.ToString());

            if (response.Headers.TryGetValues(
                "PxSessionId", out var sessionIds))
            {
                _pxSessionId = sessionIds.FirstOrDefault();
                _logger?.LogInformation("Proffix session acquired successfully.");
            }
            if (string.IsNullOrWhiteSpace(_pxSessionId))
            {
                throw new InvalidOperationException(
                    "Login succeeded, but no PxSessionId was returned.");
            }
        }

        public async Task<string> GetAllRapporte(
            int offset = 0,
            int limit = 100,
            string verrechnet = "ANY",
            string gesperrt = "ANY")
        {
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset));
            if (limit is < 1 or > MaximumReportPageSize)
                throw new ArgumentOutOfRangeException(
                    nameof(limit),
                    $"Limit must be between 1 and {MaximumReportPageSize}.");
            if (string.IsNullOrWhiteSpace(_pxSessionId))
            {
                throw new InvalidOperationException(
                    "Not logged in. Call LoginAsync() first.");
            }

            string url =
                $"{_baseUrl}/pxapi/v4/STU/Rapport" +
                $"?verrechnet={Uri.EscapeDataString(verrechnet)}" +
                $"&gesperrt={Uri.EscapeDataString(gesperrt)}" +
                $"&Offset={offset}" +
                $"&Limit={limit}";

            _logger?.LogInformation(
                "Loading Proffix reports at offset {Offset} with limit {Limit}.",
                offset,
                limit);

            using var request = new HttpRequestMessage(
                HttpMethod.Get, url);

            request.Headers.Add("PxSessionId", _pxSessionId);

            using HttpResponseMessage response =
                await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError(
                    "Proffix report request failed with {StatusCode}.",
                    response.StatusCode);
            }

            response.EnsureSuccessStatusCode();

            // Proffix may return a renewed session ID.
            if (response.Headers.TryGetValues(
                "PxSessionId", out var sessionIds))
            {
                string? renewedSessionId = sessionIds.FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(renewedSessionId))
                    _pxSessionId = renewedSessionId;
            }

            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
            {
                _logger?.LogWarning("Proffix returned an empty report response.");
                throw new InvalidOperationException(
                    "Proffix returned an empty report response.");
            }

            try
            {
                using JsonDocument document = JsonDocument.Parse(content);
                if (document.RootElement.ValueKind is not JsonValueKind.Array and
                    not JsonValueKind.Object)
                {
                    throw new JsonException(
                        "Proffix report response must be a JSON array or object.");
                }
            }
            catch (JsonException exception)
            {
                _logger?.LogError(
                    exception,
                    "Proffix returned an invalid report response.");
                throw;
            }

            return content;
        }

        public async Task LogoutAsync()
        {
            if (string.IsNullOrWhiteSpace(_pxSessionId))
                return;

            string url = $"{_baseUrl}/pxapi/v4/PRO/Login";

            using var request = new HttpRequestMessage(
                HttpMethod.Delete, url);

            request.Headers.Add("PxSessionId", _pxSessionId);

            using HttpResponseMessage response =
                await _client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                _logger?.LogError(
                    "Proffix logout failed with {StatusCode}.",
                    response.StatusCode);
            }
            response.EnsureSuccessStatusCode();

            _logger?.LogInformation("Proffix session closed successfully.");
            _pxSessionId = null;
        }

        public async Task WriteValidationResultsAsync(
            IEnumerable<ValidationResult> results)
        {
            ArgumentNullException.ThrowIfNull(results);

            if (string.IsNullOrWhiteSpace(_pxSessionId))
                throw new InvalidOperationException(
                    "Not logged in. Call LoginAsync() before writing validation results.");

            var records = results.Select(result => new
            {
                TicketId = result.TicketId,
                AgentId = result.AgentId,
                InteractionType = result.InteractionType,
                InteractionDate = result.InteractionDate,
                ResultTag = result.ResultTag,
                ResultText = result.ResultText,
                MatchingReports = result.MatchingReports
            }).ToList();

            if (records.Count == 0)
            {
                _logger?.LogInformation("No validation results available to write to Proffix.");
                return;
            }

            string requestUri = $"{_baseUrl}{ValidationTableRoute}";
            for (int index = 0; index < records.Count; index++)
            {
                string json = JsonSerializer.Serialize(records[index]);
                using var request = new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUri);
                request.Headers.Add("PxSessionId", _pxSessionId);
                request.Content = new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json");

                using var response = await _client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogError(
                        "Proffix validation write {RecordNumber} of {RecordCount} failed with {StatusCode}.",
                        index + 1,
                        records.Count,
                        response.StatusCode);
                }

                response.EnsureSuccessStatusCode();
            }

            _logger?.LogInformation(
                "Successfully wrote {Count} validation results to the Proffix validation table.",
                records.Count);
        }

        public void Dispose()
        {
            // Call LogoutAsync() explicitly before disposing.
        }
    }
}
