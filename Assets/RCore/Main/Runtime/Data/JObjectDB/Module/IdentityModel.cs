using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RCore.Data.JObject
{
	public class IdentityModel : JObjectModel<IdentityData>
	{
		public override void Init()
		{
		}
		public override void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds)
		{
		}
		public override void OnPostLoad(int utcNowTimestamp, int offlineSeconds)
		{
		}
		public override void OnUpdate(float deltaTime)
		{
		}
		public override void OnPreSave(int utcNowTimestamp)
		{
		}
	}
}