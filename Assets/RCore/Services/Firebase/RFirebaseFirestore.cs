using Cysharp.Threading.Tasks;
using Firebase.Auth;
using Firebase.Firestore;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Service
{
	[Serializable]
	[FirestoreData]
	public class PlayerIdentityDocument
	{
		[FirestoreProperty] public string Email { get; set; } = "";
		[FirestoreProperty] public string DeviceId { get; set; } = "";
		[FirestoreProperty] public string FbId { get; set; } = "";
		[FirestoreProperty] public string GPGId { get; set; } = "";
		[FirestoreProperty] public string UserName { get; set; } = "";
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
		private const string COLLECTION_PLAYERS = "players";

		private static CollectionReference m_PlayersCollection;
		private static PlayerIdentityDocument m_PlayerIdentityDocument;
		private static PlayerDataDocument m_PlayerDataDocument;

		public static FirebaseFirestore DB => FirebaseFirestore.DefaultInstance;

		public static async UniTask Init( )
		{
			m_PlayersCollection = FirebaseFirestore.DefaultInstance.Collection(COLLECTION_PLAYERS);
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityByEmailAsync(string email)
		{
			var query = m_PlayersCollection.WhereEqualTo(nameof(PlayerIdentityDocument.Email), email).Limit(1);
			var task = query.GetSnapshotAsync();
			await task;
			foreach (var documentSnapshot in task.Result.Documents)
			{
				var playerDocument = documentSnapshot.ConvertTo<PlayerIdentityDocument>();
				return playerDocument;
			}
			return null;
		}

		public static async UniTask<PlayerIdentityDocument> LoadPlayerIdentityByDeviceIdAsync(string deviceId)
		{
			var query = m_PlayersCollection.WhereEqualTo(nameof(PlayerIdentityDocument.DeviceId), deviceId).Limit(1);
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
			var query = m_PlayersCollection.WhereEqualTo(nameof(PlayerIdentityDocument.FbId), fbId).Limit(1);
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
			var query = m_PlayersCollection.WhereEqualTo(nameof(PlayerIdentityDocument.GPGId), GPGId).Limit(1);
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
			var task = m_PlayersCollection.Document(playerId).GetSnapshotAsync();
			if (task.IsCanceled || task.IsFaulted)
				return null;
			await task;
			var document = task.Result.ConvertTo<PlayerIdentityDocument>();
			return document;
		}
		
		public static async void UploadPlayerData(string pDeviceId, string pEmail, string pFbId, string GPGId, string pUserName, string pData, int pUpdateAt)
		{
		}
	}
}