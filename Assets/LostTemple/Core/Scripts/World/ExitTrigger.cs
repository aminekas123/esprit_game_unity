using UnityEngine;

namespace LostTemple
{
    /// <summary>Sortie de la salle : active une fois toutes les énigmes résolues.</summary>
    [RequireComponent(typeof(Collider))]
    public class ExitTrigger : MonoBehaviour
    {
        void Start() { GetComponent<Collider>().isTrigger = true; }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            RoomManager.Current?.OnExitReached();
        }
    }
}
