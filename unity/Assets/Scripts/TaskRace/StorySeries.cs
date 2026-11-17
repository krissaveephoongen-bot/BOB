// P3 of docs 24 section 15.3: a story chapter is three rounds of 100 s, best of three (GDD 9). Winning a round by
// three tasks or more opens the chapter's challenge stage. The class is plain state so the editor check can play a
// whole chapter without entering play mode; TaskRaceModeManager feeds it the real round scores and keeps the
// two-losses-in-a-row mercy (+15 s on the next attempt) on the device.
using System;

namespace Bob.TaskRace
{
    public enum RoundOutcome { Win, Draw, Loss }

    /// <summary>One closed round of a story chapter, as the banner and the log see it.</summary>
    public readonly struct SeriesRound
    {
        public readonly int Number;
        public readonly int LocalTasks, RivalTasks, LocalPoints, RivalPoints;
        public readonly RoundOutcome Outcome;
        public readonly int LocalWins, RivalWins;

        public SeriesRound(int number, int localTasks, int rivalTasks, int localPoints, int rivalPoints,
                           RoundOutcome outcome, int localWins, int rivalWins)
        {
            Number = number;
            LocalTasks = localTasks; RivalTasks = rivalTasks;
            LocalPoints = localPoints; RivalPoints = rivalPoints;
            Outcome = outcome; LocalWins = localWins; RivalWins = rivalWins;
        }

        public override string ToString() =>
            $"round {Number}/{StorySeries.Rounds} {Outcome} · tasks {LocalTasks}:{RivalTasks} · " +
            $"points {LocalPoints}:{RivalPoints} · series {LocalWins}-{RivalWins}";
    }

    public sealed class StorySeries
    {
        public const int Rounds = 3;
        /// <summary>Winning a round by this many tasks offers the challenge stage (GDD 9).</summary>
        public const int MarginForChallenge = 3;

        readonly float roundSeconds;
        readonly int winsToTake;

        public StorySeries(float roundSeconds = 100f, int winsToTake = 2)
        {
            this.roundSeconds = Math.Max(10f, roundSeconds);
            this.winsToTake = Math.Max(1, winsToTake);
            SecondsForRound = this.roundSeconds;
        }

        /// <summary>1-based number of the round being played.</summary>
        public int Round { get; private set; } = 1;
        public int LocalWins { get; private set; }
        public int RivalWins { get; private set; }
        /// <summary>Tasks the local side cleaned across every closed round: the reward counts the whole chapter.</summary>
        public int TotalLocalTasks { get; private set; }
        public int TotalRivalTasks { get; private set; }
        /// <summary>Points the local side scored across every closed round.</summary>
        public int TotalLocalPoints { get; private set; }
        public int TotalRivalPoints { get; private set; }
        /// <summary>Emergency stains that showed up across the chapter's rounds (T16).</summary>
        public int EmergencySpawned { get; private set; }
        /// <summary>How many of those the team cleared inside the 25 s grace.</summary>
        public int EmergencyCleared { get; private set; }
        /// <summary>docs 16 section 2.5: the team quality star "จัดการฉุกเฉินทันทุกตัว". False when none showed up.</summary>
        public bool EmergencyClean => EmergencySpawned > 0 && EmergencyCleared >= EmergencySpawned;
        /// <summary>Cards the team opened and closed again before the job was done (docs 16 section 2.5, star 2).</summary>
        public int CardsAbandoned { get; private set; }
        /// <summary>How many of those jobs were picked up and finished later in the chapter.</summary>
        public int CardsRecovered { get; private set; }
        /// <summary>Closes that were never made good: the jobs left undone when their time ran out.</summary>
        public int AbandonedLeft => Math.Max(0, CardsAbandoned - CardsRecovered);
        /// <summary>docs 16 section 2.5 star 2 "ไม่มี task ที่ถูกปิดกลางคันแล้วหมดเวลาเก็บ".</summary>
        public bool NoAbandoned => AbandonedLeft == 0;
        /// <summary>Cart formulas locked across the chapter, and how many of them passed (star 3).</summary>
        public int CartRuns { get; private set; }
        public int CartPasses { get; private set; }
        /// <summary>docs 16 section 2.5 star 3 "รถเข็น T04 ผ่านสูตร": every locked formula of the chapter passed.
        /// A chapter that never locked one has no cart star, the same rule the emergency star follows.</summary>
        public bool CartClean => CartRuns > 0 && CartPasses >= CartRuns;
        /// <summary>Rounds actually closed, 0 before the first result.</summary>
        public int RoundsPlayed => LastRound.HasValue ? LastRound.Value.Number : 0;
        /// <summary>Clock of a round; the manager starts every round at this length.</summary>
        public float SecondsForRound { get; private set; }
        public bool ChallengeOffered { get; private set; }
        public SeriesRound? LastRound { get; private set; }
        /// <summary>
        /// The series was voided before it was decided because a player left the room mid-series (docs 27 section 10.7).
        /// Nobody takes it: the round that was in play pays as a draw, and the next series starts from round one.
        /// </summary>
        public bool Abandoned { get; private set; }
        /// <summary>Two wins either way, or all three rounds played, or a voided series: the series is closed.
        /// Counts rounds actually played: `Round` already points at the next one, so it cannot drive this test.</summary>
        public bool Over => Abandoned || (LastRound.HasValue &&
                            (LocalWins >= winsToTake || RivalWins >= winsToTake || LastRound.Value.Number >= Rounds));
        /// <summary>Taking the chapter needs <see cref="winsToTake"/> round wins: one win and two draws is not a win,
        /// and a voided series never has a winner.
        /// Owns the verdict so a series cannot be "taken" by having merely lost less.</summary>
        public bool LocalTookSeries => !Abandoned && LocalWins >= winsToTake;
        /// <summary>Won without dropping a round: the "never lost a round" star of the chapter card (A3).</summary>
        public bool CleanSweep => LocalTookSeries && RivalWins == 0;
        /// <summary>
        /// 0-3 stars for a closed chapter (A3, docs 11 S27): one for taking the chapter, one for never losing a
        /// round, one for winning a round by the challenge margin. A lost or drawn chapter earns none, so the stars
        /// say how the chapter was won without ever being the condition for winning it. A series still in play has
        /// none: leading 1-0 is not a closed chapter.
        /// </summary>
        public int Stars => !Over || !LocalTookSeries ? 0 : 1 + (CleanSweep ? 1 : 0) + (ChallengeOffered ? 1 : 0);
        public string Score => $"{LocalWins}-{RivalWins}";

