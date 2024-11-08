/***
 * Author RadBear - nbhung71711 @gmail.com - 2023
 **/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace RCore.Editor.Tool
{
	public class ImageCompressor : EditorWindow
	{
#region TinyPNG Compression

		private static List<string> m_FilesCompressed;
		private static int m_ImagesProcessedCount;

		private static string AuthKey
		{
			get
			{
				var key = "api:" + Configuration.KeyValues["TINIFY_API_KEY"];
				key = Convert.ToBase64String(Encoding.UTF8.GetBytes(key));
				return $"Basic {key}";
			}
		}

		private static async void CompressTexturesWithTinyPNG(string folderPath)
		{
			m_ImagesProcessedCount = 0;
			if (string.IsNullOrEmpty(folderPath))
				return;
			string[] texturePaths = Directory.GetFiles(folderPath, "*.png", SearchOption.AllDirectories);
			EditorUtility.DisplayProgressBar("Compressing", $"Processing {texturePaths.Length}", 0f);
			for (int i = 0; i < texturePaths.Length; i++)
			{
				string texturePath = texturePaths[i];
				await Compress(texturePath, true);
				EditorUtility.DisplayProgressBar("Compressing", $"Processing {texturePaths.Length}", (i + 1) * 1f / texturePaths.Length);
			}
			EditorUtility.ClearProgressBar();
			Debug.Log($"[{nameof(ImageCompressor)}] Compressed {m_ImagesProcessedCount} textures");
		}

		//{"size":91,"type":"image/png"}
		[Serializable]
		private class InputModel
		{
			public int size;
			public string type;
		}

		//{"size":91,"type":"image/png","width":16,"height":16,"ratio":1,"url":"https://api.tinify.com/output/d6vu2y5mm231fjkjhp6fj22jd648k2vz"}
		[Serializable]
		private class OutputModel
		{
			public int size;
			public string type;
			public int width;
			public int height;
			public float ratio;
			public string url;
		}

		[Serializable]
		private class ResponseModel
		{
			public InputModel input;
			public OutputModel output;
		}

		private static bool NeedCompression(long fileSizeKb, int width, int height, out float compressedSize)
		{
			var size = width * height;
			compressedSize = size / 4f / GetNearestPowerOfTwo(Mathf.Sqrt(size));
			return fileSizeKb < compressedSize;
		}

		private static int GetNearestPowerOfTwo(float number)
		{
			int power = Mathf.CeilToInt(Mathf.Log(number, 2));
			var result = (int)Mathf.Pow(2, power);
			return result;
		}

		private static async Task Compress(string filePath, bool overwrite)
		{
			var fileInfo = new FileInfo(filePath);
			long fileSizeBytes = fileInfo.Length;
			long fileSizeKb = fileSizeBytes / 1024;
			string fileName = Path.GetFileName(filePath);
			var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(filePath);
			if (NeedCompression(fileSizeKb, texture.width, texture.height, out var compressedSize))
			{
				Debug.Log($"[{nameof(ImageCompressor)}] File {fileName} size is {fileSizeKb}kb, it needs to bigger than {compressedSize} to be compressed.");
				return;
			}
			m_FilesCompressed ??= new List<string>();
			if (m_FilesCompressed.Contains(filePath))
			{
				Debug.Log($"[{nameof(ImageCompressor)}] File {fileName} was compressed.");
				return;
			}
			var bytes = await File.ReadAllBytesAsync(filePath);
			var www = UnityWebRequest.Put("https://api.tinify.com/shrink", bytes);
			www.SetRequestHeader("Authorization", AuthKey);
			www.method = UnityWebRequest.kHttpVerbPOST;
			www.timeout = 10;
			try
			{
				var task = www.SendWebRequest();
				while (!task.isDone)
					await Task.Delay(100);

				if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
					Debug.Log(www.error);

				if (!string.IsNullOrEmpty(www.downloadHandler.text))
				{
					try
					{
						var model = JsonUtility.FromJson<ResponseModel>(www.downloadHandler.text);
						www.Dispose();

						var url = model.output.url;
						www = UnityWebRequest.Get(url);
						www.timeout = 10;
						task = www.SendWebRequest();
						while (!task.isDone)
							await Task.Delay(100);

						if (www.downloadHandler.data.Length > 100)
						{
							int sizeBytes = www.downloadHandler.data.Length;
							int sizeKb = sizeBytes / 1024;
							File.WriteAllBytes(overwrite ? filePath : filePath.Replace(".png", "_tiny.png"), www.downloadHandler.data);
							AssetDatabase.Refresh();
							AssetDatabase.ImportAsset(filePath);
							m_FilesCompressed.Add(filePath);
							Debug.Log($"[{nameof(ImageCompressor)}] {m_ImagesProcessedCount}: Compressed file {filePath} by {(fileSizeKb - sizeKb) * 1f / fileSizeKb * 100}% from {fileSizeKb}kb to {sizeKb}kb");
							m_ImagesProcessedCount++;
						}
					}
					catch (Exception e)
					{
						Debug.LogError($"[{nameof(ImageCompressor)}] Compressed file {filePath} failed\n{e}");
					}
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"[{nameof(ImageCompressor)}] Compressed file {filePath} failed\n{e}");
			}
		}

		[MenuItem("Assets/RCore/Compress Textures with TinyPNG")]
		private static async void CompressTexturesWithTinyPNG()
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
					CompressTexturesWithTinyPNG(directoryPath);
				}
				else
				{
					EditorUtility.DisplayProgressBar("Compressing", $"Processing {objects.Length}", 0f);
					string imagePath = AssetDatabase.GetAssetPath(obj);
					var extension = Path.GetExtension(imagePath);
					if (extension == ".png" || extension == ".jpg")
					{
						await Compress(imagePath, true);
						EditorUtility.DisplayProgressBar("Compressing", $"Processing {objects.Length}", (i + 1) * 1f / objects.Length);
					}
					EditorUtility.ClearProgressBar();
				}
			}
		}

