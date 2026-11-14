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
| `unity-check/` | .NET console app: spot checks + C#-vs-server parity on random stacks + the co-op moves with three `BobOnline` clients (`CoopCheck.cs`). | 275 stacks, 0 mismatches; co-op check PASS |
| `unity-typecheck/` | Type-checks every script in `unity/Assets/Scripts` without the Unity Editor (NuGet reference assemblies), for cloud sessions. | 0 errors |
| `unity/Assets/Scripts/Coop/` + `UI/` | **Online co-op scene** (`Assets/Scenes/Coop.unity`, menu **Bob > Build Coop Scene**): menu, lobby, other players, shared cart, push / pass / parking table, hold-to-send vote, split-screen test. UI kit from the UX spec: `BobUiTheme`, `BobSafeArea`, `BobHitArea`, `BobHoldButton`. | Compiled in Unity 6000.6.8f1 and played by a bot against 2 bot players: **Bob > Run Coop Test** (`COOP TEST PASSED`). Not played by real people yet; HUD layout checked by screenshots at 1920x1080, 2400x1080 and 2560x1440 |
| `art/` | Official concept boards and production docs (`docs/`, including the full UX/UI spec `docs/11_UX_UI_Spec.md` and wireframes `docs/ux/BOB_UX_Wireframes.html`, the minigame card wireframes `docs/ux/BOB_Minigame_Screens.html`, and the 3/4 cafeteria play screen `docs/ux/BOB_Play_Screen_Cafeteria.html`), sprite sources (`source/`) and generator tools (`tools/`). Unity-ready copies live in `unity/Assets`. | Concept / placeholder |

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

- Two online front-ends exist: the **Game scene's online mode** (`Game/BobGame.Online.cs`, compiled and bot-tested in Unity,
  quick match only) and the **Co-op scene** (`Coop/`, written in the cloud, type-checked only) which also has the private room
  screen, team switch, push / pass / table, hold-to-send vote and the split-screen test. Plan: port those features into the
  Game scene's online mode on the PC and keep the Co-op scene as the reference (see "Needs the PC").
- Online: abilities run on the server and in both front-ends. Reconnect after the app is closed works in the Co-op scene only
  (`BobOnline.RejoinAsync`); the Game scene's online mode does not rejoin yet. No Quick Ping or voice chat.
- Scrub and Wash abilities, obstacles other than the sofa, the shelves and the table, the other 7 scenes; Party / Community modes.
- Balance: every number is an untested starting value. `node server/balance-sim.js` compares the hero abilities
  (results and suggested tweaks: `art/docs/13_Balance_Sim.md`); nothing has been changed from it yet.
- Development passwords, the `defaultkey` server key and the `defaulthttpkey` HTTP key must be replaced before any deployment.
- Analytics: no retention or archiving yet. Unity sends match_start, round_result, ability_used and match_end through `bob_log_event`;
  online matches are also logged by the server (match_start, round_result, ability_used with `mode: mp`).

## Multiplayer

The server owns a multiplayer match: phones send intents, the server checks them and broadcasts the state. Code: `server/match.js` (bundled into `modules/index.js` by `build-module.js`), client layer `unity/Assets/Scripts/Net/BobOnline.cs`.

- 2-4 players, auto-split into two teams (players can switch side in the lobby while the other side has room).
  One seed per match, so both teams get the same room each round; each team plays in its own copy of the room.
- Each team shares one cart; each player holds at most 2 parts (`BobRules.HandTeam`).
- The server checks walking speed, distance to a station (pick), the cart (place, lift, push) or the table, budget, 5 layers and 2 methods.
  Shelves, the parking table, the sofa and the team's own cart are solid: the server pushes players out of them (same maths
  as `BobGame.World` and `Coop/BobCoopLayout.cs`; the pusher is not pushed out of the cart they hold).
- Co-op moves (GDD 20): **push the cart** (grab it, it follows you at 80% speed, one pusher per team), **pass** a part to a team-mate
  within 1 unit, **parking table** for up to 4 parts. A player who drops mid-round leaves their parts on the team's table.
