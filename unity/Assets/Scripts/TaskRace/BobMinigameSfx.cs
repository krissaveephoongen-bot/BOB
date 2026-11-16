// Battle of the Brooms: the sound layer of the S28 card (docs 24 section 15.3 phase 5, docs 16 section 5).
// Every clip name below exists in Assets/Audio/SFX; the map is one line per interaction so a designer can
// audition the whole card from this file alone.
using System.Collections.Generic;
using Bob.Art;
using UnityEngine;

namespace Bob.TaskRace
{
    /// <summary>The eight MiniKind interactions mapped onto the shipped WAV set: a touch, a step, a failure and a
    /// finish. Continuous work (scrubbing, pouring) is a loop the card starts and stops, never a sound per tick.</summary>
    public static class BobMinigameSfx
    {
        static readonly Dictionary<string, float> last = new Dictionary<string, float>();

        /// <summary>The card just opened; the tutorial video plays over it.</summary>
        public static void Open() => BobSfx.Play("ui_card_flip", 0.75f);

        /// <summary>One successful touch of this step's interaction.</summary>
        public static void Touch(MiniKind kind, string taskId)
        {
            switch (kind)
            {
                case MiniKind.Trace: Once("broom_sweep", 0.12f, 0.7f); break;
                case MiniKind.Tap: BobSfx.Play(taskId == "T03" ? "mud_splat" : "pickup", 0.85f); break;
                case MiniKind.Sort: BobSfx.Play("ui_tap", 0.75f); break;
                case MiniKind.Hold: BobSfx.Play("lift_layer", 0.8f); break;
                case MiniKind.Sequence: BobSfx.Play("ui_confirm", 0.85f); break;
                case MiniKind.Fill: Once("bubble_pop", 0.3f, 0.5f); break;
            }
        }

        /// <summary>Sort: the card landed in the right bin or slid home from the wrong one.</summary>
        public static void Sorted(bool right)
        {
            if (right) BobSfx.Play("pass_part", 0.85f);
            else BobSfx.Play("drop_invalid", 0.8f);
        }

        /// <summary>A mistake that costs nothing but time (a wrong pad, an overflowing bottle).</summary>
        public static void Wrong(string clip = "drop_invalid") => BobSfx.Play(clip, 0.85f);

        /// <summary>One step of a multi-step card closed; the next step is being laid out.</summary>
        public static void StepDone() => BobSfx.Play("reaction_good", 0.85f);

        /// <summary>The whole card is solved (the spot still awards the points itself).</summary>
        public static void Finished()
        {
            BobSfx.Play("round_win", 0.8f);
            BobSfx.Play("score_count", 0.65f);
        }

        /// <summary>Sequence pads blink the memory pattern; a soft tick per blink, not per player tap.</summary>
        public static void Flash() => Once("ui_tap", 0.1f, 0.45f);

        public static void Scrub(bool on) => BobSfx.Loop("scrub_loop", on, 0.55f);
        public static void Pour(bool on) => BobSfx.Loop("water_pour", on, 0.7f);

        static void Once(string clip, float gap, float volume)
        {
            if (last.TryGetValue(clip, out var t) && Time.unscaledTime - t < gap) return;
            last[clip] = Time.unscaledTime;
            BobSfx.Play(clip, volume);
        }
    }
}
