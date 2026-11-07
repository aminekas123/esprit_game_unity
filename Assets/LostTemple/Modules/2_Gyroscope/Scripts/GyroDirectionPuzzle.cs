using UnityEngine;

namespace LostTemple
{
    /// <summary>
    /// Salle 3c — Gyroscope : « Jacques a dit ». La tablette affiche une direction (◀ GAUCHE ou DROITE ▶) ;
    /// le joueur tourne le téléphone comme un volant dans ce sens. Tourner dans le mauvais sens, ou trop
    /// lentement, fait perdre (une vie en moins, puis on recommence la suite).
    /// Entre deux directions, il faut revenir au centre et rester immobile un court instant.
    /// </summary>
    public class GyroDirectionPuzzle : PuzzleStep
    {
        public TextMesh arrowText;
        public Renderer tablet;
        public Renderer[] lamps;
        public int commands = 6;
        [Tooltip("Angle (degrés) à atteindre en tournant le téléphone pour valider une direction.")]
        public float turnAngle = 35f;
        [Tooltip("Temps (s) pour réagir à chaque direction.")]
        public float timeLimit = 4f;

        enum Phase { Wait, Return, Failed }

        int[] sequence;      // -1 = gauche, +1 = droite
        int index;
        float angle;         // rotation cumulée depuis l'affichage de la direction (gyroscope intégré)
        float timer;
        float still;
        Phase phase = Phase.Failed;
        HudGauge timeGauge;

        void Reset() { sensor = SensorKind.Gyroscope; }

        public override float Progress => IsActive && sequence != null ? index / (float)commands : -1f;

        float Limit => timeLimit * (GameManager.Instance.HardMode ? 0.7f : 1f);

        protected override void OnBegin()
        {
            timeGauge = new HudGauge
            {
                label = "Temps pour tourner",
                color = new Color(1f, 0.75f, 0.3f),
                value = () => timer / Limit,
                visible = () => IsActive && phase == Phase.Wait,
            };
            HudGauge.Register(timeGauge);
            StartSequence();
        }

        /// <summary>Tire une nouvelle suite de directions (jamais 3 fois la même d'affilée) et lance la première.</summary>
        void StartSequence()
        {
            sequence = new int[commands];
            for (int i = 0; i < commands; i++)
            {
                int d = Random.value < 0.5f ? -1 : 1;
                if (i >= 2 && sequence[i - 1] == d && sequence[i - 2] == d) d = -d;
                sequence[i] = d;
            }
            index = 0;
            foreach (var l in lamps) SetGlow(l, Color.black, 0f);
            NextCommand();
        }

        void NextCommand()
        {
            angle = 0f;
            timer = Limit;
            phase = Phase.Wait;
            bool left = sequence[index] < 0;
            arrowText.text = left ? "◀ GAUCHE" : "DROITE ▶";
            arrowText.color = new Color(1f, 0.8f, 0.35f);
            SetGlow(tablet, new Color(1f, 0.6f, 0.2f), 0.6f);
            objective = $"Tournez le téléphone vers la {(left ? "GAUCHE" : "DROITE")} ({index + 1}/{commands})";
            UIManager.Instance.SetStep(this);
            Audio.Play(Sfx.Tick);
        }

        void Update()
        {
            if (!IsActive || phase == Phase.Failed) return;
            float rate = Sensors.GetRotationRate();   // degrés/s, positif = sens horaire = vers la droite

            if (phase == Phase.Wait)
            {
                angle += rate * Time.deltaTime;
                timer -= Time.deltaTime;
                int dir = sequence[index];
                if (angle * dir >= turnAngle) Success();
                else if (angle * dir <= -turnAngle) Fail("Mauvais sens !");
                else if (timer <= 0f) Fail("Trop lent !");
            }
            else if (phase == Phase.Return)
            {
                // Le joueur revient au centre : on attend qu'il soit immobile pour ne pas confondre ce retour avec la direction suivante
                still = Mathf.Abs(rate) < 20f ? still + Time.deltaTime : 0f;
                if (still >= 0.4f) NextCommand();
            }
        }

        void Success()
        {
            if (index < lamps.Length) SetGlow(lamps[index], new Color(0.4f, 1f, 0.5f), 3f);
            index++;
            ReportProgress();
            if (index >= commands)
            {
                arrowText.text = "BRAVO !";
                arrowText.color = new Color(0.5f, 1f, 0.6f);
                HudGauge.Unregister(timeGauge);
                Complete();
                return;
            }
            Audio.Play(Sfx.Reveal, 0.6f);
            phase = Phase.Return;
            still = 0f;
            arrowText.text = "● CENTRE";
            arrowText.color = new Color(0.75f, 0.85f, 1f);
            SetGlow(tablet, new Color(0.3f, 0.6f, 1f), 0.4f);
        }

        /// <summary>Mauvais sens ou trop lent : une vie en moins, puis la suite recommence depuis le début.</summary>
        void Fail(string reason)
        {
            phase = Phase.Failed;
            string expected = sequence[index] < 0 ? "à GAUCHE" : "à DROITE";
            arrowText.text = "PERDU !";
            arrowText.color = new Color(1f, 0.3f, 0.25f);
            SetGlow(tablet, new Color(1f, 0.1f, 0.05f), 2f);
            Audio.Play(Sfx.Fail);
            CameraRig.Shake(0.3f);
            UIManager.Instance.FlashDamage();

            var gm = GameManager.Instance;
            gm.AddFall();
            if (!gm.DemoMode && !gm.LoseLife())
            {
                UIManager.Instance.ShowMessage("PERDU", $"{reason} Il fallait tourner {expected}.\n\nVous n'avez plus de vies : la salle recommence.", "Recommencer",
                    () => { gm.RefillLives(); gm.RestartRoom(); });
                return;
            }
            string lives = gm.DemoMode ? "" : $"\n\n−1 vie (il vous en reste {gm.Lives}).";
            UIManager.Instance.ShowMessage("PERDU !", $"{reason} Il fallait tourner {expected}.{lives}\n\nLa suite de directions recommence.", "Réessayer", StartSequence);
        }

        void OnDestroy() { if (timeGauge != null) HudGauge.Unregister(timeGauge); }
    }
}
