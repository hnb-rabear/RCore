using System.Collections;
using System.Collections.Generic;
using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	public class DailyRewardModel : JObjectModel<DailyRewardData>
	{
		public override void Init() { }
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds) { }
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds) { }
		public override void OnUpdate(float deltaTime) { }
		public override void OnPreSave(int utcNowTimestamp) { }
		public override void OnRemoteConfigFetched() { }
	}
}