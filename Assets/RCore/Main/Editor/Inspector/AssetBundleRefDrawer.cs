#if ADDRESSABLES
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Inspector
{
	[CustomPropertyDrawer(typeof(AssetBundleRef<>), true)]
	public class AssetBundleRefDrawer : PropertyDrawer
	{
		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			var referenceProperty = property.FindPropertyRelative("reference");
			EditorGUI.PropertyField(position, referenceProperty, label, true);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var referenceProperty = property.FindPropertyRelative("reference");
			return EditorGUI.GetPropertyHeight(referenceProperty, label, true);
		}
	}
}
#endif