using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Animation procédurale de l'explorateur (pas besoin de squelette ni de clips) :
    /// balancement des jambes et des bras à la marche, respiration à l'arrêt, clignotement quand il est touché.
    /// </summary>
    public class CharacterAnimator : MonoBehaviour
    {
        public PlayerController player;
        public Transform body;
        public Transform legL, legR, armL, armR;
        public float stride = 38f;

        float phase;
        Vector3 bodyBase;
        Renderer[] renderers;

        void Start()
        {
            if (body != null) bodyBase = body.localPosition;
            renderers = GetComponentsInChildren<Renderer>();
        }

        void Update()
        {
            float s = player != null ? player.Speed01 : 0f;
            phase += Time.deltaTime * (2f + 9f * s);
            float swing = Mathf.Sin(phase) * stride * s;

            if (legL != null) legL.localRotation = Quaternion.Euler(swing, 0, 0);
            if (legR != null) legR.localRotation = Quaternion.Euler(-swing, 0, 0);
            if (armL != null) armL.localRotation = Quaternion.Euler(-swing * 0.8f, 0, 6f);
            if (armR != null) armR.localRotation = Quaternion.Euler(swing * 0.8f, 0, -6f);

            if (body != null)
            {
                float bob = Mathf.Abs(Mathf.Sin(phase)) * 0.06f * s + Mathf.Sin(Time.time * 2f) * 0.01f * (1f - s);
                body.localPosition = bodyBase + Vector3.up * bob;
            }

            bool visible = player == null || !player.IsHurt || Mathf.Repeat(Time.time * 10f, 1f) > 0.4f;
            foreach (var r in renderers) if (!(r is ParticleSystemRenderer) && r.enabled != visible) r.enabled = visible;
        }
    }
}
