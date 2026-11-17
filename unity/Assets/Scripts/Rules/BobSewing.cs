// Battle of the Brooms: the sewing table of S31 (docs 11 "S31 ตู้เสื้อผ้า + ตัดเย็บ", docs 31 section 2.3).
//
// Four layers stacked the way the cart is stacked (ผ้า → สี → ลาย → เครื่องประดับ), a price in coins and material
// shards, and one cloth colour that the stage disc and the T04 cart both read. Nothing here touches a rule: the
// sewn cloth is decoration, exactly like the frame it can override.
//
// The cloth is the one thing a player can make that the table does not ship, so it carries the same readability
// promise BobCartPaint gives the cart: a colour that cannot carry ink is lifted toward the cream base until it can.
using System;
using System.Collections.Generic;

namespace Bob.Rules
{
    /// <summary>One choice in one layer. Hex is the swatch colour, empty for a pattern or an accessory.</summary>
    public sealed class SewOption
    {
        public readonly string Id, Name, Hex;
        public readonly int Coins, Shards;

        public SewOption(string id, string name, string hex, int coins, int shards)
        {
            Id = id; Name = name; Hex = hex; Coins = coins; Shards = shards;
        }
    }

    public static class BobSewing
    {
        /// <summary>ผ้า → สี → ลาย → เครื่องประดับ, the four layers of docs 11.</summary>
        public const int LayerCount = 4;
        public static readonly string[] LayerNames = { "ผ้า", "สี", "ลาย", "เครื่องประดับ" };

        /// <summary>What a cut costs before the chosen layers are added.</summary>
        public const int BaseCoins = 30, BaseShards = 1;

        /// <summary>How much of the chosen colour sits over the fabric. Below half, so the fabric still reads.</summary>
        public const float ColourWeight = 0.45f;

        public static readonly SewOption[][] Layers =
        {
            new[]
            {
                new SewOption("cotton", "ผ้าฝ้าย", "#E8DCC8", 0, 0),
                new SewOption("linen", "ผ้าลินิน", "#D9C7A8", 0, 0),
                new SewOption("velvet", "ผ้ากำมะหยี่", "#8C4A57", 0, 0),
                new SewOption("canvas", "ผ้ากันน้ำ", "#6E8FA3", 0, 0),
            },
            new[]
            {
                new SewOption("cream", "ครีม", "#F6EEDE", 10, 0),
                new SewOption("gold", "ทอง", "#E8B53A", 10, 0),
                new SewOption("mint", "เขียวมิ้นต์", "#2F7F6F", 10, 0),
                new SewOption("rose", "ชมพู", "#D96E8A", 10, 0),
            },
            new[]
            {
                new SewOption("plain", "เรียบ", "", 0, 0),
                new SewOption("stripe", "ทางตั้ง", "", 8, 0),
                new SewOption("check", "ตารางหมากรุก", "", 10, 0),
                new SewOption("dot", "จุดตุ๊กตา", "", 12, 0),
            },
            new[]
            {
                new SewOption("none", "ไม่ใส่", "", 0, 0),
                new SewOption("bow", "โบว์", "", 8, 1),
                new SewOption("button", "กระดุม", "", 6, 1),
                new SewOption("pocket", "กระเป๋าเล็ก", "", 14, 1),
            },
        };

        /// <summary>True when every layer has a choice that exists — the only thing "ตัดชุดนี้" may ask for.</summary>
        public static bool Complete(int[] picks)
        {
            if (picks == null || picks.Length != LayerCount) return false;
            for (int i = 0; i < LayerCount; i++)
                if (picks[i] < 0 || picks[i] >= Layers[i].Length) return false;
            return true;
        }

        public static int Coins(int[] picks)
        {
            if (!Complete(picks)) return 0;
            int sum = BaseCoins;
            for (int i = 0; i < LayerCount; i++) sum += Layers[i][picks[i]].Coins;
            return sum;
        }

        public static int Shards(int[] picks)
        {
            if (!Complete(picks)) return 0;
            int sum = BaseShards;
            for (int i = 0; i < LayerCount; i++) sum += Layers[i][picks[i]].Shards;
            return sum;
        }

        /// <summary>
        /// The cloth a cut produces: the fabric colour with the chosen colour over it. The blend is lifted toward the
        /// cream base when it cannot carry ink, so the preview and the disc never print text on a colour that hides it.
        /// </summary>
        public static BobRgb Cloth(int[] picks)
        {
            if (!Complete(picks)) return BobCartPaint.Cream;
            var fabric = Parse(0, picks[0]);
            var colour = Parse(1, picks[1]);
            float w = Math.Max(0f, Math.Min(0.5f, ColourWeight));
            var mixed = new BobRgb(
                fabric.R + (colour.R - fabric.R) * w,
                fabric.G + (colour.G - fabric.G) * w,
                fabric.B + (colour.B - fabric.B) * w);
            return Lift(mixed);
        }

        /// <summary>Every cut is readable: the wardrobe promise, checked for all 4 × 4 fabric/colour pairs.</summary>
        public static bool Readable(int[] picks) => BobCartPaint.Readable(Cloth(picks));

        /// <summary>Material shards this account has: one at the start, one more per three matches played.</summary>
        public static int ShardsEarned(int matches) => 1 + Math.Max(0, matches) / 3;

        /// <summary>The look as it is stored and sent: four indices, colon separated ("1:2:0:3").</summary>
        public static string Encode(int[] picks)
        {
            if (!Complete(picks)) return "";
            return string.Join(":", picks);
        }

        /// <summary>The stored look, or null when the text is not four valid choices (an old or foreign value).</summary>
        public static int[] Decode(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            var parts = text.Split(':');
            if (parts.Length != LayerCount) return null;
            var picks = new int[LayerCount];
            for (int i = 0; i < LayerCount; i++)
            {
                int v;
                if (!int.TryParse(parts[i], out v)) return null;
                if (v < 0 || v >= Layers[i].Length) return null;
                picks[i] = v;
            }
            return picks;
        }

        /// <summary>The look in shop words: "ผ้าฝ้าย · ทอง · ทางตั้ง · โบว์".</summary>
        public static string Describe(int[] picks)
        {
            if (!Complete(picks)) return "ยังไม่ได้เลือกครบสี่ชั้น";
            var parts = new List<string>();
            for (int i = 0; i < LayerCount; i++) parts.Add(Layers[i][picks[i]].Name);
            return string.Join(" · ", parts);
        }

        static BobRgb Parse(int layer, int pick)
        {
            var hex = Layers[layer][pick].Hex;
            var rgb = BobCartPaint.ParseHex(hex);
            return rgb.HasValue ? rgb.Value : BobCartPaint.Cream;
        }

        /// <summary>Add cream in small deterministic steps until the colour passes the ink test.</summary>
        static BobRgb Lift(BobRgb colour)
        {
            var c = colour;
            for (int step = 0; step < 20 && !BobCartPaint.Readable(c); step++)
                c = new BobRgb(
                    c.R + (BobCartPaint.Cream.R - c.R) * 0.1f,
                    c.G + (BobCartPaint.Cream.G - c.G) * 0.1f,
                    c.B + (BobCartPaint.Cream.B - c.B) * 0.1f);
            return c;
        }
    }
}
