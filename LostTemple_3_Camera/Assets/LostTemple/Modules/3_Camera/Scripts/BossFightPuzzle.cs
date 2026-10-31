using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Boss final (salle du Coyote) en trois phases, une par capteur :
    /// 1. Luminosité — l'éblouir en couvrant ou éclairant le capteur ;
    /// 2. Accéléromètre — esquiver ses coups en penchant le téléphone du côté opposé au bras levé ;
    /// 3. Microphone — l'achever en poussant la boule d'énergie avec la voix.
    /// </summary>
    public class BossFightPuzzle : PuzzleStep
    {
        public MonsterController boss;
        public Transform bossHidden, bossStand;
        public CoyoteSpirit coyote;
        public Transform arenaCenter;
        public Renderer slamMarkerLeft, slamMarkerRight;
        public Transform ball;
        public Renderer ballRenderer;
        public Light ballLight;
        public Transform ballStart, ballEnd;
        public ParticleSystem impactFx;

        int phase;
        float sub;

        public override float Progress => IsActive ? (phase + sub) / 3f : -1f;

        void Awake()
        {
            boss.gameObject.SetActive(false);
            ball.gameObject.SetActive(false);
        }

        protected override void OnBegin() { StartCoroutine(Fight()); }

        void SetPhase(int index, SensorKind kind, string objectiveText)
        {
            phase = index;
            sub = 0f;
            sensor = kind;
            objective = objectiveText;
            UIManager.Instance.SetStep(this);
            AudioManager.Instance.Tension = 1f;
        }

        IEnumerator Fight()
        {
            // L'explorateur se place au centre de l'arène
            var pc = PlayerController.Instance;
            if (pc != null) pc.Teleport(arenaCenter.position + Vector3.up * 0.05f, Quaternion.LookRotation(Vector3.forward));

            // Apparition : le gardien des ténèbres surgit du sol devant la porte
            boss.gameObject.SetActive(true);
            boss.transform.position = bossHidden.position;
            AudioManager.Instance.Tension = 1f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.8f)
            {
                boss.transform.position = Vector3.Lerp(bossHidden.position, bossStand.position, Mathf.SmoothStep(0, 1, t));
                CameraRig.Shake(0.15f);
                yield return null;
            }
            boss.Awaken();
            while (boss.State != MonsterState.Awake) yield return null;

            // ---- Phase 1 : lumière
            SetPhase(0, SensorKind.Light, "Phase 1/3 — Éblouissez le gardien (couvrez ou éclairez le capteur de lumière)");
            float baseline = Sensors.GetLux();
            float gauge = 0f;
            while (gauge < 1f)
            {
                float change = Sensors.GetLightChange01(baseline);
                gauge = Mathf.Clamp01(gauge + (change > 0.6f ? Time.deltaTime / 2f : -Time.deltaTime / 5f));
                sub = gauge;
                boss.SetStun(gauge * 0.6f);
                if (change > 0.6f) ReportProgress();
                yield return null;
            }
            boss.SetStun(1f);
            Audio.Play(Sfx.Success);
            UIManager.Instance.ShowToast("Il est ébloui ! Mais il se reprend…");
            yield return new WaitForSeconds(1.2f);
            boss.SetStun(0f);
            boss.RoarNow();
            yield return new WaitForSeconds(1f);

            // ---- Phase 2 : esquive à l'accéléromètre
            SetPhase(1, SensorKind.Accelerometer, "Phase 2/3 — Esquivez : penchez le téléphone du côté opposé au bras levé");
            SensorManager.Instance.CalibrateTilt();
            var p = PlayerController.Instance;
            int dodges = 0;
            float windup = GameManager.Instance.HardMode ? 0.9f : 1.3f;
            while (dodges < 3)
            {
                int side = Random.value < 0.5f ? -1 : 1;   // -1 = frappe à gauche (vu par le joueur)
                var marker = side < 0 ? slamMarkerLeft : slamMarkerRight;
                var slam = StartCoroutine(boss.Slam(side, windup));
                for (float t = 0; t < windup; t += Time.deltaTime)
                {
                    SetGlow(marker, new Color(1f, 0.15f, 0.05f), 1f + 3f * Mathf.PingPong(t * 6f, 1f));
                    Dodge(p);
                    yield return null;
                }
                yield return slam;
                SetGlow(marker, Color.black, 0f);
                float rel = p.transform.position.x - arenaCenter.position.x;
                bool hit = rel * side > -0.4f;
                if (hit)
                {
                    if (!HitPlayer()) yield break;
                }
                else
                {
                    dodges++;
                    sub = dodges / 3f;
                    ReportProgress();
                    Audio.Play(Sfx.Tick);
                    UIManager.Instance.ShowToast($"Esquivé ! ({dodges}/3)");
                }
                for (float t = 0; t < 0.6f; t += Time.deltaTime) { Dodge(p); yield return null; }
            }
            Audio.Play(Sfx.Success);

            // ---- Phase 3 : la voix
            SetPhase(2, SensorKind.Microphone, "Phase 3/3 — Achevez-le : poussez la boule d'énergie avec votre voix !");
            var sm = SensorManager.Instance;
            if (!sm.MicRunning && !sm.ForceAlternative && Microphone.devices.Length > 0)
            {
                bool done = false;
                sm.RequestMicrophone(_ => done = true);
                while (!done) yield return null;
                if (sm.MicRunning) yield return sm.CalibrateMicNoise();
            }
            AudioManager.Instance.Tension = 0.3f;   // musique plus discrète pour ne pas gêner le micro
            ball.gameObject.SetActive(true);
            float k = 0f;
            float dist = Vector3.Distance(ballStart.position, ballEnd.position);
            while (k < 1f)
            {
                float level = Sensors.GetSoundLevel01();
                float voice = level >= 1f ? 2.6f * Mathf.Min(level, 2f) : 0f;
                k = Mathf.Clamp01(k + (voice - 1f * (0.6f + k)) * Time.deltaTime / dist);
                sub = k;
                ball.position = Vector3.Lerp(ballStart.position, ballEnd.position, k) + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.08f;
                ball.Rotate(Vector3.right, 200f * Mathf.Clamp01(level) * Time.deltaTime, Space.World);
                SetGlow(ballRenderer, new Color(0.35f, 0.75f, 1f), 1.2f + Mathf.Clamp01(level) * 4f);
                if (ballLight != null) ballLight.intensity = 2f + Mathf.Clamp01(level) * 5f;
                boss.SetPush(1f - Mathf.Clamp01(level));
                if (level >= 1f) ReportProgress();
                yield return null;
            }
            if (impactFx != null) { impactFx.transform.position = ball.position; impactFx.Play(); }
            ball.gameObject.SetActive(false);
            boss.Die();
            AudioManager.Instance.Tension = 0f;
            yield return new WaitForSeconds(2.4f);
            Complete();
        }

        void Dodge(PlayerController p)
        {
            if (p == null) return;
            float tilt = Sensors.GetTilt().x;
            float targetX = arenaCenter.position.x + Mathf.Clamp(tilt * 3.5f, -2.6f, 2.6f);
            float dx = (targetX - p.transform.position.x) * Mathf.Min(1f, Time.deltaTime * 8f);
            p.ForceMove(new Vector3(dx, -0.1f, 0f));
        }

        /// <summary>Le coup a touché : une vie en moins. Renvoie false si la partie de la salle est perdue.</summary>
        bool HitPlayer()
        {
            var gm = GameManager.Instance;
            Audio.Play(Sfx.Fail);
            UIManager.Instance.FlashDamage();
            CameraRig.Shake(0.4f);
            if (gm.DemoMode) { UIManager.Instance.ShowToast("Touché ! Penchez-vous de l'autre côté."); return true; }
            gm.AddFall();
            if (!gm.LoseLife())
            {
                UIManager.Instance.ShowMessage("Plus de vies !", "Le gardien des ténèbres vous a eu.\n\nLa salle recommence.", "Recommencer",
                    () => { gm.RefillLives(); gm.RestartRoom(); });
                return false;
            }
            UIManager.Instance.ShowToast($"Touché ! (−1 vie, reste {gm.Lives})");
            return true;
        }
    }
}
