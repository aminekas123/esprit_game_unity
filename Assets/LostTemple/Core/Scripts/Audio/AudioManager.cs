using UnityEngine;

namespace LostTemple
{
    public enum Sfx { Click, Success, Fail, Mechanism, Reveal, Victory, Tick, Roar, Shutter }

    /// <summary>
    /// Musique d'ambiance et retours sonores. Les sons sont synthétisés au démarrage (aucun fichier audio requis) ;
    /// l'équipe audio peut les remplacer en assignant des AudioClip dans l'inspecteur.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        public AudioClip ambientOverride;
        public AudioClip[] sfxOverrides = new AudioClip[9];

        AudioSource ambient, sfx, loop, drums;
        AudioClip[] clips;
        const int Rate = 44100;

        public float Volume { get; private set; } = 0.8f;
        public bool Haptics { get; private set; } = true;
        /// <summary>Tension demandée par le jeu (boss, rocher…) : fait monter les percussions.</summary>
        public float Tension { get; set; }
        /// <summary>Nombre de zones où le micro écoute (la musique se fait discrète).</summary>
        public static int MicZones;

        // Note de base du bourdon pour le menu (-1) puis chaque salle : chaque salle a sa propre couleur.
        static readonly float[] roomRoots = { 55f, 55f, 49f, 58.27f, 43.65f, 51.91f, 46.25f, 41.2f };
        int currentRoom = -2;
        float drumVol, duck = 1f;

        void Awake()
        {
            Instance = this;
            Volume = PlayerPrefs.GetFloat("LT_Volume", 0.8f);
            Haptics = PlayerPrefs.GetInt("LT_Haptics", 1) == 1;
            AudioListener.volume = Volume;

            ambient = gameObject.AddComponent<AudioSource>();
            ambient.loop = true; ambient.volume = 0.35f; ambient.ignoreListenerPause = true;
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.ignoreListenerPause = true;
            loop = gameObject.AddComponent<AudioSource>();
            loop.loop = true; loop.volume = 0f;

            clips = new AudioClip[9];
            clips[(int)Sfx.Click] = Tone("click", new[] { 880f }, 0.06f, 0.25f);
            clips[(int)Sfx.Success] = Tone("success", new[] { 523f, 659f, 784f, 1046f }, 0.12f, 0.35f);
            clips[(int)Sfx.Fail] = Tone("fail", new[] { 220f, 165f }, 0.18f, 0.35f, square: true);
            clips[(int)Sfx.Mechanism] = Rumble("mechanism", 1.4f);
            clips[(int)Sfx.Reveal] = Tone("reveal", new[] { 392f, 587f, 784f }, 0.22f, 0.3f);
            clips[(int)Sfx.Victory] = Tone("victory", new[] { 523f, 659f, 784f, 659f, 784f, 1046f }, 0.18f, 0.4f);
            clips[(int)Sfx.Tick] = Tone("tick", new[] { 1320f }, 0.03f, 0.15f);
            clips[(int)Sfx.Roar] = Roar("roar", 1.6f);
            clips[(int)Sfx.Shutter] = Rumble("shutter", 0.12f);
            for (int i = 0; i < clips.Length && i < sfxOverrides.Length; i++)
                if (sfxOverrides[i] != null) clips[i] = sfxOverrides[i];

            ambient.clip = ambientOverride != null ? ambientOverride : Drone(8f, 55f);
            ambient.Play();
            drums = gameObject.AddComponent<AudioSource>();
            drums.loop = true; drums.volume = 0f; drums.clip = Drums(2f);
            drums.Play();
            loop.clip = Rumble("loop", 2f);
            loop.Play();
        }

        /// <summary>Change la couleur de la musique selon la salle (-1 = menu).</summary>
        public void SetRoomMusic(int room)
        {
            if (room == currentRoom || ambientOverride != null) return;
            currentRoom = room;
            ambient.clip = Drone(8f, roomRoots[Mathf.Clamp(room + 1, 0, roomRoots.Length - 1)]);
            ambient.Play();
            Tension = 0f;
        }

