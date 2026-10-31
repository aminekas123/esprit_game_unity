using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Costumes de l'explorateur, débloqués avec le nombre total de reliques ramassées (toutes parties confondues).
    /// Le choix se fait dans les paramètres.
    /// </summary>
    public class PlayerCostume : MonoBehaviour
    {
        public struct Costume
        {
            public string name; public Color shirt, hat, pants; public int unlock;
            public Costume(string n, Color s, Color h, Color p, int u) { name = n; shirt = s; hat = h; pants = p; unlock = u; }
        }

        public static readonly Costume[] All =
        {
            new Costume("Classique", new Color(0.82f, 0.74f, 0.55f), new Color(0.4f, 0.24f, 0.12f), new Color(0.36f, 0.32f, 0.22f), 0),
            new Costume("Aventurière rouge", new Color(0.75f, 0.2f, 0.15f), new Color(0.2f, 0.12f, 0.08f), new Color(0.25f, 0.22f, 0.2f), 10),
            new Costume("Explorateur de jungle", new Color(0.35f, 0.5f, 0.25f), new Color(0.55f, 0.45f, 0.25f), new Color(0.3f, 0.28f, 0.18f), 20),
            new Costume("Ombre du temple", new Color(0.12f, 0.12f, 0.16f), new Color(0.05f, 0.05f, 0.07f), new Color(0.1f, 0.1f, 0.12f), 30),
            new Costume("Pharaon doré", new Color(1f, 0.8f, 0.3f), new Color(0.15f, 0.25f, 0.6f), new Color(0.95f, 0.9f, 0.75f), 45),
        };

        public static int Selected
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt("LT_Costume", 0), 0, All.Length - 1);
            set => PlayerPrefs.SetInt("LT_Costume", value);
        }

        public static bool IsUnlocked(int i) => GameManager.Instance.TotalRelicsEver >= All[i].unlock;

        /// <summary>Passe au costume débloqué suivant.</summary>
        public static void Next()
        {
            int i = Selected;
            for (int k = 1; k <= All.Length; k++)
            {
                int n = (i + k) % All.Length;
                if (IsUnlocked(n)) { Selected = n; break; }
            }
            foreach (var c in FindObjectsByType<PlayerCostume>()) c.Apply();
        }

        void Start() { Apply(); }

        public void Apply()
        {
            var c = All[Selected];
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                switch (r.gameObject.name)
                {
                    case "Torso": case "Sleeve": r.material.SetColor("_BaseColor", c.shirt); break;
                    case "Hat Brim": case "Hat Crown": r.material.SetColor("_BaseColor", c.hat); break;
                    case "Thigh": r.material.SetColor("_BaseColor", c.pants); break;
                }
            }
        }
    }
}
