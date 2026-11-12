# 02 · Game Asset List (6 กลุ่ม)

อ้างอิง: `concept/BOB_Game_Art_Board.png`, `concept/BOB_Asset_Sheet.png`
ลำดับความสำคัญ: **P0** = Vertical Slice (1 ด่าน เล่นครบ) · **P1** = Soft Launch · **P2** = หลังเปิดตัว / event
"Placeholder" = มีไฟล์ชั่วคราวใน `bob-art/unity/Assets/Art/` แล้ว (ใช้ทดสอบได้ รอเปลี่ยน โดยคง id เดิม)
**ตัดสินใจแล้ว (12 พ.ย. 2026): คงเกมจัดชั้นรถเข็น** แถวที่โหมดเป็น "Action" ไม่ต้องผลิต (เก็บไว้เผื่ออนาคต) ส่วน HUD ใช้แถบเวลา/งบ/ค่าสถานะ 4 ค่าแทน HP/ATK/DEF และ pickup ใช้เหรียญ/ดาวเป็นรางวัลหลังรอบ

ช่อง "โหมด" : **ทั้งคู่** = ใช้ทั้งเกมแบบ concept (แอ็กชันทำความสะอาด) และเกมจัดชั้นรถเข็นในโค้ดปัจจุบัน · **Action** = เฉพาะแบบ concept · **Stack** = เฉพาะแบบรถเข็น (ดู `05_Roster_Mapping.md`)

## A. Character Assets (4 Heroes: กวาด, ฟอง, ถู, สเปรย์)

| รหัส | Asset | รวม | Pri | โหมด | Placeholder |
| --- | --- | --- | --- | --- | --- |
| A01 | Character Master Design Sheet (มาตรฐานเดียวกับ sheet กวาด) | 4 (กวาดมีแล้ว) | P0 | ทั้งคู่ | กวาด ✓ |
| A02 | Orthographic modeling reference (front/side/back ขนาดจริง) | 4 | P0 | ทั้งคู่ | – |
| A03 | 3D Model + texture 1024² + face atlas 512² | 4 | P0 | ทั้งคู่ | sprite 2D |
| A04 | Shared Hero Rig (skeleton เดียว) | 1 | P0 | ทั้งคู่ | – |
| A05 | อนิเมชันร่วม: Idle, Walk, Run, Hit, Victory, Defeat | 6 | P0 | ทั้งคู่ | idle/walk/cheer |
| A06 | อนิเมชันต่อสู้: Attack ×3 combo ต่อ Hero, Skill ต่อ Hero, Dodge | 4×5 | P0 | Action | – |
| A07 | อนิเมชันจัดของ: PickUp, Carry_Idle, Carry_Walk, Place, Work | 5 | P0 | Stack | carry/work |
| A08 | Portrait วงกลม (HUD) + การ์ดเลือกตัว (พื้นสีประจำตัว) | 4+4 | P0 | ทั้งคู่ | portrait |
| A09 | Full-body render (หน้า Upgrade / Character Select) | 4 | P1 | ทั้งคู่ | – |
| A10 | Equipment upgrade 3 ระดับต่อ Hero (mesh swap + FX) | 12 | P1 | Action | – |
| A11 | Skin/Costume | 8 | P2 | ทั้งคู่ | – |

## B. Enemy Assets

