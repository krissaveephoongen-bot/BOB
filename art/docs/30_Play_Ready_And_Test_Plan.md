# 30 · ตรวจทั้งระบบ + แผนทดสอบเต็มระบบ (16 พ.ย. 2026)

สถานะ: **ตรวจครบ 1 รอบ · เจอบล็อกเกอร์ "เกมเล่นไม่ได้" 1 ตัวและแก้แล้ว · เพิ่ม gate ใหม่ 1 ตัว (`ready-check`)**
ยังไม่มีการรันใน Unity เลยในรอบนี้ เพราะ sandbox ไม่มี Unity — ข้อ 6 เขียนตรง ๆ ว่าเหลืออะไรให้พิสูจน์บนเครื่อง owner
**อัปเดต 17 พ.ย. 2026: รันบน editor จริงแล้ว** — คอมไพล์ 0 error, check ชั้น 2 ผ่านครบ + เจอบล็อกเกอร์คอมไพล์ออนไลน์เพิ่ม 1 ตัว (แก้แล้ว, §2.1) และได้ภาพ 2400×1080 ครบชุด (§6.1)

---

## 0. สรุปใน 6 บรรทัด

1. **เจอบล็อกเกอร์จริง:** ซีน Task Race ทั้งสามไม่มี `EventSystem` และไม่มีโค้ดสร้างให้ → build ที่เปิดซีนนี้ตรง ๆ จะ**แตะอะไรไม่ติดเลย** (HUD, ปุ่มพัก, วงล้อ ping, การ์ดมินิเกม)
2. **แก้แล้ว** ที่ `TaskRaceModeManager.Awake` → `EnsureEventSystem()` สร้าง `EventSystem` + `StandaloneInputModule` ให้เองถ้ายังไม่มี และ `DontDestroyOnLoad` ตอนเล่นจริง
3. **เพิ่ม gate `ready-check`** ที่จับ 11 ชนิดความพังเชิง "เล่นไม่ได้" ซึ่ง typecheck มองไม่เห็น (ซีนหาย/GUID ไม่ตรง, `LoadScene` ชื่อผิด, Canvas ไม่มี EventSystem, Resources หาย, `Resources.Load` ชี้ที่ว่าง, script GUID ตาย, **แอสเซตที่ซีน/prefab อ้างถูกอัปโหลด GUID ตาย**, GUID ซ้ำ, PlayerSettings ผิด, ชื่อ sprite/เสียงไม่มีจริง, **อักขระที่ฟอนต์ไม่มี glyph**) — ทดสอบย้อนกลับครบทุกข้อ
4. **ของที่ตรวจแล้วครบ:** 4 ซีนใน Build Settings, `Resources/Chapters` 6, `Resources/Stages` 6, audio prefab (38 คลิป), ฟอนต์ไทย 2 ตัว, ชื่อ sprite 18 ตัว, ชื่อเสียง 38 ตัว + tone สำรอง `ping_pop`
5. **ที่ยังค้างและไม่ใช่โค้ด:** ยังไม่มีภาพ/หลักฐานจาก Unity (ต้องรันบนเครื่อง owner), build entry `TaskRace_Duo/Team` เป็นซีนที่เข้าไม่ถึง, `server/coop-test|mp-test|mp-bots-queue` ยังยิงพอร์ต Nakama 7350 ที่ถูกถอดไปแล้ว, LFS อัปโหลดไม่ได้ (HTTP 501) เหลือไฟล์ธรรมดา 301 ไฟล์
6. **แผนทดสอบ 4 ชั้น:** ออฟไลน์ (4 คำสั่ง) → editor checks (10 ตัว) → มือถือจริง (S01→S16) → ออนไลน์ UGS พร้อมเกณฑ์ผ่านของแต่ละชั้น (ข้อ 4–5)

---

## 1. ผลตรวจ (สิ่งที่อ่านจากไฟล์จริง ไม่ได้เดา)

| ตรวจ | ผล | หมายเหตุ |
| --- | --- | --- |
| Unity / แพ็กเกจ | ✅ | 6000.6.8f1 · uGUI 2.6.0 · TMP · **legacy input** (`activeInputHandler: 0`, ไม่มี `com.unity.inputsystem`) — ตรงกับ `Input.GetAxis` ที่โค้ดใช้ |
| Build Settings | ✅ 4 ซีน | `Game`, `TaskRace`, `TaskRace_Duo`, `TaskRace_Team`; path + GUID ตรงกับ `.meta` ทุกตัว |
| `LoadScene` ในโค้ด | ⚠️ ใช้ 2 ชื่อ | มีแค่ `"Game"` กับ `"TaskRace"` → **Duo/Team เป็น build entry ที่เข้าไม่ถึง** (โหมด/จำนวนผู้เล่นส่งผ่าน static `MatchRequest`) |
| Canvas ↔ EventSystem | ❌→✅ | Task Race ไม่มี EventSystem ในซีน และไม่มีตัวสร้าง (แก้แล้ว ข้อ 2); `Game` มีของตัวเองอยู่แล้ว |
| `Resources/Chapters` | ✅ 6 | order 1–6, มี `.meta` ครบ |
| `Resources/Stages` | ✅ 6 | stage asset ครบ 6 บท |
| `Resources/Audio/BobAudio.prefab` | ✅ 38 คลิป | `BobSfx` หา clip ตามชื่อจาก array นี้ |
| ฟอนต์ TMP | ✅ | `IBMPlexSansThai-Medium SDF` + `Mitr-Medium SDF` |
| ชื่อ sprite ที่โค้ดขอ (`BobUi.Art`) | ✅ 18/18 | อยู่ใน `bob-art-manifest.json`; ชื่อที่ขาดจะกลายเป็นสี่เหลี่ยมขาว ไม่ throw |
| ชื่อเสียงที่โค้ดขอ | ✅ 38/38 + 1 | `ping_pop` ไม่มีไฟล์ → `BobTone.PingPop()` สังเคราะห์ให้ตอนรัน (ไฟล์ `.wav` จริงชนะเองถ้าใส่ทีหลัง) |
| Theme asset | ⚠️ ไม่มี asset | `BobUiTheme.Current` ตกลงไปใช้ค่าสีในโค้ด — ไม่บล็อกการเล่น |
| `server/task-race-test.js` | ✅ 80/80 | 75 สัญญาเดิม + 5 ข้อของคะแนนรวม (`taskRaceScore`) รวมกับ LINE auth ใน `bob-check` |
| `server/daily-test.js` | ✅ 34/34 | เดลี่: ตาราง seed 6 แถว + เขตเที่ยงคืน UTC+7 + สำเนาใน prototype + **HTTP จริง** (200 / 409 / 404 / 405, `no-store`) |
| `server/e2e-taskrace-test.js` | ✅ 39/39 | เล่นทั้งรอบ Task Race จริง D1–D3: เปิด/ปิดการ์ดทุกใบ ทุกขั้น, การ์ดรถเข็น 4 ชิ้น, คราบฉุกเฉิน, RESULT, แถว analytics |
| `server/coop-test.js`, `mp-test.js`, `mp-bots-queue.js` | ❌ ตาย | `ECONNREFUSED 127.0.0.1:7350` — harness ยุค Nakama ที่ถอดออกตอนย้าย pure-UGS; **ไม่ใช่ regression ของรอบนี้** |
| Git LFS | ⚠️ | 0 pointer / 301 ไฟล์ไบนารีธรรมดาใต้ `unity/Assets` (ยังใช้ได้ แค่เตือนตอน checkout); อัปโหลด LFS ตอบ 501 ด้วย token ของ session นี้ |
| หลักฐาน Play mode | ❌ ไม่มี | sandbox ไม่มี Unity — ข้อ 6 |

