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
| `main` (local, ถึง `8f17881`) | Claude (session นี้) | **P3: `StorySeries` 3 รอบ best-of-3 เฉพาะโหมดเนื้อเรื่อง** (doc 24 §15.3 P3) — `TaskRaceModeManager` เพิ่ม API, ไฟล์ใหม่ `TaskRace/StorySeries.cs` + ป้ายบอกรอบ; ไม่แตะ layout HUD/ซีน | 🟡 กำลังทำ P3 | 15 พ.ย. 2026 |
| _TBD (branch ของอีก session)_ | session ขนาน | **4.3** กล้อง/จุดงาน · **4.5** ghost hand onboarding · **4.6** specialty/ability/preview | 🟡 กำลังทำ | 15 พ.ย. 2026 |
| `origin/main` | remote | merge แล้วถึง `8655d11` ไหม? — ยังไม่ push จากฝั่งเรา | — | 15 พ.ย. 2026 |

---

## 3. งานที่ถูกจอง (claims)

สถานะ: `⏳ จองแล้ว` · `🟡 กำลังทำ` · `✅ เสร็จ/merge แล้ว` · `⏸ หยุดกลางคัน` · `❌ ยกเลิก`

| ID | งาน | เจ้าของ | Branch | สถานะ | ไฟล์/ซีนที่แตะ | อัปเดต / โน้ต |
| --- | --- | --- | --- | --- | --- | --- |
| 4.3 | กล้อง / จุดงาน (camera + station points) | อีก session | TBD | 🟡 กำลังทำ | `TaskRace/BobTaskRaceCamera.cs`, `TaskRace/TaskSpot.cs`, `Editor/TaskRaceSceneBuilder.cs`, `Scenes/TaskRace*.unity` | 15 พ.ย. — ฝั่งเราไม่แตะไฟล์เหล่านี้ระหว่างนี้ |
| 4.5 | ghost hand onboarding (สอนด้วยการทำ) | อีก session | TBD | 🟡 กำลังทำ | `TaskRace/BobTaskRaceHud.cs`, `TaskRace/BobTaskRaceTouch.cs`, `docs/11_UX_UI_Spec.md` | 15 พ.ย. — HUD ถูกใช้ร่วม: ดูข้อ 4 |
| 4.6 | specialty / ability / preview | อีก session | TBD | 🟡 กำลังทำ | `TaskRace/BobTaskRaceHero.cs`, ปุ่ม ability ใน HUD, ตารางฮีโร่ `Rules/BobMatch.cs`, อาจมี opcode ใหม่ใน `server/` | 15 พ.ย. — ถ้าจะแตะ `server/` ให้ประกาศก่อน (ฝั่งเรามีงาน server รออยู่) |
| — | merge + HUD layout/icons | ฝั่งเรา | `main` local | ✅ `8655d11` | `TaskRace/BobTaskRaceHud.cs`, `TaskRace/TaskRaceModeManager.cs`, `Game/BobGame.Tasks.cs`, `Editor/TaskRaceSceneBuilder.cs`, `Scenes/TaskRace*.unity` | 15 พ.ย. — verify ครบ solo/duo/team, ยังไม่ push |
| A | S13 เลือกด่าน + `ChapterDefinition` (P0 doc 24 §15.3) | ฝั่งเรา | `main` | ✅ `8f17881` | `Game/ChapterDefinition.cs`, `Editor/BobChapterBuilder.cs`, `Resources/Chapters/*`, `Game/BobGame.Menus.cs`, `Scenes/Game.unity` (ไม่ต้อง rebuild), `TaskRace/TaskRaceRound.cs` | 15 พ.ย. — verify ด้วย `BobGameScene.RunTest`: `GAME TEST PASSED`, ไม่มี `error CS`, รูป `Logs/bob-game-m3-home.png` + `m4b-stages.png` |
| P3 | **`StorySeries` 3 รอบ best-of-3** เฉพาะโหมดเนื้อเรื่อง: รอบละ 100 วิ, ชนะ 2 ใน 3, แพ้ติด 2 ให้เวลา +15 วิ รอบถัดไป, ชนะขาด ≥3 งานเสนอด่านท้าทาย (PvP คง 120 วิ รอบเดียว) | ฝั่งเรา | `main` | 🟡 กำลังทำ | `TaskRace/TaskRaceModeManager.cs` (เพิ่ม API/ฟิลด์ใหม่ ไม่เปลี่ยนพฤติกรรมเดิม), ไฟล์ใหม่ `TaskRace/StorySeries.cs`, `TaskRace/BobSeriesBanner.cs`; **ไม่แตะ** `BobTaskRaceHud.cs` และไม่ rebuild ซีน | 15 พ.ย. — ถ้าจำเป็นต้องเติม pips ใน HUD จะประกาศในหัวข้อ 3 อีกครั้งก่อนแก้ |

