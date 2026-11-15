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
        /// <summary>Clock of a round; the manager starts every round at this length.</summary>
        public float SecondsForRound { get; private set; }
        public bool ChallengeOffered { get; private set; }
        public SeriesRound? LastRound { get; private set; }
        /// <summary>Two wins either way, or all three rounds played: whoever has more rounds takes the chapter.
        /// Counts rounds actually played: `Round` already points at the next one, so it cannot drive this test.</summary>
        public bool Over => LastRound.HasValue &&
                            (LocalWins >= winsToTake || RivalWins >= winsToTake || LastRound.Value.Number >= Rounds);
        public bool LocalTookSeries => LocalWins > RivalWins;
        public string Score => $"{LocalWins}-{RivalWins}";

        public void Reset()
        {
            Round = 1; LocalWins = RivalWins = 0;
            ChallengeOffered = false; LastRound = null; SecondsForRound = roundSeconds;
        }

        /// <summary>Close the round on its task counts; points only break a task tie, and a tie on both is a draw.</summary>
        public SeriesRound Register(int localTasks, int rivalTasks, int localPoints, int rivalPoints)
        {
            var outcome = localTasks > rivalTasks ? RoundOutcome.Win
                        : localTasks < rivalTasks ? RoundOutcome.Loss
                        : localPoints > rivalPoints ? RoundOutcome.Win
                        : localPoints < rivalPoints ? RoundOutcome.Loss
                        : RoundOutcome.Draw;

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