---

## 2. บล็อกเกอร์ P0 ที่แก้ในรอบนี้

**อาการ:** ในซีน `TaskRace*` มี `Canvas` แต่ไม่มี `EventSystem` และไม่มีสคริปต์ไหนสร้างให้. ในเอดิเตอร์อาจไม่รู้ตัวถ้าเข้ามาจากซีน `Game` เพราะ EventSystem ตัวนั้นถูก `DontDestroyOnLoad` ค้างอยู่ แต่ **build ที่เข้าซีน Task Race ตรง ๆ (หรือหลังซีนเก่าถูกทำลาย) จะเงียบทั้งจอ** — กดปุ่มพักไม่ติด ลากวงล้อ ping ไม่หมุน เปิดการ์ดมินิเกมไม่ได้

**แก้:** `unity/Assets/Scripts/TaskRace/TaskRaceModeManager.cs`

```csharp
void Awake()
{
    cached = this;
    EnsureEventSystem();      // ← ใหม่: ก่อน ApplyMatchRequest
    ApplyMatchRequest();
    ...
}

static void EnsureEventSystem()
{
    if (UnityEngine.EventSystems.EventSystem.current != null) return;
    if (UnityEngine.Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
    var go = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem),
        typeof(UnityEngine.EventSystems.StandaloneInputModule));
    if (Application.isPlaying) UnityEngine.Object.DontDestroyOnLoad(go);
}
```

ใช้ `StandaloneInputModule` ให้ตรงกับ legacy input ของโปรเจกต์ (ถ้าเปลี่ยนไป Input System ทีหลัง ต้องเปลี่ยนเป็น `InputSystemUIInputModule` พร้อมกัน)

**gate ใหม่ `ready-check/`** — `python3 ready-check/check.py` หรือ `bash ready-check/check.sh`, exit 0/1/2, พิมพ์ `READY-CHECK PASSED` ตอนผ่าน. รอบล่าสุด:

```
scenes: Game, TaskRace, TaskRace_Duo, TaskRace_Team
art: 18 sprite name(s) checked, audio: 38 clip(s), tone fallback: ping_pop
loads: 22 Resources.Load path(s) · scripts: 35 guid(s) (12 from packages) · metas: 944 (0 duplicate(s))
settings: com.bob.battleofthebrooms · 0.1.0 (2) · minSdk 26 · landscape · legacy input
note: Resources.Load("BobUiTheme") is absent by design - optional: ...
note: LFS: 0 pointer(s), 301 plain binary file(s) under unity/Assets
READY-CHECK PASSED
```

ตรวจ 11 ข้อ (ทุกข้อทดสอบย้อนกลับแล้วว่าจับได้จริง):

| # | ตรวจ | ถ้าพังจะเห็นอะไร |
| --- | --- | --- |
| 1 | ซีนใน Build Settings มีจริง + GUID ตรง `.meta` | เปิดเกมแล้วดำ/โหลดซีนไม่ได้ |
| 2 | `LoadScene("...")` / `CanStreamedLevelBeLoaded` ชี้ซีนที่มีใน Build Settings | ปุ่มพาไปหน้าว่าง |
| 3 | Canvas ทุกซีนมี EventSystem **หรือ** มีตัวสร้างของซีนนั้น (TaskRace ↔ `TaskRaceModeManager`, Game ↔ `BobGame.World`) | แตะไม่ติดทั้งจอ |
| 4 | `Resources/Chapters` 6 · `Stages` 6 · audio prefab · ฟอนต์ 2 | หน้าจอว่าง/ฟอนต์ไทยเป็นกล่อง |
| 5 | **ทุก `Resources.Load("...")` ที่เป็นข้อความเต็ม** มีไฟล์จริง (`BobUiTheme` ยกเว้นพร้อมเหตุผลในโค้ด) | ได้ `null` เงียบ ๆ |
| 6 | **script GUID ในทุกซีน/prefab** มี `.cs` จริง + ไม่มี `m_Script: {fileID: 0}` (baseline ของสคริปต์ที่มาจากแพ็กเกจอยู่ใน `ready-check/external-scripts.txt`) | `Missing (Mono Script)` ทั้งคอมโพเนนต์ |
| 7 | **GUID ซ้ำ** ใน `.meta` | แอสเซตสลับกันเองแบบเงียบ |
| 8 | **PlayerSettings ตอนส่ง**: package id, versionCode ≥ 1, minSdk ≥ 26, แนวนอนเท่านั้น, `activeInputHandler: 0` ให้ตรงกับ `StandaloneInputModule` | APK ติดตั้งได้แต่เล่นไม่ได้/จอเบี้ยว |
| 9 | ชื่อ sprite (manifest) และชื่อเสียง (audio prefab + tone) ที่โค้ดขอ | สี่เหลี่ยมขาว/ไม่มีเสียง |
| 10 | **GUID ของแอสเซตที่ซีน/prefab/`.mat`/`.anim` อ้าง** ยังมีไฟล์จริง (7,443 จุด; GUID ที่มาจากแพ็กเกจ/บิลต์อินอยู่ใน `ready-check/external-assets.txt` 19 ตัว) + `.meta` กำพร้าเป็น note | วัตถุหายเงียบ ๆ ตอนโหลด |
| 11 | **อักขระในข้อความ UI มี glyph ในฟอนต์** (ชุด glyph อ่านจาก `BobTmpSetup`; ข้าม `Debug.Log`, คอมเมนต์, `[Header]`/`[Tooltip]`) | ขึ้นเป็นกล่องสี่เหลี่ยม |

**พิสูจน์ว่า gate จับได้จริง (ทดสอบย้อนกลับ):** ใส่ของเสียชั่วคราวทีละอย่างแล้วรัน gate → แดงตรงเป้าทุกครั้ง แล้วคืนค่ากลับ → เขียว
- เปลี่ยนชื่อ `EnsureEventSystem` → แดง 3 บรรทัด: `TaskRace has a Canvas, no EventSystem in the scene, and TaskRaceModeManager.cs does not build one` (×3 ซีน)
- เพิ่ม `Resources.Load("Props/SM_does_not_exist")` → `Resources.Load("Props/SM_does_not_exist") resolves to nothing under Assets/Resources`
- เปลี่ยน script GUID ใน `BobAudio.prefab` → `unresolved script GUID 0123…: no .cs in Assets and not in ready-check/external-scripts.txt`
- วาง `.meta` ที่ใช้ GUID ซ้ำกับ `Stage1_RentalRoom.asset` → `duplicate GUID 9d08…`
- ตั้ง `AndroidMinSdkVersion: 21` → `AndroidMinSdkVersion is 21, expected 26 or newer`
- เปลี่ยน GUID ของสไปรต์ใน `BobAudio.prefab` เป็น `deadbeef…` → `references missing asset GUID deadbeef…`
- ใส่ literal `"⏭ 65 วิ"` → `renders '⏭' (U+23ED) but the TMP font set has no such glyph`

