/***
 * Author RadBear - nbhung71711 @gmail.com - 2019
 **/
#pragma warning disable 0649
#if ACTIVE_FIREBASE_DATABASE
using Firebase;
using Firebase.Database;
#endif
using System;

namespace RCore.Service
{
    public class RDatabaseReference
    {
#if ACTIVE_FIREBASE_DATABASE
        public string name;
        public DatabaseReference reference;

        public RDatabaseReference(string pName)
        {
            reference = FirebaseDatabase.DefaultInstance.GetReference(pName);
        }
        public RDatabaseReference(string pName, DatabaseReference pParent)
        {
            name = pName;
            if (pParent == null)
                reference = FirebaseDatabase.DefaultInstance.GetReference(pName);
            else
                reference = pParent.Child(pName);
        }
        public RDatabaseReference(string pName, RDatabaseReference pParent)
        {
            name = pName;
            if (pParent == null)
                reference = FirebaseDatabase.DefaultInstance.GetReference(pName);
            else
                reference = pParent.reference.Child(pName);
        }

        public void GetData(Action<string, bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (pOnFinished != null)
                    pOnFinished(null, false);
                return;
            }
            var task = reference.GetValueAsync();
            WaitUtil.WaitTask(task, () =>
            {
                string loadData = "";
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (success)
                {
                    if (task.Result != null && task.Result.Value != null)
                        loadData = task.Result.Value.ToString();
                }

                if (pOnFinished != null)
                    pOnFinished(loadData, success);
            });
        }
        public void GetDataWithCoroutine(Action<string, bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (pOnFinished != null)
                    pOnFinished(null, false);
                return;
            }

