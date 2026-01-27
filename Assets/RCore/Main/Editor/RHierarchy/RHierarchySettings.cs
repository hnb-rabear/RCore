using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace RCore.Editor.RHierarchy
{
    public static class RHierarchySettings
    {
        // Toggles
        public static bool IsEnable { get { return EditorPrefs.GetBool("RHierarchy_Enable", true); } set { EditorPrefs.SetBool("RHierarchy_Enable", value); } }
        public static bool IsSeparatorEnabled { get { return EditorPrefs.GetBool("RHierarchy_Separator", true); } set { EditorPrefs.SetBool("RHierarchy_Separator", value); } }
        public static bool IsVisibilityEnabled { get { return EditorPrefs.GetBool("RHierarchy_Visibility", true); } set { EditorPrefs.SetBool("RHierarchy_Visibility", value); } }
        public static bool IsTagEnabled { get { return EditorPrefs.GetBool("RHierarchy_Tag", false); } set { EditorPrefs.SetBool("RHierarchy_Tag", value); } }
        public static bool IsLayerEnabled { get { return EditorPrefs.GetBool("RHierarchy_Layer", false); } set { EditorPrefs.SetBool("RHierarchy_Layer", value); } }
        public static bool IsStaticEnabled { get { return EditorPrefs.GetBool("RHierarchy_Static", false); } set { EditorPrefs.SetBool("RHierarchy_Static", value); } }
        public static bool IsChildrenCountEnabled { get { return EditorPrefs.GetBool("RHierarchy_ChildCount", true); } set { EditorPrefs.SetBool("RHierarchy_ChildCount", value); } }
        public static bool IsComponentsEnabled { get { return EditorPrefs.GetBool("RHierarchy_Components", true); } set { EditorPrefs.SetBool("RHierarchy_Components", value); } }
        public static bool IsVerticesEnabled { get { return EditorPrefs.GetBool("RHierarchy_Vertices", false); } set { EditorPrefs.SetBool("RHierarchy_Vertices", value); } }

        // Separator
        public static bool ShowRowShading = true;
        public static bool ShowSeparatorLine = true;
        public static Color EvenRowColor = new Color(0.1f, 0.1f, 0.1f, 0.1f);
        public static Color OddRowColor = new Color(0, 0, 0, 0);
        public static Color SeparatorColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

        // Visibility
        public static Color ActiveColor = Color.yellow;
        public static Color InactiveColor = Color.white;

        // TagLayer
        public static bool ShowAlwaysTagLayer = false;
        public static Color TagLabelColor = Color.gray;
        public static Color LayerLabelColor = Color.gray;

        // Static
        public static Color StaticLabelColor = Color.gray;

        // Children Count
        public static Color ChildrenCountLabelColor = Color.gray;
        
        // Vertices
        public static Color VerticesLabelColor = Color.gray;
        // Order
        public enum RComponentType
        {
            Visibility,
            Tag,
            Layer,
            Static,
            Components,
            ChildrenCount,
            Vertices
        }

        private static List<RComponentType> m_ComponentOrder;
        public static List<RComponentType> ComponentOrder
        {
            get
            {
                if (m_ComponentOrder == null)
                {
                    string orderStr = EditorPrefs.GetString("RHierarchy_Order", "");
                    if (string.IsNullOrEmpty(orderStr))
                    {
                        m_ComponentOrder = new List<RComponentType>
                        {
                            RComponentType.Visibility,
                            RComponentType.Vertices,
                            RComponentType.Components,
                            RComponentType.Static,
                            RComponentType.ChildrenCount,
                            RComponentType.Tag,
                            RComponentType.Layer
                        };
                    }
                    else
                    {
                        m_ComponentOrder = new List<RComponentType>();
                        string[] split = orderStr.Split(',');
                        foreach (var s in split)
                        {
                            if (s == "TagLayer") // Migration
                            {
                                m_ComponentOrder.Add(RComponentType.Tag);
                                m_ComponentOrder.Add(RComponentType.Layer);
                            }
                            else if (System.Enum.TryParse(s, out RComponentType type))
                            {
                                m_ComponentOrder.Add(type);
                            }
                        }
                        // Ensure all types are present (in case of updates)
                        foreach (RComponentType type in System.Enum.GetValues(typeof(RComponentType)))
                        {
                            if (!m_ComponentOrder.Contains(type)) m_ComponentOrder.Add(type);
                        }
                    }
                }
                return m_ComponentOrder;
            }
            set
            {
                m_ComponentOrder = value;
                string orderStr = string.Join(",", m_ComponentOrder);
                EditorPrefs.SetString("RHierarchy_Order", orderStr);
            }
        }
    }
}
