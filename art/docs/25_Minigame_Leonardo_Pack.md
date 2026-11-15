# 25 · Minigame art pack จาก Leonardo.ai (15 พ.ย. 2026)

ชุดภาพ/วิดีโอสำหรับมินิเกม S28 ที่อ้างอิง 9 มินิเกมของ Among Us (ผู้ใช้ส่งภาพ tutorial deck มา 9 ใบ) แล้วแปลงเป็นงานของเรา
มีสองส่วนที่ **ต่างกันโดยเจตนา**:

1. **ของที่ใช้ในเกมจริง** — ต้องมี alpha, ต้องไม่มีตัวอักษรในภาพ และต้องโหลดได้จาก `Resources` (ข้อ 2, ข้อ 6)
2. **ของที่ใช้เป็นต้นแบบเท่านั้น** — ภาพฉากและคลิปวิดีโอ ใช้ดู motion/การวางเลย์เอาต์ **ไม่เข้า build** (ข้อ 3, ข้อ 4)

> **สถานะ 15 พ.ย. 2026:** VFX 6 ไฟล์จากชุด RAW เดิมถูกตัดพื้นหลังและใช้อยู่ในเกมแล้ว (ข้อ 7) ส่วนชุดสร้างใหม่ (ข้อ 2–4) พร้อมรัน แต่ **ยังไม่ได้รันเพราะ session นี้ไม่มี `LEONARDO_API_KEY`** — tool จะหยุดทันทีพร้อมบอกชื่อ env var (ข้อ 5)

---

## 1. การแมป 9 มินิเกม → งานของเรา

| # | Among Us (ภาพที่ผู้ใช้ส่ง) | งานของเรา | สิ่งที่ต้องเพิ่ม |
| --- | --- | --- | --- |
| 1 | Fix Wiring — ลากเส้นจากจุดบนลงจุดล่าง *ถอยกลับไม่ได้* | **T01 กวาดตามทาง** | `Wire`: ลากผ่านจุดเรียงลำดับ, หลุดเส้น = เริ่มใหม่ทั้งเส้น |
| 2 | Vending keypad — กดรหัสตามโพย A1–C4 | **T15 ซักผ้า ขั้น 2** | `Keypad`: ช่องรหัส + ปุ่ม 3×4 + ✓/✗ (ตัวอักษร/ตัวเลขวาดด้วย TMP) |
| 3 | MedBay — เติมหลอด → รอ 60 วิ → กลับมาเลือกหลอดแดง | **T09 แช่คราบรอ** | `Pour` (ลากลง + กดค้างจนหมด) → รอ → **แตะเลือกคราบแดง** |
| 4 | Comms — กดเลข 5 หลักตามเวลา แล้วยืนยัน | **T15 ขั้น 2** (ตัวเดียวกับข้อ 2) | `Keypad` มีตัวจับเวลาในชุดเดียวกัน |
| 5 | Trash — ลากลงแล้วกดค้างจนเกจหมด | **T09 ขั้น 1** | `Pour`: เกจน้ำยาไหลจนหมดขณะกดค้างในอ่าง |
| 6 | แยก artifact ลง 3 กล่องตามประเภท | **T05 แยกขยะ** | มีแล้ว (ป้าย 3 ถัง + ผิดถังบินกลับ) — ใช้ `mini_bin_*` เป็นภาพถัง |
| 7 | Engine — ลากเข็มให้ตรงเส้นประ | **T06 ขั้น 2** | `Align`: หมุนเข็มให้ตรงร่องเส้นประ ±7° |
| 8 | Insert keys — หยิบกุญแจเสียบช่อง แล้วหมุนหนึ่งที | **T06 ขั้น 1** | `Insert`: ลากผ้าเข้าช่องรีด (ใช้ `Sort` เป้าเดียวได้) |
| 9 | Dropship — ลากยานตามเส้นหยัก | **T14 ล้างพื้นทั้งแถว** | ใช้ `Wire` แบบเส้นหยัก 3 แถว |

งานที่ **ไม่ต้องเพิ่ม mechanic** เพราะมีครบแล้ว: T02/T07/T08 (scrub/tap), T03 (tap), T10 (tap timed), T11 (fill), T13 (hold co-op), T15 ขั้น 1 (sequence)

---

## 2. ชุด A — prop cutouts 15 ชิ้น (ใช้ในเกมจริง)

