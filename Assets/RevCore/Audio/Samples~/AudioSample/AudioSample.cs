using UnityEngine;

namespace RevCore.Samples
{
    public class AudioSample : MonoBehaviour
    {
        private void Start()
        {
            Events.Subscribe<UISfxTriggeredEvent>(OnUISfx);
        }

        private void OnDestroy()
        {
            Events.Unsubscribe<UISfxTriggeredEvent>(OnUISfx);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
                AudioManager.Instance.PlayMusicById(0, true, 0.5f);

            if (Input.GetKeyDown(KeyCode.S))
                AudioManager.Instance.PlaySFX(0);

            if (Input.GetKeyDown(KeyCode.X))
                AudioManager.Instance.StopMusic(0.5f);

            if (Input.GetKeyDown(KeyCode.U))
                Events.Publish(new UISfxTriggeredEvent("button_click"));
        }

        private void OnUISfx(UISfxTriggeredEvent e)
        {
            Log.Info($"UI SFX triggered: {e.Sfx}");
        }
    }
}
