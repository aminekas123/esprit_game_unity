using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 6 — Luminosité : un pont de cristal invisible enjambe le gouffre. Faire varier la lumière
    /// (couvrir le capteur ou l'éclairer avec une lampe) le fait apparaître ; une fois révélé, il devient solide.
    /// </summary>
    public class LightRevealPuzzle : PuzzleStep
    {
        public Renderer[] hiddenParts;
        public Collider[] solidParts;
        public Light revealLight;
        public float revealTime = 2.5f;

        float baseline;
        float reveal;
        float lastReport;

        void Reset() { sensor = SensorKind.Light; }

        public override float Progress => IsActive ? reveal : -1f;

        void Awake()
        {
            foreach (var c in solidParts) c.enabled = false;
            Apply(0f);
        }

        protected override void OnBegin() { baseline = Sensors.GetLux(); }

        void Update()
        {
            if (!IsActive) return;
            float change = Sensors.GetLightChange01(baseline);
            if (change > 0.6f)
            {
                reveal += Time.deltaTime / revealTime;
                if (Time.time - lastReport > 0.5f) { lastReport = Time.time; ReportProgress(); }
            }
            else reveal -= Time.deltaTime / (revealTime * 3f);
            reveal = Mathf.Clamp01(reveal);
            Apply(Mathf.Max(reveal, change * 0.25f));
            Audio.SetLoopIntensity(reveal * 0.6f);
            if (reveal >= 1f) Complete();
        }

        void Apply(float v)
        {
            // Le cristal scintille d'abord par morceaux, puis apparaît entièrement.
            for (int i = 0; i < hiddenParts.Length; i++)
            {
                var r = hiddenParts[i];
                float threshold = (i + 1f) / (hiddenParts.Length + 1f) * 0.9f;
                bool on = v >= 1f || v > threshold || (v > 0.05f && Mathf.PerlinNoise(i * 3.1f, Time.time * 6f) < v);
                r.enabled = on;
                if (on) SetGlow(r, new Color(0.4f, 0.85f, 1f), 0.5f + v * 2.5f);
            }
            if (revealLight != null) revealLight.intensity = v * 4f;
        }

        protected override void OnComplete()
        {
            Apply(1f);
            foreach (var c in solidParts) c.enabled = true;
            Audio.Play(Sfx.Reveal);
        }
    }
}
