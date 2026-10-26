using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// L'esprit Coyote, gardien de la dernière salle : assis, il remue la queue, suit l'explorateur du regard
    /// et ouvre la gueule quand il parle. Il donne les missions photo et remet la clé.
    /// </summary>
    public class CoyoteSpirit : MonoBehaviour
    {
        public Transform head, jaw, tail;
        public Transform keyProp;
        public Renderer[] spiritParts;

        Quaternion headBase, jawBase, tailBase;
        float talkUntil;

        void Start()
        {
            headBase = head.localRotation;
            jawBase = jaw.localRotation;
            tailBase = tail.localRotation;
            if (keyProp != null) keyProp.gameObject.SetActive(false);
        }

        /// <summary>Affiche une réplique du Coyote et anime sa gueule.</summary>
        public void Say(string text, string button, System.Action onClose)
        {
            talkUntil = Time.time + Mathf.Clamp(text.Length * 0.04f, 1.2f, 4f);
            AudioManager.Instance.Play(Sfx.Reveal, 0.4f);
            UIManager.Instance.ShowMessage("Esprit Coyote", text, button, onClose);
        }

        /// <summary>La clé apparaît au-dessus du Coyote puis vole jusqu'à l'explorateur.</summary>
        public IEnumerator GiveKey(Transform to)
        {
            if (keyProp == null) yield break;
            keyProp.gameObject.SetActive(true);
            Vector3 a = head.position + Vector3.up * 0.8f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.8f)
            {
                keyProp.position = a + Vector3.up * t * 0.5f;
                keyProp.Rotate(0f, 360f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }
            Vector3 b = keyProp.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.9f)
            {
                Vector3 target = to.position + Vector3.up * 1.2f;
                keyProp.position = Vector3.Lerp(b, target, Mathf.SmoothStep(0, 1, t)) + Vector3.up * Mathf.Sin(t * Mathf.PI) * 1.2f;
                keyProp.Rotate(0f, 540f * Time.deltaTime, 0f, Space.World);
                yield return null;
            }
            keyProp.gameObject.SetActive(false);
        }

        void Update()
        {
            float t = Time.time;
            var p = PlayerController.Instance;
            float yaw = 0f;
            if (p != null)
            {
                Vector3 d = transform.InverseTransformPoint(p.transform.position);
                yaw = Mathf.Clamp(Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg, -50f, 50f);
            }
            head.localRotation = headBase * Quaternion.Euler(Mathf.Sin(t * 1.1f) * 3f, yaw, Mathf.Sin(t * 0.7f) * 6f);
            tail.localRotation = tailBase * Quaternion.Euler(0f, Mathf.Sin(t * 5f) * 25f, 0f);
            bool talking = Time.time < talkUntil;
            jaw.localRotation = jawBase * Quaternion.Euler(talking ? Mathf.Abs(Mathf.Sin(t * 14f)) * 25f : 0f, 0f, 0f);

            // Lueur d'esprit qui pulse
            float glow = 0.8f + Mathf.Sin(t * 2f) * 0.3f + (talking ? 0.6f : 0f);
            foreach (var r in spiritParts)
            {
                if (r == null) continue;
                var m = r.material;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(0.3f, 0.75f, 1f) * glow);
            }
        }
    }
}
