namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public static class ValidationResultTags
    {
        public const string Match = "OK";
        public const string NoMatch = "Keine Treffer";
        public const string CustomerOnly = "Treffer nur Kunde";
        public const string AgentOnly = "Treffer nur Mitarbeiter";
        public const string TicketOnly = "Treffer nur Ticket";
        public const string Ambiguous = "Unklar";
        public const string MultipleMatches = "Mehrere Treffer";
    }
}
