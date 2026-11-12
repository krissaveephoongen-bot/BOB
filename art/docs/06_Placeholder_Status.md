# 06 · สถานะ Placeholder ตาม Concept (12 พ.ย. 2026)

ทุกอย่างในตารางนี้สร้างจากโค้ด (`tools/make_art.py`, `tools/make_sheets.py`) ใช้เล่นและทดสอบใน Unity ได้ทันที ชื่อไฟล์คงที่ เมื่อศิลปินส่งงานจริงให้วางทับไฟล์ชื่อเดิม

ภาพรวม: `docs/sheets/sheet_<hero>.png`, `docs/overview_enemies_items.png`, `docs/overview_ui_fx.png`, `docs/overview_backdrops.png`

## ทำแล้ว (2D placeholder)

| กลุ่ม | ไฟล์ (ใต้ `unity/Assets/Art/Sprites/`) | หมายเหตุ |
| --- | --- | --- |
| A Heroes | `Characters/<id>/<id>_idle, walk, carry, work, cheer, portrait` | ทรงหัว/สี/อุปกรณ์ตาม concept; carry ชูมือเหนือหัว อุปกรณ์สะพายหลัง (ไม่บังหน้าแล้ว) |
| A Expressions | `Characters/<id>/<id>_faces` (8 เฟรม: happy, wink, angry, surprised, cry, effort, dizzy, joy) | ตั้ง `still` ใน manifest จึงไม่ถูกสร้างเป็นอนิเมชัน ใช้เป็นรูปสีหน้าใน UI |
| A Back view | `Characters/<id>/<id>_back` | |
| A Character sheet | `docs/sheets/sheet_<id>.png` | เลย์เอาต์ตาม sheet กวาด: hero, หน้า/หลัง, สีหน้า, ท่า, อุปกรณ์+สกิล+FX, palette |
| B Enemies | `Enemies/<id>/<id>_idle, attack, hit, defeat, spawn, portrait` | stain = เจ้าคราบ, grease = สไลม์, **ใหม่** trash = ขยะปีศาจ, germ = เชื้อโรค, boss = บอสคราบยักษ์ (เฟรม 256) ; dust/rust/scale ปรับหน้าให้สไตล์เดียวกัน |
| C Items | `Items/item_coin, coin_spin, star, gem, heart, stopwatch, magnet, bubble, chest_closed, chest_open, trophy` | |
| C/D Props | `Props/School/` 22 ชิ้น: ป้ายพื้นเปียก กรวย ถังน้ำ ถุงขยะ ถังขยะ ถังรีไซเคิล ทิชชู ขวดน้ำยา โต๊ะเรียน เก้าอี้ กระดาน ล็อกเกอร์ หน้าต่าง โต๊ะ/เก้าอี้โรงอาหาร ร้านอาหาร สไลเดอร์ ม้านั่ง ต้นไม้ พุ่มไม้ ประตูบอส ท่อ | pivot ที่ฐาน |
| D Floors | `Environments/Floors/floor_<school, wood, cafeteria, grass, sand, boiler>` + `_dirty` | tile 64 px |
| D Decals | `Environments/Decals/decal_mud, slime, goo, trash, germ, puddle` | วางทับพื้นให้เป็นห้องสกปรก |
| D Backdrops | `Environments/Backdrops/bg_<school, classroom, cafeteria, playground, boiler>` + `_dirty` | ใช้เป็นฉากหลัง/รูปด่าน |
| E VFX | `FX/fx_splash, swipe, spray_mist, hit, tornado, bubble_dome, time_freeze, clean_wave` (+ ของเดิม 6) | สกิล: กวาด=swipe/tornado, ฟอง=bubble_dome, ถู=time_freeze, สเปรย์=spray_mist |
| F UI | `UI/logo_bob`, `ui_button_play`, `ui_button_blue`, `ui_button_skill`, `ui_button_round`, `ui_portrait_frame`, `ui_ribbon`, `ui_panel_blue`, `icon_heart/coin/gem`, `UI/Skills/icon_skill_<id>`, `UI/CharacterSelect/card_<id>`, `UI/StageSelect/stage_<ฉาก>` | ปุ่ม/ริบบิ้น/แผงตั้ง 9-slice แล้ว |

ไฟล์เดิม (sofa, floor_tile, station_*, cart, part_*, ui เดิม, logo_placeholder) ยังอยู่ครบ

## การเปลี่ยนในโค้ด Unity

- `Scripts/Editor/BobArtImporter.cs`: เพิ่มฟิลด์ `still` ใน manifest และข้ามการสร้าง `.anim` ให้ sheet ที่เป็น `still` (เช่น faces) ยังไม่ได้คอมไพล์ใน Unity จริง
- ศัตรูมีท่าใหม่ `attack` และ `spawn` เป็น trigger ใน Animator Controller (สร้างอัตโนมัติจากเมนู **Bob > Art > Build Animation Clips**)
- `trash`, `germ`, `boss` ยังไม่มีในกฎเกม (`BobRules.cs` / prototype / server) ถ้าจะใช้ในรอบจริงต้องเพิ่มกฎว่าแพ้/ชนะชิ้นส่วนอะไร

## ยังต้องให้ศิลปินหรือเครื่องมือ AI ทำ

| งาน | เหตุผล | ใช้อะไรช่วย |
| --- | --- | --- |
| Master sheet แบบวาดจริงของ ฟอง ถู สเปรย์ (+ มุมข้าง) | ของจากโค้ดเป็นรูปทรงเรขาคณิต ไม่มีรายละเอียด/แสงเงาแบบภาพ concept | `04_AI_Prompts.md` §1–3 แนบ `docs/sheets/sheet_<id>.png` เป็นตัวกำหนดท่าและสี |
| โมเดล 3D + rig + อนิเมชันจริง | งาน Blender | `03_Unity_Spec.md` + ortho reference (§2 ของ prompts) |
| ศัตรู/บอสแบบวาดหรือปั้นจริง | | `04_AI_Prompts.md` §4 |
| ฉากแบบ 2.5D เต็ม (ห้องเรียน โรงอาหาร สนาม ห้องใต้ดิน) | backdrop จากโค้ดเป็นภาพแบน ใช้แค่วางเลย์เอาต์ | `04_AI_Prompts.md` §5 |
| โลโก้ BOB final + ฟอนต์ไทย | โลโก้จากโค้ดใช้ฟอนต์ระบบ | นักออกแบบกราฟิก |
| VFX แบบ particle จริงใน Unity | sheet จากโค้ดเป็น flipbook ง่ายๆ | Shuriken ตาม `02_Asset_List.md` §E |
