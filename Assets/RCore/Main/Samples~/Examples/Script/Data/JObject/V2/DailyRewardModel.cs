using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	/// <summary>
	/// Demonstrates [Inject] to access session metrics without a direct
	/// ScriptableObject reference. The DI system resolves ISessionModel
	/// automatically after all models are created.
	/// </summary>
	public class DailyRewardModel : JObjectModel<DailyRewardData>
	{
		[Inject] private ISessionModel m_session;

		public override void Init() { }

		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			// Use injected session to check daily streak
			if (m_session != null)
				Debug.Log($"[DailyReward] Session days: {m_session.Days}, streak: {m_session.DaysStreak}");
		}

		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds) { }
		public override void OnUpdate(float deltaTime) { }
		public override void OnPreSave(int utcNowTimestamp) { }
		public override void OnRemoteConfigFetched() { }
	}
}