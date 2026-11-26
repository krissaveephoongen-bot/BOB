# 33 · เทียบเว็บเดโม Godot กับเกม Unity ใน repo (26 พ.ย. 2026)

> **คำถามที่ตอบ:** เว็บเดโม `https://bobcleanup-bsn8guby.manus.game` มีอะไรบ้าง เทียบกับ Unity ใน repo แล้วขาด/เกินอะไร
> และ **คุ้มไหมที่จะเอาไปใช้ต่อ**
>
> **วิธีตรวจ (พูดตรง ๆ):** ฝั่งเดโมตรวจจากตัวไฟล์ที่ปล่อยจริง — โหลด `index.wasm` (8.3 MB) + `index.pck` (7.7 MB)
> แล้วอ่าน inventory + ตาราง i18n/tuning ที่ฝังใน pack; **ยังไม่ได้เล่นด้วยตา** (sandbox ไม่มี browser และ personal browser
> ปิดในเซสชันนี้). ฝั่ง Unity ตรวจจากโค้ด + docs 11/30 ใน repo commit `6a1c686` **ไม่ใช่การรันใน Unity**

## 0. สรุปสั้น

| คำถาม | คำตอบ |
| --- | --- |
| เดโมคืออะไร | Godot **4.7.2** export เป็น WebAssembly · **GDScript ล้วน** (24 สคริปต์ `.gd` คอมไพล์เป็น `.gdc`, ไม่มี `.dll`/mono) |
| เกมเพลย์ | อารีนาเก็บโหนด: วิ่งเก็บ **energy node** ให้ครบ หลบ **sentinel** · มีระบบชีวิต (`LIVES`) · พาวเวอร์อัป 4 อย่าง · **2 ด่าน** |
| เป็นเกมเดียวกับ repo ไหม | **ไม่** — ลูปเกมไม่ใช่รถเข็น 5 ชั้น + Task Race; ไม่มีบท/ดาว/ทีม/ออนไลน์/เดลี่/ของแต่ง |
| ใช้ของจาก repo ไหม | ใช่บางส่วน — สไปรต์ฮีโร่ `press_*` / `sweep_*` (idle/walk/work/cheer) และ `bg_classroom_0` มีอยู่ใน `unity/Assets/Art` |
| เอาไปใช้ต่อได้ไหม | **ใช้เป็นเดโม/แนวทาง UX ได้ แต่ต่อยอดเป็นเกมเต็มไม่ได้** — เหตุผลใน §4 |

## 1. ตัวเดโมมีอะไรแน่ ๆ (จากไฟล์ที่ปล่อย)

- **หน้า/การไหล:** Title → Tutorial (3 ใบ: หาทิศ / ตามพลังงาน / เปลี่ยนโมเมนตัม) → HUD (`SCORE / STAGE n/2 / LIVES`) →
  Pause (`Resume / Restart stage / Game tuning / Return to title`) → Result (`Run complete` / `Game over` /
  `Stage n complete`, `Save score`) → Leaderboard
- **ลูป:** เก็บโหนดให้ครบเพื่อผ่านด่าน, หลบ sentinel, 2 ด่าน (`First circuit`, `Second circuit`)
- **พาวเวอร์อัป 4:** Bolt (ปิด sentinel), Shield (กันhit), Hourglass (หน่วง sentinel), Magnet (ดูดโหนด)
- **บอร์ด:** **local เท่านั้น** — ข้อความในเกมเองบอก `Saved on this device.` และ
  `Practice result · {marker} · local standings only` ⇒ ไม่มีคลาวด์/บัญชีจริง
- **มี `tuning` panel 73 ค่า** (HUD opacity, reduced motion, player/sentinel speed, lives, filter, particles, zoom, audio 4 บัส)
  โหมด `LIVE / NEXT ACTION / NEXT STAGE / NEXT RUN`, มี `SYNC TO SANDBOX` ⇒ เป็น **เครื่องมือ dev ของ template**
- **i18n 151 คีย์** (en + zh) — ชื่อเกมไทยมีแค่ 4 สตริง (`สมรภูมิปัดกวาด`, `เกมทำความสะอาดห้องเรียนร่วมมือ`, `คน`, `เครื่อง`)
- **โครงสคริปต์:** `scripts/bob/{bob_game,bob_world,bob_sim,bob_net,bob_cloud,bob_data,bob_fx,bob_sfx,bob_role}`,
  `game/{maze_world,stage_catalog}`, `entities/{runner,sentinel}`, `ui/{screen_factory,menu_list,arena_frame,title_art,tutorial_callout}`,
  `score/run_score_service`, autoload `{audio_director,i18n,save_store,tuning_store}`, `scripts/manus/*` (โครงของ Manus)
- **ร่องรอยว่าเป็น template ที่เอามาเปลี่ยน skin:** `app.title = Generic Game Template`,
  `tagline = Collect every node. Find your flow.` ⇒ ลูป "เก็บโหนด" คือลูปของ template ไม่ใช่ของ BOB

