﻿using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

#if UNITY_5_3_OR_NEWER
#endif

namespace VirtueSky.AssetFinder.Editor
{
    // filter, ignore anh huong ket qua thi hien mau do
    // optimize lag duplicate khi use
    public class AssetFinderWindowAll : AssetFinderWindowBase, IHasCustomMenu
    {
        [MenuItem("Sunflower/Asset Finder/Show Window %#k", false, priority = 101)]
        public static void ShowWindow()
        {
            var _window = CreateInstance<AssetFinderWindowAll>();
            _window.InitIfNeeded();
            AssetFinderUnity.SetWindowTitle(_window, "Asset Finder");
            _window.Show();
        }

        [MenuItem("Sunflower/Asset Finder/Delete Cache", false, priority = 102)]
        public static void DeleteFinderCache()
        {
            AssetFinderCache.DeleteCache();
        }

        [NonSerialized] internal AssetFinderBookmark bookmark;
        [NonSerialized] internal AssetFinderSelection selection;


        [NonSerialized] internal AssetFinderUsedInBuild UsedInBuild;
        [NonSerialized] internal AssetFinderDuplicateTree2 Duplicated;
        [NonSerialized] internal AssetFinderRefDrawer RefUnUse;

        [NonSerialized] internal AssetFinderRefDrawer
            UsesDrawer; // [Selected Assets] are [USING] (depends on / contains reference to) ---> those assets

        [NonSerialized] internal AssetFinderRefDrawer
            UsedByDrawer; // [Selected Assets] are [USED BY] <---- those assets 

        [NonSerialized] internal AssetFinderRefDrawer
            SceneToAssetDrawer; // [Selected GameObjects in current Scene] are [USING] ---> those assets

        [NonSerialized] internal AssetFinderRefDrawer
            RefInScene; // [Selected Assets] are [USED BY] <---- those components in current Scene 

        [NonSerialized] internal AssetFinderRefDrawer
            SceneUsesDrawer; // [Selected GameObjects] are [USING] ---> those components / GameObjects in current scene

        [NonSerialized] internal AssetFinderRefDrawer
            RefSceneInScene; // [Selected GameObjects] are [USED BY] <---- those components / GameObjects in current scene


        internal int level;
        private Vector2 scrollPos;
        private string tempGUID;
        private Object tempObject;

        protected bool lockSelection
        {
            get { return selection != null && selection.isLock; }
        }

        private void OnEnable()
        {
            Repaint();
        }

        protected void InitIfNeeded()
        {
            if (UsesDrawer != null) return;

            UsesDrawer = new AssetFinderRefDrawer(this)
            {
                messageEmpty =
                    "[Selected Assets] are not [USING] (depends on / contains reference to) any other assets!"
            };

            UsedByDrawer = new AssetFinderRefDrawer(this)
            {
                messageEmpty = "[Selected Assets] are not [USED BY] any other assets!"
            };

            Duplicated = new AssetFinderDuplicateTree2(this);
            SceneToAssetDrawer = new AssetFinderRefDrawer(this)
            {
                messageEmpty =
                    "[Selected GameObjects] (in current open scenes) are not [USING] any assets!"
            };

            RefUnUse = new AssetFinderRefDrawer(this);
            RefUnUse.groupDrawer.hideGroupIfPossible = true;

            UsedInBuild = new AssetFinderUsedInBuild(this);
            bookmark = new AssetFinderBookmark(this);
            selection = new AssetFinderSelection(this);

            SceneUsesDrawer = new AssetFinderRefDrawer(this)
            {
                messageEmpty =
                    "[Selected GameObjects] are not [USING] any other GameObjects in scenes"
            };

            RefInScene = new AssetFinderRefDrawer(this)
            {
                messageEmpty =
                    "[Selected Assets] are not [USED BY] any GameObjects in opening scenes!"
            };

            RefSceneInScene = new AssetFinderRefDrawer(this)
            {
                messageEmpty =
                    "[Selected GameObjects] are not [USED BY] by any GameObjects in opening scenes!"
            };

#if UNITY_2018_OR_NEWER
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode -=
																																																																																																																																																																					                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChangedInEditMode +=
																																																																																																																																																																					                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                  OnSceneChanged;
#elif UNITY_2017_OR_NEWER
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged -= OnSceneChanged;
            UnityEditor.SceneManagement.EditorSceneManager.activeSceneChanged += OnSceneChanged;
#endif

            AssetFinderCache.onReady -= OnReady;
            AssetFinderCache.onReady += OnReady;

            AssetFinderSetting.OnIgnoreChange -= OnIgnoreChanged;
            AssetFinderSetting.OnIgnoreChange += OnIgnoreChanged;

            Repaint();
        }

#if UNITY_2018_OR_NEWER
        private void OnSceneChanged(Scene arg0, Scene arg1)
        {
            if (IsFocusingFindInScene || IsFocusingSceneToAsset || IsFocusingSceneInScene)
            {
                OnSelectionChange();
            }
        }
#endif
        protected void OnIgnoreChanged()
        {
            RefUnUse.ResetUnusedAsset();
            UsedInBuild.SetDirty();

            OnSelectionChange();
        }

