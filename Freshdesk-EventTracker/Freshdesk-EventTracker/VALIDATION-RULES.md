# Validation rules

Each Freshdesk conversation with a non-zero `user_id` is checked against the
Proffix report data. The current implementation compares the ticket number,
Freshdesk employee mapping, customer address number, and calendar date.

## Result tags

| Tag | Meaning |
| --- | --- |
| `OK` | Exactly one report matches ticket, employee, customer, and date. |
| `Mehrere Treffer` | More than one report matches all four criteria. |
| `Keine Treffer` | No report matches the ticket, employee, or customer independently. |
| `Treffer nur Kunde` | A customer match exists, but no ticket or employee match exists. |
| `Treffer nur Mitarbeiter` | An employee match exists, but no ticket or customer match exists. |
| `Treffer nur Ticket` | A ticket match exists, but no employee or customer match exists. |
| `Unklar` | Some criteria match, but they do not produce one unique full match. |

Partial-match results include a description of which criteria were found and
whether they occurred on different reports, the date is missing, or the date
does not match. Dates are compared by calendar day, not by time of day.

## Proffix field mapping requiring confirmation

The matching code currently checks `Z_Ticket` (or the ticket number in
`Bemerkungen`), `Z_FreshdeskID`/`FreshdeskAgentId`,
`AdressNrPX`/`AdressNr`/`Adressnummer`, and `Datum`/`Date`/`Rapportdatum`.
These names and the assumption that `Z_FreshdeskID` maps to the Freshdesk
conversation `user_id` have not been verified against a live Proffix schema.
Confirm the actual field names, employee mapping, date rules, and meaning of
private conversations with the Proffix owner before production use.
