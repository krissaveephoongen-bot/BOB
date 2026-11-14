# 12 · Sound Manifest — เสียงทั้งเกม แหล่ง CC0 และวิธีนำเข้า Unity

> อัปเดต 13 พ.ย. 2026 (เพิ่มข้อ 6: เสียงและดนตรีเลเยอร์ของ task race) · ใช้คู่กับ `11_UX_UI_Spec.md` (ตาราง component ระบุเสียงต่อปุ่ม) และ GDD หัวข้อ 10 (เครื่องมือเสียง)

**สถานะตอนนี้ (อัปเดต 12 พ.ย. บ่าย): แทนด้วย CC0 แล้ว 20 ตัว (ตาราง 4) ที่เหลือยังสังเคราะห์ ห้ามรัน `make_sfx.py` ซ้ำ**

**สถานะเดิม:** เสียงทั้ง 38 ตัว + เพลง 1 ตัว ใน `unity/Assets/Audio` เป็นเสียงสังเคราะห์จากโค้ด (`art/tools/make_sfx.py`) ไม่มี sample จากใคร จึงไม่มีเรื่องลิขสิทธิ์ ใช้ได้ทันทีและเป็นของเราเอง (ถือเป็น CC0 ได้)
งานถัดไปคือแทนตัวที่ฟังแล้วไม่ใช่ "งานบ้าน" (ไม้กวาด น้ำ สเปรย์ ฟอง) ด้วยเสียงจริงจากชุด **CC0** ด้านล่าง โดยใช้ **ชื่อไฟล์เดิม** โค้ดจึงไม่ต้องแก้

## 1. แหล่ง CC0 ที่คัดไว้

ไม่มีเงื่อนไขให้เครดิต ใช้ในเกมขายได้ แต่ **ต้องเปิดหน้าดาวน์โหลดตรวจป้าย licence อีกครั้งก่อนใช้** (ทำจากเครื่อง PC: เครื่อง cloud ที่ทำเอกสารนี้เปิดเว็บเหล่านี้ไม่ได้ จึงคัดจากผลค้นหาเท่านั้น) แล้วบันทึกลงตาราง 4

