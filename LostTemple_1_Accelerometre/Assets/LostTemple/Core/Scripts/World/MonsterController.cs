using System.Collections;
using UnityEngine;

namespace LostTemple
{
    public enum MonsterState { Sleeping, Waking, Awake, Dying, Dead }

    /// <summary>
    /// Le monstre de la salle du micro, animé par code : il dort recroquevillé, se réveille en rugissant
    /// quand le joueur fait du bruit, repousse la boule d'énergie, puis s'effondre en poussière quand elle le touche.
    /// </summary>
    public class MonsterController : MonoBehaviour
    {
        public Transform body, head, jaw, armL, armR;
        public Renderer[] eyes;
        public Light eyeLight;
        public ParticleSystem deathFx;

        public MonsterState State { get; private set; } = MonsterState.Sleeping;

        float wake01, push01;
        Vector3 bodyBase, scaleBase;
        Quaternion headBase, jawBase, armLBase, armRBase;
        float rise;   // 0 = recroquevillé, 1 = debout
        float roar;   // ouverture de la mâchoire
        float stun;   // 0..1 : ébloui, tête basse
        float slamL, slamR; // bras levé (0..1) pour une attaque

        void Start()
        {
            bodyBase = body.localPosition;
            scaleBase = transform.localScale;
            headBase = head.localRotation;
            jawBase = jaw.localRotation;
            armLBase = armL.localRotation;
            armRBase = armR.localRotation;
            SetEyes(0.05f);
        }

        /// <summary>Pendant le réveil : le monstre remue dans son sommeil (0..1).</summary>
        public void SetWakeProgress(float v) => wake01 = Mathf.Clamp01(v);

        /// <summary>Effort des bras quand il repousse la boule (0..1).</summary>
        public void SetPush(float v) => push01 = Mathf.Clamp01(v);

        public void Awaken()
        {
            if (State != MonsterState.Sleeping) return;
            StartCoroutine(WakeUp());
        }

        /// <summary>Ébloui par la lumière : yeux éteints, tête basse.</summary>
        public void SetStun(float v) => stun = Mathf.Clamp01(v);

        /// <summary>Lève le bras du côté donné (-1 gauche, +1 droite, vu par le joueur) puis frappe le sol.</summary>
        public IEnumerator Slam(int side, float windup)
        {
            for (float t = 0; t < 1f; t += Time.deltaTime / windup)
            {
                if (side < 0) slamL = t; else slamR = t;
                yield return null;
            }
            AudioManager.Instance.Play(Sfx.Mechanism, 0.9f);
            CameraRig.Shake(0.35f);
            for (float t = 1f; t > 0f; t -= Time.deltaTime / 0.25f)
            {
                if (side < 0) slamL = -t * 0.6f; else slamR = -t * 0.6f;
                yield return null;
            }
            slamL = slamR = 0f;
        }

        /// <summary>Rugissement à la demande (boss).</summary>
        public void RoarNow() { StartCoroutine(Roar()); }

        public void Die()
        {
            if (State == MonsterState.Dying || State == MonsterState.Dead) return;
            StopAllCoroutines();
            StartCoroutine(Death());
        }

        IEnumerator WakeUp()
        {
            State = MonsterState.Waking;
            for (float t = 0; t < 1f; t += Time.deltaTime / 1.6f)
            {
                rise = Mathf.SmoothStep(0, 1, t);
                yield return null;
            }
            rise = 1f;
            yield return Roar();
            State = MonsterState.Awake;
        }

        IEnumerator Roar()
        {
            AudioManager.Instance.Play(Sfx.Roar);
            CameraRig.Shake(0.35f);
            for (float t = 0; t < 1.4f; t += Time.deltaTime)
            {
                roar = Mathf.Sin(Mathf.Clamp01(t / 1.4f) * Mathf.PI);
                yield return null;
            }
            roar = 0f;
        }

        IEnumerator Death()
        {
            State = MonsterState.Dying;
            AudioManager.Instance.Play(Sfx.Roar, 0.8f);
            CameraRig.Shake(0.5f);
            if (deathFx != null) deathFx.Play();
            Vector3 start = transform.localPosition;
            for (float t = 0; t < 1f; t += Time.deltaTime / 2.2f)
            {
                roar = 1f;
                transform.localPosition = start + Random.insideUnitSphere * 0.08f * (1 - t) + Vector3.down * t * 1.5f;
                transform.localScale = scaleBase * Mathf.Lerp(1f, 0.05f, t * t);
                SetEyes(Mathf.Lerp(6f, 0f, t));
                yield return null;
            }
            State = MonsterState.Dead;
            foreach (var r in GetComponentsInChildren<Renderer>()) if (!(r is ParticleSystemRenderer)) r.enabled = false;
        }

        void Update()
        {
            if (State == MonsterState.Dying || State == MonsterState.Dead) return;
            float t = Time.time;

            // Pose : recroquevillé (endormi) → debout (réveillé)
            float breathe = Mathf.Sin(t * (State == MonsterState.Sleeping ? 1.2f : 2.5f));
            float stir = State == MonsterState.Sleeping ? wake01 * Mathf.Sin(t * 9f) * 0.03f : 0f;
            body.localPosition = bodyBase + Vector3.down * (1f - rise) * 0.9f + Vector3.up * breathe * 0.03f + Vector3.right * stir;
            body.localRotation = Quaternion.Euler(Mathf.Lerp(35f, 8f, rise) + push01 * 12f, Mathf.Sin(t * 0.7f) * 4f * rise, 0f);

            float lookUp = State == MonsterState.Sleeping ? 30f : -5f - roar * 20f + stun * 40f;
            head.localRotation = headBase * Quaternion.Euler(lookUp + Mathf.Sin(t * 1.3f) * 3f, Mathf.Sin(t * 0.9f) * 8f * rise, 0f);

            // Mâchoire : rugissement, claquements quand il pousse
            float snap = State == MonsterState.Awake ? Mathf.Max(0f, Mathf.Sin(t * 6f)) * 0.3f * push01 : 0f;
            jaw.localRotation = jawBase * Quaternion.Euler(Mathf.Max(roar, snap) * 40f, 0f, 0f);

            // Bras : pendants quand il dort, tendus vers l'avant quand il repousse la boule
            float reach = rise * (0.25f + push01 * 0.75f);
            float swing = Mathf.Sin(t * 2f) * 10f * rise;
            // Le monstre est tourné de 180° : son bras gauche est à droite du joueur.
            armL.localRotation = armLBase * Quaternion.Euler(-reach * 70f + swing - Mathf.Max(0f, slamR) * 160f + Mathf.Min(0f, slamR) * -60f, 0f, 0f);
            armR.localRotation = armRBase * Quaternion.Euler(-reach * 70f - swing - Mathf.Max(0f, slamL) * 160f + Mathf.Min(0f, slamL) * -60f, 0f, 0f);

            float eye = State == MonsterState.Sleeping ? 0.05f + wake01 * 2.5f : (3f + Mathf.Sin(t * 5f) * 0.8f + push01 * 2f) * (1f - stun * 0.9f);
            SetEyes(eye);
        }

        void SetEyes(float intensity)
        {
            foreach (var e in eyes)
            {
                var m = e.material;
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", new Color(1f, 0.15f, 0.05f) * intensity);
            }
            if (eyeLight != null) eyeLight.intensity = intensity * 0.6f;
        }
    }
}