Transparency On · 1:1 · พื้นหลังโปร่งใส · **ห้ามมีตัวอักษรหรือตัวเลขในภาพ** (การ์ดวาดข้อความด้วย TMP ทั้งหมด เพื่อกัน AI เขียนผิด)

| ไฟล์ | ใช้ที่ไหน | ปลายทางใน Unity |
| --- | --- | --- |
| `mini_bin_blue`, `mini_bin_grey`, `mini_bin_yellow` | T05 ถัง 3 ใบ (ขวด / กระดาษ / เศษ) | `Textures/Minigame/` → `BobMinigameHost.SetupSort` |
| `mini_keypad_panel` | T15 ขั้น 2 แผงปุ่ม | `BobMinigameHost` kind `Keypad` |
| `mini_key_square` | ปุ่มแต่ละปุ่ม (12 ปุ่ม) | kind `Keypad` |
| `mini_slip_paper` | ใบโพยรหัสเหนือแผง | kind `Keypad` |
| `mini_dial_base`, `mini_dial_needle` | T06 ขั้น 2 หน้าปัด + เข็ม | kind `Align` |
| `mini_cloth_wet` | T06 ขั้น 1 ผ้าที่ลากเข้าช่องรีด | kind `Insert` |
| `mini_bottle_pour`, `mini_basin` | T09 ขั้น 1 ขวด + อ่าง | kind `Pour` |
| `mini_stain_red`, `mini_stain_grey` | T09 ขั้น 3 เลือกคราบที่อ่อนตัว | kind `Tap` (เป้าเดียว) |
| `mini_node_mark` | T01/T14 จุดบนพื้น | kind `Wire` |
| `mini_trail_dust` | T01/T14 รอยฝุ่นเส้นหยัก | kind `Wire` |

## 3. ชุด B — scene refs 9 ภาพ (ต้นแบบเท่านั้น)

16:9 · ไม่มีคน · ไม่มีตัวอักษร · เก็บที่ `art/incoming/minigames/refs/` ใช้อ้างอิงตอนจัดเลย์เอาต์/แสง/สี เทียบกับ `Logs/taskrace-s28-*.png`

`ref_wire_trail` (T01) · `ref_sort_bins` (T05) · `ref_wring_insert` (T06-1) · `ref_dial_align` (T06-2) · `ref_soak_select` (T09) · `ref_fill_band` (T11) · `ref_keypad` (T15-2) · `ref_washer_pads` (T15-1) · `ref_mop_path` (T14)

## 4. ชุด C — motion clips 4 คลิป (ต้นแบบเท่านั้น)

คลิป 5 วิ สร้างต่อจากภาพ ref ผ่าน `POST /generations-image-to-video` (Motion 2.0):

| คลิป | ต่อจาก | ดูอะไร |
| --- | --- | --- |
| `V1_trail` | `ref_wire_trail` | ความเร็วการลากตามเส้น + จังหวะตอนเริ่มใหม่ |
| `V2_sort` | `ref_sort_bins` | ใบขยะลอยเข้าถัง + ถัง punch + ดาวป๊อป |
| `V3_keypad` | `ref_keypad` | ปุ่มยุบตอนกด + ตัวเลขขึ้นจอ + ปุ่มยืนยันติด |
| `V4_fill` | `ref_fill_band` | น้ำขึ้นถึงแถบเขียว + แถบกระพริบ + สแปลช |

**ไม่เอา mp4 เข้า build:** การ์ดมีขนาด 1400×860u และต้องเปิด/ปิดใน 180 ms — `VideoPlayer` จะเพิ่มน้ำหนัก build หลาย MB ต่อคลิป โดยได้ภาพที่เราทำเองด้วย uGUI + sprite ได้คมกว่า; คลิปใช้เป็น reference ให้คนทำ/ตรวจ motion เท่านั้น (ตรงกับที่เอกสาร 24 §15.11 ทำไปแล้วด้วย ring/pop/fly)

---

## 5. วิธีรัน

```bash
cd art
python3 tools/leonardo_minigames.py dry-run          # ดูจำนวนงาน/ประมาณเครดิต ไม่เรียก API
LEONARDO_API_KEY=... python3 tools/leonardo_minigames.py assets --n 2
LEONARDO_API_KEY=... python3 tools/leonardo_minigames.py refs
LEONARDO_API_KEY=... python3 tools/leonardo_minigames.py videos      # ต้องรัน refs ก่อน (ใช้ image id)
LEONARDO_API_KEY=... python3 tools/leonardo_minigames.py pull --id <generationId>   # ดึงงานที่ทำในเว็บแล้ว
```

