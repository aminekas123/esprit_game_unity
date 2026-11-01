using UnityEngine;
using UnityEngine.InputSystem;

namespace LostTemple
{
    /// <summary>
    /// Salle 2c — Magnétomètre (détecteur de métaux) : le coffre ancien est scellé par un aimant.
    /// Approcher le téléphone d'un objet métallique réel (clé, ciseaux, ordinateur, aimant de frigo)
    /// fait monter le champ magnétique mesuré ; le maintenir assez fort charge la jauge et ouvre le coffre.
    /// Sans magnétomètre : bouton « Approcher l'aimant » (ou touche M dans l'éditeur).
    /// </summary>
    public class MagnetChestPuzzle : PuzzleStep
    {
        public Transform lid;
        public Renderer seal;
        public ParticleSystem sparks;
        [Tooltip("Champ à atteindre, en multiple du champ ambiant mesuré au début.")]
        public float ratioNeeded = 1.5f;
        public float fillTime = 2f;

        float baseline = -1f;
        float sampleTime;
        float sampleSum;
        int samples;
        float gauge;
        float ratio = 1f;
        HudGauge hud;

        void Reset() { sensor = SensorKind.Compass; }

        public override float Progress => IsActive ? gauge : -1f;

        static bool HasMagnetometer => MagneticFieldSensor.current != null && !SensorManager.Instance.ForceAlternative;

        protected override void OnBegin()
        {
            baseline = -1f; sampleTime = 0f; sampleSum = 0f; samples = 0; gauge = 0f;
            hud = new HudGauge
            {
                label = HasMagnetometer ? "Champ magnétique" : "Aimant (4 clics ou touche M)",
                color = new Color(0.55f, 0.75f, 1f),
                // Avec le capteur : force du champ mesuré. En secours : progression des clics.
                value = () => HasMagnetometer ? Mathf.InverseLerp(1f, ratioNeeded, ratio) : gauge,
                visible = () => IsActive,
                buttonLabel = "Approcher l'aimant",
                onButton = () => { gauge = Mathf.Min(1f, gauge + 0.25f); ReportProgress(); Audio.Play(Sfx.Tick); },
                buttonVisible = () => IsActive && !HasMagnetometer,
            };
            HudGauge.Register(hud);
            if (HasMagnetometer) UIManager.Instance.ShowToast("Mesure du champ ambiant… gardez le téléphone loin du métal.");
        }

        void Update()
        {
            if (!IsActive) return;
            bool keyMagnet = Keyboard.current != null && Keyboard.current.mKey.isPressed;
            if (HasMagnetometer)
            {
                float mag = MagneticFieldSensor.current.magneticField.ReadValue().magnitude;
                if (baseline < 0f)
                {
                    // Une seconde de mesure du champ terrestre ambiant (environ 25 à 65 µT selon le lieu)
                    sampleSum += mag; samples++; sampleTime += Time.deltaTime;
                    if (sampleTime >= 1f) { baseline = Mathf.Max(5f, sampleSum / samples); UIManager.Instance.ShowToast($"Champ ambiant : {baseline:0} µT. Approchez maintenant un objet métallique !"); }
                    return;
                }
                ratio = Mathf.Lerp(ratio, mag / baseline, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }

            bool charging = keyMagnet || (HasMagnetometer && ratio >= ratioNeeded);
            if (charging)
            {
                gauge += Time.deltaTime / fillTime;
                ReportProgress();
                if (sparks != null && !sparks.isEmitting) sparks.Play();
            }
            else
            {
                // Le champ ne retombe que si l'on éloigne vraiment le téléphone du métal ;
                // en commande de secours (clics), la jauge garde ce qui a été gagné.
                if (HasMagnetometer) gauge = Mathf.Max(0f, gauge - Time.deltaTime / (fillTime * 3f));
                if (sparks != null && sparks.isEmitting) sparks.Stop();
            }
            gauge = Mathf.Clamp01(gauge);
            SetGlow(seal, new Color(0.5f, 0.75f, 1f), 0.4f + gauge * 4f);
            Audio.SetLoopIntensity(gauge * 0.6f);
            if (gauge >= 1f) Complete();
        }

        protected override void OnComplete()
        {
            HudGauge.Unregister(hud);
            if (sparks != null) sparks.Stop();
            if (lid != null) StartCoroutine(Open());
        }

        System.Collections.IEnumerator Open()
        {
            Audio.Play(Sfx.Mechanism);
            Quaternion a = lid.localRotation, b = a * Quaternion.Euler(100f, 0f, 0f);
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.2f)
            {
                lid.localRotation = Quaternion.Slerp(a, b, Mathf.SmoothStep(0, 1, t));
                yield return null;
            }
        }

        void OnDestroy() { if (hud != null) HudGauge.Unregister(hud); }
    }
}
