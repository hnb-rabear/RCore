#if UNITY_EDITOR
using System;
using System.Reflection;
using UnityEditor;

namespace RCore.Editor
{
    /// <summary>
    /// Provides extension methods for Unity's SerializedObject and SerializedProperty classes
    /// to simplify common operations in custom inspectors.
    /// </summary>
    public static class EditorSerializedPropertyExtensions
    {
        /// <summary>
        /// Draws property fields for multiple properties of a SerializedObject.
        /// </summary>
        public static void DrawFields(this SerializedObject obj, params string[] properties)
        {
            foreach (var p in properties)
                EditorGUILayout.PropertyField(obj.FindProperty(p), true);
        }

        /// <summary>
        /// Gets the actual C# object that a SerializedProperty represents using reflection.
        /// </summary>
        public static object GetTargetObject(this SerializedProperty prop)
        {
            if (prop == null) return null;

            var path = prop.propertyPath.Replace(".Array.data[", "[");
            object obj = prop.serializedObject.targetObject;
            var elements = path.Split('.');
            foreach (var element in elements)
            {
                if (element.Contains("["))
                {
                    var elementName = element.Substring(0, element.IndexOf("[", StringComparison.Ordinal));
                    var index = Convert.ToInt32(element.Substring(element.IndexOf("[", StringComparison.Ordinal)).Replace("[", "").Replace("]", ""));
                    obj = GetValue(obj, elementName, index);
                }
                else
                {
                    obj = GetValue(obj, element);
                }
            }
            return obj;
        }

        private static object GetValue(object source, string name)
        {
            if (source == null) return null;
            var type = source.GetType();
            var field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            if (field != null) return field.GetValue(source);
            var property = type.GetProperty(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (property != null) return property.GetValue(source, null);
            return null;
        }

        private static object GetValue(object source, string name, int index)
        {
            if (GetValue(source, name) is not System.Collections.IEnumerable enumerable) return null;
            var enumerator = enumerable.GetEnumerator();
            for (int i = 0; i <= index; i++)
            {
                if (!enumerator.MoveNext()) return null;
            }
            return enumerator.Current;
        }
    }
}
#endif