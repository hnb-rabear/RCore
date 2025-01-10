using UnityEngine;

namespace RCore.Data.JObject
{
	public interface IJObjectModel : IJObjectHandler
	{
		public JObjectData Data { get; }
	}

	public abstract class JObjectModel<T> : ScriptableObject, IJObjectModel where T : JObjectData
	{
		public T data;
		public abstract void Init();
		public abstract void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
		public abstract void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
		public abstract void OnUpdate(float deltaTime);
		public abstract void OnPreSave(int utcNowTimestamp);
		public void Save()
		{
			data.Save();
		}
		public JObjectData Data => data;
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