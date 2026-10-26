using System;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace LostTemple
{
    public enum SensorKind { Accelerometer, Gyroscope, Compass, Microphone, Proximity, Light }

    /// <summary>
    /// Point d'accès unique aux capteurs : disponibilité, calibrage, lissage et valeurs de secours.
    /// Les énigmes ne lisent jamais un capteur directement, elles passent par les getters "Get..."
    /// qui basculent automatiquement sur la commande alternative (UI tactile / clavier) si besoin.
    /// </summary>
    public class SensorManager : MonoBehaviour
    {
        public static SensorManager Instance { get; private set; }

        // --- Réglages (sauvegardés dans PlayerPrefs) ---
        public float Sensitivity { get; private set; } = 1f;          // 0.3 .. 2
        public float MicSensitivity { get; private set; } = 1f;       // 0.3 .. 3
        public bool ForceAlternative { get; private set; }

        // --- Valeurs de secours écrites par l'UI tactile ---
        [NonSerialized] public Vector2 FallbackTilt;
        [NonSerialized] public float FallbackRotate;      // -1 .. 1
        [NonSerialized] public float FallbackTurn;        // -1 .. 1 (boussole)
        [NonSerialized] public bool FallbackBlow;
        [NonSerialized] public bool FallbackCover;
        [NonSerialized] public float FallbackLight;       // 0 .. 1

        // --- État interne ---
        Vector3 accelFiltered;
        Vector3 accelOffset = new Vector3(0f, 0.5f, -0.85f); // téléphone tenu à ~30°, remplacé par Calibrate
        float headingSmoothed;
        bool headingInit;
        float fallbackHeading;
        float lightFiltered = -1f;
        float proximityMax = 5f;

        AudioClip micClip;
        string micDevice;
        readonly float[] micBuffer = new float[512];
        float micLevel;
        public float MicNoiseFloor { get; private set; } = 0.01f;
        public bool MicPermissionGranted { get; private set; }
        public bool MicPermissionDenied { get; private set; }

        public event Action SettingsChanged;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
            Sensitivity = PlayerPrefs.GetFloat("LT_Sensitivity", 1f);
            MicSensitivity = PlayerPrefs.GetFloat("LT_MicSensitivity", 1f);
            ForceAlternative = PlayerPrefs.GetInt("LT_ForceAlt", 0) == 1;
            if (PlayerPrefs.HasKey("LT_TiltOffsetX"))
                accelOffset = new Vector3(PlayerPrefs.GetFloat("LT_TiltOffsetX"), PlayerPrefs.GetFloat("LT_TiltOffsetY"), PlayerPrefs.GetFloat("LT_TiltOffsetZ"));
#if !UNITY_EDITOR
            MicPermissionGranted = HasMicPermissionNative();
#else
            MicPermissionGranted = Microphone.devices.Length > 0;
#endif
        }

        void OnEnable() { EnableSensors(); }

        void OnApplicationFocus(bool focus) { if (focus) EnableSensors(); }

        void OnDestroy() { StopMicrophone(); }

        static void Enable(InputDevice d)
        {
            if (d == null) return;
            if (!d.enabled) InputSystem.EnableDevice(d);
            if (d is Sensor s) s.samplingFrequency = 50f;
        }

        public void EnableSensors()
        {
            Enable(Accelerometer.current);
            Enable(UnityEngine.InputSystem.Gyroscope.current);
            Enable(MagneticFieldSensor.current);
            Enable(GravitySensor.current);
            Enable(ProximitySensor.current);
            Enable(LightSensor.current);
        }

        // ------------------------------------------------------------------ Disponibilité

        public bool IsHardwareAvailable(SensorKind kind)
        {
            switch (kind)
            {
                case SensorKind.Accelerometer: return Accelerometer.current != null;
                case SensorKind.Gyroscope: return UnityEngine.InputSystem.Gyroscope.current != null;
                case SensorKind.Compass: return MagneticFieldSensor.current != null && Accelerometer.current != null;
                case SensorKind.Microphone: return Microphone.devices.Length > 0 && !MicPermissionDenied;
                case SensorKind.Proximity: return ProximitySensor.current != null;
                case SensorKind.Light: return LightSensor.current != null;
            }
            return false;
        }

        /// <summary>Vrai si l'énigme doit utiliser la commande alternative pour ce capteur.</summary>
        public bool UsesFallback(SensorKind kind) => ForceAlternative || !IsHardwareAvailable(kind);

        public static string DisplayName(SensorKind kind)
        {
            switch (kind)
            {
                case SensorKind.Accelerometer: return "Accéléromètre";
                case SensorKind.Gyroscope: return "Gyroscope";
                case SensorKind.Compass: return "Boussole";
                case SensorKind.Microphone: return "Microphone";
                case SensorKind.Proximity: return "Proximité";
                case SensorKind.Light: return "Luminosité";
            }
            return kind.ToString();
        }

        // ------------------------------------------------------------------ Réglages

        public void SetSensitivity(float v) { Sensitivity = Mathf.Clamp(v, 0.3f, 2f); PlayerPrefs.SetFloat("LT_Sensitivity", Sensitivity); SettingsChanged?.Invoke(); }
        public void SetMicSensitivity(float v) { MicSensitivity = Mathf.Clamp(v, 0.3f, 3f); PlayerPrefs.SetFloat("LT_MicSensitivity", MicSensitivity); SettingsChanged?.Invoke(); }
        public void SetForceAlternative(bool v) { ForceAlternative = v; PlayerPrefs.SetInt("LT_ForceAlt", v ? 1 : 0); SettingsChanged?.Invoke(); }

        /// <summary>Enregistre la position actuelle du téléphone comme position "neutre" pour l'inclinaison.</summary>
        public void CalibrateTilt()
        {
            if (Accelerometer.current == null) return;
            accelOffset = Accelerometer.current.acceleration.ReadValue();
            accelFiltered = accelOffset;
            PlayerPrefs.SetFloat("LT_TiltOffsetX", accelOffset.x);
            PlayerPrefs.SetFloat("LT_TiltOffsetY", accelOffset.y);
            PlayerPrefs.SetFloat("LT_TiltOffsetZ", accelOffset.z);
        }

        // ------------------------------------------------------------------ Mise à jour

        void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (Accelerometer.current != null)
                accelFiltered = Vector3.Lerp(accelFiltered, Accelerometer.current.acceleration.ReadValue(), 1f - Mathf.Exp(-12f * dt));

            if (IsHardwareAvailable(SensorKind.Compass))
            {
                float raw = ComputeHeading();
                if (!headingInit) { headingSmoothed = raw; headingInit = true; }
                headingSmoothed = Mathf.LerpAngle(headingSmoothed, raw, 1f - Mathf.Exp(-6f * dt));
                headingSmoothed = Mathf.Repeat(headingSmoothed, 360f);
            }

            if (LightSensor.current != null)
            {
                float lux = LightSensor.current.lightLevel.ReadValue();
                lightFiltered = lightFiltered < 0f ? lux : Mathf.Lerp(lightFiltered, lux, 1f - Mathf.Exp(-5f * dt));
            }

            if (ProximitySensor.current != null)
                proximityMax = Mathf.Max(proximityMax, ProximitySensor.current.distance.ReadValue());

            UpdateMicrophone(dt);

            // Cap virtuel pour le mode secours
            fallbackHeading = Mathf.Repeat(fallbackHeading + (GetKeyboardAxis(Key.E, Key.Q) + FallbackTurn) * 90f * Sensitivity * dt, 360f);
        }

        /// <summary>
        /// Cap magnétique (0-360°) calculé à partir du champ magnétique et de la gravité (méthode Android getRotationMatrix).
        /// L'Input System renvoie l'accélération en convention Unity (-a / 9.81) et le champ magnétique brut.
        /// </summary>
        float ComputeHeading()
        {
            Vector3 a = -accelFiltered;                                   // vers le haut, repère Android
            Vector3 e = MagneticFieldSensor.current.magneticField.ReadValue();
            Vector3 h = Vector3.Cross(e, a);
            if (h.sqrMagnitude < 1e-6f) return headingSmoothed;
            h.Normalize();
            Vector3 an = a.normalized;
            Vector3 m = Vector3.Cross(an, h);
            // Téléphone à plat : on suit le haut de l'écran (axe Y). Téléphone vertical : on suit l'arrière (caméra, -Z).
            bool flat = Mathf.Abs(an.z) > 0.7f;
            float az = flat ? Mathf.Atan2(h.y, m.y) : Mathf.Atan2(-h.z, -m.z);
            return Mathf.Repeat(az * Mathf.Rad2Deg, 360f);
        }

        // ------------------------------------------------------------------ Getters utilisés par les énigmes

        /// <summary>Inclinaison lissée et calibrée : x = droite, y = avant. Environ -1..1.</summary>
        public Vector2 GetTilt()
        {
            Vector2 kb = new Vector2(GetKeyboardAxis(Key.RightArrow, Key.LeftArrow) + GetKeyboardAxis(Key.D, Key.A),
                                     GetKeyboardAxis(Key.UpArrow, Key.DownArrow) + GetKeyboardAxis(Key.W, Key.S));
            Vector2 v = FallbackTilt + Vector2.ClampMagnitude(kb, 1f) * 0.6f;
            if (!UsesFallback(SensorKind.Accelerometer))
            {
                Vector3 d = accelFiltered - accelOffset;
                Vector2 t = new Vector2(d.x, d.y);
                if (t.magnitude < 0.03f) t = Vector2.zero;                  // zone morte
                v += t * 1.6f;
            }
            return Vector2.ClampMagnitude(v * Sensitivity, 1.5f);
        }

        /// <summary>Vitesse de rotation "volant" en degrés/s (positif = sens horaire vu de face).</summary>
        public float GetRotationRate()
        {
            float v = (GetKeyboardAxis(Key.E, Key.Q) + FallbackRotate) * 90f;
            var gyro = UnityEngine.InputSystem.Gyroscope.current;
            if (!UsesFallback(SensorKind.Gyroscope) && gyro != null)
            {
                float wz = -gyro.angularVelocity.ReadValue().z * Mathf.Rad2Deg;
                if (Mathf.Abs(wz) > 4f) v += wz;                            // filtre la dérive
            }
            return v * Sensitivity;
        }

        /// <summary>Cap en degrés (0 = nord magnétique, ou cap virtuel en mode secours).</summary>
        public float GetHeading() => UsesFallback(SensorKind.Compass) ? fallbackHeading : headingSmoothed;

        /// <summary>Niveau sonore normalisé : 0 = bruit de fond, 1 = seuil de déclenchement atteint.</summary>
        public float GetSoundLevel01()
        {
            float fb = (FallbackBlow || IsKey(Key.Space)) ? 1.2f : 0f;
            if (UsesFallback(SensorKind.Microphone)) return fb;
            return Mathf.Max(fb, (micLevel - MicNoiseFloor) / Mathf.Max(0.0001f, MicThreshold - MicNoiseFloor));
        }

        public float MicRawLevel => micLevel;
        public float MicThreshold => Mathf.Max(MicNoiseFloor * 3f, 0.03f) / MicSensitivity;

        /// <summary>Vrai si le capteur de proximité est couvert (main près de l'écouteur).</summary>
        public bool IsCovered()
        {
            if (FallbackCover || IsKey(Key.C)) return true;
            if (UsesFallback(SensorKind.Proximity)) return false;
            float d = ProximitySensor.current.distance.ReadValue();
            return d < Mathf.Min(3f, proximityMax * 0.5f);
        }

        public float ProximityDistance => ProximitySensor.current != null ? ProximitySensor.current.distance.ReadValue() : -1f;

        /// <summary>Luminosité lissée en lux (-1 si indisponible).</summary>
        public float GetLux() => LightSensor.current != null ? lightFiltered : -1f;

        /// <summary>
        /// Variation de lumière par rapport à une référence : 0 = identique, 1 = variation forte
        /// (lumière divisée par 3 en couvrant, ou multipliée par 3 avec une lampe).
        /// </summary>
        public float GetLightChange01(float baselineLux)
        {
            float fb = Mathf.Max(FallbackLight, IsKey(Key.L) ? 1f : 0f);
            if (UsesFallback(SensorKind.Light)) return fb;
            float ratio = (GetLux() + 1f) / (Mathf.Max(0f, baselineLux) + 1f);
            return Mathf.Max(fb, Mathf.Clamp01(Mathf.Abs(Mathf.Log(ratio)) / Mathf.Log(3f)));
        }

        // ------------------------------------------------------------------ Microphone

        static bool HasMicPermissionNative()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Permission.HasUserAuthorizedPermission(Permission.Microphone);
