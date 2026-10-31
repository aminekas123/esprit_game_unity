using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Caméra des salles : suit l'explorateur pendant l'exploration, puis glisse vers un point de vue
    /// dédié (PuzzleStep.cameraPose) pendant une énigme. Gère aussi les secousses.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 8.5f, -6.5f);
        public float lookAhead = 1.5f;
        public float smoothing = 4f;

        static float shake;

        public static void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        void Start() { Snap(); }

        void Snap()
        {
            if (!Desired(out var p, out var r)) return;
            transform.SetPositionAndRotation(p, r);
        }

        bool Desired(out Vector3 pos, out Quaternion rot)
        {
            var step = RoomManager.Current != null ? RoomManager.Current.ActiveStep : null;
            if (step != null && step.IsActive && step.cameraPose != null)
            {
                pos = step.cameraPose.position;
                rot = step.cameraPose.rotation;
                return true;
            }
            if (target != null)
            {
                Vector3 focus = target.position + Vector3.up + Vector3.forward * lookAhead;
                pos = focus + offset;
                rot = Quaternion.LookRotation(focus - pos);
                return true;
            }
            pos = transform.position; rot = transform.rotation;
            return false;
        }

        void LateUpdate()
        {
            if (!Desired(out var p, out var r)) return;
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, p, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, r, k);
            if (shake > 0f)
            {
                transform.position += Random.insideUnitSphere * shake;
                shake = Mathf.Max(0f, shake - Time.deltaTime * 0.8f);
            }
        }
    }
}
