using System.Collections.Generic;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Réutilise la détection existante (PhotoPuzzle) : à chaque photo validée, l'objet de la couleur détectée
    /// apparaît dans la salle. Les objets précédents restent en place ; une couleur déjà présente n'est pas
    /// dupliquée (l'objet existant « bat » simplement). Positions : emplacements prédéfinis (slots), puis
    /// grille de secours, sans jamais superposer deux objets.
    /// </summary>
    public class ColorObjectSpawner : MonoBehaviour
    {
        [Tooltip("Prefabs de formes simples (cube, sphère…), utilisés à tour de rôle.")]
        public GameObject[] prefabs;
        [Tooltip("Emplacements dans la salle, remplis dans l'ordre.")]
        public Transform[] slots;
        [Tooltip("Parent des objets générés (optionnel).")]
        public Transform container;
        public float objectScale = 0.7f;
        [Tooltip("Distance minimale entre deux objets.")]
        public float minSpacing = 1.1f;
        [Tooltip("Décocher pour ne créer qu'un objet par couleur (recommandé).")]
        public bool allowDuplicates = false;

        [Header("Grille de secours (si les slots sont pleins)")]
        public Vector3 overflowOrigin = new Vector3(2f, 0.5f, -4.6f);
        public float overflowSpacingX = 1.6f;
        public float overflowSpacingZ = -1.6f;
        public int overflowColumns = 2;
        public int overflowRows = 6;

        readonly Dictionary<string, ColorObjectVisual> byColor = new Dictionary<string, ColorObjectVisual>();
        readonly List<Vector3> used = new List<Vector3>();
        int count;

        public int SpawnedCount => count;

        /// <summary>Vrai si un objet de cette couleur est déjà dans la salle.</summary>
public bool Has(PhotoTarget what) { return HasKey(what.ToString()); }

/// <summary>Vrai si un objet de cette clé de couleur (ex. "Orange") est déjà dans la salle.</summary>
        public bool HasKey(string key) { return key != null && byColor.TryGetValue(key, out var v) && v != null; }


        void OnEnable() { PhotoPuzzle.ColorDetected += OnColorDetected; }
        void OnDisable() { PhotoPuzzle.ColorDetected -= OnColorDetected; }

        void OnColorDetected(PhotoTarget what, Color c)
        {
            try { SpawnFor(what, c); }
            catch (System.Exception e) { Debug.LogException(e, this); }   // ne jamais bloquer l'énigme
        }

        /// <summary>Crée (ou fait battre) l'objet de cette couleur. Renvoie l'objet concerné.</summary>
public ColorObjectVisual SpawnFor(PhotoTarget what, Color c)
        {
            return SpawnForColor(what.ToString(), PhotoPuzzle.Describe(what), c);
        }

/// <summary>N'importe quelle couleur : crée (ou fait battre) l'objet de cette clé. Renvoie l'objet concerné.</summary>
        public ColorObjectVisual SpawnForColor(string key, string label, Color c)
        {
            if (!allowDuplicates && byColor.TryGetValue(key, out var existing) && existing != null)
            {
                existing.Pulse();
                Toast($"{label} est déjà dans la salle.");
                return existing;
            }

            Vector3 pos = NextPosition();
            var go = CreateInstance(pos);
            go.name = "Color Object - " + key;
            var visual = go.GetComponent<ColorObjectVisual>();
            if (visual == null) visual = go.AddComponent<ColorObjectVisual>();
            visual.InitColor(key, c, objectScale);

            used.Add(pos);
            byColor[key] = visual;
            count++;

            var rm = RoomManager.Current;
            if (rm != null && rm.sparks != null)
            {
                rm.sparks.transform.position = pos + Vector3.up * 0.4f;
                rm.sparks.Emit(25);
            }
            Toast($"Couleur {label} détectée : un objet apparaît dans la salle !");
            return visual;
        }


        GameObject CreateInstance(Vector3 pos)
        {
            Transform parent = container != null ? container : transform;
            if (prefabs != null && prefabs.Length > 0)
            {
                var prefab = prefabs[count % prefabs.Length];
                if (prefab != null) return Instantiate(prefab, pos, Quaternion.Euler(0f, 25f * count, 0f), parent);
            }
            // Secours si aucun prefab n'est assigné : cube simple.
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.SetPositionAndRotation(pos, Quaternion.Euler(0f, 25f * count, 0f));
            cube.transform.SetParent(parent, true);
            return cube;
        }

Vector3 NextPosition()
        {
            if (slots != null)
                foreach (var s in slots)
                    if (s != null && IsFree(s.position)) return s.position;
            int cols = Mathf.Max(1, overflowColumns);
            int max = Mathf.Max(1, overflowRows) * cols;
            for (int i = 0; i < max; i++)
            {
                var p = overflowOrigin + new Vector3((i % cols) * overflowSpacingX, 0f, (i / cols) * overflowSpacingZ);
                if (IsFree(p)) return p;
            }
            return overflowOrigin + Vector3.up * (0.5f * count);   // dernier recours : empilé, jamais au même point
        }

        bool IsFree(Vector3 p)
        {
            foreach (var u in used)
            {
                var d = u - p; d.y = 0f;
                if (d.sqrMagnitude < minSpacing * minSpacing) return false;
            }
            return true;
        }

        static void Toast(string msg)
        {
            if (UIManager.Instance != null) UIManager.Instance.ShowToast(msg);
        }
    }
}
