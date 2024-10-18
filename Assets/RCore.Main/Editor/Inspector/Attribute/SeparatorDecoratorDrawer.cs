using RCore.Inspector;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Inspector
{
    [CustomPropertyDrawer(typeof(SeparatorAttribute))]
    public class SeparatorDecoratorDrawer : DecoratorDrawer
    {
        SeparatorAttribute separatorAttribute => (SeparatorAttribute)attribute;

        public override void OnGUI(Rect _position)
        {
            if (separatorAttribute.title == "")
            {
                _position.height = 1;
                _position.y += 9;
                GUI.Box(_position, "");
            }
            else
            {
                Vector2 textSize = GUI.skin.label.CalcSize(new GUIContent(separatorAttribute.title));
                float separatorWidth = (_position.width - textSize.x) / 2.0f - 5.0f;
                _position.y += 9;

                GUI.Box(new Rect(_position.xMin, _position.yMin, separatorWidth, 1), "");
                GUI.Label(new Rect(_position.xMin + separatorWidth + 5.0f, _position.yMin - 8.0f, textSize.x, 20), separatorAttribute.title);
                GUI.Box(new Rect(_position.xMin + separatorWidth + 10.0f + textSize.x, _position.yMin, separatorWidth, 1), "");
            }
        }

        public override float GetHeight()
        {
            return 20;
        }
    }
}