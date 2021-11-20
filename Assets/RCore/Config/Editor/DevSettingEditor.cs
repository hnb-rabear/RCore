/**
 * Author NBear - Nguyen Ba Hung - nbhung71711@gmail.com - 2019
 **/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using RCore.Common;
using RCore.Editor;


using Debug = RCore.Common.Debug;

[CustomEditor(typeof(DevSetting))]
public class DevSettingEditor : Editor
{
    private DevSetting mScript;

    //-- FIREBASE CONFIGURATION
    private static string FirebaseDevConfigPath
    {
        get { return EditorPrefs.GetString("firebaseDevConfigPath"); }
        set { EditorPrefs.SetString("firebaseDevConfigPath", value); }
    }
    private static string FirebaseLiveConfigPath
    {
        get { return EditorPrefs.GetString("firebaseLiveConfigPath"); }
        set { EditorPrefs.SetString("firebaseLiveConfigPath", value); }
    }
    public static string FirebaseConfigOutputFolder
    {
        get { return EditorPrefs.GetString("FirebaseConfigOutputFolder", Application.dataPath); }
        set { EditorPrefs.SetString("FirebaseConfigOutputFolder", value); }
    }
    private static string FirebaseConfig
    {
        get { return EditorPrefs.GetString("firebase_config_" + Application.productName); }
        set { EditorPrefs.SetString("firebase_config_" + Application.productName, value); }
    }
    private static string FirebaseProjectNumber
    {
        get { return EditorPrefs.GetString("project_number"); }
        set { EditorPrefs.SetString("project_number", value); }
    }

    private string mTypedProfileName;
    private string mSelectedProfile;
    private ProfilesCollection mProfileCollections;
    private bool mRemovingProfile;
    private bool mPreviewingProfiles;
    private Dictionary<string, ReorderableList> mReorderDirectivesDict = new Dictionary<string, ReorderableList>();

    private void OnEnable()
    {
        mScript = DevSetting.Instance;
        if (mProfileCollections == null)
            mProfileCollections = ProfilesCollection.LoadOrCreateCollection();
        mRemovingProfile = false;
        mSelectedProfile = mScript.profile.name;
        mTypedProfileName = mScript.profile.name;

        mRemovingProfile = false;
        mPreviewingProfiles = false;

        InitDirectives(mScript.profile.defines);

        CheckFirebaseConfigPaths();
    }

    public override void OnInspectorGUI()
    {
        var tab = EditorHelper.Tabs("dev_setting_tabs", "Default", "Custom", "Firebases");
        GUILayout.Space(5);
        switch (tab)
        {
            case "Default":
                LoadDefault();
                break;
            case "Custom":
                DrawSettingsProfiles();
                break;
            case "Firebases":
                DrawFirebaseConfiguration();
                break;
            default:
                LoadDefault();
                break;
        }
        if (tab != "Custom")
        {
            mRemovingProfile = false;
            mPreviewingProfiles = false;
        }
        GUILayout.Space(10);
        EditorHelper.BoxVertical(() =>
        {
            EditorHelper.ButtonColor("Save", () =>
            {
                AssetDatabase.SaveAssets();
            }, Color.green);
            EditorHelper.ButtonColor("Builder", () =>
            {
                var window = EditorWindow.GetWindow<BuilderWindow>("Builder Settings", true);
                window.Show();
            });
        }, Color.white, true);

        if (GUI.changed)
        {
            EditorUtility.SetDirty(mScript);
            EditorUtility.SetDirty(mProfileCollections);
        }
    }

    private void LoadDefault()
    {
        EditorHelper.BoxVertical(() =>
        {
            EditorGUI.indentLevel++;
            base.OnInspectorGUI();
            EditorGUI.indentLevel--;
        }, default(Color), true);
    }

    //========= SETTINGS PROFILE

    private void InitDirectives(List<DevSetting.Directive> defines)
    {
        string[] currentDefines = EditorHelper.GetDirectives();
        for (int i = 0; i < currentDefines.Length; i++)
        {
            if (!ContainDirective(defines, currentDefines[i]))
                defines.Add(new DevSetting.Directive(currentDefines[i], true));
        }

        for (int i = 0; i < defines.Count; i++)
        {
            if (currentDefines.Length > 0)
            {
                bool exist = false;
                for (int j = 0; j < currentDefines.Length; j++)
                {
                    if (currentDefines[j] == defines[i].name)
                        exist = true;
                }
                defines[i].enabled = exist;
            }
            else
                defines[i].enabled = false;
        }
    }

