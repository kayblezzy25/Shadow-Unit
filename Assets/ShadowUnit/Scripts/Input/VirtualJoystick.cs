using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowUnit
{
    /// <summary>Floating joystick: the knob appears where the thumb lands inside its area.</summary>
    public class VirtualJoystick : TouchControl
    {
        public RectTransform baseRing, knob;
        public float radius = 110f;
        public Vector2 Value { get; private set; }
        public bool PushedHard { get; private set; }

        int pointerId = int.MinValue;
        Vector2 origin;
        Vector2 restPos;
        float hardTimer;

        public override bool Movable => false;

        protected override void Awake()
        {
            base.Awake();
        }

        public void SetRest(Vector2 local) { restPos = local; if (baseRing != null) baseRing.anchoredPosition = local; if (knob != null) knob.anchoredPosition = local; }

        protected override void Down(PointerEventData e)
        {
            if (pointerId != int.MinValue) return;
            pointerId = e.pointerId;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out origin);
            baseRing.anchoredPosition = origin;
            knob.anchoredPosition = origin;
        }

        protected override void Drag(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            Vector2 local;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out local);
            Vector2 d = local - origin;
            Vector2 clamped = Vector2.ClampMagnitude(d, radius);
            knob.anchoredPosition = origin + clamped;
            Value = clamped / radius;
            // pushing well past the ring towards the top auto-sprints
            bool hard = d.magnitude > radius * 1.25f && Value.y > 0.75f;
            hardTimer = hard ? hardTimer + Time.unscaledDeltaTime : 0f;
            PushedHard = hard && hardTimer > 0.15f;
        }

        protected override void Up(PointerEventData e)
        {
            if (e.pointerId != pointerId) return;
            pointerId = int.MinValue;
            Value = Vector2.zero;
            PushedHard = false;
            hardTimer = 0f;
            baseRing.anchoredPosition = restPos;
            knob.anchoredPosition = restPos;
        }

        public void Release()
        {
            pointerId = int.MinValue;
            Value = Vector2.zero;
            PushedHard = false;
            if (baseRing != null) { baseRing.anchoredPosition = restPos; knob.anchoredPosition = restPos; }
        }
    }
}