        protected void OnCSVClick()
        {
            AssetFinderRef[] csvSource = null;
            var drawer = GetAssetDrawer();

            if (drawer != null) csvSource = drawer.source;

            if (IsFocusingUnused && csvSource == null)
            {
                csvSource = RefUnUse.source;
                //if (csvSource != null) Debug.Log("d : " + csvSource.Length);
            }

            if (IsFocusingUsedInBuild && csvSource == null)
            {
                csvSource = AssetFinderRef.FromDict(UsedInBuild.refs);
                //if (csvSource != null) Debug.Log("e : " + csvSource.Length);
            }

            if (IsFocusingDuplicate && csvSource == null)
            {
                csvSource = AssetFinderRef.FromList(Duplicated.list);
                //if (csvSource != null) Debug.Log("f : " + csvSource.Length);
            }

            AssetFinderExport.ExportCSV(csvSource);
        }

        protected void OnReady()
        {
            OnSelectionChange();
        }

        public override void OnSelectionChange()
        {
            Repaint();

            isNoticeIgnore = false;
            if (!AssetFinderCache.isReady)
            {
                return;
            }

            if (focusedWindow == null)
            {
                return;
            }

            if (SceneUsesDrawer == null)
            {
                InitIfNeeded();
            }

            if (UsesDrawer == null)
            {
                InitIfNeeded();
            }

            if (!lockSelection)
            {
                ids = AssetFinderUnity.Selection_AssetGUIDs;
                selection.Clear();

                //ignore selection on asset when selected any object in scene
                if (Selection.gameObjects.Length > 0 &&
                    !AssetFinderUnity.IsInAsset(Selection.gameObjects[0]))
                {
                    ids = new string[0];
                    selection.AddRange(Selection.gameObjects);
                }
                else
                {
                    selection.AddRange(ids);
                }

                level = 0;

                if (selection.isSelectingAsset)
                {
                    UsesDrawer.Reset(ids, true);
                    UsedByDrawer.Reset(ids, false);
                    RefInScene.Reset(ids, this as IWindow);
                }
                else
                {
                    RefSceneInScene.ResetSceneInScene(Selection.gameObjects);
                    SceneToAssetDrawer.Reset(Selection.gameObjects, true, true);
                    SceneUsesDrawer.ResetSceneUseSceneObjects(Selection.gameObjects);
                }

                // auto disable enable scene / asset
                if (IsFocusingUses)
                {
                    sp2.splits[0].visible = !selection.isSelectingAsset;
                    sp2.splits[1].visible = true;
                    sp2.CalculateWeight();
                }

                if (IsFocusingUsedBy)
                {
                    sp2.splits[0].visible = true;
                    sp2.splits[1].visible = selection.isSelectingAsset;
                    sp2.CalculateWeight();
                }
            }

            if (IsFocusingGUIDs)
            {
                //objs = new Object[ids.Length];
                objs = new Dictionary<string, Object>();
                var objects = Selection.objects;
                for (var i = 0; i < objects.Length; i++)
                {
                    var item = objects[i];

#if UNITY_2018_1_OR_NEWER
                    {
                        var guid = "";
                        long fileid = -1;
                        try
                        {
                            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(item, out guid,
                                    out fileid))
                            {
                                objs.Add($"{guid}/{fileid}", objects[i]);
                                //Debug.Log("guid: " + guid + "  fileID: " + fileid);
                            }
                        }
                        catch
                        {
                        }
                    }
#else
					{
						var path = AssetDatabase.GetAssetPath(item);
                        if (string.IsNullOrEmpty(path)) continue;
                        var guid = AssetDatabase.AssetPathToGUID(path);
                        System.Reflection.PropertyInfo inspectorModeInfo =
                        typeof(SerializedObject).GetProperty("inspectorMode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        SerializedObject serializedObject = new SerializedObject(item);
                        inspectorModeInfo.SetValue(serializedObject, InspectorMode.Debug, null);

                        SerializedProperty localIdProp =
                            serializedObject.FindProperty("m_LocalIdentfierInFile");   //note the misspelling!

                        var localId = localIdProp.longValue;
                        if (localId <= 0)
                        {
                            localId = localIdProp.intValue;
                        }
                        if (localId <= 0)
                        {
                            continue;
                        }
                        if (!string.IsNullOrEmpty(guid)) objs.Add(guid + "/" + localId, objects[i]);
					}
#endif
                }
            }

            if (IsFocusingUnused)
            {
                RefUnUse.ResetUnusedAsset();
            }

            if (AssetFinderSceneCache.Api.Dirty && !Application.isPlaying)
            {
                AssetFinderSceneCache.Api.refreshCache(this);
            }

            EditorApplication.delayCall -= Repaint;
            EditorApplication.delayCall += Repaint;
        }


