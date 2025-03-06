/***
 * Author HNB-RaBear - 2019
 **/

#if FIREBASE_DATABASE
using Firebase.Database;
#endif
using System.Collections;
using System;

namespace RCore.Service
{
	public class RDatabaseReference
	{
#if FIREBASE_DATABASE
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
				pOnFinished?.Invoke(null, false);
				return;
			}
			var task = reference.GetValueAsync();
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				string loadData = "";
				bool success = !task.IsFaulted && !task.IsCanceled;
				if (success)
				{
					if (task.Result != null && task.Result.Value != null)
						loadData = task.Result.Value.ToString();
				}

				pOnFinished?.Invoke(loadData, success);
			});
		}
		public void GetDataWithCoroutine(Action<string, bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name))
			{
				pOnFinished?.Invoke(null, false);
				return;
			}

			TimerEventsGlobal.Instance.StartCoroutine(IEGetData(pOnFinished));
		}
		private IEnumerator IEGetData(Action<string, bool> pOnFinished)
		{
			var task = reference.GetValueAsync();

			yield return task;

			string loadData = "";
			bool success = !task.IsFaulted && !task.IsCanceled;
			if (success)
			{
				if (task.Result != null && task.Result.Value != null)
					loadData = task.Result.Value.ToString();
			}

			pOnFinished?.Invoke(loadData, success);
		}
		public void GetData(string child, Action<string, bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
			{
				pOnFinished?.Invoke(null, false);
				return;
			}
			var task = reference.Child(child).GetValueAsync();
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				string loadData = "";
				bool success = !task.IsFaulted && !task.IsCanceled;
				if (success)
				{
					if (task.Result != null && task.Result.Value != null)
						loadData = task.Result.Value.ToString();
				}

				pOnFinished?.Invoke(loadData, success);
			});
		}

		public void SetData(string pUploadData, Action<bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name))
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.SetValueAsync(pUploadData);
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}
		public void SetDataWithCoroutine(string pUploadData, Action<bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name))
			{
				pOnFinished?.Invoke(false);
				return;
			}
			TimerEventsGlobal.Instance.StartCoroutine(IESetData(pUploadData, pOnFinished));
		}
		public void SetJsonData(string pUploadJsonData, Action<bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name))
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.SetRawJsonValueAsync(pUploadJsonData);
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}
		private IEnumerator IESetData(string pUploadData, Action<bool> pOnFinished)
		{
			var task = reference.SetValueAsync(pUploadData);
			yield return task;
			bool success = !task.IsFaulted && !task.IsCanceled;
			pOnFinished?.Invoke(success);
		}
		public void SetData(string child, string pUploadData, Action<bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.Child(child).SetValueAsync(pUploadData);
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}
		public void SetJsonData(string child, string pUploadData, Action<bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.Child(child).SetRawJsonValueAsync(pUploadData);
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}
		public void SetJsonDataPriority(string child, string pUploadData, Action<bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.Child(child).SetRawJsonValueAsync(pUploadData, 1);
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}

		public void RemoveData(Action<bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name))
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.RemoveValueAsync();
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}
		public void RemoveData(string child, Action<bool> pOnFinished)
		{
			if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(child))
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.Child(child).RemoveValueAsync();
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}
#endif
	}

	public static class RFirebaseDatabase
	{
		private static bool m_Initialized;

		public static bool Initialized => m_Initialized;
#if FIREBASE_DATABASE
		public static void Init()
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
				pOnConnected?.Invoke();
			});
		}

		public static void GetData(DatabaseReference reference, Action<string, bool> pOnFinished)
		{
			if (reference == null)
			{
				pOnFinished?.Invoke(null, false);
				return;
			}
			var task = reference.GetValueAsync();
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				string loadData = "";
				bool success = !task.IsFaulted && !task.IsCanceled;
				if (success)
				{
					if (task.Result != null && task.Result.Value != null)
						loadData = task.Result.Value.ToString();
				}

				pOnFinished?.Invoke(loadData, success);
			});
		}
		public static void GetDataWithCoroutine(DatabaseReference reference, Action<string, bool> pOnFinished)
		{
			if (reference == null)
			{
				pOnFinished?.Invoke(null, false);
				return;
			}
			TimerEventsGlobal.Instance.StartCoroutine(IEGetData(reference, pOnFinished));
		}
		private static IEnumerator IEGetData(DatabaseReference reference, Action<string, bool> pOnFinished)
		{
			var task = reference.GetValueAsync();

			yield return task;

			string loadData = "";
			bool success = !task.IsFaulted && !task.IsCanceled;
			if (success)
			{
				if (task.Result != null && task.Result.Value != null)
					loadData = task.Result.Value.ToString();
			}

			pOnFinished?.Invoke(loadData, success);
		}

		public static void SetData(DatabaseReference reference, string pUploadData, Action<bool> pOnFinished)
		{
			if (reference == null)
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.SetValueAsync(pUploadData);
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}
		public static void SetDataWithCoroutine(DatabaseReference reference, string pUploadData, Action<bool> pOnFinished)
		{
			if (reference == null)
			{
				pOnFinished?.Invoke(false);
				return;
			}
			TimerEventsGlobal.Instance.StartCoroutine(IESetData(reference, pUploadData, pOnFinished));
		}
		private static IEnumerator IESetData(DatabaseReference reference, string pUploadData, Action<bool> pOnFinished)
		{
			var task = reference.SetValueAsync(pUploadData);
			yield return task;
			bool success = !task.IsFaulted && !task.IsCanceled;
			pOnFinished?.Invoke(success);
		}

		public static void RemoveData(DatabaseReference reference, Action<bool> pOnFinished)
		{
			if (reference == null)
			{
				pOnFinished?.Invoke(false);
				return;
			}
			var task = reference.RemoveValueAsync();
			TimerEventsGlobal.Instance.WaitTask(task, () =>
			{
				bool success = !task.IsFaulted && !task.IsCanceled;
				pOnFinished?.Invoke(success);
			});
		}
#else
        public static void Init() { }
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