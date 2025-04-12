using UnityEngine;
using System;
using System.Reflection;
using System.Collections.Generic;

namespace RCore
{
	public static class LifecycleEventsAutoLoader
	{
		// Run this after the scene loads to ensure LifecycleEventsManager potentially exists
		// Adjust RuntimeInitializeLoadType if needed (e.g., BeforeSceneLoad if required earlier)
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		public static void InitializeAndRegisterAll()
		{
			// Ensure LifecycleEventsManager instance is ready BEFORE we try registering.
			// Accessing Instance getter handles its creation.
			var manager = LifecycleEventsManager.Instance;
			if (manager == null)
			{
				Debug.LogError($"{nameof(LifecycleEventsManager)} instance could not be created. Auto-registration aborted.");
				return;
			}

			Debug.Log($"Scanning for {nameof(LifecycleEventAttribute)} types...");

			var typesToRegister = new List<Type>();
			var assemblies = AppDomain.CurrentDomain.GetAssemblies();

			foreach (var assembly in assemblies)
			{
				// Optional: Add filters to ignore certain assemblies (Unity, System, etc.)
				// for performance, though iterating isn't usually that slow.
				if (assembly.FullName.StartsWith("System") || assembly.FullName.StartsWith("Unity")) continue;
				try
				{
					var typesInAssembly = assembly.GetTypes();
					foreach (var type in typesInAssembly)
					{
						// Check if:
						// 1. It's a class
						// 2. It's not abstract
						// 3. It implements INoMonoBehaviour
						// 4. It has the [AutoRegisterWithNonMonoBehaviorAttribute] attribute
						if (type.IsClass && !type.IsAbstract &&
						    typeof(ILifecycleEvent).IsAssignableFrom(type) &&
						    type.GetCustomAttribute<LifecycleEventAttribute>() != null)
						{
							// Check for parameterless constructor
							if (type.GetConstructor(Type.EmptyTypes) == null)
							{
								Debug.LogWarning($"Type {type.Name} has [{nameof(LifecycleEventAttribute)}] but lacks a parameterless constructor. Skipping.");
								continue;
							}
							typesToRegister.Add(type);
						}
					}
				}
				catch (ReflectionTypeLoadException ex)
				{
					// Handle cases where an assembly can't be fully loaded
					Debug.LogError($"Error loading types from assembly {assembly.FullName}: {ex.Message}");
					foreach (var loaderException in ex.LoaderExceptions)
					{
						Debug.LogError($"Loader Exception: {loaderException.Message}");
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"Unexpected error scanning assembly {assembly.FullName}: {ex.Message}");
				}
			}

			Debug.Log($"Found {typesToRegister.Count} types to auto-register.");

			foreach (var type in typesToRegister)
			{
				try
				{
					// Create an instance (requires parameterless constructor)
					var instance = (ILifecycleEvent)Activator.CreateInstance(type);

					// Register it
					manager.Register(instance);
					Debug.Log($"Automatically instantiated and registered: {type.Name}");
				}
				catch (Exception ex)
				{
					Debug.LogError($"Failed to instantiate or register {type.Name}: {ex.Message}");
				}
			}
			Debug.Log("Auto-registration process complete.");
		}
	}
}