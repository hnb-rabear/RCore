using UnityEngine;

namespace RevCore.Samples
{
    public class PrefsSample : MonoBehaviour
    {
        private PlayerPrefBool m_musicEnabled;
        private PlayerPrefInt m_highScore;

        private void Awake()
        {
            m_musicEnabled = new PlayerPrefBool("sample_music", true);
            m_highScore = new PlayerPrefInt("sample_score");
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                m_musicEnabled.Value = !m_musicEnabled.Value;
                Log.Info($"Music: {m_musicEnabled.Value}", this);
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                m_highScore.Value += 10;
                Log.Info($"Score: {m_highScore.Value}", this);
            }

            if (Input.GetKeyDown(KeyCode.Return))
            {
                PlayerPrefContainer.SaveChanges();
                Log.Info("Prefs saved", this);
            }
        }
    }
}
