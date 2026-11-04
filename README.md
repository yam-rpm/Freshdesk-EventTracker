# Freshdesk Event Tracker

Windows console application for comparing Freshdesk ticket conversations with
Proffix reports. The validation rules and the Proffix field assumptions are
documented in [VALIDATION-RULES.md](Freshdesk-EventTracker/VALIDATION-RULES.md).

## Configuration

Copy `Freshdesk-EventTracker/Freshdesk_EventTracker/appsettings.example.json`
to `Freshdesk-EventTracker/Freshdesk_EventTracker/appsettings.json` and fill
in the Freshdesk and Proffix settings. `appsettings.json` is intentionally
ignored by Git. Never commit credentials or tokens.

The Freshdesk `BaseUrl` includes `/api` (for example,
`https://your-domain.freshdesk.com/api`). Alternatively, provide settings as
environment variables, using double underscores for nested keys:

- `Freshdesk__ApiKey`
- `Freshdesk__BaseUrl`
- `Proffix__BaseUrl`
- `Proffix__User`
- `Proffix__Password`
- `Proffix__Database`
- `Proffix__WriteValidationResults` (`false` unless explicitly enabled)

The sample config also enables console logging through NLog.

## Build and tests

From the repository root:

```powershell
dotnet build Freshdesk-EventTracker/Freshdesk-EventTracker.slnx
dotnet test Freshdesk-EventTracker/Freshdesk_EventTracker.xUnit/Freshdesk_EventTracker.xUnit.csproj
```

Tests use in-memory HTTP handlers and do not call Freshdesk or Proffix.

## Proffix writes

Writes are disabled by default. The optional writer targets only the fixed
`ZUS/TicketLeistVali` route, but the live table's field names, JSON schema, and
duplicate-handling behavior have not been verified. Do not enable writes until
the Proffix owner confirms the schema and approves a controlled test.

Task Scheduler automation and a production handoff/package are not configured
by this repository yet.
