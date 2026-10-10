using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowUnit
{
    /// <summary>On-screen button. Can be held, tapped, or used as a look surface while held (fire buttons).</summary>
    public class TouchButton : TouchControl
    {
        public bool lookWhileHeld;
        public Image background;
        public Text label;
        public bool Held { get; private set; }
        bool pressed;
        int pointerId = int.MinValue;
        Color baseColor;
        public bool highlight;

        protected override void Awake()
        {
            base.Awake();
            if (background == null) background = GetComponent<Image>();
            if (background != null) baseColor = background.color;
        }

        public bool ConsumePress()
        {
            bool p = pressed;
            pressed = false;
            return p;
        }

        protected override void Down(PointerEventData e)
        {
            if (pointerId != int.MinValue) return;
            pointerId = e.pointerId;
            Held = true;
            pressed = true;
            if (AudioManager.I != null && !lookWhileHeld) AudioManager.I.Play2D(AudioLibrary.UiClick, 0.25f);
        }

        protected override void Drag(PointerEventData e)
        {
            if (lookWhileHeld && e.pointerId == pointerId) MobileInputController.I.AddLook(e.delta);
        }

        protected override void Up(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            pointerId = int.MinValue;
            Held = false;
        }

        public void Release() { pointerId = int.MinValue; Held = false; pressed = false; }

        void LateUpdate()
        {
            if (background == null) return;
            Color c = baseColor;
            if (Held || highlight) c = Color.Lerp(baseColor, new Color(0.85f, 0.75f, 0.45f, baseColor.a + 0.2f), 0.6f);
            background.color = c;
        }

        public void SetBaseColor(Color c) { baseColor = c; if (background != null) background.color = c; }
    }
}
