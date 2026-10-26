using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Zone de silence : tant que l'explorateur la traverse, le micro écoute. Trop de bruit
    /// (parler, souffler, taper) remplit la jauge et déclenche des fléchettes. Le micro devient une contrainte.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class QuietZone : MonoBehaviour
    {
        public static QuietZone Current { get; private set; }

        public ParticleSystem darts;
        public float fillTime = 0.8f;

        public float Noise01 { get; private set; }
        bool inside;
        bool micAsked;

        void Start() { GetComponent<Collider>().isTrigger = true; }

        void OnDisable() { if (Current == this) Current = null; }

        void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            inside = true;
            Current = this;
            var sm = SensorManager.Instance;
            if (!sm.MicRunning && !sm.ForceAlternative && Microphone.devices.Length > 0 && !micAsked)
            {
                micAsked = true;
                sm.RequestMicrophone(ok => { if (ok) StartCoroutine(sm.CalibrateMicNoise()); });
            }
            UIManager.Instance.ShowToast("Chut… Ici, le moindre bruit réveille les pièges. Avancez en silence.");
        }

        void OnTriggerExit(Collider other)
        {
            if (other.GetComponentInParent<PlayerController>() == null) return;
            inside = false;
            if (Current == this) Current = null;
            Noise01 = 0f;
        }

        void Update()
        {
            if (!inside) return;
            var p = PlayerController.Instance;
            if (p == null || p.Frozen) return;
            float level = SensorManager.Instance.GetSoundLevel01();
            Noise01 = Mathf.Clamp01(Noise01 + (level >= 1f ? Time.deltaTime / fillTime : -Time.deltaTime * 0.6f));
            if (Noise01 >= 1f)
            {
                Noise01 = 0f;
                if (darts != null) { darts.transform.position = p.transform.position + Vector3.up * 3f; darts.Play(); }
                p.Hurt("Trop de bruit ! Des fléchettes ont jailli des murs.");
            }
        }
    }
}