#endregion

#region Remove Exif of Images

		[MenuItem("Assets/RCore/Remove Exif Of Textures")]
		private static void RemoveExifOfImages()
		{
			var objects = Selection.GetFiltered(typeof(Object), SelectionMode.Assets);
			if (objects.Length == 0)
				return;
			foreach (var obj in objects)
			{
				bool isFolder = AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(obj));
				if (isFolder)
				{
					string directoryPath = AssetDatabase.GetAssetPath(obj);
					RemoveExifOfImages(directoryPath);
				}
				else
				{
					RemoveExifAndCompressImage(obj);
				}
			}
		}

		private static void RemoveExifAndCompressImage(Object obj)
		{
			string imagePath = AssetDatabase.GetAssetPath(obj);
			var extension = Path.GetExtension(imagePath);
			if (extension == ".png" || extension == ".jpg")
				RemoveExifAndCompressImage(imagePath);
		}

		private static void RemoveExifOfImages(string pDirectoryPath)
		{
			var log = new StringBuilder().AppendFormat($"[{nameof(ImageCompressor)}]");
			var log2 = new StringBuilder().AppendFormat($"[{nameof(ImageCompressor)}]");
			string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { pDirectoryPath });
			foreach (string guid in guids)
			{
				string imagePath = AssetDatabase.GUIDToAssetPath(guid);
				var extension = Path.GetExtension(imagePath);
				if (extension == ".png" || extension == ".jpg")
				{
					var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(imagePath);
					if (texture != null)
					{
						RemoveExifAndCompressImage(imagePath);
						log.AppendFormat("Processed image: {0}\n", imagePath);
					}
				}
				else
				{
					log2.AppendFormat("Unprocessed image: {0}\n", imagePath);
				}
			}
			if (guids.Length > 0)
			{
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
				Debug.Log(log);
				Debug.Log(log2);
			}
		}

		private static void RemoveExifAndCompressImage(string imagePath)
		{
			var texture = new Texture2D(2, 2);
			var bytes = File.ReadAllBytes(imagePath);
			texture.LoadImage(bytes);
			int sizeMb = bytes.Length / 1024 / 1024;
			if (sizeMb >= 5)
			{
				Debug.LogError($"{imagePath} should be corrected format, please check the size of the image and before procession.");
				return;
			}
			// Create a new Texture2D to remove EXIF data
			var newTexture = new Texture2D(texture.width, texture.height, texture.format, false);
			newTexture.SetPixels(texture.GetPixels());
			newTexture.Apply();
			// Encode the new Texture2D as a PNG or JPEG (based on file extension)
			var extension = Path.GetExtension(imagePath).ToLower();
			byte[] newImageData = null;
			if (extension == ".jpg")
				newImageData = newTexture.EncodeToJPG();
			else if (extension == ".png")
				newImageData = newTexture.EncodeToPNG();
			if (newImageData != null)
				// Save the new image data back to the file
				File.WriteAllBytes(imagePath, newImageData);
			// Destroy the temporary textures
			DestroyImmediate(texture);
			DestroyImmediate(newTexture);
			Debug.Log($"[{nameof(ImageCompressor)}] Processed image: {imagePath}");
		}
#endregion
	}
}