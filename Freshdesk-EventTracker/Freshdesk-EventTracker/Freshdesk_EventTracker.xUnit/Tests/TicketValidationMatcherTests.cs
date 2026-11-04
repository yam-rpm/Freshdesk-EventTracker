using System.Text.Json;
using Freshdesk_EventTracker.BusinessLogic.Services;

namespace Freshdesk_EventTracker.xUnit.Tests;

public class TicketValidationMatcherTests
{
    private static readonly DateTime InteractionDate =
        new(2026, 11, 4, 10, 30, 0, DateTimeKind.Local);

    [Fact]
    public void Match_ReturnsNoMatch_WhenNoCriteriaAreFound()
    {
        var result = Match(Array.Empty<string>());

        Assert.Equal("Keine Treffer", result.ResultTag);
        Assert.Equal(0, result.MatchingReports);
    }

    [Fact]
    public void Match_ReturnsCustomerOnly_WhenOnlyCustomerMatches()
    {
        var result = Match(new[]
        {
            Report(address: "A-42")
        });

        Assert.Equal("Treffer nur Kunde", result.ResultTag);
    }

    [Fact]
    public void Match_ReturnsAgentOnly_WhenOnlyAgentMatches()
    {
        var result = Match(new[]
        {
            Report(agent: 17)
        });

        Assert.Equal("Treffer nur Mitarbeiter", result.ResultTag);
    }

    [Fact]
    public void Match_ReturnsTicketOnly_WhenOnlyTicketMatches()
    {
        var result = Match(new[]
        {
            Report(ticket: true)
        });

        Assert.Equal("Treffer nur Ticket", result.ResultTag);
    }

    [Fact]
    public void Match_ReturnsUnclear_WhenCriteriaExistOnDifferentReports()
    {
        var result = Match(new[]
        {
            Report(ticket: true),
            Report(agent: 17),
            Report(address: "A-42")
        });

        Assert.Equal("Unklar", result.ResultTag);
        Assert.Contains("nicht beim selben Rapport", result.ResultText);
    }

    [Fact]
    public void Match_ReturnsUnclear_WhenMatchingDateIsDifferent()
    {
        var result = Match(new[]
        {
            Report(
                ticket: true,
                agent: 17,
                address: "A-42",
                date: "2026-11-03")
        });

        Assert.Equal("Unklar", result.ResultTag);
        Assert.Contains("Datum stimmt nicht überein", result.ResultText);
    }

    [Fact]
    public void Match_ReturnsOk_WhenExactlyOneReportMatchesAllCriteria()
    {
        var result = Match(new[]
        {
            Report(
                ticket: true,
                agent: 17,
                address: "A-42",
                date: "2026-11-04")
        });

        Assert.Equal("OK", result.ResultTag);
        Assert.Equal(1, result.MatchingReports);
    }

    [Fact]
    public void Match_ReturnsMultipleMatches_WhenSeveralReportsMatchAllCriteria()
    {
        var result = Match(new[]
        {
            Report(
                ticket: true,
                agent: 17,
                address: "A-42",
                date: "2026-11-04"),
            Report(
                ticket: true,
                agent: 17,
                address: "A-42",
                date: "2026-11-04")
        });

        Assert.Equal("Mehrere Treffer", result.ResultTag);
        Assert.Equal(2, result.MatchingReports);
    }

    [Fact]
    public void Match_RecognizesTicketNumberInRemarksWithoutMatchingPartialNumbers()
    {
        var result = Match(new[]
        {
            """
            {
              "Bemerkungen": "Arbeit zu ticket 12345 erledigt",
              "Z_FreshdeskID": 17,
              "AdressNrPX": "A-42",
              "Datum": "2026-11-04"
            }
            """
        });

        Assert.Equal("OK", result.ResultTag);
        Assert.Equal(1, result.MatchingReports);
    }

    private static ValidationResult Match(IEnumerable<string> reportJson)
    {
        var reports = reportJson.Select(json =>
        {
            using JsonDocument document = JsonDocument.Parse(json);
            return document.RootElement.Clone();
        });

        return TicketValidationMatcher.Match(
            ticketId: 12345,
            agentId: 17,
            addressNumber: "A-42",
            interactionType: "Antwort",
            interactionDate: InteractionDate,
            reports);
    }

    private static string Report(
        bool ticket = false,
        int? agent = null,
        string? address = null,
        string? date = null)
    {
        var properties = new List<string>();
        if (ticket)
            properties.Add("\"Z_Ticket\": \"12345\"");
        if (agent.HasValue)
            properties.Add($"\"Z_FreshdeskID\": {agent.Value}");
        if (address is not null)
            properties.Add($"\"AdressNrPX\": \"{address}\"");
        if (date is not null)
            properties.Add($"\"Datum\": \"{date}\"");

        return $"{{{string.Join(", ", properties)}}}";
    }
}
