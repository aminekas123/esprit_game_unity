using System.Collections;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace LostTemple
{
    /// <summary>
    /// Salle 1 — Boule rivale pilotée par un NavMeshAgent. Elle part de l'arrivée et suit le pont jusqu'au
    /// point de départ du joueur, en sens inverse. Le joueur doit la percuter avec sa boule (inclinaison)
    /// pour la faire tomber ; si elle atteint son départ, l'épreuve recommence.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class RivalBall : MonoBehaviour
    {
        public TiltBallPuzzle puzzle;
        [Tooltip("NavMeshSurface posée sur le parcours : le NavMesh est calculé au début de l'épreuve.")]
        public NavMeshSurface course;
        public Transform rivalStart;
        public Transform playerStart;
        public float speed = 1.1f;
        [Tooltip("Vitesse d'impact minimale (m/s) pour faire tomber la boule rivale.")]
        public float hitSpeed = 1.6f;
        public float respawnDelay = 3.5f;
        public float killY = -3f;

        NavMeshAgent agent;
        Rigidbody rb;
        bool running, knocked;
        float totalDistance = 1f;
        float lastSoftHit;
        HudGauge gauge;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            rb = GetComponent<Rigidbody>();
            agent.enabled = false;
        }

        void Start()
        {
            // Type d'agent « petite boule » : le type Humanoid par défaut (rayon 0,5 m) est trop large pour ce pont étroit.
            var settings = NavMesh.CreateSettings();
            settings.agentRadius = 0.2f;
            settings.agentHeight = 0.5f;
            settings.agentClimb = 0.3f;
            settings.agentSlope = 30f;
            course.agentTypeID = settings.agentTypeID;
            course.overrideVoxelSize = true;
            course.voxelSize = 0.05f;
            course.minRegionArea = 0.05f;   // par défaut 2 m² : les ponts étroits seraient supprimés du NavMesh
            course.BuildNavMesh();
            agent.agentTypeID = settings.agentTypeID;
            agent.radius = 0.2f;
            agent.height = 0.5f;
            agent.baseOffset = 0.25f;

            gauge = new HudGauge
            {
                label = "Boule rouge → votre départ",
                color = new Color(0.95f, 0.3f, 0.2f),
                value = () => agent.enabled && agent.hasPath ? 1f - Mathf.Clamp01(agent.remainingDistance / totalDistance) : 0f,
                visible = () => running && !knocked,
            };
            HudGauge.Register(gauge);
        }

        void OnDestroy() { if (gauge != null) HudGauge.Unregister(gauge); }

        void Update()
        {
            if (puzzle == null) return;
            if (puzzle.IsActive && !running) { running = true; ResetRival(); }
            if (!puzzle.IsActive && running) { running = false; if (agent.enabled) agent.isStopped = true; if (puzzle.IsDone) StartCoroutine(Vanish()); }
            if (!running) return;

            if (!knocked)
            {
                Vector3 d = transform.position - playerStart.position;
                d.y = 0f;
                if (d.magnitude < 0.6f)
                {
                    puzzle.Fail("La boule rouge a atteint votre départ ! Percutez-la pour l'arrêter.");
                    ResetRival();
                }
            }
            else if (transform.position.y < killY)
            {
                knocked = false;
                StartCoroutine(Respawn());
            }
        }

        /// <summary>Replace la boule rivale sur l'arrivée et l'envoie vers le départ du joueur.</summary>
        public void ResetRival()
        {
            StopAllCoroutines();
            knocked = false;
            rb.isKinematic = true;
            gameObject.SetActive(true);
            agent.enabled = true;
            agent.Warp(rivalStart.position);
            agent.speed = speed * GameManager.Instance.HazardSpeed;
            agent.isStopped = false;
            agent.SetDestination(playerStart.position);
            totalDistance = Mathf.Max(1f, Vector3.Distance(rivalStart.position, playerStart.position) * 2.5f);
        }

        void OnCollisionEnter(Collision c)
        {
            if (!running || knocked || c.rigidbody != puzzle.ball) return;
            if (c.relativeVelocity.magnitude < hitSpeed)
            {
                if (Time.time - lastSoftHit > 2f) { lastSoftHit = Time.time; UIManager.Instance.ShowToast("Trop doucement ! Prenez de l'élan pour la percuter."); }
                return;
            }
            Knock(transform.position - puzzle.ball.position);
        }

        /// <summary>Percutée : la boule quitte le NavMesh et devient un objet physique qui tombe dans le vide.</summary>
        void Knock(Vector3 direction)
        {
            knocked = true;
            agent.enabled = false;
            rb.isKinematic = false;
            direction.y = 0f;
            rb.AddForce(direction.normalized * 5f + Vector3.up * 1.5f, ForceMode.Impulse);
            AudioManager.Instance.Play(Sfx.Mechanism, 0.7f);
            CameraRig.Shake(0.2f);
            UIManager.Instance.ShowToast("Boule rouge percutée ! Elle reviendra dans quelques secondes…");
            puzzle.NotifyHit();
        }

        IEnumerator Respawn()
        {
            rb.isKinematic = true;
            transform.position = rivalStart.position + Vector3.down * 20f;   // cachée sous la salle
            yield return new WaitForSeconds(respawnDelay);
            if (running) ResetRival();
        }

        IEnumerator Vanish()
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.6f)
            {
                transform.localScale = Vector3.one * 0.5f * (1f - t);
                yield return null;
            }
            gameObject.SetActive(false);
        }
    }
}
