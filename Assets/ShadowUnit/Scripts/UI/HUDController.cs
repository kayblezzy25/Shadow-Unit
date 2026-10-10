using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowUnit
{
    /// <summary>
    /// In-match HUD: health, ammo, score and timer, capture point status, objective markers, crosshair,
    /// hit markers, damage direction, kill feed, minimap, respawn countdown, plus the on-screen touch controls.
    /// </summary>
    public class HUDController : MonoBehaviour
    {
        RectTransform root, hudRoot;
        public GameObject ControlsRoot { get; private set; }
        public MobileInputController Input { get; private set; }
        public readonly List<TouchControl> Controls = new List<TouchControl>();

        Image hpFill, blueFill, redFill;
        Text hpText, ammoText, weaponText, timerText, blueText, redText, announce, respawn, reloadHint;
        readonly List<Image> chipBg = new List<Image>();
        readonly List<Image> chipFill = new List<Image>();
        readonly List<Text> markers = new List<Text>();
        readonly Image[] cross = new Image[4];
        readonly Image[] hit = new Image[4];
        readonly Image[] dmg = new Image[4];
        readonly float[] dmgUntil = new float[4];
        readonly float[] dmgAngle = new float[4];
        readonly Vector3[] dmgSource = new Vector3[4];
        int dmgNext;
        Text[] feed;
        readonly List<string> feedLines = new List<string>();
        readonly List<float> feedTimes = new List<float>();
        RectTransform minimap;
        readonly List<Image> dots = new List<Image>();
        readonly List<Text> mapLetters = new List<Text>();
        Image playerArrow;
        float hitUntil, announceUntil;
        bool hitKill;
        CanvasGroup controlsGroup;

        // last values shown, so text (and its garbage) is only rebuilt when something changes
        int lastHp = -1, lastAmmo = -1, lastReserve = -1, lastTime = -1, lastBlue = -1, lastRed = -1, lastRespawn = -1;
        bool lastReloading, lastDead;
        int[] lastMarkerDist;
        readonly Vector2[] crossOffsets = new Vector2[4];

        const float MapHalf = 72f, MapSize = 230f;

        public void Build(RectTransform canvasRoot)
        {
            root = canvasRoot;
            hudRoot = UIFactory.Rect("HUD", root).Stretch();

            // ---------- top centre: score + timer + capture chips ----------
            RectTransform top = UIFactory.Rect("Score", hudRoot).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -16), new Vector2(620, 60));
            UIFactory.Image(top, "bg", UIFactory.Panel).rectTransform.Stretch();
            Image bb = UIFactory.Image(top, "blueBg", new Color(0.1f, 0.15f, 0.22f));
            bb.rectTransform.Place(new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(8, 0), new Vector2(230, -18));
            blueFill = UIFactory.Image(bb.transform, "fill", Team.Blue.UiColor());
            blueFill.rectTransform.Place(Vector2.zero, new Vector2(0, 1), new Vector2(0, 0.5f), Vector2.zero, new Vector2(0, 0));
            Image rb = UIFactory.Image(top, "redBg", new Color(0.22f, 0.1f, 0.1f));
            rb.rectTransform.Place(new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(230, -18));
            redFill = UIFactory.Image(rb.transform, "fill", Team.Red.UiColor());
            redFill.rectTransform.Place(new Vector2(1, 0), Vector2.one, new Vector2(1, 0.5f), Vector2.zero, new Vector2(0, 0));
            blueText = UIFactory.Text(bb.transform, "0", 26, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
            blueText.rectTransform.Stretch(8);
            redText = UIFactory.Text(rb.transform, "0", 26, TextAnchor.MiddleRight, Color.white, FontStyle.Bold);
            redText.rectTransform.Stretch(8);
            timerText = UIFactory.Text(top, "7:00", 32, TextAnchor.MiddleCenter, UIFactory.Ink, FontStyle.Bold);
            timerText.rectTransform.Stretch();

            RectTransform chips = UIFactory.Rect("Chips", hudRoot).Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -84), new Vector2(220, 56));
            UIFactory.HStack(chips, 14, TextAnchor.MiddleCenter);
            foreach (CapturePoint p in MatchManager.I.Map.points)
            {
                Image bg = UIFactory.Image(chips, "chip" + p.label, new Color(0.15f, 0.15f, 0.15f, 0.9f), UIFactory.Circle);
                bg.gameObject.AddComponent<LayoutElement>().preferredWidth = 56;
                bg.GetComponent<LayoutElement>().preferredHeight = 56;
                Image fill = UIFactory.Image(bg.transform, "fill", Color.white, UIFactory.Circle);
                fill.rectTransform.Stretch(4);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                Text t = UIFactory.Text(bg.transform, p.label, 28, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                t.rectTransform.Stretch();
                chipBg.Add(bg);
                chipFill.Add(fill);

                Text m = UIFactory.Text(hudRoot, p.label, 24, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                m.rectTransform.sizeDelta = new Vector2(120, 60);
                m.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
                markers.Add(m);
            }

            announce = UIFactory.Text(hudRoot, "", 34, TextAnchor.MiddleCenter, UIFactory.Accent, FontStyle.Bold);
            announce.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -160), new Vector2(900, 50));
            announce.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.7f);

            // ---------- health (bottom centre) ----------
            RectTransform hp = UIFactory.Rect("Health", hudRoot).Place(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(420, 34));
            UIFactory.Image(hp, "bg", UIFactory.Panel).rectTransform.Stretch();
            hpFill = UIFactory.Image(hp, "fill", new Color(0.85f, 0.88f, 0.82f));
            hpFill.rectTransform.Place(Vector2.zero, new Vector2(1, 1), new Vector2(0, 0.5f), new Vector2(4, 0), new Vector2(-8, -8));
            hpText = UIFactory.Text(hp, "100", 24, TextAnchor.MiddleCenter, new Color(0.08f, 0.08f, 0.07f), FontStyle.Bold);
            hpText.rectTransform.Stretch();

            // ---------- ammo (top right, clear of the fire buttons) ----------
            RectTransform am = UIFactory.Rect("Ammo", hudRoot).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -20), new Vector2(300, 100));
            ammoText = UIFactory.Text(am, "30 / 150", 46, TextAnchor.UpperRight, UIFactory.Ink, FontStyle.Bold);
            ammoText.rectTransform.Place(new Vector2(0, 1), Vector2.one, new Vector2(1, 1), Vector2.zero, new Vector2(0, 54));
            weaponText = UIFactory.Text(am, "VK-7 CARBINE", 20, TextAnchor.UpperRight, UIFactory.Muted);
            weaponText.rectTransform.Place(new Vector2(0, 1), Vector2.one, new Vector2(1, 1), new Vector2(0, -54), new Vector2(0, 26));
            reloadHint = UIFactory.Text(hudRoot, "", 28, TextAnchor.MiddleCenter, UIFactory.Accent, FontStyle.Bold);
            reloadHint.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -90), new Vector2(400, 40));

            // ---------- kill feed ----------
            RectTransform kf = UIFactory.Rect("KillFeed", hudRoot).Place(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -130), new Vector2(460, 150));
            UIFactory.VStack(kf, 2, TextAnchor.UpperRight);
            feed = new Text[5];
            for (int i = 0; i < feed.Length; i++)
            {
                feed[i] = UIFactory.Text(kf, "", 22, TextAnchor.MiddleRight, UIFactory.Ink);
                feed[i].gameObject.AddComponent<LayoutElement>().preferredHeight = 28;
                feed[i].gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.6f);
            }

            // ---------- crosshair, hit marker, damage indicators ----------
            for (int i = 0; i < 4; i++)
            {
                cross[i] = UIFactory.Image(hudRoot, "cross", new Color(1, 1, 1, 0.9f));
                cross[i].rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, i < 2 ? new Vector2(16, 3) : new Vector2(3, 16));
                hit[i] = UIFactory.Image(hudRoot, "hit", Color.white);
                hit[i].rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 3));
                hit[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 45 + 90 * i);
                float a = (45 + 90 * i) * Mathf.Deg2Rad;
                hit[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 16f;
                hit[i].enabled = false;
                dmg[i] = UIFactory.Image(hudRoot, "damage", new Color(0.9f, 0.15f, 0.1f, 0f));
                dmg[i].rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 14));
            }

            respawn = UIFactory.Text(hudRoot, "", 44, TextAnchor.MiddleCenter, UIFactory.Ink, FontStyle.Bold);
            respawn.rectTransform.Place(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(900, 120));
            respawn.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);

            // ---------- minimap (top left, under the pause button) ----------
            minimap = UIFactory.Rect("Minimap", hudRoot).Place(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(20, -96), new Vector2(MapSize, MapSize));
            UIFactory.Image(minimap, "bg", new Color(0.08f, 0.1f, 0.08f, 0.72f)).rectTransform.Stretch();
            Image cross1 = UIFactory.Image(minimap, "axis", new Color(1, 1, 1, 0.08f)); cross1.rectTransform.Place(new Vector2(0.5f, 0), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(2, 0));
            Image cross2 = UIFactory.Image(minimap, "axis", new Color(1, 1, 1, 0.08f)); cross2.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(1, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(0, 2));
            foreach (CapturePoint p in MatchManager.I.Map.points)
            {
                Text l = UIFactory.Text(minimap, p.label, 20, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
                l.rectTransform.sizeDelta = new Vector2(30, 30);
                l.rectTransform.anchoredPosition = MapPos(p.transform.position);
                mapLetters.Add(l);
            }
            playerArrow = UIFactory.Image(minimap, "you", Color.white);
            playerArrow.rectTransform.sizeDelta = new Vector2(10, 16);

            BuildControls();
        }

        Vector2 MapPos(Vector3 world)
        {
            float x = Mathf.Clamp(world.x / MapHalf, -1f, 1f) * MapSize * 0.5f;
            float y = Mathf.Clamp(world.z / MapHalf, -1f, 1f) * MapSize * 0.5f;
            return new Vector2(x, y);
        }

        void BuildControls()
        {
            RectTransform c = UIFactory.Rect("TouchControls", hudRoot).Stretch();
            ControlsRoot = c.gameObject;
            controlsGroup = c.gameObject.AddComponent<CanvasGroup>();
            // lives on the always-active safe-area object so keyboard input keeps working when the touch controls are hidden
            Input = gameObject.AddComponent<MobileInputController>();
            Input.controlsRoot = ControlsRoot;

            // look pad: right 58% of the screen, behind the buttons
            Image lookImg = UIFactory.Image(c, "LookPad", new Color(0, 0, 0, 0f));
            lookImg.rectTransform.Place(new Vector2(0.42f, 0), Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Input.lookPad = lookImg.gameObject.AddComponent<TouchLookPad>();

            // joystick area: lower-left
            Image joyArea = UIFactory.Image(c, "JoystickArea", new Color(0, 0, 0, 0f));
            joyArea.rectTransform.Place(Vector2.zero, new Vector2(0.42f, 0.62f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            VirtualJoystick joy = joyArea.gameObject.AddComponent<VirtualJoystick>();
            Image ringImg = UIFactory.Image(joyArea.transform, "Ring", new Color(1, 1, 1, 0.35f), UIFactory.Ring);
            ringImg.raycastTarget = false;
            ringImg.rectTransform.sizeDelta = new Vector2(240, 240);
            Image knobImg = UIFactory.Image(joyArea.transform, "Knob", new Color(1, 1, 1, 0.55f), UIFactory.Circle);
            knobImg.raycastTarget = false;
            knobImg.rectTransform.sizeDelta = new Vector2(110, 110);
            joy.baseRing = ringImg.rectTransform;
            joy.knob = knobImg.rectTransform;
            ringImg.rectTransform.anchorMin = ringImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            knobImg.rectTransform.anchorMin = knobImg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            joy.SetRest(new Vector2(-120f, -60f));
            Input.joystick = joy;

            Input.fireRight = Button(c, "FIRE", "fireR", new Vector2(1, 0), new Vector2(-250, 250), 190, true, new Color(0.75f, 0.2f, 0.15f, 0.55f));
            Input.fireLeft = Button(c, "FIRE", "fireL", new Vector2(0, 0), new Vector2(560, 470), 120, true, new Color(0.75f, 0.2f, 0.15f, 0.45f));
            Input.aim = Button(c, "AIM", "aim", new Vector2(1, 0), new Vector2(-470, 170), 124, false);
            Input.reload = Button(c, "RELOAD", "reload", new Vector2(1, 0), new Vector2(-290, 470), 110, false);
            Input.jump = Button(c, "JUMP", "jump", new Vector2(1, 0), new Vector2(-100, 450), 110, false);
            Input.crouch = Button(c, "CROUCH", "crouch", new Vector2(1, 0), new Vector2(-95, 95), 110, false);
            Input.sprint = Button(c, "SPRINT", "sprint", new Vector2(0, 0), new Vector2(150, 560), 110, false);
            ApplyOpacity();
        }

        TouchButton Button(RectTransform parent, string label, string key, Vector2 anchor, Vector2 pos, float size, bool lookWhileHeld, Color? color = null)
        {
            Image bg = UIFactory.Image(parent, label, color ?? new Color(0.1f, 0.12f, 0.1f, 0.5f), UIFactory.Circle);
            bg.rectTransform.Place(anchor, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));
            Image border = UIFactory.Image(bg.transform, "Border", new Color(1, 1, 1, 0.35f), UIFactory.Ring);
            border.raycastTarget = false;
            border.rectTransform.Stretch();
            Text t = UIFactory.Text(bg.transform, label, size > 150 ? 32 : 22, TextAnchor.MiddleCenter, Color.white, FontStyle.Bold);
            t.rectTransform.Stretch();
            TouchButton b = bg.gameObject.AddComponent<TouchButton>();
            b.lookWhileHeld = lookWhileHeld;
            b.background = bg;
            b.label = t;
            b.SetBaseColor(bg.color);
            b.InitLayout(key);
            Controls.Add(b);
            return b;
        }

        public void ApplyOpacity()
        {
            if (controlsGroup != null) controlsGroup.alpha = GameSettings.HudOpacity;
            if (ControlsRoot != null) ControlsRoot.transform.localScale = Vector3.one;
        }

        public void SetVisible(bool v) { hudRoot.gameObject.SetActive(v); }

        /// <summary>Shows only the touch controls (used by layout editing from the main menu).</summary>
        public void SetOnlyControls(bool only)
        {
            foreach (Transform child in hudRoot) child.gameObject.SetActive(!only || child.gameObject == ControlsRoot);
            if (!only) foreach (Transform child in hudRoot) child.gameObject.SetActive(true);
        }

        // ---------- events ----------

        public void OnHit(bool killed, bool headshot)
        {
            hitUntil = Time.time + (killed ? 0.35f : 0.15f);
            hitKill = killed || headshot;
            if (killed) GameSettings.Vibrate();
        }

        public void OnDamaged(DamageInfo info)
        {
            Vector3 src = info.attacker != null ? info.attacker.transform.position : MatchManager.I.Player.transform.position - info.direction * 10f;
            dmgSource[dmgNext] = src;
            dmgUntil[dmgNext] = Time.time + 1.4f;
            dmgNext = (dmgNext + 1) % dmg.Length;
            if (AudioManager.I != null) AudioManager.I.Play2D(AudioLibrary.Hurt, 0.45f, Random.Range(0.9f, 1.1f));
        }

        public void OnKill(Soldier killer, Soldier victim, bool headshot)
        {
            string k = killer != null ? Colored(killer) : "<color=#999999>World</color>";
            string line = $"{k}  >  {Colored(victim)}{(headshot ? "  <color=#d8bf7e>HS</color>" : "")}";
            feedLines.Insert(0, line);
            feedTimes.Insert(0, Time.time);
            if (feedLines.Count > feed.Length) { feedLines.RemoveAt(feedLines.Count - 1); feedTimes.RemoveAt(feedTimes.Count - 1); }
        }

        static string Colored(Soldier s) => $"<color=#{ColorUtility.ToHtmlStringRGB(s.team.UiColor())}>{(s.isPlayer ? "YOU" : s.displayName)}</color>";

        public void Announce(string text) { announce.text = text.ToUpperInvariant(); announceUntil = Time.time + 3f; }

        public void ClearFeed() { feedLines.Clear(); feedTimes.Clear(); lastHp = lastAmmo = lastReserve = lastTime = lastBlue = lastRed = lastRespawn = -1; }

        // ---------- per-frame refresh ----------

        void Update()
        {
            MatchManager m = MatchManager.I;
            if (m == null || m.Player == null || (m.State != MatchState.Playing && m.State != MatchState.Paused)) return;
            PlayerController p = m.Player;
            Health h = p.Soldier.health;
            WeaponController w = p.Weapon;
            Camera cam = p.Cam;

            float hf = h.Fraction;
            hpFill.rectTransform.sizeDelta = new Vector2(-8 - (1f - hf) * (420 - 8), -8);
            hpFill.color = hf < 0.3f ? new Color(0.9f, 0.3f, 0.25f) : new Color(0.85f, 0.88f, 0.82f);
            int hpInt = Mathf.CeilToInt(h.Current);
            if (hpInt != lastHp) { lastHp = hpInt; hpText.text = hpInt.ToString(); }

            if (w.Ammo != lastAmmo || w.Reserve != lastReserve || w.Reloading != lastReloading)
            {
                lastAmmo = w.Ammo; lastReserve = w.Reserve; lastReloading = w.Reloading;
                ammoText.text = w.Reloading ? "RELOADING" : $"{w.Ammo} <size=28><color=#9a9f88>/ {w.Reserve}</color></size>";
                weaponText.text = w.data != null ? w.data.weaponName.ToUpperInvariant() : "";
                reloadHint.text = !w.Reloading && w.Ammo <= Mathf.Max(3, w.data.magazineSize / 6) ? (w.Ammo == 0 && w.Reserve == 0 ? "NO AMMO" : "RELOAD") : "";
            }

            int t = Mathf.Max(0, Mathf.CeilToInt(m.TimeLeft));
            if (t != lastTime) { lastTime = t; timerText.text = $"{t / 60}:{t % 60:00}"; }
            int blue = Mathf.FloorToInt(m.Score[0]), red = Mathf.FloorToInt(m.Score[1]);
            if (blue != lastBlue) { lastBlue = blue; blueText.text = blue.ToString(); }
            if (red != lastRed) { lastRed = red; redText.text = red.ToString(); }
            blueFill.rectTransform.sizeDelta = new Vector2(230 * Mathf.Clamp01(m.Score[0] / m.scoreToWin), 0);
            redFill.rectTransform.sizeDelta = new Vector2(230 * Mathf.Clamp01(m.Score[1] / m.scoreToWin), 0);

            for (int i = 0; i < m.Map.points.Count; i++)
            {
                CapturePoint cp = m.Map.points[i];
                Color side = cp.progress >= 0 ? Team.Blue.UiColor() : Team.Red.UiColor();
                chipFill[i].color = side;
                chipFill[i].fillAmount = Mathf.Abs(cp.progress);
                chipBg[i].color = cp.Contested ? Color.Lerp(new Color(0.15f, 0.15f, 0.15f, 0.9f), new Color(0.9f, 0.8f, 0.2f, 0.9f), 0.5f + 0.5f * Mathf.Sin(Time.time * 8f)) : new Color(0.15f, 0.15f, 0.15f, 0.9f);
                UpdateMarker(i, cp, cam);
                mapLetters[i].color = cp.owner < 0 ? Color.white : ((Team)cp.owner).UiColor();
            }

            bool dead = !p.Soldier.Alive;
            int respawnIn = dead && m.PlayerRespawnAt > 0 ? Mathf.CeilToInt(Mathf.Max(0, m.PlayerRespawnAt - Time.time)) : -1;
            if (respawnIn != lastRespawn || dead != lastDead)
            {
                lastRespawn = respawnIn; lastDead = dead;
                respawn.text = respawnIn >= 0 ? $"ELIMINATED\n<size=30>Redeploying in {respawnIn}</size>" : "";
            }

            // crosshair grows with spread and hides while aiming down sights
            float fovHalf = cam.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float px = Mathf.Tan(w.CurrentSpread * Mathf.Deg2Rad) / Mathf.Tan(fovHalf) * (root.rect.height * 0.5f);
            float gap = 8f + px;
            float crossAlpha = dead ? 0f : 1f - w.AimBlend;
            crossOffsets[0] = new Vector2(-gap - 8, 0);
            crossOffsets[1] = new Vector2(gap + 8, 0);
            crossOffsets[2] = new Vector2(0, gap + 8);
            crossOffsets[3] = new Vector2(0, -gap - 8);
            for (int i = 0; i < 4; i++)
            {
                cross[i].rectTransform.anchoredPosition = crossOffsets[i];
                cross[i].color = new Color(1, 1, 1, 0.9f * crossAlpha);
                hit[i].enabled = Time.time < hitUntil;
                hit[i].color = hitKill ? new Color(1f, 0.3f, 0.25f) : Color.white;
            }

            // damage direction arcs around the crosshair
            for (int i = 0; i < dmg.Length; i++)
            {
                float left = dmgUntil[i] - Time.time;
                if (left <= 0f) { dmg[i].color = new Color(0.9f, 0.15f, 0.1f, 0f); continue; }
                Vector3 to = dmgSource[i] - p.transform.position;
                float ang = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg - p.transform.eulerAngles.y;
                float a = (90f - ang) * Mathf.Deg2Rad;
                dmg[i].rectTransform.anchoredPosition = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 210f;
                dmg[i].rectTransform.localRotation = Quaternion.Euler(0, 0, 90f - ang + 90f);
                dmg[i].color = new Color(0.9f, 0.15f, 0.1f, Mathf.Clamp01(left / 1.4f) * 0.85f);
            }

            for (int i = 0; i < feed.Length; i++)
            {
                bool has = i < feedLines.Count && Time.time - feedTimes[i] < 7f;
                feed[i].text = has ? feedLines[i] : "";
            }
            if (Time.time > announceUntil) announce.text = "";

            UpdateMinimap(m, p);
        }

        void UpdateMarker(int index, CapturePoint cp, Camera cam)
        {
            Text marker = markers[index];
            Vector3 world = cp.transform.position + Vector3.up * 4f;
            Vector3 sp = cam.WorldToScreenPoint(world);
            float dist = Vector3.Distance(cam.transform.position, cp.transform.position);
            bool behind = sp.z < 0f;
            if (behind) { sp.x = Screen.width - sp.x; sp.y = 0f; }
            sp.x = Mathf.Clamp(sp.x, 80f, Screen.width - 80f);
            sp.y = Mathf.Clamp(sp.y, 80f, Screen.height - 160f);
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)marker.rectTransform.parent, sp, null, out local);
            marker.rectTransform.anchorMin = marker.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            marker.rectTransform.anchoredPosition = local;
            Color c = cp.owner < 0 ? Color.white : ((Team)cp.owner).UiColor();
            if (cp.Contested) c = Color.Lerp(c, Color.yellow, 0.6f);
            marker.color = c;
            if (lastMarkerDist == null || lastMarkerDist.Length != markers.Count)
            {
                lastMarkerDist = new int[markers.Count];
                for (int i = 0; i < lastMarkerDist.Length; i++) lastMarkerDist[i] = -1;
            }
            int rounded = Mathf.RoundToInt(dist);
            if (rounded != lastMarkerDist[index])
            {
                lastMarkerDist[index] = rounded;
                marker.text = $"{cp.label}\n<size=18>{rounded}m</size>";
            }
            marker.enabled = dist > cp.radius;
        }

        void UpdateMinimap(MatchManager m, PlayerController p)
        {
            int n = 0;
            foreach (Soldier s in Soldier.All)
            {
                if (s == null || s.isPlayer || !s.Alive) continue;
                bool friendly = s.team == Team.Blue;
                if (!friendly && Time.time > s.spottedUntil) continue;
                while (dots.Count <= n)
                {
                    Image d = UIFactory.Image(minimap, "dot", Color.white, UIFactory.Circle);
                    d.rectTransform.sizeDelta = new Vector2(11, 11);
                    dots.Add(d);
                }
                dots[n].enabled = true;
                dots[n].color = s.team.UiColor();
                dots[n].rectTransform.anchoredPosition = MapPos(s.transform.position);
                n++;
            }
            for (int i = n; i < dots.Count; i++) dots[i].enabled = false;
            playerArrow.rectTransform.anchoredPosition = MapPos(p.transform.position);
            playerArrow.rectTransform.localRotation = Quaternion.Euler(0, 0, -p.transform.eulerAngles.y);
            playerArrow.transform.SetAsLastSibling();
        }
    }
}
