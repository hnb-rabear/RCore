namespace RevCore
{
    public readonly struct UISfxTriggeredEvent : IEvent
    {
        public string Sfx { get; }
        public UISfxTriggeredEvent(string sfx) => Sfx = sfx;
    }

    public class AudioManager : BaseAudioManager
    {
        private static AudioManager s_instance;
        public static AudioManager Instance => s_instance;

        private void Awake()
        {
            if (s_instance == null)
                s_instance = this;
            else if (s_instance != this)
                Destroy(gameObject);
        }

        protected override void Start()
        {
            base.Start();
            Events.Subscribe<UISfxTriggeredEvent>(OnSfxTriggered);
        }

        private void OnDestroy()
        {
            Events.Unsubscribe<UISfxTriggeredEvent>(OnSfxTriggered);
        }

        private void OnSfxTriggered(UISfxTriggeredEvent e)
        {
            PlaySFX(e.Sfx, 0);
        }
    }
}
