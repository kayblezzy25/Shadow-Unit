using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowUnit
{
    /// <summary>Builds and switches between the main menu, settings, pause, results and control-layout screens.</summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager I { get; private set; }

        GameBootstrap boot;
        RectTransform canvasRoot, safe;
        HUDController hud;
        GameObject menu, settings, pause, results, layoutEdit;
        Text resultTitle, resultScore, resultTable;
        RectTransform settingsList;
        Action settingsBack;
        bool editingFromMenu;

        public static void Create(GameBootstrap bootstrap)
        {
            EnsureEventSystem();
            var go = new GameObject("UI");
            I = go.AddComponent<UIManager>();
            I.boot = bootstrap;
            I.Build();
        }

        static void EnsureEventSystem()
        {
#if UNITY_2023_1_OR_NEWER
            if (FindFirstObjectByType<EventSystem>() != null) return;
#else
            if (FindObjectOfType<EventSystem>() != null) return;
#endif
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if SU_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            // Input System UI module: touch, mouse and gamepad (stick/d-pad to move between buttons, A/Cross to press)
            var module = es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            module.AssignDefaultActions();
#elif ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<StandaloneInputModule>();
#else
            // Input System package only: add its UI module by name so this compiles without a hard reference.
            Type t = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (t != null) es.AddComponent(t);
            else Debug.LogError("Shadow Unit: no UI input module found. Set Player Settings > Active Input Handling to 'Both'.");
#endif
        }

        void Build()
        {
            var canvasGo = new GameObject("Canvas");
            canvasGo.transform.SetParent(transform);
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.6f;
            canvasGo.AddComponent<GraphicRaycaster>();
            canvasRoot = (RectTransform)canvasGo.transform;
            safe = UIFactory.Rect("SafeArea", canvasRoot).Stretch();
            safe.gameObject.AddComponent<SafeArea>();

            hud = safe.gameObject.AddComponent<HUDController>();
            hud.Build(safe);
            Button pauseBtn = UIFactory.Button(UIFactory.Rect("PauseHolder", safe).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -20), new Vector2(64, 64)), "II", () => ShowPause(true), new Vector2(64, 64));
            pauseBtn.GetComponent<RectTransform>().Stretch();
            hudPauseButton = pauseBtn.transform.parent.gameObject;

            BuildMenu();
            BuildSettings();
            BuildPause();
            BuildResults();
            BuildLayoutEdit();

            MatchManager.I.Kill += hud.OnKill;
            MatchManager.I.Announcement += hud.Announce;
            MatchManager.I.MatchEnded += OnMatchEnded;
            ShowMenu();
        }

        GameObject hudPauseButton;

        GameObject MakeScreen(string name, bool dim = true)
        {
            RectTransform r = UIFactory.Rect(name, safe).Stretch();
            if (dim) UIFactory.Image(r, "Dim", new Color(0.04f, 0.05f, 0.04f, 0.72f)).rectTransform.Stretch();
            return r.gameObject;
        }

        RectTransform Card(GameObject screen, Vector2 size)
        {
            Image bg = UIFactory.Image(screen.transform, "Card", UIFactory.Panel);
            bg.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            RectTransform inner = UIFactory.Rect("Content", bg.transform).Stretch(36);
            UIFactory.VStack(inner, 16, TextAnchor.UpperCenter);
            return inner;
        }

        static Text Heading(Transform parent, string s, int size, Color c)
        {
            Text t = UIFactory.Text(parent, s, size, TextAnchor.MiddleLeft, c, FontStyle.Bold);
            t.gameObject.AddComponent<LayoutElement>().preferredHeight = size + 14;
            return t;
        }

        // ---------- main menu ----------

        void BuildMenu()
        {
            menu = MakeScreen("MainMenu", false);
            Image shade = UIFactory.Image(menu.transform, "Shade", new Color(0.03f, 0.04f, 0.03f, 0.82f));
            shade.rectTransform.Place(Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(820, 0));
            RectTransform col = UIFactory.Rect("Column", menu.transform).Place(Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(90, 0), new Vector2(640, -160));
            UIFactory.VStack(col, 18, TextAnchor.MiddleLeft);

            Heading(col, "TACTICAL OPS  ·  PROTOTYPE", 22, UIFactory.Muted);
            Text title = Heading(col, "SHADOW UNIT", 92, UIFactory.Accent);
            title.GetComponent<LayoutElement>().preferredHeight = 100;
            Heading(col, "FRONTLINE", 46, UIFactory.Ink);
            Text sub = UIFactory.Text(col, "Battle Mode: two squads fight over the Market District, the Hub and the Watchtower in the Wasteland Ward. Hold more ground than the enemy, or score more eliminations, before time runs out.", 24, TextAnchor.UpperLeft, UIFactory.Muted);
            sub.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;

            UIFactory.Button(col, "DEPLOY  ·  WARFARE VS AI", StartWarfare, new Vector2(560, 90), true, 32);
            Button training = UIFactory.Button(col, "TRAINING GROUND  ·  PHASE 2", null, new Vector2(560, 74), false, 24);
            training.interactable = false;
            Button ops = UIFactory.Button(col, "OPERATIONS  ·  PHASE 3", null, new Vector2(560, 74), false, 24);
            ops.interactable = false;
            UIFactory.Button(col, "SETTINGS", () => ShowSettings(ShowMenu), new Vector2(560, 74), false, 26);
            UIFactory.Button(col, "CUSTOMIZE CONTROLS", () => ShowLayoutEdit(true), new Vector2(560, 74), false, 26);
            Text foot = UIFactory.Text(col, "Offline against AI bots. Online multiplayer is planned for Phase 4 and is not active in this build.", 18, TextAnchor.UpperLeft, UIFactory.Muted);
            foot.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
        }

        void StartWarfare()
        {
            HideAll();
            hud.SetVisible(true);
            hud.SetOnlyControls(false);
            hud.ClearFeed();
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null); // A/Cross is jump in play, not "press button"
            hudPauseButton.SetActive(true);
            hud.Input.ShowControls(true);
            MatchManager.I.StartMatch();
            WireHud();
        }

        bool hudWired;
        void WireHud()
        {
            if (hudWired) return;
            hudWired = true;
            PlayerController p = MatchManager.I.Player;
            p.Weapon.HitConfirmed += hud.OnHit;
            p.Soldier.health.Damaged += hud.OnDamaged;
        }

        void ShowMenu()
        {
            HideAll();
            menu.SetActive(true);
            hud.SetVisible(false);
            hudPauseButton.SetActive(false);
        }

        void HideAll()
        {
            foreach (GameObject g in new[] { menu, settings, pause, results, layoutEdit }) if (g != null) g.SetActive(false);
        }

        // ---------- settings ----------

        void BuildSettings()
        {
            settings = MakeScreen("Settings");
            RectTransform card = Card(settings, new Vector2(1100, 990));
            Heading(card, "SETTINGS", 44, UIFactory.Accent);
            settingsList = UIFactory.Rect("Rows", card);
            UIFactory.VStack(settingsList, 6);
            settingsList.gameObject.AddComponent<LayoutElement>().preferredHeight = 700;
            RectTransform buttons = UIFactory.Rect("Buttons", card);
            UIFactory.HStack(buttons, 16, TextAnchor.MiddleCenter);
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            UIFactory.Button(buttons, "DONE", () =>
            {
                GameSettings.Save();
                GameSettings.Apply(boot.Map.sun, boot.MainCamera);
                hud.ApplyOpacity();
                settingsBack?.Invoke();
            }, new Vector2(320, 72), true);
        }

        void ShowSettings(Action back)
        {
            settingsBack = back;
            HideAll();
            settings.SetActive(true);
            RebuildSettingsRows();
        }

        void RebuildSettingsRows()
        {
            foreach (Transform c in settingsList) Destroy(c.gameObject);
            Row("Look sensitivity", () => GameSettings.LookSensitivity.ToString("0.0"), d => GameSettings.LookSensitivity += d * 0.1f);
            Row("Aim (ADS) sensitivity", () => GameSettings.AimSensitivity.ToString("0.0"), d => GameSettings.AimSensitivity += d * 0.1f);
            Toggle("Invert look", () => GameSettings.InvertY, v => GameSettings.InvertY = v);
            Toggle("Gyroscope aiming", () => GameSettings.GyroAim, v => GameSettings.GyroAim = v);
            Toggle("Vibration (phone and controller)", () => GameSettings.Vibration, v => GameSettings.Vibration = v);
            Row("Controller look speed", () => GameSettings.PadLookSpeed.ToString("0.0"), d => GameSettings.PadLookSpeed += d * 0.1f);
            Toggle("Controller aim assist", () => GameSettings.AimAssist, v => GameSettings.AimAssist = v);
            Row("Graphics", () => GameSettings.Quality.ToString(), d => GameSettings.Quality = (QualityPreset)(((int)GameSettings.Quality + d + 3) % 3));
            Toggle("60 FPS mode", () => GameSettings.Fps60, v => GameSettings.Fps60 = v);
            Row("Master volume", () => Mathf.RoundToInt(GameSettings.Volume * 100) + "%", d => GameSettings.Volume += d * 0.1f);
            Row("Bot difficulty", () => GameSettings.Difficulty.ToString(), d => GameSettings.Difficulty = (BotDifficulty)(((int)GameSettings.Difficulty + d + 3) % 3));
            Row("Controls opacity", () => Mathf.RoundToInt(GameSettings.HudOpacity * 100) + "%", d => { GameSettings.HudOpacity += d * 0.1f; hud.ApplyOpacity(); });
        }

        void Row(string label, Func<string> value, Action<int> change)
        {
            RectTransform r = UIFactory.Rect("Row", settingsList);
            UIFactory.HStack(r, 12);
            r.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            Text l = UIFactory.Text(r, label, 26, TextAnchor.MiddleLeft, UIFactory.Ink);
            l.gameObject.AddComponent<LayoutElement>().preferredWidth = 520;
            Text v = UIFactory.Text(r, value(), 26, TextAnchor.MiddleCenter, UIFactory.Accent, FontStyle.Bold);
            v.gameObject.AddComponent<LayoutElement>().preferredWidth = 200;
            UIFactory.Button(r, "-", () => { change(-1); v.text = value(); }, new Vector2(90, 48));
            UIFactory.Button(r, "+", () => { change(1); v.text = value(); }, new Vector2(90, 48));
        }

        void Toggle(string label, Func<bool> get, Action<bool> set)
        {
            RectTransform r = UIFactory.Rect("Row", settingsList);
            UIFactory.HStack(r, 12);
            r.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;
            Text l = UIFactory.Text(r, label, 26, TextAnchor.MiddleLeft, UIFactory.Ink);
            l.gameObject.AddComponent<LayoutElement>().preferredWidth = 520;
            Button b = null;
            b = UIFactory.Button(r, get() ? "ON" : "OFF", () => { set(!get()); b.SetLabel(get() ? "ON" : "OFF"); }, new Vector2(392, 48));
        }

        // ---------- pause ----------

        void BuildPause()
        {
            pause = MakeScreen("Pause");
            RectTransform card = Card(pause, new Vector2(640, 600));
            Heading(card, "PAUSED", 48, UIFactory.Accent);
            UIFactory.Button(card, "RESUME", () => ShowPause(false), new Vector2(560, 84), true);
            UIFactory.Button(card, "SETTINGS", () => ShowSettings(() => { HideAll(); pause.SetActive(true); }), new Vector2(560, 74));
            UIFactory.Button(card, "CUSTOMIZE CONTROLS", () => ShowLayoutEdit(false), new Vector2(560, 74));
            UIFactory.Button(card, "QUIT TO MAIN MENU", () => { MatchManager.I.QuitToMenu(); ShowMenu(); }, new Vector2(560, 74));
        }

        public void ShowPause(bool show)
        {
            MatchManager m = MatchManager.I;
            if (show && m.State != MatchState.Playing) return;
            m.Pause(show);
            HideAll();
            if (show) { pause.SetActive(true); hud.Input.ShowControls(false); }
            else
            {
                hud.Input.ShowControls(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            }
        }

        // ---------- results ----------

        void BuildResults()
        {
            results = MakeScreen("Results");
            RectTransform card = Card(results, new Vector2(1000, 860));
            resultTitle = Heading(card, "VICTORY", 72, UIFactory.Accent);
            resultTitle.alignment = TextAnchor.MiddleCenter;
            resultScore = UIFactory.Text(card, "", 30, TextAnchor.MiddleCenter, UIFactory.Ink, FontStyle.Bold);
            resultScore.gameObject.AddComponent<LayoutElement>().preferredHeight = 50;
            resultTable = UIFactory.Text(card, "", 26, TextAnchor.UpperLeft, UIFactory.Ink);
            resultTable.font = UIFactory.Font;
            resultTable.gameObject.AddComponent<LayoutElement>().preferredHeight = 430;
            RectTransform buttons = UIFactory.Rect("Buttons", card);
            UIFactory.HStack(buttons, 16, TextAnchor.MiddleCenter);
            buttons.gameObject.AddComponent<LayoutElement>().preferredHeight = 84;
            UIFactory.Button(buttons, "PLAY AGAIN", StartWarfare, new Vector2(380, 80), true);
            UIFactory.Button(buttons, "MAIN MENU", () => { MatchManager.I.QuitToMenu(); ShowMenu(); }, new Vector2(380, 80));
        }

        void OnMatchEnded(Team winner)
        {
            HideAll();
            hud.Input.ShowControls(false);
            hud.SetVisible(false);
            hudPauseButton.SetActive(false);
            results.SetActive(true);
            MatchManager m = MatchManager.I;
            bool won = !m.IsDraw && winner == Team.Blue;
            resultTitle.text = m.IsDraw ? "DRAW" : won ? "VICTORY" : "DEFEAT";
            resultTitle.color = m.IsDraw ? UIFactory.Ink : won ? UIFactory.Accent : Team.Red.UiColor();
            resultScore.text = $"<color=#{ColorUtility.ToHtmlStringRGB(Team.Blue.UiColor())}>BLUE {Mathf.FloorToInt(m.Score[0])}</color>    ·    <color=#{ColorUtility.ToHtmlStringRGB(Team.Red.UiColor())}>RED {Mathf.FloorToInt(m.Score[1])}</color>";
            var rows = m.Roster().OrderByDescending(s => s.kills).ThenBy(s => s.deaths).ToList();
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("<color=#9a9f88>SOLDIER                         K      D</color>");
            foreach (Soldier s in rows)
            {
                string name = (s.isPlayer ? "YOU" : s.displayName).PadRight(26);
                string line = $"{name}{s.kills,4}   {s.deaths,4}";
                sb.AppendLine($"<color=#{ColorUtility.ToHtmlStringRGB(s.team.UiColor())}>{line}</color>");
            }
            resultTable.text = sb.ToString();
#if ENABLE_LEGACY_INPUT_MANAGER
            if (hud.Input.DesktopMode) hud.Input.ExitDesktopMode();
#endif
        }

        // ---------- layout edit ----------

        void BuildLayoutEdit()
        {
            layoutEdit = MakeScreen("LayoutEdit", false);
            Image bar = UIFactory.Image(layoutEdit.transform, "Bar", UIFactory.Panel);
            bar.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -20), new Vector2(1120, 96));
            RectTransform row = UIFactory.Rect("Row", bar.transform).Stretch(12);
            UIFactory.HStack(row, 14, TextAnchor.MiddleCenter);
            Text t = UIFactory.Text(row, "Drag the buttons to move them", 24, TextAnchor.MiddleLeft, UIFactory.Ink);
            t.gameObject.AddComponent<LayoutElement>().preferredWidth = 440;
            UIFactory.Button(row, "RESET", () => { foreach (TouchControl c in hud.Controls) c.ResetLayout(); }, new Vector2(170, 64));
            UIFactory.Button(row, "SAVE", () => { foreach (TouchControl c in hud.Controls) c.SaveLayout(); PlayerPrefs.Save(); EndLayoutEdit(); }, new Vector2(170, 64), true);
            UIFactory.Button(row, "CANCEL", () => { foreach (TouchControl c in hud.Controls) c.RevertLayout(); EndLayoutEdit(); }, new Vector2(170, 64));
        }

        void ShowLayoutEdit(bool fromMenu)
        {
            editingFromMenu = fromMenu;
            HideAll();
            hud.SetVisible(true);
            hud.SetOnlyControls(true);
            hud.Input.UseTouchControls();
            hud.Input.ShowControls(true);
            hudPauseButton.SetActive(false);
            TouchControl.EditMode = true;
            layoutEdit.SetActive(true);
            layoutEdit.transform.SetAsLastSibling();
        }

        void EndLayoutEdit()
        {
            TouchControl.EditMode = false;
            hud.SetOnlyControls(false);
            HideAll();
            if (editingFromMenu) ShowMenu();
            else { hudPauseButton.SetActive(true); pause.SetActive(true); hud.Input.ShowControls(false); }
        }

        void Update()
        {
            if (MobileInputController.GamepadConnected) KeepGamepadSelection();

            // always consume the key (Esc / Android back / Start) so a press on another screen can't pause the next match
            if (MatchManager.I == null || hud.Input == null || !hud.Input.ConsumePause()) return;
            if (MatchManager.I.State == MatchState.Playing) ShowPause(true);
            else if (MatchManager.I.State == MatchState.Paused && pause.activeSelf) ShowPause(false);
        }

        /// <summary>
        /// Gamepads navigate menus through the selected button, so whenever a menu screen is open and nothing on it is
        /// selected (screen just opened, or a controller was just connected), select its first button.
        /// </summary>
        void KeepGamepadSelection()
        {
            EventSystem es = EventSystem.current;
            if (es == null) return;
            GameObject current = es.currentSelectedGameObject;
            GameObject screen = ActiveScreen();
            if (screen == null) return;
            if (current != null && current.activeInHierarchy && current.transform.IsChildOf(screen.transform)) return;
            foreach (Button b in screen.GetComponentsInChildren<Button>())
            {
                if (!b.interactable) continue;
                es.SetSelectedGameObject(b.gameObject);
                return;
            }
        }

        GameObject ActiveScreen()
        {
            foreach (GameObject g in new[] { layoutEdit, settings, pause, results, menu })
                if (g != null && g.activeInHierarchy) return g;
            return null;
        }

        void OnApplicationPause(bool paused)
        {
            if (!paused || hud == null || hud.Input == null) return;
            // touches held when the app goes to the background never get a release event
            hud.Input.ResetState();
            if (MatchManager.I != null && MatchManager.I.State == MatchState.Playing) ShowPause(true);
        }
    }
}
