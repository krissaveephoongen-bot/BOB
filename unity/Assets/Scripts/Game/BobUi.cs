// Battle of the Brooms: small helpers that build uGUI from code (no prefabs yet), in the prototype's palette.
using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Bob.Game
{
    public static class BobUi
    {
        // Palette from prototype/index.html (light theme) and art/docs/00_Art_Direction.md.
        public static readonly Color Bg = Hex("#FFF6DC"), Surface = Hex("#FFFDF5"), Ink = Hex("#2B2D42"), Muted = Hex("#6B6E86"),
            Line = Hex("#E6D9B0"), Sun = Hex("#FFD166"), Teal = Hex("#1FA6A0"), Rose = Hex("#EF476F"), Good = Hex("#1E8E5A"), Bad = Hex("#C2362B"),
            Floor = Hex("#F3E3B5"), Dim = new Color(0.17f, 0.18f, 0.26f, 0.55f);

        public static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;

        public static Color Family(Bob.Rules.Family f) => f switch
        {
            Bob.Rules.Family.Tool => Hex("#F4A340"),
            Bob.Rules.Family.Chem => Hex("#4EA8DE"),
            Bob.Rules.Family.Energy => Hex("#EF476F"),
            _ => Hex("#9B7EDE"),
        };

        public static BobArtLibrary Art;
        static Font font;

        /// <summary>A font with Thai glyphs: the OS font on Windows / Android, Unity's built-in one as a last resort.
        /// TODO: ship Mitr / IBM Plex Sans Thai as a TextMeshPro asset for correct Thai mark placement on every device.</summary>
        static Font heading;
        /// <summary>Mitr for titles and bold labels; falls back to the body font.</summary>
        public static Font Heading { get { if (heading == null) heading = Resources.Load<Font>("Fonts/Mitr-Medium"); return heading != null ? heading : Font; } }

        public static Font Font
        {
            get
            {
                if (font != null) return font;
                font = Resources.Load<Font>("Fonts/IBMPlexSansThai-Medium"); // bundled (OFL), same on every device
                if (font != null) return font;
                try { font = Font.CreateDynamicFontFromOSFont(new[] { "Leelawadee UI", "Leelawadee", "Tahoma", "Noto Sans Thai", "Noto Sans Thai UI", "Sans" }, 32); }
                catch (Exception) { font = null; }
                if (font == null) font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Stretch(RectTransform r, float l = 0, float b = 0, float rt = 0, float t = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(l, b); r.offsetMax = new Vector2(-rt, -t);
            return r;
        }

        /// <summary>Pin a rect to one anchor point: pos is the offset from that anchor, size its size.</summary>
        public static RectTransform Pin(RectTransform r, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            r.anchorMin = r.anchorMax = anchor; r.pivot = pivot; r.anchoredPosition = pos; r.sizeDelta = size;
            return r;
        }

        static Sprite circle;
        /// <summary>A soft-edged white disc, for the joystick and round badges.</summary>
        public static Sprite Circle
        {
            get
            {
                if (circle != null) return circle;
                const int n = 128; var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
                var px = new Color32[n * n];
                for (int y = 0; y < n; y++)
                    for (int x = 0; x < n; x++)
                    {
                        float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(n / 2f, n / 2f));
                        byte a = (byte)(Mathf.Clamp01(n / 2f - 1 - d) * 255);
                        px[y * n + x] = new Color32(255, 255, 255, a);
                    }
                tex.SetPixels32(px); tex.Apply();
                circle = Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f), 100);
                return circle;
            }
        }

        public static Image Img(Transform parent, string sprite, float size, Color? tint = null)
        {
            var r = Rect("Img", parent);
            var img = r.gameObject.AddComponent<Image>();
            img.sprite = Art != null && Art.Has(sprite) ? Art.Get(sprite) : null;
            img.preserveAspect = true; img.raycastTarget = false; img.color = tint ?? Color.white;
            r.sizeDelta = new Vector2(size, size);
            Size(img, size, size, 0, size);
            return img;
        }

        public static Image Disc(Transform parent, Color c, float size, string name = "Disc")
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>(); img.sprite = Circle; img.color = c;
            r.sizeDelta = new Vector2(size, size);
            return img;
        }

        public static Image Panel(Transform parent, Color c, string sprite = "ui_panel", string name = "Panel")
        {
            var r = Rect(name, parent);
            var img = r.gameObject.AddComponent<Image>();
            if (Art != null && !string.IsNullOrEmpty(sprite) && Art.Has(sprite)) { img.sprite = Art.Get(sprite); img.type = Image.Type.Sliced; img.pixelsPerUnitMultiplier = 1.4f; }
            img.color = c;
            return img;
        }

        public static Text Label(Transform parent, string text, int size = 24, Color? color = null, TextAnchor align = TextAnchor.MiddleLeft, FontStyle style = FontStyle.Normal)
        {
            var r = Rect("Text", parent);
            var t = r.gameObject.AddComponent<Text>();
            t.font = style == FontStyle.Bold ? Heading : Font; t.text = text; t.fontSize = size; t.color = color ?? Ink; t.alignment = align; t.fontStyle = style == FontStyle.Bold && Heading != Font ? FontStyle.Normal : style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
            t.lineSpacing = 1.1f;
            return t;
        }

        public static Button Button(Transform parent, string text, Color bg, UnityAction onClick, int size = 24, Color? fg = null)
        {
            // The art has a yellow (main action) and a teal (go) button; any other colour tints the neutral panel.
            var sprite = bg == Teal && Art != null && Art.Has("ui_button_teal") ? "ui_button_teal" : bg == Sun ? "ui_button" : "ui_panel";
            var img = Panel(parent, sprite == "ui_panel" ? bg : Color.white, sprite, "Button");
            var b = img.gameObject.AddComponent<Button>();
            var cb = b.colors; cb.highlightedColor = new Color(1, 1, 1, 0.92f); cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1); cb.disabledColor = new Color(1, 1, 1, 0.45f); b.colors = cb;
            if (onClick != null) b.onClick.AddListener(onClick);
            var t = Label(img.transform, text, size, fg ?? Ink, TextAnchor.MiddleCenter);
            Stretch(t.rectTransform, 10, 4, 10, 4);
            return b;
        }

        public static LayoutElement Size(Component c, float minH = -1, float prefH = -1, float flexW = -1, float prefW = -1, float flexH = -1)
        {
            if (!c.TryGetComponent<LayoutElement>(out var le)) le = c.gameObject.AddComponent<LayoutElement>();
            if (minH >= 0) le.minHeight = minH;
            if (prefH >= 0) le.preferredHeight = prefH;
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (prefW >= 0) le.preferredWidth = prefW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        public static VerticalLayoutGroup VBox(Transform parent, float spacing = 6, int pad = 0, string name = "VBox")
        {
            var r = Rect(name, parent);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing; v.padding = new RectOffset(pad, pad, pad, pad);
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static HorizontalLayoutGroup HBox(Transform parent, float spacing = 6, string name = "HBox")
        {
            var r = Rect(name, parent);
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            return h;
        }

        public static GridLayoutGroup Grid(Transform parent, Vector2 cell, int columns, float spacing = 6)
        {
            var r = Rect("Grid", parent);
            var g = r.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = cell; g.spacing = new Vector2(spacing, spacing);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = columns;
            return g;
        }

        /// <summary>A white card with an ink title row, laid out vertically. Returns the content parent.</summary>
        public static VerticalLayoutGroup Box(Transform parent, string title, string small = null, int titleSize = 24, int smallSize = 18)
        {
            var img = Panel(parent, Surface, "ui_panel", "Box");
            var v = img.gameObject.AddComponent<VerticalLayoutGroup>();
            v.padding = new RectOffset(16, 16, 12, 14); v.spacing = 6;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            var head = HBox(img.transform, 8, "Head"); head.childForceExpandHeight = false;
            var t = Label(head.transform, title, titleSize, Ink, TextAnchor.MiddleLeft, FontStyle.Bold); Size(t, titleSize + 18, titleSize + 18, 1);
            if (!string.IsNullOrEmpty(small)) { var s = Label(head.transform, small, smallSize, Muted, TextAnchor.MiddleRight); Size(s, smallSize + 18, smallSize + 18, 1); }
            return v;
        }

        public static void Clear(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--)
            {
                var go = t.GetChild(i).gameObject;
                go.SetActive(false); go.transform.SetParent(null, false); // leave the layout now; Destroy only runs at frame end
                UnityEngine.Object.Destroy(go);
            }
        }
    }
}
