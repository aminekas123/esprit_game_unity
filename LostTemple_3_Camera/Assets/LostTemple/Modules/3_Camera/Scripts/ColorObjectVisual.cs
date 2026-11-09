using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Objet 3D généré par ColorObjectSpawner : prend la couleur détectée, apparaît avec une petite animation,
    /// puis flotte doucement dans la salle. Aucun collider : l'explorateur ne peut pas rester coincé.
    /// </summary>
    public class ColorObjectVisual : MonoBehaviour
    {
        public Renderer target;
        public PhotoTarget detected;
        public string colorKey;
        public Color color = Color.white;
        [Range(0f, 3f)] public float emission = 0.7f;
        public float spinSpeed = 30f;
        public float bobHeight = 0.06f;
        public float bobSpeed = 1.6f;

        Vector3 baseScale = Vector3.one;
        Vector3 basePos;
        float bornAt;
        float pulseAt = -10f;
        bool ready;

        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int ColorId = Shader.PropertyToID("_Color");
        static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        /// <summary>Applique la couleur (instance de matériau propre à cet objet) et lance l'apparition.</summary>
public void Init(PhotoTarget what, Color c, float scale)
        {
            detected = what;
            InitColor(what.ToString(), c, scale);
        }

/// <summary>Applique n'importe quelle couleur (instance de matériau propre à cet objet) et lance l'apparition.</summary>
        public void InitColor(string key, Color c, float scale)
        {
            colorKey = key;
            color = c;
            baseScale = Vector3.one * scale;
            basePos = transform.position;
            if (target == null) target = GetComponentInChildren<Renderer>();
            if (target != null)
            {
                var m = target.material;   // instance : ne modifie pas le matériau partagé du prefab
                if (m.HasProperty(BaseColorId)) m.SetColor(BaseColorId, c);
                if (m.HasProperty(ColorId)) m.SetColor(ColorId, c);
                m.EnableKeyword("_EMISSION");
                m.SetColor(EmissionId, c * emission);
            }
            var col = GetComponentInChildren<Collider>();
            if (col != null) Destroy(col);
            transform.localScale = Vector3.zero;
            bornAt = Time.time;
            ready = true;
        }


        /// <summary>Petit « battement » quand la même couleur est scannée à nouveau.</summary>
        public void Pulse() { pulseAt = Time.time; }

        void Update()
        {
            if (!ready) return;
            float age = Time.time - bornAt;
            // Apparition : grossit avec un léger dépassement, en montant depuis le sol
            float k = Mathf.Clamp01(age / 0.7f);
            float pop = k < 1f ? 1f + 0.35f * Mathf.Sin(k * Mathf.PI) * (1f - k) : 1f;
            float s = Mathf.SmoothStep(0f, 1f, k) * pop;
            float punch = 1f + 0.25f * Mathf.Max(0f, 1f - (Time.time - pulseAt) / 0.4f);
            transform.localScale = baseScale * s * punch;
            float rise = (1f - Mathf.SmoothStep(0f, 1f, k)) * -0.6f;
            transform.position = basePos + Vector3.up * (rise + Mathf.Sin(Time.time * bobSpeed + basePos.x) * bobHeight);
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
