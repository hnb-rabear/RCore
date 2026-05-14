using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    internal sealed class CharactersSetGeneratorTool : RevCoreTool
    {
        private readonly List<TextAsset> m_assets = new();
        private string m_result = string.Empty;
        private Vector2 m_scroll;

        public override string Name => "Characters Set Generator";
        public override string Category => "Generators";
        public override bool IsQuickAction => true;

        public override void OnGUI()
        {
            EditorGuiHelper.DragDropBox<TextAsset>("Drag TextAssets Here", assets =>
            {
                foreach (TextAsset asset in assets)
                    if (asset != null && !m_assets.Contains(asset))
                        m_assets.Add(asset);
            });

            for (int i = 0; i < m_assets.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                m_assets[i] = (TextAsset)EditorGUILayout.ObjectField(m_assets[i], typeof(TextAsset), false);
                if (EditorGuiHelper.ButtonColor("x", Color.red, 24))
                {
                    m_assets.RemoveAt(i);
                    i--;
                }
                EditorGUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Generate"))
            {
                var chars = new HashSet<char>();
                foreach (TextAsset asset in m_assets)
                    if (asset != null)
                        foreach (char c in asset.text)
                            chars.Add(c);
                m_result = new string(chars.OrderBy(c => c).ToArray());
            }

            m_scroll = EditorGUILayout.BeginScrollView(m_scroll, GUILayout.Height(120));
            m_result = EditorGUILayout.TextArea(m_result, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Save") && !string.IsNullOrEmpty(m_result))
            {
                string path = EditorUtility.SaveFilePanel("Save Characters Set", Application.dataPath, "characters", "txt");
                if (!string.IsNullOrEmpty(path))
                    System.IO.File.WriteAllText(path, m_result);
            }
        }
    }
}
