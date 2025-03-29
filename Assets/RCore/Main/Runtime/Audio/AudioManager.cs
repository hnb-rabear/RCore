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

		protected override void Start()
		{
			base.Start();

			EventDispatcher.AddListener<UISfxTriggeredEvent>(OnToggleChanged);
		}

		private void OnDestroy()
		{
			EventDispatcher.AddListener<UISfxTriggeredEvent>(OnToggleChanged);
		}

		private void OnToggleChanged(UISfxTriggeredEvent e)
		{
			PlaySFX(e.sfx, 0);
		}
	}
}