        public AssetFinderSplitView sp1; // container : Selection / sp2 / Bookmark 
        public AssetFinderSplitView sp2; // Scene / Assets

        void InitPanes()
        {
            sp2 = new AssetFinderSplitView(this)
            {
                isHorz = false,
                splits = new List<AssetFinderSplitView.Info>()
                {
                    new AssetFinderSplitView.Info()
                    {
                        title = new GUIContent("Scene", AssetFinderIcon.Scene.image),
                        draw = DrawScene
                    },
                    new AssetFinderSplitView.Info()
                    {
                        title = new GUIContent("Assets", AssetFinderIcon.Asset.image),
                        draw = DrawAsset
                    },
                }
            };

            sp2.CalculateWeight();

            sp1 = new AssetFinderSplitView(this)
            {
                isHorz = true,
                splits = new List<AssetFinderSplitView.Info>()
                {
                    new AssetFinderSplitView.Info()
                    {
                        title = new GUIContent("Selection", AssetFinderIcon.Selection.image),
                        weight = 0.4f, visible = false, draw = (rect) => selection.Draw(rect)
                    },
                    new AssetFinderSplitView.Info()
                    {
                        draw = (r) =>
                        {
                            if (IsFocusingUses || IsFocusingUsedBy)
                            {
                                sp2.Draw(r);
                            }
                            else
                            {
                                DrawTools(r);
                            }
                        }
                    },
                    new AssetFinderSplitView.Info()
                    {
                        title = new GUIContent("Bookmark", AssetFinderIcon.Favorite.image),
                        weight = 0.4f, visible = false, draw = (rect) => bookmark.Draw(rect)
                    },
                }
            };

            sp1.CalculateWeight();
        }

        private AssetFinderTabView tabs;
        private AssetFinderTabView bottomTabs;
        private AssetFinderSearchView search;

        void DrawScene(Rect rect)
        {
            AssetFinderRefDrawer drawer = IsFocusingUses
                ? (selection.isSelectingAsset ? null : SceneUsesDrawer)
                : (selection.isSelectingAsset ? RefInScene : RefSceneInScene);
            if (drawer == null) return;

            if (!AssetFinderSceneCache.ready)
            {
                var rr = rect;
                rr.height = 16f;

                int cur = AssetFinderSceneCache.Api.current,
                    total = AssetFinderSceneCache.Api.total;
                EditorGUI.ProgressBar(rr, cur * 1f / total, string.Format("{0} / {1}", cur, total));
                WillRepaint = true;
                return;
            }

            drawer.Draw(rect);

            var refreshRect = new Rect(rect.xMax - 16f, rect.yMin - 14f, 18f, 18f);
            if (GUI2.ColorIconButton(refreshRect, AssetFinderIcon.Refresh.image,
                    AssetFinderSceneCache.Api.Dirty ? (Color?)GUI2.lightRed : null))
            {
                AssetFinderSceneCache.Api.refreshCache(drawer.window);
            }
        }


