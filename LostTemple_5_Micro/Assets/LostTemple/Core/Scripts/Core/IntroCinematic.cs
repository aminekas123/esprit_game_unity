using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Cinématique d'introduction (salle 1, nouvelle partie uniquement) : quelques cartes d'histoire,
    /// l'explorateur tombe dans le temple, puis une dalle se referme derrière lui.
    /// </summary>
    public class IntroCinematic : MonoBehaviour
    {
        public Transform slab;
        public Vector3 slabClosedPosition;
        public ParticleSystem landingDust;

        static readonly string[] cards =
        {
            "1923. Quelque part sous les sables…",
            "Une expédition cherche depuis des années l'artefact des six sens, caché dans un temple oublié.",
            "Vous êtes le seul à avoir trouvé l'entrée.\n\nMais le sol vient de céder sous vos pieds…",
        };

        IEnumerator Start()
        {
            var gm = GameManager.Instance;
            if (gm == null || !gm.PlayIntro)
            {
                slab.position = slabClosedPosition;
                yield break;
            }
            gm.PlayIntro = false;
            yield return null;
            var p = PlayerController.Instance;
            Vector3 land = p.transform.position;
            Quaternion rot = p.transform.rotation;
            p.Frozen = true;
            p.Teleport(land + Vector3.up * 12f, rot);

            yield return UIManager.Instance.Story(cards);

            // Chute
            float v = 0f;
            Vector3 pos = p.transform.position;
            while (pos.y > land.y)
            {
                v += 25f * Time.deltaTime;
                pos.y = Mathf.Max(land.y, pos.y - v * Time.deltaTime);
                p.Teleport(pos, rot);
                yield return null;
            }
            AudioManager.Instance.Play(Sfx.Mechanism);
            CameraRig.Shake(0.5f);
            if (landingDust != null) { landingDust.transform.position = land; landingDust.Play(); }
            yield return new WaitForSeconds(0.8f);

            // La dalle se referme derrière l'explorateur
            Vector3 a = slab.position;
            for (float t = 0; t < 1f; t += Time.deltaTime / 0.7f)
            {
                slab.position = Vector3.Lerp(a, slabClosedPosition, t * t);
                yield return null;
            }
            slab.position = slabClosedPosition;
            AudioManager.Instance.Play(Sfx.Mechanism);
            CameraRig.Shake(0.6f);
            UIManager.Instance.ShowToast("La dalle s'est refermée derrière vous… Il faut trouver une autre sortie.");
            p.Frozen = false;
        }
    }
}
