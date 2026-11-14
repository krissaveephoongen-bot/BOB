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
| `unity/Assets/Scripts/TaskRace/BobTaskRaceHud.cs` | HUD ของรอบ (ข้อ 10): การ์ด **ใบงาน** ตาม `TaskRaceRound.Tasks` (✓ เสร็จ / ⋯ ผู้ถือ TASK_LOCK / รอคิว), **แถบน้ำหนัก** + %, "ทีมฟ้า N/M" และปุ่มหลัก `ทำงาน: <ชื่อ>` ที่เปิด/ปลด TASK_LOCK — สร้างด้วย `BobUi` ใน canvas 1920×1080 match height |
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
| `height` | 8.5 | สูงเหนือฮีโร่ |
| `distance` | 7.5 | ถอยหลังตามแนวนอน → pitch ประมาณ 48° |
| `fieldOfView` | 40 | เลนส์ perspective |
| `followTime` / `lookTime` | 0.22 / 0.14 | SmoothDamp |
| `lookHeight` | 0.6 | จุดเล็งเหนือฐานฮีโร่ |
| `focusZoom` | 0.85 | การ์ด S28 เปิด → ซูมเข้า |
| `focusShiftX` | 1.2 | เล็งเยื้องขวา ให้ฮีโร่/จุดงานอยู่ซ้ายของการ์ด |
| `focusTime` | 0.35 | เวลานุ่มของโฟกัส |

- ทำงานใน `LateUpdate` จึงชนะกล้องที่จัดเฟรมใน `Update` ของฉากเดิม
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

- **ทุกชิ้นอยู่ในแถบกลางของเกาะ** (`|z| ≤ ~0.74` หลังหัก footprint ตัวเอง) — แผ่นสะพานเหนือ/ใต้คร่อมขอบสนามไว้แล้ว (ข้อ 5.4) ของเดิมที่วาง z ±0.3–0.5 จึงยื่นเข้าไปใน deck
- รั้ว `fence_wood` ถูกถอดออก เพราะความยาวของมันพาดเข้าแถบสะพานตะวันตกเสมอ
- ขาด prefab ตัวใด log จะเตือน `decor prefab missing: SM_…` และข้ามชิ้นนั้น ไม่ทำให้ build ล้ม

### 5.3 เงาใต้เคาน์เตอร์ (contact AO)

- `AddContactShadows` วาง quad มืดใต้ `CounterTop` ของทุกสถานี ขนาด `Clamp(max(width, depth) × 1.45, 1.6, 2.6)` u
- ก่อนหน้านี้วัดจาก **ทุก** renderer ใต้ TaskSpot (รวมวงแหวน/เครื่องมือยาว) ได้ decal 9.9–20.2 u กลายเป็นวงม่วงล้อมเกาะทั้งใบ — ต้องวัดจาก top ของเคาน์เตอร์เท่านั้น

### 5.4 ทางเดินสะพานปลอดสิ่งกีดขวาง + ขอบน้ำ

แผ่นสะพานวัดจากซีนจริง: `x[-5.78,5.78]` ที่ `z[1.74,3.66]` และ `z[-3.66,-1.74]` เทียบกับ `Field` ครึ่งลึก 3.6 u — deck คร่อมขอบสนามเหนือ/ใต้พอดี ของประดับจึงอยู่ได้เฉพาะแถบกลาง

- `BobMapAssemblerEditor.ValidateClearance` ถูกเรียกจาก `TaskRaceSceneBuilder.BuildVariant` **หลัง** clone ฝั่ง B (ก่อน clone ยังไม่มีสถานี/ของประดับ) และ log `clearance decks: …` ตามด้วย `clearance: N prop(s) checked, 0 decor and 0 station overlap(s) on 2 deck(s)`
- กฎสามโซน: บน deck ต้องห่าง **1.5 u**, ข้าง deck **1.0 u** (สถานีผ่อนเป็น 0.2 u เพราะตัวเคาน์เตอร์คือเป้าหมายการเล่น), ทางลง/landing **1.5 u**; ตัวเลขนี้ทำให้ของประดับอยู่บนเกาะ 12 u ได้โดยไม่ต้องโล่งทั้งเกาะ
- ค่าที่ผ่านแล้ว: solo `34`, duo `36`, team `40` ชิ้น — ต้องเป็น 0 overlap ทุกโหมด; ถ้ามีจะได้ warning `clearance: <ชื่อ> on <side> is X u <zone> (need Y) x[…] z[…]`
- **ขอบน้ำ:** `TrimRiverSouth` บีบ vertex ของ mesh แม่น้ำที่ไม่สูงกว่า `cutZ = -islandDepth × 0.5` ให้มาที่ `cutZ` — solo `-4.50` (500 vertex), duo `-5.63`, team `-6.75` น้ำจึงจบเสมอขอบผาหน้าตึก ไม่ยื่นเป็นแผ่นน้ำค้างใต้เกาะ

