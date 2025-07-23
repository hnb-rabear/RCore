using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace RCore.Inspector
{
	/// <summary>
	/// Attribute to mark a method to be displayed as a button in the Unity Inspector.
	/// This allows for easy testing and execution of methods directly from the editor.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class InspectorButtonAttribute : PropertyAttribute { }

#if UNITY_EDITOR
	/// <summary>
	/// Custom editor that finds all methods marked with [InspectorButton] in a MonoBehaviour
	/// and displays them as clickable buttons in the inspector.
	/// It supports methods with parameters, which are drawn in a foldout section.
	/// </summary>
	[CustomEditor(typeof(MonoBehaviour), true)]
	[CanEditMultipleObjects]
	public class InspectorButton : UnityEditor.Editor
	{
		/// <summary>
		/// Internal class to store the state of a button in the editor,
		/// such as whether its parameter foldout is open and the current parameter values.
		/// </summary>
		private class EditorButtonState
		{
			public bool opened;
			public object[] parameters;

			public EditorButtonState(int numberOfParameters)
			{
				parameters = new object[numberOfParameters];
			}
		}

		private EditorButtonState[] editorButtonStates;

		private delegate object ParameterDrawer(ParameterInfo parameter, object val);

		// Dictionary mapping parameter types to their specific drawing functions.
		private readonly Dictionary<Type, ParameterDrawer> typeDrawer = new Dictionary<Type, ParameterDrawer>
		{
			{ typeof(float), DrawFloatParameter },
			{ typeof(int), DrawIntParameter },
			{ typeof(string), DrawStringParameter },
			{ typeof(bool), DrawBoolParameter },
			{ typeof(Color), DrawColorParameter },
			{ typeof(Vector3), DrawVector3Parameter },
			{ typeof(Vector2), DrawVector2Parameter },
			{ typeof(Quaternion), DrawQuaternionParameter }
		};

		// Dictionary mapping types to their user-friendly display names for the button signature.
		private readonly Dictionary<Type, string> typeDisplayName = new Dictionary<Type, string>
		{
			{ typeof(float), "float" },
			{ typeof(int), "int" },
			{ typeof(string), "string" },
			{ typeof(bool), "bool" },
			{ typeof(Color), "Color" },
			{ typeof(Vector3), "Vector3" },
			{ typeof(Vector2), "Vector2" },
			{ typeof(Quaternion), "Quaternion" }
		};

		/// <summary>
		/// Overrides the default inspector GUI to draw the default inspector and then
		/// add the custom buttons for marked methods.
		/// </summary>
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var mono = target as MonoBehaviour;
			if (mono == null) return;

			var methods = mono.GetType()
				.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				.OfType<MethodInfo>()
				.Where(m => Attribute.IsDefined(m, typeof(InspectorButtonAttribute)));

			if (!methods.Any()) return;

			if (editorButtonStates == null)
				CreateEditorButtonStates(methods.ToArray());

			int methodIndex = 0;
			foreach (var method in methods)
			{
				DrawButtonForMethod(targets, method, editorButtonStates[methodIndex++]);
			}
		}

		/// <summary>
		/// Initializes the state objects for each button.
		/// </summary>
		private void CreateEditorButtonStates(MethodInfo[] methods)
		{
			editorButtonStates = methods.Select(method => new EditorButtonState(method.GetParameters().Length)).ToArray();
		}

		/// <summary>
		/// Draws a button and its associated parameter fields for a given method.
		/// </summary>
		/// <param name="invocationTargets">The objects the method will be invoked on.</param>
		/// <param name="methodInfo">The metadata for the method to be invoked.</param>
		/// <param name="state">The current state of the button's UI.</param>
		private void DrawButtonForMethod(object[] invocationTargets, MethodInfo methodInfo, EditorButtonState state)
		{
			EditorGUILayout.BeginHorizontal();
			// Foldout for parameters
			if (methodInfo.GetParameters().Length > 0)
				state.opened = EditorGUI.Foldout(EditorGUILayout.GetControlRect(GUILayout.Width(10.0f)), state.opened, "");
			else
				GUILayout.Space(14.0f); // Add space to align with foldout buttons

			bool clicked = GUILayout.Button(MethodDisplayName(methodInfo), GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();

			// Draw parameter fields if the foldout is open
			if (state.opened)
			{
				EditorGUI.indentLevel++;
				for (int i = 0; i < methodInfo.GetParameters().Length; i++)
				{
					var parameterInfo = methodInfo.GetParameters()[i];
					state.parameters[i] = DrawParameterInfo(parameterInfo, state.parameters[i] ?? GetDefaultValue(parameterInfo));
				}
				EditorGUI.indentLevel--;
			}

			if (clicked)
			{
				foreach (var invocationTarget in invocationTargets)
				{
					var monoTarget = (MonoBehaviour)invocationTarget;
					var returnVal = methodInfo.Invoke(monoTarget, state.parameters);

					// If the method returns a coroutine, start it.
					if (returnVal is IEnumerator val)
						monoTarget.StartCoroutine(val);
					else if (returnVal != null)
						Debug.Log("Method call result -> " + returnVal);
				}
			}
		}

		/// <summary>
		/// Gets the default value for a parameter, which is either its specified default value
		/// or the default for its type.
		/// </summary>
		private object GetDefaultValue(ParameterInfo parameter)
		{
			return parameter.HasDefaultValue ? parameter.DefaultValue :
				parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
		}

		/// <summary>
		/// Draws the UI for a single parameter.
		/// </summary>
		private object DrawParameterInfo(ParameterInfo parameterInfo, object currentValue)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(parameterInfo.Name);
			var drawer = GetParameterDrawer(parameterInfo);
			var paramValue = drawer != null ? drawer(parameterInfo, currentValue) : currentValue;
			EditorGUILayout.EndHorizontal();
			return paramValue;
		}

		/// <summary>
		/// Gets the appropriate drawing function for a given parameter type.
		/// </summary>
		private ParameterDrawer GetParameterDrawer(ParameterInfo parameter)
		{
			if (typeof(UnityEngine.Object).IsAssignableFrom(parameter.ParameterType))
				return DrawUnityEngineObjectParameter;

			return typeDrawer.TryGetValue(parameter.ParameterType, out var drawer) ? drawer : null;
		}

		// --- Parameter Drawer Methods ---

		private static object DrawFloatParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.FloatField(Convert.ToSingle(val));

		private static object DrawIntParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.IntField(Convert.ToInt32(val));

		private static object DrawBoolParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.Toggle(Convert.ToBoolean(val));

		private static object DrawStringParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.TextField((string)val);

		private static object DrawColorParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.ColorField((Color)val);

		private static object DrawUnityEngineObjectParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.ObjectField((UnityEngine.Object)val, parameterInfo.ParameterType, true);

		private static object DrawVector2Parameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.Vector2Field("", (Vector2)val);

		private static object DrawVector3Parameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.Vector3Field("", (Vector3)val);

		private static object DrawQuaternionParameter(ParameterInfo parameterInfo, object val)
		{
			var quaternion = (Quaternion)val;
			var euler = EditorGUILayout.Vector3Field("", quaternion.eulerAngles);
			return Quaternion.Euler(euler);
		}

		/// <summary>
		/// Creates a user-friendly display name for the method, including its parameters.
		/// Example: "MyMethod(int myInt, string myString)"
		/// </summary>
		private string MethodDisplayName(MethodInfo method) =>
			$"{method.Name}({string.Join(", ", method.GetParameters().Select(MethodParameterDisplayName))})";

		/// <summary>
		/// Creates a user-friendly display name for a single method parameter.
		/// Example: "int myInt"
		/// </summary>
		private string MethodParameterDisplayName(ParameterInfo parameterInfo) =>
			$"{(typeDisplayName.TryGetValue(parameterInfo.ParameterType, out var displayName) ? displayName : parameterInfo.ParameterType.Name)} {parameterInfo.Name}";
	}
#endif
}