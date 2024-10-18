using RCore.Inspector;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Inspector
{
    [CustomPropertyDrawer(typeof(SingleLayerAttribute))]
    public class SingleLayerPropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // One line of  oxygen free code.
            property.intValue = EditorGUI.LayerField(position, label, property.intValue);
        }
    }
}