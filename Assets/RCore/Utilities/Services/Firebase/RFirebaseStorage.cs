/**
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/

#if ACTIVE_FIREBASE_STORAGE
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
using RCore.Common;
using Debug = UnityEngine.Debug;

namespace RCore.Service.RFirebase.Storage
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

        protected void LogException(Exception exception)
        {
#if ACTIVE_FIREBASE_STORAGE
            var storageException = exception as StorageException;
            if (storageException != null)
            {
                Debug.LogError(string.Format("[storage]: Error Code: {0}", storageException.ErrorCode));
                Debug.LogError(string.Format("[storage]: HTTP Result Code: {0}", storageException.HttpResultCode));
                Debug.LogError(string.Format("[storage]: Recoverable: {0}", storageException.IsRecoverableException));
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
            List<string> build = new List<string>();
            foreach (var metaData in pMetaDataDict)
            {
                string key = metaData.Key;
                string value = metaData.Value;
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
                    build.Add(string.Format("{0}={1}", key, value));
            }
            return string.Join("\\", build.ToArray());
        }

        public string GetStorageLocation(string pStorageBucket)
        {
            string folder = string.Format("{0}/{1}", pStorageBucket, rootFolder);
            return string.Format("{0}/{1}", folder, fileName);
        }
    }

    //=======================================================

    public class RFirebaseStorage
    {
        private static RFirebaseStorage mInstance;
        public static RFirebaseStorage Instance
        {
            get
            {
                if (mInstance == null)
                    mInstance = new RFirebaseStorage();
                return mInstance;
            }
        }

#if ACTIVE_FIREBASE_STORAGE

        #region Constants

        protected static readonly string URI_FILE_SCHEME = Uri.UriSchemeFile + "://";

        #endregion

        //=======================================================

        #region Members

        private string mStorageBucket;
        private bool mIsDownloading;
        private bool mIsUploading;
        private bool mIsDeleting;
        private bool mInitialized;

        public bool Initialized { get { return mInitialized; } }

        /// <summary>
        /// Cancellation token source for the current operation.
        /// </summary>
        protected CancellationTokenSource mCancellationTokenSource = new CancellationTokenSource();

        #endregion

        //=========================================================

        #region Public

        public void Initialize(bool pReset = false)
        {
            try
            {
                if (mInitialized && !pReset)
                    return;

                string appBucket = FirebaseApp.DefaultInstance.Options.StorageBucket;
                if (!string.IsNullOrEmpty(appBucket))
                    mStorageBucket = string.Format("gs://{0}", appBucket);

                mInitialized = true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
        }

        public void CancelOperation()
        {
            if ((mIsUploading || mIsDownloading || mIsDeleting) && mCancellationTokenSource != null)
            {
                try
                {
                    Debug.Log("*** Cancelling operation ***");
                    mCancellationTokenSource.Cancel();
                    mCancellationTokenSource = null;
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
        protected IEnumerator IEGetMetadata(SavedFileDefinition pStoDef)
        {
            var storageReference = GetStorageReference(pStoDef);
            Debug.Log(string.Format("Bucket: {0}", storageReference.Bucket));
            Debug.Log(string.Format("Path: {0}", storageReference.Path));
            Debug.Log(string.Format("Name: {0}", storageReference.Name));
            Debug.Log(string.Format("Parent Path: {0}", storageReference.Parent != null ? storageReference.Parent.Path : "(root)"));
            Debug.Log(string.Format("Root Path: {0}", storageReference.Root.Path));
            Debug.Log(string.Format("App: {0}", storageReference.Storage.App.Name));
            var task = storageReference.GetMetadataAsync();
            yield return new WaitForTaskStorage(task);
            if (!(task.IsFaulted || task.IsCanceled))
                Debug.Log(MetadataToString(task.Result, false) + "\n");
        }

        //== DELETE

        public void Delete(Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }

            var storageReference = GetStorageReference(pStoDef);
            var task = storageReference.DeleteAsync();
            Debug.Log(string.Format("Deleting {0}...", storageReference.Path));

            mIsDeleting = true;
            WaitUtil.WaitTask(task, () =>
            {
                mIsDeleting = false;

                if (task.IsFaulted)
                    Debug.LogError(task.Exception.ToString());

                bool success = !task.IsFaulted && !task.IsCanceled;
                if (success)
                    Debug.Log(string.Format("{0} deleted", storageReference.Path));

                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }

        public void DeleteWithCoroutine(Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }

            CoroutineUtil.StartCoroutine(IEDelete(pOnFinished, pStoDef));
        }

        private IEnumerator IEDelete(Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            var storageReference = GetStorageReference(pStoDef);
            var task = storageReference.DeleteAsync();
            Debug.Log(string.Format("Deleting {0}...", storageReference.Path));

            mIsDeleting = false;
            yield return new WaitForTaskStorage(task);
            mIsDeleting = false;

            if (task.IsFaulted)
                Debug.LogError(task.Exception.ToString());

            bool success = !task.IsFaulted && !task.IsCanceled;
            if (success)
                Debug.Log(string.Format("{0} deleted", storageReference.Path));

            if (pOnFinished != null)
                pOnFinished(success);
        }

        //== DOWNLOAD / UPLOAD STREAM

        public void UploadStream(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }

            var storageReference = GetStorageReference(pStoDef);
            var task = storageReference.PutStreamAsync(
                new MemoryStream(Encoding.ASCII.GetBytes(pContent)),
                StringToMetadataChange(pStoDef.metaData),
                new StorageProgress<UploadState>(DisplayUploadState),
                mCancellationTokenSource.Token, null);
            Debug.Log(string.Format("Uploading to {0} using stream...", storageReference.Path));

            mIsUploading = true;
            WaitUtil.WaitTask(task, () =>
            {
                mIsUploading = false;

                if (!task.IsFaulted && !task.IsCanceled)
                    Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
                else
                    Debug.LogError(string.Format("[storage]: Uploading Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

                if (task.IsFaulted)
                    Debug.LogError(task.Exception.ToString());

                if (pOnFinished != null)
                    pOnFinished(!task.IsFaulted && !task.IsCanceled);
            });
        }

        public void UploadStreamWithCoroutine(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }

            CoroutineUtil.StartCoroutine(IEUploadStream(pContent, pOnFinished, pStoDef));
        }

        private IEnumerator IEUploadStream(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            var storageReference = GetStorageReference(pStoDef);
            var task = storageReference.PutStreamAsync(
                new MemoryStream(Encoding.ASCII.GetBytes(pContent)),
                StringToMetadataChange(pStoDef.metaData),
                new StorageProgress<UploadState>(DisplayUploadState),
                mCancellationTokenSource.Token, null);
            Debug.Log(string.Format("Uploading to {0} using stream...", storageReference.Path));

            mIsUploading = true;
            yield return new WaitForTaskStorage(task);
            mIsUploading = false;

            if (!task.IsFaulted && !task.IsCanceled)
                Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
            else
                Debug.LogError(string.Format("[storage]: Uploading Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

            if (task.IsFaulted)
                Debug.LogError(task.Exception.ToString());

            if (pOnFinished != null)
                pOnFinished(!task.IsFaulted && !task.IsCanceled);
        }

        public void DownloadStream(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(null, "");
                return;
            }

            string content = "";
            var storageReference = GetStorageReference(pStoDef);
            Debug.Log(string.Format("Downloading {0} with stream ...", storageReference.Path));

            var task = storageReference.GetStreamAsync((stream) =>
            {
                var buffer = new byte[1024];
                int read;
                // Read data to render in the text view.
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    content += Encoding.Default.GetString(buffer, 0, read);
                }
            }, new StorageProgress<DownloadState>(DisplayDownloadState), mCancellationTokenSource.Token);

            mIsDownloading = true;
            WaitUtil.WaitTask(task, () =>
            {
                mIsDownloading = false;

                if (task.IsFaulted)
                    Debug.LogError(task.Exception.ToString());

                bool success = !task.IsFaulted && !task.IsCanceled;
                if (success)
                    Debug.Log("Finished downloading stream\n");
                else
                    Debug.LogError(string.Format("[storage]: Downloaded Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

                if (pOnFinished != null)
                    pOnFinished(task, content);
            });
        }

        public void DownloadStreamWithCoroutine(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(null, "");
                return;
            }

            CoroutineUtil.StartCoroutine(IEDownloadStream(pOnFinished, pStoDef));
        }

        private IEnumerator IEDownloadStream(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            string content = "";
            var storageReference = GetStorageReference(pStoDef);
            Debug.Log(string.Format("Downloading {0} with stream ...", storageReference.Path));

            var task = storageReference.GetStreamAsync((stream) =>
            {
                var buffer = new byte[1024];
                int read;
                // Read data to render in the text view.
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    content += Encoding.Default.GetString(buffer, 0, read);
                }
            }, new StorageProgress<DownloadState>(DisplayDownloadState), mCancellationTokenSource.Token);

            mIsDownloading = true;
            yield return new WaitForTaskStorage(task);
            mIsDownloading = false;

            if (task.IsFaulted)
                Debug.LogError(task.Exception.ToString());

            bool success = !task.IsFaulted && !task.IsCanceled;
            if (success)
                Debug.Log("Finished downloading stream\n");
            else
                Debug.LogError(string.Format("[storage]: Downloaded Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

            if (pOnFinished != null)
                pOnFinished(task, content);
        }

        //== DOWNLOAD / UPLOAD FILE

        public void UploadFromFile(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                pOnFinished.Raise(false);
                return;
            }

            var localFilenameUriString = PathToPersistentDataPathUriString(pOriginalFilePath);
            var storageReference = GetStorageReference(pStoDef);
            var task = storageReference.PutFileAsync(
                localFilenameUriString, StringToMetadataChange(pStoDef.metaData),
                new StorageProgress<UploadState>(DisplayUploadState),
                mCancellationTokenSource.Token, null);
            Debug.Log(string.Format("Uploading '{0}' to '{1}'...", localFilenameUriString, storageReference.Path));

            mIsUploading = true;
            WaitUtil.WaitTask(task, () =>
            {
                mIsUploading = false;

                bool success = !task.IsFaulted && !task.IsCanceled;

                if (success)
                    Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
                else
                    Debug.LogError(string.Format("[storage]: Uploading Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

                if (task.IsFaulted)
                    Debug.LogError(task.Exception.ToString());

                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }

        public void UploadFromFileWithCoroutine(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }

            CoroutineUtil.StartCoroutine(IEUploadFromFile(pOnFinished, pOriginalFilePath, pStoDef));
        }

        private IEnumerator IEUploadFromFile(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef)
        {
            var localFilenameUriString = PathToPersistentDataPathUriString(pOriginalFilePath);
            var storageReference = GetStorageReference(pStoDef);
            var task = storageReference.PutFileAsync(
                localFilenameUriString, StringToMetadataChange(pStoDef.metaData),
                new StorageProgress<UploadState>(DisplayUploadState),
                mCancellationTokenSource.Token, null);
            Debug.Log(string.Format("Uploading '{0}' to '{1}'...", localFilenameUriString, storageReference.Path));

            mIsUploading = true;
            yield return new WaitForTaskStorage(task);
            mIsUploading = false;

            if (!task.IsFaulted && !task.IsCanceled)
                Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
            else
                Debug.LogError(string.Format("[storage]: Uploading Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

            if (task.IsFaulted)
                Debug.LogError(task.Exception.ToString());

            if (pOnFinished != null)
                pOnFinished(!task.IsFaulted && !task.IsCanceled);
        }

        public void DownloadToFile(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(null, "");
                return;
            }

            var storageReference = GetStorageReference(pStoDef);
            var localFilenameUriString = PathToPersistentDataPathUriString(pOutPutPath);
            var task = storageReference.GetFileAsync(localFilenameUriString, new StorageProgress<DownloadState>(DisplayDownloadState), mCancellationTokenSource.Token);
            var content = "";
            Debug.Log(string.Format("Downloading {0} to {1}...", storageReference.Path, localFilenameUriString));

            mIsDownloading = true;
            WaitUtil.WaitTask(task, () =>
            {
                mIsDownloading = false;

                if (task.IsFaulted)
                    Debug.LogError(task.Exception.ToString());

                bool success = !task.IsFaulted && !task.IsCanceled;
                if (success)
                {
                    var filename = FileUriStringToPath(localFilenameUriString);
                    Debug.Log(string.Format("Finished downloading file {0} ({1})", localFilenameUriString, filename));
                    Debug.Log(string.Format("File Size {0} bytes\n", (new FileInfo(filename)).Length));
                    content = File.ReadAllText(filename);
                }
                else
                    Debug.LogError(string.Format("[storage]: Downloaded Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

                if (pOnFinished != null)
                    pOnFinished(task, content);
            });
        }

        public void DownloadToFileWithCoroutine(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(null, "");
                return;
            }

            CoroutineUtil.StartCoroutine(IEDownloadToFile(pOnFinished, pOutPutPath, pStoDef));
        }

        private IEnumerator IEDownloadToFile(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef)
        {
            var storageReference = GetStorageReference(pStoDef);
            var localFilenameUriString = PathToPersistentDataPathUriString(pOutPutPath);
            var task = storageReference.GetFileAsync(localFilenameUriString, new StorageProgress<DownloadState>(DisplayDownloadState), mCancellationTokenSource.Token);
            var content = "";
            Debug.Log(string.Format("Downloading {0} to {1}...", storageReference.Path, localFilenameUriString));

            mIsDownloading = true;
            yield return new WaitForTaskStorage(task);
            mIsDownloading = false;

            if (task.IsFaulted)
                Debug.LogError(task.Exception.ToString());

            bool success = !task.IsFaulted && !task.IsCanceled;
            if (success)
            {
                var filename = FileUriStringToPath(localFilenameUriString);
                Debug.Log(string.Format("Finished downloading file {0} ({1})", localFilenameUriString, filename));
                Debug.Log(string.Format("File Size {0} bytes\n", (new FileInfo(filename)).Length));
                content = File.ReadAllText(filename);
            }
            else
                Debug.LogError(string.Format("[storage]: Downloaded Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

            if (pOnFinished != null)
                pOnFinished(task, content);
        }

        //== DOWNLOAD / UPLOAD BYTES

        public void UploadBytesWithCoroutine(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }

            CoroutineUtil.StartCoroutine(IEUploadBytes(pContent, pOnFinished, pStoDef));
        }

        /// <summary>
        /// Remember coroutine can break when scene changed
        /// </summary>
        private IEnumerator IEUploadBytes(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            var storageReference = GetStorageReference(pStoDef);
            var task = storageReference.PutBytesAsync(Encoding.UTF8.GetBytes(pContent), StringToMetadataChange(pStoDef.metaData), new StorageProgress<UploadState>(DisplayUploadState), mCancellationTokenSource.Token, null);
            Debug.Log(string.Format("Uploading to {0} ...", storageReference.Path));

            mIsUploading = true;
            yield return new WaitForTaskStorage(task);
            mIsUploading = false;

            if (!task.IsFaulted && !task.IsCanceled)
                Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
            else
                Debug.LogError(string.Format("[storage]: Uploading Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

            if (task.IsFaulted)
                Debug.LogError(task.Exception.ToString());

            if (pOnFinished != null)
                pOnFinished(!task.IsFaulted && !task.IsCanceled);
        }

        public void UploadBytes(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }

            var storageReference = GetStorageReference(pStoDef);
            var task = storageReference.PutBytesAsync(Encoding.UTF8.GetBytes(pContent), StringToMetadataChange(pStoDef.metaData), new StorageProgress<UploadState>(DisplayUploadState), mCancellationTokenSource.Token, null);
            Debug.Log(string.Format("Uploading to {0} ...", storageReference.Path));

            mIsUploading = true;
            WaitUtil.WaitTask(task, () =>
            {
                mIsUploading = false;

                bool success = !task.IsFaulted && !task.IsCanceled;
                if (success)
                    Debug.Log("[storage]: Finished uploading " + MetadataToString(task.Result, false));
                else
                    Debug.LogError(string.Format("[storage]: Uploading Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

                if (task.IsFaulted)
                    Debug.LogError(task.Exception.ToString());

                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }

        public void DownloadBytes(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(null, "");
                return;
            }

            var storageRef = GetStorageReference(pStoDef);
            var task = storageRef.GetBytesAsync(0, new StorageProgress<DownloadState>(DisplayDownloadState), mCancellationTokenSource.Token);
            var content = "";
            Debug.Log(string.Format("[storage]: Downloading {0} ...", storageRef.Path));

            mIsDownloading = true;
            WaitUtil.WaitTask(task, () =>
            {
                mIsDownloading = false;

                bool success = !task.IsFaulted && !task.IsCanceled;
                if (success)
                {
                    content = Encoding.Default.GetString(task.Result);
                    Debug.Log(string.Format("[storage]: Finished downloading bytes\nFile Size {0} bytes\n", content.Length));
                }
                else
                    Debug.LogError(string.Format("[storage]: Downloaded Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

                if (task.IsFaulted)
                    LogException(task.Exception);

                if (pOnFinished != null)
                    pOnFinished(task, content);
            });
        }

        public void DownloadBytesWithCoroutine(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (!mInitialized || string.IsNullOrEmpty(pStoDef.fileName))
            {
                if (pOnFinished != null)
                    pOnFinished(null, "");
                return;
            }

            CoroutineUtil.StartCoroutine(IEDownLoadBytes(pOnFinished, pStoDef));
        }

        private IEnumerator IEDownLoadBytes(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            var storageRef = GetStorageReference(pStoDef);
            var task = storageRef.GetBytesAsync(0, new StorageProgress<DownloadState>(DisplayDownloadState), mCancellationTokenSource.Token);
            var content = "";
            Debug.Log(string.Format("[storage]: Downloading {0} ...", storageRef.Path));

            mIsDownloading = true;
            yield return new WaitForTaskStorage(task);
            mIsDownloading = false;

            if (task.IsFaulted)
                Debug.LogError(task.Exception.ToString());

            bool success = !task.IsFaulted && !task.IsCanceled;
            if (success)
            {
                content = Encoding.Default.GetString(task.Result);
                Debug.Log(string.Format("[storage]: Finished downloading bytes\nFile Size {0} bytes\n", content.Length));
            }
            else
                Debug.LogError(string.Format("[storage]: Downloaded Fail, cancel: {0}, faulted: {1}", task.IsCanceled, task.IsFaulted));

            if (pOnFinished != null)
                pOnFinished(task, content);

        }

        //==========================

        /// <summary>
        /// Get a local filesystem path from a file:// URI.
        /// </summary>
        private string FileUriStringToPath(string fileUriString)
        {
            return Uri.UnescapeDataString((new Uri(fileUriString)).PathAndQuery);
        }

        /// <summary>
        /// Retrieve a storage reference from the user specified path.
        /// </summary>
        private StorageReference GetStorageReference(SavedFileDefinition pStoDef)
        {
            string location = pStoDef.GetStorageLocation(mStorageBucket);
            // If this is an absolute path including a bucket create a storage instance.
            if (location.StartsWith("gs://") || location.StartsWith("http://") || location.StartsWith("https://"))
            {
                var storageUri = new Uri(location);
                var firebaseStorage = FirebaseStorage.GetInstance(string.Format("{0}://{1}", storageUri.Scheme, storageUri.Host));
                return firebaseStorage.GetReferenceFromUrl(location);
            }

            // When using relative paths use the default storage instance which uses the bucket supplied on creation of FirebaseApp.
            return FirebaseStorage.DefaultInstance.GetReference(location);
        }

        /// <summary>
        /// Get the local filename as a URI relative to the persistent data path if the path isn't already a file URI.
        /// </summary>
        private string PathToPersistentDataPathUriString(string filename)
        {
            if (filename.StartsWith(URI_FILE_SCHEME))
                return filename;

            return string.Format("{0}{1}/{2}", URI_FILE_SCHEME, Application.persistentDataPath, filename);
        }

        /// <summary>
        /// Write upload state to the log.
        /// </summary>
        private void DisplayUploadState(UploadState uploadState)
        {
            if (mIsUploading)
                Debug.Log(string.Format("Uploading {0}: {1} out of {2}", uploadState.Reference.Name, uploadState.BytesTransferred, uploadState.TotalByteCount));
        }

        /// <summary>
        /// Convert a string in the form:
        /// key1=value1
        /// ...
        /// keyN=valueN
        /// to a MetadataChange object.
        /// If an empty string is provided this method returns null.
        /// </summary>
        private MetadataChange StringToMetadataChange(string metadataString)
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
                    Debug.Log(string.Format("Ignoring malformed metadata line '{0}' tokens={1}", metadataStringLine, keyValue.Length));
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
        private string MetadataToString(StorageMetadata metadata, bool onlyMutableFields)
        {
            var fieldsAndValues = new Dictionary<string, object> {
                {"ContentType", metadata.ContentType},
                {"CacheControl", metadata.CacheControl},
                {"ContentDisposition", metadata.ContentDisposition},
                {"ContentEncoding", metadata.ContentEncoding},
                {"ContentLanguage", metadata.ContentLanguage}
              };
            if (!onlyMutableFields)
            {
                foreach (var kv in new Dictionary<string, object> {
                            {"Reference", metadata.Reference != null ? metadata.Reference.Path : null},
                            {"Path", metadata.Path},
                            {"Name", metadata.Name},
                            {"Bucket", metadata.Bucket},
                            {"Generation", metadata.Generation},
                            {"MetadataGeneration", metadata.MetadataGeneration},
                            {"CreationTimeMillis", metadata.CreationTimeMillis},
                            {"UpdatedTimeMillis", metadata.UpdatedTimeMillis},
                            {"SizeBytes", metadata.SizeBytes},
                            {"Md5Hash", metadata.Md5Hash}
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
                fieldAndValueStrings.Add(string.Format("{0}={1}", kv.Key, kv.Value));
            }
            return string.Join("\n", fieldAndValueStrings.ToArray());
        }

        /// <summary>
        /// Write download state to the log.
        /// </summary>
        private void DisplayDownloadState(DownloadState downloadState)
        {
            if (mIsDownloading)
                Debug.Log(string.Format("Downloading {0}: {1} out of {2}", downloadState.Reference.Name, downloadState.BytesTransferred, downloadState.TotalByteCount));
        }

        private void LogException(Exception exception)
        {
            var storageException = exception as StorageException;
            if (storageException != null)
            {
                Debug.LogError(string.Format("[storage]: Error Code: {0}", storageException.ErrorCode));
                Debug.LogError(string.Format("[storage]: HTTP Result Code: {0}", storageException.HttpResultCode));
                Debug.LogError(string.Format("[storage]: Recoverable: {0}", storageException.IsRecoverableException));
                Debug.LogError("[storage]: " + storageException.ToString());
            }
            else
            {
                Debug.LogError(exception.ToString());
            }
        }

        #endregion
#else
        public bool Initialized { get { return false; } }
        public void Initialize() { }
        public void CancelOperation() { }
        public void Delete(Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (pOnFinished != null)
                pOnFinished(false);
        }
        public void DeleteWithCoroutine(Action<bool> pOnFinished, SavedFileDefinition pStoDef) { }
        public void UploadStream(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (pOnFinished != null)
                pOnFinished(false);
        }
        public void UploadStreamWithCoroutine(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef) { }
        public void DownloadStream(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (pOnFinished != null)
                pOnFinished(Task.FromResult(0), "");
        }
        public void DownloadStreamWithCoroutine(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef) { }
        public void UploadFromFile(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef)
        {
            if (pOnFinished != null)
                pOnFinished(false);
        }
        public void UploadFromFileWithCoroutine(Action<bool> pOnFinished, string pOriginalFilePath, SavedFileDefinition pStoDef) { }
        public void DownloadToFile(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef)
        {
            if (pOnFinished != null)
                pOnFinished(Task.FromResult(0), "");
        }
        public void DownloadToFileWithCoroutine(Action<Task, string> pOnFinished, string pOutPutPath, SavedFileDefinition pStoDef) { }
        public void UploadBytesWithCoroutine(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef) { }
        public void UploadBytes(string pContent, Action<bool> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (pOnFinished != null)
                pOnFinished(false);
        }
        public void DownloadBytes(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef)
        {
            if (pOnFinished != null)
                pOnFinished(Task.FromResult(0), "");
        }
        public void DownloadBytesWithCoroutine(Action<Task, string> pOnFinished, SavedFileDefinition pStoDef) { }
#endif
    }
}