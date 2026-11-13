// Battle of the Brooms: one round's order and clock. Begin(need, seconds) at the start of a round, Tick(dt) every
// frame while the round is live. It owns the countdown (with the "stop the clock" freeze and pause), reports how close
// the stack is to the customer's spec, and raises events for the HUD and sound (low time, each whole second under the
// warning line, time up). It does not score: scoring stays in BobRules / the server.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bob.Modular
{
    [Serializable]
    public struct NeedProgress
    {
        public string Key;      // clean / gentle / speed / shine
        public int Current, Need;
        public bool Met => Current >= Need;
    }

    public class TaskManager : MonoBehaviour
    {
        [Tooltip("Seconds left at which the clock turns red and ticks")] public float warnAt = 15f;

        public float TimeLeft { get; set; }
        public float Duration { get; private set; } = 100f;
        public float Freeze { get; set; }
        public bool Paused { get; set; }
        public bool TimeUp { get; private set; }
        public bool Warning => TimeLeft <= warnAt && TimeLeft > 0;
        public float Fraction => Duration <= 0 ? 0 : Mathf.Clamp01(TimeLeft / Duration);

        public event Action TimeUpReached;
        public event Action<int> WarningTick;   // remaining whole seconds, once each, while under warnAt
        public event Action<float> Frozen;      // seconds the clock was stopped for

        readonly List<KeyValuePair<string, int>> need = new List<KeyValuePair<string, int>>();
        int lastTick = -1;

        public IReadOnlyList<KeyValuePair<string, int>> Need => need;

        /// <summary>Start a round: the spec as (stat key, minimum) pairs and the round length.</summary>
        public void Begin(IEnumerable<KeyValuePair<string, int>> spec, float seconds)
        {
            need.Clear(); if (spec != null) need.AddRange(spec);
            Duration = seconds; TimeLeft = seconds; Freeze = 0; TimeUp = false; lastTick = -1; Paused = false;
        }

        /// <summary>Stop the clock for a while (Press's ability).</summary>
        public void FreezeFor(float seconds) { Freeze = Mathf.Max(Freeze, seconds); Frozen?.Invoke(seconds); }

        public void Tick(float dt)
        {
            if (Paused || TimeUp) return;
            if (Freeze > 0) { Freeze = Mathf.Max(0, Freeze - dt); return; }
            TimeLeft -= dt;
            if (Warning)
            {
                int s = Mathf.CeilToInt(TimeLeft);
                if (s != lastTick) { lastTick = s; WarningTick?.Invoke(s); }
            }
            if (TimeLeft <= 0) { TimeLeft = 0; TimeUp = true; TimeUpReached?.Invoke(); }
        }

        /// <summary>Stat values (clean, gentle, speed, shine order matching `keys`) against the spec.</summary>
        public List<NeedProgress> Progress(string[] keys, int[] stats)
        {
            var list = new List<NeedProgress>(need.Count);
            foreach (var kv in need)
            {
                int i = Array.IndexOf(keys, kv.Key);
                list.Add(new NeedProgress { Key = kv.Key, Need = kv.Value, Current = i >= 0 && i < stats.Length ? stats[i] : 0 });
            }
            return list;
        }

        public bool AllMet(string[] keys, int[] stats)
        {
            foreach (var p in Progress(keys, stats)) if (!p.Met) return false;
            return true;
        }
    }
}
