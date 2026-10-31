using UnityEngine;

namespace LostTemple
{
    /// <summary>Fait tourner et flotter un objet (artefact, clé…).</summary>
    public class Spinner : MonoBehaviour
    {
        public float speed = 45f;
        public float bob = 0.1f;
        Vector3 basePos;

        void Start() { basePos = transform.localPosition; }

        void Update()
        {
            transform.Rotate(0f, speed * Time.deltaTime, 0f, Space.World);
            transform.localPosition = basePos + Vector3.up * Mathf.Sin(Time.time * 1.7f) * bob;
        }
    }
}
