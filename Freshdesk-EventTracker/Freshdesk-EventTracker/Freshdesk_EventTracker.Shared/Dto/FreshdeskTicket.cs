using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Freshdesk_Ticket_Viewer.Shared.Dto
{
    public class Ticket
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("subject")]
        public string Subject { get; set; } = string.Empty;

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("status")]
        public int Status { get; set; }

        // Freshdesk documentation calls this field "email".
        // It is the requester's email address.
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        // Keep this only if your actual API response contains
        // "support_email".
        [JsonPropertyName("support_email")]
        public string SupportEmail { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("phone")]
        public string Phone { get; set; } = string.Empty;

        [JsonPropertyName("to_emails")]
        public List<string> ToEmails { get; set; } = new();

        [JsonPropertyName("cc_emails")]
        public List<string> CcEmails { get; set; } = new();

        [JsonPropertyName("fwd_emails")]
        public List<string> FwdEmails { get; set; } = new();

        [JsonPropertyName("reply_cc_emails")]
        public List<string> ReplyCcEmails { get; set; } = new();

        // These are not in the standard ticket fields you posted,
        // but can remain if your API returns them.
        [JsonPropertyName("ticket_cc_emails")]
        public List<string> TicketCcEmails { get; set; } = new();

        [JsonPropertyName("ticket_bcc_emails")]
        public List<string> TicketBccEmails { get; set; } = new();

        [JsonPropertyName("fr_escalated")]
        public bool FrEscalated { get; set; }

        [JsonPropertyName("spam")]
        public bool Spam { get; set; }

        [JsonPropertyName("deleted")]
        public bool Deleted { get; set; }

        [JsonPropertyName("is_escalated")]
        public bool IsEscalated { get; set; }

        [JsonPropertyName("email_config_id")]
        public long? EmailConfigId { get; set; }

        [JsonPropertyName("group_id")]
        public long? GroupId { get; set; }

        [JsonPropertyName("requester_id")]
        public long? RequesterId { get; set; }

        [JsonPropertyName("responder_id")]
        public long? ResponderId { get; set; }

        [JsonPropertyName("company_id")]
        public long? CompanyId { get; set; }

        [JsonPropertyName("product_id")]
        public long? ProductId { get; set; }

        [JsonPropertyName("source")]
        public int Source { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("description_text")]
        public string DescriptionText { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("due_by")]
        public DateTime? DueBy { get; set; }

        [JsonPropertyName("fr_due_by")]
        public DateTime? FrDueBy { get; set; }

        [JsonPropertyName("tags")]
        public List<string> Tags { get; set; } = new();

        [JsonPropertyName("attachments")]
        public List<Attachment> Attachments { get; set; } = new();

        [JsonPropertyName("source_info")]
        public object? SourceInfo { get; set; }

        // Keep these if they are present in your actual Freshdesk response.
        [JsonPropertyName("source_additional_info")]
        public object? SourceAdditionalInfo { get; set; }

        [JsonPropertyName("structured_description")]
        public object? StructuredDescription { get; set; }

        [JsonPropertyName("association_type")]
        public string? AssociationType { get; set; }

        [JsonPropertyName("custom_fields")]
        public CustomFields CustomFields { get; set; } = new();
    }


    public class CustomFields
    {
        [JsonPropertyName("cf_serviceauftrag")]
        public string? Serviceauftrag { get; set; }

        [JsonPropertyName("cf_auftragpx")]
        public string? Auftragpx { get; set; }

        [JsonPropertyName("cf_geschlossenam")]
        public string? Geschlossenam { get; set; }

        [JsonPropertyName("cf_geschlossenamtext")]
        public string? Geschlossenamtext { get; set; }
    }


    public class Attachment
    {
        [JsonPropertyName("id")]
        public long? Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("content_type")]
        public string? ContentType { get; set; }

        [JsonPropertyName("size")]
        public long? Size { get; set; }

        [JsonPropertyName("attachment_url")]
        public string? AttachmentUrl { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }
}
