using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>Salle finale c — Insérer la clé dans la serrure : la clé tourne, puis la porte s'ouvre (RoomManager).</summary>
    public class KeyDoorPuzzle : PuzzleStep
    {
        public Transform keyInLock;
        public Renderer lockGlow;

        protected override void OnBegin() { StartCoroutine(Unlock()); }

        IEnumerator Unlock()
        {
            yield return new WaitForSeconds(0.3f);
            if (keyInLock != null)
            {
                keyInLock.gameObject.SetActive(true);
                Vector3 end = keyInLock.localPosition, start = end + new Vector3(0f, 0f, -0.6f);
                for (float t = 0; t < 1f; t += Time.deltaTime / 0.6f)
                {
                    keyInLock.localPosition = Vector3.Lerp(start, end, Mathf.SmoothStep(0, 1, t));
                    yield return null;
                }
                Audio.Play(Sfx.Tick);
                Quaternion r0 = keyInLock.localRotation;
                for (float t = 0; t < 1f; t += Time.deltaTime / 0.5f)
                {
                    keyInLock.localRotation = r0 * Quaternion.Euler(0f, 0f, 90f * Mathf.SmoothStep(0, 1, t));
                    yield return null;
                }
            }
            SetGlow(lockGlow, new Color(1f, 0.8f, 0.3f), 4f);
            Audio.Play(Sfx.Mechanism);
            yield return new WaitForSeconds(0.4f);
            Complete();
        }
    }
}
