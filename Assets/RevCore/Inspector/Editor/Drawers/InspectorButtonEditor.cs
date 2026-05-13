using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomEditor(typeof(MonoBehaviour), true)]
	[CanEditMultipleObjects]
	public class InspectorButtonEditor : UnityEditor.Editor
	{
		private class ButtonState
		{
			public bool opened;
			public object[] parameters;

			public ButtonState(int paramCount)
			{
				parameters = new object[paramCount];
			}
		}

		private static readonly Dictionary<Type, Func<ParameterInfo, object, object>> TypeDrawers = new()
		{
			{ typeof(float), (p, v) => EditorGUILayout.FloatField(p.Name, (float)(v ?? 0f)) },
			{ typeof(int), (p, v) => EditorGUILayout.IntField(p.Name, (int)(v ?? 0)) },
			{ typeof(string), (p, v) => EditorGUILayout.TextField(p.Name, (string)(v ?? "")) },
			{ typeof(bool), (p, v) => EditorGUILayout.Toggle(p.Name, (bool)(v ?? false)) },
			{ typeof(Color), (p, v) => EditorGUILayout.ColorField(p.Name, (Color)(v ?? Color.white)) },
			{ typeof(Vector2), (p, v) => EditorGUILayout.Vector2Field(p.Name, (Vector2)(v ?? Vector2.zero)) },
			{ typeof(Vector3), (p, v) => EditorGUILayout.Vector3Field(p.Name, (Vector3)(v ?? Vector3.zero)) },
		};

		private MethodInfo[] m_methods;
		private ButtonState[] m_states;

		private void FindMethods()
		{
			if (m_methods != null) return;

			var list = new List<MethodInfo>();
			var type = target.GetType();
			foreach (var method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
			{
				if (method.GetCustomAttribute<InspectorButtonAttribute>() != null)
					list.Add(method);
			}

			m_methods = list.ToArray();
			m_states = new ButtonState[m_methods.Length];
			for (int i = 0; i < m_methods.Length; i++)
				m_states[i] = new ButtonState(m_methods[i].GetParameters().Length);
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			FindMethods();

			for (int i = 0; i < m_methods.Length; i++)
				DrawButton(m_methods[i], m_states[i]);
		}

		private void DrawButton(MethodInfo method, ButtonState state)
		{
			var attr = method.GetCustomAttribute<InspectorButtonAttribute>();
			string label = attr.Label ?? FormatMethodName(method);
			var parameters = method.GetParameters();

			if (parameters.Length == 0)
			{
				if (GUILayout.Button(label))
					InvokeMethod(method, state.parameters);
				return;
			}

			state.opened = EditorGUILayout.Foldout(state.opened, label, true);
			if (!state.opened) return;

			EditorGUI.indentLevel++;
			for (int i = 0; i < parameters.Length; i++)
			{
				var p = parameters[i];
				state.parameters[i] ??= p.HasDefaultValue ? p.DefaultValue : GetDefault(p.ParameterType);
				state.parameters[i] = DrawParameter(p, state.parameters[i]);
			}
			EditorGUI.indentLevel--;

			if (GUILayout.Button($"Invoke {label}"))
				InvokeMethod(method, state.parameters);
		}

		private void InvokeMethod(MethodInfo method, object[] parameters)
		{
			foreach (var t in targets)
			{
				object result = method.Invoke(t, parameters);
				if (result is IEnumerator coroutine && t is MonoBehaviour mb)
					mb.StartCoroutine(coroutine);
			}
		}

		private static object DrawParameter(ParameterInfo param, object value)
		{
			if (typeof(UnityEngine.Object).IsAssignableFrom(param.ParameterType))
				return EditorGUILayout.ObjectField(param.Name, value as UnityEngine.Object, param.ParameterType, true);

			if (TypeDrawers.TryGetValue(param.ParameterType, out var drawer))
				return drawer(param, value);

			EditorGUILayout.LabelField(param.Name, $"Unsupported type: {param.ParameterType.Name}");
			return value;
		}

		private static string FormatMethodName(MethodInfo method)
		{
			var ps = method.GetParameters();
			if (ps.Length == 0) return method.Name;
			var parts = new string[ps.Length];
			for (int i = 0; i < ps.Length; i++)
				parts[i] = $"{ps[i].ParameterType.Name} {ps[i].Name}";
			return $"{method.Name}({string.Join(", ", parts)})";
		}

		private static object GetDefault(Type type)
			=> type.IsValueType ? Activator.CreateInstance(type) : null;
	}
}
