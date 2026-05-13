using System;
using Newtonsoft.Json;
using UnityEngine;

namespace RevCore
{
    [Serializable]
    public class SessionData : JObjectData
    {
        [Tooltip("Session counts: [0]=total, [1]=daily, [2]=weekly, [3]=monthly")]
        public int[] sessions = { 0, 0, 0, 0 };
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
            bool loaded = base.Load();
            if (sessions == null || sessions.Length < 4)
                sessions = new[] { 0, 0, 0, 0 };
            return loaded;
        }
    }
}
