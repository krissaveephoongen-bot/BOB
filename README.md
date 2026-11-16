# Battle of the Brooms (สมรภูมิปัดกวาด) — development workspace

GDD: https://claude.ai/artifact/VsJEEyQDx4HwvrjZk2MEAu
Playable prototype: https://claude.ai/artifact/M9orSH3LFGbYboRm7QAkHn

> **หลาย branch ทำงานพร้อมกัน:** กระดานจองงาน/สถานะอยู่ที่ [COORDINATION.md](COORDINATION.md) — จองงานก่อนลงมือ, แจ้งก่อนแก้ไฟล์ hot, และต่อคิว rebuild ซีนทีละคน

> **Current state (16 Nov 2026): the Unity client is offline + pure UGS.** The playable path is
> Game → Home → chapter select (6 chapters) → TaskRace story (3 rounds × 100 s, stars, next chapter).
> `Scripts/Net/` and `Scripts/Coop/` were deleted with the Nakama → UGS migration; the Nakama/LINE/
> Co-op material below is kept as server-side reference only. APK v2 (`0.1.0 (2)`) is on Firebase.
> Checks: `bash unity-typecheck/check.sh`, `cd unity-check && dotnet run`, `node server/task-race-test.js`.

## What is here

| Folder | What | State |
| --- | --- | --- |
| `prototype/index.html` | Browser prototype, solo vs a simulated rival. Holds the rules (parts, reactions, enemies, scoring). | Tested with `node prototype/test.js` |
| `server/` | Nakama 3.22.0 + Postgres 16 in Docker. `modules/index.js` is generated from the prototype rules by `build-module.js`; `rpc.js` holds the RPCs. Kept for server tests and as the protocol reference; the Unity client no longer calls it. | Tested with `server/smoke-test.sh` |
| `unity/` | Unity 6000.6.8f1 project (open this folder in Unity Hub). uGUI + TextMeshPro + Unity Gaming Services in `Packages/manifest.json`; sprites/audio in `Assets/Art`, `Assets/Audio`. | Editor check: **Bob > Run Game Test** |
| `unity/Assets/Scripts/` | `Rules/` (pure C# rules, offline rooms, Task Race plan), `Game/` (menus + solo match), `TaskRace/` (island map, story series, minigames, HUD), `UGS/` (auth, cloud save, leaderboard, Relay/Lobby scaffold), `Dev/` (bot/E2E drivers), `Editor/` (scene builders + checks). | Type-checked by `unity-typecheck/`; editor checks run in Unity |
| `unity/Assets/Scenes/Game.unity` | **Playable single-player scene**: room, 4 stations, cart, sofa, hero (tap / WASD), pick up, stack 5 layers + 2 methods, live stat bars, abilities, 3 rounds vs the simulated rival. Rooms and scores are local (`Rules/BobMatch.cs`, `Rules/BobRules.cs`); no server is needed. Code in `Scripts/Game/`. | Bot plays a full match: menu **Bob > Run Game Test** |
| `server/match.js` | **Real-time protocol reference** (`bob_coop`): server-authoritative match for 2-4 players in two teams, one shared cart per team, best of 3. The Unity client for it was removed with the pure-UGS migration. See [Multiplayer](#multiplayer). | Bots play full matches: `node server/mp-test.js 4` |
| `dashboard/index.html` | Balance dashboard: reads `bob_analytics` from the local Nakama and charts part, character, round and room stats. | Checked in Chromium, light and dark |
| `unity-check/` | .NET console app: stacking spot checks, the Task Race plan/emergency table (`BobTaskRaceRules.SelfTest`), offline room + rival invariants. Runs without Unity or the server. | `UNITY-CHECK PASSED` |
| `unity-typecheck/` | Type-checks every script in `unity/Assets/Scripts` without the Unity Editor (NuGet reference assemblies + shims for TMP/AI Navigation/Android). Gate compares against `baseline-errors.txt` so only new error kinds fail. | `bash unity-typecheck/check.sh` passes |
| `unity/Assets/Scripts/UGS/` | **Unity Gaming Services scaffold**: anonymous auth, cloud save, leaderboard, Relay/Lobby manager and debug UI. Not wired into the story UI yet. | Compiles in Unity; not exercised end-to-end |
| `art/` | Official concept boards and production docs (`docs/`, including the full UX/UI spec `docs/11_UX_UI_Spec.md` and wireframes `docs/ux/BOB_UX_Wireframes.html`, the minigame card wireframes `docs/ux/BOB_Minigame_Screens.html`, and the 3/4 cafeteria play screen `docs/ux/BOB_Play_Screen_Cafeteria.html`), sprite sources (`source/`) and generator tools (`tools/`). Unity-ready copies live in `unity/Assets`. | Concept / placeholder |

## Run

Play (no server): open `unity/` in Unity 6000.6.8f1, load `Assets/Scenes/Game.unity`, press Play.

Checks without Unity:

```bash
bash unity-typecheck/check.sh
cd unity-check && dotnet run
node server/task-race-test.js
```

The Nakama server is optional and only used by its own tests (`./start-server.sh`, `./server/smoke-test.sh`).

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

## LINE Login (server reference, docs 28)

**The Unity client no longer offers LINE login** — the pure-UGS migration left the guest path only, and the
`line_login` RPC is kept server-side for reference. The design: the browser does the OAuth dance and the
Nakama RPC exchanges the code, because LINE requires the channel secret even with PKCE — so the secret lives in
`server/.env` (git-ignored, copy `server/.env.example`) and never in the APK:

```
LINE_CHANNEL_ID=...
LINE_CHANNEL_SECRET=...
LINE_REDIRECT_URI=bob://line-login
```

Then `node build-module.js && docker compose restart nakama`, and check the server sees it:
`curl -s -X POST "http://127.0.0.1:7350/v2/rpc/line_status?http_key=defaulthttpkey&unwrap=true" -d '{}'`.
The Android build still declares the `bob://line-login` intent filter (`Editor/BobAndroidDeepLinks.cs`), but no
Unity screen consumes it after the pure-UGS migration. Full setup and limits: `art/docs/28_LINE_Login.md`.

## One rule set, three places

`prototype/index.html` is the source of truth. After changing a rule there:
1. `node server/build-module.js` (and `docker compose restart nakama` if the server is running).
2. Mirror the change by hand in `unity/Assets/Scripts/Rules/BobRules.cs` (or `BobTaskRaceRules.cs` for Task Race).
3. Run `cd unity-check && dotnet run` and `node server/task-race-test.js`; the Task Race table self-tests against
   the seed values the browser/server produce.

## Unity on your Windows PC

1. Install **Unity Hub** and **Unity 6000.6.8f1** with Android Build Support (iOS needs a Mac).
2. In Unity Hub open this repository's `unity/` folder directly — do **not** create a new project or copy
   `Assets/Scripts`. Package restore brings uGUI, TextMeshPro, AI Navigation and Unity Gaming Services.
3. Open `Assets/Scenes/Game.unity` and press Play. No Docker, Nakama server or account is needed for the
   offline story path. If the scene is missing after an art change, run **Bob > Build Game Scene**.
4. Checks that run in the Editor: **Bob > Run Game Test**, **Bob > Run Story Series Check**, **Bob > Run Pause
   Check**, **Bob > Run Countdown Check**, **Bob > Run Bot Check**, **Bob > Run E2E**.
5. Android: **Bob > Android > Build APK** (see [Android APK](#android-apk-prototype-v1)).
6. Without Unity (cloud sessions): `bash unity-typecheck/check.sh` and `cd unity-check && dotnet run`
   (needs the .NET 8 SDK). These check C# only; scene builds, play-mode checks and the APK stay on the PC.

## Not done yet

- **Online is a co-op room, not PvP yet.** S15 (16 Nov 2026) hosts or joins a 2–4 player room over UGS
  Lobby + Relay + Netcode: the host's seed is shared, poses follow at 10 Hz and a cleaned station reaches
  every device through the host. There is no two-team match, no server-scored points, and no reconnect,
  host migration or late join. The Nakama protocol in [Multiplayer](#multiplayer) is kept as a server-side
  reference.
- **Story content gaps:** emergency stains (T16) have no client implementation yet; chapters 2-6 reuse the
  chapter-1 island skin; rule cards and Cart Rush from the GDD are not enforced. Chapter unlock levels are
  being rebalanced against the current XP rewards.
- Scrub and Wash abilities, obstacles other than the sofa, the shelves and the table, the other 7 scenes; Party / Community modes.
- Balance: every number is an untested starting value. `node server/balance-sim.js` compares the hero abilities
  (results and suggested tweaks: `art/docs/13_Balance_Sim.md`); nothing has been changed from it yet.
- Development passwords, the `defaultkey` server key and the `defaulthttpkey` HTTP key must be replaced before any deployment.
- Analytics is server-side: `prototype/index.html?server=...` sends `bob_log_event` events. The Unity client no longer
  calls the server, so it logs nothing; online match logging (`mode: mp`) still exists in `server/match.js` for the
  protocol reference.

## Multiplayer

**Server-side reference only since the pure-UGS migration (the Unity client was removed).** The server owns a
multiplayer match: phones send intents, the server checks them and broadcasts the state. Code: `server/match.js`
(bundled into `modules/index.js` by `build-module.js`).

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
- **Reconnect**: a dropped player keeps their seat until the match ends; the server holds the seat and the client
  would rejoin with the stored match id (no Unity client today).
- Join by matchmaker (2-4 players) or a private room (`bob_create_match` RPC, then join with the match id).

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
| 15 PEEK | server → one client | `{total,pass,stats}` Spray's preview of the team's cart |

Room layout (world units, y up; server only now): stations tool `[0.3,4.06,2.0,4.7]`, chem `[6.0,4.06,7.7,4.7]`,
energy `[0.3,0.3,2.0,0.94]`, method `[6.0,0.3,7.7,0.94]`; table `[3.5,4.2,4.5,4.7]`; sofa `[3.0,0.85,5.0,1.15]`; cart 0.9 × 0.64 starting at (4.0, 2.53).

Tests: start the server, then `cd server && node mp-test.js 4` (bots walk, pick, place, submit) and `node coop-test.js`
(4 bots: solid shelves, pass, table, push, place on the moved cart, a dropped player's parts, the four abilities, the team clock, reconnect; 26 checks). Both must print PASS.
GitHub Actions (`.github/workflows/server-tests.yml`) runs all of these, plus the offline Task Race/LINE checks
and the Unity type-check gate, on every push and PR. `cd unity-check && dotnet run` checks the pure C# rules
(offline rooms, Task Race table) without Unity.

Note for server code: Nakama keeps match state as Go values between ticks, so `array.push` / `splice` on state can be
lost. Always assign a new array (`t.table = t.table.concat([x])`, `without(a, i)`).

More tests:
- Server only: `cd server && node mp-test.js 4` (bots walk, pick, place, use their ability, submit; must print PASS).
- To test a branch without touching the main server: `docker compose -p bobtest -f docker-compose.yml -f docker-compose.test.yml up -d` (Nakama on 7360), then `BOB_HOST=127.0.0.1:7360 node mp-test.js 4`.

## Co-op scene (removed 16 Nov 2026)

`Assets/Scripts/Coop/`, `Assets/Scripts/Net/` and `Assets/Scenes/Coop.unity` were deleted with the
Nakama → pure-UGS migration. The design and UX notes stay in `art/docs/` and the Nakama protocol above;
`Scripts/UGS/` holds the Relay/Lobby scaffold that would carry this mode next.

## Play the Unity scene

1. Open `unity/` in Unity 6000.6.8f1 and open `Assets/Scenes/Game.unity` — no server needed.
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
3. Coins, hero choice, sound settings and the chapter pick are saved on the device (`PlayerPrefs`). The game
   runs fully offline; `Scripts/UGS/` is not wired into the menus yet.
4. **Bob > Run Game Test** lets a bot play a full match in Play mode (Console: `GAME TEST PASSED`, screenshots in
   `unity/Logs/bob-game-*.png`). Command line, with the Editor closed on this project:
   `Unity.exe -projectPath unity -executeMethod Bob.EditorTools.BobGameScene.RunTest -logFile game-test.log`

Known gaps: all UI text is TextMeshPro with Mitr / IBM Plex Sans Thai SDF fonts; the art is still the placeholder
set in places; touch controls (joystick, drag) are written for phones but mostly exercised in the Editor. Emergency
stains (T16) and the per-chapter island skins are not built yet.

Solo scene extras (`Game/BobGame.Extras.cs`): walk into the cart to push it, an item rest table on the left (tap a hand slot near it to stash, tap the table or press T to take back) and a 4.5 s split-screen test view before the score cards (tap to skip).

## Needs the owner machine

The cloud checks (`unity-typecheck/check.sh`, `unity-check`, the Node server tests) cover C# and protocol only.
These steps need Unity or a phone:

1. Open `unity/` in Unity 6000.6.8f1 and let the packages import.
2. Run the Editor checks: **Bob > Run Game Test**, **Run Story Series Check**, **Run Pause Check**, **Run Countdown
   Check**, **Run Bot Check**, **Run E2E**. Rebuild the Task Race scenes only after touching a builder/spot (queue in
   `COORDINATION.md` §5).
3. Build the APK (**Bob > Android > Build APK**) and distribute through Firebase; the `bob-preprod` tester group still
   has to be created in the console.
4. Device pass: fresh install, airplane mode (offline), guest login, chapter 1 → chapter card, pause / Android back,
   audio, 20:9 and 16:9 safe areas.

## Leonardo art (12 Nov 2026)
Background-removed cutouts live in `bob-art/cutouts*` (project files, see `docs/12_RAW_Cutouts.md`). Imported so far: 8 parts, 10 items and the cart (`unity/Assets/Art/Sprites`). Needs the Unity Editor licence (Hub sign-in) to reimport and run `Bob > Run Game Test`.

Title and lobby screens now sit on the GDD-themed Leonardo room pictures (`Sprites/Environments/Lobby/lobby_{rental_room,condo,coffee_shop,kitchen}`; title = rental room, online lobby = condo). Match music is a CC0 loop; 20 sounds are CC0 (see `art/docs/12_Sound_Manifest.md`).

## Android APK (prototype v1)

Build: `Unity.exe -batchmode -quit -projectPath unity -executeMethod Bob.EditorTools.BobAndroidBuild.BuildApk` (or menu **Bob > Android > Build APK**). The build bumps `AndroidBundleVersionCode` by one and restores it if the build fails; optional flags `-bobVersionCode N`, `-bobNoBump` and `-bobVersionName X` override that. Output `Builds/BOB-prototype-v1.apk` (git-ignored; debug-signed, ARM64, IL2CPP, landscape, package `com.bob.battleofthebrooms`, min API 26).

Install: copy the APK to the phone and open it (allow "install unknown apps"), or `adb install -r BOB-prototype-v1.apk`.

**Firebase App Distribution (project `bob-preprod`)**: `bash distribute-android.sh` bumps `AndroidBundleVersionCode`, builds and uploads in one go (`FIREBASE_GROUPS=<group> ...` releases to testers straight away, `--no-bump` rebuilds at the current code — the script passes `-bobNoBump` and lets the Unity build own the bump, so a failed build never burns a release number). One-time: `npm i -g firebase-tools && firebase login`, and create the tester group in the Firebase console. The packaged app is `1:438462393037:android:76c94b4645660e52e69784` for package `com.bob.battleofthebrooms`; `unity/Assets/google-services.json` holds the same client (keep it out of public forks, and restrict the key in Google Cloud Console to this package name + the release keystore's SHA-1). Firebase rejects a repeated version code, which is why the bump is automatic rather than a manual step. A build was verified and uploaded on 16 Nov 2026 (`versionCode 2`, 89.1 MiB, debug-signed, arm64; release `0.1.0 (2)`, docs 24 §15.19). If the CLI answers 401, run `firebase -i login --reauth` — the `-i` flag is required when stdin is not a TTY — then `bash distribute-android.sh --upload-only` ships the existing APK without rebuilding. Distributing to a tester group only works once that group exists in the console (`bob-preprod` had none on 16 Nov, so the group step returned 404 while the release itself uploaded fine); leave `FIREBASE_TESTER_GROUP` empty until it does.
Server: the game plays fully offline and has no server address UI. The Nakama/Co-op multiplayer path was removed from
the client on 16 Nov 2026; the server remains a reference for a future online mode.

Each round now sits on a GDD-themed room picture (round 1 rental room, round 2 condo, round 3 coffee shop; kitchen is ready for round 4). The pause button has a 144u touch area (`BobHitArea`).

The solo scene is full-screen landscape: the camera fills the safe area, the themed room picture fills the screen and the HUD (order ticket, cart card, hand, buttons) floats over it (`FitCamera` in `BobGame.World.cs`).

UX spec 5A in the solo scene (`BobGame.Extras.cs`): a contextual main button (วาง / ยก / พักของ / หยิบคืน / มือเต็ม) at bottom right with a bobbing star over its target, hold-to-send and the ability button beside it, team ring + hero name under the feet, the cart card shows only when standing at the cart, a numbered spawn ring during the briefing and the countdown, and enemies appear 2.5 s after "ลุย!". Not done yet: the 55-degree camera and 3/4 art (F21-F24 are not drawn), spawn rotation for 2-4 players (online), passing to a team-mate and the Quick Ping wheel.

Modular scripts (`unity/Assets/Scripts/Modular/`: IInteractable, StationProp, InteractionDetector, PlayerController, TaskManager, MobileHUDController) now drive movement, what-is-near, the round clock and the HUD in the solo scene. Inspector setup guide: `art/docs/14_Modular_HUD_Setup.md`. The HUD also has the cart slot bar (5 icon slots, top right).

All UI text is TextMeshPro now (`BobUi.Label`, fonts `Assets/Resources/Fonts/TMP`); set up with menu **Bob > TextMeshPro** (see `art/docs/14_Modular_HUD_Setup.md`).

## 2.5D play scene (branch feature/scene-3d)
The solo room is now a real 3D room (wood floor from the Leonardo tile, walls, the themed room picture as a back-wall mural, sun light) seen by a perspective camera pitched 55 degrees with a gentle follow; heroes, enemies, props and FX are the Leonardo sprites standing as camera-facing billboards with soft blob shadows. Walking leans and bounces the hero and leaves dust puffs. Game logic is unchanged (room coordinates x / y = depth); switch back with `World3D = false` in `BobGame.World.cs`.

## Menus, music and game feel (13 Nov)
- Screens around the match (`Game/BobGame.Menus.cs`, UX spec section 4): splash, login (guest), Home / lobby with the time-of-day room picture, mode select (Story, Solo, Duo and Team are playable; Party and Community show why they are locked), hero select with the 15 s timer, shop with a purchase confirm (coins, no random boxes), XP and level. Icons that do not exist yet (wardrobe, friends, recipe book) are left out and listed for the art thread.
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
- **Chapter layer, stage select S13 (15 Nov 2026, docs 24 section 15.3 P0)**: `ChapterDefinition` (ScriptableObject, six assets in `Assets/Resources/Chapters`, menu **Bob > Data > Build Chapter Definitions**) carries each GDD chapter's unlock level (1 / 6 / 13 / 21 / 31 / 41 at the time; rebalanced to 1–6 in the progression pass below), its difficulty band (D1 = chapters 1–2, D2 = 3–4, D3 = 5–6), the card picture and a one-line blurb. `Chapters` loads them in unlock order and remembers the pick in `bob.chapter`. Home's chip became a chapter card (`บท N · ชื่อ`, band) that opens the new S13 screen (`BobGame.Menus.cs > StageSelect`, `Phase.Stages`): six cards, locked until the player's level reaches `unlockLevel`, a footer with the player level and the next unlock, and `เล่นบทนี้` loading the `TaskRace` scene. `TaskRaceRound.Start` now reads the picked chapter and sets its band before the round starts (the serialized difficulty stays the fallback for tests and the scene builder), and the round log names the chapter. Not yet: chapter skins on the island, rule cards and the full pin-map S13 art (P1/P2).
- **Guest login S02 is real again (16 Nov 2026, docs 11 S02, docs 24 section 15.21)**: the pure-UGS migration deleted the login screen while the E2E driver kept pressing its button, and nothing ever wrote `bob.guest`. `Phase.Login` is back: after the splash, a first launch shows `ยินดีต้อนรับนักปัดกวาด` and `เล่นเลย` signs in through `AuthenticationService.SignInAnonymouslyAsync()` via `UgsBootstrapManager` (created on the spot with `signInOnAwake = false`, because neither scene ships one), stores `bob.guest=1` and `bob.playerId`, then goes Home. A failed sign-in (10 s deadline) turns the button into `ลองอีกครั้ง` and offers `เล่นแบบออฟไลน์`, so the solo story never depends on the network. `BobE2EDriver` presses the offline entry when the network is missing, and `unity-typecheck/Shims.cs` mirrors the manager for the offline typecheck (the UGS scripts stay out of that project). Checked offline: `bob-check` prints `BOB-CHECK PASSED` and `unity-check` prints `UNITY-CHECK PASSED`; no Unity screenshot of the new S02 yet.
- **Progression pass: every round pays, stars gate the ladder (16 Nov 2026, docs 24 sections 15.1 and 15.9)**: a story chapter used to pay only its third round and still needed 78 clears (~7.4 h) to reach chapter 6. Now `StorySeries` keeps `TotalLocalTasks/RivalTasks/TotalLocalPoints/RivalPoints` and `TaskRaceModeManager.EndMatch` rewards the whole chapter (a single PvP round keeps its own totals), plus `BobTaskRaceProfile.FirstClearBonus = 100` XP for the first clear of each chapter (`BobChapters.Cleared`/`MarkCleared`, key `bob.story.cleared.<order>`, so a replay pays the normal rate). A chapter that cleaned nothing pays 0 coins/XP, so three idle rounds are not a faucet. The ladder now unlocks at LV 1 / 2 / 3 / 4 / 5 / 6 **and** requires `BobChapters.StarsToOpen = 2` stars of the previous chapter; `LevelFromXp` is clamped at `MaxLevel = 50`; under this policy the full six-chapter story is 9 clears (~51 min) of honest play. The series verdict is also fixed: `LocalTookSeries` now demands two round wins, so 1-0 with two draws is no longer a chapter win and the stars stay 0. The chapter card's next-chapter line names the star gate when the level is already met, S13's lock veil/tag/toast say which of the two gates blocks the card, and Home/S13 show `bob.taskRace.best` (the real match best) instead of the legacy band-keyed lobby score. Chips read `D{n} · 3 รอบ · ดีสุด X`. Checked offline: `unity-check` adds 8 series assertions (verdict, stars, totals, reset, three-task margin) and prints `UNITY-CHECK PASSED`; `bob-check` stays green (modules sync, task-race 39/39, LINE, typecheck 40 known kinds).
- **Solo / Duo / Team are playable offline (16 Nov 2026, docs 11 sections 3.2 and 4, docs 24 section 15.22)**: the Duo and Team scenes were baked and listed in Build Settings since 14 Nov but nothing loaded them, and S11 was reachable only from the dev tour. Home now carries a `โหมด · Solo / Duo / Team` button, S11 hands Story to S13 and Solo/Duo/Team to a new S14 team screen (your slots, four hero cards, a 2v2 / 3v3 toggle, and a summary that reads the same task table the round uses), and `MatchRequest` (static, because `LoadScene` clears scene state) carries mode, players per side, series flag and helpers into `TaskRaceModeManager.Awake` before the chapter rules apply. Team slots are offline teammate bots (`TaskRace/BobTaskRaceTeammates.cs`): same hero prefabs, slower pace (2.2 vs the rival's 1.12), no raiding, and they share the team task list, so points and `DoneCountOf` work. Duo/Team run one round (`Series = false`), so they never write stars or first-clear. When a scene is played directly in the Editor there is no request, so it keeps its baked values. Checked with `unity-typecheck` and `bob-check` only — no Unity screenshots yet.
- **Online room for 2–4 players (16 Nov 2026, docs 27 section 10, docs 24 section 15.23)**: S14 now offers `ออนไลน์ 2–4 คน`, which opens a new S15 room. The host allocates Relay and creates a Lobby whose 6-character code is shown in giant type (typed on a phone through `TouchScreenKeyboard`), and only the host can start; `bob.start` carries the host's seed, so every device draws the same task list (`TaskRaceRound.LockSeed` keeps `Start()` from rerolling it). Poses travel 10 times a second on the unreliable channel and task completions on the reliable one; both carry the sender's net id and the host rebroadcasts them, so three or four players each get their own body and a client's cleaned station reaches everyone through one authority. Remote heroes reuse the offline teammate avatar; there are no prefab spawns, no NetworkObjects and no scene changes. The round is co-op on the host's island against the pace rival — no two-team PvP, no server-scored points, no reconnect or late join, and the result card hides `เล่นอีกครั้ง` while online. Checked with `unity-typecheck`, `bob-check` and `unity-check`; no Unity screenshots or two-machine run yet.
- **Merged with origin/main (15 Nov 2026, commit 110024a + fix)**: the parallel session's "see it in half a second" pass is now in: the HUD clock is 48 px on a 940 x 124 bar placed clear of the task card (the two cream panels used to overlap), the right column reads `คะแนนทีม N`, `คู่แข่ง M · เสร็จ k/N` and `ทีมเรา P%`, task rows use category icons (`icon_skill_*`, handed to the HUD through `BindArt` because the Task Race scene has no `BobGame` to set `BobUi.Art`), the next unfinished job is highlighted, done rows are ticked and struck through, and the S28 card opens with a two-second `ลากจอยเพื่อปิดการ์ด` chip floating over the dimmed screen above the joystick (its own container, so the dark plate no longer draws over its text and the play area stays clear). The P0 behaviour stays on top (rival task count in the same line, advice footer, debug bar editor-only, result overlay and payout). The lobby art (four backdrops, five shop cut-outs) also comes from that commit, re-rendered in the same toon island style. The merge had to restore `MiniPointer`, `IsHero` and `miniDrags` in `BobGame.Tasks.cs`: commit 110024a deleted those helpers while the new card code still called them, so `origin/main` did not compile on its own.
