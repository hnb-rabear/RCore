using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute that, when applied to a field in a MonoBehaviour or ScriptableObject,
	/// automatically tries to find and assign a reference to it in the Unity Editor if the field is null.
	/// It can handle single object references and arrays/lists of Components or ScriptableObjects.
	/// </summary>
	public class AutoFillAttribute : PropertyAttribute
	{
		/// <summary>
		/// An optional path to specify where to search for the component or asset.
		/// For components, this is a relative path from the MonoBehaviour's Transform (e.g., "Child/GrandChild").
		/// For ScriptableObjects, this is a folder path within the "Assets" directory (e.g., "Assets/Data/Items").
		/// If empty, the search is performed globally (GetComponentsInChildren for components, entire AssetDatabase for ScriptableObjects).
		/// </summary>
		public string Path { get; private set; }

		/// <summary>
		/// Initializes a new instance of the AutoFillAttribute.
		/// </summary>
		/// <param name="path">Optional search path for the asset or component.</param>
		public AutoFillAttribute(string path = "")
		{
			Path = path;
		}
	}

#if UNITY_EDITOR
	/// <summary>
	/// The custom property drawer for fields marked with the [AutoFill] attribute.
	/// This class contains the editor logic to find and assign references.
	/// </summary>
	[CustomPropertyDrawer(typeof(AutoFillAttribute))]
	public class AutoFillDrawer : PropertyDrawer
	{
		/// <summary>
		/// The main GUI drawing method for the property. It determines if the property is a single reference
		/// or an array and calls the appropriate handler.
		/// </summary>
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var autoFill = (AutoFillAttribute)attribute;
			// Check if the property is an array or a list and dispatch to the correct handler.
			if (property.isArray && property.propertyType == SerializedPropertyType.Generic)
			{
				HandleArrayProperty(position, property, label, autoFill);
			}
			else
			{
				HandleObjectReferenceProperty(position, property, label, autoFill);
			}
		}

		/// <summary>
		/// Handles the logic for a single object reference property.
		/// If the reference is null, it attempts to find a suitable component or ScriptableObject.
		/// </summary>
		private void HandleObjectReferenceProperty(Rect position, SerializedProperty property, GUIContent label, AutoFillAttribute autoFill)
		{
			// Only attempt to fill if the field is currently empty.
			if (property.objectReferenceValue == null)
			{
				var fieldType = fieldInfo.FieldType;

				// Handle MonoBehaviour and Component types
				if (typeof(Component).IsAssignableFrom(fieldType))
				{
					var targetMonoBehaviour = property.serializedObject.targetObject as MonoBehaviour;
					if (targetMonoBehaviour != null)
					{
						FindComponentInChildren(autoFill, property, fieldType, targetMonoBehaviour);
					}
				}
				// Handle ScriptableObject types
				else if (typeof(ScriptableObject).IsAssignableFrom(fieldType))
				{
					FindScriptableObject(autoFill, property, fieldType);
				}
			}

			// Draw the property field as usual.
			EditorGUI.PropertyField(position, property, label);
		}

		/// <summary>
		/// Handles the logic for an array or list property.
		/// It attempts to find all matching components or ScriptableObjects and populate the array with them.
		/// </summary>
		private void HandleArrayProperty(Rect position, SerializedProperty property, GUIContent label, AutoFillAttribute autoFill)
		{
			// Determine the element type of the array/list.
			var elementType = fieldInfo.FieldType.GetElementType() ?? fieldInfo.FieldType.GetGenericArguments().FirstOrDefault();
			if (elementType == null) return;

			// Handle arrays of Components
			if (typeof(Component).IsAssignableFrom(elementType))
			{
				var targetMonoBehaviour = property.serializedObject.targetObject as MonoBehaviour;
				if (targetMonoBehaviour != null)
				{
					var components = targetMonoBehaviour.GetComponentsInChildren(elementType, true); // Include inactive
					AssignComponentsToArrayProperty(property, components);
				}
			}
			// Handle arrays of ScriptableObjects
			else if (typeof(ScriptableObject).IsAssignableFrom(elementType))
			{
				var guids = string.IsNullOrEmpty(autoFill.Path)
					? AssetDatabase.FindAssets($"t:{elementType.Name}")
					: AssetDatabase.FindAssets($"t:{elementType.Name}", new[] { autoFill.Path });
				var assets = guids.Select(AssetDatabase.GUIDToAssetPath)
					.Select(p => AssetDatabase.LoadAssetAtPath(p, elementType) as ScriptableObject)
					.ToArray();
				AssignScriptableObjectsToArrayProperty(property, assets);
			}

			// Draw the array property field, allowing user to expand and see the auto-filled elements.
			EditorGUI.PropertyField(position, property, label, true);
		}

		/// <summary>
		/// Finds a component in the children of the target MonoBehaviour and assigns it to the property.
		/// </summary>
		private void FindComponentInChildren(AutoFillAttribute autoFill, SerializedProperty property, System.Type fieldType, MonoBehaviour targetMonoBehaviour)
		{
			if (string.IsNullOrEmpty(autoFill.Path))
			{
				// If no path is specified, find the first component of the type in children.
				var component = targetMonoBehaviour.GetComponentInChildren(fieldType, true); // Include inactive
				if (component != null)
				{
					property.objectReferenceValue = component;
				}
			}
			else
			{
				// If a path is specified, find the component on the specific child transform.
				var target = targetMonoBehaviour.transform.Find(autoFill.Path);
				if (target != null)
				{
					var component = target.GetComponent(fieldType);
					if (component != null)
					{
						property.objectReferenceValue = component;
					}
				}
			}
		}

		/// <summary>
		/// Finds a ScriptableObject asset in the project and assigns it to the property.
		/// </summary>
		private void FindScriptableObject(AutoFillAttribute autoFill, SerializedProperty property, System.Type fieldType)
		{
			// Find assets of the specified type, optionally filtering by path.
			string[] guids = string.IsNullOrEmpty(autoFill.Path)
				? AssetDatabase.FindAssets($"t:{fieldType.Name}")
				: AssetDatabase.FindAssets($"t:{fieldType.Name}", new[] { autoFill.Path });
			
			// Load the first asset found from its GUID.
			if (guids.Length > 0)
			{
				var assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
				var asset = AssetDatabase.LoadAssetAtPath(assetPath, fieldType);
				property.objectReferenceValue = asset;
			}
		}

		/// <summary>
		/// Assigns an array of components to a serialized list or array property.
		/// </summary>
		private void AssignComponentsToArrayProperty(SerializedProperty property, Component[] components)
		{
			property.arraySize = components.Length;
			for (int i = 0; i < components.Length; i++)
			{
				property.GetArrayElementAtIndex(i).objectReferenceValue = components[i];
			}
		}

		/// <summary>
		/// Assigns an array of ScriptableObjects to a serialized list or array property.
		/// </summary>
		private void AssignScriptableObjectsToArrayProperty(SerializedProperty property, ScriptableObject[] assets)
		{
			property.arraySize = assets.Length;
			for (int i = 0; i < assets.Length; i++)
			{
				property.GetArrayElementAtIndex(i).objectReferenceValue = assets[i];
			}
		}
	}
#endif
}