    private void DrawSettingsProfiles()
    {
        EditorHelper.BoxVertical("Project Settings" + (mPreviewingProfiles ? " Preview" : ""), () =>
        {
            if (!mPreviewingProfiles)
            {
                DrawSettingsProfile(mScript.profile);
                GUILayout.Space(10);
                DrawProfilesSelection();
            }
            else
                DrawPreviewSettingsProfiles();
        }, Color.white, true);
    }

    private void DrawSettingsProfile(DevSetting.Profile pProfile)
    {
        EditorHelper.BoxVertical(() =>
        {
            if (EditorHelper.HeaderFoldout("General", pProfile.name + "General"))
            {
                EditorGUILayout.LabelField(pProfile.name, GUIStyleHelper.HeaderTitle);
                EditorGUILayout.LabelField("- Test Settings", EditorStyles.boldLabel);
                pProfile.enableLog = EditorHelper.Toggle(pProfile.enableLog, "Show Log", 120, 280);
                pProfile.enableDraw = EditorHelper.Toggle(pProfile.enableDraw, "Enable Draw", 120, 280);
                pProfile.showFPS = EditorHelper.Toggle(pProfile.showFPS, "Show FPS", 120, 280);
            }
        }, ColorHelper.DarkGreen, true);
        if (pProfile.defines != null)
        {
            EditorHelper.BoxVertical(() =>
            {
                bool directivesChanged = false;
                var draws = new IDraw[2];
                var btnAdd = new EditorButton()
                {
                    onPressed = () => { pProfile.defines.Add(new DevSetting.Directive()); },
                    label = "+1",
                    width = 24,
                };
                draws[0] = btnAdd;
                var btnApply = new EditorButton()
                {
                    onPressed = () => { ApplyDirectives(pProfile.defines); },
                    label = "Apply",
                    width = 50,
                };
                draws[1] = btnApply;
                if (EditorHelper.HeaderFoldout("Directives", pProfile.name + "Directives", false, draws))
                {
                    float width = EditorGUIUtility.currentViewWidth - 80;
                    if (!ContainDirective(pProfile.defines, "DEVELOPMENT"))
                        pProfile.defines.Insert(0, new DevSetting.Directive("DEVELOPMENT", false));
                    if (!ContainDirective(pProfile.defines, "UNITY_IAP"))
                        pProfile.defines.Insert(1, new DevSetting.Directive("UNITY_IAP", false));
                    if (!ContainDirective(pProfile.defines, "UNITY_MONETIZATION"))
                        pProfile.defines.Insert(3, new DevSetting.Directive("UNITY_MONETIZATION", false));

                    /*
                    float w0 = width * 0.1f, w1 = width * 0.6f, w2 = width * 0.15f, w3 = width * 0.14f;
                    EditorHelper.BoxHorizontal(() =>
                    {
                        GUILayout.Label("Sort", EditorStyles.boldLabel, GUILayout.Width(w0));
                        GUILayout.Label("Definition", EditorStyles.boldLabel, GUILayout.Width(w1));
                        GUILayout.Label("Enable", EditorStyles.boldLabel, GUILayout.Width(w2));
                        GUILayout.Label("Remove", EditorStyles.boldLabel, GUILayout.Width(w3));
                    }, default(Color), false, width);
                    for (int i = 0; i < pProfile.defines.Count; i++)
                    {
                        var d = pProfile.defines[i];

                        EditorHelper.BoxHorizontal(() =>
                        {
                            EditorHelper.Button("˄", () =>
                            {
                                if (i < pProfile.defines.Count - 1)
                                {
                                    var temp = pProfile.defines[i];
                                    pProfile.defines[i] = pProfile.defines[i - 1];
                                    pProfile.defines[i - 1] = temp;
                                    directivesChanged = true;
                                }
                            });
                            EditorHelper.Button("˅", () =>
                            {
                                if (i > 0)
                                {
                                    var temp = pProfile.defines[i];
                                    pProfile.defines[i] = pProfile.defines[i + 1];
                                    pProfile.defines[i + 1] = temp;
                                    directivesChanged = true;
                                }
                            });

                            d.name = EditorHelper.TextField(d.name, "", 0, (int)w1);
                            d.enabled = EditorHelper.Toggle(d.enabled, "", 0, (int)w2);
                            if (d.name != "DEVELOPMENT" && d.name != "UNITY_IAP" && d.name != "UNITY_MONETIZATION")
                            {
                                EditorHelper.ButtonColor("X", () =>
                                {
                                    directivesChanged = true;
                                    pProfile.defines.Remove(d);
                                }, Color.red, (int)w3);
                            }
                            else
                                EditorHelper.ButtonColor("x", () => { return; }, Color.grey, (int)w3);
                        }, default(Color), false, width);
                    }
                    */

                    if (!mReorderDirectivesDict.ContainsKey(pProfile.name) || directivesChanged)
                    {
                        var reorderList = new ReorderableList(pProfile.defines, typeof(DevSetting.Directive), true, true, true, true);
                        if (!mReorderDirectivesDict.ContainsKey(pProfile.name))
                            mReorderDirectivesDict.Add(pProfile.name, reorderList);
                        reorderList.drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            var define = pProfile.defines[index];
                            GUI.backgroundColor = define.color;
                            var toggleStyle = new GUIStyle(EditorStyles.toggle);
                            float widthTog = 20;
                            float widthColor = 60;
                            float widthName = rect.width - widthTog - widthColor - 10;
                            define.enabled = EditorGUI.Toggle(new Rect(rect.x, rect.y, widthTog, 20), define.enabled);

                            if (define.name == "DEVELOPMENT" || define.name == "UNITY_IAP" || define.name == "UNITY_MONETIZATION")
                                EditorGUI.LabelField(new Rect(rect.x + widthTog + 5, rect.y, widthName, 20), define.name);
                            else
                                define.name = EditorGUI.TextField(new Rect(rect.x + widthTog + 5, rect.y, widthName, 20), define.name);
                            define.color = EditorGUI.ColorField(new Rect(rect.x + widthTog + 5 + widthName + 5, rect.y, widthColor, 20), define.color);
                            GUI.backgroundColor = Color.white;
                        };
                        mReorderDirectivesDict[pProfile.name].onCanRemoveCallback = (list) =>
                        {
                            var define = pProfile.defines[list.index];
                            if (define.name == "DEVELOPMENT" || define.name == "UNITY_IAP" || define.name == "UNITY_MONETIZATION")
                                return false;
                            return true;
                        };
                    }
                    mReorderDirectivesDict[pProfile.name].DoLayoutList();
                }
            }, ColorHelper.DarkGreen, true);
        }
    }

    private void DrawPreviewSettingsProfiles()
    {
        var profiles = mProfileCollections.profiles;
        if (profiles.Count == 0)
            return;

        for (int i = 0; i < profiles.Count; i++)
        {
            if (EditorHelper.HeaderFoldout(profiles[i].name, "PreviewProfile" + i))
            {
                EditorHelper.BoxVertical(() =>
                {
                    DrawSettingsProfile(profiles[i]);
                }, ColorHelper.LightAzure, true);
            }
            if (i < profiles.Count - 1)
                EditorHelper.Seperator();
        }
        EditorHelper.Seperator();
        if (EditorHelper.ButtonColor("Back", Color.yellow))
            mPreviewingProfiles = false;
    }

    private void ApplyDirectives(List<DevSetting.Directive> defines)
    {
        string symbols = string.Join(";", defines
                        .Where(d => d.enabled == true)
                        .Select(d => d.name).ToArray());
        var taget = EditorUserBuildSettings.selectedBuildTargetGroup;
        PlayerSettings.SetScriptingDefineSymbolsForGroup(taget, symbols);
    }

    private bool ContainDirective(List<DevSetting.Directive> defines, string pName)
    {
        if (string.IsNullOrEmpty(pName))
            return true;

        for (int i = 0; i < defines.Count; i++)
        {
            if (defines[i].name == pName)
            {
                return true;
            }
        }
        return false;
    }

    private void DrawProfilesSelection()
    {
        EditorHelper.BoxVertical(() =>
        {
            var profiles = mProfileCollections.profiles;
            if (profiles == null) profiles = new List<DevSetting.Profile>();

            EditorHelper.BoxHorizontal(() =>
            {
                mTypedProfileName = EditorHelper.TextField(mTypedProfileName, "Profile Name");
                if (EditorHelper.ButtonColor("Save Profile", Color.green))
                {
                    if (string.IsNullOrEmpty(mTypedProfileName))
                        return;

                    int index = -1;
                    for (int i = 0; i < profiles.Count; i++)
                    {
                        if (profiles[i].name == mTypedProfileName.Trim())
                        {
                            profiles[i] = CloneProfile(mScript.profile);
                            index = i;
                            break;
                        }
                    }
                    if (index == -1)
                    {
                        var newProfile = CloneProfile(mScript.profile);
                        newProfile.name = mTypedProfileName;
                        profiles.Add(newProfile);
                    }

                    mSelectedProfile = mTypedProfileName;

                    ApplyProfile(mSelectedProfile);
                }
            }, Color.yellow);

            string[] allProfileNames = new string[profiles.Count];
            for (int i = 0; i < profiles.Count; i++)
                allProfileNames[i] = profiles[i].name;

            if (allProfileNames.Length > 0)
                EditorHelper.BoxHorizontal(() =>
                {
                    var preSelectedProfile = mSelectedProfile;
                    mSelectedProfile = EditorHelper.DropdownList(mSelectedProfile, "Profiles", allProfileNames);
                    if (mRemovingProfile)
                    {
                        if (EditorHelper.ButtonColor("Remove", Color.red))
                        {
                            for (int i = 0; i < profiles.Count; i++)
                                if (profiles[i].name == mSelectedProfile)
                                {
                                    profiles.RemoveAt(i);
                                    break;
                                }
                            mRemovingProfile = false;
                        }
                        if (EditorHelper.ButtonColor("Cancel", Color.green))
                            mRemovingProfile = false;
                    }
                    else
                    {
                        if (EditorHelper.ButtonColor("Remove", Color.yellow))
                            mRemovingProfile = true;
                        if (EditorHelper.ButtonColor("Preview", Color.yellow))
                            mPreviewingProfiles = true;
                    }

                    if (mSelectedProfile != preSelectedProfile)
                        ApplyProfile(mSelectedProfile);
                });
        }, Color.cyan, true);
    }

    private void ApplyProfile(string pName)
    {
        var profiles = mProfileCollections.profiles;
        for (int i = 0; i < profiles.Count; i++)
            if (profiles[i].name == pName)
            {
                mScript.profile = CloneProfile(profiles[i]);
                mTypedProfileName = mScript.profile.name;
                mRemovingProfile = false;
                ApplyDirectives(mScript.profile.defines);
                break;
            }
    }

    private DevSetting.Profile CloneProfile(DevSetting.Profile pProfile)
    {
        var toJson = JsonUtility.ToJson(pProfile);
        var fromJson = JsonUtility.FromJson<DevSetting.Profile>(toJson);
        return fromJson;
    }

    //========== FIREBASE

    private void DrawFirebaseConfiguration()
    {
        EditorHelper.BoxVertical("Firebase", () =>
        {
            FirebaseDevConfigPath = EditorHelper.FileSelector("Dev Config Fire", "FirebaseDevConfigPath" + GetInstanceID(), "json,txt");
            FirebaseLiveConfigPath = EditorHelper.FileSelector("Live Config Fire", "FirebaseLiveConfigPath" + GetInstanceID(), "json,txt");
            FirebaseConfigOutputFolder = EditorHelper.FolderSelector("Output Folder", "FirebaseConfigOutputFolder" + GetInstanceID());

            string testPath = Application.dataPath + FirebaseDevConfigPath;
            string livePath = Application.dataPath + FirebaseLiveConfigPath;
            string destination = Application.dataPath + FirebaseConfigOutputFolder + "/google-services.json";
            string curProjectNumber = FirebaseProjectNumber;

            EditorHelper.BoxHorizontal(() =>
            {
                if (livePath == testPath && livePath != "")
                {
                    EditorGUILayout.HelpBox("Live and Test Path must not be the same!", MessageType.Warning);
                    return;
                }

                EditorHelper.Button("Use Live Config", () =>
                {
                    FileInfo theSourceFile = new FileInfo(livePath);
                    using (StreamReader reader = theSourceFile.OpenText())
                    {
                        string content = reader.ReadToEnd();
                        var contentNode = SimpleJSON.JSON.Parse(content);
                        var projectNumberNode = contentNode["project_info"]["project_number"];
                        if (curProjectNumber != projectNumberNode)
                        {
                            curProjectNumber = projectNumberNode;
                            File.WriteAllText(destination, content);
                            FirebaseConfig = content;
                            FirebaseProjectNumber = curProjectNumber;
                            AssetDatabase.Refresh();
                            EditorApplication.ExecuteMenuItem("Assets/Play Services Resolver/Android Resolver/Force Resolve");
                            EditorApplication.ExecuteMenuItem("Assets/External Dependency Manager/Android Resolver/Force Resolve");
                        }
                    }
                });
                EditorHelper.Button("Use Dev Config", () =>
                {
                    FileInfo theSourceFile = new FileInfo(testPath);
                    using (StreamReader reader = theSourceFile.OpenText())
                    {
                        string content = reader.ReadToEnd();
                        var contentNode = SimpleJSON.JSON.Parse(content);
                        var projectNumberNode = contentNode["project_info"]["project_number"];
                        if (curProjectNumber != projectNumberNode)
                        {
                            curProjectNumber = projectNumberNode;
                            File.WriteAllText(destination, content);
                            FirebaseConfig = content;
                            FirebaseProjectNumber = curProjectNumber;
                            AssetDatabase.Refresh();
                            EditorApplication.ExecuteMenuItem("Assets/Play Services Resolver/Android Resolver/Force Resolve");
                            EditorApplication.ExecuteMenuItem("Assets/External Dependency Manager/Android Resolver/Force Resolve");
                        }
                    }
                });
            });
            string currentConfig = FirebaseConfig;
            if (string.IsNullOrEmpty(currentConfig))
            {
                var sourceFile = new FileInfo(destination);
                try
                {
                    using (StreamReader reader = sourceFile.OpenText())
                    {
                        currentConfig = reader.ReadToEnd();
                        FirebaseConfig = currentConfig;
                    }
                }
                catch
                {
                    FirebaseConfig = "firebase did not config";
                }
            }
            EditorHelper.BoxVertical(() =>
            {
                EditorGUILayout.LabelField("Preview...", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(currentConfig, GUILayout.MaxHeight(600), GUILayout.MaxWidth(EditorGUIUtility.currentViewWidth - 50));
            }, Color.white, true);
        }, default(Color), true);
    }

    private void CheckFirebaseConfigPaths()
    {
        string devFilePath = Application.dataPath + "/" + FirebaseDevConfigPath;
        string liveFilePath = Application.dataPath + "/" + FirebaseLiveConfigPath;
        string outputFolder = Application.dataPath + "/" + FirebaseConfigOutputFolder;
        if (!Directory.Exists(outputFolder))
        {
            if (FirebaseConfigOutputFolder.Contains("Assets"))
                FirebaseConfigOutputFolder = EditorHelper.FormatPathToUnityPath(FirebaseConfigOutputFolder).Replace("Assets/", "");
            else
                FirebaseConfigOutputFolder = "";
        }
        if (!File.Exists(devFilePath))
        {
            if (FirebaseDevConfigPath.Contains("Assets"))
                FirebaseDevConfigPath = EditorHelper.FormatPathToUnityPath(FirebaseDevConfigPath).Replace("Assets/", "");
            else
                FirebaseDevConfigPath = "";
        }
        if (!File.Exists(liveFilePath))
        {
            if (FirebaseLiveConfigPath.Contains("Assets"))
                FirebaseLiveConfigPath = EditorHelper.FormatPathToUnityPath(FirebaseLiveConfigPath).Replace("Assets/", "");
            else
                FirebaseLiveConfigPath = "";
        }
    }

    //======== CHEATS

    [MenuItem("RUtilities/Tools/Open Dev Settings %_#_;")]
    private static void OpenDevSettings()
    {
        Selection.activeObject = DevSetting.Instance;
    }
}