        AssetFinderRefDrawer GetAssetDrawer()
        {
            if (IsFocusingUses)
            {
                return selection.isSelectingAsset ? UsesDrawer : SceneToAssetDrawer;
            }

            if (IsFocusingUsedBy)
            {
                return selection.isSelectingAsset ? UsedByDrawer : null;
            }

            return null;
        }

        void DrawAsset(Rect rect)
        {
            var drawer = GetAssetDrawer();
            if (drawer != null) drawer.Draw(rect);
        }

        void DrawSearch()
        {
            if (search == null) search = new AssetFinderSearchView();
            search.DrawLayout();
        }

        protected override void OnGUI()
        {
            OnGUI2();
        }

        protected bool CheckDrawImport()
        {
            if (EditorApplication.isCompiling)
            {
                EditorGUILayout.HelpBox("Compiling scripts, please wait!", MessageType.Warning);
                Repaint();
                return false;
            }

            if (EditorApplication.isUpdating)
            {
                EditorGUILayout.HelpBox("Importing assets, please wait!", MessageType.Warning);
                Repaint();
                return false;
            }

            InitIfNeeded();

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorGUILayout.HelpBox("Asset Finder requires serialization mode set to FORCE TEXT!",
                    MessageType.Warning);
                if (GUILayout.Button("FORCE TEXT"))
                {
                    EditorSettings.serializationMode = SerializationMode.ForceText;
                }

                return false;
            }

            if (AssetFinderCache.hasCache && !AssetFinderCache.CheckSameVersion())
            {
                EditorGUILayout.HelpBox(
                    "Incompatible cache version found!!!\nAsset Finder will need a full refresh and this may take quite some time to finish but you would be able to work normally while the scan works in background!",
                    MessageType.Warning);
                AssetFinderCache.DrawPriorityGUI();
                if (GUILayout.Button("Scan project"))
                {
                    AssetFinderCache.DeleteCache();
                    AssetFinderCache.CreateCache();
                }

                return false;
            }

            if (!AssetFinderCache.isReady)
            {
                if (!AssetFinderCache.hasCache)
                {
                    EditorGUILayout.HelpBox(
                        "Finder cache not found!\nFirst scan may takes quite some time to finish but you would be able to work normally while the scan works in background...",
                        MessageType.Warning);

                    AssetFinderCache.DrawPriorityGUI();

                    if (GUILayout.Button("Scan project"))
                    {
                        AssetFinderCache.CreateCache();
                        Repaint();
                    }

                    return false;
                }
                else
                {
                    AssetFinderCache.DrawPriorityGUI();
                }

                if (!DrawEnable())
                {
                    return false;
                }

                AssetFinderCache api = AssetFinderCache.Api;
                string text = $"Refreshing ... {(int)(api.progress * api.workCount)} / {api.workCount}";
                Rect rect = GUILayoutUtility.GetRect(1f, Screen.width, 18f, 18f);
                EditorGUI.ProgressBar(rect, api.progress, text);
                Repaint();
                return false;
            }

            if (!DrawEnable())
            {
                return false;
            }

            return true;
        }


        protected bool IsFocusingUses
        {
            get { return tabs != null && tabs.current == 0; }
        }

        protected bool IsFocusingUsedBy
        {
            get { return tabs != null && tabs.current == 1; }
        }

        protected bool IsFocusingDuplicate
        {
            get { return tabs != null && tabs.current == 2; }
        }

        protected bool IsFocusingGUIDs
        {
            get { return tabs != null && tabs.current == 3; }
        }

        protected bool IsFocusingUnused
        {
            get { return tabs != null && tabs.current == 4; }
        }

        protected bool IsFocusingUsedInBuild
        {
            get { return tabs != null && tabs.current == 5; }
        }

        void OnTabChange()
        {
            if (deleteUnused != null) deleteUnused.hasConfirm = false;
            if (UsedInBuild != null) UsedInBuild.SetDirty();
        }

