using System;
using UnityEngine;

namespace RCore.Data.JObject
{
	/// <summary>
	/// A ScriptableObject-based model that encapsulates all the business logic for managing `SessionData`.
	/// It implements the application lifecycle callbacks (`OnPostLoad`, `OnUpdate`, etc.) to update session metrics
	/// like playtime, daily streaks, and various session counters.
	/// </summary>
	public class SessionModel : JObjectModel<SessionData>
	{
		[Tooltip("A live countdown in seconds until the next calendar day begins.")]
		public float secondsTillNextDay;
		[Tooltip("A live countdown in seconds until the next week begins (typically Monday).")]
		public float secondsTillNextWeek;

		/// <summary>
		/// Initializes the model. This method is called by the `JObjectModelCollection` during the data loading process.
		/// Currently, no specific initialization logic is needed here as data object creation is handled externally.
		/// </summary>
		public override void Init() { }

		/// <summary>
		/// Called after the session data has been loaded. This method processes offline time,
		/// increments session counters, and resets daily/weekly/monthly metrics as needed.
		/// </summary>
		/// <param name="utcNowTimestamp">The current UTC time as a Unix timestamp.</param>
		/// <param name="offlineSeconds">The duration in seconds since the last session.</param>
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
			// If this is the user's first-ever session, record the timestamp.
			if (data.firstActive == 0)
				data.firstActive = utcNowTimestamp;
			
			var lastActive = TimeHelper.UnixTimestampToDateTime(data.lastActive).ToLocalTime();
			var now = TimeHelper.UnixTimestampToDateTime(utcNowTimestamp).ToLocalTime();
			
			// If the user has been away for more than a full day, reset their daily streak.
			if ((now.Date - lastActive.Date).TotalDays > 1)
				data.daysStreak = 0;
			
			// If the calendar day has changed since the last session.
			if (lastActive.Date != now.Date)
			{
				data.days++;
				data.daysStreak++;
				data.SessionsDaily = 0; // Reset the daily session counter before incrementing.
			}
			// If the calendar week has changed.
			if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
				data.SessionsWeekly = 0;
			// If the calendar month has changed.
			if (lastActive.Year != now.Year || lastActive.Month != now.Month)
				data.SessionsMonthly = 0;
			
			// Increment all session counters for this new session.
			data.SessionsTotal++;
			data.SessionsDaily++;
			data.SessionsWeekly++;
			data.SessionsMonthly++;
			
			// Calculate the time remaining until the next day.
			secondsTillNextDay = (float)(now.Date.AddDays(1) - now).TotalSeconds;
		}

		/// <summary>
		/// Called when the application pauses or resumes. On resume, it checks for a new day and updates the last active time.
		/// </summary>
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
			if (!pause) // On Resume
			{
				CheckNewDay();
				data.lastActive = utcNowTimestamp;
			}
		}

		/// <summary>
		/// Called every frame. This method increments the total active time and ticks down the daily/weekly timers.
		/// </summary>
		public override void OnUpdate(float deltaTime)
		{
			data.activeTime += deltaTime;

			if (secondsTillNextDay > 0)
			{
				secondsTillNextDay -= deltaTime;
				if (secondsTillNextDay <= 0)
					CheckNewDay(); // A new day has started during this active session.
			}
			if (secondsTillNextWeek > 0)
			{
				secondsTillNextWeek -= deltaTime;
			}
		}

		/// <summary>
		/// Called just before the data is saved. This ensures the last active timestamp and app version are always current.
		/// </summary>
		public override void OnPreSave(int utcNowTimestamp)
		{
			data.lastActive = utcNowTimestamp;
			
			// Construct a version string that includes the version code on Android for more precise tracking.
			#if UNITY_ANDROID && !UNITY_EDITOR
			string curVersion = $"{Application.version}.{RUtil.GetVersionCode()}";
			#else
			string curVersion = Application.version;
			#endif

			// Record the install version if it hasn't been set yet.
			if (string.IsNullOrEmpty(data.installVersion))
				data.installVersion = curVersion;
			// Always update to the current version.
			data.updateVersion = curVersion;
		}

		/// <summary>
		/// A hook for logic to be executed when remote configuration is fetched.
		/// Currently not implemented.
		/// </summary>
		public override void OnRemoteConfigFetched() { }

		/// <summary>
		/// A helper method that consolidates the logic for checking if a new day, week, or month has started.
		/// It resets the relevant session counters.
		/// </summary>
		private void CheckNewDay()
		{
			var lastActive = TimeHelper.UnixTimestampToDateTime(data.lastActive).ToLocalTime();
			var now = TimeHelper.GetNow(false);
			
			if ((now.Date - lastActive.Date).TotalDays > 1)
				data.daysStreak = 0;
				
			if (lastActive.Year != now.Year || TimeHelper.GetCurrentWeekNumber(lastActive) != TimeHelper.GetCurrentWeekNumber(now))
				data.SessionsWeekly = 1;
				
			if (lastActive.Year != now.Year || lastActive.Month != now.Month)
				data.SessionsMonthly = 1;
				
			if (lastActive.Date != now.Date)
				AddOneDay();
				
			secondsTillNextDay = (float)(now.Date.AddDays(1) - now).TotalSeconds;
			secondsTillNextWeek = (float)TimeHelper.GetSecondsTillDayOfWeek(DayOfWeek.Monday, now);
		}

		/// <summary>
		/// A helper method to perform the actions associated with a new day starting:
		/// increments the total days and streak, resets the daily session count, and dispatches the `NewDayStartedEvent`.
		/// </summary>
		public void AddOneDay()
		{
			data.days++;
			data.daysStreak++;
			data.SessionsDaily = 1; // Reset daily count and start at 1 for the new day's first session.
			DispatchEvent(new NewDayStartedEvent());
		}

		/// <summary>
		/// Calculates the time in seconds that has passed since the user was last active.
		/// </summary>
		/// <returns>The duration of the offline period in seconds. Returns 0 if this is the first session.</returns>
		public virtual int GetOfflineSeconds()
		{
			int offlineSeconds = 0;
			if (data.lastActive > 0)
			{
				int utcNowTimestamp = TimeHelper.GetNowTimestamp(true);
				offlineSeconds = utcNowTimestamp - data.lastActive;
			}
			return Mathf.Max(0, offlineSeconds);
		}
	}
}