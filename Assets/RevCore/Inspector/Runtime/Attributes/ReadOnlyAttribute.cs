using UnityEngine;

namespace RevCore
{
	/// <summary>Renders the field disabled in the inspector — visible but not editable. Implemented by an editor-side property drawer.</summary>
	public sealed class ReadOnlyAttribute : PropertyAttribute { }
}
