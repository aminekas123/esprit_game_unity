using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 4c — Microphone : une phrase en anglais apparaît sur une plaque ; il faut la lire à voix haute.
    /// - Sur Android : reconnaissance vocale du téléphone (AndroidSpeech) ; réussi si au moins 70 % des mots
    ///   de la phrase sont reconnus.
    /// - Sur PC / sans reconnaissance vocale : on vérifie seulement qu'on parle assez longtemps au micro
    ///   (durée de voix proportionnelle au nombre de mots). Secours : bouton « Souffler » ou Espace maintenu.
    /// </summary>
    public class SpeakSentencePuzzle : PuzzleStep
    {
        public TextMesh plaque;
        public Renderer plaqueRenderer;
        [Range(0.3f, 1f)] public float requiredMatch = 0.7f;

        static readonly string[] sentences =
        {
            "Open the door of the temple",
            "I am not afraid of the dark",
            "The golden key belongs to me",
            "Let me pass, ancient guardian",
            "The treasure is behind this wall",
            "I will find the lost artifact",
        };

        string sentence;
        string[] targetWords;
        bool useSpeech;
        bool listening;
        float restartAt;
        float spoken;          // mode PC : secondes de voix détectées
        float needed;
        float bestMatch;
        HudGauge gauge;

        void Reset() { sensor = SensorKind.Microphone; }

        public override float Progress => IsActive ? (useSpeech ? bestMatch / requiredMatch : spoken / needed) : -1f;

        protected override void OnBegin()
        {
            sentence = sentences[Random.Range(0, sentences.Length)];
            targetWords = Words(sentence);
            needed = Mathf.Max(1.5f, targetWords.Length * 0.3f);
            spoken = 0f; bestMatch = 0f;
            plaque.text = Wrap(sentence, 15);
            // Lueur très faible : une plaque trop lumineuse « éblouit » (bloom) et cache le texte
            SetGlow(plaqueRenderer, new Color(1f, 0.75f, 0.35f), 0.08f);

            useSpeech = !Sensors.ForceAlternative && !Sensors.StepAlternative && AndroidSpeech.Available;
            objective = $"Lisez à voix haute : « {sentence} »";
            UIManager.Instance.SetStep(this);

            gauge = new HudGauge
            {
                label = useSpeech ? "Mots reconnus" : "Lecture (parlez au micro)",
                color = new Color(0.45f, 0.85f, 1f),
                value = () => Progress,
                visible = () => IsActive,
                buttonLabel = "Réécouter",
                onButton = () => Listen(),
                buttonVisible = () => IsActive && useSpeech && !listening,
            };
            HudGauge.Register(gauge);

            if (useSpeech)
            {
                // Le micro d'Unity et la reconnaissance vocale ne peuvent pas écouter en même temps.
                Sensors.StopMicrophone();
                Listen();
            }
            else UIManager.Instance.ShowToast("Lisez la phrase à voix haute, sans vous arrêter.");
        }

        void Listen()
        {
            listening = true;
            restartAt = 0f;
            AndroidSpeech.StartListening("en-US");
            UIManager.Instance.ShowToast("Je vous écoute… lisez la phrase en anglais.");
        }

        void Update()
        {
            if (!IsActive) return;
            if (useSpeech) UpdateSpeech();
            else UpdateVoiceTime();
        }

        /// <summary>Android : on compare ce qui a été reconnu avec la phrase.</summary>
        void UpdateSpeech()
        {
            string heard = AndroidSpeech.TakeResult();
            if (heard != null)
            {
                listening = false;
                float match = 0f;
                string bestText = "";
                foreach (var alt in heard.Split('|'))
                {
                    float m = Match(alt);
                    if (m > match) { match = m; bestText = alt; }
                }
                bestMatch = Mathf.Max(bestMatch, match);
                ReportProgress();
                if (match >= requiredMatch) { Complete(); return; }
                Audio.Play(Sfx.Fail);
                UIManager.Instance.ShowToast(string.IsNullOrEmpty(bestText) ? "Je n'ai rien compris. Réessayez." : $"J'ai entendu : « {bestText} » ({match * 100f:0} %). Réessayez !");
                restartAt = Time.time + 2f;
            }
            int err = AndroidSpeech.TakeError();
            if (err >= 0)
            {
                listening = false;
                if (err == 9) { UIManager.Instance.ShowToast("Autorisez le micro pour la reconnaissance vocale."); return; }
                restartAt = Time.time + 1f;   // 6 = silence, 7 = rien reconnu : on réécoute
            }
            if (!listening && restartAt > 0f && Time.time >= restartAt) Listen();
        }

        /// <summary>PC / secours : on cumule le temps où l'on parle assez fort.</summary>
        void UpdateVoiceTime()
        {
            if (Sensors.GetSoundLevel01() >= 0.8f)
            {
                spoken += Time.deltaTime;
                ReportProgress();
                SetGlow(plaqueRenderer, new Color(1f, 0.75f, 0.35f), 0.08f + spoken / needed * 0.25f);
            }
            if (spoken >= needed) Complete();
        }

        float Match(string heard)
        {
            var got = new HashSet<string>(Words(heard));
            int ok = 0;
            foreach (var w in targetWords) if (got.Contains(w)) ok++;
            return targetWords.Length == 0 ? 0f : ok / (float)targetWords.Length;
        }

        static string[] Words(string s)
        {
            var sb = new StringBuilder();
            foreach (char c in s.ToLowerInvariant()) sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
            return sb.ToString().Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        }

        static string Wrap(string s, int width)
        {
            var sb = new StringBuilder();
            int line = 0;
            foreach (var w in s.Split(' '))
            {
                if (line > 0 && line + w.Length + 1 > width) { sb.Append('\n'); line = 0; }
                else if (line > 0) { sb.Append(' '); line++; }
                sb.Append(w); line += w.Length;
            }
            return sb.ToString();
        }

        protected override void OnComplete()
        {
            HudGauge.Unregister(gauge);
            if (useSpeech) { AndroidSpeech.Stop(); Sensors.StartMicrophone(); }
            plaque.text = "PERFECT!";
            plaque.color = new Color(0.6f, 1f, 0.7f);
            SetGlow(plaqueRenderer, new Color(0.5f, 1f, 0.6f), 0.3f);
        }

        void OnDestroy()
        {
            if (gauge != null) HudGauge.Unregister(gauge);
            if (useSpeech) AndroidSpeech.Stop();
        }
    }
}
