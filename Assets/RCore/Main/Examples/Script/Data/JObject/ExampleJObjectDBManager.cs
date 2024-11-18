using RCore.Data.JObject;

namespace RCore.Example.Data.JObject
{
	public class ExampleJObjectDBManager : JObjectDBManager<ExampleJObjectsCollection>
	{
		private void Start()
		{
			Init();
		}
	}
}