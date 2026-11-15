# BOB — กระดานประสานงานระหว่าง branch

ไฟล์นี้เป็น **แหล่งความจริงเดียว** ว่าใครกำลังแก้อะไร แตะไฟล์/ซีนไหน และสถานะล่าสุดเป็นอย่างไร
ทุก session ที่ทำงานใน repo นี้ต้องอ่านก่อนเริ่ม และอัปเดตแถวของตัวเองหลังจบงาน
(เส้นทางไฟล์ทั้งหมดอ้างจาก root ของ repo นี้)

> owner จะเพิ่มกฎการเขียน/การจองในหัวข้อ 1 — **กฎของ owner มาก่อนค่าเริ่มต้นทุกข้อในไฟล์นี้**

---

## 1. กติกาการเขียน/การจอง (owner เพิ่ม)

_(รอ owner เติมกฎจริง — ด้านล่างเป็นค่าเริ่มต้นที่ใช้ไปก่อน)_

1. **จองก่อนลงมือ**: เพิ่ม/แก้แถวในหัวข้อ 3 (ชื่อ branch, ไฟล์/ซีนที่จะแตะ, วันที่) ใน commit เล็ก ๆ ก่อนเริ่มโค้ด เพื่อให้อีกฝั่งเห็นก่อน
2. **branch ใคร branch มัน**: ทำงานบน branch ของตัวเอง ห้าม rebase branch ที่ push ขึ้น remote แล้ว และ merge `main` บ่อย ๆ
3. **แตะไฟล์ hot (หัวข้อ 4) ต้องประกาศในหัวข้อ 3 ก่อน** และหลีกเลี่ยงการจัดหน้า/เปลี่ยน layout ที่ไม่ได้อยู่ในงานของตัวเอง
4. **rebuild ซีน = จองคิว (หัวข้อ 5)**: ทำทีละ session เท่านั้น และต้อง verify ผ่านก่อนปล่อยคิว พร้อมแนบชื่อ log/รูป
5. **เสร็จแล้ว**: เปลี่ยนสถานะเป็น `✅` + commit hash + log; ถ้าทิ้งกลางคันเปลี่ยนเป็น `⏸` พร้อมบอกว่าไฟล์อยู่ในสภาพไหน
6. **ห้ามแก้แถวของคนอื่น** — เขียนหมายเหตุในคอลัมน์โน้ตแทน
7. **push / deploy / อัป APK ให้รอ owner สั่งชัดเจน** — "ทำเสร็จ" แปลว่าพร้อม ไม่ใช่ส่งแล้ว

---

## 2. branch / session ที่ทำงานอยู่

| Branch | Session | ขอบเขตงาน | สถานะ | อัปเดตล่าสุด |
| --- | --- | --- | --- | --- |
| `main` (local, **ยังไม่ push**) | Claude (session นี้) | merge `origin/main` เข้าไลน์ P0, จัด HUD, ผูก art library — commit `8655d11` | 🟢 ว่าง / พร้อมจองงานใหม่ | 15 พ.ย. 2026 |
| `qoder/general-session-98ru4v` | Qoder (session ขนาน) | **4.3** · **4.5** · **4.6** ปิดครบ (✅ `9b40bab`, 4.6 ต่อด้วย `067166f`) · **B** server contract 16–21 (`196d1b3`/`27d4f1d`/`6a66ab7`) · **A** S13 + `ChapterDefinition` (`067166f`) — head `6a66ab7` (ยังไม่ merge กลับ `main`) | 🟢 เสร็จ/พร้อม review — เหลือ verify ใน Unity | 15 พ.ย. 2026 |
| `origin/main` | remote | `76a1266` (มี `8655d11` ของฝั่งเราแล้ว) | — | 15 พ.ย. 2026 |

> **หมายเหตุ Qoder (15 พ.ย.):** P3 (`StorySeries` 3×100 วิ) อยู่บน `main` **local** ของอีก session และยังไม่ push — branch นี้ merge `origin/main` `76a1266` แล้ว แต่ยัง merge P3 ไม่ได้จนกว่า `main` จะขึ้น remote (หรือ owner merge branch นี้เข้า `main`)
> จุดที่คาดว่าชนเมื่อ merge: `TaskRace/TaskRaceModeManager.cs` — ฝั่ง P3 เพิ่มฟิลด์/เมธอด/event, ฝั่งนี้เพิ่ม `ApplyChapter()` + เรียกใน `Awake()` (อ่านบทจาก S13 มาคุม `difficulty`/`matchDuration`) ทั้งสองอย่างเป็นส่วนเพิ่ม **เก็บทั้งสองฝั่ง** ได้
> ⚠️ ตอนต่อ P3: ถ้า `StorySeries` จะคุมนาฬิกา 100 วิเอง ต้องให้ `ApplyChapter` ไม่ทับ `matchDuration` ในโหมดเนื้อเรื่อง (ทุกบทตั้ง `roundTime: 120` อยู่ตอนนี้ จึงยังไม่มีผล)

