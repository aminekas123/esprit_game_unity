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

        AudioSource ambient, sfx, loop;
        AudioClip[] clips;
        const int Rate = 44100;

        public float Volume { get; private set; } = 0.8f;
        public bool Haptics { get; private set; } = true;

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

            ambient.clip = ambientOverride != null ? ambientOverride : Drone(8f);
            ambient.Play();
            loop.clip = Rumble("loop", 2f);
            loop.Play();
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

        static AudioClip Drone(float len)
        {
            var data = new float[(int)(Rate * len)];
            float[] f = { 55f, 82.5f, 110f, 165f };
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
