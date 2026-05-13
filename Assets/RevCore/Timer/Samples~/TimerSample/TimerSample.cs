using UnityEngine;

namespace RevCore.Samples
{
	public class TimerSample : MonoBehaviour
	{
		private ITimerHandle m_handle;
		private bool m_ready;

		private void Start()
		{
			m_handle = GlobalTimers.Instance.WaitForSeconds(2f, OnDelayFinished);
			GlobalTimers.Instance.WaitForCondition(() => m_ready, OnReady);
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
				m_ready = true;

			if (Input.GetKeyDown(KeyCode.C))
				m_handle?.Cancel();
		}

		private void OnDelayFinished()
		{
			Log.Info("Delay finished", this);
		}

		private void OnReady()
		{
			Log.Info("Condition met", this);
		}
	}
}
