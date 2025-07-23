using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RCore.Inspector
{
	/// <summary>
	/// An attribute used on a ScriptableObject field to expose its properties directly within the inspector of the
	/// object that holds the reference. This creates an "inline" or "nested" editor for the ScriptableObject,
	/// making it easy to edit without having to select the asset itself. It also includes a "Create" button
	/// for convenience.
	/// </summary>
	public class ExposeScriptableObjectAttribute : PropertyAttribute
	{
		public ExposeScriptableObjectAttribute() { }
	}

#if UNITY_EDITOR
	/// <summary>
	/// The custom property drawer for fields marked with the [ExposeScriptableObject] attribute.
	/// It handles drawing the foldout, the nested editor for the ScriptableObject, and the "Create" button.
	/// </summary>
	[CustomPropertyDrawer(typeof(ExposeScriptableObjectAttribute))]
	public class ExposeScriptableObjectDrawer : PropertyDrawer
	{
		private bool m_foldout = true;
		private const float PADDING = 5f;
		private static readonly Color BackgroundColor = new(0.2f, 0.2f, 0.2f, 0.5f); // Added alpha for subtlety

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);
			
			// --- Draw the Main Property Field with a Foldout ---
			var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			m_foldout = EditorGUI.Foldout(foldoutRect, m_foldout, label, true);
			
			// Draw the object reference field next to the foldout label.
			var fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(fieldRect, property, GUIContent.none, true);

			// --- Draw the Inline Editor if Folded Out ---
			if (m_foldout && property.objectReferenceValue != null && property.objectReferenceValue is ScriptableObject scriptableObject)
			{
				var serializedObject = new SerializedObject(scriptableObject);
				var prop = serializedObject.GetIterator();
				prop.NextVisible(true); // Skip the 'm_Script' field

				// --- Draw Background ---
				float startY = position.y + EditorGUIUtility.singleLineHeight + PADDING;
				float totalHeight = GetPropertyHeight(property, label) - EditorGUIUtility.singleLineHeight - PADDING;
				var backgroundRect = new Rect(position.x, startY, position.width, totalHeight);
				EditorGUI.DrawRect(backgroundRect, BackgroundColor);
				
				// --- Draw Serialized Fields ---
				float currentY = startY;
				EditorGUI.indentLevel++;
				while (prop.NextVisible(false))
				{
					float propHeight = EditorGUI.GetPropertyHeight(prop, true);
					var propRect = new Rect(position.x, currentY, position.width, propHeight);
					EditorGUI.PropertyField(propRect, prop, true);
					currentY += propHeight + EditorGUIUtility.standardVerticalSpacing;
				}
				EditorGUI.indentLevel--;
				
				serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.EndProperty();
		}

		/// <summary>
		/// Calculates the total height required for the property, including the nested editor fields when the foldout is open.
		/// </summary>
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float totalHeight = EditorGUI.GetPropertyHeight(property, label, false); // Get height of the main field only

			if (m_foldout && property.objectReferenceValue != null && property.objectReferenceValue is ScriptableObject scriptableObject)
			{
				// Add padding for the top of the exposed editor box.
				totalHeight += PADDING;
				
				var serializedObject = new SerializedObject(scriptableObject);
				var prop = serializedObject.GetIterator();
				prop.NextVisible(true); // Skip 'm_Script'

				// Add height for each visible property inside the ScriptableObject.
				while (prop.NextVisible(false))
				{
					totalHeight += EditorGUI.GetPropertyHeight(prop, true) + EditorGUIUtility.standardVerticalSpacing;
				}
				
				// Add padding for the bottom of the exposed editor box.
				totalHeight += PADDING;
			}

			return totalHeight;
		}
	}
#endif
}