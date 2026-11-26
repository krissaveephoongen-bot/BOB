# BOB M1 · Duo Co-op Vertical Slice — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship the M1 vertical slice of the 2v2 concept: one room, six tasks, two players on two devices (or one device with a smoke-test bot), landscape 19.5:9, no prank system.

**Architecture:** A new self-contained scene `DuoClean` builds its whole UI at runtime (the project has no prefabs; every screen is uGUI built from code in `BobUi`). Pure rules live in `Rules/` so `unity-check` can prove the timeline, task table, role swap and "both hands" charge rule without Unity. Networking adds one named-message channel (`bob.duo*`) to the existing `Bob.Ugs.BobOnlineRoom` (Relay + Netcode, no NetworkObjects). The host simulates time and both players' charge; the client sends input events and renders host snapshots.

**Tech Stack:** Unity 6000.6.8f1, uGUI + TextMeshPro, C# (LangVersion 9.0 for the offline check project), UGS Auth/Lobby/Relay 1.3.0/1.2.0, Netcode for GameObjects 2.14.0.

**Spec:** `art/docs/32_Coop_Concept_Reset.md` (sections 3, 4, 6, 7, 8) + `art/docs/ux/BOB_2v2_Wireframes.html` (landscape frames A, B1, B2).

## Global Constraints

