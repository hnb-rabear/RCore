namespace RCore.ModulePattern.Example
{
	[Module(nameof(ManualCreationModuleExample), AutoCreate = false, LoadOrder = 100)]
	public class ManualCreationModuleExample : IModule
	{
		public string ModuleID => "ManualModule_v0.1";
		public void Initialize() { }
		public void Tick() { }
		public void Shutdown() { }
		public void DoManualThing() { }
	}
}