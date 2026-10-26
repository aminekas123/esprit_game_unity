using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 4b — Microphone : la voix pousse une boule d'énergie vers le monstre, qui la repousse.
    /// Plus on crie fort, plus elle avance ; si on se tait, le monstre la renvoie. Quand elle le touche, il est détruit.
    /// </summary>
    public class VoiceBallPuzzle : PuzzleStep
    {
        public Transform ball;
        public Renderer ballRenderer;
        public Light ballLight;
        public Transform startPoint, monsterPoint;
        public MonsterController monster;
        public ParticleSystem impactFx;
        public float pushSpeed = 2.6f;
        public float monsterPush = 0.9f;

        float t;
        float lastReport;
        float best;

        void Reset() { sensor = SensorKind.Microphone; }

        public override float Progress => IsActive ? t : -1f;

        protected override void OnBegin()
        {
            t = 0f;
            ball.gameObject.SetActive(true);
            Place(0f);
        }

        void Update()
        {
            if (!IsActive) return;
            // Attendre que le monstre ait fini de se lever avant de jouer
            if (monster.State != MonsterState.Awake) return;

            float level = Sensors.GetSoundLevel01();
            float dist = Vector3.Distance(startPoint.position, monsterPoint.position);
            float voice = level >= 1f ? pushSpeed * Mathf.Min(level, 2f) : 0f;
            float resist = monsterPush * (0.6f + t);    // le monstre résiste davantage quand la boule approche
            t = Mathf.Clamp01(t + (voice - resist) * Time.deltaTime / dist);
            Place(Mathf.Clamp01(level));

            monster.SetPush(1f - Mathf.Clamp01(level));
            Audio.SetLoopIntensity(Mathf.Clamp01(level) * 0.8f);
            if (t > best + 0.08f) { best = t; ReportProgress(); }
            else if (level >= 1f && Time.time - lastReport > 1f) { lastReport = Time.time; ReportProgress(); }

            if (t >= 1f) Complete();
        }

        void Place(float energy)
        {
            Vector3 a = startPoint.position, b = monsterPoint.position;
            ball.position = Vector3.Lerp(a, b, t) + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.08f;
            ball.Rotate(Vector3.right, 200f * energy * Time.deltaTime, Space.World);
            float s = 0.9f + energy * 0.25f;
            ball.localScale = Vector3.one * s;
            SetGlow(ballRenderer, new Color(0.35f, 0.75f, 1f), 1.2f + energy * 4f);
            if (ballLight != null) ballLight.intensity = 2f + energy * 5f;
        }

        protected override void OnComplete()
        {
            if (impactFx != null) { impactFx.transform.position = ball.position; impactFx.Play(); }
            ball.gameObject.SetActive(false);
            monster.Die();
        }
    }
}
