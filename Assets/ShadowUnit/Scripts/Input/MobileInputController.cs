using UnityEngine;

namespace ShadowUnit
{
    /// <summary>
    /// Single source of player input. Touch widgets feed it; on desktop (Editor testing) keyboard and mouse
    /// feed it when the legacy Input Manager is enabled. Gameplay code only reads from here.
    /// </summary>
    public class MobileInputController : MonoBehaviour
    {
        public static MobileInputController I { get; private set; }

        public VirtualJoystick joystick;
        public TouchLookPad lookPad;
        public TouchButton fireRight, fireLeft, aim, reload, jump, crouch, sprint;
        public GameObject controlsRoot;

        public bool DesktopMode { get; private set; }
        public bool SprintLatched { get; set; }
        public Vector2 LastLookDelta { get; private set; }

        Vector2 lookAccum;
        bool aimToggled;
        bool reloadKey, jumpKey, crouchKey, sprintKey, pauseKey;

        void Awake() { I = this; }

        public void AddLook(Vector2 pixels) { lookAccum += pixels; }

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
#if ENABLE_LEGACY_INPUT_MANAGER
                if (DesktopMode) return Input.GetMouseButton(1);
#endif
                return aimToggled;
            }
        }

        /// <summary>Look delta in screen pixels since the last call.</summary>
        public Vector2 ConsumeLook()
        {
            Vector2 d = lookAccum;
            lookAccum = Vector2.zero;
#if ENABLE_LEGACY_INPUT_MANAGER
            if (DesktopMode)
                d += new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * 9f;
            if (GameSettings.GyroAim && SystemInfo.supportsGyroscope && !DesktopMode)
            {
                Vector3 r = Input.gyro.rotationRateUnbiased;
                d += new Vector2(-r.y, r.x) * Mathf.Rad2Deg * Time.deltaTime / 0.11f * 0.5f;
            }
#endif
            LastLookDelta = d;
            return d;
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
        }

        public void ResetState()
        {
            aimToggled = false;
            SprintLatched = false;
            lookAccum = Vector2.zero;
            if (joystick != null) joystick.Release();
            if (lookPad != null) lookPad.Release();
            foreach (TouchButton b in new[] { fireRight, fireLeft, aim, reload, jump, crouch, sprint }) if (b != null) b.Release();
        }

        public void EnterDesktopMode()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            DesktopMode = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (controlsRoot != null) controlsRoot.SetActive(false);
#endif
        }

        public void ExitDesktopMode()
        {
            DesktopMode = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (controlsRoot != null) controlsRoot.SetActive(true);
        }

        public void ShowControls(bool show)
        {
            if (controlsRoot != null) controlsRoot.SetActive(show && !DesktopMode);
            if (!show) ResetState();
        }
    }
}
