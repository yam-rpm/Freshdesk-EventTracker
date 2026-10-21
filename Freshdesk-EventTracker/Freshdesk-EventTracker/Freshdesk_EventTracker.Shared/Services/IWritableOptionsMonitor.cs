using Microsoft.Extensions.Options;

namespace Freshdesk_EventTracker.Shared.Services
{
    /// <summary>
    /// Extension of <see cref="IOptionsMonitor{T}"/> that adds the ability to update configuration values at runtime.
    /// </summary>
    /// <typeparam name="T">The type of the configuration being monitored and updated.</typeparam>
    public interface IWritableOptionsMonitor<T> : IOptionsMonitor<T> where T : class
    {
        /// <summary>
        /// Updates the stored configuration values by applying the specified changes.
        /// </summary>
        /// <param name="applyChanges">An action that applies modifications to the current instance of <typeparamref name="T"/>.</param>
        void Update(Action<T> applyChanges);
    }
}