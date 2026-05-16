namespace RevCore
{
    /// <summary>
    /// Singleton convenience wrapper around <see cref="BaseAudioManager"/>. Subscribes to
    /// <see cref="UISfxTriggeredEvent"/> on the global bus so UI elements can fire sound effects
    /// by name without referencing the audio manager directly.
    /// </summary>
    public class AudioManager : BaseAudioManager
    {
        private static AudioManager s_instance;

        /// <summary>The active singleton instance. <c>null</c> outside of play mode (or before <c>Awake</c>).</summary>
        public static AudioManager Instance => s_instance;

        private void Awake()
        {
            if (s_instance == null)
                s_instance = this;
            else if (s_instance != this)
                Destroy(gameObject);
        }

        /// <summary>Calls base, then subscribes to <see cref="UISfxTriggeredEvent"/>.</summary>
        protected override void Start()
        {
            base.Start();
            Events.Subscribe<UISfxTriggeredEvent>(OnSfxTriggered);
        }

        private void OnDestroy()
        {
            Events.Unsubscribe<UISfxTriggeredEvent>(OnSfxTriggered);

            if (s_instance == this)
                s_instance = null;
        }

        private void OnSfxTriggered(UISfxTriggeredEvent e)
        {
            PlaySFX(e.Sfx, 0);
        }
    }
}