ทั้งสองข้อใหม่**จับของจริงได้ทันที**: `⏭` บนปุ่มดีบัก `AddDebugButton(... "⏭ 65 วิ")` ถูกเปลี่ยนเป็น `→ 65 วิ` (ชุด glyph มี `→`) — ตัวอื่นที่เจอ (`§` ใน `[Header]`/`[Tooltip]`) เป็นข้อความInspector ไม่ได้วาดบนจอ จึงข้ามอย่างถูกต้อง

รอบแรกของ gate ผ่านทั้งที่ยังไม่มี EventSystem เพราะยอมรับ "มีตัวสร้างอยู่ในไฟล์ใดก็ได้" — แก้ให้ผูกซีนกับเจ้าของของมันแล้วจึงจับได้จริง

### 2.1 บล็อกเกอร์คอมไพล์ออนไลน์ CS0117 ที่ typecheck ออฟไลน์มองไม่เห็น (17 พ.ย. 2026)

รอบที่เปิด Unity จริงครั้งแรก (หลังชุดงาน cloud session) คอมไพล์ล้ม **ทั้งโปรเจกต์** ใน
`unity/Assets/Scripts/UGS/BobOnlineRoom.cs` — 3 จุด ชนิดเดียวกัน:

```
BobOnlineRoom.cs(128,21): error CS0117: 'CreateLobbyOptions' does not contain a definition for 'PlayerData'
BobOnlineRoom.cs(173,..): error CS0117: 'QuickJoinLobbyOptions' does not contain a definition for 'PlayerData'
BobOnlineRoom.cs(175,..): error CS0117: 'JoinLobbyByCodeOptions' does not contain a definition for 'PlayerData'
```

**สาเหตุ:** โค้ดเขียน `options.PlayerData = …` แต่ Lobby SDK **1.3.0** ที่ติดตั้งจริง
(`Library/PackageCache/com.unity.services.lobby@743c1c4a1740`) ให้ property `Player` (ชนิด
`Unity.Services.Lobbies.Models.Player` ที่มี `Dictionary<string, PlayerDataObject> Data`) บน option class ทั้งสามตัว
ไม่มี `PlayerData` ตรง ๆ

**แก้:** ห่อเป็น `Player` 3 จุด โดยไม่แตะ helper เดิม:

```csharp
IsPrivate = false, Player = new Player { Data = PlayerData(pendingHero) },
...
new QuickJoinLobbyOptions { Player = new Player { Data = PlayerData(pendingHero) } }
new JoinLobbyByCodeOptions { Player = new Player { Data = PlayerData(pendingHero) } }
```

**ทำไมรอบก่อนจับไม่ได้:** `unity-typecheck/BobTypeCheck.csproj` ตัด `Assets/Scripts/UGS/**/*.cs` และ
`Editor/UgsCheck.cs` ออกจาก typecheck ออฟไลน์ (ไม่มี stub ของ UGS SDK) — "typecheck ok" จึงไม่เคยคอมไพล์ไฟล์นี้.
หลังแก้: คอมไพล์จริง `0 error CS` และ check ที่รันต่อในรอบเดียวกันผ่านหมด — ดู §6.1

---

## 3. list งาน

### 3.1 ทำแล้วในรอบนี้

| # | งาน | หลักฐาน |
| --- | --- | --- |
| 1 | `EnsureEventSystem` ใน Task Race (P0) | typecheck เขียว · `ready-check` เขียว |
| 2 | gate `ready-check/check.py` + `check.sh` | รันแล้ว PASSED |
| 3 | เอกสารนี้ + แถวใน `COORDINATION.md` + bullet ใน `README` | ไฟล์ใน commit เดียวกัน |

### 3.2 ต้องรันใน Unity ก่อนบอกว่า "พร้อมเล่น" (ทำได้ทันทีที่มีเครื่อง)

| # | งาน | คำสั่ง |
| --- | --- | --- |
| 4 | E2E ทั้งเส้น (S02 → Home → S11 → S13 → S14 → TaskRace → ผล) | `Bob/Run E2E` |
| 5 | บอทเล่นแมตช์เต็มในซีน Game ✅ 17 พ.ย. (`GAME TEST PASSED: 2:0 in 2 rounds`) | `Bob/Run Game Test` |
| 6 | บอทเก็บงาน + ข้ามสะพาน + sabotage ใน Task Race ✅ 17 พ.ย. (`BOT CHECK PASSED`) | `Bob/Run Bot Check` |
| 7 | อนุกรม 3 ยก + ดาว 1–3 + XP เคลียร์ครั้งแรก ✅ 17 พ.ย. (`rules PASSED`) | `Bob/Run Story Series Check` |
| 8 | การ์ดผลบท + ปุ่มบทถัดไป ✅ 17 พ.ย. (`CHAPTER RESULT CHECK PASSED`) | `Bob/Run Chapter Result Check` |
| 9 | พักเกม: นาฬิกาหยุดจริงแล้วเดินต่อ ✅ 17 พ.ย. (`PAUSE CHECK PASSED`) | `Bob/Run Pause Check` |
| 10 | คานต์ดาวน์ 3-2-1-ลุย! ✅ 17 พ.ย. (2.7 วิ ตรงสเปก) | `Bob/Run Countdown Check` |
| 11 | บันไดบท (LV 2–6, ดาว ≥ 2) ✅ 17 พ.ย. (`6 chapters, ladder ok`) | `Bob/Chapters/Check Ladder` |
| 12 | ภาพ/เส้นทาง NavMesh ทั้ง 3 ขนาดเกาะ ✅ 17 พ.ย. (`done` ทั้ง solo/duo/team) | `TaskRaceVerify.Run / RunDuo / RunTeam` |
| 13 | UGS: anonymous → Relay → Lobby | `Bob/Run UGS Check` |
| 14 | build APK + ติดตั้งบนเครื่องจริง | `Bob/Android/Build APK` |

### 3.3 งานฟีเจอร์ที่ยังไม่ทำ (ไม่บล็อกการเล่น 3 บทแรก)

T16 แถวฉุกเฉิน + คราบบอส · T12 ภาพพอร์เทรต/ช่องเส้นประ · S25 ก่อน/หลัง · แท็บตั้งค่า + โหมดกดค้าง · แชท/พร้อม/S16 เลือกฮีโร่ออนไลน์ · PvP สองทีมออนไลน์ · music layering (ยังไม่มี stem)

### 3.4 งานบ้าน (ทำเมื่อ owner อนุญาต)

ลบหรือเปลี่ยนชื่อ build entry `TaskRace_Duo/Team` (หรือผูกให้ S14 เลือกซีนจริง) · ปลด/เก็บ `coop-test`/`mp-test` ยุค Nakama · แก้ LFS ให้อัปโหลดได้ (ต้องมีสิทธิ์เขียน LFS ของ repo)

---

## 4. แผนทดสอบเต็มระบบ (4 ชั้น)

### ชั้น 1 — ออฟไลน์ ไม่ต้องมี Unity (รันได้ทุก commit)

