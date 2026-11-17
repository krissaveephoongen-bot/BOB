// Battle of the Brooms: checking the host's round line (docs 27 section 10.7, the "no server validation" gap in 10.6).
//
// The host is the only judge of a closed online round, and a client applies the host's numbers without recomputing
// them: that is what keeps a room from disagreeing about a score. The client can still notice when the line it was
// handed does not match the round it just played on its own island, and say so on the card instead of pretending
// everything agreed. Nothing here changes the result — the host's word still wins.
using System;

namespace Bob.Rules
{
    public static class BobSeriesCheck
    {
        /// <summary>
        /// The points a client is allowed to differ by before it counts as a dispute: points are built from this
        /// device's own clock (time left) and its own cart passes, so a second of drift is normal and is not cheating.
        /// The task count is not allowed to drift at all — stations are synced by `bob.task`.
        /// </summary>
        public const float PointTolerance = 0.10f;

        /// <summary>
        /// True when the host's line agrees with what this device saw for its own side. Only the local side is
        /// comparable: the rival island runs on the host, so its numbers are the host's word either way.
        /// </summary>
        public static bool Agrees(int seenTasks, int seenPoints, int lineTasks, int linePoints)
        {
            if (seenTasks != lineTasks) return false;
            int gap = Math.Abs(seenPoints - linePoints);
            int allowed = (int)(Math.Max(1, Math.Abs(linePoints)) * PointTolerance);
            return gap <= allowed;
        }

        /// <summary>
        /// The line the result card prints when the numbers did not match: one short sentence, no accusation, and
        /// the fact that the host still decided (docs 27 section 10.7).
        /// </summary>
        public static string DisputedLine(int disputes) =>
            disputes <= 0 ? "" : $"ผลไม่ตรงกับที่เห็น {disputes} รอบ · เจ้าของห้องตัดสิน";

        /// <summary>The same sentence as a toast/toast-length chip when only one round is in question.</summary>
        public static string DisputedChip => "ผลไม่ตรงกับที่เห็น · เจ้าของห้องตัดสิน";
    }
}