## 2. เทียบทีละด้าน

| ด้าน | Unity ใน repo | เว็บเดโม Godot |  judgement |
| --- | --- | --- | --- |
| ภาษา/สแตก | C# 29,867 บรรทัด (Rules 16 ไฟล์, TaskRace 40, Game 18, UGS 9) | GDScript ~24 ไฟล์ | เดโมต่อยอดเป็นเกม C# ไม่ได้; C# ของ Godot ก็ export web ไม่ได้ (docs 26 §0.4) |
| ลูปหลัก | Task Race บนเกาะ: ใบงานทีม + คู่แข่งจำลอง + 4 สถานี × 3 ขนาดเกาะ | เก็บโหนดหลบศัตรู 2 ด่าน | **คนละเกม** ไม่มีอะไรใช้ร่วมนอกจากชื่อ |
| รถเข็น T04 | 5 ชั้น + 2 วิธี, งบ 100, เพดานค่า, ดาวคุณภาพ, ตัวเลขตรง C#↔JS↔prototype | ไม่มี | เดโมไม่ได้แตะระบบที่ลงทุนมากที่สุด |
| เนื้อเรื่อง | 6 บท × 3 ยก, ดาว 1–3, บันไดปลด, คำพูดบนการ์ด (docs 29) | ไม่มีบท/เรื่อง | เดโมไม่มีชั้นเนื้อหาเลย |
| โหมด | Solo / Duo / Team (ออฟไลน์, เพื่อนบอท), Story series | เดี่ยวเท่านั้น | เดโมไม่มีทีม |
| ออนไลน์ | UGS Relay/Lobby 2–4, host-authoritative, `bob.*`, best-of-3, คราบ sync | `bob_net.gd` มีอยู่ แต่บอร์ดเป็น local | เดโมยังไม่ได้พิสูจน์ออนไลน์จริง |
| เดลี่/บอร์ด | server seed + `daily_<day>` + การ์ดโชว์ top 5 + Cloud Save ต่อบัญชี | local leaderboard (`Saved on this device`) | ขาดทั้งระบบ |
| ของแต่ง/เศรษฐกิจ | เหรียญ/XP/เลเวล + ตารางปลด 6 ชิ้น + ตู้เสื้อผ้า 4 แท็บ + ตัดเย็บ + กระเป๋า | ไม่มี | ขาดทั้งระบบ |
| UI | 20+ หน้าจอ (S00–S33 ที่ทำแล้ว) สร้างด้วยโค้ด uGUI | 6 หน้า (title/tutorial/HUD/pause/result/leaderboard) | เดโมบางกว่า 3–4 เท่า |
| เครื่องมือ dev | `Editor/` 4.9k บรรทัด (scene builders + checks), `unity-check`, `ready-check` | tuning panel 73 ค่า + `run_score_service` | **จุดที่เดโมทำได้ดีกว่า**: tuning runtime |
| i18n | ไทยเป็นหลัก, ข้อความฝังในโค้ด | 151 คีย์ en/zh + scaffold เปลี่ยนภาษา | เดโมออกแบบ i18n ดีกว่า (แต่ยังไม่มีไทยจริง) |
| อาร์ต/เสียง | `Art/` 57 MB, audio 38 คลิป + 4 MP3, 3D ตัวละคร/เกาะ | สไปรต์ชุดเดียวกับ repo + bg 1 + ฟอนต์ Sora/Figtree/Noto | เดโมยืมของ repo ไปใช้ (ไม่ใช่ของใหม่) |
| การปล่อย | APK v2 `0.1.0 (2)` บน Firebase (ยังไม่ทดสอบเครื่องจริงรอบล่าสุด) | **เว็บ-deploy จริง เปิดได้จากทุกเบราว์เซอร์** | จุดที่เดโมชนะชัดเจน: แจกจ่าย |

## 3. ของที่เดโม "มี" แต่ repo ยังไม่มี

1. **ช่องทางเว็บที่แชร์ได้ทันที** — ลิงก์เดียว ไม่ต้องติดตั้ง (repo ยังมีแค่ APK + `prototype/index.html` แบบออฟไลน์)
2. **tuning panel แบบ runtime 73 ค่า** พร้อมแนวคิด `LIVE / NEXT ACTION / NEXT STAGE` และการกันไม่ให้ run ที่ปรับค่าไปขึ้นบอร์ด
   (`marks the run as practice`) — แนวคิดนี้ย้ายมา Unity ได้ตรง ๆ และมีประโยชน์กับ owner ตอนจูน
3. **i18n แบบคีย์** (151 คีย์) + สลับภาษาบนหน้าเมนู — repo ยังฝังข้อความไทยในโค้ด
4. **loading/retry UI ของเว็บ** (`กำลังโหลด…`, ปุ่ม `ลองใหม่` เมื่อ asset โหลดไม่ครบ) — บทเรียนตรงกับปัญหาที่ repo เจอตอน UGS ล่ม

