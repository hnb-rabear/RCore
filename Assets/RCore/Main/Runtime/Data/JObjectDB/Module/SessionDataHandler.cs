using System;

namespace RCore.Data.JObject
{
	public class NewDayStartedEvent : BaseEvent { }

	public class SessionDataHandler : JObjectHandler<JObjectDBManager>
	{
		public float secondsTillNextDay;
		
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			var sessionData = dbManager.sessionData;
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
				sessionData.sessionsDaily = 0; // Reset daily sessions count
			}
			if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
				sessionData.sessionsWeekly = 0; // Reset weekly sessions count
			if (lastActive.Year != now.Year || lastActive.Month != now.Month)
				sessionData.sessionsMonthly = 0; // Reset monthly sessions count
			sessionData.sessionsTotal++;
			sessionData.sessionsDaily++;
			sessionData.sessionsWeekly++;
			sessionData.sessionsMonthly++;
			secondsTillNextDay = now.Date.AddDays(1).ToUnixTimestampInt() - utcNowTimestamp;
		}
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
			var sessionData = dbManager.sessionData;
			if (!pause)
			{
				CheckNewDay();
				sessionData.lastActive = utcNowTimestamp;
			}
		}
		public override void OnUpdate(float deltaTime)
		{
			var sessionData = dbManager.sessionData;
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
			var sessionData = dbManager.sessionData;
			sessionData.lastActive = utcNowTimestamp;
		}
		private void CheckNewDay()
		{
			var sessionData = dbManager.sessionData;
			var lastActive = TimeHelper.UnixTimestampToDateTime(sessionData.lastActive).ToLocalTime();
			var now = TimeHelper.GetNow(false);
			if ((now - lastActive).TotalDays > 1)
				sessionData.daysStreak = 0; //Reset days streak
			if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
				sessionData.sessionsWeekly = 1; // Reset weekly sessions count
			if (lastActive.Year != now.Year || lastActive.Month != now.Month)
				sessionData.sessionsMonthly = 1; // Reset monthly sessions count
			if (lastActive.Date != now.Date)
				AddOneDay();
			secondsTillNextDay = (float)(now.Date.AddDays(1) - now).TotalSeconds;
		}
		public void AddOneDay()
		{
			var sessionData = dbManager.sessionData;
			sessionData.days++;
			sessionData.daysStreak++;
			sessionData.sessionsDaily = 1; // Reset daily sessions count
			DispatchEvent(new NewDayStartedEvent());
		}
	}
}