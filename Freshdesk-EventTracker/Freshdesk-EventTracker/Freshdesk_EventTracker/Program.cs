using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Freshdesk_EventTracker.BusinessLogic.Services;
using Freshdesk_EventTracker.Shared.Services;


namespace Freshdesk_EventTracker
{
    public class Program
    {
        private static Microsoft.Extensions.Logging.ILogger? _logger;

        internal static async Task Main(string[] args)
        {
            var builder = Host.CreateDefaultBuilder(args);

            //ILogger logger;
            //IApp app = host.Services.GetRequiredService<IApp>();

            // Configuration erweitern, z.B. JSON-Datei
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            });

            // Host bauen, um Config zu bekommen
            using IHost tempHost = builder.Build();

            IConfiguration configuration = tempHost.Services.GetRequiredService<IConfiguration>();

            // -----------------------------
            // 2) Early Logger (vor DI)
            // -----------------------------
            SetupEarlyLogger(configuration);

            try
            {
                _logger?.LogInformation("Console App wird gestartet...");

                // --------------------------------------------------
                // 3) Host komplett neu aufbauen – jetzt MIT Logging
                // --------------------------------------------------
                var finalHost = CreateHostWithServices(args, configuration);

                var connectionString = configuration.GetConnectionString("SqlServerDbConnection");

                // -----------------------------
                // 4) App-Service starten
                // -----------------------------
                var app = finalHost.Services.GetRequiredService<IApp>();

                // Führt zu einem Fehlerfall
                await app.RunAppAsync();

                _logger?.LogInformation("Console App wurde beendet.");
            }
            catch (Exception ex)
            {
                if (_logger != null)
                { _logger.LogError(ex, "Während dem Aufstarten der App ist ein unerwarteter Fehler aufgetreten."); }

                throw;
            }
            finally
            {
            }
        }
        private static IHost CreateHostWithServices(string[] args, IConfiguration configuration)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                    logging.AddNLog(new NLogLoggingConfiguration(configuration.GetRequiredSection("NLog")));
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddTransient<IApp, App>();
                })
                .Build();
        }


        /// <summary>
        /// Erstellt einen LoggerFactory-basierten Early-Logger
        /// </summary>
        private static void SetupEarlyLogger(IConfiguration configuration)
        {
            var factory = LoggerFactory.Create(logging =>
            {
                logging.ClearProviders();
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.AddNLog(new NLogLoggingConfiguration(configuration.GetRequiredSection("NLog")));
            });

            _logger = factory.CreateLogger("EarlyLogger");
        }
    }


    /*
        Add-Migration
        - Default project: wählen "ConsoleSolution1.EntityFramework"
        - Ausführen: Add-Migration Initial-Checkin -Verbose
        - Update-Database -Project ConsoleSolution1.EntityFramework -StartupProject ConsoleSolution1 -Verbose
     */
}