using System;
using System.Collections.Generic;
using System.Text;


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

        public TicketValidationService(
            FreshdeskService freshdesk,
            ProffixService proffix)
        {
            _freshdesk = freshdesk;
            _proffix = proffix;
        }

        public async Task<List<ValidationResult>> ValidateAsync()
        {
            var results = new List<ValidationResult>();

           
            // Read Freshdesk tickets and Proffix reports.
            var tickets = await _freshdesk.GetAllTicketsRawAsync();

            string rapportJson = await _proffix.GetAllRapporte(
                offset: 0,
                limit: 10000);

            using var rapportDocument =
                JsonDocument.Parse(rapportJson);

            var reports = GetReportObjects(
                rapportDocument.RootElement).ToList();

            foreach (var ticket in tickets)
            {
                int ticketId = GetInt(ticket, "id");

                if (ticketId == 0)
                    continue;

                int customerId = GetInt(ticket, "requester_id");

                string addressNumber = GetCustomField(
                    ticket, "cf_adressnrpx", "AdressNrPX");

                var conversations =
                    await _freshdesk.GetTicketConversationsRawAsync(
                        ticketId);

                foreach (var conversation in conversations)
                {
                    // Ignore conversations without an identified author.
                    int agentId = GetInt(conversation, "user_id");

                    if (agentId == 0)
                        continue;

                    // Only validate agent-created replies and notes.
                    bool isPrivate = GetBool(conversation, "private");

                    string interactionType =
                        isPrivate ? "Notiz" : "Antwort";

                    DateTime? interactionDate = GetDate(
                        conversation, "created_at");

                    // Find reports mentioning the ticket number.
                    var ticketReports = reports
                        .Where(report => MentionsTicket(
                            report, ticketId))
                        .ToList();

                    // Match the agent and customer.
                    var matchingReports = ticketReports
                        .Where(report =>
                            ReportAgentMatches(report, agentId))
                        .Where(report =>
                            CustomerMatches(report, addressNumber))
                        .Where(report =>
                            DateMatches(report, interactionDate))
                        .ToList();

                    var result = new ValidationResult
                    {
                        TicketId = ticketId,
                        AgentId = agentId,
                        InteractionType = interactionType,
                        InteractionDate = interactionDate,
                        MatchingReports = matchingReports.Count
                    };

                    if (matchingReports.Count == 1)
                    {
                        result.ResultTag = "OK";
                        result.ResultText =
                            "Ticket, Agent, Kunde und Datum stimmen " +
                            "mit einer Proffix-Leistung überein.";
                    }
                    else if (matchingReports.Count > 1)
                    {
                        result.ResultTag = "Unklar";
                        result.ResultText =
                            "Mehrere passende Proffix-Leistungen gefunden.";
                    }
                    else if (ticketReports.Count == 0)
                    {
                        result.ResultTag = "Fail";
                        result.ResultText =
                            "Keine Proffix-Leistung mit dieser Ticketnummer gefunden.";
                    }
                    else
                    {
                        result.ResultTag = "Unklar";
                        result.ResultText =
                            "Ticketnummer gefunden, aber Agent, Kunde " +
                            "oder Datum stimmt nicht eindeutig überein.";
                    }

                    results.Add(result);
                }
            }

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
            }
        }

        private static bool MentionsTicket(
            JsonElement report, int ticketId)
        {
            string ticket = ticketId.ToString();

            string ticketField = GetString(
                report, "Z_Ticket", "Z_Ticket.Value");

            string remarks = GetString(
                report, "Bemerkungen", "Bemerkung");

            return ticketField == ticket ||
                (!string.IsNullOrWhiteSpace(remarks) &&
                 System.Text.RegularExpressions.Regex.IsMatch(
                     remarks,
                     $@"(?<!\d){System.Text.RegularExpressions.Regex.Escape(ticket)}(?!\d)"));
        }

        private static bool ReportAgentMatches(
            JsonElement report, int agentId)
        {
            // Replace these with the actual Proffix employee
            // field and its Z_FreshdeskID mapping.
            string value = GetString(
                report, "Z_FreshdeskID", "FreshdeskAgentId");

            return int.TryParse(value, out int mappedAgentId)
                && mappedAgentId == agentId;
        }

        private static bool CustomerMatches(
            JsonElement report, string addressNumber)
        {
            if (string.IsNullOrWhiteSpace(addressNumber))
                return false;

            string reportAddress = GetString(
                report, "AdressNrPX", "AdressNr", "Adressnummer");

            return !string.IsNullOrWhiteSpace(reportAddress)
                && reportAddress == addressNumber;
        }

        private static bool DateMatches(
            JsonElement report, DateTime? interactionDate)
        {
            if (interactionDate == null)
                return false;

            DateTime? reportDate = GetDate(
                report, "Datum", "Date", "Rapportdatum");

            return reportDate.HasValue &&
                reportDate.Value.Date == interactionDate.Value.Date;
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
