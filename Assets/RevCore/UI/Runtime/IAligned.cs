using System;

namespace RevCore.UI
{
	public interface IAligned
	{
		void Align();
		void AlignByTweener(Action onFinish);
	}
}
