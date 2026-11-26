// Battle of the Brooms: M1 duo slice (docs 32). The host owns the clock and both hands; the client sends three kinds of
// input and renders the host's snapshot; with no room it runs the other hand as a bot so the slice is testable offline.
using System;
using UnityEngine;

namespace Bob.Duo
{
    public sealed class BobDuoMatch : MonoBehaviour
    {
        public const byte HoldCode = 0, TapCode = 1, DragCode = 2;
        const float SnapshotSeconds = 0.2f;
        const float BotSeconds = 0.35f;

        public readonly BobDuoTaskState[] Tasks = new BobDuoTaskState[BobDuoRules.TaskCount];

        public bool Running { get; private set; }
        public bool Over { get; private set; }
        public bool Won { get; private set; }
        public float Elapsed { get; private set; }
        public int TaskIndex { get; private set; }
        public int LocalSlot { get; private set; }
        public bool IsHost { get; private set; }
        public bool Online => room != null && room.InRoom && room.DuoStarting;
        public bool Solo => room == null || !room.InRoom;
        public DuoPhase Phase => BobDuoRules.PhaseAt(Elapsed);
        public float Percent
        {
            get
            {
                float sum = 0f;
                foreach (var task in Tasks) sum += task.Progress;
                return sum / BobDuoRules.TaskCount * 100f;
            }
        }

        Bob.Ugs.BobOnlineRoom room;
        BobDuoHud hud;
        readonly bool[] held = new bool[2];
        float snapAt, botAt, pendingDrag;
        bool botOn;

        public void Bind(BobDuoHud ui) => hud = ui;

        public void BeginOnline()
        {
            room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room == null || !room.InRoom)
            {
                BeginSoloSmoke();
                return;
            }
            IsHost = room.IsHost;
            LocalSlot = IsHost ? 0 : 1;
            // A rematch re-enters here on the same room; never stack a second subscription.
            room.DuoActionReceived -= OnAction;
            room.DuoSnapshotReceived -= OnSnapshot;
            room.DuoEnded -= OnEnded;
            room.DuoActionReceived += OnAction;
            room.DuoSnapshotReceived += OnSnapshot;
            room.DuoEnded += OnEnded;
            ResetMatch();
            Running = true;
            hud.ShowHud();
            Debug.Log("[Duo] match start · " + (IsHost ? "host slot 0" : "client slot 1") + " · " + room.Players + " players");
        }

        public void BeginSoloSmoke()
        {
            room = null;
            IsHost = true;
            LocalSlot = 0;
            botOn = true;
            ResetMatch();
            Running = true;
            hud.ShowHud();
            Debug.Log("[Duo] solo smoke · bot plays slot 1");
        }

        void OnDestroy()
        {
            if (room == null) return;
            room.DuoActionReceived -= OnAction;
            room.DuoSnapshotReceived -= OnSnapshot;
            room.DuoEnded -= OnEnded;
        }

        void ResetMatch()
        {
            for (int i = 0; i < Tasks.Length; i++) Tasks[i] = new BobDuoTaskState(i);
            Elapsed = 0f;
            TaskIndex = 0;
            Over = Won = false;
            held[0] = held[1] = false;
            pendingDrag = 0f;
            hud.RefreshHeader();
        }

        // ---------------------------------------------------------------- input from the local device

        public void Hold(int slot, bool down)
        {
            if (!Running || Over) return;
            held[slot & 1] = down;
            if (!IsHost && Online && (slot & 1) == LocalSlot) room.SendDuoAction(LocalSlot, HoldCode, down ? 1f : -1f);
        }

        public void Tap(int slot)
        {
            if (!Running || Over) return;
            slot &= 1;
            if (BobDuoRules.GestureFor(TaskIndex, slot) != DuoGesture.Rhythm) return;
            Tasks[TaskIndex].Add(slot, 1f / BobDuoRules.NeedFor(TaskIndex, slot));
            if (!IsHost && Online && slot == LocalSlot) room.SendDuoAction(LocalSlot, TapCode, 1f);
        }

        public void Drag(int slot, float units)
        {
            if (!Running || Over || units <= 0f) return;
            slot &= 1;
            if (BobDuoRules.GestureFor(TaskIndex, slot) != DuoGesture.Drag) return;
            Tasks[TaskIndex].Add(slot, units / BobDuoRules.NeedFor(TaskIndex, slot));
            if (!IsHost && Online && slot == LocalSlot) pendingDrag += units;
        }

        // ---------------------------------------------------------------- the clock

