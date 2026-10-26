using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using static LostTemple.UIStyle;

namespace LostTemple
{
    /// <summary>
    /// Toute l'interface, construite par code : menu principal, paramètres, diagnostic des capteurs,
    /// HUD (objectif, capteur actif, jauge, pause, indice), tutoriels, commandes alternatives, pause, victoire.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        Canvas canvas;
        RectTransform root;

        // Écrans
        GameObject mainMenu, settings, diagnostic, hud, pause, victory, modal, codePad, hintBox;
        Text objectiveText, sensorText, titleText, toastText, hintText, modalTitle, modalBody, victoryText;
        Button modalButton, modalAltButton;
        Text modalButtonText;
        Image progressFill;
        GameObject progressBar;
        RectTransform fallbackArea;
        Text[] diagStatus, diagValue;
        Text[] codeSlots;
        Action<int> codeCallback;
        GameObject continueButton;
        GameObject settingsBackTarget;
        Slider sensSlider, micSlider, volSlider;
        Text altToggleText, hapticsToggleText;

        PuzzleStep step;
        Coroutine toastRoutine, titleRoutine;

        void Awake()
        {
            Instance = this;
            BuildCanvas();
            BuildHud();
            BuildMainMenu();
            BuildSettings();
            BuildDiagnostic();
            BuildPause();
            BuildVictory();
            BuildCodePad();
            BuildModal();
            BuildToast();
            HideAll();
            GameManager.Instance.StateChanged += OnStateChanged;
        }

        void HideAll(bool keepGameplayOverlays = false)
        {
            foreach (var g in new[] { mainMenu, settings, diagnostic, hud, pause, victory }) g.SetActive(false);
            if (keepGameplayOverlays) return;
            // Tutoriel/message, clavier de symboles et indice restent visibles pendant une pause.
            modal.SetActive(false); codePad.SetActive(false); hintBox.SetActive(false);
        }

        void OnStateChanged(GameState s)
        {
            HideAll(s == GameState.Playing || s == GameState.Paused);
            switch (s)
            {
                case GameState.Menu:
                    mainMenu.SetActive(true);
                    continueButton.SetActive(GameManager.Instance.UnlockedRoom > 0);
                    break;
                case GameState.Playing:
                    hud.SetActive(true);
                    modal.transform.SetAsLastSibling();
                    toastText.transform.SetAsLastSibling();
                    SetStep(step != null && step ? step : null);
                    break;
                case GameState.Paused:
                    hud.SetActive(true);
                    pause.SetActive(true);
                    pause.transform.SetAsLastSibling();
                    toastText.transform.SetAsLastSibling();
                    RefreshSettings();
                    break;
                case GameState.Victory:
                    var gm = GameManager.Instance;
                    float t = Time.time - gm.RunStartTime;
                    string stars = "<color=#F2B847>" + new string('★', gm.Stars) + "</color><color=#555A70>" + new string('★', 3 - gm.Stars) + "</color>";
                    victoryText.text = $"<size=110>{stars}</size>\n\nVous avez récupéré l'artefact et échappé au temple.\n\nTemps : {(int)t / 60} min {(int)t % 60:00} s\nIndices utilisés : {gm.Hints}\nChutes de la boule : {gm.Falls}";
                    victory.SetActive(true);
                    break;
            }
        }

