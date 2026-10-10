using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ShadowUnit
{
    /// <summary>
    /// Base for on-screen controls. In layout-edit mode, dragging moves the control and the position is saved.
    /// </summary>
    public abstract class TouchControl : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public string layoutKey;
        protected RectTransform rect;
        protected Vector2 defaultPosition;

        public static bool EditMode;

        protected virtual void Awake() { rect = (RectTransform)transform; }

        public void InitLayout(string key)
        {
            layoutKey = key;
            if (rect == null) rect = (RectTransform)transform;
            defaultPosition = rect.anchoredPosition;
            if (PlayerPrefs.HasKey("su_pos_" + key + "_x"))
                rect.anchoredPosition = new Vector2(PlayerPrefs.GetFloat("su_pos_" + key + "_x"), PlayerPrefs.GetFloat("su_pos_" + key + "_y"));
        }

        public void SaveLayout()
        {
            if (string.IsNullOrEmpty(layoutKey)) return;
            PlayerPrefs.SetFloat("su_pos_" + layoutKey + "_x", rect.anchoredPosition.x);
            PlayerPrefs.SetFloat("su_pos_" + layoutKey + "_y", rect.anchoredPosition.y);
        }

        public void ResetLayout()
        {
            if (string.IsNullOrEmpty(layoutKey)) return;
            PlayerPrefs.DeleteKey("su_pos_" + layoutKey + "_x");
            PlayerPrefs.DeleteKey("su_pos_" + layoutKey + "_y");
            rect.anchoredPosition = defaultPosition;
        }

        public virtual bool Movable => true;

        public void OnPointerDown(PointerEventData e) { if (EditMode) return; Down(e); }
        public void OnPointerUp(PointerEventData e) { if (EditMode) return; Up(e); }
        public void OnDrag(PointerEventData e)
        {
            if (EditMode)
            {
                if (!Movable) return;
                Canvas c = GetComponentInParent<Canvas>();
                float scale = c != null ? c.scaleFactor : 1f;
                rect.anchoredPosition += e.delta / scale;
                return;
            }
            Drag(e);
        }

        protected abstract void Down(PointerEventData e);
        protected abstract void Up(PointerEventData e);
        protected virtual void Drag(PointerEventData e) { }
    }
}
