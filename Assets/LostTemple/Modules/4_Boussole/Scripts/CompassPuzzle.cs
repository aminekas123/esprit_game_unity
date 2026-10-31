using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 2a — Magnétomètre : une aiguille magique indique de quel côté se tourner.
    /// Le joueur pivote sur lui-même jusqu'à viser la direction cachée et la tient quelques instants.
    /// La cible est relative au cap de départ, pour que l'énigme marche quelle que soit l'orientation de la pièce.
    /// </summary>
    public class CompassPuzzle : PuzzleStep
    {
        public Transform needle;
        public Renderer needleGem;
        public Transform passageWall;
        public float tolerance = 18f;
        public float holdTime = 1.5f;
        [Tooltip("Nombre de directions successives à trouver.")]
        public int directions = 3;
        [Tooltip("Une lampe par direction trouvée (sur le socle de la boussole).")]
        public Renderer[] stepLamps;

        float target;
        int found;
        float hold;
        float bestError = 999f;

        void Reset() { sensor = SensorKind.Compass; }

        public override float Progress => IsActive ? (found + hold / holdTime) / directions : -1f;

        protected override void OnBegin()
        {
            found = 0;
            target = Mathf.Repeat(Sensors.GetHeading() + Random.Range(110f, 250f), 360f);
            foreach (var l in stepLamps) SetGlow(l, Color.black, 0f);
        }

        /// <summary>Une direction trouvée : la lampe s'allume et l'aiguille indique la suivante.</summary>
        void DirectionFound()
        {
            if (found < stepLamps.Length) SetGlow(stepLamps[found], new Color(1f, 0.8f, 0.3f), 3f);
            found++;
            hold = 0f;
            bestError = 999f;
            ReportProgress();
            if (found >= directions) { Complete(); return; }
            Audio.Play(Sfx.Reveal);
            UIManager.Instance.ShowToast($"Direction {found}/{directions} trouvée ! L'aiguille indique la suivante.");
            float turn = Random.Range(100f, 170f) * (Random.value < 0.5f ? -1f : 1f);
            target = Mathf.Repeat(target + turn, 360f);
        }

        void Update()
        {
            if (!IsActive) return;
            float error = Mathf.DeltaAngle(Sensors.GetHeading(), target);   // >0 : tourner à droite
            float abs = Mathf.Abs(error);

            if (needle != null)
                needle.localRotation = Quaternion.Slerp(needle.localRotation, Quaternion.Euler(0f, error, 0f), 1f - Mathf.Exp(-8f * Time.deltaTime));

            float closeness = 1f - Mathf.Clamp01(abs / 180f);
            SetGlow(needleGem, Color.Lerp(new Color(0.2f, 0.4f, 1f), new Color(1f, 0.8f, 0.3f), closeness), 0.5f + closeness * 2.5f);

            if (abs < bestError - 15f) { bestError = abs; ReportProgress(); }

            if (abs < tolerance)
            {
                if (hold == 0f) Audio.Play(Sfx.Tick);
                hold += Time.deltaTime;
                Audio.SetLoopIntensity(hold / holdTime);
                if (hold >= holdTime) DirectionFound();
            }
            else
            {
                hold = Mathf.Max(0f, hold - Time.deltaTime * 2f);
                Audio.SetLoopIntensity(0f);
            }
        }

        protected override void OnComplete()
        {
            if (passageWall != null) StartCoroutine(Slide());
        }

        System.Collections.IEnumerator Slide()
        {
            Audio.Play(Sfx.Mechanism);
            Vector3 a = passageWall.position, b = a + Vector3.down * (passageWall.lossyScale.y + 0.1f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.5f)
            {
                passageWall.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }
    }
}
