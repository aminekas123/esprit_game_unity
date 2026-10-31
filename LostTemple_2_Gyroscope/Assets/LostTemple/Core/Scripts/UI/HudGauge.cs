using System;
using System.Collections.Generic;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Jauge (et bouton optionnel) qu'un module peut afficher dans le HUD sans que le Core le connaisse :
    /// bruit de la zone de silence, choix du miroir, etc. Le module s'enregistre puis se désinscrit.
    /// </summary>
    public class HudGauge
    {
        public string label;
        public Color color = Color.white;
        public Func<float> value;
        public Func<bool> visible;
        public string buttonLabel;
        public Action onButton;
        public Func<bool> buttonVisible;

        public static readonly List<HudGauge> All = new List<HudGauge>();
        public static void Register(HudGauge g) { if (!All.Contains(g)) All.Add(g); }
        public static void Unregister(HudGauge g) { All.Remove(g); }
    }

    /// <summary>Un module qui doit réagir quand toutes les énigmes de la salle sont résolues (ex. le rocher).</summary>
    public interface IRoomDoneListener
    {
        void OnRoomDone();
    }
}
