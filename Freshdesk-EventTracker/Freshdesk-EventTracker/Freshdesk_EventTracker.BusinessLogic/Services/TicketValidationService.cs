using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public class TicketValidationService
    {
        private readonly FreshdeskService _freshdesk;
        private readonly ProffixService _proffix;
        private readonly ILogger? _logger;

        public TicketValidationService(
            FreshdeskService freshdesk,
            ProffixService proffix,
            ILogger? logger = null)
        {
            _freshdesk = freshdesk;
            _proffix = proffix;
            _logger = logger;
        }

        public async Task<List<ValidationResult>> ValidateAsync()
        {
            var results = new List<ValidationResult>();

            _logger?.LogInformation("Starting ticket validation run.");

            // Read Freshdesk tickets and Proffix reports.
            List<JsonElement> tickets;
            try
            {
                tickets = await _freshdesk.GetAllTicketsRawAsync();
            }
            catch (HttpRequestException exception)
            {
                _logger?.LogError(
                    exception,
                    "Freshdesk ticket retrieval failed.");
                throw;
            }
            catch (JsonException exception)
            {
                _logger?.LogError(
                    exception,
                    "Freshdesk returned invalid ticket data.");
                throw;
            }

            _logger?.LogInformation("Fetched {TicketCount} Freshdesk tickets.", tickets.Count);
            if (tickets.Count == 0)
                _logger?.LogWarning("Freshdesk returned no tickets to validate.");

            const int pageSize = 10000;
            var reports = new List<JsonElement>();
            for (int offset = 0; ; offset += pageSize)
            {
                string rapportJson;
                try
                {
                    rapportJson = await _proffix.GetAllRapporte(
                        offset,
                        pageSize);
                }
                catch (HttpRequestException exception)
                {
                    _logger?.LogError(
                        exception,
                        "Proffix report retrieval failed at offset {Offset}.",
                        offset);
                    throw;
                }

                List<JsonElement> pageReports;
                try
                {
                    using var rapportDocument = JsonDocument.Parse(rapportJson);
                    pageReports = GetReportObjects(
                        rapportDocument.RootElement).ToList();
                }
                catch (JsonException exception)
                {
                    _logger?.LogError(
                        exception,
                        "Proffix report data was invalid at offset {Offset}.",
                        offset);
                    throw;
                }

                reports.AddRange(pageReports);

                if (pageReports.Count < pageSize)
                    break;
            }

            _logger?.LogInformation("Fetched {ReportCount} Proffix report entries.", reports.Count);
            if (reports.Count == 0)
                _logger?.LogWarning("Proffix returned no reports to compare.");

            foreach (var ticket in tickets)
            {
                int ticketId = GetInt(ticket, "id");

                if (ticketId == 0)
                {
                    _logger?.LogWarning(
                        "Skipping Freshdesk ticket without a valid ticket ID.");
                    continue;
                }

                int customerId = GetInt(ticket, "requester_id");

                string addressNumber = GetCustomField(
                    ticket, "cf_adressnrpx", "AdressNrPX");

                List<JsonElement> conversations;
                try
                {
                    conversations =
                        await _freshdesk.GetTicketConversationsRawAsync(
                            ticketId);
                }
                catch (HttpRequestException exception)
                {
                    _logger?.LogError(
                        exception,
                        "Freshdesk conversation retrieval failed for ticket {TicketId}.",
                        ticketId);
                    throw;
                }
                catch (JsonException exception)
                {
                    _logger?.LogError(
                        exception,
                        "Freshdesk returned invalid conversations for ticket {TicketId}.",
                        ticketId);
                    throw;
                }

                if (conversations.Count == 0)
                {
                    _logger?.LogWarning(
                        "Freshdesk ticket {TicketId} has no conversations.",
                        ticketId);
                }

                foreach (var conversation in conversations)
                {
                    // Ignore conversations without an identified author.
                    int agentId = GetInt(conversation, "user_id");

                    if (agentId == 0)
                    {
                        _logger?.LogWarning(
                            "Skipping a conversation on ticket {TicketId} without a valid user ID.",
                            ticketId);
                        continue;
                    }

                    // Only validate agent-created replies and notes.
                    bool isPrivate = GetBool(conversation, "private");

                    string interactionType =
                        isPrivate ? "Notiz" : "Antwort";

                    DateTime? interactionDate = GetDate(
                        conversation, "created_at");

                    var result = TicketValidationMatcher.Match(
                        ticketId,
                        agentId,
                        addressNumber,
                        interactionType,
                        interactionDate,
                        reports);

                    results.Add(result);
                }
            }

            _logger?.LogInformation(
                "Validation run finished with {ResultCount} result entries.",
                results.Count);
            if (results.Count == 0)
                _logger?.LogWarning("Validation completed without any interactions to report.");

            return results;
        }

        private static IEnumerable<JsonElement> GetReportObjects(
            JsonElement root)
        {
            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                    yield return item.Clone();

                yield break;
            }

            if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var item in property.Value.EnumerateArray())
                            yield return item.Clone();

                        yield break;
                    }
                }

                throw new JsonException(
                    "Proffix report response object did not contain an array.");
            }

            throw new JsonException(
                "Proffix report response must be an array or an object containing an array.");
        }

        private static string GetCustomField(
            JsonElement ticket, params string[] names)
        {
            if (!ticket.TryGetProperty(
                "custom_fields", out var fields))
                return "";

            return GetString(fields, names);
        }

        private static string GetString(
            JsonElement element, params string[] names)
        {
            foreach (string name in names)
            {
                if (element.ValueKind != JsonValueKind.Object)
                    continue;

                if (element.TryGetProperty(name, out var value))
                {
                    if (value.ValueKind == JsonValueKind.String)
                        return value.GetString() ?? "";

                    if (value.ValueKind == JsonValueKind.Number)
                        return value.ToString();
                }
            }

            return "";
        }

        private static int GetInt(
            JsonElement element, string name)
        {
            string value = GetString(element, name);

            return int.TryParse(value, out int result)
                ? result
                : 0;
        }

        private static bool GetBool(
            JsonElement element, string name)
        {
            return element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(name, out var value) &&
                value.ValueKind == JsonValueKind.True;
        }

        private static DateTime? GetDate(
            JsonElement element, params string[] names)
        {
            string value = GetString(element, names);

            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out var date)
                ? date
                : null;
        }
    }
}
