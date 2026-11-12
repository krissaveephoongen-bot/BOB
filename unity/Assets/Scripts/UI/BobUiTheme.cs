// Battle of the Brooms: design tokens from art/docs/11_UX_UI_Spec.md section 2, in one place (spec 10.3).
// Code reads BobUiTheme.Current; a theme asset (Create > Bob > UI Theme) can override it, e.g. a colour-blind theme.
// The old BobUi palette (PR #2 placeholder) still works; new screens should use these tokens.
using UnityEngine;

namespace Bob.UI
{
    [CreateAssetMenu(menuName = "Bob/UI Theme")]
    public class BobUiTheme : ScriptableObject
    {
        [Header("Colours (spec 2.1)")]
        public Color ink = Hex("#041E4F");
        public Color inkMuted = Hex("#5A5F7A");
        public Color frame = Hex("#2E4484");
        public Color surface = Hex("#FFFDF5");
        public Color surfaceCream = Hex("#FEEECF");
        public Color primary = Hex("#F6C431");
        public Color primaryLight = Hex("#FFD166");
        public Color accent = Hex("#FD7713");
        public Color secondary = Hex("#1A6FC4");
        public Color sky = Hex("#3695F2");
        public Color brandRed = Hex("#FB2F44");
        public Color success = Hex("#178A4F");
        public Color danger = Hex("#D81E36");
        public Color warn = Hex("#FD7713");
        public Color goodSpark = Hex("#3DDC84");
        public Color dim = new Color(4 / 255f, 30 / 255f, 79 / 255f, 0.7f);

        [Header("Heroes and part families (spec 2.1)")]
        public Color heroSweep = Hex("#F79A2E");
        public Color heroBubble = Hex("#3D8FE0");
        public Color heroPress = Hex("#2FA97A");
        public Color heroSpray = Hex("#8E6BD8");
        public Color familyTool = Hex("#F4A340");
        public Color familyChem = Hex("#4EA8DE");
        public Color familyEnergy = Hex("#EF476F");
        public Color familyMethod = Hex("#9B7EDE");

        [Header("Type sizes in canvas units, 1920x1080 match height (spec 2.2)")]
        public int display = 120;
        public int h1 = 72;
        public int h2 = 56;
        public int buttonLg = 60;
        public int button = 46;
        public int body = 42;
        [Tooltip("The smallest text allowed anywhere")] public int caption = 36;
        public float thaiLineHeight = 1.5f;

        [Header("Touch targets in canvas units (spec 1.2)")]
        public float hitMin = 144f;
        public float hitInMatch = 160f;
        public float hitGap = 16f;
        public Vector2 submitSize = new Vector2(440, 180);
        [Tooltip("Hold time to send the cart (spec BTN-Submit)")] public float submitHold = 0.6f;

        [Header("Spacing (spec 2.3)")]
        public float spaceXs = 8, spaceS = 16, spaceM = 24, spaceL = 40, spaceXl = 64;
        public float safeInset = 24f;

        [Header("Motion in seconds (spec 2.4)")]
        public float pressDown = 0.07f, pressUp = 0.12f, pop = 0.22f, slide = 0.28f, screen = 0.45f;
        [Tooltip("Settings > reduce motion: every animation at most this long")] public float reducedMotionMax = 0.15f;

        static BobUiTheme current;
        /// <summary>The theme in use: one put in Resources/BobUiTheme, else the built-in defaults above.</summary>
        public static BobUiTheme Current
        {
            get
            {
                if (current != null) return current;
                current = Resources.Load<BobUiTheme>("BobUiTheme");
                if (current == null) current = CreateInstance<BobUiTheme>();
                return current;
            }
            set => current = value;
        }

        public Color Hero(string id) => id switch { "bubble" => heroBubble, "press" => heroPress, "spray" => heroSpray, _ => heroSweep };
        public Color Family(Bob.Rules.Family f) => f switch
        {
            Bob.Rules.Family.Tool => familyTool, Bob.Rules.Family.Chem => familyChem, Bob.Rules.Family.Energy => familyEnergy, _ => familyMethod,
        };

        /// <summary>An animation time, shortened when the player turned on reduce motion (PlayerPrefs bob.reduceMotion).</summary>
        public float Motion(float seconds) => PlayerPrefs.GetInt("bob.reduceMotion", 0) == 1 ? Mathf.Min(seconds, reducedMotionMax) : seconds;

        static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;
    }
}
