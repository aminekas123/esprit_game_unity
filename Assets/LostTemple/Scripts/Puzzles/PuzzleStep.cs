using System;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Une étape d'énigme d'une salle. Le RoomManager active les étapes l'une après l'autre.
    /// Boucle de jeu : observer l'indice → utiliser le capteur → retour visuel/sonore → valider.
    /// </summary>
    public abstract class PuzzleStep : MonoBehaviour
    {
        [TextArea] public string objective = "Objectif";
        [TextArea] public string tutorial = "Explication du geste.";
        [TextArea] public string hint = "Indice.";
        public SensorKind sensor;
        [Tooltip("Décocher pour une étape purement tactile (pas de capteur ni de commande alternative).")]
        public bool usesSensor = true;

        public bool IsActive { get; private set; }
        public bool IsDone { get; private set; }

        /// <summary>Progression 0..1 affichée dans le HUD (optionnelle, -1 = pas de jauge).</summary>
        public virtual float Progress => -1f;

        public event Action<PuzzleStep> Completed;
        public event Action Progressed;

        protected SensorManager Sensors => SensorManager.Instance;
        protected AudioManager Audio => AudioManager.Instance;

        public void Begin()
        {
            IsActive = true;
            OnBegin();
        }

        protected virtual void OnBegin() { }

        /// <summary>À appeler quand le joueur avance : remet à zéro le délai avant l'indice.</summary>
        protected void ReportProgress() => Progressed?.Invoke();

        protected void Complete()
        {
            if (IsDone) return;
            IsDone = true;
            IsActive = false;
            Audio.Play(Sfx.Success);
            Audio.SetLoopIntensity(0f);
            OnComplete();
            Completed?.Invoke(this);
        }

        protected virtual void OnComplete() { }

        // Utilitaire : allume/éteint un objet émissif (symboles, runes, yeux…)
        protected static void SetGlow(Renderer r, Color c, float intensity)
        {
            if (r == null) return;
            var m = r.material;
            m.EnableKeyword("_EMISSION");
            m.SetColor("_EmissionColor", c * intensity);
        }
    }
}
