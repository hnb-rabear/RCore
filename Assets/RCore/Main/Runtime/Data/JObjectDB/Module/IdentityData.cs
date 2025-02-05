using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.Data.JObject
{
	[Serializable]
	public class IdentityData : JObjectData
	{
		public string deviceId;
		public string gpgsId;
		public int level;
	}
}