        void Update()
        {
            if (diagnostic.activeSelf) RefreshDiagnostic();
            if (hud.activeSelf && step != null)
            {
                float p = step.Progress;
                progressBar.SetActive(p >= 0f && step.IsActive);
                if (p >= 0f) progressFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(p), 1f);
            }
        }

        // ================================================================== Construction

        void BuildCanvas()
        {
            var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(transform, false);
            canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            root = Stretch(Rect("SafeArea", go.transform));
            ApplySafeArea();

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(transform, false);
        }

        void ApplySafeArea()
        {
            Rect sa = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            root.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            root.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
        }

        GameObject FullScreen(string name, Color bg)
        {
            var p = Panel(name, root, bg);
            Stretch(p.rectTransform);
            return p.gameObject;
        }

        /// <summary>Carte centrale avec une colonne d'éléments.</summary>
        RectTransform Card(GameObject screen, float width = 900)
        {
            var card = Panel("Card", screen.transform, NightLight);
            var rt = card.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, 0);
            Column(card.transform);
            var fit = card.gameObject.AddComponent<ContentSizeFitter>();
            fit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            return rt;
        }

        void BuildHud()
        {
            hud = Stretch(Rect("HUD", root)).gameObject;

            var top = Panel("Top", hud.transform, Night);
            var trt = top.rectTransform;
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = Vector2.one; trt.pivot = new Vector2(0.5f, 1);
            trt.sizeDelta = new Vector2(0, 300);

            objectiveText = Label(top.transform, "", 46, TextAnchor.UpperLeft);
            Stretch(objectiveText.rectTransform, 40, 90, 200, 30);
            sensorText = Label(top.transform, "", 36, TextAnchor.LowerLeft, Gold);
            Stretch(sensorText.rectTransform, 40, 30, 200, 0);

            var pauseBtn = MakeButton(top.transform, "II", () => GameManager.Instance.Pause(), 130);
            var prt = (RectTransform)pauseBtn.transform;
            prt.anchorMin = prt.anchorMax = new Vector2(1, 1); prt.pivot = new Vector2(1, 1);
            prt.sizeDelta = new Vector2(130, 130); prt.anchoredPosition = new Vector2(-30, -30);

            var hintBtn = MakeButton(top.transform, "?", () => { if (step != null) ShowHint(step.hint); }, 130);
            var hrt = (RectTransform)hintBtn.transform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(1, 1); hrt.pivot = new Vector2(1, 1);
            hrt.sizeDelta = new Vector2(130, 130); hrt.anchoredPosition = new Vector2(-30, -165);

            // Jauge de progression
            var bar = Panel("Progress", hud.transform, new Color(0, 0, 0, 0.6f));
            var brt = bar.rectTransform;
            brt.anchorMin = new Vector2(0.1f, 1); brt.anchorMax = new Vector2(0.9f, 1); brt.pivot = new Vector2(0.5f, 1);
            brt.sizeDelta = new Vector2(0, 30); brt.anchoredPosition = new Vector2(0, -320);
            progressFill = Panel("Fill", bar.transform, Gold);
            progressFill.rectTransform.anchorMin = Vector2.zero; progressFill.rectTransform.anchorMax = new Vector2(0, 1);
            progressFill.rectTransform.offsetMin = progressFill.rectTransform.offsetMax = Vector2.zero;
            progressBar = bar.gameObject;

            // Zone des commandes alternatives
            fallbackArea = Rect("Fallback", hud.transform);
            fallbackArea.anchorMin = new Vector2(0, 0); fallbackArea.anchorMax = new Vector2(1, 0); fallbackArea.pivot = new Vector2(0.5f, 0);
            fallbackArea.sizeDelta = new Vector2(0, 460); fallbackArea.anchoredPosition = new Vector2(0, 40);

            // Indice
            var hb = Panel("Hint", hud.transform, new Color(0.25f, 0.17f, 0.05f, 0.95f));
            var hbrt = hb.rectTransform;
            hbrt.anchorMin = new Vector2(0.05f, 1); hbrt.anchorMax = new Vector2(0.95f, 1); hbrt.pivot = new Vector2(0.5f, 1);
            hbrt.sizeDelta = new Vector2(0, 220); hbrt.anchoredPosition = new Vector2(0, -370);
            hintText = Label(hb.transform, "", 40);
            Stretch(hintText.rectTransform, 30, 20, 120, 20);
            var close = MakeButton(hb.transform, "×", HideHint, 100, new Color(0, 0, 0, 0.3f));
            var crt = (RectTransform)close.transform;
            crt.anchorMin = crt.anchorMax = new Vector2(1, 0.5f); crt.pivot = new Vector2(1, 0.5f);
            crt.sizeDelta = new Vector2(100, 100); crt.anchoredPosition = new Vector2(-10, 0);
            hintBox = hb.gameObject;

            // Titre de salle (grand texte centré qui s'efface)
            titleText = Label(hud.transform, "", 80, TextAnchor.MiddleCenter, Gold);
            titleText.fontStyle = FontStyle.Bold;
            Stretch(titleText.rectTransform, 40, 0, 40, 0);
        }

        void BuildMainMenu()
        {
            mainMenu = FullScreen("MainMenu", new Color(0.02f, 0.03f, 0.08f, 0.55f));
            var col = Rect("Column", mainMenu.transform);
            col.anchorMin = new Vector2(0.1f, 0.08f); col.anchorMax = new Vector2(0.9f, 0.92f);
            col.offsetMin = col.offsetMax = Vector2.zero;
            var v = Column(col, 30, 0);
            v.childAlignment = TextAnchor.LowerCenter;

            var title = Label(col, "LOST TEMPLE", 120, TextAnchor.MiddleCenter, Gold);
            title.fontStyle = FontStyle.Bold;
            Size(title, 200);
            Size(Label(col, "L'éveil des capteurs", 56, TextAnchor.UpperCenter, Amber), 120);
            var spacer = Rect("Spacer", col); spacer.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1;

            continueButton = MakeButton(col, "Continuer", () => GameManager.Instance.ContinueGame(), 150).gameObject;
            MakeButton(col, "Jouer", () => GameManager.Instance.NewGame(), 150, new Color(0.55f, 0.38f, 0.1f));
            MakeButton(col, "Paramètres", () => OpenSettings(mainMenu), 150);
            MakeButton(col, "Commandes / Capteurs", () => { mainMenu.SetActive(false); diagnostic.SetActive(true); }, 150);
            MakeButton(col, "Quitter", () => GameManager.Instance.Quit(), 150);
        }

        void BuildSettings()
        {
            settings = FullScreen("Settings", Night);
            var card = Card(settings);
            Size(Label(card, "Paramètres", 64, TextAnchor.MiddleCenter, Gold), 110);
            BuildSettingsControls(card);
            MakeButton(card, "Retour", () => { settings.SetActive(false); if (settingsBackTarget != null) settingsBackTarget.SetActive(true); });
        }

        void BuildSettingsControls(Transform parent)
        {
            var sm = SensorManager.Instance;
            sensSlider = MakeSlider(parent, "Sensibilité des mouvements", 0.3f, 2f, sm.Sensitivity, sm.SetSensitivity);
            micSlider = MakeSlider(parent, "Sensibilité du micro", 0.3f, 3f, sm.MicSensitivity, sm.SetMicSensitivity);
            volSlider = MakeSlider(parent, "Volume", 0f, 1f, AudioManager.Instance.Volume, AudioManager.Instance.SetVolume);
            var alt = MakeButton(parent, "", () => { sm.SetForceAlternative(!sm.ForceAlternative); RefreshSettings(); SetStep(step); });
            altToggleText = alt.GetComponentInChildren<Text>();
            var hap = MakeButton(parent, "", () => { AudioManager.Instance.SetHaptics(!AudioManager.Instance.Haptics); RefreshSettings(); });
            hapticsToggleText = hap.GetComponentInChildren<Text>();
            MakeButton(parent, "Recalibrer l'inclinaison (tenez le téléphone normalement)", () => { sm.CalibrateTilt(); ShowToast("Position neutre enregistrée"); }, 150);
            RefreshSettings();
        }

        void RefreshSettings()
        {
            var sm = SensorManager.Instance;
            altToggleText.text = "Commandes alternatives : " + (sm.ForceAlternative ? "<color=#F2B847>OUI</color>" : "NON");
            hapticsToggleText.text = "Vibrations : " + (AudioManager.Instance.Haptics ? "<color=#F2B847>OUI</color>" : "NON");
            sensSlider.SetValueWithoutNotify(sm.Sensitivity);
            micSlider.SetValueWithoutNotify(sm.MicSensitivity);
            volSlider.SetValueWithoutNotify(AudioManager.Instance.Volume);
        }

        void OpenSettings(GameObject from)
        {
            settingsBackTarget = from;
            from.SetActive(false);
            RefreshSettings();
            settings.SetActive(true);
        }

        void BuildDiagnostic()
        {
            diagnostic = FullScreen("Diagnostic", Night);
            var card = Card(diagnostic, 1000);
            Size(Label(card, "Diagnostic des capteurs", 60, TextAnchor.MiddleCenter, Gold), 100);
            Size(Label(card, "Bougez le téléphone pour voir les valeurs changer. Un capteur absent est remplacé par une commande tactile.", 36), 110);

            int n = Enum.GetValues(typeof(SensorKind)).Length;
            diagStatus = new Text[n]; diagValue = new Text[n];
            for (int i = 0; i < n; i++)
            {
                var row = Panel("Row", card, new Color(0, 0, 0, 0.25f));
                Size(row, 120);
                var name = Label(row.transform, SensorManager.DisplayName((SensorKind)i), 42, TextAnchor.UpperLeft);
                Stretch(name.rectTransform, 24, 50, 400, 10);
                diagStatus[i] = Label(row.transform, "", 38, TextAnchor.UpperRight);
                Stretch(diagStatus[i].rectTransform, 400, 50, 24, 10);
                diagValue[i] = Label(row.transform, "", 34, TextAnchor.LowerLeft, new Color(0.75f, 0.8f, 0.95f));
                Stretch(diagValue[i].rectTransform, 24, 10, 24, 60);
            }
            MakeButton(card, "Tester le micro", () => SensorManager.Instance.RequestMicrophone(ok =>
            {
                if (ok) StartCoroutine(SensorManager.Instance.CalibrateMicNoise());
                else ShowToast("Micro indisponible : le bouton « Souffler » le remplacera.");
            }));
            MakeButton(card, "Retour", () => { diagnostic.SetActive(false); OnStateChanged(GameManager.Instance.State); });
        }

        void RefreshDiagnostic()
        {
            var sm = SensorManager.Instance;
            for (int i = 0; i < diagStatus.Length; i++)
            {
                var k = (SensorKind)i;
                bool hw = sm.IsHardwareAvailable(k);
                diagStatus[i].text = hw ? "<color=#5BE38A>détecté</color>" : "<color=#F07060>absent → tactile</color>";
                diagValue[i].text = sm.DescribeValue(k);
            }
        }

        void BuildPause()
        {
            pause = FullScreen("Pause", new Color(0.02f, 0.03f, 0.08f, 0.85f));
            var card = Card(pause);
            Size(Label(card, "Pause", 70, TextAnchor.MiddleCenter, Gold), 110);
            MakeButton(card, "Reprendre", () => GameManager.Instance.Resume(), 140, new Color(0.55f, 0.38f, 0.1f));
            MakeButton(card, "Recommencer la salle", () => GameManager.Instance.RestartRoom());
            MakeButton(card, "Paramètres", () => OpenSettings(pause));
            MakeButton(card, "Diagnostic capteurs", () => { pause.SetActive(false); diagnostic.SetActive(true); });
            MakeButton(card, "Menu principal", () => GameManager.Instance.GoToMenu());
        }

        void BuildVictory()
        {
            victory = FullScreen("Victory", new Color(0.05f, 0.04f, 0.02f, 0.85f));
            var card = Card(victory);
            var t = Label(card, "Artefact récupéré !", 76, TextAnchor.MiddleCenter, Gold);
            t.fontStyle = FontStyle.Bold;
            Size(t, 140);
            victoryText = Label(card, "", 44);
            Size(victoryText, 480);
            MakeButton(card, "Rejouer", () => GameManager.Instance.NewGame(), 140, new Color(0.55f, 0.38f, 0.1f));
            MakeButton(card, "Menu principal", () => GameManager.Instance.GoToMenu());
        }

        void BuildCodePad()
        {
            codePad = Panel("CodePad", root, Night).gameObject;
            var rt = (RectTransform)codePad.transform;
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
            rt.sizeDelta = new Vector2(0, 720);
            Column(codePad.transform, 20, 30);
            Size(Label(codePad.transform, "Touchez les symboles dans l'ordre des salles", 42, TextAnchor.MiddleCenter, Gold), 70);

            var slots = Rect("Slots", codePad.transform);
            Size(slots, 110);
            var h = slots.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = 20; h.childForceExpandWidth = true; h.childControlWidth = true; h.childControlHeight = true;
            codeSlots = new Text[4];
            for (int i = 0; i < 4; i++)
            {
                var s = Panel("Slot", slots, new Color(0, 0, 0, 0.4f));
                codeSlots[i] = Label(s.transform, "?", 40);
                Stretch(codeSlots[i].rectTransform);
            }

            var grid = Rect("Grid", codePad.transform);
            Size(grid, 420);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(310, 190); g.spacing = new Vector2(20, 20);
            g.childAlignment = TextAnchor.MiddleCenter; g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 3;
            for (int i = 0; i < GameManager.GlyphNames.Length; i++)
            {
                int k = i;
                var b = MakeButton(grid, GameManager.GlyphNames[i], () => codeCallback?.Invoke(k), 190);
                b.GetComponentInChildren<Text>().color = GameManager.GlyphColors[i];
            }
        }

        void BuildModal()
        {
            modal = FullScreen("Modal", new Color(0.02f, 0.03f, 0.08f, 0.8f));
            var card = Card(modal);
            modalTitle = Label(card, "", 60, TextAnchor.MiddleCenter, Gold);
            Size(modalTitle, 100);
            modalBody = Label(card, "", 42);
            modalBody.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            modalButton = MakeButton(card, "OK", null, 140, new Color(0.55f, 0.38f, 0.1f));
            modalButtonText = modalButton.GetComponentInChildren<Text>();
            modalAltButton = MakeButton(card, "Utiliser la commande alternative", null, 120);
        }

        void BuildToast()
        {
            toastText = Label(root, "", 44, TextAnchor.MiddleCenter, TextColor);
            var rt = toastText.rectTransform;
            rt.anchorMin = new Vector2(0.05f, 0.5f); rt.anchorMax = new Vector2(0.95f, 0.5f);
            rt.sizeDelta = new Vector2(0, 160); rt.anchoredPosition = new Vector2(0, 200);
            var sh = toastText.gameObject.AddComponent<Outline>();
            sh.effectColor = new Color(0, 0, 0, 0.9f); sh.effectDistance = new Vector2(3, -3);
        }

        // ================================================================== API pour le jeu

        public void ShowRoomTitle(string title)
        {
            if (titleRoutine != null) StopCoroutine(titleRoutine);
            titleRoutine = StartCoroutine(FadeText(titleText, title, 2.5f));
        }

        public void ShowToast(string msg)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(FadeText(toastText, msg, 2.5f));
        }

        IEnumerator FadeText(Text t, string msg, float duration)
        {
            t.text = msg;
            Color c = t.color;
            for (float x = 0; x < duration; x += Time.unscaledDeltaTime)
            {
                c.a = Mathf.Clamp01(Mathf.Min(x * 4f, (duration - x) * 2f));
                t.color = c;
                yield return null;
            }
            t.text = "";
        }

        public void ShowHint(string hint)
        {
            if (!hintBox.activeSelf) GameManager.Instance.AddHint();
            hintText.text = "Indice : " + hint;
            hintBox.SetActive(true);
        }

        public void HideHint() { hintBox.SetActive(false); }

        /// <summary>Fenêtre modale générique (message, permission, symbole découvert).</summary>
        public void ShowMessage(string title, string body, string button, Action onClose, string altLabel = null, Action onAlt = null)
        {
            modalTitle.text = title;
            modalBody.text = body;
            modalButtonText.text = button;
            modalButton.onClick.RemoveAllListeners();
            modalButton.onClick.AddListener(() => { AudioManager.Instance.Play(Sfx.Click); modal.SetActive(false); onClose?.Invoke(); });
            modalAltButton.gameObject.SetActive(onAlt != null);
            modalAltButton.onClick.RemoveAllListeners();
            if (onAlt != null)
            {
                modalAltButton.GetComponentInChildren<Text>().text = altLabel;
                modalAltButton.onClick.AddListener(() => { AudioManager.Instance.Play(Sfx.Click); modal.SetActive(false); onAlt(); });
            }
            modal.SetActive(true);
        }

        public void ShowTutorial(PuzzleStep s, Action onContinue)
        {
            var sm = SensorManager.Instance;
            if (!s.usesSensor) { ShowMessage(s.objective, s.tutorial, "Continuer", onContinue); return; }
            string sensor = SensorManager.DisplayName(s.sensor);
            string mode = sm.UsesFallback(s.sensor)
                ? $"\n\n<color=#F2B847>{sensor} indisponible ou désactivé : utilisez les boutons à l'écran.</color>"
                : "";
#if UNITY_EDITOR
            mode += "\n\n<size=32><color=#9AA6C8>Éditeur : flèches/ZQSD = inclinaison, A/E = rotation, Espace = souffle, C = couvrir, L = lumière.</color></size>";
#endif
            if (sm.UsesFallback(s.sensor) || sm.ForceAlternative)
                ShowMessage(s.objective, s.tutorial + mode, "Continuer", onContinue);
            else
                ShowMessage(s.objective, s.tutorial + mode, "Continuer", onContinue, "Utiliser les boutons à la place", () =>
                {
                    sm.SetForceAlternative(true);
                    onContinue();
                });
        }

        /// <summary>Met à jour le HUD et les commandes alternatives pour l'étape active.</summary>
        public void SetStep(PuzzleStep s)
        {
            step = s;
            for (int i = fallbackArea.childCount - 1; i >= 0; i--) Destroy(fallbackArea.GetChild(i).gameObject);
            if (s == null) { objectiveText.text = ""; sensorText.text = ""; progressBar.SetActive(false); return; }

            var sm = SensorManager.Instance;
            objectiveText.text = s.objective;
            if (!s.usesSensor) { sensorText.text = "Interaction : écran tactile"; return; }
            bool fb = sm.UsesFallback(s.sensor);
            sensorText.text = "Capteur : " + SensorManager.DisplayName(s.sensor) + (fb ? "  (commande tactile)" : "");
            if (fb) BuildFallbackControls(s.sensor);
        }

        void BuildFallbackControls(SensorKind kind)
        {
            var sm = SensorManager.Instance;
            switch (kind)
            {
                case SensorKind.Accelerometer:
                {
                    var bg = Panel("Stick", fallbackArea, new Color(1, 1, 1, 0.08f));
                    var rt = bg.rectTransform;
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f); rt.sizeDelta = new Vector2(420, 420);
                    var knob = Panel("Knob", bg.transform, new Color(Gold.r, Gold.g, Gold.b, 0.8f));
                    knob.rectTransform.sizeDelta = new Vector2(160, 160);
                    knob.raycastTarget = false;
                    var stick = bg.gameObject.AddComponent<VirtualStick>();
                    stick.knob = knob.rectTransform;
                    stick.onChanged = v => sm.FallbackTilt = v;
                    break;
                }
                case SensorKind.Gyroscope:
                    Pair("↺ Tourner", v => sm.FallbackRotate = v ? -1f : 0f, "Tourner ↻", v => sm.FallbackRotate = v ? 1f : 0f);
                    break;
                case SensorKind.Compass:
                    Pair("◀ Gauche", v => sm.FallbackTurn = v ? -1f : 0f, "Droite ▶", v => sm.FallbackTurn = v ? 1f : 0f);
                    break;
                case SensorKind.Microphone:
                    Single("Maintenir pour souffler", v => sm.FallbackBlow = v);
                    break;
                case SensorKind.Proximity:
                    Single("Maintenir = couvrir le capteur", v => sm.FallbackCover = v);
                    break;
                case SensorKind.Light:
                    Single("Maintenir = allumer la lampe", v => sm.FallbackLight = v ? 1f : 0f);
                    break;
            }
        }

        void Single(string label, Action<bool> cb)
        {
            var h = Hold(fallbackArea, label, cb);
            var rt = (RectTransform)h.transform;
            rt.anchorMin = new Vector2(0.1f, 0.2f); rt.anchorMax = new Vector2(0.9f, 0.8f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        void Pair(string a, Action<bool> ca, string b, Action<bool> cb)
        {
            var h1 = (RectTransform)Hold(fallbackArea, a, ca).transform;
            h1.anchorMin = new Vector2(0.05f, 0.2f); h1.anchorMax = new Vector2(0.48f, 0.8f); h1.offsetMin = h1.offsetMax = Vector2.zero;
            var h2 = (RectTransform)Hold(fallbackArea, b, cb).transform;
            h2.anchorMin = new Vector2(0.52f, 0.2f); h2.anchorMax = new Vector2(0.95f, 0.8f); h2.offsetMin = h2.offsetMax = Vector2.zero;
        }

        public void ShowCodePad(Action<int> onPick)
        {
            codeCallback = onPick;
            UpdateCodePad(0);
            codePad.SetActive(true);
        }

        public void UpdateCodePad(int entered)
        {
            var code = GameManager.Instance.Code;
            for (int i = 0; i < 4; i++)
            {
                codeSlots[i].text = i < entered ? GameManager.GlyphNames[code[i]] : "?";
                codeSlots[i].color = i < entered ? GameManager.GlyphColors[code[i]] : TextColor;
            }
        }

        public void HideCodePad() { codePad.SetActive(false); }
    }
}
