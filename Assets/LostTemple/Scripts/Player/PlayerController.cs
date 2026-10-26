using UnityEngine;
using UnityEngine.InputSystem;

namespace LostTemple
{
    /// <summary>
    /// L'explorateur : déplacement au joystick tactile (ou ZQSD/flèches dans l'éditeur), relatif à la caméra.
    /// Bloqué pendant les énigmes. Un piège ou une chute le renvoie au dernier point de passage.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public float speed = 4.2f;
        public float turnSpeed = 720f;
        public float gravity = -25f;
        public float killY = -3f;

        /// <summary>Entrée du joystick virtuel (écrite par l'UI).</summary>
        public Vector2 StickInput { get; set; }
        /// <summary>Vrai pendant une énigme : le joueur ne bouge plus.</summary>
        public bool Frozen { get; set; }
        /// <summary>Vitesse normalisée 0..1, lue par l'animation.</summary>
        public float Speed01 { get; private set; }
        public bool IsHurt => hurtTimer > 0f;
        /// <summary>Invisible aux gardiens tant que le capteur de proximité est couvert (énergie limitée).</summary>
        public bool IsCloaked { get; private set; }
        public float CloakEnergy { get; private set; } = 1f;

        CharacterController cc;
        float vy;
        Vector3 checkpoint;
        Quaternion checkpointRot;
        float hurtTimer;

        void Awake()
        {
            Instance = this;
            cc = GetComponent<CharacterController>();
            checkpoint = transform.position;
            checkpointRot = transform.rotation;
        }

        void OnDestroy() { if (Instance == this) Instance = null; }

        void Update()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            hurtTimer -= dt;

            Vector2 input = Frozen ? Vector2.zero : Vector2.ClampMagnitude(StickInput + KeyboardInput(), 1f);

            Vector3 fwd = Vector3.forward, right = Vector3.right;
            var cam = Camera.main;
            if (cam != null)
            {
                fwd = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
                right = Vector3.Cross(Vector3.up, fwd);
            }
            Vector3 move = (right * input.x + fwd * input.y) * speed;
            if (move.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(move), turnSpeed * dt);

            if (cc.isGrounded && vy < 0f) vy = -2f;
            vy += gravity * dt;
            cc.Move((move + Vector3.up * vy) * dt);
            Speed01 = Mathf.Lerp(Speed01, move.magnitude / speed, 1f - Mathf.Exp(-12f * dt));

            if (transform.position.y < killY) Hurt("Vous êtes tombé dans le vide !");

            // Invisibilité : seulement dans les salles gardées, 3 s maximum, se recharge en 4 s
            bool want = Enemy.Count > 0 && !Frozen && SensorManager.Instance.IsCovered();
            if (want && CloakEnergy > 0f) CloakEnergy = Mathf.Max(0f, CloakEnergy - dt / 3f);
            else if (!want) CloakEnergy = Mathf.Min(1f, CloakEnergy + dt / 4f);
            bool cloaked = want && CloakEnergy > 0f;
            if (cloaked && !IsCloaked) AudioManager.Instance.Play(Sfx.Tick, 0.6f);
            IsCloaked = cloaked;
        }

        static Vector2 KeyboardInput()
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed || k.rightArrowKey.isPressed ? 1f : 0f) - (k.aKey.isPressed || k.leftArrowKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed || k.upArrowKey.isPressed ? 1f : 0f) - (k.sKey.isPressed || k.downArrowKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }

        public void SetCheckpoint(Vector3 position)
        {
            checkpoint = position;
            checkpointRot = transform.rotation;
        }

        /// <summary>Touché par un piège : flash rouge, vibration, retour au point de passage.</summary>
        public void Hurt(string message)
        {
            if (hurtTimer > 0f || Frozen) return;
            hurtTimer = 1.2f;
            var gm = GameManager.Instance;
            if (gm.DemoMode)
            {
                // En démo, pas de vie perdue : simple retour au point de passage.
                UIManager.Instance.FlashDamage();
                UIManager.Instance.ShowToast(message);
                Teleport(checkpoint, checkpointRot);
                return;
            }
            gm.AddFall();
            AudioManager.Instance.Play(Sfx.Fail);
            UIManager.Instance.FlashDamage();
            CameraRig.Shake(0.25f);
            if (!gm.LoseLife())
            {
                // Plus de vies : la salle recommence avec les vies de départ.
                Frozen = true;
                UIManager.Instance.ShowMessage("Plus de vies !", message + "\n\nVous n'avez plus de reliques. La salle recommence avec " + gm.StartLivesNow + " vie(s).",
                    "Recommencer", () => { gm.RefillLives(); gm.RestartRoom(); });
                return;
            }
            UIManager.Instance.ShowToast(message + $"  (−1 vie, reste {gm.Lives})");
            Teleport(checkpoint, checkpointRot);
        }

        /// <summary>Déplacement imposé (boss : esquive au téléphone) même quand le joueur est figé.</summary>
        public void ForceMove(Vector3 delta)
        {
            if (cc.enabled) cc.Move(delta);
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            cc.enabled = true;
            vy = 0f;
        }
    }
}
