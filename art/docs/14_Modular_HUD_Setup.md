# 14 · สคริปต์ HUD/การเล่นแบบโมดูลาร์ และวิธีตั้งค่าใน Unity (13 พ.ย. 2026)

สคริปต์อยู่ที่ `unity/Assets/Scripts/Modular/` (namespace `Bob.Modular`) ฉากเดี่ยว (`BobGame`) ใช้ครบทุกตัวแล้ว: สร้างปุ่ม/พร็อพด้วยโค้ดแล้วส่งให้โมดูลเหล่านี้ทำงาน ถ้าต้องการทำเป็น prefab ใน Inspector ใช้ขั้นตอนข้างล่างได้เลย

| สคริปต์ | หน้าที่ | ใช้ในเกมแล้วที่ |
| --- | --- | --- |
| `IInteractable.cs` | สัญญาของทุกอย่างที่ฮีโร่ยืนใกล้แล้วกดใช้ได้ (ชั้นวาง รถเข็น โต๊ะ ถังขยะ เพื่อนร่วมทีม) | พร็อพทุกชิ้นในห้อง |
| `StationProp.cs` | คอมโพเนนต์วางบนพร็อพ: ชนิด รหัส ระยะ ขอบเขต (ฟังก์ชัน เพราะรถเข็นเคลื่อนที่) ข้อความปุ่ม สิ่งที่เกิดเมื่อกด | ชั้นวาง 4 จุด รถเข็น โต๊ะพักของ |
| `InteractionDetector.cs` | หาว่ายืนใกล้อะไร แยกตามชนิด และเลือก "เป้าหลัก" ตามลำดับความสำคัญ (รถเข็น > โต๊ะ > ถังขยะ > เพื่อน > ชั้นวาง) พร้อมอีเวนต์ `PrimaryChanged` | ตัวกำหนด near/nearCart/nearTable |
| `PlayerController.cs` | เดินในหน่วยห้อง: ปุ่ม/จอย/แตะเพื่อเดิน กันชนผนัง/ชั้นวาง/โซฟา ตรวจติดสิ่งกีดขวาง | การเดินของฮีโร่ |
| `TaskManager.cs` | นาฬิการอบ (หยุดเวลา, พัก), อีเวนต์ `WarningTick` / `TimeUpReached`, ความคืบหน้าตามสเปกใบงาน | นาฬิกา 100 วินาที และสกิล "รีดเวลา" |
| `MobileHUDController.cs` | ตัวจับ widget ของ HUD: pill นาฬิกากะพริบแดงเมื่อเหลือ <15 วินาที, ตัวบอกรอบ, ตัวนับเหรียญ, ปุ่มหยุด, แถบช่องรถเข็น 5 ช่อง (ไอคอนอย่างเดียว), ปุ่มหลัก, ปุ่มกดค้างส่งพร้อมวงแหวน `fillAmount` | HUD ฉากเดี่ยว |

หมายเหตุ: ข้อความรองรับทั้ง **TextMeshProUGUI** (`...Tmp`) และ `Text` เดิม ฟิลด์ไหนมีค่าก็ใช้ตัวนั้น (TMP ชนะ) ฉากเดี่ยวและ Coop ใช้ TMP แล้ว (ดูหัวข้อ TextMeshPro ท้ายเอกสาร) ปุ่มหลักใช้ `Button` ปุ่มส่งใช้ `IPointerDownHandler/IPointerUpHandler` (ใน `BobHoldButton` ที่มีอยู่แล้ว หรือตัวสำรองในคอนโทรลเลอร์ถ้าปุ่มยังไม่มี)

## ตั้งค่าใน Inspector ทีละขั้น (ทำเองแบบ prefab)

1. **ฉาก:** สร้างฉากใหม่ (หรือเปิด `Assets/Scenes/Game.unity` แล้วทำใน GameObject ว่าง) สร้าง GameObject ชื่อ `Systems`
2. **TaskManager:** เพิ่ม `Bob.Modular.TaskManager` ให้ `Systems` ตั้ง `Warn At = 15` เรียก `Begin(spec, 100)` ตอนเริ่มรอบและ `Tick(Time.deltaTime)` ทุกเฟรมที่รอบกำลังเล่น ผูก `TimeUpReached` กับฟังก์ชันส่งทดสอบ `WarningTick` กับเสียงติ๊ก
3. **InteractionDetector:** เพิ่มให้ `Systems` เรียก `Evaluate(playerPosition)` ทุกเฟรม ฟัง `PrimaryChanged` เพื่อเปลี่ยนข้อความปุ่มหลักจาก `Primary.Prompt`
4. **พร็อพ:** ที่ชั้นวางแต่ละชิ้น (GameObject ที่มี SpriteRenderer) เพิ่ม `StationProp` แล้วตั้ง
   - `Kind` = Shelf / Cart / Table, `Id` = tool / chem / energy / method (รถเข็นเว้นว่างได้)
   - `Reach` = 0.46 (ชั้นวาง) หรือ 0.5 (รถเข็น/โต๊ะ), `Bounds` = ขอบเขตหน่วยห้อง (x,y,กว้าง,สูง) ของพร็อพ
   - `Sprite` ถ้าอยากให้ไฮไลต์ด้วยสี, `Prompt` ข้อความปุ่ม
   - ผูก `OnInteract` ในโค้ด (ฟิลด์สาธารณะ) หรือใช้ `Register` ใน `InteractionDetector` ตอนสร้างพร็อพ