- Sending is a **vote** (UX spec BTN-Submit): the cart is sent when half the online team has voted.
- **Abilities** (GDD 6), once per player per round: Sweep swaps two neighbouring layers, Bubble shields the team's cart from its worst
  bad reaction, Spray gets a private score preview (op 15, only to that player), Press stops the team's clock for 8 s.
- Each team has its own 100 s clock (Press can stop it). When it runs out the cart goes as it is. The round ends when both teams have
  sent; best of 3, ties go to the team with more budget left.
- **Reconnect**: a dropped player keeps their seat. `BobOnline` stores the match id on the phone until the match ends, and the Co-op
  scene joins it again on connect (`BobOnline.RejoinAsync`).
- Join by matchmaker (`BobOnline.FindAsync`, 2-4 players) or a private room (`bob_create_match` RPC, `BobOnline.CreateAsync` / `JoinAsync` with the match id).

| Op | Direction | Payload |
| --- | --- | --- |
| 1 MOVE | client → server | `{x,y}` world units, about 10 per second |
| 2 PICK / 3 PLACE / 4 LIFT | client → server | `{id}` / `{i}` hand index / `{}` |
| 5 READY / 6 SUBMIT | client → server | `{char,ready,team?}` in the lobby / `{vote?}` (a vote to send) |
| 7 PUSH / 8 PASS / 9 TABLE | client → server | `{on}` grab or let go of the cart / `{i,to?}` hand index, team-mate id / `{put:i}` or `{take:k}` |
| 14 ABILITY | client → server | `{}`, `{swap:i}` or `{i}` (Sweep: swap layer i with the one above; ignored by the others) |
| 10 STATE | server → clients | phase, round, room, players[] (with `vote`, `ab` = ability used), teams[] (stack, `cx,cy` cart, `pusher`, `table`, `time`, `freeze`, `shield`) |
| 11 POS | server → clients | `{time,tt:[t0,t1],p:[{id,x,y}],c:[{x,y}]}` team clocks, players and both carts, every tick (10 Hz) during build |
| 12 RESULT / 13 ERROR | server → clients | per-round scores and winner / Thai refusal text for one player |
| 15 PEEK | server → one client | `{total,pass,stats}` Spray's preview of the team's cart (`BobOnline.OnPeek` / `OnPreview`) |

Room layout (world units, y up; both server and Unity): stations tool `[0.3,4.06,2.0,4.7]`, chem `[6.0,4.06,7.7,4.7]`,
energy `[0.3,0.3,2.0,0.94]`, method `[6.0,0.3,7.7,0.94]`; table `[3.5,4.2,4.5,4.7]`; sofa `[3.0,0.85,5.0,1.15]`; cart 0.9 × 0.64 starting at (4.0, 2.53).

