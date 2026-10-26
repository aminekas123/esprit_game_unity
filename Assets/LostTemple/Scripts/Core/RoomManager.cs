using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Déroulement d'une salle : tutoriel de chaque étape, activation des énigmes dans l'ordre,
    /// indice après une période sans progression, ouverture de la porte et révélation du symbole.
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Current { get; private set; }

        public int roomIndex;
        public PuzzleStep[] steps;
        [Tooltip("Porte qui descend dans le sol quand la salle est terminée.")]
        public Transform door;
        [Tooltip("Plaque où le symbole découvert s'affiche (optionnel).")]
        public TextMesh symbolText;
        public Renderer symbolPlate;
        [Tooltip("Étincelles jouées à chaque réussite (optionnel).")]
        public ParticleSystem sparks;
        public float hintDelay = 25f;

        int stepIndex = -1;
        float lastProgressTime;
        bool hintShown;

        public PuzzleStep ActiveStep => stepIndex >= 0 && stepIndex < steps.Length ? steps[stepIndex] : null;

        void Awake() { Current = this; }

        void OnDestroy() { if (Current == this) Current = null; }

        IEnumerator Start()
        {
            if (symbolText != null) symbolText.text = "";
            foreach (var s in steps) { s.Completed += OnStepCompleted; s.Progressed += OnProgress; }
            yield return null;
            UIManager.Instance.ShowRoomTitle(GameManager.RoomTitles[roomIndex]);
            yield return new WaitForSeconds(1.2f);
            NextStep();
        }

        void Update()
        {
            var step = ActiveStep;
            if (step == null || !step.IsActive || hintShown) return;
            if (Time.time - lastProgressTime > hintDelay)
            {
                hintShown = true;
                UIManager.Instance.ShowHint(step.hint);
            }
        }

        void OnProgress() { lastProgressTime = Time.time; }

        void NextStep()
        {
            stepIndex++;
            if (stepIndex >= steps.Length) { StartCoroutine(FinishRoom()); return; }
            StartCoroutine(PrepareAndBegin(steps[stepIndex]));
        }

        IEnumerator PrepareAndBegin(PuzzleStep step)
        {
            bool ok = false;
            UIManager.Instance.ShowTutorial(step, () => ok = true);
            while (!ok) yield return null;

            // Le micro demande une permission expliquée au joueur, puis une mesure du bruit de fond.
            if (step.usesSensor && step.sensor == SensorKind.Microphone && !SensorManager.Instance.ForceAlternative && Microphone.devices.Length > 0)
            {
                if (!SensorManager.Instance.MicRunning)
                {
                    bool done = false;
                    UIManager.Instance.ShowMessage("Autorisation du micro",
                        "Le gardien se réveille avec le son de votre voix ou de votre souffle.\n\nLe jeu mesure seulement le volume sonore : rien n'est enregistré ni envoyé.\n\nSi vous refusez, un bouton remplacera le micro.",
                        "Autoriser", () => SensorManager.Instance.RequestMicrophone(_ => done = true));
                    while (!done) yield return null;
                }
                if (SensorManager.Instance.MicRunning)
                {
                    UIManager.Instance.ShowToast("Silence… mesure du bruit ambiant");
                    yield return SensorManager.Instance.CalibrateMicNoise();
                }
            }
            if (step.usesSensor && step.sensor == SensorKind.Accelerometer) SensorManager.Instance.CalibrateTilt();

            lastProgressTime = Time.time;
            hintShown = false;
            UIManager.Instance.SetStep(step);
            step.Begin();
        }

        void OnStepCompleted(PuzzleStep s)
        {
            UIManager.Instance.HideHint();
            if (s.fxAnchor != null) Burst(s.fxAnchor.position);
            StartCoroutine(AfterStep());
        }

        void Burst(Vector3 pos)
        {
            if (sparks == null) return;
            sparks.transform.position = pos;
            sparks.Emit(60);
        }

        IEnumerator AfterStep()
        {
            yield return new WaitForSeconds(0.8f);
            NextStep();
        }

        IEnumerator FinishRoom()
        {
            UIManager.Instance.SetStep(null);
            var gm = GameManager.Instance;
            int glyph = roomIndex < gm.Code.Length ? gm.Code[roomIndex] : -1;

            if (door != null)
            {
                AudioManager.Instance.Play(Sfx.Mechanism);
                Burst(door.position + Vector3.up * 1.5f);
                Vector3 a = door.position, b = a + Vector3.down * (door.lossyScale.y + 0.1f);
                for (float t = 0; t < 1f; t += Time.deltaTime / 1.6f)
                {
                    door.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, t)) + Random.insideUnitSphere * 0.02f * (1 - t);
                    yield return null;
                }
                door.position = b;
            }

            if (glyph >= 0 && roomIndex < 3)
            {
                if (symbolText != null)
                {
                    symbolText.text = GameManager.GlyphNames[glyph];
                    symbolText.color = GameManager.GlyphColors[glyph];
                }
                if (symbolPlate != null)
                {
                    var m = symbolPlate.material;
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", GameManager.GlyphColors[glyph] * 2f);
                }
                AudioManager.Instance.Play(Sfx.Reveal);
                yield return new WaitForSeconds(0.6f);
                UIManager.Instance.ShowMessage("Symbole découvert",
                    $"Symbole n°{roomIndex + 1} : <b><color=#{ColorUtility.ToHtmlStringRGB(GameManager.GlyphColors[glyph])}>{GameManager.GlyphNames[glyph]}</color></b>\n\nRetenez-le : la salle de l'artefact vous demandera les symboles dans l'ordre.",
                    "Salle suivante", () => gm.NextRoom());
            }
        }
    }
}
