using System;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;
using UnityEngine.UI;

namespace RCore.Editor
{
    public abstract class AssetPathPreviewEditorBase : UnityEditor.Editor
    {
        private UnityEditor.Editor m_DefaultEditor;
        private Type m_DefaultEditorType;

        protected abstract string DefaultEditorTypeName { get; }
        protected abstract string GetPathLabel();
        protected virtual void DrawExtraInfo() { }

        protected void OnEnable()
        {
            m_DefaultEditorType = Type.GetType(DefaultEditorTypeName);
            CreateDefaultEditor();
        }

        protected void OnDisable()
        {
            if (m_DefaultEditor != null)
                DestroyImmediate(m_DefaultEditor);
        }

        public override void OnInspectorGUI()
        {
            CreateDefaultEditor();

            if (m_DefaultEditor != null)
                m_DefaultEditor.OnInspectorGUI();
            else
                base.OnInspectorGUI();

            DrawPathInfo();
        }

        public override bool HasPreviewGUI()
        {
            CreateDefaultEditor();
            return m_DefaultEditor != null ? m_DefaultEditor.HasPreviewGUI() : base.HasPreviewGUI();
        }

        public override GUIContent GetPreviewTitle()
        {
            CreateDefaultEditor();
            return m_DefaultEditor != null ? m_DefaultEditor.GetPreviewTitle() : base.GetPreviewTitle();
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            CreateDefaultEditor();

            if (m_DefaultEditor != null)
                m_DefaultEditor.OnPreviewGUI(r, background);
            else
                base.OnPreviewGUI(r, background);

            DrawPreviewPathOverlay(r);
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            CreateDefaultEditor();

            if (m_DefaultEditor != null)
                m_DefaultEditor.OnInteractivePreviewGUI(r, background);
            else
                base.OnInteractivePreviewGUI(r, background);

            DrawPreviewPathOverlay(r);
        }

        public override void OnPreviewSettings()
        {
            CreateDefaultEditor();
            if (m_DefaultEditor != null)
                m_DefaultEditor.OnPreviewSettings();
            else
                base.OnPreviewSettings();
        }

        private void CreateDefaultEditor()
        {
            if (m_DefaultEditor != null || m_DefaultEditorType == null)
                return;

            CreateCachedEditor(targets, m_DefaultEditorType, ref m_DefaultEditor);
        }

        private void DrawPathInfo()
        {
            if (targets == null || targets.Length != 1)
                return;

            var path = GetPathLabel();
            if (string.IsNullOrEmpty(path))
                return;

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("Asset Path", EditorStyles.boldLabel);
            DrawSelectableLabel("Path", path);
            DrawExtraInfo();
        }

        protected static void DrawSelectableLabel(string label, string value)
        {
            if (string.IsNullOrEmpty(value))
                value = "-";

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel(label);
            EditorGUILayout.SelectableLabel(value, EditorStyles.textField, GUILayout.Height(EditorGUIUtility.singleLineHeight));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawPreviewPathOverlay(Rect previewRect)
        {
            if (targets == null || targets.Length != 1)
                return;

            var label = GetPathLabel();
            if (string.IsNullOrEmpty(label))
                return;

            var overlayRect = new Rect(previewRect.x + 4f, previewRect.y + 4f, previewRect.width - 8f, 44f);

            var style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Clip,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            var shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = Color.black;
            style.normal.textColor = Color.white;

            var shadowRect = new Rect(overlayRect.x + 1f, overlayRect.y + 1f, overlayRect.width, overlayRect.height);
            GUI.Label(shadowRect, label, shadowStyle);
            GUI.Label(overlayRect, label, style);
        }
    }

    [CustomEditor(typeof(TextureImporter))]
    [CanEditMultipleObjects]
    public class TextureImporterPathPreviewEditor : AssetPathPreviewEditorBase
    {
        protected override string DefaultEditorTypeName => "UnityEditor.TextureImporterInspector, UnityEditor";

        protected override string GetPathLabel()
        {
            return AssetDatabase.GetAssetPath(target);
        }
    }

    [CustomEditor(typeof(Sprite))]
    [CanEditMultipleObjects]
    public class SpritePathPreviewEditor : AssetPathPreviewEditorBase
    {
        protected override string DefaultEditorTypeName => "UnityEditor.SpriteInspector, UnityEditor";

        protected override string GetPathLabel()
        {
            var sprite = target as Sprite;
            if (sprite == null)
                return null;

            var path = AssetDatabase.GetAssetPath(sprite);
            return string.IsNullOrEmpty(path) ? null : $"{path}/{sprite.name}";
        }

        protected override void DrawExtraInfo()
        {
            var sprite = target as Sprite;
            if (sprite == null)
                return;

            DrawSelectableLabel("Name", sprite.name);
            DrawSelectableLabel("Size", $"{sprite.rect.width:0.#} x {sprite.rect.height:0.#}");

            if (sprite.texture != null)
                DrawSelectableLabel("Texture", AssetDatabase.GetAssetPath(sprite.texture));
        }
    }

    [CustomEditor(typeof(Image))]
    [CanEditMultipleObjects]
    public class ImagePathPreviewEditor : ImageEditor
    {
        private Image m_Target;

        protected override void OnEnable()
        {
            base.OnEnable();
            m_Target = target as Image;
        }

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            base.OnPreviewGUI(r, background);
            DrawPreviewPathOverlay(r);
        }

        public override void OnInteractivePreviewGUI(Rect r, GUIStyle background)
        {
            base.OnInteractivePreviewGUI(r, background);
            DrawPreviewPathOverlay(r);
        }

        private static string GetPathLabel(Sprite sprite)
        {
            if (sprite == null)
                return null;

            var path = AssetDatabase.GetAssetPath(sprite);
            if (string.IsNullOrEmpty(path))
                return null;

            return string.IsNullOrEmpty(sprite.name) ? path : $"{path}/{sprite.name}";
        }

        private void DrawPreviewPathOverlay(Rect previewRect)
        {
            var sprite = m_Target != null ? m_Target.sprite : null;
            var label = GetPathLabel(sprite);
            if (string.IsNullOrEmpty(label))
                return;

            var overlayRect = new Rect(previewRect.x + 4f, previewRect.y + 4f, previewRect.width - 8f, 44f);
            var style = new GUIStyle(EditorStyles.whiteLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                clipping = TextClipping.Clip,
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            var shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = Color.black;
            style.normal.textColor = Color.white;

            var shadowRect = new Rect(overlayRect.x + 1f, overlayRect.y + 1f, overlayRect.width, overlayRect.height);
            GUI.Label(shadowRect, label, shadowStyle);
            GUI.Label(overlayRect, label, style);
        }
    }
}
