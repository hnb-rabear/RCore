using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	[ExecuteInEditMode]
	public class ScreenshotTaker : EditorWindow
	{
		private int m_resWidth = Screen.width * 4;
		private int m_resHeight = Screen.height * 4;

		public Camera myCamera;
		private int m_scale = 1;

		private string m_path = "";
		//bool showPreview = true;
		private RenderTexture m_renderTexture;

		private bool m_isTransparent;

		public static void ShowWindow()
		{
			var editorWindow = GetWindow(typeof(ScreenshotTaker));
			editorWindow.autoRepaintOnSceneChange = true;
			editorWindow.Show();
			editorWindow.titleContent = new GUIContent("Screenshot");
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Resolution", EditorStyles.boldLabel);
			m_resWidth = EditorGUILayout.IntField("Width", m_resWidth);
			m_resHeight = EditorGUILayout.IntField("Height", m_resHeight);

			EditorGUILayout.Space();

			m_scale = EditorGUILayout.IntSlider("Scale", m_scale, 1, 15);

			EditorGUILayout.HelpBox(
				"The default mode of screenshot is crop - so choose a proper width and height. The scale is a factor " + "to multiply or enlarge the renders without loosing quality.",
				MessageType.None);

			EditorGUILayout.Space();

			GUILayout.Label("Save Path", EditorStyles.boldLabel);

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.TextField(m_path, GUILayout.ExpandWidth(false));
			if (GUILayout.Button("Browse", GUILayout.ExpandWidth(false)))
				m_path = EditorUtility.SaveFolderPanel("Path to Save Images", m_path, Application.dataPath);

			EditorGUILayout.EndHorizontal();

			EditorGUILayout.HelpBox("Choose the folder in which to save the screenshots ", MessageType.None);
			EditorGUILayout.Space();

			GUILayout.Label("Select Camera", EditorStyles.boldLabel);

			myCamera = EditorGUILayout.ObjectField(myCamera, typeof(Camera), true, null) as Camera;

			if (myCamera == null)
				myCamera = Camera.main;

			m_isTransparent = EditorGUILayout.Toggle("Transparent Background", m_isTransparent);

			EditorGUILayout.HelpBox("Choose the camera of which to capture the render. You can make the background transparent using the transparency option.", MessageType.None);

			EditorGUILayout.Space();
			EditorGUILayout.BeginVertical();
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

			EditorGUILayout.EndVertical();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Screenshot will be taken at " + m_resWidth * m_scale + " x " + m_resHeight * m_scale + " px", EditorStyles.boldLabel);

			if (GUILayout.Button("Take Screenshot", GUILayout.MinHeight(60)))
			{
				if (m_path == "")
				{
					m_path = EditorUtility.SaveFolderPanel("Path to Save Images", m_path, Application.dataPath);
					Debug.Log("Path Set");
					TakeHiResShot();
				}
				else
				{
					TakeHiResShot();
				}
			}

			EditorGUILayout.Space();
			EditorGUILayout.BeginHorizontal();

			if (GUILayout.Button("Open Last Screenshot", GUILayout.MaxWidth(160), GUILayout.MinHeight(40)))
				if (lastScreenshot != "")
				{
					Application.OpenURL("file://" + lastScreenshot);
					Debug.Log("Opening File " + lastScreenshot);
				}

			if (GUILayout.Button("Open Folder", GUILayout.MaxWidth(100), GUILayout.MinHeight(40)))
				Application.OpenURL("file://" + m_path);

			EditorGUILayout.EndHorizontal();

			if (m_takeHiResShot)
			{
				int resWidthN = m_resWidth * m_scale;
				int resHeightN = m_resHeight * m_scale;
				var rt = new RenderTexture(resWidthN, resHeightN, 24);
				myCamera.targetTexture = rt;

				var tFormat = m_isTransparent ? TextureFormat.ARGB32 : TextureFormat.RGB24;

				var screenShot = new Texture2D(resWidthN, resHeightN, tFormat, false);
				myCamera.Render();
				RenderTexture.active = rt;
				screenShot.ReadPixels(new Rect(0, 0, resWidthN, resHeightN), 0, 0);
				myCamera.targetTexture = null;
				RenderTexture.active = null;
				byte[] bytes = screenShot.EncodeToPNG();
				string filename = ScreenShotName(resWidthN, resHeightN);

				System.IO.File.WriteAllBytes(filename, bytes);
				Debug.Log($"Took screenshot to: {filename}");
				Application.OpenURL(filename);
				m_takeHiResShot = false;
			}

			EditorGUILayout.HelpBox("In case of any error, make sure you have Unity Pro as the plugin requires Unity Pro to work.", MessageType.Info);
		}


		private bool m_takeHiResShot;
		public string lastScreenshot = "";


		public string ScreenShotName(int width, int height)
		{
			string strPath = "";

			strPath = $"{m_path}/screen_{width}x{height}_{System.DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
			lastScreenshot = strPath;

			return strPath;
		}

		public void TakeHiResShot()
		{
			Debug.Log("Taking Screenshot");
			m_takeHiResShot = true;
		}
	}
}