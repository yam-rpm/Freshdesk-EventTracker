using Freshdesk_EventTracker.Shared.Options;
using Freshdesk_EventTracker.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections;

namespace Freshdesk_EventTracker.Services
{
    public class ConfigurationReloadService : IConfigurationReloadService
    {
        private readonly IConfiguration _Configuration;
        private readonly IServiceProvider _ServiceProvider;
        private ServiceCollectionHolder _ServiceCollectionHolder;

        private readonly Dictionary<Type, object> _optionsMonitors;

        public string _MandantKey { get; set; }
        public string _MandantName { get; set; }

        /// <summary>
        ///     Initializes a new instance of the <see cref="ConfigurationReloadService"/> class,
        ///     which manages runtime configuration reloading for multiple option types using
        ///     <see cref="IWritableOptionsMonitor{T}"/> instances.
        /// </summary>
        /// <param name="configurationRoot">The root configuration (not used internally).</param>
        /// <param name="configuration">The scoped configuration instance used to access settings like "Mandant".</param>
        /// <param name="serviceProvider">The application's <see cref="IServiceProvider"/> for resolving services.</param>
        /// <param name="serviceCollectionHolder">Holds the registered service collection for dynamic access.</param>
        public ConfigurationReloadService(IConfiguration configurationRoot, IConfiguration configuration, IServiceProvider serviceProvider
          ,ServiceCollectionHolder serviceCollectionHolder)
        {
            _Configuration = configuration;
            _ServiceProvider = serviceProvider;
            _ServiceCollectionHolder = serviceCollectionHolder;
        }

        public sealed class FreshdeskOptions
        {
            public string BaseUrl { get; set; } = string.Empty;
            public string ApiKey { get; set; } = string.Empty;
        }


        /// <summary>
        ///     Reloads the application's configuration and reinitializes option settings
        ///     for the current tenant using the provided <see cref="IServiceCollection"/>.
        /// </summary>
        /// <param name="serviceCollection">The service collection to update with reloaded configuration data.</param>
        /// <returns>The updated <see cref="IServiceCollection"/> instance.</returns>
        public IServiceCollection ReloadConfigurationNotAsync(IServiceCollection serviceCollection)
        {
            using var scope = _ServiceProvider.CreateScope();
            var services = scope.ServiceProvider;
            var optionsMonitor = services.GetService<IOptionsMonitor<MandantSettingOptions>>();
            try
            {
                if (_Configuration is IConfigurationRoot configurationRoot)
                {
                    configurationRoot.Reload();
                }

                if (optionsMonitor != null)
                {
                    // Diese Zeile aktualisiert dynamisch die Optionen für den Mandanten
                    optionsMonitor.OnChange(updatedOptions =>
                    {
                        UpdateConfigurationOnChange(serviceCollection);

                    });

                    UpdateConfigurationOnChange(serviceCollection);
                }

            }
            catch { }

            return serviceCollection;
        }

        /// <summary>
        ///     Retrieves the configuration key of the mandant based on the provided mandant name.
        /// </summary>
        /// <param name="mandantName">The name of the mandant to search for.</param>
        /// <returns>
        ///     Returns the configuration key (mandantKey) corresponding to the given mandant name,
        ///     or an empty string if no matching mandant is found.
        /// </returns>
        private string GetMandantKey(string mandantName)
        {
            var mandanten = _Configuration.GetSection("SchnittstelleProffixWebshopsJsonConfigFileOption:MandantSettingOptions").GetChildren();
            string mandantKey = "";

            foreach (var mandant in mandanten)
            {
                try
                {
                    mandantKey = mandant.Key;
                    var sectionName = _Configuration.GetRequiredSection($"SchnittstelleProffixWebshopsJsonConfigFileOption:MandantSettingOptions:{mandantKey}:MandantName").Value;
                    var proffixOptions = _Configuration.GetRequiredSection($"SchnittstelleProffixWebshopsJsonConfigFileOption:MandantSettingOptions:{mandantKey}:ProffixOptions");

                    if (mandantName == sectionName)
                    {
                        break;
                    }
                }
                catch (Exception ex)
                { }
            }

            return mandantKey;
        }