Tests: start the server, then `cd server && node mp-test.js 4` (bots walk, pick, place, submit) and `node coop-test.js`
(4 bots: solid shelves, pass, table, push, place on the moved cart, a dropped player's parts, the four abilities, the team clock, reconnect; 26 checks). Both must print PASS.
GitHub Actions (`.github/workflows/server-tests.yml`) runs all of these, plus the Unity type-check, on every push and PR.
`cd unity-check && dotnet run` runs the same co-op moves through the C# client.

Note for server code: Nakama keeps match state as Go values between ticks, so `array.push` / `splice` on state can be
lost. Always assign a new array (`t.table = t.table.concat([x])`, `without(a, i)`).

**In the Unity scene** (`Scripts/Game/BobGame.Online.cs`): title button **ออนไลน์ 2-4 คน** → matchmaker → lobby (both teams, heroes, Ready) → rounds. The scene copies the server state into the normal game fields, so the HUD, cart card and result screens are the same as solo. Team-mates walk in your room with what they carry; the other team plays the same room in its own copy, and its layer count and submit show in the top bar. The server runs each team's clock (pause does not stop it) and ends the round when both teams submit.

More tests:
- Server only: `cd server && node mp-test.js 4` (bots walk, pick, place, use their ability, submit; must print PASS).
- To test a branch without touching the main server: `docker compose -p bobtest -f docker-compose.yml -f docker-compose.test.yml up -d` (Nakama on 7360), then `BOB_HOST=127.0.0.1:7360 node mp-test.js 4`; the Editor follows `BOB_PORT=7360`.
- Unity + bots: `cd server && node mp-host.js 2 ../unity/Logs/bob-mp-match.txt`, then menu **Bob > Run Online Test** (or `-executeMethod Bob.EditorTools.BobGameScene.RunOnlineTest`). The Unity bot joins with one bot team-mate against one bot, must print GAME TEST PASSED; screenshots `unity/Logs/bob-game-online-*.png`.

## Co-op scene (Unity)

`Assets/Scripts/Coop/` (game) and `Assets/Scripts/UI/` (UI kit), built into `Assets/Scenes/Coop.unity` by **Bob > Build Coop Scene**.
Written in a cloud session: type-checked with `dotnet build unity-typecheck` (0 errors) but **not yet compiled or played in Unity**.

1. Menu: quick match (2-4 players), create a private room, or join with a room code; server IP field (shared with the Game scene).
2. Lobby (UX S14): two team columns with hero and ✓ พร้อม, room code + copy, hero picker, **ย้ายทีม**, **พร้อม**. Starts when everyone is ready and both teams have a player.
3. Match: walk with WASD, the left-side joystick, or a tap on the floor. Near a station the right column lists its parts (tap to pick).
   Tap a part in the hand to put it where it fits from where you stand: **cart** (place) → **table** (park) → **team-mate within reach** (pass).
   Near the cart: lift the top layer, **จับเข็นรถ / ปล่อยรถเข็น**. Near the table: take parts back. Send = **hold 0.6 s** (a team vote).
   The order ticket shows the 4 live stats vs the spec; the cart card lists the shared stack.
   The hero's ability button (yellow, once a round) sits in the right column; Sweep's shows one button per pair of layers at the cart.
   The clock is the team's own (it turns green while Press holds it). If the app closes mid-match, connecting again rejoins the same seat.
4. Test (UX S25): the screen splits, left = our room, right = theirs, each with its final cart, score, pass ✓/✗ and the dirt wiped as far as the score.
5. Result after 3 rounds; Esc / Android back leaves from the lobby or result.
Two players on one PC: run one copy in the Editor and one as a Windows build (or Unity's Multiplayer Play Mode), both pointing at 127.0.0.1.

Sounds: 6 new co-op sounds (`pass_part`, `table_put`, `cart_grab`, `lobby_join`, `ready_on`, `vote_tick`) made by
`python3 art/tools/make_sfx.py`; the full list, CC0 sources to replace placeholders and import settings are in
`art/docs/12_Sound_Manifest.md`.

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
drag) are written for phones but so far tested only in the Editor; the bot test does not cover drag. The split-screen
test view, cart pushing, passing and the parking table (GDD 9, 20) live in the online co-op scene (above), not in this
single-player scene.

Solo scene extras (`Game/BobGame.Extras.cs`): walk into the cart to push it, an item rest table on the left (tap a hand slot near it to stash, tap the table or press T to take back) and a 4.5 s split-screen test view before the score cards (tap to skip). The online versions of these live in the co-op scene.

## Needs the PC (one sitting)

Everything above was done in the cloud; these steps need Unity, Docker Desktop or a phone. Do them in order:

1. `git pull` this branch, start Docker Desktop, then `start-server.ps1` (`server/modules/index.js` is already rebuilt).
2. Open `unity/` in Unity 6000.6.8f1 and let it import. Done on 2026-11-12: `Scripts/Coop/` and `Scripts/UI/` compile with no errors in Unity 6, and the `.meta` files, `Coop.unity`, `BobAudio.prefab` and Build Settings are committed.
3. **Bob > Build Coop Scene**. Automatic check: start the other players with `cd server && node mp-host.js 2 ../unity/Logs/bob-mp-match.txt`, then **Bob > Run Coop Test** (or `-executeMethod Bob.EditorTools.BobCoopScene.RunTest`, no `-batchmode`); screenshots go to `unity/Logs/bob-coop-*.png`. By hand: play two copies (Editor + a Windows build, or Multiplayer Play Mode): create a room,
   join with the code, ready up, and try push / pass / table / hold-to-send, each hero's ability, closing and reopening one copy
   mid-round (it must rejoin), and the split-screen test.
4. ~~Commit what Unity made~~ (done).
   **UX review fixes (2026-11-13):** menu fits the screen; hand cards show the icon beside the name; cart card lists each layer with its icon and a up/down marker where it reacts; result screen lists every round's score; pause button (top right) opens a pause card and a leave confirm (Esc / Android Back too; the server clock keeps running); lobby hero buttons have portraits; text in the match HUD is >= 36u, player names above heads are larger. Still missing from the spec: team-mate item tray, Quick Ping, TextMeshPro + Mitr / IBM Plex Sans Thai fonts, left-hand mode.
   **Layout (fixed 2026-11-12):** in a match the room camera now sits between the side columns (order ticket + team cart card on the left, action buttons + send on the right), under the top bar and above the hand row (`BobCoopGame.FitRoomCamera`); menus, lobby and result get a solid background; a backdrop camera clears the screen so nothing stale shows; the split-screen test keeps the rooms between the score boxes and the result line. To check another screen size: `BOB_VIEW=2400x1080` before starting Unity, then **Bob > Run Coop Test**.
5. TextMeshPro: import TMP Essentials and make Mitr / IBM Plex Sans Thai font assets (UX spec 2.2); then switch `BobUi.Label` to TMP.
6. Optional: swap placeholder sounds for CC0 ones (`art/docs/12_Sound_Manifest.md`), art for the parking table (asset C07).
7. Android: build an APK, install it, set the server IP to the PC's LAN IP, play one online match phone vs PC.

## Leonardo art (12 Nov 2026)
Background-removed cutouts live in `bob-art/cutouts*` (project files, see `docs/12_RAW_Cutouts.md`). Imported so far: 8 parts, 10 items and the cart (`unity/Assets/Art/Sprites`). Needs the Unity Editor licence (Hub sign-in) to reimport and run `Bob > Run Game Test`.

Title and lobby screens now sit on the GDD-themed Leonardo room pictures (`Sprites/Environments/Lobby/lobby_{rental_room,condo,coffee_shop,kitchen}`; title = rental room, online lobby = condo). Match music is a CC0 loop; 20 sounds are CC0 (see `art/docs/12_Sound_Manifest.md`).

## Android APK (prototype v1)

Build: `Unity.exe -batchmode -quit -projectPath unity -executeMethod Bob.EditorTools.BobAndroidBuild.BuildApk` (or menu **Bob > Android > Build APK**). Output `Builds/BOB-prototype-v1.apk` (git-ignored; debug-signed, ARM64, IL2CPP, landscape, package `com.bob.battleofthebrooms`, min API 24).

Install: copy the APK to the phone and open it (allow "install unknown apps"), or `adb install -r BOB-prototype-v1.apk`.
Server: the game plays offline without a server. For online, start Docker + `start-server.ps1` on the PC, put the phone on the same Wi-Fi, allow port 7350 in Windows Firewall, then in the game open Settings and enter the PC's LAN IP (`ipconfig`, e.g. 192.168.x.x).

Each round now sits on a GDD-themed room picture (round 1 rental room, round 2 condo, round 3 coffee shop; kitchen is ready for round 4). The pause button has a 144u touch area (`BobHitArea`).

The solo scene is full-screen landscape: the camera fills the safe area, the themed room picture fills the screen and the HUD (order ticket, cart card, hand, buttons) floats over it (`FitCamera` in `BobGame.World.cs`). The Coop scene still uses its own split layout.

UX spec 5A in the solo scene (`BobGame.Extras.cs`): a contextual main button (วาง / ยก / พักของ / หยิบคืน / มือเต็ม) at bottom right with a bobbing star over its target, hold-to-send and the ability button beside it, team ring + hero name under the feet, the cart card shows only when standing at the cart, a numbered spawn ring during the briefing and the countdown, and enemies appear 2.5 s after "ลุย!". Not done yet: the 55-degree camera and 3/4 art (F21-F24 are not drawn), spawn rotation for 2-4 players (online), passing to a team-mate and the Quick Ping wheel.

Modular scripts (`unity/Assets/Scripts/Modular/`: IInteractable, StationProp, InteractionDetector, PlayerController, TaskManager, MobileHUDController) now drive movement, what-is-near, the round clock and the HUD in the solo scene. Inspector setup guide: `art/docs/14_Modular_HUD_Setup.md`. The HUD also has the cart slot bar (5 icon slots, top right).

**Direction (13 Nov 2026): task race.** Each round is now a team task list of short one-finger cleaning minigames at stations around the room; the team splits the work, the HUD shows team and rival progress ("ทีมฟ้า 4/6 · 67%"), the first team to 100% wins the round, and the music adds layers and speeds up as a team nears the end or the rival nears a win. Enemies no longer steal anything: they are stains that pop up as emergency tasks. Cart stacking stays as one long task (T04) that gives the team buffs. The GDD is updated; the working spec is `art/docs/16_Task_Race_Minigame_Spec.md` (16 minigames, the first 8 specified in full), the music plan is `art/docs/17_Music_Layers.md`, and the HUD/minigame screens are in `art/docs/11_UX_UI_Spec.md` (S20, S22, S25, S26, S28). The study behind it is `art/docs/15_Task_Minigame_Study.md`. **Not built yet:** the prototype rules, server (`bob_coop` opcodes 16-21), Unity minigames (`Scripts/Minigames/`) and `BobMusicLayers` still run the old cart-only round.

**Level layout for the task race (13 Nov 2026).** `art/docs/19_Level_Layout_Task_Stations.md` sets where minigame stations (TaskSpots) go in a 14×8 room: only inside the band x 2.2–11.8, y 1.0–7.3 (clear of the task list and both thumb zones), a shared spine of parts shelf (back centre) → 4 spawn pads (middle) → cart (front centre), at least 1.8 tiles between stations, at least 5.6 tiles of walking between the steps of a long task, and four zones the seed fills evenly so a team can split the room. Wayfinding: floor rings for everyone, one personal ★ suggestion per player, floor breadcrumbs when a task-list row is tapped, a preview chip at 2.6 tiles; the card still opens only on tap. Three sample rooms for the home chapter (rental room, two-room condo, and a restaurant kitchen proposed as the boss room) are drawn in `art/docs/ux/BOB_Level_Layouts.html` and checked by `node art/docs/tools/check_layouts.js` (all pass; walking is 17–25% of the round). With the first task counts in spec 16 §2.2 a perfect 2–4 player team finished in about 35–47 s, so on 14 Nov 2026 difficulty now scales with team size (2 players D+1, 3–4 players D+2, more short tasks); a perfect team now finishes in 63–75 s at every size. No game code changed.

## AA art review and art bible (13 Nov 2026)

`art/docs/20_AA_Art_Review.md` scores the current in-game art against an AA bar (style consistency, silhouettes at the game camera, materials, light, animation, VFX, UI, budgets, pipeline) and lists where the docs disagree (world setting, enemy roster, logo palette, camera pitch, outline width). `art/docs/21_AA_Art_Bible.md` is the AA art bible (approved 13 Nov 2026: first chapter = GDD home rooms, 3D kit characters, 45 deg camera, concept logo palette, Tripo/Meshy for AI props): shape language, camera 45 deg / ortho 4.1 / heroes 1.15 u, per-type budgets, one toon material + one palette texture for everything, light rig, flipbook faces, animation weights, a 5-layer VFX recipe, a no-artist pipeline and an 8-point asset gate. The reference page `art/docs/ux/BOB_AA_Art_Bible.html` renders real models at the game camera.

The no-artist pipeline is tested: `art/tools/blender/bob_kit.py` (Blender, or `pip install bpy`) builds the 4 heroes and 4 enemies from primitives with the shared rig and idle/walk/cheer, all inside the tri budgets, into `art/aa/kit/` (GLB + FBX). `art/tools/blender/bob_prop_cleanup.py` turns a raw AI image-to-3D mesh into a budgeted, palette-snapped prop (`art/aa/props/`). Not in Unity yet: the toon shaders, outline feature and light prefab come first (Art Bible section 12).
