using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RevCore.Editor
{
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public sealed class ShowIfDrawer : PropertyDrawer
	{
		// Cache compiled accessor per (target type, member name). First lookup pays the reflection +
		// Expression.Compile cost (~tens of microseconds); every later call goes through a tight
		// delegate invocation (~tens of nanoseconds). A null entry means the member wasn't found —
		// we cache that too so the three failed reflection lookups don't repeat every OnGUI.
		private static readonly Dictionary<(Type, string), Func<object, bool>> s_accessorCache = new();
		private static readonly HashSet<(Type, string)> s_missLog = new();

		private ShowIfAttribute Attribute => (ShowIfAttribute)attribute;

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return ShouldShow(property)
				? EditorGUI.GetPropertyHeight(property, label, true)
				: -EditorGUIUtility.standardVerticalSpacing;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (ShouldShow(property))
				EditorGUI.PropertyField(position, property, label, true);
		}

		private bool ShouldShow(SerializedProperty property)
		{
			object target = property.serializedObject.targetObject;
			var type = target.GetType();
			string name = Attribute.ConditionMemberName;
			var accessor = GetOrBuildAccessor(type, name);
			if (accessor != null)
				return accessor(target);

			// Log once per unique miss so the Console doesn't drown if the member is genuinely missing
			// and OnGUI fires many times before the developer notices.
			if (s_missLog.Add((type, name)))
				Debug.LogError($"ShowIf: bool member '{name}' not found on {type.Name}");
			return true;
		}

		private static Func<object, bool> GetOrBuildAccessor(Type type, string name)
		{
			var key = (type, name);
			if (s_accessorCache.TryGetValue(key, out var cached))
				return cached;

			var accessor = BuildAccessor(type, name);
			s_accessorCache[key] = accessor;
			return accessor;
		}

		private static Func<object, bool> BuildAccessor(Type type, string name)
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var targetParam = Expression.Parameter(typeof(object), "t");
			var typedTarget = Expression.Convert(targetParam, type);

			var field = type.GetField(name, flags);
			if (field != null && field.FieldType == typeof(bool))
				return Expression.Lambda<Func<object, bool>>(Expression.Field(typedTarget, field), targetParam).Compile();

			var prop = type.GetProperty(name, flags);
			if (prop != null && prop.PropertyType == typeof(bool))
				return Expression.Lambda<Func<object, bool>>(Expression.Property(typedTarget, prop), targetParam).Compile();

			var method = type.GetMethod(name, flags, null, Type.EmptyTypes, null);
			if (method != null && method.ReturnType == typeof(bool))
				return Expression.Lambda<Func<object, bool>>(Expression.Call(typedTarget, method), targetParam).Compile();

			return null;
		}
	}
}
