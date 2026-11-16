# 24 · Task Race Map System: เกาะคู่โค้งแบบมินิเดียรามา

เวอร์ชัน 0.8 · 14 พ.ย. 2026 · สถานะ: แผนที่จริง import แล้ว (5 ชิ้น ≤ 8k tri), ซีนสามโหมด `TaskRace.unity` (solo, 6 จุด) / `TaskRace_Duo.unity` (1v1, 8 จุด) / `TaskRace_Team.unity` (2v2/3v3, 12 จุด) สร้างด้วยคำสั่งเดียวต่อโหมด และตรวจผ่านทั้งสองกติกาทุกซีน · เพิ่ม **ผู้อำนวยการรอบ** (`TaskRaceRound`): สุ่มรายการงานตาม doc 16 จาก seed, เติมงานให้สถานี และวัดปริมาณงานต่อโหมด (ข้อ 14) · เพิ่ม **HUD ใบงาน + แถบน้ำหนัก** (`BobTaskRaceHud`) ที่ผูกกับรอบจริงแล้ว (ข้อ 10) · เพิ่ม **แสง เงา และบรรยากาศ** รอบที่ 2 (ข้อ 3.1): shader รับเงาจริง + shadow caster โค้งตามโลก, ambient อ่านจาก `RenderSettings`, sheen, ฟุ้ง (fog) และท้องฟ้าไล่สีที่ติดกล้อง · **รอบที่ 3**: เกาะสองฝั่งเหมือนกันเป๊ะ (prefab เดียว + seed เดียว), สะพานไม้มองเห็นได้ทั้งสองโหมด, เงาใต้เคาน์เตอร์แบบ AO ที่จำกัดขนาด (กันวงม่วงรอบเกาะ) · **รอบที่ 4 (14 พ.ย. 2026):** เก็บของออกจาก deck สะพาน (แถบกลางเกาะ |z| ≤ 0.74, clearance validator 3 โซน), ตัดปลายแม่น้ำใต้ให้ flush กับผาหน้า, **กติกากลางรอบจริง** — นาฬิกา 120 วิ, โหมด `TimedBridgeRaid` (ประตูเขย่า 65 วิ / ทรุด 60 วิ + `NavMeshObstacle` carve), แต้มทีม 100/งาน, ป่วน 1 ครั้ง/สถานี/แมตช์, เกราะ 150 Pts 30 วิ (atomic), HUD แถบเวลา/แต้ม + การ์ดป่วน + prompt เกราะ, ดีบักบาร์ 8 ปุ่ม (ข้อ 6.2, 12)

> ระบบแผนที่สำหรับรอบ Task Race (doc 16) แบบเกาะลอยคู่ที่แลดูเป็นลูกโลกใบเล็ก โดยใช้พร็อพ Tripo ที่ clean แล้ว
> ทำงานร่วมกับการ์ดมินิเกม S28/วง F29 เดิม และรองรับสองกติกาบนแผนที่เดียวกัน

## 1. หลักสถาปัตยกรรม (สำคัญ)

**ทุกอย่างที่เกี่ยวกับเกมยังแบนราบบนระนาบ X–Z** — transform, collider, NavMesh, ระยะ NEAR 1.2, การเดินของฮีโร่, การวาง TaskSpot
ส่วน "ความโค้ง" เป็นเพียงการดัด **vertex ที่วาด** ใน shader เท่านั้น

```
worldPos.y -= _BobCurvature * _CurvatureScale * dot(d, d)      // d = worldPos.xz - camera.xz
```

ผลที่ได้:

- NavMesh, `NavMeshObstacle`, collider และพิกัดงานทั้งหมดไม่ต้องแก้เมื่อเปลี่ยนความโค้ง
- เปลี่ยน `_BobCurvature` แล้วทุกวัสดุในฉากโค้งพร้อมกัน (global float ตัวเดียว)
- UI ที่ไม่ผ่าน shader (วงแหวนพื้น, ป้ายชื่อ) อ่านค่าชดเชยจากสูตรเดียวกันผ่าน `BobCurvedWorld.Drop()` จึงนั่งบนพื้นโค้งพอดี
- ปิดความโค้งได้ตลอด (`0`) เพื่อ debug หรือทำเมนู flyover โดยไม่กระทบกติกา

## 2. ไฟล์และความรับผิดชอบ

| ไฟล์ | หน้าที่ |
| --- | --- |
| `unity/Assets/Resources/Shaders/BobCurvedWorld.hlsl` | ฟังก์ชัน `BobCurvePosition` / `BobCurveNormal` ใช้ร่วมทุกวัสดุ |
| `unity/Assets/Resources/Shaders/BobCurvedWorld.shader` | `BOB/CurvedWorld`: outline pass (inverted hull, หนาคงที่ตามพิกเซล) + ForwardBase toon lit (ramp 0.45/0.78, **รับเงาจริง**, ambient trilight จากฉาก, sheen, rim, mottle, fog, dirt) + **ShadowCaster pass ที่ดัด vertex ตามความโค้ง** (ข้อ 3.1) |
| `unity/Assets/Shaders/BobToonLit.shader` | `BOB/ToonLit` (ฮีโร่/พร็อพไม่โค้ง): สูตรเดียวกับ `BOB/CurvedWorld` แต่ไม่มี curvature — รับเงา, sheen, ambient จากฉาก, fog; shadow caster ใช้ FallBack `Diffuse` |
| `unity/Assets/Scripts/TaskRace/BobCurvedWorldController.cs` | `BobCurvedWorld` (static: global, `Drop`, `Curve`, `CreateMaterial`) + `BobCurvedWorldController` (blend ค่าความโค้ง, context menu 0 / 0.010 / 0.018 / 0.026) |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceCamera.cs` | กล้องตามฮีโร่แบบ SmoothDamp + โฟกัสการ์ด S28 |
| `unity/Assets/Scripts/TaskRace/TaskSpot.cs` | จุดงาน: วงแหวน F29, ป้ายพรีวิว, NEAR, TASK_LOCK, เก็บ progress 10 วิ, event ให้ HUD |
| `unity/Assets/Scripts/TaskRace/TaskRaceModeManager.cs` | สลับ PureRace / TimedBridgeRaid, กลุ่ม barrier/bridge, นาฬิกา 120 วิ + เปิดสะพานที่ 60 วิ, แต้มทีม + ซื้อเกราะแบบ atomic, NavMeshObstacle/NavMeshLink, **สลับ NavMesh ที่ bake ไว้ต่อโหมด** (`ApplyNavMesh`), ปิดการ์ดเมื่อจอยเอียง > 70%, debug hooks |
| `unity/Assets/Scripts/TaskRace/TaskRaceRound.cs` | ผู้อำนวยการรอบ: คลังมินิเกม 15 รายการ (T01–T15), สุ่มรายการงานแบบมี seed ตามสูตร doc 16 ข้อ 2.2, แจกงานให้สถานีตามหมวด + เติมงานใหม่เมื่อทำเสร็จ, ประมาณปริมาณงาน, เปิดรายการพร้อมสถานะให้ HUD (`Tasks`, `Version`) — ดูข้อ 14 |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceHud.cs` | HUD ของรอบ (ข้อ 10): การ์ด **ใบงาน** ตาม `TaskRaceRound.Tasks` (✓ เสร็จ / ⋯ ผู้ถือ TASK_LOCK / รอคิว), **แถบน้ำหนัก** + %, "ทีมเรา N/M" และปุ่มหลัก `ทำงาน: <ชื่อ>` ที่เปิด/ปลด TASK_LOCK — สร้างด้วย `BobUi` ใน canvas 1920×1080 match height |
| `unity/Assets/Scripts/TaskRace/BobSkyBackdrop.cs` | ท้องฟ้าไล่สีแบบวาด (pale blue ล่าง → zenith น้ำเงินเข้ม, cloud puffs) เป็น mesh ที่ **เขียนใน clip space (NDC −1..1)** วาดด้วย `BOB/SkyNDC` (output `v.vertex.xy` ตรง ๆ) จึงเต็มเฟรมทุก FOV/aspect โดยไม่ต้องมี quad 70 u; `HideAndDontSave`, ไม่แคสต์เงา (ข้อ 3.1) |
| `unity/Assets/Scripts/TaskRace/BobFieldDressing.cs` | มัณฑนศิลป์พื้นหญ้าต่อเกาะ (ข้อ 5.2): ใบหญ้า 2 ด้าน, ดอกเดซี่, ก้อนหิน และแถบตัดหญ้า สร้างครั้งเดียวเป็น mesh เดียว UV ชี้ palette, seed 1975 (ฝั่ง A/B ได้เหมือนกัน), เว้น 1.7 u รอบสถานีและเว้นใต้สะพาน |
| `unity/Assets/Scripts/TaskRace/BobBridgeDressing.cs` | สะพานไม้จริงแทน prefab ที่อ่านเป็นแผ่นครีม: แผ่นไม้เหลื่อม + ตะเข็บสี, คานข้าง, เสา/ราวบน-กลาง, ตอม่อหยั่งลงเหว; mesh ถูก parent ใต้ component จึงอยู่/หายตามกลุ่มที่มันสังกัด |
| `unity/Assets/Scripts/TaskRace/BobBridgeGate.cs` | ประตูไม้แดง-ครีมปิดกลางสะพานในโหมด PureRace (อยู่ใต้ `RiverBarrier_Group` ซึ่งเปิดเฉพาะโหมดนั้น) — สะพานยังอยู่ในภาพ แต่เดินผ่านไม่ได้ |
| `unity/Assets/Scripts/TaskRace/BobWaterfallDressing.cs` | ผาหัวน้ำตกที่สร้างเอง (แทน mesh import ที่เป็นแผ่นแก้ว): prism หิน 3 แถวเอียงเหลื่อม, ตะไคร่บนยอด, ชั้นน้ำ 5 ริบบิ้น, ฟองที่ปาก, สระน้ำ + เศษกระเซ็น + หมอกรอบสระ |
| `unity/Assets/Scripts/Editor/BobSceneDump.cs` | `Tools > BOB > Dump Task Race Renderers` — รายการ renderer ที่เปิดอยู่และใหญ่กว่า 3 u พร้อม bounds/วัสดุ ใช้ตอบคำถาม "ชิ้นไหนวาดแผ่นสีอ่อนนั้น" |
| `unity/Assets/Scripts/Editor/BobMapAssemblerEditor.cs` | หน้าต่าง `Tools > BOB > Task Race Map Assembler`: ประกอบแผนที่, clone Team A → Team B, สลับโหมด, validate |
| `unity/Assets/Scripts/Editor/TaskRaceSceneBuilder.cs` | `Tools > BOB > Build Task Race Scene (solo / duo / team)` — สร้างซีนครบทั้งฉาก (กล้อง/แสง/ฮีโร่/แผนที่/สถานี/โหมด), ติด `NavMeshModifier` (ข้อ 6.1), bake NavMesh สองชุดต่อซีน แล้วเพิ่มเข้า Build Settings ตามลำดับ solo → duo → team; ขนาดและจำนวนสถานีดูข้อ 13 |
| `unity/Assets/Scenes/NavMesh/{TaskRace,_Duo,_Team}_{PureRace,TimedBridgeRaid}.asset` | NavMesh ที่ bake ไว้ล่วงหน้าต่อซีนต่อโหมด (สร้างใหม่ทุกครั้งที่ build ซีน) — `TaskRaceModeManager` สลับให้ตอนเปลี่ยนโหมด |
| `unity/Assets/Scripts/Editor/TaskRaceVerify.cs` | `-executeMethod Bob.EditorTools.TaskRaceVerify.Run` (หรือ `RunDuo` / `RunTeam`) — เปิดซีน ตรวจจุดงาน/ทั้งสองโหมด/เส้นทาง NavMesh ระหว่างเกาะ แล้วเรนเดอร์ `Logs/taskrace{-duo,-team}-{preview,wide,play}.png` |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceHero.cs` | ฮีโร่ทดสอบ: CharacterController เดินบนระนาบแบน + หันตามทิศเดิน |

Namespace ของทั้งหมดคือ `Bob.TaskRace` (assembler อยู่ `Bob.EditorTools`)

## 3. Shader และค่าความโค้ง

- `_BobCurvature` เป็น **global** (`Shader.SetGlobalFloat`) ไม่ใช่ property ของ material — ถ้าประกาศเป็น property ใน shader ค่าของ material จะทับ global
- แต่ละ material มี `_CurvatureScale` (0–3) เผื่อต้องการให้พื้นน้ำหรือฉากหลังโค้งมาก/น้อยกว่าพื้นเดิน
- ค่าเริ่มต้น `0.018` (Ever-Planet look ที่กล้องสูง 8.5 u), `RuntimeInitializeOnLoadMethod` รีเซ็ตให้ทุกเพลย์
- Outline pass ใช้ `n.xy` ใน view space แล้วบวกใน clip space ตาม `_ScreenParams` — เส้นคงที่ 2.5 px โดยประมาณ
- ShadowCaster pass ของ `BOB/CurvedWorld` ใช้สูตรเดียวกับ `UnityClipSpaceShadowCasterPos` (normal offset จาก `unity_LightShadowBias.z`) แต่ทำบนตำแหน่งที่ **ดัดความโค้งแล้ว** — เงาที่ทอดจึงตรงกับ silhouette ที่เห็น ไม่ลอยอยู่บนระนาบแบน (ก่อนหน้านี้ใช้ FallBack `Diffuse` ซึ่งแบน)

### 3.1 แสง เงา และบรรยากาศ (รอบที่ 2, 14 พ.ย. 2026)

เป้าหมาย: ให้ฉาก ตัวละคร และพร็อพมีน้ำหนักแสงเงาแบบดิโอราม่าพาสเทล (Cheesecake Rush) ไม่แบนเป็นสีพิมพ์

| องค์ประกอบ | ค่า | หมายเหตุ |
| --- | --- | --- |
| Ambient | `AmbientMode.Trilight` · sky `(0.56,0.70,0.94)` · equator `(0.82,0.80,0.82)` · ground `(0.42,0.36,0.50)` · intensity 1 | shader อ่าน `unity_AmbientSky/Equator/Ground` โดยตรง (ไม่ใช้สีคงที่ใน material แล้ว) — แก้ที่ `RenderSettings` ที่เดียวพอ |
| ดวงอาทิตย์ | directional · `(1,0.96,0.88)` · intensity **0.85** · rotation `(46,75,0)` | มาจากหลังซ้ายและเบี้ยวออกจากแกนกล้องมาก เงาจึงทอดข้างวัตถุ (ไม่ซ่อนอยู่หลังวัตถุจากมุมกล้องต่ำ) |
| เงา | Soft · strength 0.85 · bias 0.02 · normal bias 0.2 · near plane 0.2 | QualitySettings Ultra แต่ `TaskRaceSceneBuilder` ตั้ง `shadowDistance = 45` (แผนที่กว้าง ~30 u) — 150 u แบบ default เอา cascade ไปทิ้งบนท้องฟ้า เงาจึงหยัก |
| Fog | Linear · สี `(0.80,0.89,0.98)` · 25 → 90 u | ดึงเกาะฝั่งไกลเข้าหาท้องฟ้าเล็กน้อย |
| ท้องฟ้า | `BobSkyBackdrop` (mesh ใน NDC + `BOB/SkyNDC`) | ไม่มี skybox; clear colour เป็น backup `(0.80,0.90,0.98)`; เต็มเฟรมทุก aspect โดยไม่ขึ้นกับระยะกล้อง |

ใน shader (ทั้ง `BOB/CurvedWorld` และ `BOB/ToonLit`):

- **รับเงา**: `AutoLight.cginc` + `SHADOW_COORDS` / `TRANSFER_SHADOW_WPOS` (ใช้ตำแหน่งโลกที่ดัดโค้งแล้ว) / `SHADOW_ATTENUATION`
- **เงานุ่มแบบพาสเทล**: `_ShadowTint` (ค่าเริ่มต้น `(0.55,0.58,0.78)`) เป็นสัดส่วนแสงหลักที่เหลือในเงา → เงาไม่ดำ สีโทนเย็น
- **แสงรวม**: `lit = ambient * lerp(0.55, 0.18, t) + _LightColor0 * t * lerp(_ShadowTint, 1, shadow)` โดย `t` คือ ramp 3 ขั้นเดิม
- **Sheen**: Blinn-Phong แคบ (`_Shininess` 24–32, `_Sheen` 0.06–0.10) ถูกคูณด้วย `t` และ `shadow` — เงาสะท้อนเล็ก ๆ บนขอบพร็อพ ไม่ทำให้ดูพลาสติก
- **Mottle**: `_Mottle` (±3.5% ตาม world XZ, เฉพาะผิวที่หันขึ้น) กันพื้นหญ้าแบนเป็นสีเดียว
- **Fog**: `#pragma multi_compile_fog` + `UNITY_APPLY_FOG` รวมถึง outline pass

`TaskRaceSceneBuilder` ยังเปิด **cast/receive shadow ให้ทุก renderer ของแผนที่และฮีโร่** (`EnableShadows`) เพราะ prefab ที่ import มาปิดไว้ (`m_CastShadows: 0`) ตามลุคห้องเช่าเดิม — ถ้าไม่เปิด เงาจะไม่เกิดเลยแม้ shader จะรับเงาได้


ปรับความโค้งสด ๆ: เลือก `TaskRaceMap/Systems` → คลิกขวา `BobCurvedWorldController` → `Curvature: ...`
หรือเรียก `BobCurvedWorld.SetGlobal(v)` / `controller.SetCurvature(v)` (มี blend ตาม `blendSpeed`)

## 4. กล้อง `BobTaskRaceCamera`

| ค่า | ค่าเริ่มต้น | ความหมาย |
| --- | --- | --- |
| `pitch` | 45 | เงยขึ้นจากแนวราบ (องศา) — **มติ C4** ของ docs 20 §4 |
| `distance` | 5.2 | ถอยหลังตามแนวนอน |
| `Height` (อ่านอย่างเดียว) | `tan(pitch) × distance` = 5.2 | ความสูงเหนือฮีโร่ ถอดจากมุม ไม่มีฟิลด์ที่สองให้เพี้ยนจากกัน |
| `fieldOfView` | 40 | เลนส์ perspective |
| `followTime` / `lookTime` | 0.22 / 0.14 | SmoothDamp |
| `lookHeight` | 0.6 | จุดเล็งเหนือฐานฮีโร่ |
| `focusZoom` | 0.85 | การ์ด S28 เปิด → ซูมเข้า |
| `focusShiftX` | 1.2 | เล็งเยื้องขวา ให้ฮีโร่/จุดงานอยู่ซ้ายของการ์ด |
| `focusTime` | 0.35 | เวลานุ่มของโฟกัส |

- ทำงานใน `LateUpdate` จึงชนะกล้องที่จัดเฟรมใน `Update` ของฉากเดิม
- **16 พ.ย. 2026:** เดิมเป็น `height 6.2 / distance 5.2` (≈50°) และตารางนี้ยังเขียน 8.5/7.5 (≈48°) ซึ่งไม่ตรงกันทั้งคู่. ตอนนี้เหลือ `pitch = 45` ตัวเดียวตามมติ C4 (45° size 4.1) แล้วถอด `Height` ออกมา — แก้ที่มุมที่เดียว ทุกช็อต (`TaskRaceVerify` อ่าน `rig.Height`/`rig.Distance`) ตามไปด้วย
- `SetMinigameFocus(bool)` เรียกจาก `TaskRaceModeManager` เมื่อ `TaskSpot.AnyOpened/AnyClosed`
- ตั้ง `BobCurvedWorld.SampleCamera` ทุกเฟรม เพื่อให้ป้าย/วงแหวนใช้จุดกึ่งกลางความโค้งเดียวกับ shader

## 5. แผนที่เกาะคู่ (ไม่ mirror)

```
                 CliffWaterfall (ฉากหลังเหนือ) · Waterfall_Detail (ผาหิน+น้ำตกที่สร้างเอง)
   TeamA (-X)          RiverChannel (กลาง)          TeamB (+X)
  Island + Props    BridgeDetail_Group (สะพาน N/S)   Island + Props (สำเนาเลื่อน X)
   + Decor_*        Bridges_Group (walk collider)     + Decor_* (สำเนา)
                    BridgeBarrier_Group (ประตู N/S เปิดทุกโหมด)
                    RiverBarrier_Group (ตลิ่ง+hedge, PureRace เท่านั้น)
```

- **สองเกาะต้องเหมือนกันทุกจุด:** ทั้ง TeamA และ TeamB ใช้ prefab เกาะตัวเดียวกัน (`SM_bob_map_island_land`) scale เดียวกัน ไม่มี "island B" แยกอีกแล้ว — การแข่งที่เกาะไม่เหมือนกันเสียเปรียบฝั่งหนึ่งด้วยตาเปล่า; prop/สถานี/มัณฑนศิลป์ทุกชิ้นถูก clone จาก A ไป B ด้วยการเลื่อน X อย่างเดียว และ `BobFieldDressing` ทั้งสองฝั่งใช้ seed 1975 เดียวกัน
- Team B คือ **สำเนา Team A ที่เลื่อนตาม X เท่านั้น** (`TransformPoint` + `Vector3.right * dx`) ไม่มีการหมุน/กลับด้าน
- เหตุผล: HUD มือถือ (จอยล่างซ้าย, ปุ่มทำงานล่างขวา, ใบงานบนซ้าย) บังมุมเดียวกันทั้งสองเกาะ จุดงานจึงไม่ถูกบังฝั่งใดฝั่งหนึ่ง
- **พื้นเล่นคือ slab `Field`** (cylinder + convex MeshCollider, top y = 0) วางบนเกาะ ไม่ใช่ bounds ของ mesh เกาะ — เกาะ Tripo เป็น *ชุดประกอบ* (พื้นดิน + ก้อนหิน + ต้นไม้/ของลอย) จึง fit แต่ละแกนด้วย `FitAxisAligned` แล้วเลื่อนด้วย `AlignGround` ให้ percentile 0.6 ของพื้นใน footprint ลงมาแตะ -0.05 (ถ้าใช้ bounds top ต้นไม้ที่ลอยอยู่จะกลายเป็น "พื้น" แล้วพื้นจริงจมลงไป 0.5–5.8 u ตามที่ตรวจด้วย raycast)
- เกาะจึงเป็นฉากหลังล้วน ๆ: เนิน/ตลิ่ง/ก้อนหินยังโผล่พ้น slab ได้ และไม่ต้องใช้ MeshCollider บน mesh ที่ decimate แล้ว
- **สะพานมีสองชั้น:** `BridgeDetail_Group` (เปิดตลอด) ถือแผ่นไม้/ราว/ตอม่อที่ `BobBridgeDressing` สร้าง และ `Bridges_Group` (เปิดเฉพาะ TimedBridgeRaid) ถือเฉพาะ `Walkable` collider ที่ bake เข้า NavMesh — สะพานจึง **อยู่ในภาพทุกโหมด** แต่กลางสะพานมีประตูแดง-ครีม (`BridgeBarrier_Group`, เปิดทุกโหมด) ที่ปิดทางจนกว่าจะถึงเวลาของโหมด แทนที่จะซ่อนสะพานทั้งหลัง (ทำให้เกาะดูขาดจากกันอย่างไร้เหตุผล)
- **mesh ที่สร้างตอน `OnEnable` ต้องอยู่ใต้ component ของตัวเอง:** mesh เป็น `HideAndDontSave` จึงไม่ถูกเซฟลงซีน — ถ้า component อยู่ใต้กลุ่มที่ปิดตอน build (เช่น `Bridges_Group` ใน PureRace) mesh จะไม่ถูกสร้างเลย ฉากที่เซฟจึงเห็นสะพานหาย; ย้าย deck ไป `BridgeDetail_Group` และ parent mesh ใต้ transform ของ component แก้ทั้งสองปัญหา
- **ความยาวสะพานคิดจากขอบ `Field` ไม่ใช่ความกว้างช่องน้ำ:** `Field` เป็นวงรี (cylinder) ขอบฝั่งที่ z ของสะพานจึงแคบกว่าเส้นผ่านศูนย์กลาง สูตรคือ `2 × (islandGap/2 − bankEdge(z) + 1.2)` โดย `bankEdge(z) = fieldHalfX × √(1 − (z/fieldHalfZ)²)` — ปลายสะพานจึงจมเข้าหาเกาะ 1.2 u เกินขอบจริง ซึ่งมากกว่าการกัดของ NavMesh (agent radius 0.5 u) พอที่พื้นสองเกาะจะเชื่อมกัน
- `Walkable` ถูก parent ไว้ใต้สะพาน (เพื่อให้เปิด/ปิดตามโหมด) แต่ prefab สะพานถูกหมุน 90° และ scale ไม่เท่ากันทุกแกน — **ห้ามเซ็ต `localScale = 1/lossyScale`** (แกน X/Z จะสลับกัน ได้กล่องยาว 21 u กว้าง 0.8 u) ให้วาง `localScale = 1` แล้วหาร `BoxCollider.size` ด้วย `lossyScale` ที่วัดได้แทน; ตรวจด้วย `TaskRaceVerify` ที่ log `deck ... collider x[...] z[...]`
- TaskSpot วางบนพื้น ไม่ใช่บนเคาน์เตอร์: กำหนด `ringAnchor` เป็นจุดพื้น (ถ้าว่างจะใช้ transform ของตัวเอง)
- **วัสดุ/สี:** map piece ทุกชิ้นใช้ `Resources/Materials/M_BOB_CurvedMap.mat` (BOB/CurvedWorld + `T_BOB_Palette` แบบ point) และ UV ทุกจุดชี้ไปช่อง palette — flat `_Color` ที่ไม่มี texture จะไม่ตรงสีกับ mesh อื่น `UsePaletteCell` ใน assembler จึงตั้ง UV เป็นช่อง `grass (8,2)`, `dirt (14,1)`, `stone (10,2)`, `wood (10,1)`

### 5.1 มัณฑนศิลป์พื้นหญ้า (`BobFieldDressing`)

สร้างเป็น mesh เดียวต่อเกาะ (UV ชี้ palette, วัสดุ `M_BOB_Dressing` = `BOB/Dressing` แบบ 2 ด้าน ไม่มี outline pass) ด้วย seed 1975:

| องค์ประกอบ | จำนวน (สเกลจากพื้นที่วงรี) | ลักษณะ |
| --- | --- | --- |
| ใบหญ้า | `area × 18` (~790 / 1,236 / 1,780 ต่อเกาะ solo/duo/team) | ใบสามเหลี่ยมเรียว 6–9 ใบต่อกอ สูง 0.08–0.19 u, ปลายสว่างกว่าโคน |
| ดอกเดซี่ | `area × 1.8` (~79 / 124 / 178) | พัดกลีบ 3 ชั้น + ตากลมสีทอง อ่านเป็นดอกไม้จากทุกมุม |
| ก้อนหิน | `max(10, area × 0.3)` (~13 / 20 / 29) | กล่องหกเหลี่ยมจมลงหญ้า ไม่มี collider |
| แถบตัดหญ้า | `max(8, area × 0.35)` (~15 / 24 / 34) | วงหลายเหลี่ยมแบน เฉด `0.97–1.08` ของหญ้าเข้ม (เดิมเข้ม 0.86 แล้วอ่านเป็นลายพราง) |

- เว้นรัศมี 1.7 u รอบทุก `TaskSpot` และเว้นกรอบ collider ของสะพาน (±0.25 u) — ใบหญ้าไม่แทงขึ้นมาใต้ทางเดิน
- ทั้งสองเกาะใช้ค่าเดียวกันทั้งหมด จึงได้ลายหญ้าเหมือนกันแบบจุดต่อจุด

### 5.2 มัณฑนศิลป์อุปกรณ์ประกอบฉาก (14 ชิ้นต่อเกาะ)

วางใต้ `TeamA/Props` **ก่อน** `CloneTeamA()` ชิ้นทั้งหมดจึงถูกคัดลอกไป Team B ตรงตำแหน่ง (เลื่อน X เท่านั้น) ตำแหน่งเขียนบนเกาะ 12 u แล้วคูณ `islandSize / 12`; ทุกชิ้นถูกวางให้ฐานแตะ y = 0.02 และสเกลด้วยความสูงเป้าหมาย:

| กลุ่ม | prop (Resources/Props) | บทบาท |
| --- | --- | --- |
| ไฟ | `street_lamp` ×2 | คนละมุมของแถบกลาง ให้แนวทางเดินและจุดโฟกัส |
| ปลูก | `bush_green` ×1 (ย่อ 0.6 m), `pot_blue`, `pot_ceramic_gold` | พุ่มดอกย่อเล็กสุดเพราะต้นเต็มกว้าง 3.0 × 2.9 u เกินแถบกลาง |
| คาเฟ่ | `table_round_cafe`, `stool_red` ×2 | มุมพักริมสะพาน สีแดงตัดหญ้าเขียว |
| ของขาย | `crate_oranges` ×2 | ลังส้มข้างทาง ใกล้โซนสถานี |
| ป้าย | `chalkboard_easel`, `sign_stand_wood` | ป้ายชอล์ก + ป้ายไม้ บอกบรรยากาศตลาด |
| ธรรมชาติ | `rocks_grass` ×2 | ก้อนหินโผล่หญ้า เสริมงานของ dressing |

- **ทุกชิ้นอยู่ในแถบกลางของเกาะ** (`|z| ≤ ~0.74` หลังหัก footprint ตัวเอง) — แผ่นสะพานช่วงเดียว (z 1.88–3.52) คร่อมขอบสนามฝั่งเหนือไว้แล้ว (ข้อ 5.4) ของเดิมที่วาง z ±0.3–0.5 จึงยื่นเข้าไปใน deck
- รั้ว `fence_wood` ถูกถอดออก เพราะความยาวของมันพาดเข้าแถบสะพานตะวันตกเสมอ
- ขาด prefab ตัวใด log จะเตือน `decor prefab missing: SM_…` และข้ามชิ้นนั้น ไม่ทำให้ build ล้ม

### 5.3 เงาใต้เคาน์เตอร์ (contact AO)

- `AddContactShadows` วาง quad มืดใต้ `CounterTop` ของทุกสถานี ขนาด `Clamp(max(width, depth) × 1.45, 1.6, 2.6)` u
- ก่อนหน้านี้วัดจาก **ทุก** renderer ใต้ TaskSpot (รวมวงแหวน/เครื่องมือยาว) ได้ decal 9.9–20.2 u กลายเป็นวงม่วงล้อมเกาะทั้งใบ — ต้องวัดจาก top ของเคาน์เตอร์เท่านั้น

### 5.4 ทางเดินสะพานปลอดสิ่งกีดขวาง + ขอบน้ำ

แผ่นสะพานวัดจากซีนจริง (15 พ.ย. 2026, **สะพานเดียว**): render `x[-6.03,6.03] z[1.85,3.55]`, collider `x[-5.78,5.78] z[1.88,3.52]` เทียบกับ `Field` ครึ่งลึก 3.6 u — deck คร่อมขอบสนามฝั่งเหนือพอดี ของประดับจึงอยู่ได้เฉพาะแถบกลาง (`|z| ≤ ~0.74`) ใต้สะพาน

- **กฎจำนวนสะพาน:** เกาะสองเกาะ = สะพาน **หนึ่ง** ช่วง — สะพานกินที่เฉพาะระหว่างสองฝั่ง ถ้าจะมีสองสะพานต้องมีเกาะที่สามมารองรับ (owner, 15 พ.ย. 2026) ของเดิมมี `Bridge_North` + `Bridge_South` ทั้งที่เกาะมีแค่สองใบ จึงเหลือช่วงเดียวที่ z = `islandDepth × 0.3` (solo 2.70 · duo 3.38 · team 4.05) — เป็นเลนที่ว่างจากแถว E-counter (spine เริ่มที่ z 1.30) และอยู่ฝั่งที่กล้องเล่นมองเห็น (+Z)

- `BobMapAssemblerEditor.ValidateClearance` ถูกเรียกจาก `TaskRaceSceneBuilder.BuildVariant` **หลัง** clone ฝั่ง B (ก่อน clone ยังไม่มีสถานี/ของประดับ) และ log `clearance decks: …` ตามด้วย `clearance: N prop(s) checked, 0 decor and 0 station overlap(s) on 1 deck(s)`
- กฎสามโซน: บน deck ต้องห่าง **1.5 u**, ข้าง deck **1.0 u** (สถานีผ่อนเป็น 0.2 u เพราะตัวเคาน์เตอร์คือเป้าหมายการเล่น), ทางลง/landing **1.5 u**; ตัวเลขนี้ทำให้ของประดับอยู่บนเกาะ 12 u ได้โดยไม่ต้องโล่งทั้งเกาะ
- ค่าที่ผ่านแล้ว: solo / duo / team `12` ชิ้นตรวจแล้ว — ต้องเป็น 0 overlap ทุกโหมด; ถ้ามีจะได้ warning `clearance: <ชื่อ> on <side> is X u <zone> (need Y) x[…] z[…]`
- **ขอบน้ำ:** `TrimRiverSouth` บีบ vertex ของ mesh แม่น้ำที่ไม่สูงกว่า `cutZ = -islandDepth × 0.5` ให้มาที่ `cutZ` — solo `-4.50` (500 vertex), duo `-5.63`, team `-6.75` น้ำจึงจบเสมอขอบผาหน้าตึก ไม่ยื่นเป็นแผ่นน้ำค้างใต้เกาะ

## 6. สองโหมดบนแผนที่เดียว

