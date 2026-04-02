using UnityEngine;
using UnityEditor;
using System.IO;

namespace RCore.Editor.Tool
{
	internal class ScreenshotTakerDrawer
	{
		private int m_resWidth = Screen.width * 4;
		private int m_resHeight = Screen.height * 4;
		private Camera m_camera;
		private int m_scale = 1;
		private string m_path = "";
		private bool m_isTransparent;
		private bool m_takeHiResShot;
		private string m_lastScreenshot = "";

		public void DrawOnGUI()
		{
			EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
			m_resWidth = EditorGUILayout.IntField("Width", m_resWidth);
			m_resHeight = EditorGUILayout.IntField("Height", m_resHeight);

			EditorGUILayout.Space();

			m_scale = EditorGUILayout.IntSlider("Scale", m_scale, 1, 15);

			EditorGUILayout.HelpBox(
				"The default mode of screenshot is crop - so choose a proper width and height. The scale is a factor to multiply or enlarge the renders without loosing quality.",
				MessageType.None);

			EditorGUILayout.Space();

			GUILayout.Label("Save Path", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			m_path = EditorGUILayout.TextField(m_path);
			if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
				m_path = EditorUtility.SaveFolderPanel("Path to Save Images", m_path, Application.dataPath);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.HelpBox("Choose the folder in which to save the screenshots", MessageType.None);
			EditorGUILayout.Space();

			GUILayout.Label("Select Camera", EditorStyles.boldLabel);
			m_camera = EditorGUILayout.ObjectField(m_camera, typeof(Camera), true, null) as Camera;
			EditorGUILayout.HelpBox("Choose the camera of which to capture the render", MessageType.None);

			if (m_camera == null)
				m_camera = Camera.main;

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
			EditorGUILayout.LabelField("Screenshot will be taken at " + m_resWidth * m_scale + " x " + m_resHeight * m_scale + " px", EditorStyles.boldLabel);

			var buttonStyle = new GUIStyle(GUI.skin.button);
			buttonStyle.fontSize = 20;
			if (GUILayout.Button("Take Screenshot", buttonStyle, GUILayout.MinHeight(60)))
			{
				if (m_path == "")
				{
					m_path = EditorUtility.SaveFolderPanel("Path to Save Images", m_path, Application.dataPath);
					Debug.Log("Path Set");
				}
				TakeHiResShot();
			}

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Open Last Screenshot", GUILayout.MinHeight(30)))
				if (m_lastScreenshot != "")
				{
					Application.OpenURL("file://" + m_lastScreenshot);
					Debug.Log("Opening File " + m_lastScreenshot);
				}
			if (GUILayout.Button("Open Folder", GUILayout.MinHeight(30)))
				Application.OpenURL("file://" + m_path);
			EditorGUILayout.EndHorizontal();

			if (m_takeHiResShot && m_camera != null)
			{
				int resWidthN = m_resWidth * m_scale;
				int resHeightN = m_resHeight * m_scale;
				var rt = new RenderTexture(resWidthN, resHeightN, 24);
				m_camera.targetTexture = rt;

				var tFormat = m_isTransparent ? TextureFormat.ARGB32 : TextureFormat.RGB24;
				var screenShot = new Texture2D(resWidthN, resHeightN, tFormat, false);
				m_camera.Render();
				RenderTexture.active = rt;
				screenShot.ReadPixels(new Rect(0, 0, resWidthN, resHeightN), 0, 0);
				m_camera.targetTexture = null;
				RenderTexture.active = null;
				byte[] bytes = screenShot.EncodeToPNG();
				string filename = $"{m_path}/screen_{resWidthN}x{resHeightN}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
				m_lastScreenshot = filename;

				File.WriteAllBytes(filename, bytes);
				Debug.Log($"Took screenshot to: {filename}");
				Application.OpenURL(filename);
				m_takeHiResShot = false;
			}
		}

		private void TakeHiResShot()
		{
			Debug.Log("Taking Screenshot");
			m_takeHiResShot = true;
		}
	}
}
