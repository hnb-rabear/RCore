/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern.Example
{
	// Example: High load order, auto-created by default
	[Module(nameof(AudioModuleExample), LoadOrder = 10)]
	public class AudioModuleExample : IModule
	{
		public string ModuleID => "AudioSystemModule_v1.0_Example";
		public void Initialize() { }
		public void Tick() { }
		public void Shutdown() { }
		public void PlaySound(string soundName)
		{
			// Implementation for playing a sound
		}
	}
}