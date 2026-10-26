using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LostTemple
{
    public enum GameState { Menu, Playing, Paused, Victory }

    /// <summary>
    /// État global du jeu : menu, partie, pause, victoire. Crée les autres managers au démarrage,
    /// charge les salles et garde la progression (sauvegarde locale PlayerPrefs, aucun serveur).
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public const string MenuScene = "MainMenu";
        /// <summary>Une salle par capteur, puis la salle du Coyote (caméra).</summary>
        public static readonly string[] RoomScenes =
            { "Room1_Entree", "Room2_Boussole", "Room3_Statues", "Room4_Monstre", "Room5_Runes", "Room6_Lumiere", "Room7_Coyote" };
        public static readonly string[] RoomTitles =
        {
            "Salle 1 — Le pont suspendu", "Salle 2 — Le labyrinthe", "Salle 3 — Les statues", "Salle 4 — Le monstre",
            "Salle 5 — La cachette", "Salle 6 — Le pont invisible", "Salle finale — L'esprit Coyote"
        };

        /// <summary>Les six symboles du temple : nom affiché et couleur.</summary>
        public static readonly string[] GlyphNames = { "Soleil", "Lune", "Serpent", "Œil", "Étoile", "Plume" };
        public static readonly Color[] GlyphColors =
        {
            new Color(1f, 0.78f, 0.25f), new Color(0.6f, 0.75f, 1f), new Color(0.35f, 0.9f, 0.5f),
            new Color(0.95f, 0.4f, 0.35f), new Color(1f, 1f, 0.85f), new Color(0.8f, 0.55f, 1f)
        };

        public GameState State { get; private set; } = GameState.Menu;
        public int CurrentRoom { get; private set; } = -1;
        public int UnlockedRoom => PlayerPrefs.GetInt("LT_Room", 0);

        // Statistiques de la partie en cours (affichées sur l'écran de victoire)
        public float RunStartTime { get; private set; }
        public int Hints { get; private set; }
        public int Falls { get; private set; }
        public int Relics { get; private set; }
        public int Secrets { get; private set; }
        public void AddHint() => Hints++;
        public void AddFall() => Falls++;
        public void AddSecret() => Secrets++;

        // Modes de jeu
        /// <summary>Mode difficile : pièges plus rapides, 1 seule vie au départ, pas d'indices.</summary>
        public bool HardMode { get; private set; }
        /// <summary>Mode démo (soutenance) : on saute l'exploration, pas de dégâts, bouton « Salle suivante ».</summary>
        public bool DemoMode { get; private set; }
        /// <summary>Vrai juste après « Jouer » : la salle 1 joue la cinématique d'introduction.</summary>
        public bool PlayIntro { get; set; }
        /// <summary>Multiplicateur de vitesse des pièges, ennemis et rocher.</summary>
        public float HazardSpeed => HardMode ? 1.4f : 1f;

        /// <summary>Nombre total de reliques ramassées depuis toujours (débloque les costumes).</summary>
        public int TotalRelicsEver => PlayerPrefs.GetInt("LT_TotalRelics", 0);
        /// <summary>Pages du journal débloquées (une par salle terminée).</summary>
        public int JournalPages => PlayerPrefs.GetInt("LT_Journal", 0);

        public float BestTime(int room) => PlayerPrefs.GetFloat("LT_Best" + room, 0f);

        /// <summary>Enregistre le temps d'une salle ; renvoie vrai si c'est un nouveau record.</summary>
        public bool SubmitTime(int room, float seconds)
        {
            PlayerPrefs.SetInt("LT_Journal", Mathf.Max(JournalPages, room + 1));
            if (DemoMode) return false;
            float best = BestTime(room);
            bool record = best <= 0f || seconds < best;
            if (record) PlayerPrefs.SetFloat("LT_Best" + room, seconds);
            PlayerPrefs.Save();
            return record;
        }

        // Vies : chaque relique ramassée donne une vie, chaque piège en retire une.
        public const int StartLives = 3;
        public const int MaxLives = 9;
        public int Lives { get; private set; } = StartLives;
        public int StartLivesNow => HardMode ? 1 : StartLives;

        public void AddRelic()
        {
            Relics++;
            Lives = Mathf.Min(MaxLives, Lives + 1);
            PlayerPrefs.SetInt("LT_TotalRelics", TotalRelicsEver + 1);
        }

        /// <summary>Retire une vie. Renvoie false s'il n'en reste plus (la salle recommence).</summary>
        public bool LoseLife()
        {
            Lives = Mathf.Max(0, Lives - 1);
            return Lives > 0;
        }

        /// <summary>Après une salle perdue, on repart avec les vies de départ.</summary>
        public void RefillLives() => Lives = StartLivesNow;

        /// <summary>1 à 3 étoiles : +1 en finissant avec au moins 5 vies, +1 avec 3 indices ou moins.</summary>
        public int Stars => Mathf.Min(3, 1 + (Lives >= 5 || (HardMode && Lives >= 1) ? 1 : 0) + (Hints <= 3 ? 1 : 0));

        public event Action<GameState> StateChanged;
        public event Action<int> RoomLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Bootstrap()
        {
            if (Instance != null) return;
            var go = new GameObject("[LostTemple]");
            DontDestroyOnLoad(go);
            go.AddComponent<SensorManager>();
            go.AddComponent<AudioManager>();
            go.AddComponent<GameManager>();
            go.AddComponent<UIManager>();
            go.AddComponent<AmbientLightSync>();
        }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        bool sceneHandled;

        // La toute première scène est déjà chargée quand le jeu démarre : sceneLoaded n'est pas
        // forcément appelé pour elle, d'où ce rattrapage (sinon le menu ou le HUD restent cachés).
        void Start()
        {
            if (!sceneHandled) OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            sceneHandled = true;
            int idx = Array.IndexOf(RoomScenes, scene.name);
            CurrentRoom = idx;
            Time.timeScale = 1f;
            SetState(idx >= 0 ? GameState.Playing : GameState.Menu);
            if (idx >= 0) RoomLoaded?.Invoke(idx);
            AudioManager.Instance.SetRoomMusic(idx);
        }

        void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }

        // ------------------------------------------------------------------ Navigation

        public void NewGame() => NewGame(false);

        public void NewGame(bool hard)
        {
            HardMode = hard;
            DemoMode = false;
            PlayIntro = true;
            ResetStats();
            PlayerPrefs.SetInt("LT_Room", 0);
            PlayerPrefs.Save();
            LoadRoom(0);
        }

        /// <summary>Mode démo pour la soutenance : enchaîne le meilleur moment de chaque capteur.</summary>
        public void StartDemo()
        {
            HardMode = false;
            DemoMode = true;
            PlayIntro = false;
            ResetStats();
            Lives = MaxLives;
            LoadRoom(0);
        }

        public void ContinueGame()
        {
            HardMode = false;
            DemoMode = false;
            PlayIntro = false;
            ResetStats();
            LoadRoom(Mathf.Clamp(UnlockedRoom, 0, RoomScenes.Length - 1));
        }

        void ResetStats()
        {
            RunStartTime = Time.time;
            Hints = 0;
            Falls = 0;
            Relics = 0;
            Secrets = 0;
            Lives = StartLivesNow;
            SensorStats.Reset();
        }

        public void LoadRoom(int index)
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(RoomScenes[index]);
        }

        public void RestartRoom() { if (CurrentRoom >= 0) LoadRoom(CurrentRoom); }

        /// <summary>Appelé par le RoomManager quand la salle est terminée et que le joueur continue.</summary>
        public void NextRoom()
        {
            int next = CurrentRoom + 1;
            if (next >= RoomScenes.Length) { Victory(); return; }
            PlayerPrefs.SetInt("LT_Room", Mathf.Max(UnlockedRoom, next));
            PlayerPrefs.Save();
            LoadRoom(next);
        }

        public void Pause()
        {
            if (State != GameState.Playing) return;
            Time.timeScale = 0f;
            SetState(GameState.Paused);
        }

        public void Resume()
        {
            if (State != GameState.Paused) return;
            Time.timeScale = 1f;
            SetState(GameState.Playing);
        }

        public void Victory()
        {
            PlayerPrefs.SetInt("LT_Room", 0);
            PlayerPrefs.Save();
            SetState(GameState.Victory);
        }

        public void GoToMenu()
        {
            Time.timeScale = 1f;
            SensorManager.Instance.StopMicrophone();
            SceneManager.LoadScene(MenuScene);
        }

        public void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