---

## 4. ไฟล์/โซนเสี่ยงชน (hot files) และวิธีแบ่ง

| ไฟล์/โซน | ใครต้องใช้ | ข้อตกลง |
| --- | --- | --- |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceHud.cs` | 4.5 ghost hand, 4.6 ปุ่ม ability/preview, S28 stage ของฝั่งเรา, งาน HUD อื่น ๆ | **ไฟล์ร้อนที่สุด** — แก้แบบเพิ่มของใหม่ (ฟังก์ชัน/ฟิลด์แยก) อย่ารื้อ layout; ถ้าต้องเปลี่ยนค่าจัดหน้า (PadL, แถบบน, แถวงาน, การ์ด) ให้ประกาศในหัวข้อ 3 ก่อน. สถานะปัจจุบัน (15 พ.ย.): แถบบน 940×124 ชิดขวาการ์ด, แถวงานมีไอคอน + ไฮไลต์งานถัดไป, chip ปิดการ์ดอยู่บนบรรทัดคำใบ้, `BindArt` ผูก `BobArtLibrary` |
| `unity/Assets/Scripts/TaskRace/BobMinigameHost.cs` | 4.5 (สอนในพื้นที่เล่น), 4.6 (ตัวช่วยตามถนัด/preview), S28 3D stage + เสียงมินิเกมของฝั่งเรา | แก้แบบเพิ่ม mechanic/ของตกแต่ง อย่าเปลี่ยนสัญญา `Begin/Tick/Cancel/Progress` โดยไม่แจ้ง |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceTouch.cs` | 4.5 (มือผี/ไฮไลต์นิ้ว) | ประกาศก่อนแก้พื้นที่รับสัมผัสหรือ dead zone |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceHero.cs` | 4.3 (กล้องตาม), 4.6 (ability) | ถ้าทั้งสองฝั่งต้องใช้ ให้แยกคนละฟังก์ชัน/คนละ field |
| `unity/Assets/Scripts/TaskRace/TaskSpot.cs` | 4.3 (จุดงาน), T16 คราบฉุกเฉิน | T16 รอจน 4.3 merge ก่อน |
| `unity/Assets/Scripts/TaskRace/TaskRaceRound.cs`, `TaskRaceModeManager.cs` | seed/งาน/ability/server/ผลแมตช์ | เพิ่ม API ใหม่ได้ แต่ห้ามเปลี่ยนพฤติกรรมเดิม (แต้ม/เวลา/รางวัล) โดยไม่แจ้ง |
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
| A | ~~**S13 เลือกด่าน + `ChapterDefinition`**~~ ✅ `8f17881` — เสร็จ 15 พ.ย. 2026 | — | — | — |
| B | **server contract** `TASK_BEGIN`/`TASK_LOCK` + seed (Unity client สร้างไฟล์ใหม่) | อยู่ใน `server/` + ไฟล์ client ใหม่ ไม่แตะ HUD/ซีน | `server/`, `Net/` หรือ `TaskRace/ใหม่` | **กลาง** — ถ้า 4.6 เพิ่ม opcode ability ต้องประสานก่อน |
| C | **เสียงมินิเกม** (tick/สำเร็จ/พลาด ต่อ mechanic) | เพิ่ม asset + ตัวฟังเหตุการณ์ ไม่รื้อ mechanic | `unity/Assets/Audio/`, `TaskRace/BobMinigameSfx.cs` (+ hook เล็กใน host) | **กลาง** — host/HUD ใช้ร่วมกับ 4.5/4.6 |
| D | **T16 คราบฉุกเฉิน** | ต้องแก้ `TaskSpot`/`TaskRaceRound` + จุดงาน = โซน 4.3 | `TaskRace/`, `Scenes/TaskRace*.unity` | **สูง** — ควรทำหลัง 4.3 merge |
| E | **ตรวจ APK บนเครื่องจริง + sync docs** | ไม่แตะโค้ดเกม | `Builds/`, `README.md`, `art/docs/` | **ต่ำมาก** — ทำคู่กับงานอื่นได้ |

ข้อเสนอ: เริ่ม **A** ก่อน (ไม่แตะไฟล์ hot, ไม่ต้อง rebuild ซีน Task Race) แล้วค่อยหยิบ B หลังอีกฝั่งประกาศว่าไม่แตะ `server/`

---

## 7. บันทึกสถานะ (log)

- **2026-11-15 · ฝั่งเรา — A เสร็จ (`8f17881`)**: `ChapterDefinition` 6 บท (ช01–ช06, ปลดที่ระดับ 1/6/13/21/31/41, แบนด์ D1/D1/D2/D2/D3/D3) + `Chapters` loader (`bob.chapter`) + จอ S13 (`Phase.Stages`) + การ์ดบทใน Home + `TaskRaceRound.Start` ใช้แบนด์ของบท (builder serialized ยังเป็น fallback). verify: `unity/Logs/game-test-stages.log` → `GAME TEST PASSED`, ไม่มี `error CS`; รูป `bob-game-m3-home.png`, `bob-game-m4b-stages.png`. แก้ที่เจอจากรูป: `▸` เป็นกล่อง (font ไม่มี glyph) เอาออก, ข้อความระดับด้านล่างเปลี่ยนเป็น Ink + outline ให้อ่านออกบนฉากหลังสว่าง. ยังไม่ push
- **2026-11-15 · ฝั่งเรา — เริ่ม P3**: รอบเนื้อเรื่อง 3 รอบ best-of-3 (รอบละ 100 วิ), แพ้ติด 2 → +15 วิ, ชนะขาด ≥3 งาน → เสนอด่านท้าทาย. ทำใน `TaskRaceModeManager` แบบเพิ่ม API (`storySeries`, `LastRound`, event `RoundEnded`) โดย `EndMatch` เดิมยังเป็นการจบแมตช์/ใช้โดย `DebugShowResult` ของ verify; PvP ปิดสวิตช์นี้แล้วได้พฤติกรรมเดิม 120 วิ
- **2026-11-15 · ฝั่งเรา**: merge `origin/main` (commit `110024a` จาก session ขนาน) เข้าไลน์ P0 → merge commit `8655d11` (local, ยังไม่ push). แก้ที่เจอหลัง merge: คืน `MiniPointer`/`IsHero`/`miniDrags` ใน `BobGame.Tasks.cs` (branch เดิมคอมไพล์ไม่ผ่าน), แถบบน 124 px ชิดขวาการ์ด, แยกบรรทัด `คู่แข่ง` / `ทีมเรา %`, ผูก `BobArtLibrary` ให้ไอคอนแถวงาน, chip ปิดการ์ดย้ายมาบนบรรทัดคำใบ้. verify ครบ solo/duo/team (log `verify5-*.log`) — ยังไม่สร้าง APK ใหม่
- **2026-11-15 · อีก session**: เริ่ม 4.3 / 4.5 / 4.6 (ประกาศจาก owner) — ยังไม่มีรายละเอียดไฟล์/commit ในกระดานนี้

---

## 8. วิธีอัปเดตกระดาน (สั้น ๆ)

1. ก่อนเริ่ม: เพิ่มแถวในหัวข้อ 3 + (ถ้าใช้) จองคิวในหัวข้อ 5 → commit แยก `docs(coord): claim <งาน>`
2. ระหว่างทำ: ถ้าแตะไฟล์นอกเหนือจากที่จองไว้ ให้แก้แถวเดิม ไม่ต้องเพิ่มแถวใหม่
3. จบงาน: อัปเดตสถานะ + commit hash + log/รูป → merge เข้า `main` ของตัวเอง
4. งานใหม่ที่ยังไม่มีเจ้าของ: เพิ่มในหัวข้อ 6 พร้อมประเมินความเสี่ยงชน
