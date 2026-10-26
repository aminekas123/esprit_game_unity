using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 3a — Microphone : souffler ou parler fort pour réveiller le gardien.
    /// Une jauge se remplit tant que le son dépasse le seuil (calibré sur le bruit de fond) et redescend sinon.
    /// </summary>
    public class MicPuzzle : PuzzleStep
    {
        public Renderer[] eyes;
        public Transform guardianHead;
        public float fillTime = 2.5f;

        float gauge;
        float lastReport;
        Vector3 headBase;

        void Awake() { if (guardianHead != null) headBase = guardianHead.localPosition; }

        void Reset() { sensor = SensorKind.Microphone; }

        public override float Progress => IsActive ? gauge : -1f;

        void Update()
        {
            if (!IsActive) return;
            float level = Sensors.GetSoundLevel01();
            if (level >= 1f)
            {
                gauge += Time.deltaTime / fillTime;
                if (Time.time - lastReport > 0.5f) { lastReport = Time.time; ReportProgress(); }
            }
            else gauge -= Time.deltaTime / (fillTime * 2f);
            gauge = Mathf.Clamp01(gauge);

            foreach (var e in eyes) SetGlow(e, new Color(0.3f, 0.8f, 1f), gauge * 5f);
            if (guardianHead != null) guardianHead.localPosition = headBase + Random.insideUnitSphere * gauge * 0.03f;
            Audio.SetLoopIntensity(gauge);

            if (gauge >= 1f) Complete();
        }
    }
}
