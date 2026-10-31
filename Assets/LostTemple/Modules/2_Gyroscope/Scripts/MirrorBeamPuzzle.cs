using UnityEngine;
using UnityEngine.InputSystem;

namespace LostTemple
{
    /// <summary>
    /// Salle 3b — Gyroscope : faire pivoter des miroirs (comme un volant) pour guider un rayon de lumière
    /// jusqu'au cristal. Un bouton (ou la touche N) passe au miroir suivant. Le rayon est recalculé à chaque image.
    /// </summary>
    public class MirrorBeamPuzzle : PuzzleStep
    {
        public Transform source;
        public Transform[] mirrors;
        public Renderer[] mirrorHighlights;
        public Collider target;
        public Renderer targetRenderer;
        public LineRenderer beam;
        public float holdTime = 1f;
        public int maxBounces = 6;

        int selected;
        float hold;
        float[] angles;
        HudGauge gauge;
        readonly Vector3[] points = new Vector3[8];

        void Reset() { sensor = SensorKind.Gyroscope; }

        public override float Progress => IsActive ? hold / holdTime : -1f;

        void Awake()
        {
            angles = new float[mirrors.Length];
            for (int i = 0; i < mirrors.Length; i++) angles[i] = mirrors[i].eulerAngles.y;
            if (beam != null) beam.enabled = false;
        }

        protected override void OnBegin()
        {
            selected = 0;
            beam.enabled = true;
            gauge = new HudGauge
            {
                label = "Rayon sur le cristal",
                color = new Color(1f, 0.85f, 0.4f),
                value = () => hold / holdTime,
                visible = () => IsActive,
                buttonLabel = "Miroir suivant ▶",
                onButton = NextMirror,
                buttonVisible = () => IsActive,
            };
            HudGauge.Register(gauge);
            Highlight();
        }

        void NextMirror()
        {
            selected = (selected + 1) % mirrors.Length;
            Audio.Play(Sfx.Tick);
            Highlight();
            ReportProgress();
        }

        void Update()
        {
            if (!IsActive) return;
            if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame) NextMirror();

            float rate = Sensors.GetRotationRate();
            angles[selected] += rate * Time.deltaTime;
            mirrors[selected].rotation = Quaternion.Euler(0f, angles[selected], 0f);
            if (Mathf.Abs(rate) > 5f) ReportProgress();

            bool onTarget = Trace();
            hold = onTarget ? hold + Time.deltaTime : Mathf.Max(0f, hold - Time.deltaTime * 2f);
            SetGlow(targetRenderer, new Color(1f, 0.85f, 0.4f), onTarget ? 2f + hold * 4f : 0.3f);
            Audio.SetLoopIntensity(onTarget ? 0.5f : 0f);
            if (hold >= holdTime) Complete();
        }

        /// <summary>Lance le rayon depuis la source et le fait rebondir sur les miroirs. Vrai s'il touche le cristal.</summary>
        bool Trace()
        {
            Vector3 origin = source.position, dir = source.forward;
            int n = 0;
            points[n++] = origin;
            bool hitTarget = false;
            for (int b = 0; b <= maxBounces && n < points.Length; b++)
            {
                if (!Physics.Raycast(origin, dir, out var hit, 30f, ~0, QueryTriggerInteraction.Ignore))
                {
                    points[n++] = origin + dir * 30f;
                    break;
                }
                points[n++] = hit.point;
                if (hit.collider == target) { hitTarget = true; break; }
                bool isMirror = false;
                foreach (var m in mirrors) if (hit.collider.transform.IsChildOf(m)) { isMirror = true; break; }
                if (!isMirror) break;
                dir = Vector3.Reflect(dir, hit.normal);
                origin = hit.point + dir * 0.02f;
            }
            beam.positionCount = n;
            for (int i = 0; i < n; i++) beam.SetPosition(i, points[i]);
            return hitTarget;
        }

        void Highlight()
        {
            for (int i = 0; i < mirrorHighlights.Length; i++)
                SetGlow(mirrorHighlights[i], new Color(1f, 0.75f, 0.3f), i == selected ? 2.5f : 0f);
        }

        protected override void OnComplete()
        {
            HudGauge.Unregister(gauge);
            foreach (var h in mirrorHighlights) SetGlow(h, Color.black, 0f);
            SetGlow(targetRenderer, new Color(1f, 0.85f, 0.4f), 6f);
        }

        void OnDestroy() { if (gauge != null) HudGauge.Unregister(gauge); }
    }
}