## 6. สองโหมดบนแผนที่เดียว

ซีนถูก bake และเซฟให้เริ่มที่ **`TimedBridgeRaid`** (ค่าเริ่มต้น `mode` ของ manager) — PureRace ยังเลือกได้จาก Inspector/หน้าต่าง assembler

| | PureRace | TimedBridgeRaid (ค่าเริ่มต้นของซีน) |
| --- | --- | --- |
| `RiverBarrier_Group` | เปิด — ตลิ่ง + hedge มี collider + `NavMeshObstacle` (carve) | ปิด |
| `Bridges_Group` | ปิด — เหลือแต่ walk collider ที่ไม่ทำงาน | เปิด — collider + `NavMeshLink` ของสะพานเหนือ/ใต้ |
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
| หัวการ์ด | `round.DoneCount` / `TaskCount` | `ใบงาน · ทีมฟ้า 3/6` (ตัวเลข = จำนวนงานเสร็จ ไม่ใช่น้ำหนัก ตาม doc 16 ข้อ 2.2) |
| แถบน้ำหนัก | `round.DoneWeight` / `TotalWeight` | เติมด้วย `MoveTowards` 0.75/วิ ไม่กระโดด, สี primary → success, มี `%` ข้างแถบ |
| แถวรายการ | `round.Tasks` (ลำดับตามแผน, T04 แรก) | วงกลมหมวด + ชื่องาน + `×น้ำหนัก`; `✓` = เสร็จ, `⋯ <ชื่อผู้ถือล็อก> · ชื่องาน` = มีคนเปิดการ์ด, `รอคิว` = งานยังไม่ถึงสถานี |
| ปุ่มหลัก | `TaskSpot.ActiveNear` | `ทำงาน: <ชื่อ>` → เรียก `TryOpen` (ได้ TASK_LOCK); ระหว่างเปิด = `กำลังทำ: <ชื่อ>` กดซ้ำ = `Close(false)` (progress คง 10 วิ); โดนบล็อก = toast แดงจาก `Blocked` |
| ปุ่มหลัก (ป่วน) | `near.CanSabotageNow` | `ป่วนจุดงาน: <ชื่อ>` สีแดง → `TrySabotage` → เปิดการ์ดป่วน 2.75 วิ |
| ปุ่มหลัก (เกราะ) | `near.ShowShieldPrompt` | `ติดเกราะป้องกัน (-150 Pts)` สีเข้ม; กดไม่ได้ถ้าแต้มไม่พอ (`CanAffordShield`); สำเร็จแล้วมี toast จาก manager |
| แถบบน | `manager.RemainingSeconds` / `LocalPoints` / mode | `เวลา mm:ss` + `Team Points: N` + บรรทัดโหมด (`Bridge Raid · สะพานเปิดที่ 60 วิ` → `สะพานเปิดแล้ว` → `Pure Race · สะพานปิดทั้งแมตช์`); แถบเวลาเหลืองที่ 65 วิ แดงที่ 10 วิ |
| การ์ด S28 | `BeginCard(spot, sabotage)` | stand-in: แถบวิ่งเต็มตาม `ExpectedSeconds` (ขั้นต่ำ 2.5 วิ), ป่วน 2.75 วิ; ปิดด้วย × หรือจอย > 70% (`TaskSpot.CancelOpenSessions`) |
| ดีบักบาร์ | manager debug hooks | 8 ปุ่ม (ข้อ 6.2) ซ่อนได้เหลือชิป `DBG` |
| ท้ายการ์ด | `round.Summary` | บรรทัดเดียวกับ log: จำนวนงาน, น้ำหนัก, `cards`, `workload` |

