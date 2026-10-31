using UnityEngine;

namespace LostTemple
{
    public enum SecretGesture { FlipFaceDown, Shake, Shout, Darkness, CoverLong }

    /// <summary>
    /// Relique secrète : invisible tant que le joueur n'a pas fait le geste caché de la salle
    /// (retourner le téléphone, le secouer, crier, faire le noir, couvrir longtemps le capteur).
    /// Les pages du journal donnent des indices.
    /// </summary>
    public class SecretRelic : MonoBehaviour
    {
        public SecretGesture gesture;
        public GameObject relic;
        public ParticleSystem revealFx;

        bool revealed;
        float hold;

        void Start()
        {
            relic.SetActive(false);
            var sm = SensorManager.Instance;
            sm.Shaken += OnShake;
            sm.Flipped += OnFlip;
        }

        void OnDestroy()
        {
            var sm = SensorManager.Instance;
            if (sm == null) return;
            sm.Shaken -= OnShake;
            sm.Flipped -= OnFlip;
        }

        /// <summary>Seulement pendant l'exploration, pour ne pas révéler le secret par hasard pendant une énigme.</summary>
        static bool Exploring
        {
            get
            {
                var rm = RoomManager.Current;
                var p = PlayerController.Instance;
                return rm != null && rm.ActiveStep == null && p != null && !p.Frozen;
            }
        }

        void OnShake() { if (gesture == SecretGesture.Shake && Exploring) Reveal(); }
        void OnFlip() { if (gesture == SecretGesture.FlipFaceDown && Exploring) Reveal(); }

        void Update()
        {
            if (revealed || !Exploring) return;
            var sm = SensorManager.Instance;
            bool doing = false;
            switch (gesture)
            {
                case SecretGesture.Shout: doing = sm.MicRunning && sm.GetSoundLevel01() > 2.5f; break;
                case SecretGesture.Darkness: doing = (sm.GetLux() >= 0f && sm.GetLux() < 3f) || UnityEngine.InputSystem.Keyboard.current?.lKey.isPressed == true; break;
                case SecretGesture.CoverLong: doing = sm.IsCovered(); break;
            }
            hold = doing ? hold + Time.deltaTime : 0f;
            if (hold > (gesture == SecretGesture.Shout ? 0.6f : 3f)) Reveal();
        }

        void Reveal()
        {
            if (revealed) return;
            revealed = true;
            relic.SetActive(true);
            if (revealFx != null) revealFx.Play();
            GameManager.Instance.AddSecret();
            AudioManager.Instance.Play(Sfx.Victory, 0.6f);
            CameraRig.Shake(0.1f);
            UIManager.Instance.ShowRoomTitle("✦ Un secret s'est révélé ! ✦");
        }
    }
}
