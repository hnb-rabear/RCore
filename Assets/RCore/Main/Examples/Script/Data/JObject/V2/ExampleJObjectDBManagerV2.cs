using System;
using System.Collections;
using System.Collections.Generic;
using RCore.Data.JObject;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	public class ExampleJObjectDBManagerV2 : JObjectDBManagerV2<ExampleJObjectModelCollection>
	{
		private void Start()
		{
			Init();
		}
	}
}