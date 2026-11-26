// Battle of the Brooms: one duo task's two hands (docs 32 section 4). Charge is normalised 0..1; an idle hand decays
// after a grace period, which is the whole "must do it together" rule in one place.
using System;

namespace Bob.Duo
{
    public sealed class BobDuoTaskState
    {
        readonly float[] charge = new float[2];
        readonly float[] idle = new float[2];

        public BobDuoTaskState(int index) { Index = index; }

        public int Index { get; }
        public bool Done { get; private set; }
        public float Progress => (float)Math.Min(charge[0], charge[1]);
        public bool IsComplete => charge[0] >= 1f && charge[1] >= 1f;

        public float Charge(int slot) => charge[slot & 1];
        public float Idle(int slot) => idle[slot & 1];

        /// <summary>Add normalised work for one hand. A tap or a drag delta is one Add call, a hold is Add(dt/need).</summary>
        public void Add(int slot, float amount)
        {
            if (Done || amount <= 0f) return;
            slot &= 1;
            charge[slot] = (float)Math.Min(1f, charge[slot] + amount);
            idle[slot] = 0f;
            // Both hands full is completion even if the frame ends before the next Tick.
            if (IsComplete)
            {
                Done = true;
                charge[0] = charge[1] = 1f;
            }
        }

        /// <summary>Called every frame on the host. `active` means that hand produced input this frame.</summary>
        public void Tick(float dt, bool active0, bool active1)
        {
            if (Done || dt <= 0f) return;
            TickSlot(0, dt, active0);
            TickSlot(1, dt, active1);
            if (IsComplete)
            {
                Done = true;
                charge[0] = charge[1] = 1f;
            }
        }

        /// <summary>Host snapshot applied on a client: the host's word on both halves and on completion.</summary>
        public void Restore(float charge0, float charge1, bool done)
        {
            charge[0] = Clamp01(charge0);
            charge[1] = Clamp01(charge1);
            idle[0] = idle[1] = 0f;
            Done = done || (charge[0] >= 1f && charge[1] >= 1f);
        }

        void TickSlot(int slot, float dt, bool active)
        {
            if (active) { idle[slot] = 0f; return; }
            idle[slot] += dt;
            if (idle[slot] > BobDuoRules.IdleAfter)
                charge[slot] = Math.Max(0f, charge[slot] - BobDuoRules.DecayPerSecond * dt);
        }

        static float Clamp01(float value) => value < 0f ? 0f : value > 1f ? 1f : value;
    }
}
