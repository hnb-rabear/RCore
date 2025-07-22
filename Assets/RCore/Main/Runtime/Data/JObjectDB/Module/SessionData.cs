using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RCore.Data.JObject
{
	/// <summary>
	/// An event that is dispatched when the session handler detects that a new calendar day has begun for the user.
	/// This is useful for triggering daily rewards, resetting daily quests, etc.
	/// </summary>
	public class NewDayStartedEvent : BaseEvent { }

	/// <summary>
	/// A serializable data class responsible for storing all session-related information for a user.
	/// This includes metrics like session counts, daily streaks, playtime, and version tracking.
	/// </summary>
	[Serializable]
	public class SessionData : JObjectData
	{
		[Tooltip("Tracks session counts over different timeframes.")]
		public int[] sessions = { 0, 0, 0, 0 }; // 0: sessionsTotal, 1: sessionsDaily, 2: sessionsWeekly, 3: sessionsMonthly
		
		[Tooltip("The total number of unique days the user has opened the application.")]
		public int days;
		
		[Tooltip("The current consecutive daily login streak.")]
		public int daysStreak;
		
		[Tooltip("The total time in seconds the user has been active in the application.")]
		public float activeTime;
		
		[Tooltip("The Unix timestamp of the last recorded user activity.")]
		public int lastActive;
		
		[Tooltip("The Unix timestamp of the user's very first session.")]
		public int firstActive;
		
		[Tooltip("The application version string when the user first installed the game.")]
		public string installVersion;
		
		[Tooltip("The application version string recorded after the last update.")]
		public string updateVersion;
		
		/// <summary>
		/// A convenient property to get or set the total number of sessions.
		/// This property is ignored during JSON serialization to avoid data duplication.
		/// </summary>
		[JsonIgnore] public int SessionsTotal { get => sessions[0]; set => sessions[0] = value; }
		
		/// <summary>
		/// A convenient property to get or set the number of sessions for the current day.
		/// This property is ignored during JSON serialization.
		/// </summary>
		[JsonIgnore] public int SessionsDaily { get => sessions[1]; set => sessions[1] = value; }
		
		/// <summary>
		/// A convenient property to get or set the number of sessions for the current week.
		/// This property is ignored during JSON serialization.
		/// </summary>
		[JsonIgnore] public int SessionsWeekly { get => sessions[2]; set => sessions[2] = value; }
		
		/// <summary>
		/// A convenient property to get or set the number of sessions for the current month.
		/// This property is ignored during JSON serialization.
		/// </summary>
		[JsonIgnore] public int SessionsMonthly { get => sessions[3]; set => sessions[3] = value; }

		/// <summary>
		/// Overrides the base Load method to provide backward compatibility.
		/// It ensures that the `sessions` array is properly initialized even if loading from older data
		/// that does not contain this field.
		/// </summary>
		/// <returns>True if loading was successful, otherwise false.</returns>
		public override bool Load()
		{
			bool wasLoaded = base.Load();
			// Ensure the sessions array is initialized for data loaded from older versions.
			if (sessions == null || sessions.Length < 4)
				sessions = new[] { 0, 0, 0, 0 };
			return wasLoaded;
		}
	}
}