| รหัส | Asset | รวม | Pri | โหมด | Placeholder |
| --- | --- | --- | --- | --- | --- |
| B01 | เจ้าคราบ (Dirt Monster) model + anim (Idle/Move/Attack/Hit/Defeat/Spawn) | 1 + 6 | P0 | ทั้งคู่ | stain |
| B02 | สไลม์ (Slime) | 1 + 6 | P0 | ทั้งคู่ | grease (สีต่าง) |
| B03 | ขยะปีศาจ (Trash Monster) | 1 + 6 | P1 | ทั้งคู่ | – |
| B04 | เชื้อโรค (Germ) | 1 + 6 | P1 | ทั้งคู่ | – |
| B05 | Mini variants (สี/ขนาด/หมวก ต่างกัน สำหรับด่านหลัง) | 4–8 | P1 | Action | – |
| B06 | **บอสคราบยักษ์** model + rig + 8 anim (Idle, Taunt, Slam, Shoot, Rain, Hit, Phase, Defeat) | 1 | P1 | ทั้งคู่ | – |
| B07 | Dirt decal บนพื้น/ผนัง (โคลน, สไลม์, ขยะ, เชื้อโรค) 3 แบบต่อชนิด | 12 | P0 | ทั้งคู่ | floor_tile_dirty |
| B08 | ศัตรูเดิมในโค้ด: Dust, Rust, Scale (ถ้ายังใช้กฎปฏิกิริยาเดิม) | 3 | P1 | Stack | ✓ |

## C. Equipment, Props & Items (ตามแผง "Props & Items" / "Game Assets")

| รหัส | Asset | รวม | Pri | โหมด |
| --- | --- | --- | --- | --- |
| C01 | อุปกรณ์ Hero: ไม้กวาด, ฟองน้ำ, ไม้ถู, ขวดสเปรย์ (+ ถังที่ใช้เป็นหมวก) | 5 | P0 | ทั้งคู่ |
| C02 | Pickups: เหรียญ, ดาว, เพชร, หัวใจ, นาฬิกาจับเวลา, แม่เหล็ก, ฟองสบู่ (2 แบบ) | 8 | P0 | Action |
| C03 | Props ในฉาก: ป้ายพื้นเปียก, กรวยส้ม, ถังน้ำฟ้า, ถุงขยะดำ, ถังขยะ, ถังรีไซเคิล, ม้วนทิชชู, ขวดน้ำยาฟ้า | 8 | P0 | ทั้งคู่ |
| C04 | ชิ้นส่วน 15 ชิ้น (cloth … circle) 3D + icon | 15 | P0 | Stack |
| C05 | รถเข็น + ชั้น 5 ระดับ, ชั้นวาง 4 หมวด | 6 | P0 | Stack |
| C06 | หีบรางวัล (ปิด/เปิด), ถ้วยรางวัล, กล่องอัปเกรด | 4 | P1 | ทั้งคู่ |

## D. Environment Assets (ตามแถว Game Environment)

| รหัส | ฉาก | ชุดที่ต้องมี | Pri |
| --- | --- | --- | --- |
| D01 | โรงเรียน (ภายนอก) — ใช้เป็นหน้า Stage Select / Lobby | อาคาร 3 ชั้นมีนาฬิกา, ต้นไม้, รั้ว, ท้องฟ้า | P1 |
| D02 | ห้องเรียน | โต๊ะเรียน, เก้าอี้, กระดาน, หน้าต่าง, ตู้, กรวย | **P0 (ด่านแรก)** |
| D03 | โรงอาหาร | โต๊ะยาว, เก้าอี้สี, ร้านมีกันสาด, ถาด, ถังขยะ | P1 |
| D04 | สนามเด็กเล่น (outdoor) | สไลเดอร์, ชิงช้า, ต้นไม้, พื้นทราย/หญ้า | P1 |
| D05 | ห้องใต้ดิน / ห้องท่อ (ด่านบอส) | ท่อ, ผนังหิน, ประตูม่วงเรืองแสง, สไลม์เขียวบนพื้น | P1 |
| D06 | **Dirty / Clean variant ทุกฉาก** (พื้นสกปรก/พื้นสะอาด) | material `_DirtAmount` + decal | P0 |
| D07 | Tileset พื้น: กระเบื้องหิน, ไม้, ยางสนาม (สะอาด/สกปรก) | 3×2 | P0 |

ขนาดด่านมาตรฐาน: 14 × 8 unit (landscape), tile 1 unit, กล้อง orthographic size 4.5

