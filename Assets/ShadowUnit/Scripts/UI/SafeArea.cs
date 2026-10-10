using UnityEngine;

namespace ShadowUnit
{
    /// <summary>Keeps a RectTransform inside the device safe area (notches, rounded corners).</summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int lastSize;

        void Update()
        {
            Rect sa = Screen.safeArea;
            var size = new Vector2Int(Screen.width, Screen.height);
            if (sa == last && size == lastSize) return;
            last = sa;
            lastSize = size;
            var r = (RectTransform)transform;
            Vector2 min = sa.position, max = sa.position + sa.size;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            min.x /= Screen.width; min.y /= Screen.height;
            max.x /= Screen.width; max.y /= Screen.height;
            r.anchorMin = min;
            r.anchorMax = max;
            r.offsetMin = r.offsetMax = Vector2.zero;
        }
    }
}
