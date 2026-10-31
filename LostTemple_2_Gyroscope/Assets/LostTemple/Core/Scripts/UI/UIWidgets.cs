using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LostTemple
{
    /// <summary>Bouton à maintenir (commandes alternatives : souffler, couvrir, tourner…).</summary>
    public class HoldButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Action<bool> onChanged;
        bool down;

        public void OnPointerDown(PointerEventData e) { Set(true); }
        public void OnPointerUp(PointerEventData e) { Set(false); }
        public void OnPointerExit(PointerEventData e) { Set(false); }
        void OnDisable() { Set(false); }

        void Set(bool v)
        {
            if (down == v) return;
            down = v;
            var img = GetComponent<Image>();
            if (img != null) img.color = v ? UIStyle.Gold : UIStyle.ButtonColor;
            onChanged?.Invoke(v);
        }
    }

    /// <summary>Joystick virtuel : remplace l'inclinaison si l'accéléromètre n'est pas utilisable.</summary>
    public class VirtualStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public RectTransform knob;
        public Action<Vector2> onChanged;

        public void OnPointerDown(PointerEventData e) { OnDrag(e); }

        public void OnDrag(PointerEventData e)
        {
            var rt = (RectTransform)transform;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var p);
            float r = rt.rect.width * 0.5f;
            Vector2 v = Vector2.ClampMagnitude(p / r, 1f);
            knob.anchoredPosition = v * r;
            onChanged?.Invoke(v);
        }

        public void OnPointerUp(PointerEventData e)
        {
            knob.anchoredPosition = Vector2.zero;
            onChanged?.Invoke(Vector2.zero);
        }

        void OnDisable() { if (knob != null) knob.anchoredPosition = Vector2.zero; onChanged?.Invoke(Vector2.zero); }
    }

    /// <summary>Palette (bleu nuit / or / ambre) et fabrique d'éléments uGUI créés par code.</summary>
    public static class UIStyle
    {
        public static readonly Color Night = new Color(0.04f, 0.06f, 0.14f, 0.92f);
        public static readonly Color NightLight = new Color(0.09f, 0.12f, 0.24f, 0.95f);
        public static readonly Color ButtonColor = new Color(0.16f, 0.2f, 0.36f, 1f);
        public static readonly Color Gold = new Color(0.95f, 0.72f, 0.28f, 1f);
        public static readonly Color Amber = new Color(1f, 0.55f, 0.15f, 1f);
        public static readonly Color TextColor = new Color(0.96f, 0.92f, 0.82f, 1f);

        static Font font;
        public static Font Font => font != null ? font : (font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"));

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        public static Image Panel(string name, Transform parent, Color color)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size, TextAnchor anchor = TextAnchor.MiddleCenter, Color? color = null)
        {
            var rt = Rect("Text", parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font; t.fontSize = size; t.alignment = anchor; t.text = text;
            t.color = color ?? TextColor; t.supportRichText = true;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static LayoutElement Size(Component c, float height, float width = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
            if (width > 0) { le.preferredWidth = width; le.minWidth = width; }
            return le;
        }

        public static Button MakeButton(Transform parent, string text, Action onClick, float height = 130, Color? color = null)
        {
            var img = Panel("Button", parent, color ?? ButtonColor);
            var b = img.gameObject.AddComponent<UnityEngine.UI.Button>();
            var cb = b.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cb.pressedColor = new Color(0.8f, 0.8f, 0.8f); b.colors = cb;
            var l = Label(img.transform, text, 50);
            Stretch(l.rectTransform, 20, 0, 20, 0);
            b.onClick.AddListener(() => { AudioManager.Instance.Play(Sfx.Click); onClick?.Invoke(); });
            Size(img, height);
            return b;
        }

        public static HoldButton Hold(Transform parent, string text, Action<bool> onChanged)
        {
            var img = Panel("Hold", parent, ButtonColor);
            var l = Label(img.transform, text, 46);
            Stretch(l.rectTransform, 10, 0, 10, 0);
            var h = img.gameObject.AddComponent<HoldButton>();
            h.onChanged = onChanged;
            return h;
        }

        public static Slider MakeSlider(Transform parent, string label, float min, float max, float value, Action<float> onChange)
        {
            var row = Rect("Slider " + label, parent);
            Size(row, 150);
            var title = Label(row, label, 42, TextAnchor.UpperLeft);
            title.rectTransform.anchorMin = new Vector2(0, 0.55f); title.rectTransform.anchorMax = Vector2.one;
            title.rectTransform.offsetMin = title.rectTransform.offsetMax = Vector2.zero;

            var area = Rect("Area", row);
            area.anchorMin = Vector2.zero; area.anchorMax = new Vector2(1, 0.5f);
            area.offsetMin = new Vector2(30, 10); area.offsetMax = new Vector2(-30, -10);
            var bg = Panel("Background", area, ButtonColor); Stretch(bg.rectTransform, 0, 20, 0, 20);
            var fillArea = Rect("Fill Area", area); Stretch(fillArea, 0, 20, 0, 20);
            var fill = Panel("Fill", fillArea, Gold); fill.rectTransform.sizeDelta = Vector2.zero;
            var handleArea = Rect("Handle Area", area); Stretch(handleArea);
            var handle = Panel("Handle", handleArea, TextColor); handle.rectTransform.sizeDelta = new Vector2(60, 0);

            var s = row.gameObject.AddComponent<Slider>();
            s.fillRect = fill.rectTransform; s.handleRect = handle.rectTransform; s.targetGraphic = handle;
            s.minValue = min; s.maxValue = max; s.value = value;
            s.onValueChanged.AddListener(v => onChange(v));
            return s;
        }

        public static VerticalLayoutGroup Column(Transform t, float spacing = 24, int padding = 40)
        {
            var v = t.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlHeight = true; v.childControlWidth = true;
            v.childForceExpandHeight = false; v.childForceExpandWidth = true;
            v.childAlignment = TextAnchor.MiddleCenter;
            return v;
        }
    }
}
