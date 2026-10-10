using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowUnit
{
    /// <summary>Full-height area on the right half of the screen; dragging turns the camera.</summary>
    public class TouchLookPad : TouchControl
    {
        int pointerId = int.MinValue;
        public override bool Movable => false;

        protected override void Down(PointerEventData e)
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (e.pointerId < 0 && !Application.isMobilePlatform) { MobileInputController.I.EnterDesktopMode(); return; }
#endif
            if (pointerId == int.MinValue) pointerId = e.pointerId;
        }

        protected override void Drag(PointerEventData e)
        {
            if (e.pointerId == pointerId) MobileInputController.I.AddLook(e.delta);
        }

        protected override void Up(PointerEventData e)
        {
            if (e.pointerId == pointerId) pointerId = int.MinValue;
        }

        public void Release() { pointerId = int.MinValue; }
    }
}
