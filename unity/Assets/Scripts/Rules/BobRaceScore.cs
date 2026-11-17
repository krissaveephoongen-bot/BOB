// Battle of the Brooms: the combined Task Race score (docs 16 section 2.6), pure C# with no Unity types so the phone,
// the browser prototype and the server can be checked against the same numbers (prototype taskRaceScore /
// server/modules/index.js). It is what one finished match — or one closed chapter — is worth on the leaderboard:
// work done, the cart's formula, the outcome and the clock left, scaled by difficulty.
namespace Bob.Rules
{
    public static class BobRaceScore
    {
        public const int PerTask = 100;
        public const int PerCartPass = 75;
        public const int WinBonus = 400;
        public const int DrawBonus = 150;
        public const int PerSecondLeft = 2;
        public const int MaxSecondsLeft = 120;
        public const int MaxCartPasses = 4;
        public const int DifficultyStepPercent = 20;

        /// <summary>
        /// tasks x100 + cart passes x75 + seconds left x2 + (win 400 / draw 150), then x1.00 / 1.20 / 1.40 by D1..D3.
        /// Integer maths only: the JS mirror floors the same division, so both sides print the same score.
        /// </summary>
        public static int Of(int tasksDone, int cartPasses, bool won, bool draw, int secondsLeft, int difficulty)
        {
            int tasks = Clamp(tasksDone, 0, 99);
            int cart = Clamp(cartPasses, 0, MaxCartPasses);
            int secs = Clamp(secondsLeft, 0, MaxSecondsLeft);
            int outcome = won ? WinBonus : draw ? DrawBonus : 0;
            int raw = tasks * PerTask + cart * PerCartPass + secs * PerSecondLeft + outcome;
            int percent = 100 + DifficultyStepPercent * (Clamp(difficulty, 1, 3) - 1);
            return raw * percent / 100;
        }

        /// <summary>The same score, written out for the log: which part earned what, so a board entry can be explained.</summary>
        public static string Explain(int tasksDone, int cartPasses, bool won, bool draw, int secondsLeft, int difficulty)
        {
            int tasks = Clamp(tasksDone, 0, 99);
            int cart = Clamp(cartPasses, 0, MaxCartPasses);
            int secs = Clamp(secondsLeft, 0, MaxSecondsLeft);
            int outcome = won ? WinBonus : draw ? DrawBonus : 0;
            int percent = 100 + DifficultyStepPercent * (Clamp(difficulty, 1, 3) - 1);
            return $"{tasks} task x{PerTask} + {cart} cart x{PerCartPass} + {secs}s x{PerSecondLeft} + {outcome} " +
                   $"x{percent}% = {Of(tasksDone, cartPasses, won, draw, secondsLeft, difficulty)}";
        }

        static int Clamp(int v, int lo, int hi) => v < lo ? lo : v > hi ? hi : v;
    }
}
