# BOB – Battle of the Brooms : เอกสารผลิต Character & Game Asset (2.5D Cartoon, Unity)

เวอร์ชัน 0.1 · 12 พ.ย. 2026 · สถานะ: ร่างเพื่อรีวิว

ชุดเอกสารนี้แปลง Art Direction ที่ตกลงไว้ (2.5D Stylized Cartoon, Chibi 3D, URP, กล้อง Orthographic) ให้เป็นสเปกที่ทีมอาร์ตและทีม Unity ใช้ผลิตได้จริง โดยยึดเอกลักษณ์จากชุดภาพ BOB เดิม (`bob-art/preview.png`, `bob-art/prototype/`) เป็นหลัก

| ไฟล์ | เนื้อหา | ใครใช้ |
| --- | --- | --- |
| [00_Art_Direction.md](00_Art_Direction.md) | Visual identity, สัดส่วน, สี, วัสดุ, แสง, กล้อง, กฎการอ่านง่ายบนมือถือ | ทุกคน |
| [01_Character_Bible.md](01_Character_Bible.md) | Master design ของ 4 Heroes + ศัตรู 5 ตัว + บอส "เจ้าคราบ" (ทรง, สี HEX, อุปกรณ์, หน้า, ท่าทาง) | Concept / 3D artist |
| [02_Asset_List.md](02_Asset_List.md) | รายการ asset 6 กลุ่ม (A–F) พร้อมรหัส ลำดับความสำคัญ และสถานะเทียบชุด placeholder | Producer / Artist |
| [03_Unity_Spec.md](03_Unity_Spec.md) | Poly/texture budget, rig, animation, shader URP, import, โฟลเดอร์, prefab, การตั้งชื่อ, optimization | 3D artist / Unity dev |
| [04_AI_Prompts.md](04_AI_Prompts.md) | Prompt สำหรับ ChatGPT / Midjourney / เครื่องมือ AI ใช้สร้าง Character Sheet และ asset ให้ตรงสไตล์ | Concept artist |
| [06_Placeholder_Status.md](06_Placeholder_Status.md) | สถานะ placeholder ที่ผลิตด้วยโค้ดตาม concept และงานที่ยังต้องให้ศิลปิน/AI ทำ | ทุกคน |
| [05_Roster_Mapping.md](05_Roster_Mapping.md) | เทียบ roster ใหม่ (กวาด/ฟอง/ถู/สเปรย์/เจ้าคราบ) กับตัวละครที่โค้ดเกมใช้อยู่ และประเด็นที่ต้องตัดสินใจ | Game designer |

## สรุปสั้น

1. **ยึดภาพต้นฉบับ** `concept/BOB_Game_Art_Board.png` และ `concept/BOB_Asset_Sheet.png`: กวาด (ผ้าคาดหัวแดง เสื้อส้ม ไม้กวาด), ฟอง (มวยคู่ เสื้อฟ้า ฟองน้ำ), ถู (ถังโลหะบนหัว เสื้อเขียว ไม้ถู), สเปรย์ (ผมบ็อบดำ แว่นกันลม เสื้อม่วง ขวดสเปรย์ชมพู) ทุกตัวใส่ผ้ากันเปื้อนครีม ยกระดับเป็น 3D toon shading + เส้นขอบน้ำตาลบาง
2. **ตัวละครเป็น 3D, กล้อง Orthographic มุมก้ม 40–45°** ใช้ skeleton ร่วมกันทั้ง 4 Heroes เพื่อแชร์อนิเมชันชุดเดียว (ลดงานอนิเมชันลงมาก)
3. **ไอคอน UI, ไอเทม และ VFX แบบ flipbook เป็น 2D sprite** เพราะคมกว่าและเบากว่าบนมือถือ
4. **Concept เป็นเกมแอ็กชัน (HP/ATK/DEF) แต่โค้ดที่ทำไว้เป็นเกมจัดชั้นรถเข็น** **ตัดสินใจแล้ว: คงเกมจัดชั้นรถเข็น ใช้ภาพตาม concept** ดู `05_Roster_Mapping.md` (Asset List ระบุแล้วว่าชิ้นไหนใช้กับแบบไหน)

## ลำดับการผลิตที่แนะนำ (Milestone)

| # | งาน | ผลลัพธ์ที่ส่ง | ประมาณ |
| --- | --- | --- | --- |
| M1 | Character Master Design "กวาด" (มี sheet แล้ว) + เพิ่มแผ่น ortho สำหรับ modeler | ortho front/side/back, color HEX ยืนยัน | 2–3 วัน |
| M2 | Master Design ฟอง / ถู / สเปรย์ + เจ้าคราบ / สไลม์ / บอส ตามเลย์เอาต์ sheet กวาด | Sheet ครบ + line-up เทียบความสูง | 1–2 สัปดาห์ |
| M3 | 3D Hero Base Mesh + Shared Rig | 1 ตัวเข้า Unity ได้, ทดสอบ shader/outline บนมือถือจริง | 1–2 สัปดาห์ |
| M4 | อนิเมชันชุดหลัก (Idle/Walk/Run/Carry/Place/Work/Skill/Hit/Victory/Defeat) | `.fbx` animation + Animator Controller | 2 สัปดาห์ |
| M5 | ศัตรู 5 ตัว + บอส, ห้องแรก 1 ห้อง (สกปรก/สะอาด) | Prefab พร้อม LOD | 2–3 สัปดาห์ |
| M6 | UI/VFX ชุดจริง แทน placeholder | Sprite Atlas + VFX prefabs | 1–2 สัปดาห์ |

ระหว่างผลิต ไฟล์ placeholder ใน `bob-art/unity/` ใช้ต่อได้ตามเดิม ชื่อไฟล์และ id ไม่เปลี่ยน งานจริงจะค่อยๆ เข้ามาแทนทีละชิ้น
