using UnityEngine.Device;

namespace RCore.Data.JObject
{
	public class SessionDataHandler : JObjectHandler<JObjectDataCollection>
	{
		public float secondsTillNextDay;

		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			var sessionData = dataCollection.sessionData;
			if (sessionData.firstActive == 0)
				sessionData.firstActive = utcNowTimestamp;
			var lastActive = TimeHelper.UnixTimestampToDateTime(sessionData.lastActive).ToLocalTime();
			var now = TimeHelper.UnixTimestampToDateTime(utcNowTimestamp).ToLocalTime();
			if ((now - lastActive).TotalDays > 1)
				sessionData.daysStreak = 0; //Reset days streak
			if (lastActive.Date != now.Date)
			{
				sessionData.days++;
				sessionData.daysStreak++;
				sessionData.SessionsDaily = 0; // Reset daily sessions count
			}
			if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
				sessionData.SessionsWeekly = 0; // Reset weekly sessions count
			if (lastActive.Year != now.Year || lastActive.Month != now.Month)
				sessionData.SessionsMonthly = 0; // Reset monthly sessions count
			sessionData.SessionsTotal++;
			sessionData.SessionsDaily++;
			sessionData.SessionsWeekly++;
			sessionData.SessionsMonthly++;
			secondsTillNextDay = now.Date.AddDays(1).ToUnixTimestampInt() - utcNowTimestamp;
		}
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
			var sessionData = dataCollection.sessionData;
			if (!pause)
			{
				CheckNewDay();
				sessionData.lastActive = utcNowTimestamp;
			}
		}
		public override void OnUpdate(float deltaTime)
		{
			var sessionData = dataCollection.sessionData;
			sessionData.activeTime += deltaTime;

			if (secondsTillNextDay > 0)
			{
				secondsTillNextDay -= deltaTime;
				if (secondsTillNextDay <= 0)
					CheckNewDay();
			}
		}
		public override void OnPreSave(int utcNowTimestamp)
		{
			var sessionData = dataCollection.sessionData;
			sessionData.lastActive = utcNowTimestamp;
#if UNITY_ANDROID
			string curVersion = $"{Application.version}.{RUtil.GetVersionCode()}";
#else
			string curVersion = Application.version;
#endif
			if (string.IsNullOrEmpty(sessionData.installVersion))
				sessionData.installVersion = curVersion;
			sessionData.updateVersion = curVersion;
		}
		private void CheckNewDay()
		{
			var sessionData = dataCollection.sessionData;
			var lastActive = TimeHelper.UnixTimestampToDateTime(sessionData.lastActive).ToLocalTime();
			var now = TimeHelper.GetNow(false);
			if ((now - lastActive).TotalDays > 1)
				sessionData.daysStreak = 0; //Reset days streak
			if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
				sessionData.SessionsWeekly = 1; // Reset weekly sessions count
			if (lastActive.Year != now.Year || lastActive.Month != now.Month)
				sessionData.SessionsMonthly = 1; // Reset monthly sessions count
			if (lastActive.Date != now.Date)
				AddOneDay();
			secondsTillNextDay = (float)(now.Date.AddDays(1) - now).TotalSeconds;
		}
		public void AddOneDay()
		{
			var sessionData = dataCollection.sessionData;
			sessionData.days++;
			sessionData.daysStreak++;
			sessionData.SessionsDaily = 1; // Reset daily sessions count
			DispatchEvent(new NewDayStartedEvent());
		}
	}
}