using UnityEngine;
#if SU_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ShadowUnit
{
    /// <summary>
    /// Single source of player input. Touch widgets feed it; on desktop (Editor testing) keyboard and mouse
    /// feed it when the legacy Input Manager is enabled; a gamepad feeds it when the Input System package is installed.
    /// Gameplay code only reads from here.
    ///
    /// Gamepad layout (Xbox names; PlayStation in brackets):
    ///   Left stick move · L3 sprint · Right stick look · RT [R2] fire · LT [L2] aim (hold)
    ///   A [Cross] jump · B [Circle] crouch · X [Square] reload · Start [Options] pause
    /// </summary>
    public class MobileInputController : MonoBehaviour
    {
        public static MobileInputController I { get; private set; }

        public VirtualJoystick joystick;
        public TouchLookPad lookPad;
        public TouchButton fireRight, fireLeft, aim, reload, jump, crouch, sprint;
        public GameObject controlsRoot;

        public bool DesktopMode { get; private set; }
        /// <summary>True after the gamepad was last used; the touch controls hide until the screen is touched again.</summary>
        public bool GamepadActive { get; private set; }
        public bool SprintLatched { get; set; }
        public Vector2 LastLookDelta { get; private set; }

        Vector2 lookAccum;
        bool aimToggled;
        bool reloadKey, jumpKey, crouchKey, sprintKey, pauseKey;
        bool controlsWanted;

        const float StickDeadZone = 0.15f, TriggerPoint = 0.35f;
        /// <summary>Gamepad look speed in degrees per second at full stick and "Controller look speed" 1.0.</summary>
        const float PadYawSpeed = 190f, PadPitchSpeed = 130f;

        Vector2 padMove, padLook;
        bool padFire, padAim;
        float rumbleUntil = -1f;

        void Awake() { I = this; }

        public void AddLook(Vector2 pixels) { lookAccum += pixels; }

        /// <summary>True when a gamepad is plugged in or paired (whether or not it is being used).</summary>
        public static bool GamepadConnected
        {
            get
            {
#if SU_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
                return Gamepad.current != null;
#else
                return false;
#endif
            }
        }

        public Vector2 Move
        {
            get
            {
                Vector2 v = joystick != null ? joystick.Value : Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
                if (DesktopMode)
                {
                    v.x = (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0);
                    v.y = (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0);
                }
#endif
                if (padMove.sqrMagnitude > 0f) v = padMove;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        public bool JoystickPushedHard
        {
            get
            {
#if ENABLE_LEGACY_INPUT_MANAGER
                if (DesktopMode) return Input.GetKey(KeyCode.LeftShift);
#endif
                return joystick != null && joystick.PushedHard;
            }
        }

        public bool FireHeld
        {
            get
            {
                if (padFire) return true;
#if ENABLE_LEGACY_INPUT_MANAGER
                if (DesktopMode) return Input.GetMouseButton(0);
#endif
                return (fireRight != null && fireRight.Held) || (fireLeft != null && fireLeft.Held);
            }
        }

        public bool AimActive
        {
            get
            {
                if (padAim) return true;
#if ENABLE_LEGACY_INPUT_MANAGER
                if (DesktopMode) return Input.GetMouseButton(1);
#endif
                return aimToggled;
            }
        }

        /// <summary>Look delta in screen pixels since the last call (touch, mouse and gyro).</summary>
        public Vector2 ConsumeLook()
        {
            Vector2 d = lookAccum;
            lookAccum = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (DesktopMode)
                d += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 9f;
            if (GameSettings.GyroAim && SystemInfo.supportsGyroscope && !DesktopMode)
            {
                // gyro rates are in the device's portrait frame; map them to the current landscape screen axes
                Vector3 r = Input.gyro.rotationRateUnbiased;
                Vector2 g = Screen.orientation == ScreenOrientation.LandscapeRight ? new Vector2(r.x, r.y)
                          : Screen.orientation == ScreenOrientation.LandscapeLeft ? new Vector2(-r.x, -r.y)
                          : new Vector2(-r.y, r.x);
                d += g * Mathf.Rad2Deg * Time.deltaTime / 0.11f * 0.5f;
            }
#endif
            LastLookDelta = d;
            return d;
        }

        /// <summary>
        /// Gamepad look for this frame in degrees (x = yaw, y = pitch up), already scaled by the stick curve,
        /// the "Controller look speed" setting and delta time. PlayerController applies aiming and aim-assist slowdown.
        /// </summary>
        public Vector2 PadLookDegrees
        {
            get
            {
                if (padLook.sqrMagnitude <= 0f) return Vector2.zero;
                float speed = GameSettings.PadLookSpeed;
                return new Vector2(padLook.x * PadYawSpeed, padLook.y * PadPitchSpeed) * speed * Time.deltaTime;
            }
        }

        public bool ConsumeReload() { bool k = reloadKey; reloadKey = false; return (reload != null && reload.ConsumePress()) || k; }
        public bool ConsumeJump() { bool k = jumpKey; jumpKey = false; return (jump != null && jump.ConsumePress()) || k; }
        public bool ConsumeCrouch() { bool k = crouchKey; crouchKey = false; return (crouch != null && crouch.ConsumePress()) || k; }
        public bool ConsumeSprint() { bool k = sprintKey; sprintKey = false; return (sprint != null && sprint.ConsumePress()) || k; }
        public bool ConsumePause() { bool k = pauseKey; pauseKey = false; return k; }

        void Update()
        {
            if (aim != null && aim.ConsumePress()) aimToggled = !aimToggled;
            if (aim != null) aim.highlight = aimToggled;
            if (sprint != null) sprint.highlight = SprintLatched;

#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0 && DesktopMode) ExitDesktopMode();
            if (DesktopMode)
            {
                if (Input.GetKeyDown(KeyCode.R)) reloadKey = true;
                if (Input.GetKeyDown(KeyCode.Space)) jumpKey = true;
                if (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.LeftControl)) crouchKey = true;
                if (Input.GetKeyDown(KeyCode.Escape)) { pauseKey = true; ExitDesktopMode(); }
            }
            else if (Input.GetKeyDown(KeyCode.Escape)) pauseKey = true;
#endif
            ReadGamepad();
            if (GamepadActive && TouchedScreen()) SetGamepadActive(false);
            if (rumbleUntil > 0f && Time.unscaledTime > rumbleUntil) StopRumble();
        }

        void ReadGamepad()
        {
            padMove = padLook = Vector2.zero;
            padFire = padAim = false;
#if SU_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            Gamepad pad = Gamepad.current;
            if (pad == null) return;

            padMove = Stick(pad.leftStick.ReadValue(), false);
            padLook = Stick(pad.rightStick.ReadValue(), true);
            if (GameSettings.InvertY) padLook.y = -padLook.y;
            padFire = pad.rightTrigger.ReadValue() > TriggerPoint;
            padAim = pad.leftTrigger.ReadValue() > TriggerPoint;

            if (pad.buttonSouth.wasPressedThisFrame) jumpKey = true;
            if (pad.buttonEast.wasPressedThisFrame) crouchKey = true;
            if (pad.buttonWest.wasPressedThisFrame) reloadKey = true;
            if (pad.leftStickButton.wasPressedThisFrame) sprintKey = true;
            if (pad.startButton.wasPressedThisFrame) pauseKey = true;

            bool used = padMove != Vector2.zero || padLook != Vector2.zero || padFire || padAim
                || pad.buttonSouth.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame || pad.buttonWest.wasPressedThisFrame
                || pad.buttonNorth.wasPressedThisFrame || pad.startButton.wasPressedThisFrame || pad.leftStickButton.wasPressedThisFrame;
            if (used && !GamepadActive) SetGamepadActive(true);
#endif
        }

        /// <summary>Radial dead zone, rescaled so movement starts smoothly; look gets a squared curve for fine aim.</summary>
        static Vector2 Stick(Vector2 raw, bool curve)
        {
            float m = raw.magnitude;
            if (m < StickDeadZone) return Vector2.zero;
            float t = Mathf.Clamp01((m - StickDeadZone) / (1f - StickDeadZone));
            if (curve) t *= t;
            return raw / m * t;
        }

        bool TouchedScreen()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.touchCount > 0) return true;
#endif
#if SU_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed) return true;
#endif
            return false;
        }

        void SetGamepadActive(bool on)
        {
            GamepadActive = on;
            if (on)
            {
                // drop any half-finished touch input so nothing stays held
                if (joystick != null) joystick.Release();
                if (lookPad != null) lookPad.Release();
                foreach (TouchButton b in new[] { fireRight, fireLeft, aim, reload, jump, crouch, sprint }) if (b != null) b.Release();
                aimToggled = false;
            }
            RefreshControls();
        }

        /// <summary>Short controller vibration (respects the Vibration setting). Low = heavy motor, high = light motor.</summary>
        public void Rumble(float low, float high, float seconds)
        {
#if SU_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            if (!GameSettings.Vibration || !GamepadActive || Gamepad.current == null) return;
            Gamepad.current.SetMotorSpeeds(low, high);
            rumbleUntil = Time.unscaledTime + seconds;
#endif
        }

        public void StopRumble()
        {
            rumbleUntil = -1f;
#if SU_INPUT_SYSTEM && ENABLE_INPUT_SYSTEM
            if (Gamepad.current != null) Gamepad.current.SetMotorSpeeds(0f, 0f);
#endif
        }

        void OnDisable() { StopRumble(); }
        void OnApplicationPause(bool paused) { if (paused) StopRumble(); }

        /// <summary>Show the touch controls again even if a gamepad was the last thing used (layout editing).</summary>
        public void UseTouchControls() { if (GamepadActive) SetGamepadActive(false); }

        public void ResetState()
        {
            aimToggled = false;
            SprintLatched = false;
            lookAccum = Vector2.zero;
            if (joystick != null) joystick.Release();
            if (lookPad != null) lookPad.Release();
            foreach (TouchButton b in new[] { fireRight, fireLeft, aim, reload, jump, crouch, sprint }) if (b != null) b.Release();
            StopRumble();
        }

        public void EnterDesktopMode()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            DesktopMode = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            RefreshControls();
#endif
        }

        public void ExitDesktopMode()
        {
            DesktopMode = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            RefreshControls();
        }

        public void ShowControls(bool show)
        {
            controlsWanted = show;
            RefreshControls();
            if (!show) ResetState();
        }

        /// <summary>Touch controls show only when the current screen wants them and neither the mouse nor a gamepad is in use.</summary>
        void RefreshControls()
        {
            if (controlsRoot != null) controlsRoot.SetActive(controlsWanted && !DesktopMode && !GamepadActive);
        }
    }
}
