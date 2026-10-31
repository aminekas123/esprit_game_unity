using UnityEngine;

namespace LostTemple
{
    /// <summary>Flamme de torche : intensité qui vacille.</summary>
    public class TorchFlicker : MonoBehaviour
    {
        Light l;
        float baseIntensity, seed;

        void Start() { l = GetComponent<Light>(); baseIntensity = l.intensity; seed = Random.value * 10f; }

        void Update() { l.intensity = baseIntensity * (0.85f + 0.15f * Mathf.PerlinNoise(seed, Time.time * 4f)); }
    }
}
