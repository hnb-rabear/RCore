using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
#endif
using UnityEngine;

namespace RCore.Common
{
    public static class Env
    {
        public static readonly string EnvPath = Path.Combine(Application.dataPath, "../", $".env");
        public static readonly string ResourcesDirPath = Path.Combine(Application.dataPath, "Resources");
        public static readonly string BuiltInEnvPath = Path.Combine(ResourcesDirPath, $"env.txt");

        private static Dictionary<string, string> variables;
        private static Dictionary<string, string> builtInVariables;

        static Env()
        {
            Load();
        }

        public static void LogEnv()
        {
            if (variables != null)
                foreach (var variable in variables)
                    UnityEngine.Debug.Log($"[.env] {variable.Key} {variable.Value}");

            if (builtInVariables != null)
                foreach (var variable in builtInVariables)
                    UnityEngine.Debug.Log($"[builtIn env]{variable.Key} {variable.Value}");
        }

        public static Dictionary<string, string> ParseEnvironmentFile(string contents)
        {
            return contents.Trim().Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("#") && l.IndexOf("=", StringComparison.Ordinal) != -1)
                .ToDictionary(l => l.Substring(0, l.IndexOf("=", StringComparison.Ordinal)).Trim().ToUpper(), l => l.Substring(l.IndexOf("=", StringComparison.Ordinal) + 1).Trim().Trim('"', '\''));
        }

        public static bool TryParse(string key, out string value)
        {
            value = "";
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            value = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(value) && variables != null && variables.TryGetValue(key, out string variable))
            {
                value = variable;
                return true;
            }
#endif
            if (string.IsNullOrEmpty(value) && builtInVariables != null && builtInVariables.TryGetValue(key, out string inVariable))
            {
                value = inVariable;
                return true;
            }
            return !string.IsNullOrEmpty(value);
        }

        public static bool TryParse(string key, out bool value)
        {
            value = false;
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            if (bool.TryParse(Environment.GetEnvironmentVariable(key), out value))
            {
                return true;
            }
            if (variables != null && variables.ContainsKey(key) && bool.TryParse(variables[key], out value))
            {
                return true;
            }
#endif
            return builtInVariables != null && builtInVariables.ContainsKey(key) && bool.TryParse(builtInVariables[key], out value);
		}

        public static bool TryParse(string key, out double value)
        {
            value = 0;
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            if (double.TryParse(Environment.GetEnvironmentVariable(key), out value))
            {
                return true;
            }
            if (variables != null && variables.ContainsKey(key) && double.TryParse(variables[key], out value))
            {
                return true;
            }
#endif
            return builtInVariables != null && builtInVariables.ContainsKey(key) && double.TryParse(builtInVariables[key], out value);
		}

        public static bool TryParse(string key, out float value)
        {
            value = 0;
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            if (float.TryParse(Environment.GetEnvironmentVariable(key), out value))
            {
                return true;
            }
            if (variables != null && variables.ContainsKey(key) && float.TryParse(variables[key], out value))
            {
                return true;
            }
#endif
            return builtInVariables != null && builtInVariables.ContainsKey(key) && float.TryParse(builtInVariables[key], out value);
		}

        public static bool TryParse(string key, out int value)
        {
            value = 0;
#if UNITY_SERVER || UNITY_EDITOR || DEVELOPMENT
            key = key.ToUpper();
            if (int.TryParse(Environment.GetEnvironmentVariable(key), out value))
            {
                return true;
            }
            if (variables != null && variables.ContainsKey(key) && int.TryParse(variables[key], out value))
            {
                return true;
            }
#endif
            return builtInVariables != null && builtInVariables.ContainsKey(key) && int.TryParse(builtInVariables[key], out value);
		}

        private static void SetSystemVariable(string key, object value)
        {
            var systemValue = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(systemValue))
                Environment.SetEnvironmentVariable(key, value.ToString());
        }

        public static void Load()
        {
            variables = new Dictionary<string, string>();
            var envPath = Path.Combine(Application.dataPath, "../", $".env");
            if (File.Exists(envPath))
            {
                var content = File.ReadAllText(envPath, Encoding.UTF8);
                variables = ParseEnvironmentFile(content);
            }

            var envTextAsset = Resources.Load<TextAsset>($"env");
            if (envTextAsset != null)
                builtInVariables = ParseEnvironmentFile(envTextAsset.text);
#if UNITY_EDITOR || DEVELOPMENT
            LogEnv();
#endif
        }
    }

    //===============================================================================

