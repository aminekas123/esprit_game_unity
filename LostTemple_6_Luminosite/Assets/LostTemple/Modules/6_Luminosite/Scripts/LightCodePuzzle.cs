using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 6b — Luminosité : reproduire un code lumineux affiché au-dessus de la porte, comme du morse.
    /// Couvrir le capteur (ou l'éclairer) crée une « ombre » : courte (● moins de 0,9 s) ou longue (▬ plus de 1,1 s).
    /// Une erreur remet le code à zéro.
    /// </summary>
    public class LightCodePuzzle : PuzzleStep
    {
        public Renderer[] lamps;
        public TextMesh codeText;
        public float shortMax = 0.9f, longMin = 1.1f;

        bool[] code;          // vrai = ombre longue
        int index;
        float baseline;
        bool shadow;
        float shadowStart;

        void Reset() { sensor = SensorKind.Light; }

        public override float Progress => IsActive && code != null ? index / (float)code.Length : -1f;

        protected override void OnBegin()
        {
            code = new bool[lamps.Length];
            for (int i = 0; i < code.Length; i++) code[i] = Random.value < 0.5f;
            if (code.Length > 1 && System.Array.TrueForAll(code, c => c == code[0])) code[1] = !code[0];
            baseline = Sensors.GetLux();
            index = 0;
            Show();
        }

        void Show()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < code.Length; i++) sb.Append(code[i] ? "▬ " : "● ");
            if (codeText != null) codeText.text = sb.ToString().TrimEnd();
            for (int i = 0; i < lamps.Length; i++)
                SetGlow(lamps[i], i < index ? new Color(1f, 0.8f, 0.3f) : new Color(0.4f, 0.75f, 1f), i < index ? 3f : i == index ? 1.2f : 0.3f);
        }

        void Update()
        {
            if (!IsActive) return;
            float change = Sensors.GetLightChange01(baseline);
            if (!shadow && change > 0.6f) { shadow = true; shadowStart = Time.time; ReportProgress(); }
            else if (shadow && change < 0.35f)
            {
                shadow = false;
                float d = Time.time - shadowStart;
                if (d < 0.12f) return;                    // simple scintillement : ignoré
                bool isLong = d >= longMin;
                bool isShort = d <= shortMax;
                if ((code[index] && isLong) || (!code[index] && isShort))
                {
                    index++;
                    Audio.Play(Sfx.Tick);
                    ReportProgress();
                    Show();
                    if (index >= code.Length) Complete();
                }
                else
                {
                    string msg = code[index] ? "Trop court ! ▬ = ombre longue (plus d'une seconde)." : "Trop long ! ● = ombre brève.";
                    index = 0;
                    Audio.Play(Sfx.Fail);
                    UIManager.Instance.ShowToast(msg + " Le code recommence.");
                    Show();
                }
            }
            // Pendant l'ombre, la lampe en cours se remplit pour aider à doser la durée
            if (shadow && index < lamps.Length)
            {
                float d = Time.time - shadowStart;
                SetGlow(lamps[index], d >= longMin ? new Color(1f, 0.5f, 0.2f) : new Color(0.6f, 0.9f, 1f), 1.2f + Mathf.Min(d, 1.5f) * 2f);
            }
        }
    }
}