| ชุด | ผู้ทำ | ใช้กับ | ลิงก์ |
| --- | --- | --- | --- |
| Interface Sounds | Kenney (CC0 ทั้งชุด) | ui_tap, ui_confirm, ui_cancel, ui_card_flip, vote_tick, ready_on | [kenney.nl/assets/interface-sounds](https://kenney.nl/assets/interface-sounds) |
| UI Audio | Kenney (CC0) | สำรองของเสียง UI, lobby_join | [kenney.nl/assets/ui-audio](https://kenney.nl/assets/ui-audio) |
| Impact Sounds | Kenney (CC0) | place_layer, table_put, cart_grab, footstep, enemy_hit | [kenney.nl/assets/impact-sounds](https://kenney.nl/assets/impact-sounds) |
| RPG Audio | Kenney (CC0) | pickup (ผ้า/หนัง), coin, broom_sweep (cloth) | [kenney.nl/assets/rpg-audio](https://kenney.nl/assets/rpg-audio) |
| 512 Sound Effects (8-bit style) | Juhani Junkala (CC0) | round_start, round_win, round_lose, timer_warning ถ้าอยากได้โทนเกมคลาสสิก | [opengameart.org/content/512-sound-effects-8-bit-style](https://opengameart.org/content/512-sound-effects-8-bit-style) |
| CC0 audio ของ rubberduck (100 CC0 SFX, 80 CC0 RPG SFX, ชุด footsteps/น้ำ/slime) | rubberduck (CC0) | water_pour, mud_splat, bubble_pop, enemy_spawn, enemy_defeat, footstep | [รวมผลงาน](https://opengameart.org/content/cc0-audio-uploader-rubberduck), [80 CC0 RPG SFX](https://opengameart.org/node/86018) |
| qubodup audio CC0 | qubodup (CC0) | spray_psst, air_whoosh, steam_hiss | [opengameart.org/content/qubodup-audio-cc0](https://opengameart.org/content/qubodup-audio-cc0) |
| Water splash and sand footsteps | (ดูหน้า) | water_pour, footstep บนพื้นเปียก | [opengameart.org/content/water-splash-and-sand-footsteps](https://opengameart.org/content/water-splash-and-sand-footsteps) |
| Freesound | หลายคน | เสียงเฉพาะทาง (ไม้กวาดจริง, ขวดสเปรย์) | [freesound.org](https://freesound.org) ใช้ตัวกรอง **License: Creative Commons 0** เท่านั้น ห้าม CC-BY-NC |

กฎ: รับเฉพาะ **CC0** (หรือของที่เราทำเอง) เพื่อไม่ต้องจัดการเครดิตและเงื่อนไขในเกมที่มีรายได้ ห้าม CC-BY-NC/ND และห้ามเสียงที่ผู้อัปโหลดไม่ได้อัดเอง (ดูคำอธิบายในหน้า)

## 2. รายการเสียงในเกม

ชื่อ = ชื่อไฟล์ใน `unity/Assets/Audio/SFX/*.wav` และชื่อที่โค้ดเรียก `BobSfx.Play("…")` · ความยาวคือของ placeholder ปัจจุบัน · **ลำดับแทน** 1 = แทนก่อน (ได้ยินบ่อยหรือสื่อธีมงานบ้าน)

| ชื่อ | ใช้ตอน | ยาว (วิ) | ลำดับแทน | ควรหาเสียงแบบ |
| --- | --- | --- | --- | --- |
| ui_tap | แตะปุ่มทั่วไป | 0.05 | 2 | คลิกนุ่ม ไม่แหลม |
| ui_confirm | ยืนยัน, เข้าคิว, สร้างห้อง | 0.25 | 2 | ติ๊ง 2 โน้ตขึ้น |
| ui_cancel | ย้อนกลับ, ปล่อยรถ, ยกเลิกพร้อม | 0.25 | 3 | 2 โน้ตลง |
| ui_card_flip | เปิดการ์ด/ใบงาน | 0.09 | 3 | กระดาษพลิก |
| coin | ได้เหรียญ | 0.35 | 3 | เหรียญกระทบ |
| pickup | หยิบชิ้นส่วน, หยิบจากโต๊ะ | 0.12 | 1 | ผ้า/ขวดพลาสติกถูกหยิบ |
| place_layer | วางชั้นบนรถเข็น | 0.25 | 1 | ของวางลงตะกร้า "ตุ้บ" |
| lift_layer | ยกชั้นบนออก | 0.22 | 2 | ดึงขึ้น "วูบ" สั้น |
| drop_invalid | วางไม่ได้ / เซิร์ฟเวอร์ปฏิเสธ | 0.18 | 2 | buzz ต่ำ สุภาพ |
| reaction_good / reaction_bad | ปฏิกิริยาเสริม/หักล้าง | 0.7 / 0.5 | 2 | ประกายวิ้ง / ฟองล้นปุด |
| fizz | เบกกิ้งโซดา+น้ำส้มสายชู | 1.0 | 1 | ฟู่ฟองซ่า |
| bubble_pop | ability โล่ฟอง | 0.14 | 1 | ฟองแตก |
| steam_hiss | ไอน้ำ, ability หยุดเวลา | 1.1 | 1 | ไอพ่นฉ่า |
| spray_psst | ฉีดพ่น, ability เล็งเป้า | 0.35 | 1 | กดสเปรย์ "ฟึ่บ" |
| broom_sweep | กวาด | 0.45 | 1 | ขนไม้กวาดถูพื้น |
| scrub_loop | แปรงขัด (loop) | 0.48 | 1 | แปรงขัดถี่ ๆ ต่อเนื่อง ตัดให้ loop เนียน |
| air_whoosh | ลมเป่า | 0.5 | 2 | ลมวูบ |
| water_pour | น้ำร้อน/เท | 0.9 | 1 | น้ำเทจากกาลงถัง |
| footstep | ก้าวเดิน | 0.07 | 2 | รองเท้าแตะบนพื้นกระเบื้อง 3-4 แบบสุ่ม |
| cart_roll_loop | เข็นรถ (loop) | 1.0 | 1 | ล้อพลาสติกบนกระเบื้อง loop |
| enemy_hit / enemy_defeat / enemy_spawn | ศัตรูโดน/แพ้/โผล่ | 0.3 / 1.0 / 0.6 | 2 | สไลม์ "แปะ", ปุ้งหาย + ประกาย, โผล่ "บึ้บ" |
| mud_splat | โคลน | 0.7 | 2 | โคลนกระเด็น |
| timer_tick / timer_warning | 10 วิสุดท้าย | 0.03 / 0.4 | 3 | ติ๊กนาฬิกา / บี๊บสองครั้ง |
| round_start / round_win / round_lose | เริ่มรอบ, ชนะ, แพ้ | 1.0 / 1.5 / 1.4 | 3 | jingle สั้น สดใส (แพ้ยังต้องน่ารัก ไม่เศร้า) |
| ability_activate | ใช้ ability | 0.5 | 2 | วูบขึ้น + ประกาย |
| score_count | นับคะแนนขึ้น | 0.04 | 3 | ติ๊กสั้นมาก (เล่นถี่) |
| **pass_part** (ใหม่) | ส่งของให้เพื่อน | 0.32 | 1 | วูบ + ติ๊งรับ |
| **table_put** (ใหม่) | พักของบนโต๊ะ | 0.22 | 2 | เคาะโต๊ะไม้ |
| **cart_grab** (ใหม่) | จับเข็นรถ | 0.3 | 2 | ด้ามโลหะ "กริ๊ก" + ล้อเริ่มหมุน |
| **lobby_join** (ใหม่) | เข้าห้องสำเร็จ | 0.45 | 3 | ติ๊ง 3 โน้ตขึ้น |
| **ready_on** (ใหม่) | กดพร้อม | 0.3 | 3 | ติ๊งสั้นชัด |
| **vote_tick** (ใหม่) | กดค้างจนโหวตส่ง | 0.06 | 3 | ติ๊กยืนยัน |
| music_placeholder_loop | เพลงในแมตช์ | 16.0 | 2 | ต้องการเพลงจริง (ดูข้อ 5) |

## 3. วิธีนำเข้า Unity (ทำบน PC)

1. ดาวน์โหลดชุดจากข้อ 1 ลงโฟลเดอร์นอก repo ก่อน ฟังแล้วเลือก 1 ไฟล์ต่อชื่อ
2. ตัดหัวท้ายเงียบ, normalize ที่ −1 dBFS (Audacity: Effect > Normalize), mono, 44.1 kHz, ตัวที่เป็น loop ตัดที่ zero-crossing
3. บันทึกทับเป็น `unity/Assets/Audio/SFX/<ชื่อเดิม>.wav` (ใช้ชื่อเดิมทุกตัว โค้ดไม่ต้องแก้)
4. Import settings (เลือกหลายไฟล์พร้อมกันได้):
   - เสียงสั้น < 1 วิ: Load Type = **Decompress On Load**, Compression = **ADPCM**, Force To Mono ✓
   - เสียงยาว/loop (scrub_loop, cart_roll_loop, steam_hiss): **Compressed In Memory**, Vorbis Quality 70
   - เพลง: **Streaming**, Vorbis Quality 60, Load In Background ✓
5. เมนู **Bob > Art > Build Audio Prefab** (หรือ Bob > Build Coop Scene ซึ่งเรียกให้) เพื่อให้ `BobAudio.prefab` เห็นไฟล์ใหม่
6. เพิ่มแถวในตาราง 4 ทุกไฟล์ที่ไม่ใช่ของเราเอง แล้ว commit ไฟล์ .wav + .meta + เอกสารนี้พร้อมกัน
7. ถ้าจะสร้าง placeholder ใหม่จากโค้ด: `python3 art/tools/make_sfx.py` (ต้องมี numpy) ไฟล์เดิมออกมาเหมือนเดิมทุกไบต์ ถ้าแทนด้วยเสียงจริงแล้ว **อย่ารันซ้ำ** เพราะจะเขียนทับ

## 4. บันทึก licence (เติมเมื่อแทนเสียง)

| ไฟล์ | แหล่ง (ลิงก์หน้า) | ผู้ทำ | Licence ที่เห็นบนหน้า | วันที่ตรวจ |
| --- | --- | --- | --- | --- |
| table_put, place_layer, lobby_join, cart_grab, enemy_hit, ui_confirm, drop_invalid, pass_part, ready_on | Kenney RPG Audio / Interface / Impact (kenney.nl, ผ่านงาน Summer Engine `library/sounds/rpg-sound-effects`: impactWood_light_002/003, doorOpen_2, doorClose_1/4, chop, confirmation_002, error_006, drop_002) | Kenney | CC0 1.0 (CREDITS.txt ในโฟลเดอร์ต้นทาง) | 13 พ.ย. 2026 |
| เพลง 4 เพลง: `menu_apple_cider` (Apple Cider), `game_the_fun_run` (the_fun_run), `result_squashin_bugs` (squashin_bugs_fixed), `tense_insect_factory` (Insect Factory) | Zane Little (opengameart.org, ko-fi.com/zanelittlemusic) | Zane Little | CC0 1.0 | 13 พ.ย. 2026 · สลับตามหน้าจอ: เมนู / เล่น / ใกล้หมดเวลา (<15 วิ) / ผลแมตช์ |
| ไฟล์ที่เหลือ (water_pour, spray_psst, steam_hiss, broom_sweep, scrub_loop, fizz, mud_splat ฯลฯ) | `art/tools/make_sfx.py` | ทีม BOB (สังเคราะห์) | ของเราเอง | 12 พ.ย. 2026 |
| ui_tap, ui_confirm, ui_cancel, ui_card_flip, drop_invalid, lobby_join, ready_on, vote_tick, timer_tick, pass_part | Kenney Interface Sounds (OpenGameArt: opengameart.org/content/interface-sounds, ไฟล์ click_001, confirmation_001, back_001, scroll_001, error_001, open_001, toggle_001, tick_001, tick_002, pluck_001) | Kenney | CC0 (ป้าย CC0 บนหน้า OGA + `art/licenses/kenney-interface-sounds-LICENSE.txt`) | 12 พ.ย. 2026 |
| pickup, coin, place_layer, table_put, bubble_pop, cart_grab, lift_layer, enemy_hit, enemy_defeat, enemy_spawn | RPG Sound Pack (OpenGameArt: opengameart.org/content/rpg-sound-pack, ไฟล์ cloth, coin, cloth-heavy, wood-small, bubble, metal-small1, armor-light, slime2, slime5, slime8) | artisticdude | CC0 (ป้าย CC0 บนหน้า OGA) | 12 พ.ย. 2026 |

## 5. เพลง

เพลงในแมตช์ควรเป็น loop 60-90 วินาที 110-125 bpm สดใสแบบของเล่น ไม่มีเสียงร้อง (ไม่แย่งความสนใจจากเสียงวาง/ปฏิกิริยา) แหล่ง CC0: ชุด music ของ Kenney (ตรวจหน้า [kenney.nl/assets](https://kenney.nl/assets) หมวด Audio) หรือจ้างทำเพลงเฉพาะเกม ลด volume ตอนช่วงทดสอบ 10 วินาทีเพื่อให้เสียงทำความสะอาดเด่น

**Task race (13 พ.ย. 2026):** เพลงในแมตช์เปลี่ยนเป็นเพลงแยก 4 stem ที่เปิดตามความคืบหน้า แผนเต็มอยู่ใน `17_Music_Layers.md` ข้อกำหนดด้านบน (สดใส ไม่มีเสียงร้อง) ยังใช้ แต่ล็อกเป็น **120 bpm, C major, loop 8 ห้อง = 16.0 วิ** และต้องมีชุด fast 130 bpm

## 6. เสียงใหม่ของ Task Race (ยังไม่มีไฟล์)

ตั้งชื่อตามนี้เพื่อให้โค้ดเรียกได้ตรง (`BobSfx.Play("…")`, `BobMusicLayers`) ทุกตัวยังไม่มีไฟล์ ใช้ placeholder สังเคราะห์ก่อนได้ หาแหล่ง CC0 ตามข้อ 1 หรือจ้างทำ (เพลง)

| ชื่อ | ใช้ตอน | ยาว (วิ) | ลำดับ | ควรหาเสียงแบบ |
| --- | --- | --- | --- | --- |
| mus_task_L1 / L2 / L3 / L4 | ดนตรีเลเยอร์ในรอบ (พื้น / ทำนอง / เร่ง / ไคลแมกซ์) | 16.0 loop | 1 | stem ของเพลงเดียวกัน 120 bpm C major |
| mus_task_L1..L4_fast | ชุดเร่งเมื่อ L4 เปิด | 14.77 loop | 1 | stem เดียวกันที่ 130 bpm |
| sting_task_done (4 แบบ) | จบขั้นของ task | 0.3 | 1 | โน้ตเดียวในคอร์ด C ของ marimba + ประกาย |
| sting_progress_25 / 50 / 75 | ทีมเราผ่านขั้น | 0.8 | 1 | arpeggio ไล่ขึ้น |
| sting_rival_step | คู่แข่งผ่านขั้น | 0.6 | 1 | 2 โน้ตต่ำ ไม่ดุ |
| sting_emergency | คราบฉุกเฉินโผล่ | 0.7 | 1 | แตรสั้นแบบการ์ตูน |
| sting_team_win / sting_round_end | ถึง 100% / รอบจบแบบไม่ชนะ | 2.0 / 1.5 | 2 | ฉลอง / น่ารักไม่เศร้า |
| trash_drop | ขยะลงถังถูก (T05) | 0.25 | 1 | ของตกถังพลาสติก |
| wring | บิดผ้า (T06) | 0.6 | 3 | น้ำหยดจากผ้า |
| furniture_drag | ยกของสองคน (T13) | 0.8 | 2 | เฟอร์นิเจอร์ไม้ลากบนพื้น |
| dish_clink | จานสะอาด / วางบนชั้น (T07) | 0.3 | 3 | จานเซรามิกกระทบเบาๆ |
| glass_squeak | เช็ดกระจก (T08) | 0.4 | 3 | ผ้าถูกระจกเสียงเอี๊ยด สั้นๆ ไม่แสบหู |
| dust_poof | ไล่ฝุ่นใต้โต๊ะโดน (T10) | 0.3 | 3 | ปุ๊ฟ ฝุ่นฟุ้งแบบการ์ตูน |
| chair_drag | ลากเก้าอี้เข้าช่อง (T12) | 0.6 | 2 | ขาเก้าอี้ลากพื้น (สั้นกว่า furniture_drag) |
| mop_swish | ไม้ถูพื้นแต่ละช่วง (T14) | 0.5 | 3 | ไม้ถูเปียกปัดพื้น |
| washer_beep_1..4 | ปุ่มเครื่องซักผ้า 4 ปุ่ม (T15) | 0.2 | 4 | บี๊บ 4 โน้ตใน C major (C E G C') ให้จำด้วยหูได้ |
| washer_start | กดลำดับครบ เครื่องเริ่มหมุน (T15) | 1.2 | 1 | มอเตอร์เครื่องซักผ้าเริ่มหมุน + น้ำไหล |

ใช้เสียงเดิมต่อในมินิเกม: `broom_sweep` (T01), `scrub_loop` (T02, T09), `spray_psst` + `enemy_hit` (T03), `water_pour` + `fizz` (T09), `cart_grab` (T13), `enemy_spawn` / `enemy_defeat` (T16), `drop_invalid` (ลงผิดถัง)

มินิเกมระยะ 2 ใช้เสียงเดิมต่อ: `wring` (T06), `water_pour` (T07, T11), `table_put` (T12), `spray_psst` (T08), `scrub_loop` (T07), `broom_sweep` (T10), `drop_invalid` (เทล้น T11)
