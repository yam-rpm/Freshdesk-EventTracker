using Freshdesk_EventTracker.BusinessLogic.Services;
using Freshdesk_Ticket_Viewer.Shared.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public class App : IApp
    {
        private readonly ILogger<App> _logger;

        public App(ILogger<App> logger)
        {
            _logger = logger;
        }

        public async Task RunAppAsync(
            string apiKey,
            string freshdeskUrl,
            string proffixUrl,
            string proffixUser,
            string proffixPassword,
            string database,
            string? customTableRoute = null,
            bool writeValidationResults = false)
        {
            var freshdesk = new FreshdeskService(
                apiKey,
                freshdeskUrl,
                _logger);

            var proffix = new ProffixService(
                proffixUrl,
                _logger);

            try
            {
                _logger.LogInformation("Proffix Login...");
                Console.WriteLine("Proffix Login...");

                await proffix.LoginAsync(
                    proffixUser,
                    proffixPassword,
                    database,
                    new[] { "VOL" });

                _logger.LogInformation("Login successful.");
                Console.WriteLine("Login successful.");
                Console.WriteLine("Starting validation...");

                var validator = new TicketValidationService(
                    freshdesk,
                    proffix,
                    _logger);

                var results = await validator.ValidateAsync();

                foreach (var result in results)
                {
                    var message =
                        $"Ticket: {result.TicketId} | " +
                        $"Agent: {result.AgentId} | " +
                        $"Type: {result.InteractionType} | " +
                        $"Result: {result.ResultTag} | " +
                        $"{result.ResultText}";

                    _logger.LogInformation(message);
                    Console.WriteLine(message);
                }

                Console.WriteLine();
                Console.WriteLine($"Interactions checked: {results.Count}");
                Console.WriteLine(
                    $"OK: {results.Count(r => r.ResultTag == ValidationResultTags.Match)}");
                Console.WriteLine(
                    $"No match: {results.Count(r => r.ResultTag == ValidationResultTags.NoMatch)}");
                Console.WriteLine(
                    $"Partial or unclear: {results.Count(r =>
                        r.ResultTag != ValidationResultTags.Match &&
                        r.ResultTag != ValidationResultTags.NoMatch)}");

                if (writeValidationResults)
                {
                    var targetRoute = customTableRoute ?? "/pxapi/v4/ZUS/TicketLeistVali";
                    _logger.LogInformation(
                        "Writing {Count} validation results to Proffix custom table '{Route}'.",
                        results.Count,
                        targetRoute);

                    await proffix.WriteValidationResultsAsync(
                        results,
                        targetRoute);
                }
                else
                {
                    _logger.LogInformation(
                        "Proffix custom-table writing is disabled via configuration.");
                }
            }
            finally
            {
                await proffix.LogoutAsync();
                _logger.LogInformation("Proffix session closed.");
                Console.WriteLine("Proffix session closed.");
            }
        }
    }
}