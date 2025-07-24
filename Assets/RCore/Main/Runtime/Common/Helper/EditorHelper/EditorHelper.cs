/***
 * Author HNB-RaBear - 2017
 */

#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
	public static class EditorHelper
	{
#region EditorComponentUtil

		public static List<T> FindAll<T>() where T : Component => EditorComponentUtil.FindAll<T>();
		
		public static void ReplaceGameObjectsInScene(ref List<GameObject> selections, List<GameObject> prefabs)
		{
			for (var i = selections.Count - 1; i >= 0; --i)
			{
				GameObject newObject;
				var selected = selections[i];
				var prefab = prefabs[UnityEngine.Random.Range(0, prefabs.Count)];
				if (prefab.IsPrefab())
				{
					newObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
				}
				else
				{
					newObject = Object.Instantiate(prefab);
					newObject.name = prefab.name;
				}

				if (newObject == null)
				{
					UnityEngine.Debug.LogError("Error instantiating prefab");
					break;
				}

				Undo.RegisterCreatedObjectUndo(newObject, "Replace With Prefabs");
				newObject.transform.parent = selected.transform.parent;
				newObject.transform.localPosition = selected.transform.localPosition;
				newObject.transform.localRotation = selected.transform.localRotation;
				newObject.transform.localScale = selected.transform.localScale;
				newObject.transform.SetSiblingIndex(selected.transform.GetSiblingIndex());
				Undo.DestroyObjectImmediate(selected);
				selections[i] = newObject;
			}
		}
		
		public static Dictionary<GameObject, List<T>> FindComponents<T>(GameObject[] objs, ConditionalDelegate<T> pValidCondition) where T : Component => EditorComponentUtil.FindComponents(objs, pValidCondition);

		public static void ReplaceTextsByTextTMP(params GameObject[] gos) => EditorComponentUtil.ReplaceTextsByTextTMP(gos);

#endregion

		//========================================

#region EditorDrawing

		public static void BoxVerticalOpen(int id, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0) => EditorDrawing.BoxVerticalOpen(id, color, isBox, pFixedWidth, pFixedHeight);

		public static void BoxVerticalClose(int id) => EditorDrawing.BoxVerticalClose(id);

		public static Rect BoxVertical(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0) => EditorDrawing.BoxVertical(doSomething, color, isBox, pFixedWidth, pFixedHeight);

		public static Rect BoxVertical(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0) => EditorDrawing.BoxVertical(pTitle, doSomething, color, isBox, pFixedWidth, pFixedHeight);

		public static Rect BoxHorizontal(Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0) => EditorDrawing.BoxHorizontal(doSomething, color, isBox, pFixedWidth, pFixedHeight);

		public static Rect BoxHorizontal(string pTitle, Action doSomething, Color color = default, bool isBox = false, float pFixedWidth = 0, float pFixedHeight = 0) => EditorDrawing.BoxHorizontal(pTitle, doSomething, color, isBox, pFixedWidth, pFixedHeight);

		public static void DrawLine(float padding = 0) => EditorDrawing.DrawLine(padding);

		public static void Separator(string label = null, Color labelColor = default) => EditorDrawing.Separator(label, labelColor);

		public static void SeparatorBox() => EditorDrawing.SeparatorBox();

#endregion

		//========================================

#region EditorLayout

		public static void GridDraws(int pCell, List<IDraw> pDraws, Color color = default) => EditorLayout.GridDraws(pCell, pDraws, color);

		public static Vector2 ScrollBar(ref Vector2 scrollPos, float height, Action action) => EditorLayout.ScrollBar(ref scrollPos, height, action);

		public static bool Foldout(string label) => EditorLayout.Foldout(label, null);

		public static bool HeaderFoldout(string label, string key = "", bool minimalistic = false, params IDraw[] pHorizontalDraws) => EditorLayout.HeaderFoldout(label, key, minimalistic, pHorizontalDraws);

		public static void HeaderFoldout(string label, string key, Action pOnFoldOut, params IDraw[] pHorizontalDraws) => EditorLayout.HeaderFoldout(label, key, pOnFoldOut, pHorizontalDraws);

		public static void ListReadonlyObjects<T>(string pName, List<T> pList, List<string> pLabels = null, bool pShowObjectBox = true) where T : Object => EditorLayout.ListReadonlyObjects(pName, pList, pLabels, pShowObjectBox);

		public static bool ListObjects<T>(string pName, ref List<T> pObjects, List<string> pLabels, bool pShowObjectBox = true, bool pReadOnly = false, IDraw[] pAdditionalDraws = null) where T : Object =>
			EditorLayout.ListObjects(pName, ref pObjects, pLabels, pShowObjectBox, pReadOnly, pAdditionalDraws);

		public static void PagesForList(int pCount, string pName, Action<int> pOnDraw, IDraw[] p_drawAtFirst = null, IDraw[] p_drawAtLast = null) => EditorLayout.PagesForList(pCount, pName, pOnDraw, p_drawAtFirst, p_drawAtLast);

		public static void ListObjectsWithSearch<T>(ref List<T> pList, string pName, bool pShowObjectBox = true) where T : Object => EditorLayout.ListObjectsWithSearch(ref pList, pName, pShowObjectBox);

		public static bool ListKeyObjects<TKey, TValue>(string pName, ref List<SerializableKeyValue<TKey, TValue>> pList, bool pShowObjectBox = true, bool pReadOnly = false, IDraw[] pAdditionalDraws = null)
			where TValue : Object => EditorLayout.ListKeyObjects(pName, ref pList, pShowObjectBox, pReadOnly, pAdditionalDraws);

		public static string Tabs(string pKey, params string[] pTabsName) => EditorLayout.Tabs(pKey, pTabsName);

		public static void DragDropBox<T>(string pName, Action<T[]> pOnDrop) where T : Object => EditorLayout.DragDropBox(pName, pOnDrop);

		public static ReorderableList CreateReorderableList<T>(T[] pObjects, string pName) where T : Object => EditorLayout.CreateReorderableList(pObjects, pName);

		public static ReorderableList CreateReorderableList<T>(List<T> pObjects, string pName) where T : Object => EditorLayout.CreateReorderableList(pObjects, pName);

#endregion

		//========================================

#region EditorDrawing

		private static void DrawHeaderTitle(string pHeader) => EditorDrawing.DrawHeaderTitle(pHeader);

		public static void DrawTextureIcon(Texture pTexture, Vector2 pSize) => EditorDrawing.DrawTextureIcon(pTexture, pSize);

#endregion

		//========================================

#region EditorGui

		public static bool ConfirmPopup(string pMessage = null, string pYes = null, string pNo = null) => EditorGui.ConfirmPopup(pMessage, pYes, pNo);

		public static bool Button(string label, int width = 0, int height = 0) => EditorGui.Button(label, width, height);

		public static bool ButtonColor(string label, Color color = default, int width = 0, int height = 0) => EditorGui.Button(label, color, width, height);

		public static bool Button(string label, Texture2D icon, Color color = default, int width = 0, int height = 0) => EditorGui.Button(label, icon, color, width, height);

		public static string FolderField(string defaultPath, string label, int labelWidth = 0, bool pFormatToUnityPath = true) => EditorGui.FolderField(defaultPath, label, labelWidth, pFormatToUnityPath);

		public static string FileField(string defaultPath, string label, string extension, int labelWidth = 0, bool pFormatToUnityPath = true) => EditorGui.FileField(defaultPath, label, extension, labelWidth, pFormatToUnityPath);

		public static string TextField(string value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false, Color color = default) => EditorGui.TextField(value, label, labelWidth, valueWidth, readOnly, color);

		public static string TextArea(string value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false) => EditorGui.TextArea(value, label, labelWidth, valueWidth, readOnly);

		public static string DropdownList(string value, string label, string[] selections, int labelWidth = 80, int valueWidth = 0) => EditorGui.DropdownList(value, label, selections, labelWidth, valueWidth);

		public static int DropdownList(int value, string label, int[] selections, int labelWidth = 80, int valueWidth = 0) => EditorGui.DropdownList(value, label, selections, labelWidth, valueWidth);

		public static T DropdownListEnum<T>(T value, string label, int labelWidth = 80, int valueWidth = 0) where T : struct, IConvertible => EditorGui.DropdownList(value, label, labelWidth, valueWidth);

		public static T DropdownList<T>(T selectedObj, string label, List<T> pOptions) where T : Object => EditorGui.DropdownList(selectedObj, label, pOptions);

		public static bool Toggle(bool value, string label, int labelWidth = 80, int valueWidth = 0, Color color = default) => EditorGui.Toggle(value, label, labelWidth, valueWidth, color);

		public static int IntField(int value, string label, int labelWidth = 80, int valueWidth = 0, bool readOnly = false, int pMin = 0, int pMax = 0) => EditorGui.IntField(value, label, labelWidth, valueWidth, readOnly, pMin, pMax);

		public static float FloatField(float value, string label, int labelWidth = 80, int valueWidth = 0, float pMin = 0, float pMax = 0) => EditorGui.FloatField(value, label, labelWidth, valueWidth, pMin, pMax);

		public static Object ObjectField<T>(Object value, string label, int labelWidth = 80, int valueWidth = 0, bool showAsBox = false) => EditorGui.ObjectField<T>(value, label, labelWidth, valueWidth, showAsBox);

		public static void LabelField(string label, int width = 0, bool isBold = true, TextAnchor pTextAnchor = TextAnchor.MiddleLeft, Color pTextColor = default) => EditorGui.Label(label, width, isBold, pTextAnchor, pTextColor);

		public static Color ColorField(Color value, string label, int labelWidth = 80, int valueWidth = 0) => EditorGui.ColorField(value, label, labelWidth, valueWidth);

		public static Vector2 Vector2Field(Vector2 value, string label, int labelWidth = 80, int valueWidth = 0) => EditorGui.Vector2Field(value, label, labelWidth, valueWidth);

		public static Vector3 Vector3Field(Vector3 value, string label, int labelWidth = 80, int valueWidth = 0) => EditorGui.Vector3Field(value, label, labelWidth, valueWidth);

		public static float[] ArrayField(float[] values, string label, bool showHorizontal = true, int labelWidth = 80, int valueWidth = 0) => EditorGui.ArrayField(values, label, showHorizontal, labelWidth, valueWidth);

#endregion

		//========================================

#region SerializedProperty SerializedObject

		public static object GetTargetObjectOfProperty(SerializedProperty prop) => EditorSerializedPropertyExtensions.GetTargetObjectOfProperty(prop);

#endregion

		//========================================

#region EditorBuildUtil

		public static void RemoveDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.RemoveDirective(pSymbol, pTarget);

		public static void RemoveDirective(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.RemoveDirective(pSymbols, pTarget);

		public static void AddDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.AddDirective(pSymbol, pTarget);

		public static void AddDirectives(List<string> pSymbols, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.AddDirectives(pSymbols, pTarget);

		public static string[] GetDirectives(BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.GetDirectives(pTarget);

		public static bool ContainDirective(string pSymbol, BuildTargetGroup pTarget = BuildTargetGroup.Unknown) => EditorBuildUtil.ContainDirective(pSymbol, pTarget);

		public static string[] GetScenePaths() => EditorBuildUtil.GetScenePaths();

		public static string GetBuildName() => EditorBuildUtil.GetBuildName();

#endregion

		//========================================

#region EditorFileUtil

		public static string SaveFilePanel(string mainDirectory, string defaultName, string content, string extension = "json,txt") => EditorFileUtil.SaveFilePanel(mainDirectory, defaultName, content, extension);

		public static void SaveFile(string path, string content) => EditorFileUtil.SaveFile(path, content);

		public static void SaveJsonFilePanel<T>(string pMainDirectory, string defaultName, T obj) => EditorFileUtil.SaveJsonFilePanel(pMainDirectory, defaultName, obj);

		public static void SaveJsonFile<T>(string pPath, T pObj) => EditorFileUtil.SaveJsonFile(pPath, pObj);

		public static bool LoadJsonFilePanel<T>(string pMainDirectory, ref T pOutput) => EditorFileUtil.LoadJsonFilePanel(pMainDirectory, ref pOutput);

		public static string LoadFilePanel(string pMainDirectory, string extensions = "json,txt") => EditorFileUtil.LoadFilePanel(pMainDirectory, extensions);

		public static KeyValuePair<string, string> LoadFilePanel2(string pMainDirectory, string extensions = "json,txt") => EditorFileUtil.LoadFilePanel2(pMainDirectory, extensions);

		public static bool LoadJsonFromFile<T>(string pPath, ref T pOutput) => EditorFileUtil.LoadJsonFromFile(pPath, ref pOutput);

		public static void SaveXMLFile<T>(string pPath, T pObj) => EditorFileUtil.SaveXMLFile(pPath, pObj);

		public static T LoadXMLFile<T>(string pPath) => EditorFileUtil.LoadXMLFile<T>(pPath);

		public static string OpenFolderPanel(string pFolderPath = null) => EditorFileUtil.OpenFolderPanel(pFolderPath);

		public static string FormatPathToUnityPath(string path) => EditorFileUtil.FormatPathToUnityPath(path);

		public static string[] GetDirectories(string path) => EditorFileUtil.GetDirectories(path);


		public static List<string> OpenFilePanelWithFilters(string title, string[] filter) => EditorFileUtil.OpenFilePanelWithFilters(title, filter);

		public static string OpenFilePanel(string title, string extension, string directory = null) => EditorFileUtil.OpenFilePanel(title, extension, directory);

#endregion

		//========================================

#region EditorAssetUtil

		public static void Save(Object pObj) => EditorAssetUtil.Save(pObj);

		public static string GetObjectFolderName(Object pObj) => EditorAssetUtil.GetObjectFolderName(pObj);

		public static Object LoadAsset(string path) => EditorAssetUtil.LoadAsset(path);

		public static T LoadAsset<T>(string path) where T : Object => EditorAssetUtil.LoadAsset<T>(path);

		public static string ObjectToGuid(Object obj) => EditorAssetUtil.ObjectToGuid(obj);

		public static T CreateScriptableAsset<T>(string path) where T : ScriptableObject => EditorAssetUtil.CreateScriptableAsset<T>(path);

		public static List<T> GetObjects<T>(string pPath, string filter, bool getChild = true) where T : Object => EditorAssetUtil.GetObjects<T>(pPath, filter, getChild);

		public static List<AnimationClip> GetAnimClipsFromFBX() => EditorAssetUtil.GetAnimClipsFromFBX();

		public static ModelImporterClipAnimation[] GetAnimationsFromModel(string pPath) => EditorAssetUtil.GetAnimationsFromModel(pPath);

		public static AnimationClip GetAnimationFromModel(string pPath, string pName) => EditorAssetUtil.GetAnimationFromModel(pPath, pName);

		public static void ExportSelectedFoldersToUnityPackage() => EditorAssetUtil.ExportSelectedFoldersToUnityPackage();

		public static void RefreshAssetsInSelectedFolder(string filter) => EditorAssetUtil.RefreshAssetsInSelectedFolder(filter);

		public static void RefreshAssets(string filter, string folderPath = null) => EditorAssetUtil.RefreshAssets(filter, folderPath);

		public static void BuildReferenceMapCache<T>(string[] assetGUIDs, List<T> cachedObjects) where T : Object => EditorAssetUtil.BuildReferenceMapCache(assetGUIDs, cachedObjects);

		public static Dictionary<string, int> SearchAndReplaceGuid<T>(List<T> oldObjects, T newObject, string[] assetGUIDs) where T : Object => EditorAssetUtil.SearchAndReplaceGuid(oldObjects, newObject, assetGUIDs);

		public static string[] ReadMetaFile(Object pObject) => EditorAssetUtil.ReadMetaFile(pObject);

		public static string ReadContentMetaFile(Object pObject) => EditorAssetUtil.ReadContentMetaFile(pObject);

		public static void WriteMetaFile(Object pObject, string[] pLines, bool pRefreshDatabase) => EditorAssetUtil.WriteMetaFile(pObject, pLines, pRefreshDatabase);

		public static Dictionary<string, EditorAssetUtil.SpriteInfo> GetPivotsOfSprites(Sprite pSpriteFrom) => EditorAssetUtil.GetPivotsOfSprites(pSpriteFrom);

		public static void SetTextureReadable(Texture2D p_texture2D, bool p_readable) => EditorAssetUtil.SetTextureReadable(p_texture2D, p_readable);

		public static void CopyPivotAndBorder(Sprite pOriginal, Sprite pTarget, bool pRefreshDatabase) => EditorAssetUtil.CopyPivotAndBorder(pOriginal, pTarget, pRefreshDatabase);

		public static void ExportSpritesFromTexture(Object pObj, string pExportDirectory = null, string pNamePattern = null, bool pRenameOriginal = false) => EditorAssetUtil.ExportSpritesFromTexture(pObj, pExportDirectory, pNamePattern, pRenameOriginal);

		private static bool IsDirectory(string path) => EditorAssetUtil.IsDirectory(path);

		public static void DrawAssetsList<T>(AssetsList<T> assets, string pDisplayName, bool @readonly = false, List<string> labels = null) where T : Object => EditorLayout.DrawAssetsList(assets, pDisplayName, @readonly, labels);

#endregion
	}
}
#endif