- `--only mini_dial_base` หรือ `--only V2_sort` สำหรับแก้ทีละชิ้น
- id ของภาพที่สร้างแล้วเก็บใน `art/incoming/minigames/_ids.json` (ใช้ต่อสำหรับวิดีโอ)
- ค่าเริ่มต้น: Phoenix 1.0, alchemy on, prompt enhance off, transparency `foreground_only` เฉพาะชุด A
- `LEONARDO_MODEL=<uuid>` สลับโมเดลได้โดยไม่แก้โค้ด (เช่น โมเดลที่เพิ่งปล่อย), `LEONARDO_VIDEO_RES=RESOLUTION_1080` สำหรับคลิปคมขึ้น (เครดิตแพงขึ้น)
- `pull` ดึงของที่มีอยู่แล้วในบัญชี: ทุก `generated_images[i].url` + `motionMP4URL` ถ้ามี ลง `art/incoming/minigames/pull/<generationId>/00.jpg, 00.mp4, …` — ใช้เมื่อ owner สร้างภาพ/คลิปจากหน้าเว็บ Leonardo เองแล้วส่ง uuid มาให้ (ไม่กินเครดิตเพิ่ม)
- ประมาณเครดิต: 15 prompt × 2 ภาพ = 30 ภาพ + 9 ref = 39 ภาพ (~2–4 เครดิต/ภาพ ตามโหมด) + 4 คลิป — **ตรวจเครดิตในบัญชีก่อนรันเต็มชุด**
- **ข้อจำกัดของ sandbox:** shell ที่ agent รันอยู่บล็อกคำสั่งที่มีคีย์ฝังอยู่ (credential guard) จึงต้องเป็น owner รันเองใน terminal — ส่งแค่ผลลัพธ์/ไฟล์กลับมา

### 5.1 ทางที่ใช้จริงตอนนี้: เจนผ่านหน้าเว็บ (ไม่มี API credit)

owner ไม่มี credit ฝั่ง API แต่เจนผ่านหน้าเว็บได้ → ใช้ `prompts` พิมพ์ข้อความเดียวกับที่ API จะส่ง แล้ววางในหน้าเว็บเอง

```bash
cd art
python tools/leonardo_minigames.py prompts                    # ทั้ง 15 prop + 9 ref + 4 คลิป
python tools/leonardo_minigames.py prompts --only mini_bin_blue,v2_sort
```

ตั้งค่าในหน้าเว็บให้ตรงกับชุด API: **Phoenix 1.0 · 1024×1024 · alchemy on · prompt enhance off** และถ้าโมเดลมีสวิตช์ **transparent background** ให้เปิด; ถ้าไม่มีก็ปล่อยพื้นเรียบไว้ ตัวแปลงจะตัดพื้นให้

วางไฟล์ที่ดาวน์โหลดลง `art/incoming/minigames/raw/` โดย**ตั้งชื่อไฟล์เป็นชื่อในตาราง** (`mini_bin_blue.png`, `mini_keypad_panel.png`, …) แล้วรัน:

```bash
cd art
python tools/minigame_pack_to_unity.py --dry      # ดูผลก่อน ไม่เขียนไฟล์
python tools/minigame_pack_to_unity.py            # เขียนลง unity/Assets/Resources/Textures/Minigame/
```

- ชื่อไฟล์ซ้ำจากเบราว์เซอร์ (`mini_bin_blue (1).png`) ถูกทำให้เป็น `mini_bin_blue` ให้เอง; ไฟล์ที่ชื่อไม่อยู่ใน 15 ชื่อยังแปลงได้แต่พิมพ์เตือน
- PNG ที่มี alpha อยู่แล้ว (export แบบ transparent) ใช้ alpha เดิม; ไฟล์ทึบจะถูก flood fill จากขอบเฟรม (`NEAR_BG 26`, blur 1.2) แล้ว trim + cap 512 px
- ไฟล์ `.mp4` ใน `raw/` ถือเป็น motion reference: พิมพ์ชื่อไว้เฉย ๆ ไม่แปลง ไม่ขึ้นเรือ
- ท้ายรายงานบอกชื่อที่ยังขาด เพื่อให้เจนเพิ่มได้เฉพาะใบนั้น

### 5.2 วิดีโออ้างอิงจาก owner (9 เกม)

