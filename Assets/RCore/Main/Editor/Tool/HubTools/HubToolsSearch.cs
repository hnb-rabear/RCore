using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	public class SearchComponentReferenceTool : RCoreHubTool
	{
		public override string Name => "Component Reference Finder";
		public override string Category => "Search";
		public override string Description => "Search for Prefabs or Assets containing a specific Component.";
		public override bool IsQuickAction => false;

		private FindComponentReferenceDrawer m_drawer;

		public override void Initialize()
		{
			m_drawer = new FindComponentReferenceDrawer();
		}

		public override void DrawFocusMode()
		{
			m_drawer.DrawOnGUI();
		}
	}

	public class SearchScriptFinderTool : RCoreHubTool
	{
		public override string Name => "Script Finder";
		public override string Category => "Search";
		public override string Description => "Scan all script references across the project tree.";
		public override bool IsQuickAction => false;

		private ScriptFinder m_drawer;

		public override void Initialize()
		{
			m_drawer = new ScriptFinder();
		}

		public override void DrawFocusMode()
		{
			m_drawer.DrawOnGUI();
		}
	}

	public class SearchParticleSystemTool : RCoreHubTool
	{
		public override string Name => "ParticleSystem Finder";
		public override string Category => "Search";
		public override string Description => "Scan all Particle Systems in the project or scene.";
		public override bool IsQuickAction => false;

		private ParticleSystemFinder m_drawer;

		public override void Initialize()
		{
			m_drawer = new ParticleSystemFinder();
		}

		public override void DrawFocusMode()
		{
			m_drawer.DrawOnGUI();
		}
	}

	public class SearchPersistentEventTool : RCoreHubTool
	{
		public override string Name => "Persistent Event Finder";
		public override string Category => "Search";
		public override string Description => "Display all UnityEvents and Button Click listeners assigned via Inspector.";
		public override bool IsQuickAction => false;

		private PersistentEventFinder m_drawer;

		public override void Initialize()
		{
			m_drawer = new PersistentEventFinder();
		}

		public override void DrawFocusMode()
		{
			m_drawer.DrawOnGUI();
		}
	}
}