| คำสั่ง | พิสูจน์ | ผ่านเมื่อ |
| --- | --- | --- |
| `bash unity-typecheck/check.sh` | ทุกสคริปต์ compile เทียบ baseline | `typecheck ok: 43 known Unity-6 gap kind(s), no new errors` |
| `cd unity-check && dotnet run` | กติกาล้วน: กอง 5 ชั้น, ตารางฉุกเฉิน, series/stars, ตารางคะแนนรวม 10 แถว, แถบอันดับ, seed เดลี่, ตารางของตกแต่ง 13 ข้อ, ห้องออฟไลน์, **ออนไลน์ best-of-3 11 ข้อ (ผลรอบ/โมฆะ/ผู้ชนะ/บรรทัดเก่า)** | `UNITY-CHECK PASSED` |
| `node server/e2e-taskrace-test.js` | **เล่นทั้งรอบ**ผ่าน module จริง: lobby → race → การ์ดทุกใบ/ทุกขั้น → T04 4 ชิ้น → คราบ 3 ครั้ง → RESULT + analytics | `E2E PASSED: 39/39 checks` |
| `/usr/local/bin/bob-check` | module server สด + `task-race-test` + `daily-test` + LINE auth + typecheck | `BOB-CHECK PASSED` |
| `bash ready-check/check.sh` | **เล่นได้จริงใน build**: ซีน/GUID/EventSystem/Resources/ชื่อ asset | `READY-CHECK PASSED` |

เกณฑ์รวม: ทั้ง 4 ต้องเขียวก่อน merge ทุกครั้ง. ถ้าตัวใดแดง = หยุด ไม่ใช่ "ข้ามไปก่อน"

**กับดัก typecheck (17 พ.ย. 2026):** `baseline-errors.txt` เป็น LF แต่บนเครื่อง Windows ที่ `core.autocrlf` เปิด
ไฟล์จะถูก checkout เป็น CRLF ทำให้ `comm` เทียบไม่ตรงและขึ้น "new error" ทั้งกระดาน (ทั้งที่ไม่มีอะไรเปลี่ยน) —
ถ้าเกิดจาก CRLF ให้แปลงเฉพาะ line ending เป็น LF โดยไม่เปลี่ยนรายการ baseline; ใช้ `--update` เฉพาะเมื่อทบทวน
ทุก gap ใหม่และยืนยันกับ Unity จริงแล้วว่าเป็น API ที่ shim ไม่รองรับ เช่น `Editor/TaskRaceShots.cs` → 40 → 43 kind
ห้ามรับ baseline ใหม่เพียงเพื่อให้ gate เขียว

**ตรวจหลังรวม cloud ถึง `ecb5796` (17 พ.ย. 2026):** typecheck 43, ready-check, task-race 80/80, daily 34/34,
e2e 39/39 และ LINE auth ผ่าน. `unity-check` compile target net8.0 ผ่านหลัง restore จาก NuGet ทางการ;
เครื่องนี้มี runtime .NET 10/11 แต่ไม่มี 8 จึงรันด้วย `DOTNET_ROLL_FORWARD=Major` บน .NET 10 ได้ `UNITY-CHECK PASSED`
(ไม่ใช่หลักฐาน runtime .NET 8). log `unity/Logs/verify-merge-{gates,contracts,rules-net10}.log`;
`/usr/local/bin/bob-check` ไม่มีบนเครื่อง Windows นี้ จึงยังไม่ได้รัน wrapper/live-module ซ้ำในรอบนี้.
Unity จริงหลังรวมผ่าน `SHOTS PASSED` (`verify-shots8-merged.log`: ระหว่าง toast ลูกศร False,
หลัง toast True ที่ `(244,-330)`) และ `GAME TEST PASSED: 2:0 in 2 rounds` (`verify-gametour-merged.log`),
ไม่มี `error CS`; ถ่ายหน้า Home/เมนูและสนามใหม่ที่ 2400×1080. ทั้งสอง log มีข้อความโหลด assembly ของ
`Unity.AI.Navigation.Editor.ConversionSystem` ไม่สำเร็จ แต่ editor ทำงานและไดรเวอร์จบ exit 0;
ยังไม่ใช่การตรวจออนไลน์สองเครื่องหรือ APK.

**หมายเหตุ 17 พ.ย. 2026 (การ์ด T04 อ่านน้อยลง):** การเปลี่ยนรอบนี้เป็น UI/ปฏิสัมพันธ์ใน `BobMinigameHost`
(4 ช่อง, แถบสเกลเพดาน, แถวดาว, ป้าย `+N`/`-N`, มิเตอร์งบ, ทุ่มวิธี, บัฟครบสี่) — ชั้น 1 ยืนยันได้แค่ "ยังคอมไพล์/สัญญาไม่เพี้ยน"
(`bob-check`, `e2e` 39/39, `ready-check`) ส่วน *การมองเห็นจริง* ต้องรอชั้น 2/3 เพราะ sandbox ไม่มี Unity. กติกาที่บังคับในโค้ด
(`CartWhyCannot`: 4 ช่อง/วิธี ≤ 2/งบ 100) และค่าเพดานถูกยืนยันโดย `unity-check` + `task-race-test` 75/75 อยู่แล้ว

### ชั้น 2 — Editor checks (เปิด Unity 6000.6.8f1 ที่โฟลเดอร์ `unity/`)

รันตามลำดับ 11→4→5→6→7→8→9→10→12 (ข้อ 3.2). ทุกตัวพิมพ์บรรทัด `... CHECK PASSED` ใน Console และหลายตัวมีภาพใน `unity/Logs/`. ตัวที่ใช้ Play mode (`E2E`, `Game Test`, `Bot Check`, `Story Series`, `Pause`, `Countdown`, `UGS`) **ห้ามใช้ `-batchmode -quit`** เพราะต้องมี Game view; รันจากเมนูหรือ `-executeMethod` แบบไม่ batchmode ตามคอมเมนต์หัวไฟล์. `TaskRaceVerify` เป็น edit-mode ล้วน รัน batchmode ได้

### ชั้น 3 — เครื่องจริง (Android, แนวนอน)

เดินตามลำดับนี้บนเครื่องจริง 1 เครื่อง (จดผลทีละข้อ):

1. **S01 Splash** → เข้า S02 เองภายใน ~2 วิ
2. **S02 Login**: กด `เล่นเลย` → ขึ้น Home (ถ้าไม่มีเน็ต ต้องเห็น `เล่นแบบออฟไลน์` และเข้า Home ได้)
3. **Home**: เหรียญ/XP/เลเวลตรงกับที่เล่นครั้งก่อน (ปิดแอปแล้วเปิดใหม่ต้องคงอยู่ = PlayerPrefs รอด)
4. **S11 โหมด**: การ์ด Task Race กดได้, การ์ดที่ยังไม่เปิดบอกเหตุผล
5. **S13 บท**: บท 1 ปลด, บท 2–6 ล็อกพร้อมข้อความ `ต้องมีดาวบทก่อน …` / LV; ปุ่มเล่นซ้ำบทเก่าได้
6. **S14 ทีม**: เลือกฮีโร่แล้วเข้าเกมได้
7. **TaskRace**: คานต์ดาวน์ 3-2-1 → นาฬิกาเดิน · จอยลากได้ · แตะพื้นเดินได้ · วงล้อ ping หมุนได้และมีเสียง `ping_pop` · ปุ่มพักหยุดนาฬิกาจริง · ปุ่มบ้านกลับจุดเกิด
8. **การ์ดมินิเกม**: เปิด/ปิดด้วย × และด้วยการเดินออก (เกิน 70% แล้วปิด = งานรีเซ็ต) · ทำครบแล้วขึ้นเครื่องหมายเสร็จ
9. **จบ 3 ยก**: ได้ดาวตามกติกา (ชนะ 2 ยก = ผ่าน; 1–0 + เสมอ 2 รอบ = ไม่นับ), XP เข้ากระเป๋า, บทถัดไปปลดจริง
10. **ทนทาน**: กด Home ของเครื่องกลางแมตช์แล้วกลับเข้า → ไม่ค้าง/ไม่รีเซ็ตเงิน · หมุนจอ (ล็อกแนวนอนอยู่) · เครื่องช้า (RAM 3 GB) เฟรมไม่ตกจนเล่นไม่ได้
11. **รีเซ็ตข้อมูล**: ล้างข้อมูลแอป → กลับไป S02, ไม่มีของค้างจากโปรไฟล์เก่า
12. **เสียง**: SFX ครบ, ping pop ดังเฉพาะตอนกด ping (ไม่ดังตอนเปิดวงล้อ)