        /// <summary>
        ///     Updates various application configuration sections when a configuration change is detected.
        ///     Intended to be called in response to a configuration reload event.
        /// </summary>
        /// <param name="serviceCollection">
        ///     The service collection used during application setup (currently unused).
        /// </param>
        public void UpdateConfigurationOnChange(IServiceCollection serviceCollection)
        {
            _MandantName = _Configuration.GetValue<string>("Mandant");

            var mandanten = _Configuration.GetSection("SchnittstelleProffixWebshopsJsonConfigFileOption:MandantSettingOptions").GetChildren();
            foreach (var mandant in mandanten)
            {
                _MandantKey = GetMandantKey(_MandantName);

                // Update configuration section
                try
                {
                    //UpdateConfig<ProffixOptions>();
                    //UpdateConfig<ShopwareAPIOptions>();
                    //UpdateConfig<NotizOption>();
                    //UpdateConfig<VideoOption>();
                    //UpdateConfig<MediaOption>();
                    //UpdateConfig<BackgroundTaskOption>();
                }
                catch (Exception ex)
                {
                    // Fehlerhandling
                }

                break;
            }
        }

        /// <summary>
        ///     Updates the runtime configuration of a specified options type <typeparamref name="T"/>
        ///     using values from the app configuration based on the current mandant key.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the options to update. Must be a class with a parameterless constructor.
        /// </typeparam>
        private void UpdateConfig<T>() where T : class, new()
        {
            try
            {
                string sectionPath = $"SchnittstelleProffixWebshopsJsonConfigFileOption:MandantSettingOptions:{_MandantKey}:{typeof(T).Name}";
                var configData = _Configuration.GetRequiredSection(sectionPath).Get<T>();

                if (configData != null && _optionsMonitors.TryGetValue(typeof(T), out var monitor))
                {
                    (monitor as IWritableOptionsMonitor<T>)?.Update(options =>
                    {
                        UpdateOptions(options, configData);
                    });
                }
            }
            catch
            {
                // Fehlerhandling
            }
        }

        /// <summary>
        ///     Updates the properties of a target options object with values from a source object of the same type.
        ///     Handles nested complex types recursively and skips null values in the source.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the options objects. Must be a class with accessible properties.
        /// </typeparam>
        /// <param name="target">
        ///     The target object whose properties will be updated.
        /// </param>
        /// <param name="source">
        ///     The source object providing the new values.
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if either <paramref name="target"/> or <paramref name="source"/> is <c>null</c>.
        /// </exception>
        public static void UpdateOptions<T>(T target, T source)
        {
            if (target == null || source == null)
                throw new ArgumentNullException("Source or target cannot be null");

            try
            {
                var properties = typeof(T).GetProperties();
                foreach (var prop in properties)
                {
                    if (prop.CanWrite)
                    {
                        var targetValue = prop.GetValue(target);
                        var sourceValue = prop.GetValue(source);

                        // Überprüfen, ob die Eigenschaft ein komplexes Objekt ist (nicht ein primitiver Typ oder String)
                        if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                        {
                            // Wenn der Zielwert null ist und der Quellwert nicht null ist, dann Instanz erzeugen
                            if (targetValue == null && sourceValue != null)
                            {
                                // Erstelle eine neue Instanz des Typs der verschachtelten Eigenschaft
                                targetValue = Activator.CreateInstance(prop.PropertyType);
                                prop.SetValue(target, targetValue);  // Setze das Zielobjekt

                                // Stelle sicher, dass die Properties des neuen Objekts auch aktualisiert werden
                                GenericUpdateOptions(targetValue, sourceValue, prop.PropertyType);
                            }
                            else if (targetValue != null && sourceValue != null)
                            {
                                // Wenn beide Werte nicht null sind, rekursiv die Properties aktualisieren
                                GenericUpdateOptions(targetValue, sourceValue, prop.PropertyType);
                            }
                        }
                        else
                        {
                            // Für primitive Typen direkt die Werte übertragen
                            if (sourceValue != null)
                            {
                                prop.SetValue(target, sourceValue);
                            }
                        }
                    }
                }
            }
            catch (Exception ex) { }
        }

