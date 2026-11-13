# 13 · รายการ placeholder ที่ยังเหลือในเกม และแผนแทนด้วย asset จริง (13 พ.ย. 2026)

เป้าหมาย: ไม่มี placeholder จากโค้ดเหลือใน prototype v1 (ผู้ใช้สั่ง 12 พ.ย.) สถานะตอนนี้อ้างอิง `unity/Assets/Art/bob-art-manifest.json` (228 รายการ) และ `unity/Assets/Audio/SFX` (38 ตัว)

## A. แทนด้วยภาพ Leonardo ที่ลบพื้นหลังแล้ว (มีภาพพร้อม นำเข้าได้เลย)

**นำเข้าแล้ว (13 พ.ย.): ชั้นวางสถานี 4 จุด + ชิ้นส่วนวางบนชั้น, โต๊ะพักของ (prop_desk), ศัตรู 8 ชนิด (idle/attack/hit/defeat/spawn/portrait สร้างจากท่านิ่ง)**

| กลุ่ม | ไฟล์ใน Unity ที่เป็น placeholder | ภาพจริงที่มี (`bob-art/cutouts_alt_best` / `cutouts_best`) | วิธีทำ |
| --- | --- | --- | --- |
| ชั้นวางสถานี 4 จุด | `Props/station_tool, chem, energy, method` | `cart/shelf_tool, chem, energy, method` (v3 ไม่มีตุ๊กตา) | ใส่ในกรอบ 384×256 แล้วคงสีตระกูลเป็นขอบ/ป้าย |
| โต๊ะพักของ | (กล่องสีน้ำตาลที่โค้ดวาด) | `props/prop_desk` | ใช้ภาพโต๊ะแทน |
| รถเข็น | `Props/cart_layer` (ชั้นสีแบน) | `cart/cart_shelf_lv1-5` | ใช้ภาพรถเข็นตามจำนวนชั้นแทนการวางแผ่นสี |
| ศัตรู 8 ชนิด | `Enemies/*` ทั้ง 5 ท่า (sheet จากโค้ด) | `enemies/<id>_idle, attack, hit, defeat` | สร้าง sheet จากท่านิ่ง (เด้ง/พุ่ง/สั่น/ยุบ) เหมือนฮีโร่ spawn = ขยายจากเล็ก portrait = ครอปหน้า |
| ฮีโร่: portrait, faces, back | `Characters/*/_portrait, _faces, _back` | `heroes/<id>/<id>_master, face_*, back` | ครอปหน้า/จัด sheet |
| ไอเท็ม | `Items/item_coin_spin` | `items/item_coin` | หมุน/ยืดแกน X เป็น 8 เฟรม |
| FX 14 ชุด | `FX/*` | `vfx/fx_dust, foam, hitstar, levelup, slimesplat, sparkle, splash, spraymist, swipe, tornado` (10 ภาพนิ่ง) | ทำ flipbook ด้วยการขยาย/จาง/หมุน ที่เหลือ (steam, poof, fizz, bubble_pop, target, bubble_dome, time_freeze, clean_wave) ใช้ภาพที่ใกล้ที่สุดหรือรอข้อ B |
| Props/School 22 ชิ้น | `Props/School/*` | `props/prop_*` 22 ชิ้น (bucket มีหน้าตา, window มีแมว ต้องสร้างใหม่ตามหมายเหตุ) | ใส่ตามชื่อ |
| ฉากหลัง 5 ห้อง | `Environments/Backdrops/*` | `backgrounds/bg_*` (16:9, ไม่ต้องลบพื้นหลัง) | ย่อเป็น 1280×720 |
| UI: ปุ่ม/แผง/ริบบิ้น/กรอบ | `UI/ui_button*, ui_panel, ui_card, ui_bar_*` | `ui/ui_button_blue, play, round, skill, ui_panel_blue, ui_portrait_frame, ui_ribbon, popup_*` | ต้องตั้ง 9-slice ใหม่ ภาพจริงเป็นวัตถุเดี่ยว ใช้กับปุ่มใหญ่/popup ก่อน |
| UI: ไอคอน | `icon_pause, ...` | `ui/icon_close, home, pause, settings, shop, sound, coin_stack, gem_pile, equip_*` | แทนตามชื่อ |

