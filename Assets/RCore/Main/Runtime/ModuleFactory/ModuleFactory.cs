/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using UnityEngine; // For Debug.Log

    /// <summary>
    /// Contains immutable metadata about a discovered module type,
    /// derived from its ModuleAttribute.
    /// </summary>
    public readonly struct ModuleMetadata // Using readonly struct for immutability and value type
    {
        public Type ModuleType { get; }
        public string Key { get; }
        public bool AutoCreate { get; }
        public int LoadOrder { get; }

        public ModuleMetadata(Type type, string key, bool autoCreate, int loadOrder)
        {
            ModuleType = type;
            Key = key;
            AutoCreate = autoCreate;
            LoadOrder = loadOrder;
        }
    }

    /// <summary>
    /// A static factory responsible for discovering and creating module instances.
    /// It uses reflection to scan loaded assemblies for classes that implement IModule
    /// and are decorated with the [Module] attribute. It serves as the central point
    /// for module instantiation before they are registered with the ModuleManager.
    /// </summary>
    public static class ModuleFactory
    {
        // Dictionary to store module metadata, keyed by the string provided in the ModuleAttribute.
        private static Dictionary<string, ModuleMetadata> m_moduleMetadataMap;
        private static bool m_isInitialized = false;

        /// <summary>
        /// Initializes the factory by scanning all loaded assemblies for types that are
        /// decorated with the [Module] attribute. This is a lazy-initialized, one-time operation.
        /// </summary>
        private static void InitializeFactory()
        {
            if (m_isInitialized) return;

            m_moduleMetadataMap = new Dictionary<string, ModuleMetadata>();

            // Scan all assemblies in the current AppDomain
            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
                    // Find all non-abstract classes that implement IModule and have the ModuleAttribute
                    var types = assembly.GetTypes()
                        .Where(t => t.IsClass && !t.IsAbstract && typeof(IModule).IsAssignableFrom(t) &&
                                    t.GetCustomAttribute<ModuleAttribute>(false) != null);

                    foreach (Type type in types)
                    {
                        ModuleAttribute attribute = type.GetCustomAttribute<ModuleAttribute>(false);
                        if (attribute != null) // Should always be true due to the Where clause
                        {
                            if (m_moduleMetadataMap.ContainsKey(attribute.Key))
                            {
                                Debug.LogWarning($"[ModuleFactory] Duplicate module key '{attribute.Key}' found for type '{type.FullName}'. Overwriting with the last one found. Original: '{m_moduleMetadataMap[attribute.Key].ModuleType.FullName}'");
                            }
							// Store the collected metadata
                            var metadata = new ModuleMetadata(type, attribute.Key, attribute.AutoCreate, attribute.LoadOrder);
                            m_moduleMetadataMap[attribute.Key] = metadata;
                            // Reduced logging verbosity slightly
                            // Debug.Log($"[ModuleFactory] Registered module: Key='{metadata.Key}', Type='{metadata.ModuleType.FullName}', AutoCreate={metadata.AutoCreate}, LoadOrder={metadata.LoadOrder}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
					// Handle cases where an assembly can't be fully loaded
                    Debug.LogError($"[ModuleFactory] Error loading types from assembly '{assembly.FullName}': {ex.Message}");
                    foreach (var loaderException in ex.LoaderExceptions)
                    {
                        Debug.LogError($"  LoaderException: {loaderException.Message}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModuleFactory] Unexpected error scanning assembly '{assembly.FullName}': {ex.Message}");
                }
            }
            m_isInitialized = true;
            Debug.Log($"[ModuleFactory] Initialization complete. Found {m_moduleMetadataMap.Count} module types.");
        }

        /// <summary>
        /// Creates an instance of a module associated with the given key.
        /// IMPORTANT: This method CANNOT instantiate types that inherit from MonoBehaviour.
        /// </summary>
        /// <param name="key">The unique key of the module to create (defined in its ModuleAttribute).</param>
        /// <returns>An instance of the module as IModule, or null if the key is not found, creation fails, or the type is a MonoBehaviour.</returns>
        public static IModule CreateModule(string key)
        {
            if (!m_isInitialized)
            {
                InitializeFactory();
            }

            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[ModuleFactory] Cannot create module: key is null or empty.");
                return null;
            }

            if (m_moduleMetadataMap.TryGetValue(key, out ModuleMetadata metadata))
            {
                 // --- Check if the type is a MonoBehaviour ---
				// Prevent instantiation of MonoBehaviours, as this must be done by Unity.
                if (typeof(MonoBehaviour).IsAssignableFrom(metadata.ModuleType))
                {
                    Debug.LogError($"[ModuleFactory] Cannot create module with key '{key}'. Type '{metadata.ModuleType.FullName}' is a MonoBehaviour. Use AddComponent or manual registration instead.");
                    return null;
                }
                // --- End Check ---

                try
                {
					// Create an instance of the class using its default constructor.
                    IModule moduleInstance = Activator.CreateInstance(metadata.ModuleType) as IModule;
                    if (moduleInstance != null)
                    {
                        // Debug.Log($"[ModuleFactory] Successfully created module with key '{key}'. Type: {metadata.ModuleType.FullName}"); // Logged by ModuleManager now
                        return moduleInstance;
                    }
                    else
                    {
                        // This case might be less likely now due to the IsAssignableFrom check, but kept for safety.
                        Debug.LogError($"[ModuleFactory] Failed to cast instance of type '{metadata.ModuleType.FullName}' to IModule for key '{key}'.");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ModuleFactory] Error creating instance of module type '{metadata.ModuleType.FullName}' for key '{key}': {ex.Message}\n{ex.StackTrace}");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning($"[ModuleFactory] No module found with key '{key}'.");
                return null;
            }
        }

        /// <summary>
        /// Gets a list of all unique keys for the modules discovered by the factory.
        /// </summary>
        /// <returns>A List<string> of registered module keys.</returns>
        public static List<string> GetAvailableModuleKeys()
        {
            if (!m_isInitialized)
            {
                InitializeFactory();
            }
            return m_moduleMetadataMap.Keys.ToList();
        }
        
        /// <summary>
        /// Tries to retrieve the metadata for a specific module key.
        /// </summary>
        /// <param name="key">The key of the module.</param>
        /// <param name="metadata">When this method returns, contains the metadata associated with the specified key, if the key is found; otherwise, the default value for the type of the metadata parameter.</param>
        /// <returns>True if metadata was found for the key; otherwise, false.</returns>
        public static bool TryGetModuleMetadata(string key, out ModuleMetadata metadata)
        {
            if (!m_isInitialized)
            {
                InitializeFactory();
            }
            return m_moduleMetadataMap.TryGetValue(key, out metadata);
        }


        /// <summary>
        /// Gets metadata for all modules that are marked for auto-creation, sorted by their LoadOrder.
        /// It filters out MonoBehaviour types, as they cannot be created by this factory and must be handled manually by Unity.
        /// </summary>
        /// <returns>A sorted list of ModuleMetadata for modules to be auto-created.</returns>
        public static List<ModuleMetadata> GetModulesForAutoCreation()
        {
            if (!m_isInitialized)
            {
                InitializeFactory();
            }

            var autoCreateModules = m_moduleMetadataMap.Values
                .Where(meta => meta.AutoCreate)
                .OrderBy(meta => meta.LoadOrder)
                .ToList();

            // Filter out MonoBehaviours as the factory cannot create them
            var nonMonoBehaviours = autoCreateModules
                .Where(meta => !typeof(MonoBehaviour).IsAssignableFrom(meta.ModuleType))
                .ToList();

            // Log a warning for any AutoCreate=true MonoBehaviours found, as this is a common misconfiguration.
            var monoBehaviours = autoCreateModules.Except(nonMonoBehaviours);
            foreach(var mbMeta in monoBehaviours)
            {
                 Debug.LogWarning($"[ModuleFactory] Module '{mbMeta.Key}' (Type: {mbMeta.ModuleType.FullName}) is marked for AutoCreate but is a MonoBehaviour. It will be skipped by automatic factory creation. Register it manually from its Awake/Start method if needed.");
            }

            return nonMonoBehaviours;
        }
    }
}