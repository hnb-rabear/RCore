using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using UnityEditor;
using UnityEngine;

namespace RCore.Common
{
    [Serializable]
    public class Directive
    {
        public Directive() { }
        public Directive(string pName, bool pEnable)
        {
            name = pName;
            pEnable = enabled;
        }
        public string name;
        public bool enabled = true;
        public bool @fixed;
    }

    public class DirectivesWindow : EditorWindow
    {
        protected List<Directive> mDirectives = new List<Directive>();

        private void OnEnable()
        {
            using (var sw = File.AppendText("Assets/Editor/DirectivesCollection.txt")) { }

            mDirectives = LoadDirectives();

            var currentDefines = GetCurrentDefines();
            for (int i = 0; i < currentDefines.Count; i++)
            {
                if (!ContainDefine(currentDefines[i]))
                    mDirectives.Add(new Directive(currentDefines[i], true));
            }

            for (int i = 0; i < mDirectives.Count; i++)
            {
                if (currentDefines.Count > 0)
                {
                    bool isUsed = false;
                    for (int j = 0; j < currentDefines.Count; j++)
                    {
                        if (currentDefines[j] == mDirectives[i].name)
                        {
                            isUsed = true;
                            break;
                        }
                    }
                    mDirectives[i].enabled = isUsed;
                }
                else
                {
                    mDirectives[i].enabled = false;
                }
            }
        }

        private void OnGUI()
        {
            DrawDirectives();
        }

        private void DrawDirectives()
        {
            float windowWidth = position.width - 10;
            EditorHelper.BoxVertical("Projective Directives", () =>
            {
                float w1 = 200, w2 = 50, w3 = 50, w4 = 50;
                w1 = windowWidth * 0.6f;
                w2 = windowWidth * 0.1f;
                w3 = windowWidth * 0.1f;
                w4 = windowWidth * 0.1f;

                if (mDirectives.Count > 0)
                    EditorHelper.BoxHorizontal(() =>
                    {
                        GUILayout.Label("Directive", EditorStyles.boldLabel, GUILayout.Width(w1), GUILayout.Height(30));
                        GUILayout.Label("Enable", EditorStyles.boldLabel, GUILayout.Width(w2));
                        GUILayout.Label("Fixed", EditorStyles.boldLabel, GUILayout.Width(w3));
                        GUILayout.Label("", EditorStyles.boldLabel, GUILayout.Width(w4));
                    });

                for (int i = 0; i < mDirectives.Count; i++)
                {
                    int i1 = i;
                    EditorHelper.BoxHorizontal(() =>
                    {
                        mDirectives[i1].name = EditorHelper.TextField(mDirectives[i1].name, "", 0, (int)w1);
                        mDirectives[i1].enabled = EditorHelper.Toggle(mDirectives[i1].enabled, "", 0, (int)w2);
                        mDirectives[i1].@fixed = EditorHelper.Toggle(mDirectives[i1].@fixed, "", 0, (int)w3);
                        if (!mDirectives[i1].@fixed)
                        {
                            EditorHelper.ButtonColor("X", () =>
                            {
                                mDirectives.RemoveAt(i1);
                            }, Color.red, (int)w4);
                        }
                        else
                            GUILayout.Label("Lock", EditorStyles.boldLabel, GUILayout.Width(w4));
                    });
                }

                //===================================================

                EditorHelper.BoxHorizontal(() =>
                {
                    EditorHelper.ButtonColor("Add Directive", () =>
                    {
                        var lastDirective = mDirectives.LastOrDefault();
                        var newDirective = new Directive();
                        if (lastDirective != null)
                        {
                            newDirective.enabled = lastDirective.enabled;
                        }
                        mDirectives.Add(newDirective);
                        return;
                    }, default);
                    //
                    EditorHelper.ButtonColor("Save", () =>
                    {
                        SaveDirectives(mDirectives);
                    });
                    //
                    EditorHelper.ButtonColor("Apply", () =>
                    {
                        ApplyDirectiveSymbols();
                    }, Color.green);
                    //
                    EditorHelper.ButtonColor("Revert", () =>
                    {
                        mDirectives = LoadDirectives();
                    }, Color.yellow);
                }, default);

            }, default, true, windowWidth);
        }

        private void ApplyDirectiveSymbols()
        {
            string symbols = string.Join(";", mDirectives.Where(d => d.enabled == true).Select(d => d.name).ToArray());
            var taget = EditorUserBuildSettings.selectedBuildTargetGroup;
            PlayerSettings.SetScriptingDefineSymbolsForGroup(taget, symbols);
        }

        private List<Directive> LoadDirectives()
        {
            var directives = new List<Directive>();

            var serializer = new XmlSerializer(typeof(List<Directive>));
			using TextReader reader = new StreamReader("Assets/Editor/DirectivesCollection.txt");
			try
			{
				directives = (List<Directive>)serializer.Deserialize(reader);
			}
			catch
			{
				directives = new List<Directive>();
			}

			return directives;
        }

        private void SaveDirectives(List<Directive> pPirectives)
        {
            var serializer = new XmlSerializer(typeof(List<Directive>));
            using (TextWriter writer = new StreamWriter("Assets/Editor/DirectivesCollection.txt"))
            {
                serializer.Serialize(writer, mDirectives);
            }
            AssetDatabase.Refresh();
        }

        private List<string> GetCurrentDefines()
        {
            string defineStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(EditorUserBuildSettings.selectedBuildTargetGroup);
            var currentDefines = defineStr.Split(';').ToList();
            for (int i = 0; i < currentDefines.Count; i++)
                currentDefines[i] = currentDefines[i].Trim();
            return currentDefines;
        }

        private bool ContainDefine(string pDefine)
        {
            if (string.IsNullOrEmpty(pDefine))
                return true;

            for (int i = 0; i < mDirectives.Count; i++)
            {
                if (mDirectives[i].name == pDefine)
                    return true;
            }
            return false;
        }

        [MenuItem("RCore/Tools/Open Directives Window (obsolete)")]
        private static void OpenDirectivesEditorWindow()
        {
            var window = GetWindow<DirectivesWindow>("Directives Manager", true);
            window.Show();
        }
    }
}