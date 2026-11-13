# BOB – Battle of the Brooms : เอกสารผลิต Character & Game Asset (2.5D Cartoon, Unity)

เวอร์ชัน 0.2 · 12 พ.ย. 2026 · สถานะ: ร่างเพื่อรีวิว (เพิ่มภาพ RAW จาก Leonardo ครบทุกแถวของ Asset List)

> **กฎเอกสาร:** ทุกครั้งที่อัปเดตงาน ต้องอัปเดตไฟล์ .md ที่เกี่ยวข้องในรอบเดียวกัน (README, docs/, Asset List, สถานะ)

ชุดเอกสารนี้แปลง Art Direction ที่ตกลงไว้ (2.5D Stylized Cartoon, Chibi 3D, URP, กล้อง Orthographic) ให้เป็นสเปกที่ทีมอาร์ตและทีม Unity ใช้ผลิตได้จริง โดยยึดเอกลักษณ์จากชุดภาพ BOB เดิม (`bob-art/preview.png`, `bob-art/prototype/`) เป็นหลัก

| ไฟล์ | เนื้อหา | ใครใช้ |
| --- | --- | --- |
| [00_Art_Direction.md](00_Art_Direction.md) | Visual identity, สัดส่วน, สี, วัสดุ, แสง, กล้อง, กฎการอ่านง่ายบนมือถือ | ทุกคน |
| [01_Character_Bible.md](01_Character_Bible.md) | Master design ของ 4 Heroes + ศัตรู 5 ตัว + บอส "เจ้าคราบ" (ทรง, สี HEX, อุปกรณ์, หน้า, ท่าทาง) | Concept / 3D artist |
| [02_Asset_List.md](02_Asset_List.md) | รายการ asset 6 กลุ่ม (A–F) พร้อมรหัส ลำดับความสำคัญ และสถานะเทียบชุด placeholder | Producer / Artist |
| [03_Unity_Spec.md](03_Unity_Spec.md) | Poly/texture budget, rig, animation, shader URP, import, โฟลเดอร์, prefab, การตั้งชื่อ, optimization | 3D artist / Unity dev |
| [04_AI_Prompts.md](04_AI_Prompts.md) | Prompt สำหรับ ChatGPT / Midjourney / เครื่องมือ AI ใช้สร้าง Character Sheet และ asset ให้ตรงสไตล์ | Concept artist |
| [08_Leonardo_Production_Pack.md](08_Leonardo_Production_Pack.md) | **เริ่มที่นี่ (ใช้ Leonardo.ai)**: ตั้งค่า, Image Guidance, prompt + negative prompt ทีละไฟล์ | ผู้ใช้ / Concept artist |
| [10_Leonardo_RAW.md](10_Leonardo_RAW.md) | **ภาพ RAW จาก Leonardo.ai** 740 ภาพ + ฉากหลังล็อบบี้ 22 ฉาก + ชุดเติมส่วนที่ขาด 81 รายการ + รอบสร้างใหม่ภาพที่ใช้ไม่ได้: โฟลเดอร์ ชื่อไฟล์ ค่าที่ใช้ ข้อควรระวัง และ gen id สำหรับดาวน์โหลดซ้ำ | ทุกคน |
| [12_RAW_Cutouts.md](12_RAW_Cutouts.md) | ภาพ RAW ที่ลบพื้นหลังแล้ว (`cutouts/`, `cutouts_best/`) และรายการที่นำเข้า Unity / ยังต้องแก้ (ไฟล์อยู่บนเครื่องผู้ใช้) | ทุกคน |
| [09_Leonardo_Full_Prompts.md](09_Leonardo_Full_Prompts.md) / [.csv](09_Leonardo_Full_Prompts.csv) | **Prompt เต็ม 172 รายการ** ก็อปวางได้ทันที เรียงตามลำดับผลิต พร้อมชื่อไฟล์ โฟลเดอร์ `incoming/` ค่าตั้ง และ negative prompt (CSV ไว้ติ๊กงานที่เสร็จ) | ผู้ใช้ / Concept artist |
| [14_Leonardo_Regen_Prompts.csv](14_Leonardo_Regen_Prompts.csv) | prompt รอบสร้างใหม่ภาพที่ตัดพื้นหลังไม่ได้ (25 รายการ) | ผู้ใช้ / Concept artist |
| [07_ChatGPT_Production_Pack.md](07_ChatGPT_Production_Pack.md) | ทางเลือก: prompt สำหรับ ChatGPT ทีละไฟล์ ตั้งชื่อไฟล์ และวิธีส่งกลับมาเข้า Unity | Concept artist / ผู้ใช้ |
| [06_Placeholder_Status.md](06_Placeholder_Status.md) | สถานะ placeholder ที่ผลิตด้วยโค้ดตาม concept และงานที่ยังต้องให้ศิลปิน/AI ทำ | ทุกคน |
| [05_Roster_Mapping.md](05_Roster_Mapping.md) | เทียบ roster ใหม่ (กวาด/ฟอง/ถู/สเปรย์/เจ้าคราบ) กับตัวละครที่โค้ดเกมใช้อยู่ และประเด็นที่ต้องตัดสินใจ | Game designer |
| [ux/BOB_Minigame_Screens.html](ux/BOB_Minigame_Screens.html) | **wireframe การ์ดมินิเกม (13 พ.ย. 2026):** หน้าจอ S28 ของมินิเกม task race ทั้ง 16 ตัว (MVP 8 + ระยะ 2 อีก 8) ขณะเล่น การ์ดบนจอเล่นพร้อมหมายเลขอธิบาย และสถานะร่วม 8 แบบ (มือผีสอน, ถนัด, ทำต่อ, รอเพื่อน, ฉุกเฉิน, สำเร็จ, งานถูกล็อก, โหมดช่วยกดค้าง) | UX / Art / Unity |
| [11_UX_UI_Spec.md](11_UX_UI_Spec.md) | สเปก UX/UI ทั้งเกม: แผนผังหน้าจอ 30+ จอ, flow, HUD, component library (ขนาด u/dp, สถานะ, เสียง/สั่น), tokens สี/ฟอนต์ไทย, accessibility, วิธีทำใน Unity (uGUI + TMP) + wireframe [`ux/BOB_UX_Wireframes.html`](ux/BOB_UX_Wireframes.html) และหน้าจอเล่น 3/4 โรงอาหาร [`ux/BOB_Play_Screen_Cafeteria.html`](ux/BOB_Play_Screen_Cafeteria.html) (ข้อ 5A) | Game designer / UI artist / Unity dev |
| [12_Sound_Manifest.md](12_Sound_Manifest.md) | รายการเสียงทั้งเกม (39 ไฟล์ รวมเสียง co-op ใหม่ 6 ตัว + ข้อ 6 เสียง/เพลง task race ที่ยังต้องหา), แหล่ง CC0 ที่คัดไว้ (Kenney, OpenGameArt, Freesound CC0), วิธีนำเข้า Unity และตารางบันทึก licence | Sound / Unity dev |
| [13_Balance_Sim.md](13_Balance_Sim.md) | ผลจำลองสมดุลฮีโร่ 20,000 รอบ (`server/balance-sim.js`): ability แต่ละตัวช่วยได้แค่ไหน และข้อเสนอปรับตัวเลข (ยังไม่ได้แก้เกม) | Game design |
| [15_Task_Minigame_Study.md](15_Task_Minigame_Study.md) | **ศึกษา (13 พ.ย. 2026):** ระบบ task ของ Among Us (common/short/long, แถบความคืบหน้ารวม, ทำไมสนุก) → ข้อเสนอเปลี่ยนรอบเล่น BOB เป็น task list มินิเกมทำความสะอาด 16 ตัว (MVP 8), แถบทีม+คู่แข่ง, ดนตรีเลเยอร์ และสิ่งที่กติกา/server ต้องเปลี่ยน (**อนุมัติแล้ว** ใช้เป็นเหตุผลและแหล่งที่มา) | Game designer / ทุกคน |
| [16_Task_Race_Minigame_Spec.md](16_Task_Race_Minigame_Spec.md) | **สเปกใช้งานจริง (13 พ.ย. 2026):** รอบเล่น task race (ใบงาน = รายการงานของทีม, น้ำหนัก, แถบทีม/คู่แข่ง, ลมเป่าช่วย, คราบฉุกเฉิน, การชนะ), มินิเกม 16 ตัวแบบละเอียดครบ (MVP 8 ตัว + ระยะ 2 อีก 8 ตัว: ควบคุม, เกณฑ์, D1–D3, ถนัด, เวลาขั้นต่ำ server, เสียง, art) และสัญญา opcode | Game designer / Unity / Server |
| [17_Music_Layers.md](17_Music_Layers.md) | แผนดนตรีเลเยอร์ 4 stem (120 bpm, เปิดตาม % ทีม/คู่แข่ง/เวลา, quantize ขอบห้อง, ชุด fast 130 bpm, stinger, ducking, `BobMusicLayers` ใน Unity, ทางเลือกการผลิต) | Sound / Unity dev |
| [22_Tripo_CLI.md](22_Tripo_CLI.md) | **Tripo CLI (13 พ.ย. 2026):** ติดตั้ง `tripo-cli`, ตั้งคีย์ผ่าน `TRIPO_API_KEY` เท่านั้น, คำสั่งที่ไม่เสียเครดิต (doctor/balance/--dry-run), สูตรสร้าง prop จาก prompt/ภาพ Leonardo, ประมาณการเครดิต และตัวอย่างเรียก V3 API ด้วย Python | Art / ผู้ใช้ |

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
| M6 | UI/VFX ชุดจริง แทน placeholder ตาม `11_UX_UI_Spec.md` (F01–F24) | Sprite Atlas + UI prefabs + VFX prefabs | 1–2 สัปดาห์ |

ระหว่างผลิต ไฟล์ placeholder ใน `bob-art/unity/` ใช้ต่อได้ตามเดิม ชื่อไฟล์และ id ไม่เปลี่ยน งานจริงจะค่อยๆ เข้ามาแทนทีละชิ้น
