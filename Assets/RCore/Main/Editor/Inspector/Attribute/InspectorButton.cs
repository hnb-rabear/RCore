using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;
using UnityEditor;
using System.Reflection;

namespace RCore.Editor.Inspector
{
	[CustomEditor(typeof(MonoBehaviour), true)]
	[CanEditMultipleObjects]
	public class InspectorButton : UnityEditor.Editor
	{
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

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var mono = target as MonoBehaviour;
			var methods = mono.GetType()
				.GetMembers(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				.OfType<MethodInfo>()
				.Where(m => Attribute.IsDefined(m, typeof(InspectorButtonAttribute)));

			if (editorButtonStates == null)
				CreateEditorButtonStates(methods.ToArray());

			int methodIndex = 0;
			foreach (var method in methods)
			{
				DrawButtonForMethod(targets, method, editorButtonStates[methodIndex++]);
			}
		}

		private void CreateEditorButtonStates(MethodInfo[] methods)
		{
			editorButtonStates = methods.Select(method => new EditorButtonState(method.GetParameters().Length)).ToArray();
		}

		private void DrawButtonForMethod(object[] invocationTargets, MethodInfo methodInfo, EditorButtonState state)
		{
			EditorGUILayout.BeginHorizontal();
			state.opened = EditorGUI.Foldout(EditorGUILayout.GetControlRect(GUILayout.Width(10.0f)), state.opened, "");
			bool clicked = GUILayout.Button(MethodDisplayName(methodInfo), GUILayout.ExpandWidth(true));
			EditorGUILayout.EndHorizontal();

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

					if (returnVal is IEnumerator val)
						monoTarget.StartCoroutine(val);
					else if (returnVal != null)
						Debug.Log("Method call result -> " + returnVal);
				}
			}
		}

		private object GetDefaultValue(ParameterInfo parameter)
		{
			return parameter.HasDefaultValue ? parameter.DefaultValue :
				parameter.ParameterType.IsValueType ? Activator.CreateInstance(parameter.ParameterType) : null;
		}

		private object DrawParameterInfo(ParameterInfo parameterInfo, object currentValue)
		{
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(parameterInfo.Name);
			var drawer = GetParameterDrawer(parameterInfo);
			var paramValue = drawer(parameterInfo, currentValue);
			EditorGUILayout.EndHorizontal();
			return paramValue;
		}

		private ParameterDrawer GetParameterDrawer(ParameterInfo parameter)
		{
			return typeof(UnityEngine.Object).IsAssignableFrom(parameter.ParameterType) ? DrawUnityEngineObjectParameter :
				typeDrawer.TryGetValue(parameter.ParameterType, out var drawer) ? drawer : null;
		}

		private static object DrawFloatParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.FloatField(Convert.ToSingle(val));

		private static object DrawIntParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.IntField((int)val);

		private static object DrawBoolParameter(ParameterInfo parameterInfo, object val) =>
			EditorGUILayout.Toggle((bool)val);

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

		private static object DrawQuaternionParameter(ParameterInfo parameterInfo, object val) =>
			Quaternion.Euler(EditorGUILayout.Vector3Field("", ((Quaternion)val).eulerAngles));

		private string MethodDisplayName(MethodInfo method) =>
			$"{method.Name}({string.Join(", ", method.GetParameters().Select(MethodParameterDisplayName))})";

		private string MethodParameterDisplayName(ParameterInfo parameterInfo) =>
			$"{(typeDisplayName.TryGetValue(parameterInfo.ParameterType, out var displayName) ? displayName : parameterInfo.ParameterType.ToString())} {parameterInfo.Name}";
	}
}