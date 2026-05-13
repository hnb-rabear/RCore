using UnityEngine;

namespace RevCore.Samples
{
	public struct ScoreChangedEvent : IEvent
	{
		public int newScore;
	}

	public class EventBusSample : MonoBehaviour
	{
		private void OnEnable()
		{
			Events.Subscribe<ScoreChangedEvent>(OnScoreChanged);
		}

		private void OnDisable()
		{
			Events.Unsubscribe<ScoreChangedEvent>(OnScoreChanged);
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
				Events.Publish(new ScoreChangedEvent { newScore = Random.Range(0, 100) });
		}

		private void OnScoreChanged(ScoreChangedEvent evt)
		{
			Log.Info($"Score changed to {evt.newScore}");
		}
	}
}