### ชั้น 4 — ออนไลน์ (UGS)

`Bob/Run UGS Check` พิสูจน์ anonymous sign-in → Relay allocation → create/delete Lobby. ที่ยัง**ไม่ผูก**กับ UI เนื้อเรื่อง: cloud save, leaderboard, S16 เลือกฮีโร่, แชท/พร้อม, PvP. ชั้นนี้จึงผ่านได้แค่ระดับ "stack ทำงาน" ไม่ใช่ "ผู้เล่นสองคนเล่นด้วยกันได้" — ข้อนั้นต้องรอข้อ 3.3

---

## 5. เกณฑ์ "พร้อมเล่น" (Definition of Done)

- [ ] ชั้น 1 เขียวทั้ง 4 คำสั่ง บน commit ที่จะแจก
- [ ] ชั้น 2 ผ่านครบ 10 ตัว (มี log/ภาพเป็นหลักฐาน)
- [ ] ชั้น 3 ครบ 12 ข้อบนเครื่องจริง 1 เครื่อง ไม่มีข้อใด "ข้าม"
- [ ] บท 1→3 เล่นจบได้ด้วยมือเปล่า (ไม่ต้องใช้ดีบัก/ปุ่มลัด)
- [ ] ปิดแอปกลางทางแล้วเปิดใหม่ เงิน/XP/ดาวไม่หายและไม่ทวี
- [ ] ไม่มี error แดงใน Console ตอนเล่นเส้นหลัก (warning ที่รู้อยู่แล้วไม่นับ)
- [ ] APK ติดตั้งจากไฟล์เดียวได้บนเครื่องที่ไม่มี Unity
- [ ] เอกสาร 29/30 ตรงกับสิ่งที่เกมพูดจริง (คำขวัญ, ชื่อบท, เงื่อนไขปลด)

---

## 6. สิ่งที่ sandbox นี้พิสูจน์ไม่ได้ (เขียนตรง ๆ)

- **ไม่มี Unity ใน sandbox** → ไม่มีภาพหน้าจอ, ไม่มี Play mode, ไม่มี APK จากรอบนี้. ทุกอย่างที่เขียนว่า "แก้แล้ว" ในเอกสารนี้ยืนยันด้วย typecheck + ready-check (อ่านไฟล์จริง) เท่านั้น
- **ไม่มีเซิร์ฟเวอร์ Nakama** → `coop-test`/`mp-test` แดงเพราะพอร์ต 7350 ไม่มีคนฟัง ไม่ใช่โค้ดใหม่พัง
- **LFS อัปโหลดไม่ได้** (501) → ไฟล์ไบนารีใหม่ต้องเป็นไฟล์ธรรมดา หรือรอสิทธิ์ LFS
- ภาพ/ตัวเลขประสิทธิภาพบนมือถือต้องมาจากเครื่องจริงเท่านั้น

### 6.1 รอบที่พิสูจน์บน editor จริง (17 พ.ย. 2026)

sandbox ของ cloud session ไม่มี Unity — รอบนี้รันบนเครื่อง owner (editor ว่าง), `-projectPath unity/` ตรง ๆ:

| ชั้น | งาน | ผล |
| --- | --- | --- |
| คอมไพล์ | คอมไพล์จริงทั้งโปรเจกต์ | รอบแรก **ล้ม CS0117 ×3** (UGS, §2.1) → แก้แล้ว **0 error CS** |
| 2 | `BobChapterCheck` (§3.2 ข้อ 11) | `6 chapters, ladder ok (chapter 1 at level 1)` |
| 2 | `ChapterResultCheck` (ข้อ 8) | `CHAPTER RESULT CHECK PASSED` |
| 2 | `StorySeriesCheck` (ข้อ 7) | `series 1-2 lost: ok` · `rules PASSED` |
| 2 | `RaceCountdownCheck` (ข้อ 10) | 2.7 วิ (ตรงสเปก) · `COUNTDOWN CHECK PASSED` |
| 2 | `TaskRaceVerify.Run/RunDuo/RunTeam` (ข้อ 12) | `done` ทั้งสาม · solo 6 แถว 0/9, duo/team 7 แถว 0/13 · การ์ดบท 1 ดาว 3/3 · S28 9 การ์ด fx 6/6 |
| 2 | `BotCheck` (ข้อ 6, play mode) | บอท 20.90 u · เก็บ/ข้าม/ป่วน true · `BOT CHECK PASSED` |
| 2 | `PauseCheck` (ข้อ 9, play mode) | timeScale 0.00 · clock แช่/เดินต่อ · `PAUSE CHECK PASSED` |
| 2 | `BobGameScene.RunTest` (ข้อ 5, play mode) | `GAME TEST PASSED: 2:0 in 2 rounds` ที่ 2400×1080 |
| 1 | `ready-check` | `READY-CHECK PASSED` (พื้นเดียวกับ §2) |
| 1 | `server/task-race-test.js` (หลัง merge คะแนนรวม) | 80/80 |
| ใหม่ | `Editor/TaskRaceShots.cs` (editor-only) | 7 ภาพ runtime-only: ถาดเพื่อน (chips=1), วงล้อปิง, แบนเนอร์คราบ, ลูกศรระหว่าง toast (ไม่มีลูกศร), ลูกศรหลัง toast, การ์ดปราบคราบ, การ์ด T04 |
| — | ภาพหน้าโปรแกรม 2400×1080 | S13/S14/S15 + การ์ดจบ/ผลบท/พัก/S28 ครบชุด (รายการใน docs 24 §15.34) |

**ข้อสังเกตเดิม — แก้แล้ว 17 พ.ย. (ARROW▲):** ลูกศรขอบจอทับแบนเนอร์/ปุ่ม "ปราบ" + แถบ toast: ยกเพดาน clamp
100 → 210 และให้ลูกศรยืนพักระหว่าง toast ขึ้น (`BobTaskRaceHud.RenderEmergencyArrow` เท่านั้น);
ไดรเวอร์ยืนยันสองสภาพต่อการรัน — ระหว่าง toast `active=False`, หลัง toast `active=True pos=(-333,-330)` ใต้เพดาน 330
(`Logs/verify-shots7.log`, `SHOTS PASSED`); ภาพ `taskrace-emergency-{toast,arrow}-2400x1080.png`. รายละเอียด docs 24 §15.34

**ยังเหลือเป็นเครื่องจริง:** ติดตั้ง APK, ประสิทธิภาพ, LINE OAuth, ออนไลน์ 2 เครื่อง, Firebase tester group, UGS check (ข้อ 13, ต้องมีเน็ต/โปรเจกต์ UGS)

---

