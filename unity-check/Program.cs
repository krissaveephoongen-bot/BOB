// Rules-only checks that run without Unity and without the game server:
// (1) spot checks for the stacking rules, (2) the Task Race plan/emergency table
// against the values the browser and server produce, (3) offline room/rival invariants.
// The Nakama client checks that used to live here went away with the pure-UGS migration;
// server behaviour is covered by server/task-race-test.js and the other node tests.
using System;
using System.Collections.Generic;
using System.Linq;
using Bob.Rules;
using Bob.TaskRace;

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

        // The combined race score (docs 16 2.6). Same hand-computed rows as server/task-race-test.js, so the phone,
        // the browser and the server are proven to print the same number for the same match.
        var scoreRows = new (int tasks, int cart, bool won, bool draw, int secs, int diff, int want)[]
        {
            (0, 0, false, false, 0, 1, 0),
            (5, 0, false, false, 0, 1, 500),
            (5, 2, false, false, 0, 1, 650),
            (5, 2, true, false, 0, 3, 1470),
            (8, 4, true, false, 60, 1, 1620),
            (0, 0, true, false, 120, 2, 768),
            (3, 0, false, true, 10, 1, 470),
            (7, 1, true, false, 30, 2, 1482),
            (1, 1, false, false, 1, 3, 247),
            (200, 9, true, false, 999, 3, 15176),
        };
        foreach (var row in scoreRows)
            Check($"score {row.tasks}t/{row.cart}c/{row.secs}s D{row.diff} = {row.want}",
                BobRaceScore.Of(row.tasks, row.cart, row.won, row.draw, row.secs, row.diff) == row.want);
        Check("score: the cart term stops at four passes",
            BobRaceScore.Of(6, 4, false, false, 0, 1) == BobRaceScore.Of(6, 9, false, false, 0, 1));
        Check("score: the clock term stops at the round length",
            BobRaceScore.Of(6, 0, false, false, 120, 1) == BobRaceScore.Of(6, 0, false, false, 400, 1));
        Check("score: D3 is worth 40% more than D1 on the same play",
            BobRaceScore.Of(10, 4, true, false, 0, 3) == BobRaceScore.Of(10, 4, true, false, 0, 1) * 14 / 10);
        Console.WriteLine("race score table checked");

        // The board card's bars (docs 31 section 3): length against the leader, a floor so a low score still reads.
        Check("board: the leader's bar is full", BobRaceScore.BarFraction(2268, 2268) == 1f);
        Check("board: half the score is half a bar", Math.Abs(BobRaceScore.BarFraction(810, 1620) - 0.5f) < 0.0001f);
        Check("board: a tiny score keeps a visible sliver", BobRaceScore.BarFraction(10, 2000) == 0.08f);
        Check("board: no score or no leader draws nothing",
            BobRaceScore.BarFraction(0, 100) == 0f && BobRaceScore.BarFraction(50, 0) == 0f);
        Check("board: a score past the leader stops at the track", BobRaceScore.BarFraction(9000, 1620) == 1f);
        Console.WriteLine("board bars checked");

        // The daily challenge (docs 31 2.2): the day turns at UTC+7 midnight and the seed belongs to the server.
        // Same six rows as server/daily-test.js, so a phone that drifts from GET /daily/seed fails this gate.
        long Ms(int y, int mo, int d, int h, int mi, int sec) =>
            new DateTimeOffset(new DateTime(y, mo, d, h, mi, sec, DateTimeKind.Utc)).ToUnixTimeMilliseconds();
        var noonTh = Ms(2026, 11, 17, 5, 0, 0);
        Check("daily: noon on 2026-11-17 is day 20774", BobDaily.DayNumber(noonTh) == 20774);
        Check("daily: the day turns at UTC+7 midnight",
            BobDaily.DayNumber(Ms(2026, 11, 17, 16, 59, 59)) == 20774 &&
            BobDaily.DayNumber(Ms(2026, 11, 17, 17, 0, 0)) == 20775);
        Check("daily: a fresh day lasts a whole day",
            BobDaily.UntilNextDayMs(Ms(2026, 11, 17, 17, 0, 0)) == BobDaily.DayMs);
        Check("daily: the day halves by noon", BobDaily.UntilNextDayMs(noonTh) == 43200000L);
        var seedRows = new (int day, int want)[]
        {
            (20774, 8131275), (20775, 1364361), (20776, 8028718),
            (0, 716302), (1, 4737385), (365, 3934344),
        };
        foreach (var row in seedRows)
            Check($"daily: seed of day {row.day} is {row.want}", BobDaily.SeedFor(row.day) == row.want);
        Check("daily: a seed is in 1..8388607",
            new[] { 0, 1, 20774, 20775, 99999, 1234567 }.All(d => BobDaily.SeedFor(d) >= 1 && BobDaily.SeedFor(d) <= 8388607));
        Check("daily: thirty days give thirty different seeds",
            Enumerable.Range(20774, 30).Select(BobDaily.SeedFor).Distinct().Count() == 30);
        Check("daily: the seed and the day agree",
            BobDaily.Matches(8131275, 20774) && !BobDaily.Matches(8131275, 20775) && !BobDaily.Matches(0, 20774));
        Check("daily: a new day refills the attempt",
            BobDaily.AttemptsLeft(20775, 20774, 1) == 1 && BobDaily.AttemptsLeft(20774, 20774, 0) == 1);
        Check("daily: a played day does not",
            BobDaily.AttemptsLeft(20774, 20774, 1) == 0 && BobDaily.AttemptsLeft(20774, 20774, 5) == 0);
        Console.WriteLine("daily seed table checked");

        // The cosmetics table (docs 31 2.3): decoration only, bought with counters the device already keeps. The whole
        // point is that no row asks for skill the game does not measure, and that progress never overshoots its target.
        Check("unlocks: six pieces with unique ids",
            BobUnlocks.All.Length == 6 && BobUnlocks.All.Select(d => d.Id).Distinct().Count() == 6);
        Check("unlocks: a brand new player has earned nothing", BobUnlocks.EarnedCount(new BobUnlockStats(0, 0, 0, 0, 0)) == 0);
        Check("unlocks: three matches earn the sweep title",
            BobUnlocks.Earned(BobUnlocks.Get("title_sweep"), new BobUnlockStats(3, 0, 0, 0, 0)));
        Check("unlocks: two matches do not",
            !BobUnlocks.Earned(BobUnlocks.Get("title_sweep"), new BobUnlockStats(2, 0, 0, 0, 0)));
        Check("unlocks: one win earns the teal frame",
            BobUnlocks.Earned(BobUnlocks.Get("frame_teal"), new BobUnlockStats(0, 1, 0, 0, 0)));
        Check("unlocks: 900 best points earn the sun frame, 899 do not",
            BobUnlocks.Earned(BobUnlocks.Get("frame_sun"), new BobUnlockStats(0, 0, 900, 0, 0)) &&
            !BobUnlocks.Earned(BobUnlocks.Get("frame_sun"), new BobUnlockStats(0, 0, 899, 0, 0)));
        Check("unlocks: three days of daily earn the steady title",
            BobUnlocks.Earned(BobUnlocks.Get("title_steady"), new BobUnlockStats(0, 0, 0, 0, 3)) &&
            !BobUnlocks.Earned(BobUnlocks.Get("title_steady"), new BobUnlockStats(0, 0, 0, 0, 2)));
        Check("unlocks: three cleared chapters earn the clean title",
            BobUnlocks.Earned(BobUnlocks.Get("title_clean"), new BobUnlockStats(0, 0, 0, 3, 0)));
        Check("unlocks: progress stops at the target",
            BobUnlocks.Progress(BobUnlocks.Get("frame_sun"), new BobUnlockStats(0, 0, 5000, 0, 0)) == 900);
        Check("unlocks: the progress line reads as asked",
            BobUnlocks.Requirement(BobUnlocks.Get("title_steady"), new BobUnlockStats(0, 0, 0, 0, 1)) == "วันเดลี่ 1/3");
        Check("unlocks: missing lists only what is earned and not owned",
            BobUnlocks.Missing(new BobUnlockStats(3, 1, 0, 0, 0), new List<string> { "title_sweep" })
                .SequenceEqual(new[] { "frame_teal" }));
        Check("unlocks: a finished player earns all six",
            BobUnlocks.EarnedCount(new BobUnlockStats(9, 9, 2000, 9, 9)) == 6);
        Check("unlocks: every frame carries a colour and every title does not",
            BobUnlocks.All.Where(d => !d.Title).All(d => d.Hex.Length == 7 && d.Hex[0] == '#') &&
            BobUnlocks.All.Where(d => d.Title).All(d => d.Hex.Length == 0));
        Console.WriteLine("unlock table checked");

        // The T04 cart wears the worn frame colour as paint only (docs 31 section 2.3). The card draws ink text on the
        // cart body, so the paint is a tint and not a repaint: every frame in the table must leave the body light
        // enough to stay readable, and a bare cart must keep the cream it shipped with.
        static bool Same(BobRgb a, BobRgb b) =>
            Math.Abs(a.R - b.R) < 1e-4f && Math.Abs(a.G - b.G) < 1e-4f && Math.Abs(a.B - b.B) < 1e-4f;
        Check("paint: a bare cart keeps the shipped cream body",
            Same(BobCartPaint.Body(false, new BobRgb(0.1f, 0.2f, 0.3f)), BobCartPaint.Cream));
        Check("paint: the shipped cream body is readable on its own", BobCartPaint.Readable(BobCartPaint.Cream));
        Check("paint: every frame colour shows on the body",
            BobUnlocks.All.Where(d => !d.Title)
                .All(d => !Same(BobCartPaint.Body(true, BobCartPaint.ParseHex(d.Hex).Value), BobCartPaint.Cream)));
        Check("paint: a painted body stays light enough for ink text",
            BobUnlocks.All.Where(d => !d.Title)
                .All(d => BobCartPaint.Readable(BobCartPaint.Body(true, BobCartPaint.ParseHex(d.Hex).Value))));
        Check("paint: the weight is a tint, never a repaint", BobCartPaint.Weight > 0f && BobCartPaint.Weight <= 0.5f);
        Check("paint: the hex reader takes the table's colours and refuses junk",
            BobCartPaint.ParseHex("#2f7f6f").HasValue &&
            Math.Abs(BobCartPaint.ParseHex("#2f7f6f").Value.G - 0x7f / 255f) < 1e-4f &&
            !BobCartPaint.ParseHex("2F7F6F").HasValue && !BobCartPaint.ParseHex("#GGGGGG").HasValue &&
            !BobCartPaint.ParseHex("#2F7F6").HasValue);
        Console.WriteLine("cart paint checked");

        // S31, the wardrobe (docs 31 section 2.3): the two tabs partition the six-piece table, each piece says where it
        // shows, and a cloud profile can never make the device wear something it does not own.
        var titles = BobUnlocks.Of(true);
        var frames = BobUnlocks.Of(false);
        Check("wardrobe: the two tabs cover every piece exactly once",
            titles.Length + frames.Length == BobUnlocks.All.Length &&
            titles.All(d => d.Title) && frames.All(d => !d.Title));
        Check("wardrobe: both tabs have pieces", titles.Length == 3 && frames.Length == 3);
        Check("wardrobe: every piece says where it shows",
            BobUnlocks.All.All(d => !string.IsNullOrEmpty(BobUnlocks.Where(d))) &&
            BobUnlocks.Where(BobUnlocks.Get("title_sweep")) != BobUnlocks.Where(BobUnlocks.Get("frame_teal")));
        Check("wardrobe: a merge keeps both sides and drops repeats",
            BobUnlocks.MergeOwned(new[] { "frame_teal", "title_sweep" }, new[] { "title_sweep", "frame_sun" })
                .SequenceEqual(new[] { "frame_teal", "title_sweep", "frame_sun" }));
        Check("wardrobe: empty ids never enter the owned list", BobUnlocks.MergeOwned(new[] { "" }, null).Length == 0);
        Check("wardrobe: the device keeps a worn id it still owns",
            BobUnlocks.ResolveWorn("frame_sun", "frame_pink", false, new[] { "frame_sun", "frame_pink" }) == "frame_sun");
        Check("wardrobe: an empty device adopts the cloud's id",
            BobUnlocks.ResolveWorn("", "frame_pink", false, new[] { "frame_pink" }) == "frame_pink");
        Check("wardrobe: an unowned cloud id is refused",
            BobUnlocks.ResolveWorn("", "frame_pink", false, new[] { "frame_sun" }) == "");
        Check("wardrobe: a piece cannot fill the other slot",
            BobUnlocks.ResolveWorn("", "title_sweep", false, new[] { "title_sweep" }) == "" &&
            BobUnlocks.ResolveWorn("", "frame_sun", true, new[] { "frame_sun" }) == "");
        Check("wardrobe: a stale local id falls back to the cloud's",
            BobUnlocks.ResolveWorn("frame_pink", "frame_sun", false, new[] { "frame_sun" }) == "frame_sun");
        Console.WriteLine("wardrobe checked");


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

        // The story chapter verdict (StorySeries): two round wins take the chapter, and the chapter card's stars hang
        // off the same closed series. The editor check also plays this in Unity; these keep the rules honest offline.
        var oneWin = new StorySeries(100f, 2);
        oneWin.Register(6, 5, 0, 0);
        oneWin.Register(5, 5, 0, 0);
        oneWin.Register(5, 5, 0, 0);
        Check("series: one win and two draws do not take the chapter",
            oneWin.Over && oneWin.Score == "1-0" && !oneWin.LocalTookSeries);
        Check("series: a chapter not taken pays no stars", oneWin.Stars == 0);

        var sweep = new StorySeries(100f, 2);
        sweep.Register(6, 3, 0, 0);
        sweep.Register(6, 4, 0, 0);
        Check("series: 2-0 with a three-task win is a clean sweep",
            sweep.Over && sweep.LocalTookSeries && sweep.CleanSweep && sweep.ChallengeOffered && sweep.Stars == 3);
        Check("series: the decided series does not point past round two", sweep.Round == 2);

        var close = new StorySeries(100f, 2);
        close.Register(6, 5, 0, 0);
        close.Register(5, 6, 0, 0);
        close.Register(6, 5, 0, 0);
        Check("series: 2-1 takes the chapter with one star", close.LocalTookSeries && !close.CleanSweep && close.Stars == 1);

        var totals = new StorySeries(100f, 2);
        totals.Register(6, 4, 60, 40);
        totals.Register(5, 7, 50, 70);
        Check("series: totals sum every closed round",
            totals.TotalLocalTasks == 11 && totals.TotalRivalTasks == 11 &&
            totals.TotalLocalPoints == 110 && totals.TotalRivalPoints == 110 && totals.RoundsPlayed == 2);
        totals.Reset();
        Check("series: reset clears the totals",
            totals.TotalLocalTasks == 0 && totals.TotalLocalPoints == 0 && totals.RoundsPlayed == 0 && totals.Score == "0-0");

        // The online best-of-three (docs 27 section 10.7): the room's series is the same StorySeries, and the host is
        // the one judge — a round closed with RegisterOutcome counts the outcome the host saw, whatever this device's
        // own counts were. The void rule is the leave rule: a series still in play can be abandoned, a decided one can not.
        Check("online: the round outcome follows tasks first",
            StorySeries.OutcomeOf(6, 5, 0, 0) == RoundOutcome.Win &&
            StorySeries.OutcomeOf(5, 6, 0, 0) == RoundOutcome.Loss);
        Check("online: points break a task tie",
            StorySeries.OutcomeOf(5, 5, 60, 40) == RoundOutcome.Win &&
            StorySeries.OutcomeOf(5, 5, 40, 60) == RoundOutcome.Loss);
        Check("online: a tie on both is a draw", StorySeries.OutcomeOf(5, 5, 50, 50) == RoundOutcome.Draw);

        var hostWord = new StorySeries(100f, 2);
        var hostLine = hostWord.RegisterOutcome(RoundOutcome.Win, 4, 9, 0, 0);
        Check("online: the host's outcome counts even when the counts disagree",
            hostLine.Outcome == RoundOutcome.Win && hostLine.LocalWins == 1 && hostLine.RivalWins == 0);
        hostWord.RegisterOutcome(RoundOutcome.Win, 2, 8, 0, 0);
        Check("online: two host wins close the series at round two",
            hostWord.Over && hostWord.LocalTookSeries && hostWord.RoundsPlayed == 2 && hostWord.Score == "2-0");
        Check("online: a client applying a stale line cannot reopen a closed round",
            hostWord.Round == 2 && hostWord.LastRound.Value.Number == 2);

        var voided = new StorySeries(100f, 2);
        voided.Register(6, 5, 0, 0);
        Check("online: a live series can be voided", voided.Abandon() && voided.Abandoned && voided.Over);
        Check("online: a voided series has no winner",
            !voided.LocalTookSeries && voided.Stars == 0 && voided.Score == "1-0" && voided.RoundsPlayed == 1);

        var lateLeave = new StorySeries(100f, 2);
        lateLeave.Register(6, 5, 0, 0);
        lateLeave.Register(6, 5, 0, 0);
        Check("online: a decided series keeps its winner when someone leaves",
            !lateLeave.Abandon() && !lateLeave.Abandoned && lateLeave.LocalTookSeries && lateLeave.Score == "2-0");

        var emptyVoid = new StorySeries(100f, 2);
        Check("online: leaving before the first result still voids the series",
            emptyVoid.Abandon() && emptyVoid.Over && !emptyVoid.LocalTookSeries && emptyVoid.RoundsPlayed == 0);

        var withDraw = new StorySeries(100f, 2);
        withDraw.Register(6, 6, 70, 70);
        withDraw.Register(3, 4, 0, 0);
        withDraw.Register(7, 6, 0, 0);
        Check("online: a draw is a played round but no win",
            withDraw.Over && withDraw.RoundsPlayed == 3 && withDraw.LocalWins == 1 && withDraw.RivalWins == 1 &&
            !withDraw.LocalTookSeries);

        voided.Reset();
        Check("online: reset clears the void flag", !voided.Abandoned && !voided.Over && voided.Score == "0-0");

        var margin = new StorySeries(100f, 2);
        margin.Register(6, 4, 0, 0);
        Check("series: two tasks do not open the challenge", !margin.ChallengeOffered);
        margin.Register(6, 3, 0, 0);
        Check("series: exactly three tasks does", margin.ChallengeOffered);

        // The T16 quality star (docs 16 section 2.5): a chapter counts every stain of every round, and only a
        // chapter where all of them were cleared earns it. No stains at all is not "clean".
        var quiet = new StorySeries(100f, 2);
        Check("series: a chapter without stains has no emergency star", !quiet.EmergencyClean && quiet.EmergencySpawned == 0);
        quiet.NoteEmergency(true);
        quiet.NoteEmergency(false);
        quiet.NoteEmergency(true);
        Check("series: one spread stain costs the emergency star",
            quiet.EmergencySpawned == 3 && quiet.EmergencyCleared == 2 && !quiet.EmergencyClean);
        var spotless = new StorySeries(100f, 2);
        spotless.NoteEmergency(true);
        spotless.NoteEmergency(true);
        Check("series: every stain cleared earns it", spotless.EmergencyClean && spotless.EmergencyCleared == 2);
        spotless.Register(6, 5, 0, 0);
        spotless.Reset();
        Check("series: reset clears the emergency tally",
            spotless.EmergencySpawned == 0 && spotless.EmergencyCleared == 0 && !spotless.EmergencyClean);

        // Star 2 (docs 16 section 2.5): a card closed before the job was done only costs the star if nobody finishes
        // that job later in the chapter.
        var tidy = new StorySeries(100f, 2);
        Check("series: a chapter that never closed a card keeps the no-abandon star", tidy.NoAbandoned && tidy.CardsAbandoned == 0);
        tidy.NoteCardClosed();
        tidy.NoteCardClosed();
        Check("series: two closes with nothing recovered cost it",
            tidy.CardsAbandoned == 2 && tidy.AbandonedLeft == 2 && !tidy.NoAbandoned);
        tidy.NoteCardRecovered();
        Check("series: one job picked up again still leaves a close open",
            tidy.AbandonedLeft == 1 && !tidy.NoAbandoned);
        tidy.NoteCardRecovered();
        Check("series: every close forgiven earns it back", tidy.AbandonedLeft == 0 && tidy.NoAbandoned);
        tidy.NoteCardRecovered();
        Check("series: a spare recovery never goes negative", tidy.AbandonedLeft == 0);

        // Star 3: only the cart formulas the team actually locked count, and all of them must pass.
        var carts = new StorySeries(100f, 2);
        Check("series: a chapter without a cart lock has no cart star", !carts.CartClean && carts.CartRuns == 0);
        carts.NoteCart(true);
        carts.NoteCart(false);
        Check("series: one failed formula costs the cart star",
            carts.CartRuns == 2 && carts.CartPasses == 1 && !carts.CartClean);
        carts.NoteCart(true);
        Check("series: 2/3 is still not every formula", !carts.CartClean);
        carts.Reset();
        Check("series: reset clears the cart tally", carts.CartRuns == 0 && carts.CartPasses == 0 && !carts.CartClean);
        carts.NoteCart(true);
        Check("series: every formula passed earns the star", carts.CartClean && carts.CartPasses == 1);

        // The three quality stars are reports only: they must not change the chapter's own 0-3 stars.
        var plain = new StorySeries(100f, 2);
        plain.Register(6, 4, 0, 0);
        plain.Register(6, 4, 0, 0);
        int starsBefore = plain.Stars;
        plain.NoteCardClosed();
        plain.NoteEmergency(false);
        plain.NoteCart(false);
        Check("series: quality stars never move the chapter stars", plain.Stars == starsBefore && plain.Stars == 2);

        // Docs 29 section 6 item 7: the tagline is settled text that both result cards draw. The narrow chip beside
        // the chapter stars is 300u wide with 24pt Thai, so the line has to stay short, and it has to stay inside the
        // Thai block: the font set carries no emoji and no star glyph, so anything outside U+0E00-U+0E7F draws boxes.
        Check("story: the tagline is the settled words", BobStoryText.Slogan == "งานสะอาดต้องทำสามรอบ");
        Check("story: the tagline fits the narrow chip", BobStoryText.Slogan.Length <= 20);
        Check("story: the tagline uses Thai glyphs only", BobStoryText.Slogan.All(c => c <= '\u0E7F'));

        Console.WriteLine(fails == 0 ? "UNITY-CHECK PASSED" : $"UNITY-CHECK FAILED: {fails}");
        return fails == 0 ? 0 : 1;
    }
}
