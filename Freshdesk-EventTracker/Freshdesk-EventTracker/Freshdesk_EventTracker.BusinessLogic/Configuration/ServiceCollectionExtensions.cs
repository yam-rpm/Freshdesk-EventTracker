using Freshdesk_EventTracker.BusinessLogic.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Freshdesk_EventTracker.BusinessLogic.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBusinessLogic(this IServiceCollection services, IConfiguration configuration, string mandantName, int mandantIndex, IServiceCollection serviceCollectionHolder)
        {
            

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

            return services;
        }
    }
}
