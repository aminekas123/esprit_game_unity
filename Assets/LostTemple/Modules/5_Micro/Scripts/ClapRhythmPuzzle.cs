using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LostTemple
{
    /// <summary>
    /// Salle 4b — Microphone : le monstre réveillé est hypnotisé par un rythme. Le jeu joue un rythme
    /// (groupes de coups), le joueur le reproduit en tapant dans ses mains. Une erreur rejoue le rythme.
    /// </summary>
    public class ClapRhythmPuzzle : PuzzleStep
    {
        public MonsterController monster;
        public Renderer[] beatLamps;
        [Tooltip("Pause (s) qui sépare deux groupes de claquements.")]
        public float groupGap = 0.9f;

        static readonly int[][] patterns = { new[] { 2, 3 }, new[] { 3, 1 }, new[] { 1, 2, 1 }, new[] { 3, 2 }, new[] { 2, 1, 2 } };

        int[] pattern;
        readonly List<int> groups = new List<int>();
        int count, lit;
        float lastOnset = -10f;
        bool armed, listening;

        void Reset() { sensor = SensorKind.Microphone; }

        public override float Progress
        {
            get
            {
                if (!IsActive || pattern == null) return -1f;
                int total = 0; foreach (var p in pattern) total += p;
                return lit / (float)total;
            }
        }

        protected override void OnBegin()
        {
            pattern = patterns[Random.Range(0, patterns.Length)];
            objective = $"Rythme à taper : {PatternText()}   (pause d'1 s entre les groupes)";
            UIManager.Instance.SetStep(this);
            StartCoroutine(Demo());
        }

        /// <summary>Le rythme écrit à l'écran, ex. « ●● · ●●● ».</summary>
        string PatternText()
        {
            var parts = new string[pattern.Length];
            for (int i = 0; i < pattern.Length; i++) parts[i] = new string('●', pattern[i]);
            return string.Join("  ·  ", parts);
        }

        /// <summary>
        /// Volume pour compter les claquements : volume INSTANTANÉ du micro (≈30 ms, sans lissage),
        /// rapporté au seuil. Le niveau lissé redescend trop lentement et fusionnerait deux claps rapprochés.
        /// Commandes de secours : bouton « Souffler » ou touche Espace (un appui = un clap).
        /// </summary>
        float ClapLevel()
        {
            float fallback = (Sensors.FallbackBlow || (Keyboard.current != null && Keyboard.current.spaceKey.isPressed)) ? 1.5f : 0f;
            if (Sensors.UsesFallback(SensorKind.Microphone) || !Sensors.MicRunning) return fallback;
            return Mathf.Max(fallback, Sensors.MicInstant / Mathf.Max(0.0001f, Sensors.MicThreshold));
        }

        /// <summary>Le temple joue le rythme à reproduire.</summary>
        IEnumerator Demo()
        {
            listening = false;
            groups.Clear(); count = 0; lit = 0;
            foreach (var l in beatLamps) SetGlow(l, Color.black, 0f);
            UIManager.Instance.ShowToast("Écoutez le rythme… puis tapez dans vos mains !");
            yield return new WaitForSeconds(1f);
            int k = 0;
            foreach (int g in pattern)
            {
                for (int i = 0; i < g; i++)
                {
                    Audio.Play(Sfx.Tick);
                    CameraRig.Shake(0.05f);
                    if (k < beatLamps.Length) SetGlow(beatLamps[k], new Color(0.4f, 0.8f, 1f), 2f);
                    k++;
                    yield return new WaitForSeconds(0.3f);
                }
                yield return new WaitForSeconds(groupGap);
            }
            foreach (var l in beatLamps) SetGlow(l, Color.black, 0f);
            yield return new WaitForSeconds(0.5f);   // laisser le son du temple s'éteindre avant d'écouter
            armed = false;
            listening = true;
            UIManager.Instance.ShowToast("À vous ! Reproduisez le rythme.");
        }

        void Update()
        {
            if (!IsActive || !listening) return;
            float level = ClapLevel();
            if (!armed && level < 0.5f) armed = true;            // le son est retombé : prêt pour le clap suivant
            if (armed && level >= 1f && Time.time - lastOnset > 0.12f)
            {
                armed = false;
                count++;
                lastOnset = Time.time;
                if (lit < beatLamps.Length) SetGlow(beatLamps[lit], new Color(1f, 0.8f, 0.3f), 3f);
                lit++;
                ReportProgress();
            }

            if (count > 0 && Time.time - lastOnset > groupGap)
            {
                groups.Add(count);
                count = 0;
                int i = groups.Count - 1;
                if (i >= pattern.Length || groups[i] != pattern[i])
                {
                    Audio.Play(Sfx.Fail);
                    UIManager.Instance.ShowToast($"Mauvais rythme : {groups[i]} clap(s) au lieu de {(i < pattern.Length ? pattern[i] : 0)}. Le temple le rejoue.");
                    StartCoroutine(Demo());
                    return;
                }
                // Pas de son ici : le micro le prendrait pour un claquement
                if (groups.Count == pattern.Length) Complete();
                else UIManager.Instance.ShowToast($"Groupe {groups.Count}/{pattern.Length} réussi !");
            }
        }

        protected override void OnComplete() { StartCoroutine(Hypnotize()); }

        IEnumerator Hypnotize()
        {
            for (float t = 0; t < 1.5f; t += Time.deltaTime) { monster.SetStun(Mathf.Sin(t / 1.5f * Mathf.PI)); yield return null; }
            monster.SetStun(0f);
        }
    }
}
