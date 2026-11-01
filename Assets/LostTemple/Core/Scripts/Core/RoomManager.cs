using System.Collections;
using System.Text;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Déroulement d'une salle : l'explorateur se rend à chaque station (faisceau doré), l'énigme démarre
    /// avec son tutoriel, puis la station suivante s'allume. Missions secondaires : reliques à ramasser.
    /// Quand toutes les énigmes sont résolues, la porte s'ouvre et la sortie mène à la salle suivante.
    /// </summary>
    public class RoomManager : MonoBehaviour
    {
        public static RoomManager Current { get; private set; }

        [Tooltip("Place de la salle dans l'histoire complète (0 à 6) : titre, page du journal, relique secrète.")]
        public int storyIndex;

        /// <summary>Position de la salle dans ce projet (ordre des Build Settings).</summary>
        int RoomIndex => GameManager.Instance != null && GameManager.Instance.CurrentRoom >= 0 ? GameManager.Instance.CurrentRoom : 0;
        public PuzzleStep[] steps;
        [Tooltip("Une station par étape, dans le même ordre.")]
        public Station[] stations;
        [Tooltip("Porte qui descend dans le sol quand la salle est terminée.")]
        public Transform door;
        [Tooltip("Effets d'ouverture (poussière, gravats, lumière). Optionnel.")]
        public DoorFX doorFx;
        public ExitTrigger exit;
        [Tooltip("Étincelles jouées à chaque réussite (optionnel).")]
        public ParticleSystem sparks;
        [Tooltip("Composants (IRoomDoneListener) prévenus quand la salle est terminée, ex. le rocher géant.")]
        public MonoBehaviour[] roomDoneListeners;
        public float hintDelay = 25f;

        int stepIndex = -1;
        bool stepStarted;
        bool roomDone;
        float lastProgressTime;
        bool hintShown;
        float roomStart;

        public int RelicsTotal { get; private set; }
        public int RelicsFound { get; private set; }

        public PuzzleStep ActiveStep => stepStarted && stepIndex >= 0 && stepIndex < steps.Length ? steps[stepIndex] : null;
        PuzzleStep NextStepToReach => !stepStarted && stepIndex >= 0 && stepIndex < steps.Length ? steps[stepIndex] : null;

        void Awake() { Current = this; }

        void OnDestroy() { if (Current == this) Current = null; }

        IEnumerator Start()
        {
            if (exit != null) exit.gameObject.SetActive(false);
            RelicsTotal = FindObjectsByType<Relic>().Length;
            foreach (var s in steps) { s.Completed += OnStepCompleted; s.Progressed += OnProgress; }
            foreach (var st in stations) if (st != null) st.SetMarker(false);
            yield return null;
            UIManager.Instance.ShowRoomTitle(Journal.RoomTitles[Mathf.Clamp(storyIndex, 0, Journal.RoomTitles.Length - 1)]);
            yield return new WaitForSeconds(1f);
            roomStart = Time.time;
            AdvanceStep();
        }

        void Update()
        {
            if (hintShown || roomDone || GameManager.Instance.HardMode) return;
            if (Time.time - lastProgressTime < hintDelay) return;
            hintShown = true;
            var step = ActiveStep;
            if (step != null && step.IsActive) UIManager.Instance.ShowHint(step.hint);
            else if (NextStepToReach != null) UIManager.Instance.ShowHint("Suivez le faisceau doré : c'est là que se trouve la prochaine énigme.");
        }

        void OnProgress() { lastProgressTime = Time.time; }

        // ------------------------------------------------------------------ Missions (affichées dans le HUD)

        public string MissionText
        {
            get
            {
                var sb = new StringBuilder();
                for (int i = 0; i < steps.Length; i++)
                {
                    bool done = steps[i].IsDone;
                    bool current = i == stepIndex && !roomDone;
                    string line = steps[i].objective;
                    if (done) sb.Append("<color=#7BD88F>✓ ").Append(line).Append("</color>\n");
                    else if (current) sb.Append("<color=#F2B847>▶ ").Append(line).Append("</color>\n");
                    else sb.Append("<color=#7A8099>• ").Append(line).Append("</color>\n");
                }
                if (RelicsTotal > 0)
                    sb.Append(RelicsFound >= RelicsTotal ? "<color=#7BD88F>✓ " : "<color=#C9B27A>◆ ")
                      .Append("Reliques : ").Append(RelicsFound).Append("/").Append(RelicsTotal).Append("</color>\n");
                if (roomDone) sb.Append("<color=#F2B847>▶ ").Append(HasArtefact ? "Récupérez l'artefact" : "Franchissez la porte").Append("</color>");
                return sb.ToString();
            }
        }

        /// <summary>Objectif principal affiché en haut de l'écran pendant l'exploration.</summary>
        public string ExploreObjective
        {
            get
            {
                if (roomDone) return HasArtefact ? "La porte est ouverte : allez chercher l'artefact !" : "La porte est ouverte : sortez de la salle";
                var s = NextStepToReach;
                return s != null ? "Rejoignez le faisceau doré : " + char.ToLower(s.objective[0]) + s.objective.Substring(1) : "";
            }
        }

        bool IsLastRoom => RoomIndex == GameManager.RoomScenes.Length - 1;
        bool HasArtefact => storyIndex == Journal.RoomTitles.Length - 1;

        public void CollectRelic(Vector3 pos)
        {
            RelicsFound++;
            GameManager.Instance.AddRelic();
            AudioManager.Instance.Play(Sfx.Reveal);
            Burst(pos, 25);
            UIManager.Instance.ShowToast($"Relique trouvée : +1 vie ! ({GameManager.Instance.Lives} vies)");
            OnProgress();
        }

        // ------------------------------------------------------------------ Étapes

        void AdvanceStep()
        {
            SensorManager.Instance.StepAlternative = false;   // le choix « boutons » ne vaut que pour l'épreuve précédente
            stepIndex++;
            stepStarted = false;
            hintShown = false;
            lastProgressTime = Time.time;
            UIManager.Instance.SetStep(null);
            if (stepIndex >= steps.Length) { StartCoroutine(FinishRoom()); return; }
            var st = StationFor(stepIndex);
            if (st != null && GameManager.Instance.DemoMode && PlayerController.Instance != null)
            {
                // Démo : on saute l'exploration et on se place directement sur l'énigme.
                var p = PlayerController.Instance;
                p.Teleport(st.transform.position + Vector3.up * 0.05f, p.transform.rotation);
            }
            if (st != null) st.SetMarker(true);
            else StartStep(); // pas de station : l'énigme démarre directement
        }

        Station StationFor(int i) => stations != null && i < stations.Length ? stations[i] : null;

        public void OnStationEntered(Station st)
        {
            if (stepStarted || roomDone || stepIndex < 0 || stepIndex >= steps.Length) return;
            if (StationFor(stepIndex) != st) return;
            st.SetMarker(false);
            var p = PlayerController.Instance;
            if (p != null) p.SetCheckpoint(st.transform.position + Vector3.up * 0.1f);
            StartStep();
        }

        void StartStep()
        {
            stepStarted = true;
            var p = PlayerController.Instance;
            if (p != null) p.Frozen = true;
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
            if (s.fxAnchor != null) Burst(s.fxAnchor.position, 60);
            CameraRig.Shake(0.08f);
            StartCoroutine(AfterStep());
        }

        IEnumerator AfterStep()
        {
            yield return new WaitForSeconds(1.2f);
            var p = PlayerController.Instance;
            if (p != null) p.Frozen = false;
            AdvanceStep();
        }

        void Burst(Vector3 pos, int count)
        {
            if (sparks == null) return;
            sparks.transform.position = pos;
            sparks.Emit(count);
        }

        IEnumerator FinishRoom()
        {
            roomDone = true;
            UIManager.Instance.SetStep(null);
            var p = PlayerController.Instance;
            if (p != null) p.Frozen = true;

            if (doorFx != null) yield return doorFx.Open();
            else if (door != null)
            {
                AudioManager.Instance.Play(Sfx.Mechanism);
                CameraRig.Shake(0.15f);
                Vector3 a = door.position, b = a + Vector3.down * (door.lossyScale.y + 0.1f);
                for (float t = 0; t < 1f; t += Time.deltaTime / 1.6f)
                {
                    door.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, t));
                    yield return null;
                }
            }
            if (door != null) Burst(door.position + Vector3.up * 3f, 60);
            if (exit != null) exit.gameObject.SetActive(true);

            var gm = GameManager.Instance;
            float time = Time.time - roomStart;
            bool record = gm.SubmitTime(RoomIndex, time);
            gm.UnlockJournal(storyIndex);
            string clock = $"{(int)time / 60}:{(int)time % 60:00}";
            UIManager.Instance.ShowRoomTitle((HasArtefact ? "La dernière porte est ouverte !" : "Salle terminée !") + $"\n<size=50>{clock}{(record ? "  ★ Nouveau record !" : "")}</size>");

            if (!gm.DemoMode && storyIndex < Journal.Pages.Length)
            {
                yield return new WaitForSeconds(1.5f);
                bool closed = false;
                UIManager.Instance.ShowMessage($"Journal de l'explorateur — page {storyIndex + 1}", Journal.Pages[storyIndex], "Continuer", () => closed = true);
                while (!closed) yield return null;
            }
            if (p != null) p.Frozen = false;
            if (roomDoneListeners != null)
                foreach (var l in roomDoneListeners) if (l is IRoomDoneListener rl) rl.OnRoomDone();
        }

        public void OnExitReached()
        {
            if (!roomDone) return;
            GameManager.Instance.NextRoom();
        }
    }
}
