using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Zone où l'explorateur doit se rendre pour lancer une énigme. Un faisceau doré la signale
    /// tant que c'est la prochaine mission.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class Station : MonoBehaviour
    {
        public PuzzleStep step;
        public GameObject marker;

        void Start()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        void Update()
        {
            if (marker != null && marker.activeSelf)
                marker.transform.Rotate(0f, 60f * Time.deltaTime, 0f);
        }

        public void SetMarker(bool on) { if (marker != null) marker.SetActive(on); }

        void OnTriggerEnter(Collider other) { Check(other); }
        void OnTriggerStay(Collider other) { Check(other); }

        void Check(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            RoomManager.Current?.OnStationEntered(this);
        }
    }
}