### 6.2 UI R1 native Unity (17 พ.ย. 2026 — ผ่านครบสามสัดส่วน + 4K)

- รุ่น source: `ui/unity-r1-20261117` หลังแบบ `5621332`; checkpoint `5258883` commit/push แล้วตามคำสั่ง owner.
  งานตรวจและปรับ harness หลัง checkpoint อยู่ใน commit ของรอบนี้บน branch เดิม (owner สั่ง commit push);
  ยังไม่สร้าง APK, ไม่ merge เข้า branch อื่น, ไม่ deploy.
- กติกา server ผ่าน 80/80 และ E2E 39/39; `python ready-check/check.py` ผ่าน (Windows ไม่มี `python3`).
  `UNITY-CHECK PASSED` ด้วย `DOTNET_ROLL_FORWARD=Major dotnet run --project unity-check/BobCheck.csproj`
  เพราะเครื่องมี .NET 10/11 แต่ไม่มี runtime 8; ไม่ติดตั้ง package หรือแก้ project configuration.
- ใช้สำเนา `compile-check/` เนื่องจาก Editor เปิด `unity/` อยู่; backup ไฟล์ mirror เดิมก่อน sync,
  ไม่ปิด Editor ของ owner. Compile snapshot แรก exit 0 และไม่พบ `error CS` ใน `Logs/ui-r1-compile-01.log`.
- `Bob.EditorTools.TaskRaceShots.RunR1` และ `RunR1Heroes` เป็น editor-only checks:
  fixture จัดฉาก/seed, ส่วนปุ่ม/เลือกของ/ลาก/กดค้างส่ง pointer ผ่าน raycast ของ EventSystem จริง;
  ภาพใช้ `ScreenCapture` จาก Game view และ JSON ขนาดข้อความ. ไม่ใช่ OS mouse หรือ physical touch.
- `Logs/ui-r1-heroes-04.log` / `05.log` / `06.log`: **HERO CHECKS PASSED**, exit 0 ที่
  1920×1080 / 2400×1080 / 1440×1080 ตามลำดับ; เลือกครบสี่ตัว, ข้อความ 130%, เสร็จสิ้นกลับ Home,
  ปิดกลับ Home และหยุด countdown ผ่าน. ภาพอยู่ `Logs/ui-r1-heroes-04/` ถึง `06/`;
  ตรวจภาพ Press 130% ทั้งสามสัดส่วนแล้ว ข้อความและปุ่มไม่ถูกตัด; run05/06 เพิ่ม TMP truncation assertions.
  run02/run03 เคย FAIL เพราะ runner กดก่อนเฟรมของหน้าที่สร้างใหม่วาดเสร็จ; รอ rendered frame แล้ว
  raycast ของ `HeroDone`/`HeroClose` ตกที่ปุ่มจริง ไม่ใช่ `Screens`.
- เพิ่ม native checks สำหรับเลื่อนใบงานสิบแถว, joystick, พัก/เล่นต่อ, เรียก, T04 ลาก/ยกชั้นบน/
  ปล่อย hold ก่อนครบ/ล็อกสำเร็จ/ไม่ผ่าน/กองว่าง, คราบ+toast+ลูกศร และ RenderTexture ของมินิเกมอื่น.
  `Logs/ui-r1-play-02.log`: T04 และ hold ทั้งสามกรณีผ่าน; emergency fixture FAIL เพราะเรียก debug hook
  ก่อน Update สร้าง schedule จึงเพิ่มการรอเฟรมและหยุดทันทีเมื่อ fixture ไม่เกิด ไม่แก้ runtime กติกา.
  ภาพยืนยัน paper/cream/mint/sun กลับตรงค่าสี แต่ชื่อใบงานยาวยังถูกตัดที่ 130% จึงเปลี่ยนเป็น wrap
  พร้อมความสูงแถวตามข้อความและ scroll; เพิ่ม assertion ตรวจ TMP truncation.
- `Logs/ui-r1-play-03.log` / `04.log` ผ่านที่ 1920×1080 / 2400×1080 รวม emergency, arrow,
  ข้อความ 130% และคืน camera/overlay หลัง capture. run05 ที่ 1440×1080 เหลือคำแนะนำ T04 ถูกตัดสองภาพ;
  ลดคำซ้ำเป็น `ลากให้ผ่านสูตร แล้วกดค้างล็อก` ไม่ลด font. ภาพยังพบ SeriesPill ทับ budget bar และ delta FX
  ทับหัวข้อ stat จึงแยก pill เป็น canvas order 19 และจำกัด FX ใกล้แถบค่า.
- run06/07 ผ่านที่ 1440×1080 / 1920×1080 รวม layer assertions และ grace 25 วิ;
  ภาพ 4:3 ยืนยัน hint ครบ, pill ไม่ทับ budget และ delta FX ไม่ทับชื่อ stat.
  fixture คราบตั้ง elapsed ตรงเวลา spawn ใน schedule เพื่อถ่าย grace 25 วิจริง ไม่ใช่เร่ง spawn จากอนาคต.
- run08 ที่ 2400×1080 มีสาม FAIL ต่อเนื่องจาก short-hold สูตรสำเร็จล็อกในหนึ่งเฟรมหลัง capture;
  layout/130%/คราบ/กล้องผ่าน. หนึ่งเฟรมของ Editor ไม่รับประกันว่าจะสั้นกว่า 0.6 วิ:
  short-hold fixture จึงส่ง pointer จริงแล้ว tick 0.12 วิ (20%), ปล่อยแล้ว tick 0.05 วิ (10%) และ 0.06 วิ (0%)
  ในเฟรมเดียว ตรวจ decay `dt * 2`; full hold ยังผ่าน pointer และ Update จริงพร้อมรอ 0.7 วิ.
  ไม่เปลี่ยน runtime หรือเกณฑ์ผ่าน. run09/10/11 **R1 CHECKS PASSED**, exit 0 ที่2400/1920/1440ตามลำดับ.
- Review พบ joystick zone สูง560u บัง88uล่างของ viewport ใบงาน: เลื่อนลำดับ zone ให้อยู่หลัง HUD ปกติ
  และยกเหนือ dim เฉพาะขณะเปิดมินิเกม ก่อนคืนเมื่อปิด; ไม่เปลี่ยนขนาด/ความไว/เกณฑ์ยกเลิก70%.
  เพิ่ม native drag จากขอบล่างใบงานก่อนและหลังเปิดการ์ด พร้อมตรวจว่า ScrollRect รับ drag และ hero ไม่เดิน,
  joystick ยังยกเลิก TASK_LOCK ได้ และการ์ดจบรอบกับ dim แสดง/ซ่อนพร้อมกัน; run12/13/14 assertions ผ่านทั้งหมด.
