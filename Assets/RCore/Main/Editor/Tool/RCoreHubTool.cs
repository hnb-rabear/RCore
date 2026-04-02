using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RCore.Editor.Tool
{
	/// <summary>
	/// Base class for all tools integrated into the RCore Smart Minimap Hub.
	/// </summary>
	public abstract class RCoreHubTool
	{
		/// <summary>
		/// The display name of the tool on the Minimap.
		/// </summary>
		public abstract string Name { get; }

		/// <summary>
		/// The category sidebar this tool belongs to (e.g. "Navigate", "Search", "UI Tools").
		/// </summary>
		public abstract string Category { get; }

		/// <summary>
		/// A brief description of what the tool does.
		/// </summary>
		public abstract string Description { get; }

		/// <summary>
		/// Determines how the tool is rendered.
		/// If true, the tool UI is small enough to be embedded directly inside the Minimap Grid card.
		/// If false, the card will display an "Open" button to view the tool in fullscreen Focus Mode.
		/// </summary>
		public abstract bool IsQuickAction { get; }

		/// <summary>
		/// Called when the hub initializes all tools. Use this to load saved settings or scan data.
		/// </summary>
		public virtual void Initialize() { }

		/// <summary>
		/// Draws the tool UI inside a constrained Card container.
		/// Only relevant if IsQuickAction == true.
		/// Do NOT wrap in complex layouts, just raw fields and buttons.
		/// </summary>
		public virtual void DrawCard() { }

		/// <summary>
		/// Draws the tool UI in fullscreen Focus Mode.
		/// Only relevant if IsQuickAction == false.
		/// </summary>
		public virtual void DrawFocusMode() { }
	}
}
