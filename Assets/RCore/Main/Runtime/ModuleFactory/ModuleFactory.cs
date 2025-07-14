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
    /// Contains metadata about a discovered module.
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

    public static class ModuleFactory
    {
        // Dictionary to store module metadata, keyed by the string provided in the ModuleAttribute.
        private static Dictionary<string, ModuleMetadata> m_moduleMetadataMap;
        private static bool m_isInitialized = false;

        /// <summary>
        /// Initializes the factory by scanning assemblies for module types.
        /// This is automatically called the first time a module is requested.
        /// </summary>
        private static void InitializeFactory()
        {
            if (m_isInitialized) return;

            m_moduleMetadataMap = new Dictionary<string, ModuleMetadata>();

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (Assembly assembly in assemblies)
            {
                try
                {
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
                            var metadata = new ModuleMetadata(type, attribute.Key, attribute.AutoCreate, attribute.LoadOrder);
                            m_moduleMetadataMap[attribute.Key] = metadata;
                            // Reduced logging verbosity slightly
                            // Debug.Log($"[ModuleFactory] Registered module: Key='{metadata.Key}', Type='{metadata.ModuleType.FullName}', AutoCreate={metadata.AutoCreate}, LoadOrder={metadata.LoadOrder}");
                        }
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
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
        /// Creates an instance of the module associated with the given key.
        /// IMPORTANT: This cannot instantiate MonoBehaviour types. Use AddComponent instead for those.
        /// </summary>
        /// <param name="key">The key of the module to create (defined in ModuleAttribute).</param>
        /// <returns>An instance of IModule, or null if the key is not found, creation fails, or the type is a MonoBehaviour.</returns>
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
                if (typeof(MonoBehaviour).IsAssignableFrom(metadata.ModuleType))
                {
                    Debug.LogError($"[ModuleFactory] Cannot create module with key '{key}'. Type '{metadata.ModuleType.FullName}' is a MonoBehaviour. Use AddComponent or manual registration instead.");
                    return null;
                }
                // --- End Check ---

                try
                {
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
        /// Gets all available module keys that have been registered.
        /// </summary>
        /// <returns>A list of registered module keys.</returns>
        public static List<string> GetAvailableModuleKeys()
        {
            if (!m_isInitialized)
            {
                InitializeFactory();
            }
            return m_moduleMetadataMap.Keys.ToList();
        }
        
        /// <summary>
        /// Gets metadata for a specific module key.
        /// </summary>
        /// <param name="key">The key of the module.</param>
        /// <param name="metadata">The output metadata if found.</param>
        /// <returns>True if metadata was found, false otherwise.</returns>
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
        /// Filters out MonoBehaviour types as they cannot be auto-created by the factory.
        /// </summary>
        /// <returns>A sorted list of ModuleMetadata for non-MonoBehaviour modules.</returns>
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

            // Log a warning for any AutoCreate=true MonoBehaviours found
            var monoBehaviours = autoCreateModules.Except(nonMonoBehaviours);
            foreach(var mbMeta in monoBehaviours)
            {
                 Debug.LogWarning($"[ModuleFactory] Module '{mbMeta.Key}' (Type: {mbMeta.ModuleType.FullName}) is marked for AutoCreate but is a MonoBehaviour. It will be skipped by automatic factory creation. Register it manually from its Awake/Start method if needed.");
            }

            return nonMonoBehaviours;
        }
    }
}