- ตรวจภาพ run12 พบ HUD ยังวาดเหนือการ์ดจบรอบที่4:3 แม้ sortingOrder assertions ผ่าน:
  pixel บนกระดาษ HUD ก่อน/หลังจบรอบยังเป็น RGB255/253/245 ขณะที่1920/2400หรี่เป็น166/165/159.
  เปลี่ยน pill จาก nested canvas เป็น root canvas แยก order19; การ์ดจบรอบอยู่ root order40.
  เพิ่ม pixel regression เปรียบเทียบกระดาษ HUD ก่อน/หลัง dim ในภาพจริง: run15 (1440) ผ่าน แต่ run16 (1920)
  ยัง FAIL เฉพาะ pixel dim; chain จึงไม่รัน run17. ไม่ถือว่าแยก root แล้วแก้ครบ.
  run18 ที่ 1920×1080 **ผ่าน** หลัง capture helper เก็บ/คืน sorting layer และ order พร้อมโหมด/กล้อง แล้ว log
  ลำดับเรนเดอร์จริง (`CountdownCanvas order=45 rendered=3`, `SeriesCanvas order=40 rendered=2`,
  `Canvas order=20 rendered=1`, `SeriesPillCanvas order=19 rendered=0`) — ค่าที่ FAIL ใน run16 เป็น artefact
  ของตอน capture ไม่ใช่ HUD ในเกม; กระดาษ HUD ก่อน/หลัง dim = RGBA(1.000, 0.992, 0.961) → (0.651, 0.647, 0.624)
  เหมือนกันทุกสัดส่วน. run19 (1440) / run20 (2400) / run21 (1920 หลังแก้ material ตัวละคร) /
  run22 (3840×2160 หลังแก้เส้น services + ออนไลน์) = **R1 CHECKS PASSED**, exit 0, ไม่มี FAIL และไม่มี exception
  (`Logs/ui-r1-play-19.log:15416`, `Logs/ui-r1-play-20.log:15602`, `Logs/gap-services-play-22.log:15178`).
- ชุดสีรถเข็นตามกรอบตกแต่ง (docs 31 §2.3) เริ่มที่ run23 ซึ่ง **FAIL** เพราะ fixture อ่าน `cartDropZone`
  ก่อนที่การ์ดจะถึงขั้นซ้อน — โซนนั้นถูกสร้างตอนนี้จึงเป็น null แล้ว NullReference ทำ coroutine ตาย, watchdog
  รายงาน `R1 timeout; started=True frame=5243` (เก็บไว้แค่ 355 PASS). run24 ผ่าน 408 / 0 FAIL แต่ภาพชี้ว่า
  ทาสีเส้นชั้นของในรถด้วยทำให้ `frame_teal` `#2F7F6F` แยกไม่ออกจากเขียว gameplay `#256552` ของสัญญาณ
  "ชั้นบนสุด" จึงถอยจุดนั้นให้อยู่เป็นสีเทากลางตามเดิม. run25 ที่ 1920×1080 (เฉพาะเส้นกรอบตัวรถเข็น +
  null-guard ให้ตกเป็น Check แทน Exception) = **R1 CHECKS PASSED**, 398 PASS / 0 FAIL / 0 exception, exit 0
  พร้อม `PASS trolley outline takes the worn frame colour`
  (`compile-check/Logs/cart-tint-play-25.log`, ภาพ `compile-check/Logs/cart-tint-play-25/cart-frame-teal-1920x1080.png`).
- การขยาย 130% ใน runner ปิด TMP auto-size และคูณ font size จริง ไม่ใช่ dynamic type ของระบบ.
  Android Back, รอยบากจริง, dynamic type ของระบบ, performance มือถือ และออนไลน์สองเครื่องยังไม่ยืนยัน.
- ภาพ native บางเฟรมมีปื้นสี magenta บนตัวละครในโลก 3D เช่น `ui-r1-play-16/hud-1920x1080.png` — **วินิจฉัยและแก้แล้ว**:
  สี่ prefab `unity/Assets/Resources/Characters/SK_hero_{sweep,bubble,spray,press}.prefab` มี renderer ที่
  `m_Enabled: 1` แต่ช่อง material ว่าง (`{fileID: 0}`) และ Unity วาด material ที่หาไม่เจอเป็นสี magenta
  (shader ไม่ได้พัง, ไฟไม่ผิด). ผูกช่อง Outline กลับที่ `Materials/M_BOB_Outline.mat` (`_Pixels: 2.5`) ทั้งสี่ไฟล์,
  ให้ `BobPropImporter` ผูกใหม่อัตโนมัติตอน import เพราะ material ชั่วคราวที่ `BobToon.Outline` สร้างเก็บใน prefab ไม่ได้
  และเพิ่ม `check_hero_materials()` ใน `ready-check/check.py` กันย้อนกลับ. หลักฐาน: `Logs/ui-r1-hero-fix-21.log`
  บรรทัด 1178/1191/1204/1217 `PASS hero … has supported persistent materials` และภาพ
  `Logs/ui-r1-play-18/hud-1920x1080.png` (ปื้น magenta) เทียบ `Logs/ui-r1-hero-fix-21/hud-1920x1080.png`
  (ขอบเข้มปกติ) — นับจากนี้ผล UI R1 ใช้รับรองภาพตัวละครใน Editor ได้ ยังไม่ใช่คำรับรองบนมือถือ
- **การ์ด overlay ของ Home (docs 31 §2.1–§2.3, docs 11 §14.2)** ย้ายจากชุด `BobUi` เดิมมา `BobUiR1` ทั้ง plate,
  label, ปุ่ม และแถว — run 26 `Logs/cards-play-26.log` = 138 PASS / 32 FAIL เปิดสามกลุ่มปัญหา: (ก) assertion ฟอนต์
  เทียบชื่อ asset แต่ `CreateFontAsset` ตอนรันงานปล่อยชื่อว่าง จึงเปลี่ยนไปเทียบ `atlasPopulationMode == Dynamic`
  (ตัว pre-baked ใน Resources เป็น Static) (ข) บอร์ดใน Editor อ่านไม่ได้จริง (UGS leaderboard 404 — dashboard ยังไม่มี
  config) จึงเพิ่ม `BoardFixture` สร้าง `BobBoardSnapshot` ด้วย reflection เพื่อวัด layout แถว และเปลี่ยนข้อตรวจ
  เป็น "ตก branch ใด branch หนึ่ง" แทนการบังคับว่ามีแถว (ค) 20 ข้อ `130% text height` FAIL เพราะแถวของอันดับ/ตู้เสื้อผ้า
  ทับกันเกินการ์ด → ใส่รายการยาวใน `ScrollList` (ScrollRect) และเพิ่มความสูง rect ของแถว/ป้ายแทนการลดฟอนต์.
  run 27 = 245/4 เหลือ 4 ข้อที่ `raycast Button hits Overlay` — การ์ดที่ความสูงมาจาก `ContentSizeFitter`
  มี plate พื้นที่ 0 จนถึง layout passถัดไป จึง force rebuild + `Canvas.ForceUpdateCanvases` + รอเฟรมที่วาดจริง
  (helper `Drawn`) แล้วส่งทุกการกดในการ์ดผ่าน `Tap`; ภาพ run 27 ยังชี้ว่าแถวในลิสต์กว้าง 0 เพราะ content ของ ScrollRect
  ไม่ถูกยึดเต็ม viewport — แก้ที่ `ScrollList` ตรง ๆ. run 28 = 251/0 `CARD CHECKS PASSED` แต่ภาพตู้เสื้อผ้าจบกลางแถวที่ 4
  จึง quantize ความสูง viewport เป็นจำนวนแถวเต็ม (row pitch = แถว + ช่องไฟ 12). run 29 ที่ 1920×1080 =
  **`CARD CHECKS PASSED` 249 PASS / 0 FAIL**, exit 0, `Logs/cards-play-29.log` + ภาพ 12 ไฟล์
  `Logs/cards-play-29/card-{board,board-ranked,daily,pause,settings,wardrobe}[-text130]-1920x1080.png`
  (Exception หนึ่งบรรทัดใน log คือ `[UGS] leaderboard read failed … 404` ที่ service log เอง ไม่ใช่ของไดรเวอร์ —
  branch นี้จึงถูกถ่ายเป็นภาพจริงใน run 29 ส่วน branch ออฟไลน์อยู่ใน run 28). การ์ดที่แก้ค่าถูกคืนก่อนจบ run:
  `PASS the card run leaves the sound and music switches as it found them` และ
  `cosmetics restored: frame="frame_teal" title="" owned="title_sweep,frame_teal"`; ภาพ `card-wardrobe` ถ่ายหลังกด `ใช้`
  แถวที่ได้จึงขึ้น `ใช้อยู่` สองแถว (ป้ายกับกรอบคนละ slot). ด่านอื่นหลังแก้: `typecheck ok: 43 known kinds`,
  `UNITY-CHECK PASSED`, `READY-CHECK PASSED` (glyphs 3011 literal). ยังไม่ใช่คำรับรอง touch จริง/Android Back/
  dynamic type ของระบบ และยังไม่มีการยืนยันคะแนนจริงบนคลาวด์

