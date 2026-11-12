// Battle of the Brooms: the co-op room layout and movement rules, a copy of server/match.js (pure C#, no UnityEngine,
// so unity-check/ can compare it with the server). World units: the prototype's 800x500 px room / 100, y up.
// Each team plays in its own copy of this room (GDD 20); the scene draws team 1's copy to the right of team 0's.
using System;

namespace Bob.Coop
{
    /// <summary>An axis-aligned box [X0, Y0, X1, Y1] in world units.</summary>
    public readonly struct Box
    {
        public readonly float X0, Y0, X1, Y1;
        public Box(float x0, float y0, float x1, float y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }
        public float Cx => (X0 + X1) / 2; public float Cy => (Y0 + Y1) / 2; public float W => X1 - X0; public float H => Y1 - Y0;
        /// <summary>Distance from a point to the box (0 inside).</summary>
        public float Dist(float x, float y)
        {
            float cx = Math.Max(X0, Math.Min(X1, x)), cy = Math.Max(Y0, Math.Min(Y1, y));
            return (float)Math.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
        }
    }

    public static class BobCoopLayout
    {
        public const float RoomW = 8f, RoomH = 5f;
        /// <summary>Distance at which you can use a station, the cart or the table (server NEAR).</summary>
        public const float Near = 0.6f;
        public const float PassNear = 1.0f, Radius = 0.16f;
        /// <summary>Walking speed the client should use; the server allows 25% more for lag.</summary>
        public const float Speed = 2.3f, PushSpeed = 0.8f;
        public const int Hand = 2, TableMax = 4;
        public const float CartW = 0.9f, CartH = 0.64f;
        public const float CartStartX = 4.0f, CartStartY = 2.53f;
        /// <summary>Gap between team 0's and team 1's copy of the room in the scene (world units).</summary>
        public const float TeamOffsetX = 9.5f;

        public static readonly string[] StationIds = { "tool", "chem", "energy", "method" };
        public static readonly string[] StationNames = { "ชั้นวางอุปกรณ์", "ตู้น้ำยา", "จุดแรงทำงาน", "กระดานวิธีใช้" };
        public static readonly Box[] Stations =
        {
            new Box(0.3f, 4.06f, 2.0f, 4.7f), new Box(6.0f, 4.06f, 7.7f, 4.7f),
            new Box(0.3f, 0.3f, 2.0f, 0.94f), new Box(6.0f, 0.3f, 7.7f, 0.94f),
        };
        /// <summary>Parking table (โต๊ะพักของ): leave parts for a team-mate.</summary>
        public static readonly Box Table = new Box(3.5f, 4.2f, 4.5f, 4.7f);
        public static readonly Box Sofa = new Box(3.0f, 0.85f, 5.0f, 1.15f);
        static readonly Box[] Solids = { Stations[0], Stations[1], Stations[2], Stations[3], Table, Sofa };

        public static int StationIndex(string family) => Array.IndexOf(StationIds, family);
        public static Box CartBox(float cx, float cy) => new Box(cx - CartW / 2, cy - CartH / 2, cx + CartW / 2, cy + CartH / 2);
        /// <summary>Where a player of `team` starts each round.</summary>
        public static (float x, float y) Spawn(int team) => (team == 1 ? 4.6f : 3.4f, 0.6f);

        /// <summary>Push a body of radius r out of the shelves, the table and the sofa, then keep it in the room.
        /// Same as mpSolve in server/match.js, so the client predicts what the server will accept.</summary>
        public static (float x, float y) Solve(float x, float y, float r = Radius)
        {
            foreach (var b in Solids)
            {
                float x0 = b.X0 - r, y0 = b.Y0 - r, x1 = b.X1 + r, y1 = b.Y1 + r;
                if (x <= x0 || x >= x1 || y <= y0 || y >= y1) continue;
                float dl = x - x0, dr = x1 - x, db = y - y0, dt = y1 - y, m = Math.Min(Math.Min(dl, dr), Math.Min(db, dt));
                if (m == dl) x = x0; else if (m == dr) x = x1; else if (m == db) y = y0; else y = y1;
            }
            return (Math.Max(0.18f, Math.Min(7.82f, x)), Math.Max(0.18f, Math.Min(4.82f, y)));
        }

        /// <summary>A pushed cart's centre: out of the solids (its half height as the radius) and inside the room.</summary>
        public static (float x, float y) SolveCart(float x, float y)
        {
            var p = Solve(x, y, CartH / 2);
            return (Math.Max(CartW / 2, Math.Min(RoomW - CartW / 2, p.x)), Math.Max(CartH / 2, Math.Min(RoomH - CartH / 2, p.y)));
        }
    }
}
