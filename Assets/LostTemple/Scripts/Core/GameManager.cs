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
        public static readonly string[] RoomScenes = { "Room1_Entree", "Room2_Labyrinthe", "Room3_Gardien", "Room4_Artefact" };
        public static readonly string[] RoomTitles = { "Salle 1 — L'entrée", "Salle 2 — Le labyrinthe", "Salle 3 — Le gardien", "Salle finale — L'artefact" };

        /// <summary>Les six symboles du temple : nom affiché et couleur.</summary>
        public static readonly string[] GlyphNames = { "Soleil", "Lune", "Serpent", "Œil", "Étoile", "Plume" };
        public static readonly Color[] GlyphColors =
        {
            new Color(1f, 0.78f, 0.25f), new Color(0.6f, 0.75f, 1f), new Color(0.35f, 0.9f, 0.5f),
            new Color(0.95f, 0.4f, 0.35f), new Color(1f, 1f, 0.85f), new Color(0.8f, 0.55f, 1f)
        };

        public GameState State { get; private set; } = GameState.Menu;
        public int CurrentRoom { get; private set; } = -1;
        /// <summary>Code de 4 symboles tiré au début de la partie ; chaque salle en révèle un.</summary>
        public int[] Code { get; private set; } = new int[4];
        public int UnlockedRoom => PlayerPrefs.GetInt("LT_Room", 0);

        // Statistiques de la partie en cours (affichées sur l'écran de victoire)
        public float RunStartTime { get; private set; }
        public int Hints { get; private set; }
        public int Falls { get; private set; }
        public int Relics { get; private set; }
        public const int RelicsInGame = 12;
        public void AddRelic() => Relics++;
        public void AddHint() => Hints++;
        public void AddFall() => Falls++;

        /// <summary>1 à 3 étoiles : +1 avec au moins 8 reliques sur 12, +1 avec peu d'indices et de chutes.</summary>
        public int Stars => 1 + (Relics >= RelicsInGame * 2 / 3 ? 1 : 0) + (Hints <= 3 && Falls <= 6 ? 1 : 0);

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
        }

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            LoadCode();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy() { SceneManager.sceneLoaded -= OnSceneLoaded; }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            int idx = Array.IndexOf(RoomScenes, scene.name);
            CurrentRoom = idx;
            Time.timeScale = 1f;
            SetState(idx >= 0 ? GameState.Playing : GameState.Menu);
            if (idx >= 0) RoomLoaded?.Invoke(idx);
        }

        void SetState(GameState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }

        // ------------------------------------------------------------------ Code des symboles

        void LoadCode()
        {
            string saved = PlayerPrefs.GetString("LT_Code", "");
            if (saved.Length == 4) { for (int i = 0; i < 4; i++) Code[i] = saved[i] - '0'; }
            else NewCode();
        }

        void NewCode()
        {
            var pool = new System.Collections.Generic.List<int> { 0, 1, 2, 3, 4, 5 };
            string s = "";
            for (int i = 0; i < 4; i++)
            {
                int k = UnityEngine.Random.Range(0, pool.Count);
                Code[i] = pool[k];
                pool.RemoveAt(k);
                s += Code[i];
            }
            PlayerPrefs.SetString("LT_Code", s);
        }

        // ------------------------------------------------------------------ Navigation

        public void NewGame()
        {
            NewCode();
            ResetStats();
            PlayerPrefs.SetInt("LT_Room", 0);
            PlayerPrefs.Save();
            LoadRoom(0);
        }

        public void ContinueGame()
        {
            ResetStats();
            LoadRoom(Mathf.Clamp(UnlockedRoom, 0, RoomScenes.Length - 1));
        }

        void ResetStats()
        {
            RunStartTime = Time.time;
            Hints = 0;
            Falls = 0;
            Relics = 0;
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
            if (next >= RoomScenes.Length) return;
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