5. **ผู้เล่น:** เพิ่ม `PlayerController` ให้ GameObject ฮีโร่ ตั้ง `Speed = 2.3`, `Radius = 0.16`, `Room` = ขนาดห้อง (0,0,8,5) ตั้ง `Solids` เป็นฟังก์ชันที่คืนสี่เหลี่ยมของชั้นวาง/โซฟา/โต๊ะ แล้วเรียก `Step(inputVector, Time.deltaTime)` ทุกเฟรม (inputVector ศูนย์ = เดินตามจุดที่แตะ ถ้ามี `WalkTo`)
6. **HUD:** สร้าง Canvas (Screen Space - Overlay, Canvas Scaler = Scale With Screen Size 1920×1080, Match = 1) ภายใต้ Safe Area สร้างลูกตามนี้ แล้วเพิ่ม `MobileHUDController` ให้ GameObject `HUD` และลากฟิลด์ใส่
   - **บนกลาง:** `Image` พื้นมนยาว (pill) → ลากใส่ `Timer Pill`, ข้อความเวลา → `Timer Text Tmp` (หรือ `Timer Text`), ข้อความ "รอบ 1/3" → `Round Text`
   - **บนขวา:** ข้อความเหรียญ → `Coin Text`, ปุ่มหยุด → `Pause Button`, แถว `Image` 5 ตัว (Raycast Target ปิด) → `Cart Slots`
   - **ล่างขวา:** `Button` ใหญ่ ขนาด ≥ 160u → `Primary Button` + ข้อความ → `Primary Label` + พื้นปุ่ม → `Primary Image`; `Button` ส่ง → `Submit Button`; `Image` ชนิด Filled / Radial 360 / Origin Top ล้อมปุ่ม → `Submit Ring` (`Submit Hold Seconds` = 0.6)
   - ผูก `On Primary` กับ `InteractionDetector.InteractPrimary`, `On Pause` กับเมนูหยุด, `On Submit` กับฟังก์ชันส่งทดสอบ
7. **เรียกใช้:** ทุกเฟรม `hud.SetTimer(task.TimeLeft, task.Freeze > 0, roundLive)`; เมื่อกองเปลี่ยน `hud.SetCartSlots(icons)`; เมื่อเป้าหลักเปลี่ยน `hud.SetPrimary(prompt, enabled, color)`
8. **พื้นที่กด:** เรียก `Bob.UI.BobHitArea.Ensure(button.targetGraphic, 144)` กับปุ่มทุกปุ่ม (UX spec 1.2)

## TextMeshPro (ทำแล้ว 13 พ.ย.)

ข้อความทั้ง HUD/เมนู/ฉาก Coop ใช้ `TextMeshProUGUI` แล้ว ผ่าน `BobUi.Label` (Mitr สำหรับตัวหนา, IBM Plex Sans Thai สำหรับเนื้อหา) ขั้นตอนที่ทำและทำซ้ำได้:
1. `Bob > TextMeshPro > 1 Import Essentials` (นำเข้า TMP Essential Resources; ต้องรัน batchmode แบบไม่ใส่ `-quit`) → ได้โฟลเดอร์ `Assets/TextMesh Pro`
2. `Bob > TextMeshPro > 2 Build Font Assets` สร้าง `Assets/Resources/Fonts/TMP/IBMPlexSansThai-Medium SDF.asset` และ `Mitr-Medium SDF.asset` (SDFAA, atlas 2048, Dynamic, เติมอักษรไทยล่วงหน้า ~200 ตัว)
3. ช่องกรอก IP เซิร์ฟเวอร์ (InputField) ยังใช้ `Text` เดิมเพราะ `InputField` ต้องใช้ข้อความรุ่นเก่า (`BobUi.LabelLegacy`)
4. อักขระ ▲ ▼ ● ○ ✓ ✕ ★ ไม่มีในฟอนต์ไทย จึงเปลี่ยนเป็น + - • ◦ ในข้อความ ถ้าต้องการสัญลักษณ์ ใช้ไอคอนภาพ
5. ขอบตัวอักษรใช้ `BobUi.OutlineText` (ขอบของ TMP) ไม่ใช้คอมโพเนนต์ `Outline` ของ uGUI ซึ่งใช้กับ TMP ไม่ได้

## การทดสอบ

บอตทดสอบ `Bob > Run Game Test` เล่นครบแมตช์ผ่านด้วยโมดูลเหล่านี้ (เดินผ่าน `PlayerController` ตรวจใกล้ผ่าน `InteractionDetector` นาฬิกาผ่าน `TaskManager`)
