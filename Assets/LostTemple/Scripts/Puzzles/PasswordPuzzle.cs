using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle finale b — Répéter au Coyote son mot de passe (les fragments obtenus avec les photos),
    /// parmi des syllabes pièges. Une erreur remet la saisie à zéro. En récompense, le Coyote donne la clé.
    /// </summary>
    public class PasswordPuzzle : PuzzleStep
    {
        public CoyoteSpirit coyote;
        public PhotoPuzzle photos;
        public string[] decoys = { "MA", "RU", "SI" };

        string[] choices;
        int entered;

        public override float Progress => IsActive ? entered / (float)photos.fragments.Length : -1f;

        protected override void OnBegin()
        {
            var list = new System.Collections.Generic.List<string>(photos.fragments);
            list.AddRange(decoys);
            for (int i = list.Count - 1; i > 0; i--) { int k = Random.Range(0, i + 1); (list[i], list[k]) = (list[k], list[i]); }
            choices = list.ToArray();
            entered = 0;
            UIManager.Instance.ShowChoicePad("Touchez les syllabes du mot de passe dans l'ordre", choices, photos.fragments.Length, OnPick);
        }

        void OnPick(int i)
        {
            if (!IsActive) return;
            if (choices[i] == photos.fragments[entered])
            {
                entered++;
                ReportProgress();
                Audio.Play(Sfx.Tick);
                UIManager.Instance.UpdateChoicePad(photos.fragments, entered);
                if (entered >= photos.fragments.Length)
                {
                    UIManager.Instance.HideChoicePad();
                    StartCoroutine(Reward());
                }
            }
            else
            {
                entered = 0;
                Audio.Play(Sfx.Fail);
                UIManager.Instance.UpdateChoicePad(photos.fragments, 0);
                UIManager.Instance.ShowToast("Ce n'est pas le bon ordre… recommencez.");
            }
        }

        IEnumerator Reward()
        {
            bool closed = false;
            coyote.Say("Tu as bien appris, petit explorateur.\n\nVoici la <b>clé dorée</b>. Elle ouvre la dernière porte… et l'artefact t'attend derrière.", "Prendre la clé", () => closed = true);
            while (!closed) yield return null;
            var p = PlayerController.Instance;
            if (p != null) yield return coyote.GiveKey(p.transform);
            Audio.Play(Sfx.Success);
            UIManager.Instance.ShowToast("Vous avez la clé dorée !");
            Complete();
        }
    }
}
