/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern
{
	public interface IModule
	{
		string ModuleID { get; }
		void Initialize();
		void Tick();
		void Shutdown();
	}
}