            CoroutineUtil.StartCoroutine(IEGetData(pOnFinished));
        }
        private IEnumerator IEGetData(Action<string, bool> pOnFinished)
        {
            var task = reference.GetValueAsync();

            yield return new WaitForTask(task);

            string loadData = "";
            bool success = !task.IsFaulted && !task.IsCanceled;
            if (success)
            {
                if (task.Result != null && task.Result.Value != null)
                    loadData = task.Result.Value.ToString();
            }

            if (pOnFinished != null)
                pOnFinished(loadData, success);
        }
        public void GetData(string child, Action<string, bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
            {
                if (pOnFinished != null)
                    pOnFinished(null, false);
                return;
            }
            var task = reference.Child(child).GetValueAsync();
            WaitUtil.WaitTask(task, () =>
            {
                string loadData = "";
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (success)
                {
                    if (task.Result != null && task.Result.Value != null)
                        loadData = task.Result.Value.ToString();
                }

                if (pOnFinished != null)
                    pOnFinished(loadData, success);
            });
        }

        public void SetData(string pUploadData, Action<bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.SetValueAsync(pUploadData);
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }
        public void SetDataWithCoroutine(string pUploadData, Action<bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }

            CoroutineUtil.StartCoroutine(IESetData(pUploadData, pOnFinished));
        }
        public void SetJsonData(string pUploadJsonData, Action<bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.SetRawJsonValueAsync(pUploadJsonData);
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }
        private IEnumerator IESetData(string pUploadData, Action<bool> pOnFinished)
        {
            var task = reference.SetValueAsync(pUploadData);
            yield return new WaitForTask(task);
            bool success = !task.IsFaulted && !task.IsCanceled;
            if (pOnFinished != null)
                pOnFinished(success);
        }
        public void SetData(string child, string pUploadData, Action<bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.Child(child).SetValueAsync(pUploadData);
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }
        public void SetJsonData(string child, string pUploadData, Action<bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.Child(child).SetRawJsonValueAsync(pUploadData);
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }
        public void SetJsonDataPriority(string child, string pUploadData, Action<bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.Child(child).SetRawJsonValueAsync(pUploadData, 1);
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }

        public void RemoveData(Action<bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.RemoveValueAsync();
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }
        public void RemoveData(string child, Action<bool> pOnFinished)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.Child(child).RemoveValueAsync();
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }
#endif
    }

    public static class RFirebaseDatabase
    {
        private static bool m_Initialized;

        public static bool Initialized => m_Initialized;
#if ACTIVE_FIREBASE_DATABASE
        public static void Initialize()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;
        }
        public static void CheckOnline(Action pOnConnected)
        {
            var reference = FirebaseDatabase.DefaultInstance.GetReference(".info/connected");
            GetData(reference, (loadData, success) =>
            {
                if (pOnConnected != null)
                    pOnConnected();
            });
        }

        public static void GetData(DatabaseReference reference, Action<string, bool> pOnFinished)
        {
            if (reference == null)
            {
                if (pOnFinished != null)
                    pOnFinished(null, false);
                return;
            }
            var task = reference.GetValueAsync();
            WaitUtil.WaitTask(task, () =>
            {
                string loadData = "";
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (success)
                {
                    if (task.Result != null && task.Result.Value != null)
                        loadData = task.Result.Value.ToString();
                }

                if (pOnFinished != null)
                    pOnFinished(loadData, success);
            });
        }
        public static void GetDataWithCoroutine(DatabaseReference reference, Action<string, bool> pOnFinished)
        {
            if (reference == null)
            {
                if (pOnFinished != null)
                    pOnFinished(null, false);
                return;
            }
            CoroutineUtil.StartCoroutine(IEGetData(reference, pOnFinished));
        }
        private static IEnumerator IEGetData(DatabaseReference reference, Action<string, bool> pOnFinished)
        {
            var task = reference.GetValueAsync();

            yield return new WaitForTask(task);

            string loadData = "";
            bool success = !task.IsFaulted && !task.IsCanceled;
            if (success)
            {
                if (task.Result != null && task.Result.Value != null)
                    loadData = task.Result.Value.ToString();
            }

            if (pOnFinished != null)
                pOnFinished(loadData, success);
        }

        public static void SetData(DatabaseReference reference, string pUploadData, Action<bool> pOnFinished)
        {
            if (reference == null)
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.SetValueAsync(pUploadData);
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }
        public static void SetDataWithCoroutine(DatabaseReference reference, string pUploadData, Action<bool> pOnFinished)
        {
            if (reference == null)
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            CoroutineUtil.StartCoroutine(IESetData(reference, pUploadData, pOnFinished));
        }
        private static IEnumerator IESetData(DatabaseReference reference, string pUploadData, Action<bool> pOnFinished)
        {
            var task = reference.SetValueAsync(pUploadData);
            yield return new WaitForTask(task);
            bool success = !task.IsFaulted && !task.IsCanceled;
            if (pOnFinished != null)
                pOnFinished(success);
        }

        public static void RemoveData(DatabaseReference reference, Action<bool> pOnFinished)
        {
            if (reference == null)
            {
                if (pOnFinished != null)
                    pOnFinished(false);
                return;
            }
            var task = reference.RemoveValueAsync();
            WaitUtil.WaitTask(task, () =>
            {
                bool success = !task.IsFaulted && !task.IsCanceled;
                if (pOnFinished != null)
                    pOnFinished(success);
            });
        }
#else
        public static void Initialize() { }
        public static void CheckOnline(Action pOnConnected) { }
        public static void GetData(object reference, Action<string, bool> pOnFinished)
        {
            pOnFinished?.Invoke("", false);
        }
        public static void GetDataWithCoroutine(object reference, Action<string, bool> pOnFinished)
        {
            pOnFinished?.Invoke("", false);
        }
        public static void SetData(object reference, string pUploadData, Action<bool> pOnFinished)
        {
            pOnFinished?.Invoke(false);
        }
        public static void SetDataWithCoroutine(object reference, string pUploadData, Action<bool> pOnFinished)
        {
            pOnFinished?.Invoke(false);
        }
        public static void RemoveData(object reference, Action<bool> pOnFinished)
        {
            pOnFinished?.Invoke(false);
        }
#endif
    }
}
