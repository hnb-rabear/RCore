using UnityEngine;

namespace RevCore
{
	/// <summary>
	/// On an <see cref="int"/> field, renders a single-layer picker (Unity's built-in <c>LayerField</c>)
	/// instead of an integer input. Implemented by an editor-side property drawer.
	/// </summary>
	public sealed class SingleLayerAttribute : PropertyAttribute { }
}
