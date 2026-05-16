using System;

namespace RevCore.UI
{
	/// <summary>
	/// Marker interface for components that arrange their children in a layout. Implemented by
	/// <see cref="HorizontalAlignmentUI"/>, <see cref="VerticalAlignmentUI"/>, and
	/// <see cref="TableAlignmentUI"/>. Lets callers re-layout without caring about the specific axis.
	/// </summary>
	public interface IAligned
	{
		/// <summary>Recomputes child positions immediately.</summary>
		void Align();

		/// <summary>Recomputes child positions over a tween. Invokes <paramref name="onFinish"/> when complete.</summary>
		void AlignByTweener(Action onFinish);
	}
}