---

## 3. งานที่ถูกจอง (claims)

สถานะ: `⏳ จองแล้ว` · `🟡 กำลังทำ` · `✅ เสร็จ/merge แล้ว` · `⏸ หยุดกลางคัน` · `❌ ยกเลิก`

| ID | งาน | เจ้าของ | Branch | สถานะ | ไฟล์/ซีนที่แตะ | อัปเดต / โน้ต |
| --- | --- | --- | --- | --- | --- | --- |
| 4.3 | กล้อง / จุดงาน (camera + station points) | Qoder session นี้ | `qoder/general-session-98ru4v` | ✅ `9b40bab` (ค่าถูกเขียนทับซ้ำที่ merge `1ae7230`) | `TaskRace/BobTaskRaceCamera.cs` (สูง 6.2 / ถอย 5.2 / look 0.72 + 40°), `TaskRace/TaskSpot.cs` (ป้ายไอคอนลอย + วงแหวนหมวดหมู่ + `taskId`), `TaskRace/BobBridgeDressing.cs` (`width 1.7`), `TaskRace/BobWaterfallDressing.cs` (`halfWidth 2.2/2.4/2.8`, `topY 2`), `Editor/BobMapAssemblerEditor.cs`, `Editor/TaskRaceVerify.cs` (กล้องช็อต S28/ผลลัพธ์ใช้ค่าใหม่), `Scenes/TaskRace.unity` + `_Duo` + `_Team` (14 บรรทัด/ไฟล์), `Resources/Icons/icon_ability·skill_*` | **ไม่แตะ `TaskRace/BobTaskRaceHud.cs`** — PadL / แถบบน 940×124 / แถวงาน / การ์ด มาจาก `8655d11` ของอีก session ที่ merge เข้ามา แล้วไม่ได้แก้ค่าเหล่านั้น. **สถานะ rebuild: ต้อง rebuild** — ค่าในซีนถูกเขียนใน YAML ตรง ๆ ยังไม่มี Unity รันชุดสุดท้าย; รอคิวข้อ 5 แถว 2. หลักฐานที่มี: `git show 9b40bab`, grep ค่าใน YAML (§5 แถว 2); **ยังไม่มี log/รูป** ของซีนชุดนี้ |
| 4.5 | ghost hand onboarding (สอนด้วยการทำ) | Qoder session นี้ | `qoder/general-session-98ru4v` | ✅ `9b40bab` | `Game/BobGame.Tasks.cs` (`GhostSweep`, `miniGhostStroke` — ลากตามสเต็ปจริง 1.4 วิ + เฟด, ครั้งเดียวต่อเครื่อง `bob.tutorial`) · ไม่แตะ `TaskRace/` และไม่แตะ HUD/ซีน | ไม่มี opcode/server. Home มีปุ่มเล่นเดียวอยู่แล้ว (`BobGame.Menus.cs`) จึงไม่ต้องมีด่านสอนแยก. **หลักฐาน:** typecheck `dotnet build unity-typecheck/BobTypeCheck.csproj` ไม่มี error นอก baseline (58 = `CS0246`/`CS0234` เท่านั้น); **ยังไม่มีภาพ Unity** — ต้องเล่น `Game` scene ครั้งแรก (ไม่ใช้คิว Task Race) แล้วแคป ghost hand |
| 4.6 | specialty / ability / preview | Qoder session นี้ | `qoder/general-session-98ru4v` | ✅ `9b40bab` (แถบเลือกบทถูกแทนที่ต่อใน `067166f`) | `Game/BobGame.Menus.cs` (specialty +25% มีผลจริง, `ItemUse`, การ์ดบท/S13 แทนชิป band 3 ใบ), `Game/BobGame.Tasks.cs` (ability 1 ครั้ง/แมตช์ + reach/threshold/spray), `Game/BobGame.cs` (`Phase.Chapters`), `Game/BobGame.Hud.cs` (ป้าย ability ในล็อบบี้ HUD), `Rules/ChapterDefinition.cs` + `Rules/BobChapters.cs` + `Resources/Chapters/*.asset` (ใหม่) | **ไม่แตะ `TaskRace/BobTaskRaceHud.cs`** และ **ไม่มี opcode ใหม่** — ability 4.6 เป็นโลจิกฝั่ง client; opcode 16–21 เป็นของงาน B (ไม่ซ้ำกับ server contract ที่อีกฝั่งรอ). หมายเหตุ: best score ยังคีย์ด้วย band เดิม (`bob.taskbest.1..3`) แม้หน้าเลือกด่านจะเป็นรายบท. **หลักฐาน:** typecheck baseline + asset 6 ไฟล์ผ่านการอ่าน YAML/กติกา order·D; **ยังไม่มีภาพ S13/Home** — ต้องเปิด `Game` scene (ไม่ใช้คิว Task Race) |
| — | merge + HUD layout/icons | ฝั่งเรา | `main` local | ✅ `8655d11` | `TaskRace/BobTaskRaceHud.cs`, `TaskRace/TaskRaceModeManager.cs`, `Game/BobGame.Tasks.cs`, `Editor/TaskRaceSceneBuilder.cs`, `Scenes/TaskRace*.unity` | 15 พ.ย. — verify ครบ solo/duo/team, ยังไม่ push |
| B | **server contract: `TASK_BEGIN` / `TASK_LOCK` + seed** (doc 16 §8, opcode 16–21) | Qoder session นี้ (owner เคาะแล้ว) | `qoder/general-session-98ru4v` | ✅ `196d1b3` + `27d4f1d` + `6a66ab7` | `prototype/index.html` (บล็อก `TASK RACE`), `server/modules/index.js` (regen), `server/match.js`, `server/task-race-test.js` (ใหม่), `unity/Assets/Scripts/Rules/BobTaskRaceRules.cs` (ใหม่), `Net/BobOnline.cs`, `Net/BobServer.cs`, `TaskRace/TaskRaceRound.cs` (PlanRound), `art/docs/16` §8 | 15 พ.ย. — เทสต์ `node server/task-race-test.js` = **39/39** (ไม่ต้องมี Nakama/docker) และ C# `SelfTest()` ตรงกับ JS 5 seed. **เปลี่ยนพฤติกรรม 1 จุด**: `TaskRaceRound.PlanRound` ใช้ `BobTaskRaceRules` (mulberry32) แทน `System.Random` → seed เดิมได้ชุดมินิเกมต่างจากเดิม (องค์ประกอบ/น้ำหนักเท่าเดิม) ถ้าไม่ต้องการ revert commit `27d4f1d`. **(owner เคาะ 15 พ.ย.: เก็บไว้)** · `spot` ตรวจแล้ว: ต้องเป็นสถานีฝั่งผู้เล่น (`S<n>_A`/`S<n>_B`) และหนึ่งสถานีถือบัตรใบเดียว (ล็อกจำ spot, DONE/CANCEL ปลดเอง) — เทสต์ถัดไปเป็น 39/39; ที่ยังไม่มีคือระยะห่างจริงจากจุดงาน (server ไม่ถือพิกัด) |
| A | **S13 เลือกด่าน + `ChapterDefinition`** (doc 24 §15.3 P0) | Qoder session นี้ | `qoder/general-session-98ru4v` | ✅ `067166f` | ใหม่: `Rules/ChapterDefinition.cs`, `Rules/BobChapters.cs`, `Resources/Chapters/*.asset`; แก้: `Game/BobGame.Menus.cs`, `Game/BobGame.cs` (enum), `Game/BobGame.Hud.cs` (case), `TaskRace/TaskRaceModeManager.cs` (รับค่า D/เวลา), `art/docs/24` §15.3 | 15 พ.ย. — บท 1–6 มาจาก SO ใน Resources; Home มีการ์ดบทเปิด S13; ค่าที่ส่งเข้า `TaskRaceRound` คือ `difficulty` + `matchDuration` เท่านั้น (ไม่แตะ `PlanRound`) · แก้ `TaskRaceModeManager.Awake` (+`ApplyChapter()`) ซึ่งเป็นไฟล์เดียวกับ P3 — merge แบบเก็บทั้งสองฝั่ง. ฉาก `TaskRace*.unity` **ไม่แตะ** จึงไม่ต้องเบคซีน. หลักฐาน: typecheck baseline, YAML 6 ไฟล์ผ่าน, `node server/task-race-test.js` 39/39; ยังไม่มีภาพ S13 |
| — | งานถัดไปฝั่งเรา | **ยังไม่จอง** — เสนอไว้ที่หัวข้อ 6 | — | ⏳ | — | รอ owner เคาะ |

