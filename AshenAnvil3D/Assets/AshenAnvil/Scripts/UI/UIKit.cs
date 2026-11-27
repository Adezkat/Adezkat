using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace AshenAnvil
{
    /// <summary>
    /// Code-first builder for Unity UI (uGUI). Generates its own rounded sprites so no
    /// texture assets are needed, and keeps a consistent dark-fantasy theme.
    /// </summary>
    public static class UIKit
    {
        public static readonly Color Bg = new Color(0.07f, 0.06f, 0.07f, 0.92f);
        public static readonly Color PanelColor = new Color(0.13f, 0.11f, 0.12f, 0.94f);
        public static readonly Color PanelLight = new Color(0.2f, 0.17f, 0.17f, 0.96f);
        public static readonly Color Accent = new Color(1f, 0.52f, 0.18f);
        public static readonly Color AccentDark = new Color(0.55f, 0.22f, 0.06f);
        public static readonly Color TextColor = new Color(0.96f, 0.92f, 0.86f);
        public static readonly Color TextDim = new Color(0.66f, 0.6f, 0.56f);
        public static readonly Color Good = new Color(0.35f, 0.85f, 0.4f);
        public static readonly Color Bad = new Color(0.95f, 0.3f, 0.25f);
        public static readonly Color Gold = new Color(1f, 0.8f, 0.25f);
        public static readonly Color ButtonColor = new Color(0.26f, 0.21f, 0.2f);

        static Font font;
        static Sprite rounded, circle, square;

        public static Font Font
        {
            get
            {
                if (font == null)
                {
                    try { font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
                    if (font == null) try { font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
                    if (font == null) font = Font.CreateDynamicFontFromOSFont("Arial", 32);
                }
                return font;
            }
        }

        public static Sprite Rounded => rounded != null ? rounded : rounded = MakeRounded(64, 18);
        public static Sprite Circle => circle != null ? circle : circle = MakeCircle(128);
        public static Sprite Square => square != null ? square : square = MakeSquare();

        // ------------------------------------------------------------------ canvas
        public static Canvas CreateCanvas(string name, int sortOrder = 0)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            canvas.pixelPerfect = false;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;
            go.AddComponent<GraphicRaycaster>();
            EnsureEventSystem();
            return canvas;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null || Compat.FindOne<EventSystem>() != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            UnityEngine.Object.DontDestroyOnLoad(es);
        }

        /// <summary>Insets content away from notches and rounded corners.</summary>
        public static RectTransform SafeArea(Transform canvas)
        {
            var rt = Rect("SafeArea", canvas);
            Stretch(rt);
            rt.gameObject.AddComponent<SafeAreaFitter>();
            return rt;
        }

        // ------------------------------------------------------------------ rects
        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.layer = 5; // UI
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Stretch(RectTransform rt, float left = 0, float top = 0, float right = 0, float bottom = 0)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 offsetMin = default, Vector2 offsetMax = default)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        public static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pos, Vector2 size, Vector2? pivot = null)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot ?? anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return rt;
        }

        // ------------------------------------------------------------------ widgets
        public static Image Panel(Transform parent, Color color, string name = "Panel", Sprite sprite = null)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite ?? Rounded;
            img.type = Image.Type.Sliced;
            img.color = color;
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 32, Color? color = null,
            TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal, string name = "Label")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color ?? TextColor;
            t.alignment = align;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        public static Text Outline(Text t, float size = 2f)
        {
            var o = t.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0, 0, 0, 0.8f);
            o.effectDistance = new Vector2(size, -size);
            return t;
        }

        public static Button Button(Transform parent, string label, Action onClick, Color? color = null, int fontSize = 32, string name = null)
        {
            var img = Panel(parent, color ?? ButtonColor, name ?? "Button_" + label);
            var btn = img.gameObject.AddComponent<Button>();
            var colors = btn.colors;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f);
            colors.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.6f);
            btn.colors = colors;
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(() => { Sfx.Play(SfxId.Click); onClick(); });
            var t = Label(img.transform, label, fontSize, TextColor, TextAnchor.MiddleCenter, FontStyle.Bold);
            Stretch(t.rectTransform, 8, 4, 8, 4);
            return btn;
        }

        public static void SetButtonLabel(Button b, string text)
        {
            var t = b.GetComponentInChildren<Text>();
            if (t != null) t.text = text;
        }

        public static LayoutElement Layout(Component c, float height = -1, float width = -1, float flexWidth = -1, float flexHeight = -1)
        {
            var le = c.GetComponent<LayoutElement>() ?? c.gameObject.AddComponent<LayoutElement>();
            if (height >= 0) le.preferredHeight = le.minHeight = height;
            if (width >= 0) le.preferredWidth = le.minWidth = width;
            if (flexWidth >= 0) le.flexibleWidth = flexWidth;
            if (flexHeight >= 0) le.flexibleHeight = flexHeight;
            return le;
        }

        public static VerticalLayoutGroup VStack(RectTransform rt, float spacing = 12, int padding = 0, bool expandHeight = false)
        {
            var v = rt.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(padding, padding, padding, padding);
            v.childControlWidth = true;
            v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = expandHeight;
            return v;
        }

        public static HorizontalLayoutGroup HStack(RectTransform rt, float spacing = 12, int padding = 0, bool expandWidth = true)
        {
            var h = rt.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.padding = new RectOffset(padding, padding, padding, padding);
            h.childControlWidth = true;
            h.childControlHeight = true;
            h.childForceExpandWidth = expandWidth;
            h.childForceExpandHeight = true;
            return h;
        }

        /// <summary>Vertical scrolling list. Returns the content transform to add rows to.</summary>
        public static RectTransform ScrollList(Transform parent, float spacing = 10, int padding = 10)
        {
            var bg = Panel(parent, new Color(0, 0, 0, 0.25f), "Scroll");
            var scroll = bg.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 40f;

            var viewport = Rect("Viewport", bg.transform);
            Stretch(viewport);
            var vImg = viewport.gameObject.AddComponent<Image>();
            vImg.color = Color.white;
            vImg.sprite = Square;
            viewport.gameObject.AddComponent<Mask>().showMaskGraphic = false;

            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            VStack(content, spacing, padding);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.viewport = viewport;
            scroll.content = content;
            return content;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var c = t.GetChild(i).gameObject;
                c.SetActive(false);
                UnityEngine.Object.Destroy(c);
            }
        }

        /// <summary>Horizontal bar with a filled foreground. Set fill.fillAmount to update.</summary>
        public static Image Bar(Transform parent, Color background, Color fillColor, out Image fill, string name = "Bar")
        {
            var bg = Panel(parent, background, name, Square);
            bg.type = Image.Type.Simple;
            var f = Rect("Fill", bg.transform);
            Stretch(f, 3, 3, 3, 3);
            fill = f.gameObject.AddComponent<Image>();
            fill.sprite = Square;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            fill.color = fillColor;
            fill.raycastTarget = false;
            bg.raycastTarget = false;
            return bg;
        }

        public static Slider Slider(Transform parent, float min, float max, float value, Action<float> onChange)
        {
            var root = Rect("Slider", parent);
            var slider = root.gameObject.AddComponent<Slider>();

            var bg = Panel(root, new Color(0, 0, 0, 0.5f), "Background");
            Anchor(bg.rectTransform, new Vector2(0, 0.35f), new Vector2(1, 0.65f));

            var fillArea = Rect("FillArea", root);
            Anchor(fillArea, new Vector2(0, 0.35f), new Vector2(1, 0.65f), new Vector2(10, 0), new Vector2(-10, 0));
            var fill = Panel(fillArea, Accent, "Fill");
            fill.rectTransform.sizeDelta = Vector2.zero;

            var handleArea = Rect("HandleArea", root);
            Stretch(handleArea, 20, 0, 20, 0);
            var handle = Rect("Handle", handleArea);
            handle.sizeDelta = new Vector2(44, 0);
            var hImg = handle.gameObject.AddComponent<Image>();
            hImg.sprite = Circle;
            hImg.color = TextColor;

            slider.fillRect = fill.rectTransform;
            slider.handleRect = handle;
            slider.targetGraphic = hImg;
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange?.Invoke(v));
            return slider;
        }

        // ------------------------------------------------------------------ sprites
        static Sprite MakeRounded(int size, int radius)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color32[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                float dy = Mathf.Max(0, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        }

        static Sprite MakeCircle(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(r - d) * 255));
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        static Sprite MakeSquare()
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var px = new Color32[16];
            for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new UnityEngine.Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        }

        public static string Hex(Color c) => "#" + ColorUtility.ToHtmlStringRGB(c);
        public static string Colored(string text, Color c) => $"<color={Hex(c)}>{text}</color>";
    }
}
