using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Caméra des salles : suit l'explorateur pendant l'exploration, puis glisse vers un point de vue
    /// dédié (PuzzleStep.cameraPose) pendant une énigme. Gère aussi les secousses.
    /// L'angle de vue s'adapte à la forme de l'écran : sur un téléphone en portrait (écran étroit),
    /// la caméra s'élargit pour garder la même largeur visible qu'en paysage.
    /// Option (followHeading) : la caméra se place derrière l'explorateur et tourne avec lui.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target;
        public Vector3 offset = new Vector3(0f, 8.5f, -6.5f);
        public float lookAhead = 1.5f;
        public float smoothing = 4f;
        [Tooltip("Largeur de vue (angle horizontal, en degrés) garantie pendant une énigme.")]
        public float puzzleHorizontalFov = 66f;
        [Tooltip("Largeur de vue (angle horizontal, en degrés) garantie pendant l'exploration.")]
        public float followHorizontalFov = 44f;
        [Tooltip("La caméra se place derrière l'explorateur et tourne avec lui (sinon : vue fixe vers le nord).")]
        public bool followHeading;
        [Tooltip("Délai de rotation de la caméra (plus grand = plus doux).")]
        public float yawSmoothTime = 0.45f;
        [Tooltip("Vitesse de rotation maximale de la caméra (degrés par seconde).")]
        public float maxYawSpeed = 140f;

        float yaw, yawVel;
        bool yawInit;

        static float shake;
        Camera cam;
        float baseFov = 60f;

        public static void Shake(float amount) { shake = Mathf.Max(shake, amount); }

        void Start()
        {
            cam = GetComponent<Camera>();
            if (cam != null) baseFov = cam.fieldOfView;
            Snap();
        }

        void Snap()
        {
            if (!Desired(out var p, out var r, out bool posing)) return;
            transform.SetPositionAndRotation(p, r);
            if (cam != null) cam.fieldOfView = TargetFov(posing);
        }

        bool Desired(out Vector3 pos, out Quaternion rot, out bool posing)
        {
            posing = false;
            var step = RoomManager.Current != null ? RoomManager.Current.ActiveStep : null;
            if (step != null && step.IsActive && step.cameraPose != null)
            {
                yawInit = false;   // à la fin de l'énigme, la caméra revient derrière l'explorateur
                pos = step.cameraPose.position;
                rot = step.cameraPose.rotation;
                posing = true;
                return true;
            }
            if (target != null)
            {
                Quaternion yr = Quaternion.identity;
                if (followHeading) { UpdateYaw(); yr = Quaternion.Euler(0f, yaw, 0f); }
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

        /// <summary>
        /// Angle de vue vertical (celui qu'utilise Unity) qui garantit l'angle horizontal voulu
        /// selon le rapport largeur/hauteur de l'écran. Jamais plus serré que l'angle d'origine (écrans larges).
        /// </summary>
        float TargetFov(bool posing)
        {
            float h = (posing ? puzzleHorizontalFov : followHorizontalFov) * Mathf.Deg2Rad;
            float aspect = cam != null && cam.aspect > 0f ? cam.aspect : 0.5f;
            float v = 2f * Mathf.Atan(Mathf.Tan(h * 0.5f) / aspect) * Mathf.Rad2Deg;
            return Mathf.Clamp(Mathf.Max(baseFov, v), 30f, 105f);
        }

        void LateUpdate()
        {
            if (!Desired(out var p, out var r, out bool posing)) return;
            float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, p, k);
            transform.rotation = Quaternion.Slerp(transform.rotation, r, k);
            if (cam != null) cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, TargetFov(posing), k);
            if (shake > 0f)
            {
                transform.position += Random.insideUnitSphere * shake;
                shake = Mathf.Max(0f, shake - Time.deltaTime * 0.8f);
            }
        }
    }
}
