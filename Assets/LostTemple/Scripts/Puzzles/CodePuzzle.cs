using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle finale b — Réutiliser les indices : toucher les quatre symboles découverts dans l'ordre des salles
    /// pour libérer l'artefact. Une erreur remet la séquence à zéro.
    /// </summary>
    public class CodePuzzle : PuzzleStep
    {
        public Transform artefact;
        public Renderer artefactRenderer;
        public Light artefactLight;

        int entered;

        public override float Progress => IsActive ? entered / 4f : -1f;

        protected override void OnBegin()
        {
            entered = 0;
            UIManager.Instance.ShowCodePad(OnPick);
        }

        void Update()
        {
            if (artefact != null) artefact.Rotate(0f, 40f * Time.deltaTime, 0f, Space.World);
        }

        void OnPick(int glyph)
        {
            if (!IsActive) return;
            if (GameManager.Instance.Code[entered] == glyph)
            {
                entered++;
                ReportProgress();
                Audio.Play(Sfx.Tick);
                UIManager.Instance.UpdateCodePad(entered);
                if (entered >= 4)
                {
                    UIManager.Instance.HideCodePad();
                    Complete();
                }
            }
            else
            {
                entered = 0;
                Audio.Play(Sfx.Fail);
                UIManager.Instance.UpdateCodePad(0);
                UIManager.Instance.ShowToast("Mauvais ordre… le mécanisme se réinitialise.");
            }
        }

        protected override void OnComplete() { StartCoroutine(Release()); }

        IEnumerator Release()
        {
            Audio.Play(Sfx.Mechanism);
            Vector3 a = artefact.position, b = a + Vector3.up * 1.2f;
            for (float t = 0; t < 1f; t += Time.deltaTime / 2f)
            {
                artefact.position = Vector3.Lerp(a, b, Mathf.SmoothStep(0, 1, t));
                SetGlow(artefactRenderer, new Color(1f, 0.75f, 0.3f), 1f + t * 6f);
                if (artefactLight != null) artefactLight.intensity = 2f + t * 10f;
                yield return null;
            }
            Audio.Play(Sfx.Victory);
            yield return new WaitForSeconds(0.8f);
            GameManager.Instance.Victory();
        }
    }
}