        void Update()
        {
            if (!Running || Over) return;

            if (Online && !IsHost)
            {
                // Optimistic local view; the host snapshot corrects it five times a second.
                Elapsed = Mathf.Min(Elapsed + Time.deltaTime, BobDuoRules.MatchSeconds);
                if (pendingDrag > 0f) { room.SendDuoAction(LocalSlot, DragCode, pendingDrag); pendingDrag = 0f; }
                var local = Tasks[TaskIndex];
                if (BobDuoRules.GestureFor(TaskIndex, LocalSlot) == DuoGesture.Hold && held[LocalSlot])
                    local.Add(LocalSlot, Time.deltaTime / BobDuoRules.NeedFor(TaskIndex, LocalSlot));
                local.Tick(Time.deltaTime, held[0], held[1]);
                if (local.Done && TaskIndex < BobDuoRules.TaskCount - 1) { TaskIndex++; held[0] = held[1] = false; }
                // A lost DuoEnd must not leave the client running: Finish() only sends when Online && IsHost.
                if (Elapsed >= BobDuoRules.MatchSeconds) { Finish(); return; }
                hud.RefreshMatch(this);
                return;
            }

            Elapsed += Time.deltaTime;

            // At a phase change, unfinished tasks are left behind (they still count toward %).
            if (Phase != DuoPhase.Done)
            {
                int first = BobDuoRules.FirstTaskOf(Phase);
                if (TaskIndex < first) TaskIndex = first;
            }

            if (botOn) BotStep(ref botAt, Time.deltaTime);

            var task = Tasks[Mathf.Min(TaskIndex, BobDuoRules.TaskCount - 1)];
            if (BobDuoRules.GestureFor(task.Index, LocalSlot) == DuoGesture.Hold && held[LocalSlot])
                task.Add(LocalSlot, Time.deltaTime / BobDuoRules.NeedFor(task.Index, LocalSlot));
            if (botOn && BobDuoRules.GestureFor(task.Index, 1) == DuoGesture.Hold && held[1])
                task.Add(1, Time.deltaTime / BobDuoRules.NeedFor(task.Index, 1));
            task.Tick(Time.deltaTime, held[0], held[1]);

            if (task.Done) NextTask();
            if (Elapsed >= BobDuoRules.MatchSeconds || AllDone()) Finish();

            if (Online && IsHost && Time.time >= snapAt)
            {
                snapAt = Time.time + SnapshotSeconds;
                room.SendDuoSnapshot(Snapshot());
            }

            hud.RefreshMatch(this);
        }

        void NextTask()
        {
            if (TaskIndex >= BobDuoRules.TaskCount - 1) { Finish(); return; }
            TaskIndex++;
            held[0] = held[1] = false;
            Debug.Log("[Duo] task done · now " + BobDuoRules.Task(TaskIndex).Name);
        }

        bool AllDone()
        {
            foreach (var task in Tasks) if (!task.Done) return false;
            return true;
        }

        void Finish()
        {
            if (Over) return;
            Over = true;
            Running = false;
            Won = AllDone();
            if (Online && IsHost) room.SendDuoEnd(Won, Percent);
            string detail = Won ? "ทำครบ 6 งาน · " + BobDuoRules.Clock(Elapsed) : "งานเสร็จ " + DoneCount() + "/6";
            hud.ShowResult(Won, Percent, detail);
            Debug.Log("[Duo] finish · " + detail + " · " + Percent.ToString("0") + "%");
        }

        int DoneCount()
        {
            int n = 0;
            foreach (var task in Tasks) if (task.Done) n++;
            return n;
        }

        // ---------------------------------------------------------------- host ↔ client

        void OnAction(int slot, byte code, float amount)
        {
            if (!IsHost || Over) return;
            switch (code)
            {
                case HoldCode: held[slot & 1] = amount > 0f; break;
                case TapCode: Tap(slot); break;
                case DragCode: Drag(slot, amount); break;
            }
        }

        void OnSnapshot(Bob.Ugs.BobOnlineRoom.DuoSnapshot snap)
        {
            if (IsHost) return;
            Elapsed = snap.Elapsed;
            TaskIndex = Mathf.Clamp(snap.Task, 0, BobDuoRules.TaskCount - 1);
            for (int i = 0; i < Tasks.Length; i++)
            {
                bool done = (snap.DoneMask & (1 << i)) != 0;
                Tasks[i].Restore(i == TaskIndex ? snap.Charge0 : (done ? 1f : 0f),
                                 i == TaskIndex ? snap.Charge1 : (done ? 1f : 0f), done);
            }
        }

        void OnEnded(bool won, float percent)
        {
            Over = true;
            Running = false;
            Won = won;
            hud.ShowResult(won, percent, won ? "ทำครบ 6 งาน" : "หมดเวลา");
        }

        Bob.Ugs.BobOnlineRoom.DuoSnapshot Snapshot()
        {
            int mask = 0;
            for (int i = 0; i < Tasks.Length; i++) if (Tasks[i].Done) mask |= 1 << i;
            var task = Tasks[Mathf.Min(TaskIndex, BobDuoRules.TaskCount - 1)];
            return new Bob.Ugs.BobOnlineRoom.DuoSnapshot
            {
                Charge0 = task.Charge(0),
                Charge1 = task.Charge(1),
                Elapsed = Elapsed,
                Task = TaskIndex,
                DoneMask = mask,
            };
        }

        // ---------------------------------------------------------------- the smoke-test bot

        void BotStep(ref float timer, float dt)
        {
            timer -= dt;
            if (timer > 0f) return;
            timer = BotSeconds;
            int index = Mathf.Min(TaskIndex, BobDuoRules.TaskCount - 1);
            switch (BobDuoRules.GestureFor(index, 1))
            {
                case DuoGesture.Hold: held[1] = !held[1]; break;
                case DuoGesture.Rhythm: held[1] = false; Tap(1); break;
                default: held[1] = false; Drag(1, BobDuoRules.NeedFor(index, 1) / 12f); break;
            }
        }
    }
}
