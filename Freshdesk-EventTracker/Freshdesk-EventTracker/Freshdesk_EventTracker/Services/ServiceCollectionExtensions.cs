using Freshdesk_EventTracker.BusinessLogic.Configuration;
using Freshdesk_EventTracker.BusinessLogic.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Freshdesk_EventTracker.Services
{
    /// <summary>
    ///     Extension methods for <see cref="IServiceCollection"/> to add tenant-specific configuration and related services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddMandantenConfiguration(this IServiceCollection services, IConfiguration configuration, IServiceCollection serviceCollectionHolder)
        {
            services.AddHttpClient<IApiClient, ApiClient>(client =>
            {
                client.BaseAddress = new Uri("");

            });

            /*
                // Zweck: DI für Konfigurationsobjekte um Options-Klassen mit Daten aus appsettings.json oder anderen Quellen zu befüllen
                services.AddOptions<TestService>();

                // Kurzlebig / pro Nutzung
                // Jede Abfrage durch DI bekommt eine neue Instanz. Ideal für leichtgewichtige, zustandslose Services.
                services.AddTransient<ITestService, TestService>();

                // Pro Scope (z. B. pro Request)
                // Ideal für Services, die zustandsbehaftet innerhalb eines Requests sein sollen.
                services.AddScoped<ITestService, TestService>();

                // Einmalig / global
                // Ideal für globale, zustandslose oder thread-safe Services.
                services.AddSingleton<ITestService, TestService>();
            */

            try
            {
                // ServiceProvider temporär erstellen, um den Dienst sofort aufzurufen
                using var serviceProvider = services.BuildServiceProvider();
                var configReloadService = serviceProvider.GetRequiredService<IConfigurationReloadService>();
                configReloadService.ReloadConfigurationNotAsync(services);
                configReloadService.UpdateConfigurationOnChange(services);

                services.AddBusinessLogic(configuration, configReloadService._MandantName, (Convert.ToInt32(configReloadService._MandantKey)), serviceCollectionHolder);
            }
            catch { }

            return services;
        }
    }
}