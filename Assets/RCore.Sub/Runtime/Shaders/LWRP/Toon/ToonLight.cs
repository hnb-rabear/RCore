using UnityEngine;

// Add script to lighting source
// Updates toon shadows for toon shader

public class ToonLight : MonoBehaviour
{
    private Light light = null;

    private void OnEnable()
    {
        light = GetComponent<Light>();
    }

    void Update()
    {
        Shader.SetGlobalVector("_ToonLightDirection", -transform.forward);
    }
}
