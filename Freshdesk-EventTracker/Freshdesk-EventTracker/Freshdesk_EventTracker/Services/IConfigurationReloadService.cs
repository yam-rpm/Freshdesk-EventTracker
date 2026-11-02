using Microsoft.Extensions.DependencyInjection;

namespace Freshdesk_EventTracker.Services
{
    public interface IConfigurationReloadService
    {
        /// <summary>
        ///     Gets or sets the tenant key used to identify the current configuration scope.
        /// </summary>
        string _MandantKey { get; set; }

        /// <summary>
        ///     Gets or sets the tenant name associated with the current configuration.
        /// </summary>
        string _MandantName { get; set; }

        /// <summary>
        ///     Reloads the configuration synchronously based on the provided service collection and returns the updated collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection to reload configuration for.</param>
        /// <returns>The updated IServiceCollection after reloading the configuration.</returns>
        IServiceCollection ReloadConfigurationNotAsync(IServiceCollection serviceCollection);

        /// <summary>
        ///     Updates the configuration dynamically when a change is detected in the provided service collection.
        /// </summary>
        /// <param name="serviceCollection">The service collection to update on configuration change.</param>
        void UpdateConfigurationOnChange(IServiceCollection serviceCollection);
    }
}
