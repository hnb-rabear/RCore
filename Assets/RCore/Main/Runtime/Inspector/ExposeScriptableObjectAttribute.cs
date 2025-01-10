using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Inspector
{
	public class ExposeScriptableObjectAttribute : PropertyAttribute
	{
		public ExposeScriptableObjectAttribute() { }
	}

#if UNITY_EDITOR
	[CustomPropertyDrawer(typeof(ExposeScriptableObjectAttribute))]
	public class ExposeScriptableObjectDrawer : PropertyDrawer
	{
		private bool m_foldout = true;
		private const float PADDING = 5f;
		private static readonly Color BackgroundColor = new(0.2f, 0.2f, 0.2f);

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			// Draw the property field with foldout
			var foldoutRect = new Rect(position.x, position.y, 10, position.height);
			float fieldWidth = m_foldout ? position.width : position.width - 70;
			var fieldRect = new Rect(position.x, position.y, fieldWidth, position.height);
			var buttonRect = new Rect(position.x + position.width - 65, position.y, 65, position.height);

			m_foldout = EditorGUI.Foldout(foldoutRect, m_foldout, GUIContent.none);
			EditorGUI.PropertyField(fieldRect, property, label, true);

			// Only show the Create button if the foldout is closed
			if (!m_foldout)
			{
				// Check if the object is null
				GUI.enabled = property.objectReferenceValue == null;

				// Draw the Create button
				if (GUI.Button(buttonRect, "Create"))
				{
					var objectType = fieldInfo.FieldType.GetElementType() ?? fieldInfo.FieldType;

					var newObject = ScriptableObject.CreateInstance(objectType);
					if (newObject != null)
					{
						string assetPath = AssetDatabase.GetAssetPath(property.serializedObject.targetObject);
						string directoryPath = !string.IsNullOrEmpty(assetPath) ? System.IO.Path.GetDirectoryName(assetPath) : null;

						if (string.IsNullOrEmpty(directoryPath))
						{
							directoryPath = EditorUtility.OpenFolderPanel("Select Folder to Save New Asset", "Assets", "");
							if (string.IsNullOrEmpty(directoryPath))
							{
								Debug.LogWarning("No folder selected. Creation canceled.");
								return;
							}

							directoryPath = "Assets" + directoryPath.Substring(Application.dataPath.Length);
						}

						string newAssetPath = AssetDatabase.GenerateUniqueAssetPath(directoryPath + "/New" + objectType.Name + ".asset");

						AssetDatabase.CreateAsset(newObject, newAssetPath);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();

						property.objectReferenceValue = newObject;
						property.serializedObject.ApplyModifiedProperties();

						Debug.Log("New " + objectType.Name + " asset created and assigned to the field at " + newAssetPath);
					}
					else
					{
						Debug.LogError("Failed to create a new instance of " + objectType.Name);
					}
				}

				GUI.enabled = true;
			}

			// Draw foldout for serialized fields
			if (property.objectReferenceValue != null && property.objectReferenceValue is ScriptableObject)
			{
				var scriptableObject = (ScriptableObject)property.objectReferenceValue;
				var serializedObject = new SerializedObject(scriptableObject);
				var prop = serializedObject.GetIterator();
				prop.NextVisible(true);

				if (m_foldout)
				{
					// Draw background
					var backgroundRect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight + PADDING, position.width, GetPropertyHeight(property, label) - PADDING * 2);
					EditorGUI.DrawRect(backgroundRect, BackgroundColor);

					position.y += EditorGUIUtility.standardVerticalSpacing + PADDING;
					while (prop.NextVisible(false))
					{
						position.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
						EditorGUI.PropertyField(new Rect(position.x + PADDING, position.y, position.width - PADDING * 2, position.height), prop, true);
					}
				}
				serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float totalHeight = EditorGUI.GetPropertyHeight(property, label, true);

			if (property.objectReferenceValue != null && property.objectReferenceValue is ScriptableObject)
			{
				var serializedObject = new SerializedObject((ScriptableObject)property.objectReferenceValue);
				var prop = serializedObject.GetIterator();
				prop.NextVisible(true);

				totalHeight += m_foldout ? PADDING * 2 : 0;

				if (m_foldout)
				{
					while (prop.NextVisible(false))
					{
						totalHeight += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
					}
				}
			}

			return totalHeight;
		}
	}
#endif
}