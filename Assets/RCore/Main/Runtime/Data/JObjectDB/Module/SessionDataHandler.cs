namespace RCore.Data.JObject
{
	public class SessionDataHandler : JObjectHandler<JObjectDBManager>
	{
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			var sessionData = manager.sessionData;
			if (sessionData.firstActive == 0)
				sessionData.firstActive = utcNowTimestamp;
			var lastActive = TimeHelper.UnixTimestampToDateTime(sessionData.lastActive);
			var utcNow = TimeHelper.UnixTimestampToDateTime(utcNowTimestamp);
			if ((utcNow - lastActive).TotalDays > 1)
				sessionData.daysStreak = 0; //Reset days streak
			if (lastActive.Date != utcNow.Date)
			{
				sessionData.days++;
				sessionData.daysStreak++;
				sessionData.sessionsDaily = 0; // Reset daily sessions count
			}
			if (lastActive.Year != utcNow.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(utcNow))
				sessionData.sessionsWeekly = 0; // Reset weekly sessions count
			if (lastActive.Year != utcNow.Year || lastActive.Month != utcNow.Month)
					sessionData.sessionsMonthly = 0; // Reset monthly sessions count
			sessionData.sessionsTotal++;
			sessionData.sessionsDaily++;
			sessionData.sessionsWeekly++;
			sessionData.sessionsMonthly++;
		}
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
			var sessionData = manager.sessionData;
			if (!pause)
				sessionData.lastActive = utcNowTimestamp;
		}
		public override void OnUpdate(float deltaTime)
		{
			var sessionData = manager.sessionData;
			sessionData.activeTime += deltaTime;
		}
		public override void OnPreSave(int utcNowTimestamp)
		{
			var sessionData = manager.sessionData;
			sessionData.lastActive = utcNowTimestamp;
		}
	}
}