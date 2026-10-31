using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 2b — Boussole : la rose des vents demande de se tourner vers de VRAIS points cardinaux
    /// (Nord, Est, Sud, Ouest), dans un ordre tiré au hasard. Contrairement à l'aiguille magique,
    /// la cible est absolue : elle dépend du nord magnétique réel.
    /// </summary>
    public class CardinalPuzzle : PuzzleStep
    {
        [Tooltip("Repère « N » de la rose : il tourne pour montrer où se trouve le vrai nord.")]
        public Transform northMarker;
        [Tooltip("Lampes N, E, S, O (dans cet ordre).")]
        public Renderer[] lamps = new Renderer[4];
        public int count = 3;
        public float tolerance = 20f;
        public float holdTime = 1.5f;

        static readonly string[] names = { "le NORD", "l'EST", "le SUD", "l'OUEST" };

        int[] order;
        int index;
        float hold;

        void Reset() { sensor = SensorKind.Compass; }

        public override float Progress => IsActive ? (index + hold / holdTime) / count : -1f;

        protected override void OnBegin()
        {
            var pool = new System.Collections.Generic.List<int> { 0, 1, 2, 3 };
            order = new int[count];
            for (int i = 0; i < count; i++) { int k = Random.Range(0, pool.Count); order[i] = pool[k]; pool.RemoveAt(k); }
            index = 0;
            foreach (var l in lamps) SetGlow(l, Color.black, 0f);
            Announce();
        }

        void Announce()
        {
            objective = $"Tournez-vous vers {names[order[index]]} ({index + 1}/{count})";
            UIManager.Instance.SetStep(this);
            UIManager.Instance.ShowToast($"Tournez-vous vers {names[order[index]]} !");
            SetGlow(lamps[order[index]], new Color(0.4f, 0.7f, 1f), 1.2f);
        }

        void Update()
        {
            if (!IsActive) return;
            float heading = Sensors.GetHeading();
            if (northMarker != null)
                northMarker.localRotation = Quaternion.Slerp(northMarker.localRotation, Quaternion.Euler(0f, -heading, 0f), 1f - Mathf.Exp(-6f * Time.deltaTime));

            float target = order[index] * 90f;
            float error = Mathf.Abs(Mathf.DeltaAngle(heading, target));
            if (error < tolerance)
            {
                if (hold == 0f) Audio.Play(Sfx.Tick);
                hold += Time.deltaTime;
                ReportProgress();
                SetGlow(lamps[order[index]], new Color(1f, 0.8f, 0.3f), 1.5f + hold / holdTime * 3f);
                if (hold >= holdTime) Found();
            }
            else hold = Mathf.Max(0f, hold - Time.deltaTime * 2f);
        }

        void Found()
        {
            SetGlow(lamps[order[index]], new Color(1f, 0.8f, 0.3f), 4f);
            hold = 0f;
            index++;
            if (index >= count) { Complete(); return; }
            Audio.Play(Sfx.Reveal);
            Announce();
        }
    }
}
