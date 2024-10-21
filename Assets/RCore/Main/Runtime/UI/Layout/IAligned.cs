/***
 * Author RadBear - nbhung71711 @gmail.com - 2017
 **/

using UnityEngine;
using System;
#if UNITY_EDITOR
#endif

namespace RCore.UI
{
	public interface IAligned
	{
		public void Align();
		public void AlignByTweener(Action onFinish, AnimationCurve pCurve = null);
	}
}