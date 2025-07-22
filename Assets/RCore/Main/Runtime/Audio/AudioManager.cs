/**
 * Author HNB-RaBear - 2021
 **/

namespace RCore.Audio
{
	/// <summary>
	/// A singleton implementation of the BaseAudioManager that provides a global access point
	/// for all audio-related functionalities. It also integrates with the event system
	/// to play UI sound effects automatically when triggered.
	/// </summary>
	public class AudioManager : BaseAudioManager
	{
		private static AudioManager m_Instance;
		
		/// <summary>
		/// Gets the singleton instance of the AudioManager.
		/// </summary>
		public static AudioManager Instance => m_Instance;

		/// <summary>
		/// Enforces the singleton pattern. If an instance of AudioManager already exists,
		/// this instance is destroyed. Otherwise, it sets itself as the singleton instance.
		/// </summary>
		private void Awake()
		{
			if (m_Instance == null)
				m_Instance = this;
			else if (m_Instance != this)
				Destroy(gameObject);
		}

		/// <summary>
		/// Initializes the AudioManager by calling the base class's Start method and
		/// subscribing to the UISfxTriggeredEvent to handle UI sound effects.
		/// </summary>
		protected override void Start()
		{
			base.Start();

			EventDispatcher.AddListener<UISfxTriggeredEvent>(OnSfxTriggered);
		}

		/// <summary>
		/// Cleans up by unsubscribing from the UISfxTriggeredEvent when the object is destroyed.
		/// </summary>
		private void OnDestroy()
		{
			// It's good practice to remove listeners to prevent memory leaks.
			EventDispatcher.RemoveListener<UISfxTriggeredEvent>(OnSfxTriggered);
		}

		/// <summary>
		/// Event handler for the UISfxTriggeredEvent. Plays the requested sound effect.
		/// </summary>
		/// <param name="e">The event containing the name of the sound effect to play.</param>
		private void OnSfxTriggered(UISfxTriggeredEvent e)
		{
			// Plays the sound effect with no limit on concurrent instances.
			PlaySFX(e.sfx, 0);
		}
	}
}