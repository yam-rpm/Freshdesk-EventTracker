using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Freshdesk_EventTracker.Shared.Services
{
    /// <summary>
    /// Provides a custom implementation of <see cref="IWritableOptionsMonitor{T}"/> that allows runtime updates of configuration options.
    /// </summary>
    /// <typeparam name="T">The type of the configuration options.</typeparam>
    public class CustomOptionsMonitor<T> : IWritableOptionsMonitor<T> where T : class, new()
    {
        private readonly IOptionsMonitor<T> _innerMonitor;
        private readonly ConcurrentDictionary<string, T> _values = new();
        private event Action<T, string> _onChange;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomOptionsMonitor{T}"/> class with an underlying options monitor.
        /// </summary>
        /// <param name="innerMonitor">The underlying <see cref="IOptionsMonitor{T}"/> instance.</param>
        public CustomOptionsMonitor(IOptionsMonitor<T> innerMonitor)
        {
            _innerMonitor = innerMonitor;
            _values[""] = _innerMonitor.CurrentValue;
        }

        /// <summary>
        /// Gets the current value of the options.
        /// </summary>
        public T CurrentValue => _values[""];

        /// <summary>
        /// Gets the named options value. If no custom value exists, returns the value from the underlying monitor.
        /// </summary>
        /// <param name="name">The name of the named configuration instance.</param>
        /// <returns>The options instance of type <typeparamref name="T"/>.</returns>
        public T Get(string name) => _values.TryGetValue(name, out var value) ? value : _innerMonitor.Get(name);

        /// <summary>
        /// Registers a listener to be invoked whenever the options change.
        /// </summary>
        /// <param name="listener">A callback to invoke when the options are updated.</param>
        /// <returns>An <see cref="IDisposable"/> that can be used to unsubscribe the listener.</returns>
        public IDisposable OnChange(Action<T, string> listener)
        {
            _onChange += listener;
            return _innerMonitor.OnChange(listener);
        }

        /// <summary>
        /// Applies runtime changes to the options and invokes any registered change listeners.
        /// </summary>
        /// <param name="applyChanges">A delegate that applies modifications to a new instance of the options.</param>
        public void Update(Action<T> applyChanges)
        {
            var newValue = new T();
            applyChanges(newValue);
            _values[""] = newValue;
            _onChange?.Invoke(newValue, "");
        }
    }
}