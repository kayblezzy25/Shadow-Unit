using System;
using UnityEngine;
using UnityEngine.UI;

namespace ShadowUnit
{
    /// <summary>Small helpers for building uGUI from code.</summary>
    public static class UIFactory
    {
        static Font font;
        static Sprite circle, ring;

        public static readonly Color Panel = new Color(0.07f, 0.08f, 0.07f, 0.86f);
        public static readonly Color PanelLight = new Color(0.16f, 0.18f, 0.15f, 0.92f);
        public static readonly Color Accent = new Color(0.85f, 0.75f, 0.47f, 1f);
        public static readonly Color Ink = new Color(0.93f, 0.91f, 0.84f, 1f);
        public static readonly Color Muted = new Color(0.62f, 0.64f, 0.56f, 1f);

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (Exception) { font = null; }
                if (font == null) { try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (Exception) { font = null; } }
                return font;
            }
        }

        public static Sprite Circle => circle != null ? circle : (circle = MakeCircle(false));
        public static Sprite Ring => ring != null ? ring : (ring = MakeCircle(true));

        static Sprite MakeCircle(bool hollow)
        {
            const int S = 128;
            var t = new Texture2D(S, S, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(S / 2f, S / 2f));
                float outer = Mathf.Clamp01(S / 2f - d);
                float a = hollow ? outer * Mathf.Clamp01(d - (S / 2f - 7f)) : outer;
                t.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            t.Apply();
            return Sprite.Create(t, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), 100f);
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        /// <summary>Sets anchors, pivot, position and size in one call.</summary>
        public static RectTransform Place(this RectTransform r, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = anchorMin;
            r.anchorMax = anchorMax;
            r.pivot = pivot;
            r.anchoredPosition = pos;
            r.sizeDelta = size;
            return r;
        }

        public static RectTransform Stretch(this RectTransform r, float inset = 0f)
        {
            r.anchorMin = Vector2.zero;
            r.anchorMax = Vector2.one;
            r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(inset, inset);
            r.offsetMax = new Vector2(-inset, -inset);
            return r;
        }

        public static Image Image(Transform parent, string name, Color c, Sprite sprite = null)
        {
            RectTransform r = Rect(name, parent);
            Image img = r.gameObject.AddComponent<Image>();
            img.color = c;
            img.sprite = sprite;
            return img;
        }

        public static Text Text(Transform parent, string s, int size, TextAnchor anchor, Color c, FontStyle style = FontStyle.Normal)
        {
            RectTransform r = Rect("Text", parent);
            Text t = r.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = s;
            t.fontSize = size;
            t.alignment = anchor;
            t.color = c;
            t.fontStyle = style;
            t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Vector2 size, bool primary = false, int fontSize = 30)
        {
            Image bg = Image(parent, "Button_" + label, primary ? Accent : PanelLight);
            bg.rectTransform.sizeDelta = size;
            Button b = bg.gameObject.AddComponent<Button>();
            ColorBlock cb = b.colors;
            cb.highlightedColor = new Color(1f, 1f, 1f, 1f);
            cb.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cb.disabledColor = new Color(1f, 1f, 1f, 0.35f);
            b.colors = cb;
            b.onClick.AddListener(() =>
            {
                if (AudioManager.I != null) AudioManager.I.Play2D(AudioLibrary.UiClick, 0.4f);
                onClick?.Invoke();
            });
            Text t = Text(bg.transform, label, fontSize, TextAnchor.MiddleCenter, primary ? new Color(0.1f, 0.1f, 0.08f) : Ink, FontStyle.Bold);
            t.rectTransform.Stretch();
            var le = bg.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = size.y;
            le.preferredWidth = size.x;
            return b;
        }

        public static void SetLabel(this Button b, string s)
        {
            Text t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = s;
        }

        public static VerticalLayoutGroup VStack(RectTransform r, float spacing, TextAnchor align = TextAnchor.UpperLeft)
        {
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HStack(RectTransform r, float spacing, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = false;
            h.childForceExpandHeight = false;
            return h;
        }
    }
}
