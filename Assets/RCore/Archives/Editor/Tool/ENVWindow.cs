using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RCore.Editor.Tool
{
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

    public class ENVWindow : EditorWindow
    {
        private Vector2 m_scrollPosition;
        private List<Tuple<string, string>> m_tempConfig = new List<Tuple<string, string>>();
        private bool m_hadEnvFile;

        private void OnEnable()
        {
            m_hadEnvFile = File.Exists(Env.EnvPath);

            if (m_hadEnvFile)
                LoadEnvironmentFile();
        }

        private void Update()
        {
            if (EditorApplication.isPlaying && !EditorApplication.isPaused)
                Repaint();
        }

        private void OnGUI()
        {
            if (!m_hadEnvFile)
            {
                if (!GUILayout.Button("Create .env File"))
                    return;

                try
                {
                    File.WriteAllText(Env.EnvPath, SerializeEnvDictionary(new Dictionary<string, string> { { "VARIABLE_NAME", "VALUE" } }));
                    m_hadEnvFile = true;
                    LoadEnvironmentFile();
                }
                catch (Exception err)
                {
                    EditorUtility.DisplayDialog("Error", err.Message, "Ok");
                }
                return;
            }

            m_scrollPosition = GUILayout.BeginScrollView(m_scrollPosition);

            for (var i = 0; i < m_tempConfig.Count; i += 1)
            {
                GUILayout.BeginHorizontal();

                var key = GUILayout.TextField(m_tempConfig[i].Item1, GUILayout.ExpandWidth(false), GUILayout.Width(200));
                var value = GUILayout.TextField(m_tempConfig[i].Item2, GUILayout.Width(position.width - 260));

                if (m_tempConfig.Count == 1)
                    GUI.enabled = false;

                if (GUILayout.Button("-", GUILayout.Width(23)))
                {
                    m_tempConfig.RemoveAt(i);
                    continue;
                }

                if (m_tempConfig.Count == 1)
                    GUI.enabled = true;

                if (GUILayout.Button("+", GUILayout.Width(23)))
                {
                    m_tempConfig.Insert(i + 1, new Tuple<string, string>("", ""));
                    continue;
                }

                GUILayout.EndHorizontal();

                if (!key.Equals(m_tempConfig[i].Item1) || !value.Equals(m_tempConfig[i].Item2))
                    m_tempConfig[i] = new Tuple<string, string>(key, value);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Save Changes"))
            {
                try
                {
                    File.WriteAllText(Env.EnvPath, SerializeEnvDictionary(m_tempConfig.ToDictionary(item => item.Item1, item => item.Item2)));
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
            m_tempConfig = Env.ParseEnvironmentFile(content).Select(item => new Tuple<string, string>(item.Key, item.Value)).ToList();
        }

        private static string SerializeEnvDictionary(Dictionary<string, string> dictionary)
        {
            if (dictionary.Any(item => string.IsNullOrEmpty(item.Key)))
                throw new Exception("One or more keys are missing! Please fix and try again.");

            return string.Join(Environment.NewLine, dictionary.Select(item => $"{item.Key}={item.Value}"));
        }

        public static void ShowWindow()
        {
            GetWindow(typeof(ENVWindow), false, ".env Editor", true);
        }
    }
}