using System;
using System.Collections.Generic;
using System.Net.Http.Json;
using System.Net.Http;
using System.Net.Http.Json;

namespace Freshdesk_EventTracker.BusinessLogic.Services
{
    public sealed class ApiClient(HttpClient httpClient) : IApiClient
    {
        public async Task<T?> GetAsync<T>(
            string endpoint,
            CancellationToken ct = default)
        {
            using var response = await httpClient.GetAsync(endpoint, ct);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<T>(
                cancellationToken: ct);
        }
    }
}