---

## 4. ไฟล์/โซนเสี่ยงชน (hot files) และวิธีแบ่ง

| ไฟล์/โซน | ใครต้องใช้ | ข้อตกลง |
| --- | --- | --- |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceHud.cs` | 4.5 ghost hand, 4.6 ปุ่ม ability/preview, S28 stage ของฝั่งเรา, งาน HUD อื่น ๆ | **ไฟล์ร้อนที่สุด** — แก้แบบเพิ่มของใหม่ (ฟังก์ชัน/ฟิลด์แยก) อย่ารื้อ layout; ถ้าต้องเปลี่ยนค่าจัดหน้า (PadL, แถบบน, แถวงาน, การ์ด) ให้ประกาศในหัวข้อ 3 ก่อน. สถานะปัจจุบัน (15 พ.ย.): แถบบน 940×124 ชิดขวาการ์ด, แถวงานมีไอคอน + ไฮไลต์งานถัดไป, chip ปิดการ์ดอยู่บนบรรทัดคำใบ้, `BindArt` ผูก `BobArtLibrary` |
| `unity/Assets/Scripts/TaskRace/BobMinigameHost.cs` | 4.5 (สอนในพื้นที่เล่น), 4.6 (ตัวช่วยตามถนัด/preview), S28 3D stage + เสียงมินิเกมของฝั่งเรา | แก้แบบเพิ่ม mechanic/ของตกแต่ง อย่าเปลี่ยนสัญญา `Begin/Tick/Cancel/Progress` โดยไม่แจ้ง |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceTouch.cs` | 4.5 (มือผี/ไฮไลต์นิ้ว) | ประกาศก่อนแก้พื้นที่รับสัมผัสหรือ dead zone |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceHero.cs` | 4.3 (กล้องตาม), 4.6 (ability) | ถ้าทั้งสองฝั่งต้องใช้ ให้แยกคนละฟังก์ชัน/คนละ field |
| `unity/Assets/Scripts/TaskRace/TaskSpot.cs` | 4.3 (จุดงาน), T16 คราบฉุกเฉิน | T16 รอจน 4.3 merge ก่อน |
| `unity/Assets/Scripts/TaskRace/TaskRaceRound.cs`, `TaskRaceModeManager.cs` | seed/งาน/ability/server/ผลแมตช์ | เพิ่ม API ใหม่ได้ แต่ห้ามเปลี่ยนพฤติกรรมเดิม (แต้ม/เวลา/รางวัล) โดยไม่แจ้ง · ตั้งแต่ 15 พ.ย. `PlanRound` เรียก `Rules/BobTaskRaceRules.cs` — แก้ตารางมินิเกม/สูตร seed ที่ไฟล์นั้นที่เดียว (และรัน `node build-module.js` ให้ server ตามด้วย) |
| `unity/Assets/Scripts/Editor/TaskRaceSceneBuilder.cs` | 4.3 (จุดงาน/กล้อง), ฝั่งเรา (`BindArt`), งานที่ต้องเบคซีน | ทุกการแก้ต้อง rebuild ซีนตาม → อยู่ในคิวข้อ 5 |
| `unity/Assets/Scenes/TaskRace.unity`, `TaskRace_Duo.unity`, `TaskRace_Team.unity` + `Assets/Scenes/NavMesh/*` | ทุกฝั่งที่ rebuild | ไฟล์ YAML หลายหมื่นบรรทัด — **ห้ามmerge ด้วยมือ**, แก้พร้อมกันไม่ได้; ใช้คิวข้อ 5 |
| `unity/Assets/Scenes/Game.unity` + `Game/BobGame*.cs` | ฝั่งเรา (Home/ด่าน), S13 | แยกจากโซน TaskRace — ชนกันยาก |
| `server/` (`bob_coop`, `rpc.js`, `build-module.js`) | 4.6 (opcode ability?), server contract ของฝั่งเรา | ประกาศในหัวข้อ 3 ก่อนแตะ opcode |

---

## 5. คิว rebuild ซีน Task Race (ทำทีละคน)

ซีน Task Race ถูกเบคด้วย builder และเก็บ HUD/จุดงานไว้ในไฟล์ซีน → ถ้าสองฝั่ง rebuild พร้อมกันจะได้ไฟล์ชนกันหนัก
**ก่อน rebuild ให้เขียนแถว "จองคิว rebuild" ด้านล่างนี้ก่อน แล้วลบ/ทำเครื่องหมาย ✅ เมื่อปล่อยคิว**

| คิว | เจ้าของ | จะ rebuild อะไร | เริ่ม | สถานะ |
| --- | --- | --- | --- | --- |
| 1 | ฝั่งเรา (ปิดแล้ว) | `TaskRace` + `_Duo` + `_Team` (แถบบน/ไอคอน/chip) | 15 พ.ย. 2026 | ✅ verify5 ผ่านทั้ง 3 โหมด, log `unity/Logs/verify5-*.log`, รูป `taskrace{,-duo,-team}-hud.png` |
| 2 | Qoder session นี้ (ปิดแล้ว) | รวมซีนที่ฝั่งเรา rebuild (`76a1266`) เข้า branch แล้วเขียนทับค่าของ 4.3 ลงใน YAML: กล้อง `6.2 / 5.2 / 0.72`, สะพาน `width: 1.7`, น้ำตก `halfWidth 2.2 / 2.4 / 2.8`, `topY: 2` | 15 พ.ย. 2026 | ⚠️ **ต้อง rebuild** — ยังไม่ verify ใน Unity และยังไม่ปล่อยคิว: ไม่ได้เปิด editor ใน sandbox นี้; ต้องมีคนรัน `TaskRaceSceneBuilder.Build*` + `TaskRaceVerify.Run*` อีกรอบเพื่อเบคค่าจาก builder จริงแล้วแนบ log + `taskrace*.png` (งาน A/S13 **ไม่ต้อง** เบคซีน — ไม่มี component ใหม่ในฉาก) |

คำสั่งที่ใช้ (อ้างจาก `BOB-repo/unity`):

```bash
U="C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe"; P="C:/Users/Jakgrits/BOB-repo/unity"
"$U" -batchmode -quit -projectPath "$P" -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.Build     -logFile Logs/xxx-solo.log
"$U" -batchmode -quit -projectPath "$P" -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.BuildDuo  -logFile Logs/xxx-duo.log
"$U" -batchmode -quit -projectPath "$P" -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.BuildTeam -logFile Logs/xxx-team.log
"$U" -batchmode -quit -projectPath "$P" -executeMethod Bob.EditorTools.TaskRaceVerify.Run          -logFile Logs/verify-xxx-solo.log
"$U" -batchmode -quit -projectPath "$P" -executeMethod Bob.EditorTools.TaskRaceVerify.RunDuo       -logFile Logs/verify-xxx-duo.log
"$U" -batchmode -quit -projectPath "$P" -executeMethod Bob.EditorTools.TaskRaceVerify.RunTeam      -logFile Logs/verify-xxx-team.log
```

เกณฑ์ผ่าน: ทุก log ไม่มี `error CS`, มีบรรทัด `player HUD: N task row(s) …` และ `result overlay: open=True …`
APK (เฉพาะเมื่อ owner สั่ง): `Bob.EditorTools.BobAndroidBuild.BuildApk` → `Builds/BOB-prototype-v1.apk`

---

## 6. งานฝั่งเรา — ตัวเลือกที่ไม่ชนกับ 4.3 / 4.5 / 4.6

เรียงตามความเสี่ยงชน (ต่ำ → สูง) — ยังไม่จอง รอ owner เคาะ

| ลำดับ | งาน | ทำไมไม่ชน | ไฟล์/โซน | ความเสี่ยง |
| --- | --- | --- | --- | --- |
| A | **S13 เลือกด่าน + `ChapterDefinition`** (P0 ของ doc 24 §15.3): SO บท/ระดับปลดล็อก/ค่า D เริ่มต้น + การ์ดใน Home ส่งค่าเข้าจอ Task Race | อยู่ฝั่งล็อบบี้ (`Game.unity`, `BobGame.Menus.cs`) ไม่แตะซีน Task Race หรือ HUD | `Game/`, `Editor/` ใหม่, `Scenes/Game.unity` | **ต่ำ** — แนะนำ |
| B | **server contract** `TASK_BEGIN`/`TASK_LOCK` + seed (Unity client สร้างไฟล์ใหม่) | อยู่ใน `server/` + ไฟล์ client ใหม่ ไม่แตะ HUD/ซีน | `server/`, `Net/` หรือ `TaskRace/ใหม่` | **กลาง** — ถ้า 4.6 เพิ่ม opcode ability ต้องประสานก่อน |
| C | **เสียงมินิเกม** (tick/สำเร็จ/พลาด ต่อ mechanic) | เพิ่ม asset + ตัวฟังเหตุการณ์ ไม่รื้อ mechanic | `unity/Assets/Audio/`, `TaskRace/BobMinigameSfx.cs` (+ hook เล็กใน host) | **กลาง** — host/HUD ใช้ร่วมกับ 4.5/4.6 |
| D | **T16 คราบฉุกเฉิน** | ต้องแก้ `TaskSpot`/`TaskRaceRound` + จุดงาน = โซน 4.3 | `TaskRace/`, `Scenes/TaskRace*.unity` | **สูง** — ควรทำหลัง 4.3 merge |
| E | **ตรวจ APK บนเครื่องจริง + sync docs** | ไม่แตะโค้ดเกม | `Builds/`, `README.md`, `art/docs/` | **ต่ำมาก** — ทำคู่กับงานอื่นได้ |

ข้อเสนอ: เริ่ม **A** ก่อน (ไม่แตะไฟล์ hot, ไม่ต้อง rebuild ซีน Task Race) แล้วค่อยหยิบ B หลังอีกฝั่งประกาศว่าไม่แตะ `server/`

---

## 7. บันทึกสถานะ (log)

- **2026-11-15 · ฝั่งเรา**: merge `origin/main` (commit `110024a` จาก session ขนาน) เข้าไลน์ P0 → merge commit `8655d11` (local, ยังไม่ push). แก้ที่เจอหลัง merge: คืน `MiniPointer`/`IsHero`/`miniDrags` ใน `BobGame.Tasks.cs` (branch เดิมคอมไพล์ไม่ผ่าน), แถบบน 124 px ชิดขวาการ์ด, แยกบรรทัด `คู่แข่ง` / `ทีมเรา %`, ผูก `BobArtLibrary` ให้ไอคอนแถวงาน, chip ปิดการ์ดย้ายมาบนบรรทัดคำใบ้. verify ครบ solo/duo/team (log `verify5-*.log`) — ยังไม่สร้าง APK ใหม่
- **2026-11-15 · อีก session**: เริ่ม 4.3 / 4.5 / 4.6 (ประกาศจาก owner) — ยังไม่มีรายละเอียดไฟล์/commit ในกระดานนี้
- **2026-11-15 · Qoder (`qoder/general-session-98ru4v`) · B**: ทำสัญญา doc 16 §8 ครบ opcode 16–21 บนแมตช์ `bob_coop` เดิม (`mode = taskrace`) → `196d1b3` + `27d4f1d`. กฎ (ตาราง 15 มินิเกม, `TaskMinMs`, สูตรประกอบ 2.2, `taskPlan`, `taskEmergencyTimes/Enemy`) อยู่ในบล็อก `TASK RACE` ของ `prototype/index.html` แล้วรัน `node build-module.js` สร้าง `modules/index.js` ใหม่ (ไม่มีโค้ดแก้มือในไฟล์ generated). `match.js`: เพิ่ม `OP` 16–21, ตรวจ BEGIN (อยู่ในรายการ/ไม่ซ้ำ/ขั้นถัดไป/ไม่ถูกล็อก), DONE (เจ้าของล็อก/ขั้นตรง/**เวลาขั้นต่ำจากนาฬิกา server**), CANCEL (ปลดล็อก เก็บขั้นที่ทำแล้ว), ส่ง `PROGRESS` แยกทีม (team + rival pct), `EMERGENCY` 2–3 จังหวะจาก seed (at 40/75/100, grace 25 วิ, enemy เดียวกันทั้งสองทีม), `TASK_LOCK`, และ `RESULT` แบบถ่วงน้ำหนัก (เสมอ = winner −1). ห้อง cart เดิมไม่เปลี่ยน (เทสต์ยืนยัน phase build + ไม่มีออปโค้ดใหม่). เทสต์ใหม่ `server/task-race-test.js` โหลด `modules/index.js` เข้า vm แล้วขับ `matchInit/matchJoin/matchLoop` ด้วย nk ปลอม = **36/36**; ฝั่ง C# `Rules/BobTaskRaceRules.cs` (mulberry32 เขียนมือ) มี `SelfTest()` เทียบค่าทอง 5 seed + enemy + MinMs — รันผ่านใน scratch net6 (เครื่องนี้มี SDK 6, `unity-check` ต้อง net8 จึงรันตรงไม่ได้) และ typecheck `dotnet build unity-typecheck` ยังเป็น 58 error ที่เป็น `CS0246/CS0234` ทั้งหมด. `Net/BobOnline.cs` ส่ง/รับ 6 ออปโค้ด + `BobServer.CreateTaskRaceMatchAsync`. **ต้องเคาะ:** `TaskRaceRound.PlanRound` เปลี่ยนมาเรียก `BobTaskRaceRules` (จึงเปลี่ยน RNG เป็น mulberry32 ให้ server ตรงกับเครื่อง) — องค์ประกอบ/น้ำหนัก/เวลาเท่าเดิม แต่ seed เดิมจะได้ชุดมินิเกมคนละชุด; ถ้า owner ไม่อยากให้เปลี่ยน ให้ revert `27d4f1d` แล้วสัญญานี้จะใช้ได้เฉพาะฝั่ง server
- **2026-11-15 · Qoder (`qoder/general-session-98ru4v`) · 4.3**: กล้อง/จุดงาน → `9b40bab` (ค่ากล้อง 6.2/5.2/0.72 + 40°, ป้ายไอคอนจุดงาน, สะพาน 1.7, น้ำตก 2.2/2.4/2.8+topY 2) แล้วเขียนค่าทับซ้ำบนซีนที่ rebuild ตอน merge `1ae7230`; ไม่แตะ `BobTaskRaceHud.cs` (PadL/แถบ 940×124 มาจาก `8655d11`). **ยังไม่มี log/รูป — ต้อง rebuild** (คิวข้อ 5 แถว 2)
- **2026-11-15 · Qoder (`qoder/general-session-98ru4v`) · 4.5**: ghost hand → `9b40bab` (`GhostSweep`/`miniGhostStroke`, ครั้งเดียวต่อเครื่อง); ไม่แตะ HUD/ซีน/`server/`; หลักฐาน = typecheck ไม่มี error นอก baseline, **ภาพ Unity ยังไม่มี** (ต้องเล่น `Game` scene)
- **2026-11-15 · Qoder (`qoder/general-session-98ru4v`) · 4.6**: specialty/ability/preview → `9b40bab`, แล้วแถบเลือกบทถูกแทนด้วยการ์ดบท+S13 ใน `067166f`; ไม่แตะ `BobTaskRaceHud.cs`, ไม่มี opcode ใหม่; หลักฐาน = typecheck + YAML asset 6 ไฟล์, **ภาพ S13/Home ยังไม่มี**
- **2026-11-15 · Qoder (`qoder/general-session-98ru4v`)**: ปิด 4.3/4.5/4.6 → commit `9b40bab` (24 ไฟล์) บนฐาน `2c9f81c`, push ขึ้น branch แล้ว. ต่อมา merge `origin/main` `76a1266` (มี `8655d11` ของฝั่งเรา) เข้า branch: ชนเฉพาะ YAML 3 ซีน → **ใช้ฉบับ rebuild ของฝั่งเราเป็นฐาน** แล้วเขียนทับค่าของ 4.3 (`BobTaskRaceCamera` 6.2/5.2/0.72, `BobBridgeDressing.width 1.7`, `BobWaterfallDressing.halfWidth 2.2/2.4/2.8 + topY 2`), `TaskSpot.cs` merge อัตโนมัติและมีครบทั้งสองฝั่ง (icon sign + `taskId`/`ForceComplete`), `TaskRaceVerify.cs` ปรับช็อตผลลัพธ์/S28 มาใช้กล้องใหม่. เจอ `IsHero` ซ้ำ 2 นิยามหลัง merge (`BobGame.Tasks.cs`) — ลบตัวที่เพิ่มมาใหม่ เหลือของฝั่งเรา. typecheck `dotnet build unity-typecheck/BobTypeCheck.csproj` = 58 error และเป็น `CS0246/CS0234` ทั้งหมด (baseline namespace เดิม; ไม่มี syntax/dup symbol). **ยังไม่เปิด Unity/ยังไม่ rebuild ซีน — ตามคิวข้อ 5 แถว 2**
- **2026-11-15 · Qoder (`qoder/general-session-98ru4v`) · A**: S13 + `ChapterDefinition` → `067166f` (จองไว้ที่ `6017d06`). ชั้นข้อมูล: `Rules/ChapterDefinition.cs` (ScriptableObject: order/title/place/unlockLevel/difficulty/roundTime/backdrop/ruleCards) + `Rules/BobChapters.cs` (โหลดจาก `Resources/Chapters` เรียงตาม order, `bob.chapter` คือ picked, `bob.band` ตามบท, `Validate()`), asset 6 ไฟล์ `Chapter1_RentalRoom … Chapter6_Market` (LV 1/6/13/21/31/41, D1/D1/D2/D2/D3/D3, 120 วิ, การ์ดกติกาข้อ 15.1) และเมนู `Bob/Chapters/Check Ladder` (`Editor/BobChapterCheck.cs`). UI: Home เปลี่ยนชิป "Task Race" เป็นการ์ดบท (ภาพ/ชื่อ/D/เวลา/ดีสุด) เปิด S13 เป็นตารางการ์ด 3×2 — การ์ดล็อกมี LV และฉากมืด, แตะแล้วบันทึก, ปุ่ม "เริ่มด่านนี้" โหลดซีนเดิม. ฝั่งเกม: `TaskRaceModeManager.Awake` อ่านบทเดียวกันแล้ว `ConfigureRound(players, D, seed)` + `matchDuration` **ก่อน** `TaskRaceRound.Start` วาดรายการ (สูตรสุ่มยังอยู่ที่ `BobTaskRaceRules`). ตรวจแล้ว: `dotnet build unity-typecheck` = 58 error baseline (`CS0246/CS0234` เท่านั้น), asset ทั้ง 6 ผ่านการอ่านแบบ YAML + กติกา order/D/level, `node server/task-race-test.js` ยัง 36/36. **ยังไม่เปิด Unity**: หน้าตา S13/การ์ด Home, การโหลด asset 6 ไฟล์ และค่า D ที่เข้าซีน ต้องดูใน Unity — ค้างในคิวข้อ 5 แถว 2 พร้อม rebuild ซีน. ที่ยังไม่ทำ: บังคับใช้การ์ดกติกา (P2), แผนที่หมุด/ดาว + skin ฉาก (P1), `roundTime` 100 วิ (P3), ผูก `BobOnline.CreateTaskRaceAsync` ให้ส่ง `difficulty` ของบทตอนต่อเน็ต- **2026-11-15 · Qoder (`qoder/general-session-98ru4v`) · push + ปิดช่อง `spot`**: push branch ขึ้น remote แล้ว (`e050ef5` → `854f295`, ตรวจด้วย `git ls-remote` = `854f295`). ต่อมาปิดช่องที่ค้างในสัญญา doc 16 §8: `TASK_BEGIN` ตรวจ `spot` ว่าเป็นสถานีของฝั่งผู้เล่น (`/^S<n>_[AB]$/` + ทีมตรง) และกันสองคนถือบัตรคนละใบที่สถานีเดียวกัน โดยเก็บ `spot` ไว้ในตัวล็อก (ไม่ต้องมีตารางจุดงานและไม่ต้องมีที่ให้ล็อกค้าง) → `server/match.js` + `server/modules/index.js` (regen ด้วย `node server/build-module.js`) + `server/task-race-test.js` เพิ่ม 3 เคส = **39/39**. **เคาะ RNG ตามที่เสนอ: เก็บ `27d4f1d`** (mulberry32 ให้ client/server วาดชุดเดียวกัน). ที่ยังปิดไม่ได้จาก sandbox นี้: rebuild/verify 3 ซีน (ไม่มี Unity — คิวข้อ 5 แถว 2), P1 skin ฉาก, P2 บังคับใช้การ์ดกติกา, และ `CreateTaskRaceAsync` ยังไม่มีผู้เรียก (ตอนต่อเน็ตต้องส่ง `difficulty` ของบทไปด้วย)
---

## 8. วิธีอัปเดตกระดาน (สั้น ๆ)

1. ก่อนเริ่ม: เพิ่มแถวในหัวข้อ 3 + (ถ้าใช้) จองคิวในหัวข้อ 5 → commit แยก `docs(coord): claim <งาน>`
2. ระหว่างทำ: ถ้าแตะไฟล์นอกเหนือจากที่จองไว้ ให้แก้แถวเดิม ไม่ต้องเพิ่มแถวใหม่
3. จบงาน: อัปเดตสถานะ + commit hash + log/รูป → merge เข้า `main` ของตัวเอง
4. งานใหม่ที่ยังไม่มีเจ้าของ: เพิ่มในหัวข้อ 6 พร้อมประเมินความเสี่ยงชน
