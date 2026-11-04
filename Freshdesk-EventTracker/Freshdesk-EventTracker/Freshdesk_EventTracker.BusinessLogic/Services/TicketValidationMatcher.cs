using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public static class TicketValidationMatcher
    {
        public static ValidationResult Match(
            int ticketId,
            int agentId,
            string addressNumber,
            string interactionType,
            DateTime? interactionDate,
            IEnumerable<JsonElement> reports)
        {
            ArgumentNullException.ThrowIfNull(reports);

            var reportList = reports.ToList();
            var ticketReports = reportList
                .Where(report => MentionsTicket(report, ticketId))
                .ToList();
            var agentReports = reportList
                .Where(report => ReportAgentMatches(report, agentId))
                .ToList();
            var customerReports = reportList
                .Where(report => CustomerMatches(report, addressNumber))
                .ToList();
            var matchingReports = ticketReports
                .Where(report => ReportAgentMatches(report, agentId))
                .Where(report => CustomerMatches(report, addressNumber))
                .Where(report => DateMatches(report, interactionDate))
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
                result.ResultTag = ValidationResultTags.Match;
                result.ResultText =
                    "Ticket, Mitarbeiter, Kunde und Datum stimmen mit " +
                    "einer Proffix-Leistung überein.";
            }
            else if (matchingReports.Count > 1)
            {
                result.ResultTag = ValidationResultTags.MultipleMatches;
                result.ResultText =
                    "Mehrere Proffix-Leistungen stimmen mit allen Kriterien überein.";
            }
            else if (ticketReports.Count == 0 &&
                     agentReports.Count == 0 &&
                     customerReports.Count == 0)
            {
                result.ResultTag = ValidationResultTags.NoMatch;
                result.ResultText =
                    "Weder Ticketnummer, Mitarbeiter noch Kunde wurden in den Proffix-Leistungen gefunden.";
            }
            else if (ticketReports.Count == 0 &&
                     agentReports.Count == 0)
            {
                result.ResultTag = ValidationResultTags.CustomerOnly;
                result.ResultText =
                    "Kunde gefunden, aber keine passende Ticketnummer oder Mitarbeiterzuordnung.";
            }
            else if (ticketReports.Count == 0 &&
                     customerReports.Count == 0)
            {
                result.ResultTag = ValidationResultTags.AgentOnly;
                result.ResultText =
                    "Mitarbeiter gefunden, aber keine passende Ticketnummer oder Kundenzuordnung.";
            }
            else if (agentReports.Count == 0 &&
                     customerReports.Count == 0)
            {
                result.ResultTag = ValidationResultTags.TicketOnly;
                result.ResultText =
                    "Ticketnummer gefunden, aber keine passende Mitarbeiter- oder Kundenzuordnung.";
            }
            else
            {
                result.ResultTag = ValidationResultTags.Ambiguous;
                result.ResultText = DescribePartialMatch(
                    ticketReports.Count > 0,
                    agentReports.Count > 0,
                    customerReports.Count > 0,
                    interactionDate.HasValue,
                    ticketReports.Any(report =>
                        ReportAgentMatches(report, agentId) &&
                        CustomerMatches(report, addressNumber)));
            }

            return result;
        }

        private static string DescribePartialMatch(
            bool ticketFound,
            bool agentFound,
            bool customerFound,
            bool interactionDateAvailable,
            bool ticketAgentAndCustomerFound)
        {
            var matchedCriteria = new List<string>();
            if (ticketFound)
                matchedCriteria.Add("Ticketnummer");
            if (agentFound)
                matchedCriteria.Add("Mitarbeiter");
            if (customerFound)
                matchedCriteria.Add("Kunde");

            string description =
                $"Teilweise Übereinstimmung: {string.Join(", ", matchedCriteria)} gefunden.";

            if (!ticketAgentAndCustomerFound)
                return description + " Die Kriterien stimmen nicht beim selben Rapport überein.";

            if (!interactionDateAvailable)
                return description + " Das Interaktionsdatum fehlt.";

            return description + " Das Datum stimmt nicht überein oder die Zuordnung ist mehrdeutig.";
        }

        private static bool MentionsTicket(JsonElement report, int ticketId)
        {
            string ticket = ticketId.ToString(CultureInfo.InvariantCulture);
            string ticketField = GetString(report, "Z_Ticket", "Z_Ticket.Value");
            string remarks = GetString(report, "Bemerkungen", "Bemerkung");

            return ticketField == ticket ||
                (!string.IsNullOrWhiteSpace(remarks) &&
                 Regex.IsMatch(
                     remarks,
                     $@"(?<!\d){Regex.Escape(ticket)}(?!\d)"));
        }

        private static bool ReportAgentMatches(JsonElement report, int agentId)
        {
            string value = GetString(
                report, "Z_FreshdeskID", "FreshdeskAgentId");

            return int.TryParse(value, out int mappedAgentId) &&
                mappedAgentId == agentId;
        }

        private static bool CustomerMatches(JsonElement report, string addressNumber)
        {
            if (string.IsNullOrWhiteSpace(addressNumber))
                return false;

            string reportAddress = GetString(
                report, "AdressNrPX", "AdressNr", "Adressnummer");

            return !string.IsNullOrWhiteSpace(reportAddress) &&
                string.Equals(reportAddress, addressNumber, StringComparison.Ordinal);
        }

        private static bool DateMatches(
            JsonElement report,
            DateTime? interactionDate)
        {
            if (!interactionDate.HasValue)
                return false;

            DateTime? reportDate = GetDate(
                report, "Datum", "Date", "Rapportdatum");

            return reportDate.HasValue &&
                reportDate.Value.Date == interactionDate.Value.Date;
        }

        private static string GetString(JsonElement element, params string[] names)
        {
            foreach (string name in names)
            {
                if (element.ValueKind != JsonValueKind.Object ||
                    !element.TryGetProperty(name, out JsonElement value))
                {
                    continue;
                }

                if (value.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                    return value.ToString();

                if (value.ValueKind == JsonValueKind.Object &&
                    value.TryGetProperty("Value", out JsonElement nestedValue) &&
                    nestedValue.ValueKind is JsonValueKind.String or JsonValueKind.Number)
                {
                    return nestedValue.ToString();
                }
            }

            return "";
        }

        private static DateTime? GetDate(JsonElement element, params string[] names)
        {
            string value = GetString(element, names);
            return DateTime.TryParse(
                value,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeLocal,
                out DateTime date)
                ? date
                : null;
        }
    }
}