#if UNITY_EDITOR
    public class EnvBuilder : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder => 1;

        public void OnPostprocessBuild(BuildReport report)
        {
			FileUtil.DeleteFileOrDirectory(Env.BuiltInEnvPath);
        }

        public void OnPreprocessBuild(BuildReport report)
		{
			if (!File.Exists(Env.EnvPath))
				return;

			if (!Directory.Exists(Env.ResourcesDirPath))
				Directory.CreateDirectory(Env.ResourcesDirPath);

			FileUtil.ReplaceFile(Env.EnvPath, Env.BuiltInEnvPath);
			Env.Load();
		}
    }

    public class EnvironmentFileEditor : EditorWindow
    {
        private Vector2 m_ScrollPosition;
        private List<Tuple<string, string>> m_TempConfig = new List<Tuple<string, string>>();
        private bool m_HadEnvFile;

        private void OnEnable()
        {
            m_HadEnvFile = File.Exists(Env.EnvPath);

            if (m_HadEnvFile)
                LoadEnvironmentFile();
        }

        private void Update()
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                Repaint();
        }

        private void OnGUI()
        {
            if (!m_HadEnvFile)
            {
				if (!GUILayout.Button("Create .env File"))
					return;

				try
				{
					File.WriteAllText(Env.EnvPath, SerializeEnvDictionary(new Dictionary<string, string> { { "VARIABLE_NAME", "VALUE" } }));
					m_HadEnvFile = true;
					LoadEnvironmentFile();
				}
				catch (Exception err)
				{
					EditorUtility.DisplayDialog("Error", err.Message, "Ok");
				}
				return;
            }

            m_ScrollPosition = GUILayout.BeginScrollView(m_ScrollPosition);

            for (var i = 0; i < m_TempConfig.Count; i += 1)
            {
                GUILayout.BeginHorizontal();

                var key = GUILayout.TextField(m_TempConfig[i].Item1, GUILayout.ExpandWidth(false), GUILayout.Width(200));
                var value = GUILayout.TextField(m_TempConfig[i].Item2, GUILayout.Width(position.width - 260));

                if (m_TempConfig.Count == 1)
                    GUI.enabled = false;

                if (GUILayout.Button("-", GUILayout.Width(23)))
                {
                    m_TempConfig.RemoveAt(i);
                    continue;
                }

                if (m_TempConfig.Count == 1)
                    GUI.enabled = true;

                if (GUILayout.Button("+", GUILayout.Width(23)))
                {
                    m_TempConfig.Insert(i + 1, new Tuple<string, string>("", ""));
                    continue;
                }

                GUILayout.EndHorizontal();

                if (!key.Equals(m_TempConfig[i].Item1) || !value.Equals(m_TempConfig[i].Item2))
                    m_TempConfig[i] = new Tuple<string, string>(key, value);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Changes"))
            {
                try
                {
                    File.WriteAllText(Env.EnvPath, SerializeEnvDictionary(m_TempConfig.ToDictionary(item => item.Item1, item => item.Item2)));
                }
                catch (Exception err)
                {
                    EditorUtility.DisplayDialog("Error", err.Message, "Ok");
                }
            }

            if (GUILayout.Button("Reload"))
                LoadEnvironmentFile();

            if (GUILayout.Button("Delete .env File"))
            {
                if (EditorUtility.DisplayDialog("Confirm", $"Delete {Env.EnvPath}?", "Ok", "Cancel"))
                    File.Delete(Env.EnvPath);
            }

            if (GUILayout.Button("Open Folder"))
            {
                System.Diagnostics.Process.Start(Path.Combine(Application.dataPath, "../"));
            }

            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        private void LoadEnvironmentFile()
		{
			if (!File.Exists(Env.EnvPath))
				return;

			var content = File.ReadAllText(Env.EnvPath, Encoding.UTF8);
			m_TempConfig = Env.ParseEnvironmentFile(content).Select(item => new Tuple<string, string>(item.Key, item.Value)).ToList();
		}

		private static string SerializeEnvDictionary(Dictionary<string, string> dictionary)
        {
            if (dictionary.Any(item => string.IsNullOrEmpty(item.Key)))
                throw new Exception("One or more keys are missing! Please fix and try again.");

            return string.Join(Environment.NewLine, dictionary.Select(item => $"{item.Key}={item.Value}"));
        }

        [MenuItem("RCore/Tools/ENV Manager")]
        public static void ShowWindow()
        {
            GetWindow(typeof(EnvironmentFileEditor), false, "ENV Manager", true);
        }
    }
#endif
}