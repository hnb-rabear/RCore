namespace RCore.Data.JObject
{
	[System.Serializable]
	public abstract class JObjectHandler<T> : IJObjectHandler where T : JObjectDataCollection
	{
		public T dataCollection;
		public abstract void OnPause(bool pause, int utcNowTimestamp, int offlineSeconds);
		public abstract void OnPostLoad(int utcNowTimestamp, int offlineSeconds);
		public abstract void OnUpdate(float deltaTime);
		public abstract void OnPreSave(int utcNowTimestamp);
		public void OnRemoteConfigFetched() { }
		public void Save()
		{
			throw new System.NotImplementedException("This method should not be called. Save functionality is not implemented.");
		}
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