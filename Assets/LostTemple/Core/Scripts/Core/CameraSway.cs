using UnityEngine;

namespace LostTemple
{
    /// <summary>Léger mouvement de caméra pour donner vie aux décors fixes.</summary>
    public class CameraSway : MonoBehaviour
    {
        public float amplitude = 0.15f;
        public float speed = 0.3f;
        Vector3 basePos;

        void Start() { basePos = transform.position; }

        void Update()
        {
            float t = Time.time * speed;
            transform.position = basePos + new Vector3(Mathf.Sin(t) * amplitude, Mathf.Sin(t * 1.3f) * amplitude * 0.5f, 0f);
        }
    }
}
