/***
 * Author HNB-RaBear - 2019
 **/

#if FIREBASE_STORAGE
using Firebase;
using Firebase.Storage;
#endif
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace RCore.Service
{
	public class WaitForTaskStorage : CustomYieldInstruction
	{
		Task task;

		public WaitForTaskStorage(Task task)
		{
			this.task = task;
		}

		public override bool keepWaiting
		{
			get
			{
				if (task.IsCompleted)
				{
					if (task.IsFaulted)
						LogException(task.Exception);

					return false;
				}
				return true;
			}
		}

		private void LogException(Exception exception)
		{
#if FIREBASE_STORAGE
			var storageException = exception as StorageException;
			if (storageException != null)
			{
				Debug.LogError($"[storage]: Error Code: {storageException.ErrorCode}");
				Debug.LogError($"[storage]: HTTP Result Code: {storageException.HttpResultCode}");
				Debug.LogError($"[storage]: Recoverable: {storageException.IsRecoverableException}");
				Debug.LogError("[storage]: " + storageException.ToString());
			}
			else
#endif
				Debug.LogError(exception.ToString());
		}
	}

	//=======================================================

	public class SavedFileDefinition
	{
		public string rootFolder { get; private set; }
		public string fileName { get; private set; }
		public string metaData { get; private set; }

		public SavedFileDefinition(string pRootFolder, string pFileName, Dictionary<string, string> metaDataDict = null)
		{
			fileName = pFileName;
			rootFolder = pRootFolder;
			metaData = BuildMetaData(metaDataDict);
		}

		public string BuildMetaData(Dictionary<string, string> pMetaDataDict)
		{
			var build = new List<string>();
			foreach (var metaData in pMetaDataDict)
			{
				string key = metaData.Key;
				string value = metaData.Value;
				if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
					build.Add($"{key}={value}");
			}
			return string.Join("\\", build.ToArray());
		}

		public string GetStorageLocation(string pStorageBucket)
		{
			string folder = $"{pStorageBucket}/{rootFolder}";
			return $"{folder}/{fileName}";
		}
	}

	//=======================================================

	public static class RFirebaseStorage
	{
#if FIREBASE_STORAGE

		private static readonly string URI_FILE_SCHEME = Uri.UriSchemeFile + "://";

		//=======================================================

		private static string m_StorageBucket;
		private static bool m_IsDownloading;
		private static bool m_IsUploading;
		private static bool m_IsDeleting;
		private static bool m_Initialized;

		public static bool Initialized => m_Initialized;

		/// <summary>
		/// Cancellation token source for the current operation.
		/// </summary>
		private static CancellationTokenSource m_CancellationTokenSource = new CancellationTokenSource();

		//=========================================================

		public static void Initialize()
		{
			if (m_Initialized)
				return;

			string appBucket = FirebaseApp.DefaultInstance.Options.StorageBucket;
			if (!string.IsNullOrEmpty(appBucket))
				m_StorageBucket = $"gs://{appBucket}";

			m_Initialized = true;
		}

		public static void CancelOperation()
		{
			if ((m_IsUploading || m_IsDownloading || m_IsDeleting) && m_CancellationTokenSource != null)
			{
				try
				{
					Debug.Log("*** Cancelling operation ***");
					m_CancellationTokenSource.Cancel();
					m_CancellationTokenSource = null;
				}
				catch (Exception ex)
				{
					Debug.Log(ex.ToString());
				}
			}
		}

		//== METADATA

		/// <summary>
		/// Download and display Metadata for the storage reference.
		/// </summary>
		private static IEnumerator IEGetMetadata(SavedFileDefinition pStoDef)
		{
			var storageReference = GetStorageReference(pStoDef);
			Debug.Log($"Bucket: {storageReference.Bucket}");
			Debug.Log($"Path: {storageReference.Path}");
			Debug.Log($"Name: {storageReference.Name}");
			Debug.Log($"Parent Path: {(storageReference.Parent != null ? storageReference.Parent.Path : "(root)")}");
			Debug.Log($"Root Path: {storageReference.Root.Path}");
			Debug.Log($"App: {storageReference.Storage.App.Name}");
			var task = storageReference.GetMetadataAsync();
			yield return new WaitForTaskStorage(task);
			if (!(task.IsFaulted || task.IsCanceled))
				Debug.Log(MetadataToString(task.Result, false) + "\n");
		}

		//== DELETE

		public static void Delete(Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(false);
				return;
			}

			var storageReference = GetStorageReference(pStoDef);
			var task = storageReference.DeleteAsync();
			Debug.Log($"Deleting {storageReference.Path}...");

			m_IsDeleting = true;
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				m_IsDeleting = false;

				if (task.IsFaulted)
					Debug.LogError(task.Exception.ToString());

				bool success = !task.IsFaulted && !task.IsCanceled;
				if (success)
					Debug.Log($"{storageReference.Path} deleted");

				pOnFinished?.Invoke(success);
			});
		}

		public static void DeleteWithCoroutine(Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(false);
				return;
			}

			TimerEventsGlobal.Instance.StartCoroutine(IEDelete(pOnFinished, pStoDef));
		}

		private static IEnumerator IEDelete(Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			var storageReference = GetStorageReference(pStoDef);
			var task = storageReference.DeleteAsync();
			Debug.Log($"Deleting {storageReference.Path}...");

			m_IsDeleting = false;
			yield return new WaitForTaskStorage(task);
			m_IsDeleting = false;

			if (task.IsFaulted)
				Debug.LogError(task.Exception.ToString());

			bool success = !task.IsFaulted && !task.IsCanceled;
			if (success)
				Debug.Log($"{storageReference.Path} deleted");

			pOnFinished?.Invoke(success);
		}

		//== DOWNLOAD / UPLOAD STREAM

		public static void UploadStream(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(false);
				return;
			}

			var storageReference = GetStorageReference(pStoDef);
			var task = storageReference.PutStreamAsync(
				new MemoryStream(Encoding.ASCII.GetBytes(pContent)),
				StringToMetadataChange(pStoDef.metaData),
				new StorageProgress<UploadState>(DisplayUploadState),
				m_CancellationTokenSource.Token, null);
			Debug.Log($"Uploading to {storageReference.Path} using stream...");

			m_IsUploading = true;
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				m_IsUploading = false;

				if (!task.IsFaulted && !task.IsCanceled)
					Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
				else
					Debug.LogError($"[storage]: Uploading Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

				if (task.IsFaulted)
					Debug.LogError(task.Exception.ToString());

				pOnFinished?.Invoke(!task.IsFaulted && !task.IsCanceled);
			});
		}

		public static void UploadStreamWithCoroutine(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(false);
				return;
			}

			TimerEventsGlobal.Instance.StartCoroutine(IEUploadStream(pContent, pOnFinished, pStoDef));
		}

		private static IEnumerator IEUploadStream(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			var storageReference = GetStorageReference(pStoDef);
			var task = storageReference.PutStreamAsync(
				new MemoryStream(Encoding.ASCII.GetBytes(pContent)),
				StringToMetadataChange(pStoDef.metaData),
				new StorageProgress<UploadState>(DisplayUploadState),
				m_CancellationTokenSource.Token, null);
			Debug.Log($"Uploading to {storageReference.Path} using stream...");

			m_IsUploading = true;
			yield return new WaitForTaskStorage(task);
			m_IsUploading = false;

			if (!task.IsFaulted && !task.IsCanceled)
				Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
			else
				Debug.LogError($"[storage]: Uploading Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

			if (task.IsFaulted)
				Debug.LogError(task.Exception.ToString());

			pOnFinished?.Invoke(!task.IsFaulted && !task.IsCanceled);
		}

		public static void DownloadStream(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(null, "");
				return;
			}

			string content = "";
			var storageReference = GetStorageReference(pStoDef);
			Debug.Log($"Downloading {storageReference.Path} with stream ...");

			var task = storageReference.GetStreamAsync((stream) =>
			{
				var buffer = new byte[1024];
				int read;
				// Read data to render in the text view.
				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					content += Encoding.Default.GetString(buffer, 0, read);
				}
			}, new StorageProgress<DownloadState>(DisplayDownloadState), m_CancellationTokenSource.Token);

			m_IsDownloading = true;
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				m_IsDownloading = false;

				if (task.IsFaulted)
					Debug.LogError(task.Exception.ToString());

				bool success = !task.IsFaulted && !task.IsCanceled;
				if (success)
					Debug.Log("Finished downloading stream\n");
				else
					Debug.LogError($"[storage]: Downloaded Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

				pOnFinished?.Invoke(task, content);
			});
		}

		public static void DownloadStreamWithCoroutine(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(null, "");
				return;
			}

			TimerEventsGlobal.Instance.StartCoroutine(IEDownloadStream(pOnFinished, pStoDef));
		}

		private static IEnumerator IEDownloadStream(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
		{
			string content = "";
			var storageReference = GetStorageReference(pStoDef);
			Debug.Log($"Downloading {storageReference.Path} with stream ...");

			var task = storageReference.GetStreamAsync((stream) =>
			{
				var buffer = new byte[1024];
				int read;
				// Read data to render in the text view.
				while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
				{
					content += Encoding.Default.GetString(buffer, 0, read);
				}
			}, new StorageProgress<DownloadState>(DisplayDownloadState), m_CancellationTokenSource.Token);

			m_IsDownloading = true;
			yield return new WaitForTaskStorage(task);
			m_IsDownloading = false;

			if (task.IsFaulted)
				Debug.LogError(task.Exception.ToString());

			bool success = !task.IsFaulted && !task.IsCanceled;
			if (success)
				Debug.Log("Finished downloading stream\n");
			else
				Debug.LogError($"[storage]: Downloaded Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

			pOnFinished?.Invoke(task, content);
		}

		//== DOWNLOAD / UPLOAD FILE

		public static void UploadFromFile(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished.Raise(false);
				return;
			}

			var localFilenameUriString = PathToPersistentDataPathUriString(pOriginalFilePath);
			var storageReference = GetStorageReference(pStoDef);
			var task = storageReference.PutFileAsync(
				localFilenameUriString, StringToMetadataChange(pStoDef.metaData),
				new StorageProgress<UploadState>(DisplayUploadState),
				m_CancellationTokenSource.Token, null);
			Debug.Log($"Uploading '{localFilenameUriString}' to '{storageReference.Path}'...");

			m_IsUploading = true;
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				m_IsUploading = false;

				bool success = !task.IsFaulted && !task.IsCanceled;

				if (success)
					Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
				else
					Debug.LogError($"[storage]: Uploading Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

				if (task.IsFaulted)
					Debug.LogError(task.Exception.ToString());

				pOnFinished?.Invoke(success);
			});
		}

		public static void UploadFromFileWithCoroutine(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(false);
				return;
			}

			TimerEventsGlobal.Instance.StartCoroutine(IEUploadFromFile(pOnFinished, pOriginalFilePath, pStoDef));
		}

		private static IEnumerator IEUploadFromFile(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef)
		{
			var localFilenameUriString = PathToPersistentDataPathUriString(pOriginalFilePath);
			var storageReference = GetStorageReference(pStoDef);
			var task = storageReference.PutFileAsync(
				localFilenameUriString, StringToMetadataChange(pStoDef.metaData),
				new StorageProgress<UploadState>(DisplayUploadState),
				m_CancellationTokenSource.Token, null);
			Debug.Log($"Uploading '{localFilenameUriString}' to '{storageReference.Path}'...");

			m_IsUploading = true;
			yield return new WaitForTaskStorage(task);
			m_IsUploading = false;

			if (!task.IsFaulted && !task.IsCanceled)
				Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
			else
				Debug.LogError($"[storage]: Uploading Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

			if (task.IsFaulted)
				Debug.LogError(task.Exception.ToString());

			pOnFinished?.Invoke(!task.IsFaulted && !task.IsCanceled);
		}

		public static void DownloadToFile(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(null, "");
				return;
			}

			var storageReference = GetStorageReference(pStoDef);
			var localFilenameUriString = PathToPersistentDataPathUriString(pOutPutPath);
			var task = storageReference.GetFileAsync(localFilenameUriString, new StorageProgress<DownloadState>(DisplayDownloadState), m_CancellationTokenSource.Token);
			var content = "";
			Debug.Log($"Downloading {storageReference.Path} to {localFilenameUriString}...");

			m_IsDownloading = true;
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				m_IsDownloading = false;

				if (task.IsFaulted)
					Debug.LogError(task.Exception.ToString());

				bool success = !task.IsFaulted && !task.IsCanceled;
				if (success)
				{
					var filename = FileUriStringToPath(localFilenameUriString);
					Debug.Log($"Finished downloading file {localFilenameUriString} ({filename})");
					Debug.Log($"File Size {(new FileInfo(filename)).Length} bytes\n");
					content = File.ReadAllText(filename);
				}
				else
					Debug.LogError($"[storage]: Downloaded Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

				pOnFinished?.Invoke(task, content);
			});
		}

		public static void DownloadToFileWithCoroutine(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(null, "");
				return;
			}

			TimerEventsGlobal.Instance.StartCoroutine(IEDownloadToFile(pOnFinished, pOutPutPath, pStoDef));
		}

		private static IEnumerator IEDownloadToFile(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef)
		{
			var storageReference = GetStorageReference(pStoDef);
			var localFilenameUriString = PathToPersistentDataPathUriString(pOutPutPath);
			var task = storageReference.GetFileAsync(localFilenameUriString, new StorageProgress<DownloadState>(DisplayDownloadState), m_CancellationTokenSource.Token);
			var content = "";
			Debug.Log($"Downloading {storageReference.Path} to {localFilenameUriString}...");

			m_IsDownloading = true;
			yield return new WaitForTaskStorage(task);
			m_IsDownloading = false;

			if (task.IsFaulted)
				Debug.LogError(task.Exception.ToString());

			bool success = !task.IsFaulted && !task.IsCanceled;
			if (success)
			{
				var filename = FileUriStringToPath(localFilenameUriString);
				Debug.Log($"Finished downloading file {localFilenameUriString} ({filename})");
				Debug.Log($"File Size {(new FileInfo(filename)).Length} bytes\n");
				content = File.ReadAllText(filename);
			}
			else
				Debug.LogError($"[storage]: Downloaded Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

			pOnFinished?.Invoke(task, content);
		}

		//== DOWNLOAD / UPLOAD BYTES

		public static void DownloadFile(SavedFileDefinition pStoDef, Action<string> pFoundFile, Action pNotFoundFile, Action pFailed)
		{
			if (!m_Initialized)
				pFailed.Raise();

			DownloadBytes((task, content) =>
			{
				bool success = task != null && !task.IsFaulted && !task.IsCanceled;
				if (success)
					pFoundFile.Raise(content);
				else if (task != null)
				{
					if (task.IsFaulted)
					{
						string exception = task.Exception.ToString().ToLower();
						if (exception.Contains("not found") || exception.Contains("not exist"))
							pNotFoundFile.Raise();
						else
							pFailed.Raise();
					}
					else
						pFailed.Raise();
				}
				else
					pFailed.Raise();
			}, pStoDef);
		}
		public static void UploadBytesWithCoroutine(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(false);
				return;
			}

			TimerEventsGlobal.Instance.StartCoroutine(IEUploadBytes(pContent, pOnFinished, pStoDef));
		}

		/// <summary>
		/// Remember coroutine can break when scene changed
		/// </summary>
		private static IEnumerator IEUploadBytes(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			var storageReference = GetStorageReference(pStoDef);
			var task = storageReference.PutBytesAsync(Encoding.UTF8.GetBytes(pContent), StringToMetadataChange(pStoDef.metaData), new StorageProgress<UploadState>(DisplayUploadState), m_CancellationTokenSource.Token, null);
			Debug.Log($"Uploading to {storageReference.Path} ...");

			m_IsUploading = true;
			yield return new WaitForTaskStorage(task);
			m_IsUploading = false;

			if (!task.IsFaulted && !task.IsCanceled)
				Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
			else
				Debug.LogError($"[storage]: Uploading Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

			if (task.IsFaulted)
				Debug.LogError(task.Exception.ToString());

			pOnFinished?.Invoke(!task.IsFaulted && !task.IsCanceled);
		}

		public static void UploadBytes(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(false);
				return;
			}

			var storageReference = GetStorageReference(pStoDef);
			var task = storageReference.PutBytesAsync(Encoding.UTF8.GetBytes(pContent), StringToMetadataChange(pStoDef.metaData), new StorageProgress<UploadState>(DisplayUploadState), m_CancellationTokenSource.Token, null);
			Debug.Log($"Uploading to {storageReference.Path} ...");

			m_IsUploading = true;
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				m_IsUploading = false;

				bool success = !task.IsFaulted && !task.IsCanceled;
				if (success)
					Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
				else
					Debug.LogError($"[storage]: Uploading Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

				if (task.IsFaulted)
					Debug.LogError(task.Exception.ToString());

				pOnFinished?.Invoke(success);
			});
		}

		public static void DownloadBytes(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(null, "");
				return;
			}

			var storageRef = GetStorageReference(pStoDef);
			var task = storageRef.GetBytesAsync(0, new StorageProgress<DownloadState>(DisplayDownloadState), m_CancellationTokenSource.Token);
			var content = "";
			Debug.Log($"[storage]: Downloading {storageRef.Path} ...");

			m_IsDownloading = true;
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				m_IsDownloading = false;

				bool success = !task.IsFaulted && !task.IsCanceled;
				if (success)
				{
					content = Encoding.Default.GetString(task.Result);
					Debug.Log($"[storage]: Finished downloading bytes\nFile Size {content.Length} bytes\n");
				}
				else
					Debug.LogError($"[storage]: Downloaded Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

				if (task.IsFaulted)
					LogException(task.Exception);

				pOnFinished?.Invoke(task, content);
			});
		}

		public static void DownloadBytesWithCoroutine(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
		{
			if (!m_Initialized || string.IsNullOrEmpty(pStoDef.fileName))
			{
				pOnFinished?.Invoke(null, "");
				return;
			}

			TimerEventsGlobal.Instance.StartCoroutine(IEDownLoadBytes(pOnFinished, pStoDef));
		}

		private static IEnumerator IEDownLoadBytes(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
		{
			var storageRef = GetStorageReference(pStoDef);
			var task = storageRef.GetBytesAsync(0, new StorageProgress<DownloadState>(DisplayDownloadState), m_CancellationTokenSource.Token);
			var content = "";
			Debug.Log($"[storage]: Downloading {storageRef.Path} ...");

			m_IsDownloading = true;
			yield return new WaitForTaskStorage(task);
			m_IsDownloading = false;

			if (task.IsFaulted)
				Debug.LogError(task.Exception.ToString());

			bool success = !task.IsFaulted && !task.IsCanceled;
			if (success)
			{
				content = Encoding.Default.GetString(task.Result);
				Debug.Log($"[storage]: Finished downloading bytes\nFile Size {content.Length} bytes\n");
			}
			else
				Debug.LogError($"[storage]: Downloaded Fail, cancel: {task.IsCanceled}, faulted: {task.IsFaulted}");

			pOnFinished?.Invoke(task, content);

		}

		//==========================

		/// <summary>
		/// Get a local filesystem path from a file:// URI.
		/// </summary>
		private static string FileUriStringToPath(string fileUriString)
		{
			return Uri.UnescapeDataString((new Uri(fileUriString)).PathAndQuery);
		}

		/// <summary>
		/// Retrieve a storage reference from the user specified path.
		/// </summary>
		private static StorageReference GetStorageReference(SavedFileDefinition pStoDef)
		{
			string location = pStoDef.GetStorageLocation(m_StorageBucket);
			// If this is an absolute path including a bucket create a storage instance.
			if (location.StartsWith("gs://") || location.StartsWith("http://") || location.StartsWith("https://"))
			{
				var storageUri = new Uri(location);
				var firebaseStorage = FirebaseStorage.GetInstance($"{storageUri.Scheme}://{storageUri.Host}");
				return firebaseStorage.GetReferenceFromUrl(location);
			}

			// When using relative paths use the default storage instance which uses the bucket supplied on creation of FirebaseApp.
			return FirebaseStorage.DefaultInstance.GetReference(location);
		}

		/// <summary>
		/// Get the local filename as a URI relative to the persistent data path if the path isn't already a file URI.
		/// </summary>
		private static string PathToPersistentDataPathUriString(string filename)
		{
			if (filename.StartsWith(URI_FILE_SCHEME))
				return filename;

			return $"{URI_FILE_SCHEME}{Application.persistentDataPath}/{filename}";
		}

		/// <summary>
		/// Write upload state to the log.
		/// </summary>
		private static void DisplayUploadState(UploadState uploadState)
		{
			if (m_IsUploading)
				Debug.Log($"Uploading {uploadState.Reference.Name}: {uploadState.BytesTransferred} out of {uploadState.TotalByteCount}");
		}

		/// <summary>
		/// Convert a string in the form:
		/// key1=value1
		/// ...
		/// keyN=valueN
		/// to a MetadataChange object.
		/// If an empty string is provided this method returns null.
		/// </summary>
		private static MetadataChange StringToMetadataChange(string metadataString)
		{
			var metadataChange = new MetadataChange();
			var customMetadata = new Dictionary<string, string>();
			bool hasMetadata = false;
			foreach (var metadataStringLine in metadataString.Split(new char[] { '\n' }))
			{
				if (metadataStringLine.Trim() == "")
					continue;

				var keyValue = metadataStringLine.Split(new char[] { '=' });
				if (keyValue.Length != 2)
				{
					Debug.Log($"Ignoring malformed metadata line '{metadataStringLine}' tokens={keyValue.Length}");
					continue;
				}

				hasMetadata = true;

				var key = keyValue[0];
				var value = keyValue[1];
				if (key == "CacheControl")
					metadataChange.CacheControl = value;
				else if (key == "ContentDisposition")
					metadataChange.ContentDisposition = value;
				else if (key == "ContentEncoding")
					metadataChange.ContentEncoding = value;
				else if (key == "ContentLanguage")
					metadataChange.ContentLanguage = value;
				else if (key == "ContentType")
					metadataChange.ContentType = value;
				else
					customMetadata[key] = value;
			}
			if (customMetadata.Count > 0)
				metadataChange.CustomMetadata = customMetadata;
			return hasMetadata ? metadataChange : null;
		}

		/// <summary>
		/// Convert a Metadata object to a string.
		/// </summary>
		private static string MetadataToString(StorageMetadata metadata, bool onlyMutableFields)
		{
			var fieldsAndValues = new Dictionary<string, object>
			{
				{ "ContentType", metadata.ContentType },
				{ "CacheControl", metadata.CacheControl },
				{ "ContentDisposition", metadata.ContentDisposition },
				{ "ContentEncoding", metadata.ContentEncoding },
				{ "ContentLanguage", metadata.ContentLanguage }
			};
			if (!onlyMutableFields)
			{
				foreach (var kv in new Dictionary<string, object>
				         {
					         { "Reference", metadata.Reference != null ? metadata.Reference.Path : null },
					         { "Path", metadata.Path },
					         { "Name", metadata.Name },
					         { "Bucket", metadata.Bucket },
					         { "Generation", metadata.Generation },
					         { "MetadataGeneration", metadata.MetadataGeneration },
					         { "CreationTimeMillis", metadata.CreationTimeMillis },
					         { "UpdatedTimeMillis", metadata.UpdatedTimeMillis },
					         { "SizeBytes", metadata.SizeBytes },
					         { "Md5Hash", metadata.Md5Hash }
				         })
				{
					fieldsAndValues[kv.Key] = kv.Value;
				}
			}
			foreach (var key in metadata.CustomMetadataKeys)
			{
				fieldsAndValues[key] = metadata.GetCustomMetadata(key);
			}
			var fieldAndValueStrings = new List<string>();
			foreach (var kv in fieldsAndValues)
			{
				fieldAndValueStrings.Add($"{kv.Key}={kv.Value}");
			}
			return string.Join("\n", fieldAndValueStrings.ToArray());
		}

		/// <summary>
		/// Write download state to the log.
		/// </summary>
		private static void DisplayDownloadState(DownloadState downloadState)
		{
			if (m_IsDownloading)
				Debug.Log($"Downloading {downloadState.Reference.Name}: {downloadState.BytesTransferred} out of {downloadState.TotalByteCount}");
		}

		private static void LogException(Exception exception)
		{
			var storageException = exception as StorageException;
			if (storageException != null)
			{
				Debug.LogError($"[storage]: Error Code: {storageException.ErrorCode}");
				Debug.LogError($"[storage]: HTTP Result Code: {storageException.HttpResultCode}");
				Debug.LogError($"[storage]: Recoverable: {storageException.IsRecoverableException}");
				Debug.LogError("[storage]: " + storageException.ToString());
			}
			else
			{
				Debug.LogError(exception.ToString());
			}
		}

#else
        public static bool Initialized => false;
        public static void Initialize() { }
        public static void CancelOperation() { }
        public static void Delete(Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            pOnFinished?.Invoke(false);
        }
        public static void DeleteWithCoroutine(Action<bool> pOnFinished, SavedFileDefinition pStoDef) { }
        public static void UploadStream(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            pOnFinished?.Invoke(false);
        }
        public static void UploadStreamWithCoroutine(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef) { }
        public static void DownloadStream(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            pOnFinished?.Invoke(Task.FromResult(0), "");
        }
        public static void DownloadStreamWithCoroutine(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef) { }
        public static void UploadFromFile(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef)
        {
            pOnFinished?.Invoke(false);
        }
        public static void UploadFromFileWithCoroutine(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef) { }
        public static void DownloadToFile(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef)
        {
            pOnFinished?.Invoke(Task.FromResult(0), "");
        }
        public static void DownloadToFileWithCoroutine(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef) { }
        public static void UploadBytesWithCoroutine(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef) { }
        public static void UploadBytes(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            pOnFinished?.Invoke(false);
        }
        public static void DownloadBytes(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            pOnFinished?.Invoke(Task.FromResult(0), "");
        }
        public static void DownloadBytesWithCoroutine(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef) { }
        public static void DownloadFile(SavedFileDefinition pStoDef, Action<string> pFoundFile, Action pNotFoundFile, Action pFailed)
        {
            pFailed?.Invoke();
        }
#endif
	}
}