ซีนถูก bake และเซฟให้เริ่มที่ **`TimedBridgeRaid`** (ค่าเริ่มต้น `mode` ของ manager) — PureRace ยังเลือกได้จาก Inspector/หน้าต่าง assembler

| | PureRace | TimedBridgeRaid (ค่าเริ่มต้นของซีน) |
| --- | --- | --- |
| `RiverBarrier_Group` | เปิด — ตลิ่ง + hedge มี collider + `NavMeshObstacle` (carve) | ปิด |
| `Bridges_Group` | ปิด — เหลือแต่ walk collider ที่ไม่ทำงาน | เปิด — collider + `NavMeshLink` ของสะพาน (ช่วงเดียว) |
| `BridgeBarrier_Group` | **เปิด** — ประตูแดง-ครีมกลางสะพานปิดตลอดแมตช์ | **เปิด** — เขย่า 5 วิที่ 65 วิ, ทรุด 1.5 u ใน 0.6 วิที่ 60 วิ แล้วเปิดทาง |
| `BridgeDetail_Group` | เปิด — แผ่นไม้/ราว/ตอม่อยังอยู่ในภาพ | เปิด |
| กติกา | ต่างคนต่างอยู่เกาะตัวเอง — เห็นสะพานแต่ผ่านประตูไม่ได้ | 60 วิแรกแยกเกาะ, 60 วิหลังข้ามไปแย่ง `TASK_LOCK`/ป่วนสถานีฝั่งตรงข้ามได้ |

- `TaskRaceModeManager.ApplyMode()` เรียก `CollectBarriers()` ก่อนเสมอ (idempotent — `Configure` แค่ปรับขนาด blocker เดิม) แล้วสลับกลุ่ม, สร้าง/ปรับ obstacle หรือ link, สลับ NavMesh และ `CloseBarriers()` — จึงเรียกได้ทั้ง Play และ edit mode (verify/debug bar ใช้ path นี้)
- **`BobBridgeBarrier` คือกำแพงจริง:** `BoxCollider` เต็มความกว้าง deck (`0.4 × 1.2 × width`) + `NavMeshObstacle` (carve, `carveOnlyStationary`) เปิดพร้อมกัน; `Open()` ปิด blocker ทันทีแล้วค่อย animate ลง (edit mode ลงทันที) — `NavMeshLink` ของสะพานจึงรับช่วงเดินได้; `CloseInstant()` คืนตำแหน่งเดิมตอนรีเซ็ต/สลับโหมด
- `BobBridgeGate` เป็น mesh ภาพเท่านั้น — component `BobBridgeBarrier` อยู่ GameObject เดียวกัน จึงทรุดไปพร้อมกัน
- **ห้าม `AddComponent` ใน `OnValidate`:** Unity โยน `SendMessage cannot be called during Awake/CheckConsistency/OnValidate`; ใช้ `EnsureParts()` สร้าง blocker และ `ApplyBlockerSize()` ปรับขนาดเท่านั้น
- `NavMeshLink` เป็นของจริงจากแพ็กเกจ **AI Navigation 2.0.16** (assembly `Unity.AI.Navigation`) — จุดปลายคำนวณจาก world bounds ของสะพาน (`EnsureBridgeLink`) ไม่ใช่ค่าคงที่
- `TaskRaceModeManager.Instance` เป็น lazy singleton (`FindAnyObjectByType` ครั้งแรก, เคลียร์ใน `OnDestroy`) เพื่อให้ edit-mode verify และ debug bar เรียกใช้ได้โดยไม่ต้องมี `Awake`
- event `ModeChanged`, `BridgesWillOpen`, `BridgesOpened`, `PointsChanged`, `ToastRequested` ให้ HUD ผูกได้โดยไม่ต้อง poll

### 6.1 NavMesh ต่อโหมด (bake ล่วงหน้า)

`TaskRaceSceneBuilder` bake NavMesh **สองชุดต่อซีน** ก่อนเซฟ: `Assets/Scenes/NavMesh/{Scene}_{PureRace,TimedBridgeRaid}.asset` (asset เก่าที่ชื่อ `_BridgeRaid` ถูกลบตอน build)
แล้ว `TaskRaceModeManager.ApplyNavMesh()` สลับชุดตามโหมด (`RemoveData` → `navMeshData = ...` → `AddData`)

- `NavMeshSurface` อยู่บน `TaskRaceMap` (`collectObjects = Children`, `useGeometry = PhysicsColliders`)
- `TagWalkables` ติด `NavMeshModifier` ให้ collider ทุกตัวในแผนที่: เฉพาะที่ชื่อ `Field` และ `Walkable` (พื้น + สะพาน) ถูก bake, ที่เหลือ (ตลิ่ง, hedge, หน้าผา, prop, เกาะ Tripo) ถูก `ignoreFromBuild`
- PureRace ได้พื้นสองเกาะแยกกัน (ไม่มีสะพาน active) — TimedBridgeRaid ได้พื้นสองเกาะ + แผ่นสะพานที่เชื่อมกันจริง
- agent radius ยังเป็นค่า Humanoid ของโปรเจกต์ (0.5 u) จึงกัดขอบพื้นเข้าไป ~0.5 u — นี่คือเหตุผลที่สะพานต้องจมเข้าหาเกาะ 1.2 u ไม่ใช่แค่แตะขอบ
- **ประตูที่ปิด carve deck จริงตอนเล่น:** obstacle ของ `BobBridgeBarrier` ตัดพื้นกลางสะพาน ทำให้ path ฝั่งตรงข้ามเป็น `PathPartial` จนถึง 60 วิ; พอ `Open()` ปิด blocker path กลับเป็น `PathComplete` (ข้อ 12 พิสูจน์ทั้งสองสภาวะ)
- Bake ใช้เวลาไม่กี่วินาทีและทำใน batch mode ได้ (ข้อ 12) — ไม่ต้อง bake ใน Play mode

### 6.2 กติกากลางรอบ: เวลา, แต้ม, ป่วน, เกราะ

| กติกา | ค่า | ที่อยู่ในโค้ด |
| --- | --- | --- |
| เวลาแมตช์ | 120 วิ (วิ่งทั้งสองโหมด) | `TaskRaceModeManager.matchDuration` |
| เตือนสะพาน | เหลือ 65 วิ → เขย่า 5 วิ + toast `สะพานจะเปิดใน 5 วินาที!` | `warningAtRemaining`, `warningShakeSeconds` |
| เปิดสะพาน | เหลือ 60 วิ → ประตูทรุด 1.5 u ใน 0.6 วิ + toast `สะพานเปิดแล้ว! ข้ามไปป่วนจุดงานคู่แข่งได้!` | `openAtRemaining`, `barrierDropHeight`, `barrierDropSeconds` |
| แต้มทีม |  pools แยกต่อทีม เริ่ม 0; `+100` ทุกครั้งที่ Dirty→Clean (รวม re-clean หลังโดนป่วน) | `pointsPerTaskComplete`, `AddPoints` |
| ป่วน | 1 ครั้งต่อสถานีต่อแมตช์; ใช้ได้เมื่อสะพานเปิด + จุดเป็นของคู่แข่ง + Clean + ยังไม่เคยป่วน + ไม่มีเกราะ; การ์ด S28 2.75 วิ, จอย > 70% ยกเลิก | `TaskSpot.TrySabotage / CompleteSabotage` |
| ผลของการป่วน | จุดกลับเป็น Dirty, วงแหวนกลับมา, งานเดิมถูกใส่คืน, ฝ่ายรับ `done`/`doneWeight` −1, `ping_pop` + toast `คราบกลับมา! …` | `TaskRaceRound.HandleSabotage` |
| เกราะ | 150 Pts, 30 วิ, ซื้อแบบ atomic (เช็กแล้วหักในจังหวะเดียว); เจ้าของต้องอยู่ NEAR + จุด Clean + ยังไม่มีเกราะ; บล็อกการป่วนด้วย `จุดงานนี้มีเกราะป้องกันอยู่!` | `TryPurchaseShield`, `TaskSpot.ApplyShield` |
| สถานีสะอาดค้าง | `restockDelay 6` วิ ก่อนงานถัดไปขึ้นสถานี — ช่วงนั้นคือหน้าต่างป่วน; ถ้าถูกป่วนก่อนถึงเวลาเติม งานที่รออยู่ถูกยกเลิก | `TaskRaceRound.restocks` |

- **ดีบักบาร์:** `showDebugBar` (เปิดในซีน) สร้างปุ่ม 8 ปุ่มมุมขวาบน — `โหมด: Raid` (สลับโหมด), `เวลา -10`, `เปิดสะพาน`, `+100 แต้ม`, `ป่วน` (บังคับป่วนจุดศัตรูจุดแรก), `เกราะ` (ให้เกราะฟรีจุดตัวเอง), `รีเซ็ต` (เริ่มแมตช์ + รอบใหม่), `ซ่อน` (เหลือชิป `DBG` ไว้เปิดคืน)
- การ์ด S28 ของงานยังเป็น **stand-in** ที่แถบวิ่งเต็มตาม `ExpectedSeconds` (ขั้นต่ำ 2.5 วิ) — มินิเกมจริงจะมาแทนในเกม layer; การ์ดป่วนใช้เวลา 2.75 วิเหมือนกัน

## 7. `TaskSpot` (F29 + S28)

**รัศมี**

- `previewRadius = 2.6` → แสดงป้ายพรีวิวสั้น (จุดสีตามหมวด, ชื่อ, `~N วิ`)
- `interactRadius = 1.2` → NEAR: วงแหวนไฮไลต์ และเกมเปลี่ยนปุ่มหลักเป็น `ทำงาน: <ชื่อ>`

**หมวด** `TaskCategory { Sweep, Wash, Scrub, Spray, Everyone }` → สี #F4A340 / #4EA8DE / #EF476F / #9B7EDE / #1FA6A0

**TASK_LOCK**

```csharp
bool ok = spot.TryOpen("ฉัน", out var message);   // false = ทำแล้ว หรือคนอื่นล็อกอยู่
if (!ok && message != null) Toast(message);       // "มายด์กำลังทำอยู่ ไปจุดอื่นนะ"
```

- ผู้ที่ล็อกเห็นป้าย `ตัวอักษรชื่อ ⋯` เหนือวงแหวน
- ปิดการ์ด: `spot.Close(completed)` — `completed = false` เก็บ `Progress` ไว้ `retainedProgressSeconds = 10` วิ (`RetainedProgress`)
- จอยเอียงเกิน `cancelThreshold = 0.7` → `TaskSpot.CancelOpenSessions()` ปิดการ์ดทันที (เหมือน S28)
- event: `NearChanged(spot, bool)`, `Opened(spot)`, `Closed(spot, bool)`, `Blocked(spot, message)` + static `AnyOpened` / `AnyClosed`; `TaskSpot.ActiveNear` คือจุดที่ใกล้ที่สุดในรัศมี NEAR
- component นี้ **ไม่สร้าง UI** — เกม/HUD เป็นคนเปิดการ์ด 1400×860 เอง
- `hasTask` / `SetTaskActive(bool)` — สถานีที่รอบไม่มีงานเหลือแล้วจะถูกปิด (ไม่มีวง, ไม่มีป้าย, กดเปิดไม่ได้) และ `TryOpen` คืน `false` พร้อมข้อความ `จุดนี้ไม่มีงานแล้ว`; `Configure(...)` เปิด `hasTask` ให้เองเมื่อแจกงานใหม่

**เจ้าของ / สะอาด–สกปรก / ป่วน / เกราะ**

- `ownerTeam` (TeamA = เกาะซ้าย, TeamB = สำเนา) + `TaskSpot.SetOwnerTeam`; `IsEnemySpot` เทียบกับ `TaskSpot.LocalTeam` (manager ตั้งจาก `localTeam`)
- `SpotState { Dirty, Clean }`: `Close(true)` → `Clean` + แจ้ง manager ให้บวกแต้ม; `HandleClosed` ของรอบจะเติมงานถัดไปหลัง `restockDelay`
- `TrySabotage(who, out msg)` / `CompleteSabotage()` — เก็บ TASK_LOCK ในโหมดป่วน, สำเร็จแล้ว `_sabotageCount = 1` (`MaxSabotagesPerMatch`), จุดกลับ Dirty, `RestoreSabotagedTask()` คืนงานเดิม; `SabotageUsed` ใช้ซ่อน prompt และกันการป่วนซ้ำ
- `TryBuyShield(out msg)` → `TaskRaceModeManager.TryPurchaseShield` (atomic) → `ApplyShield(30)`; `IsProtected` / `ShieldRemaining` นับถอยหลังใน `Update`
- prompt getters สำหรับ HUD: `CanSabotageNow` (สะพานเปิด + ศัตรู + Clean + ยังไม่ป่วน), `ShowSabotagePrompt` (+ IsNear), `ShowShieldPrompt`, `CanAffordShield`
- event เพิ่ม: `Sabotaged(spot)` + static `AnySabotaged`; ป้าย `ShieldBadge` สีทองและวงแหวนทองแสดงตอนมีเกราะ

## 8. Assembler และ asset ที่ต้องเตรียม

`Tools > BOB > Task Race Map Assembler`

**ชื่อ prefab เป้าหมาย** (ช่องในหน้าต่างกำหนดเองได้)

| ช่อง | prefab ที่หาให้อัตโนมัติ (`Assets/Resources/Props`) | GLB ดิบใน OneDrive/Rig | tri หลัง clean |
| --- | --- | --- | --- |
| Island (both teams) | `SM_bob_map_island_land` | `floating island 3d model.glb` | 42,640 → 8,000 |
| River | `SM_bob_map_river_channel` | `stylized river water 3d model.glb` | 51,638 → 8,000 |
| Bridge | `SM_bob_map_wooden_bridge` | `wooden footbridge 3d model.glb` | 53,657 → 7,999 |
| Cliff | `SM_bob_map_cliff_waterfall` | `waterfall diorama 3d model.glb` | 57,117 → 7,999 |

**เกาะเดียวใช้ทั้งสองฝั่ง** — ช่อง `Island B` ถูกลบออกจากหน้าต่างแล้ว (เดิมใส่ `land2` ได้ ทำให้เกาะสองฝั่งต่างกัน 209 vs 105 scale และสูง 12.6 vs 4.8 u); `SM_bob_map_island_land2` ยังอยู่ในโปรเจกต์เพื่อใช้เป็นเกาะที่สาม/ฉากแยกได้ แต่ไม่ใช่แผนที่แข่ง

โบนัสที่ยังไม่ผูกกับโค้ด: `SM_bob_map_plaza` (`circular cobblestone plaza`, 55,684 → 6,172 tri) ใช้เป็นลานกลางหรือเกาะที่สามได้

**สถานะ asset:** clean/decimate/import **เสร็จแล้ว** — ผ่าน `bob_prop_cleanup.py` ด้วย `--tint` (palette-snapped UV) แล้ว FBX อยู่ใน `Assets/Art/Map`, prefab อยู่ใน `Assets/Resources/Props` (เมนู `Bob > Art > Build Map Prefabs`); ใช้ `M_BOB_CurvedMap.mat` (มี outline pass ในตัว จึงไม่ต้องมี hull child)
ถ้าช่องว่างและเปิด `Use placeholders` หน้าต่างจะสร้างกล่องติดป้ายแทน เพื่อทดสอบเลย์เอาต์/โหมดได้ทันที

**ปุ่มในหน้าต่าง**

1. `Build map skeleton` — สร้าง `TaskRaceMap/{TeamA,TeamB}/{Island,Props}`, river (ตัดปลายใต้ flush ข้อ 5.4), cliff, `BridgeDetail_Group` (deck ที่มองเห็นทุกโหมด), `BridgeBarrier_Group` (ประตูแดง-ครีม 2 บาน + `BobBridgeBarrier`), `RiverBarrier_Group` (ตลิ่ง + hedge, PureRace), `Bridges_Group` (walk collider), `Systems` (manager + controller) และ BoxCollider แบน; ถ้าเปิด `Add BobTaskRaceCamera` จะเพิ่มกล้องให้ Main Camera พร้อม target ฮีโร่
2. `Clone Team A layout to Team B` — สำเนาพร็อพ + TaskSpot ใต้ `TeamA/Props` ไป `TeamB/Props` ด้วยการเลื่อน X และตั้ง id ใหม่ `S3_A → S3_B` (ฝั่ง B ได้ `SetOwnerTeam(TeamB)`)
3. `PureRace` / `TimedBridgeRaid` — เขียนค่าให้ manager ในฉากทันที (ค่าเริ่มต้นของทุกซีนคือ TimedBridgeRaid)
4. `Validate map` — รายงานจำนวนจุด, id ซ้ำ/ว่าง และ reference ของกลุ่มใน manager

## 9. ขั้นตอนตั้งค่าใน Inspector (ทีละขั้น)

1. **สร้างซีนทั้งฉาก** — `Tools > BOB > Build Task Race Scene` (หรือ batch `-executeMethod Bob.EditorTools.TaskRaceSceneBuilder.Build`) สร้าง `Assets/Scenes/TaskRace.unity` ใหม่ทั้งไฟล์: Main Camera + `BobTaskRaceCamera`, ดวงอาทิตย์ + ambient, ฮีโร่ `SK_hero_sweep` (CharacterController + `BobTaskRaceHero`), แผนที่จาก assembler, สถานีไม้ 3 จุดบน Team A แล้ว clone ไป Team B, ผู้อำนวยการรอบ `TaskRaceRound`, NavMesh สองชุด (ข้อ 6.1), โหมด TimedBridgeRaid และแทรกซีนใน Build Settings ลำดับ 2
2. **ปรับด้วยหน้าต่าง assembler** — ถ้าต้องวางเอง/แก้เลย์เอาต์ ใช้ขั้น 3–10 ด้านล่าง
3. **Import asset** — ทำเสร็จแล้ว (ข้อ 8); ถ้าเปลี่ยนโมเดลให้รัน `bob_prop_cleanup.py` แล้ว `Bob > Art > Build Map Prefabs` ใหม่
4. **เปิดหน้าต่าง** — เมนู `Tools > BOB > Task Race Map Assembler`
5. **ลาก prefab** เข้าช่อง Island (both teams), River, Bridge, Cliff (หรือกด `Find by name`)
6. **ตั้งเลย์เอาต์** — `Island width` ≈ 12, `Island centres` ≈ 16 (ช่องน้ำ = 4 u), `Bridge yaw` 0, `River yaw` 90
7. กด **Build map skeleton** → ตรวจว่า `TaskRaceMap` มี TeamA/TeamB/Systems ครบ
8. **วางสถานี** — ใต้ `TaskRaceMap/TeamA/Props` วางเคาน์เตอร์ (`Counter` ไม้ + `CounterTop` หิน) + prop ที่เข้าเรื่อง (`espresso_machine`, `bucket_metal_blue`, `basket_clothes`) แล้วเพิ่ม `TaskSpot` ให้ root ของสถานี:
   - `Spot ID` เช่น `S3_A`, `Task Name`, `Category`
   - `Expected Seconds` ตาม doc 16
   - `Ring Anchor` = empty บนพื้นใต้สถานี (ถ้าไม่ตั้ง วงแหวนจะอยู่ที่ origin ของ prop ซึ่งอาจลอย)
   - ปล่อย `Player` ว่างได้ ถ้าฮีโร่มี tag `Player`
   - prop จาก Tripo ต้อง **compose** rotation: `Quaternion.Euler(0, yaw, 0) * prefab.localRotation` (root มี X 270 จาก importer — เขียนทับแล้วโมเดลจะนอนราบ)
9. กด **Clone Team A layout to Team B** → ตรวจ id ฝั่ง B ลงท้าย `_B` และตำแหน่งเลื่อนตาม X เท่านั้น
10. **โหมด** — เลือก `PureRace` หรือ `TimedBridgeRaid` ในหน้าต่าง (หรือใน Inspector ของ `TaskRaceMap/Systems`)
11. **วัสดุโค้ง** — เปิด `Swap map materials to BOB/CurvedWorld` ก่อน Build หรือใช้ `M_BOB_CurvedMap.mat` ที่ตั้งไว้แล้ว (อย่าใช้ flat `_Color` เพราะจะไม่ตรงกับสี palette ของชิ้นอื่น)
12. **NavMesh** — ทำอัตโนมัติในขั้น 1: `TaskRaceSceneBuilder` ติด `NavMeshModifier`, bake สองชุด (PureRace/TimedBridgeRaid) ลง `Assets/Scenes/NavMesh/` แล้วผูกให้ manager (ข้อ 6.1) ถ้าแก้แผนที่ด้วยหน้าต่าง assembler เอง ให้รัน build ซีนซ้ำเพื่อ bake ใหม่
13. **HUD** — `TaskRaceSceneBuilder` สร้าง `TaskRaceHUD` และ `Bind(round)` ให้แล้ว (ข้อ 10) ถ้าประกอบแผนที่เองด้วยหน้าต่าง assembler ให้เพิ่ม `BobTaskRaceHud` เองแล้ว `Bind` กับ `TaskRaceRound` และผูก `manager.joystickInput` กับค่าจอยจริง
14. กด **Validate map** และเล่นทดสอบทั้งสองโหมด

## 10. การเชื่อมกับ HUD / เกม

### 10.1 `BobTaskRaceHud` (ผูกกับรอบแล้ว)

`TaskRaceSceneBuilder` วาง `TaskRaceHUD` ไว้ที่ root ของซีน (ไม่ผูกกับ `TaskRaceMap` เพราะ canvas ไม่ควรอยู่ใต้ transform ของแผนที่)
และเรียก `hud.Bind(round)` + `hud.Build(cam)` ตอนสร้างซีน — ฉากในเอดิเตอร์จึงแสดง HUD จริง ไม่ต้องกด Play

| ส่วน | ที่มา | ลักษณะ |
| --- | --- | --- |
| หัวการ์ด | `round.DoneCount` / `TaskCount` | `ใบงาน · ทีมเรา 3/6` (ตัวเลข = จำนวนงานเสร็จ ไม่ใช่น้ำหนัก ตาม doc 16 ข้อ 2.2) |
| แถบน้ำหนัก | `round.DoneWeight` / `TotalWeight` | เติมด้วย `MoveTowards` 0.75/วิ ไม่กระโดด, สี primary → success, มี `%` ข้างแถบ |
| แถวรายการ | `round.Tasks` (ลำดับตามแผน, T04 แรก) | ไอคอนหมวด (`icon_skill_*` จาก `BobArtLibrary` — builder ส่งให้ HUD ด้วย `BindArt` เพราะซีน Task Race ไม่มี `BobGame` มาตั้ง static) + ชื่องาน + `×น้ำหนัก`; งานถัดไปที่ยังไม่เสร็จมีแถบ accent ด้านซ้าย + พื้นอ่อนเพื่อให้ตาไหลไปจุดนั้น; `✓` + ขีดฆ่า = เสร็จ, `⋯ <ชื่อผู้ถือล็อก> · ชื่องาน` = มีคนเปิดการ์ด, `รอคิว` = งานยังไม่ถึงสถานี |
| ปุ่มหลัก | `TaskSpot.ActiveNear` | `ทำงาน: <ชื่อ>` → เรียก `TryOpen` (ได้ TASK_LOCK); ระหว่างเปิด = `กำลังทำ: <ชื่อ>` กดซ้ำ = `Close(false)` (progress คง 10 วิ); โดนบล็อก = toast แดงจาก `Blocked` |
| ปุ่มหลัก (ป่วน) | `near.CanSabotageNow` | `ป่วนจุดงาน: <ชื่อ>` สีแดง → `TrySabotage` → เปิดการ์ดป่วน 2.75 วิ |
| ปุ่มหลัก (เกราะ) | `near.ShowShieldPrompt` | `ติดเกราะป้องกัน (-150 Pts)` สีเข้ม; กดไม่ได้ถ้าแต้มไม่พอ (`CanAffordShield`); สำเร็จแล้วมี toast จาก manager |
| แถบบน | `manager.RemainingSeconds` / `LocalPoints` / `RivalPoints` / `round.DoneWeight` / mode | นาฬิกาเป็นตัวใหญ่สุดของจอ (48 px, แถบ 940×124 วางชิดขวาการ์ดใบงาน ไม่ทับกัน): `เวลา mm:ss` + `คะแนนทีม N` + `คู่แข่ง M · เสร็จ k/N` + `ทีมเรา P%` + บรรทัดโหมด (`Bridge Raid · สะพานเปิดที่ 60 วิ` → `สะพานเปิดแล้ว` → `Pure Race · สะพานปิดทั้งแมตช์`); แถบเวลาส้มที่ 65 วิ แดงที่ 10 วิ |
| การ์ด S28 | `BeginCard(spot, sabotage)` + `BobMinigameHost` | 13 งานที่อยู่ในห้องเล่นมินิเกมจริงในพื้นที่ 1240×620 (8 mechanic); T04/T12 ยังเป็นแถบวิ่งในโลก, ป่วน 2.75 วิ; ปิดด้วย × หรือจอย > 70%; ตอนเปิดมี chip เข้ม `ลากจอยเพื่อปิดการ์ด` 2 วิ — วางลอยเหนือจอยมุมล่างซ้ายบนฉากที่มืด (ไม่ทับพื้นที่เล่น) และเป็น container แยกเพราะลูก uGUI วาดทับข้อความของ parent |
| ดีบักบาร์ | manager debug hooks | 8 ปุ่ม (ข้อ 6.2) — **ปิดเป็นค่าเริ่มต้น** (`showDebugBar = false`) และวาดเฉพาะใน editor / development build เท่านั้น (`DebugBarAllowed`) APK ที่ลงเครื่องผู้เล่นไม่เห็นชิป `DBG` เลย |
| ท้ายการ์ด | `AdviceLine()` | บรรทัดแนะนำผู้เล่น: งานที่เหลือ + `เดินใกล้จุดงานแล้วกด ทำงาน` (สะพานปิด) / `สะพานเปิดแล้ว ป่วนจุดงานคู่แข่งได้` / `Pure Race` — ไม่มีตัวเลข `workload`/seed แบบดีบักแล้ว |

- HUD อ่าน `round.Version` ทุกเฟรม — ผู้อำนวยการรอบ bump ค่านี้เมื่อมีงานใหม่ขึ้นสถานี, มีคนล็อก/ปลดล็อก, หรือมีงานเสร็จ จึงสร้างแถวใหม่เฉพาะตอนเปลี่ยน (แถบกับ % อัปเดตทุกเฟรม)
- Canvas เป็น `ScreenSpaceCamera` ที่ผูกกับ `Camera.main` (ไม่ใช่ Overlay) เพื่อให้ `TaskRaceVerify` สลับกล้องชั่วคราวแล้วถ่ายภาพ `-hud.png` ได้ — Overlay จะไม่ถูกวาดลง RenderTexture
- `joystickInput` ไม่ต้องผูกจาก HUD: manager หา `BobTaskRaceHero.InputVector` เองถ้าช่องว่าง (คีย์บอร์ดเป็น fallback บนเดสก์ท็อป)
- toast ของป่วน/เกราะ/เปิดสะพานมาจาก `TaskRaceModeManager.ToastRequested` — HUD ไม่ต้องรู้กติกาเอง
- **รวมกับ `origin/main` (commit 110024a, 15 พ.ย. 2026):** เก็บงาน "อ่านได้ในครึ่งวินาที" จาก session ขนาน — นาฬิกา 48 px, `คะแนนทีม` / `คู่แข่ง` / `ทีมเรา %`, ไอคอนหมวด, ไฮไลต์งานถัดไป, ขีดฆ่างานเสร็จ และ chip สอนปิดการ์ด — โดยคงของ P0 ไว้ครบ (จำนวนงานคู่แข่งในบรรทัดเดียวกับแต้ม, advice footer, debug bar ที่ปิดใน build ผู้เล่น, ผลแมตช์/รางวัล) และซ่อม `BobGame.Tasks.cs` ที่ commit นั้นลบ `MiniPointer` / `IsHero` / `miniDrags` ทิ้งแต่โค้ดยังเรียกอยู่ (branch เดิมคอมไพล์ไม่ผ่าน). หลัง merge มีการจัดแนวใหม่รอบเดียว: แถบบนสูงขึ้นเป็น 124 และวางชิดขวาการ์ดใบงานเพื่อไม่ให้การ์ดสองใบครีมทับกัน (เดิม `คู่แข่ง` กับ `ทีมเรา %` นั่งบรรทัดเดียวกันจนทับ), ไอคอนแถวถอยพ้นแถบ accent 6 px (`DiscW` 24 → 40), chip สอนปิดการ์ดย้ายมาทับบรรทัดคำใบ้ใต้พื้นที่เล่น (เดิมวางทับเป้าในพื้นที่เล่น และเพลตเข้มเป็นลูกของ label จึงวาดทับข้อความตัวเอง), และผูก `BobArtLibrary` เข้า HUD (`BindArt`) เพราะในเอดิเตอร์ของซีนนี้ไม่เคยมีใครตั้ง `BobUi.Art` ไอคอนจึงเคยออกมาเป็นสี่เหลี่ยมสีทึบ

### 10.3 จอยลอย, แตะเพื่อเดิน, AI คู่แข่ง, ผลแมตช์และรางวัล (2026-11-14)

**จอยลอย + แตะเพื่อเดิน — `BobTaskRaceTouch`** (ติดกับ `TaskRaceHUD`, สร้างใน `hud.Build`)

| ส่วน | พฤติกรรม |
| --- | --- |
| โซนจอย (ล่างซ้าย 760×560) | กดตรงไหนก็ได้ในโซน → ฐานจอย + knob โผล่ใต้นิ้วโป้ง; ลาก = เดิน (`fullTilt` 150 canvas unit, dead zone 12%); ปล่อยแล้วฐานกลับจุดพัก |
| โซนแตะเดิน (เต็มจอ) | แตะครั้งเดียว = วางจุดหมายบนพื้นใต้ Finger (`Plane y = hero.y` + `ScreenPointToRay`); แตะบนปุ่ม/การ์ดของ HUD ไม่ถึงโซนนี้ เพราะโซนทั้งสองเป็นลูกคนแรกของหน้า HUD |
| ฐานจอยตอนพัก | เป็นวง ink โปร่ง (0.18) + จุดกลาง — มองเห็นบนทั้งหญ้าและท้องฟ้าสีอ่อน; ตอนลากเปลี่ยนเป็น knob ขาว 0.9 |
| ร่างกายหยุดตอนเปิดการ์ด | `BobTaskRaceHero.IsFrozen` (การ์ด S28 / ผลแมตช์) — แต่ `InputVector` ยังอ่านจอยอยู่ จึงยังใช้ท่ากดจอย > 70% เพื่อปิดการ์ดได้; โซนจอยถูกยกขึ้นเหนือ dim ของการ์ด (`KeepJoystickOnTop`) แต่แผงการ์ดยังอยู่บนสุด |
| คีย์บอร์ด | ถ้าไม่มีจอย (`input == null`) ใช้ `Input.GetAxisRaw` เหมือนเดิม — editor/emulator เล่นได้ทันที |
| กันตกขอบเกาะ | แตะฝั่งเกาะตรงข้ามขณะสะพานปิด = ไม่มีผล (กันเดินตกแม่น้ำ; แถบบนบอกเวลาสะพานเปิด); ถ้าหลุดขอบจริง `BobTaskRaceHero` เก็บจุดยืนพื้นล่าสุดและ respawn ที่จุดนั้นเมื่อ `y < −6` — ไม่มี "ตาย" หรือจอค้าง |

**AI คู่แข่ง — `BobTaskRaceRival`** (อยู่ใน `Systems` ของทั้งสามซีน)

- โมเดลจังหวะ ไม่ใช่ navmesh agent: ทุก `round.EstimatedSeconds / TaskCount × pace` (± `jitter`) จะ `TaskSpot.ForceComplete()` จุด Team B ที่ยัง Dirty ตัวแรกตาม SpotId แล้วรอ `restockDelay` ตามปกติ — แต้ม, จำนวนงาน และการเติมงานเดินผ่าน path เดียวกับคนเล่น
- ค่าเริ่มต้น `pace 1.12` (ช้ากว่าค่าประมาณ 12%), `headStart 4 วิ` (ผู้เล่นได้ออกก่อน), `jitter ±12%`, retry 1 วิ เมื่อทุกสถานีฝั่ง B สะอาดรอเติมงาน
- `pace` คือคันโยก balance แบบล่องหน: 1.0 = ตามค่าประมาณพอดี, 1.2 = คู่แข่งผ่อนปรน — รอบนี้ไม่ต้องแก้กติกาหรือ nerf ฝั่งผู้เล่น
- หยุดทันทีเมื่อ `manager.LastResult.HasValue` หรือหมดเวลา; ครบรายการฝั่ง B แล้วเรียก `manager.EndMatch()` เอง

**ผลแมตช์ + รางวัล — S27 `ShowResult` / `BobTaskRaceProfile`**

