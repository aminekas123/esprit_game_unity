using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 3b — Proximité : couvrir puis découvrir le haut du téléphone (près de l'écouteur) trois fois
    /// pour allumer les trois runes et ouvrir la cachette du gardien.
    /// </summary>
    public class ProximityPuzzle : PuzzleStep
    {
        public Renderer[] runes;
        public Transform cacheLid;

        int count;
        bool wasCovered;

        void Reset() { sensor = SensorKind.Proximity; }

        public override float Progress => IsActive ? count / (float)runes.Length : -1f;

        protected override void OnBegin()
        {
            wasCovered = Sensors.IsCovered();
            foreach (var r in runes) SetGlow(r, Color.black, 0f);
        }

        void Update()
        {
            if (!IsActive) return;
            bool covered = Sensors.IsCovered();
            if (covered && count < runes.Length) SetGlow(runes[count], new Color(0.4f, 0.9f, 1f), 0.8f);
            if (wasCovered && !covered)
            {
                SetGlow(runes[count], new Color(0.4f, 0.9f, 1f), 4f);
                count++;
                ReportProgress();
                if (count >= runes.Length) Complete();
                else Audio.Play(Sfx.Tick);
            }
            wasCovered = covered;
        }

        protected override void OnComplete()
        {
            if (cacheLid != null) StartCoroutine(Open());
        }

        System.Collections.IEnumerator Open()
        {
            Audio.Play(Sfx.Mechanism);
            Quaternion a = cacheLid.localRotation, b = a * Quaternion.Euler(100f, 0f, 0f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
            {
                cacheLid.localRotation = Quaternion.Slerp(a, b, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }
    }
}
