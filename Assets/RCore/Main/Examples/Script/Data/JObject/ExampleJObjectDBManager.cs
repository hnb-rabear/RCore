using RCore.Data.JObject;
using RCore.Inspector;
using UnityEngine;

namespace RCore.Example.Data.JObject
{
	public class ExampleJObjectDBManager : JObjectDBManager<ExampleJObjectDataCollection>
	{
		[SerializeField, CreateScriptableObject] protected AchievementModel asb;
			
		private void Start()
		{
			Init();
		}
	}
}