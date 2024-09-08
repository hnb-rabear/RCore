#if ACTIVE_FIREBASE_FIRESTORE
using Firebase.Firestore;
#endif
namespace RCore.Service
{
    public class RFirebaseFireStore
    {
#if ACTIVE_FIREBASE_FIRESTORE
        protected static FirebaseFirestore db => FirebaseFirestore.DefaultInstance;

        protected static IEnumerator IEGetKnownValue(string collection, string document, Action<IDictionary<string, object>> pCallback)
        {
            var doc1 = db.Collection(collection).Document(document);
            var task = doc1.GetSnapshotAsync();
            yield return new WaitForTask(task);
            if (!(task.IsFaulted || task.IsCanceled))
            {
                DocumentSnapshot snap = task.Result;
                IDictionary<string, object> dict = snap.ToDictionary();
                pCallback.Invoke(dict);
            }
        }

        private static CollectionReference GetCollectionReference(string collectionPath)
        {
            return db.Collection(collectionPath);
        }

        private static DocumentReference GetDocumentReference(string documentId, string collectionPath)
        {
            if (documentId == "")
                return GetCollectionReference(collectionPath).Document();
            return GetCollectionReference(collectionPath).Document(documentId);
        }

        private static IEnumerator IEWriteDoc(DocumentReference doc, IDictionary<string, object> data, Action<bool> pCallback)
        {
            Task setTask = doc.SetAsync(data);
            yield return new WaitForTask(setTask);
            if (setTask.IsFaulted || setTask.IsCanceled)
                pCallback?.Invoke(false);
            else
                pCallback?.Invoke(true);
        }

        private static IEnumerator IEUpdateDoc(DocumentReference doc, IDictionary<string, object> data, Action<bool> pCallback)
        {
            Task updateTask = doc.UpdateAsync(data);
            yield return new WaitForTask(updateTask);
            if (updateTask.IsFaulted || updateTask.IsCanceled)
                pCallback?.Invoke(false);
            else
                pCallback?.Invoke(true);
        }

        private static IEnumerator IEReadDoc(DocumentReference doc, Action<IDictionary<string, object>> pCallback)
        {
            Task<DocumentSnapshot> getTask = doc.GetSnapshotAsync();
            yield return new WaitForTask(getTask);
            if (getTask.IsFaulted || getTask.IsCanceled)
            {
                pCallback?.Invoke(null);
            }
            else
            {
                DocumentSnapshot snap = getTask.Result;
                IDictionary<string, object> resultData = snap.ToDictionary();
                pCallback?.Invoke(resultData);
            }
        }
#endif
    }
}