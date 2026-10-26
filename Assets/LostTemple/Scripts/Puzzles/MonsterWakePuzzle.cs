using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 4a — Microphone : parler fort ou souffler pour réveiller le monstre.
    /// Une jauge se remplit tant que le son dépasse le seuil (calibré sur le bruit de fond) et redescend sinon.
    /// </summary>
    public class MonsterWakePuzzle : PuzzleStep
    {
        public MonsterController monster;
        public float fillTime = 2.5f;

        float gauge;
        float lastReport;

        void Reset() { sensor = SensorKind.Microphone; }

        public override float Progress => IsActive ? gauge : -1f;

        void Update()
        {
            if (!IsActive) return;
            if (Sensors.GetSoundLevel01() >= 1f)
            {
                gauge += Time.deltaTime / fillTime;
                if (Time.time - lastReport > 0.5f) { lastReport = Time.time; ReportProgress(); }
            }
            else gauge -= Time.deltaTime / (fillTime * 2f);
            gauge = Mathf.Clamp01(gauge);

            monster.SetWakeProgress(gauge);
            Audio.SetLoopIntensity(gauge);
            if (gauge >= 1f) Complete();
        }

        protected override void OnComplete() { monster.Awaken(); }
    }
}
