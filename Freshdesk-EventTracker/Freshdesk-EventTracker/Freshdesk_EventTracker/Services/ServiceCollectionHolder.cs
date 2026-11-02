using Microsoft.Extensions.DependencyInjection;

namespace Freshdesk_EventTracker.Services
{
    public class ServiceCollectionHolder
    {
        /// <summary>
        ///     The underlying IServiceProvider used to resolve services.
        /// </summary>
        private IServiceProvider _serviceProvider;

        /// <summary>
        ///     The collection of service descriptors.
        /// </summary>
        public IServiceCollection _services { get; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceCollectionHolder"/> class
        ///     with the given service collection and builds the default service provider.
        /// </summary>
        /// <param name="services">The service collection to hold and manage.</param>
        public ServiceCollectionHolder(IServiceCollection services)
        {
            _services = services;
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        ///     Executes an update action on the internal service collection.
        /// </summary>
        /// <param name="updateAction">The action that modifies the service collection.</param>
        public void UpdateServices(Action<IServiceCollection> updateAction)
        {
            updateAction(_services);
        }

        /// <summary>
        ///     Gets the current <see cref="IServiceProvider"/> instance.
        /// </summary>
        /// <returns>The current service provider.</returns>
        public IServiceProvider GetServiceProvider() => _serviceProvider;

        /// <summary>
        /// Replaces the existing service provider with a new one.
        /// </summary>
        /// <param name="newServiceProvider">The new service provider to use.</param>
        public void ReplaceServiceProvider(IServiceProvider newServiceProvider)
        {
            _serviceProvider = newServiceProvider;
        }

        // Prüft, ob ein spezifischer Service (z. B. ProffixOptions) enthalten ist
        /// <summary>
        ///     Checks if a specific service type is registered and available from the service provider.
        /// </summary>
        /// <typeparam name="T">The type of the service to check for.</typeparam>
        /// <returns>True if the service is registered and can be resolved; otherwise, false.</returns>

        public bool ContainsService<T>()
        {
            return _serviceProvider.GetService<T>() != null;
        }
    }
}
