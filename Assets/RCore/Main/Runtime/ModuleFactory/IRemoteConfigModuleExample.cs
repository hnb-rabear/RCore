/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern.Example
{
	public interface IRemoteConfigModuleExample : IModule
	{
		string GetString(string key, string defaultValue);
		int GetInt(string key, int defaultValue);
		bool GetBool(string key, bool defaultValue);
	}
}