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

All UI text is TextMeshPro now (`BobUi.Label`, fonts `Assets/Resources/Fonts/TMP`); set up with menu **Bob > TextMeshPro** (see `art/docs/14_Modular_HUD_Setup.md`). The server-address input keeps a legacy `Text` (InputField needs it).

## 2.5D play scene (branch feature/scene-3d)
The solo room is now a real 3D room (wood floor from the Leonardo tile, walls, the themed room picture as a back-wall mural, sun light) seen by a perspective camera pitched 55 degrees with a gentle follow; heroes, enemies, props and FX are the Leonardo sprites standing as camera-facing billboards with soft blob shadows. Walking leans and bounces the hero and leaves dust puffs. Game logic is unchanged (room coordinates x / y = depth); switch back with `World3D = false` in `BobGame.World.cs`.

## Menus, music and game feel (13 Nov)
- Screens around the match (`Game/BobGame.Menus.cs`, UX spec section 4): splash, login (guest), Home / lobby with the time-of-day room picture, mode select (Solo and Friends playable, the rest show why they are locked), hero select with the 15 s timer, shop with a purchase confirm (coins, no random boxes), XP and level. Icons that do not exist yet (wardrobe, friends, recipe book) are left out and listed for the art thread.
- Music: four CC0 tracks (Zane Little) crossfade by screen (`BobSfx.Track`): menu, game, tense under 15 s, result. Nine Kenney CC0 sounds replaced placeholders. Credits in `art/docs/12_Sound_Manifest.md`.
- Feel: the hero accelerates and brakes with squash and stretch (`PlayerController`), the cart keeps rolling after a push, tilts into turns, its load bounces and it kicks up dust, a hit enemy blinks, recoils, squashes and sparks.
- Test note: the bot waits for the Editor to settle before Play (the Editor changes define symbols when it starts, which recompiled scripts mid-match). After an APK build the Editor is switched back to Windows.

**Direction (13 Nov 2026): task race.** Each round is now a team task list of short one-finger cleaning minigames at stations around the room; the team splits the work, the HUD shows team and rival progress ("ทีมเรา 4/6 · 67%"), the first team to 100% wins the round, and the music adds layers and speeds up as a team nears the end or the rival nears a win. Enemies no longer steal anything: they are stains that pop up as emergency tasks. Cart stacking stays as one long task (T04) that gives the team buffs. The GDD is updated; the working spec is `art/docs/16_Task_Race_Minigame_Spec.md` (16 minigames, the first 8 specified in full), the music plan is `art/docs/17_Music_Layers.md`, and the HUD/minigame screens are in `art/docs/11_UX_UI_Spec.md` (S20, S22, S25, S26, S28). The study behind it is `art/docs/15_Task_Minigame_Study.md`. **Not built yet:** the prototype rules, server (`bob_coop` opcodes 16-21), Unity minigames (`Scripts/Minigames/`) and `BobMusicLayers` still run the old cart-only round.

## Hero roster expansion (13 Nov 2026, 8 heroes chosen)

`art/docs/24_Hero_Roster_Expansion.md` sets 8 heroes at launch instead of 4 (Krits chose 8) (adds ซัก Wash `wash`, รีด Press `iron`, ปัด Duster `duster`, ดูด Vac `vac`) and then one hero per season, each with a head silhouette that reads at the 45 deg camera, a minigame specialty (new minigames T17-T23 proposed), a task race ability, a Blender kit build note and an image prompt. All 8 abilities are rewritten for the task race (section 4): the old cart abilities only worked during task T04, and a clock freeze does nothing in a race to 100%. The new set works all round on walking, minigames, emergency stains, the task list and waiting tasks, never fills the bar by itself and never touches the rival team. `press` stays the id for ถู; the new ironing hero is `iron`. No code or models changed yet; the code still runs the old cart abilities.

## AA art review and art bible (13 Nov 2026)

