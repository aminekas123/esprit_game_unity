using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 2b — Gyroscope : faire pivoter le téléphone comme un volant pour tourner chaque statue
    /// jusqu'à ce que son rayon touche le symbole lumineux au sol.
    /// </summary>
    public class StatueAlignPuzzle : PuzzleStep
    {
        public Transform[] statues;
        public Renderer[] beams;
        public Renderer[] targets;
        public float[] targetAngles = { 60f, -90f, 150f };
        public float tolerance = 10f;
        public float holdTime = 0.7f;

        int current;
        float hold;
        float[] angles;
        float lastReported;

        void Reset() { sensor = SensorKind.Gyroscope; }

        public override float Progress => IsActive ? (current + hold / holdTime) / statues.Length : -1f;

        void Awake()
        {
            angles = new float[statues.Length];
            for (int i = 0; i < statues.Length; i++) angles[i] = statues[i].localEulerAngles.y;
        }

        protected override void OnBegin()
        {
            current = 0;
            Highlight();
        }

        void Update()
        {
            if (!IsActive) return;
            float rate = Sensors.GetRotationRate();
            angles[current] += rate * Time.deltaTime;
            statues[current].localRotation = Quaternion.Euler(0f, angles[current], 0f);

            float error = Mathf.Abs(Mathf.DeltaAngle(angles[current], targetAngles[current]));
            float closeness = 1f - Mathf.Clamp01(error / 90f);
            SetGlow(beams[current], new Color(1f, 0.75f, 0.3f), 0.8f + closeness * 3f);
            SetGlow(targets[current], GameManager.GlyphColors[current], 0.5f + closeness * 2f);

            if (Mathf.Abs(rate) > 5f && Time.time - lastReported > 1f) { lastReported = Time.time; ReportProgress(); }

            if (error < tolerance)
            {
                hold += Time.deltaTime;
                if (hold >= holdTime)
                {
                    angles[current] = targetAngles[current];
                    statues[current].localRotation = Quaternion.Euler(0f, angles[current], 0f);
                    SetGlow(targets[current], GameManager.GlyphColors[current], 4f);
                    hold = 0f;
                    current++;
                    ReportProgress();
                    if (current >= statues.Length) { Complete(); return; }
                    Audio.Play(Sfx.Reveal);
                    UIManager.Instance.ShowToast($"Statue {current}/{statues.Length} alignée !");
                    Highlight();
                }
            }
            else hold = Mathf.Max(0f, hold - Time.deltaTime);
        }

        void Highlight()
        {
            for (int i = 0; i < statues.Length; i++)
                SetGlow(beams[i], new Color(1f, 0.75f, 0.3f), i == current ? 1f : (i < current ? 2f : 0f));
        }
    }
}