- `TaskRaceModeManager.EndMatch()` ตัดสินครั้งเดียว (`LastResult.HasValue` กันจ่ายซ้ำ): ฝั่งเราเสร็จครบทุกงาน = ชนะทันที (`RoundWon`), ฝั่งคู่แข่งครบก่อน = แพ้, ไม่งั้นเทียบแต้มที่เหลือ (เท่ากัน = เสมอ); ตั้ง `RemainingSeconds = 0` แล้วยิง `MatchEnded`
- `BobTaskRaceProfile.Record` จ่าย `coins = 25 + (ชนะ 50 / เสมอ 20 / แพ้ 10) + แต้มของเรา/20` และ `xp = 30 + (ชนะ 25) + งานที่ทำเสร็จ × 8` ลงคีย์เดียวกับ lobby (`bob.coins`, `bob.xp`) + `bob.taskRace.best/matches/wins`; `Save()` ทันที
- Overlay S27: หัว `ชนะ! / เสมอ / แพ้`, `เรา mine : theirs คู่แข่ง`, `งานเสร็จ x/N · คู่แข่ง y/N`, กล่องรางวัล `+coins เหรียญ +xp XP` + สถิติ (`สถิติใหม่!` เมื่อแต้มเราสูงกว่าเดิม), ปุ่ม `เล่นอีกครั้ง` (`RestartMatch`: seed ใหม่ + `BeginMatch`, เกตปิด, 120 วิ) และ `กลับเมนู` (`SceneManager.LoadScene("Game")`)
- เกราะกันการจ่ายซ้ำ: `HasShownResult`/`resultRoot` ใน HUD, `LastResult` ใน manager; verify ใช้ `DebugShowResult()` (`record:false`) จึงไม่แตะ wallet ของเครื่อง developer
- เงินที่จ่ายในแมตช์โผล่ที่แถบสกุลเงินของ Home ทันทีเมื่อกลับมา เพราะเป็น PlayerPrefs คีย์เดียวกับ lobby


### 10.2 สัญญาที่เหลือให้เกม layer

```csharp
TaskSpot.AnyOpened += s => OpenMinigameCard(s);        // การ์ด S28 ของ task
TaskSpot.AnyClosed += s => CloseMinigameCard(s);

// ส่ง progress จากการ์ดกลับเข้าจุดงาน
spot.AddProgress(delta);          // 0..1
spot.Close(completed: true);      // จบงาน → รอบนับน้ำหนักและเติมงานถัดไปให้สถานี
```

`TaskRaceModeManager.joystickInput` เป็น `Func<Vector2>` — ตั้งเป็น `() => joystick.Value` เพื่อให้กฎ "จอย > 70% ปิดการ์ด" ทำงาน
ถ้าไม่ตั้ง จะใช้ `Input.GetAxisRaw` ของคีย์บอร์ดสำหรับทดสอบบนเดสก์ท็อป

## 11. ข้อจำกัดและงานถัดไป

- สถานีในซีนเป็น **ตัวอย่าง** (solo 3 / duo 4 / team 6 จุดต่อเกาะ, เคาน์เตอร์ไม้ + prop) เพื่อทดสอบเลย์เอาต์และความหนาแน่น ไม่ใช่ชุดสถานีจริงตาม doc 16/19
- หน้าตัดของเกาะทั้งสองฝั่งเหมือนกันแล้ว (prefab เดียว) — ถ้าต้องการเกาะที่ต่างกันจริง ๆ ต้องทำ prefab ใหม่ที่ผ่าน pipeline เดียวกัน ไม่ใช่ใช้ `land2` ที่สูงกว่า 2.6 เท่า
- เกาะสองฝั่งเหมือนกันทุกจุดรวมถึงลายหญ้า/หิน/decor — ถ้าอนาคตเพิ่ม variation ต่อฝั่ง ต้องทำแบบ symmetric (เช่นสะท้อนรอบแกน X) ไม่ใช่ seed ต่างกัน ซึ่งจะทำให้ฝั่งหนึ่งดูได้เปรียบ
- ตัวละคร rig ใหม่ 3 ตัว (`bob_hero_sweep/spray/press` ใน Rig) มี skin แต่ **ไม่มี animation clip** และไม่มี rig ของ bubble — ฮีโร่ในซีนใช้ `SK_hero_sweep` แบบนิ่ง ยังไม่มีแอนิเมชันเดิน
- **การ์ดมินิเกม S28 ทำแล้ว 8 mechanic** (`BobMinigameHost`): trace (T01/T14), scrub (T02/T07-1/T08-2/T09-2), tap (T03/T10/T07-3/T08-1), sort (T05), hold (T06/T07-2/T09-1/T13), fill (T11), sequence (T15) — T04 (cart) และ T12 (chairs) อยู่ในโลกพร้อมแถบเวลาตาม doc 16, การ์ดป่วนเป็นแบบจับเวลา; `Tick` คืน `true` เมื่อจบแล้ว HUD เรียก `spot.Close(completed: true)` จุดเดียว (ข้อ 10.2) ยังเหลือ **ฉาก prop 3D (RenderTexture)** ที่ใช้ `SM_*`/`SK_*` จริงแทนรูปทรง 2D
- `TaskRaceRound` ยังไม่มีคราบฉุกเฉิน T16 และยังไม่รับ seed จาก server — ทั้งสองเกาะใช้ seed เดียวกันในเครื่องเดียว (ข้อ 14); แถบคู่แข่งบน HUD ทำแล้ว (ข้อ 10.3); เสียงของการ์ดมินิเกมทำแล้ว 16 พ.ย. 2026 (ข้อ 15.18)
- ยังไม่มี server contract ของ Task Race map (`TASK_BEGIN`/`TASK_LOCK` ตาม doc 16) — TASK_LOCK รอบนี้เป็นฝั่ง client
- คู่แข่งเป็น **โมเดลจังหวะ** (`BobTaskRaceRival`, ข้อ 10.3) ไม่ใช่ agent ที่เดินบน NavMesh: ยังไม่มีการเดินข้ามสะพานไปป่วนจุดฝั่งเราให้เห็น และยังไม่ตอบโต้การป่วน (เมื่อมี server/AI จริง ให้แทน `ForceComplete` ด้วย action ที่เดินและเปิดการ์ดจริง — HUD และกติกาไม่ต้องแก้)

## 12. การตรวจสอบ

สร้างซีนใหม่ทีละโหมด (ผ่านเมื่อ exit code 0 และ log มี `[TaskRaceScene] ... N task spot(s), map prefabs real` — N = 6 / 8 / 12):

```bash
"C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "C:/Users/Jakgrits/BOB-repo/unity" \
  -executeMethod Bob.EditorTools.TaskRaceSceneBuilder.Build \
  -logFile "C:/Users/Jakgrits/BOB-repo/unity/Logs/taskrace-Build-build.log"
# สลับ Build -> BuildDuo -> BuildTeam (สร้างตามลำดับนี้เสมอ เพื่อให้ Build Settings เรียง solo → duo → team)
```

ตรวจซีน + เรนเดอร์ภาพ (`Run` / `RunDuo` / `RunTeam`):

```bash
".../Unity.exe" -batchmode -quit -projectPath "C:/Users/Jakgrits/BOB-repo/unity" \
  -executeMethod Bob.EditorTools.TaskRaceVerify.Run \
  -logFile "C:/Users/Jakgrits/BOB-repo/unity/Logs/taskrace-verify-Run.log"
```

ผลที่ต้องเห็น: `spots:` **12 id** (`S1_A..S6_B`, rig 6 สถานี × 2 ฝั่ง ทุกโหมด), `baked tasks:` ชื่อมินิเกมจริงบนทุกจุด, `round D1`–`round D3` (จำนวนงาน `players`, `mean station`, `workload` และรายการ T-id),
`floor under spots: (x,z)y=0.00` ทุกจุด, `TimedBridgeRaid: ... barriers=1 links=1`, `PureRace: obstacles=3 (3 before) carving=3`, แล้วได้ภาพ

**NavMesh + ประตูสะพาน (ข้อ 6.1)** — build log ต้องมี `navmesh geometry: 3 walkable collider(s), 3 ignored` (Field 2 + deck 1) และ
`navmesh baked: PureRace ... (solo 24 / duo 28 / team 28 tri), TimedBridgeRaid ... (40 / 44 / 46 tri)` — ส่วนต่างคือแผ่นสะพานช่วงเดียว (16 / 16 / 18 tri) และต้องไม่เป็น 0
verify log ต้องได้ **สองสภาวะ** ของโหมดเดียวกัน (ประตูปิด carve deck / ประตูเปิดเชื่อม):

```
navmesh TimedBridgeRaid gates shut: 40 tri, ...
nav TimedBridgeRaid gates shut: ... status=PathPartial corners=4 length 9.0 u      <- ประตู carve ตัดกลางสะพาน
navmesh TimedBridgeRaid gates open: 40 tri, ...
nav TimedBridgeRaid gates open: ... status=PathComplete corners=5 length 21.0 u    <- ข้ามได้
probe Bridge centre ...: on navmesh, off by 0.00 u                                 <- แผ่นสะพานอยู่บน NavMesh
deck Bridge: ... collider BoxCollider x[-5.78,5.78] y[-1.00,0.00] z[1.88,3.52]     <- ขนาดถูก (ยาว ~12, กว้าง ~1.7)
navmesh PureRace: 24 tri, ...
nav PureRace: ... status=PathPartial ...                                          <- ข้ามไม่ได้ (ไม่มีสะพาน)
```

- verify วัดรอบเปิดโดย `manager.DebugOpenBridges()` + ปิด `bridgeBarrierGroup` + `RemoveData/AddData` ของ `raidNavMesh` — กันการ carve ค้างจาก edit mode
- ถ้า `gates open` ยัง `PathPartial`: ดู `deck ... collider x[...]` ก่อน — ถ้ากว้างกว่าสะพานแปลว่า scale ถูก invert ผิด (ข้อ 5); ถ้าขนาดถูกแต่ยังไม่เชื่อม ให้เพิ่มระยะจมเข้าหาเกาะ (1.2 ในสูตร `bridgeTarget`) แล้ว build ซีนใหม่
- ถ้า `gates shut` กลับเป็น `PathComplete`: obstacle ของ `BobBridgeBarrier` ไม่ carve — เช็ก `carving = true` และ `width` ตรงกับ deck

**กติกากลางรอบ (ข้อ 6.2)** — verify log ต้องมี (solo `6`, duo `7`, team `9` งาน):

```
gameplay start: clock 120s, bridges open=False, barriers=2, points A/B 0/0
gameplay clean: points A/B 100/100 (expected +100 each), round done A 1/6 B 1/6
gameplay sabotage while shut: blocked=True msg="สะพานยังปิดอยู่ ข้ามไม่ได้"
gameplay sabotage open: opened=True ... state=Dirty count=1/1 defender B done 1->0/6
gameplay sabotage repeat: opened=False msg="จุดนี้ถูกป่วนไปแล้ว" re-clean B done 1/6
gameplay shield: bought=True ... protected=True timer 30s, pool A 150->0 ... enemy sabotage blocked=True msg="จุดงานนี้มีเกราะป้องกันอยู่!"
gameplay clock: at 60s bridges open=True, gates dropped=True (2 barrie(s))
```

- `barriers=0` → `ApplyMode()` ไม่ได้เรียก `CollectBarriers()` (edit mode ไม่มี `Awake`)
- points ยัง 0/0 → `TaskRaceModeManager.Instance` เป็น null (ต้องเป็น lazy singleton)
- `bought=False` → แต้มทีมไม่ถึง 150; verify เติมให้ถึงก่อนซื้อ (งานละ 100)

**HUD ผู้เล่น + touch + คู่แข่ง + ผลแมตช์ (ข้อ 10.1, 10.3)** — verify log ต้องมี (solo 6 / duo 7 / team 9 แถว):

```
player HUD: 6 task row(s), canvas ScreenSpaceCamera, touch=joystick + walk, rival=pace 1.12, ...
result overlay: open=True draw 0:0 points · tasks 0/6 vs 0/6 · reward 45 coins +30 xp · best 0
```

- `rival=missing` → ซีนยังไม่ถูก build ใหม่หลังเพิ่ม `BobTaskRaceRival` (build ทุกโหมด) หรือ component ไม่อยู่ใต้ `Systems`
- `touch=missing` → `showDebugBar`/`buildTouchControls` ใน Inspector ถูกปิด หรือ `BobTaskRaceTouch` หา `BobTaskRaceHero` ไม่เจอ (ดู warning)
- `result overlay: open=False` → `manager.EndMatch` ไม่ถูกเรียก หรือ HUD ไม่ได้ผูก manager (`Hook`)
- ภาพ `Logs/taskrace{-duo,-team}-hud.png` = HUD ที่ผู้เล่นเห็น (ดีบักบาร์ถูกซ่อนแล้ว), `...-result.png` = overlay S27; verify ใช้ `record:false` จึงไม่เปลี่ยน wallet

แล้วได้ภาพ
`Logs/taskrace{-duo,-team}-play.png` (มุมกล้องจริงของฮีโร่), `...-preview.png` (มุมกลาง), `...-wide.png` (ภาพรวมสองเกาะ)

**แสง เงา และท้องฟ้า (ข้อ 3.1)** — build log ต้องมี `cast/receive shadows on for N renderer(s) under TaskRaceMap`
และอีกบรรทัดสำหรับ `Player` (solo ≈ 86 + 2, duo ≈ 68 + 2, team ≈ 122 + 2) — ถ้าไม่มี เงาจะไม่เกิดแม้ shader รับเงาได้
ในภาพ `-preview.png` / `-wide.png` ต้องเห็นเงาของฮีโร่ ลูกบาศก์เคาน์เตอร์ และพุ่มไม้ทอดไปทางขวาของวัตถุบนพื้นหญ้า
และเห็นฟ้าตั้งแต่ pale blue ด้านล่างถึงน้ำเงินเข้มด้านบนพร้อม cloud puffs (ไม่มีแถบพื้นหลังแบนสีเดียว และไม่มีขอบ quad ตรงกลางภาพ)
ถ้าเห็นเงาแต่แบนเป็นวงกลมเทา: ยังใช้ FallBack shadow caster ของวัตถุที่ import มา (ปิด `m_CastShadows`) — เรียก `EnableShadows` ใน builder อีกครั้ง
ถ้าเงาเป็นเส้นหยัก/จุด (shadow acne) บนพื้นหญ้า: เพิ่ม `shadowNormalBias` ของดวงอาทิตย์ (ปัจจุบัน 0.2)

**ความเท่าเทียมของสองเกาะ + มัณฑนศิลป์ (รอบที่ 4)** — build log ต้องมี
`[TaskRaceScene] island decor: 14 prop(s) placed on Team A, cloned to Team B.`, `field dressing: 2 island(s).`
`contact shadows under N station(s).` (N = 6 / 8 / 12 ตามโหมด), `river trimmed flush with the south cliff at z -4.50 / -5.63 / -6.75: 500 vertex/vertices moved.`
และ `clearance: 34 / 36 / 40 prop(s) checked, 0 decor and 0 station overlap(s)` (solo/duo/team)
ภาพ `-wide.png` / `-preview.png` ต้องเห็น: สะพานไม้จริงพาดสองฝั่งทั้งเหนือและใต้, **ประตูแดง-ครีม 2 บานกลางสะพาน (เปิดทุกโหมด)**,
พุ่มดอก/โต๊ะคาเฟ่/ลังส้ม/ไฟถนนอยู่ตำแหน่งเดียวกันทั้งสองเกาะ, ของทุกชิ้นอยู่บนหญ้าไม่ทับ deck, แม่น้ำจบที่ขอบผาหน้า และไม่มีวงสีม่วงอ่อนใต้สถานี (AO ถูกจำกัด 1.6–2.6 u)
ผาหัวน้ำตกต้องเป็นหินสีเทา-น้ำเงินเข้ม (palette `slate (2,2)` / `dark slate (3,2)`, shade 0.68–0.84) ไม่ใช่แผ่นครีมใหญ่
ตลิ่งสองฝั่งช่องน้ำ (`Bank_West/East`) ใช้สีดิน `dirt (14,1)` — ก่อนหน้านี้ใช้ `stone (10,2)` แล้วเห็นเป็นคานคอนกรีตสีครีมยาวพาดผ่านสะพาน

**HUD (ข้อ 10)** — verify log ต้องมี `[TaskRaceVerify] HUD: N task row(s), canvas ScreenSpaceCamera` (N = 6 / 7 / 9 ตามโหมด)
และได้ภาพ `Logs/taskrace{-duo,-team}-hud.png` ที่เห็น:
การ์ดใบงานมุมซ้ายบน ชื่อ `ใบงาน · ทีมเรา 0/N`, แถว `T04 จัดรถเข็นตามสูตร ×2` มาก่อนเสมอ, แถวที่สถานียังไม่หยิบมาขึ้น `รอคิว`, แถบน้ำหนัก 0%;
**แถบบนกลาง** `เวลา 02:00` + `Team Points: 0` + บรรทัดโหมด; **ดีบักบาร์มุมขวาบน 8 ปุ่ม** (โหมด/เวลา/สะพาน/แต้ม/ป่วน/เกราะ/รีเซ็ต/ซ่อน)
ถ้า N = 0 หรือการ์ดว่าง: `TaskRaceRound` ไม่ได้เริ่มรอบ (autoStart ปิด หรือ StartRound ล้มเหลว) หรือ `hud.Bind(round)` ไม่ได้ถูกเรียกตอน build ซีน
ถ้าการ์ดทับกัน/สูงผิด: layout ในเอดิเตอร์ยังไม่ rebuild — `Build()` เรียก `Canvas.ForceUpdateCanvases` + `LayoutRebuilder` ให้แล้ว; ถ้าแก้ layout เองให้เรียกซ้ำ

เช็กลิสต์ในเกม:

- เปลี่ยน `curvature` 0 → 0.018 → 0.026 แล้วพื้น/กล้อง/ป้าย/วงแหวนโค้งพร้อมกันโดยไม่มี collider เพี้ยน
- เดินเข้า 2.6 u → ป้ายโผล่; เข้า 1.2 u → วงแหวนไฮไลต์และปุ่มหลักเปลี่ยนชื่อ
- เปิดการ์ด → กล้องซูม 0.85 และเล็งเยื้องขวา 1.2; กด × หรือดันจอย > 70% → การ์ดปิดและ progress คงอยู่ 10 วิ
- PureRace: เดินชนตลิ่งไม่ได้, ประตูกลางสะพานปิดตลอด; TimedBridgeRaid: 60 วิแรกผ่านประตูไม่ได้, หลัง 60 วิประตูทรุดแล้วข้ามไปแตะจุดฝั่งตรงข้ามได้ (เจอ TASK_LOCK toast ถ้าคนอื่นถืออยู่)
- ทำจุดงานจน Clean → `Team Points` ขึ้น +100; พอครบ 150 แตะจุดเดิมอีกครั้งเพื่อซื้อเกราะ (toast + วงทอง 30 วิ)
- หลังสะพานเปิด เดินไปจุดของคู่แข่งที่ Clean → ปุ่มเปลี่ยนเป็น `ป่วนจุดงาน: <ชื่อ>`; การ์ดวิ่ง 2.75 วิ, จอย > 70% ยกเลิก; สำเร็จแล้วจุดกลับ Dirty และงานเดิมถูกใส่คืน
- ดีบักบาร์: กด `เวลา -10` สองครั้งรัว ๆ ให้เวลาต่ำกว่า 65 → 60 แล้วดูประตูเขย่าและทรุด; `รีเซ็ต` คืนเวลา 120 วิ/แต้ม 0/ประตูปิด
- Team B ทุกจุดอยู่ตำแหน่งเดียวกับ Team A เมื่อเทียบกับกรอบจอ (ไม่ mirror)

## 13. ขนาดแผนที่ → โหมด (solo / duo / team)

กติกาเหมือนกันทุกโหมด ต่างแค่ **ขนาดเกาะ** กับ **จำนวนสถานี** — `TaskRaceSceneBuilder` มี `BuildVariant(scenePath, islandSize, islandGap, stations)` ตัวเดียว
ตำแหน่งสถานีเขียนไว้บนเกาะ 12 u แล้วคูณ `islandSize / 12` จึงคงสัดส่วนเดิมทุกขนาด

| | Solo | Duo (1v1) | Team (2v2 / 3v3) |
| --- | --- | --- | --- |
| คำสั่ง build | `TaskRaceSceneBuilder.Build` | `BuildDuo` | `BuildTeam` |
| ซีน | `Assets/Scenes/TaskRace.unity` | `TaskRace_Duo.unity` | `TaskRace_Team.unity` |
| เกาะกว้าง (u) | 12 | 15 | 18 |
| ระยะศูนย์กลางเกาะ (u) | 16 | 20 | 24 |
| ช่องน้ำ (u) | 4 | 5 | 6 |
| สนาม `Field` (เต็ม) | 9.6 × 7.2 | 12 × 9 | 14.4 × 10.8 |
| พื้นที่สนาม (u²) | ~54 | ~85 | ~122 |
| สถานีต่อเกาะ | 3 | 4 | 6 |
| จุดงานทั้งแผนที่ | 6 | 8 | 12 |
| วิ่งข้ามสนามตาม X (3.2 u/s) | 3.0 วิ | 3.75 วิ | 4.5 วิ |
| สถานีตรงข้ามฝั่ง (ข้ามสะพาน) | 5.0 วิ | 6.25 วิ | 7.5 วิ |
| ความยาวสะพาน (u) | 12.05 | 14.46 | 16.88 |
| ฐาน NavMesh ที่ bake (PureRace → TimedBridgeRaid) | 24 → 52 tri | 28 → 60 tri | 28 → 64 tri |

- `islandGap` ต้อง ≥ `islandSize + 3` (assembler บังคับผ่าน `SetLayout`) เพื่อให้มีช่องน้ำพอสำหรับตลิ่ง (PureRace) และสะพาน (TimedBridgeRaid)
- ความกว้างสะพานคงที่ 2 u ทุกโหมด — ยาวขึ้นแต่ไม่หนาขึ้น ยังอ่านเป็นสะพานไม้เดินแคบ; ความยาวคิดจากขอบวงรีของ `Field` (ข้อ 5) ไม่ใช่แค่ความกว้างช่องน้ำ จึงยาวกว่าเส้นผ่านศูนย์กลางช่องราว 3 เท่าและจมเข้าหาเกาะข้างละ 1.2 u
- กล้อง fov 40 สูง 8.5 ถอย 7.5 เห็นแนวกว้าง ~14.7 u ที่ตัวฮีโร่: Solo เห็นเกาะทั้งใบ, Duo เกือบทั้งใบ, Team เห็นราว 4 ใน 5 ของความกว้าง — Team จึงต้องพึ่งการเดิน/สะพานมากที่สุด ซึ่งตรงกับกติกา 2v2/3v3 ที่ต้องแยกกันทำงาน
- ทางเลือกประหยัด: ใช้ 15/20 ทั้ง Duo และ Team แล้วเพิ่มสถานีเป็น 6 จุด (เฟรมกล้องเท่ากันทุกโหมด แต่จุดงานจะแน่นขึ้นเป็น ~3.5 u ระหว่างเคาน์เตอร์)
- ทุกซีนถูกเพิ่มเข้า Build Settings อัตโนมัติ (ต่อจาก `TaskRace.unity`) — index 0 ยังเป็นซีนเกมเดิม ไม่เปลี่ยน
- **จุดเกิดฮีโร่ = จุดในครึ่งใต้ของสนามที่ห่างสถานีมากที่สุด** (`ChooseSpawn`, สแกนกริด 0.5 u) เพราะจุดเดิม `z = -0.2 × islandSize` ไปทับสถานี S6 ของโหมด team (ห่างแค่ 0.75 u ทำให้เริ่มเกมในวง NEAR) — log รายงาน `hero spawn (x,z), nearest station d=...`
- ตรวจทั้งสามซีน: `TaskRaceVerify.Run` / `RunDuo` / `RunTeam` → `Logs/taskrace{,-duo,-team}-{play,preview,wide,hud}.png`

## 14. ปริมาณงานต่อโหมด + กลไกการสุ่มมินิเกม

`TaskRaceRound` (บน `TaskRaceMap/Systems`) เป็นผู้อำนวยการรอบ: มันสร้าง **รายการงานของทีม** ตาม doc 16 ข้อ 2.2
จากคลังมินิเกม 15 ตัว แล้วแจกให้สถานีที่สร้างไว้ — สถานีจึงไม่ต้องมีจำนวนเท่างาน

### 14.1 รายการงานและปริมาณงานที่วัดได้

รายการมาจาก seed เดียวกันทั้งสองเกาะ รถเข็น **T04 มาก่อนเสมอ** ตัวอื่นสลับด้วย Fisher–Yates (`System.Random`)

| โหมด | `players` | งานในรอบ | long / common / short | น้ำหนัก | ตัวอย่างรายการ (seed 1975) | งานรวม D1 / D2 / D3 |
| --- | --- | --- | --- | --- | --- | --- |
| Solo | 1 | 6 | 2 / 0 / 4 | 9 | T04 T14 T02 T08 T15 T06 | 63 / 71 / 79 วิ |
| Duo | 2 | 7 | 2 / 1 / 4 | 13 | T04 T14 T13 T03 T15 T10 T08 | 77 / 86 / 96 วิ |
| Team | 3 | 9 | 2 / 1 / 6 | 15 | T04 T14 T13 T03 T15 T10 T08 T02 T06 | 91 / 95 / 98 วิ |

- **งานรวม = เวลาการ์ด + เวลาเดิน** โดยเวลาการ์ดคือ `เวลาเป้าของมินิเกม × (1 + 0.15 × (D − 1))` และเวลาเดินคือระยะสถานีเฉลี่ยต่องาน ÷ 3.2 u/วิ (ระยะที่วัดจากซีนจริง: solo 4.86 u, duo 5.85 u, team 6.76 u)
- Team อยู่ที่ **91–98 วิ** ซึ่งคือกรอบเป้า **90–100 วิ จากรอบ 120 วิ** ของ doc 16 ข้อ 2.2 พอดี ส่วน solo (63–79) และ duo (77–96) สั้นกว่าตามขนาดทีม จึงเหลือที่ให้พลาด/คราบฉุกเฉินมากกว่า
- ทีม 3–4 คนที่ D2/D3 ลด short ลงขั้นละ 1 ตัว (`CompositionFor(players, difficulty, …)`) เพราะตัวคูณเวลา 15%/ขั้นจะดันรายการ 9 ตัวไปเกินเป้า (ถ้าไม่ลด D3 จะราว 113 วิ); ที่ D2/D3 รายการจึงเหลือ 8 / 7 งาน (น้ำหนัก 14 / 13)
- `players = 4` (4v4) ใช้สูตร 11 งาน (1 / 3 / 7) — ยังไม่มีซีนที่ตั้งค่านี้ แต่ตั้งได้ใน Inspector

### 14.2 คลังมินิเกม

`MinigameLibrary.All` มี 15 รายการ `(Id, Name, Category, Span, Weight, Seconds)`: T01 กวาดตามทาง, T02 ถูคราบวน, T03 ฉีดให้ตรงจุด, T04 จัดรถเข็นตามสูตร, T05 แยกขยะ, T06 บิดผ้าถู, T07 ล้างจานกองสูง, T08 เช็ดกระจก, T09 แช่คราบรอ, T10 ไล่ฝุ่นใต้โต๊ะ, T11 เติมน้ำยาตามระดับ, T12 จัดโต๊ะเก้าอี้, T13 ยกของหนักสองคน, T14 ล้างพื้นทั้งแถว, T15 จำลำดับเครื่องซักผ้า
**T16 คราบฉุกเฉินไม่อยู่ในคลัง** — มันเกิดกลางรอบตาม doc 16 ข้อ 4 ไม่ใช่รายการที่วางแผนไว้

การคัดตัว: เอาเฉพาะมินิเกมที่ **มีหมวดตรงกับสถานีบนแผนที่** (หรือ Everyone) — แผนที่ที่มีแต่ Wash/Scrub/Sweep/Spray/Everyone จึงใช้ได้ทั้ง 15 ตัว; ไม่ซ้ำในรอบเดียว; ถ้าหมวดที่ต้องการหมดจะเติมจากหมวดอื่นเพื่อให้ครบจำนวน (ยกเว้นกรณีสุดท้ายจริงๆ)

### 14.3 การแจกงานให้สถานี + การเติมงาน

- แต่ละสถานีหยิบงานแรกในรายการที่ **หมวดตรงกับตัวเอง** ก่อน → ถ้าไม่มี หยิบงาน Everyone → ถ้ายังไม่มีหยิบตัวแรกที่เหลือ
- ทำเสร็จแล้วสถานี **เติมงานถัดไปของฝั่งตัวเองทันที** (`Assigned → ResetTask → Configure → SetTaskActive(true)`) จึงใช้ 3 สถานีรับ 6 งาน (solo) และ 6 สถานีรับ 9 งาน (team) ได้
- งานในรายการหมด → `SetTaskActive(false)`: วงแหวน/ป้ายหาย กดเปิดไม่ได้ (`TryOpen` คืน `false` + ข้อความ `จุดนี้ไม่มีงานแล้ว`)
- ทั้งสองเกาะได้รายการเดียวกันและเติมพร้อมกัน → การแข่งยังยุติธรรม; `DoneCount` / `DoneWeight` ของฝั่ง A คือตัวเลขบน HUD (`ใบงาน · ทีมเรา N/M`) และแถบน้ำหนัก (`BobTaskRaceHud`, ข้อ 10)
- ครบทุกงาน → `RoundWon` (log `won: 9/9 tasks, weight 15/15`)

### 14.4 ค่าใน Inspector

| ช่อง | ค่าเริ่มต้น | ความหมาย |
| --- | --- | --- |
| `players` | 1 / 2 / 3 ตามซีน | ขนาดทีม → จำนวนงาน (ข้อ 14.1) |
| `difficulty` | 1 | D1–D3: ตัวคูณเวลา 15%/ขั้น + ลด short ของทีมใหญ่ |
| `seed` | 1975 | seed ที่ฝังในซีน (ซีนถูก bake รายการนี้ไว้ให้ดูในเอดิเตอร์) |
| `randomSeedEachPlay` | เปิด | ตอนเล่นจริงสุ่ม seed ใหม่ทุกรอบ (ปิดเพื่อทดสอบซ้ำได้ผลเดิม) |
| `autoStart` | เปิด | เริ่มรอบเองใน `Start()` |

- ContextMenu **Round: reroll seed and restart** สุ่ม seed ใหม่และแจกงานใหม่โดยไม่ต้องเข้า Play
- **ตั้งแต่ 15 พ.ย. 2026:** `difficulty` และนาฬิกาของรอบมาจากบทที่เลือกใน S13 (`ChapterDefinition`) ผ่าน `TaskRaceModeManager.Awake` — ค่าที่ฝังในซีนเป็น fallback เมื่อไม่มี asset บท; `players`, `seed` ยังมาจากซีนเหมือนเดิม
- log ตอนเริ่มรอบ: `[TaskRaceRound] seed … · players … · difficulty … · N tasks (long/common/short) · weight … · cards … · workload …` ตามด้วยงานของทุกสถานีฝั่ง A
- ยังไม่ทำ: คราบฉุกเฉิน T16, server seed contract (`TASK_BEGIN`), AI คู่แข่ง — HUD รายการงาน/แถบน้ำหนักทำแล้ว (ข้อ 10)

## 15. ด่านใน GDD เทียบกับด่านที่สร้างแล้ว (2026-11-14)

ข้อนี้เทียบ **ด่านที่ GDD วางไว้** (บทที่ 1–6, ระดับผู้เล่น 1–50) กับ **สิ่งที่ซีน TaskRace ทำได้จริงตอนนี้** แล้วเสนอทางปรับที่ยังใช้แผนที่เกาะคู่เดิมได้ — ไม่ใช่การรื้อระบบ แต่เป็นการเติมชั้น "บท/ฉาก" ให้รอบที่เล่นได้อยู่แล้ว

### 15.1 ด่านตาม GDD (ข้อ 11 และข้อ 20)

| บท | ระดับ | ฉาก | กลไกที่เพิ่ม |
| --- | --- | --- | --- |
| 1. ห้องเช่า | 1–5 | ห้องเดียวขนาดเล็ก ของทุกอย่างอยู่ใกล้ | 1 เงื่อนไข เวลาเหลือเฟือ เรียนรู้เดิน/หยิบ/ลาก/วาง |
| 2. รับงานเพื่อนบ้าน | 6–12 | คอนโด 2 ห้อง ติดประตูแคบ | 2 เงื่อนไข + แหล่งแรง (น้ำร้อน/ไอ) + งบจำกัด |
| 3. ร้านค้าในซอย | 13–20 | ร้านกาแฟ 3 เคาน์เตอร์ / ร้านตัดผม / ร้านอาหาร | วิธีใช้ (แช่ ฉีดพ่น ถูวน) ผิวบอบบาง ปฏิกิริยาหักล้าง |
| 4. บ้านเก่ามีเรื่องเล่า | 21–30 | บ้านไม้ 2 ชั้น มีบันได ตู้น้ำยาชั้นบน | 3 เงื่อนไข คราบซ้อนหลายชนิด ถนอมของเดิม งบยกข้ามรอบ |
| 5. งานใหญ่ของย่าน | 31–40 | โรงเรียนทางเดินยาว / ศาลาชุมชน 4 มุม | เวลาสั้นลง ชิ้นส่วนสุ่มและไม่บอกค่า |
| 6. ฟื้นตลาดเก่า | 41–50 | ตลาดหลายโซนเชื่อมกัน | การ์ดกติกาพิเศษ รวมทุกกลไก + โหมด Community |

กฎกลางของฉาก (GDD ข้อ 20): สองทีมได้ฉากผังเดียวกันคนละชุด · ไปกลับไกลสุด ≤ 6 วิ · เล่นจบได้คนเดียว · สิ่งกีดขวาง (พื้นเปียก ประตูแคบ เคาน์เตอร์) เสียเวลาเท่านั้น ไม่ทำให้แพ้ทันที · ตัวละครเดินทะลุกันได้

