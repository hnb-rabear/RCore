#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;

namespace RCore.Service
{
	[CustomPropertyDrawer(typeof(ABConfig<>))]
	public class ABConfigPropertyDrawer : PropertyDrawer
	{
		// Dictionary to keep text field values persistent per property
		private static Dictionary<string, string> _jsonInputs = new Dictionary<string, string>();

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.BeginProperty(position, label, property);

			var allowFetchingProp = property.FindPropertyRelative("allowFetching");
			var valueProp = property.FindPropertyRelative("value");

			// --- 1. Header ---
			Rect headerRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

			Color originalColor = GUI.contentColor;
			if (!allowFetchingProp.boolValue) GUI.contentColor = Color.gray;

			// Draw Foldout
			property.isExpanded = EditorGUI.PropertyField(headerRect, property, label, false);

			GUI.contentColor = originalColor;

			// --- 2. Expanded Content ---
			if (property.isExpanded)
			{
				EditorGUI.indentLevel++;
				float spacing = EditorGUIUtility.standardVerticalSpacing;
				float lineHeight = EditorGUIUtility.singleLineHeight;

				// A. allowFetching Field
				Rect enabledRect = new Rect(position.x, headerRect.y + lineHeight + spacing, position.width, lineHeight);
				EditorGUI.PropertyField(enabledRect, allowFetchingProp);

				// B. Value Field
				float valueY = enabledRect.y + lineHeight + spacing;
				float valueHeight = EditorGUI.GetPropertyHeight(valueProp, true);
				Rect valueRect = new Rect(position.x, valueY, position.width, valueHeight);
				EditorGUI.PropertyField(valueRect, valueProp, true);

				// C. Import / Export Section
				float toolsY = valueRect.y + valueHeight + spacing;
				DrawToolsSection(position, toolsY, property);

				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}

		private void DrawToolsSection(Rect basePosition, float currentY, SerializedProperty property)
		{
			string key = property.propertyPath;
			if (!_jsonInputs.ContainsKey(key)) _jsonInputs[key] = "";

			float lineHeight = EditorGUIUtility.singleLineHeight;
			float spacing = EditorGUIUtility.standardVerticalSpacing;

			// 1. "Json Input" Text Field
			Rect labelRect = new Rect(basePosition.x, currentY, EditorGUIUtility.labelWidth, lineHeight);
			Rect textRect = new Rect(basePosition.x + EditorGUIUtility.labelWidth, currentY, basePosition.width - EditorGUIUtility.labelWidth, lineHeight);

			GUI.Label(labelRect, "Json Data");
			_jsonInputs[key] = EditorGUI.TextField(textRect, _jsonInputs[key]);

			// 2. Buttons Row (Import | Export)
			float buttonY = currentY + lineHeight + spacing;
			float buttonWidth = (basePosition.width) / 2f - 2f; // Split width for two buttons

			Rect importBtnRect = new Rect(basePosition.x, buttonY, buttonWidth, lineHeight);
			Rect exportBtnRect = new Rect(basePosition.x + buttonWidth + 4f, buttonY, buttonWidth, lineHeight);

			// -- Import Button --
			if (GUI.Button(importBtnRect, "Import from String"))
			{
				ApplyImport(property, _jsonInputs[key]);
			}

			// -- Export Button --
			if (GUI.Button(exportBtnRect, "Export to Clipboard"))
			{
				string json = PerformExport(property);
				if (!string.IsNullOrEmpty(json))
				{
					// Update the text field so user can see what was exported
					_jsonInputs[key] = json;

					// Copy to system clipboard
					EditorGUIUtility.systemCopyBuffer = json;

					Debug.Log($"[ABConfig] JSON exported to clipboard:\n{json}");
				}
			}
		}

		// --- Reflection Helper: Import ---
		private void ApplyImport(SerializedProperty property, string json)
		{
			object targetObject = GetTargetObjectWithProperty(property);
			if (targetObject != null)
			{
				MethodInfo method = targetObject.GetType().GetMethod("Import", BindingFlags.Public | BindingFlags.Instance);
				if (method != null)
				{
					method.Invoke(targetObject, new object[] { json });
					property.serializedObject.Update();
					EditorUtility.SetDirty(property.serializedObject.targetObject);
				}
			}
		}

		// --- Reflection Helper: Export ---
		private string PerformExport(SerializedProperty property)
		{
			object targetObject = GetTargetObjectWithProperty(property);
			if (targetObject != null)
			{
				MethodInfo method = targetObject.GetType().GetMethod("Export", BindingFlags.Public | BindingFlags.Instance);
				if (method != null)
				{
					// Invoke Export() and cast result to string
					return (string)method.Invoke(targetObject, null);
				}
			}
			return null;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			float height = EditorGUIUtility.singleLineHeight;

			if (property.isExpanded)
			{
				var valueProp = property.FindPropertyRelative("value");
				float spacing = EditorGUIUtility.standardVerticalSpacing;

				height += spacing + EditorGUIUtility.singleLineHeight; // allowFetching
				height += spacing + EditorGUI.GetPropertyHeight(valueProp, true); // Value

				// Extra height for Tools (Text Field + Buttons)
				height += spacing + EditorGUIUtility.singleLineHeight; // Text Field row
				height += spacing + EditorGUIUtility.singleLineHeight; // Buttons row
				height += spacing; // Padding
			}

			return height;
		}

		// --- Reflection Utilities (Same as before) ---
		private object GetTargetObjectWithProperty(SerializedProperty prop)
		{
			var path = prop.propertyPath.Replace(".Array.data[", "[");
			object obj = prop.serializedObject.targetObject;
			var elements = path.Split('.');
			foreach (var element in elements)
			{
				if (element.Contains("["))
				{
					var elementName = element.Substring(0, element.IndexOf("["));
					var index = System.Convert.ToInt32(element.Substring(element.IndexOf("[")).Replace("[", "").Replace("]", ""));
					obj = GetValue_Imp(obj, elementName, index);
				}
				else
				{
					obj = GetValue_Imp(obj, element);
				}
			}
			return obj;
		}

		private object GetValue_Imp(object source, string name)
		{
			if (source == null) return null;
			var type = source.GetType();
			while (type != null)
			{
				var f = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
				if (f != null) return f.GetValue(source);
				var p = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
				if (p != null) return p.GetValue(source, null);
				type = type.BaseType;
			}
			return null;
		}

		private object GetValue_Imp(object source, string name, int index)
		{
			var enumerable = GetValue_Imp(source, name) as System.Collections.IEnumerable;
			if (enumerable == null) return null;
			var enm = enumerable.GetEnumerator();
			for (int i = 0; i <= index; i++)
			{
				if (!enm.MoveNext()) return null;
			}
			return enm.Current;
		}
	}
}
#endif