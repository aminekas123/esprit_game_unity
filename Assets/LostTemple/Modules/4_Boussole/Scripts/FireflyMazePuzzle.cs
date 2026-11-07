using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 2d — Boussole : une luciole avance toute seule dans un labyrinthe au sol. Sa direction suit
    /// le cap du téléphone (relatif au cap de départ) : on la dirige en tournant sur soi-même.
    /// Si elle touche un mur, elle revient au départ.
    /// Course : une boule rouge (FireflyHunter, NavMeshAgent) la poursuit ; si elle la rattrape, une vie est perdue.
    /// </summary>
    public class FireflyMazePuzzle : PuzzleStep
    {
        public Transform orb;
        public Transform arrow;
        public Transform start;
        public Transform goal;
        public Collider[] walls;
        public float speed = 0.9f;
        public float radius = 0.2f;
        public float goalRadius = 0.45f;
        public FireflyHunter hunter;

        float startHeading;
        float pauseUntil;
        float bestDist;
        readonly Collider[] hits = new Collider[8];

        void Reset() { sensor = SensorKind.Compass; }

        public override float Progress
        {
            get
            {
                if (!IsActive) return -1f;
                float total = Vector3.Distance(start.position, goal.position);
                return 1f - Mathf.Clamp01(Vector3.Distance(orb.position, goal.position) / total);
            }
        }

        protected override void OnBegin()
        {
            startHeading = Sensors.GetHeading();
            Respawn();
        }

        void Respawn()
        {
            orb.position = start.position;
            pauseUntil = Time.time + 1.2f;   // laisse le temps de s'orienter avant de repartir
            bestDist = Vector3.Distance(start.position, goal.position);
            if (hunter != null) hunter.ResetHunter(1.2f);
        }

        void Update()
        {
            if (!IsActive) return;
            float yaw = Mathf.DeltaAngle(startHeading, Sensors.GetHeading());
            Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
            if (arrow != null) arrow.rotation = Quaternion.LookRotation(dir);

            if (Time.time < pauseUntil) return;
            Vector3 next = orb.position + dir * speed * Time.deltaTime;
            int n = Physics.OverlapSphereNonAlloc(next, radius, hits, ~0, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
                if (System.Array.IndexOf(walls, hits[i]) >= 0) { StartCoroutine(Bump()); return; }
            orb.position = next;

            float d = Vector3.Distance(orb.position, goal.position);
            if (d < bestDist - 0.4f) { bestDist = d; ReportProgress(); }
            if (d < goalRadius) { if (hunter != null) hunter.Hide(); Complete(); }
        }

        /// <summary>La boule rouge a rattrapé la luciole : une vie en moins, puis la course recommence.</summary>
        public void Caught()
        {
            pauseUntil = float.MaxValue;
            Audio.Play(Sfx.Fail);
            CameraRig.Shake(0.3f);
            UIManager.Instance.FlashDamage();

            var gm = GameManager.Instance;
            gm.AddFall();
            if (!gm.DemoMode && !gm.LoseLife())
            {
                UIManager.Instance.ShowMessage("RATTRAPÉE", "La boule rouge a attrapé la luciole.\n\nVous n'avez plus de vies : la salle recommence.", "Recommencer",
                    () => { gm.RefillLives(); gm.RestartRoom(); });
                return;
            }
            string lives = gm.DemoMode ? "" : $"\n\n−1 vie (il vous en reste {gm.Lives}).";
            UIManager.Instance.ShowMessage("RATTRAPÉE !", $"La boule rouge a attrapé la luciole !{lives}\n\nTournez plus vite pour la distancer.", "Réessayer", Respawn);
        }

        IEnumerator Bump()
        {
            pauseUntil = float.MaxValue;
            if (hunter != null) hunter.Stop();
            Audio.Play(Sfx.Fail);
            CameraRig.Shake(0.1f);
            UIManager.Instance.ShowToast("La luciole s'est cognée ! Tournez plus tôt pour changer de direction.");
            yield return new WaitForSeconds(0.5f);
            Respawn();
        }
    }
}
