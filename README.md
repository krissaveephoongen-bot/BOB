# Battle of the Brooms (สมรภูมิปัดกวาด) — development workspace

GDD: https://claude.ai/artifact/VsJEEyQDx4HwvrjZk2MEAu
Playable prototype: https://claude.ai/artifact/M9orSH3LFGbYboRm7QAkHn

## What is here

| Folder | What | State |
| --- | --- | --- |
| `prototype/index.html` | Browser prototype, solo vs a simulated rival. Holds the rules (parts, reactions, enemies, scoring). | Tested with `node prototype/test.js` |
| `server/` | Nakama 3.22.0 + Postgres 16 in Docker. `modules/index.js` is generated from the prototype rules by `build-module.js`; `rpc.js` holds the RPCs. | Tested with `server/smoke-test.sh` |
| `unity/` | Unity 6000.6.8f1 project (open this folder in Unity Hub). Nakama SDK + uGUI in `Packages/manifest.json`; sprites/audio in `Assets/Art`, `Assets/Audio`. | Play-mode smoke test passes (Bob > Run Smoke Test) |
| `unity/Assets/Scripts/` | `Rules/BobRules.cs` (client copy of the rules, pure C#), `Net/BobServer.cs` (Nakama wrapper: sign in, socket, match, RPCs), `Dev/BobSmokeTest.cs` (Play-mode end-to-end check). | Compiled and checked against the server by `unity-check/` |
| `unity/Assets/Scenes/Game.unity` | **Playable single-player scene**: room, 4 stations, cart, sofa, hero (tap / WASD), pick up, stack 5 layers + 2 methods, live stat bars, abilities, 3 rounds vs the simulated rival. Room and our score come from the local Nakama (`bob_make_round`, `bob_score_stack`); offline it uses the same rules locally. Code in `Scripts/Game/`, rival + rooms in `Rules/BobMatch.cs`. | Bot plays a full match: menu **Bob > Run Game Test** |
| `server/match.js` + `unity/Assets/Scripts/Net/BobOnline.cs` | **Real-time multiplayer** (`bob_coop`): server-authoritative match for 2-4 players in two teams, one shared cart per team, best of 3. See [Multiplayer](#multiplayer). | Bots play full matches: `node server/mp-test.js 4` |
| `dashboard/index.html` | Balance dashboard: reads `bob_analytics` from the local Nakama and charts part, character, round and room stats. | Checked in Chromium, light and dark |
| `unity-check/` | .NET console app: spot checks + C#-vs-server parity on random stacks. | 275 stacks, 0 mismatches |
| `art/` | Official concept boards and production docs (`docs/`), sprite sources (`source/`) and generator tools (`tools/`). Unity-ready copies live in `unity/Assets`. | Concept / placeholder |

## Run

```bash
./start-server.sh
```

```bash
./server/smoke-test.sh
```

```bash
cd unity-check && NO_PROXY=127.0.0.1,localhost dotnet run
```

## Analytics (play data for balancing)

Every play event is one row in Postgres table `bob_events` (`created_at, user_id, match_id, event, data jsonb`),
created by the Nakama module on start. Code: `server/analytics.js`.

| Event | Written by | Main fields in `data` |
| --- | --- | --- |
| `stack_scored` | the server itself, inside `bob_score_stack` (cannot be faked) | stack, cost, budget, stats, score, enemies, need, char |
| `match_start` / `match_end` | client via `bob_log_event` | char, seed / wins, losses, rounds, coins |
| `round_result` | client via `bob_log_event` | round, result, stack, my_total, rival_total, pass, time_left, ability_used |
| `ability_used` | client via `bob_log_event` | round, ability |

- `bob_log_event` accepts only the four client events above, max 4 KB, match id `[A-Za-z0-9_-]{1,64}`.
- `bob_analytics` returns the aggregates. It answers only server-to-server calls (`?http_key=`), never a player's session.
- **Prototype:** open `prototype/index.html?server=http://127.0.0.1:7350` (served over http, for example
  `python3 -m http.server 8088` in this folder) and it sends events. Without `?server=` it stays offline.
- **Bot playtests:** `cd server && NO_PROXY=127.0.0.1,localhost node simulate.js 50` plays 50 matches with the
  rival AI at random skill levels, so the dashboard has data before real players do.
- **Dashboard:** open `http://127.0.0.1:8088/dashboard/index.html` (same http server). It defaults to
  `http://127.0.0.1:7350` and the development key `defaulthttpkey`.
- Raw SQL: `docker compose exec postgres psql -U postgres -d nakama -c "select event, count(*) from bob_events group by 1"`.

## One rule set, three places

`prototype/index.html` is the source of truth. After changing a rule there:
1. `node server/build-module.js` and `docker compose restart nakama` (in `server/`).
2. Mirror the change by hand in `unity/Assets/Scripts/Rules/BobRules.cs`.
3. Run `unity-check`; any drift shows as a MISMATCH.

## Unity on your Windows PC

1. **Docker Desktop** (WSL 2 backend). Start it and wait for "Engine running".
2. **Server:** in PowerShell, from this folder: `powershell -ExecutionPolicy Bypass -File .\start-server.ps1`,
   then `powershell -ExecutionPolicy Bypass -File .\server\smoke-test.ps1`. The last call must say REFUSED.
3. **Unity Hub** (`winget install Unity.UnityHub`), sign in, and install **Unity 6 LTS** with Android Build Support
   (iOS needs a Mac).
4. In Unity Hub: New project > Universal 3D (URP), name `BobClient`.
5. Copy `unity/Assets/Scripts` into `BobClient/Assets/` (so you get `Assets/Scripts/Rules`, `Net`, `Dev`).
6. Window > Package Manager > + > Add package from git URL:
   `https://github.com/heroiclabs/nakama-unity.git?path=/Packages/Nakama`
   (or import `Nakama.unitypackage` from the nakama-unity GitHub releases page). Then set
   Edit > Project Settings > Player > Other Settings > Api Compatibility Level to **.NET Standard 2.1**.
   If you also import the art scripts, add `"com.unity.ugui"` in Package Manager (the Unity 6.6 template lacks it).
7. Menu **Bob > Run Smoke Test** (or add the **BobSmokeTest** component to a GameObject and press Play).
   Pass = Console shows `match created: ...`, a server score, and `SMOKE TEST PASSED`.
   On a phone, set the component's Host to the PC's LAN IP and allow port 7350 in Windows Firewall.

`unity-check/` (needs the .NET 8 SDK: `cd unity-check; dotnet run`) runs the same match + score flow and
275-stack client/server parity check without Unity.

## Not done yet

- Multiplayer in the scene: draw the other players and each team's cart from `BobOnline`, plus a lobby screen. The server side and the client network layer are done.
- Multiplayer server does not block walking through stations or the sofa yet (it only caps speed and keeps players in the room).
- Unity scene, character controller, drag-and-drop, UI. Only rules and networking wrapper exist.
- Scrub and Wash abilities, cart pushing, obstacles other than the sofa, the other 7 scenes.
- Balance: every number is an untested starting value.
- Development passwords, the `defaultkey` server key and the `defaulthttpkey` HTTP key must be replaced before any deployment.
- Analytics: no retention or archiving yet; Unity client does not send `bob_log_event` yet (only `stack_scored` is logged from Unity).

## Multiplayer

The server owns a multiplayer match: phones send intents, the server checks them and broadcasts the state. Code: `server/match.js` (bundled into `modules/index.js` by `build-module.js`), client layer `unity/Assets/Scripts/Net/BobOnline.cs`.

- 2-4 players, auto-split into two teams. One seed per match, so both teams get the same room each round.
- Each team shares one cart; each player holds at most 2 parts (`BobRules.HandTeam`).
- The server checks walking speed, distance to a station (pick) or the cart (place, lift), budget, 5 layers and 2 methods.
- A round ends when both teams submit or the 100 s clock runs out; best of 3, ties go to the team with more budget left.
- Join by matchmaker (`BobOnline.FindAsync`, 2-4 players) or a private room (`bob_create_match` RPC, `BobOnline.CreateAsync` / `JoinAsync` with the match id).

| Op | Direction | Payload |
| --- | --- | --- |
| 1 MOVE | client → server | `{x,y}` world units, about 10 per second |
| 2 PICK / 3 PLACE / 4 LIFT | client → server | `{id}` / `{i}` hand index / `{}` |
| 5 READY / 6 SUBMIT | client → server | `{char,ready}` in the lobby / `{}` |
| 10 STATE | server → clients | phase, round, room, players[], teams[] (shared stacks) |
| 11 POS | server → clients | `{time,p:[{id,x,y}]}` every tick (10 Hz) during build |
| 12 RESULT / 13 ERROR | server → clients | per-round scores and winner / Thai refusal text for one player |

Test: start the server, then `cd server && node mp-test.js 4` (bots walk, pick, place, submit; must print PASS).

## Play the Unity scene

1. Start the server (above), open `unity/` in Unity 6000.6.8f1 and open `Assets/Scenes/Game.unity`.
   If the scene is missing or the art changed, run **Bob > Build Game Scene** (rebuilds `Assets/Art/BobArtLibrary.asset`).
2. Press Play. The main menu shows the hero; pick one of the four portraits, then **เล่น**. Each round opens with the
   room brief (enemies, spec, budget), a 3-2-1 countdown, then the round. Landscape mobile layout:
   - top left: order ticket (enemies + 4 live stat bars with the spec's target line);
   - top centre: round pips, timer, score; top right: budget and pause;
   - right: the cart stack (lift / Sweep's swap), then the ability button and a big **ส่งทดสอบ** (submit);
   - bottom centre: the hand (3 slots); left: a floating joystick wherever the thumb lands.
   Walk with the joystick, a tap on the floor, or WASD / arrows. Near a station its shelf pops up: tap a part to pick it.
   Near the cart, tap or drag a hand part onto the cart (or press 1-3); drag the top layer back to the hand (or Q) to
   lift it. F uses the ability, Enter submits, Esc / Android back pauses. Settings: sound, music, server address.
3. The main menu shows `● ออนไลน์` when the room and our score come from Nakama. Without a server it says `○ ออฟไลน์`
   and scores locally with the same rules. On a phone, enter the PC's LAN IP in Settings (http is allowed in the player
   settings). Coins, hero, sound and server address are saved on the device.
4. **Bob > Run Game Test** lets a bot play a full match in Play mode (Console: `GAME TEST PASSED`, screenshots in
   `unity/Logs/bob-game-*.png`). Command line, with the Editor closed on this project:
   `Unity.exe -projectPath unity -executeMethod Bob.EditorTools.BobGameScene.RunTest -logFile game-test.log`

Known gaps: Thai text uses the OS font (Leelawadee UI on Windows, Noto Sans Thai on Android) through legacy uGUI Text,
so tone marks can sit slightly off; shipping Mitr / IBM Plex Sans Thai as a TextMeshPro asset fixes that. The art is
the 2D placeholder set; the 3D toon pipeline in `art/docs/03_Unity_Spec.md` replaces it later. Touch controls (joystick,
drag) are written for phones but so far tested only in the Editor; the bot test does not cover drag. Not yet built: the
split-screen before/after test view (GDD 9), pushing the cart, passing items, the item rest table (GDD 20).
