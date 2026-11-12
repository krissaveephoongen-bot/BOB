// Verifies the Unity-side scripts without Unity: (1) rule spot checks, (2) C# rules vs the live Nakama server on random stacks.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Bob.Net;
using Bob.Rules;

static class Program
{
    static int fails;
    static void Check(string name, bool ok) { if (!ok) { fails++; Console.WriteLine("FAIL " + name); } }

    static async Task<int> Main()
    {
        var a = BobRules.EvalStack(new[] { "hot", "soap" });
        Check("hot under soap: clean 7", a.Get("clean") == 7);
        Check("soap under hot: speed -1", BobRules.EvalStack(new[] { "soap", "hot" }).Get("speed") == -1);
        Check("shield cancels worst reaction", BobRules.EvalStack(new[] { "soap", "hot" }, null, true).Get("speed") == 1);
        Check("grease with heat: clean 9", BobRules.EvalStack(new[] { "hot", "soap" }, new[] { "grease" }).Get("clean") == 9);
        Check("method cannot be first", BobRules.WhyCannotPlace(new List<string>(), "soak", 100) != null);
        Console.WriteLine("spot checks done, fails=" + fails);

        var srv = new BobServer();
        await srv.SignInAsync("unity-check-device-0001");
        Console.WriteLine("ping: " + await srv.PingAsync());
        await srv.ConnectAsync();
        var match = await srv.CreateMatchAsync();
        Check("match created", !string.IsNullOrEmpty(match.Id));
        var mseed = BobServer.SeedFromMatchId(match.Id);
        using (var mr = JsonDocument.Parse(await srv.ScoreStackAsync(mseed, 1, "[\"cloth\"]", BobRules.RoundBudget, 0.5, false)))
            Console.WriteLine($"match {match.Id} seed {mseed}: score {mr.RootElement.GetProperty("score").GetProperty("total").GetInt32()}");
        var rnd = new Random(7); int compared = 0, mismatch = 0;
        for (int seed = 1; seed <= 25; seed++)
        for (int round = 1; round <= 3; round++)
        {
            using var room = JsonDocument.Parse(await srv.MakeRoundAsync(seed, round));
            var offer = room.RootElement.GetProperty("offer").EnumerateArray().Select(x => x.GetString()).ToList();
            var enemies = room.RootElement.GetProperty("enemies").EnumerateArray().Select(x => x.GetString()).ToList();
            var need = room.RootElement.GetProperty("spec").GetProperty("need").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32());
            for (int t = 0; t < 4; t++)
            {
                var stack = new List<string>(); int n = 1 + rnd.Next(5);
                for (int i = 0; i < n; i++)
                {
                    var id = rnd.Next(4) == 0 ? BobRules.Methods[rnd.Next(3)] : offer[rnd.Next(offer.Count)];
                    if (BobRules.WhyCannotPlace(stack, id, BobRules.RoundBudget - BobRules.StackCost(stack)) == null) stack.Add(id);
                }
                if (stack.Count == 0) continue;
                bool shield = rnd.Next(2) == 0; double tf = rnd.Next(0, 101) / 100.0;
                using var res = JsonDocument.Parse(await srv.ScoreStackAsync(seed, round, JsonSerializer.Serialize(stack), BobRules.RoundBudget, tf, shield));
                var ev = BobRules.EvalStack(stack, enemies, shield);
                var sc = BobRules.TotalScore(ev, need, BobRules.RoundBudget - ev.Cost, BobRules.RoundBudget, tf);
                var st = res.RootElement.GetProperty("stats");
                bool same = BobRules.StatKeys.All(k => st.GetProperty(k).GetInt32() == ev.Get(k))
                            && res.RootElement.GetProperty("score").GetProperty("total").GetInt32() == sc.Total
                            && res.RootElement.GetProperty("score").GetProperty("pass").GetBoolean() == sc.Pass;
                compared++; if (!same) { mismatch++; Console.WriteLine($"MISMATCH seed {seed} round {round}: {string.Join(",", stack)}"); }
            }
        }
        Console.WriteLine($"server parity: {compared} stacks compared, {mismatch} mismatches");

        // Offline rooms (BobMatch.MakeRound) must equal the server's bob_make_round for the same seed.
        int rooms = 0, roomMismatch = 0;
        foreach (var seed in new[] { 1, 2, 3, 7, 42, 999, 123456, 654321, 8388607 })
        for (int round = 1; round <= 3; round++)
        {
            using var room = JsonDocument.Parse(await srv.MakeRoundAsync(seed, round));
            var r = BobMatch.MakeRound(round, seed);
            var offer = room.RootElement.GetProperty("offer").EnumerateArray().Select(x => x.GetString()).ToList();
            var enemies = room.RootElement.GetProperty("enemies").EnumerateArray().Select(x => x.GetString()).ToList();
            var need = room.RootElement.GetProperty("spec").GetProperty("need").EnumerateObject().ToDictionary(p => p.Name, p => p.Value.GetInt32());
            bool same = offer.SequenceEqual(r.Offer) && enemies.SequenceEqual(r.Enemies)
                        && need.Count == r.Need.Count && need.All(kv => r.Need.TryGetValue(kv.Key, out var v) && v == kv.Value);
            rooms++; if (!same) { roomMismatch++; Console.WriteLine($"ROOM MISMATCH seed {seed} round {round}"); }
        }
        Console.WriteLine($"room parity: {rooms} rooms compared, {roomMismatch} mismatches");
        mismatch += roomMismatch;
        var rv = BobMatch.Rival(BobMatch.MakeRound(1, 42), 42, BobRules.RoundBudget);
        Check("rival plays a stack", rv != null && BobRules.CountLayers(rv.Stack) > 0);
        await srv.CloseAsync();
        return fails + mismatch == 0 ? 0 : 1;
    }
}
