using RCore.Data.JObject;
using RCore.Inspector;
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