using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace RCore.Data.JObject
{
	public interface IJObjectHandler
	{
		public void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
		public void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
		public void OnUpdate(float deltaTime);
		public void OnPreSave(int utcNowTimestamp);
	}
	
	[System.Serializable]
	public abstract class JObjectHandler<T> : IJObjectHandler where T : JObjectDBManager
	{
		public T dbManager;
		public abstract void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
		public abstract void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
		public abstract void OnUpdate(float deltaTime);
		public abstract void OnPreSave(int utcNowTimestamp);
		/// <summary>
		/// Triggers an event when data changes.
		/// </summary>
		/// <param name="e">Instance of the event</param>
		/// <param name="pDeBounce">Delay in seconds to prevent excessive event dispatches.</param>
		/// <typeparam name="T">Type of the event.</typeparam>
		protected void DispatchEvent<T>(T e, float pDeBounce = 0) where T : BaseEvent
		{
			if (pDeBounce > 0)
			{
				TimerEventsGlobal.Instance.WaitForSeconds(new CountdownEvent()
				{
					id = RUtil.GetStableHashCode(typeof(T).Name),
					waitTime = pDeBounce,
					onTimeOut = f => EventDispatcher.Raise(e),
					unscaledTime = true,
				});
			}
			else
				EventDispatcher.Raise(e);
		}
	}
}