owner มีคลิปของทั้ง 9 อาร์คีไทป์ — ใช้เป็น motion reference ไม่เข้า build (เหตุผลเดียวกับข้อ 4)

วางไฟล์ที่ `art/incoming/minigames/videos/` ตั้งชื่อตามอาร์คีไทป์: `wire`, `keypad`, `pour`, `digits`, `trash`, `sort`, `align`, `key`, `path` (`.mp4/.webm/.mov/.m4v/.avi`)

```bash
cd art
/c/Python313/python tools/minigame_video_frames.py                 # 12 เฟรมต่อคลิป
/c/Python313/python tools/minigame_video_frames.py --every 0.25    # ถี่ขึ้น ไว้จับจังหวะ
/c/Python313/python tools/minigame_video_frames.py wire keypad     # เฉพาะบางคลิป
```

- เครื่องนี้ **ไม่มี ffmpeg** → อ่าน/ตัดเฟรมด้วย OpenCV (`cv2 4.10.0`) แทน
- ผลลัพธ์: `videos/frames/<คลิป>/f000.jpg…` + contact sheet `videos/frames/<คลิป>_sheet.jpg` ที่มี timecode กำกับทุกช่อง
- ใช้ตอบ 3 คำถามต่ออาร์คีไทป์: **ระยะเวลา**ของท่าสำเร็จ, **จำนวนสเต็ป**ที่ต้องทำ, **จังหวะตอบสนอง**ตอนกดผิด — แล้วเทียบกับค่าที่ตั้งไว้ใน `BobMinigameHost`
- ไม่มีคลิปก็ไม่พัง: สคริปต์พิมพ์โฟลเดอร์ที่ต้องวางกับรายชื่อที่ใช้ตั้งไฟล์ให้

## 6. สัญญาการนำเข้า Unity

- ไฟล์วางที่ `unity/Assets/Resources/Textures/Minigame/<name>.png` (RGBA)
- โปรเจกต์นี้เป็น 3D จึง import เป็น Texture ปกติ → host สร้าง sprite เอง:
  `Resources.Load<Texture2D>("Textures/Minigame/<name>")` + `Sprite.Create(tex, rect, pivot .5, 100)`
- ห้ามพึ่งตัวอักษรในภาพ: รหัส/ป้าย/ตัวเลขทั้งหมดเป็น TMP
- ถ้าไฟล์ไม่มี ให้ fallback เป็นรูปทรง uGUI เดิม (ไม่ throw) — แบบเดียวกับ `fx_*` ในข้อ 7

## 7. ของเดิมที่ใช้อยู่แล้ว (จาก `art/leonardo_raw/vfx/`)

`art/tools/raw_vfx_to_unity.py` ตัดพื้นหลัง navy ของชุด RAW ด้วย flood fill จากขอบ (Subjet คงเงามืดของตัวเองไว้ เพราะไม่ติดกับขอบ) → crop → ย่อไม่เกิน 512 px → เขียน PNG ลง `Textures/Minigame/`

| sprite | ใช้ใน `BobMinigameHost` |
| --- | --- |
| `fx_hitstar` | ป๊อปของชิ้นใหญ่ (การ์ด/สไลม์/ปุ่ม) แทนวงแหวนแบน |
| `fx_sparkle` | สำเร็จขั้น (hold/fill) และแยกขยะถูกถัง |
| `fx_foam` | ฟองใต้นิ้วตอนถู (ทุก 0.14 วิ) |
| `fx_dust` | กองฝุ่น T10 ตอนหมดอายุ |
| `fx_splash` | น้ำถึงแถบเขียว T11 |
| `fx_slimesplat` | แตะสไลม์ T03 |

หลักฐาน: `Logs/verify-s28w.log` → `S28 fx <id>: … fx=6/6 -> +0.06s … -> +0.42s rings=0`, ภาพ `Logs/taskrace-s28-{t01,t02,t03,t05,t06,t08,t10,t11,t15}.png` เห็นดาวป๊อปจริงในใบ T05/T03

## 8. ยังไม่ทำ

- ยังไม่ได้รันชุด A–C จริง (ไม่มี API key ใน session)
- kind `Wire` / `Keypad` / `Align` / `Pour` / `Insert` ยังไม่เขียนใน `BobMinigameHost` — ข้อ 1 คือสเปกของงานรอบถัดไป
- ยังไม่ได้ทำ sprite สำรับ `mini_*` เข้า Resources (รอไฟล์จริงก่อน; เก็บ fallback uGUI ไว้)
