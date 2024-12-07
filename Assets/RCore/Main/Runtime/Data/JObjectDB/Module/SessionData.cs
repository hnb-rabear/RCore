using Newtonsoft.Json;
using System;
using UnityEngine;

namespace RCore.Data.JObject
{
	public class NewDayStartedEvent : BaseEvent { }

	[Serializable]
	public class SessionData : JObjectData
	{
		public int[] sessions = { 0, 0, 0, 0 }; // 0: sessionsTotal, 1: sessionsDaily, 2: sessionWeekly, 3: sessionMonthly
		public int days;
		public int daysStreak;
		public float activeTime;
		public int lastActive;
		public int firstActive;
		public string installVersion;
		public string updateVersion;
		[JsonIgnore] public int SessionsTotal { get => sessions[0]; set => sessions[0] = value; }
		[JsonIgnore] public int SessionsDaily { get => sessions[1]; set => sessions[1] = value; }
		[JsonIgnore] public int SessionsWeekly { get => sessions[2]; set => sessions[2] = value; }
		[JsonIgnore] public int SessionsMonthly { get => sessions[3]; set => sessions[3] = value; }
		public override bool Load()
		{
			var load = base.Load();
			if (sessions == null || sessions.Length == 0)
				sessions = new[] { 0, 0, 0, 0 };
			return load;
		}
	}
}