using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Ouverture spectaculaire d'une porte de pierre : grondement qui monte, secousses, nuage de poussière,
    /// gravats qui tombent, puis la porte s'enfonce dans le sol et une lumière dorée jaillit de l'ouverture.
    /// </summary>
    public class DoorFX : MonoBehaviour
    {
        public Transform door;
        public ParticleSystem dust;
        public ParticleSystem debris;
        public Light glow;
        public Renderer lightSpill;
        public float duration = 2.2f;

        public IEnumerator Open()
        {
            var audio = AudioManager.Instance;
            audio.Play(Sfx.Mechanism);
            audio.SetLoopIntensity(1f);

            // 1. Le mécanisme se déverrouille : petites secousses et poussière
            if (dust != null) dust.Play();
            for (float t = 0; t < 0.5f; t += Time.deltaTime)
            {
                CameraRig.Shake(0.05f + t * 0.1f);
                door.position += Random.insideUnitSphere * 0.01f;
                yield return null;
            }

            // 2. La porte s'enfonce, les gravats tombent, la lumière passe
            if (debris != null) debris.Play();
            audio.Play(Sfx.Mechanism, 0.8f);
            Vector3 a = door.position, b = a + Vector3.down * (door.lossyScale.y + 0.1f);
            for (float t = 0; t < 1f; t += Time.deltaTime / duration)
            {
                float k = Mathf.SmoothStep(0, 1, t);
                door.position = Vector3.Lerp(a, b, k) + Random.insideUnitSphere * 0.03f * (1 - t);
                CameraRig.Shake(0.12f * (1 - t));
                if (glow != null) glow.intensity = k * 6f;
                if (lightSpill != null)
                {
                    var m = lightSpill.material;
                    m.EnableKeyword("_EMISSION");
                    m.SetColor("_EmissionColor", new Color(1f, 0.75f, 0.4f) * (k * 3f));
                }
                audio.SetLoopIntensity(1f - t * 0.7f);
                yield return null;
            }
            door.position = b;
            audio.SetLoopIntensity(0f);
            audio.Play(Sfx.Reveal);
            CameraRig.Shake(0.25f);
            if (dust != null) dust.Emit(80);
        }
    }
}