## 4. คุ้มไหมที่จะเอาไปใช้ต่อ — ข้อสรุป

**ใช้เป็น "เดโมโชว์" ได้ แต่ใช้เป็นฐานทำเกมต่อไม่ได้** เหตุผล:

1. **คนละลูปเกม** — สิ่งที่ลงทุนหนักที่สุดของ repo (รถเข็น 5 ชั้น, Task Race, ดาว/บท, ซีรีส์ออนไลน์) ไม่มีอยู่ในเดโมเลย
   ⇒ ถ้าจะทำต่อจากเดโม ต้องเขียนใหม่เกือบทั้งหมด แล้วทิ้งงาน Unity ที่ผ่านด่านไปแล้ว 29.9k บรรทัด
2. **ภาษาปิดทาง** — เดโมเป็น GDScript; docs 26 สรุปไว้ว่า Godot 4.7 ยังให้ C# บนมือถือเป็น experimental และ **C# export web ไม่ได้**
   ⇒ จะได้ทั้ง C# (มือถือ) และเว็บในเอนจินเดียวไม่ได้ ถ้าดูเดโมเป็น "อนาคต" จะต้องเลือกอย่างใดอย่างหนึ่ง
3. **ความเสี่ยงกติกาสองชุด** — repo เตือนไว้แล้ว (docs 24) ว่าตัวเลขต้องมีที่เดียว; เดโมมีลูป/คะแนน/พาวเวอร์อัปของตัวเอง
   ⇒ ถ้าปล่อยให้คนเล่นทั้งสอง จะกลายเป็นคนละเกมชื่อเดียวกัน
4. **สิ่งที่ได้จริงมี 4 อย่าง (ใน §3)** — และ 3 อย่างย้ายมา Unity ได้ถูกกว่า: tuning ที่อ่านค่าจาก `PlayerPrefs`/`ScriptableObject`,
   i18n แบบคีย์, หน้า loading/retry ที่บอกสถานะชัด

**คำแนะนำ (เลือก 1 ใน 3):**

- **ก. (แนะนำ) เก็บเดโมเป็นลิงก์โชว์** — ใส่ใน docs/README ว่า "เว็บเดโม Godot 4.7.2 (GDScript, template reskin)" ไม่ใช่ build ของเกมนี้
  แล้ว **ยก 3 ไอเดียใน §3 กลับมา Unity** เป็นงานเล็ก (tuning + i18n key + loading/retry) — ได้ประโยชน์โดยไม่แตะ engine
- **ข. ถ้าอยากเทสต์ Godot จริง** — ทำ **spike 10 วันตาม docs 26 §8** (1 เกาะ + HUD + 2 มินิเกม + APK) ด้วย **C#** เท่านั้น;
  เดโมนี้ใช้เป็น reference ด้าน UX ไม่ใช่ base code
- **ค. ถ้า owner ต้องการเว็บเป็นช่องทางหลัก** — ต้องยอมรับว่าเป็นคนละผลิตภัณฑ์: GDScript + 2 ด่าน + local board
  และต้องตัดสินใจเรื่องคลาวด์/บัญชีใหม่ทั้งหมด (UGS ใช้กับ Godot ไม่ได้ตรง ๆ)

> ข้อควรระวังด้านสิทธิ์/ที่มา: เดโมใช้สไปรต์ `press_*`/`sweep_*` และ `bg_classroom_0` ชุดเดียวกับ `unity/Assets/Art`
> — เป็นอาร์ตของโปรเจกต์เองจึงใช้ได้ แต่หมายความว่าเดโม **ไม่ได้เพิ่มมูลค่าอาร์ตใหม่** และควรบันทึกว่าใครเป็นผู้สร้างบิลด์นี้

## 5. ภาคผนวก — หลักฐานที่ตรวจ

- `GET /` → 200, `text/html`, 245,833 ไบต์, `<title>BOB — สมรภูมิปัดกวาด</title>`
- `index.wasm` → 200 `application/wasm` 8,300,310 ไบต์ · `index.pck` → 200 `application/octet-stream` 7,668,528 ไบต์ (ทั้งคู่ผ่าน 1 redirect)
- pack header: `GDPC` v4 · Godot **4.7.2** · ไม่พบ `.dll`/mono ⇒ GDScript
- i18n/tuning catalog 151 คีย์ (groups: `tuning` 73, `tutorial` 14, `result` 12, `hud` 9, `pause` 5, `confirm` 5, `leaderboard` 4, `menu` 4 …)
- สคริปต์ที่อ้างใน pack: `scripts/bob/*` (9), `game/*` (2), `entities/*` (2), `ui/*` (5), `score/*` (1), `autoload/*` (4), `scripts/manus/*` (6)
- ข้อความที่ยืนยันว่าเป็น template: `app.title = Generic Game Template`, `app.subtitle = PRECISION. IN MOTION.`
