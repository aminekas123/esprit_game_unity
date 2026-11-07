using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LostTemple
{
    /// <summary>
    /// Salle 2d — Boule chasseuse : une boule rouge pilotée par un NavMeshAgent part derrière la luciole
    /// (avec quelques secondes de retard) et la poursuit dans le labyrinthe. C'est une course : il faut
    /// amener la luciole à l'anneau doré avant d'être rattrapé. Si elle la touche, une vie est perdue.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class FireflyHunter : MonoBehaviour
    {
        public FireflyMazePuzzle puzzle;
        [Tooltip("NavMeshSurface du labyrinthe : le NavMesh est calculé au lancement de la salle.")]
        public NavMeshSurface maze;
        public Transform hunterStart;
        public float speed = 1f;
        [Tooltip("Avance (s) laissée à la luciole avant que la chasseuse parte.")]
        public float headStart = 2.5f;
        public float catchDistance = 0.4f;

        NavMeshAgent agent;
        float goAt = float.MaxValue;
        bool chasing;
        float nextRepath;
        HudGauge gauge;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            agent.enabled = false;
        }

        void Start()
        {
            // Type d'agent « petite boule » (le type Humanoid par défaut, rayon 0,5 m, ne passe pas entre les murets)
            var settings = NavMesh.CreateSettings();
            settings.agentRadius = 0.18f;
            settings.agentHeight = 0.4f;
            settings.agentClimb = 0.1f;   // les murets (0,35 m) restent des obstacles
            settings.agentSlope = 20f;
            maze.agentTypeID = settings.agentTypeID;
            maze.overrideVoxelSize = true;
            maze.voxelSize = 0.05f;
            maze.minRegionArea = 0.05f;
            maze.BuildNavMesh();
            agent.agentTypeID = settings.agentTypeID;
            agent.radius = 0.18f;
            agent.height = 0.4f;
            agent.baseOffset = 0.2f;
            agent.acceleration = 20f;
            agent.angularSpeed = 720f;
            agent.autoBraking = false;

            gauge = new HudGauge
            {
                label = "Boule rouge : écart avec la luciole",
                color = new Color(0.95f, 0.3f, 0.2f),
                value = () => 1f - Mathf.Clamp01(Gap() / 4f),
                visible = () => chasing,
            };
            HudGauge.Register(gauge);
            Hide();
        }

        void OnDestroy() { if (gauge != null) HudGauge.Unregister(gauge); }

        float Gap()
        {
            Vector3 d = transform.position - puzzle.orb.position;
            d.y = 0f;
            return d.magnitude;
        }

        /// <summary>Replace la chasseuse à son départ ; elle partira « delay » secondes plus tard.</summary>
        public void ResetHunter(float delay)
        {
            gameObject.SetActive(true);
            agent.enabled = true;
            agent.Warp(hunterStart.position);
            agent.isStopped = true;
            chasing = false;
            goAt = Time.time + delay + headStart;
        }

        /// <summary>Arrête la chasseuse (épreuve finie, ou message à l'écran).</summary>
        public void Stop()
        {
            chasing = false;
            goAt = float.MaxValue;
            if (agent.enabled) agent.isStopped = true;
        }

        public void Hide()
        {
            Stop();
            gameObject.SetActive(false);
        }

        void Update()
        {
            if (!puzzle.IsActive) return;
            if (!chasing)
            {
                if (Time.time < goAt) return;
                chasing = true;
                agent.speed = speed * GameManager.Instance.HazardSpeed;
                agent.isStopped = false;
                Audio.Play(Sfx.Tick);
                UIManager.Instance.ShowToast("La boule rouge vous poursuit ! Foncez vers l'anneau doré !");
            }

            if (Time.time >= nextRepath)
            {
                nextRepath = Time.time + 0.2f;
                agent.SetDestination(puzzle.orb.position);
            }

            if (Gap() < catchDistance)
            {
                Stop();
                puzzle.Caught();
            }
        }

        static AudioManager Audio => AudioManager.Instance;
    }
}