### 6.3 ช่องว่างเซฟ / อันดับ / เดลี่ / ออนไลน์ที่ปิดรอบนี้ (17 พ.ย. 2026)

- **Cloud Save เคยไม่โหลดเลย** เพราะไม่มีซีนไหนวาง `UgsCloudSaveManager` ไว้ และ bootstrap ไม่ได้เรียกโหลด
  ส่วน `UgsLeaderboardManager` ตายเป็น singleton ตอนโหลดฉาก (การ์ดอันดับจึงเงียบหลังเข้า Task Race)
  แก้ด้วย `Ensure()` ในทั้งสองคลาส (สร้างตัวเอง + `DontDestroyOnLoad`, คืน `null` นอก Play mode),
  `UgsBootstrapManager.KickoffAsync` เรียก `Ensure()` แล้ว `LoadProfileAsync()` จริง, `BobBoard` และ
  `BobRaceScoreReporter` ใช้ `Ensure()` แทนการ `new GameObject` เอง — docs 27 §3 บันทึกไว้แล้ว
- แขกที่เคยล็อกอินแล้วเปิดเกมรอบใหม่: `Game.BootFlow` อ่าน `PlayerPrefs["bob.guest"]` แล้วสร้าง
  `UgsBootstrapManager` ทันที (เดิมต้องกดปุ่ม S02 ก่อนเท่านั้น) — cloud เก็บเฉพาะ cosmetic ผ่าน
  `UnlockItemAndSaveAsync`, เหรียญ/XP ยังอยู่ key `bob.*` บนเครื่องตามเดิม (ยังไม่มีนโยบาย merge cloud ↔ เครื่อง
  จึงไม่แต่งตัวเลขขึ้นใหม่ — owner ต้องสั่งก่อน)
- เดลี่ตรวจแล้วยังถูก: `BobDailyStore` ลงบัญชีตาม key วัน และ `BobDailyFeed` เทียบ seed กับ `BobDaily.Matches`
  ก่อนนับ → ไม่แก้
- ซีรีส์ที่ถูกยกเลิก (มีคนออกกลางรอบ) เคยยังจ่ายสถิติ/คะแนนบอร์ด → `TaskRaceModeManager` เพิ่ม
  `MatchSequence` + `LastMatchCanceled` และธง `roundRegistered` (งานรอบปัจจุบันเข้า series แล้ว EndMatch
  ไม่บวกซ้ำ; ถ้าโมฆะก่อนลงทะเบียนถึงบวกครั้งเดียว), `BobTaskRaceProfile.Record(canceled: true)` จ่ายเฉพาะ
  เหรียญ/XP ไม่เขียน `BestPoints`/`Matches`/`Wins`, `BobRaceScore` ไม่นับผลชนะและเวลาเหลือ, reporter ไม่ส่งขึ้นบอร์ด
- ออนไลน์: `bob.task` มีเลขรอบ, ฝั่งรับทิ้งแพ็กเก็ตคนละรอบแล้วเรียก `TaskSpot.SettleFromRoom()` (เส้นทางจริงของ
  สถานีสอง pass) และ host rebroadcast ด้วยรอบเดิม; `bob.start` พก `difficulty` + `stageOrder` ของ host
  (clamp ที่ปลายทาง ไม่ throw บนสาย Netcode) client จึงเล่นด่านเดียวกัน; host หลุดกลางรอบที่ยังไม่จบ → client เห็น
  `AbandonSeries` เหมือนกด back
- หลักฐานชั้นที่รันได้บนเครื่องนี้: `bash unity-typecheck/check.sh` (ไม่มี error ชนิดใหม่),
  `dotnet run --project unity-check/BobCheck.csproj` = `UNITY-CHECK PASSED`, mirror compile
  `Logs/gap-services-compile-01.log` exit 0 ไม่มี `error CS`, play-mode `Logs/gap-services-play-22.log`
  = 396 PASS ไม่มี FAIL
- **ยังยืนยันบนเครื่องนี้ไม่ได้ (Gate):** ค่าจริงของ UGS (ต้องล็อกอิน Unity Cloud + เปิด Cloud Save / Leaderboards /
  Lobby / Relay ใน dashboard), ออนไลน์สองเครื่อง, APK + มือถือจริง, Firebase tester group — คงตาม §6.1 ข้อ 13/14

## 7. ลำดับลงมือถัดไป

1. ~~owner เปิด Unity รันข้อ 3.2~~ → **เสร็จ 17 พ.ย.** (ตาราง §6.1); ข้อ 13 (UGS) กับข้อ 14 (APK บนเครื่อง) ยังต้องรัน
2. ผมแก้เฉพาะที่ log ชี้ (ไม่เดา) แล้วรันชั้น 1 ซ้ำจนเขียว — ยังเขียวหลังแก้ CS0117
3. เมื่อชั้น 2 ผ่าน: build APK (ข้อ 14) → เดินชั้น 3
4. ระหว่างรอเครื่อง: เริ่มข้อ 3.3 ตามลำดับที่ owner เรียง (T16 → S16/แชท → PvP; `bob_score_stack` ปิดแล้วที่ docs 24 §15.32)
5. งานบ้าน 3.4 (ซีนตาย, test เก่า, LFS) ทำเมื่อ owner อนุมัติ เพราะแตะ Build Settings/shared repo

**หมายเหตุ 17 พ.ย. 2026 (ลีดเดอร์บอร์ด + คะแนนรวม, docs 31):** สูตร `BobRaceScore` ถูกตรึงด้วยตารางเลข 10 แถวชุดเดียวกัน
ทั้ง C# (`unity-check`) และ JS (`task-race-test` 80/80) ⇒ เลขไม่เพี้ยนระหว่างเครื่อง/เซิร์ฟเวอร์. **ชั้น 1 พิสูจน์ได้แค่นั้น**:
การส่งขึ้น `global_high_score` (`BobRaceScoreReporter`) ต้องรอเครื่องที่ล็อกอิน UGS — sandbox ไม่มี Unity, ไม่มีเน็ตไป Unity Cloud
และไม่มีบัญชีของโปรเจกต์ จึงยังไม่มีหลักฐานว่าคะแนนขึ้นบอร์ดจริงหรือได้อันดับเท่าไร
