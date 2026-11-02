using System;
using System.Collections.Generic;
using System.Text;
using System;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public class TicketInteraction
    {
        public int TicketId { get; set; }
        public int AgentId { get; set; }
        public int CustomerId { get; set; }

        public string InteractionType { get; set; } = "";
        public string TicketAddressNumber { get; set; } = "";
        public DateTime? InteractionDate { get; set; }
    }

    public class ValidationResult
    {
        public int TicketId { get; set; }
        public int AgentId { get; set; }
        public string InteractionType { get; set; } = "";
        public DateTime? InteractionDate { get; set; }

        public string ResultTag { get; set; } = "";
        public string ResultText { get; set; } = "";

        public int MatchingReports { get; set; }
    }
}
