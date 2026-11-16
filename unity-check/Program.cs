// Rules-only checks that run without Unity and without the game server:
// (1) spot checks for the stacking rules, (2) the Task Race plan/emergency table
// against the values the browser and server produce, (3) offline room/rival invariants.
// The Nakama client checks that used to live here went away with the pure-UGS migration;
// server behaviour is covered by server/task-race-test.js and the other node tests.
using System;
using System.Collections.Generic;
using System.Linq;
using Bob.Rules;

static class Program
{
    static int fails;
    static void Check(string name, bool ok) { if (!ok) { fails++; Console.WriteLine("FAIL " + name); } }

    static int Main()
    {
        var a = BobRules.EvalStack(new[] { "hot", "soap" });
        Check("hot under soap: clean 7", a.Get("clean") == 7);
        Check("soap under hot: speed -1", BobRules.EvalStack(new[] { "soap", "hot" }).Get("speed") == -1);
        Check("shield cancels worst reaction", BobRules.EvalStack(new[] { "soap", "hot" }, null, true).Get("speed") == 1);
        Check("grease with heat: clean 9", BobRules.EvalStack(new[] { "hot", "soap" }, new[] { "grease" }).Get("clean") == 9);
        Check("method cannot be first", BobRules.WhyCannotPlace(new List<string>(), "soak", 100) != null);
        Console.WriteLine("stacking spot checks done");

        foreach (var bad in BobTaskRaceRules.SelfTest())
        {
            fails++;
            Console.WriteLine("FAIL task race: " + bad);
        }
        Console.WriteLine("task race table checked");

        int rooms = 0;
        foreach (var seed in new[] { 1, 2, 3, 7, 42, 999, 123456, 654321, 8388607 })
        for (int round = 1; round <= 3; round++)
        {
            var r = BobMatch.MakeRound(round, seed);
            var again = BobMatch.MakeRound(round, seed);
            Check($"room {seed}/{round}: has offer", r.Offer != null && r.Offer.Length > 0);
            Check($"room {seed}/{round}: has enemies", r.Enemies != null && r.Enemies.Length > 0);
            Check($"room {seed}/{round}: has need", r.Need.Count > 0);
            Check($"room {seed}/{round}: deterministic",
                r.Offer.SequenceEqual(again.Offer) && r.Enemies.SequenceEqual(again.Enemies));
            rooms++;
        }
        Console.WriteLine($"offline rooms checked: {rooms}");

        var rv = BobMatch.Rival(BobMatch.MakeRound(1, 42), 42, BobRules.RoundBudget);
        Check("rival plays a stack", rv != null && BobRules.CountLayers(rv.Stack) > 0);

        Console.WriteLine(fails == 0 ? "UNITY-CHECK PASSED" : $"UNITY-CHECK FAILED: {fails}");
        return fails == 0 ? 0 : 1;
    }
}
