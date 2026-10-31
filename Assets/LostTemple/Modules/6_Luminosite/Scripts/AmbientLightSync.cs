using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostTemple
{
    /// <summary>
    /// La lumière réelle de la pièce règle l'ambiance du temple : dans le noir, il devient plus sombre
    /// et brumeux ; en plein jour, plus clair. Désactivable dans les paramètres.
    /// </summary>
    public class AmbientLightSync : MonoBehaviour
    {
        public static bool Enabled
        {
            get => PlayerPrefs.GetInt("LT_LightSync", 1) == 1;
            set => PlayerPrefs.SetInt("LT_LightSync", value ? 1 : 0);
        }

        Color baseAmbient;
        float baseFog;
        float factor = 1f;

        void Awake() { SceneManager.sceneLoaded += OnLoaded; }
        void OnDestroy() { SceneManager.sceneLoaded -= OnLoaded; }

        void OnLoaded(Scene s, LoadSceneMode m)
        {
            baseAmbient = RenderSettings.ambientLight;
            baseFog = RenderSettings.fogDensity;
            factor = 1f;
        }

        void Update()
        {
            float lux = SensorManager.Instance != null ? SensorManager.Instance.GetLux() : -1f;
            float target = 1f;
            if (Enabled && lux >= 0f)
                target = lux < 5f ? 0.6f : lux < 50f ? Mathf.Lerp(0.6f, 1f, (lux - 5f) / 45f) : lux > 1000f ? 1.2f : Mathf.Lerp(1f, 1.2f, (lux - 50f) / 950f);
            factor = Mathf.Lerp(factor, target, 1f - Mathf.Exp(-1.5f * Time.unscaledDeltaTime));
            if (baseFog <= 0f) return;
            RenderSettings.ambientLight = baseAmbient * factor;
            RenderSettings.fogDensity = baseFog * Mathf.Lerp(1.6f, 0.8f, Mathf.InverseLerp(0.6f, 1.2f, factor));
        }
    }
}
