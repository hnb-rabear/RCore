using UnityEngine;

namespace RevCore.Samples
{
	public class InspectorSample : MonoBehaviour
	{
		[ReadOnly] public int health = 100;
		[Separator("Settings")]
		[Comment("Movement speed in units per second")]
		public float speed = 5f;
		[Highlight] public string playerName;
		[ShowIf("showDebug")] public bool debugMode;
		public bool showDebug;
		[SingleLayer] public int groundLayer;
		[TagSelector] public string enemyTag;
		[SpriteBox] public Sprite icon;

		[InspectorButton("Reset Health")]
		public void ResetHealth()
		{
			health = 100;
		}
	}
}