## E. VFX Assets (ตามแผง "Skills & Effects" / "VFX & Particles")

| รหัส | Effect | เทคนิค | Pri |
| --- | --- | --- | --- |
| E01 | Water splash / คลื่นน้ำ (skill ฟอง, ถู) | Particle + mesh trail | P0 |
| E02 | Foam / Bubble (ฟองลอย, ฟองแตก) | Particle sprite | P0 |
| E03 | Dust & Dirt (ฝุ่นฟุ้ง, โคลนกระเด็น) | Flipbook | P0 |
| E04 | Swipe slash ทอง (กวาด attack) | Mesh trail + shader | P0 |
| E05 | Tornado (skill กวาด) | Mesh หมุน + particle | P0 |
| E06 | Spray mist ระยะไกล (สเปรย์) | Particle cone | P0 |
| E07 | Hit (ดาวแตก + ประกาย) | Flipbook | P0 |
| E08 | Sparkle clean (พื้นสะอาด, ศัตรูหาย) | Particle | P0 |
| E09 | Slime splat / พื้นเหนียว | Decal + particle | P1 |
| E10 | Level up / Upgrade glow | Particle + UI | P1 |
| E11 | Room clean wipe (คลื่นความสะอาดทั้งด่าน) | Shader dissolve | P1 |

ใช้ **Particle System (Shuriken)** เป็นหลัก — VFX Graph ใช้ได้เฉพาะเครื่องที่รองรับ compute shader จึงไม่แนะนำเป็นค่าเริ่มต้นบนมือถือ

## F. UI Assets (ตามแผง Game UI / Character Select / Stage Select / Upgrade / UI Elements)

| รหัส | Asset | Pri |
| --- | --- | --- |
| F01 | โลโก้ BOB (แบนเนอร์แดง + วงกลมเหลือง/ฟ้า + ไม้กวาด/ไม้ถูไขว้) + เวอร์ชันเล็ก | P0 |
| F02 | หน้า Title: ปุ่ม PLAY (เหลืองทอง), SETTINGS, SHOP (ฟ้า) | P0 |
| F03 | Character Select: การ์ด 4 ใบพื้นสีประจำตัว + กรอบเลือก | P0 |
| F04 | Stage Select: การ์ดด่าน + ดาว 3 ดวง | P0 |
| F05 | Upgrade: portrait, Lv, ดาว 5, แถบ HP/ATK/DEF, ปุ่ม Upgrade, ราคาเหรียญ | P1 |
| F06 | HUD: ปุ่มหยุด, portrait 4 ตัว + HP bar, เหรียญ, หัวใจ, ดาว | P0 |
| F07 | ปุ่ม skill ใหญ่ (วงกลมฟ้า) + ปุ่มอุปกรณ์ 2 ปุ่ม | P0 |
| F08 | Icons: skill 4, อุปกรณ์ 4, ค่าสถานะ, สกุลเงิน (เหรียญ/เพชร) | P0 |
| F09 | Popup: ชนะ/แพ้, รางวัล, หยุดเกม, ยืนยัน | P0 |
| F10 | App icon 1024 + Splash | P1 |
| F11 | Font ไทย+อังกฤษ หัวกลมหนา (เช่น Mitr, Kanit Bold) — ต้องเป็นฟอนต์ OFL | P0 |

## สรุปปริมาณ P0

| กลุ่ม | ประมาณ |
| --- | --- |
| A | 4 model, 1 rig, ~31 animation, 4 face atlas, 8 portrait/card |
| B | 2 ศัตรู + 12 animation, 12 decal |
| C | 5 อุปกรณ์, 8 pickup, 8 prop (+15 ชิ้นส่วนถ้าโหมด Stack) |
| D | 1 ห้องเรียน × 2 สถานะ + tileset |
| E | 8 effect |
| F | ~60 sprite + font |
