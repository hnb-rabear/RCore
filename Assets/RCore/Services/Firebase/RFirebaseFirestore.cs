using Cysharp.Threading.Tasks;
#if FIREBASE_FIRESTORE
using Firebase.Firestore;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace RCore.Service
{
#if FIREBASE_FIRESTORE
	[Serializable]
	[FirestoreData]
	public class PlayerIdentityDoc
	{
		[FirestoreProperty] public string Email { get; set; }
		[FirestoreProperty] public string FbId { get; set; }
		[FirestoreProperty] public string GPGId { get; set; }
		[FirestoreProperty] public int Level { get; set; }
		[FirestoreProperty] public string Country { get; set; }
		[FirestoreProperty] public int LastActive { get; set; }
		[FirestoreProperty] public int Version { get; set; }
		[FirestoreProperty] public string Identity { get; set; } // Avatar, Display Name, etc...
	}

	[Serializable]
	[FirestoreData]
	public class PlayerDataDoc
	{
		[FirestoreProperty] public string Data { get; set; }
	}

	public static class RFirebaseFirestore
	{
		private const string COLLECTION_PLAYER_DATA = "player_data";
		private const string COLLECTION_PLAYER_IDENTITY = "player_identity";
		private static CollectionReference m_PlayerDataCollection;
		private static CollectionReference m_PlayerIdentityCollection;
		private static PlayerIdentityDoc m_PlayerIdentityDoc;
		private static PlayerDataDoc m_PlayerDataDoc;
		private static FirebaseFirestore DB => FirebaseFirestore.DefaultInstance;
		public static string UserId
		{
			get
			{
				string id = Social.localUser.id;
				if (string.IsNullOrEmpty(id) || id == "0")
					id = SystemInfo.deviceUniqueIdentifier;
				return id;
			}
		}
		public static void Init()
		{
			m_PlayerDataCollection = DB.Collection(COLLECTION_PLAYER_DATA);
			m_PlayerIdentityCollection = DB.Collection(COLLECTION_PLAYER_IDENTITY);
		}
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityByEmailAsync(string email)
		{
			if (string.IsNullOrEmpty(email))
				return null;
			var query = m_PlayerIdentityCollection.WhereEqualTo(nameof(PlayerIdentityDoc.Email), email).Limit(1);
			var task = query.GetSnapshotAsync();
			await task;
			if (task.IsCanceled || task.IsFaulted)
				return null;
			foreach (var documentSnapshot in task.Result.Documents)
			{
				var playerDocument = documentSnapshot.ConvertTo<PlayerIdentityDoc>();
				return playerDocument;
			}
			return null;
		}
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityByFbIdAsync(string fbId)
		{
			if (string.IsNullOrEmpty(fbId))
				return null;
			var query = m_PlayerIdentityCollection.WhereEqualTo(nameof(PlayerIdentityDoc.FbId), fbId).Limit(1);
			var task = query.GetSnapshotAsync();
			await task;
			if (task.IsCanceled || task.IsFaulted)
				return null;
			foreach (var documentSnapshot in task.Result.Documents)
			{
				var playerDocument = documentSnapshot.ConvertTo<PlayerIdentityDoc>();
				return playerDocument;
			}
			return null;
		}
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityByGPGIdAsync(string GPGId)
		{
			if (string.IsNullOrEmpty(GPGId))
				return null;
			var query = m_PlayerIdentityCollection.WhereEqualTo(nameof(PlayerIdentityDoc.GPGId), GPGId).Limit(1);
			var task = query.GetSnapshotAsync();
			await task;
			if (task.IsCanceled || task.IsFaulted)
				return null;
			foreach (var documentSnapshot in task.Result.Documents)
			{
				var playerDocument = documentSnapshot.ConvertTo<PlayerIdentityDoc>();
				return playerDocument;
			}
			return null;
		}
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityDoc(string playerId)
		{
			if (string.IsNullOrEmpty(playerId))
				return null;
			var task = m_PlayerIdentityCollection.Document(playerId).GetSnapshotAsync();
			await task;
			if (task.IsCanceled || task.IsFaulted)
				return null;
			var document = task.Result.ConvertTo<PlayerIdentityDoc>();
			return document;
		}
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityDoc()
		{
			return await LoadPlayerIdentityDoc(UserId);
		}
		public static async UniTask<PlayerDataDoc> LoadPlayerDataDocAsync(string playerId)
		{
			if (string.IsNullOrEmpty(playerId))
				return null;
			var task = m_PlayerDataCollection.Document(playerId).GetSnapshotAsync();
			await task;
			if (task.IsCanceled || task.IsFaulted)
				return null;
			var document = task.Result.ConvertTo<PlayerDataDoc>();
			return document;
		}
		public static async UniTask<PlayerDataDoc> LoadPlayerDataDocAsync()
		{
			return await LoadPlayerDataDocAsync(UserId);
		}
		public static async UniTask<bool> UploadPlayerIdentityAsync(string playerId, string pEmail, string pFbId, string GPGId, int pLevel, string pCountry, int pLastActive, string pIdentity)
		{
			if (string.IsNullOrEmpty(playerId)) return false;
			var task = m_PlayerIdentityCollection.Document(playerId).UpdateAsync(new Dictionary<string, object>()
			{
				{ nameof(PlayerIdentityDoc.Email), pEmail },
				{ nameof(PlayerIdentityDoc.FbId), pFbId },
				{ nameof(PlayerIdentityDoc.GPGId), GPGId },
				{ nameof(PlayerIdentityDoc.Level), pLevel },
				{ nameof(PlayerIdentityDoc.Country), pCountry },
				{ nameof(PlayerIdentityDoc.LastActive), pLastActive },
				{ nameof(PlayerIdentityDoc.Identity), pIdentity },
			});
			await task;
			bool success = !task.IsCanceled && !task.IsFaulted;
			return success;
		}
		public static async UniTask<bool> UploadPlayerDataAsync(string playerId, string pData)
		{
			var task = m_PlayerDataCollection.Document(playerId).UpdateAsync(new Dictionary<string, object>()
			{
				{ nameof(PlayerDataDoc.Data), pData },
			});
			await task;
			bool success = !task.IsCanceled && !task.IsFaulted;
			return success;
		}
		public static async UniTask<bool> UploadPlayerIdentityAsync(string pEmail, string pFbId, string pGPGId, int pLevel, string pCountry, int pLastActive, string pIdentity)
		{
			return await UploadPlayerIdentityAsync(UserId, pEmail, pFbId, pGPGId, pLevel, pCountry, pLastActive, pIdentity);
		}
		public static async UniTask<bool> UploadPlayerDataAsync(string pData)
		{
			return await UploadPlayerDataAsync(UserId, pData);
		}
		public static async UniTask<List<PlayerIdentityDoc>> FindPlayerIdentityDocs(string country = null, int timestampNowUtc = 0, int version = 0, int limit = 0)
		{
			var docs = new List<PlayerIdentityDoc>();
			if (string.IsNullOrEmpty(country))
				return docs;
			Query query = m_PlayerIdentityCollection;
			if (version > 0)
				query = query.WhereIn(nameof(PlayerIdentityDoc.Version), new object[] { version - 1, version });
			if (!string.IsNullOrEmpty(country))
				query = query.WhereEqualTo(nameof(PlayerIdentityDoc.Country), country);
			if (timestampNowUtc > 0)
				query = query.WhereGreaterThan(nameof(PlayerIdentityDoc.LastActive), timestampNowUtc - 2 * 24 * 3600);
			if (limit > 0)
				query = query.Limit(limit);
			var task = query.GetSnapshotAsync();
			await task;
			if (task.IsCanceled || task.IsFaulted)
				return docs;
			foreach (var documentSnapshot in task.Result.Documents)
			{
				var coopDoc = documentSnapshot.ConvertTo<PlayerIdentityDoc>();
				docs.Add(coopDoc);
			}
			return docs;
		}
		private static object[] GetRandomNumbers(int from, int to, int total)
		{
			if (total > to - from + 1)
				return new object[2] { from, to };

			var result = new object[total];
			var range = to - from + 1;
			var numbers = new int[range];
			for (int i = 0; i < range; i++)
				numbers[i] = from + i;
			for (int i = 0; i < total; i++)
			{
				int randomIndex = Random.Range(i, range);
				result[i] = numbers[randomIndex];
				numbers[randomIndex] = numbers[i];
			}
			return result;
		}
	}
#else
	public class PlayerIdentityDoc
	{
		public string Email { get; set; }
		public string FbId { get; set; }
		public string GPGId { get; set; }
		public int Level { get; set; }
		public int Country { get; set; }
		public int LastActive { get; set; }
		public string Identity { get; set; } // Avatar, Display Name, etc...
	}

	public class PlayerDataDoc
	{
		public string Data { get; set; }
	}

	public static class RFirebaseFirestore
	{
		public static void Init() { }
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityByEmailAsync(string email) => null;
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityByFbIdAsync(string fbId) => null;
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityByGPGIdAsync(string GPGId) => null;
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityDoc(string playerId) => null;
		public static async UniTask<PlayerIdentityDoc> LoadPlayerIdentityDoc() => null;
		public static async UniTask<PlayerDataDoc> LoadPlayerDataDoc(string playerId) => null;
		public static async UniTask<PlayerDataDoc> LoadPlayerDataDoc() => null;
		public static async UniTask<bool> UploadPlayerIdentityAsync(string playerId, string pEmail, string pFbId, string GPGId, int pLevel, string pCountry, int pLastActive, string pIdentity) => false;
		public static async UniTask<bool> UploadPlayerDataAsync(string playerId, string pData) => false;
		public static async UniTask<bool> UploadPlayerIdentityAsync(string pEmail, string pFbId, string pGPGId, int pLevel, string pCountry, int pLastActive, string pIdentity) => false;
		public static async UniTask<bool> UploadPlayerDataAsync(string pData) => false;
		public static async UniTask<List<PlayerIdentityDoc>> FindPlayerIdentityDocs(string country = null, int timestampNowUtc = 0, int version = 0, int limit = 0) => null;
	}
#endif
}