## B. ต้องให้เธรดภาพสร้างเพิ่ม (ไม่มีใน Leonardo RAW หรือภาพที่มีใช้ไม่ได้)

| # | asset | ใช้ทำอะไร | หมายเหตุ |
| --- | --- | --- | --- |
| B1 | **F21-F24 ชุดมุม 3/4 โรงอาหาร**: พื้นเรียบ (กระเบื้อง) + พื้นสกปรก, ผนังหลัง, เคาน์เตอร์/ชั้นวางตามตระกูล (อุปกรณ์/น้ำยา/แรง/วิธีใช้), รถเข็นมุม 3/4, ถังขยะ | กล้อง pitch 55° ตามสเปก 5A.1 | ต้องวาดมุมเฉียงเดียวกันทุกชิ้น |
| B2 | ฉากห้องเช่า: **โซฟา**, พื้นห้อง, ผนัง 3/4 | ด่านแรกตาม GDD ข้อ 20 | โซฟา placeholder ปัจจุบัน |
| B3 | ฉากร้านกาแฟและครัว: เคาน์เตอร์ยาว ตู้ ซิงก์ พื้นเปียก ป้ายพื้นเปียก ประตูแคบ | ด่าน 3-4 | มีภาพพื้นหลังล็อบบี้แล้ว แต่ไม่มีชิ้นส่วนวางเล่น |
| B4 | ไอคอน HUD: ขจัดคราบ/ถนอมผิว/เร็ว/เงา, นาฬิกา, งบ, ดาว/ดาวว่าง, ติ๊ก/กากบาท, ยก, ability | แถบค่าและ HUD | ชุดไอคอนเดียวกัน สไตล์เหนียวหนา |
| B5 | โลโก้ "BOB สมรภูมิปัดกวาด" มีตัวอักษรไทยที่ถูกต้อง | เมนูหลัก, โหลด | ตอนนี้เป็นกล่องสีกับฟอนต์ |
| B6 | การ์ดเลือกตัวละคร 4 ใบ + การ์ดเลือกด่าน | เลือกตัว/ด่าน | ภาพ screen_* ใน Leonardo มีตัวอักษรมั่ว |
| B7 | ท่าฮีโร่/ศัตรูหลายเฟรมจริง (ถ้ามีเวลา) | อนิเมชัน | ตอนนี้ใช้ท่านิ่ง + การเด้งจากโค้ด |
| B8 | ชิ้นส่วนบน tray มือ: ภาพพื้นหลังการ์ดมือ/ชั้นรถเข็น (ถ้าต้องการ) | UI | ใช้ ui_panel ได้ ไม่เร่ง |

## C. เสียงที่ยังเป็นเสียงสังเคราะห์ (หา CC0 หรือสร้างเพิ่ม)

`water_pour, spray_psst, steam_hiss, broom_sweep, scrub_loop, fizz, mud_splat, air_whoosh, footstep, cart_roll_loop, reaction_good, reaction_bad, ability_activate, round_start, round_win, round_lose, timer_warning, score_count, enemy_*` (บางตัวใช้ CC0 แล้ว: ดู `12_Sound_Manifest.md`) และเพลง AI ที่ยังรอเครดิต

## ลำดับนำเข้า

1. A: ชั้นวาง + โต๊ะ + รถเข็น (เห็นผลทันทีในจอเล่น)
2. A: ศัตรู 8 ชนิด + ไอคอน/UI + FX + props
3. B1-B2 (ภาพ 3/4) → ตั้งกล้อง pitch 55°
4. B3-B6 ตามด่านและเมนู
