using Freshdesk_EventTracker.BusinessLogic.Services;
using Freshdesk_Ticket_Viewer.Shared.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public class FreshdeskService
    {
        private static readonly HttpClient client = new HttpClient();
        private readonly string Basestring;
        private readonly string API_KEY;
        private readonly string VERSION = "v2";
        private readonly string apiReadyString;

        public FreshdeskService(string apiKey, string basestring)
        {
            API_KEY = apiKey;
            Basestring = basestring;

            string authString = $"{API_KEY}:x";

            apiReadyString = Convert.ToBase64String(
                Encoding.ASCII.GetBytes(authString));
        }
        public async Task<List<Ticket>> GetAllTickets()
        {
            try
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", apiReadyString);

                using HttpResponseMessage response =
                    await client.GetAsync($"{Basestring}/{VERSION}/tickets");

                response.EnsureSuccessStatusCode();

                List<Ticket> responseBody =
                    await response.Content.ReadFromJsonAsync<List<Ticket>>();

                Console.WriteLine(responseBody);

                return responseBody;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine($"Message: {e.Message}");

                return new List<Ticket>();
            }
        }


        public async Task<List<JsonElement>> GetAllTicketsRawAsync()
        {
            var tickets = new List<JsonElement>();

            try
            {
                client.DefaultRequestHeaders.Authorization =
                    new AuthenticationHeaderValue("Basic", apiReadyString);

                for (int page = 1; ; page++)
                {
                    string url =
                        $"{Basestring}/{VERSION}/tickets" +
                        $"?page={page}&per_page=100";

                    using HttpResponseMessage response =
                        await client.GetAsync(url);

                    string content =
                        await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        Console.WriteLine(
                            $"Freshdesk error: {(int)response.StatusCode} " +
                            $"{response.StatusCode}");
                        Console.WriteLine(content);

                        response.EnsureSuccessStatusCode();
                    }

                    using JsonDocument document =
                        JsonDocument.Parse(content);

                    JsonElement items = document.RootElement;

                    if (items.ValueKind != JsonValueKind.Array ||
                        items.GetArrayLength() == 0)
                    {
                        break;
                    }

                    foreach (JsonElement item in items.EnumerateArray())
                    {
                        tickets.Add(item.Clone());
                    }

                    if (items.GetArrayLength() < 100)
                    {
                        break;
                    }
                }

                return tickets;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                return new List<JsonElement>();
            }
            catch (JsonException e)
            {
                Console.WriteLine($"JSON error: {e.Message}");
                return new List<JsonElement>();
            }
        }


        public async Task<List<JsonElement>> GetTicketConversationsRawAsync(
            int ticketId)
        {
            string url =
                $"{Basestring}/{VERSION}/tickets/{ticketId}/conversations";

            using var response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync());

            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return new List<JsonElement>();

            return document.RootElement
                .EnumerateArray()
                .Select(item => item.Clone())
                .ToList();
        }


    }
}