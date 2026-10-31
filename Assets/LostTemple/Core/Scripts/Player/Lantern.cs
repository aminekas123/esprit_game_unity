using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Lanterne de l'explorateur. Dans les salles sombres (drains = vrai), elle s'épuise avec le temps :
    /// secouer le téléphone (accéléromètre) la ranime.
    /// </summary>
    public class Lantern : MonoBehaviour
    {
        public static Lantern Instance { get; private set; }

        public Light lightSource;
        public Renderer flame;
        public bool drains;
        public float duration = 40f;

        public float Energy { get; private set; } = 1f;
        public bool Drains => drains;

        float baseIntensity, baseRange;
        bool warned;

        void Awake() { Instance = this; }

        void Start()
        {
            baseIntensity = lightSource.intensity;
            baseRange = lightSource.range;
            if (SensorManager.Instance != null) SensorManager.Instance.Shaken += Recharge;
        }

        void OnDestroy()
        {
            if (SensorManager.Instance != null) SensorManager.Instance.Shaken -= Recharge;
            if (Instance == this) Instance = null;
        }

        void Update()
        {
            var p = PlayerController.Instance;
            if (drains && p != null && !p.Frozen)
            {
                Energy = Mathf.Max(0f, Energy - Time.deltaTime / duration);
                if (Energy < 0.3f && !warned)
                {
                    warned = true;
                    UIManager.Instance.ShowToast("Votre lanterne faiblit ! Secouez le téléphone pour la ranimer.");
                }
            }
            float flicker = 0.9f + 0.1f * Mathf.PerlinNoise(Time.time * 5f, 0.3f);
            lightSource.intensity = baseIntensity * (0.1f + 0.9f * Energy) * flicker * (drains ? 1.4f : 1f);
            lightSource.range = baseRange * (0.4f + 0.6f * Energy) * (drains ? 1.4f : 1f);
            if (flame != null)
            {
                var m = flame.material;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(1f, 0.55f, 0.15f) * (0.3f + 3.7f * Energy));
            }
        }

        public void Recharge()
        {
            if (!drains) return;
            bool wasLow = Energy < 0.6f;
            Energy = 1f;
            warned = false;
            if (wasLow)
            {
                AudioManager.Instance.Play(Sfx.Reveal, 0.5f);
                UIManager.Instance.ShowToast("La flamme repart de plus belle !");
            }
        }
    }
}
