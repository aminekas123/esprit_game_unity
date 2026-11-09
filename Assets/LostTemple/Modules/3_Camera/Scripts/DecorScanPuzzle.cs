using System.Collections;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle de décoration : le joueur scanne librement des objets avec la caméra du téléphone ; la couleur
    /// dominante détectée (même détecteur que PhotoPuzzle) devient un objet 3D qui décore la salle.
    /// Il peut scanner autant de couleurs qu'il veut, puis termine pour ouvrir la porte.
    /// </summary>
    public class DecorScanPuzzle : PuzzleStep
    {
        public ColorObjectSpawner spawner;
        [Range(0.1f, 0.9f)] public float requiredShare = 0.3f;

        void Reset() { sensor = SensorKind.Camera; }

protected override void OnBegin() { Ask(null); }

// Aucune fenêtre à valider : le viseur s'ouvre directement, le résultat s'affiche dans son bandeau,
        // et le bouton « Terminer » du viseur ferme l'énigme quand le joueur est satisfait.
        void Ask(string last)
        {
            string msg = "Scannez un objet : sa <b>couleur</b> décorera la salle";
            if (!string.IsNullOrEmpty(last)) msg = "<color=#F2B847>" + last + "</color>\n" + msg;
            PhotoCameraUI.Instance.ShowFree(msg, OnPhoto, OnDone);
        }

void OnPhoto(Color32[] pixels)
        {
            if (!IsActive) return;
            if (ColorAnalyzer.Analyze(pixels, requiredShare, out DetectedColor found))
            {
                bool already = spawner != null && spawner.HasKey(found.key);
                if (!already)
                {
                    SensorStats.Photos.Add(found.name.ToLower());
                    ReportProgress();
                    Audio.Play(Sfx.Reveal);
                }
                if (spawner != null) spawner.SpawnForColor(found.key, found.name, found.color);
                int n = spawner != null ? spawner.SpawnedCount : 0;
                // Le viseur se referme le temps de voir l'objet apparaître, puis se rouvre tout seul.
                StartCoroutine(Reopen($"{(already ? "Déjà présent" : "Ajouté")} : {found.name}  ·  {n} objet(s) dans la salle", 1.6f));
            }
            else
            {
                Audio.Play(Sfx.Fail);
                StartCoroutine(Reopen($"Aucune couleur reconnue ({found.share * 100f:0} %). Approchez l'objet, avec assez de lumière.", 0.3f));
            }
        }

        // Le clavier de choix n'occupe que le bas de l'écran : la salle et ses décorations restent visibles.



    

void OnDone()
        {
            if (IsActive) Complete();
        }

        IEnumerator Reopen(string result, float delay)
        {
            yield return new WaitForSecondsRealtime(delay);
            if (IsActive) Ask(result);
        }

        void Awake() { skipTutorial = true; }
}
}