        void InitTabs()
        {
            tabs = AssetFinderTabView.Create(this, false,
                "Uses", "Used By", "Duplicate", "GUIDs", "Unused Assets", "Uses in Build"
            );
            tabs.onTabChange = OnTabChange;
            tabs.callback = new DrawCallback()
            {
                BeforeDraw = () =>
                {
                    if (GUI2.ToolbarToggle(ref selection.isLock,
                            selection.isLock
                                ? AssetFinderIcon.Lock.image
                                : AssetFinderIcon.Unlock.image,
                            new Vector2(-1, 2), "Lock Selection"))
                    {
                        WillRepaint = true;
                    }
                },

                AfterDraw = () =>
                {
                    //GUILayout.Space(16f);

                    if (GUI2.ToolbarToggle(ref sp1.isHorz, AssetFinderIcon.Panel.image,
                            Vector2.zero, "Layout"))
                    {
                        sp1.CalculateWeight();
                        Repaint();
                    }

                    if (GUI2.ToolbarToggle(ref sp1.splits[0].visible,
                            AssetFinderIcon.Selection.image, Vector2.zero, "Show / Hide Selection"))
                    {
                        sp1.CalculateWeight();
                        Repaint();
                    }

                    if (GUI2.ToolbarToggle(ref sp2.splits[0].visible, AssetFinderIcon.Scene.image,
                            Vector2.zero, "Show / Hide Scene References"))
                    {
                        sp2.CalculateWeight();
                        Repaint();
                    }

                    if (GUI2.ToolbarToggle(ref sp2.splits[1].visible, AssetFinderIcon.Asset.image,
                            Vector2.zero, "Show / Hide Asset References"))
                    {
                        sp2.CalculateWeight();
                        Repaint();
                    }


                    if (GUI2.ToolbarToggle(ref sp1.splits[2].visible,
                            AssetFinderIcon.Favorite.image, Vector2.zero, "Show / Hide Bookmarks"))
                    {
                        sp1.CalculateWeight();
                        Repaint();
                    }
                }
            };
        }

        protected bool DrawHeader()
        {
            if (tabs == null) InitTabs();
            if (bottomTabs == null)
            {
                bottomTabs = AssetFinderTabView.Create(this, true,
                    new GUIContent(AssetFinderIcon.Setting.image, "Settings"),
                    new GUIContent(AssetFinderIcon.Ignore.image, "Ignore"),
                    new GUIContent(AssetFinderIcon.Filter.image, "Filter by Type")
                );
                bottomTabs.current = -1;
            }

            tabs.DrawLayout();

            return true;
        }


