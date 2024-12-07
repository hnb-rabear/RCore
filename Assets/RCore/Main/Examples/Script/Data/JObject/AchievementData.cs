using RCore.Data.JObject;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	[Serializable]
	public class TaskData : IComparable<TaskData>
	{
		public int id; // Foreign key, linked to task configuration table
		public int type; // Custom type of task, for example: Kill n enemies, Kill n enemy A, Level-up n times, Reach level n, ...
		public int count;
		public int targetId; // Custom target id
		public int targetNumber;
		public bool claimed;
		public float Fill
		{
			get => count * 1f / targetNumber;
			set => count = Mathf.RoundToInt(targetNumber * value);
		}
		public bool Finished()
		{
			return claimed && Fill >= 1;
		}
		public bool Claimable()
		{
			return !claimed && Fill >= 1;
		}
		public int CompareTo(TaskData other)
		{
			if (!claimed && other.claimed)
				return -1;
			if (claimed && !other.claimed)
				return 1;
			if (!claimed && claimed == other.claimed)
				return Fill.CompareTo(other.Fill);
			return id.CompareTo(other.id);
		}
	}
	
	[Serializable]
	public class AchievementData : JObjectData
	{
		public Action<TaskData> onTaskUpdated;
		public List<TaskData> achievements;
		public TaskData Get(int id)
		{
			return achievements.Find(x => x.id == id);
		}
		public int CountNotices()
		{
			return achievements.FindAll(x => x.Claimable()).Count;
		}
		public void AddProgress(int id, int value)
		{
			var data = achievements.Find(x => x.id == id);
			if (data == null)
				return;
			data.count += value;
			onTaskUpdated?.Invoke(data);
		}
		public void SetProgress(int id, int value)
		{
			var data = achievements.Find(x => x.id == id);
			if (data == null)
				return;
			data.count += value;
			onTaskUpdated?.Invoke(data);
		}
	}
}