#else
            return true;
#endif
        }

        /// <summary>Demande la permission micro (Android) puis démarre l'écoute. Le résultat est passé au callback.</summary>
        public void RequestMicrophone(Action<bool> onResult)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                var cb = new PermissionCallbacks();
                cb.PermissionGranted += _ => { MicPermissionGranted = true; MicPermissionDenied = false; StartMicrophone(); onResult?.Invoke(true); };
                cb.PermissionDenied += _ => { MicPermissionDenied = true; onResult?.Invoke(false); };
                cb.PermissionDeniedAndDontAskAgain += _ => { MicPermissionDenied = true; onResult?.Invoke(false); };
                Permission.RequestUserPermission(Permission.Microphone, cb);
                return;
            }
#endif
            MicPermissionGranted = true;
            StartMicrophone();
            onResult?.Invoke(Microphone.devices.Length > 0);
        }

        public void StartMicrophone()
        {
            if (micClip != null || Microphone.devices.Length == 0) return;
            micDevice = Microphone.devices[0];
            // Le son n'est jamais enregistré ni envoyé : on lit seulement le niveau RMS d'un tampon circulaire d'1 s.
            micClip = Microphone.Start(micDevice, true, 1, 16000);
        }

        public void StopMicrophone()
        {
            if (micClip == null) return;
            Microphone.End(micDevice);
            micClip = null;
            micLevel = 0f;
        }

        public bool MicRunning => micClip != null;

        /// <summary>Mesure le bruit de fond pendant ~1 s pour régler le seuil automatiquement.</summary>
        public System.Collections.IEnumerator CalibrateMicNoise()
        {
            float sum = 0f; int n = 0;
            float t = 0f;
            while (t < 1f)
            {
                t += Time.unscaledDeltaTime;
                if (micClip != null) { sum += micLevel; n++; }
                yield return null;
            }
            if (n > 0) MicNoiseFloor = Mathf.Max(0.003f, sum / n);
        }

        void UpdateMicrophone(float dt)
        {
            if (micClip == null) return;
            int pos = Microphone.GetPosition(micDevice) - micBuffer.Length;
            if (pos < 0) return;
            micClip.GetData(micBuffer, pos);
            float s = 0f;
            for (int i = 0; i < micBuffer.Length; i++) s += micBuffer[i] * micBuffer[i];
            float rms = Mathf.Sqrt(s / micBuffer.Length);
            // Montée rapide, descente lente : un souffle court reste détectable.
            micLevel = rms > micLevel ? Mathf.Lerp(micLevel, rms, 1f - Mathf.Exp(-30f * dt)) : Mathf.Lerp(micLevel, rms, 1f - Mathf.Exp(-6f * dt));
        }

        // ------------------------------------------------------------------ Diagnostic

        public string DescribeValue(SensorKind kind)
        {
            switch (kind)
            {
                case SensorKind.Accelerometer:
                    return Accelerometer.current != null ? $"x {accelFiltered.x:0.00}  y {accelFiltered.y:0.00}  z {accelFiltered.z:0.00}" : "—";
                case SensorKind.Gyroscope:
                    var g = UnityEngine.InputSystem.Gyroscope.current;
                    return g != null ? $"{g.angularVelocity.ReadValue() * Mathf.Rad2Deg:0}°/s" : "—";
                case SensorKind.Compass:
                    return IsHardwareAvailable(kind) ? $"cap {headingSmoothed:0}°" : "—";
                case SensorKind.Microphone:
                    if (MicPermissionDenied) return "permission refusée";
                    return micClip != null ? $"niveau {micLevel:0.000} (seuil {MicThreshold:0.000})" : "arrêté — appuyez sur Tester";
                case SensorKind.Proximity:
                    return ProximitySensor.current != null ? $"{ProximityDistance:0.0} cm {(IsCovered() ? "(couvert)" : "")}" : "—";
                case SensorKind.Light:
                    return LightSensor.current != null ? $"{GetLux():0} lux" : "—";
            }
            return "";
        }

        // ------------------------------------------------------------------ Clavier (tests dans l'éditeur)

        static bool IsKey(Key k) => Keyboard.current != null && Keyboard.current[k].isPressed;
        static float GetKeyboardAxis(Key pos, Key neg) => (IsKey(pos) ? 1f : 0f) - (IsKey(neg) ? 1f : 0f);
    }
}
