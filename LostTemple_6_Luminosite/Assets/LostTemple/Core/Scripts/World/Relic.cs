using UnityEngine;

namespace LostTemple
{
    /// <summary>Relique dorée à ramasser (mission secondaire). Tourne et flotte pour attirer l'œil.</summary>
    [RequireComponent(typeof(Collider))]
    public class Relic : MonoBehaviour
    {
        Vector3 basePos;
        float seed;

        void Start()
        {
            basePos = transform.position;
            seed = Random.value * 6f;
            GetComponent<Collider>().isTrigger = true;
        }

        void Update()
        {
            transform.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            transform.position = basePos + Vector3.up * Mathf.Sin(Time.time * 2f + seed) * 0.12f;
        }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            RoomManager.Current?.CollectRelic(transform.position);
            Destroy(gameObject);
        }
    }
}
