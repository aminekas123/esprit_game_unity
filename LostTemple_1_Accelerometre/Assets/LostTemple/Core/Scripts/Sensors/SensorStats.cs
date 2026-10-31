using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Ce que le joueur a fait avec ses capteurs pendant la partie, affiché sur l'écran « Vos capteurs »
    /// à la fin : montre au jury comment chaque capteur a été utilisé. Rien n'est envoyé ni enregistré ailleurs.
    /// </summary>
    public static class SensorStats
    {
        public static float MaxSoundDb;
        public static float MaxTiltDeg;
        public static float GyroTotalDeg;
        public static float CompassTotalDeg;
        public static float MinLux = -1f, MaxLux = -1f;
        public static int ProximityCovers;
        public static int Shakes;
        public static int Flips;
        public static readonly List<string> Photos = new List<string>();

        public static void Reset()
        {
            MaxSoundDb = MaxTiltDeg = GyroTotalDeg = CompassTotalDeg = 0f;
            MinLux = MaxLux = -1f;
            ProximityCovers = Shakes = Flips = 0;
            Photos.Clear();
        }

        public static void Lux(float lux)
        {
            if (lux < 0f) return;
            MinLux = MinLux < 0f ? lux : Mathf.Min(MinLux, lux);
            MaxLux = Mathf.Max(MaxLux, lux);
        }

        public static string Report()
        {
            var sb = new StringBuilder();
            void Line(string sensor, string value) => sb.Append("<color=#F2B847>").Append(sensor).Append("</color>  ").Append(value).Append('\n');
            Line("Accéléromètre", $"inclinaison maximale {MaxTiltDeg:0}° · {Shakes} secousse(s) · {Flips} retournement(s)");
            Line("Gyroscope", $"{GyroTotalDeg:0}° de rotation au total ({GyroTotalDeg / 360f:0.0} tours)");
            Line("Boussole", $"{CompassTotalDeg:0}° parcourus en tournant sur vous-même");
            Line("Microphone", MaxSoundDb > 0f ? $"cri le plus fort ≈ {MaxSoundDb:0} dB" : "non utilisé");
            Line("Proximité", $"capteur couvert {ProximityCovers} fois");
            Line("Luminosité", MaxLux >= 0f ? $"de {MinLux:0} à {MaxLux:0} lux" : "non disponible");
            Line("Caméra", Photos.Count > 0 ? $"{Photos.Count} photo(s) : {string.Join(", ", Photos)}" : "aucune photo");
            return sb.ToString();
        }
    }
}
