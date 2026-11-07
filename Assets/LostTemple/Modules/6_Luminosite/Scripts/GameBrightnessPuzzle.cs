using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LostTemple
{
    /// <summary>
    /// Salle 5b — Luminosité : luminosité automatique INVERSÉE. Le capteur de lumière règle la luminosité
    /// du jeu (exposition de l'image) : lumière forte → le jeu s'assombrit au minimum ; lumière faible
    /// (capteur caché) → le jeu s'éclaircit au maximum.
    /// Pour valider, il faut tenir chacun des deux extrêmes pendant quelques secondes.
    /// </summary>
    public class GameBrightnessPuzzle : PuzzleStep
    {
        public TextMesh screenText;
        public Renderer screen;
        public Renderer brightLamp;   // s'allume quand « lumière forte → jeu sombre » est validé
        public Renderer darkLamp;     // s'allume quand « noir → jeu lumineux » est validé
        [Tooltip("Exposition (EV) du jeu quand la lumière est faible : luminosité maximale.")]
        public float maxExposure = 2.4f;
        [Tooltip("Exposition (EV) du jeu quand la lumière est forte : luminosité minimale.")]
        public float minExposure = -1.2f;
        public float holdTime = 2f;

        Volume volume;
        ColorAdjustments color;
        float baseExposure = 0.9f;
        float baseline;
        float brightness = 0.5f;     // luminosité du jeu affichée (0 = minimum, 1 = maximum)
        float darkHeld, brightHeld;
        bool darkDone, brightDone;
        bool restoring;
        HudGauge gauge;

        void Reset() { sensor = SensorKind.Light; }

        public override float Progress => IsActive ? ((darkDone ? 0.5f : darkHeld / holdTime * 0.5f) + (brightDone ? 0.5f : brightHeld / holdTime * 0.5f)) : -1f;

        protected override void OnBegin()
        {
            baseline = Sensors.GetLux();
            darkHeld = brightHeld = 0f;
            darkDone = brightDone = false;
            SetGlow(brightLamp, Color.black, 0f);
            SetGlow(darkLamp, Color.black, 0f);
            CreateVolume();
            AmbientLightSync.Suspended = true;

            gauge = new HudGauge
            {
                label = "Luminosité du jeu",
                color = new Color(1f, 0.9f, 0.5f),
                value = () => brightness,
                visible = () => IsActive,
            };
            HudGauge.Register(gauge);
            if (!Sensors.UsesFallback(SensorKind.Light) && baseline < 8f)
                UIManager.Instance.ShowToast("La pièce est très sombre : allumez une lumière pour mieux voir l'effet.");
        }

        /// <summary>Volume de post-traitement prioritaire : il remplace l'exposition du profil du temple.</summary>
        void CreateVolume()
        {
            if (volume != null) return;
            foreach (var v in FindObjectsByType<Volume>(FindObjectsSortMode.None))
                if (v.sharedProfile != null && v.sharedProfile.TryGet(out ColorAdjustments ca) && ca.postExposure.overrideState)
                { baseExposure = ca.postExposure.value; break; }
            volume = gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 100f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            color = profile.Add<ColorAdjustments>(true);
            color.postExposure.Override(baseExposure);
            volume.profile = profile;
        }

        void Update()
        {
            if (restoring)
            {
                float e = Mathf.MoveTowards(color.postExposure.value, baseExposure, Time.deltaTime * 1.5f);
                color.postExposure.Override(e);
                if (Mathf.Approximately(e, baseExposure)) { restoring = false; Destroy(volume); volume = null; }
                return;
            }
            if (!IsActive) return;

            // Luminosité inversée : beaucoup de lumière → jeu sombre, peu de lumière → jeu lumineux
            float level = Sensors.GetLightLevel01(baseline);
            float target = 1f - level;
            brightness = Mathf.MoveTowards(brightness, target, Time.deltaTime * 1.2f);
            color.postExposure.Override(Mathf.Lerp(minExposure, maxExposure, brightness));

            if (!brightDone)
            {
                brightHeld = brightness <= 0.12f ? brightHeld + Time.deltaTime : Mathf.Max(0f, brightHeld - Time.deltaTime);
                if (brightHeld >= holdTime) { brightDone = true; SetGlow(brightLamp, new Color(1f, 0.85f, 0.3f), 4f); Audio.Play(Sfx.Tick); ReportProgress(); }
            }
            if (!darkDone)
            {
                darkHeld = brightness >= 0.88f ? darkHeld + Time.deltaTime : Mathf.Max(0f, darkHeld - Time.deltaTime);
                if (darkHeld >= holdTime) { darkDone = true; SetGlow(darkLamp, new Color(0.45f, 0.75f, 1f), 4f); Audio.Play(Sfx.Tick); ReportProgress(); }
            }

            if (screenText != null)
            {
                string state = brightness <= 0.12f ? "LUMIERE FORTE\njeu au minimum" : brightness >= 0.88f ? "OBSCURITE\njeu au maximum" : "Luminosite du jeu";
                screenText.text = $"{state}\n{brightness * 100f:0} %";
            }
            SetGlow(screen, new Color(1f, 0.95f, 0.8f), 0.05f + brightness * 0.6f);

            if (brightDone && darkDone) Complete();
        }

        protected override void OnComplete()
        {
            HudGauge.Unregister(gauge);
            AmbientLightSync.Suspended = false;
            restoring = volume != null;
            if (screenText != null) screenText.text = "REGLAGE\nTERMINE !";
        }

        void OnDestroy()
        {
            if (gauge != null) HudGauge.Unregister(gauge);
            AmbientLightSync.Suspended = false;
        }
    }
}
