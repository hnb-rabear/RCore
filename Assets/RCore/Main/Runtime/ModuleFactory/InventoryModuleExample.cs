/***
 * Author Gemini - 2025
 **/

namespace RCore.ModulePattern.Example
{
	// Example: Default load order, explicitly set AutoCreate (though true is default)
	[Module(nameof(InventoryModuleExample), LoadOrder = 0, AutoCreate = true)]
	public class InventoryModuleExample : IModule
	{
		public string ModuleID => "InventorySystemModule_Alpha_Example";
		public void Initialize() { }
		public void Tick() { }
		public void Shutdown() { }
		public bool HasItem(string itemName) => false; // Placeholder return value
	}
}