`art/docs/20_AA_Art_Review.md` scores the current in-game art against an AA bar (style consistency, silhouettes at the game camera, materials, light, animation, VFX, UI, budgets, pipeline) and lists where the docs disagree (world setting, enemy roster, logo palette, camera pitch, outline width). `art/docs/21_AA_Art_Bible.md` is the AA art bible (approved 13 Nov 2026: first chapter = GDD home rooms, 3D kit characters, 45 deg camera, concept logo palette, Tripo/Meshy for AI props): shape language, camera 45 deg / ortho 4.1 / heroes 1.15 u, per-type budgets, one toon material + one palette texture for everything, light rig, flipbook faces, animation weights, a 5-layer VFX recipe, a no-artist pipeline and an 8-point asset gate. The reference page `art/docs/ux/BOB_AA_Art_Bible.html` renders real models at the game camera.

`art/docs/23_Asset_Master_Table.md` (data in `23_Asset_Master_Table.csv`, tables rebuilt by `python3 art/docs/tools/asset_table.py`) is the master list of 3D scene assets for all 11 planned rooms: Core pieces every room uses, theme kits (home, modular 0.6 m kitchen, shop, school, outdoor) and one signature heavy object per room, with production line, Tripo credits, priority and status. It also records the furniture check for the rental room L1: only the sofa, bed and fallen futon go to Tripo; shelf, cart, tables, drawers and kitchen modules are scripted in Blender. Minigame cards are 3D (Krits' choice, 13 Nov 2026): each card is a close-up camera on the real room object with the room blurred behind, so the 2D card backgrounds and sprites (F27/F28) become 3D; the extra small pieces are group W-M.

The no-artist pipeline is tested: `art/tools/blender/bob_kit.py` (Blender, or `pip install bpy`) builds the 8 heroes (4 original + wash, iron, duster, vac from doc 24) and 4 enemies from primitives with the shared rig and idle/walk/cheer, all inside the tri budgets, into `art/aa/kit/` (GLB + FBX). `art/tools/blender/bob_prop_cleanup.py` turns a raw AI image-to-3D mesh into a budgeted, palette-snapped prop (`art/aa/props/`). Not in Unity yet: the toon shaders, outline feature and light prefab come first (Art Bible section 12).

## Play screen toward the target picture (cafeteria set, toon look)
- Camera: a fixed, low front-on perspective shot (pitch 30, 32° fov) that makes the back wall and the garden trees stand up behind the play floor; the hero moves inside the set and the camera never follows. The set (`Game/BobGame.Set.cs`) is built in code: tiled floor, teal play mat, long tables with plates and benches, a back wall with shelves, signs above the stations (TextMeshPro, face the camera). All of it uses `BOB/ToonLit` (3-step ramp, rim, dirt amount) and `BOB/Outline` (constant 2-2.5 px hull outline) through `Modular/BobToon.cs`; materials live in `Assets/Resources/Materials` (menu **Bob > Art > Build Toon Materials**).
- 3D heroes: drop a prefab at `Assets/Resources/Heroes/SK_<sweep|bubble|spray|press>.prefab` and the game uses it instead of the sprite (scaled to the sprite height, toon material + outline, faces its walking direction, drives Animator floats `Speed` and bool `Carry` if present). No prefab = the Leonardo sprite as before.
- The first chapter in the Art Bible (D1) is the home rooms; the cafeteria set is the first test set the user asked for, `BuildSet()` can be swapped per chapter.

### 3D heroes (blockout kit) in Unity
- `Assets/Art/Heroes/SK_{sweep,bubble,spray,press}.fbx` + `T_BOB_Palette.png` come from `art/aa/kit`. Menu **Bob > Art > Build Hero Prefabs** (or `-executeMethod Bob.EditorTools.BobHeroImporter.Build`) makes `Resources/Heroes/SK_<id>.prefab` with an AnimatorController (idle/walk, float `Speed`, bool `Carry`; `cheer` state kept for later) and the toon material on the palette.
- `BobToon.Outline` now also handles skinned meshes (second skinned renderer on the same bones). `BobGame.TryHeroModel` uses the prefab when present, else the Leonardo sprite. Bot test passes with the 3D hero (log line `Bob: hero model <id> -> 3D prefab`).
- Next: rental-room chapter (GDD / Art Bible D1) replacing the cafeteria set; enemies/boss FBX from the kit; Carry pose clip.
- Play scene is now the **rental room** (GDD chapter 1, Art Bible D1): wood floor, rug, window, sofa, bed, fallen futon, kitchenette, TV unit and table, built in `BobGame.Set.cs > BuildRentalRoom()` from scripted toon shapes, replaced one by one by the cleaned Tripo models when they are in `Resources/Props` (see the Tripo pack bullet below).
- **Tripo model pack (doc 23)**: 95 cleaned models from the 98 raw Tripo GLBs (3 junk generations skipped) live in `art/aa/props`; the clean pass is `art/tools/blender/bob_prop_cleanup.py` driven by `art/aa/tripo_map.json` / `tripo_jobs.json` and `bob_clean_batch.py` (0 skipped, 0 failed; every asset inside its Art Bible tri budget except `SM_ac_indoor_unit` 1 356 vs 800 — its 192 non-manifold edges stop the collapse decimator). Unity side: FBX in `Assets/Art/Props` (85) and `Assets/Art/Characters` (10), menus **Bob > Art > Build Prop Prefabs** / **Build Character Prefabs** (`BobPropImporter`) build `Resources/Props/SM_*` and `Resources/Characters/SK_*`. `BobGame.Set.cs > TryProp()` drops a model in place of its placeholder, so the room uses models as soon as a prefab exists; it keeps the prefab root scale (the FBX importer compensates Blender's cm export with a root scale of 100, so writing scale 1 shrinks a model 100x). Characters are static (no rig yet). Match test passes with the pack (`GAME TEST PASSED`, shots in `unity/Logs/bob-game-*.png`).
- Kit enemies (`SK_dirt`, `SK_slime`, `SK_trash`, `SK_boss_slime`) are imported by the same menu **Bob > Art > Build Hero Prefabs** (clips idle/caught). `BobGame.Set.cs > TryEnemyModel` swaps the enemy sprite for the 3D model with a 35% tint of the rules colour; placeholder mapping `EnemyKit`: stain, rust -> dirt; dust -> trash; grease, scale -> slime. Each rules enemy needs its own model later (doc 23).
- **Diorama set (14 Nov 2026)**: the rental room is a display piece, not an endless floor. `Game/BobGame.Diorama.cs` writes every surface in code: grass with blades and daisies, a four-plank deck, rock mottle, plank plinth, the teal checker play mat and the wallpaper. The piece is a grass island over a cut-cake stack of soil and rock bands (the strata are geometry bands, each with its own outline, so they read at any zoom; a fine band texture this small just averaged to one brown tone) on a wooden plinth with cream mouldings, ringed by a garden of trees, bushes, flowers, rocks, mushrooms and lanterns (`DressDioramaRim()`, procedural stand-ins for the garden models the art pack lacks). The sky is a written `BOB/Unlit` quad parented to the camera (no light can tint it): gradient, sun glow, clouds and a low sea of cloud under the piece. `FitCamera3D` is a fixed perspective camera (pitch 30, fov 32) that frames the play set like a close diorama photo: the vertical band from the tallest back canopy to the island's front lip fills ~86% of the screen with the canopy just under the top HUD strip, the island runs past the side edges (more crop on tall/narrow windows), and the front garden, rock and plinth run off behind the bottom button row. Wall decor from the pack (`SM_window_arched`, `SM_ac_wall_teal`, `SM_clock_alarm`) hangs on the back wall. Room coordinates and touch mapping are unchanged; match test passes (`GAME TEST PASSED`, shots in `unity/Logs/bob-game-*.png`).
- Room scale is now **14 x 8** (Art Bible / doc 19 L1), hero 1.15 u (`HeroH`). Still the 4 stations of the current rules, placed on doc 19 L1 coordinates (tool shelf S1, chem cabinet S5, energy S8, method board S9); spawn (6.5, 3.3), cart (7, 2), sofa at the left wall (1.3, 5.2). Next step: expand to the 9 stations of doc 19 (needs the rules change). The coop scene (`BobCoopLayout`, 8 x 5) is unchanged.
- All 11 task points S1-S11 and the 3 emergency points E1-E3 of doc 19 L1 are drawn on the floor (`BobGame.Set.cs > BuildTaskPoints`, data table `L1Points`). They are markers only: the playable rules still use 4 stations (S1, S5, S8, S9). The task-race rules (doc 16: task list, 16 minigames, seed, A/B lift, emergency stains, server `TASK_BEGIN`/`TASK_LOCK`) are the next, large step.
- **Task Race (solo, offline) prototype**: mode card "Task Race" on the mode screen -> `BobGame.Tasks.cs`. 6 short tasks on the L1 task points (S6/S7 sweep T01, S9 sort T05 x2, S1 glass T08 x2), 120 s, interaction circle 1.2 (doc 16 section 3), card 1400 x 860, hero bonus +25% for sweep (T01/T05) and bubble (T08). Win = all 6 done; result panel gives XP. Bot test: `Unity.exe -projectPath unity -executeMethod Bob.EditorTools.BobGameScene.RunTaskTest` (scripted pointer via `botPtr`), prints `GAME TEST PASSED: task race 6/6`; screenshots `unity/Logs/bob-game-task-*.png`. Not yet: long and common tasks (T04, T13), emergency stains, server `TASK_BEGIN`/`TASK_LOCK` (asks before touching Nakama), the other 13 minigames.
- **Task Race map system (14 Nov 2026, doc 24)**: a twin-island mini-planet map for the task race. Gameplay, colliders and NavMesh stay flat on X–Z; only the drawn vertices bend, in `BOB/CurvedWorld` (`_BobCurvature` global, default 0.018, plus `_CurvatureScale` per material), and `BobCurvedWorld.Drop()` mirrors the same formula for world labels and ground rings. Team B is an exact +X translation of Team A, never a mirror, so the mobile HUD covers the same corners on both islands. `BobTaskRaceCamera` follows the hero (8.5 up, 7.5 back, fov 40) and eases to a tighter, right-shifted look while the S28 card is open. `TaskSpot` owns the F29 ring and compact label (preview 2.6, NEAR 1.2), the client TASK_LOCK (name badge + `...`, others get a toast) and the 10 s partial-progress retention; closing by × or joystick past 70% is handled by `TaskRaceModeManager`, which also switches PureRace (bank barrier + carve obstacle, no bridges) and BridgeRaid (bridges linked by a real `NavMeshLink` from AI Navigation 2.0.16). All five map pieces are cleaned to ≤ 8,000 tri and imported as `SM_bob_map_*` prefabs; the Tripo islands are assemblies, so `AlignGround` plus a flat `Field` slab at y = 0 (grass palette UV + convex MeshCollider) is the play plane, and every mesh is tinted through palette UVs (`M_BOB_CurvedMap`), never a flat `_Color`. `Tools > BOB > Build Task Race Scene (solo / duo / team)` writes one scene per mode in one command — camera, light, `SK_hero_sweep` hero, the map, sample counters with real props cloned to Team B, PureRace mode and the Build Settings entry (always listed solo → duo → team): `TaskRace.unity` (12 u island, 16 u centres, 3 stations/side, 6 spots), `TaskRace_Duo.unity` (15/20, 4 stations, 8 spots) and `TaskRace_Team.unity` (18/24, 6 stations, 12 spots). Station layout is authored on the 12 u island and scaled by size/12; the hero spawns on the clearest south-half field point (≥ 2.6 u from every station). `Bob.EditorTools.TaskRaceVerify.Run` / `RunDuo` / `RunTeam` re-checks both modes and renders `Logs/taskrace{-duo,-team}-{play,preview,wide}.png`. **NavMesh (14 Nov 2026):** every scene bakes two NavMeshes before it saves — `Assets/Scenes/NavMesh/{TaskRace,_Duo,_Team}_{PureRace,BridgeRaid}.asset` — with a `NavMeshModifier` on every collider so only the `Field` slabs and the bridge decks enter the bake; `TaskRaceModeManager.ApplyNavMesh` swaps the bake with the mode, so PureRace is two islands and BridgeRaid is one walkable map. Two geometry traps were fixed for this: the deck's `Walkable` box is now sized by dividing the world bounds by the measured `lossyScale` (inverting the turned prefab's scale by hand built a 21 u x 0.8 u deck), and the bridge length is measured against the `Field` ellipse at the bridge's z and runs 1.2 u past it (`2 x (gap/2 - bankEdge(z) + 1.2)` = 12.05 / 14.46 / 16.88 u), because the NavMesh erodes the bank by the 0.5 u agent radius and a 9 u plank stopped 0.3 u short. Verify: BridgeRaid `PathComplete` (hero -> Team B spot, 12.9 / 16.2 / 22.3 u; 52 / 60 / 64 tri) and PureRace `PathPartial` in all three scenes. Last runs: `Logs/taskrace-build-light3-{solo,duo,team}.log` and `Logs/taskrace-verify-light3-{solo,duo,team}.log` (6/8/12 unique spots, floor y = 0.00 under every spot, 2 links, 2 carving obstacles, 6/7/9 HUD rows in each scene). **Round director (doc 24 section 14, 14 Nov 2026):** `TaskRaceRound` on `TaskRaceMap/Systems` builds the team task list of docs 16 section 2.2 from a seeded Fisher–Yates shuffle of the 15 minigames (T04 cart first, never repeated, only categories the map has), hands each station the first task of its own category (Everyone next, then any) and restocks that station when the task is done, so 3 / 4 / 6 stations carry 6 / 7 / 9 tasks. The scenes are baked with seed 1975; a played round rerolls the seed (`randomSeedEachPlay`). Measured workload (cards + walk at 3.2 u/s): solo 63 / 71 / 79 s, duo 77 / 86 / 96 s, team 91 / 95 / 98 s at D1–D3 — all inside the 120 s round and the 90–100 s target, and a 3–4 player list sheds one short per difficulty step because the 15% card stretch would otherwise push 9 tasks to ~113 s at D3. `TaskSpot.SetTaskActive(false)` idles a station with no work left (no ring or label, `TryOpen` refused). `TaskRaceVerify` prints the plan and workload of all three bands. **Task list HUD (14 Nov 2026):** `BobTaskRaceHud` is added to every scene by the builder and bound to the round. Its "ใบงาน" card lists the plan in order (T04 first) with the category disc, the name and the weight, and marks each row เสร็จ / ทำอยู่ (TASK_LOCK held) / รอคิว (no station has taken it yet); the header counts tasks (`ทีมเรา 3/6`) while the bar and % use weights, as docs 16 section 2.2 asks, and the bar steps rather than snaps. The main button follows `TaskSpot.ActiveNear` (`ทำงาน: <name>` takes the TASK_LOCK, a second press releases it, a refusal shows the `Blocked` toast). The canvas is screen-space-camera on `Camera.main` so the verify pass can photograph it: `Logs/taskrace{-duo,-team}-hud.png`, rows logged as `HUD: 6 / 7 / 9 task row(s)`. **Lighting pass (14 Nov 2026):** the scenes are lit like a pastel diorama photo rather than a flat unlit model. `BOB/CurvedWorld` and the non-curved `BOB/ToonLit` now sample the shadow map (`SHADOW_COORDS` / `TRANSFER_SHADOW_WPOS` on the bent world position, `SHADOW_ATTENUATION`) and keep 55–78% of the key light in shadow through a cool `_ShadowTint`, so shadows read as blue pastel instead of black; `BOB/CurvedWorld` also carries its own `SHADOWCASTER` pass that applies the same curve and `UnityClipSpaceShadowBias` normal offset, so shadow maps see the dome and not the flat plane. Ambient is the scene trilight read straight from `unity_AmbientSky/Equator/Ground` (sky 0.56/0.70/0.94, equator 0.82/0.80/0.82, ground 0.42/0.36/0.50), plus a tight Blinn-Phong sheen and a world-space mottle of ±3.5% on upward faces. The sun is `(46, 75, 0)` at 0.85 with soft shadows (strength 0.85, bias 0.02 / normal 0.2 / near 0.2), and a linear fog 25 → 90 u in 0.80/0.89/0.98 ties the island rims to the sky. The imported prefabs shipped with `m_CastShadows: 0` / `m_ReceiveShadows: 0`, so the builder now re-enables both on map and hero renderers (`cast/receive shadows on for N renderer(s)`, 86/68/122 + 2). `BobSkyBackdrop` draws a written gradient sky with clouds at depth 70 on whatever camera renders (play, preview, wide, verify), fitted to the frustum so there is no quad seam. Setup: docs 24 section 3.1; check: doc 24 section 12 item "แสง เงา และท้องฟ้า". The S28 card is live (`BobMinigameHost`, 8 mechanics in the 1240 x 620 play area; T04 cart and T12 chairs stay in the world with the timed bar, and a completed card still ends in `spot.Close(completed: true)`). Not yet: the card's 3D prop stage (a RenderTexture stage of the real `SM_*` / `SK_*` prefabs instead of flat 2D shapes), the real station set from docs 16/19, emergency stains T16, the server seed contract, a walking AI (the rival is a pace model, doc 24 section 10.3). Setup guide: `art/docs/24_Task_Race_Map_System.md` (section 10 = HUD contract, section 13 = map size per mode, section 14 = workload and randomization).
- **Task Race parity, bridge and dressing pass (14 Nov 2026, doc 24 sections 5–6)**: the two islands are now the same prefab at the same scale with the same dressing seed, because a race on two differently shaped islands is unfair on sight — the `Island B` slot is gone from the assembler (`island_land2` stays in the project as an optional third island, never a race map). The plank footbridge moved to an always-active `BridgeDetail_Group` (deck, rails, piers) while `Bridges_Group` keeps only the `Walkable` colliders and `NavMeshLink`s, and PureRace seals each deck with a red-and-cream `BobBridgeGate` under `RiverBarrier_Group` instead of hiding the bridge — the islands read as connected in both modes, and only the walkable layer changes. Generated dressing meshes (`HideAndDontSave`) are parented to their own component so an inactive group cannot save a scene without a bridge deck. Each island gets a 16-piece prop set (two street lamps, two yellow-flowering bushes, pots, a cafe table with red stools, orange crates, chalkboard, sign, fence, rocks) placed under `TeamA/Props` before the clone, so Team B receives the identical layout by translation only. Contact-AO decals are measured from the counter top and clamped to 1.6–2.6 u (they were unioning every renderer of a station and drawing 10–20 u violet rings around each island). The waterfall headwall was rebuilt in slate palette cells (no pale stone accents, narrow lip, a skirt of rock under the lip) with its plunge pool moved onto the river's north mouth, grass tufts are `area x 18` with brighter tips, and the mown patches use a shade at or above 1 (they used to read as camouflage). The sky mesh is clip-space (`BOB/SkyNDC`) and `shadowDistance` is 45 for the ~30 u map. Rebuilt and verified all three scenes: 6 / 8 / 12 spots, `PathComplete` 12.9 / 16.2 / 22.3 u in BridgeRaid and `PathPartial` in PureRace, 16 decor props per island, AO under 6 / 8 / 12 stations. Renders: `unity/Logs/taskrace{-duo,-team}-{play,preview,wide,hud}.png`.
- **Task Race gameplay, barriers and bridge-lane cleanup (14 Nov 2026, doc 24 sections 5.4 and 6–6.2)**: the three remaining visual overlaps are fixed in the rebuilt scenes and enforced by the builder. Props moved off both bridge decks — decor now lives in the island's central band (|z| <= 0.74 after each prop's own footprint), the yellow bush is shrunk to 0.6 m because the full plant is 3.0 x 2.9 u, the S2 counter moved onto the centre line, and `BobMapAssemblerEditor.ValidateClearance` runs *after* the Team B clone (34 / 36 / 40 props checked, 0 station and 0 decor overlaps) with a three-zone rule: 1.5 u on a deck, 1.0 u beside it (0.2 u for station counters, which are the play targets) and 1.5 u at a landing. The south river mesh is clamped flush at `z = -islandDepth/2` (-4.50 / -5.63 / -6.75, 500 vertices) so no water sheet hangs past the front cliff, and the bridge ends meet the shoreline with the inner counters/bushes clear. `BridgeRaid` is renamed **`TimedBridgeRaid`** (NavMesh asset `..._TimedBridgeRaid.asset`, legacy `_BridgeRaid` deleted). A new always-active `BridgeBarrier_Group` carries a red-and-cream `BobBridgeBarrier` per deck: a full-width `BoxCollider` plus a carving `NavMeshObstacle` keep the deck blocked, the clock warns at 65 s remaining (5 s shake + `สะพานจะเปิดใน 5 วินาที!`), then at 60 s drops the gates 1.5 u over 0.6 s, disables both blockers and lets the bridge `NavMeshLink`s carry players across (`สะพานเปิดแล้ว! ข้ามไปป่วนจุดงานคู่แข่งได้!`); the 120 s clock runs in both modes. Shared team pools now pay 100 points per Dirty->Clean (including a re-clean) and sell a 30 s shield for 150 through an atomic `TryPurchaseShield`; the HUD top bar shows `เวลา mm:ss`, `Team Points: N` and the mode line, turning amber at 65 s and red at 10 s. `TaskSpot` owns `ownerTeam`, `SpotState {Dirty, Clean}`, one sabotage per station per match (S28 card 2.75 s, joystick past 70% cancels, completion makes the station Dirty again, restores the same task and drops the defender's done/doneWeight by one) and the timed shield (`isProtected`, gold ring + badge, blocks sabotage with `จุดงานนี้มีเกราะป้องกันอยู่!`). `TaskRaceRound` keeps a cleaned station Clean for `restockDelay 6 s` so it can be sabotaged, puts the same task back after a hit and cancels the pending restock. A lightweight debug bar (always on in the scene) adds eight buttons — mode toggle, -10 s, open bridges, +100 points, sabotage, shield, reset and hide (a `DBG` chip restores it); `TaskRaceModeManager.Instance` is now a lazy singleton and `ApplyMode()` collects the barriers itself, so the edit-mode verify and the bar work without `Awake`. Verified exit 0 in all three scenes: gates shut give `PathPartial`, gates open `PathComplete` (7.6 -> 13.9 u solo, 9.7 -> 17.5 duo, 9.3 -> 18.7 team), `barriers=2`, points 100/100 on clean, sabotage refused while shut, sabotage drops defender B 1 -> 0, the repeat is refused with `จุดนี้ถูกป่วนไปแล้ว`, the shield spends 150 -> 0 and blocks an enemy sabotage, and the 65 -> 60 crossing drops both gates. Logs: `unity/Logs/taskrace-verify{-duo,-team}.log`; renders `unity/Logs/taskrace{-duo,-team}-{play,preview,wide,hud}.png`.
- **Task Race playability pass (14 Nov 2026, doc 24 sections 10.1 and 10.3)**: the map is now playable on a phone without a keyboard. `BobTaskRaceTouch` adds two invisible uGUI zones as the first children of the HUD page — a bottom-left floating joystick (ink base + centre dot at rest so new players see where it lives, knob on press, 150 canvas-unit full tilt, 12% dead zone) and a full-screen tap-to-walk catcher that raycasts onto the hero's floor plane; HUD panels still win every tap because the zones are behind them, and the joystick zone is lifted above the S28 dim so the 70% cancel gesture still works while the card is open. `BobTaskRaceHero` became a tap/stick walker (`CharacterController`, arrive radius 0.4, eased stop over 0.9 u) that freezes its body but keeps publishing `InputVector` while a card or the result is up; keyboard axes remain the desktop fallback. Taps on the far island are ignored while the gates are shut, and if the hero still leaves the rim it respawns on the last ground it stood on (`y < -6`), so there is no void death. `BobTaskRaceRival` is the Team B opponent: an offline pace model that cleans its own stations every `round.EstimatedSeconds / TaskCount x pace` (±12% jitter, 4 s head start, retry 1 s while the island restocks) through the normal `TaskSpot.ForceComplete` path, so points, counts and restocks behave exactly like a human station; `pace` (1.12 by default) is the invisible balance lever and finishing the whole Team B list ends the match (`manager.EndMatch()`). `TaskRaceModeManager` now decides a match once (`LastResult` guard, `RoundWon`/rival finish/clock zero), zeroes the clock and raises `MatchEnded`; `BobTaskRaceProfile.Record` pays coins `25 + win 50 / draw 20 / loss 10 + local points/20` and XP `30 + win 25 + tasks x 8` into the lobby's own `bob.coins` / `bob.xp` keys plus `bob.taskRace.best/matches/wins`, so the Home currency chips show the match payout. The S27 overlay (`ShowResult`) shows win/draw/loss, `เรา N : M คู่แข่ง`, both task counts, the reward, the best score (`สถิติใหม่!`), and buttons `เล่นอีกครั้ง` (`RestartMatch`: new seed, gates shut, 120 s) and `กลับเมนู` (`SceneManager.LoadScene("Game")`). The player HUD lost its debug leaks: the eight-button debug bar is off by default and gated to editor/development builds (`DebugBarAllowed`), the task-card footer now reads a player advice line (`เหลือ N งาน · เดินใกล้จุดงานแล้วกด ทำงาน`, or the bridges-open / Pure Race line) instead of the round workload/seed summary, and the top bar reads `เรา N · คู่แข่ง M` + `คู่แข่งเสร็จ k/N` + the mode line. The lobby's Home now shows Task Race as a static card and `เล่น` loads the `TaskRace` scene (`BobAndroidBuild` lists Game + TaskRace). Verified in all three scenes (exit 0): `player HUD: 6/7/9 task row(s), touch=joystick + walk, rival=pace 1.12`, `result overlay: open=True draw 0:0 ... reward 45 coins +30 xp`; renders `unity/Logs/taskrace{-duo,-team}-hud.png` and `...-result.png` (the verify calls `EndMatch(record:false)`, so it never touches the wallet). Next: the card's 3D prop stage (RenderTexture), T16, server contract, chapter/stage layer (doc 24 section 15).
- **Merged with origin/main (15 Nov 2026, commit 110024a + fix)**: the parallel session's "see it in half a second" pass is now in: the HUD clock is 48 px on a 940 x 124 bar placed clear of the task card (the two cream panels used to overlap), the right column reads `คะแนนทีม N`, `คู่แข่ง M · เสร็จ k/N` and `ทีมเรา P%`, task rows use category icons (`icon_skill_*`, handed to the HUD through `BindArt` because the Task Race scene has no `BobGame` to set `BobUi.Art`), the next unfinished job is highlighted, done rows are ticked and struck through, and the S28 card opens with a two-second `ลากจอยเพื่อปิดการ์ด` chip floating over the dimmed screen above the joystick (its own container, so the dark plate no longer draws over its text and the play area stays clear). The P0 behaviour stays on top (rival task count in the same line, advice footer, debug bar editor-only, result overlay and payout). The lobby art (four backdrops, five shop cut-outs) also comes from that commit, re-rendered in the same toon island style. The merge had to restore `MiniPointer`, `IsHero` and `miniDrags` in `BobGame.Tasks.cs`: commit 110024a deleted those helpers while the new card code still called them, so `origin/main` did not compile on its own.
