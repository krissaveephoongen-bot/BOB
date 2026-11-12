// Co-op check: three C# clients (Net/BobOnline.cs, the same code Unity runs) play the new co-op moves against the live
// Nakama match, and the client-side layout (Coop/BobCoopLayout.cs) must predict the server's wall push-out.
using System;
using System.Linq;
using System.Threading.Tasks;
using Bob.Coop;
using Bob.Net;

static class CoopCheck
{
    static int fails;
    static void Check(string name, bool ok, string info = "") { Console.WriteLine((ok ? "  ok   " : "  FAIL ") + name + (info.Length > 0 ? "  " + info : "")); if (!ok) fails++; }

    static async Task<bool> Until(Func<bool> f, int ms = 4000) { for (int t = 0; t < ms; t += 50) { if (f()) return true; await Task.Delay(50); } return f(); }

    static (float x, float y) At(BobOnline o) { var p = o.MePlayer; return (p.x, p.y); }

    /// <summary>Walks like the Unity scene: 0.25 u steps about 9 times a second, from the server's position.</summary>
    static async Task WalkTo(BobOnline o, float x, float y)
    {
        for (int k = 0; k < 80; k++)
        {
            var (px, py) = At(o); float dx = x - px, dy = y - py, d = MathF.Sqrt(dx * dx + dy * dy);
            if (d < 0.08f) return;
            float st = Math.Min(d, 0.25f);
            o.Move(px + dx / d * st, py + dy / d * st);
            await Task.Delay(110);
        }
    }
    static async Task LeaveSpawn(BobOnline o) { var sx = At(o).x < 4 ? 2.6f : 5.4f; await WalkTo(o, sx, 0.5f); await WalkTo(o, sx, 2.0f); }

    public static async Task<int> Run(string host)
    {
        Console.WriteLine("co-op check (3 C# clients):");
        var srv = new BobServer[3]; var on = new BobOnline[3];
        for (int i = 0; i < 3; i++)
        {
            srv[i] = new BobServer("http", host, 7350, "defaultkey");
            await srv[i].SignInAsync("bob-unity-coop-check-" + i);
            await srv[i].ConnectAsync();
        }
        on[0] = await BobOnline.CreateAsync(srv[0], 31337);
        for (int i = 1; i < 3; i++) on[i] = await BobOnline.JoinAsync(srv[i], on[0].MatchId);
        string refused = null; foreach (var o in on) o.OnRefused += t => refused = t;
        Check("lobby: 3 players seen", await Until(() => on.All(o => o.State?.players?.Length == 3)));
        var t0 = on.Where(o => o.MyTeam == 0).ToArray();
        Check("lobby: 2 on team 0, 1 on team 1", t0.Length == 2);
        for (int i = 0; i < 3; i++) on[i].Ready(new[] { "sweep", "bubble", "spray" }[i]);
        Check("round 1 starts", await Until(() => on.All(o => o.State.phase == "build" && o.State.round == 1), 6000));

        var a = t0[0]; var c = t0[1];
        // 1) wall push-out matches the C# layout: step from the spawn into the sofa
        var (sx, sy) = At(a); a.Move(sx, sy + 0.2f);
        var want = BobCoopLayout.Solve(sx, sy + 0.2f);
        await Until(() => Math.Abs(At(a).y - want.y) < 0.02f, 2000);
        Check("client Solve() predicts the server's sofa push-out", Math.Abs(At(a).x - want.x) < 0.02f && Math.Abs(At(a).y - want.y) < 0.02f,
            $"server {At(a)} client {want}");

        // 2) pick, pass to a team-mate
        var part = a.State.room.offer.First(id => Bob.Rules.BobRules.Parts[id].Family == Bob.Rules.Family.Tool);
        await Task.WhenAll(LeaveSpawn(a), LeaveSpawn(c));
        var st = BobCoopLayout.Stations[0];
        await WalkTo(a, st.Cx, st.Y0 - 0.3f); a.Pick(part);
        Check("pick", await Until(() => a.MePlayer.hand.Length == 1));
        await WalkTo(c, 4.0f, 3.2f); await WalkTo(a, 4.5f, 3.2f);
        a.Pass(0, c.Me);
        Check("pass to a team-mate", await Until(() => c.MePlayer.hand.FirstOrDefault() == part && a.MePlayer.hand.Length == 0));
        // 3) table
        await WalkTo(c, BobCoopLayout.Table.Cx, BobCoopLayout.Table.Y0 - 0.35f); c.TablePut(0);
        Check("park on the table (MpTeam.table parsed)", await Until(() => c.MyCart.table?.FirstOrDefault() == part));
        c.TableTake(0);
        Check("take from the table", await Until(() => c.MePlayer.hand.FirstOrDefault() == part && c.MyCart.table.Length == 0));
        // 4) push the cart
        await WalkTo(a, 4.6f, BobCoopLayout.CartStartY); a.Push(true);
        Check("grab the cart (pusher parsed)", await Until(() => a.MyCart.pusher == a.Me));
        await WalkTo(a, 3.4f, BobCoopLayout.CartStartY); a.Push(false);
        Check("let go", await Until(() => a.MyCart.pusher == ""));
        Check("cart moved (POS carts parsed)", a.MyCart.cx < 3.0f, "cx " + a.MyCart.cx);
        // 5) place on the moved cart, then one vote of two sends it
        await WalkTo(c, a.MyCart.cx, a.MyCart.cy - 0.55f); c.Place(0);
        Check("place on the moved cart", await Until(() => c.MyCart.stack?.FirstOrDefault() == part));
        a.Submit();
        Check("one vote of two sends the cart", await Until(() => a.MyCart.submitted && a.MePlayer.vote));
        Check("no refusals on the happy path", refused == null, refused ?? "");

        foreach (var o in on) await o.LeaveAsync();
        foreach (var s in srv) await s.CloseAsync();
        Console.WriteLine($"co-op check: {(fails == 0 ? "PASS" : fails + " failed")}");
        return fails;
    }
}