        protected bool DrawFooter()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                bottomTabs.DrawLayout();
                GUILayout.FlexibleSpace();
                DrawAssetViewSettings();
                GUILayout.FlexibleSpace();
                DrawViewModes();
            }
            GUILayout.EndHorizontal();
            return false;
        }

        void DrawAssetViewSettings()
        {
            var isDisable = !sp2.splits[1].visible;
            EditorGUI.BeginDisabledGroup(isDisable);
            {
                GUI2.ToolbarToggle(ref AssetFinderSetting.s.displayAssetBundleName,
                    AssetFinderIcon.AssetBundle.image, Vector2.zero,
                    "Show / Hide Assetbundle Names");
#if UNITY_2017_1_OR_NEWER
                GUI2.ToolbarToggle(ref AssetFinderSetting.s.displayAtlasName,
                    AssetFinderIcon.Atlas.image, Vector2.zero, "Show / Hide Atlas packing tags");
#endif
                GUI2.ToolbarToggle(ref AssetFinderSetting.s.showUsedByClassed,
                    AssetFinderIcon.Material.image, Vector2.zero, "Show / Hide usage icons");
                GUI2.ToolbarToggle(ref AssetFinderSetting.s.displayFileSize,
                    AssetFinderIcon.Filesize.image, Vector2.zero, "Show / Hide file size");

                if (GUILayout.Button("CSV", EditorStyles.toolbarButton))
                {
                    OnCSVClick();
                }
            }
            EditorGUI.EndDisabledGroup();
        }

        void DrawViewModes()
        {
            var gMode = AssetFinderSetting.GroupMode;
            if (GUI2.EnumPopup(ref gMode, new GUIContent(AssetFinderIcon.Group.image, "Group by"),
                    EditorStyles.toolbarPopup, GUILayout.Width(80f)))
            {
                AssetFinderSetting.GroupMode = gMode;
                markDirty();
            }

            GUILayout.Space(16f);

            var sMode = AssetFinderSetting.SortMode;
            if (GUI2.EnumPopup(ref sMode, new GUIContent(AssetFinderIcon.Sort.image, "Sort by"),
                    EditorStyles.toolbarPopup, GUILayout.Width(50f)))
            {
                AssetFinderSetting.SortMode = sMode;
                RefreshSort();
            }
        }

        protected void OnGUI2()
        {
            if (!CheckDrawImport())
            {
                return;
            }

            // EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height),
            //     GameDataEditor.ColorBackgroundRectWindowSunflower.ToColor());
            // GUI.contentColor = GameDataEditor.ColorTextContentWindowSunflower.ToColor();
            // GUI.backgroundColor = GameDataEditor.ColorContentWindowSunflower.ToColor();
            if (sp1 == null) InitPanes();

            DrawHeader();
            sp1.DrawLayout();
            DrawSettings();
            DrawFooter();

            if (WillRepaint)
            {
                Repaint();
            }
        }


        private AssetFinderDeleteButton deleteUnused;


        void DrawTools(Rect rect)
        {
            if (IsFocusingDuplicate)
            {
                rect = GUI2.Padding(rect, 2f, 2f);

                GUILayout.BeginArea(rect);
                Duplicated.DrawLayout();
                GUILayout.EndArea();
                return;
            }

            if (IsFocusingUnused)
            {
                rect = GUI2.Padding(rect, 2f, 2f);

                if ((RefUnUse.refs != null && RefUnUse.refs.Count == 0))
                {
                    GUILayout.BeginArea(rect);
                    {
                        EditorGUILayout.HelpBox("Wow! So clean!?", MessageType.Info);
                        EditorGUILayout.HelpBox(
                            "Your project does not has have any unused assets, or have you just hit DELETE ALL?",
                            MessageType.Info);
                        EditorGUILayout.HelpBox(
                            "Your backups are placed at Library/AssetFinder/ just in case you want your assets back!",
                            MessageType.Info);
                    }
                    GUILayout.EndArea();
                }
                else
                {
                    rect.yMax -= 40f;
                    GUILayout.BeginArea(rect);
                    RefUnUse.DrawLayout();
                    GUILayout.EndArea();

                    var toolRect = rect;
                    toolRect.yMin = toolRect.yMax;

                    var lineRect = toolRect;
                    lineRect.height = 1f;

                    GUI2.Rect(lineRect, Color.black, 0.5f);

                    toolRect.xMin += 2f;
                    toolRect.xMax -= 2f;
                    toolRect.height = 40f;

                    if (deleteUnused == null)
                    {
                        deleteUnused = new AssetFinderDeleteButton()
                        {
                            warningMessage =
                                "It's absolutely safe to delete them all!\nA backup (.unitypackage) will be created so you can import it back later!",
                            deleteLabel = new GUIContent("DELETE ASSETS",
                                AssetFinderIcon.Delete.image),
                            confirmMessage = "Create backup at Library/AssetFinder/"
                        };
                    }

                    GUILayout.BeginArea(toolRect);
                    deleteUnused.Draw(() => { AssetFinderUnity.BackupAndDeleteAssets(RefUnUse.source); });
                    GUILayout.EndArea();
                }

                return;
            }

            if (IsFocusingUsedInBuild)
            {
                UsedInBuild.Draw(rect);
                return;
            }

            if (IsFocusingGUIDs)
            {
                rect = GUI2.Padding(rect, 2f, 2f);

                GUILayout.BeginArea(rect);
                DrawGUIDs();
                GUILayout.EndArea();
                return;
            }
        }

        void DrawSettings()
        {
            if (bottomTabs.current == -1) return;

            GUILayout.BeginVertical(GUILayout.Height(100f));
            {
                GUILayout.Space(2f);
                switch (bottomTabs.current)
                {
                    case 0:
                    {
                        AssetFinderSetting.s.DrawSettings();
                        break;
                    }

                    case 1:
                    {
                        if (AssetType.DrawIgnoreFolder())
                        {
                            markDirty();
                        }

                        break;
                    }

                    case 2:
                    {
                        if (AssetType.DrawSearchFilter())
                        {
                            markDirty();
                        }

                        break;
                    }
                }
            }
            GUILayout.EndVertical();

            var rect = GUILayoutUtility.GetLastRect();
            rect.height = 1f;
            GUI2.Rect(rect, Color.black, 0.4f);
        }

        protected void markDirty()
        {
            UsedByDrawer.SetDirty();
            UsesDrawer.SetDirty();
            Duplicated.SetDirty();
            SceneToAssetDrawer.SetDirty();
            RefUnUse.SetDirty();

            RefInScene.SetDirty();
            RefSceneInScene.SetDirty();
            SceneUsesDrawer.SetDirty();
            UsedInBuild.SetDirty();
            WillRepaint = true;
        }

        protected void RefreshSort()
        {
            UsedByDrawer.RefreshSort();
            UsesDrawer.RefreshSort();
            Duplicated.RefreshSort();
            SceneToAssetDrawer.RefreshSort();
            RefUnUse.RefreshSort();

            UsedInBuild.RefreshSort();
        }
        // public bool isExcludeByFilter;

        protected bool checkNoticeFilter()
        {
            var rsl = false;

            if (IsFocusingUsedBy && !rsl)
            {
                rsl = UsedByDrawer.isExclueAnyItem();
            }

            if (IsFocusingDuplicate)
            {
                return Duplicated.isExclueAnyItem();
            }

            if (IsFocusingUses && rsl == false)
            {
                rsl = UsesDrawer.isExclueAnyItem();
            }

            //tab use by
            return rsl;
        }

        protected bool checkNoticeIgnore()
        {
            bool rsl = isNoticeIgnore;
            return rsl;
        }


        private Dictionary<string, Object> objs;
        private string[] ids;

        private void DrawGUIDs()
        {
            GUILayout.Label("GUID to Object", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            {
                string guid = EditorGUILayout.TextField(tempGUID ?? string.Empty);
                EditorGUILayout.ObjectField(tempObject, typeof(Object), false,
                    GUILayout.Width(120f));

                if (GUILayout.Button("Paste", EditorStyles.miniButton, GUILayout.Width(70f)))
                {
                    guid = EditorGUIUtility.systemCopyBuffer;
                }

                if (guid != tempGUID && !string.IsNullOrEmpty(guid))
                {
                    tempGUID = guid;

                    tempObject = AssetFinderUnity.LoadAssetAtPath<Object>
                    (
                        AssetDatabase.GUIDToAssetPath(tempGUID)
                    );
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10f);
            if (objs == null) // || ids == null)
            {
                return;
            }

            //GUILayout.Label("Selection", EditorStyles.boldLabel);
            //if (ids.Length == objs.Count)
            {
                scrollPos = GUILayout.BeginScrollView(scrollPos);
                {
                    //for (var i = 0; i < ids.Length; i++)
                    foreach (var item in objs)
                    {
                        //if (!objs.ContainsKey(ids[i])) continue;

                        GUILayout.BeginHorizontal();
                        {
                            //var obj = objs[ids[i]];
                            var obj = item.Value;

                            EditorGUILayout.ObjectField(obj, typeof(Object), false,
                                GUILayout.Width(150));
                            string idi = item.Key;
                            GUILayout.TextField(idi, GUILayout.Width(240f));
                            if (GUILayout.Button("Copy", EditorStyles.miniButton,
                                    GUILayout.Width(50f)))
                            {
                                tempObject = obj;
                                //EditorGUIUtility.systemCopyBuffer = tempGUID = item.Key;
                                tempGUID = item.Key;

                                //string guid = "";
                                //long file = -1;
                                //if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out file))
                                //{
                                //    EditorGUIUtility.systemCopyBuffer = tempGUID = idi + "/" + file;

                                //    if (!string.IsNullOrEmpty(tempGUID))
                                //    {
                                //        tempObject = obj;
                                //    }
                                //}  
                            }
                        }
                        GUILayout.EndHorizontal();
                    }
                }
                GUILayout.EndScrollView();
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Merge Selection To"))
            {
                AssetFinderExport.MergeDuplicate(tempGUID);
            }

            EditorGUILayout.ObjectField(tempObject, typeof(Object), false, GUILayout.Width(120f));
            GUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }
    }
}