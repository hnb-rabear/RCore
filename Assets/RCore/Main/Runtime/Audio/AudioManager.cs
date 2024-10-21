namespace RCore.Audio
{
    public class AudioManager : BaseAudioManager
    {
        private static AudioManager m_Instance;
        public static AudioManager Instance => m_Instance;

        private void Awake()
        {
            if (m_Instance == null)
                m_Instance = this;
            else if (m_Instance != this)
                Destroy(gameObject);
        }

        private void Start()
        {
            EventDispatcher.AddListener<SFXTriggeredEvent>(OnToggleChanged);
        }

        private void OnDestroy()
        {
            EventDispatcher.AddListener<SFXTriggeredEvent>(OnToggleChanged);
        }

        private void OnToggleChanged(SFXTriggeredEvent e)
        {
            PlaySFX(e.sfx, 0);
        }
    }
}