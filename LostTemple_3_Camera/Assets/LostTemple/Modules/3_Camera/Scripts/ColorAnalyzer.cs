using UnityEngine;

namespace LostTemple
{
    /// <summary>Couleur dominante trouvée dans une photo.</summary>
    public struct DetectedColor
    {
        public string key;     // identifiant stable, ex. "Orange" (une seule instance d'objet par clé)
        public string name;    // nom affiché, ex. "ORANGE"
        public Color color;    // vraie couleur moyenne de la zone dominante
        public float share;    // part des pixels (0..1)
    }

    /// <summary>
    /// Détection de couleur « libre » : 12 teintes (tous les 30°) + marron, blanc, gris, noir.
    /// Les vraies couleurs priment sur blanc/gris/noir (un mur blanc derrière l'objet ne gagne pas).
    /// L'objet généré prend la couleur réellement mesurée, pas une couleur prédéfinie.
    /// </summary>
    public static class ColorAnalyzer
    {
        const int Brown = 12, White = 13, Gray = 14, Black = 15, Count = 16;

        static readonly string[] Keys =
        {
            "Red", "Orange", "Yellow", "Lime", "Green", "Turquoise", "Cyan", "Sky", "Blue", "Violet", "Magenta", "Pink",
            "Brown", "White", "Gray", "Black"
        };
        static readonly string[] Names =
        {
            "ROUGE", "ORANGE", "JAUNE", "VERT CITRON", "VERT", "TURQUOISE", "CYAN", "BLEU CIEL", "BLEU", "VIOLET", "MAGENTA", "ROSE",
            "MARRON", "BLANC", "GRIS", "NOIR"
        };

        /// <summary>Renvoie vrai si une couleur dépasse minShare. Remplit toujours « result » (meilleur candidat).</summary>
        public static bool Analyze(Color32[] px, float minShare, out DetectedColor result)
        {
            result = default;
            if (px == null || px.Length == 0) return false;

            int step = Mathf.Max(1, px.Length / 4000);
            var n = new int[Count];
            var sr = new float[Count]; var sg = new float[Count]; var sb = new float[Count];
            int total = 0;
            for (int i = 0; i < px.Length; i += step)
            {
                var p = px[i];
                int c = Classify(p);
                n[c]++; sr[c] += p.r; sg[c] += p.g; sb[c] += p.b;
                total++;
            }

            int bestChroma = -1, bestNeutral = -1;
            for (int c = 0; c <= Brown; c++) if (bestChroma < 0 || n[c] > n[bestChroma]) bestChroma = c;
            for (int c = White; c <= Black; c++) if (bestNeutral < 0 || n[c] > n[bestNeutral]) bestNeutral = c;

            float chromaShare = n[bestChroma] / (float)total;
            float neutralShare = n[bestNeutral] / (float)total;

            int win; bool ok;
            if (chromaShare > 0f && chromaShare >= minShare) { win = bestChroma; ok = true; }
            else if (neutralShare > 0f && neutralShare >= minShare) { win = bestNeutral; ok = true; }
            else { win = chromaShare >= neutralShare ? bestChroma : bestNeutral; ok = false; }

            float cnt = Mathf.Max(1, n[win]) * 255f;
            var avg = new Color(sr[win] / cnt, sg[win] / cnt, sb[win] / cnt, 1f);
            result = new DetectedColor
            {
                key = Keys[win],
                name = Names[win],
                color = Display(win, avg),
                share = n[win] / (float)total
            };
            return ok;
        }

public static int ClassCount { get { return Count; } }

        public static string NameOf(int cls) { return Names[cls]; }

        /// <summary>Couleur représentative d'une classe (boutons de secours quand la caméra est absente).</summary>
        public static Color Sample(int cls)
        {
            if (cls < 12) return Color.HSVToRGB(cls * 30f / 360f, 0.85f, 0.9f);
            switch (cls)
            {
                case Brown: return new Color(0.43f, 0.25f, 0.12f);
                case White: return new Color(0.95f, 0.95f, 0.92f);
                case Gray: return new Color(0.5f, 0.5f, 0.5f);
                default: return new Color(0.04f, 0.04f, 0.05f);
            }
        }


        static int Classify(Color32 p)
        {
            Color.RGBToHSV(p, out float h, out float s, out float v);
            if (v < 0.15f) return Black;
            if (s < 0.2f) return v > 0.8f ? White : Gray;
            int bin = (int)((h * 360f + 15f) / 30f) % 12;
            if (bin == 0 && s < 0.5f && v > 0.75f) return 11;      // rouge clair = rose
            if ((bin == 1 || bin == 2) && v < 0.55f) return Brown;  // orange / jaune sombre = marron
            return bin;
        }

        /// <summary>Garde la vraie teinte, mais assure un objet lisible dans la salle (ni terne ni invisible).</summary>
        static Color Display(int cls, Color avg)
        {
            if (cls == White) return new Color(0.95f, 0.95f, 0.92f);
            if (cls == Black) return new Color(0.04f, 0.04f, 0.05f);
            Color.RGBToHSV(avg, out float h, out float s, out float v);
            if (cls == Gray) return Color.HSVToRGB(0f, 0f, Mathf.Clamp(v, 0.35f, 0.7f));
            if (cls == Brown) return Color.HSVToRGB(h, Mathf.Max(s, 0.5f), Mathf.Clamp(v, 0.25f, 0.5f));
            return Color.HSVToRGB(h, Mathf.Max(s, 0.55f), Mathf.Clamp(v, 0.45f, 1f));
        }
    }
}
