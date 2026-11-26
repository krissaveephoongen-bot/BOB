# 33 · M1 Duo Co-op Slice

## สถานะ

**implemented in code 25 พ.ย. 2026 — ยังไม่ play-verified ในรอบนี้ · no prank layer · no progression writes**

- โค้ดของ slice M1 ตามแผน [`docs/superpowers/plans/2026-11-25-bob-m1-duo-coop-slice.md`](../../docs/superpowers/plans/2026-11-25-bob-m1-duo-coop-slice.md) เขียนครบทั้ง 8 งานแล้ว
- **ยังไม่เคยเปิด Unity Editor ในรอบนี้** — `Assets/Scenes/DuoClean.unity` ยังไม่ถูก bake และยังไม่ได้รันทดสอบสองเครื่องจริง
- ด่านอัตโนมัติที่รันได้โดยไม่ต้องเปิด Editor ทั้งสองด่านเขียว (ดู [`## ทดสอบ`](#ทดสอบ))
- สถานะนี้ตรงกับแถว `M1-DUO▲` ใน [`COORDINATION.md`](../../COORDINATION.md) §3 (ยังไม่ commit)

## ทางเข้า

Home → `โหมด` → `Duo` → ปุ่ม **`คู่ 2 คน · M1`** → โหลดซีน `DuoClean` ผ่าน `BobSceneLoader` พร้อมหัวข้อ **`ห้องคู่ 2 คน`**

- ปุ่มนี้เรียก `OpenDuoSlice` ใน `Game/BobGame.Menus.cs` แล้วเรียก `BobSceneLoader.Load(BobOnlineRoom.DuoScene, "ห้องคู่ 2 คน")`
- **ถ้าซีน `DuoClean` ยังไม่อยู่ใน Build Settings** `BobSceneLoader.Load` จะเตือนแล้วกลับทันที — ปุ่มจะดูเหมือนกดแล้วไม่มีอะไรเกิดขึ้น จนกว่า owner จะ bake ซีน (ดูหัวข้อสุดท้าย)

## กติกา

| ข้อ | ค่า |
| --- | --- |
| ความยาวแมตช์ | 240 วิ = 4:00 |
| ช่วงเวลา | ด้วยกัน 75 วิ → แยกฝั่ง 105 วิ → ด้วยกัน 60 วิ |
| จำนวนงาน | 6 งาน ตามลำดับ: คราบติดพื้น, เก็บขยะ, เช็ดกระจก, ล้างจาน, ตากผ้า, ฝุ่นเพดาน |
| บทบาท | บทบาท primary สลับทุกงาน — งานคู่ (0, 2, 4) slot 0 เป็น primary, งานคี่ (1, 3, 5) slot 1 เป็น primary (`BobDuoRules.PrimarySlot`) |
| ท่าบังคับ | มี 3 ท่าเท่านั้น: hold, rhythm tap (จังหวะ 0.75 วิ, หน้าต่าง ±0.2 วิ), drag (สเกล 1920/Screen.width) |
| งานแรก (ตัวอย่างค่า) | คราบติดพื้น = hold 3.0 / rhythm 8 ครั้ง |
| การนับงานเสร็จ | ต้องให้มือทั้งสองข้างถึง 1.0 จึงนับว่างานเสร็จ |
| มือที่วาง | เงียบเกิน 2.5 วิ แล้วลดลงวินาทีละ 0.35 |
| tap ผิดจังหวะ | นับเป็นการลองหนึ่งครั้ง แต่ไม่เพิ่มอะไร |
| ตอนออด | ไม่มี game over — จอผลโชว์เปอร์เซ็นต์ที่ทำได้ ณ วินาทีนั้น (บางส่วน) |

ค่าทั้งหมดอยู่ใน `Rules/BobDuoRules.cs` (ช่วงเวลา, ตารางงาน, การสลับบทบาท) และ `Rules/BobDuoTaskState.cs` (charge, การลดของมือที่วาง, การนับงานเสร็จ)

## สองเครื่อง

1. host กด **`สร้างห้อง`** — เปิด UGS Lobby + Relay (dtls) และโชว์รหัสห้อง 6 ตัว
2. client กด **`ใส่รหัสห้อง`** แล้วพิมพ์รหัส — บนมือถือใช้คีย์บอร์ดสัมผัส ใน editor ใช้คีย์บอร์ดจริง
3. host กด **`เริ่มเมื่อครบ 2 คน`** เมื่อ lobby โชว์ 2/2

กลไกภายใน:

- **host เป็นเจ้าของนาฬิกาและ charge ของมือทั้งสองข้าง** และ broadcast snapshot วินาทีละ 5 ครั้ง
- client ใส่ input ของตัวเองลงทันที (optimistic) แล้วถูกแก้ด้วย snapshot ถัดไป
- Message id: `bob.duostart`, `bob.duoact`, `bob.duosnap`, `bob.duoend`
- `bob.duoact` payload `slot(int) code(byte) amount(float)` — code `0` = ระดับ hold (±1), `1` = tap ที่ตรงจังหวะ, `2` = drag (หน่วย)
  - tap ส่งแบบ ReliableSequenced, hold/drag ส่งแบบ Unreliable
  - client ส่งขึ้น server, host เป็นผู้ broadcast
- `bob.duosnap` payload `Charge0`, `Charge1`, `Elapsed`, `Task`, `DoneMask` ส่งแบบ reliable ที่ 5 Hz

โค้ดฝั่งเน็ตอยู่ใน duo channel ของ `UGS/BobOnlineRoom.cs` เท่านั้น

## ทดสอบ

### ทดลองในเครื่องเดียว (solo smoke)

ปุ่มใน lobby **`ทดลองคนเดียว (คู่เป็นบอท)`** — บอทเล่นสล็อตอีกข้างให้ ไม่ต้องมีเพื่อนและไม่ต้องสร้างห้อง

### Automation ใน Editor

เมนู **`Bob/Run Duo Slice Check`** (ใน `Editor/BobDuoSceneBuilder.cs`):

1. เปิด Play mode และขับสล็อต 0 ให้เอง (กดการ์ดผ่าน `BobDuoHud.HoldForCheck` เหมือนนิ้วกด ไม่ใช่เรียก `BobDuoMatch` ตรง ๆ เพื่อไม่ให้การ์ดเขียนทับทุกเฟรม)
2. ที่ประมาณ 3.4 วิ จับภาพไปที่ `unity/Logs/duo-slice.png` (ก่อนงานที่ 1 จะเสร็จ เพื่อให้เห็นแถบที่กำลังขึ้นและ pip ทั้ง 6)
3. ที่ประมาณ 9 วิ log บรรทัด `[DuoCheck] PASSED/FAILED · task N · P% · over=…` แล้วหยุด Play mode ด้วย `ExecuteMenuItem("Edit/Play")`
4. เกณฑ์ PASS คือ **งานที่ 1 ต้องเสร็จ** (`Tasks[0].Done`) ไม่ใช่แค่เปอร์เซ็นต์มากกว่าศูนย์ — งานที่ 1 เสร็จได้ต่อเมื่อมือทั้งสองข้างขยับ อินพุตฝั่งเครื่องนี้จึงต้องถึง simulation จริง
5. เมนูนี้ไม่ทำงานระหว่าง Play mode (จะเตือนให้กด Stop ก่อน) เพื่อไม่ให้ flag ค้างข้ามรอบ

### ด่านที่รันแล้วในรอบนี้ (หลักฐานเดียวที่ยืนยันได้)

```
$ bash unity-typecheck/check.sh
typecheck ok: 43 known Unity-6 gap kind(s), no new errors
```

```
$ cd unity-check && DOTNET_ROLL_FORWARD=Major dotnet run
…
duo slice table checked
…
UNITY-CHECK PASSED
```

ทั้งสองด่านเป็นแบบ static: ด่านแรกตรวจว่าโค้ด Unity ไม่มี error ใหม่เกินชุด gap ที่รู้แล้ว 43 ชนิด ด่านที่สองรัน self-test ของกติกา (`BobDuoRules`, `BobDuoTaskState`) ใน `unity-check` — **ไม่ใช่หลักฐานว่าเกมเล่นได้จริง**

### ยังไม่ได้ทดสอบ

- ยังไม่มี Play-mode run ของ `DuoClean` ในรอบนี้ (`[DuoCheck]` ยังไม่เคยรัน) เพราะ Editor ไม่ได้เปิด
- ยังไม่มีภาพ `unity/Logs/duo-slice.png`
- ยังไม่ได้ทดสอบสองเครื่องจริง — **status: implemented in code, editor + two-device run pending owner**

## ข้อจำกัดที่รู้

- **ถ้า host หลุด client ไม่ได้รหัสจบ** — ไม่มี reconnect และไม่มี host migration ตัว client จึงค้างอยู่กับ host ที่ตายไปแล้ว ต้องกลับหน้าหลักเอง
- **หนึ่งห้อง สองคนเท่านั้น** — `StartDuo` ปฏิเสธทุกจำนวนที่ไม่ใช่ 2
- **ไม่มี matchmaking** — ต้องส่งรหัสห้องกันเอง
- **ไม่มี voice chat**
- **ไม่มี prank layer** — เป็นงาน M3
- **ไม่เขียนความคืบหน้า** — ไม่มี progression write / ไม่มีรางวัล
- **ไม่มีอาร์ตแยกงาน** — งานแต่ละงานมีแค่ข้อความกับสี พื้นที่ท่าทุกท่าคือแผงร่วมแผงเดียว
- **เปอร์เซ็นต์ฝั่ง client ต่ำกว่าจริง** สำหรับงานที่ host ทิ้งไว้ที่รอยต่อช่วง — ตัวเลขสุดท้ายเอามาจาก `bob.duoend` ของ host
- **client ส่งระดับ hold ทุกเฟรม** ระหว่างที่กดท่าค้างอยู่ (ประมาณ 60 ข้อความ Unreliable เล็ก ๆ ต่อวินาที) — ตัวนี้ทำให้กู้สภาพได้เองตอนแพ็กเก็ตหาย
- **layout ตรวจแล้วที่ 16:9, 19.5:9, 20:9 และ 2:1** — ที่ **16:10 แถบบน 1792 หน่วยจะล้นออก 35 หน่วย และการ์ดแมตช์สองใบซ้อนกัน 17 หน่วย**
- **ระยะลากยังไม่ได้ลองด้วยนิ้วจริง** — งานลากต้องลากรวม 2400–2800 หน่วย (ประมาณ 1.3–1.5 ความกว้างจอที่อ้างอิง 1920) ตัวเลขนี้ผ่าน self-test แต่ความรู้สึกจริงต้องรอ owner ทดลองและอาจต้องลด
- **ไม่มีปุ่มยกเลิกระหว่างพิมพ์รหัสห้อง**

## ไฟล์ที่แตะ

เพิ่ม:

- `unity/Assets/Scripts/Rules/BobDuoRules.cs`
- `unity/Assets/Scripts/Rules/BobDuoTaskState.cs`
- `unity/Assets/Scripts/Duo/BobDuoHud.cs`
- `unity/Assets/Scripts/Duo/BobDuoBootstrap.cs`
- `unity/Assets/Scripts/Duo/BobDuoMatch.cs`
- `unity/Assets/Scripts/Duo/BobDuoGestures.cs`
- `unity/Assets/Scripts/Duo/BobDuoDevCheck.cs`
- `unity/Assets/Scripts/Editor/BobDuoSceneBuilder.cs`

แก้:

- `unity/Assets/Scripts/UGS/BobOnlineRoom.cs` — เฉพาะ duo channel
- `unity/Assets/Scripts/Game/BobGame.Menus.cs` — ปุ่ม `คู่ 2 คน · M1` + `OpenDuoSlice`
- `unity-typecheck/Shims.cs` — duo mirror
- `unity-typecheck/BobCheck.csproj` + `unity-check/Program.cs` — self-test ของกติกา

เอกสาร:

- ไฟล์นี้, `art/docs/32_Coop_Concept_Reset.md` §8/§13, `art/docs/README.md`, `COORDINATION.md` §3

## ขั้นที่ owner ต้องรันเอง

1. **bake ซีน** — เปิด Unity 6 แล้วรันเมนู builder ใน `Editor/BobDuoSceneBuilder.cs` เพื่อสร้าง `Assets/Scenes/DuoClean.unity` จากนั้นใส่ซีนใน Build Settings ให้เรียบร้อย (ถ้าไม่ใส่ ปุ่ม `คู่ 2 คน · M1` จะเตือนแล้วไม่ไปไหน)
2. **เปิดซีน** — เปิด `Assets/Scenes/DuoClean.unity`
3. **play solo smoke** — กด Play แล้วทดลองท่า hold / rhythm / drag ให้ครบอย่างน้อยหนึ่งงาน ผ่านปุ่ม `ทดลองคนเดียว (คู่เป็นบอท)` หรือจากปุ่มใน lobby
4. **รันเมนู check** — `Bob/Run Duo Slice Check` แล้วรอประมาณ 9 วิ
5. **อ่านภาพ** — เปิด `unity/Logs/duo-slice.png` เทียบกับ layout ที่ต้องการ และอ่านบรรทัด `[DuoCheck] …` ใน Console
6. **ทดสอบสองเครื่อง** — ทำ host/client ตามหัวข้อ `## สองเครื่อง` และเล่นหนึ่งแมตช์จนถึงออด
