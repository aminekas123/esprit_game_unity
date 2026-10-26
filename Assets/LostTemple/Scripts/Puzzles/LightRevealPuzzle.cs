using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle finale a — Luminosité : faire varier la lumière (couvrir le capteur ou l'éclairer avec une lampe)
    /// pour révéler le dernier symbole gravé sur le mur.
    /// </summary>
    public class LightRevealPuzzle : PuzzleStep
    {
        public TextMesh hiddenSymbol;
        public Renderer wallGlyph;
        public Light revealLight;
        public float revealTime = 2f;

        float baseline;
        float reveal;
        float lastReport;

        void Reset() { sensor = SensorKind.Light; }

        public override float Progress => IsActive ? reveal : -1f;

        protected override void OnBegin()
        {
            baseline = Sensors.GetLux();
            int glyph = GameManager.Instance.Code[3];
            hiddenSymbol.text = GameManager.GlyphNames[glyph];
            Apply(0f);
        }

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
            int glyph = GameManager.Instance.Code[3];
            Color c = GameManager.GlyphColors[glyph];
            hiddenSymbol.color = new Color(c.r, c.g, c.b, v);
            SetGlow(wallGlyph, c, v * 3f);
            if (revealLight != null) revealLight.intensity = v * 4f;
        }

        protected override void OnComplete()
        {
            Apply(1f);
            Audio.Play(Sfx.Reveal);
        }
    }
}
