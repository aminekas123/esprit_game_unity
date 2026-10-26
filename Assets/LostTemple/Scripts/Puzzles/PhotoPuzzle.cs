using UnityEngine;

namespace LostTemple
{
    public enum PhotoTarget { Red, Blue, Green, Yellow, Bright }

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

        int index;

        void Reset() { sensor = SensorKind.Camera; }

        public override float Progress => IsActive ? index / (float)targets.Length : -1f;

        protected override void OnBegin()
        {
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
                default: return new Color(0.98f, 0.98f, 0.95f);
            }
        }

        /// <summary>Part des pixels de la photo qui correspondent à la couleur demandée (0..1).</summary>
        public static float Share(Color32[] px, PhotoTarget t)
        {
            if (px == null || px.Length == 0) return 0f;
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
                    default: m = v > 0.85f && s < 0.35f; break;
                }
                if (m) ok++;
            }
            return ok / (float)px.Length;
        }
    }
}
