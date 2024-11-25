using System;
using UnityEngine;
using UnityEngine.Events;

namespace RCore
{
	public class AnimEventListener : MonoBehaviour
	{
		public Action noParamEvent;
		public Action<string> stringParamEvent;

		public void Callback()
		{
			noParamEvent?.Invoke();
		}

		private void EventCallback(string pEvent)
		{
			stringParamEvent?.Invoke(pEvent);
		}
	}
}