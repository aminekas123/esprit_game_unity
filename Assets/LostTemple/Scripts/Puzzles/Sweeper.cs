using UnityEngine;

namespace LostTemple
{
    /// <summary>Piège de la salle 1 : une barre de pierre qui tourne au-dessus du pont et pousse la boule.</summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Sweeper : MonoBehaviour
    {
        public float speed = 50f;
        Rigidbody rb;

        void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        void FixedUpdate()
        {
            float k = GameManager.Instance != null ? GameManager.Instance.HazardSpeed : 1f;
            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, speed * k * Time.fixedDeltaTime, 0f));
        }
    }
}
