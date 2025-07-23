#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace RCore.Editor
{
	public static class EditorAssetUtil
	{
		public struct SpriteInfo
		{
			public string name;
			public Vector2 pivot;
			public Vector4 border;
			public int alignment;
		}

		private static Dictionary<int, string> m_ObjectFolderCaches;
		private static Dictionary<string, HashSet<string>> m_InverseReferenceMap;
		private static int m_ReferencesCount;
		
		public static void Save(Object pObj)
		{
			EditorUtility.SetDirty(pObj);
			AssetDatabase.SaveAssets();
		}
		
		public static void SaveAsset(Object obj)
		{
			EditorUtility.SetDirty(obj);
			AssetDatabase.SaveAssets();
		}

		public static T CreateScriptableAsset<T>(string path) where T : ScriptableObject
		{
			var asset = ScriptableObject.CreateInstance<T>();

			var directoryPath = Path.GetDirectoryName(path);
			if (!Directory.Exists(directoryPath))
				if (directoryPath != null)
					Directory.CreateDirectory(directoryPath);

			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();
			EditorUtility.FocusProjectWindow();
			return asset;
		}
		
		public static string GetObjectFolderName(Object pObj)
		{
			m_ObjectFolderCaches ??= new Dictionary<int, string>();
			if (m_ObjectFolderCaches.ContainsKey(pObj.GetInstanceID()))
				return m_ObjectFolderCaches[pObj.GetInstanceID()];

			var path = AssetDatabase.GetAssetPath(pObj);
			var pathWithoutFilename = Path.GetDirectoryName(path);
			var pathSplit = pathWithoutFilename.Split(Path.DirectorySeparatorChar);
			string folder = pathSplit[pathSplit.Length - 1];
			m_ObjectFolderCaches.Add(pObj.GetInstanceID(), folder);
			return folder;
		}

		public static void ClearObjectFolderCaches() => m_ObjectFolderCaches?.Clear();

		public static Object LoadAsset(string path)
		{
			if (string.IsNullOrEmpty(path)) return null;
			return AssetDatabase.LoadMainAssetAtPath(path);
		}
		
		/// <summary>
		/// Convenience function to load an asset of specified type, given the full path to it.
		/// </summary>
		public static T LoadAsset<T>(string path) where T : Object
		{
			var obj = LoadAsset(path);
			if (obj == null) return null;

			var val = obj as T;
			if (val != null) return val;

			if (typeof(T).IsSubclassOf(typeof(Component)))
				if (obj is GameObject go)
					return go.GetComponent(typeof(T)) as T;

			return null;
		}

		public static string ObjectToGuid(Object obj)
		{
			string path = AssetDatabase.GetAssetPath(obj);
			return !string.IsNullOrEmpty(path) ? AssetDatabase.AssetPathToGUID(path) : null;
		}

		public static void ExportSpritesFromTexture(Object pObj, string pExportDirectory = null, string pNamePattern = null, bool pRenameOriginal = false)
		{
			var results = new List<Sprite>();
			string path = AssetDatabase.GetAssetPath(pObj);
			var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
			if (sprites.Length > 0)
			{
				if (string.IsNullOrEmpty(pExportDirectory))
					pExportDirectory = Path.GetDirectoryName(path);
				var texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
				if (!texture2D.isReadable)
					SetTextureReadable(texture2D, true);
				foreach (var sprite in sprites)
				{
					int x = (int)sprite.rect.x;
					int y = (int)sprite.rect.y;
					int width = (int)sprite.rect.width;
					int height = (int)sprite.rect.height;
					var newTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
					var pixels = sprite.texture.GetPixels(x, y, width, height);
					newTexture.SetPixels(pixels);
					newTexture.Apply();
					byte[] newTextureData = newTexture.EncodeToPNG();

					string customName = sprite.name;
					if (!string.IsNullOrEmpty(pNamePattern))
					{
						var match = Regex.Match(customName, @"\d+$");
						if (match.Success)
						{
							int number = int.Parse(match.Value);
							string numberStr = number.ToString();
							if (sprites.Length > 100)
								numberStr = number.ToString("D3");
							else if (sprites.Length > 10)
								numberStr = number.ToString("D2");
							customName = pNamePattern + numberStr;
						}
					}
					string newSpritePath = Path.Combine(pExportDirectory, $"{customName}.png");
					File.WriteAllBytes(newSpritePath, newTextureData);
					Object.DestroyImmediate(newTexture);
				}
				AssetDatabase.Refresh();
				string[] metaFileContent = null;
				if (pRenameOriginal)
					metaFileContent = ReadMetaFile(pObj);

				foreach (var sprite in sprites)
				{
					string customName = sprite.name;
					if (!string.IsNullOrEmpty(pNamePattern))
					{
						var match = Regex.Match(customName, @"\d+$");
						if (match.Success)
						{
							int number = int.Parse(match.Value);
							string numberStr = number.ToString();
							if (sprites.Length > 100)
								numberStr = number.ToString("D3");
							else if (sprites.Length > 10)
								numberStr = number.ToString("D2");
							customName = pNamePattern + numberStr;
						}
					}
					string newSpritePath = Path.Combine(pExportDirectory, $"{customName}.png");
					var newSprite = AssetDatabase.LoadAssetAtPath<Sprite>(newSpritePath);
					CopyPivotAndBorder(sprite, newSprite, false);
					results.Add(newSprite);

					if (pRenameOriginal)
					{
						for (int line = 0; line < metaFileContent.Length; line++)
						{
							string pattern = $@"\b{sprite.name}\b";
							metaFileContent[line] = Regex.Replace(metaFileContent[line], pattern, customName);
						}
					}
				}
				if (pRenameOriginal)
				{
					string projectPath = Application.dataPath.Replace("/Assets", "");
					string metaPath = $"{projectPath}\\{AssetDatabase.GetAssetPath(pObj)}.meta";
					File.WriteAllLines(metaPath, metaFileContent);
				}
				AssetDatabase.Refresh();
			}
		}

		public static void CopyPivotAndBorder(Sprite pOriginal, Sprite pTarget, bool pRefreshDatabase)
		{
			var spriteInfo = GetPivotsOfSprites(pOriginal);
			var pivotForm = spriteInfo[pOriginal.name].pivot;
			var borderFrom = spriteInfo[pOriginal.name].border;
			var alignmentFrom = spriteInfo[pOriginal.name].alignment;

			var lines = ReadMetaFile(pTarget);
			var nameLines = lines.Where(line => line.Trim().StartsWith("name:", StringComparison.OrdinalIgnoreCase)).ToList();
			if (nameLines.Count > 0) //SpriteTo is inside a atlas
			{
				int nameIndex = 0;
				bool foundName = false;
				for (int i = 0; i < lines.Length; i++)
				{
					bool found = false;
					int spaceIndex = 0;
					if (lines[i].Trim().StartsWith("name:", StringComparison.OrdinalIgnoreCase))
					{
						string name = lines[i].Replace("name:", "").Trim();
						if (name == pTarget.name)
						{
							nameIndex = i;
							foundName = true;
						}
					}
					if (foundName && i > nameIndex && lines[i].Trim().StartsWith("alignment:", StringComparison.OrdinalIgnoreCase))
					{
						spaceIndex = lines[i].IndexOf("alignment:", StringComparison.OrdinalIgnoreCase);
						lines[i] = $"alignment: {alignmentFrom}"; //Replace pivot
					}
					else if (foundName && i > nameIndex && lines[i].Trim().StartsWith("pivot:", StringComparison.OrdinalIgnoreCase))
					{
						if (alignmentFrom == 0 && pivotForm == Vector2.zero)
							continue;
						spaceIndex = lines[i].IndexOf("pivot:", StringComparison.OrdinalIgnoreCase);
						lines[i] = $"pivot: {{x: {pivotForm.x}, y: {pivotForm.y}}}"; //Replace pivot
					}
					else if (foundName && i > nameIndex && lines[i].Trim().StartsWith("border:", StringComparison.OrdinalIgnoreCase))
					{
						spaceIndex = lines[i].IndexOf("border:", StringComparison.OrdinalIgnoreCase);
						lines[i] = $"border: {{x: {borderFrom.x}, y: {borderFrom.y}, z: {borderFrom.z}, w: {borderFrom.w}}}"; //Replace border
						found = true;
					}
					if (spaceIndex > 0)
					{
						for (int s = 0; s < spaceIndex; s++)
							lines[i] = lines[i].Insert(0, " ");
					}
					if (found)
						break;
				}
			}
			else
			{
				for (int i = 0; i < lines.Length; i++)
				{
					bool found = false;
					int spaceIndex = 0;
					if (lines[i].Trim().StartsWith("alignment:", StringComparison.OrdinalIgnoreCase))
					{
						spaceIndex = lines[i].IndexOf("alignment:", StringComparison.OrdinalIgnoreCase);
						lines[i] = $"alignment: {alignmentFrom}"; //Replace pivot
					}
					else if (lines[i].Trim().StartsWith("spritePivot:", StringComparison.OrdinalIgnoreCase))
					{
						if (alignmentFrom == 0 && pivotForm == Vector2.zero)
							continue;
						spaceIndex = lines[i].IndexOf("spritePivot:", StringComparison.OrdinalIgnoreCase);
						lines[i] = $"spritePivot: {{x: {pivotForm.x}, y: {pivotForm.y}}}"; //Replace pivot
					}
					else if (lines[i].Trim().StartsWith("spriteBorder:", StringComparison.OrdinalIgnoreCase))
					{
						spaceIndex = lines[i].IndexOf("spriteBorder:", StringComparison.OrdinalIgnoreCase);
						lines[i] = $"spriteBorder: {{x: {borderFrom.x}, y: {borderFrom.y}, z: {borderFrom.z}, y: {borderFrom.w}}}"; //Replace border
						found = true;
					}
					if (spaceIndex > 0)
						for (int s = 0; s < spaceIndex; s++)
							lines[i] = lines[i].Insert(0, " ");
					if (found)
						break;
				}
			}

			WriteMetaFile(pTarget, lines, pRefreshDatabase);
		}

		public static Dictionary<string, int> SearchAndReplaceGuid<T>(List<T> oldObjects, T newObject, string[] assetGUIDs) where T : Object
		{
			if (assetGUIDs == null)
			{
				const string searchFilter = "t:Object";
				string[] searchDirectories = { "Assets" };
				assetGUIDs = AssetDatabase.FindAssets(searchFilter, searchDirectories);
			}
			var updatedAssets = new Dictionary<string, int>();

			if (oldObjects.Count == 0)
				return updatedAssets;

			var inverseReferenceMap = new Dictionary<string, HashSet<string>>();
			int referencesCount = 0;
			if (m_InverseReferenceMap == null)
			{
				// Initialize map to store all paths that have a reference to our selectedGuids
				foreach (var selectedObj in oldObjects)
				{
					if (selectedObj == null)
						continue;
					string selectedPath = AssetDatabase.GetAssetPath(selectedObj);
					string selectedGuid = AssetDatabase.AssetPathToGUID(selectedPath);
					inverseReferenceMap[selectedGuid] = new HashSet<string>();
				}

				// Scan all assets and store the inverse reference if contains a reference to any selectedGuid...
				var scanProgress = 0;
				foreach (var guid in assetGUIDs)
				{
					scanProgress++;
					var path = AssetDatabase.GUIDToAssetPath(guid);
					if (IsDirectory(path))
						continue;

					var dependencies = AssetDatabase.GetDependencies(path);
					foreach (var dependency in dependencies)
					{
						EditorUtility.DisplayProgressBar("Scanning guid references on:", path, (float)scanProgress / assetGUIDs.Length);

						var dependencyGuid = AssetDatabase.AssetPathToGUID(dependency);
						if (inverseReferenceMap.ContainsKey(dependencyGuid))
						{
							inverseReferenceMap[dependencyGuid].Add(path);

							// Also include .meta path. This fixes broken references when an FBX uses external materials
							// var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
							// inverseReferenceMap[dependencyGUID].Add(metaPath);

							referencesCount++;
						}
					}
				}
			}
			else
			{
				inverseReferenceMap = m_InverseReferenceMap;
				referencesCount = m_ReferencesCount;
			}

			string newPath = AssetDatabase.GetAssetPath(newObject);
			string newGuid = AssetDatabase.AssetPathToGUID(newPath);
			AssetDatabase.TryGetGUIDAndLocalFileIdentifier(newObject, out string assetId, out long newFileId);
			var countProgress = 0;
			int countReplaced = 0;
			foreach (var selectedObj in oldObjects)
			{
				if (selectedObj == null)
					continue;
				bool found = false;
				string selectedPath = AssetDatabase.GetAssetPath(selectedObj);
				string selectedGuid = AssetDatabase.AssetPathToGUID(selectedPath);
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(selectedObj, out assetId, out long selectedFileId);
				var referencePaths = inverseReferenceMap[selectedGuid];
				foreach (var referencePath in referencePaths)
				{
					if (referencePath == selectedPath)
						continue;

					countProgress++;

					EditorUtility.DisplayProgressBar($"Replacing GUID: {selectedPath}", referencePath, (float)countProgress / referencesCount);

					if (IsDirectory(referencePath))
						continue;

					var contents = File.ReadAllText(referencePath);

					if (contents.Contains($"fileID: {selectedFileId}, guid: {selectedGuid}"))
					{
						contents = contents.Replace($"fileID: {selectedFileId}, guid: {selectedGuid}", $"fileID: {newFileId}, guid: {newGuid}");
						File.WriteAllText(referencePath, contents);
						countReplaced++;
						found = true;
					}
				}

				UnityEngine.Debug.Log($"Replace GUID in: {selectedPath}");
				updatedAssets.Add(selectedPath, countReplaced);

				if (found)
					EditorUtility.SetDirty(selectedObj);
			}
			return updatedAssets;
		}

		public static void SetTextureReadable(Texture2D p_texture2D, bool p_readable)
		{
			var lines = ReadMetaFile(p_texture2D);
			for (int i = 0; i < lines.Length; i++)
			{
				if (lines[i].Trim().StartsWith("isReadable:", StringComparison.OrdinalIgnoreCase))
				{
					int spaceIndex = lines[i].IndexOf("isReadable:", StringComparison.OrdinalIgnoreCase);
					string readable = p_readable ? "1" : "0";
					lines[i] = $"isReadable: {readable}"; //Replace pivot
					for (int s = 0; s < spaceIndex; s++)
						lines[i] = lines[i].Insert(0, " ");
					break;
				}
			}
			WriteMetaFile(p_texture2D, lines, true);
		}

		public static string[] ReadMetaFile(Object pObject)
		{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			string path = $"{projectPath}\\{AssetDatabase.GetAssetPath(pObject)}";
			string metaPath = $"{path}.meta";
			string[] lines = File.ReadAllLines(metaPath);
			return lines;
		}

		public static void WriteMetaFile(Object pObject, string[] pLines, bool pRefreshDatabase)
		{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			string path = $"{projectPath}\\{AssetDatabase.GetAssetPath(pObject)}";
			string metaPath = $"{path}.meta";
			File.WriteAllLines(metaPath, pLines);
			if (pRefreshDatabase)
				AssetDatabase.Refresh();
		}

		public static string ReadContentMetaFile(Object pObject)
		{
			string projectPath = Application.dataPath.Replace("/Assets", "");
			string path = $"{projectPath}\\{AssetDatabase.GetAssetPath(pObject)}";
			string metaPath = $"{path}.meta";
			string content = File.ReadAllText(metaPath);
			return content;
		}

		public static Dictionary<string, SpriteInfo> GetPivotsOfSprites(Sprite pSpriteFrom)
		{
			var results = new Dictionary<string, SpriteInfo>();
			var lines = ReadMetaFile(pSpriteFrom);
			var nameLines = lines.Where(line => line.Trim().StartsWith("name:", StringComparison.OrdinalIgnoreCase)).ToList();
			if (nameLines.Count > 0) //SpriteFrom is inside a atlas
			{
				//Get names of all sprites inside atlas which contain spriteFrom
				var names = new List<string>();
				foreach (var line in nameLines)
				{
					string name = line.Replace("name: ", "").Trim();
					names.Add(name);
				}

				var alignmentLines = lines.Where(line => line.Trim().StartsWith("alignment:", StringComparison.OrdinalIgnoreCase)).ToList();
				var alignments = new List<int>();
				for (int i = 0; i < alignmentLines.Count; i++)
				{
					if (i == 0)
						continue;
					string line = alignmentLines[i];
					var alignmentStr = line.Replace("alignment: ", "").Trim();
					alignments.Add(int.Parse(alignmentStr));
				}

				//Get pivots of all sprites inside atlas which contain spriteFrom
				var pivotLines = lines.Where(line => line.Trim().StartsWith("pivot:", StringComparison.OrdinalIgnoreCase)).ToList();
				var pivots = new List<Vector2>();
				foreach (var line in pivotLines)
				{
					var pivotStr = line.Replace("pivot: ", "").Trim();
					var pivot = JsonUtility.FromJson<RVector2>(pivotStr);
					pivots.Add(new Vector2(pivot.x, pivot.y));
				}

				var borders = new List<Vector4>();
				var borderLines = lines.Where(line => line.Trim().StartsWith("border:", StringComparison.OrdinalIgnoreCase)).ToList();
				foreach (var line in borderLines)
				{
					var borderStr = line.Replace("border: ", "").Trim();
					var border = JsonUtility.FromJson<RVector4>(borderStr);
					borders.Add(new Vector4(border.x, border.y, border.z, border.w));
				}
				for (int i = 0; i < names.Count; i++)
					results.Add(names[i], new SpriteInfo
					{
						name = names[i],
						pivot = pivots[i],
						border = borders[i],
						alignment = alignments[i],
					});
			}
			else
			{
				var alignmentLine = lines.First(line => line.Trim().StartsWith("alignment: ", StringComparison.OrdinalIgnoreCase));
				var alignmentStr = alignmentLine.Replace("alignment: ", "").Trim();
				var alignment = int.Parse(alignmentStr);

				var pivotLine = lines.First(line => line.Trim().StartsWith("spritePivot: ", StringComparison.OrdinalIgnoreCase));
				var pivotStr = pivotLine.Replace("spritePivot: ", "").Trim();
				var pivot = JsonUtility.FromJson<RVector2>(pivotStr);

				var borderStr = lines.First(line => line.Trim().StartsWith("spriteBorder: ", StringComparison.OrdinalIgnoreCase));
				var border = JsonUtility.FromJson<RVector4>(borderStr);

				results.Add(pSpriteFrom.name, new SpriteInfo
				{
					name = pSpriteFrom.name,
					pivot = new Vector2(pivot.x, pivot.y),
					border = new Vector4(border.x, border.y, border.z, border.w),
					alignment = alignment,
				});
			}
			return results;
		}
		
		public static void BuildReferenceMapCache<T>(string[] assetGUIDs, List<T> cachedObjects) where T : Object
		{
			if (assetGUIDs == null)
			{
				const string searchFilter = "t:Object";
				string[] searchDirectories = { "Assets" };
				assetGUIDs = AssetDatabase.FindAssets(searchFilter, searchDirectories);
			}

			m_InverseReferenceMap = new Dictionary<string, HashSet<string>>();
			m_ReferencesCount = 0;

			// Initialize map to store all paths that have a reference to our selectedGuids
			foreach (var selectedObj in cachedObjects)
			{
				string selectedPath = AssetDatabase.GetAssetPath(selectedObj);
				string selectedGuid = AssetDatabase.AssetPathToGUID(selectedPath);
				m_InverseReferenceMap[selectedGuid] = new HashSet<string>();
			}

			// Scan all assets and store the inverse reference if contains a reference to any selectedGuid...
			var scanProgress = 0;
			foreach (var guid in assetGUIDs)
			{
				scanProgress++;
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (IsDirectory(path))
					continue;

				var dependencies = AssetDatabase.GetDependencies(path);
				foreach (var dependency in dependencies)
				{
					EditorUtility.DisplayProgressBar("Scanning guid references on:", path, (float)scanProgress / assetGUIDs.Length);

					var dependencyGuid = AssetDatabase.AssetPathToGUID(dependency);
					if (m_InverseReferenceMap.ContainsKey(dependencyGuid))
					{
						m_InverseReferenceMap[dependencyGuid].Add(path);

						// Also include .meta path. This fixes broken references when an FBX uses external materials
						// var metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(path);
						// inverseReferenceMap[dependencyGUID].Add(metaPath);

						m_ReferencesCount++;
					}
				}
			}
		}
		
		public static void RefreshAssets(string filter, string folderPath = null)
		{
			if (string.IsNullOrEmpty(folderPath))
			{
				folderPath = EditorFileUtil.OpenFolderPanel();
				if (string.IsNullOrEmpty(folderPath))
					return;
				folderPath = EditorFileUtil.FormatPathToUnityPath(folderPath);
			}
			var assetGUIDs = AssetDatabase.FindAssets(filter, new[] { folderPath });
			foreach (string guid in assetGUIDs)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
				if (asset != null)
					EditorUtility.SetDirty(asset);
			}
			AssetDatabase.SaveAssets();
		}
		
		public static void RefreshAssetsInSelectedFolder(string filter)
		{
			var objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
			if (objects.Length == 0)
				return;
			for (int i = 0; i < objects.Length; i++)
			{
				var obj = objects[i];
				bool isFolder = AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj));
				if (isFolder)
				{
					string directoryPath = AssetDatabase.GetAssetPath(obj);
					RefreshAssets(filter, directoryPath);
				}
			}
		}
		
		public static void ExportSelectedFoldersToUnityPackage()
		{
			var objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
			if (objects.Length == 0)
				return;

			var folders = new List<string>();
			for (int i = 0; i < objects.Length; i++)
			{
				var obj = objects[i];
				bool isFolder = AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj));
				if (isFolder)
					folders.Add(AssetDatabase.GetAssetPath(obj));
			}
			if (folders.Count > 0)
			{
				string directoryPath = AssetDatabase.GetAssetPath(objects[0]);
				string packagePath = EditorUtility.SaveFilePanel("Export Unity Package", directoryPath, objects[0].name + ".unitypackage", "unitypackage");
				AssetDatabase.ExportPackage(folders.ToArray(), packagePath, ExportPackageOptions.Recurse | ExportPackageOptions.Interactive);
			}
		}
		
		/// <summary>
		/// Example: GetObjects<AudioClip>(@"Assets\Game\Sounds\Musics", "t:AudioClip")
		/// </summary>
		/// <returns></returns>
		public static List<T> GetObjects<T>(string pPath, string filter, bool getChild = true)
			where T : Object
		{
			var directories = EditorFileUtil.GetDirectories(pPath);

			var list = new List<T>();

			var resources = AssetDatabase.FindAssets(filter, directories);

			foreach (var re in resources)
			{
				if (getChild)
				{
					var childAssets = AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(re));
					foreach (var child in childAssets)
					{
						if (child is T o)
						{
							list.Add(o);
						}
					}
				}
				else
				{
					list.Add(Assign<T>(AssetDatabase.GUIDToAssetPath(re)));
				}
			}

			return list;
		}
		
		private static T Assign<T>(string pPath) where T : Object
		{
			return AssetDatabase.LoadAssetAtPath(pPath, typeof(T)) as T;
		}
		
		public static bool IsDirectory(string path) => File.GetAttributes(path).HasFlag(FileAttributes.Directory);
		
		public static ModelImporterClipAnimation[] GetAnimationsFromModel(string pPath)
		{
			var mi = AssetImporter.GetAtPath(pPath) as ModelImporter;
			if (mi != null)
				return mi.defaultClipAnimations;
			return null;
		}

		public static AnimationClip GetAnimationFromModel(string pPath, string pName)
		{
			//var anims = GetAnimationsFromModel(pPath);
			//if (anims != null)
			//    foreach (var anim in anims)
			//        if (anim.name == pName)
			//            return anim;
			//return null;

			var representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(pPath);
			foreach (var asset in representations)
			{
				var clip = asset as AnimationClip;
				if (clip != null && clip.name == pName)
					return clip;
			}

			return null;
		}
		
		public static List<AnimationClip> GetAnimClipsFromFBX()
		{
			var list = new List<AnimationClip>();
			var selections = Selection.objects;
			foreach (var s in selections)
			{
				var path = AssetDatabase.GetAssetPath(s);
				var representations = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
				foreach (var asset in representations)
				{
					var clip = asset as AnimationClip;
					if (clip != null)
						list.Add(clip);
				}
			}

			return list;
		}
	}
}
#endif