        /// <summary>
        ///     Recursively updates the properties of a target object with values from a source object of the same type,
        ///     including support for nested classes, arrays, and generic lists. Only non-null values from the source are copied.
        /// </summary>
        /// <typeparam name="T">
        ///     The object type to be updated. Must be a class type.
        /// </typeparam>
        /// <param name="target">
        ///     The destination object whose properties will be updated.
        /// </param>
        /// <param name="source">
        ///     The source object that provides the updated property values.
        /// </param>
        /// <param name="propertyType">
        ///     The <see cref="Type"/> used to retrieve property metadata for processing. Usually typeof(T).
        /// </param>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if either <paramref name="target"/> or <paramref name="source"/> is null.
        /// </exception>
        public static void GenericUpdateOptions<T>(T target, T source, Type propertyType)
        {
            if (target == null || source == null)
                throw new ArgumentNullException("Source or target cannot be null");

            try
            {
                var properties = propertyType.GetProperties();
                foreach (var prop in properties)
                {
                    try
                    {
                        if (!prop.CanWrite) continue;

                        var targetValue = prop.GetValue(target);
                        var sourceValue = prop.GetValue(source);

                        if (source is IList sourceIList && target is IList targetIList)
                        {
                            // Bestimme den Elementtyp anhand des source-Objekts
                            Type elementType = null;
                            if (source.GetType().IsGenericType)
                            {
                                elementType = source.GetType().GetGenericArguments()[0];
                            }
                            else
                            {
                                elementType = typeof(object);
                            }

                            targetIList.Clear();
                            foreach (var sourceItem in sourceIList)
                            {
                                if (sourceItem != null && sourceItem.GetType().IsClass && sourceItem.GetType() != typeof(string))
                                {
                                    object targetItem = Activator.CreateInstance(sourceItem.GetType());
                                    GenericUpdateOptions(targetItem, sourceItem, sourceItem.GetType());
                                    targetIList.Add(targetItem);
                                }
                                else
                                {
                                    targetIList.Add(sourceItem);
                                }
                            }
                        }

                        // 1. Prüfen, ob Property ein Array ist
                        else if (prop.PropertyType.IsArray)
                        {
                            Type elementType = prop.PropertyType.GetElementType();

                            if (targetValue == null && sourceValue != null)
                            {
                                // Neues Array mit passender Länge erstellen
                                Array sourceArray = (Array)sourceValue;
                                Array targetArray = Array.CreateInstance(elementType, sourceArray.Length);
                                prop.SetValue(target, targetArray);
                                targetValue = targetArray;
                            }

                            if (targetValue != null && sourceValue != null)
                            {
                                Array targetArray = (Array)targetValue;
                                Array sourceArray = (Array)sourceValue;

                                for (int i = 0; i < sourceArray.Length; i++)
                                {
                                    object sourceItem = sourceArray.GetValue(i);

                                    if (elementType.IsClass && elementType != typeof(string))
                                    {
                                        object targetItem = Activator.CreateInstance(elementType);
                                        GenericUpdateOptions(targetItem, sourceItem, elementType);
                                        targetArray.SetValue(targetItem, i);
                                    }
                                    else
                                    {
                                        targetArray.SetValue(sourceItem, i);
                                    }
                                }
                            }
                        }
                        // 2. Prüfen, ob Property eine generische List<T> ist
                        else if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                        {
                            Type elementType = prop.PropertyType.GetGenericArguments()[0];

                            if (targetValue == null && sourceValue != null)
                            {
                                targetValue = Activator.CreateInstance(prop.PropertyType);
                                prop.SetValue(target, targetValue);
                            }

                            if (targetValue != null && sourceValue != null)
                            {
                                var targetList = (IList)targetValue;
                                var sourceList = (IList)sourceValue;

                                targetList.Clear();
                                foreach (var sourceItem in sourceList)
                                {
                                    if (elementType.IsClass && elementType != typeof(string))
                                    {
                                        object targetItem = Activator.CreateInstance(elementType);
                                        GenericUpdateOptions(targetItem, sourceItem, elementType);
                                        targetList.Add(targetItem);
                                    }
                                    else
                                    {
                                        targetList.Add(sourceItem);
                                    }
                                }
                            }
                        }
                        // 3. Prüfen, ob Property eine komplexe Klasse ist
                        else if (prop.PropertyType.IsClass && prop.PropertyType != typeof(string))
                        {
                            if (targetValue == null && sourceValue != null)
                            {
                                targetValue = Activator.CreateInstance(prop.PropertyType);
                                prop.SetValue(target, targetValue);
                            }

                            if (targetValue != null && sourceValue != null)
                            {
                                GenericUpdateOptions(targetValue, sourceValue, prop.PropertyType);
                            }
                        }
                        // 4. Primitive Werte oder Strings direkt kopieren
                        else
                        {
                            if (sourceValue != null)
                            {
                                prop.SetValue(target, sourceValue);
                            }
                        }
                    }
                    catch (Exception exec)
                    {
                        // Log oder Fehlerbehandlung
                    }
                }
            }
            catch (Exception ex)
            {
                // Log oder Fehlerbehandlung
            }
        }
    }
}