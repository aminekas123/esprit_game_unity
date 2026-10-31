using System.Collections.Generic;
using UnityEngine;

namespace LostTemple
{
    public enum HazardMode { Always, Periodic }

    /// <summary>
    /// Piège qui renvoie l'explorateur au dernier point de passage.
    /// Mode Periodic : pics qui sortent du sol, jets de feu… avec une plaque qui rougit juste avant.
    /// Mode Always : barre tournante, lame…
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Hazard : MonoBehaviour
    {
        public HazardMode mode = HazardMode.Periodic;
        public float period = 3f;
        public float activeTime = 1.2f;
        public float offset;
        [Tooltip("Partie qui sort (pics) : position active = position d'origine.")]
        public Transform movingPart;
        public Vector3 hiddenOffset = new Vector3(0f, -0.6f, 0f);
        public ParticleSystem fx;
        public Light fxLight;
        public Renderer warnPlate;
        public string message = "Aïe ! Un piège…";

        Vector3 basePos;
        float raise;
        bool active;
        Material warnMat;

        public bool IsActive => mode == HazardMode.Always || active;

        static readonly List<Hazard> all = new List<Hazard>();
        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        /// <summary>Distance au piège actif le plus proche (sert à faire monter la musique).</summary>
        public static float NearestActiveDistance(Vector3 p)
        {
            float best = float.MaxValue;
            foreach (var h in all)
                if (h.IsActive) best = Mathf.Min(best, Vector3.Distance(h.transform.position, p));
            return best;
        }

        void Start()
        {
            if (movingPart != null) basePos = movingPart.localPosition;
            if (warnPlate != null) { warnMat = warnPlate.material; warnMat.EnableKeyword("_EMISSION"); }
            GetComponent<Collider>().isTrigger = true;
        }

        void Update()
        {
            if (mode == HazardMode.Always) return;
            float t = Mathf.Repeat(Time.time * GameManager.Instance.HazardSpeed + offset, period);
            active = t < activeTime;
            float warn = !active && t > period - 0.8f ? Mathf.InverseLerp(period - 0.8f, period, t) : 0f;

            raise = Mathf.MoveTowards(raise, active ? 1f : 0f, Time.deltaTime * (active ? 12f : 3f));
            if (movingPart != null) movingPart.localPosition = basePos + hiddenOffset * (1f - raise);

            if (fx != null)
            {
                var em = fx.emission;
                em.enabled = active;
            }
            if (fxLight != null) fxLight.intensity = active ? 3f + Random.value : 0f;
            if (warnMat != null) warnMat.SetColor("_EmissionColor", new Color(1f, 0.25f, 0.1f) * (warn * 2.5f + (active ? 1.5f : 0f)));
        }

        void OnTriggerStay(Collider other)
        {
            if (!IsActive) return;
            var p = other.GetComponentInParent<PlayerController>();
            if (p != null) p.Hurt(message);
        }
    }
}