- HUD อ่าน `round.Version` ทุกเฟรม — ผู้อำนวยการรอบ bump ค่านี้เมื่อมีงานใหม่ขึ้นสถานี, มีคนล็อก/ปลดล็อก, หรือมีงานเสร็จ จึงสร้างแถวใหม่เฉพาะตอนเปลี่ยน (แถบกับ % อัปเดตทุกเฟรม)
- Canvas เป็น `ScreenSpaceCamera` ที่ผูกกับ `Camera.main` (ไม่ใช่ Overlay) เพื่อให้ `TaskRaceVerify` สลับกล้องชั่วคราวแล้วถ่ายภาพ `-hud.png` ได้ — Overlay จะไม่ถูกวาดลง RenderTexture
- `joystickInput` ไม่ต้องผูกจาก HUD: manager หา `BobTaskRaceHero.InputVector` เองถ้าช่องว่าง (คีย์บอร์ดเป็น fallback บนเดสก์ท็อป)
- toast ของป่วน/เกราะ/เปิดสะพานมาจาก `TaskRaceModeManager.ToastRequested` — HUD ไม่ต้องรู้กติกาเอง

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
- **การ์ดมินิเกม S28 จริงยังไม่มี** — HUD มีการ์ด stand-in ที่แถบวิ่งเต็มตาม `ExpectedSeconds` แล้วเรียก `spot.Close(true)` เอง; เมื่อมินิเกมจริงมาแทน ให้เกม layer เป็นคน `AddProgress` / `Close(completed)` (ข้อ 10.2)
- `TaskRaceRound` ยังไม่มีคราบฉุกเฉิน T16 และยังไม่รับ seed จาก server — ทั้งสองเกาะใช้ seed เดียวกันในเครื่องเดียว (ข้อ 14) จึงยังไม่มีแถบคู่แข่งบน HUD
- ยังไม่มี server contract ของ Task Race map (`TASK_BEGIN`/`TASK_LOCK` ตาม doc 16) — TASK_LOCK รอบนี้เป็นฝั่ง client
- แถบคะแนนฝั่ง B ยังไม่แสดงบน HUD (แต้มของคู่แข่งถูกนับแล้วใน `manager.PointsOf(TeamB)`), และยังไม่มี AI คู่แข่งเดินป่วนเอง

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

ผลที่ต้องเห็น: `spots:` 6 id ไม่ซ้ำ (`S1_A..S3_B`; duo = 8, team = 12), `baked tasks:` ชื่อมินิเกมจริงบนทุกจุด, `round D1`–`round D3` (จำนวนงาน `players`, `mean station`, `workload` และรายการ T-id),
`floor under spots: (x,z)y=0.00` ทุกจุด, `TimedBridgeRaid: ... barriers=2 links=2`, `PureRace: obstacles=4 (4 before) carving=4`, แล้วได้ภาพ

**NavMesh + ประตูสะพาน (ข้อ 6.1)** — build log ต้องมี `navmesh geometry: 4 walkable collider(s), 2 ignored` และ
`navmesh baked: PureRace ... (solo 24 / duo 28 / team 28 tri), TimedBridgeRaid ... (52 / 60 / 64 tri)` — ส่วนต่างคือแผ่นสะพาน (28 / 32 / 36 tri) และต้องไม่เป็น 0
verify log ต้องได้ **สองสภาวะ** ของโหมดเดียวกัน (ประตูปิด carve deck / ประตูเปิดเชื่อม):

```
navmesh TimedBridgeRaid gates shut: 52 tri, ...
nav TimedBridgeRaid gates shut: ... status=PathPartial corners=2 length 7.6 u     <- ประตู carve ตัดกลางสะพาน
navmesh TimedBridgeRaid gates open: 52 tri, ...
nav TimedBridgeRaid gates open: ... status=PathComplete corners=5 length 13.9 u   <- ข้ามได้
probe Bridge_North centre ...: on navmesh, off by 0.00 u                          <- แผ่นสะพานอยู่บน NavMesh
deck Bridge_North: ... collider BoxCollider x[-5.78,5.78] y[-1.00,0.00] z[1.74,3.66]  <- ขนาดถูก (ยาว ~12, กว้าง ~1.9)
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
การ์ดใบงานมุมซ้ายบน ชื่อ `ใบงาน · ทีมฟ้า 0/N`, แถว `T04 จัดรถเข็นตามสูตร ×2` มาก่อนเสมอ, แถวที่สถานียังไม่หยิบมาขึ้น `รอคิว`, แถบน้ำหนัก 0%;
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
- ทั้งสองเกาะได้รายการเดียวกันและเติมพร้อมกัน → การแข่งยังยุติธรรม; `DoneCount` / `DoneWeight` ของฝั่ง A คือตัวเลขบน HUD (`ใบงาน · ทีมฟ้า N/M`) และแถบน้ำหนัก (`BobTaskRaceHud`, ข้อ 10)
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
- log ตอนเริ่มรอบ: `[TaskRaceRound] seed … · players … · difficulty … · N tasks (long/common/short) · weight … · cards … · workload …` ตามด้วยงานของทุกสถานีฝั่ง A
- ยังไม่ทำ: คราบฉุกเฉิน T16, server seed contract (`TASK_BEGIN`), AI คู่แข่ง — HUD รายการงาน/แถบน้ำหนักทำแล้ว (ข้อ 10)
