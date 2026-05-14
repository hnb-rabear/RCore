using UnityEditor;
using UnityEngine;

namespace RevCore.Tools.Editor
{
    public class ScreenshotTakerWindow : EditorWindow
    {
        private int m_resWidth = 2560;
        private int m_resHeight = 1440;
        private int m_scale = 1;
        private string m_path = "";
        private string m_lastScreenshot = "";
        private bool m_isTransparent;
        private bool m_takeHiResShot;
        private Camera m_camera;

        [MenuItem("RevCore/Tools/Screenshot Taker", priority = 300)]
        public static void Open()
        {
            var window = GetWindow<ScreenshotTakerWindow>("Screenshot Taker");
            window.autoRepaintOnSceneChange = true;
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
            m_resWidth = EditorGUILayout.IntField("Width", m_resWidth);
            m_resHeight = EditorGUILayout.IntField("Height", m_resHeight);

            EditorGUILayout.Space();
            m_scale = EditorGUILayout.IntSlider("Scale", m_scale, 1, 15);
            EditorGUILayout.HelpBox("Scale multiplies resolution without losing quality.", MessageType.None);

            EditorGUILayout.Space();
            GUILayout.Label("Save Path", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.TextField(m_path);
            if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
                m_path = EditorUtility.SaveFolderPanel("Path to Save Images", m_path, Application.dataPath);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();
            GUILayout.Label("Select Camera", EditorStyles.boldLabel);
            m_camera = EditorGUILayout.ObjectField(m_camera, typeof(Camera), true) as Camera;
            if (m_camera == null)
                m_camera = Camera.main;

            if (m_camera == null)
            {
                EditorGUILayout.HelpBox("No camera selected or found.", MessageType.Warning);
                return;
            }

            m_isTransparent = EditorGUILayout.Toggle("Transparent Background", m_isTransparent);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Default Options", EditorStyles.boldLabel);
            if (GUILayout.Button("Set To Screen Size"))
            {
                m_resHeight = (int)Handles.GetMainGameViewSize().y;
                m_resWidth = (int)Handles.GetMainGameViewSize().x;
            }
            if (GUILayout.Button("Default Size"))
            {
                m_resHeight = 1440;
                m_resWidth = 2560;
                m_scale = 1;
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Screenshot: {m_resWidth * m_scale} x {m_resHeight * m_scale} px", EditorStyles.boldLabel);

            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 20 };
            if (GUILayout.Button("Take Screenshot", buttonStyle, GUILayout.MinHeight(60)))
            {
                if (string.IsNullOrEmpty(m_path))
                    m_path = EditorUtility.SaveFolderPanel("Path to Save Images", m_path, Application.dataPath);
                if (!string.IsNullOrEmpty(m_path))
                    m_takeHiResShot = true;
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Open Last Screenshot", GUILayout.MinHeight(30)) && !string.IsNullOrEmpty(m_lastScreenshot))
                Application.OpenURL("file://" + m_lastScreenshot);
            if (GUILayout.Button("Open Folder", GUILayout.MinHeight(30)) && !string.IsNullOrEmpty(m_path))
                Application.OpenURL("file://" + m_path);
            EditorGUILayout.EndHorizontal();

            if (m_takeHiResShot)
            {
                int w = m_resWidth * m_scale;
                int h = m_resHeight * m_scale;
                var rt = new RenderTexture(w, h, 24);
                m_camera.targetTexture = rt;

                var format = m_isTransparent ? TextureFormat.ARGB32 : TextureFormat.RGB24;
                var screenshot = new Texture2D(w, h, format, false);
                m_camera.Render();
                RenderTexture.active = rt;
                screenshot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
                m_camera.targetTexture = null;
                RenderTexture.active = null;

                byte[] bytes = screenshot.EncodeToPNG();
                string filename = $"{m_path}/screen_{w}x{h}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                System.IO.File.WriteAllBytes(filename, bytes);
                m_lastScreenshot = filename;
                Debug.Log($"Screenshot saved: {filename}");

                DestroyImmediate(screenshot);
                rt.Release();
                DestroyImmediate(rt);
                m_takeHiResShot = false;
            }
        }
    }
}
