using Freshdesk_EventTracker.BusinessLogic.Services;
using Freshdesk_Ticket_Viewer.Shared.Dto;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{

    //    public class App : IApp
    //    {
    //        public async Task RunAppAsync(string API_KEY)
    //        {
    //            var freshdeskService = new FreshdeskService(
    //                    $"{API_KEY}",
    //                    "https://rpmit.freshdesk.com/api"
    //                );


    //            List<Ticket> tickets = await freshdeskService.GetAllTickets();

    //            foreach (var ticket in tickets)
    //            {
    //                Console.WriteLine(ticket);
    //            }

    //            var Proffix = new ProffixService("https://remote.rpm-it.ch:9443");

    //            var user = "";
    //            var password = "";
    //            var database = "";

    //            string[] modules = { "ZEI", "STU", "ADR" };

    //            await Proffix.LoginAsync(user, password, database, modules);
    //        }

    //    }

    public class App : IApp
    {
        public async Task RunAppAsync(
            string apiKey,
            string freshdeskUrl,
            string proffixUrl,
            string proffixUser,
            string proffixPassword,
            string database)
        {
            var freshdesk = new FreshdeskService(
                apiKey,
                freshdeskUrl);

            var proffix = new ProffixService(proffixUrl);

            try
            {
                Console.WriteLine("Proffix Login...");

                await proffix.LoginAsync(
                    proffixUser,
                    proffixPassword,
                    database,
                    new[] { "VOL" });

                Console.WriteLine("Login successful.");
                Console.WriteLine("Starting read-only validation...");

                var validator = new TicketValidationService(
                    freshdesk,
                    proffix);

                var results = await validator.ValidateAsync();

                foreach (var result in results)
                {
                    Console.WriteLine(
                        $"Ticket: {result.TicketId} | " +
                        $"Agent: {result.AgentId} | " +
                        $"Type: {result.InteractionType} | " +
                        $"Result: {result.ResultTag} | " +
                        $"{result.ResultText}");
                }

                Console.WriteLine();
                Console.WriteLine($"Interactions checked: {results.Count}");
                Console.WriteLine(
                    $"OK: {results.Count(r => r.ResultTag == "OK")}");
                Console.WriteLine(
                    $"Fail: {results.Count(r => r.ResultTag == "Fail")}");
                Console.WriteLine(
                    $"Unclear: {results.Count(r => r.ResultTag == "Unklar")}");

                // No Proffix write operation is performed.
            }
            finally
            {
                await proffix.LogoutAsync();
                Console.WriteLine("Proffix session closed.");
            }
        }
    }
}