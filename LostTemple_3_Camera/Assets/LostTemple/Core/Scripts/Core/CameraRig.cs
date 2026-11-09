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
        [Tooltip("La caméra se place derrière l'explorateur et tourne avec lui.")]
        public bool followHeading = true;
        [Tooltip("Délai de rotation de la caméra (plus grand = plus doux).")]
        public float yawSmoothTime = 0.45f;
        [Tooltip("Vitesse de rotation maximale de la caméra (degrés par seconde).")]
        public float maxYawSpeed = 140f;

        float yaw, yawVel;
        bool yawInit;

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
                yawInit = false;   // à la fin de l'énigme, la caméra revient derrière l'explorateur
                pos = step.cameraPose.position;
                rot = step.cameraPose.rotation;
                return true;
            }
            if (target != null)
            {
                UpdateYaw();
                var yr = Quaternion.Euler(0f, followHeading ? yaw : 0f, 0f);
                Vector3 focus = target.position + Vector3.up + yr * Vector3.forward * lookAhead;
                pos = focus + yr * offset;
                rot = Quaternion.LookRotation(focus - pos);
                return true;
            }
            pos = transform.position; rot = transform.rotation;
            return false;
        }

/// <summary>Oriente la caméra derrière l'explorateur, en douceur, seulement quand il avance.</summary>
        void UpdateYaw()
        {
            float want = target.eulerAngles.y;
            if (!yawInit) { yaw = want; yawVel = 0f; yawInit = true; return; }
            var pc = PlayerController.Instance;
            bool moving = pc == null || pc.Speed01 > 0.15f;
            if (moving) yaw = Mathf.SmoothDampAngle(yaw, want, ref yawVel, yawSmoothTime, maxYawSpeed);
            else yawVel = 0f;
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
