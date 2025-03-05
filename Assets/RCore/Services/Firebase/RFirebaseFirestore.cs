using Cysharp.Threading.Tasks;
#if FIREBASE_FIRESTORE
using Firebase.Firestore;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Service
{
#if FIREBASE_FIRESTORE
	[Serializable]
	[FirestoreData]
	public class PlayerIdentityDocument
	{
		[FirestoreProperty] public string Email { get; set; } = "";
		[FirestoreProperty] public string FbId { get; set; } = "";
		[FirestoreProperty] public string GPGId { get; set; } = "";
		[FirestoreProperty] public string DisplayName { get; set; } = "";
		[FirestoreProperty] public int LastActive { get; set; } = 0;
	}

	[Serializable]
	[FirestoreData]
	public class PlayerDataDocument
	{
		[FirestoreProperty] public string Data { get; set; }
	}

	public static class RFirebaseFirestore
	{
		private const string COLLECTION_PLAYER_DATA = "player_data";
		private const string COLLECTION_PLAYER_IDENTITY = "player_identity";

		private static CollectionReference m_PlayerDataCollection;
		private static CollectionReference m_PlayerIdentityCollection;
		private static PlayerIdentityDocument m_PlayerIdentityDocument;
		private static PlayerDataDocument m_PlayerDataDocument;

		private static FirebaseFirestore DB => FirebaseFirestore.DefaultInstance;
		public static string UserId
		{
			get
			{
				string id = Social.localUser.id;
				if (string.IsNullOrEmpty(id))
					id = SystemInfo.deviceUniqueIdentifier;
				return id;
			}
		}

		public static void Init()
		{
			m_PlayerDataCollection = DB.Collection(COLLECTION_PLAYER_DATA);
			m_PlayerIdentityCollection = DB.Collection(COLLECTION_PLAYER_IDENTITY);
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityByEmailAsync(string email)
		{
			var query = m_PlayerIdentityCollection.WhereEqualTo(nameof(PlayerIdentityDocument.Email), email).Limit(1);
			var task = query.GetSnapshotAsync();
			await task;
			foreach (var documentSnapshot in task.Result.Documents)
			{
				var playerDocument = documentSnapshot.ConvertTo<PlayerIdentityDocument>();
				return playerDocument;
			}
			return null;
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityByFbIdAsync(string fbId)
		{
			var query = m_PlayerIdentityCollection.WhereEqualTo(nameof(PlayerIdentityDocument.FbId), fbId).Limit(1);
			var task = query.GetSnapshotAsync();
			await task;
			foreach (var documentSnapshot in task.Result.Documents)
			{
				var playerDocument = documentSnapshot.ConvertTo<PlayerIdentityDocument>();
				return playerDocument;
			}
			return null;
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityByGPGIdAsync(string GPGId)
		{
			var query = m_PlayerIdentityCollection.WhereEqualTo(nameof(PlayerIdentityDocument.GPGId), GPGId).Limit(1);
			var task = query.GetSnapshotAsync();
			await task;
			foreach (var documentSnapshot in task.Result.Documents)
			{
				var playerDocument = documentSnapshot.ConvertTo<PlayerIdentityDocument>();
				return playerDocument;
			}
			return null;
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityDocument(string playerId)
		{
			var task = m_PlayerIdentityCollection.Document(playerId).GetSnapshotAsync();
			if (task.IsCanceled || task.IsFaulted)
				return null;
			await task;
			var document = task.Result.ConvertTo<PlayerIdentityDocument>();
			return document;
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerDataDocument(string playerId)
		{
			var task = m_PlayerDataCollection.Document(playerId).GetSnapshotAsync();
			if (task.IsCanceled || task.IsFaulted)
				return null;
			await task;
			var document = task.Result.ConvertTo<PlayerIdentityDocument>();
			return document;
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityDocument()
		{
			return await LoadPlayerIdentityDocument(UserId);
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerDataDocument()
		{
			return await LoadPlayerDataDocument(UserId);
		}

		public static async UniTask<bool> UploadPlayerIdentityAsync(string playerId, string pEmail, string pFbId, string GPGId, string pDisplayName, int pLastActive)
		{
			var task = m_PlayerIdentityCollection.Document(playerId).UpdateAsync(new Dictionary<string, object>()
			{
				{ nameof(PlayerIdentityDocument.Email), pEmail },
				{ nameof(PlayerIdentityDocument.FbId), pFbId },
				{ nameof(PlayerIdentityDocument.GPGId), GPGId },
				{ nameof(PlayerIdentityDocument.DisplayName), pDisplayName },
				{ nameof(PlayerIdentityDocument.LastActive), pLastActive },
			});
			await task;
			bool success = !task.IsCanceled && !task.IsFaulted;
			return success;
		}

		public static async UniTask<bool> UploadPlayerDataAsync(string playerId, string pData)
		{
			var task = m_PlayerDataCollection.Document(playerId).UpdateAsync(new Dictionary<string, object>()
			{
				{ nameof(PlayerDataDocument.Data), pData },
			});
			await task;
			bool success = !task.IsCanceled && !task.IsFaulted;
			return success;
		}

		public static async UniTask<bool> UploadPlayerIdentityAsync(string pEmail, string pFbId, string GPGId, string pDisplayName, int pLastActive)
		{
			return await UploadPlayerIdentityAsync(UserId, pEmail, pFbId, GPGId, pDisplayName, pLastActive);
		}

		public static async UniTask<bool> UploadPlayerDataAsync(string pData)
		{
			return await UploadPlayerDataAsync(UserId, pData);
		}
	}
#else
	[Serializable]
	public class PlayerIdentityDocument
	{
		public string Email { get; set; } = "";
		public string FbId { get; set; } = "";
		public string GPGId { get; set; } = "";
		public string DisplayName { get; set; } = "";
		public int LastActive { get; set; } = 0;
	}
	public static class RFirebaseFirestore
	{
		public static void Init() { }
		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityByEmailAsync(string email) => null;
		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityByFbIdAsync(string fbId) => null;
		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityByGPGIdAsync(string GPGId) => null;
		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityDocument(string playerId) => null;
		public static async UniTask<PlayerIdentityDocument> LoadPlayerDataDocument(string playerId) => null;
		public static async UniTask<bool> UploadPlayerIdentityAsync(string playerId, string pEmail, string pFbId, string GPGId, string pDisplayName, int pLastActive) => false;
		public static async UniTask<bool> UploadPlayerDataAsync(string playerId, string pData) => false;
	}
#endif
}