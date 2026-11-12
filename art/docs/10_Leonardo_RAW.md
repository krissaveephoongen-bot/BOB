# 10 · ภาพ RAW จาก Leonardo.ai (12 พ.ย. 2026)

สร้างครบทุก asset ในบัญชี Leonardo ของผู้ใช้ผ่าน Claude in Chrome รวม 740 ภาพ (+ ฉากหลังล็อบบี้ 144 ภาพ + ชุดเติมส่วนที่ขาด 324 ภาพ + รอบสร้างใหม่ 128 ภาพ) เก็บไว้ที่ `bob-art/leonardo_raw/` (ทั้งใน project files และ `C:\Users\Jakgrits\SweapBattle\bob-art\leonardo_raw\`) ใน repo อยู่ที่ `art/leonardo_raw/` (Git LFS, `.gitattributes` ที่ราก repo) ภาพรวม: `docs/overview_leonardo_raw.jpg`, `docs/overview_lobby_backgrounds.jpg`, `docs/overview_leonardo_gapfill.jpg`

**สถานะ: RAW** ยังไม่ลบพื้นหลัง ยังไม่ตัดเฟรม ยังไม่ได้แทน placeholder ใน Unity ผู้ใช้จะนำไปจัดต่อเอง

## ค่าที่ใช้

- Model: Phoenix 1.0 · Style: Dynamic · Contrast: Medium · Mode: Fast · Prompt Enhance: Off · 4 ภาพต่อ prompt
- ขนาด: สี่เหลี่ยม 896×896 · 16:9 = 1184×672
- กวาด: ใช้ Character Reference (High) จากภาพ concept · ฟอง/ถู/สเปรย์: ข้อความอย่างเดียว (ไม่มี Character Reference)
- สำนวน prompt ฮีโร่ที่ผู้ใช้ปรับเอง: "chibi toy figure with a very big round head and tiny body, head about half of total height, large glossy round eyes ... plain light background" (แทน "transparent background" ใน `09_Leonardo_Full_Prompts.md`)
- ใช้เครดิตประมาณ 2,800 (16 ต่อ prompt)

## ฉากหลังล็อบบี้ 18 ฉาก (เพิ่ม 12 พ.ย. 2026)

`lobby_backgrounds/` (16:9, 72 ภาพ) ฉากว่างมีเวทีกลมตรงกลางด้านหน้าไว้วางฮีโร่ ไม่มีคน ไม่มี UI:
hallway_morning, classroom, cafeteria, playground, rooftop_sunset, library, science_lab, gym, music_room, art_room, garden_spring, rainy_day, night_moon, winter_snow, halloween, christmas, pool_summer, janitor_hq (ชื่อไฟล์ `lobby_<ฉาก>_<0-3>.jpg`)

`lobby_backgrounds_with_kid/` คือรอบแรก (prompt เดียวกันแต่ยังไม่ได้กันคน) แทบทุกภาพมีเด็กยืนบนเวที เก็บไว้เป็นตัวอย่างการจัดวาง

Prompt: `empty mobile game lobby background scene, <ฉาก>, wide landscape view from a low front angle, an empty round stage platform in the centre foreground with nothing on it, empty scene with no people, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume like a vinyl toy, warm soft lighting, bright clean colours` · Negative: `person, people, boy, girl, child, kid, character, figure, mascot, text, letters, words, logo, watermark, UI, buttons, icons, realistic, photo, blurry`

ข้อควรระวัง: science_lab บางภาพมีโครงกระดูก/ฟักทองบนเวที, christmas แบบที่ 4 มีป้ายคล้ายโลโก้ BOB

| asset (ไม่มีคน) | gen id |
| --- | --- |
| `lobby_art_room` | `1f1de868-4bae-61e0-9fb2-672bd214795c` |
| `lobby_cafeteria` | `1f1de866-0712-6dd0-a947-f7df3d826626` |
| `lobby_christmas` | `1f1de86a-42a1-6000-b74d-4ba6e1dc911a` |
| `lobby_classroom` | `1f1de865-b3b1-6a60-8f82-88a45f1cd4e9` |
| `lobby_garden_spring` | `1f1de868-9ea8-6cb0-8de7-c509f3dc86ab` |
| `lobby_gym` | `1f1de867-a69b-6d10-8982-0e8c5e60017e` |
| `lobby_halloween` | `1f1de869-ece2-6510-91bd-72545626f7b6` |
| `lobby_hallway_morning` | `1f1de865-65ee-66c0-8f53-190a3491c087` |
| `lobby_janitor_hq` | `1f1de86a-ea52-6b00-9ab6-c5798ecfdc41` |
| `lobby_library` | `1f1de866-ff27-62a0-ac30-5b1b3fec9e28` |
| `lobby_music_room` | `1f1de867-f8b3-6710-b080-f18a008f49c4` |
| `lobby_night_moon` | `1f1de869-46a1-6480-b07a-41a9bb23fe48` |
| `lobby_playground` | `1f1de866-5b37-6640-a29a-53221865307a` |
| `lobby_pool_summer` | `1f1de86a-996c-63d0-a5a9-951698b83a21` |
| `lobby_rainy_day` | `1f1de868-f2b7-6590-b856-02b2b0ca4c5b` |
| `lobby_rooftop_sunset` | `1f1de866-ad98-6420-b178-92a5386d27d5` |
| `lobby_science_lab` | `1f1de867-523f-6230-8e71-1ce051631c49` |
| `lobby_winter_snow` | `1f1de869-9874-68c0-99ff-24ca80a444ac` |

## ชุดเติมส่วนที่ขาดตาม Asset List (เพิ่ม 12 พ.ย. 2026)

81 รายการ × 4 แบบ = 324 ภาพ (สี่เหลี่ยม ยกเว้น splash 16:9) ภาพรวม `docs/overview_leonardo_gapfill.jpg` ไฟล์ zip `raw_parts/leonardo_gapfill_1..3.zip`

| โฟลเดอร์ | รหัสใน 02 | รายการ |
| --- | --- | --- |
| `parts/` | C04 | part_ + cloth, sponge, brush, broom, soap, vinegar, soda, polish, hot, steam, scrub, air, soak, spray, circle (ตรงกับ id ใน `PARTS` ของเกม) |
| `cart/` | C05 | cart_base, cart_shelf_lv1–lv5, shelf_tool / chem / energy / method (สีส้ม/ฟ้า/แดง/ม่วง) |
| `equipment/` | C01 | equip_broom, sponge, mop, spray_bottle, bucket_hat |
| `decals/` | B07 | decal_mud / slime / trash / germ × 1–3 (เล็ก/กลาง/ใหญ่) |
| `tiles/` | D07 | tile_stone / wood / rubber × clean / dirty (ลายต่อเนื่อง มุมบนลงล่าง) |
| `vfx/` (พื้นน้ำเงินเข้ม) | E | fx_splash, foam, dust, sparkle, hitstar, spraymist, swipe, tornado, slimesplat, levelup |
| `ui/` | F06–F09 | icon_equip_broom/sponge/mop/spray, icon_pause/settings/home/close/sound/shop, icon_coin_stack, icon_gem_pile, popup_win/lose/pause/confirm/reward |
| `items/` | C06 | item_upgrade_box |
| `enemies/` | B06 | boss_shoot, boss_rain, boss_phase |
| `screens/` | F10 | app_icon, splash_screen (16:9) |

Suffix ของ prompt (ต่อท้ายคำบรรยายแต่ละชิ้น):
- ของ/อุปกรณ์/ชิ้นส่วน: `single game item icon, three-quarter view, ...` แบบเดียวกับ items เดิม · รถเข็น/ชั้น: แบบเดียวกับ props เดิม · ไอคอน/ป๊อปอัป: แบบเดียวกับ ui เดิม
- คราบ: `flat top-down view, game floor decal, irregular organic outline, ... plain light grey background`
- ลายพื้น: `seamless tileable floor texture, flat top-down orthographic view, fills the whole frame edge to edge, no objects, no shadows, ... hand-painted`
- VFX: `single game visual effect sprite, cartoon VFX, bold simple shapes, soft glow, ... plain dark navy background` (พื้นเข้มเพื่อให้ตัดด้วย additive/screen ได้ง่าย)

ข้อควรระวัง:
- ป๊อปอัป confirm / pause / win / lose / reward มีตัวอักษรภาษาอังกฤษติดมา (Confirm, Pause menu, VICTORY, DEFEAT, REWARD) ต้องลบหรือสร้างใหม่ถ้าจะใส่ข้อความไทย
- shelf_* มีตุ๊กตาเล็กวางอยู่บนชั้น · decal_trash_2/3 มีไม้กวาดปนมา · icon_close แบบแรกเป็นรูปดอกจันแทน X (ดูแบบอื่น)
- tile_rubber_clean มีไม้กวาด/ลูกบอลบนพื้น ใช้แบบอื่นหรือครอป · ลายพื้นยังไม่ได้ทดสอบว่าต่อกันได้จริง
- equip_mop หัวไม้ถูเป็นสีเขียว · boss_shoot ไม่เห็นการยิงชัด · app_icon เป็นเด็กคาดผ้าแดง (คล้ายกวาด แต่ไม่ได้ใช้ Character Reference)
- เครดิตที่ใช้รอบนี้ประมาณ 1,330 (83 prompt รวมที่สร้างซ้ำ 2 รายการ)

| asset | gen id |
| --- | --- |
| `app_icon` | `1f1de8d1-0e42-6c20-b62e-bb072013bb28` |
| `boss_phase` | `1f1de8d0-b88b-6660-9a44-0882011c3eb5` |
| `boss_rain` | `1f1de8d0-65f4-6d20-bb99-25856d25a38e` |
| `boss_shoot` | `1f1de8d0-159e-66a0-9fab-4a58462d9cea` |
| `cart_base` | `1f1de8b1-e4eb-60d0-895a-7c6366c88466` |
| `cart_shelf_lv1` | `1f1de8b3-0ade-6840-a566-a5296ed52b74` |
| `cart_shelf_lv2` | `1f1de8b3-5ce7-67e0-8faa-d17463d67b61` |
| `cart_shelf_lv3` | `1f1de8b3-af94-60b0-b2a7-a81e9c130800` |
| `cart_shelf_lv4` | `1f1de8b4-079f-61b0-addb-4817a10c2c28` |
| `cart_shelf_lv5` | `1f1de8b4-578c-6880-b1e5-8051c99267f8` |
| `decal_germ_1` | `1f1de8ba-9e7d-67c0-b7c7-dfc9a572b39b` |
| `decal_germ_2` | `1f1de8ba-f3b5-6e40-bddd-e2b7c5c41b53` |
| `decal_germ_3` | `1f1de8bb-4d69-6c20-8153-3feda106a11f` |
| `decal_mud_1` | `1f1de8b7-ac24-6b10-b67a-d068118ccf5c` |
| `decal_mud_2` | `1f1de8b7-fd12-6770-a4ef-1a09bc1f7156` |
| `decal_mud_3` | `1f1de8b8-51bf-6b60-8f95-110a5642a9fd` |
| `decal_slime_1` | `1f1de8b8-a528-6400-ad98-30cb44f946ee` |
| `decal_slime_2` | `1f1de8b9-05ed-6330-8099-3131dec2ff48` |
| `decal_slime_3` | `1f1de8b9-4cf4-6cb0-b920-423efbd56816` |
| `decal_trash_1` | `1f1de8b9-a139-60f0-b499-02fb9ff43415` |
| `decal_trash_2` | `1f1de8b9-f3ab-6040-8119-f839bf64f003` |
| `decal_trash_3` | `1f1de8ba-4d48-6e90-88bd-d0981687eddc` |
| `equip_broom` | `1f1de8c8-a934-6110-8759-9275d03b15b7` |
| `equip_bucket_hat` | `1f1de8b7-67c3-6cf0-aa12-062a0764225b` |
| `equip_mop` | `1f1de8c8-e4dd-6810-9b0c-0668b8504fb7` |
| `equip_sponge` | `1f1de8b6-51a9-6f60-bf1c-08cfbae8424e` |
| `equip_spray_bottle` | `1f1de8b6-f79f-6500-9cf3-f609c1e50205` |
| `fx_dust` | `1f1de8bc-43dc-6940-9368-3d4b43b26444` |
| `fx_foam` | `1f1de8bb-fa52-6e50-b86f-721264ba781c` |
| `fx_hitstar` | `1f1de8bc-ee30-6180-94db-71da38504ff2` |
| `fx_levelup` | `1f1de8bf-0138-6eb0-a7ed-b44d45c0b73d` |
| `fx_slimesplat` | `1f1de8be-69b8-6bd0-a161-c2e613fb9f82` |
| `fx_sparkle` | `1f1de8bc-9662-6110-bc65-4b29b67d15d3` |
| `fx_splash` | `1f1de8bb-9953-65a0-a1d1-fe5e0655ec5d` |
| `fx_spraymist` | `1f1de8bd-4031-6bf0-8cd7-68bc5d5890f6` |
| `fx_swipe` | `1f1de8bd-92d2-6170-b70d-5078b3872d4d` |
| `fx_tornado` | `1f1de8bd-e96c-6800-b008-9477938fefa0` |
| `icon_close` | `1f1de8c1-8e93-6380-827f-881f11222a23` |
| `icon_coin_stack` | `1f1de8c2-8b40-6470-9a21-1d1991e0b6f3` |
| `icon_equip_broom` | `1f1de8bf-418e-6a50-832b-77cfbd5158e0` |
| `icon_equip_mop` | `1f1de8bf-e88e-61c0-a737-b6889d81cb8f` |
| `icon_equip_sponge` | `1f1de8bf-9536-6a90-ad85-8154bb05d012` |
| `icon_equip_spray` | `1f1de8c0-3991-6db0-9a82-eddd6e0c666d` |
| `icon_gem_pile` | `1f1de8c2-de05-63e0-8a27-b6825e025b63` |
| `icon_home` | `1f1de8c1-4240-6a50-9d80-c597b8329305` |
| `icon_pause` | `1f1de8c0-8fc8-62b0-a549-046636896027` |
| `icon_settings` | `1f1de8c0-e56e-6700-a708-69547a54fe05` |
| `icon_shop` | `1f1de8c2-32b3-6d20-99a2-11b7b94e7462` |
| `icon_sound` | `1f1de8c1-e1dc-6050-a56a-c56da12b01db` |
| `item_upgrade_box` | `1f1de8c4-f18f-6760-90cf-b546fd8ec3b8` |
| `part_air` | `1f1de8ac-51e0-6c90-8d37-8103d0f22783` |
| `part_broom` | `1f1de8a9-abdd-6020-957c-14197edcd310` |
| `part_brush` | `1f1de8a9-5930-6750-b522-265becc3aa96` |
| `part_circle` | `1f1de8ae-14a1-6da0-932b-e1628896691e` |
| `part_cloth` | `1f1de8a8-c29f-6890-8e2d-fa9f6e2c9adc` |
| `part_hot` | `1f1de8ab-5876-6b00-8067-9871d8f46e72` |
| `part_polish` | `1f1de8ab-0541-66b0-a543-20e0f3c1637f` |
| `part_scrub` | `1f1de8ab-ff9a-6c60-bd05-5d42b8d69482` |
| `part_soak` | `1f1de8ac-a747-6940-8de4-66b92edc2b96` |
| `part_soap` | `1f1de8aa-102b-6b80-9e67-daea119fe196` |
| `part_soda` | `1f1de8aa-afba-6e30-ac95-02bb815bcc5b` |
| `part_sponge` | `1f1de8a9-04dd-68b0-9a8c-a57c86f6bd37` |
| `part_spray` | `1f1de8ac-fcab-6ee0-9873-ae04b7a32541` |
| `part_steam` | `1f1de8ab-abdf-63a0-ac6d-6d44488c9f98` |
| `part_vinegar` | `1f1de8aa-5d46-67d0-9500-eeb3764cea82` |
| `popup_confirm` | `1f1de8c4-5cfc-6cb0-a17c-c12321032258` |
| `popup_lose` | `1f1de8c3-9a68-6c80-88e4-269f819470f3` |
| `popup_pause` | `1f1de8c4-03af-6770-95e9-50c588c165af` |
| `popup_reward` | `1f1de8c4-ba3a-66c0-8cff-3d68b4b6e4d0` |
| `popup_win` | `1f1de8c3-5ccd-64c0-8a81-a7d8d51edd56` |
| `shelf_chem` | `1f1de8b5-0001-6880-8788-b5052e679c16` |
| `shelf_energy` | `1f1de8b5-51a6-6690-b1bd-103d5e83bea7` |
| `shelf_method` | `1f1de8b5-a72a-6800-a10e-9d9fffd76681` |
| `shelf_tool` | `1f1de8b4-ac68-62a0-90e1-76d1047e0581` |
| `splash_screen` | `1f1de8d3-9777-60c0-913d-c9f3f429e07d` |
| `tile_rubber_clean` | `1f1de8cc-e97a-6b30-a0da-122beb3a75a6` |
| `tile_rubber_dirty` | `1f1de8cd-3dea-6e90-a4d6-3c7647030b89` |
| `tile_stone_clean` | `1f1de8cb-9d57-6d80-8128-9c0688aa4d7b` |
| `tile_stone_dirty` | `1f1de8cb-f774-6b10-a583-5c4fb49b8861` |
| `tile_wood_clean` | `1f1de8cc-4119-63b0-add4-534f1c890eaf` |
| `tile_wood_dirty` | `1f1de8cc-9531-68d0-9762-b96e4ab02217` |

## รอบสร้างใหม่ตาม 12_RAW_Cutouts (12 พ.ย. 2026 เย็น)

สร้างใหม่เฉพาะภาพที่ตัดพื้นหลังไม่สวยหรือมีของเกิน ไฟล์ใหม่ใช้ชื่อเดิมต่อท้าย `_v2_<0-3>` หรือ `_v3_<0-3>` (ไฟล์เดิมยังอยู่ ไม่ได้ทับ) prompt ทุกชิ้นเติม `single isolated object, plain pure white background, no floor, no table, no shadow, no other objects` เพื่อให้ตัดพื้นหลังง่าย

| ไฟล์ใหม่ | แบบที่แนะนำ / หมายเหตุ |
| --- | --- |
| `parts/part_hot, polish, scrub, soak, soap, steam, vinegar _v2_*` | พื้นขาวล้วน ไม่มีคน scrub เปลี่ยนเป็นแผ่นใยขัด · polish_v2_0/1/3 ยังมีคำว่า "floor polish" บนฝา ใช้ `_v2_2` · soap_v2_2/3 มีหน้ายิ้ม ใช้ `_v2_0/1` |
| `ui/icon_gem_pile_v2_*` | กองเพชรสีฟ้า ไม่มีโถ |
| `ui/icon_skill_bubble / press / spray _v2_*` | ตรงกับอุปกรณ์: ฟองน้ำ+ฟองสบู่ / ไม้ถู+คลื่นน้ำ / ขวดสเปรย์ชมพู |
| `ui/popup_pause_v2_1`, `ui/popup_reward_v2_*` | ไม่มีตัวอักษร (แบบอื่นของ pause ยังมีคำว่า PAUSE) |
| `ui/popup_confirm / lose / win _v3_*` | รอบที่ 3 ไม่มีตัวอักษรทุกแบบ (รอบ v2 ยังมีคำ Confirm/DEFEAT/VICTORY อย่าใช้) |
| `cart/shelf_tool / chem / energy / method _v3_*` | ชั้นว่างทุกแบบ ไม่มีตุ๊กตา (รอบ v2 ยังมีตุ๊กตา อย่าใช้) |
| `tiles/tile_rubber_clean_v2_*` | ไม่มีของวาง |
| `lobby_backgrounds/lobby_rental_room, condo, coffee_shop, kitchen _0-3` | ฉากใหม่ 16:9 ตาม GDD (ร้านกาแฟมีป้ายเมนูตัวเล็ก) |

ไม่ได้ทำ: science_lab / halloween (ข้ามตามที่สั่ง), sprite sheet ท่าฮีโร่/ศัตรู (ต้องทำเอง) · เครดิตรอบนี้ประมาณ 520 (32 prompt) เหลือราว 2,900 · zip: `raw_parts/leonardo_regen.zip`

| asset | รอบ | gen id |
| --- | --- | --- |
| `icon_gem_pile` | v2 | `1f1deaed-136c-67a0-9210-e5a7b4966e10` |
| `icon_skill_bubble` | v2 | `1f1deaed-6bd1-6df0-a885-73c1eaa2c800` |
| `icon_skill_press` | v2 | `1f1deaed-b905-60e0-bfb2-8b2383d2e31b` |
| `icon_skill_spray` | v2 | `1f1deaee-3a67-69d0-bd59-31fd596204f5` |
| `lobby_coffee_shop` | new | `1f1deaf8-57de-6720-8142-ce1e7fa16484` |
| `lobby_condo` | new | `1f1deaf8-0a9c-69d0-a9c2-49e177fd5476` |
| `lobby_kitchen` | new | `1f1deaf8-b6da-6da0-8133-c9467fd9a620` |
| `lobby_rental_room` | new | `1f1deaf7-c25a-6140-8f67-56869631aa32` |
| `part_hot` | v2 | `1f1deaea-91ad-65e0-badd-8913c82b0651` |
| `part_polish` | v2 | `1f1deaea-e0de-6ce0-8dff-ac0e010bdf01` |
| `part_scrub` | v2 | `1f1deaeb-6160-6c10-8540-d0a7df27877e` |
| `part_soak` | v2 | `1f1deaeb-b0be-6230-8b74-884b89a07b72` |
| `part_soap` | v2 | `1f1deaec-09b3-6930-b895-4f88242fc9ad` |
| `part_steam` | v2 | `1f1deaec-6d80-6e40-ade3-12aae83ba248` |
| `part_vinegar` | v2 | `1f1deaec-be25-66c0-a145-aaa113ad1a34` |
| `popup_confirm` | v2 | `1f1deaef-cd1c-6e00-b418-14026e16c11c` |
| `popup_confirm (v3)` | v3 | `1f1deafc-f14d-6c90-8f0b-1c576e813333` |
| `popup_lose` | v2 | `1f1deaef-22d0-6af0-a5cf-88af492e46ce` |
| `popup_lose (v3)` | v3 | `1f1deafd-8c04-6b80-91d1-d25e71b97b7e` |
| `popup_pause` | v2 | `1f1deaef-775e-6310-9ebd-b42f67a45d0a` |
| `popup_reward` | v2 | `1f1deaf0-2442-6720-a352-892989f16dab` |
| `popup_win` | v2 | `1f1deaee-cb27-6470-9c93-5a4fb2ad520f` |
| `popup_win (v3)` | v3 | `1f1deafe-2133-6a30-8ff1-54679ee7376b` |
| `shelf_chem` | v2 | `1f1deaf0-d057-62e0-999b-bf750e067b87` |
| `shelf_chem (v3)` | v3 | `1f1deafb-5d8b-6f80-bfeb-2d8bbe0dcd40` |
| `shelf_energy` | v2 | `1f1deaf1-59ed-6590-ba07-b92cd5106d59` |
| `shelf_energy (v3)` | v3 | `1f1deafe-86fc-6c40-b446-1bf8bae6ad18` |
| `shelf_method` | v2 | `1f1deaf1-abdb-6780-af33-c9e2ab22ba91` |
| `shelf_method (v3)` | v3 | `1f1deafc-90a8-6930-a653-90905e3bb803` |
| `shelf_tool` | v2 | `1f1deaf0-78e8-65e0-9188-d91280c023b4` |
| `shelf_tool (v3)` | v3 | `1f1deafa-fcb5-6ee0-8321-e9d55a296038` |
| `tile_rubber_clean` | v2 | `1f1deaf4-df06-6800-a764-95fc42df50b0` |

## โครงสร้างไฟล์

ชื่อไฟล์: `<asset>_<0-3>.jpg` ตัวเลขท้าย = แบบที่ 1–4 ไฟล์ `_alt1_` คือชุดที่สร้างซ้ำ

| โฟลเดอร์ | เนื้อหา |
| --- | --- |
| `heroes/sweep, bubble, press, spray/` | master, idle, walk_a, walk_b, run, carry, work, skill, hit, cheer, defeat, back, side + face_happy/wink/angry/surprised/cry/effort/dizzy/joy |
| `enemies/` | stain, grease, trash, germ, boss, dust, rust, scale × idle/attack/hit/defeat + boss_taunt, boss_slam |
| `items/` | item_coin, star, gem, heart, stopwatch, magnet, bubble, chest_closed, chest_open, trophy |
| `props/` | prop_ + 22 ชิ้นตาม `02_Asset_List.md` |
| `ui/` | ui_button_play/blue/skill/round, ui_portrait_frame, ui_ribbon, ui_panel_blue, icon_skill_<hero>, logo_bob_base (ไม่มีตัวอักษร) |
| `backgrounds/` (16:9) | bg_classroom, cafeteria, playground, boiler, school + `_dirty` |
| `lobby_backgrounds/` (16:9) | ฉากหลังล็อบบี้ 22 ฉาก เวทีว่าง (18 ฉากแรก + ห้องเช่า คอนโด ร้านกาแฟ ครัว) |
| `parts/`, `cart/`, `equipment/`, `decals/`, `tiles/`, `vfx/` | ชุดเติมส่วนที่ขาด (ดูหัวข้อด้านบน) |
| `screens/` (16:9) | screen_login, screen_loading (download), screen_lobby, screen_menu, screen_character_select, screen_stage_select, screen_result, transition_frame, transition_wipe |

## ข้อควรระวัง

- สีหน้าของกวาดออกมาเต็มตัว (Character Reference บังคับทรง) ต้องครอปหน้าเอง; ท่า carry ของกวาด กล่องอยู่บนหลังแทนชูเหนือหัว
- ฟอง/ถู/สเปรย์ไม่มี Character Reference รายละเอียดชุดต่างกันเล็กน้อยระหว่างท่า; ไม้ถูของถูบางภาพเป็นไม้กวาด
- screens บางหน้ามีตัวอักษรมั่วติดมา; prop_bucket มีหน้าตา, prop_window มีแมว ควรสร้างใหม่ถ้าจะใช้
- ภาพบน CDN ของ Leonardo เปิดได้สาธารณะที่ `https://cdn.leonardo.ai/users/<user id>/generations/<gen id>/phoenix-v1.0_<prompt 80 ตัวอักษรแรก แทนช่องว่าง/เครื่องหมายด้วย _>-<0-3>.jpg`

## Generation id (สำหรับหาในประวัติ Leonardo หรือดาวน์โหลดซ้ำ)

| asset | gen id |
| --- | --- |
| `bg_boiler` | `1f1de820-25ed-6480-9743-00820d43130c` |
| `bg_boiler_dirty` | `1f1de820-7800-6060-8f50-52d280f301ed` |
| `bg_cafeteria` | `1f1de81e-db51-60d0-a828-ca369a4cc1a3` |
| `bg_cafeteria_dirty` | `1f1de81f-4e76-6970-8864-b71c329b3669` |
| `bg_classroom` | `1f1de81e-36fa-6bd0-80c0-834b2f9ee137` |
| `bg_classroom_dirty` | `1f1de81e-8996-6330-83bf-aa4bce326880` |
| `bg_playground` | `1f1de81f-807e-6350-932f-90305487406d` |
| `bg_playground_dirty` | `1f1de81f-d31c-61c0-b0e0-dd02d506b731` |
| `bg_school` | `1f1de820-cb83-66b0-86b8-8eb165e27e8d` |
| `bg_school_dirty` | `1f1de821-2052-6d80-80f7-e5cd3ad016ce` |
| `boss_attack` | `1f1de7ea-eaf0-6fb0-a7eb-e64bac935165` |
| `boss_defeat` | `1f1de7eb-908c-6000-9011-73354028f9df` |
| `boss_hit` | `1f1de7eb-3b62-63e0-b1d7-f736d143dc8c` |
| `boss_idle` | `1f1de7ea-9615-6590-a370-5a7cdcc9fc41` |
| `boss_slam` | `1f1de7ec-6bee-61c0-9fd8-da41d8e1da53` |
| `boss_taunt` | `1f1de7eb-f53c-65e0-af2c-300bfff5e006` |
| `bubble_back` | `1f1de802-2395-6980-93e6-cb9b6049ee1b` |
| `bubble_carry` | `1f1de800-3236-6ef0-a01d-9866ce8126fa` |
| `bubble_cheer` | `1f1de801-817f-6740-9e2e-9bed086b7b4b` |
| `bubble_defeat` | `1f1de801-ea53-6640-a162-ea4ca55970eb` |
| `bubble_face_angry` | `1f1de803-6f87-69f0-a029-48fbb773ec06` |
| `bubble_face_cry` | `1f1de804-1a57-6a60-9333-f5a8d262ec43` |
| `bubble_face_dizzy` | `1f1de804-b99d-6930-bfb6-5a5088734933` |
| `bubble_face_effort` | `1f1de804-674d-6cc0-ac40-adc63ece0b06` |
| `bubble_face_happy` | `1f1de802-cb84-6510-9fbf-acf9bffa30f7` |
| `bubble_face_joy` | `1f1de805-0d78-6dc0-9519-083a5e111016` |
| `bubble_face_surprised` | `1f1de803-c1ed-65f0-97b8-4c6c14c9cd5d` |
| `bubble_face_wink` | `1f1de803-1c30-62c0-805d-22e1a6a62a93` |
| `bubble_hit` | `1f1de801-2b30-6ba0-9a0b-67a5a6e25006` |
| `bubble_idle` | `1f1de7fe-e36a-6ed0-926c-f1e7246565c3` |
| `bubble_master` | `1f1de81a-2841-6a70-af43-a2613eb8e501` |
| `bubble_run` | `1f1de7ff-dcd5-6060-9b93-938bce9ce07e` |
| `bubble_side` | `1f1de802-7a10-6440-a6e5-4e31499fa6a0` |
| `bubble_skill` | `1f1de800-da20-6c60-b740-4a5c37b9841e` |
| `bubble_walk_a` | `1f1de7ff-3757-64d0-9918-ffd124592737` |
| `bubble_walk_b` | `1f1de7ff-8e97-6ba0-93d3-80ef586bfd9c` |
| `bubble_work` | `1f1de800-84dc-6290-86c9-45edee09c1ae` |
| `dust_attack` | `1f1de7ed-141e-6c00-9bd3-70c73d6b58de` |
| `dust_defeat` | `1f1de7ed-b7aa-66d0-ad17-c0956884be04` |
| `dust_hit` | `1f1de7ed-67d5-66a0-8af6-5903efd11977` |
| `dust_idle` | `1f1de7ec-becb-67d0-b951-baf2e59211f2` |
| `germ_attack` | `1f1de7e9-aab2-68f0-bb4e-bd346deab3f5` |
| `germ_defeat` | `1f1de7ea-423c-6810-8bfb-b0145c2ce5f5` |
| `germ_hit` | `1f1de7e9-ef5c-6af0-bc91-a6297b39a778` |
| `germ_idle` | `1f1de7e8-7983-61e0-8459-627fff9c344d` |
| `grease_attack` | `1f1de818-85de-6b80-bbab-9034d4d72b58` |
| `grease_defeat` | `1f1de819-2c3f-67e0-b2dd-1f359f788849` |
| `grease_hit` | `1f1de818-d90f-61b0-9c71-a7a549ca3113` |
| `grease_idle` | `1f1de7e5-dd4a-6aa0-877a-feef32d13f41` |
| `icon_skill_bubble` | `1f1de7fd-97a4-6d80-bc62-397e4130f106` |
| `icon_skill_press` | `1f1de7fd-ec7b-6980-b66e-9e8dd165ad05` |
| `icon_skill_spray` | `1f1de7fe-3cb9-6960-8842-994e5463291f` |
| `icon_skill_sweep` | `1f1de7fd-41d7-6830-8f65-49bc65cf6814` |
| `item_bubble` | `1f1de7f2-a0e2-6920-880b-cf521afddd49` |
| `item_chest_closed` | `1f1de7f2-ea2f-6380-94c5-f55207c43cd2` |
| `item_chest_open` | `1f1de7f3-3b97-6100-87d3-e5c650135e82` |
| `item_coin` | `1f1de7f0-a20f-6160-ab3a-9b4db2b77f76` |
| `item_gem` | `1f1de7f1-4d4a-6890-b687-6fbb8d9ba3e8` |
| `item_heart` | `1f1de7f1-9e0c-65d0-9353-2dd7cfcd95a2` |
| `item_magnet` | `1f1de7f2-4367-6e80-8fa9-03e1df76d3b4` |
| `item_star` | `1f1de7f0-f602-6c90-b039-935794f34af9` |
| `item_stopwatch` | `1f1de7f2-0947-6890-aa21-3ace9be8006c` |
| `item_trophy` | `1f1de7f3-8e80-6a60-8fc2-6a068ac5309c` |
| `logo_bob_base` | `1f1de7fe-91de-6760-8c1a-259d8ac5dea2` |
| `press_back` | `1f1de808-a596-62d0-89d3-52e5d93cad6c` |
| `press_carry` | `1f1de806-af70-65f0-aaa0-0e482255d17d` |
| `press_cheer` | `1f1de808-0227-6cc0-bb4b-38e7f40caa22` |
| `press_defeat` | `1f1de81a-7a54-6650-88f6-c470267e7df2` |
| `press_face_angry` | `1f1de809-fb5a-6da0-9a74-e1533b535e44` |
| `press_face_cry` | `1f1de80a-9d62-6580-9a83-107bab4ffe9f` |
| `press_face_dizzy` | `1f1de80b-4309-6920-87e0-bb33f3724c71` |
| `press_face_effort` | `1f1de80a-ef94-6d30-bc85-a2be36309cab` |
| `press_face_happy` | `1f1de809-5127-6130-9564-97d7a5dd5380` |
| `press_face_joy` | `1f1de80b-921d-6b60-9fc4-04542ace63e7` |
| `press_face_surprised` | `1f1de80a-512f-6820-a3b3-388c9f7f0f61` |
| `press_face_wink` | `1f1de809-a47e-6860-825a-17e0fe66dd85` |
| `press_hit` | `1f1de807-ab04-6ab0-a356-b195022d6fc3` |
| `press_idle` | `1f1de805-5fea-6d10-894b-218b738cb7fa` |
| `press_master` | `1f1de7e3-9881-6060-9ff5-9e6a84a9ff3c` |
| `press_run` | `1f1de806-5fce-6a10-a994-c21c132778da` |
| `press_side` | `1f1de808-fa63-6290-a0b5-606d01ff3dbf` |
| `press_skill` | `1f1de807-5534-6e50-8bc8-42096ddc1de0` |
| `press_walk_a` | `1f1de805-b431-6860-b8fb-3e0e4009eae5` |
| `press_walk_b` | `1f1de806-0d7e-6da0-99c4-90d9ecee5908` |
| `press_work` | `1f1de807-0183-61d0-bda9-9b550424c1de` |
| `prop_bench` | `1f1de7f9-611d-6540-aa71-f98956967305` |
| `prop_blackboard` | `1f1de7f7-1c8b-6d70-b306-682331cb30f5` |
| `prop_boss_door` | `1f1de7fa-5f33-6b70-ab06-aaf9d9307c8b` |
| `prop_bucket` | `1f1de7f4-85c2-6fd0-a94b-cc8ca1726689` |
| `prop_bush` | `1f1de7fa-069d-67e0-9e8d-39f44be5f82e` |
| `prop_cafe_chair` | `1f1de7f8-6882-6c00-877e-a4c72f05896c` |
| `prop_cafe_table` | `1f1de7f8-1ad5-67f0-a895-840f8bf6562d` |
| `prop_chair` | `1f1de7f6-cb30-6340-862d-ae36d6cfd51c` |
| `prop_cone` | `1f1de7f4-3397-6d50-b911-b9d7873e3806` |
| `prop_desk` | `1f1de7f6-78f1-6840-bf75-87f06b41bcfe` |
| `prop_food_stall` | `1f1de7f8-bddd-6560-a4c8-fa2d210eaa71` |
| `prop_locker` | `1f1de7f7-6eb9-6700-b1c6-c2de1da7b8d5` |
| `prop_pipe` | `1f1de7fa-aacf-62f0-99c2-9b584283b8c5` |
| `prop_recycle_bin` | `1f1de7f5-7ebc-6c80-acfb-7b8104393ef1` |
| `prop_slide` | `1f1de7f9-0d46-6ed0-8ab3-25e9f598c99e` |
| `prop_spray_blue` | `1f1de7f6-2407-63c0-9de6-9d14f91a9504` |
| `prop_toilet_paper` | `1f1de7f5-d2eb-6130-b51a-c878368f4541` |
| `prop_trash_bag` | `1f1de7f4-d8b6-6570-ba56-13a45fe8dd88` |
| `prop_trash_bin` | `1f1de7f5-2b23-66a0-987d-1fb3f68185ed` |
| `prop_tree` | `1f1de7f9-b312-6c60-8b85-2fc0df8534c0` |
| `prop_wet_sign` | `1f1de7f3-e422-6090-a8e5-d58871605f2e` |
| `prop_window` | `1f1de7f7-c2cf-6510-aac0-50243e3a028a` |
| `rust_attack` | `1f1de7ee-5f57-63b0-8d33-4f1b9952733c` |
| `rust_defeat` | `1f1de7ef-054f-6060-8de9-8c3a36cd9388` |
| `rust_hit` | `1f1de7ee-b239-67e0-a99e-406e59afe408` |
| `rust_idle` | `1f1de7ee-09ff-6160-bcba-732d60429fd4` |
| `scale_attack` | `1f1de7ef-a9fd-63a0-b4da-17095d708624` |
| `scale_defeat` | `1f1de7f0-4eb7-6a30-a1f7-28ce75c11f61` |
| `scale_hit` | `1f1de7f0-017a-6b00-844b-791ac50a8b0d` |
| `scale_idle` | `1f1de7ef-580c-6aa0-99f4-37ed54ca3671` |
| `screen_character_select` | `1f1de822-bdbb-65d0-9f5a-5d4ab19acbc4` |
| `screen_loading` | `1f1de821-c899-6750-9c4b-f8a4d373873e` |
| `screen_lobby` | `1f1de822-17fb-6b90-9b91-81c7e2757084` |
| `screen_login` | `1f1de821-8c9f-6740-b514-a1cc5a766847` |
| `screen_menu` | `1f1de822-6aaf-6990-b1d3-32224be6fa75` |
| `screen_result` | `1f1de823-69e3-6a10-9f14-30a5d38603de` |
| `screen_stage_select` | `1f1de823-0fb0-6cf0-88ca-b679ffde453b` |
| `spray_back` | `1f1de80f-2b8b-6f10-a72c-362b8fe2c9a6` |
| `spray_carry` | `1f1de80d-3406-6930-8eb8-203ab770c79b` |
| `spray_cheer` | `1f1de80e-907d-61e0-a4a1-7d3f6b027b56` |
| `spray_defeat` | `1f1de80e-dccf-6b10-9f00-4898095a211b` |
| `spray_face_angry` | `1f1de810-7a5f-6460-8fab-a834ea4370f1` |
| `spray_face_cry` | `1f1de811-21e0-6220-88b2-f5408c6d2d3a` |
| `spray_face_dizzy` | `1f1de811-c8df-6990-9434-09a41c2da401` |
| `spray_face_effort` | `1f1de811-8597-67a0-99e9-0407b206aea2` |
| `spray_face_happy` | `1f1de80f-d2e8-62e0-bc82-d4d3eee48258` |
| `spray_face_joy` | `1f1de812-1cf7-6eb0-a1d3-2d86449abfce` |
| `spray_face_surprised` | `1f1de810-d06c-6150-b374-256e04bca0d4` |
| `spray_face_wink` | `1f1de810-287f-6cd0-b5f7-003efc495871` |
| `spray_hit` | `1f1de80e-2ede-6e20-88f5-24dbf2529979` |
| `spray_idle` | `1f1de80b-e5f1-6ac0-b703-048be41b6ac2` |
| `spray_master` | `1f1de7e3-ecc0-6680-9c75-6b041cb70a94` |
| `spray_master` | `1f1de7e4-3d78-6780-b06d-f0440ec2be37` |
| `spray_run` | `1f1de80c-e1c0-6900-a4bd-f5b2459f9fbe` |
| `spray_side` | `1f1de80f-7fb5-65a0-8e16-6c1e17109bff` |
| `spray_skill` | `1f1de80d-e2fc-69d0-a1c3-8ecef3dbdfeb` |
| `spray_walk_a` | `1f1de80c-3cbf-65a0-9f87-bbe4d86e995b` |
| `spray_walk_b` | `1f1de80c-8df3-6ed0-909a-da1bb54a271d` |
| `spray_work` | `1f1de80d-90a5-6830-8602-a775471ca77c` |
| `stain_attack` | `1f1de7e4-e654-6020-a527-75a572d6b51b` |
| `stain_defeat` | `1f1de7e5-89bd-6810-845c-04f54fcec03b` |
| `stain_hit` | `1f1de7e5-36e9-6e40-8d43-37fd04b0b0ba` |
| `stain_idle` | `1f1de7e4-90c8-6980-b444-92a980aca81f` |
| `sweep_back` | `1f1de7d4-c913-68e0-9960-2b10ae36a431` |
| `sweep_carry` | `1f1de7d2-24cc-61d0-9f2f-2f242d9704bb` |
| `sweep_carry_alt` | `1f1de787-f553-6b50-9f9a-c849f116f3f4` |
| `sweep_cheer` | `1f1de7c1-0155-6320-ac88-c2f9989414a0` |
| `sweep_cheer_alt` | `1f1de7c1-aa79-6fa0-9ec3-11d899775b8f` |
| `sweep_defeat` | `1f1de7d4-801c-65b0-98b0-309838f24e6a` |
| `sweep_defeat_alt` | `1f1de78e-f2fb-6900-ab03-b68aa244a5fc` |
| `sweep_face_angry` | `1f1de7d6-3646-6c40-91b4-ab2e0e3708fb` |
| `sweep_face_cry` | `1f1de7d6-c9cf-6520-a2e6-542dd749b5af` |
| `sweep_face_dizzy` | `1f1de7d7-6910-65d0-b885-71a3adc42194` |
| `sweep_face_effort` | `1f1de7d7-125b-6190-a265-eddea4a5e2ca` |
| `sweep_face_happy` | `1f1de7d8-420d-6ae0-a96c-4367001b3dd9` |
| `sweep_face_joy` | `1f1de7d7-a51d-6e60-a992-2860bd463388` |
| `sweep_face_surprised` | `1f1de7d6-7f9d-62e0-bc3a-c8f80fb1c6fc` |
| `sweep_face_wink` | `1f1de7d5-ee5c-61f0-ad1a-3fe9238a2656` |
| `sweep_hit` | `1f1de7bf-90dc-6950-ac2b-3f2148c68920` |
| `sweep_idle` | `1f1de7b8-2697-68d0-923a-c9b7dae03413` |
| `sweep_master` | `1f1de797-9ce2-6c40-9690-c0fac3aebb1b` |
| `sweep_run` | `1f1de7c2-e6ea-6470-b003-b45146f945b5` |
| `sweep_side` | `1f1de7d5-13ee-6270-9226-9bd90a81ac14` |
| `sweep_skill` | `1f1de7bc-3d69-6640-ac1b-0c4fa866408b` |
| `sweep_walk_a` | `1f1de7b9-8f4f-64d0-be4b-33a009f99ece` |
| `sweep_walk_b` | `1f1de7d5-59ce-6560-86e4-546ebf8f89a8` |
| `sweep_work` | `1f1de7d3-a49f-6640-a843-64fb46786d92` |
| `transition_frame` | `1f1de823-b618-6e80-b5bd-60c3ce9bca41` |
| `transition_wipe` | `1f1de824-0a5f-69d0-91f7-b9ea9a50a8f3` |
| `trash_attack` | `1f1de819-d215-61b0-995d-bd20fc9bd7be` |
| `trash_defeat` | `1f1de7e8-2471-6c60-9a43-9247b3034b4a` |
| `trash_hit` | `1f1de7e7-d268-6cc0-9db3-f211aef6288d` |
| `trash_idle` | `1f1de819-7f24-6320-bc01-38a186319dad` |
| `ui_button_blue` | `1f1de7fb-5071-6870-b3f0-59b1fafda158` |
| `ui_button_play` | `1f1de7fa-fe92-60e0-a51f-d36819eae4bf` |
| `ui_button_round` | `1f1de7fc-0c9a-6790-9fd8-0e746996c66f` |
| `ui_button_skill` | `1f1de7fb-a5a5-60d0-a753-cbb25377f7aa` |
| `ui_panel_blue` | `1f1de7fc-f0b1-6960-bfc5-cfab15fceb2d` |
| `ui_portrait_frame` | `1f1de7fc-4b3d-6a10-b695-e4ac131ff800` |
| `ui_ribbon` | `1f1de7fc-9d0c-6030-9558-c9f4e5298950` |
