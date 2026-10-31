using UnityEngine;

namespace LostTemple
{
    /// <summary>Réglages partagés, sauvegardés localement (PlayerPrefs).</summary>
    public static class GameSettings
    {
        /// <summary>La lumière réelle de la pièce règle l'ambiance du temple (module Luminosité).</summary>
        public static bool LightSync
        {
            get => PlayerPrefs.GetInt("LT_LightSync", 1) == 1;
            set => PlayerPrefs.SetInt("LT_LightSync", value ? 1 : 0);
        }
    }
}
