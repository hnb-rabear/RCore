using UnityEngine;

namespace RCore.Data.JObject
{
	/// <summary>
	/// A logic handler responsible for managing the state of `SessionData`.
	/// It processes application lifecycle events to update session metrics like playtime,
	/// daily streaks, and session counts. It also detects when a new calendar day has started.
	/// </summary>
	public class SessionDataHandler : JObjectHandler<JObjectDataCollection>
	{
		/// <summary>
		/// A countdown timer tracking the remaining seconds until the next calendar day begins.
		/// </summary>
		public float secondsTillNextDay;

		/// <summary>
		/// Called after the session data is loaded. This method is crucial for calculating changes
		/// since the last session, such as updating daily, weekly, and monthly session counts,
		/// and resetting the daily streak if necessary.
		/// </summary>
		/// <param name="utcNowTimestamp">The current UTC time as a Unix timestamp.</param>
		/// <param name="offlineSeconds">The duration in seconds since the last session.</param>
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			var sessionData = dataCollection.sessionData;
			// Record the first-ever session time.
			if (sessionData.firstActive == 0)
				sessionData.firstActive = utcNowTimestamp;

			var lastActive = TimeHelper.UnixTimestampToDateTime(sessionData.lastActive).ToLocalTime();
			var now = TimeHelper.UnixTimestampToDateTime(utcNowTimestamp).ToLocalTime();

			// If more than a full day has passed, the streak is broken.
			if ((now - lastActive).TotalDays >= 2)
				sessionData.daysStreak = 0;

			// Check if the calendar day has changed since the last session.
			if (lastActive.Date != now.Date)
			{
				sessionData.days++;
				sessionData.daysStreak++;
				sessionData.SessionsDaily = 0; // Reset daily counter.
				DispatchEvent(new NewDayStartedEvent());
			}

			// Check if the week has changed.
			if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
				sessionData.SessionsWeekly = 0; // Reset weekly counter.

			// Check if the month has changed.
			if (lastActive.Year != now.Year || lastActive.Month != now.Month)
				sessionData.SessionsMonthly = 0; // Reset monthly counter.
			
			// Increment all session counters for this new session.
			sessionData.SessionsTotal++;
			sessionData.SessionsDaily++;
			sessionData.SessionsWeekly++;
			sessionData.SessionsMonthly++;
			
			// Calculate the time remaining until the next day.
			secondsTillNextDay = (float)(now.Date.AddDays(1) - now).TotalSeconds;
		}

		/// <summary>
		/// Called when the application is paused or resumed.
		/// When resuming, it updates the `lastActive` timestamp.
		/// </summary>
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
			if (!pause)
			{
				// On resume, check if a new day has started during the pause.
				CheckNewDay();
				dataCollection.sessionData.lastActive = utcNowTimestamp;
			}
		}

		/// <summary>
		/// Called every frame. It increments the total active time and checks if a new day has started.
		/// </summary>
		public override void OnUpdate(float deltaTime)
		{
			dataCollection.sessionData.activeTime += deltaTime;

			// Countdown to the next day.
			if (secondsTillNextDay > 0)
			{
				secondsTillNextDay -= deltaTime;
				if (secondsTillNextDay <= 0)
				{
					// A new day has started during this session.
					CheckNewDay();
				}
			}
		}

		/// <summary>
		/// Called just before saving. It updates the `lastActive` timestamp and records the current app version.
		/// </summary>
		public override void OnPreSave(int utcNowTimestamp)
		{
			var sessionData = dataCollection.sessionData;
			sessionData.lastActive = utcNowTimestamp;
			
			// Construct a version string that includes the version code on Android.
#if UNITY_ANDROID && !UNITY_EDITOR
			string curVersion = $"{Application.version}.{RUtil.GetVersionCode()}";
#else
			string curVersion = Application.version;
#endif

			// Record install and update versions.
			if (string.IsNullOrEmpty(sessionData.installVersion))
				sessionData.installVersion = curVersion;
			sessionData.updateVersion = curVersion;
		}

		/// <summary>
		/// A helper method to check if a new day, week, or month has started since the last check.
		/// This can be called on resume or during an active session.
		/// </summary>
		private void CheckNewDay()
		{
			var sessionData = dataCollection.sessionData;
			var lastActive = TimeHelper.UnixTimestampToDateTime(sessionData.lastActive).ToLocalTime();
			var now = TimeHelper.GetNow(false);

			// Check for a new calendar day.
			if (lastActive.Date != now.Date)
			{
				// Reset streak if a day was missed.
				if ((now.Date - lastActive.Date).TotalDays > 1)
					sessionData.daysStreak = 0; 
				
				AddOneDay();
			}

			// Check for a new week.
			if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
				sessionData.SessionsWeekly = 1;
				
			// Check for a new month.
			if (lastActive.Year != now.Year || lastActive.Month != now.Month)
				sessionData.SessionsMonthly = 1; 

			// Recalculate time until the next day.
			secondsTillNextDay = (float)(now.Date.AddDays(1) - now).TotalSeconds;
		}

		/// <summary>
		/// A helper method to increment the day counters and dispatch the `NewDayStartedEvent`.
		/// </summary>
		public void AddOneDay()
		{
			var sessionData = dataCollection.sessionData;
			sessionData.days++;
			sessionData.daysStreak++;
			sessionData.SessionsDaily = 1; // Reset daily session count and start with 1.
			DispatchEvent(new NewDayStartedEvent());
		}
	}
}