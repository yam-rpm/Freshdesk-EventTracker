using Freshdesk_EventTracker.BusinessLogic;
using Freshdesk_EventTracker.BusinessLogic.Services;
using Freshdesk_EventTracker.Shared.Options;
using Freshdesk_EventTracker.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using System.Net.Http.Headers;
using System.Text;
using static Freshdesk_EventTracker.Services.ConfigurationReloadService;

namespace Freshdesk_EventTracker
{
    public class Program
    {
        private static Microsoft.Extensions.Logging.ILogger? _logger;

        internal static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile(
                    "appsettings.json",
                    optional: false,
                    reloadOnChange: true);
            });

            using IHost tempHost = builder.Build();

            IConfiguration configuration =
                tempHost.Services.GetRequiredService<IConfiguration>();

            SetupEarlyLogger(configuration);

            try
            {
                _logger?.LogInformation("Console App wird gestartet...");

                using var finalHost =
                    CreateHostWithServices(args, configuration);

                var app = finalHost.Services.GetRequiredService<IApp>();

                await app.RunAppAsync(configuration["Freshdesk:ApiKey"], configuration["Freshdesk:BaseUrl"], configuration["Proffix:BaseUrl"],"YAM", "d353fb7a5f5b83cb82ebc17e22bdecd8f431ec38eef6435070586b89f4edf8c9", "rpminformatikGmbH");
                

                _logger?.LogInformation("Console App wurde beendet.");
            }
            catch (Exception ex)
            {
                _logger?.LogError(
                    ex,
                    "Während dem Aufstarten der App ist ein unerwarteter Fehler aufgetreten.");

                throw;
            }
        }

        private static IHost CreateHostWithServices(
            string[] args,
            IConfiguration configuration)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    config.AddConfiguration(configuration);
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);

                    logging.AddNLog(
                        new NLogLoggingConfiguration(
                            configuration.GetRequiredSection("NLog")));
                })
                .ConfigureServices((context, services) =>
                {
                    // Freshdesk configuration
                    services.Configure<FreshdeskOptions>(
                        configuration.GetSection("Freshdesk"));

                    // Freshdesk HTTP client
                    services.AddHttpClient<IApiClient, ApiClient>(
                        (serviceProvider, client) =>
                        {
                            var options = serviceProvider
                                .GetRequiredService<
                                    Microsoft.Extensions.Options.IOptions<FreshdeskOptions>>()
                                .Value;

                            client.BaseAddress = new Uri(
                                options.BaseUrl.TrimEnd('/') + "/");

                            var credentials = Convert.ToBase64String(
                                Encoding.UTF8.GetBytes(
                                    $"{options.ApiKey}:X"));

                            client.DefaultRequestHeaders.Authorization =
                                new AuthenticationHeaderValue(
                                    "Basic", credentials);

                            client.DefaultRequestHeaders.Accept.Add(
                                new MediaTypeWithQualityHeaderValue(
                                    "application/json"));
                        });

                    // Application services
                    services.AddTransient<FreshdeskService>();
                    services.AddTransient<IApp, App>();
                })
                .Build();
        }

        private static void SetupEarlyLogger(
            IConfiguration configuration)
        {
            var factory = LoggerFactory.Create(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);

                logging.AddNLog(
                    new NLogLoggingConfiguration(
                        configuration.GetRequiredSection("NLog")));
            });

            _logger = factory.CreateLogger("EarlyLogger");
        }
    }
}