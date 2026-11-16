# 30 · ตรวจทั้งระบบ + แผนทดสอบเต็มระบบ (16 พ.ย. 2026)

สถานะ: **ตรวจครบ 1 รอบ · เจอบล็อกเกอร์ "เกมเล่นไม่ได้" 1 ตัวและแก้แล้ว · เพิ่ม gate ใหม่ 1 ตัว (`ready-check`)**
ยังไม่มีการรันใน Unity เลยในรอบนี้ เพราะ sandbox ไม่มี Unity — ข้อ 6 เขียนตรง ๆ ว่าเหลืออะไรให้พิสูจน์บนเครื่อง owner

---

## 0. สรุปใน 6 บรรทัด

1. **เจอบล็อกเกอร์จริง:** ซีน Task Race ทั้งสามไม่มี `EventSystem` และไม่มีโค้ดสร้างให้ → build ที่เปิดซีนนี้ตรง ๆ จะ**แตะอะไรไม่ติดเลย** (HUD, ปุ่มพัก, วงล้อ ping, การ์ดมินิเกม)
2. **แก้แล้ว** ที่ `TaskRaceModeManager.Awake` → `EnsureEventSystem()` สร้าง `EventSystem` + `StandaloneInputModule` ให้เองถ้ายังไม่มี และ `DontDestroyOnLoad` ตอนเล่นจริง
3. **เพิ่ม gate `ready-check`** ที่จับ 5 ชนิดความพังเชิง "เล่นไม่ได้" ซึ่ง typecheck มองไม่เห็น (ซีนหาย, GUID ไม่ตรง, `LoadScene` ชื่อผิด, Canvas ไม่มี EventSystem, asset ที่โหลดด้วยชื่อหาย, ชื่อ sprite/เสียงที่โค้ดขอไม่มีจริง)
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
| `server/task-race-test.js` | ✅ 41/41 | รวมกับ LINE auth ใน `bob-check` |
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

**gate ใหม่ `ready-check/`** — `python3 ready-check/check.py` หรือ `bash ready-check/check.sh`, exit 0/1/2, พิมพ์ `READY-CHECK PASSED` ตอนผ่าน. ตรวจ: build scene มีจริง+GUID ตรง · `LoadScene` ชี้ซีนที่มีใน Build Settings · Canvas ทุกซีนมี EventSystem หรือมีตัวสร้าง · Resources ครบ · ชื่อ sprite/เสียงที่โค้ดขอมีจริง · สรุปสถานะ LFS. รอบล่าสุด:

```
scenes: Game, TaskRace, TaskRace_Duo, TaskRace_Team
art: 18 sprite name(s) checked, audio: 38 clip(s), tone fallback: ping_pop
note: LFS: 0 pointer(s), 301 plain binary file(s) under unity/Assets
READY-CHECK PASSED
```

**พิสูจน์ว่า gate จับได้จริง (ทดสอบย้อนกลับ):** ลองเปลี่ยนชื่อ `EnsureEventSystem` ใน `TaskRaceModeManager.cs` ชั่วคราว แล้วรัน gate → แดง 3 บรรทัดตรงเป้า:

```
READY-CHECK FAILED:
  - TaskRace has a Canvas, no EventSystem in the scene, and TaskRaceModeManager.cs does not build one
  - TaskRace_Duo has a Canvas, no EventSystem in the scene, and TaskRaceModeManager.cs does not build one
  - TaskRace_Team has a Canvas, no EventSystem in the scene, and TaskRaceModeManager.cs does not build one
```