> **เนื้อเรื่องของบันไดนี้ (ร่าง 16 พ.ย. 2026, รอ owner อนุมัติ):** เหตุผลในโลกของเรื่องว่าทำไมแต่ละบทมีกติกานี้ อยู่ใน [`29_Story_Premise.md`](29_Story_Premise.md) — คำขวัญ "งานสะอาดต้องทำสามรอบ" อธิบาย best-of-3, ดาว 1–3 คือคะแนนของผู้ว่าจ้าง และการปลดล็อกด้วย LV+ดาวคือใบรับรองของย่าน (ตัวเลข LV ในตารางข้างบนเป็นค่าที่ใช้ตอนร่าง; ค่าจริงตอนนี้ 1–6 ตาม §15.20)
แมตช์ตาม GDD = 3 รอบ รอบละ 100 วิ ชนะ 2 ใน 3 รถเข็นร่วมกันคันเดียว ถือของ 2 ชิ้น (solo 3) ชั้นบนสุดหยิบได้

### 15.2 สิ่งที่ซีนทำได้ตอนนี้

- **แผนที่เดียว:** เกาะคู่ دیорама 12 / 15 / 18 u, 3 / 4 / 6 สถานีต่อฝั่ง ไม่มีห้องหรือผนัง — ฉากคือลานเกาะ ไม่ใช่ห้องในเรื่อง
- **งาน = การ์ด S28:** มินิเกม T01–T15 ทำที่สถานี (T04 รถเข็น และ T12 เก้าอี้ ยังเป็นการ์ดแบบจับเวลา) ไม่มีการเดินถือของขึ้นรถเข็น
- **รอบเดียว 120 วิ** ต่อหนึ่งแมตช์; โหมด `PureRace` และ `TimedBridgeRaid`
- **ระดับความยาก D1–D3** = เวลาการ์ด ×1.15 ต่อขั้น เท่านั้น ยังไม่มีจำนวนเงื่อนไข ผิวบอบบาง ชิ้นส่วนสุ่ม หรือการ์ดกติกา
- **กฎ GDD ที่ทำครบแล้ว:** สองเกาะผังเหมือนกัน (clone + seed เดียว) · สถานีไกลสุดเดิน ~1.5–2.1 วิ (ระยะเฉลี่ย 4.86 / 5.85 / 6.76 u ÷ 3.2 u/วิ) ซึ่ง ≤ 6 วิ · solo จบได้ทุกงาน · ไม่มีอุปสรรคที่ทำให้แพ้

### 15.3 ช่องว่างและลำดับที่ควรปรับ

| ลำดับ | สิ่งที่ GDD มี | สิ่งที่ขาด | วิธีเติมบนระบบเดิม |
| --- | --- | --- | --- |
| P0 | บท 1–6 + ระดับ 1–50 ปลดล็อก | ไม่มีชั้นบท/ด่าน | `ChapterDefinition` (ScriptableObject): ชื่อบท ระดับปลดล็อก ฉาก ค่า D เริ่มต้น ชุดการ์ดกติกา → หน้าเลือกด่าน (S13) ส่งเข้า `TaskRaceRound` ตัวเดิม — **ทำแล้ว 15 พ.ย. 2026**: `Rules/ChapterDefinition.cs` + `Rules/BobChapters.cs` + `Resources/Chapters/Chapter1_RentalRoom … Chapter6_Market.asset`, S13 = การ์ด 6 ใบ (`Game/BobGame.Menus.cs`), ค่าที่ส่งเข้าเกมคือ `difficulty` + `roundTime` ผ่าน `TaskRaceModeManager.Awake`; การ์ดกติกายังแสดงอย่างเดียว (บังคับใช้ = P2), แผนที่หมุด/ดาว และ skin ฉาก = P1 |
| P1 | ฉาก 7 แบบ (ห้องเช่า คอนโด ร้านกาแฟ ครัว บ้านไม้ โรงเรียน ศาลา ตลาด) | ฉากเดียว (เกาะ) | "skin ชั้น" บนเกาะ: วัสดุพื้น + วงผนัง/ฉากกั้น + prop ประจำบท 6–8 ชิ้น (เคาน์เตอร์ร้านกาแฟ พื้นเปียกครัว บันไดบ้านไม้ ม้านั่งทางเดินโรงเรียน) ไม่แตะ gameplay |
| P2 | เงื่อนไข 1 → 2 → 3, ผิวบอบบาง, ชิ้นส่วนสุ่ม, การ์ดกติกา | D1–D3 ขยับแค่เวลา | ต่อยอดใน `BobMinigameHost`: คราบผิวบอบบาง (ถูเร็วเกิน = เริ่มคราบใหม่), คอมโบ 2 ขั้นที่มีอยู่แล้ว (T08/T09), ชิ้นส่วนซ่อน (T10) และการ์ดกติกาปิดหมวด/เพิ่มเงื่อนไข |
| P3 | 3 รอบ ×100 วิ ชนะ 2 ใน 3 + แพ้ติด 2 ให้เวลา +15 วิ + ชนะขาด 3 เสนอด่านท้าทาย | รอบเดียว 120 วิ | `StorySeries` ครอบรอบ: 3 รอบ รอบละ 100 วิ best-of-3 เฉพาะโหมดเนื้อเรื่อง (PvP คง 120 วิ) |
| P4 | รถเข็น: เดิน–หยิบ–ถือ 2 ชิ้น–ซ้อนชั้น–ส่งต่อ | ไม่มี | โหมดแยก "Cart Rush" หรือภารกิจที่ 2 ของบท 2 — ต้องมีฉากห้องจริงก่อน จึงควรทำหลัง P1 |
| P0 (สถานะ) | 6 บทใน `Resources/Chapters` | — | ด่าน 1–6 ปลดที่ LV 1 / 2 / 3 / 4 / 5 / 6 **และ** ต้องมีดาวบทก่อนหน้า ≥ 2 ดวง (`BobChapters.StarsToOpen`), เพดาน LV 50 (`MaxLevel`); ช่วงระดับในตาราง §15.1 คือเป้าตาม GDD ส่วนค่าที่บังคับใช้จริงคือ `unlockLevel` ใน asset · D ต่อบท = band เดิม (1-2 → D1, 3-4 → D2, 5-6 → D3) · นาฬิกาทุกบท 120 วิ (โหมดเนื้อเรื่อง P3 ใช้ 100 วิต่อรอบ) · ภาพการ์ดใช้ชุด `lobby_*` ที่มีจริง (บท 5–6 ใช้ภาพซ้ำจนกว่า P1 จะทำ skin) |

**ข้อสรุป:** ระบบที่ขาดไม่ใช่ตัวมินิเกม แต่เป็น "ชั้นบท" (chapter data + stage select + scene skin) ที่ห่อรอบเดิมอยู่ — ถ้าทำ P0+P1 ก่อน จะได้ภาพรวมเกม 6 บทตาม GDD โดยไม่ต้องแก้กฎการแข่ง และเก็บ P2–P4 ไว้เป็นกลไกของบทนั้น ๆ

**ลำดับที่ owner เคาะ 15 พ.ย. 2026** (เรียงตามความสำคัญ; ทุกเฟสมีเกณฑ์จบของตัวเอง):