        void Update()
        {
            // Tension : boss/rocher, ou piège actif tout proche de l'explorateur
            float t = Tension;
            var p = PlayerController.Instance;
            if (p != null)
            {
                float d = Hazard.NearestActiveDistance(p.transform.position);
                if (d < 5f) t = Mathf.Max(t, Mathf.InverseLerp(5f, 1.5f, d) * 0.8f);
            }
            drumVol = Mathf.Lerp(drumVol, t * 0.55f, 1f - Mathf.Exp(-3f * Time.unscaledDeltaTime));
            drums.pitch = 1f + t * 0.15f;

            // Pendant que le micro écoute, la musique se fait discrète pour ne pas fausser la mesure.
            var step = RoomManager.Current != null ? RoomManager.Current.ActiveStep : null;
            bool micListening = MicZones > 0 || (step != null && step.IsActive && step.usesSensor && step.sensor == SensorKind.Microphone);
            duck = Mathf.Lerp(duck, micListening ? 0.2f : 1f, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
            ambient.volume = 0.35f * duck;
            drums.volume = drumVol * duck;
        }

        public void SetVolume(float v)
        {
            Volume = Mathf.Clamp01(v);
            AudioListener.volume = Volume;
            PlayerPrefs.SetFloat("LT_Volume", Volume);
        }

        public void SetHaptics(bool v)
        {
            Haptics = v;
            PlayerPrefs.SetInt("LT_Haptics", v ? 1 : 0);
        }

        public void Play(Sfx s, float volume = 1f)
        {
            sfx.PlayOneShot(clips[(int)s], volume);
            if (s == Sfx.Success || s == Sfx.Fail || s == Sfx.Victory) Vibrate();
        }

        /// <summary>Retour haptique sur les réussites et les échecs (Android/iOS uniquement).</summary>
        public void Vibrate()
        {
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            if (Haptics) Handheld.Vibrate();
#endif
        }

        /// <summary>Son continu (grondement) dont le volume suit la progression d'une énigme (0..1).</summary>
        public void SetLoopIntensity(float v)
        {
            loop.volume = Mathf.Clamp01(v) * 0.5f;
            loop.pitch = 0.8f + Mathf.Clamp01(v) * 0.6f;
        }

        // ------------------------------------------------------------------ Synthèse

        static AudioClip Tone(string name, float[] notes, float noteLen, float amp, bool square = false)
        {
            int perNote = (int)(Rate * noteLen);
            var data = new float[perNote * notes.Length + Rate / 5];
            for (int n = 0; n < notes.Length; n++)
            {
                for (int i = 0; i < perNote + Rate / 5 && n * perNote + i < data.Length; i++)
                {
                    float t = i / (float)Rate;
                    float env = Mathf.Exp(-t * 9f) * Mathf.Clamp01(t * 400f);
                    float ph = 2f * Mathf.PI * notes[n] * t;
                    float w = square ? Mathf.Sign(Mathf.Sin(ph)) * 0.5f : Mathf.Sin(ph) + 0.3f * Mathf.Sin(ph * 2f);
                    data[n * perNote + i] += w * env * amp;
                }
            }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Rumble(string name, float len)
        {
            var data = new float[(int)(Rate * len)];
            var rng = new System.Random(7);
            float lp = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                lp += ((float)rng.NextDouble() * 2f - 1f - lp) * 0.02f;               // bruit filtré passe-bas
                float env = Mathf.Clamp01(t * 8f) * Mathf.Clamp01((len - t) * 8f);
                data[i] = (lp * 3f + 0.25f * Mathf.Sin(2f * Mathf.PI * 55f * t)) * env * 0.5f;
            }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        /// <summary>Rugissement : grondement grave modulé + bruit, avec une attaque brutale.</summary>
        static AudioClip Roar(string name, float len)
        {
            var data = new float[(int)(Rate * len)];
            var rng = new System.Random(11);
            float lp = 0f, ph = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float env = Mathf.Clamp01(t * 12f) * Mathf.Pow(1f - t / len, 1.5f);
                float f = 70f + 40f * Mathf.Sin(t * 7f) - 25f * t;
                ph += 2f * Mathf.PI * f / Rate;
                lp += ((float)rng.NextDouble() * 2f - 1f - lp) * 0.08f;
                float growl = Mathf.Sin(ph) * (0.6f + 0.4f * Mathf.Sin(t * 60f)) + Mathf.Sign(Mathf.Sin(ph * 0.5f)) * 0.25f;
                data[i] = (growl * 0.6f + lp * 2.5f) * env * 0.6f;
            }
            var c = AudioClip.Create(name, data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        /// <summary>Boucle de percussions (grosse caisse + toms) à 120 BPM.</summary>
        static AudioClip Drums(float len)
        {
            var data = new float[(int)(Rate * len)];
            float beat = 0.5f;
            int[] pattern = { 1, 0, 2, 1, 1, 0, 2, 3 };   // sur des croches
            for (int n = 0; n < pattern.Length; n++)
            {
                if (pattern[n] == 0) continue;
                int start = (int)(n * beat * 0.5f * Rate);
                float f0 = pattern[n] == 1 ? 60f : pattern[n] == 2 ? 110f : 150f;
                for (int i = 0; i < Rate * 0.3f && start + i < data.Length; i++)
                {
                    float t = i / (float)Rate;
                    float f = f0 * (1f + 1.5f * Mathf.Exp(-t * 30f));
                    data[start + i] += Mathf.Sin(2f * Mathf.PI * f * t) * Mathf.Exp(-t * (pattern[n] == 1 ? 9f : 14f)) * 0.7f;
                }
            }
            var c = AudioClip.Create("drums", data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }

        static AudioClip Drone(float len, float root)
        {
            var data = new float[(int)(Rate * len)];
            // Fondamentale, quinte, octave, tierce mineure de l'octave (arrondies pour boucler sans clic sur 8 s)
            float[] f = { Mathf.Round(root * len) / len, Mathf.Round(root * 1.5f * len) / len, Mathf.Round(root * 2f * len) / len, Mathf.Round(root * 2.4f * len) / len };
            for (int i = 0; i < data.Length; i++)
            {
                float t = i / (float)Rate;
                float s = 0f;
                for (int k = 0; k < f.Length; k++)
                    s += Mathf.Sin(2f * Mathf.PI * f[k] * t) * (0.5f + 0.5f * Mathf.Sin(2f * Mathf.PI * t / len * (k + 1))) / (k + 1);
                data[i] = s * 0.18f;
            }
            var c = AudioClip.Create("ambient", data.Length, 1, Rate, false);
            c.SetData(data, 0);
            return c;
        }
    }
}
