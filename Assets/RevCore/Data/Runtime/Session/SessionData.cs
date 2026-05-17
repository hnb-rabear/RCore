using System;
using Newtonsoft.Json;
using UnityEngine;

namespace RevCore
{
    /// <summary>
    /// Persisted state for <see cref="SessionModel"/>. Counters live in a single <see cref="int"/> array
    /// to keep the JSON form compact; named accessors expose individual slots.
    /// </summary>
    [Serializable]
    public class SessionData : JObjectData
    {
        /// <summary>Session counters: [0]=total, [1]=daily, [2]=weekly, [3]=monthly.</summary>
        [Tooltip("Session counts: [0]=total, [1]=daily, [2]=weekly, [3]=monthly")]
        public int[] sessions = { 0, 0, 0, 0 };
        /// <summary>Number of distinct calendar days the player has been active.</summary>
        public int days;
        /// <summary>Current consecutive-day streak.</summary>
        public int daysStreak;
        /// <summary>Cumulative active foreground time in seconds across all sessions.</summary>
        public float activeTime;
        /// <summary>Unix timestamp (UTC seconds) of the last save.</summary>
        public int lastActive;
        /// <summary>Unix timestamp (UTC seconds) of the very first launch.</summary>
        public int firstActive;
        /// <summary>App version recorded at first launch.</summary>
        public string installVersion;
        /// <summary>App version recorded at most recent save.</summary>
        public string updateVersion;

        /// <summary>Lifetime session count.</summary>
        [JsonIgnore] public int SessionsTotal { get => sessions[0]; set => sessions[0] = value; }
        /// <summary>Sessions in the current calendar day.</summary>
        [JsonIgnore] public int SessionsDaily { get => sessions[1]; set => sessions[1] = value; }
        /// <summary>Sessions in the current ISO week.</summary>
        [JsonIgnore] public int SessionsWeekly { get => sessions[2]; set => sessions[2] = value; }
        /// <summary>Sessions in the current calendar month.</summary>
        [JsonIgnore] public int SessionsMonthly { get => sessions[3]; set => sessions[3] = value; }

        /// <inheritdoc />
        /// <remarks>Backfills the <see cref="sessions"/> array when loading old save data with a missing or short array.</remarks>
        public override bool Load()
        {
            bool loaded = base.Load();
            if (sessions == null || sessions.Length < 4)
                sessions = new[] { 0, 0, 0, 0 };
            return loaded;
        }
    }
}
