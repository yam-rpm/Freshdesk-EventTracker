
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public class ProffixService : IDisposable
    {
        private static readonly HttpClient client = new HttpClient();

        private readonly string BaseUrl;
        private string PxSessionId;

        public ProffixService(string baseUrl)
        {
            BaseUrl = baseUrl.TrimEnd('/');
        }

        public async Task LoginAsync(string Benutzer, string Passwort, string Datenbank, string[] Module)
        {
            string url = $"{BaseUrl}/pxapi/v4/PRO/Login";

            var loginData = new
            {
                Benutzer = Benutzer,
                Passwort = Passwort,
                Datenbank = new
                {
                    Name = Datenbank
                },
                Module = Module
            };

            string json = JsonSerializer.Serialize(loginData);

            using var request = new HttpRequestMessage(
                HttpMethod.Post, url);

            request.Content = new StringContent(
                json, Encoding.UTF8, "application/json");

            request.Headers.Accept.Add(
                new MediaTypeWithQualityHeaderValue("*/*"));

            using HttpResponseMessage response =
                await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
            Console.WriteLine(response.StatusCode.ToString());

            if (response.Headers.TryGetValues(
                "PxSessionId", out var sessionIds))
            {
                PxSessionId = System.Linq.Enumerable.First(sessionIds);
            }
            else
            {
                throw new InvalidOperationException(
                    "Login succeeded, but no PxSessionId was returned.");
            }
        }

        public async Task<string> GetAllRapporte(
            int offset = 0,
            int limit = 100,
            string verrechnet = "ANY",
            string gesperrt = "ANY")
        {
            if (string.IsNullOrWhiteSpace(PxSessionId))
            {
                throw new InvalidOperationException(
                    "Not logged in. Call LoginAsync() first.");
            }

            string url =
                $"{BaseUrl}/pxapi/v4/STU/Rapport" +
                $"?verrechnet={Uri.EscapeDataString(verrechnet)}" +
                $"&gesperrt={Uri.EscapeDataString(gesperrt)}" +
                $"&Offset={offset}" +
                $"&Limit={limit}";

            using var request = new HttpRequestMessage(
                HttpMethod.Get, url);

            request.Headers.Add("PxSessionId", PxSessionId);

            using HttpResponseMessage response =
                await client.SendAsync(request);

            // Proffix may return a renewed session ID.
            if (response.Headers.TryGetValues(
                "PxSessionId", out var sessionIds))
            {
                PxSessionId =
                    System.Linq.Enumerable.First(sessionIds);
            }

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task LogoutAsync()
        {
            if (string.IsNullOrWhiteSpace(PxSessionId))
                return;

            string url = $"{BaseUrl}/pxapi/v4/PRO/Login";

            using var request = new HttpRequestMessage(
                HttpMethod.Delete, url);

            request.Headers.Add("PxSessionId", PxSessionId);

            using HttpResponseMessage response =
                await client.SendAsync(request);

            response.EnsureSuccessStatusCode();

            PxSessionId = null;
        }

        public void Dispose()
        {
            // Call LogoutAsync() explicitly before disposing.
        }
    }
}
