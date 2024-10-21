using Newtonsoft.Json;
using System;

namespace RCore.Data.JObject
{
	[Serializable]
	public class UserSessionCollection : JObjectCollection
	{
		public int[] sessions = { 0, 0, 0, 0 }; // 0: sessionsTotal, 1: sessionsDaily, 2: sessionWeekly, 3: sessionMonthly
		public int days;
		public int daysStreak;
		public float activeTime;
		public int lastActive;
		public int firstActive;
		[JsonIgnore] public int sessionsTotal
		{
			get => sessions[0];
			set => sessions[0] = value;
		}
		[JsonIgnore] public int sessionsDaily
		{
			get => sessions[1];
			set => sessions[1] = value;
		}
		[JsonIgnore] public int sessionsWeekly
		{
			get => sessions[2];
			set => sessions[2] = value;
		}
		[JsonIgnore] public int sessionsMonthly
		{
			get => sessions[3];
			set => sessions[3] = value;
		}
		public override bool Load()
		{
			var load = base.Load();
			if (sessions == null || sessions.Length == 0)
				sessions = new[] { 0, 0, 0, 0 };
			return load;
		}
	}
}