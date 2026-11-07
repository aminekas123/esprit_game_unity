using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 6c — Luminosité : un grand brasier bloque la porte. Couvrir le capteur de lumière avec la main
    /// l'étouffe petit à petit ; si on retire la main trop tôt, il reprend. Maintenu assez longtemps, il s'éteint.
    /// </summary>
    public class BigFirePuzzle : PuzzleStep
    {
        public ParticleSystem[] flames;
        public ParticleSystem steam;
        public Light fireLight;
        public Collider blocker;
        [Tooltip("Secondes de capteur couvert pour éteindre complètement le feu.")]
        public float coverTime = 5f;
        [Tooltip("Secondes pour que le feu reprenne entièrement quand on retire la main.")]
        public float regrowTime = 4f;

        float strength = 1f;
        float baseline;
        float[] baseRates;
        float[] baseSizes;
        float baseLight;
        bool covered;
        bool hintShown;
        float lastReport;

        void Reset() { sensor = SensorKind.Light; }

        public override float Progress => IsActive ? 1f - strength : -1f;

        void Awake()
        {
            baseRates = new float[flames.Length];
            baseSizes = new float[flames.Length];
            for (int i = 0; i < flames.Length; i++)
            {
                baseRates[i] = flames[i].emission.rateOverTime.constant;
                baseSizes[i] = flames[i].main.startSizeMultiplier;
            }
            if (fireLight != null) baseLight = fireLight.intensity;
        }

        protected override void OnBegin()
        {
            baseline = Sensors.GetLux();
            strength = 1f;
            if (!Sensors.UsesFallback(SensorKind.Light) && baseline < 6f)
                UIManager.Instance.ShowToast("La pièce est trop sombre : allumez la lumière, sinon la main ne fera pas d'ombre.");
        }

        void Update()
        {
            // Le feu vacille en permanence, même avant l'épreuve
            if (fireLight != null)
                fireLight.intensity = baseLight * strength * (0.85f + 0.3f * Mathf.PerlinNoise(Time.time * 7f, 0.3f));
            if (!IsActive) return;

            bool now = Sensors.IsLightCovered(baseline);
            if (now && !covered) Audio.Play(Sfx.Tick, 0.5f);
            if (!now && covered && strength > 0.05f && !hintShown)
            {
                hintShown = true;
                UIManager.Instance.ShowToast("Le feu reprend ! Gardez la main sur le capteur jusqu'au bout.");
            }
            covered = now;

            strength += covered ? -Time.deltaTime / coverTime : Time.deltaTime / regrowTime;
            strength = Mathf.Clamp01(strength);
            Apply();
            if (Time.time - lastReport > 0.3f) { lastReport = Time.time; ReportProgress(); }
            if (strength <= 0f) Complete();
        }

        void Apply()
        {
            for (int i = 0; i < flames.Length; i++)
            {
                var em = flames[i].emission;
                em.rateOverTime = baseRates[i] * strength;
                var main = flames[i].main;
                main.startSizeMultiplier = baseSizes[i] * Mathf.Lerp(0.35f, 1f, strength);
            }
        }

        protected override void OnComplete()
        {
            foreach (var f in flames) f.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            if (steam != null) steam.Play();
            if (blocker != null) blocker.enabled = false;
            if (fireLight != null) StartCoroutine(FadeLight());
            Audio.Play(Sfx.Reveal);
            UIManager.Instance.ShowToast("Le brasier est éteint ! Le passage est libre.");
        }

        IEnumerator FadeLight()
        {
            float a = fireLight.intensity;
            for (float t = 0; t < 1f; t += Time.deltaTime)
            {
                fireLight.intensity = Mathf.Lerp(a, 0f, t);
                yield return null;
            }
            fireLight.enabled = false;
        }
    }
}