        /// <summary>One stain came up; cleared says whether it was put down inside the grace (T16 feeds this).</summary>
        public void NoteEmergency(bool cleared)
        {
            EmergencySpawned++;
            if (cleared) EmergencyCleared++;
        }

        /// <summary>A task card was closed before the job was done (the × button, the joystick or a round end).</summary>
        public void NoteCardClosed() => CardsAbandoned++;

        /// <summary>An abandoned job was finished after all; it no longer counts against the star.</summary>
        public void NoteCardRecovered() => CardsRecovered++;

        /// <summary>The T04 formula was locked this round; passed says whether the stack met the difficulty need.</summary>
        public void NoteCart(bool passed)
        {
            CartRuns++;
            if (passed) CartPasses++;
        }

        public void Reset()
        {
            Round = 1; LocalWins = RivalWins = 0;
            EmergencySpawned = EmergencyCleared = 0;
            CardsAbandoned = CardsRecovered = 0;
            CartRuns = CartPasses = 0;
            TotalLocalTasks = TotalRivalTasks = TotalLocalPoints = TotalRivalPoints = 0;
            ChallengeOffered = false; LastRound = null; SecondsForRound = roundSeconds;
            Abandoned = false;
        }

        /// <summary>
        /// Void a series that is still in play (docs 27 section 10.7): a player left the room mid-series, so the room
        /// cannot keep playing a shared best-of-three. False when the series was already decided — a finished result
        /// is never taken back by a later departure.
        /// </summary>
        public bool Abandon()
        {
            if (Over) return false;
            Abandoned = true;
            return true;
        }

        /// <summary>The round's winner from the two task counts, points only breaking a task tie, then a draw.</summary>
        public static RoundOutcome OutcomeOf(int localTasks, int rivalTasks, int localPoints, int rivalPoints) =>
            localTasks > rivalTasks ? RoundOutcome.Win
          : localTasks < rivalTasks ? RoundOutcome.Loss
          : localPoints > rivalPoints ? RoundOutcome.Win
          : localPoints < rivalPoints ? RoundOutcome.Loss
          : RoundOutcome.Draw;

        /// <summary>Close the round on its task counts; points only break a task tie, and a tie on both is a draw.</summary>
        public SeriesRound Register(int localTasks, int rivalTasks, int localPoints, int rivalPoints) =>
            RegisterOutcome(OutcomeOf(localTasks, rivalTasks, localPoints, rivalPoints),
                localTasks, rivalTasks, localPoints, rivalPoints);

        /// <summary>
        /// Close a round whose winner was decided elsewhere — the online host is the one judge of a shared round
        /// (docs 27 section 10.7). The maths of the totals and the series score are the same as <see cref="Register"/>;
        /// callers must not register a round after <see cref="Over"/>, the room ignores stale results before this call.
        /// </summary>
        public SeriesRound RegisterOutcome(RoundOutcome outcome, int localTasks, int rivalTasks, int localPoints, int rivalPoints)
        {
            TotalLocalTasks += Math.Max(0, localTasks);
            TotalRivalTasks += Math.Max(0, rivalTasks);
            TotalLocalPoints += Math.Max(0, localPoints);
            TotalRivalPoints += Math.Max(0, rivalPoints);

            if (outcome == RoundOutcome.Win)
            {
                LocalWins++;
                if (localTasks - rivalTasks >= MarginForChallenge) ChallengeOffered = true;
            }
            else if (outcome == RoundOutcome.Loss) RivalWins++;

            var line = new SeriesRound(Round, localTasks, rivalTasks, localPoints, rivalPoints,
                outcome, LocalWins, RivalWins);
            LastRound = line;

            bool decided = LocalWins >= winsToTake || RivalWins >= winsToTake || Round >= Rounds;
            if (!decided) Round++;
            return line;
        }
    }
}