รอบแรกของ gate ผ่านทั้งที่ยังไม่มี EventSystem เพราะยอมรับ "มีตัวสร้างอยู่ในไฟล์ใดก็ได้" — แก้ให้ผูกซีนกับเจ้าของของมัน (TaskRace → `TaskRaceModeManager.cs`, Game → `BobGame.World.cs`) แล้วจึงจับได้จริง

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
| 5 | บอทเล่นแมตช์เต็มในซีน Game | `Bob/Run Game Test` |
| 6 | บอทเก็บงาน + ข้ามสะพาน + sabotage ใน Task Race | `Bob/Run Bot Check` |
| 7 | อนุกรม 3 ยก + ดาว 1–3 + XP เคลียร์ครั้งแรก | `Bob/Run Story Series Check` |
| 8 | การ์ดผลบท + ปุ่มบทถัดไป | `Bob/Run Chapter Result Check` |
| 9 | พักเกม: นาฬิกาหยุดจริงแล้วเดินต่อ | `Bob/Run Pause Check` |
| 10 | คานต์ดาวน์ 3-2-1-ลุย! | `Bob/Run Countdown Check` |
| 11 | บันไดบท (LV 2–6, ดาว ≥ 2) | `Bob/Chapters/Check Ladder` |
| 12 | ภาพ/เส้นทาง NavMesh ทั้ง 3 ขนาดเกาะ | `TaskRaceVerify.Run / RunDuo / RunTeam` |
| 13 | UGS: anonymous → Relay → Lobby | `Bob/Run UGS Check` |
| 14 | build APK + ติดตั้งบนเครื่องจริง | `Bob/Android/Build APK` |

### 3.3 งานฟีเจอร์ที่ยังไม่ทำ (ไม่บล็อกการเล่น 3 บทแรก)

T16 แถวฉุกเฉิน + คราบบอส · T12 ภาพพอร์เทรต/ช่องเส้นประ · S25 ก่อน/หลัง · แท็บตั้งค่า + โหมดกดค้าง · แชท/พร้อม/S16 เลือกฮีโร่ออนไลน์ · `bob_score_stack` ฝั่ง server + ตัวเรียกฝั่ง client · PvP สองทีมออนไลน์ · music layering (ยังไม่มี stem)

### 3.4 งานบ้าน (ทำเมื่อ owner อนุญาต)

ลบหรือเปลี่ยนชื่อ build entry `TaskRace_Duo/Team` (หรือผูกให้ S14 เลือกซีนจริง) · ปลด/เก็บ `coop-test`/`mp-test` ยุค Nakama · แก้ LFS ให้อัปโหลดได้ (ต้องมีสิทธิ์เขียน LFS ของ repo)

---

## 4. แผนทดสอบเต็มระบบ (4 ชั้น)

### ชั้น 1 — ออฟไลน์ ไม่ต้องมี Unity (รันได้ทุก commit)

| คำสั่ง | พิสูจน์ | ผ่านเมื่อ |
| --- | --- | --- |
| `bash unity-typecheck/check.sh` | ทุกสคริปต์ compile เทียบ baseline | `typecheck ok: 40 known Unity-6 gap kind(s), no new errors` |
| `cd unity-check && dotnet run` | กติกาล้วน: กอง 5 ชั้น, ตารางฉุกเฉิน, series/stars, ห้องออฟไลน์ | `UNITY-CHECK PASSED` |
| `/usr/local/bin/bob-check` | module server สด + `task-race-test` + LINE auth + typecheck | `BOB-CHECK PASSED` |
| `bash ready-check/check.sh` | **เล่นได้จริงใน build**: ซีน/GUID/EventSystem/Resources/ชื่อ asset | `READY-CHECK PASSED` |

เกณฑ์รวม: ทั้ง 4 ต้องเขียวก่อน merge ทุกครั้ง. ถ้าตัวใดแดง = หยุด ไม่ใช่ "ข้ามไปก่อน"

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

---

## 7. ลำดับลงมือถัดไป

1. owner เปิด Unity รันข้อ 3.2 ตามลำดับ แล้วส่ง `unity/Logs/*.log` กับภาพที่ได้กลับมา — ผมอ่านให้และแก้จุดที่ตก
2. ผมแก้เฉพาะที่ log ชี้ (ไม่เดา) แล้วรันชั้น 1 ซ้ำจนเขียว
3. เมื่อชั้น 2 ผ่าน: build APK (ข้อ 14) → เดินชั้น 3
4. ระหว่างรอเครื่อง: เริ่มข้อ 3.3 ตามลำดับที่ owner เรียง (T16 → S16/แชท → `bob_score_stack` → PvP)
5. งานบ้าน 3.4 (ซีนตาย, test เก่า, LFS) ทำเมื่อ owner อนุมัติ เพราะแตะ Build Settings/shared repo