| เฟส | งาน | เกณฑ์จบ |
| --- | --- | --- |
| 0 | ปิดแผ่นม่วงบนเกาะในโหมดเนื้อเรื่อง (ข้อ 15.5 "ค้าง (ภาพ)") — probe แบบ pixel bisect ด้วย RenderTexture; ถ้า 2 รอบไม่เจอ ใช้ทางลัดบังคับ island mesh variant เดียวทั้งสามโหมดแล้ว rebuild | `series-1-start.png` ไม่มีม่วง, duo/team ไม่เปลี่ยน, `SERIES CHECK PASSED` |
| 1 | P3 ที่เหลือ (ข้อ 15.5) + การ์ด S13 ต้องอ่าน `100 วิ × 3` ไม่ใช่ `roundTime` 120 | การ์ดตรงกฎจริง, ดาว 0–3, profile มีผลรายรอบ, แพ้ติดนับต่อบท |
| 2 | P1 skin 6 บท: material tint พื้น + prop 6–8 ชิ้น/บท จาก `Assets/Resources/Props` ผ่าน `BobFieldDressing`; ภาพการ์ด S13 ถ่ายจาก skin ใหม่ (ไม่ต้อง gen) | แต่ละบทเห็นความต่างชัด, ไม่แตะ spot/gameplay, trio verify ผ่าน |
| 3 | P2 **บังคับใช้การ์ดกติกาครบทั้ง 8 ใบ** (`ruleCards` → `TaskRaceRound`/`BobMinigameHost`): เงื่อนไข 1–3, แหล่งแรง, งบจำกัด, วิธีเฉพาะทาง, ผิวบอบบาง, คราบซ้อน, ถนอมของเดิม, เวลาสั้นลง, ชิ้นส่วนสุ่ม, การ์ดพิเศษ | การ์ดบน S13 ตรงกับสิ่งที่เกมบังคับจริงทุกใบ |
| 4 | P4 Cart Rush เป็น**ภารกิจที่ 2 ของบท 2** (ไม่เพิ่มเมนู Home) | เล่นครบ loop เดิน–หยิบ 2 ชิ้น–ซ้อน–ส่งต่อ, มี HUD/ผล |
| 5 | `BobMinigameSfx` ผูก 8 mini kind กับ WAV 38 ไฟล์ที่มี + T16 emergency stain + rival bar (#47) ขับด้วย seed ท้องถิ่นใน solo | ทุก kind มีเสียง tick/สำเร็จ/พลาด → ✅ 16 พ.ย. 2026 (ข้อ 15.18); rival bar ✅ ทำแล้ว; **T16 ยังไม่ทำ** |
| 6 | ต่อ online PvP บน pure UGS (`UgsRelayLobbyManager` + Netcode ตาม `27`) ส่ง `difficulty`/seed ต่อบท, ทดสอบ 2 client — **หลัง APK v1**. ของเดิม (`BobOnline` / `BobServer` + Nakama client) ถูกลบออกจาก Unity 16 พ.ย. 2026 เหลือสัญญา opcode ฝั่ง `server/` เท่านั้น | PvP ข้ามเครื่องเล่นได้ |
| 7 | APK + Firebase App Distribution **หลังเฟส 0–1**, sync docs, ตอบ #44; S28 3D stage (#46) เลื่อนไปหลัง APK | APK บนเครื่องจริงผ่าน, เอกสารตรงโค้ด |

### 15.4 P0 ที่ทำแล้ว (15 พ.ย. 2026)

> หลัง merge `c769fa8` (15 พ.ย. 2026) เหลือ implementation เดียว: ชุดของ session ขนาน (`Rules/…`). ชุดแรกของฝั่งเรา (`Game/ChapterDefinition.cs` + `Chapters` loader + `BobChapterBuilder` + `ch0*.asset`) ถูกถอดออกทั้งชุดเพื่อไม่ให้มีสองระบบอ่าน `bob.chapter` คนละแบบ

- **`ChapterDefinition`** (ScriptableObject, `Rules/ChapterDefinition.cs`): `order`, `title`, `place`, `unlockLevel`, `difficulty` D1–D3, `roundTime`, `backdrop`, `blurb`, `ruleCards` — 6 asset อยู่ใน `Assets/Resources/Chapters/Chapter1_RentalRoom … Chapter6_Market` ตามตาราง §15.1: **LV 1 / 2 / 3 / 4 / 5 / 6** (ปรับ 16 พ.ย. 2026 จาก 1/6/13/21/31/41 ที่ทำให้ต้องเคลียร์ 78 รอบ ≈ 7.4 ชม. กว่าจะถึงบท 6), band D1 (บท 1–2) / D2 (3–4) / D3 (5–6), นาฬิกา 120 วิ (ยกเว้นโหมดเนื้อเรื่องที่ P3 ใช้ 100 วิต่อรอบ); เมนูตรวจบันได **Bob > Chapters > Check Ladder** (`Editor/BobChapterCheck.cs`)
- **`BobChapters`** (static loader): โหลดจาก `Resources/Chapters` เรียงตาม `order`, เก็บบทที่เลือกใน PlayerPrefs `bob.chapter` และคุม `bob.band` ให้คะแนนดีสุดเดิมตามบท, `Unlocked(chapter, level)` = **LV ถึง `unlockLevel` และดาวบทก่อนหน้า ≥ `StarsToOpen` (2)**, `Selected(level)` / `LastUnlocked` / `Validate()`; `XpPerLevel = 200`, `MaxLevel = 50` (`LevelFromXp` clamp), `Cleared(order)` / `MarkCleared(order)` สำหรับโบนัสเคลียร์ครั้งแรก (`bob.story.cleared.<order>`)
- **S13 เลือกบท** (`BobGame.Menus.cs > ChapterSelect`, `Phase.Chapters`): การ์ด 6 ใบ 3×2 (ภาพฉาก + หมายเลขบท + ชื่อ + สถานที่ + `LV/D` + ดีสุด + การ์ดกติกา) ล็อกจนถึงระดับปลดล็อกพร้อมม่านมืด, ปุ่ม `เริ่มด่านนี้` → `PressPlay` โหลดซีน `TaskRace`; ปุ่ม back / Android back กลับ Home
- **Home:** ชิป Task Race เดิมเป็นการ์ดบท (ภาพ + `บท N` + ชื่อ + `D · เวลา · ดีสุด`) แตะเพื่อเข้า S13 — ปุ่ม `เล่น` ยังเริ่มบทที่เลือก
- **`TaskRaceModeManager.ApplyChapter()`** ใน `Awake`: ตั้ง `matchDuration = chapter.RoundTime` และ `ConfigureRound(players, chapter.Difficulty, seed)` **ก่อน** `TaskRaceRound.Start` วาดรายการ; ตัว `TaskRaceRound` ไม่ได้อ่านบทเองแล้ว (สูตรสุ่มยังอยู่ที่ `BobTaskRaceRules`)
- **ยังไม่ทำ (P1/P2):** skin ชั้นบนเกาะ, การบังคับใช้ `ruleCards`, ดาว 0–3 และแผนที่หมุดแบบ S13 เต็มรูปแบบ — หน้าเลือกบทตอนนี้ใช้การ์ดภาพล็อบบี้แทน

### 15.5 P3 ที่ทำแล้ว (15 พ.ย. 2026)

- **`StorySeries`** (`TaskRace/StorySeries.cs`, ตรรกะล้วน ไม่ผูก MonoBehaviour เพื่อให้ editor check เล่นครบซีรีส์ได้โดยไม่ต้องเข้า Play): `Rounds = 3`, `WinsToTake = 2`, `SecondsForRound`; `Register(localTasks, rivalTasks, localPoints, rivalPoints)` ปิดรอบ — เทียบงานก่อน แต้มใช้ตัดสินเฉพาะงานเท่ากัน, เท่ากันทั้งคู่ = เสมอ; `Over` จบเมื่อฝ่ายใดฝ่ายหนึ่งชนะครบ 2 หรือเล่นครบรอบ 3
  - เสมอไม่นับให้ใคร: 0-0 สองรอบยังเล่นรอบสามต่อ (ทดสอบใน `StorySeriesCheck`)
  - **บั๊กที่แก้ตอนตรวจ:** เดิม `Over` ใช้ `Round >= Rounds` แต่ `Round` ถูกเลื่อนเป็นรอบถัดไปแล้วหลังปิดรอบ → ซีรีส์ 0-0 จบที่รอบ 2 ตอนนี้ `Over` วัดจาก `LastRound.Value.Number`
  - `ChallengeOffered`: ชนะรอบด้วยส่วนต่าง ≥ `MarginForChallenge` (3) งาน
- **`TaskRaceModeManager`** — เฉพาะ `storySeries = true` (PvP คงรอบเดียว `matchDuration` 120 วิ ไม่แตะ): `BeginMatch` สร้าง `StorySeries(storyRoundSeconds, seriesWinsToTake)`, เริ่มรอบแรกด้วย `StorySecondsForRound`; จบรอบ → `FinishRound()` แจกแต้ม/ประกาศ `RoundEnded` ตามเดิม แล้วรอ `nextRoundDelay` (3 วิ) → `BeginRound()`; ครบซีรีส์ → `EndMatch(seriesWon, seriesDraw)` และ HUD overlay ทำงานเหมือน PvP
  - ค่า Inspector ใหม่: `storySeries`, `seriesWinsToTake` 2, `storyRoundSeconds` 100, `loseStreakBonusSeconds` 15, `nextRoundDelay` 3 (`OnValidate` clamp)
  - **mercy แพ้ติด 2:** `StoryLossStreak` (PlayerPrefs `bob.story.lossStreak`) นับบทที่แพ้ติด; ครบ 2 บท รอบแรกของบทถัดไปได้ +15 วิ พร้อม toast; ชนะซีรีส์แล้วรีเซ็ต — best-of-3 จบที่ 2 ชนะ จึงไม่มีทางแพ้ติด 2 รอบภายในซีรีส์เดียว โบนัสจึงลงที่รอบแรกของบทถัดไป
  - `DebugFinishRound()` จบรอบด้วยคะแนนจริงของรอบนั้น (ใช้โดย check)
- **`BobSeriesBanner`** (`TaskRace/BobSeriesBanner.cs`) — canvas แยก `sortingOrder 40` ไม่แตะ layout ของ `BobTaskRaceHud`: การ์ดกลางจอ 760×380 ตอนปิดรอบ (หัวข้อสีตามผล, ป้าย `ด่านท้าทายปลดแล้ว!` เมื่อ `ChallengeOffered`, `เรา X - Y คู่แข่ง`, นับถอยหลังรอบถัดไป) + แถบล่างกลางจอ `รอบ N/3 · เรา X - Y คู่แข่ง` ตลอดซีรีส์; ผูก `MatchStarted` / `RoundEnded` / `MatchEnded`
- **ตรวจ:** เมนู **Bob > Run Story Series Check** (`Editor/StorySeriesCheck.cs`) — โหมดแก้ไขตรวจกฎรวมเส้นทางเสมอ, เข้า Play แล้วจบ 3 รอบจริงด้วย `DebugFinishRound` นับรอบ 1→2→3, ถ่าย `Logs/series-1-start … 5-result.png` (มีชุด toggle Field / Soil / Dressing / CloudSea / fog สำหรับไล่ปัญหาเรื่องภาพ) ปิดท้าย `[SeriesCheck] SERIES CHECK PASSED`; ผลรันล่าสุด: rules PASSED, chapter end last round 3, overlay ผลเปิด
- **ยังไม่ทำ:** หน้าจอด่านท้าทายปลายทาง (ตอนนี้แค่ป้ายแจ้ง), ดาว 0–3 ต่อบท, การบันทึกผลรายรอบลง `BobTaskRaceProfile` (ยังบันทึกเป็นแมตช์เดียว), สถิติแพ้ติดต่อบท (ตอนนี้นับรวมทุกบท), ข้อความบนการ์ด S13 ที่ยังพิมพ์ 120 วิ (ที่จริงโหมดเนื้อเรื่องใช้ 100 วิ × 3 รอบ — `ChapterDefinition.roundTime` มีผลกับ PvP เท่านั้น)
- **แก้แล้ว 15 พ.ย. 2026 — แผ่นม่วงบนเกาะ (ProbeSlab ใน `StorySeriesCheck`)**: ในซีน solo ระหว่างเล่นซีรีส์ พื้นเกือบทั้งเกาะเคยถูกวาดเป็นแผ่นม่วง `#A600A6` (กล้องเล่น) แต่ preview/wide และ duo/team ปกติ. วิธีหาเจ้าของ: render กล้องเล่นลง RenderTexture 64×64 แล้วนับพิกเซลม่วง ปิด renderer ทีละตัว — ผลคือ **`TaskRaceMap/TeamA/Island/Outline` เพียงตัวเดียว** (45.4% → 0.0%). สาเหตุ: prefab ทุกตัวใน `Resources/Props` ถูก import ตอนที่ `Resources/Materials/M_BOB_Outline` ยังไม่มี child `Outline` (inverted hull จาก `BobToon.Outline`) จึงมี `m_Materials: - {fileID: 0}` มาตั้งแต่ commit `5f93e71` — ใน Scene view ไม่ถูกวาด แต่พอเข้า Play mode static batching รวมเป็น `Combined Mesh (root: scene)` แล้ว submesh ที่ไม่มี material ออกมาเป็น error magenta. แก้ด้วยเมนู **`Bob/Fix Null Outline Renderers`** (`Editor/BobOutlineFix.cs`) ปิด renderer `Outline` ที่ material ว่าง: 91 ตัวใน prefab + 27/29/33 ตัวใน `TaskRace{,_Duo,_Team}.unity`; เส้นขอบของพื้นแผนที่ยังมาจาก pass OUTLINE ใน `BOB/CurvedWorld` ตามเดิม. ตรวจซ้ำ: `slab probe: 0.0% magenta`, `SERIES CHECK PASSED`, ภาพ `Logs/series-1-start.png` เขียวปกติ

### 15.6 เฟส 2–3 ของแผน twin-island (15 พ.ย. 2026)

**เฟส 2 — ยาร์ดโมดูลาร์บนเกาะคู่ (`Editor/BobTwinIslandRig.cs`).** สร้างชุดเดียวบน Team A จาก prop ที่มีใน `Resources/Props` (ไม่ import asset ใหม่) แล้ว mirror ไป Team B ด้วยการสะท้อน transform ข้ามระนาบ x = 0 ของเกาะ: `localPosition.x -> -x`, `localRotation = (x, -y, -z, w)`, `localScale` คงเดิม — **ไม่มี `localScale.x = -1` ที่ไหน** (collider, NavMesh, ป้ายข้อความ และ normal ของแสงจึงถูกทั้งสองฝั่ง) และเป็นสูตรที่ตรงเป๊ะทางคณิตศาสตร์แม้ parent จะ scale ไม่เท่ากัน (`M·P·L·M = (M·P·M)(M·L·M)`, `M·S·M = S`). ทุกครั้งที่ rebuild ซีนจะได้ยาร์ดเดิมทุกจุด

| สถานี | เฟอร์นิเจอร์/แองเคอร์ (Team A, island-local) | prop ที่ใช้จริง | มินิเกม |
| --- | --- | --- | --- |
| S1 ล้างจานกองสูง | แขน E-counter ที่ 1 (x −3.15, z 0.85) | `SM_plates_stack`, `SM_dish_rack_blue` บนท็อป 0.85 ม. | T07 |
| S2 แยกขยะ | แขนที่ 2 (x −1.75) | `SM_bin_recycle_blue`, `SM_bin_trash_grey`, `SM_trash_bag_black` | T05 |
| S3 จัดรถเข็นตามสูตร | แขนที่ 3 (x −0.35) | `SM_crate_oranges` ×2, `SM_sack_coffee`, `SM_board_menu` | T04 |
| S4 ไล่ฝุ่นใต้โต๊ะ | โต๊ะขาเปิด (x −4.55, z 0.55) | ขา/โครงจาก primitive + `SM_bucket_metal_blue`, `SM_brush_scrub_wood`, ฝุ่น 3 กอง (`Dust`, 12/18/35°) ใต้ท็อป | T10 |
| S5 เช็ดกระจก | ชั้นวาง 3 ชั้นเปิดโล่ง มี `Slot_Top/Mid/Bottom` (x 3.25) | `SM_spray_bottle` (Top), `SM_gloves_pair` (Mid) | T08 |
| S6 จำลำดับเครื่องซักผ้า | ถัดจากชั้นวาง (x 4.55) | `SM_washing_machine` (สูง 0.84, สเกล 1.4), `SM_basket_clothes`, `SM_detergent_pump` | T15 |

- **E-counter:** สัน 4.2 × 0.5 ม. สูง 0.85 ม. ที่ z 1.55 + แขน 3 ท่อน 1.3 ม. หันเข้าหาสนาม (arm z 0.65, x −3.15/−1.75/−0.35) พร้อม toe kick — ใช้ `M_BOB_Station` และ UV palette ของ `M_BOB_CurvedMap`; แขนแต่ละท่อนมี `Counter`/`CounterTop` ของตัวเอง sehingga clearance/contact shadow วัดจากแขน ไม่ใช่สัน
- **กำแพงหลัง (135°):** รัศมี 2.8 ม. ศูนย์ที่ x −1.75 / z 1.45, กว้าง 18 ท่อน, สูง 1.85 ม. ในช่วง ±45° แล้ว taper เชิงเส้นลงเหลือ curb 0.35 ม. ที่ ±67.5°; ปลายตะวันออกหยุดที่ x 0.84 / z 2.52 จึงไม่กินปากสะพานเหนือ; `WallMountSocket` 5 ตัวที่มุม −54/−27/0/27/54° (สูงสุด 1.30 ม.) ใส่ `industrial_fan`, `shelf_wall_books`, `tv_flat`, `clock_alarm`, `ac_wall_teal` — socket พร้อมรับ prop ใหม่โดยไม่ต้องแก้โค้ด
- **ธรรมชาติ 3 ชั้น:** ใหญ่ `bush_green` ×3 (สูง 1.15–1.30, กว้างสุด 1.80 กันล้ำเคาน์เตอร์/สะพาน), กลาง `plant_pot`/`pot_blue`/`pot_ceramic_gold` ×4 (สเกล 0.9, yaw ต่างกันทุกต้น), เล็ก `rocks_grass` ×4 (สเกล 0.8) — ย้ายออกจากทางขึ้นสะพานเหนือทั้งหมด
- **ตรวจแล้ว (rebuild trio):** `[TwinRig] 7 root(s) built on Team A, 7 mirrored to Team B`, `clearance: 12 prop(s) checked, 0 decor and 0 station overlap(s)` ทั้ง solo/duo/team, 12 `TaskSpot` (6/ฝั่ง) เดินถึงทุกจุด, ภาพ `Logs/taskrace-play.png`, `-wide.png`

**เฟส 3 — `StageConfig` หกด่าน (`Rules/StageConfig.cs`, `Rules/BobStages.cs`, `Editor/BobStageBuilder.cs`, `Resources/Stages/*.asset`).** ด่านผูกกับบทตาม `order` (บท 1 → Stage 1 … บท 6 → Stage 6) ไม่มีคีย์เลือกด่านใหม่; asset สร้าง/อัปเดตซ้ำได้ด้วยเมนู **`Tools/BOB/Stages/Rebuild Six Stage Assets`** (idempotent ผ่าน SerializedObject)

| ด่าน | บท | นาฬิกา | สะพานเปิดเมื่อเหลือ | แต้ม/งาน | กฎพิเศษ |
| --- | --- | --- | --- | --- | --- |
| 1 ห้องเช่า | 1 | 120 วิ | 60 วิ | 100 | — |
| 2 รับงานเพื่อนบ้าน | 2 | 120 วิ | 60 วิ | 75 | — |
| 3 ร้านค้าในซอย | 3 | 120 วิ | 60 วิ | 100 | ปิดการ์ดก่อนเสร็จ = สตัน 1.5 วิ |
| 4 บ้านเก่ามีเรื่องเล่า | 4 | 120 วิ | 60 วิ | 100 | คราบ (`T02`) ต้องขัด 2 รอบ |
| 5 งานใหญ่ของย่าน | 5 | 90 วิ | 45 วิ | 100 | แจกงานแบบสุ่ม ไม่ยึดหมวดสถานี |
| 6 ฟื้นตลาดเก่า | 6 | 120 วิ | 60 วิ | 100 | โบนัสปิดด่าน +200 |

- **ผูกเข้าเกม:** `TaskRaceModeManager.ApplyStage()` รันใน `Awake` ต่อจาก `ApplyChapter()` (ด่านชนะค่า chapter): ตั้ง `matchDuration`, `openAtRemaining`, `warningAtRemaining = open + 5`, `pointsPerTaskComplete` และส่ง `SetStageRules(randomize, twoPass)` ให้ `TaskRaceRound`; โหมดเรื่องยังใช้ `storyRoundSeconds` 100 วิ × 3 ของตัวเอง
- **กฎที่บังคับจริง:** `TaskSpot.requiredPasses` + `LastCardCompleted` (คราบ 2 รอบ: รอบแรก `OnTaskPass` → toast "รอบแรกเสร็จ ขัดอีกรอบให้สะอาด!" แล้วยัง Dirty), `TaskRaceRound.TakeNext(..., anyOrder)` (สุ่มลำดับ), `BobTaskRaceHero.Stun()` (หยุดรับ stick/tap, HUD ยังอ่าน stick), `EndMatch` จ่าย `StageBonus` พร้อม toast; ค่าคงที่คราบคือ `TaskRaceRound.StainId = "T02"`
- **S13/Home/Mode:** `ChapterSelect` อ่าน `BobStages.Get(chapter.Order)` → บรรทัดการ์ด `LV x · Dn · 120 วิ · สะพาน 60 วิ` และ `สถานที่ · ดีสุด`, ชิปบน Home ใช้ `ClockSeconds` ของด่าน, ปุ่ม `เริ่มด่านนี้` = `PressPlay`; Mode Select ปุ่ม `เลือก` → `Phase.Chapters` (เดิมกลับ Home); ชื่อ/ข้อมูลการ์ดโหมดเปิด TMP Auto Size (20–46) เพื่อให้ "Community" ไม่ล้น
- **หัวข้อ `เลือกโหมด` / `เลือกด่าน` อ่านไม่ชัด (แก้ 15 พ.ย.):** ข้อความขาวบนฟ้าครีมจมหายเพราะ `BobUi.OutlineText` ตั้งแค่ `outlineWidth/outlineColor` แต่ตัว shader `TMP_SDF-Mobile` วาด outline ใต้ keyword `OUTLINE_ON` ซึ่งวัสดุฟอนต์ (`Mitr-Medium SDF`, `IBMPlexSansThai-Medium SDF`) ไม่ได้เปิด → outline ทั้งเกมไม่เคยถูกวาด. แก้: helper เปิด keyword บนวัสดุต่อ label (`t.fontMaterial.EnableKeyword("OUTLINE_ON")`) และเปลี่ยน `#pragma shader_feature __ OUTLINE_ON` เป็น `multi_compile` ใน `Assets/TextMesh Pro/Shaders/TMP_SDF-Mobile.shader` เพื่อให้ variant ติดไปกับ build (shader_feature จะถูกตัดถ้าไม่มี material ที่บันทึกค่า keyword ไว้); ภาพยืนยัน `Logs/bob-game-m4-modes.png` / `m4b-chapters.png` มีขอบกรมท่ารอบตัวอักษรแล้ว
- **ตรวจแล้ว (`TaskRaceVerify`):** `stages: 6 asset(s) … validate ok · saved #1`; `two-pass T02: first pass -> Dirty (passes 1/2), second -> Clean`; `random: 4 Team A station(s) hold a task from another family`; `stun: stunned=True for 1.5s`; trio verify ผ่านซ้ำหลังเฟส 4 (log `Logs/verify-rig-{solo,duo,team}.log`, `Logs/verify-{solo,duo,team}6.log`, build `Logs/build-rig-{solo5,duo2,team2}.log`)

### 15.7 เฟส 4–5 ของแผน twin-island (15 พ.ย. 2026)

**เฟส 4 — บอท Team B + แผงดีบัก (`TaskRace/TaskRaceBotController.cs`, `Editor/BotCheck.cs`).** บอทอยู่ในซีน solo เท่านั้น (duo/team ยังใช้ `BobTaskRaceRival` แบบเดิม) และเดินด้วย waypoint ไม่ใช้ `NavMeshAgent`: แผนที่แบนและ clearance check การันตีเลนสะพานอยู่แล้ว waypoint ข้ามสะพานคือ `(sign(from.x)·mouth, 0, z) → (0,0,z) → (−sign·mouth,0,z) → เป้าหมาย` โดย `z` คือตำแหน่งของ deck ใน `Bridges_Group` และ `mouth` คือ `|bounds.max.x|` ของ collider `Walkable` ของสะพานนั้น (เดิมฮาร์ดโค้ด `z = ±2.7` / `mouth = 5` ซึ่งถูกเฉพาะซีน solo ก่อนเหลือสะพานเดียว — 15 พ.ย. 2026 อ่านจากซีนแทน)

- ค่าเริ่มต้น: `speed 3.0` (ฮีโร่ 3.2), `pace 1.12` (เวลางาน = ค่าประมาณ × 1.12), `headStart 4 วิ`, `retrySeconds 1`, `raidCooldown 14 วิ`, `sabotageHoldSeconds 2.75` (เท่าการ์ดผู้เล่น), `arriveRadius 0.55`
- งาน: เลือกสถานี Dirty ของตัวเองที่ใกล้สุด → เดิน → ครบเวลา → `ForceComplete` (แต้ม Team B ออกตามค่าด่านผ่าน `TaskRaceRound`) → ถ้าพูลถึง `shieldItemCost` (150) และสถานียังไม่ติดเกราะ ซื้อเกราะ 30 วิทันที; เมื่อ `AreBridgesOpen` และพ้น cooldown จะเลือกสถานี Clean ของ Team A ที่ใกล้สุด (`!IsProtected`, `!SabotageUsed`) เดินข้ามสะพาน → `TrySabotage("คู่แข่ง")` → `CompleteSabotage`
- **ซีนบิลเดอร์:** `BuildVariant` ใส่ `Bot` (mesh `Characters/SK_hero_sweep`, yaw 90° คูณ prefab) เฉพาะ `players == 1` และเปิด shadow ให้ mesh; เมื่อมีบอทจะไม่สร้าง `BobTaskRaceRival`
- **แผงดีบัก:** 10 ปุ่มในกริด 176×52 (บาร์ 756×190 มุมขวาบน) — `โหมด: Raid`, `เวลา -10`, `⏭ 65 วิ` (`DebugSetRemaining(warningAtRemaining)`), `เปิดสะพาน`, `+150 แต้ม`, `ป่วน`, `เกราะ`, `ด่าน +1`, `รีเซ็ต`, `ซ่อน`; แสดงเฉพาะ `showDebugBar && (isEditor || isDebugBuild)`
- **ดีบักเปลี่ยนด่าน:** `TaskRaceModeManager.ApplyStage(StageConfig)` แยกจาก `ApplyStage()` ที่อ่านด่านที่บันทึก; `DebugApplyStage(order)` ใช้ลองด่าน 1–6 ในซีนเดียวโดยไม่บันทึกโปรไฟล์ (ปุ่ม `ด่าน +1` วน 6 ด่าน)
- **ตรวจ:** `TaskRaceVerify.CheckBot` รายงานใน solo `bot: idle at (9.7,-2.7) on TeamB …` และใน duo/team `bot: none (PvP variant …)`; `Editor/BotCheck.cs` (เมนู **Bob > Run Bot Check**, `-executeMethod Bob.EditorTools.BotCheck.Run` — ต้องรันโดยไม่ใส่ `-batchmode -quit` เพราะโค้ดเข้าสู่ Play และออกเอง) ใช้ hook `DebugWorkNow()` / `DebugRaidNow()` สั่งบอททำงานและข้ามสะพาน: ผลรัน `clean 1 on S2_B` → ตั้ง Team A `S1_A` สะอาด → `bridges open True` → เดินข้ามสะพานเหนือ (z 2.7, x 9.4 → −11.2) → `sabotage 1` → `BOT CHECK PASSED` (moved 20.90 u, log `Logs/bot-check4.log`)
- **บั๊กที่เจอตอนตรวจ:** `TaskSpot.TrySabotage` ตัดสินฝ่ายจาก `IsEnemySpot` (`LocalTeam != ownerTeam`) ซึ่งเป็นมุมของผู้เล่น Team A — บอท Team B จึงโดน `"จุดนี้เป็นของทีมเรา"` ทุกครั้ง. แยกเป็น `TrySabotageAs(TeamId attacker, …)` (ผู้เล่นเรียกผ่าน `TrySabotage` เดิม ไม่เปลี่ยนพฤติกรรม) และบอทเรียก `TrySabotageAs(TeamId.TeamB, "คู่แข่ง", …)`; `CompleteSabotage`/`TaskRaceRound.HandleSabotage` ยังตัดยอดฝั่งเจ้าของสถานีจาก `sideOf` ตามเดิม
- `StorySeriesCheck` มี probe บอทแล้ว: `[SeriesCheck] bot start: …` และ `bot after 16.2s: … walked 1.28 u` — เดินจริงแต่รอบซีรีส์ถูกปิดทุก ~4 วิ จึงยังทำงานไม่จบ (คนละกรณีกับ BotCheck ที่ปล่อยให้เล่นเต็มรอบ); series ยัง `SERIES CHECK PASSED` (log `Logs/series-bot3.log`)

**เฟส 5 — Docker + Firebase App Distribution (`distribute-android.sh`, `.env.example`).**

- แพ็กเกจ Android: **`com.bob.battleofthebrooms`** (`unity/ProjectSettings/ProjectSettings.asset`); `AndroidBundleVersionCode` ปัจจุบัน 2 และสคริปต์ bump เองทุกครั้ง (sed เฉพาะตัวเลข คง CRLF)
- `bash distribute-android.sh` = bump versionCode → Unity batch build (`Bob.EditorTools.BobAndroidBuild.BuildApk`) → อัปโหลด; มี `--no-bump` และ `--upload-only`; `FIREBASE_GROUPS=internal-testers` เพิ่มกลุ่มผู้ทดสอบ
- อัปโหลดผ่าน Docker `node:20-alpine` รัน `npx -y firebase-tools appdistribution:distribute` โดย mount repo เป็น `/work` และตั้ง `GOOGLE_APPLICATION_CREDENTIALS=/work/firebase-key.json` — เครื่องนี้ไม่ต้องติดตั้ง Firebase tooling; ถ้าไม่มีคีย์จะใช้ CLI ที่ล็อกอินอยู่; ถ้าไม่มีทั้งคู่จะพิมพ์ `SKIPPED the upload` แล้วจบ exit 0 (APK ยังอยู่ที่ `Builds/BOB-prototype-v1.apk`)
- ซ่อนความลับ: `firebase-key.json` และ `.env` อยู่ใน `.gitignore`; `.env.example` มีแต่ค่าไม่ลับ (`FIREBASE_APP_ID`, `FIREBASE_TESTER_GROUP`, `FIREBASE_KEY_PATH`); แอปอยู่ใน Firebase project **bob-preprod**
- ยังไม่รันจริง: รอบนี้ไม่มี APK และไม่อัปโหลด (รอ owner สั่ง) — คำสั่งเดียวคือ `bash distribute-android.sh`

### 15.8 A1 — หยุด/ออกกลางแมตช์ (15 พ.ย. 2026)

ที่มา: ตารางหน้าจอ/ทางตันใน `art/docs/11` §3.2 — Task Race ไม่มีปุ่มออกกลางแมตช์และไม่มี Esc handler ผู้เล่นต้องปิดแอป

- **`TaskRaceModeManager`**: `Paused` (get), `CanPause => !Paused && !CardOpen && !LastResult.HasValue`, `SetPaused(bool)` คืน false เมื่อถูกปฏิเสธ, `TogglePause()`; การหยุดใช้ `Time.timeScale = 0` เป็นตัวพา — ทุกกฎที่อ่าน `deltaTime` (นาฬิกา, บอท, ฮีโร่) หยุดพร้อมกันโดยไม่ต้องรู้จัก pause. `Update` ข้ามการอ่านจอยเมื่อ pause (กัน `CancelOpenSessions` ยิงตอนหยุด), `BeginMatch` / `EndMatch` / `OnDestroy` คืน `timeScale = 1` เสมอ (ฉาก Game ต้องไม่ถูกโหลดมาแบบแช่แข็ง)
- **`BobTaskRaceHud`**: ปุ่ม `หยุด` 104 × 56u มุมขวาบน (`BuildPause`, `BobTaskRaceHud.cs:539`) + การ์ด 900 × 560u `หยุดเกม` / "นาฬิกาและคู่แข่งหยุดชั่วคราว" / `เล่นต่อ` (primary) / `ออกจากการแข่งขัน` → `ResumeMatch()` แล้ว `BackToLobby()` (`LoadScene("Game")`); Esc / Android back (`OnBackPressed`) = ปิดการ์ด S28 ก่อน (ผ่าน `TaskSpot.CancelOpenSessions`) → toggle pause; ปุ่มซ่อนเมื่อ `PauseOpen || CardOpen || ResultOpen || LastResult` (`RenderPause`)
- **เหตุผลที่ pause ไม่ทับการ์ด S28/ผลแมตช์:** การ์ดมินิเกมเดินด้วย `Time.unscaledDeltaTime` (ตั้งใจ — ต้องเดินได้แม้เกมหน่วง) ถ้าหยุดกลางการ์ดได้จะเกิดการ์ดที่เดินต่อบนนาฬิกาที่หยุด; ผลแมตช์ก็ปลดล็อกปุ่มไปเองอยู่แล้ว
- **ยังไม่ทำ:** แท็บ `ตั้งค่า` ในแผง (ตาม `11` S24), P01 ถามซ้ำก่อนออก, กรณีแมตช์ออนไลน์ "เวลาไม่หยุด" (ไม่มี caller ของระบบออนไลน์แล้ว — client Nakama เดิมถูกลบ 16 พ.ย. 2026, ดู `27`)
- **ตรวจ (2 ชั้น):**
  - `TaskRaceVerify.RenderPause` (edit mode, log `Logs/verify-pause-solo.log`): `pause card: open=True paused=True timeScale=0.00 button visible=False` → ปิดแล้ว `timeScale=1.00`, ภาพ `Logs/taskrace-pause-2400x1080.png`
  - `Editor/PauseCheck.cs` (เมนู **Bob > Run Pause Check**, รันโดยไม่ใส่ `-batchmode -quit`): Play จริง → `start` 94.07 วิ, pause 0.8 วิจริงแล้วนาฬิกา **94.07 เท่าเดิม**, resume แล้ว 93.56 วิ, เปิดสถานีถือ `TASK_LOCK` แล้ว `SetPaused(true)` ถูกปฏิเสธ (`timeScale 1.00`) → `[PauseCheck] PAUSE CHECK PASSED` (log `Logs/pausecheck-solo.log`, ภาพ `Logs/pausecheck-1-button.png`, `pausecheck-2-paused.png`)
- **ไม่แตะ:** ฉาก `TaskRace*.unity` (ไม่ต้อง rebuild), `TaskSpot`, `BobTaskRaceHero`, `BobMinigameHost`

### 15.9 A3 — หน้าจบบท + ดาว 0–3 + บทถัดไป (15 พ.ย. 2026)

ที่มา: แผน tier-A จากทางตัน/journey (A1 → A2 → A4 → A3) — จบซีรีส์ของบทแล้วการ์ด S27 จบที่ `เล่นอีกครั้ง`/`กลับเมนู` เฉย ๆ ไม่มีอะไรบอกว่า "ผ่านบทแล้ว" ไม่มีดาว และไม่มีทางไปบท 2 จากผลแมตช์ (ต้องกลับ Home → เลือกด่านเองถึงจะเจอ)

- **`StorySeries`** (กติกาดาว, `StorySeries.cs`): `LocalTookSeries => LocalWins >= winsToTake` (ต้องชนะ 2 รอบ — 1–0 + เสมอ 2 รอบ **ไม่ใช่** การชนะบทแล้ว, แก้ 16 พ.ย. 2026) และ `CleanSweep => LocalTookSeries && RivalWins == 0`; `Stars => !Over || !LocalTookSeries ? 0 : 1 + (CleanSweep ? 1 : 0) + (ChallengeOffered ? 1 : 0)` — ★1 ชนะบท, ★2 ไม่เสียรอบเลย (2–0), ★3 ชนะรอบไหนขาด ≥ 3 งาน (ธงเดิม `ChallengeOffered`, `MarginForChallenge = 3`). ซีรีส์ที่ยังไม่จบได้ 0 ดวง (นำ 1–0 ไม่ใช่บทที่ปิดแล้ว) และแพ้/เสมอได้ 0 — ดาวจึงเป็น *คำอธิบายว่าเก่งแค่ไหน* ไม่ใช่เงื่อนไขชนะ. ซีรีส์ยังเก็บ `TotalLocalTasks/RivalTasks/TotalLocalPoints/RivalPoints` (ผลรวมทุกรอบที่ปิดแล้ว) และ `RoundsPlayed` ไว้จ่ายรางวัลทั้งบท
- **`BobChapters`** (`Rules/BobChapters.cs`): `XpPerLevel = 200`, `MaxLevel = 50`, `LevelFromXp(int)` -> `Clamp(1 + xp/200, 1, 50)`, `StarsPrefix = "bob.story.stars."`, `Stars(int order)` / `Stars(ChapterDefinition)`, `SaveStars(order, stars)` เก็บ **best** (`Mathf.Max` เดิม, clamp 0–3) แล้ว `PlayerPrefs.Save()`, `TotalStars`, `Next(chapter)` (chain 1→2…5→6, บท 6 = null), `StarsToOpen = 2` และ `Cleared(order)` / `MarkCleared(order)` (`bob.story.cleared.<order>`) สำหรับโบนัสเคลียร์ครั้งแรก
- **`TaskRaceModeManager.EndMatch`**: เขียนดาวก่อน `MatchEnded?.Invoke(...)` เสมอเมื่อ `record && series != null && series.Over`, และ `MarkCleared` เมื่อ `LocalTookSeries` — การ์ดจึงอ่านค่าที่บันทึกใหม่ทันที (รางวัล XP ก็ถูกเขียนก่อนเช่นกัน ปุ่มเลื่อนบทจึงใช้ LV หลังรับรางวัล). รางวัลของโหมดเนื้อเรื่องคิดจาก `series.TotalLocalTasks/TotalLocalPoints` ทุกรอบ + `BobTaskRaceProfile.FirstClearBonus = 100` เมื่อเคลียร์บทนั้นครั้งแรก (รีเพลย์ได้เฉพาะอัตราปกติ); รอบที่ไม่ได้เก็บงานเลย (`localTasks <= 0`) จ่าย 0 เหรียญ/0 XP เพื่อไม่ให้สามรอบว่างเป็นแหล่งฟาร์ม
- **`BobTaskRaceHud.ShowResult`**: แตกเป็น 2 การ์ด — ซีรีส์ที่ `Over` → `BuildChapterCard` (ใหม่), ไม่งั้น `BuildMatchCard` (โค้ดเดิมของ S27)
  - การ์ด 1080 × 840u (สูงกว่าการ์ดผลรอบ 80u เพื่อให้บรรทัดบทถัดไปมีแถบของตัวเอง ไม่ทับแผงรางวัล): หัว `ผ่านบท!` (เขียว) / `เสมอบท` (ink) / `ยังไม่ผ่าน` (แดง) 92pt → `บท n · ชื่อบท` → `ชนะ w – l` 64pt → ดาว 96u × 3 → บรรทัดเงื่อนไข `✓/✕ ชนะบท / ไม่เสียรอบ / ชนะขาด ≥ 3 งาน` → `RewardBlock` (แผงเดิม 960 × 148u) → บรรทัดบทถัดไป → ปุ่มล่าง 3 ช่อง 300 × 132u (`กลับเมนู` / `เล่นอีกครั้ง` / `บทถัดไป`)
  - **ดาวเป็นสไปรต์ ไม่ใช่ตัวอักษร:** ฟอนต์ TMP ทั้งสองตัว (Mitr, IBM Plex Thai — atlas แบบ static) ไม่มีอักขระ ★ U+2605 / ☆ U+2606 (มีแค่ ✓ U+2713) การใช้ข้อความจึงได้กล่องว่าง 3 ใบ → `StarIcons` วาด `icon_star` (ได้) / `icon_star_empty` (ยังไม่ได้) จาก art library; S13 ใน `BobGame.Menus.cs` ใช้ภาพเดียวกัน 28u มุมขวาล่างของการ์ดบท
  - **ไม่มีทางตัน:** `บทถัดไป` ปลดแล้ว → `BobChapters.Select(next, level)` แล้ว `SceneManager.LoadScene("TaskRace")` (Awake อ่าน `bob.chapter` → D/นาฬิกา/ด่านเอง; ถ้าซีนไม่อยู่ใน Build Settings จะ log warning แทนการค้าง); บทที่ยังล็อก → ปุ่มทองเทา `interactable = false` + ข้อความแดงที่ `NextNote` เลือกตามด่านที่ยังไม่ผ่าน: LV ไม่ถึง → `บทถัดไปปลดที่ LV n · คุณ LV m`, LV ถึงแต่ดาวไม่ครบ → `ต้องมีดาวบท n ครบ 2 ดวง · ตอนนี้ k/2`; บท 6 (`Next == null`) → ไม่มีปุ่มนั้นเลย เหลือ 2 ปุ่ม + บรรทัด `จบเนื้อเรื่องหลักแล้ว · เล่นซ้ำเพื่อเก็บดาวให้ครบ`. S13 ใช้กฎเดียวกัน: veil เขียน `ต้องมี LV n` หรือ `ต้องมีดาวบท n ครบ 2 ดวง`, ป้ายการ์ดขึ้น `ล็อก LV n` / `ล็อก · ดาวบทก่อนไม่ครบ`, และ `PickChapter` toast ตามด่านนั้น
- **ตรวจ (2 ชั้น):**
  - `Editor/ChapterResultCheck.cs` (เมนู **Bob > Run Chapter Result Check**, batch ได้): กติกาดาว 7 เคส ((6,3)(6,4)=3, (6,5)(5,4)=2, 2–1=1, 0–2=0, เสมอ 3 รอบ=0, 1–0 + เสมอ 2 รอบ=0 / ไม่ชนะบท, ชนะแต่ไม่เคยขาด=0, นำกลางซีรีส์=0), บันไดบท (`Next` 1→2…5→6, null ที่บท 6, `LevelFromXp` 0/199→1, 200→2, -50→1, เพดาน LV 50, บท 2 ปลดที่ LV 2 และต้องมีดาวบท 1 ≥ 2), ที่เก็บดาว (best-of, clamp 9→3, แยกบทต่อบท) และ wiring (`BuildChapterCard`, `GoToNextChapter`, `StarIcons`, `DebugComposeSeries`) → `[ChapterCheck] CHAPTER RESULT CHECK PASSED` (log `Logs/chaptercheck.log`; คืนค่าดาวบท 1 เดิมหลังทดสอบ) — ชุดเดียวกันนี้มีสำเนาแบบไม่ต้องเปิด Unity อยู่ใน `unity-check/Program.cs` (8 series assertion)
  - `TaskRaceVerify.RenderChapter` (edit mode, `Logs/verify-a3-solo.log`): `manager.DebugComposeSeries(6, 3, 6, 4)` → `DebugShowResult()` → `chapter card: chapter 1 "ห้องเช่า" · series 2-0 · stars 3/3 · next #2 "รับงานเพื่อนบ้าน" LV 2 · player LV 19`, ภาพ `Logs/taskrace-chapter-2400x1080.png` (ดาวทอง 3/3, ปุ่ม `บทถัดไป` ทอง) → `DebugCloseResult` → `DebugResetMatch`
- **ยังไม่ทำ:** รูปทีม/หีบ/ปุ่มดูโฆษณา ×2 ในหน้าจบบท (สเปก S27 เดิม), แอนิเมชันดาวร่วง/นับเหรียญ, หน้าแผนที่หมุดของ S13, ดาวรวมบน Home
- **ไม่แตะ:** ฉาก `TaskRace*.unity` (ไม่ต้อง rebuild — การ์ดสร้างตอน runtime), `TaskSpot`, `BobTaskRaceHero`, `BobMinigameHost` / opcode ของ S28

### 15.10 S28 3D prop stage — เวที 3D จริงในการ์ดมินิเกม (15 พ.ย. 2026)

ที่มา: การ์ด S28 ยังวาดพื้นที่เล่นด้วยรูปทรง uGUI แบน (Disc/Panel) ทั้งที่คลังมี `SM_*` และ rig `SK_*` ครบ — owner สั่งชัดว่าให้ใช้ของที่ทำมา ("ผมทำ asset มาตั้งเยอะ คุณจะไม่ใช้เหรอ")

- **ฉาก:** `TaskRace/BobMinigameStage.cs` (ใหม่) สร้างกล้อง orthographic → `RenderTexture` 1240 × 620 บน layer 31: วางที่ pitch 18°, สูง 4 หน่วยเหนือโต๊ะ, ถอยหลัง 1.31, ortho size 3.1, โฟกัสที่ root `(0,-400,0)`; การ์ดโชว์ RT ผ่าน RawImage ในกรอบเดิม — **rect เดิมทุกใบยังเป็น hitbox/ป้ายชื่อ**, กติกาและลำดับหน้าจอไม่เปลี่ยน
- **คณิตวางของบนโต๊ะ:** `OnCounter(px)` แปลงพิกเซลของพื้นที่เล่น (1240 × 620) เป็นจุดบนระนาบโต๊ะที่ depth = (CamUp + y·cos 18°)/sin 18°; `At(px, depth)` = ตำแหน่งกล้อง + rotation × (px/100, depth) · 100 px = 1 หน่วย → พร็อพแถวหลังที่ยึด row R สูง H ต้องมี `R + H·100 ≤ 310` (ครึ่งความสูงพื้นที่เล่น) ไม่งั้นขอบบนถูกตัด — พลาดไปสองรอบกับถังของ T05
- **โต๊ะ + เงา:** `BuildBackdrop` = slab 34 × 34 + ไม้ 12 แผ่นกว้าง 2.26 สลับเฉด เพื่อให้มีรอยต่อแนวลึก; ไฟหลัก 1 ดวง 0.85 (`LightShadows.Soft`, strength 0.65) — `BOB/ToonLit` มีเฉพาะ ForwardBase จึงรับ Directional ดวงเดียว (ดวงที่สองถูกเมินเงียบ ๆ) และค่าที่เกิน 1.0 จะ clip เป็นขาวจนลายไม้และเงาหาย; ทุกชิ้นมีเงาสัมผัสแบบ quad 64 × 64 alpha รัศมี (`Sprites/Default` + MaterialPropertyBlock `_Color`) สำรองเงาจริง
- **ของบนเวที:** `Prop(resource,height,x,yaw,row)` วางพร็อพจริง (`SM_bin_*`, `SM_sink_cabinet`, `SM_washing_machine`, `SM_bed_single_blue`, `SM_window_arched`, `SM_brush_*`, `SM_bucket_blue`, `SM_spray_bottle`, `SM_detergent_pump`, …) เป็นฉากหลัง; ชิ้นที่ผู้เล่นแตะเป็น twin ของ rect: `AddBox` (แผ่น/ถัง), `AddDisc` (คราบ จาน ปุ่ม), `AddBlob` (ฝุ่น), `AddPane` (คราบเปียก), `AddProp` (พร็อพจริงเป็นชิ้นเกม เช่นขวดสเปรย์ T08), `AddSlime` (rig `SK_enemy_slime_green/mud/dust/dark`) · `Place(twin, px)` ขยับ twin ตาม rect และสเกลเงาตามขนาดตัว, `Kill`/`SetActive` เก็บเงาไปด้วย
- **กับดักที่เจอและแก้:**
  1. rig `SK_*` นำเข้า FBX ด้วย root scale ×100 — `localScale = s` ไปแทนที่ 100 ทำให้สไลม์เหลือ 2 px: ต้อง `localScale *= s` (พร็อพตั้ง `= 1` ก่อนวัดจึงไม่โดน)
  2. `Bounds(go)` เป็น world bounds — การเซ็นเตอร์ต้องเทียบ `bounds.center - wrap.position` ไม่ใช่ `+= -bounds.center` (ไม่งั้นของลอยขึ้น 400 หน่วยพ้นกล้อง เหลือแต่เงา)
  3. กล่อง/พร็อพต้องยึดฐานที่ **ขอบล่างของ rect** (`at - (0, h/2)`) ไม่ใช่กลาง rect ไม่งั้นป้ายชื่อไปโผล่ใต้ตัวของ
  4. ไฟเกิน 1.0 บนโต๊ะสีครีมทำให้ทั้งพื้น clip เป็นขาว — ลด albedo โต๊ะ (0.90, 0.85, 0.74) และเหลือไฟหลักดวงเดียว
- **ตรวจ:** `TaskRaceVerify.RenderS28` (edit mode) ถ่าย 9 การ์ดตามชนิดปฏิสัมพันธ์ — `T02` (ถู), `T01` (ลาก), `T05` (แยกขยะ), `T11` (เติม), `T15` (ลำดับ), `T06` (กดค้าง+หมุน), `T10` (แตะ), `T08` (หลายขั้น), `T03` (สไลม์) → `Logs/taskrace-s28-{id}.png` (1600 × 900); ทุกช็อตเรียก `BobMinigameStage.RenderForCapture()` ให้ camera เรนเดอร์ RT ก่อน แล้ว log `S28 card …: live=True cardOpen=True step=… hint=…` ครบทุกใบ (log `Logs/verify-s28s.log`)
- **ยังไม่ทำ:** แถบเติมน้ำยา T11 และจอ/ปุ่ม T15 ยังเป็นแผง uGUI 2D ซ้อนบนเวที, hint/ป้ายชื่อ/แถบพลังยังเป็น uGUI (ตั้งใจ — 2D เป็น hitbox/ข้อความ), hold ring T06 ยังเป็นวงขาวจางบนจอ, ยังไม่มีฉากเฉพาะของ T04/T12/T13
- **ไม่แตะ:** กติกา (`BobTaskRaceRules`), opcode 16–21 / `server/`, `TaskSpot`, ฉาก `TaskRace*.unity` (ไม่ rebuild), `BobTaskRaceHud` frame

### 15.11 Feedback grammar — ตอบสนองแบบ Among Us (15 พ.ย. 2026)

ที่มา: owner ส่ง concept art ของ Among Us แล้วสั่ง "อ่านรูปแบบ concept art, motion, movement … ปรับมินิเกมเราให้เทียบเท่าหรือใกล้เคียงแนวคิด" — ลิงก์ Instagram ดึงภาพไม่ได้ (คืนมาแต่ CSS/JSON ไม่มีภาพ) จึงยึดหลักที่เกมใช้จริงแทน: **หนึ่งงานหนึ่งกริยา, ทุกการแตะมีคำตอบในเฟรมเดียว, คำตอบจบใน < 0.3 วิ, ผิดแล้วย้อนกลับไม่ใช่กระโดดมั่ว**

- **ไม่ลอกผิว sci-fi:** เกมเรายังเป็นพาสเทล diorama ตาม `bob-style-target` — ยืมเฉพาะไวยากรณ์การตอบสนอง ไม่เอากรอบเหล็ก/โทนเข้มของ Among Us
- **วงแหวนแตะ (`Ring`):** `BobUi.Disc` ใต้ `fx` โผล่ที่จุดสัมผัส ขยาย 0.35 → 1.5 เท่าใน 0.30 วิ (ease-out) แล้วจาง — ทอง = โดน, เขียว = ถูก, แดง = ผิด, ขาว = ฝอยตอนถู
- **ป๊อป/บี้หาย (`Pop` + `TickItem`):** ของที่ถูกเก็บไม่ `SetActive(false)` ทันทีอีกแล้ว — hitbox ตายทันทีแต่ร่างหด 0.18 วิ (`rect.localScale` = 0.45k + 0.55k²) และ twin 3D หดตามเพราะ `SyncTwin` อ่าน `rect.localScale.y`; `SyncTwin` ยังใช้ `rect.anchoredPosition` แทน `home` เพื่อให้ทั้งการบินและการสั่นเห็นบนเวที

| งาน | ตอนนี้ |
| --- | --- |
| T01/T14 ลาก | ฝุ่นแต่ละจุดป๊อป + วงแหวนทองเมื่อนิ้วผ่าน |
| T02/T07/T09 ถู | วงแหวนฝอยขาวทุก 0.14 วิใต้นิ้ว, คราบหดตาม hp, คราบหมดป๊อป |
| T03 แตะสไลม์ | สไลม์บี้ + วงแหวน, ตำแหน่งตายทันที |
| T05 แยกขยะ | ใบที่ถูกต้อง **บินเข้าถัง** 0.16 วิ + ถัง punch 1.18 เท่า + วงแหวนเขียว; ผิด **บินกลับช่องเดิม** (ไม่วาร์ปไปสุ่มจุดเหมือนเดิม) + สั่น 18 px 0.26 วิ + วงแหวนแดง + hint "ผิดถัง! ลองอีกที" |
| T06/T09/T13 กดค้าง | punch ตอนกด, วงแหวนเขียวตอนครบขั้น |
| T11 เติมน้ำยา | แถบเขียวกระพริบ (alpha .35 → .90) + วงแหวนเขียวตอนน้ำถึงแถบ, ล้น = วงแหวนแดงและรีเซ็ตสถานะแถบ |
| T15 ลำดับ | ปุ่มที่ถูก punch + วงแหวนสีปุ่ม; ผิด = ปุ่มนั้นสั่น + วงแหวนแดง |
| T10 ฝุ่น | ฝุ่นที่หมดอายุ **พัฟหาย** (วงแหวนขาว) ไม่กะพริบหายเงียบ |

- **ค่าเวลา:** `PopTime .18 / RingTime .30 / PunchTime .20 / ShakeTime .26 / FlyTime .16` (const ใน `BobMinigameHost`) — เร็วกว่า `BobUiTheme.pop .22` เล็กน้อยเพื่อให้ทันการกดถี่
- **จังหวะ:** `TickJuice` ถูกเรียกก่อนเช็ก `complete` เพื่อให้วงแหวน/ป๊อปลูกสุดท้ายเล่นจบแม้การ์ดไขแล้ว
- **ตรวจ:** `TaskRaceVerify.RenderS28` เรียก `hud.DebugProbeCard()` **ก่อน** ถ่ายภาพ แล้วเดินเวลา 0.06 วิให้ดาวป๊อปโผล่ในภาพจริง จากนั้น log 3 จุดเวลา: `S28 fx <id>: rings=2 popping=1 … fx=6/6 → +0.06s … → +0.42s rings=0 popping=0` (log `Logs/verify-s28w.log`), ภาพชุดเดิม `Logs/taskrace-s28-*.png`
- **ยังไม่ทำ:** SFX ต่อการกระทำ (งานมินิเกมยังไม่มี SFX ในคลัง), haptics, ParticleSystem จริง (ตอนนี้เป็น sprite 2D ที่ใช้ภาพ Leonardo — ดู §15.12), และแผงเฉพาะรายงานแบบ Among Us (ยังใช้เวที 3D + ป้ายร่วม)

### 15.12 สไปรต์ VFX จาก Leonardo (15 พ.ย. 2026)

owner สั่ง "api Leonardo ai … ดึง ภาพและวิดีโอมาใช้เป็นต้นแบบ และผสานใช้ในเกมส์" — session นี้ไม่มี `LEONARDO_API_KEY` จึงแยกเป็นสองทาง: **ของที่มีอยู่แล้วเข้าสู่เกมทันที** และ **ชุดสร้างใหม่พร้อมรันเมื่อมี key** (`art/docs/25_Minigame_Leonardo_Pack.md`)

- **ของเดิม (`art/leonardo_raw/vfx/`):** `art/tools/raw_vfx_to_unity.py` ตัดพื้น navy ออกจาก JPG ด้วย flood fill จากขอบ (threshold 26, blur ขอบ 1.2 px) → crop → ย่อไม่เกิน 512 → `unity/Assets/Resources/Textures/Minigame/{fx_hitstar,fx_sparkle,fx_foam,fx_dust,fx_splash,fx_slimesplat}.png`
- **โหลดแบบไม่พึ่ง import type:** โปรเจกต์เป็น 3D จึง import เป็น Texture ปกติ — `LoadSprite()` ทำ `Resources.Load<Texture2D>` + `Sprite.Create` เอง; ถ้าไฟล์หายจะ fallback เป็นวงแหวนแบนเดิม (ไม่ throw)
- **`Fx(sprite, at, size, life, spin)`:** ใช้ machinery เดียวกับ `Ring` (`RingFx` + `TickItem` loop) แต่เพิ่ม `aspect` (ภาพไม่จัตุรัส) และ `spin` (หมุน 30–70° ระหว่าง bloom)
- **จุดที่ใช้:** ชิ้นใหญ่ (`size ≥ 120`) ป๊อปด้วย `fx_hitstar`; สำเร็จขั้น hold/fill + แยกขยะถูกถัง = `fx_sparkle`; ฟองตอนถู = `fx_foam`; ฝุ่น T10 หมดอายุ = `fx_dust`; น้ำถึงแถบเขียว T11 = `fx_splash`; แตะสไลม์ T03 = `fx_slimesplat`; ของชิ้นเล็กยังใช้วงแหวนถูก ๆ เพื่อไม่ให้ฝุ่นหลายจุดกลายเป็นกำแพงประกาย
- **หลักฐาน:** `Logs/verify-s28w.log` — 9 การ์ดขึ้น `fx=6/6` (โหลด sprite ครบ 6), `rings=2 popping=1` ที่ +0.06 วิ, `rings=0 popping=0` ที่ +0.42 วิ; ภาพ `Logs/taskrace-s28-t05.png` / `-t03.png` เห็นดาวป๊อปจริงบนการ์ด


### 15.13 ชุดภาพอ้างอิง "เกาะพาสเทล" (15 พ.ย. 2026)

owner เจนผ่านหน้าเว็บ Leonardo เอง (ไม่มี API credit) แล้วส่งมา 10 รูป — ซ้ำ 2 รูป จึงเหลือ 8 ไฟล์ที่ `art/reference/islands/` เป็น **skin/มูดอ้างอิงของแผนที่** ไม่ใช่ชุด prop มินิเกม (`art/docs/25`)

| ไฟล์ | ฉาก | ใช้เป็น |
| --- | --- | --- |
| `island_twin_market_water.jpg` | เกาะคู่ + สะพานไม้ 2 ฝั่ง + น้ำตกคั่นกลาง + ป้ายร้าน | **เป้าหมายสกินหลัก** — ตรงกับเลย์เอาต์ TW2 (เกาะคู่ + สะพาน) ที่สุด |
| `island_twin_dome.jpg` | เกาะคู่เล็ก + ห้องโดมชมพูตัดโชว์ + สะพาน | สี/รูปทรงห้องสองฝั่ง (Team A/B) |
| `island_twin_detailed.jpg` | เกาะคู่ใหญ่ + ผิวน้ำแบน + ชั้นดิน | ระยะ/สเกลกล้องมุมสูง |
| `island_twin_market.jpg` | เกาะคู่ + ตลาดเบจ/เขียว | ทางเลือกโทนอุ่นของบท 6 |
| `island_room_bedroom.jpg` | เกาะเดี่ยว + ห้องนอน (หน้าต่าง+โต๊ะ+ชั้นหนังสือ) | การ์ดบท 1 / ฉากหลัง Home |
| `island_arch_kitchen.jpg` | เกาะเดี่ยว + ผนังโค้ง + เคาน์เตอร์อ่างล้าง + พัดลม | การ์ดบท 2 |
| `island_heart_wash.jpg` | เกาะรูปหัวใจ + เคาน์เตอร์ + เครื่องซักผ้า | การ์ดบท 3 |
| `island_teardrop_kitchen.jpg` | เกาะหยดน้ำ + ผนังซุ้ม + อ่างล้าง + หน้าต่างโค้ง | การ์ดบท 4 |

**พาเลตต์ที่วัดได้ (median cut 8 สี):** ฉากหลังครีม `#FBE6CC`–`#FCEBDB`, หญ้าโอลีฟ–ไลม์ `#A9A452`/`#AFAB68`, ดินทราย `#9D6E44`/`#A56F39`, น้ำฟ้าแบน `#75C1D9`, ฟอง `#A0DED0`, ไม้/ขอบผนัง `#D3C993`

**ส่วนต่างจากซีนที่สร้างอยู่ (5 ข้อ):**
1. ฉากหลังเป็น **กระดาษครีมแบน** ไม่มี gradient sky / cloud sea / fog
2. เกาะตัดด้วย **ขอบหญ้าหนาห้อยเป็นปาก** + ดินทรายแถบเดียว (ไม่ใช่ rock strata หลายชั้น)
3. น้ำเป็น **ระนาบแมตต์ฟ้าแบน** (ไม่ใช่ cloud sea) และน้ำตกเป็นริบบิ้นแบนมีฟองม้วนที่ปลาย
4. ห้องเป็น **เปลือกซุ้ม/โดมตัดโชว์** เปิดหันเข้ากล้อง ผนังสีเดียว — ไม่ใช่กำแพง 135° + ชั้นวางทั้งชุดของ TW2
5. สะพานเป็น **แผ่นไม้ล้วนไม่มีราว** และมี **เงาเบลอใต้เกาะ** วงใหญ่

**ยังไม่ทำ:** สลับสกินจริง (ยังไม่มี material/แสงชุดใหม่), ไม่ได้ใช้เป็นการ์ด S13 จริง (การ์ดยังใช้ art id 4 ใบ `lobby_*` จาก `BobArtLibrary`), และไม่ได้ผูกกับ `StageConfig`

### 15.14 ผลลองสกินรอบแรก — look-dev (15 พ.ย. 2026)

owner เลือกทาง "ทำ look-dev สกินแผนที่ก่อนแตะกติกา" → ทำ pass ที่ **ไม่เซฟซีน** ตัวเดียว: `unity/Assets/Scripts/Editor/BobIslandSkinDev.cs` (`Bob > Render Island Skin (look-dev)`) เปิด `Scenes/TaskRace.unity`, เปลี่ยนสี/ปิดของในหน่วยความจำ, เรนเดอร์ 3 มุม (`Logs/skin-play.png` มุมเล่น, `-high.png` มุมสูง, `-wide.png` มุมกว้าง) แล้วจบโดยไม่ save — ของจริงในซีนยังเป็นสกินเดิมทั้งหมด

| สิ่งที่เปลี่ยน | วิธี | ค่าที่ใช้ |
| --- | --- | --- |
| ฉากหลัง | ปิด `BobSkyBackdrop`, `BobCloudSea`, fog; ambient แบน | camera clear `#FBE6CC` |
| หญ้า | slab `Field` + dressing mesh ทาสีใหม่ให้เป็นโอลีฟเดียว | slab `#A3AC58` (mottle .02), dressing `#A0A95C` (mottle .04) |
| ดิน/โขดใต้เกาะ | `Field_Soil`/`Island` ทึบ | `#A56F39` / `#C08B5A` |
| ผนัง 135° + `WallSeg*` + `Counter`/`CounterTop` | ทึบเป็นครีม | ผนัง `#F6E7CE`, หน้าเคาน์เตอร์ `#F7EBD6`, ตัวเคาน์เตอร์ `#D9BF98` |
| หินน้ำตก (`SM_rocks_grass`) + `RiverChannel` | ทึบเป็นหินอ่อน | `#E4DEC8` / `#D6E5E1` |
| น้ำ | Quad แบน `BOB/Unlit` ในร่องกลางเกาะ (ไม่ใช่ sea ทั้งฉาก) | `#75C1D9`, 6×8 u ที่ y −5.5 |
| สะพาน | ปิด `BobBridgeBarrier`, `BobBridgeDressing.rails = false`, ทาสีแผ่นไม้ | `#D3C993` |

**สิ่งที่ติด (แก้ไปแล้วในรอบเดียวกัน):**

1. dressing/น้ำตก **ผูก parent ไม่ได้** — ถูกสร้างตอนรันเป็น object ลอยที่ `HideFlags.HideAndDontSave` → `FindObjectsByType` ไม่คืนมา ต้องใช้ `Resources.FindObjectsOfTypeAll<Renderer>()` + กรอง `EditorUtility.IsPersistent` (ของใน asset) ออก
2. ชื่อ mesh จริงไม่ตรงกับที่เดา: ร่องน้ำคือ **`RiverChannel`**, หินน้ำตกคือ **`SM_rocks_grass`** — matching ต้องเก็บชื่อจาก log ไม่ใช่จากชื่อ object ที่ตั้งไว้
3. dressing ใช้ **multiply tint** ไม่ได้: พาเลตต์หญ้าเป็นเขียวสด คุณด้วยสีโอลีฟแล้วออกมาเป็นฟางแห้งส้ม → ต้องทึบ (`_MainTex` ขาว 1×1 + `_Color`) ซึ่งยอมเสียดอกเดซี่/หินเล็กใน mesh เดียวกันไป
4. น้ำแบบ sea ทั้งฉากทำให้ฉากหลังกลายเป็นน้ำ → ตัดเหลือร่องกว้าง 6 u ยาว 8 u กลางเกาะ

**ที่ยังต่างจาก reference (เหลือไว้ให้ owner เคาะ):** เงาเบลอใต้เกาะยังไม่มี · ผิวน้ำเป็นสี่เหลี่ยมแข็ง ไม่มีขอบ/ฟองม้วน · ผนังหลังยังเป็นเปลือกหินหยัก ไม่ใช่ซุ้ม/โดมตัดโชว์ · แผ่นไม้สะพานขาวเกิน reference · prop เดิม (พุ่มเขียวสด, ถังฟ้า, ตะกร้า) ยังใช้พาเลตต์เก่าและตัดกับสกินใหม่

**ไม่แตะ:** กติกา, `TaskSpot`, station layout, `TaskRaceModeManager`, opcode/server, และไม่ save `Scenes/TaskRace.unity` (เปลี่ยนในซีนมีแค่ field `rails` ของ `BobBridgeDressing` ซึ่ง default = true จึงไม่มีผลกับการเล่น)

### 15.15 บรรยากาศมีชีวิต · ฟีดแบ็กจบงาน · วิดีโอเมนู (15 พ.ย. 2026)

owner สั่ง 4 ข้อ: (1) แสงแดดอุ่นพาสเทล + bloom เบา, (2) ambient motion ของฉาก, (3) squash & stretch + ดาวทองตอนงานเสร็จ, (4) วิดีโอพื้นหลังเมนู + ช่องคลิปสอนในมินิเกม โดยคุมที่ **เบา/60fps/ไม่ over-engineer** ทุกอย่างสร้างจากโค้ดตอนรัน ไม่มี asset ใหม่ และไม่แตะ YAML ของซีน

**ข้อกำหนดที่ถูก "แปลง" เพราะสแตกไม่รองรับ (ต้องบอกทุกครั้งที่สรุปงาน):**

| ที่สั่งมา | สแตกจริง | ที่ทำแทน |
| --- | --- | --- |
| URP Post-Processing `Volume` (Bloom .35 / threshold 1.05, Color Adjustments +0.15 EV / +10 sat) | โปรเจกต์เป็น **built-in RP** — manifest มีแต่ `com.unity.ugui` ไม่มี URP และไม่มี post-processing package | ต่อ `BobPostGrade` (มีอยู่แล้ว) ด้วย exposure/saturation และเพิ่ม `Hidden/BOB/Bloom` = half-res bright pass + blur 2 pass (3 draw/เฟรม, ไม่มี compute, ไม่มีกล้องเพิ่ม) |
| ลมบน "ต้นไม้ + ผ้าใบร้าน" | ซีนไม่มีต้นไม้และไม่มีผ้าใบลายทาง | ลมบนพุ่มไม้ 3 ระดับ (`SM_bush_green` ×3) + กระถาง (`SM_plant_pot`/`SM_pot_blue`/`SM_pot_ceramic_gold` ×4) + หญ้าทั้งเกาะ (ผ่าน shader) |
| ไอร้อนจาก "station กาแฟ/สตีม" (บท 2, 3, 6) | ไม่มี station กาแฟในซีน | ไอขาวอบอุ่นวางบนทุก `TaskSpot` ที่ `Category == Wash` (S1 จาน, S5 แก้ว) — station ที่มีน้ำจริง |
| ช่องคลิปสอนใน `UniversalMinigameModal` | ไม่มีคลาสนี้ในโปรเจกต์ | ช่องคลิปมุมบนซ้ายของการ์ด S28 (`BobMinigameHost` → `BobTutorialClip`) เฟดออกใน 4 วิ ไม่บังพื้นที่เล่น |
| `transcode` / `codec = H264` บน `VideoClipImporter` | Unity 6 ย้ายไปอยู่บน `defaultTargetSettings` | `importer.defaultTargetSettings` → `enableTranscoding = true`, `codec = VideoCodec.H264`, 1280×720, `importAudio = false` |

**ไฟล์ที่เพิ่ม/แก้**

| ไฟล์ | ทำอะไร | ค่าหลัก |
| --- | --- | --- |
| `TaskRace/DioramaLightingRig.cs` (แก้) | แสงแดดอุ่น + ambient ไล่สี | sun `#FFF3DC` 1.12 · `ShadowStrength .45` · Soft · Trilight sky `(.98,.95,.88)` / equator `(1,.93,.84)` / ground `(.74,.64,.54)` · fog off · **save/restore ตอน OnDisable** กัน tint ค้างไป lobby |
| `TaskRace/BobPostGrade.cs` + `Resources/Shaders/BobPostGrade.shader` + `BobBloom.shader` (ใหม่) | เกรด + bloom แทน URP volume | exposure `+0.03` · saturation `1.10` · bloom `0.35` · threshold `1.05` (HDR) / `0.98` (LDR) · blur 5-tap ×2 |
| `TaskRace/DioramaAmbient.cs` (ใหม่) | builder ตัวเดียว: แสง, ลม, dust, steam, water, juice, grade | `TaskRaceModeManager.Awake` → `Ensure(gameObject)` |
| `TaskRace/AmbientWindSway.cs` (แก้) | แกว่งด้วย `Time.unscaledTime` | พุ่ม 2.2°/0.95 · กระถาง 1.0°/1.35 · เฟสจาก world position |
| `Resources/Shaders/BobDressing.shader` + `TaskRace/BobFieldDressing.cs` (แก้) | ลมหญ้าใน shader ตัวเดียว | **alpha ของ vertex = น้ำหนักลม** (โคน 0 ปลาย 1) + `_Wind` ผ่าน MaterialPropertyBlock |
| `TaskRace/BobWaterFlow.cs` (ใหม่) | overlay น้ำไหล + ฝอยน้ำตก | river `0.35` u/s, falls `1.15` u/s · texture 32×64 procedural (Repeat) · mist 8/วิ |
| `TaskRace/BobStationJuice.cs` (ใหม่) | ฟีดแบ็กจบงาน | `0.35s`: `1 → (1.15,.85,1.15) → (.95,1.08,.95) → 1` · sheen flash 0.45 วิ · sparkle 22 ตัว |
| `TaskRace/BobAmbientFx.cs` (ใหม่) | dot texture + material + factory | 64×64 radial alpha สร้างตอนรัน · `Sprites/Default` (shader เดียวที่มี alpha ในโปรเจกต์) |
| `Game/MenuBackgroundVideo.cs` · `Game/BobTutorialClip.cs` · `Editor/BobVideoImportSettings.cs` (ใหม่) | วิดีโอเมนู + ช่องสอน + import rule | RT 1280×720 zero-depth ใบเดียว · ไม่มีคลิป = ซ่อนช่อง ไม่มีเฟรมดำ |
| `Editor/TaskRaceVerify.cs` (แก้) | สร้าง ambient pass ก่อนถ่าย + `Warm(4s)` ให้ particle มีตัวในเฟรมแรก | เรียก `Preview()` / `Warm()` / `PreviewTearDown()` |

**ค่าที่ต้องจูนหน้างาน (วัดจริง ไม่ใช่ค่าจากสเปก)**

- exposure `+0.15` ตามสเปกทำให้ภาพพาสเทลไหม้: วัดช็อตกว้างได้พิกเซลใกล้ขาว **59.5%** (ก่อน pass 0.1%) ฟ้าเปลี่ยนจากฟ้าอ่อนเป็นมินต์ขาว → ลดเหลือ `+0.03` ได้ `Lmean 0.853` / ใกล้ขาว `8.7%` (ก่อน pass `0.842` / `0.1%`) และช็อตเล่น `Lmean 0.561` เท่าเดิม (ก่อน 0.564) แต่ใกล้ขาวลดจาก 8.8% → 1.5%
- threshold ใน LDR: buffer ARGB32 ไม่มีอะไรเกิน 1.0 → `1.05` ไม่มีวันทำงาน ต้องใช้ `0.98` (HDR ยังใช้ 1.05)
- dust: 56–62 ตัวในกล้อง แต่ alpha ที่ได้จริงแค่ 64–167/255 และขนาด 0.1–0.24 u → เห็นเป็นจุดจางมาก ปรับสี `(1,.96,.82)` alpha .9, rate 6/วิ, max 110

**วิดีโอ (วางไฟล์แล้วได้เลย ไม่ต้องแก้โค้ด)**

1. วาง `.mp4` (หรือ mov/m4v/webm/avi) ที่ `unity/Assets/Video/`
2. `BobVideoImportSettings` (AssetPostprocessor) ตั้ง import อัตโนมัติ: **ไม่มี audio track**, `sRGBClip`, transcode H264 1280×720
3. `OnPostprocessAllAssets` ย้ายไฟล์ไป `Assets/Resources/Video/` ให้เอง (deferred `AssetDatabase.MoveAsset`; ถ้าปลายทางมีอยู่แล้วจะไม่ย้ายและ log เตือน) — เพราะ `Resources.Load` เป็นทางเดียวที่เมนูเข้าถึงคลิปได้โดยไม่มี serialized reference
4. ชื่อที่เกมค้นหา: `Video/menu_loop` (พื้นหลัง S13) และ `Video/tutorial_<taskId>` (เช่น `tutorial_T02`)
5. ไม่มีไฟล์ = `MenuBackgroundVideo.Available` เป็น false / ช่องสอนถูกซ่อน — ไม่มีเฟรมดำ ไม่มี error

**คลิปที่เลือกวางจริง (15 พ.ย. 2026)** — copy จาก `Assets/Leonardo/Video` เป็นชื่อที่ runtime ค้นหา; ต้นฉบับ 13 คลิปยังอยู่ครบ

| ไฟล์ใน `Assets/Video/` | ต้นทาง (Hailuo) | ใช้ที่ | ขนาด |
| --- | --- | --- | ---: |
| `menu_loop.mp4` | cutaway wooden room | พื้นหลัง S13 + S11 | 4.0 MB |
| `tutorial_T05.mp4` | three sorting bins | แยกขยะ | 4.1 MB |
| `tutorial_T07.mp4` | ceramic kitchen sink | ล้างจานกองสูง | 7.2 MB |
| `tutorial_T08.mp4` | glass display shelf | เช็ดกระจก | 4.8 MB |
| `tutorial_T10.mp4` | broom under oak table | ไล่ฝุ่นใต้โต๊ะ | 4.6 MB |
| `tutorial_T11.mp4` | retro espresso machine | เติมน้ำยาตามระดับ | 4.8 MB |
| `tutorial_T15.mp4` | washing machine | จำลำดับเครื่องซักผ้า | 4.1 MB |

- เมนูเลือก **cutaway wooden room** ไม่ใช่ twin-island trailer เพราะวัด first/last frame diff (mean abs, 0–255): cutaway room `3.4` เกือบไร้รอยต่อ ส่วน trailer `53.9` / aerial fly-by `57.6` / sink `52.8` จะเห็นรอยตัดชัดทุก 6.6 วิ (การ์ดสอนถือแค่ 4 วิ จึงไม่กังวลเรื่องนี้)
- ยังไม่ transcode: เปิด Unity Editor ครั้งถัดไป importer จะแปลง 720p/no-audio แล้วย้ายเข้า `Resources/Video` เอง; ก่อนถึงตอนนั้น `Available` ยังเป็น false และเกมเล่นได้ปกติโดยไม่มีวิดีโอ
- อีก 6 คลิป (island cutaway, aerial fly-by, twin-island trailer, trash chute, door lock, kitchen counter) ยังไม่ผูก — เก็บไว้ใช้กับ T01/T02/T04/T06/T09/T12/T13 รอบถัดไป
- **batchmode:** `EditorApplication.delayCall` ไม่ทันรันก่อน `-quit` คลิปจึงถูก import แต่ไม่ย้าย — เพิ่ม `BobVideoImportSettings.MovePending()` (ย้ายทุกคลิปที่ค้างใน `Assets/Video` แบบ synchronous) และ `BobAndroidBuild.BuildApk` เรียกก่อน build; APK 21:09 15 พ.ย. มีคลิปครบ 7 (65.8 → 78.6 MB, log `Bob: 7 video clip(s) moved to Resources/Video before the build`)

**กับดักที่เจอในรอบนี้ (แก้แล้ว)**

1. `VideoClip` ต้อง `using UnityEngine.Video;` ไม่งั้น `error CS0246` ทั้ง editor assembly (รอบแรก compile ไม่ผ่านทั้งชุด)
2. `velocityOverLifetime` ต้องตั้ง **ทั้ง 3 แกนเป็น curve mode เดียวกัน** ไม่งั้น Unity ขึ้น `Particle Velocity curves must all be in the same mode` และ `Simulate` ไม่เดิน — `new MinMaxCurve(x)` กับ `new MinMaxCurve(min,max)` คนละ mode
3. **`FindObjectsByType` มองไม่เห็น object ที่ `HideFlags.HideAndDontSave`** (กับดักเดิม §15.14 ข้อ 1) — `BobFieldDressing`/`BobWaterfallDressing` ถูกสร้างเป็นแบบนั้น ทำให้ลมหญ้าและ overlay น้ำไม่เคยผูกเลย ต้องใช้ `Resources.FindObjectsOfTypeAll<T>()` + เช็ก `gameObject.scene.IsValid()` (helper `DioramaAmbient.Live<T>()`)
4. กล่อง dust เคยคำนวณจาก min/max ของ `TaskSpot` แล้วมี station นอกเกาะดึงกล่องไปถึง x = −9.7 → ฝุ่นกระจายจนมองไม่เห็น เปลี่ยนไปใช้ bounds ของ `BobFieldDressing` (พื้นหญ้าจริงของสองเกาะ)
5. `Transform` ของ station อยู่ที่ระดับพื้น → สเกล root squash ได้โดยไม่ลอย; palindrome scale คืนรูปด้วย smoothstep ต่อช่วง
6. `Space.Prewarm` ใช้กับ particle ที่ `loop = false` ไม่ได้ (sparkle burst) ต้อง `prewarm = false` ชัด ๆ
7. ต้อง `SetPropertyBlock(null)` คืนค่า `_Sheen` หลัง flash — ปลอดภัยเพราะสีพาเลตต์อบใน UV ของ mesh ไม่ได้ใช้ property block

**หลักฐาน:** `Logs/ambient-final.log` — compile 0 error, `[DioramaAmbient] DustMotes: … live at …, Sprites/Default, tex=AmbientDot, alpha=…`, `WaterMist: …`, `Exiting … return code 0`; ภาพ `Logs/taskrace-play.png`, `-wide.png`, `-preview.png`, `-hud.png` เทียบกับชุดก่อน pass `Logs/taskrace-*-2400x1080.png` (12:02) และตัวเลข luminance ที่วัดด้วยสคริปต์ (ระบุในตารางข้างบน)

**ยังไม่ทำ:** 7 คลิปที่วางไว้ยังรอ transcode ใน Unity และยังไม่ commit (ขนาดรวม 35.4 MB ต้องตัดสินใจเรื่อง LFS ก่อน), ไม่มี SFX ต่อการกระทำ, dust/mist ยังเป็น dot กลม (ยังไม่ใช่ bokeh หลายชั้น), ผิวน้ำยังเป็น quad แบนไม่มีขอบฟอง (§15.13 ข้อค้างเดิม), เงาเบลอใต้เกาะยังไม่มี

**ไม่แตะ:** กติกา (`BobTaskRaceRules`), ตรรกะ `TaskSpot`, layout `BobTaskRaceHud`, opcode 16–21 / `server/`, YAML ของ `TaskRace*.unity` (ไม่ rebuild), และไม่ commit/push

### 15.16 สะพานเดียวสำหรับสองเกาะ (15 พ.ย. 2026)

owner ดูภาพกว้างของแผนที่แล้วชี้ว่า **สองสะพานบนสองเกาะไม่สมเหตุสมผล — ถ้าจะมีสองสะพานต้องมีเกาะที่สาม** แผนที่เดิมมี `Bridge_North` (z +0.3·depth) กับ `Bridge_South` (z −0.3·depth) ทั้งที่เกาะมีแค่สองใบ

- **แก้ที่ต้นทาง:** `BobMapAssemblerEditor.BuildMap` สร้าง deck เดียวชื่อ `Bridge` ที่ `z = +islandDepth × 0.3` (solo `2.70` · duo `3.38` · team `4.05`) พร้อม `Walkable` collider, `BridgeDetail_Group` deck และ `BridgeBarrier_Group` gate อย่างละชุด; เหตุผลที่เลือกเลนนี้ไม่ใช่ z = 0 คือแถว E-counter เริ่มที่ island-local z `1.30` — deck กลางจอจะพาดผ่านเคาน์เตอร์ และเลนนี้ยังเป็นฝั่ง +Z ที่กล้องเล่นมองเห็นข้างหน้าฮีโร่
- **บอท:** `TaskRaceBotController.BuildRoute` เลิกฮาร์ดโค้ด `z = ±2.7 / mouth = 5` — อ่าน `z` จาก child ของ `manager.bridgesGroup` และ `mouth` จาก `|bounds.max.x|` ของ collider `Walkable` (ค่าเดิมถูกเฉพาะ solo ด้วยซ้ำ)
- **ผลหลัง rebuild ทั้ง 3 ซีน** (`Logs/build-{Build,BuildDuo,BuildTeam}.log`, 0 error): `clearance decks: … z[1.88,3.52]` (duo `2.56–4.19`, team `3.23–4.87`), `12 prop(s) checked … on 1 deck(s)`, `navmesh geometry: 3 walkable collider(s), 3 ignored`, `navmesh baked: PureRace 24/28/28 tri, TimedBridgeRaid 40/44/46 tri (bridge decks add 16/16/18)`
- **verify** (`Logs/onebridge.log`): `barriers=1 links=1`, `nav … gates shut status=PathPartial`, `gates open status=PathComplete corners=5 length 21.0 u`, `probe Bridge west/centre/east: on navmesh off by 0.03/0.00/0.03 u`, `deck Bridge: collider x[-5.78,5.78] z[1.88,3.52]`, `gameplay start: … barriers=1`; duo/team (`Logs/onebridge-{duo,team}.log`) ได้ `PathComplete 26.0 / 31.1 u` ที่ `z 3.38 / 4.05`, `probe … off by ≤0.05 u`, `player HUD: 7 / 9 task row(s)`
- **บอทข้ามจริง** (`Logs/bot-onebridge.log`): `clean 1 on S2_B` → `bridges open … 1 gate(s) lowered` → `sabotage 1 on S1_A` → `BOT CHECK PASSED`
- ภาพ: `Logs/taskrace-wide.png` (สะพานช่วงเดียวใต้ผาน้ำตก), `Logs/taskrace-play.png`, `-preview.png`, `-hud.png` · **ย้อนกลับได้:** ปรับ `bridgeZ` ใน assembler แล้วเรียก `Tools > BOB > Build Task Race Scene` ทั้งสามโหมด (จะ bake NavMesh ใหม่ให้เอง)
- **ไม่แตะ:** กติกา/opcode/`server/`, layout ของ `TaskRaceHud`, สถานีและของประดับ (clearance ยังเป็น 0 overlap) · **ยังไม่ commit/push**

### 15.17 E2E ของเส้นทางที่ผู้เล่นเจอจริง (15 พ.ย. 2026)

owner สั่ง "เทส e2e ของแอพ" — ของเดิม (`BobGameScene.RunTest` + `BobGame.Dev.AutoPlay`) บอทเล่น *ในซีนเดียว* ไม่เคยข้ามไป `TaskRace` เลย จึงไม่เคยพิสูจน์ว่าการเดินเรื่องทั้งเส้นต่อกันได้ ของใหม่จึงเป็นตัวขับที่ข้ามซีนได้จริง

- **ไฟล์ใหม่:** `Assets/Scripts/Dev/BobE2EDriver.cs` (ตัวขับ, `DontDestroyOnLoad`) + `Assets/Scripts/Editor/BobE2E.cs` (ปุ่ม `Bob > Run E2E` และ entry point สำหรับ command line) — เปิด `Game.unity`, รอ editor นิ่ง 12 วิ, `EnterPlaymode`, watchdog 900 วิ, แล้วแปลง `[E2E] E2E PASSED/FAILED` เป็น exit code
- **ลำดับที่กดจริง (ไม่ใช่ teleport):** ปุ่ม `เล่นเลย (Guest)` (S02) → `เล่น` (S10) → รอ `TaskRaceModeManager` ขึ้น → นับถอยหลัง 3-2-1 ครบ → เช็กว่านาฬิกาเดิน (`RemainingSeconds` ลดใน 1 วิ) → ต่อรอบ: `BobTaskRaceHero.SetDestination(จุดของสถานี)` แล้วรอ `TaskSpot.IsNear`, กดปุ่ม `ทำงาน` ของ HUD, กด `BobMinigameHost.DebugSolve()` จนการ์ดปิด, เช็ก `spot.Done` → `DebugAddPoints(400)` + `DebugFinishRound()` (จำลอง "หมดเวลา") → รอการ์ดบท (`กลับเมนู`) → กด `กลับเมนู` → รอหน้า Home กลับมา
- **กติกาของตัวขับ:** รอบจะเริ่มได้เมื่อ **ไม่มีบัตรเปิด · ไม่นับถอยหลัง · นาฬิกา > 1 · มีสถานีของฝั่งเราที่ยังสกปรก** (เพราะสถานีถูกแจกใหม่ใน `BeginRound` และ `FinishRound` ปฏิเสธระหว่างช่วงส่งต่อรอบ); จะหยุดทำงานก็ต่อเมื่อ **งานของเรานำคู่แข่ง** (เสมองานตัดสินด้วยแต้มที่เติมให้) ไม่งั้นอาจแพ้ด้วยจำนวนงานทั้งที่ฟีดแบ็กทุกอย่างถูก; `DebugFinishRound` ยิงซ้ำทุก 0.5 วิจนกว่าซีรีส์จะลงบัญชี
- **บั๊กที่เจอระหว่างทำ** (บันทึกไว้เพราะเป็นกับดักของ harness แนวนี้):
  1. จุดยืนห่าง `0.75 × InteractRadius` ทำให้ walk หยุดที่ `arriveRadius` 0.4 แล้วจบที่ d = 1.30 ทั้งที่รัศมีจริง 1.20 → HUD ไม่โผล่ `ทำงาน`; ต้องเดินเข้าหาศูนย์สถานี
  2. `WaitFor(!CardOpen && !IsCountingDown)` ผ่านได้ในช่วงแบนเนอร์ 3 วิระหว่างรอบ (นาฬิกา 0 ไม่ได้กัน) → เก็บงานใส่รอบที่ยังไม่เริ่ม แล้ว `DebugFinishRound` ถูกเมินเพราะ `roundEnding` → ต้องรอรอบที่ "เล่นได้จริง" ตามเงื่อนไขข้างบน
  3. `Unity` สองตัวเปิดโปรเจกต์เดียวกัน = dialog `Error!` แล้ว log หยุดที่ `Successfully changed project path` (upm แสดง `parent process [19612] is no longer running`) — ต้องเปิดทีละตัว
- **ผล** (`Logs/e2e-run6.log`, exit 0): `TaskRace up: stage 1 "ห้องเช่า", series=True, 100 s × 2 wins, local TeamA` → `round 1 … S5_A "ถูคราบวน" (T02) … clean, score 0 → 100` → `round 1/3 Win · tasks 1:0 · points 500:0 · series 1-0` → `round 2 … S5_A "แช่คราบรอ" (T09) … clean` → `round 2/3 Win · series 2-0` → `match end: win … reward 100 coins +63 xp` → `chapter card: … series 2-0 · stars 2/3 · coins 1410 → 1510` → `pressed "กลับเมนู"` → `E2E PASSED: login → home → TaskRace (stage 1) → 2-0 with 2/3 stars, +100 coins → home` → `wallet restored, exit code 0`
- **ภาพ** (`BOB_VIEW=2400x1080`): `Logs/e2e-1-login-2400x1080.png`, `-2-home`, `-3-play`, `-4-near`, `-5-card`, `-6-clean`, `-7-chapter` (การ์ด "ผ่านบท!" 2 ดาว + ปุ่ม 3 ปุ่ม), `-8-home-return` (Home กลับมา, เหรียญ 1510 ก่อนคืนค่า)
- **คืนกระเป๋า:** `BobE2E` snapshot `bob.guest/coins/xp/taskRace.*/story.lossStreak/story.stars.1–6` ก่อนรัน แล้วเซ็ตคืนตอนจบ (`bob.guest` ถูกบังคับเป็น 0 เพื่อให้ S02 ถูกทดสอบทุกครั้ง)
- **ยังไม่ครอบ:** โหมดดูโอ/ทีม, การข้ามสะพานไปก่อกวนฝั่งคู่แข่ง, ปุ่ม `เล่นอีกครั้ง` / `บทถัดไป`, การซื้อของในร้าน, และการเล่นด้วยนิ้วจริง (ตัวขับใช้ API เดียวกับที่ touch เรียก ไม่ได้ผ่าน pointer event)
- **ไม่แตะ:** กติกา/opcode/`server/`/ซีน/`TaskSpot`/layout HUD · **ยังไม่ commit/push**

### 15.18 เสียงของการ์ดมินิเกม + audio player ที่ boot เอง (16 พ.ย. 2026)

งานเฟส 5 ของ #47 (ข้อ 15.3) ส่วนเสียง — ของเดิม `BobMinigameHost` เงียบสนิท และ**ซีน Task Race ทั้งสามไม่มี audio player อยู่ในฉากเลย** (GUID ของ `Assets/Art/BobAudio.prefab` ไม่ถูกอ้างใน `TaskRace{,_Duo,_Team}.unity`; `BobRaceCountdown` มี guard `FindAnyObjectByType<BobSfx>() == null` ไว้แล้วจึงไม่มีเสียงนับถอยหลัง) การเรียก `BobSfx.Play` ตรง ๆ จะได้แค่ `Debug.LogWarning`

- **`BobSfx` boot ตัวเอง** (`Assets/Scripts/Art/BobSfx.cs`): `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` — ถ้า `inst` ยังว่าง (แปลว่าซีนไม่มี prefab หรือ prefab ยังไม่ Awake) จะ `Resources.Load<GameObject>("Audio/BobAudio")` แล้ว `Instantiate` ครั้งเดียว; ตั้ง `AudioListener.volume` ตาม `bob.sound` ด้วย (เดิมค่านี้ถูกใช้แค่ตอนกดสวิตช์ใน Settings → เปิดแอปใหม่แล้วเสียงกลับมาเต็มแม้ผู้เล่นปิดไว้)
- **`Assets/Resources/Audio/BobAudio.prefab`**: สำเนา byte-identical ของ `Assets/Art/BobAudio.prefab` (คลิปทั้ง 38 ไฟล์ชี้ GUID เดิม, meta ใหม่) ให้ `Resources.Load` เห็น — ฉาก `Game` ที่มี prefab อยู่แล้วยังใช้ของเดิม ไม่มี instance ซ้ำ เพราะ `Awake` ของ prefab ในซีนรันก่อน boot hook
- **`TaskRace/BobMinigameSfx.cs`** (ใหม่): map 8 `MiniKind` → ชื่อคลิปที่มีจริงใน `Assets/Audio/SFX`
  | kind | แตะสำเร็จ | ผิด/พลาด | ต่อเนื่อง |
  | --- | --- | --- | --- |
  | Trace | `broom_sweep` (throttle 0.12 วิ) | — | — |
  | Scrub | เงียบ (สัมผัสคือ loop) | — | `scrub_loop` เริ่มที่กด หยุดที่ปล่อย/ปิดการ์ด |
  | Tap | `pickup` · T03 `mud_splat` | — | — |
  | Sort | `ui_tap` ตอนหยิบ, `pass_part` ตอนลงถังถูก | `drop_invalid` (ลงถังผิด) | — |
  | Hold | `lift_layer` | — | — |
  | Fill | `bubble_pop` ตอนถึงขีดเขียว | `drop_invalid` (ล้น) | `water_pour` ระหว่างกดค้าง |
  | Sequence | `ui_confirm` | `ui_cancel` | `ui_tap` ตอนแพดกะพริบ (throttle 0.1 วิ) |
  | Timed | ไม่มี (การ์ดจับเวลาไม่ใช่การ์ดมินิเกม) | — | — |
  ทุกขั้นที่มีหลายขั้น: `reaction_good` ตอนจบขั้น · จบการ์ด: `round_win` + `score_count` · เปิดการ์ด: `ui_card_flip`
- **hook:** `BobMinigameHost` เรียกที่ `Begin/SetupStep/Advance/Hide/Cancel/PointerDown/PointerUp` และจุดสำเร็จ/พลาดของแต่ละ kind เท่านั้น — **ไม่มีเสียงต่อ tick** และ loop ทุกตัวถูกปิดใน `SetupStep/Hide/Cancel/PointerUp`
- **ตรวจ:** typecheck offline = baseline เดิม (ไม่มี error นอก `CS0246/CS0234`); Unity batchmode compile **0 error** ในสำเนาโปรเจกต์ (`Logs/compile-minigame-sfx.log`, exit 0); `SfxCheck` (`-executeMethod` เปิด `Resources.Load` จริง) = `[SFXCHK] clips=38 missing=0 tracks=4 music=True` → `SFX CHECK PASSED` (`Logs/sfx-check.log`)
- **ยังไม่ได้ยินเสียงจริงใน Play Mode** — editor เดิมยังถูกล็อกโดย Unity PID 14820 (ต้องปิดตัวนั้นก่อนจึงจะรัน play-mode check ได้)
- **ยังเหลือของเฟส 5 (#47):** คราบฉุกเฉิน **T16** — ต้องมี enemy row ใน HUD, นาฬิกา 25 วิ, มินิเกมย่อ D−1 และกติกา "ไม่ทัน = ลาม +short 1" ซึ่งยังไม่มีโครงใน `TaskRaceRound` เลย (แถบคู่แข่งทำแล้ว)

### 15.19 APK ที่มีทั้งเสียงและ Task Race (16 พ.ย. 2026)

build แรกหลังงาน pure UGS + SFX — สั่งด้วย `Bob.EditorTools.BobAndroidBuild.BuildApk` จาก**สำเนาโปรเจกต์** `compile-check/` เพราะ editor ตัวจริงถูกล็อกโดย Unity PID 14820 (batchmode ค้างตั้งแต่ 03:53) ทั้งที่ไฟล์สคริปต์/asset ชุดเดียวกัน

- **ผล build:** `Bob: APK build Succeeded, 0 errors, 886 MB -> C:\Users\Jakgrits\BOB-repo\Builds\BOB-prototype-v1.apk` (log `compile-check/Logs/apk-build.log`, exit 0, 21 นาที — IL2CPP 1,738 objects + link + Gradle)
- **aapt2 badging:** `package com.bob.battleofthebrooms versionCode=2 versionName=0.1.0`, `targetSdk 36`, `native-code arm64-v8a`, `INTERNET` permission, label `Battle of the Brooms`, launchable `com.unity3d.player.UnityPlayerGameActivity`; ขนาดไฟล์ 93,393,738 bytes (89.1 MiB) จากเดิม 78,642,203 bytes (75.0 MiB)
- **ลายเซ็น:** `CN=Android Debug` (SHA-256 `91a710ef…71e0`) — debug keystore ตามที่ `BobAndroidBuild` ตั้งไว้ ใช้กับ App Distribution ได้ แต่ Play Store ไม่ได้
- **ทำไมโตจาก 75 MB:** `libil2cpp.so` 34.8 → 54.4 MB และมี `global-metadata.dat` 10.1 MB เพิ่มมา — โค้ดที่เพิ่มจากชุด pure UGS (Netcode/Relay/Lobby/Cloud Save/Authentication) ไม่ใช่ asset หลุดเข้ามา (ไม่มีไฟล์เดี่ยว > 8 MB อื่นเลย)
- **สำรองของเก่า:** `Builds/BOB-prototype-v1-prev.apk` (75 MB, 15 พ.ย. 21:09) — `Builds/` ถูก gitignore
- **อัปโหลด Firebase App Distribution สำเร็จ (16 พ.ย. 2026):** auth ของ CLI ตายเพราะ refresh token หมดอายุ (`apps:list` = 401 `invalid_grant`) — แก้ด้วย `firebase -i login --reauth` (flag `-i` จำเป็นเมื่อ stdin ไม่ใช่ TTY, token ใหม่มาแล้ว) แล้ว `bash distribute-android.sh --upload-only` ได้ release **`0.1.0 (2)`** id `1df3mfumitkpo` (ไม่ชนกฎห้ามซ้ำ versionCode เพราะ v2 ยังไม่เคยขึ้น)
  - console: `https://console.firebase.google.com/project/bob-preprod/appdistribution/app/android:com.bob.battleofthebrooms/releases/1df3mfumitkpo`
  - tester app: `https://appdistribution.firebase.google.com/testerapps/1:438462393037:android:76c94b4645660e52e69784/releases/1df3mfumitkpo`
  - ลิงก์โหลดตรง (หมดอายุ 1 ชม.): `.../releases/1df3mfumitkpo/binaries/df6d7dae.../app.apk` — ตรวจแล้ว `http 200`, 93,393,738 bytes ตรงกับไฟล์ใน `Builds/`
  - **ขั้นส่งถึง tester group ยัง 404:** โปรเจกต์ `bob-preprod` ยังไม่มี tester/group เลย (`appdistribution:testers:list` ว่าง) แต่ `.env` ระบุชื่อ group ไว้ → ต้องสร้าง group ใน Console ก่อน หรือปล่อย `FIREBASE_GROUPS` ว่างจนกว่าจะมี tester
  - ทางสำรองที่ยังใช้ได้: service account key ที่ `BOB-repo/firebase-key.json` (gitignored) + Docker path ของสคริปต์

### 15.20 Progression pass — ทุกรอบจ่าย, ดาวเปิดบท, บันได 1–6 (16 พ.ย. 2026)

ที่มา: ผล QA วัดได้ว่าซีรีส์หนึ่งจ่ายแค่รอบที่ 3 (รอบ 1–2 ที่ลงแรงไปหายไปทั้งหมด) และบันไดเดิม 1/6/13/21/31/41 ต้องเคลียร์ 78 รอบ ≈ 7.4 ชม. กว่าจะเห็นบท 6 ทั้งที่บทหนึ่งใช้ ~5 นาที

- **รางวัลทั้งบท:** `StorySeries` เก็บ `TotalLocalTasks/TotalRivalTasks/TotalLocalPoints/TotalRivalPoints` + `RoundsPlayed`; `TaskRaceModeManager.EndMatch` ใช้ยอดรวมเหล่านี้เป็น `rewardTasks/rewardPoints` ของ `BobTaskRaceProfile.Record` (PvP รอบเดียวยังใช้ยอดของรอบนั้น) — รอบที่แพ้ก็ยังจ่ายงานที่เก็บได้
- **โบนัสเคลียร์ครั้งแรก:** `BobTaskRaceProfile.FirstClearBonus = 100` XP จ่ายครั้งเดียวต่อบท ผ่าน `BobChapters.Cleared(order)` / `MarkCleared(order)` (`bob.story.cleared.<order>`); รีเพลย์ได้อัตราปกติ. ถ้าซีรีส์จบโดย `localTasks == 0` ทั้งบท จ่าย 0 เหรียญ/0 XP (กันการฟาร์มด้วยสามรอบว่าง)
- **กติกาชนะบท:** `LocalTookSeries => LocalWins >= winsToTake` — เดิม `LocalWins > RivalWins` ทำให้ 1–0 + เสมอ 2 รอบถูกนับเป็น "ผ่านบท" และได้ดาว 1–3 ดวง ทั้งที่ชนะรอบเดียว; ตอนนี้ต้องชนะ 2 รอบเท่านั้น และ `CleanSweep` อิง `LocalTookSeries`
- **บันไดและดาว:** asset บท 2–6 เปลี่ยน `unlockLevel` เป็น 2/3/4/5/6; `BobChapters.Unlocked` เพิ่มเงื่อนไขดาวบทก่อนหน้า ≥ `StarsToOpen = 2`; `LevelFromXp` clamp ที่ `MaxLevel = 50`. ภายใต้นโยบายนี้ เส้นเรื่องครบ 6 บทใช้ 9 เคลียร์ (~51 นาที)
- **UI:** `NextNote` แยกข้อความ LV ไม่ถึง กับดาวไม่ครบ (`ต้องมีดาวบท n ครบ 2 ดวง · ตอนนี้ k/2`); S13 veil/ป้าย/toast บอกด่านที่ติดจริง; ชิป Home/S13 เปลี่ยนจาก `bob.taskbest.<band>` (คะแนนมินิเกมล็อบบี้เดิมที่ไม่เคยถูกเขียนในโหมดนี้) มาเป็น `bob.taskRace.best` และอ่าน `D{n} · 3 รอบ · ดีสุด X`
- **บั๊กสภาพรอบที่แก้พร้อมกัน:** ปุ่มหยุดพักถูกปฏิเสธระหว่าง 3-2-1 (`CanPause`); `TaskSpot.TryOpen` ปฏิเสธช่วงคานต์ดาวน์/ผลจบ และปฏิเสธจุดของเกาะคู่แข่ง (ข้อความ `จุดนี้ของฝั่งคู่แข่ง`); รีไววัล/บอทไม่ล้างการ์ดที่ผู้เล่นถืออยู่ (`ForceComplete` คืน false แล้วไปเลือกจุดใหม่); `HideCard` ปล่อย `TASK_LOCK` เมื่อการ์ดถูกปิดด้วยเส้นทางอื่น; ฮีโร่กลับจุดเกิดทุกครั้งที่เริ่มรอบ (`GoHome`, มี guard ไม่ให้ย้ายก่อน `Start`); ปุ่ม × ใหญ่ 160u และเลื่อนนาฬิกาในการ์ดไปทางซ้าย 40u ไม่ทับกัน; ชิปคำใบ้สั้นลงเป็น `กด × เพื่อปิด · งานค้างไว้ 10 วิ`
- **ตรวจ:** `unity-check` เพิ่ม 8 assertion ของ `StorySeries` (1–0+เสมอไม่ชนะ, ดาว 0, 2–0 ขาด 3 = 3 ดาว, 2–1 = 1 ดาว, ผลรวมทุกรอบ, reset, margin 3) → `UNITY-CHECK PASSED`; `bob-check` = modules sync + task-race 39/39 + LINE + typecheck 40 known kinds; `Editor/ChapterResultCheck` และ `Editor/StorySeriesCheck` อัปเดตความคาดหวังเป็นกติกาใหม่ (ยังต้องรันใน Unity)
- **ยังไม่ทำ:** เอฟเฟกต์ตัวเลขรางวัล/ดาวร่วง, ดาวรวมบน Home, ปรับ `bob.taskbest.*` ของมินิเกมล็อบบี้ให้เลิกใช้ชื่อซ้ำ

### 15.21 S02 ล็อกอินเกสต์จริง — UGS anonymous ก่อนถึง Home (16 พ.ย. 2026)

ที่มา: การย้ายมา pure UGS (commit `01c960f`) ลบหน้าจอ S02 ไปพร้อมกับ `Scripts/Net/` และ `Scripts/Coop/` แต่ `BobE2EDriver` ยังกดปุ่ม `เล่นเลย` อยู่ — ทางเข้าเรื่องจึงข้ามด่านล็อกอินไปเลย และ `bob.guest` ไม่มีใครเขียนนอกจากตัว E2E เอง

- **Boot flow:** `BootFlow` ยังโชว์ Splash 1.6 วิ แล้วอ่าน `bob.guest` — `0` → `Phase.Login` (S02), `1` → `Phase.Title` (Home) ตามเดิม. `Phase.Login` ถูกเพิ่มใน enum และ `RenderScreen` (`BobGame.Hud.cs`)
- **หน้าจอ (`BobGame.Menus.cs:127`):** panel กลางจอ 780u บนภาพ `lobby_condo` — โลโก้, `ยินดีต้อนรับนักปัดกวาด`, คำอธิบายสั้น, ปุ่ม `เล่นเลย` / `กำลังเชื่อมต่อ…` / `ลองอีกครั้ง`, และบรรทัดเล็ก `บัญชีจริง (Google / Apple) จะมาในเวอร์ชันถัดไป`
- **การล็อกอิน (`GuestSignIn`, บรรทัด 165):** ถ้า `UgsBootstrapManager.Instance` เป็น null จะสร้าง GameObject `UGS` เองพร้อม `signInOnAwake = false` (ซีน `Game`/`TaskRace` ไม่มีตัวนี้ฝังอยู่) — กันไม่ให้ `Start` ของ manager แย่งล็อกอินกับหน้าจอ; รอ settle 3 วิถ้า manager กำลังล็อกอินอยู่แล้ว, ไม่พร้อมจึงเรียก `KickoffAsync()` พร้อมเพดาน 10 วิ
- **สำเร็จ:** `EnterAsGuest` เขียน `bob.guest=1` + `bob.playerId` แล้วไป `Phase.Title`; **ล้มเหลว:** `signInFailed = true`, ปุ่มกลายเป็น `ลองอีกครั้ง`, แสดง `LastError` แบบสั้น (ตัด 48 ตัวอักษร) และเพิ่มปุ่ม `เล่นแบบออฟไลน์` ที่เข้า Home ได้ทันที (เขียน `bob.guest=1` ด้วย กันหน้าจอกลายเป็นกำแพง) — เนื้อเรื่องคนเดียวไม่ต้องพึ่งเน็ต
- **E2E:** `BobE2EDriver` กด `เล่นเลย` แล้วรอปุ่ม `เล่น` ของ Home; ถ้าระหว่างนั้นเจอ `เล่นแบบออฟไลน์` (เครื่องใน CI ไม่มีเน็ต) จะกดปุ่มนั้นแล้วไปต่อ — log `UGS unreachable: pressed "เล่นแบบออฟไลน์"`. `MenuTour` เพิ่มภาพ `m2-login`
- **Typecheck:** `unity/Assets/Scripts/UGS/**` ถูกตัดออกจาก `BobTypeCheck.csproj` เพราะไม่มี SDK บน NuGet ฉะนั้น `Game` ที่อ้าง `UgsBootstrapManager` ต้องมี shim — เพิ่ม `namespace Bob.Ugs` ใน `unity-typecheck/Shims.cs` (เฉพาะ `Instance/signInOnAwake/IsReady/PlayerId/LastError/KickoffAsync/EnsureReadyAsync`) ตามกฎ "mirror เฉพาะที่โค้ดแตะ"
- **ตรวจ:** `bob-check` = modules sync + task-race 39/39 + LINE + `typecheck ok: 40 known kinds` → `BOB-CHECK PASSED`; `unity-check` = `UNITY-CHECK PASSED`; ยังไม่ได้รันใน Unity Editor (หน้าจอ/ปุ่มจริงยังต้องถ่ายภาพยืนยัน)
- **ยังไม่ทำ:** ภาพ `screen_login` ซ้าย/ขวาตามสเปก, ปุ่ม Google/Apple จริง, หน้าผูกบัญชี (S03/S04), การเตือนผูกบัญชีหลังแมตช์ที่ 3, และการโหลด cloud profile อัตโนมัติเมื่อกลับเข้าเกมจาก session เก่า

### 15.22 โหมด Solo / Duo / Team เล่นได้จริง — หน้าจอเลือกทีม + สัญญาระหว่างซีน (16 พ.ย. 2026)

ที่มา: ซีน `TaskRace_Duo.unity` / `TaskRace_Team.unity` ถูกเบคและลงทะเบียนใน `EditorBuildSettings.asset` มาตั้งแต่ 14 พ.ย. แต่ **ไม่มีโค้ดไหนโหลดมันเลย** — S11 (`Phase.Modes`) เข้าได้จาก dev tour อย่างเดียว และ `Team` ในโหมดเก่าเป็นเพียงป้ายชื่อ งานรอบนี้คือทำทางเข้าให้ครบ: Home → S11 → S14 → ฉากโหมดนั้น

- **สัญญาระหว่างซีน — `Game/MatchRequest.cs` (ใหม่):** static `Has/ModeId/Players/Series/Helpers` + `Set(...)` / `Story()` เพราะ `SceneManager.LoadScene` ล้าง state ของซีน ไม่มีอะไรส่งต่อได้นอกจาก static (ไม่ใช้ PlayerPrefs เพื่อไม่ให้ค่าค้างข้ามการเปิดเกม หรือตอนกด Play ใน Editor ตรง ๆ ซีนจะเห็น `Has = false` และใช้ค่าที่เบคไว้เอง)
- **S11 (`Menus.cs:474`):** `Modes` 6 ใบมี `Scene`, `ListPlayers`, `Series`, `Team` — Story `TaskRace`/1/ซีรีส์, Solo `TaskRace`/1, Duo `TaskRace_Duo`/2, Team `TaskRace_Team`/3, Party/Community ล็อกพร้อมเหตุผล · `PressMode` (`:504`) ปล่อยการ์ดล็อกเป็น toast, Story → S13, ที่เหลือ → S14 · เข้าถึงได้จากปุ่ม `โหมด · Solo / Duo / Team` บน Home (`Menus.cs:359` บริเวณ `PressPlay`), `PressPlay` เองเรียก `MatchRequest.Story()` แล้ว `LoadMatchScene("TaskRace")` · `LoadMatchScene` (`:371`) เช็ก `CanStreamedLevelBeLoaded` ก่อน แล้วค่อย `LoadScene`; ถ้าไม่มีฉากจะขึ้น toast แทนการค้าง
- **S14 (`Menus.cs:564` `TeamSetup`):** ห้องออฟไลน์ ไม่มีเน็ต/รหัสห้อง — ช่องทีม 1–3 ช่อง, ช่องเพื่อนแตะเพื่อสลับฮีโร่ (`CyclePartner`), การ์ดเลือกฮีโร่ 4 ใบ, การ์ดสรุปขวาบอก **จำนวนงานของทีมจาก `TaskRaceRound.CompositionFor` ตัวเดียวกับที่รอบจริงใช้** (จึงไม่มีทางที่การ์ดกับแมตช์พูดไม่ตรงกัน), นาฬิกา/เวลาสะพานจาก `BobStages.Get(chapter.Order)`, ปุ่ม `2 พบ 2`/`3 พบ 3` (Team), `เริ่มแมตช์` · `StartPendingMatch` (`:656`) เขียน `MatchRequest.Set(m.Id, m.Team ? slots : m.ListPlayers, m.Series, helpers)` แล้วโหลด `m.Scene`
- **ฝั่งฉาก — `TaskRace/TaskRaceModeManager.cs`:** `Awake` เรียก `ApplyMatchRequest()` (`:196/211`) **ก่อน** `ApplyChapter()` เพื่อให้ `list.ConfigureRound(MatchRequest.Players, list.Difficulty, seed)` มีผลกับกติกาบท (`storySeries = MatchRequest.Series`) · `Start` (`:304`) ถ้ามี `Helpers` เรียก `BobTaskRaceTeammates.Spawn(helpers, localTeam)`; log `[TaskRace] lobby pick: …`
- **เพื่อนบอท — `TaskRace/BobTaskRaceTeammates.cs` (ใหม่):** สร้าง `TaskRaceBotController` บนเกาะเรา 1–2 ตัว ใช้พรีแฟบ `Characters/SK_hero_<id>` (fallback `sweep`, ไม่มีพรีแฟบ = แคปซูล), `pace = 2.2` (คู่แข่ง 1.12), `headStart 6/8 วิ`, `buyShield = true`, `raidEnemy = false` — เพื่อนเก็บงานฝั่งเราเท่านั้น ไม่ข้ามไปป่วน และไม่แย่งงานผู้เล่นเร็วเกินไป. บอทใช้ `round.DoneCountOf(team)` / `manager.PointsOf(team)` / `manager.TryPurchaseShield(team, spot)` จึงนับคะแนนและปิดรอบร่วมกับผู้เล่นได้จริง
- **บอททั่วไป — `TaskRaceBotController`:** เพิ่ม `team` + `raidEnemy`; ทุกจุดที่เคยฮาร์ดโค้ด Team B เปลี่ยนเป็น `team`/`OtherTeam` (`DoneCountOf`, `TrySabotageAs`, `StationsOf`) — บอท Team B เดิมในซีน Solo ยังทำงานเหมือนเดิม (`team = TeamB`, `raidEnemy = true`)
- **ไม่แตะเนื้อเรื่อง:** Solo/Duo/Team ส่ง `Series = false` → `storySeries = false` → `series == null` → `EndMatch` ไม่เขียนดาว/`MarkCleared` และ HUD ใช้การ์ดผลแมตช์ปกติ (`BuildMatchCard`) · reward ยังจ่ายผ่าน `BobTaskRaceProfile.Record` ตามยอดรอบเดียว
- **ขนาดทีมจริง:** 2v2 → `ConfigureRound(2, …)` = 7 งานบน 6 สถานี/ฝั่ง; 3v3 → 3 = 9 งาน (ตาราง §13/§14) ฉะนั้นสวิตช์บน S14 เปลี่ยนภาระงานจริง ไม่ใช่แค่ข้อความ
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors` (`FindAnyObjectByType`, `Resources.Load<GameObject>`, `ShadowCastingMode`, `CompositionFor(... out …)` และ `Bob.TaskRace` อ้าง `Bob.Game.MatchRequest` ผ่านได้ — assembly เดียว ไม่มี asmdef) และ `bob-check` = `BOB-CHECK PASSED` (modules sync + task-race 39/39 + LINE)
- **ยังไม่ทำ:** ไม่ได้เปิด Unity ถ่าย S11/S14/Duo/Team (ภาพยืนยันยังไม่มี), เพื่อนบอทเป็นฮีโร่ล้วน (ยังไม่มีสกิล), บอททุกตัวสมองเดียวกัน, คู่แข่งที่มองเห็นบนเกาะยังเป็น pace model ล่องหน, ห้องออนไลน์จริง + S15 matchmaker ยังเป็นคิว 7b (`art/docs/27` §7)

### 15.23 โหมดออนไลน์ 2–4 คน — seed เดียวกันทั้งห้อง + สถานีที่เก็บร่วม (16 พ.ย. 2026)

ที่มา: §15.22 ทำให้ Solo/Duo/Team เล่นได้แบบออฟไลน์ แต่ยังไม่มีทางเล่นด้วยกันจริง งานคิว 7b คือต่อ UGS Lobby + Relay + Netcode เข้ากับรอบ Task Race โดยไม่แก้ฉากและไม่ทำ prefab ผู้เล่น (สเปกเต็ม: `art/docs/27` §10)

- **`Game/MatchRequest.cs`:** เพิ่ม `Online`, `Host`, `Seed` (รีเซ็ตเป็น false/0 ใน `Set`) และ `SetOnline(bool host, int players, int seed)` — เขียน `ModeId = "online"`, `Series = false`, `Helpers = empty`, `Players = clamp(1..4)` · ทางออนไลน์ใช้ฉาก `TaskRace` ฉากเดียว ไม่ใช้ `TaskRace_Duo`/`TaskRace_Team`
- **seed ล็อก — `TaskRace/TaskRaceRound.cs`:** เพิ่ม `seedLocked` + `LockSeed(int)`;
  `Start()` เปลี่ยนเป็น `StartRound(seedLocked || !randomSeedEachPlay ? seed : Random.Range(1, 100000))` — ไม่งั้น host/client จะสุ่มคนละเลขตอนฉากเริ่ม และใบงานคนละชุด
- **`TaskRace/TaskRaceModeManager.cs`:** `ApplyMatchRequest` เลือก `seed = MatchRequest.Seed > 0 ? Seed : list.ConfiguredSeed` แล้ว `ConfigureRound(MatchRequest.Players, Difficulty, seed)` + `LockSeed(seed)` เมื่อเป็นออนไลน์; log ต่อท้าย `online host/client seed N` · `Start()` เรียก `BobTaskRaceOnline.Attach()` เฉพาะเมื่อ `MatchRequest.Online` (โหมดออฟไลน์ไม่สร้าง component นี้เลย)
- **ห้อง — `UGS/BobOnlineRoom.cs` (ใหม่):** host สร้าง Relay allocation (`maxPlayers - 1` ช่อง เพราะ host กินหนึ่ง), เก็บ join code ใน lobby data (`RelayJoinCode`), client เข้าด้วยโค้ด 6 ตัวหรือ quick join · `StartMatch(seed, players)` ส่ง `bob.start` (reliable) ให้ทุกคนแล้วโหลดฉาก; `SendPose` 10 Hz (unreliable), `SendTask` (reliable) · ข้อความผูกกับ `CustomMessagingManager` ไม่มี NetworkObject/prefab/scene sync
- **host เป็นผู้ตัดสิน + relay:** client ส่ง `bob.pose`/`bob.task` ถึง host คนเดียว; host apply แล้ว rebroadcast พร้อม **origin id ใน payload** (ถ้าใช้ client id บนสาย 3–4 คนจะถูกยุบเป็นตัวเดียว) และผู้รับข้าม event ที่ `origin == LocalNetId` เพื่อไม่ apply ซ้ำ
- **ฉาก — `TaskRace/BobTaskRaceOnline.cs` (ใหม่):** รับ `PoseReceived` → สร้าง/ขยับร่างระยะไกลด้วย `BobTaskRaceTeammates.Avatar(heroId)` (lerp 12), poll `round.StationsOf(team)` 4 Hz แล้วส่ง `bob.task` เมื่อ `Done` เปลี่ยน false→true; ฝั่งรับเรียก `spot.ForceComplete()` และจำ `knownDone` ไม่ให้ส่งกลับ · สถานีที่สะอาดตั้งแต่เฟรมแรกเป็น baseline ไม่ใช่ event
- **ขนาดรอบ:** `Players` ของห้อง (2–4) ไหลเข้า `CompositionFor` ตามตาราง §13/§14 — 2 คน = 1 common/2 long/4 short, 4 คน = 1/3/7 (หัก short ตาม D2/D3) ทุกเครื่องได้เลขชุดเดียวกันเพราะ seed เดียวกัน
- **การ์ดผล:** `TaskRace/BobTaskRaceHud.cs` — ออนไลน์ซ่อน `เล่นอีกครั้ง` และขึ้น `ออนไลน์ · กลับเมนูแล้วสร้างห้องใหม่เพื่อเล่นอีกครั้ง`; `BackToLobby` เรียก `room.LeaveAsync()` ก่อนกลับ · ไม่มีการส่งผลจบข้ามเครื่อง แต่ละเครื่องจบเมื่อรายการฝั่งตัวเองครบ/หมดเวลา
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors` (`UGS/**` ถูกตัดจาก csproj อยู่แล้ว; `BobOnlineRoom` มี shim ใน `unity-typecheck/Shims.cs`) และ `bob-check` = `BOB-CHECK PASSED`; ยังไม่ได้เปิด Unity ถ่ายหน้าจอ S15/สองเครื่อง
- **ยังไม่ทำ:** สองทีมจริง/คะแนนรวมบนเซิร์ฟเวอร์ (ตอนนี้ co-op บนเกาะ host แข่งคู่แข่งจำลอง), แอนิเมชัน/สกิลของฮีโร่คนอื่น, reconnect / host migration / late join, ผลแมตช์ข้ามเครื่อง, และ S16 เลือกตัวละครในแมตช์ (ออนไลน์ใช้ฮีโร่จาก S14)

### 15.24 ถ้อยคำเนื้อเรื่องบนการ์ดด่านและการ์ดจบบท (16 พ.ย. 2026)

ที่มา: `art/docs/29_Story_Premise.md` ร่างเนื้อเรื่อง 6 บทไว้แล้ว แต่ยังไม่ลงเกม — ข้อ 7 ของไฟล์นั้นระบุงานเล็กสามข้อ (บรรทัดเรื่องใต้ชื่อบท / บรรทัดจบบท / บรรทัดจบเกม) รอบนี้ลงทั้งสามข้อเป็นข้อความล้วน ไม่แตะกติกา ไม่แตะฉาก

- **`Rules/ChapterDefinition.cs`:** เพิ่ม `storyLine` + `clearedLine` (มี getter `StoryLine`/`ClearedLine`, คืน `""` เมื่อยังไม่เติม) — asset 6 ไฟล์ใน `Resources/Chapters/` เติมตามข้อ 5 ของ docs 29 (มือ, ไม่มี builder)
- **S13 (`Menus.cs` `ChapterSelect`):** `storyLine` เป็นบรรทัดที่ 2 ใต้ชื่อบท (22pt, `enableAutoSizing` 18–22, สีจางเมื่อบทล็อก) · การ์ดสูง 330 → **366** และแถว place/แท็ก/การ์ดกติกาขยับลงตาม (366 ยังเว้นจาก TopBar 19u และเส้นโน้ตล่างจอ)
- **การ์ดจบบท (`BobTaskRaceHud.BuildChapterCard`):** การ์ด 840 → **900**; ถ้าผ่านบทจะแสดง `clearedLine` ของบทนั้นที่ band ใหม่เหนือบรรทัดบทถัดไป (ไม่แสดงเมื่อแพ้/เสมอ) · บท 6 (ไม่มีบทถัดไป) ใช้ band นั้นกับสามบรรทัดจบเรื่อง `BobChapters.FinaleTitle` / `FinaleLine` / `FinaleNote` แทน "จบเนื้อเรื่องหลักแล้ว"
- **กันการ์ดว่าง:** `BobChapters.Validate()` เพิ่มข้อผิดพลาดเมื่อบทไหนไม่มี `storyLine`/`clearedLine` และ `Bob/Chapters/Check Ladder` พิมพ์ทั้งสองบรรทัดใน log
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors`, `bob-check` = `BOB-CHECK PASSED`, `unity-check` = `UNITY-CHECK PASSED`; ยังไม่ได้เปิด Unity ถ่าย S13/การ์ดจบบท/การ์ดจบเกม
- **ยังค้าง:** คำถาม 7 ข้อใน docs 29 §6 (ชื่อย่าน, คุณนภ, ป้าแหวว, ราชาคราบพูด, ใบ้ซีซัน 2, ความยาวข้อความ, คำขวัญ "งานสะอาดต้องทำสามรอบ") — ถ้อยคำที่ลงคือร่าง แก้ได้ที่ asset; บทพูด 2–3 บรรทัดแบบการ์ดบทสนทนายังไม่ทำ (ต้องหน้าจอใหม่)

### 15.25 คราบฉุกเฉิน T16 — กติกา + แบนเนอร์แดง + งานลาม (16 พ.ย. 2026)

ที่มา: docs 16 ข้อ 4 และแถว T16 ถูกเขียนเป็นสเปกไว้ตั้งแต่ต้น แต่ในโค้ดไม่มีอะไรเกิดขึ้น — ไม่มีศัตรู ไม่มีนาฬิกา 25 วิ ไม่มีแถว `คราบลาม`. รอบนี้ทำตามสเปกเป็น v1 โดย **ไม่เบคซีน** (HUD สร้าง component เองตอนเล่น) และ **ไม่แตะ `server/`**

- **`TaskRace/BobEmergencyStains.cs` (ใหม่):** ตารางจังหวะมาจาก `BobTaskRaceRules.EmergencyTimes(seed)` — 2–3 จังหวะที่ 40/75/100 วิ ของรอบ 120 วิ; รอบบทสั้นกว่า 120 วิ ย่อตามสัดส่วน (`roundSeconds / 120`). จุดงานเลือกด้วย `System.Random(seed * 997 + 17)` จาก `round.StationsOf(TaskSpot.LocalTeam)` และไม่ให้ซ้ำจุดเดิมติดกัน. `Ensure(round, manager)` ผูกกับ `MatchStarted`/`MatchEnded`; `elapsed` เดินเฉพาะเมื่อ `!Paused && !IsCountingDown && !IsMatchOver && !LastResult.HasValue` นาฬิกา 25 วิจึงหยุดตามเกมจริง
- **ศัตรูในโลก:** พรีแฟบ `Resources/Characters/SK_boss_stain` (เจ้าคราบ) / `SK_enemy_slime_green` (สไลม์) / `SK_enemy_slime_mud` (ขยะปีศาจ) วางข้างสถานี 1.35u, ปรับสเกลด้วย `Renderer.bounds` ให้สูง 1.15u และวางเท้าบนพื้น (บทเรียนเดียวกับ §15.10: root ของ rig มีสเกลติดมา ห้ามแทนที่). เด้งขึ้นใน 0.35 วิ + หายใจ 3.5%, ตอนจบหดหาย 0.35 วิ
- **จุดอ่อนตามบท:** `WeaknessPool()` — เจ้าคราบ (T02) ทุกรอบ; สไลม์ (T03) ตั้งแต่บท 2 หรือรอบ 2 ของบท; ขยะปีศาจ (T05) ตั้งแต่บท 2. การ์ดใช้ `BobMinigameHost.BeginEmergency(id, D-1)` — play area เดิม แต่ **ไม่มีสถานี** (`emergency = true` ทำให้ `Tick` เล่นได้โดย `spot == null`); T05 ได้ 4 ใบ (`SetupSort` ใช้ `emergency && taskId == "T05"`)
- **`BobMinigameHost.BaseId`:** แถวงานลามมี id `T02~3` เพื่อไม่ชนกับแถวเดิมใน `entryOf`; ตัวการ์ดตัด suffix ก่อนเข้า `KindOf`/`StepsOf`/`HintFor`/`BobTutorialClip` และ `Begin(...)` ก็ใช้ `BaseId` เช่นกัน
- **HUD — `BobTaskRaceHud`:** แบนเนอร์ `Emergency` ล่างกลาง 720×132 (แถบแดง 8u, ชื่อ `คราบฉุกเฉิน · <ศัตรู>`, บรรทัด `เหลือ n วิ · <จุดงาน>`, แถบ 25 วิ, ปุ่ม `ปราบ` 160×86) ซ่อนเมื่อไม่มีการ์ด/ผล/หยุด. `CardOpen` นับ `cardEmergency` ด้วย; `TickEmergencyCard` เดินมินิเกมและตัดจบเมื่อ `stains.Active == false` (หมดเวลาระหว่างเปิดการ์ด) — ปิดการ์ดแล้วคราบยังอยู่ ไม่ใช่การหนี
- **งานลาม — `TaskRaceRound.AddPenaltyTask(baseSpec, team, name)`:** เพิ่ม `TaskEntry` + `side.Remaining` + `TaskCount = tasks.Count` + `TotalWeight += 1` + `Version++` → แถวใหม่ขึ้น `รอคิว` ทันที และสถานีจะรับไปทำตอน restock รอบถัดไป (ทุกเลนมีงานมากกว่าสถานี การรอคิวจึงมีจริง); แถบความคืบหน้าถอยเพราะตัวหารโต (docs 16 §2.4)
- **เสียง:** `enemy_spawn` + `timer_warning` ตอนโผล่, `enemy_defeat` ตอนจบทั้งสองทาง (`sting_emergency` ไม่มีไฟล์)
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors`; `bob-check` = `BOB-CHECK PASSED` (modules sync + task-race 39/39 + LINE + typecheck); `cd unity-check && dotnet run` = `UNITY-CHECK PASSED` (`EmergencyTimes` เป็นของเดิมที่เทสต์อยู่แล้ว)
- **ยังไม่ทำ:** บอสคราบยักษ์ (2 คน T02+T03 พร้อมกัน), ลูกศรขอบจอ `ARROW-Edge`, ภาพศัตรูในการ์ด, แถวรายการแบบ "ขอบแดง + วงนับถอยหลังในแถว" ของ docs 11 (ใช้แบนเนอร์แทน), จำนวน 2/2/3 ตาม D (จำนวนมาจาก seed), ดาวคุณภาพ "จัดการฉุกเฉินทันทุกตัว" ใน `StorySeries.Stars`, การซิงก์ออนไลน์ของสถานะคราบ (แต่ละเครื่องรันเกาะตัวเองด้วย seed เดียวกัน — ยังไม่มี `bob.emergency`), และยังไม่ได้เปิด Unity ถ่ายแบนเนอร์/ศัตรูในฉาก

### 15.26 รถเข็น T04 และเก้าอี้ T12 — มินิเกมจริงของสองงานสุดท้าย (16 พ.ย. 2026)

ที่มา: ใน `BobMinigameHost` สองงานนี้เคยเป็น `MiniKind.Timed` — เปิดการ์ดแล้วได้แถบเวลามีข้อความ "จัดรถเข็นในห้อง แล้วกลับมาล็อกสูตร" เฉย ๆ ไม่มีกลไกอะไรเลย. รอบนี้ทำทั้งสองตัวตาม docs 16 §4 (T04) และ §5 (T12) โดย **ไม่แตะฉาก** และ **ไม่แตะ `server/`**

- **T04 — การ์ด 2 ขั้น (`MiniKind.Cart`):** ขั้น 1 `SetupCartShelf` ปูชั้นวางของ 6 ชิ้นจาก `BobRules.Parts` (seed = `SpotId.GetHashCode()`, อย่างน้อย 4 ชิ้นเป็นของจริงไม่ใช่วิธีใช้) แตะหยิบ 3 ชิ้นตามสเปก; ขั้น 2 `SetupCartBuild` มีรถเข็น 5 ช่อง (`BobRules.MaxLayers`), ชิปที่หยิบมา 3 ใบลากลงรถเข็นได้, กดชั้นบนสุดเพื่อเอาออกได้, และ **กดค้าง "ล็อกสูตร" 0.6 วิ** (`cartHold`) ตามสเปกขั้น 2
- **การตัดสินสูตร** อยู่ใน pure C# ไม่ใช่ใน UI: `BobTaskRaceRules.CartNeed(seed, D)` คืนค่าที่ต้องผ่าน 4 ตัว (ค่าดาวของรอบสูงกว่าอีกสามตัว และ +1 ทุกขั้น D), `CartPassCount(stats, need)` นับเงื่อนไขที่ผ่าน, `CartPassed(pass, D)` = ผ่าน 1/2/3 เงื่อนไขตาม D, `CartBuffs(stats, need)` คืนคีย์บัฟ — ค่าดิบของการ์ดมาจาก `BobRules.EvalStack` ตัวเดียวกับที่ server ใช้ (`unity-check` เทสต์ตารางนี้อยู่แล้ว; เพิ่มเคสของ T04 เข้า `SelfTest`)
- **บัฟ:** `TaskRaceRound.NoteCartResult(stats)` เรียกครั้งเดียวต่อรอบ (การล็อกครั้งแรกคือครั้งที่ตัดสิน) เก็บ `CartPassed` / `CartPassCount` / `CartBuffs` และลงมือกับตัวที่ระบบรองรับจริง: `speed` → `BobTaskRaceHero.speedScale = 1.1` (เดินเร็วขึ้น 10% ถึงจบรอบ, ถูกรีเซ็ตใน `StartRound`) — อีกสามคีย์ถูกเก็บเป็นธงให้ระบบที่จะมาใช้ทีหลัง (ขจัดคราบ → T02/T16, ถนอม → T03, เงา → ดาวคุณภาพ)
- **T12 — ในโลก ไม่มีการ์ด (`TaskRace/BobChairTask.cs` ใหม่):** `TaskRaceRound.AssignTo` เรียก `BobChairTask.Attach(spot, players)` เมื่อสถานี **ฝ่ายเรา** ได้การ์ด T12. ตัว component สร้างเก้าอี้ล้มจาก `Props/SM_chair_bentwood` 2–4 ตัว (จำนวน = จำนวนผู้เล่น, ทีม 1 คน = 2 ตัวตามน้ำหนัก 2) วางรอบสถานี 2.45–2.9u พร้อมวงสีทีมที่พื้น + ช่องวางเป็นวงบาง (`LineRenderer` 40 เหลี่ยม) ที่ 1.75u หน้าสถานี
- **วงกลม/ช่อง ต้องอิงพื้นโค้ง:** ทุกวงใช้ `BobCurvedWorld.Drop(p)` เหมือนวงจุดงานของ `TaskSpot` (บทเรียน §15.10/4.3) ไม่งั้นวงจะลอยหรือจมเมื่อกล้องเบนโค้ง; เก้าอี้วางเท้าด้วย `Renderer.bounds.min.y` ไม่ใช่ `position.y`
- **กดปุ่มหลักแทนการเพิ่มปุ่ม:** `BobChairTask.PromptFor(near, out label, out can)` + `TryPress(near)` ถูกเรียกก่อนเส้นทางปกติใน `BobTaskRaceHud.RenderMain`/`OnMain`. เพราะเก้าอี้อยู่ห่างสถานีเกิน `interactRadius` (1.2u) ปุ่มจึงต้องโผล่ได้ **แม้ `TaskSpot.ActiveNear == null`** — `show = near != null || chairPrompt`. ข้อความ: `จับเก้าอี้` / `ลากเก้าอี้ไปที่โต๊ะ` / `วางเก้าอี้ที่ช่อง` / `เดินไปที่เก้าอี้ (n/N)`; กดที่สถานีเปล่า ๆ จะไม่เปิดการ์ด T12 อีก (toast `เดินไปที่เก้าอี้ที่ล้มอยู่ก่อน`)
- **ระหว่างลาก:** เก้าอี้ลอยที่ `hero.forward * 0.85` + `hero.dragFactor = 0.8` (ช้าลง 20% ตามสเปก) — `BobTaskRaceHero` มี `speedScale` กับ `dragFactor` คูณเข้าความเร็วเดิม (`speed * clamp(speedScale*dragFactor)`) เพื่อให้บัฟรถเข็นกับงานเก้าอี้อยู่ร่วมกันได้; ครบทุกตัว → `spot.ForceComplete()` ผ่านเส้นทาง `Closed` ปกติ (ไม่มี TASK_LOCK) แล้ว component ลบของตัวเองใน 1.1 วิ
- **เสียง:** `cart_grab` ตอนจับ, `cart_roll_loop` loop ระหว่างลาก, `table_put` ตอนเข้าที่, `drop_invalid` ตอนวางไม่ลงช่อง (ทุกไฟล์มีอยู่ใน `Assets/Audio/SFX`)
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors`; `bob-check` = `BOB-CHECK PASSED`; `cd unity-check && dotnet run` = `UNITY-CHECK PASSED` (SelfTest ที่เพิ่มครอบ `CartNeed`/`CartPassCount`/`CartPassed`/`CartBuffs`)
- **ยังไม่ทำ:** ขั้นเดินไปหยิบของที่ชั้นวางในห้องจริงของ T04 (หยิบใน card), งบ 100 ของ S20, บัฟอีกสามตัวที่ยังไม่มีระบบอ่าน, ภาพหน้าฮีโร่บนเก้าอี้, ของรกขวางทางตาม D, ช่องเส้นประสี่เหลี่ยม, ระยะลากตาม D, server ถือตำแหน่งเก้าอี้/ตรวจด้วย `bob_score_stack`, สถิติ `sample` ของทั้งสองงาน, และยังไม่ได้เปิด Unity ถ่ายการ์ด T04 หรือฉากเก้าอี้ T12

### 15.27 T16 เติมเต็ม — จำนวนตาม D + ลูกศรขอบจอ (16 พ.ย. 2026)

ที่มา: v1 ของ §15.25 ยังขาดสองข้อที่ docs 16 ข้อ 4 / แถว T16 เขียนไว้ชัด — "จำนวนศัตรูต่อรอบ 2/2/3 ตาม D" และ "ลูกศรขอบจอ". รอบนี้ปิดทั้งสองข้อ **โดยไม่แตะฉาก** และแก้ `server/` เท่าที่เป็น mirror ของกฎเดียวกัน

- **จำนวนตาม D:** `BobTaskRaceRules.EmergencyTimes(seed, difficulty)` — D1/D2 คืน 2 จังหวะ, D3 คืน 3 จังหวะ ที่ 40 / 75 / 100 วิ ของรอบ 120 วิ (รอบสั้นย่อตามสัดส่วนเหมือนเดิม). ค่าที่เคยมาจาก `rng` ในฟังก์ชันถูกถอดออก เพราะสเปกกำหนดจำนวนตายตัวตาม D; seed ยังอยู่ในลายเซ็นเพื่อให้ server replay และ browser mirror เรียกด้วยอาร์กิวเมนต์เดียวกัน
- **SelfTest:** เพิ่มเช็ก `EmergencyTimes(1975, 1|2|3).Count` = 2 / 2 / 3 และเคสตารางถูกแก้ให้ตรง (seed 1975 D1 = `40,75` แล้ว)
- **mirror ฝั่ง JS:** `prototype/index.html` `taskEmergencyTimes(seed, difficulty)` (D≥3 → 3), `server/match.js` ส่ง `s.difficulty` เข้าไป, `node server/build-module.js` สร้าง `modules/index.js` ใหม่ (ไฟล์ gen — ห้ามแก้มือ)
- **สัญญา server มีเทสต์:** `server/task-race-test.js` export `taskEmergencyTimes` ออกจาก sandbox แล้วเช็กตรง ๆ ว่า D2 ยังได้ 2 และ D3 ได้ 3 (เพิ่มจากเช็กเดิม "2-3 จังหวะ") → `41/41 checks passed`
- **ลูกศรขอบจอ (`BobTaskRaceHud`):** `BuildEmergency` สร้าง `EmergencyArrow` 88×88 บน `page` พร้อม sprite สามเหลี่ยมที่สร้างตอนรัน (`ArrowSprite`, `Texture2D` 64×64) เพราะโปรเจกต์ไม่มีไอคอนลูกศรและฟอนต์ TMP ไม่มีอักขระลูกศร — เลี่ยงปัญหาช่องว่างใน static glyph เหมือน §15.10
- **การวาง:** `RenderEmergencyArrow(show)` อ่าน `BobEmergencyStains.TargetPosition` (=`current.Body.transform.position`) → `Camera.WorldToViewportPoint` → ถ้าอยู่ในกรอบปลอดภัย (`|dx| ≤ 0.40`, `|dy| ≤ 0.38`, z > 0) ซ่อน; ถ้าอยู่นอก/หลังกล้อง (`z < 0` กลับทิศ) คำนวณ `t = min(halfW/|dx|, halfH/|dy|)` โดย `halfW = 960 − 76`, `halfH = 540 − 100` แล้ววางที่ `dir * t` หมุน `atan2 − 90°` ให้ยอดลูกศรชี้ทางคราบ
- **สถานะการซ่อน:** ใช้เงื่อนไขเดียวกับแบนเนอร์ (`stains.Active && !ResultOpen && !PauseOpen && !CardOpen`) จึงไม่ทับการ์ด S28 การ์ดหยุด หรือหน้าผล; กระพริบ alpha 0.72–1.0 ที่ 7 rad/s และเปลี่ยนสีเป็น `danger` เมื่อเหลือ ≤ 8 วิ
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors`; `bob-check` = `BOB-CHECK PASSED` (modules sync, task-race **41/41**, LINE, typecheck); `cd unity-check && dotnet run` = `UNITY-CHECK PASSED`
- **ยังค้าง:** บอสคราบยักษ์ (2 คน T02+T03), ภาพศัตรูในการ์ด, ดาวคุณภาพ "จัดการฉุกเฉินทันทุกตัว", แถวรายการแบบขอบแดง + วงนับถอยหลังในแถวของ docs 11 (ยังใช้แบนเนอร์), การซิงก์ออนไลน์ (`bob.emergency`), ไฟล์ `sting_emergency.wav` และยังไม่ได้เปิด Unity ถ่ายลูกศร/จำนวน 3 ตัวที่ D3

### 15.28 T12 ตาม D (ของรก + ระยะลาก) และดาวคุณภาพฉุกเฉิน (16 พ.ย. 2026)

ที่มา: T12 ลงโค้ดแล้วใน §15.26 แต่ยังคงที่ทุกค่า — ระยะลากเท่ากันทุก D และไม่มีของรก ส่วนดาวคุณภาพของ docs 16 §2.5 ยังไม่ได้นับเลย. รอบนี้ปิดทั้งคู่ **ไม่แตะฉาก**

- **ระยะลากตาม D — `BobChairTask.DragDistance(D)`** = 3 / 4 / 5 หน่วย (D1 / D2 / D3) ตามตาราง T12. วัดจากเก้าอี้ถึงช่อง (`SlotAhead = 1.75u`) รัศมีที่วางเก้าอี้จึงเป็น `max(1.75, DragDistance − 1.75) + jitter 0–0.35` → ทางเดินจริง 3.5 / 4.0 / 5.0 หน่วย (D1 ไม่สั้นกว่านี้ได้เพราะช่องอยู่ 1.75u หน้าสถานีแล้ว)
- **ของรก — `BobChairTask.ClutterCount(D)`** = 0 / 1 / 2 ชิ้น วางข้างสถานี `(±1.45, 0, 0.55)` ใช้พร็อพที่มีอยู่ (`SM_crate_oranges`, `SM_bin_trash_grey`, `SM_cone_traffic`) — **ไม่ใช่ของประดับเฉย ๆ**: ใส่ `BoxCollider` (90% ของ bounds) ให้ `CharacterController` ของฮีโร่ชน และ `NavMeshObstacle` แบบ carve ให้เพื่อนบอทคำนวณเส้นทางอ้อม. วางไว้ข้างสถานีเลย ไม่ปิดทางตรงหน้าช่อง จึงอ้อมได้เสมอ ไม่มีทางตัน
- **ทำไมไม่ปิดทางตรง:** ถ้าบล็อกกลางทางเดิน ฮีโร่ที่แบกเก้าอี้จะติด และบอทที่ถือใบงาน T12 อาจวนอยู่กับที่ (navmesh ไม่ถูกเบคใหม่ตอนรัน) — ของรกจึงวางเป็น "คอขวดข้าง" ที่บังคับให้เดินอ้อม ไม่ใช่กำแพง
- **ดาวคุณภาพฉุกเฉิน — `StorySeries.NoteEmergency(bool cleared)`**: `BobEmergencyStains` เรียกตอน `Clear()` (cleared = true) และตอน `Spread()` (false) ผ่าน `manager.Series`; `EmergencySpawned` / `EmergencyCleared` / `EmergencyClean` (spawned > 0 && cleared == spawned) ใช้กับการ์ดจบบท — บรรทัดเดิม `✓ ชนะบท · ✓ ไม่เสียรอบ · ✕ ชนะขาด 3 งาน` ต่อท้ายด้วย `✓ คราบ 2/2` (มี auto-size 20–26 เพราะบรรทัดยาวขึ้น). กฎดาวบท (`Stars` 0–3) ไม่เปลี่ยน: ดาวคุณภาพเป็นรายงาน ไม่ใช่เงื่อนไขชนะ ตาม docs 16 §2.5
- **ไม่มีคราบ = ไม่มีดาวดวงนี้** (`EmergencySpawned == 0` → `EmergencyClean` false) เพื่อไม่ให้บทที่ไม่เจอคราบได้ดาวฟรี
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors`; `bob-check` = `BOB-CHECK PASSED`; `cd unity-check && dotnet run` = `UNITY-CHECK PASSED` (เพิ่ม 4 assertion ของ `NoteEmergency`/`Reset`: ไม่มีคราบ → ไม่มีดาว, ลาม 1 ตัว → หลุดดาว, ครบ → ได้ดาว, reset → เคลียร์)
- **ยังค้าง:** ภาพหน้าฮีโร่เจ้าของบนเก้าอี้, ช่องเส้นประสี่เหลี่ยม, server ถือตำแหน่งเก้าอี้, สถิติ `sample` (ระยะ/เวลาลาก), ดาวคุณภาพอีกสองดวง (งานไม่ถูกปิดกลางคัน / รถเข็นผ่านสูตร), การเอา `EmergencyClean` ไปผูกกับรางวัลหรือภารกิจ, และยังไม่ได้เปิด Unity ดูของรกกับระยะลากทั้งสามระดับ (ต้องยืนยันว่าเพื่อนบอทอ้อมได้จริง)

### 15.29 UX มือถือของแผนที่ — กล้อง 45°, Quick Ping P03, ส่งเก้าอี้ให้เพื่อน (16 พ.ย. 2026)

ที่มา: README/docs 11 ยังค้างสามข้อของ UX มือถือ — กล้อง 55° (ซึ่งขัดกับมติ C4 ว่า 45°), Quick Ping 6 คำ, และ "ส่งต่อ" ให้เพื่อน. รอบนี้ปิดทั้งสามข้อในโหมด Task Race **โดยไม่แตะฉาก** (สร้าง UI/เอฟเฟกต์ตอนรันทั้งหมด)

- **กล้อง:** `BobTaskRaceCamera` เปลี่ยนจาก `height` เป็น `pitch` (45°, `Range 20–80`) + `distance 5.2` และถอดความสูงเป็น `Height = tan(pitch)·distance` (5.2u). ข้อขัดแย้งของเอกสารปิดตามมติ **C4 = 45° size 4.1** (docs 20 §4): docs 11 §5A.1 แก้ตารางเป็น 45°/4.1 แล้ว, docs 24 §4 แก้ตารางกล้องเป็น pitch/distance. `TaskRaceVerify` ไม่ hardcode 6.2/5.2 อีก — อ่าน `rig.Height` กับ `rig.Distance` ถ้าฉากมี rig
- **Quick Ping P03 (`BobTaskRaceHud`):** ปุ่ม `ปิง` 132u (Disc ของ `T.secondary`) เหนือปุ่มหลักที่ `(-cardInset, 14+150+14)`; กดค้าง → `PingWheel` เปิด (catcher เต็มจอไว้ปิดด้วยการแตะนอกวง), ชิป 6 คำ 184u เรียงครึ่งวงกลมรัศมี 258u รอบ hub 300×110u (ศูนย์กลางอยู่ที่ page-local `(600, −110)`, อยู่เหนือ-ซ้ายของนิ้วโป้ง). คำ: `ช่วยวางอันนี้ · ฉันไปหยิบเอง · ยกชั้นบนออกที · ใกล้หมดเวลา! · เยี่ยม! · ขอบคุณ` ตาม docs 11 §5A.5
- **การเลือก:** ใช้ "ชิปที่ใกล้ที่สุด" ไม่ใช่มุมจากจุดกด (วงไม่ได้อยู่ตรงปุ่ม) โดยมี dead zone 34u กันการเลือกทันทีตอนกด; ปล่อยนิ้วบนชิป = ส่ง, ปล่อยในที่ว่าง = วงคงอยู่แล้วแตะชิปต่อ (เส้นทาง "แตะแล้วแตะช่อง" ของสเปก); กดปุ่มซ้ำหรือแตะนอกวง = ปิด; ปุ่ม Esc/Back ปิดวงก่อนทุกอย่าง
- **คำพูด:** แผ่นครีม 460×84 เหนือหัว 2.15u ติดตามตัว 2.5 วิ + จาง 0.35 วิ (CanvasGroup), clamp ในหน้า; ถ้ายืนใกล้จุดงานของทีม (≤8u) จะต่อท้าย `· ชื่องาน` และเรียก `TaskRaceRound.PingSpot(spot)` → เพื่อนบอท (`TaskRaceBotController.DirtyOwnSpot`) เลือกจุดนั้นก่อน 12 วิ (`PingSeconds`) เพื่อให้คำสั่งมีผลจริงในโหมดออฟไลน์ ไม่ใช่แค่ข้อความ
- **ทำไมปุ่มซ่อนใน Solo:** `PingAllowed` = `round.Players > 1` หรือกำลังเล่นออนไลน์; โหมดเดี่ยวไม่มีใครรับคำ จึงไม่โชว์ปุ่มหลอก. วงจะปิดเองเมื่อการ์ด S28/การ์ดหยุด/หน้าผลขึ้น และตอนกด Esc
- **ส่งเก้าอี้ให้เพื่อน (docs 11 §5A.3 "ส่งต่อ") — `BobChairTask`:** ถือเก้าอี้แล้วยืนห่างเพื่อนบอทฝ่ายเรา ≤ `PassRadius 2.6u` (และยังไม่ถึงช่องวาง) ปุ่มหลักเปลี่ยนเป็น `ส่งเก้าอี้ให้เพื่อน`. กดแล้วเก้าอี้ย้ายไปลอยที่มือเพื่อน (`carrier.forward*0.5 + 0.45u`), เสียง `pass_part`, ฮีโร่กลับเดินเร็วปกติ และเพื่อนเดินไปช่องว่างที่ใกล้ที่สุดแล้ววางเอง (`TaskRaceBotController.CarryTo` = งาน `Job.Courier` ที่ข้ามตารางงานเดิมชั่วคราว). ถ้าเพื่อนรับไม่ได้ (แมตช์จบ/ถูกตรึง) เก้าอี้กลับมามือฮีโร่; ถ้าเดินไม่ถึงใน 20 วิ เก้าอี้ถูกวางลงพื้นไม่ลอยค้าง. เก้าอี้ที่เพื่อนถืออยู่ถูกกันไม่ให้ฮีโร่หยิบซ้ำ (`NearestTippedChair` ข้าม `carriedChair`) และ `Seat` กันการนับซ้ำ
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors`; `bob-check` = `BOB-CHECK PASSED`; `cd unity-check && dotnet run` = `UNITY-CHECK PASSED`
- **ทำแล้ว 16 พ.ย. 2026:** ไอคอน 6 แบบ (F13) ใช้สไปรต์ในชุด UI ที่มีอยู่ (`icon_check`/`stat_speed`/`icon_lift`/`icon_timer`/`icon_star`/`icon_heart`) วาง 56u เหนือคำในชิป 184u และ `ping_pop` **สังเคราะห์ตอนรัน** ด้วย `BobTone.PingPop()` (0.16 วิ 880→1500 Hz) ไม่มีไฟล์ `.wav` เพราะ LFS เขียนไม่ได้ — ใช้ทั้งตอนส่งปิงและตอนถูกป่วน — ดู docs 11 §5A.5, docs 12 §6
- **ยังค้าง:** วงล้อแบบวงกลมเต็ม (ตอนนี้เป็นครึ่งวงเพราะปุ่มอยู่มุมจอ), ภาพหน้าฮีโร่ในถาดเพื่อน (`HUD-MateTray` — ทำตัวแผงแล้ว ดู §15.30), การส่งคำ ping ผ่านเน็ตเวิร์ก (`bob.chat`), และยังไม่ได้เปิด Unity วัดชิปบนจอจริง/ทดสอบส่งเก้าอี้กับเพื่อนบอทในโหมด Duo

### 15.30 ถาดของเพื่อน (`HUD-MateTray`) — เห็นว่าเพื่อนทำอะไร แล้วเรียกได้ (16 พ.ย. 2026)

ที่มา: docs 11 §5A ตาราง HUD ในรอบแข่งขันเขียนแถว `HUD-MateTray` ว่า "เพิ่มสถานะ กำลังทำ: <task> / ว่าง เพื่อให้แบ่งงานได้" และ README ค้างเรื่องเพื่อนที่มองไม่เห็นว่าใครทำอะไรในโหมด Duo/Team ออฟไลน์. รอบนี้ทำแผงจริง **โดยไม่แตะฉาก** (สร้างตอนรัน)

- **ที่วาง:** คอลัมน์ขวาที่ว่างจริง — แถบบนจบที่ x 1536, ปุ่มปิงมีขอบบนที่ page y −760, แถบคราบฉุกเฉินมีขอบบนที่ −770. แผงจึงอยู่ชิดขวาบนที่ `(-24, −500)` ชิป 300 × 92u ห่างกัน 8u → 1 เพื่อน (Duo/2v2) จบที่ −592, 2 เพื่อน (3v3) จบที่ −692, คำใบ้ต่อท้ายที่ −704…−730 — **ไม่ทับแถบบน ปุ่มหลัก ปุ่มปิง แถบคราบ หรือจอย**
- **ข้อมูล:** อ่านจากงานของบอทโดยตรง ไม่เดาจากระยะทาง — เพิ่ม `TaskRaceBotController.State` (`MateState.Idle/Walking/Working/Sabotaging/Carrying`) และ `Spot` (สถานีเป้าหมาย). บรรทัดสถานะ: `กำลังทำ: <งาน>` (job Work) · `ไป: <งาน>` (job Walk) · `กำลังยกเก้าอี้ไปช่อง` (job Courier จาก §15.29) · `ป่วนจุดงานคู่แข่ง` (job Sabotage) · `ว่าง` (Idle). ชื่อชิปเป็น `เพื่อน 1/2` ตามลำดับ name ของบอท (Teammate1, Teammate2) และอวตารเป็น Disc 64u + เลข (ยังไม่มีภาพ F-series)
- **การแตะ:** แตะชิป = **"มาช่วยงานนี้"** → หาจุดงานของทีมที่ใกล้ตัวผู้เล่น (≤8u, สูตรเดียวกับ `PingTarget` ของวงล้อ) แล้ว `round.PingSpot(spot)` → บอททุกตัวเลือกจุดนั้นก่อน 12 วิ. ไม่มีจุดงานใกล้ตัว = toast `ยืนใกล้จุดงานก่อน แล้วค่อยเรียกเพื่อน` + เสียง `drop_invalid`; สำเร็จ = toast `เรียกเพื่อนมาช่วย: <งาน>` + `ui_confirm` + log `[TaskRaceMateTray] call for help at <จุด>`
- **การแสดง/ซ่อน:** `MateTrayAllowed` = `round.Players > 1` และไม่ใช่ออนไลน์ (ออนไลน์มีร่างจริงจาก `BobTaskRaceOnline` อยู่แล้ว) และต้องมีบอทฝ่ายเราจริง (`!raidEnemy` + `team == TaskSpot.LocalTeam`); ซ่อนตอนการ์ด S28/การ์ดหยุด/หน้าผลขึ้น. สแกนใหม่ทุก 0.75 วิ และสร้างชิปใหม่เฉพาะเมื่อ **จำนวน** เพื่อนเปลี่ยน (คำในชิปอัปเดตทุกเฟรมเมื่อข้อความเปลี่ยนเท่านั้น)
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors`; `bob-check` = `BOB-CHECK PASSED`; `cd unity-check && dotnet run` = `UNITY-CHECK PASSED`. Hook สำหรับถ่ายภาพ/เทสต์: `DebugMateTray`, `DebugMateChips`, `DebugCallMateHelp()`
- **ยังค้าง:** ภาพอวตารจริงในชิป (F-series), ไอคอนของในมือ (ตอนนี้บอกเป็นข้อความ), แตะชิปเพื่อเรียก **เฉพาะคนนั้น** (ตอนนี้ทั้งทีมตอบ), และยังไม่ได้เปิด Unity ดูแผงจริงในโหมด Duo/Team

### 15.31 ดาวคุณภาพครบสามดวง — ไม่ทิ้งงาน และรถเข็นผ่านสูตร (16 พ.ย. 2026)

ที่มา: docs 16 §2.5 ระบุดาวคุณภาพทีมสามดวง ซึ่งเป็น **รายงานวิธีเล่น** ไม่ใช่เงื่อนไขชนะ. ดวงแรก (คราบทันทุกตัว) ทำไว้แล้วในงาน T12▲; รอบนี้ปิดอีกสองดวงที่เหลือ

- **ดวงที่ 2 · ไม่ทิ้งงาน:** `BobTaskRaceHud.HideCard` คือจุดเดียวที่การ์ดปิดทุกทาง (ปุ่ม ×, จอย 70%, รอบจบ, หน้าผล) — ถ้าปิดแล้วสถานียัง `Dirty` (และไม่ใช่การ์ดป่วน) จะ `StorySeries.NoteCardClosed()` + จำ `TaskId` ไว้ใน `abandonedCards`. ถ้าใบงานนั้นจบในบทเดียวกัน (`TaskEntry.Done`) `RenderRows → NoteRecoveredJobs()` จะ `NoteCardRecovered()` แล้วลบ id ทิ้ง → **ปิดกลางคันไม่ผิด ถ้ากลับมาเก็บให้จบ**; ที่เหลือค้าง (`AbandonedLeft = closes − recovered`) คือของที่หมดเวลาเก็บจริง ๆ
- **ดวงที่ 3 · รถเข็นผ่านสูตร:** `TickCard` ตอนการ์ด T04 จบ อ่าน `round.CartPassed` ก่อน/หลัง `NoteCartResult` — ถ้าเปลี่ยนจาก `null` เป็นค่าจริง (คือการล็อกแรกของรอบที่ `TaskRaceRound` ยอมรับ) จะ `StorySeries.NoteCart(passed)` ครั้งเดียว; ดวงนี้ต้องผ่าน **ทุกครั้งที่ล็อก** (`CartClean = CartRuns > 0 && CartPasses >= CartRuns`) — บทที่ไม่เคยล็อกเลยไม่มีดาว (กฎเดียวกับคราบ เพื่อไม่ให้ได้ดาวฟรี)
- **การแสดงบนการ์ดจบบท:** ต่อท้ายบรรทัดคำถามเดิม เฉพาะดวงที่ "มีเรื่องให้รายงาน" — `· ✓/✕ คราบ n/m` (เมื่อมีคราบ) · `· ✓/✕ ไม่ทิ้งงาน` (เมื่อเคยปิดกลางคัน) · `· ✓/✕ รถเข็น p/r` (เมื่อเคยล็อกสูตร) และลด auto-size ต่ำสุดเป็น 16 เพื่อให้บรรทัดที่ยาวที่สุดยังอยู่ใน 1080u
- **ไม่แตะตรรกะดาวของบท:** `StorySeries.Stars` (0–3 ดวงของบท) ไม่เปลี่ยน — `unity-check` มี assertion ว่าบทที่ชนะ 2 รอบยังได้ 2 ดาวเท่าเดิมแม้ดาวคุณภาพจะตกครบ
- **ตรวจ:** `bash unity-typecheck/check.sh` = `typecheck ok: 40 known Unity-6 gap kind(s), no new errors`; `bob-check` = `BOB-CHECK PASSED`; `cd unity-check && dotnet run` = `UNITY-CHECK PASSED` (เพิ่ม 10 assertion: ไม่เคยปิด → ผ่าน, ปิด 2 → ตก, คืน 1 → ยังตก, คืนครบ → ผ่าน, คืนเกินไม่ติดลบ, ไม่ล็อกสูตร → ไม่มีดาว, 1/2 → ตก, 2/3 → ตก, reset เคลียร์, ผ่านครบ → ได้, ดาวคุณภาพไม่ขยับดาวบท)
- **ยังค้าง:** ยังไม่ได้เปิด Unity ดูบรรทัดที่ยาวที่สุดบนการ์ดจบ (ต้องเช็ก auto-size 16 บนจอ 20:9), ยังไม่ผูกดาวคุณภาพกับรางวัล/ภารกิจ (docs 16 §2.5 บอกว่า "ใช้ในรางวัลและภารกิจ")
