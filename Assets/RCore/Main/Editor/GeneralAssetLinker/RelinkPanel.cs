using System;
using System.Collections.Generic;
using RCore.Config;
using RCore.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RCore.Editor
{
	public class RelinkPanel : IAssetCatalogPanel
	{
		private enum RelinkScope
		{
			PrefabsOnly,
			ScenesOnly,
			PrefabsAndScenes,
		}

		private enum RelinkAction
		{
			ReplaceUsages,
			RenameKeyAndReplaceUsages,
		}

		private enum KeyTarget
		{
			Old,
			New,
		}

		private class RelinkResult
		{
			public int usageCount;
			public int assetCount;
			public readonly List<string> changedAssets = new List<string>();
			public readonly List<string> skippedAssets = new List<string>();
			public bool cancelled;
		}

		public string Title => "Relink Tools";

		private const float ACTION_BUTTON_HEIGHT = 30f;
		private const float UPDATED_ROW_HEIGHT = 22f;
		private const float UPDATED_ICON_WIDTH = 22f;
		private const float UPDATED_SELECT_WIDTH = 60f;

		private AssetCatalogWindow m_Window;
		private CatalogAssetType m_Type = CatalogAssetType.Sprite;
		private RelinkScope m_Scope = RelinkScope.PrefabsAndScenes;
		private string m_OldKey = string.Empty;
		private string m_NewKey = string.Empty;
		private string m_LastSummary = string.Empty;
		private MessageType m_LastSummaryType = MessageType.Info;
		private List<string> m_LastChangedAssets = new List<string>();

		public void OnEnable(AssetCatalogWindow pWindow)
		{
			m_Window = pWindow;
		}

		public void OnDisable() { }

		public void OnGUI(Rect pRect)
		{
			GUILayout.BeginArea(pRect);

			EditorGUILayout.BeginVertical("box");
			EditorGUILayout.LabelField("Relink Tools", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Replace Usages changes linker component keys only. Rename Key + Replace Usages also renames the AssetCatalog entry.", MessageType.Info);

			m_Type = EditorHelper.DropdownListEnum(m_Type, "Asset Type", 100);
			m_Scope = EditorHelper.DropdownListEnum(m_Scope, "Scope", 100);
			DrawKeyRow("Old Key", KeyTarget.Old);
			DrawKeyRow("New Key", KeyTarget.New);

			GUILayout.Space(8);
			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Replace Usages", GUILayout.Width(150), GUILayout.Height(ACTION_BUTTON_HEIGHT)))
				Run(RelinkAction.ReplaceUsages);
			if (GUILayout.Button("Rename Key + Replace Usages", GUILayout.Width(230), GUILayout.Height(ACTION_BUTTON_HEIGHT)))
				Run(RelinkAction.RenameKeyAndReplaceUsages);
			EditorGUILayout.EndHorizontal();

			DrawRelinkSummary();
			DrawChangedAssets();

			EditorGUILayout.EndVertical();

			GUILayout.EndArea();
		}

		private void DrawRelinkSummary()
		{
			if (string.IsNullOrEmpty(m_LastSummary))
				return;

			GUILayout.Space(8);
			EditorGUILayout.LabelField("Summary", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox(m_LastSummary, m_LastSummaryType);
		}

		private void DrawChangedAssets()
		{
			if (m_LastChangedAssets.Count == 0)
				return;

			GUILayout.Space(8);
			GUILayout.Label($"Updated Assets ({m_LastChangedAssets.Count})", EditorStyles.boldLabel);
			EditorGUILayout.BeginHorizontal(EditorStyles.toolbar, GUILayout.Height(18f));
			GUILayout.Space(UPDATED_ICON_WIDTH);
			GUILayout.Label("Path", EditorStyles.miniBoldLabel);
			GUILayout.Label("Action", EditorStyles.miniBoldLabel, GUILayout.Width(UPDATED_SELECT_WIDTH));
			EditorGUILayout.EndHorizontal();

			for (int i = 0; i < m_LastChangedAssets.Count; i++)
				DrawChangedAssetRow(m_LastChangedAssets[i], i);
		}

		private void DrawChangedAssetRow(string pPath, int pIndex)
		{
			var rowRect = EditorGUILayout.GetControlRect(false, UPDATED_ROW_HEIGHT);
			if (pIndex % 2 == 0)
				EditorGUI.DrawRect(rowRect, new Color(0f, 0f, 0f, 0.1f));

			var iconRect = new Rect(rowRect.x, rowRect.y + 1f, UPDATED_ICON_WIDTH, UPDATED_ROW_HEIGHT - 2f);
			var buttonRect = new Rect(rowRect.xMax - UPDATED_SELECT_WIDTH, rowRect.y + 2f, UPDATED_SELECT_WIDTH, UPDATED_ROW_HEIGHT - 4f);
			var pathRect = new Rect(iconRect.xMax + 4f, rowRect.y + 2f, buttonRect.x - iconRect.xMax - 8f, UPDATED_ROW_HEIGHT - 4f);

			var icon = AssetDatabase.GetCachedIcon(pPath);
			if (icon != null)
				GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit);
			GUI.Label(pathRect, AssetCatalogEditorGui.StripAssetsPrefix(pPath), EditorStyles.miniLabel);

			if (GUI.Button(buttonRect, "Select"))
			{
				var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(pPath);
				if (obj != null)
				{
					Selection.activeObject = obj;
					EditorGUIUtility.PingObject(obj);
				}
			}
		}

		private void DrawKeyRow(string pLabel, KeyTarget pTarget)
		{
			var key = GetKey(pTarget);
			EditorGUILayout.BeginHorizontal();
			key = EditorHelper.TextField(key, pLabel, 100);
			if (EditorHelper.Button("Browse...", 80))
			{
				var target = pTarget;
				AssetCatalogPickerWindow.Open(m_Type, pickedKey =>
				{
					SetKey(target, pickedKey);
					m_Window.Repaint();
				});
			}
			EditorGUILayout.EndHorizontal();
			SetKey(pTarget, key);
		}

		private string GetKey(KeyTarget pTarget)
		{
			return pTarget == KeyTarget.Old ? m_OldKey : m_NewKey;
		}

		private void SetKey(KeyTarget pTarget, string pKey)
		{
			if (pTarget == KeyTarget.Old)
				m_OldKey = pKey;
			else
				m_NewKey = pKey;
		}

		private void Run(RelinkAction pAction)
		{
			var catalog = m_Window.Catalog;
			m_LastChangedAssets.Clear();
			m_OldKey = m_OldKey.Trim();
			m_NewKey = m_NewKey.Trim();

			if (!Validate(catalog, pAction, out var error))
			{
				SetSummary(error, MessageType.Error);
				return;
			}

			var message = pAction == RelinkAction.ReplaceUsages
				? $"Replace {m_Type} linker usages from '{m_OldKey}' to '{m_NewKey}'? AssetCatalog entries stay unchanged."
				: $"Rename {m_Type} catalog key '{m_OldKey}' to '{m_NewKey}' and replace matching linker usages?";
			if (!EditorHelper.ConfirmPopup(message, "Run", "Cancel"))
			{
				SetSummary("Operation cancelled.", MessageType.Info);
				return;
			}

			var result = ReplaceUsages();
			if (result.cancelled)
			{
				SetSummary("Operation cancelled before scene scan.", MessageType.Info);
				return;
			}
			if (result.usageCount == 0)
			{
				SetSummary($"No matching usages found for key '{m_OldKey}'. No changes written.", MessageType.Warning);
				return;
			}

			bool catalogChanged = false;
			if (pAction == RelinkAction.RenameKeyAndReplaceUsages)
			{
				if (!catalog.EditorTryRenameKey(m_Type, m_OldKey, m_NewKey, out var renameError))
				{
					SetSummary($"Usages were replaced, but catalog rename failed: {renameError}", MessageType.Error);
					AssetDatabase.SaveAssets();
					return;
				}
				catalogChanged = true;
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			m_LastChangedAssets.AddRange(result.changedAssets);

			var skipped = result.skippedAssets.Count > 0 ? $"\nSkipped:\n- {string.Join("\n- ", result.skippedAssets)}" : string.Empty;
			var changed = result.changedAssets.Count > 0 ? $"\nUpdated Assets:\n- {string.Join("\n- ", result.changedAssets)}" : string.Empty;
			var summary = $"Done. Catalog changed: {catalogChanged}. Usages updated: {result.usageCount}. Assets changed: {result.assetCount}.{skipped}{changed}";
			SetSummary(summary, MessageType.Info);
			Debug.Log($"[AssetCatalogKeyRelink] {summary}");
		}

		private bool Validate(AssetCatalog pCatalog, RelinkAction pAction, out string pError)
		{
			pError = null;
			if (pCatalog == null)
			{
				pError = "No AssetCatalog found in Resources.";
				return false;
			}
			if (string.IsNullOrEmpty(m_OldKey))
			{
				pError = "Old key is empty.";
				return false;
			}
			if (string.IsNullOrEmpty(m_NewKey))
			{
				pError = "New key is empty.";
				return false;
			}
			if (m_OldKey == m_NewKey)
			{
				pError = "Old key and new key are the same.";
				return false;
			}
			if (!pCatalog.EditorHasKey(m_Type, m_OldKey))
			{
				pError = $"Old key '{m_OldKey}' not found in AssetCatalog.";
				return false;
			}

			bool newExists = pCatalog.EditorHasKey(m_Type, m_NewKey);
			if (pAction == RelinkAction.ReplaceUsages && !newExists)
			{
				pError = $"New key '{m_NewKey}' not found in AssetCatalog. Replace Usages would create broken linker references.";
				return false;
			}
			if (pAction == RelinkAction.RenameKeyAndReplaceUsages && newExists)
			{
				pError = $"New key '{m_NewKey}' already exists in AssetCatalog. Rename mode requires a new unused key.";
				return false;
			}
			return true;
		}

		private RelinkResult ReplaceUsages()
		{
			var result = new RelinkResult();
			if (ShouldScanScenes() && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
			{
				result.cancelled = true;
				return result;
			}

			try
			{
				if (m_Scope == RelinkScope.PrefabsOnly || m_Scope == RelinkScope.PrefabsAndScenes)
					ScanPrefabs(result);
				if (ShouldScanScenes())
					ScanScenes(result);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
			return result;
		}

		private bool ShouldScanScenes()
		{
			return m_Scope == RelinkScope.ScenesOnly || m_Scope == RelinkScope.PrefabsAndScenes;
		}

		private void ScanPrefabs(RelinkResult pResult)
		{
			var guids = AssetDatabase.FindAssets("t:Prefab");
			for (int i = 0; i < guids.Length; i++)
			{
				var path = AssetDatabase.GUIDToAssetPath(guids[i]);
				EditorUtility.DisplayProgressBar("Replacing Linker Keys", path, guids.Length == 0 ? 1f : (float)i / guids.Length);
				GameObject root = null;
				try
				{
					root = PrefabUtility.LoadPrefabContents(path);
					int count = UpdateInHierarchy(root);
					if (count > 0)
					{
						PrefabUtility.SaveAsPrefabAsset(root, path);
						pResult.usageCount += count;
						pResult.assetCount++;
						pResult.changedAssets.Add(path);
					}
				}
				catch (Exception ex)
				{
					pResult.skippedAssets.Add($"{path} ({ex.Message})");
				}
				finally
				{
					if (root != null)
						PrefabUtility.UnloadPrefabContents(root);
				}
			}
		}

		private void ScanScenes(RelinkResult pResult)
		{
			var setup = EditorSceneManager.GetSceneManagerSetup();
			var guids = AssetDatabase.FindAssets("t:Scene");
			try
			{
				for (int i = 0; i < guids.Length; i++)
				{
					var path = AssetDatabase.GUIDToAssetPath(guids[i]);
					EditorUtility.DisplayProgressBar("Replacing Linker Keys", path, guids.Length == 0 ? 1f : (float)i / guids.Length);
					try
					{
						var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
						int count = 0;
						foreach (var root in scene.GetRootGameObjects())
							count += UpdateInHierarchy(root);

						if (count > 0)
						{
							EditorSceneManager.MarkSceneDirty(scene);
							EditorSceneManager.SaveScene(scene);
							pResult.usageCount += count;
							pResult.assetCount++;
							pResult.changedAssets.Add(path);
						}
					}
					catch (Exception ex)
					{
						pResult.skippedAssets.Add($"{path} ({ex.Message})");
					}
				}
			}
			finally
			{
				if (setup != null && setup.Length > 0)
					EditorSceneManager.RestoreSceneManagerSetup(setup);
			}
		}

		private int UpdateInHierarchy(GameObject pRoot)
		{
			switch (m_Type)
			{
				case CatalogAssetType.Sprite:
					var count = UpdateComponents(EditorHelper.FindComponents<GeneralSpriteLinker>(new[] { pRoot }, ComponentHasOldKey));
					count += UpdateComponents(EditorHelper.FindComponents<GeneralSpriteRendererLinker>(new[] { pRoot }, ComponentHasOldKey));
					return count;
				case CatalogAssetType.Texture2D:
					return UpdateComponents(EditorHelper.FindComponents<GeneralTextureLinker>(new[] { pRoot }, ComponentHasOldKey));
				case CatalogAssetType.AudioClip:
					return UpdateComponents(EditorHelper.FindComponents<GeneralAudioLinker>(new[] { pRoot }, ComponentHasOldKey));
				default:
					return 0;
			}
		}

		private bool ComponentHasOldKey<T>(T pComponent) where T : Component
		{
			var serializedObject = new SerializedObject(pComponent);
			var keyProperty = serializedObject.FindProperty("m_Key");
			return keyProperty != null && keyProperty.stringValue == m_OldKey;
		}

		private int UpdateComponents<T>(Dictionary<GameObject, List<T>> pComponents) where T : Component
		{
			int count = 0;
			foreach (var pair in pComponents)
			{
				foreach (var component in pair.Value)
				{
					var serializedObject = new SerializedObject(component);
					var keyProperty = serializedObject.FindProperty("m_Key");
					if (keyProperty == null || keyProperty.stringValue != m_OldKey)
						continue;

					keyProperty.stringValue = m_NewKey;
					serializedObject.ApplyModifiedPropertiesWithoutUndo();
					EditorUtility.SetDirty(component);
					count++;
				}
			}
			return count;
		}

		private void SetSummary(string pMessage, MessageType pType)
		{
			m_LastSummary = pMessage;
			m_LastSummaryType = pType;
			m_Window.Repaint();
		}
	}
}
