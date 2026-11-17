// Battle of the Brooms: the paint on the T04 cart (docs 31 section 2.3), as pure C# with no Unity types.
//
// The owner's rule is decoration only: the worn frame colour also paints the cart body in the T04 card, so the
// wardrobe is visible while playing and not only on Home. Nothing here touches a stat, a rule, a cost or a clock;
// the card reads the resulting colour and nothing else.
//
// The card draws ink text straight on the cart body, so the paint is a tint and not a repaint: the body has to stay
// light enough to read. MinLuminance is that promise, and unity-check asserts it for every frame in the table.
using System;

namespace Bob.Rules
{
    /// <summary>An sRGB colour in 0..1 floats, so this file can be checked without Unity.</summary>
    public readonly struct BobRgb
    {
        public readonly float R, G, B;

        public BobRgb(float r, float g, float b) { R = r; G = g; B = b; }
    }

    public static class BobCartPaint
    {
        /// <summary>The unpainted cart body: the cream panel the card shipped with.</summary>
        public static readonly BobRgb Cream = new BobRgb(0.97f, 0.94f, 0.87f);

        /// <summary>How much frame colour goes into the body. A tint: the cream still decides the value.</summary>
        public const float Weight = 0.28f;

        /// <summary>Any painted body stays at or above this luminance, so the ink text on it keeps its contrast.</summary>
        public const float MinLuminance = 0.72f;

        /// <summary>The cart body colour: cream when nothing is worn, otherwise cream tinted toward the frame colour.</summary>
        public static BobRgb Body(bool painted, BobRgb frame)
        {
            if (!painted) return Cream;
            float w = Math.Max(0f, Math.Min(0.5f, Weight));
            return new BobRgb(
                Cream.R + (frame.R - Cream.R) * w,
                Cream.G + (frame.G - Cream.G) * w,
                Cream.B + (frame.B - Cream.B) * w);
        }

        /// <summary>Relative luminance (Rec. 709) of an sRGB colour, the number the readability promise is about.</summary>
        public static float Luminance(BobRgb c) => 0.2126f * c.R + 0.7152f * c.G + 0.0722f * c.B;

        /// <summary>True when ink text drawn on this body keeps its contrast.</summary>
        public static bool Readable(BobRgb c) => Luminance(c) >= MinLuminance;

        /// <summary>Parse "#RRGGBB", the form the unlock table stores a frame colour in; null when it is not one.</summary>
        public static BobRgb? ParseHex(string hex)
        {
            if (string.IsNullOrEmpty(hex) || hex.Length != 7 || hex[0] != '#') return null;
            for (int i = 1; i < 7; i++)
            {
                char c = hex[i];
                bool digit = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!digit) return null;
            }
            return new BobRgb(
                Convert.ToInt32(hex.Substring(1, 2), 16) / 255f,
                Convert.ToInt32(hex.Substring(3, 2), 16) / 255f,
                Convert.ToInt32(hex.Substring(5, 2), 16) / 255f);
        }
    }
}
