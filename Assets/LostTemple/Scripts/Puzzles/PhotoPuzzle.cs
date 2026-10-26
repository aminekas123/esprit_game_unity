using UnityEngine;

namespace LostTemple
{
    public enum PhotoTarget { Red, Blue, Green, Yellow, Bright, Dark, Detailed }

    /// <summary>
    /// Salle finale a — Caméra : le Coyote demande de photographier, dans le monde réel, quelque chose
    /// d'une certaine couleur. Le jeu analyse le centre de la photo ; chaque photo réussie révèle
    /// un fragment de son mot de passe. Aucune photo n'est enregistrée ni envoyée.
    /// </summary>
    public class PhotoPuzzle : PuzzleStep
    {
        public CoyoteSpirit coyote;
        public PhotoTarget[] targets = { PhotoTarget.Red, PhotoTarget.Blue, PhotoTarget.Bright };
        [Tooltip("Fragment du mot de passe révélé après chaque photo (même ordre que targets).")]
        public string[] fragments = { "KA", "YO", "TÉ" };
        [Range(0.1f, 0.9f)] public float requiredShare = 0.3f;
        [Tooltip("Tirer 3 missions au hasard à chaque partie au lieu de la liste fixe.")]
        public bool randomize = true;

        int index;

        void Reset() { sensor = SensorKind.Camera; }

        public override float Progress => IsActive ? index / (float)targets.Length : -1f;

        protected override void OnBegin()
        {
            if (randomize)
            {
                var pool = new System.Collections.Generic.List<PhotoTarget>((PhotoTarget[])System.Enum.GetValues(typeof(PhotoTarget)));
                targets = new PhotoTarget[3];
                for (int i = 0; i < 3; i++) { int k = Random.Range(0, pool.Count); targets[i] = pool[k]; pool.RemoveAt(k); }
            }
            index = 0;
            Ask();
        }

        void Ask()
        {
            string what = Describe(targets[index]);
            UIManager.Instance.ShowCamera($"Photographiez quelque chose de <b>{what}</b>", targets[index], OnPhoto);
        }

        void OnPhoto(Color32[] pixels)
        {
            if (!IsActive) return;
            var target = targets[index];
            float share = Share(pixels, target);
            if (share >= requiredShare)
            {
                string frag = fragments[Mathf.Min(index, fragments.Length - 1)];
                SensorStats.Photos.Add(Describe(target).ToLower());
                index++;
                ReportProgress();
                Audio.Play(Sfx.Reveal);
                if (index >= targets.Length)
                {
                    coyote.Say($"Parfait ! Le dernier fragment est « <b>{frag}</b> ».\n\nTu connais maintenant mon mot de passe : <b>{string.Join("-", fragments)}</b>.\nRépète-le-moi dans l'ordre.", "Continuer", Complete);
                }
                else
                {
                    coyote.Say($"Bravo, c'est bien {Describe(target)} !\n\nFragment n°{index} de mon mot de passe : « <b>{frag}</b> ».\n\nMission suivante : quelque chose de <b>{Describe(targets[index])}</b>.", "Prendre la photo", Ask);
                }
            }
            else
            {
                Audio.Play(Sfx.Fail);
                coyote.Say($"Hmm… je ne vois pas assez de {Describe(target)} au centre de ta photo ({share * 100f:0} %).\n\nApproche-toi pour que l'objet remplisse le cadre, avec assez de lumière.", "Réessayer", Ask);
            }
        }

        public static string Describe(PhotoTarget t)
        {
            switch (t)
            {
                case PhotoTarget.Red: return "ROUGE";
                case PhotoTarget.Blue: return "BLEU";
                case PhotoTarget.Green: return "VERT";
                case PhotoTarget.Yellow: return "JAUNE";
                case PhotoTarget.Dark: return "TRÈS SOMBRE";
                case PhotoTarget.Detailed: return "PLEIN DE DÉTAILS (texte, motifs)";
                default: return "TRÈS LUMINEUX";
            }
        }

        public static Color Sample(PhotoTarget t)
        {
            switch (t)
            {
                case PhotoTarget.Red: return new Color(0.85f, 0.12f, 0.1f);
                case PhotoTarget.Blue: return new Color(0.12f, 0.3f, 0.9f);
                case PhotoTarget.Green: return new Color(0.15f, 0.75f, 0.2f);
                case PhotoTarget.Yellow: return new Color(0.95f, 0.85f, 0.1f);
                case PhotoTarget.Dark: return new Color(0.04f, 0.04f, 0.05f);
                case PhotoTarget.Detailed: return new Color(0.5f, 0.5f, 0.5f);
                default: return new Color(0.98f, 0.98f, 0.95f);
            }
        }

        /// <summary>Part des pixels de la photo qui correspondent à la couleur demandée (0..1).</summary>
        public static float Share(Color32[] px, PhotoTarget t)
        {
            if (px == null || px.Length == 0) return 0f;
            if (t == PhotoTarget.Detailed)
            {
                // Beaucoup de détails = forte variation de luminosité entre les pixels.
                float sum = 0f, sq = 0f;
                foreach (var p in px) { float l = (p.r * 0.3f + p.g * 0.59f + p.b * 0.11f) / 255f; sum += l; sq += l * l; }
                float mean = sum / px.Length, sd = Mathf.Sqrt(Mathf.Max(0f, sq / px.Length - mean * mean));
                return Mathf.Clamp01(sd / 0.18f * 0.3f);
            }
            int ok = 0;
            foreach (var p in px)
            {
                Color.RGBToHSV(p, out float h, out float s, out float v);
                bool m;
                switch (t)
                {
                    case PhotoTarget.Red: m = (h < 0.04f || h > 0.94f) && s > 0.4f && v > 0.2f; break;
                    case PhotoTarget.Blue: m = h > 0.53f && h < 0.72f && s > 0.35f && v > 0.15f; break;
                    case PhotoTarget.Green: m = h > 0.22f && h < 0.45f && s > 0.3f && v > 0.15f; break;
                    case PhotoTarget.Yellow: m = h > 0.11f && h < 0.19f && s > 0.4f && v > 0.4f; break;
                    case PhotoTarget.Dark: m = v < 0.15f; break;
                    default: m = v > 0.85f && s < 0.35f; break;
                }
                if (m) ok++;
            }
            return ok / (float)px.Length;
        }
    }
}