- **Landscape only.** Canvas stays 1920×1080, `CanvasScaler` ScaleWithScreenSize, match 0.5; every element inside 64 u side / 48 u top-bottom safe margins so 19.5:9 phones show the same layout. No portrait code paths.
- **Match length 240 s** (ด้วยกัน 75 + แยกฝั่ง 105 + ด้วยกัน 60) — inside the 4–5 minute band of spec §3.
- **Exactly three gestures:** hold, tap-to-rhythm, drag. No walking, inventory, cart, pranks, per-player score, hero abilities, matchmaking, voice chat (spec §7 cut list).
- **Roles swap every task:** task 0 first slot = slot A, task 1 = slot B, task 2 = A … so neither device always gives orders (spec §4 failure rules).
- **No game over.** A miss costs time only; unfinished tasks count as partial % at the final bell (spec §4).
- **Thai UI copy, English identifiers.** Copy comes from the spec tables verbatim where they exist (`คราบติดพื้น`, `แยกฝั่ง`, `รับมือ` is *not* in M1).
- **This slice adds nothing to saved progression.** No coins, XP, stars, leaderboard writes.
- **Approval rules in force:** no `git commit`/`push`, no file deletion, no `rm`, one Unity Editor instance at a time (check `COORDINATION.md` before opening Unity; write only this session's row).
- **UGS scripts are outside `unity-typecheck`.** Compile evidence for `BobOnlineRoom.cs` and the `Duo/` MonoBehaviours comes from opening Unity once per task group; the offline rules gate is the fast loop.
- **This machine has no .NET 8 runtime** (SDK 11, runtimes 10/11 only). Run the offline gate as `cd unity-check && DOTNET_ROLL_FORWARD=Major dotnet run`; plain `dotnet run` dies with “framework 8.0.0 not found”. Verified 25 Nov 2026: the roll-forward run prints `UNITY-CHECK PASSED`, and `bash unity-typecheck/check.sh` prints `typecheck ok: 43 known Unity-6 gap kind(s), no new errors`.

## File Structure

| File | Responsibility |
| --- | --- |
| `unity/Assets/Scripts/Rules/BobDuoRules.cs` | Timeline constants, 6-task table, phase/task mapping, slot↔role swap, copy, self-test |
| `unity/Assets/Scripts/Rules/BobDuoTaskState.cs` | One task's two charges, idle decay, completion, snapshot restore |
| `unity-check/BobCheck.csproj` | Add the two rules files to the offline compile |
| `unity-check/Program.cs` | Run `BobDuoRules.SelfTest()` with the other tables |
| `unity/Assets/Scripts/UGS/BobOnlineRoom.cs` | `bob.duostart` / `bob.duoact` / `bob.duosnap` / `bob.duoend` channel |
| `unity/Assets/Scripts/Duo/BobDuoMatch.cs` | Phase clock, host simulation, client input+snapshot, solo bot, result |
| `unity/Assets/Scripts/Duo/BobDuoGestures.cs` | Hold / rhythm / drag pointer components |
| `unity/Assets/Scripts/Duo/BobDuoHud.cs` | Lobby panel, match HUD, result panel (built from code) |
| `unity/Assets/Scripts/Duo/BobDuoBootstrap.cs` | Scene entry: builds canvas content, wires match↔hud, Back button |
| `unity/Assets/Scripts/Duo/BobDuoDevCheck.cs` | Play-mode self-check + screenshot used as evidence |
| `unity/Assets/Scripts/Editor/BobDuoSceneBuilder.cs` | Bakes `Assets/Scenes/DuoClean.unity` + Build Settings + check menu |
| `unity/Assets/Scripts/Game/BobGame.Menus.cs` | S14 button that opens the slice |

No new art assets. The room backdrop reuses `lobby_rental_room` through `BobUi.Img`; task icons use existing sprites (`icon_*`, hero portraits) only if already present — otherwise text only, which is acceptable for M1.

---

### Task 1: Pure duo rules + task state, proven by `unity-check`

**Files:**
- Create: `unity/Assets/Scripts/Rules/BobDuoRules.cs`
- Create: `unity/Assets/Scripts/Rules/BobDuoTaskState.cs`
- Modify: `unity-check/BobCheck.csproj` (the `<Compile Include=...>` list)
- Modify: `unity-check/Program.cs` (after the `task race table checked` line)

**Interfaces:**
- Consumes: nothing (pure C#, no `UnityEngine`).
- Produces: `Bob.Duo.DuoPhase`, `Bob.Duo.DuoGesture`, `Bob.Duo.DuoTaskDef`, `Bob.Duo.BobDuoRules` (`Together1Seconds=75f`, `SplitSeconds=105f`, `Together2Seconds=60f`, `MatchSeconds=240f`, `TaskCount=6`, `IdleAfter=2.5f`, `DecayPerSecond=0.35f`, `Task(int)`, `PhaseAt(float)`, `FirstTaskOf(DuoPhase)`, `PrimarySlot(int)`, `RoleFor(int,int)`, `GestureFor(int,int)`, `NeedFor(int,int)`, `HintFor(int,int)`, `PhaseThai(DuoPhase)`, `SelfTest()`), and `Bob.Duo.BobDuoTaskState` (`Charge(int)`, `Progress`, `Idle(int)`, `IsComplete`, `Done`, `Add(int,float)`, `Tick(float,bool,bool)`, `Restore(float,float,bool)`).

- [ ] **Step 1: Write the failing checks first**

In `unity-check/Program.cs`, directly after `Console.WriteLine("task race table checked");`, add:

```csharp
        // The M1 duo slice (docs 32 sections 3 and 4): timeline, the six tasks, the role swap, and the rule that one
        // player alone cannot finish anything.
        foreach (var bad in BobDuoRules.SelfTest())
        {
            fails++;
            Console.WriteLine("FAIL duo: " + bad);
        }
        Console.WriteLine("duo slice table checked");
```

and add `using Bob.Duo;` next to the other `using` lines at the top of the file.

- [ ] **Step 2: Run the check to prove it fails to compile**

Run: `cd unity-check && DOTNET_ROLL_FORWARD=Major dotnet run`
Expected: `error CS0103: The name 'BobDuoRules' does not exist in the current context`.

- [ ] **Step 3: Add the task table and timeline**

Create `unity/Assets/Scripts/Rules/BobDuoRules.cs`:

```csharp
// Battle of the Brooms: the M1 duo slice (docs 32 sections 3-4). One room, six tasks, three gestures, two players.
// Pure C# so unity-check can prove the timeline and the table without Unity or a server.
using System;
using System.Collections.Generic;

namespace Bob.Duo
{
    public enum DuoPhase { Together1, Split, Together2, Done }

    /// <summary>The only three inputs the slice allows (docs 32 section 4).</summary>
    public enum DuoGesture { Hold, Rhythm, Drag }

    /// <summary>Slot 0 is the host device, slot 1 the joining device; which of them gives the first role swaps per task.</summary>
    public struct DuoTaskDef
    {
        public string Id, Name;
        public string PrimaryRole, SecondaryRole;
        public DuoGesture Primary, Secondary;
        public float PrimaryNeed, SecondaryNeed; // seconds of hold / good taps / drag units
    }

    public static class BobDuoRules
    {
        public const float Together1Seconds = 75f;
        public const float SplitSeconds = 105f;
        public const float Together2Seconds = 60f;
        public const float MatchSeconds = Together1Seconds + SplitSeconds + Together2Seconds; // 240 = 4:00
        public const int TaskCount = 6;
        public const float IdleAfter = 2.5f;
        public const float DecayPerSecond = 0.35f;

        // docs 32 section 4, row for row. Both columns of a task are shown, so a miss always reads as "the work", never
        // as the partner's fault.
        public static readonly DuoTaskDef[] Tasks =
        {
            new DuoTaskDef { Id = "duo.floor",   Name = "คราบติดพื้น", PrimaryRole = "ฉีดน้ำยาให้ทั่ว",   Primary = DuoGesture.Hold,   PrimaryNeed = 3.0f,  SecondaryRole = "ถูตามจังหวะ",   Secondary = DuoGesture.Rhythm, SecondaryNeed = 8f  },
            new DuoTaskDef { Id = "duo.trash",   Name = "เก็บขยะ",     PrimaryRole = "ลากถุงขยะ",        Primary = DuoGesture.Drag,   PrimaryNeed = 2600f, SecondaryRole = "โยนขยะลงถุง", Secondary = DuoGesture.Rhythm, SecondaryNeed = 8f  },
            new DuoTaskDef { Id = "duo.mirror",  Name = "เช็ดกระจก",   PrimaryRole = "ฉีดน้ำยาให้ทั่วแผง", Primary = DuoGesture.Hold,   PrimaryNeed = 2.5f,  SecondaryRole = "เช็ดตามแนว",   Secondary = DuoGesture.Drag,   SecondaryNeed = 2400f },
            new DuoTaskDef { Id = "duo.dishes",  Name = "ล้างจาน",     PrimaryRole = "ล้างจาน",          Primary = DuoGesture.Drag,   PrimaryNeed = 2800f, SecondaryRole = "เช็ดและวาง",   Secondary = DuoGesture.Rhythm, SecondaryNeed = 10f },
            new DuoTaskDef { Id = "duo.laundry", Name = "ตากผ้า",      PrimaryRole = "บิดผ้า",           Primary = DuoGesture.Hold,   PrimaryNeed = 3.0f,  SecondaryRole = "ตากและหนีบ",   Secondary = DuoGesture.Drag,   SecondaryNeed = 2600f },
            new DuoTaskDef { Id = "duo.ceiling", Name = "ฝุ่นเพดาน",   PrimaryRole = "ประคองบันได",      Primary = DuoGesture.Hold,   PrimaryNeed = 3.5f,  SecondaryRole = "ปัดฝุ่น",      Secondary = DuoGesture.Rhythm, SecondaryNeed = 12f },
        };

        public static DuoTaskDef Task(int index) => Tasks[Clamp(index, 0, TaskCount - 1)];

        public static DuoPhase PhaseAt(float elapsed)
        {
            if (elapsed < Together1Seconds) return DuoPhase.Together1;
            if (elapsed < Together1Seconds + SplitSeconds) return DuoPhase.Split;
            if (elapsed < MatchSeconds) return DuoPhase.Together2;
            return DuoPhase.Done;
        }

        /// <summary>First task that belongs to a phase: task 0 with-together, 1-4 split, 5 with-together again.</summary>
        public static int FirstTaskOf(DuoPhase phase)
        {
            switch (phase)
            {
                case DuoPhase.Together1: return 0;
                case DuoPhase.Split: return 1;
                case DuoPhase.Together2: return 5;
                default: return TaskCount;
            }
        }

        /// <summary>The slot that owns the primary role of a task. Alternating is what makes the two players swap hands.</summary>
        public static int PrimarySlot(int taskIndex) => (taskIndex & 1) == 0 ? 0 : 1;

        public static bool IsPrimary(int taskIndex, int slot) => slot == PrimarySlot(taskIndex);

        public static DuoGesture GestureFor(int taskIndex, int slot) =>
            IsPrimary(taskIndex, slot) ? Task(taskIndex).Primary : Task(taskIndex).Secondary;

        public static float NeedFor(int taskIndex, int slot) =>
            IsPrimary(taskIndex, slot) ? Task(taskIndex).PrimaryNeed : Task(taskIndex).SecondaryNeed;

        public static string RoleFor(int taskIndex, int slot) =>
            IsPrimary(taskIndex, slot) ? Task(taskIndex).PrimaryRole : Task(taskIndex).SecondaryRole;

        public static string HintFor(int taskIndex, int slot) => GestureFor(taskIndex, slot) switch
        {
            DuoGesture.Hold => "กดค้างบนการ์ดจนกว่าแถบจะเต็ม",
            DuoGesture.Rhythm => "แตะการ์ดตามจังหวะ ตรงกลางวงจะติด",
            _ => "ลากนิ้วบนการ์ดให้ทั่ว",
        };

        public static string PhaseThai(DuoPhase phase)
        {
            switch (phase)
            {
                case DuoPhase.Together1: return "ด้วยกัน";
                case DuoPhase.Split: return "แยกฝั่ง";
                case DuoPhase.Together2: return "ด้วยกันอีกครั้ง";
                default: return "จบ";
            }
        }

        /// <summary>Clock text for the HUD: 4:00 -> 0:00.</summary>
        public static string Clock(float elapsed)
        {
            float left = MatchSeconds - elapsed;
            if (left < 0f) left = 0f;
            int seconds = (int)Math.Ceiling(left);
            return (seconds / 60) + ":" + (seconds % 60).ToString("00");
        }

        /// <summary>Every assertion the offline gate runs. Returns one line per failure; empty means green.</summary>
        public static IEnumerable<string> SelfTest()
        {
            var bad = new List<string>();
            void Check(string name, bool ok) { if (!ok) bad.Add(name); }

            Check("match lasts 4:00", MatchSeconds == 240f);
            Check("phases add up", Together1Seconds + SplitSeconds + Together2Seconds == MatchSeconds);
            Check("first phase at 0", PhaseAt(0f) == DuoPhase.Together1);
            Check("with-together until 1:15", PhaseAt(74.99f) == DuoPhase.Together1);
            Check("split starts at 1:15", PhaseAt(75f) == DuoPhase.Split);
            Check("split until 3:00", PhaseAt(179.99f) == DuoPhase.Split);
            Check("final together at 3:00", PhaseAt(180f) == DuoPhase.Together2);
            Check("final together until 4:00", PhaseAt(239.99f) == DuoPhase.Together2);
            Check("bell at 4:00", PhaseAt(240f) == DuoPhase.Done);
            Check("done stays done", PhaseAt(999f) == DuoPhase.Done);

            Check("first split task is 1", FirstTaskOf(DuoPhase.Split) == 1);
            Check("final task is 5", FirstTaskOf(DuoPhase.Together2) == 5);
            Check("six tasks", Tasks.Length == TaskCount);

            for (int i = 0; i < TaskCount; i++)
            {
                var t = Task(i);
                Check("task " + i + " has an id", !string.IsNullOrEmpty(t.Id));
                Check("task " + i + " has a name", !string.IsNullOrEmpty(t.Name));
                Check("task " + i + " needs two roles", !string.IsNullOrEmpty(t.PrimaryRole) && !string.IsNullOrEmpty(t.SecondaryRole) && t.PrimaryRole != t.SecondaryRole);
                Check("task " + i + " halves differ", t.Primary != t.Secondary);
                Check("task " + i + " needs are positive", t.PrimaryNeed > 0f && t.SecondaryNeed > 0f);
                Check("task " + i + " swaps hands", PrimarySlot(i) == (i % 2 == 0 ? 0 : 1));
                Check("task " + i + " is two-handed", RoleFor(i, 0) != RoleFor(i, 1));
            }

            Check("only hold/rhythm/drag appear", UsesOnlyThreeGestures());
            Check("clock starts at 4:00", Clock(0f) == "4:00");
            Check("clock ends at 0:00", Clock(240f) == "0:00");

            // A task cannot be finished by one hand, and an idle hand drags the work back (spec section 4).
            var solo = new BobDuoTaskState(0);
            solo.Add(0, 1f);
            for (int i = 0; i < 60; i++) solo.Tick(0.1f, true, false);
            Check("one hand cannot finish", !solo.Done && solo.Charge(1) == 0f);

            var pair = new BobDuoTaskState(2);
            pair.Add(0, 1f); pair.Add(1, 1f);
            Check("both hands finish", pair.Done && pair.IsComplete);
            Check("finished progress is full", pair.Progress == 1f);

            var decay = new BobDuoTaskState(1);
            decay.Add(0, 0.9f); decay.Add(1, 0.9f);
            for (int i = 0; i < 30; i++) decay.Tick(0.1f, true, false); // 3 s: 0.5 s past the grace
            Check("idle hand loses progress", decay.Charge(1) < 0.9f && decay.Charge(1) > 0.7f);
            Check("idle hand stops the task", !decay.Done && decay.Progress < 0.9f);
            decay.Add(0, 1f); decay.Add(1, 1f);
            Check("a returning hand finishes it", decay.Done);

            var idle = new BobDuoTaskState(3);
            idle.Add(0, 0.5f);
            for (int i = 0; i < 100; i++) idle.Tick(0.1f, false, false);
            Check("progress never goes negative", idle.Charge(0) == 0f);

            var restored = new BobDuoTaskState(4);
            restored.Restore(0.4f, 0.6f, false);
            Check("snapshot restore keeps both halves", Math.Abs(restored.Charge(0) - 0.4f) < 0.0001f && Math.Abs(restored.Charge(1) - 0.6f) < 0.0001f);
            restored.Restore(1f, 1f, true);
            Check("snapshot restore can close a task", restored.Done);

            return bad;
        }

        static bool UsesOnlyThreeGestures()
        {
            foreach (var t in Tasks)
                foreach (var g in new[] { t.Primary, t.Secondary })
                    if (g != DuoGesture.Hold && g != DuoGesture.Rhythm && g != DuoGesture.Drag) return false;
            return true;
        }

        static int Clamp(int value, int min, int max) => value < min ? min : value > max ? max : value;
    }
}
```

- [ ] **Step 4: Add the state machine**

Create `unity/Assets/Scripts/Rules/BobDuoTaskState.cs`:

```csharp
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
```

- [ ] **Step 5: Add both files to the offline check project**

In `unity-check/BobCheck.csproj`, extend the single `<Compile Include=...>` string with:

```
../unity/Assets/Scripts/Rules/BobDuoRules.cs;../unity/Assets/Scripts/Rules/BobDuoTaskState.cs
```

(keep the existing entries; append before the closing quote).

- [ ] **Step 6: Run the gate**

Run: `cd unity-check && DOTNET_ROLL_FORWARD=Major dotnet run`
Expected: `duo slice table checked` and `UNITY-CHECK PASSED`, no `FAIL duo:` lines.

- [ ] **Step 7: Type-check the Unity scripts**

Run: `bash unity-typecheck/check.sh`
Expected: `typecheck ok: ... no new errors` (the two new Rules files compile in both gates; nothing in `Duo/` exists yet).

---

### Task 2: Duo message channel in the online room

**Files:**
- Modify: `unity/Assets/Scripts/UGS/BobOnlineRoom.cs`

**Interfaces:**
- Consumes: existing `nm`, `InRoom`, `IsHost`, `Wire(NetworkManager)`, named-message patterns at lines ~40-46 and ~410-415.
- Produces: `public const string DuoScene = "DuoClean"`, `public struct DuoSnapshot { float Charge0, Charge1, Elapsed; int Task, DoneMask; }`, `public event Action<int,byte,float> DuoActionReceived`, `public event Action<DuoSnapshot> DuoSnapshotReceived`, `public event Action DuoStarted`, `public event Action<bool,float> DuoEnded`, `public bool DuoStarting`, `public bool StartDuo(int players)`, `public void SendDuoAction(int slot, byte code, float amount)`, `public void SendDuoSnapshot(DuoSnapshot snap)`, `public void SendDuoEnd(bool won, float percent)`.

- [ ] **Step 1: Add the message ids next to the existing ones**

After `const string AbandonMessage = "bob.abandon";` add:

```csharp
        // docs 32 M1: one channel for the duo slice. Actions are small and frequent; snapshots are the host's truth.
        const string DuoStartMessage = "bob.duostart";
        const string DuoActionMessage = "bob.duoact";
        const string DuoSnapshotMessage = "bob.duosnap";
        const string DuoEndMessage = "bob.duoend";
```

- [ ] **Step 2: Add the scene constant and the public surface**

After `public const string MatchScene = "TaskRace";`:

```csharp
        /// <summary>The M1 duo slice scene (docs 32 section 8).</summary>
        public const string DuoScene = "DuoClean";
```

Before the `// ---------------------------------------------------------------- match handoff` section add:

```csharp
        // ---------------------------------------------------------------- duo slice (docs 32 M1)

        /// <summary>Two numbers for the current task plus the authoritative clock; everything a client draws.</summary>
        public struct DuoSnapshot
        {
            public float Charge0, Charge1, Elapsed;
            public int Task, DoneMask;
        }

        /// <summary>slot 0/1, code 0 = hold start(+1)/stop(-1), 1 = good tap, 2 = drag units.</summary>
        public event Action<int, byte, float> DuoActionReceived;
        public event Action<DuoSnapshot> DuoSnapshotReceived;
        public event Action DuoStarted;
        public event Action<bool, float> DuoEnded;

        /// <summary>A duo match is running in this room (clients learn it from bob.duostart).</summary>
        public bool DuoStarting { get; private set; }
        public bool DuoOver { get; private set; }

        /// <summary>Host starts the duo slice. Two players only: the slice has exactly two roles.</summary>
        public bool StartDuo(int players)
        {
            if (!InRoom || !IsHost || nm == null || players != 2) return false;
            DuoStarting = true;
            DuoOver = false;
            using (var writer = new FastBufferWriter(4, Allocator.Temp))
            {
                writer.WriteValueSafe(players);
                nm.CustomMessagingManager.SendNamedMessageToAll(DuoStartMessage, writer, NetworkDelivery.ReliableSequenced);
            }
            return true;
        }

        /// <summary>Input from a device: the host applies it, a client sends it to the host.</summary>
        public void SendDuoAction(int slot, byte code, float amount)
        {
            if (nm == null || !DuoStarting || DuoOver) return;
            // A tap must never be lost (it is a whole 1/8 of a task); holds and drags are continuous and lossy is fine.
            var delivery = code == 1 ? NetworkDelivery.ReliableSequenced : NetworkDelivery.Unreliable;
            using (var writer = new FastBufferWriter(16, Allocator.Temp))
            {
                writer.WriteValueSafe(slot);
                writer.WriteValueSafe(code);
                writer.WriteValueSafe(amount);
                if (nm.IsHost)
                    nm.CustomMessagingManager.SendNamedMessageToAll(DuoActionMessage, writer, delivery);
                else
                    nm.CustomMessagingManager.SendNamedMessage(DuoActionMessage, NetworkManager.ServerClientId, writer, delivery);
            }
        }

        /// <summary>Host broadcast, five times a second (the client lerps between them).</summary>
        public void SendDuoSnapshot(DuoSnapshot snap)
        {
            if (nm == null || !nm.IsHost) return;
            using (var writer = new FastBufferWriter(24, Allocator.Temp))
            {
                writer.WriteValueSafe(snap.Charge0);
                writer.WriteValueSafe(snap.Charge1);
                writer.WriteValueSafe(snap.Elapsed);
                writer.WriteValueSafe(snap.Task);
                writer.WriteValueSafe(snap.DoneMask);
                nm.CustomMessagingManager.SendNamedMessageToAll(DuoSnapshotMessage, writer, NetworkDelivery.ReliableSequenced);
            }
        }

        public void SendDuoEnd(bool won, float percent)
        {
            if (nm == null || !nm.IsHost) return;
            DuoOver = true;
            using (var writer = new FastBufferWriter(8, Allocator.Temp))
            {
                writer.WriteValueSafe(won);
                writer.WriteValueSafe(percent);
                nm.CustomMessagingManager.SendNamedMessageToAll(DuoEndMessage, writer, NetworkDelivery.ReliableSequenced);
            }
        }
```

- [ ] **Step 3: Add the four handlers**

Place them next to `HandleStart`:

```csharp
        void HandleDuoStart(ulong sender, FastBufferReader reader)
        {
            if (nm == null || nm.IsHost || sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out int players);
            if (players != 2) return;
            DuoStarting = true;
            DuoOver = false;
            DuoStarted?.Invoke();
        }

        void HandleDuoAction(ulong sender, FastBufferReader reader)
        {
            if (nm == null || !DuoStarting) return;
            if (!nm.IsHost) return; // only the host simulates; clients consume snapshots
            reader.ReadValueSafe(out int slot);
            reader.ReadValueSafe(out byte code);
            reader.ReadValueSafe(out float amount);
            DuoActionReceived?.Invoke(slot & 1, code, amount);
        }

        void HandleDuoSnapshot(ulong sender, FastBufferReader reader)
        {
            if (nm == null || nm.IsHost || sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out float c0);
            reader.ReadValueSafe(out float c1);
            reader.ReadValueSafe(out float elapsed);
            reader.ReadValueSafe(out int task);
            reader.ReadValueSafe(out int mask);
            DuoSnapshotReceived?.Invoke(new DuoSnapshot { Charge0 = c0, Charge1 = c1, Elapsed = elapsed, Task = task, DoneMask = mask });
        }

        void HandleDuoEnd(ulong sender, FastBufferReader reader)
        {
            if (nm == null || nm.IsHost || sender != NetworkManager.ServerClientId) return;
            reader.ReadValueSafe(out bool won);
            reader.ReadValueSafe(out float percent);
            DuoStarting = false;
            DuoOver = true;
            DuoEnded?.Invoke(won, percent);
        }
```

- [ ] **Step 4: Register the handlers**

In `Wire(...)` after the `AbandonMessage` registration:

```csharp
            net.CustomMessagingManager.RegisterNamedMessageHandler(DuoStartMessage, HandleDuoStart);
            net.CustomMessagingManager.RegisterNamedMessageHandler(DuoActionMessage, HandleDuoAction);
            net.CustomMessagingManager.RegisterNamedMessageHandler(DuoSnapshotMessage, HandleDuoSnapshot);
            net.CustomMessagingManager.RegisterNamedMessageHandler(DuoEndMessage, HandleDuoEnd);
```

- [ ] **Step 5: Compile evidence**

Open the project in Unity (one editor only — check `COORDINATION.md` first) and confirm the Console has no errors in `BobOnlineRoom.cs`.
Expected: no compiler errors. (UGS scripts are excluded from `unity-typecheck`; this manual compile is the gate for this task.) Run `bash unity-typecheck/check.sh` too — it must stay green.

---

### Task 3: Duo scene, bootstrap, and the static landscape HUD

**Files:**
- Create: `unity/Assets/Scripts/Duo/BobDuoBootstrap.cs`
- Create: `unity/Assets/Scripts/Duo/BobDuoHud.cs` (layout half; live values land in Task 5)
- Create: `unity/Assets/Scripts/Editor/BobDuoSceneBuilder.cs`
- Create: `unity/Assets/Scenes/DuoClean.unity` (generated by the builder)

**Interfaces:**
- Consumes: `BobUi` helpers (`Rect`, `Stretch`, `Pin`, `Panel`, `Label`, `Button`, `Disc`, `ToTmp`), `BobSceneLoader.Load`.
- Produces: `Bob.Duo.BobDuoBootstrap` (component in the scene), `Bob.Duo.BobDuoHud.Build(Canvas, BobDuoMatch)`, and the scene path `Assets/Scenes/DuoClean.unity`.

- [ ] **Step 1: Write the HUD layout with live labels wired to stub values**

Create `unity/Assets/Scripts/Duo/BobDuoHud.cs`. This step builds the whole landscape frame once; Task 5 fills the input areas and Task 6 binds real numbers.

```csharp
// Battle of the Brooms: the M1 duo slice HUD (docs 32 section 6, wireframe frames B1/B2). Landscape 1920x1080, built
// from code like every other screen in the project. Lobby panel first, match view behind it, result panel on top.
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bob.Duo
{
    public sealed class BobDuoHud : MonoBehaviour
    {
        sealed class Slot
        {
            public GameObject panel;
            public TMP_Text title, role, hint, partner, state;
            public Image meter, partnerBar;
            public RectTransform pad;
            public GameObject rhythmRing;
        }

        Canvas canvas;
        BobDuoMatch match;
        GameObject lobby, matchRoot, resultRoot;
        TMP_Text clock, phase, progress, code, status, resultTitle, resultLine;
        Image[] pips = new Image[BobDuoRules.TaskCount];
        readonly Slot mine = new Slot(), theirs = new Slot();
        Button startButton, hostButton, joinButton;
        string typedCode = "";
        bool busy;

        public void Build(Canvas target, BobDuoMatch owner)
        {
            canvas = target;
            match = owner;
            BuildLobby();
            BuildMatch();
            BuildResult();
            ShowLobby(true);
        }

        void BuildLobby()
        {
            lobby = Frame("Lobby"); // Frame already fills with BobUi.Bg; no second backdrop
            var card = BobUi.Panel(lobby.transform, BobUi.Surface);
            BobUi.Pin((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 720));
            var c = card.transform;
            var top = new Vector2(0.5f, 1f);

            var title = BobUi.Label(c, "ห้องคู่ · 2 คน", 64, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(title.rectTransform, top, top, new Vector2(0, -36), new Vector2(1000, 84));

            var subtitle = BobUi.Label(c, "หนึ่งห้อง 6 งาน 4 นาที · สองเครื่อง สองบทบาท", 26, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(subtitle.rectTransform, top, top, new Vector2(0, -128), new Vector2(1000, 40));

            hostButton = BobUi.Button(c, "สร้างห้อง", BobUi.Sun, HostRoom, 32);
            BobUi.Pin((RectTransform)hostButton.transform, top, top, new Vector2(-190, -200), new Vector2(340, 104));
            joinButton = BobUi.Button(c, "ใส่รหัสห้อง", BobUi.Surface, JoinRoom, 32, BobUi.Ink);
            BobUi.Pin((RectTransform)joinButton.transform, top, top, new Vector2(190, -200), new Vector2(340, 104));

            code = BobUi.Label(c, "", 52, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(code.rectTransform, top, top, new Vector2(0, -324), new Vector2(900, 70));

            status = BobUi.Label(c, "", 24, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(status.rectTransform, top, top, new Vector2(0, -400), new Vector2(980, 44));

            startButton = BobUi.Button(c, "เริ่มเมื่อครบ 2 คน", BobUi.Teal, StartPressed, 40, BobUi.Surface);
            BobUi.Pin((RectTransform)startButton.transform, top, top, new Vector2(0, -462), new Vector2(460, 104));
            startButton.interactable = false;

            var solo = BobUi.Button(c, "ทดลองคนเดียว (คู่เป็นบอท)", BobUi.Surface, SoloPressed, 26, BobUi.Ink);
            BobUi.Pin((RectTransform)solo.transform, top, top, new Vector2(0, -586), new Vector2(460, 88));

            var foot = BobUi.Label(c, "แนวนอน 19.5:9 · ทีมเดียวกันเห็นความคืบหน้ากัน · ไม่มีป่วนในรอบนี้", 22, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(foot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0, 28), new Vector2(1000, 34));
        }

        void BuildMatch()
        {
            matchRoot = Frame("Match");
            var bar = BobUi.Panel(matchRoot.transform, BobUi.Surface);
            BobUi.Pin((RectTransform)bar.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -48), new Vector2(1792, 96));
            clock = BobUi.Label(bar.transform, "4:00", 52, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(clock.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(180, 76));
            phase = BobUi.Label(bar.transform, "ด้วยกัน", 30, BobUi.Teal, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(phase.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(220, 0), new Vector2(320, 76));
            progress = BobUi.Label(bar.transform, "งาน 1/6 · 0%", 26, BobUi.Muted, TextAnchor.MiddleRight);
            BobUi.Pin(progress.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(520, 76));

            var card = BobUi.Panel(matchRoot.transform, BobUi.Surface);
            BobUi.Pin((RectTransform)card.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(64, -176), new Vector2(1080, 700));
            mine.panel = card.gameObject;
            mine.title = BobUi.Label(card.transform, "คราบติดพื้น", 56, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(mine.title.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -32), new Vector2(1000, 70));
            mine.role = BobUi.Label(card.transform, "ฉีดน้ำยาให้ทั่ว", 34, BobUi.Teal, TextAnchor.MiddleLeft, FontStyle.Bold);
            BobUi.Pin(mine.role.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -110), new Vector2(1000, 50));
            mine.hint = BobUi.Label(card.transform, "", 24, BobUi.Muted);
            BobUi.Pin(mine.hint.rectTransform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(40, -162), new Vector2(1000, 40));

            // The gesture area: 940x330 in the middle of the card.
            var pad = BobUi.Panel(card.transform, BobUi.Cream);
            BobUi.Pin((RectTransform)pad.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -220), new Vector2(940, 330));
            mine.pad = (RectTransform)pad.transform;

            var track = BobUi.Panel(card.transform, BobUi.Line);
            BobUi.Pin((RectTransform)track.transform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 42), new Vector2(940, 34));
            mine.meter = BobUi.Panel(track.transform, BobUi.Teal);
            BobUi.Stretch((RectTransform)mine.meter.transform, 0, 0, 0, 0);
            mine.meter.type = Image.Type.Filled;
            mine.meter.fillMethod = Image.FillMethod.Horizontal;
            mine.meter.fillAmount = 0f;

            var side = BobUi.Panel(matchRoot.transform, BobUi.Surface);
            BobUi.Pin((RectTransform)side.transform, new Vector2(1, 1), new Vector2(1, 1), new Vector2(-64, -176), new Vector2(630, 700));
            var s = side.transform;
            var st = new Vector2(0f, 1f);
            void At(TMP_Text label, float top, float height, int size, TextAnchor align)
            {
                label.fontSize = size;
                label.alignment = BobUi.ToTmp(align);
                BobUi.Pin(label.rectTransform, st, st, new Vector2(28, -top), new Vector2(560, height));
            }

            var mateLabel = BobUi.Label(s, "คู่หูของคุณ", 30, BobUi.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(mateLabel, 24, 40, 30, TextAnchor.MiddleLeft);
            theirs.panel = side.gameObject;
            theirs.title = BobUi.Label(s, "เพื่อน", 44, BobUi.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(theirs.title, 72, 60, 44, TextAnchor.MiddleLeft);
            theirs.role = BobUi.Label(s, "", 28, BobUi.Teal, TextAnchor.MiddleLeft);
            At(theirs.role, 140, 44, 28, TextAnchor.MiddleLeft);
            theirs.hint = BobUi.Label(s, "", 22, BobUi.Muted, TextAnchor.UpperLeft);
            At(theirs.hint, 190, 56, 22, TextAnchor.UpperLeft);
            theirs.state = BobUi.Label(s, "ยังไม่เริ่ม", 24, BobUi.Muted, TextAnchor.MiddleLeft);
            At(theirs.state, 252, 36, 24, TextAnchor.MiddleLeft);

            var pbar = BobUi.Panel(s, BobUi.Line);
            BobUi.Pin((RectTransform)pbar.transform, st, st, new Vector2(28, -302), new Vector2(560, 30));
            theirs.partnerBar = BobUi.Panel(pbar.transform, BobUi.Green);
            BobUi.Stretch((RectTransform)theirs.partnerBar.transform);
            theirs.partnerBar.type = Image.Type.Filled;
            theirs.partnerBar.fillMethod = Image.FillMethod.Horizontal;
            theirs.partnerBar.fillAmount = 0f;

            var pipsLabel = BobUi.Label(s, "งานทั้ง 6", 26, BobUi.Muted, TextAnchor.MiddleLeft, FontStyle.Bold);
            At(pipsLabel, 360, 36, 26, TextAnchor.MiddleLeft);
            for (int i = 0; i < pips.Length; i++)
            {
                var pip = BobUi.Panel(s, BobUi.Line);
                BobUi.Pin((RectTransform)pip.transform, st, st, new Vector2(28 + i * 88, -408), new Vector2(64, 40));
                pips[i] = pip;
            }

            var sideFoot = BobUi.Label(s, "ทำครบ 6 งานก่อนชนะ\nหมดเวลาเทียบ % ความสะอาด", 22, BobUi.Muted, TextAnchor.UpperLeft);
            BobUi.Pin(sideFoot.rectTransform, st, st, new Vector2(28, -500), new Vector2(560, 110));
        }

        void BuildResult()
        {
            resultRoot = Frame("Result");
            var card = BobUi.Panel(resultRoot.transform, BobUi.Surface);
            BobUi.Pin((RectTransform)card.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000, 620));
            var c = card.transform;
            var top = new Vector2(0.5f, 1f);

            resultTitle = BobUi.Label(c, "จบรอบ", 64, BobUi.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BobUi.Pin(resultTitle.rectTransform, top, top, new Vector2(0, -56), new Vector2(880, 90));
            resultLine = BobUi.Label(c, "", 30, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(resultLine.rectTransform, top, top, new Vector2(0, -178), new Vector2(880, 110));

            var again = BobUi.Button(c, "เล่นอีกครั้ง", BobUi.Sun, AgainPressed, 40);
            BobUi.Pin((RectTransform)again.transform, top, top, new Vector2(0, -330), new Vector2(460, 104));
            var home = BobUi.Button(c, "กลับหน้าหลัก", BobUi.Surface, HomePressed, 28, BobUi.Ink);
            BobUi.Pin((RectTransform)home.transform, top, top, new Vector2(0, -462), new Vector2(460, 88));
        }

        GameObject Frame(string name)
        {
            var panel = BobUi.Rect(name, canvas.transform);
            BobUi.Stretch(panel);
            panel.gameObject.AddComponent<Image>().color = BobUi.Bg;
            return panel.gameObject;
        }

        public void ShowLobby(bool show)
        {
            lobby.SetActive(show);
            matchRoot.SetActive(!show);
            resultRoot.SetActive(false);
        }

        public void ShowHud()
        {
            lobby.SetActive(false);
            matchRoot.SetActive(true);
            resultRoot.SetActive(false);
        }

        public void ShowResult(bool won, float percent, string detail)
        {
            lobby.SetActive(false);
            matchRoot.SetActive(false);
            resultRoot.SetActive(true);
            resultTitle.text = won ? "สะอาดครบ!" : "หมดเวลา";
            resultTitle.color = won ? BobUi.Good : BobUi.Ink;
            resultLine.text = detail + " · ความสะอาด " + Mathf.RoundToInt(percent) + "%";
        }

        // The lobby buttons are wired here so the frame is provably interactive; their bodies arrive in Task 4.
        void HostRoom() { }
        void JoinRoom() { }
        void StartPressed() { }
        void SoloPressed() { }
        void AgainPressed() { }
        void HomePressed() { }
    }
}
```

- [ ] **Step 2: Add the bootstrap**

Create `unity/Assets/Scripts/Duo/BobDuoBootstrap.cs`:

```csharp
// Battle of the Brooms: the DuoClean scene is one canvas plus this object. Everything else is built at runtime so the
// scene stays a one-screen file that never conflicts with the island scenes.
using UnityEngine;

namespace Bob.Duo
{
    public sealed class BobDuoBootstrap : MonoBehaviour
    {
        void Awake()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) { Debug.LogError("[Duo] DuoClean has no Canvas"); return; }
            var match = gameObject.AddComponent<BobDuoMatch>();
            var hud = gameObject.AddComponent<BobDuoHud>();
            hud.Build(canvas, match);
            match.Bind(hud);
            Debug.Log("[Duo] slice booted · landscape " + Screen.width + "x" + Screen.height);
        }
    }
}
```

`BobDuoMatch` does not exist until Task 5; to keep this task compilable, Step 2 also adds the minimal shell now and Task 5 replaces its body:

Create `unity/Assets/Scripts/Duo/BobDuoMatch.cs` in this step with exactly this content (Task 5 will extend it; do not leave it like this at the end of the plan):

```csharp
// Battle of the Brooms: M1 duo slice orchestration. Shell only in Task 3; the simulation lands in Task 5.
using UnityEngine;

namespace Bob.Duo
{
    public sealed class BobDuoMatch : MonoBehaviour
    {
        public bool Running { get; private set; }
        public bool Over { get; private set; }
        public bool Won { get; private set; }
        public float Elapsed { get; private set; }
        public int TaskIndex { get; private set; }
        public int LocalSlot { get; private set; }
        public bool Online { get; private set; }
        public bool IsHost { get; private set; }

        public void Bind(BobDuoHud hud) { }
        public void BeginOnline() { BeginSoloSmoke(); } // replaced by the real room path in Task 5
        public void BeginSoloSmoke() { Running = true; }
    }
}
```

- [ ] **Step 3: Bake the scene through an editor builder**

Create `unity/Assets/Scripts/Editor/BobDuoSceneBuilder.cs`:

```csharp
// Battle of the Brooms: bakes Assets/Scenes/DuoClean.unity (docs 32 M1). Run from the menu once; the scene itself only
// holds a camera, an EventSystem, a 1920x1080 canvas and the bootstrap object.
using Bob.Duo;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bob.EditorTools
{
    public static class BobDuoSceneBuilder
    {
        public const string ScenePath = "Assets/Scenes/DuoClean.unity";

        [MenuItem("Bob/Build Duo Clean Scene")]
        public static void Build()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bob.Game.BobUi.Bg;
            cam.orthographic = true;
            camGo.transform.position = new Vector3(0, 0, -10);

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            var canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            new GameObject("Duo", typeof(BobDuoBootstrap));

            EditorSceneManager.SaveScene(scene, ScenePath);

            // DuoClean sits after the three Task Race scenes (solo -> duo -> team -> duo co-op).
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(s => s.path == ScenePath))
            {
                int at = scenes.FindIndex(s => s.path.EndsWith("TaskRace_Team.unity"));
                scenes.Insert(at < 0 ? scenes.Count : at + 1, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[DuoScene] " + ScenePath + " built and added to Build Settings");
        }

        [MenuItem("Bob/Open Duo Clean Scene")]
        public static void Open() => EditorSceneManager.OpenScene(ScenePath);
    }
}
```

- [ ] **Step 4: Bake and look at it**

In Unity: menu `Bob/Build Duo Clean Scene`, then `Bob/Open Duo Clean Scene`, then Play.
Expected Console: `[Duo] slice booted · landscape 1920x1080` (or the Game-view size), the lobby card is centred with สร้างห้อง / ใส่รหัสห้อง / เริ่มเมื่อครบ 2 คน / ทดลองคนเดียว, and no layout element touches the 19.5:9 edges. Take a Game-view screenshot for evidence.

- [ ] **Step 5: Run the offline gates**

Run: `bash unity-typecheck/check.sh` and `cd unity-check && DOTNET_ROLL_FORWARD=Major dotnet run`.
Expected: both green. (If the type-check reports the new `Duo` scripts, it is because it type-checks all of `Assets/Scripts`; fix any new error rather than editing the baseline.)

---

### Task 4: Lobby flow — host, join by code, start at two

**Files:**
- Modify: `unity/Assets/Scripts/Duo/BobDuoHud.cs`

**Interfaces:**
- Consumes: `Bob.Ugs.BobOnlineRoom.Ensure()`, `HostAsync(string,int)`, `JoinAsync(string,string)`, `LeaveAsync()`, `Code`, `Players`, `Status`, `InRoom`, `IsHost`, `DuoStarting`, `StartDuo(int)`.
- Produces: lobby that fills the room and calls `match.BeginOnline()` when the host starts (match method added in Task 5; this task calls `BeginSoloSmoke` only as the offline path).

- [ ] **Step 1: Replace the empty lobby handlers with the real room flow**

Replace the six stub methods at the bottom of `BobDuoHud.cs` with:

```csharp
        System.Threading.Tasks.Task<bool> roomTask;
        TouchScreenKeyboard keyboard;

        void HostRoom()
        {
            if (busy) return;
            busy = true;
            status.text = "กำลังสร้างห้อง…";
            var room = Bob.Ugs.BobOnlineRoom.Ensure();
            roomTask = room.HostAsync(HeroId(), 2);
        }

        void JoinRoom()
        {
            if (busy) return;
            keyboard = TouchScreenKeyboard.Open(typedCode, TouchScreenKeyboardType.Default, false, false, true, false, "รหัสห้อง 6 ตัว");
            typedCode = "";
            status.text = "พิมพ์รหัส 6 ตัวจากเจ้าของห้อง";
        }

        void StartPressed()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room == null || !room.InRoom || !room.IsHost || room.Players < 2) return;
            if (!room.StartDuo(2)) { status.text = "เริ่มไม่ได้ · ต้องมี 2 คนพอดี"; return; }
            match.BeginOnline();
        }

        void SoloPressed()
        {
            match.BeginSoloSmoke();
            ShowHud();
        }

        void AgainPressed()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room != null && room.InRoom)
            {
                if (!room.IsHost) { resultLine.text = "รอเจ้าของห้องเริ่มรอบใหม่"; return; }
                room.StartDuo(2);
                match.BeginOnline();
                return;
            }
            match.BeginSoloSmoke();
            ShowHud();
        }

        void HomePressed()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room != null && room.InRoom) _ = room.LeaveAsync();
            Bob.Game.BobSceneLoader.Load("Game", "กลับหน้าหลัก");
        }

        static string HeroId()
        {
            var pick = Bob.Game.MatchRequest.Has && Bob.Game.MatchRequest.Helpers.Length > 0
                ? Bob.Game.MatchRequest.Helpers[0]
                : "sweep";
            return string.IsNullOrEmpty(pick) ? "sweep" : pick;
        }
```

- [ ] **Step 2: Drive the room UI from `Update`**

Add above `ShowLobby`:

```csharp
        void Update()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            bool inRoom = room != null && room.InRoom;

            if (keyboard != null)
            {
                if (keyboard.active)
                {
                    string typed = CleanCode(keyboard.text);
                    if (typed != typedCode) typedCode = typed;
                }
                else
                {
                    keyboard = null;
                    if (typedCode.Length == 6 && !busy)
                    {
                        busy = true;
                        status.text = "กำลังเข้าห้อง " + typedCode + "…";
                        roomTask = Bob.Ugs.BobOnlineRoom.Ensure().JoinAsync(HeroId(), typedCode);
                    }
                }
            }

            if (roomTask != null && roomTask.IsCompleted)
            {
                bool ok = roomTask.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && roomTask.Result;
                roomTask = null;
                busy = false;
                status.text = ok ? "อยู่ในห้องแล้ว" : "เข้าห้องไม่สำเร็จ · ลองใหม่";
            }

            if (inRoom)
            {
                code.text = room.Code;
                status.text = room.Players + "/2 คน · " + (room.IsHost ? "คุณเป็นเจ้าของห้อง" : "รอเจ้าของห้องเริ่ม");
                hostButton.interactable = false;
                joinButton.interactable = false;
                startButton.interactable = room.IsHost && room.Players >= 2;
            }
            else
            {
                if (!busy) status.text = status.text == "" ? "สร้างห้องหรือใส่รหัสจากเพื่อน" : status.text;
                hostButton.interactable = !busy;
                joinButton.interactable = !busy;
                startButton.interactable = false;
            }
        }

        static string CleanCode(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            var sb = new System.Text.StringBuilder(6);
            foreach (char c in raw.ToUpperInvariant())
                if (sb.Length < 6 && ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) sb.Append(c);
            return sb.ToString();
        }
```

- [ ] **Step 3: Listen for the host's start, even though the room is created after this panel**

The room does not exist yet when the lobby is built, so subscribe lazily. Add to `BobDuoHud`:

```csharp
        Bob.Ugs.BobOnlineRoom listened;

        void ListenToRoom()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room == listened) return;
            if (listened != null) listened.DuoStarted -= OnDuoStarted;
            listened = room;
            if (listened != null) listened.DuoStarted += OnDuoStarted;
        }

        void OnDuoStarted()
        {
            ShowHud();
            match.BeginOnline();
        }

        void OnDestroy()
        {
            if (listened != null) listened.DuoStarted -= OnDuoStarted;
        }
```

and call `ListenToRoom();` as the first line inside `Update()`.

- [ ] **Step 4: Two-editor evidence**

Open two play sessions (Editor + a second build or a second project copy) with the same UGS project. Host: สร้างห้อง → code appears. Client: ใส่รหัสห้อง → same code. Start button enables at 2/2; pressing it on the host shows the HUD on both.
Expected: both consoles print `[Duo] match start` (log added in Task 5) and the clock runs. If only one machine is available, record that limit and rely on the solo smoke path — do not claim two-device success without it.

- [ ] **Step 5: Gates**

Run: `bash unity-typecheck/check.sh`; compile in Unity.
Expected: green; no Console errors after a full host→start→result→home cycle.

---

### Task 5: The three gestures and the task card

**Files:**
- Create: `unity/Assets/Scripts/Duo/BobDuoGestures.cs`
- Modify: `unity/Assets/Scripts/Duo/BobDuoHud.cs`
- Replace the shell: `unity/Assets/Scripts/Duo/BobDuoMatch.cs`

**Interfaces:**
- Consumes: Task 1 rules/state, Task 2 room channel.
- Produces: `BobDuoMatch.BeginOnline()`, `BeginSoloSmoke()`, `Hold(int,bool)`, `Tap(int)`, `Drag(int,float)`, plus read-only `Tasks`, `Percent`, `LocalGesture`, `RoleFor` accessors used by the HUD.

- [ ] **Step 1: Add the pointer components**

Create `unity/Assets/Scripts/Duo/BobDuoGestures.cs`:

```csharp
// Battle of the Brooms: the only three inputs of the slice (docs 32 section 4). Each one is a small pointer adapter so
// BobDuoHud can build the card and BobDuoMatch can read intent without knowing about uGUI events.
using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Bob.Duo
{
    /// <summary>Press and keep pressing. Reads as "Held" so the match samples it every frame.</summary>
    public sealed class BobDuoHoldArea : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool Held { get; private set; }
        public void OnPointerDown(PointerEventData e) => Held = true;
        public void OnPointerUp(PointerEventData e) => Held = false;
        public void OnPointerExit(PointerEventData e) => Held = false;

        /// <summary>The card changed task under the finger: a hold must not survive into the next gesture.</summary>
        public void Release() => Held = false;
    }

    /// <summary>Tap on the beat. Beat 0.75 s, ±0.2 s window; a late or early tap still counts as a try but adds nothing.</summary>
    public sealed class BobDuoRhythmPad : MonoBehaviour, IPointerDownHandler
    {
        public const float Beat = 0.75f;
        public const float Window = 0.2f;

        public event Action<bool> Tapped; // true = good tap

        public void OnPointerDown(PointerEventData e)
        {
            float p = Mathf.Repeat(Time.time, Beat);
            bool good = p <= Window || p >= Beat - Window;
            Tapped?.Invoke(good);
        }

        /// <summary>The ring's only visual: how far into the beat we are.</summary>
        public static float BeatProgress => Mathf.Repeat(Time.time, Beat) / Beat;
    }

    /// <summary>Drag anywhere on the card; the delta is scaled to the 1920-wide reference so a phone and a tablet owe
    /// the same distance.</summary>
    public sealed class BobDuoDragPad : MonoBehaviour, IBeginDragHandler, IDragHandler
    {
        float pending;

        public void OnBeginDrag(PointerEventData e) { }
        public void OnDrag(PointerEventData e) => pending += e.delta.magnitude * (1920f / Mathf.Max(1, Screen.width));

        public float Consume()
        {
            float value = pending;
            pending = 0f;
            return value;
        }
    }
}
```

- [ ] **Step 2: Replace the match shell with the real simulation**

Replace `unity/Assets/Scripts/Duo/BobDuoMatch.cs` with:

```csharp
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
                if (local.Done && TaskIndex < BobDuoRules.TaskCount - 1) TaskIndex++;
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
```

- [ ] **Step 3: Wire the pads into the card**

In `BobDuoHud.BuildMatch`, after the pad panel is created, add the three components and keep references:

```csharp
            mine.hold = pad.gameObject.AddComponent<BobDuoHoldArea>();
            mine.rhythm = pad.gameObject.AddComponent<BobDuoRhythmPad>();
            mine.drag = pad.gameObject.AddComponent<BobDuoDragPad>();
            mine.rhythm.Tapped += good => { if (good) match.Tap(Slot); };
```

Add to the `Slot` class:

```csharp
            public BobDuoHoldArea hold;
            public BobDuoRhythmPad rhythm;
            public BobDuoDragPad drag;
```

Add to `BobDuoHud`:

```csharp
        int Slot => match.LocalSlot;
        DuoGesture lastGesture;
```

The rhythm pad and the hold area share the same panel; `BobDuoHoldArea` and `BobDuoDragPad` coexist because uGUI calls each interface independently. `BobDuoRhythmPad` is only *shown* on rhythm tasks (Step 4), which prevents a hold and a tap landing on the same press.

- [ ] **Step 4: Render the current task and read the pads every frame**

Add to `BobDuoHud`:

```csharp
        public void RefreshHeader()
        {
            if (clock != null) clock.text = BobDuoRules.Clock(0f);
        }

        public void RefreshMatch(BobDuoMatch live)
        {
            if (live == null) return;
            int index = Mathf.Clamp(live.TaskIndex, 0, BobDuoRules.TaskCount - 1);
            int slot = live.LocalSlot;
            var def = BobDuoRules.Task(index);
            var task = live.Tasks[index];

            clock.text = BobDuoRules.Clock(live.Elapsed);
            phase.text = BobDuoRules.PhaseThai(live.Phase);
            progress.text = "งาน " + (index + 1) + "/6 · " + Mathf.RoundToInt(live.Percent) + "%";

            mine.title.text = def.Name;
            mine.role.text = BobDuoRules.RoleFor(index, slot);
            mine.hint.text = BobDuoRules.HintFor(index, slot);
            mine.meter.fillAmount = task.Charge(slot);

            theirs.role.text = BobDuoRules.RoleFor(index, 1 - slot);
            theirs.hint.text = BobDuoRules.HintFor(index, 1 - slot);
            theirs.state.text = task.Charge(1 - slot) >= 1f ? "ฝั่งนี้พร้อม" : "กำลังทำอยู่";
            theirs.partnerBar.fillAmount = task.Charge(1 - slot);

            for (int i = 0; i < pips.Length; i++)
                pips[i].color = live.Tasks[i].Done ? BobUi.Good : i == index ? BobUi.Sun : BobUi.Line;

            var gesture = BobDuoRules.GestureFor(index, slot);
            if (gesture != lastGesture) { mine.hold.Release(); lastGesture = gesture; }
            mine.hold.enabled = gesture == DuoGesture.Hold;
            mine.drag.enabled = gesture == DuoGesture.Drag;
            mine.rhythm.enabled = gesture == DuoGesture.Rhythm;
            if (mine.rhythmRing != null) mine.rhythmRing.SetActive(gesture == DuoGesture.Rhythm);

            if (gesture == DuoGesture.Hold) match.Hold(slot, mine.hold.Held);
            else if (gesture == DuoGesture.Drag)
            {
                float units = mine.drag.Consume();
                if (units > 0f) match.Drag(slot, units);
            }
        }
```

Also in `BuildMatch`, create the rhythm ring (a thin disc that pulses) inside the pad and assign `mine.rhythmRing`:

```csharp
            var ring = BobUi.Disc(pad.transform, BobUi.Line, 180f, "BeatRing");
            BobUi.Pin((RectTransform)ring.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 180));
            mine.rhythmRing = ring.gameObject;
```

and give the pad a visible instruction label:

```csharp
            var padNote = BobUi.Label(pad.transform, "พื้นที่ทำภารกิจ · กดค้าง / แตะตามจังหวะ / ลาก", 24, BobUi.Muted, TextAnchor.MiddleCenter);
            BobUi.Pin(padNote.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 18), new Vector2(900, 40));
```

`BobDuoMatch.BeginOnline` sets `hud.ShowHud()`; the HUD methods referenced (`RefreshHeader`, `RefreshMatch`, `ShowHud`, `ShowResult`) must exist exactly once — replace the Task 3 `ShowResult` call signature only if it differs.

- [ ] **Step 5: Play the solo smoke path to the result**

Play `DuoClean`, press `ทดลองคนเดียว (คู่เป็นบอท)`.
Expected: the first card is `คราบติดพื้น · ฉีดน้ำยาให้ทั่ว`, hold fills your meter, the bot fills its half, the task advances, later tasks switch your role to the second column, timer reaches `0:00`, result panel shows `หมดเวลา` or `สะอาดครบ!` with a %. Console prints one `[Duo] task done` per task and one `[Duo] finish`.

- [ ] **Step 6: Gates**

Run `bash unity-typecheck/check.sh` and `cd unity-check && DOTNET_ROLL_FORWARD=Major dotnet run`; compile in Unity.
Expected: green; no `MissingReferenceException` over a full solo run.

---

### Task 6: Play-mode self-check with screenshot evidence

**Files:**
- Create: `unity/Assets/Scripts/Duo/BobDuoDevCheck.cs`
- Modify: `unity/Assets/Scripts/Duo/BobDuoBootstrap.cs`
- Modify: `unity/Assets/Scripts/Editor/BobDuoSceneBuilder.cs` (menu item that arms the check)

**Interfaces:**
- Consumes: `BobDuoBootstrap`, `BobDuoMatch.BeginSoloSmoke`, HUD fields.
- Produces: Console line `[DuoCheck] PASSED` and `Logs/duo-slice.png`.

- [ ] **Step 1: Add the check component**

Create `unity/Assets/Scripts/Duo/BobDuoDevCheck.cs`:

```csharp
// Battle of the Brooms: the evidence run for the M1 slice. Plays the solo smoke match through one full task, asserts the
// HUD moved, writes a screenshot under Logs/ and quits Play mode. Armed only by the editor menu.
using System.IO;
using UnityEngine;

namespace Bob.Duo
{
    public sealed class BobDuoDevCheck : MonoBehaviour
    {
        BobDuoMatch match;
        BobDuoHud hud;
        float startedAt = -1f;
        bool shot, holding;
        float driveAt;

        public void Arm(BobDuoMatch owner, BobDuoHud ui) { match = owner; hud = ui; startedAt = Time.time; }

        /// <summary>The check plays slot 0 the way a finger would, so the run actually advances without a human.</summary>
        void Drive()
        {
            if (Time.time < driveAt) return;
            driveAt = Time.time + 0.3f;
            int index = Mathf.Clamp(match.TaskIndex, 0, BobDuoRules.TaskCount - 1);
            switch (BobDuoRules.GestureFor(index, 0))
            {
                case DuoGesture.Hold: holding = !holding; match.Hold(0, holding); break;
                case DuoGesture.Rhythm: holding = false; match.Hold(0, false); match.Tap(0); break;
                default: holding = false; match.Hold(0, false); match.Drag(0, BobDuoRules.NeedFor(index, 0) / 12f); break;
            }
        }

        void Update()
        {
            if (match == null || startedAt < 0f) return;
            if (Time.time - startedAt < 1.5f) return;
            Drive();

            if (!shot && Time.time - startedAt > 6f)
            {
                shot = true;
                var logs = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
                Directory.CreateDirectory(logs);
                ScreenCapture.CaptureScreenshot(Path.Combine(logs, "duo-slice.png"));
            }
            if (Time.time - startedAt > 9f)
            {
                bool ok = (match.Running || match.Over) && match.Percent > 0f;
                Debug.Log((ok ? "[DuoCheck] PASSED" : "[DuoCheck] FAILED") + " · task " + match.TaskIndex +
                          " · " + Mathf.RoundToInt(match.Percent) + "% · over=" + match.Over + " · screenshot unity/Logs/duo-slice.png");
#if UNITY_EDITOR
                UnityEditor.EditorApplication.ExitPlaymode();
#endif
            }
        }
    }
}
```

- [ ] **Step 2: Arm it from the bootstrap when the menu asked**

In `BobDuoBootstrap.Awake`, after `match.Bind(hud)`:

```csharp
            if (PlayerPrefs.GetInt("bob.duo.devcheck", 0) == 1)
            {
                PlayerPrefs.SetInt("bob.duo.devcheck", 0);
                PlayerPrefs.Save();
                match.BeginSoloSmoke();
                gameObject.AddComponent<BobDuoDevCheck>().Arm(match, hud);
            }
```

- [ ] **Step 3: Add the menu item**

In `BobDuoSceneBuilder.cs`:

```csharp
        [MenuItem("Bob/Run Duo Slice Check")]
        public static void RunCheck()
        {
            PlayerPrefs.SetInt("bob.duo.devcheck", 1);
            PlayerPrefs.Save();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.EnterPlaymode();
        }
```

- [ ] **Step 4: Run it**

Menu `Bob/Run Duo Slice Check`; wait for Play mode to exit by itself.
Expected Console: `[DuoCheck] PASSED · task … · …% · over=False` with a non-zero %, and `unity/Logs/duo-slice.png` exists showing the landscape card with a filled meter and the six task pips.

- [ ] **Step 5: Look at the screenshot**

Read `unity/Logs/duo-slice.png` and check: landscape frame, no element under 48 u from any edge, Thai text not clipped, card on the left and partner column on the right. Note any defect as a fix in Task 5, not as a caveat in the report.

---

### Task 7: Entry from the menu

**Files:**
- Modify: `unity/Assets/Scripts/Game/BobGame.Menus.cs` (S14 right column, right after the `ออนไลน์ 2–4 คน` button at ~line 712)

**Interfaces:**
- Consumes: `BobSceneLoader.Load(string,string)`, `MatchRequest.Set(string,int,bool,string[])`.
- Produces: `void OpenDuoSlice()` on the menu screen.

- [ ] **Step 1: Add the button and the handler**

After:

```csharp
            var online = BobUi.Button(right, "ออนไลน์ 2–4 คน", BobUi.Surface, OpenOnlineRoom, 28, BobUi.Ink);
            BobUi.Size(online, 96, 104);
```

insert:

```csharp
            // docs 32 M1: the duo co-op slice, two devices, one room, six tasks.
            var duoSlice = BobUi.Button(right, "คู่ 2 คน · M1", BobUi.Teal, OpenDuoSlice, 28, BobUi.Surface);
            BobUi.Size(duoSlice, 96, 104);
```

and add next to `OpenOnlineRoom`:

```csharp
        void OpenDuoSlice()
        {
            Sfx("ui_confirm");
            MatchRequest.Set("duo", 1, false, new[] { charId });
            BobSceneLoader.Load(Bob.Ugs.BobOnlineRoom.DuoScene, "ห้องคู่ 2 คน");
        }
```

- [ ] **Step 2: Verify the route**

Play from the Home screen: `โหมด` → Duo → the new `คู่ 2 คน · M1` button → loading screen → the duo lobby. Back button (lobby header, added below) returns to Home.

- [ ] **Step 3: Add a Back button to the lobby**

In `BobDuoHud.BuildLobby`, after `status`:

```csharp
            var back = BobUi.Button(lobby.transform, "กลับ", BobUi.Surface, LeavePressed, 24, BobUi.Ink);
            BobUi.Pin((RectTransform)back.transform, new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -24), new Vector2(150, 76));
```

```csharp
        void LeavePressed()
        {
            var room = Bob.Ugs.BobOnlineRoom.Instance;
            if (room != null && room.InRoom) _ = room.LeaveAsync();
            Bob.Game.BobSceneLoader.Load("Game", "กลับหน้าหลัก");
        }
```

- [ ] **Step 4: Gates**

Run `bash unity-typecheck/check.sh`, `cd unity-check && DOTNET_ROLL_FORWARD=Major dotnet run`, and play the route in Unity.
Expected: green; Home → duo lobby → solo smoke → result → กลับหน้าหลัก returns Home with no orphan canvas.

---

### Task 8: Docs and the final gate run

**Files:**
- Modify: `art/docs/32_Coop_Concept_Reset.md` (§8 M1 status, §13)
- Modify: `art/docs/README.md` (doc-32 row + a change bullet)
- Modify: `COORDINATION.md` (this session's row only)
- Create: `art/docs/33_M1_Duo_Slice.md` (the slice's controls, scene entry, test steps, known limits)

**Interfaces:**
- Consumes: everything above.
- Produces: written state that matches the code, so the next session does not re-derive it.

- [ ] **Step 1: Write the slice note**

Create `art/docs/33_M1_Duo_Slice.md` with these sections and facts (fill in the real measured values from the check run):

- `# 33 · M1 Duo Co-op Slice` — status line `implemented 25 พ.ย. 2026 · no prank layer · no progression writes`.
- `## ทางเข้า` — Home → โหมด → Duo → `คู่ 2 คน · M1`.
- `## กติกา` — 4:00, 75/105/60, 6 tasks, roles swap each task, hold/rhythm/drag, unfinished = % at the bell.
- `## สองเครื่อง` — host creates, client joins by 6-letter code, host starts at 2/2; host owns clock; client snapshot 5 Hz.
- `## ทดสอบ` — the solo smoke button, `Bob/Run Duo Slice Check`, `unity/Logs/duo-slice.png`.
- `## ข้อจำกัดที่รู้` — no reconnect, no prank, no matchmaking, no voice, one room, no per-task art beyond text/icons, client view is optimistic between snapshots.

- [ ] **Step 2: Update the spec's own status**

In `art/docs/32_Coop_Concept_Reset.md` §8 item 1, append `— ทำแล้ว (slice) 25 พ.ย. 2026 ที่ [33_M1_Duo_Slice.md](33_M1_Duo_Slice.md)`.
In §13, mark item 3 done and point at the plan `docs/superpowers/plans/2026-11-25-bob-m1-duo-coop-slice.md`; keep item 4 (owner approval) unchecked for M2.

- [ ] **Step 3: Update the doc index**

In `art/docs/README.md`, add a row:

```markdown
| [33_M1_Duo_Slice.md](33_M1_Duo_Slice.md) | **M1 duo co-op slice (25 พ.ย. 2026):** ทางเข้า, 6 งาน 4:00, สองบทบาทสลับทุกงาน, host/client และขั้นทดสอบ — ยังไม่มีป่วน/แรงค์/รางวัล | Engineering / Game design |
```

and a change bullet at the end of the recent-changes list describing the scene, the channel names, the gates run and the limits from §1 above.

- [ ] **Step 4: Update the coordination board**

Replace only this session's row in `COORDINATION.md` with the files touched and the Unity-Editor time used; do not edit other rows.

- [ ] **Step 5: Final gates**

Run, in this order, and paste the tails into the session report:

```bash
bash unity-typecheck/check.sh
cd unity-check && DOTNET_ROLL_FORWARD=Major dotnet run
```

Expected: `typecheck ok: ... no new errors` and `UNITY-CHECK PASSED`.
Then run the Unity check menu once more (`Bob/Run Duo Slice Check`) and confirm `[DuoCheck] PASSED`.
Report two-device status honestly: passed only if both devices were actually used; otherwise "editor + solo smoke verified, two-device run pending owner".

- [ ] **Step 6: Working-tree check, no commit**

Run: `git status --short` and `git diff --stat`.
Expected: only this slice's files changed. **Do not commit, do not push** — the owner ships.

---

## Self-Review

**Spec coverage**

- §0 4 heroes with equal abilities → no ability code at all; hero id only picks a portrait/label. ✅
- §3 structure 75/105/60 = 240 s → Task 1 constants and checks. ✅
- §4 six tasks, two roles, three gestures, forgiving failure → Task 1 table + `BobDuoTaskState` decay; partial % at the bell in `Finish`. ✅
- §4 roles swap each task → `PrimarySlot` alternates; asserted. ✅
- §6 4-player room → **deliberately out of M1**; the channel is 2 slots and `StartDuo(2)` refuses any other count. M2 will change it. ✅
- §6 host owns time and decisions; client sends actions → Task 2 + Task 5 (`IsHost` simulation, `OnAction`, `OnSnapshot`). ✅
- §6 private room by code → Task 4 reuses `BobOnlineRoom` lobby. ✅
- §6 split-phase progress visibility → partner column shows role + charge (the owner-approved "status only" answer). ✅
- §7 cut list → no walking/inventory/prank/matchmaking/voice; no progression writes. ✅
- §8 M1 order → this plan is M1 only; M2/M3 untouched. ✅
- Landscape 19.5:9 → canvas 1920×1080 + 64 u margins; visual evidence in Task 6. ✅

**Placeholder scan**

- No TBD/TODO/"similar to Task N" remains. The Task 3 `BobDuoMatch` shell is explicitly replaced in Task 5 Step 2,
  and the plan says so at both points.
- Every rule assertion in Task 1 has a matching implementation line; the decay test uses partial charges because a
  full pair completes immediately by design.

**Type consistency**

- `BobDuoRules.RoleFor(int,int)` returns `string` everywhere (Task 1 fixes this before consumers exist).
- `BobDuoTaskState.Restore(float,float,bool)` is the only snapshot setter used by Task 5.
- `DuoSnapshot` field names (`Charge0`, `Charge1`, `Elapsed`, `Task`, `DoneMask`) match Task 2's writer, Task 5's reader and `Snapshot()`.
- HUD methods called by the match: `ShowHud`, `ShowResult`, `RefreshHeader`, `RefreshMatch` — all defined in Tasks 3/5.
- Codes `0/1/2` defined once in `BobDuoMatch` (`HoldCode/TapCode/DragCode`) and documented in `BobOnlineRoom.SendDuoAction`.
