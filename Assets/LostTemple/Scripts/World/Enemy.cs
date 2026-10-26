using System.Collections.Generic;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Gardien momifié qui patrouille entre des points. S'il voit l'explorateur dans son cône de vision
    /// (lumière rouge), il le renvoie au point de passage. Couvrir le capteur de proximité rend invisible.
    /// </summary>
    public class Enemy : MonoBehaviour
    {
        static readonly List<Enemy> all = new List<Enemy>();
        public static int Count => all.Count;

        public Transform[] waypoints;
        public float speed = 1.6f;
        public float viewDistance = 4.5f;
        public float viewAngle = 70f;
        public Transform legL, legR, armL, armR;
        public Light visionLight;

        int target;
        float phase;
        bool alerted;

        void OnEnable() { all.Add(this); }
        void OnDisable() { all.Remove(this); }

        void Update()
        {
            if (waypoints == null || waypoints.Length == 0) return;
            float spd = speed * (GameManager.Instance != null ? GameManager.Instance.HazardSpeed : 1f);
            Vector3 to = waypoints[target].position - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.2f) { target = (target + 1) % waypoints.Length; return; }
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(to), 200f * Time.deltaTime);
            transform.position += transform.forward * spd * Time.deltaTime;

            // Marche raide de momie
            phase += Time.deltaTime * spd * 4f;
            float sw = Mathf.Sin(phase) * 25f;
            if (legL != null) legL.localRotation = Quaternion.Euler(sw, 0, 0);
            if (legR != null) legR.localRotation = Quaternion.Euler(-sw, 0, 0);
            if (armL != null) armL.localRotation = Quaternion.Euler(-80f + sw * 0.2f, 0, 0);
            if (armR != null) armR.localRotation = Quaternion.Euler(-80f - sw * 0.2f, 0, 0);

            Detect();
        }

        void Detect()
        {
            var p = PlayerController.Instance;
            if (p == null || p.Frozen) return;
            Vector3 d = p.transform.position - transform.position;
            d.y = 0f;
            bool close = d.magnitude < 0.9f;
            bool seen = d.magnitude < viewDistance && Vector3.Angle(transform.forward, d) < viewAngle * 0.5f;
            bool visible = !p.IsCloaked;
            if (visionLight != null) visionLight.color = Color.Lerp(visionLight.color, seen && visible ? new Color(1f, 0.1f, 0.05f) : new Color(1f, 0.45f, 0.2f), 0.2f);
            if ((seen || close) && visible)
            {
                if (!alerted) AudioManager.Instance.Play(Sfx.Roar, 0.35f);
                alerted = true;
                p.Hurt("Un gardien vous a repéré ! Couvrez le capteur de proximité pour devenir invisible.");
            }
            else alerted = false;
        }
    }
}
