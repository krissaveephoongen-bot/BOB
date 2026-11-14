# 24 · Task Race Map System: เกาะคู่โค้งแบบมินิเดียรามา

เวอร์ชัน 0.4 · 14 พ.ย. 2026 · สถานะ: แผนที่จริง import แล้ว (5 ชิ้น ≤ 8k tri), ซีนสามโหมด `TaskRace.unity` (solo, 6 จุด) / `TaskRace_Duo.unity` (1v1, 8 จุด) / `TaskRace_Team.unity` (2v2/3v3, 12 จุด) สร้างด้วยคำสั่งเดียวต่อโหมด และตรวจผ่านทั้งสองกติกาทุกซีน · รอบนี้เพิ่ม **ผู้อำนวยการรอบ** (`TaskRaceRound`): สุ่มรายการงานตาม doc 16 จาก seed, เติมงานให้สถานี และวัดปริมาณงานต่อโหมด (ข้อ 14)

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
| `unity/Assets/Resources/Shaders/BobCurvedWorld.shader` | `BOB/CurvedWorld`: outline pass (inverted hull, หนาคงที่ตามพิกเซล) + ForwardBase toon lit (ramp 0.45/0.78, rim, ambient ฟ้า/ม่วง, dirt) เท่าสูตร `BOB/ToonLit` |
| `unity/Assets/Scripts/TaskRace/BobCurvedWorldController.cs` | `BobCurvedWorld` (static: global, `Drop`, `Curve`, `CreateMaterial`) + `BobCurvedWorldController` (blend ค่าความโค้ง, context menu 0 / 0.010 / 0.018 / 0.026) |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceCamera.cs` | กล้องตามฮีโร่แบบ SmoothDamp + โฟกัสการ์ด S28 |
| `unity/Assets/Scripts/TaskRace/TaskSpot.cs` | จุดงาน: วงแหวน F29, ป้ายพรีวิว, NEAR, TASK_LOCK, เก็บ progress 10 วิ, event ให้ HUD |
| `unity/Assets/Scripts/TaskRace/TaskRaceModeManager.cs` | สลับ PureRace / BridgeRaid, กลุ่ม barrier/bridge, NavMeshObstacle/NavMeshLink, **สลับ NavMesh ที่ bake ไว้ต่อโหมด** (`ApplyNavMesh`), ปิดการ์ดเมื่อจอยเอียง > 70% |
| `unity/Assets/Scripts/TaskRace/TaskRaceRound.cs` | ผู้อำนวยการรอบ: คลังมินิเกม 15 รายการ (T01–T15), สุ่มรายการงานแบบมี seed ตามสูตร doc 16 ข้อ 2.2, แจกงานให้สถานีตามหมวด + เติมงานใหม่เมื่อทำเสร็จ, ประมาณปริมาณงาน — ดูข้อ 14 |
| `unity/Assets/Scripts/Editor/BobMapAssemblerEditor.cs` | หน้าต่าง `Tools > BOB > Task Race Map Assembler`: ประกอบแผนที่, clone Team A → Team B, สลับโหมด, validate |
| `unity/Assets/Scripts/Editor/TaskRaceSceneBuilder.cs` | `Tools > BOB > Build Task Race Scene (solo / duo / team)` — สร้างซีนครบทั้งฉาก (กล้อง/แสง/ฮีโร่/แผนที่/สถานี/โหมด), ติด `NavMeshModifier` (ข้อ 6.1), bake NavMesh สองชุดต่อซีน แล้วเพิ่มเข้า Build Settings ตามลำดับ solo → duo → team; ขนาดและจำนวนสถานีดูข้อ 13 |
| `unity/Assets/Scenes/NavMesh/{TaskRace,_Duo,_Team}_{PureRace,BridgeRaid}.asset` | NavMesh ที่ bake ไว้ล่วงหน้าต่อซีนต่อโหมด (สร้างใหม่ทุกครั้งที่ build ซีน) — `TaskRaceModeManager` สลับให้ตอนเปลี่ยนโหมด |
| `unity/Assets/Scripts/Editor/TaskRaceVerify.cs` | `-executeMethod Bob.EditorTools.TaskRaceVerify.Run` (หรือ `RunDuo` / `RunTeam`) — เปิดซีน ตรวจจุดงาน/ทั้งสองโหมด/เส้นทาง NavMesh ระหว่างเกาะ แล้วเรนเดอร์ `Logs/taskrace{-duo,-team}-{preview,wide,play}.png` |
| `unity/Assets/Scripts/TaskRace/BobTaskRaceHero.cs` | ฮีโร่ทดสอบ: CharacterController เดินบนระนาบแบน + หันตามทิศเดิน |

Namespace ของทั้งหมดคือ `Bob.TaskRace` (assembler อยู่ `Bob.EditorTools`)

## 3. Shader และค่าความโค้ง

- `_BobCurvature` เป็น **global** (`Shader.SetGlobalFloat`) ไม่ใช่ property ของ material — ถ้าประกาศเป็น property ใน shader ค่าของ material จะทับ global
- แต่ละ material มี `_CurvatureScale` (0–3) เผื่อต้องการให้พื้นน้ำหรือฉากหลังโค้งมาก/น้อยกว่าพื้นเดิน
- ค่าเริ่มต้น `0.018` (Ever-Planet look ที่กล้องสูง 8.5 u), `RuntimeInitializeOnLoadMethod` รีเซ็ตให้ทุกเพลย์
- Outline pass ใช้ `n.xy` ใน view space แล้วบวกใน clip space ตาม `_ScreenParams` — เส้นคงที่ 2.5 px โดยประมาณ
- `FallBack "Diffuse"` เพื่อให้ shadow caster ยังแบน (เงาไม่โค้งตาม ซึ่งมองไม่เห็นในมุมกล้องนี้)

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
                 CliffWaterfall (ฉากหลังเหนือ)
   TeamA (-X)      RiverChannel (กลาง)        TeamB (+X)
  Island + Props   Bridges_Group (N/S)       Island + Props (สำเนาเลื่อน X)
                   RiverBarrier_Group (ตลิ่ง+hedge)
```

- Team B คือ **สำเนา Team A ที่เลื่อนตาม X เท่านั้น** (`TransformPoint` + `Vector3.right * dx`) ไม่มีการหมุน/กลับด้าน
- เหตุผล: HUD มือถือ (จอยล่างซ้าย, ปุ่มทำงานล่างขวา, ใบงานบนซ้าย) บังมุมเดียวกันทั้งสองเกาะ จุดงานจึงไม่ถูกบังฝั่งใดฝั่งหนึ่ง
- **พื้นเล่นคือ slab `Field`** (cylinder + convex MeshCollider, top y = 0) วางบนเกาะ ไม่ใช่ bounds ของ mesh เกาะ — เกาะ Tripo เป็น *ชุดประกอบ* (พื้นดิน + ก้อนหิน + ต้นไม้/ของลอย) จึง fit แต่ละแกนด้วย `FitAxisAligned` แล้วเลื่อนด้วย `AlignGround` ให้ percentile 0.6 ของพื้นใน footprint ลงมาแตะ -0.05 (ถ้าใช้ bounds top ต้นไม้ที่ลอยอยู่จะกลายเป็น "พื้น" แล้วพื้นจริงจมลงไป 0.5–5.8 u ตามที่ตรวจด้วย raycast)
- เกาะจึงเป็นฉากหลังล้วน ๆ: เนิน/ตลิ่ง/ก้อนหินยังโผล่พ้น slab ได้ และไม่ต้องใช้ MeshCollider บน mesh ที่ decimate แล้ว
- สะพานมี child `Walkable` เป็น BoxCollider แบน top y = 0 (ทางเดินจริง) และต้อง `FaceLongAxisX` + `FitAxisAligned(bridgeTarget, 2 u)` เพื่อให้ทอดข้ามช่องน้ำเป็นแผ่นไม้แคบ ไม่ใช่แท่งกว้าง 4.9 u
- **ความยาวสะพานคิดจากขอบ `Field` ไม่ใช่ความกว้างช่องน้ำ:** `Field` เป็นวงรี (cylinder) ขอบฝั่งที่ z ของสะพานจึงแคบกว่าเส้นผ่านศูนย์กลาง สูตรคือ `2 × (islandGap/2 − bankEdge(z) + 1.2)` โดย `bankEdge(z) = fieldHalfX × √(1 − (z/fieldHalfZ)²)` — ปลายสะพานจึงจมเข้าหาเกาะ 1.2 u เกินขอบจริง ซึ่งมากกว่าการกัดของ NavMesh (agent radius 0.5 u) พอที่พื้นสองเกาะจะเชื่อมกัน
- `Walkable` ถูก parent ไว้ใต้สะพาน (เพื่อให้เปิด/ปิดตามโหมด) แต่ prefab สะพานถูกหมุน 90° และ scale ไม่เท่ากันทุกแกน — **ห้ามเซ็ต `localScale = 1/lossyScale`** (แกน X/Z จะสลับกัน ได้กล่องยาว 21 u กว้าง 0.8 u) ให้วาง `localScale = 1` แล้วหาร `BoxCollider.size` ด้วย `lossyScale` ที่วัดได้แทน; ตรวจด้วย `TaskRaceVerify` ที่ log `deck ... collider x[...] z[...]`
- TaskSpot วางบนพื้น ไม่ใช่บนเคาน์เตอร์: กำหนด `ringAnchor` เป็นจุดพื้น (ถ้าว่างจะใช้ transform ของตัวเอง)
- **วัสดุ/สี:** map piece ทุกชิ้นใช้ `Resources/Materials/M_BOB_CurvedMap.mat` (BOB/CurvedWorld + `T_BOB_Palette` แบบ point) และ UV ทุกจุดชี้ไปช่อง palette — flat `_Color` ที่ไม่มี texture จะไม่ตรงสีกับ mesh อื่น `UsePaletteCell` ใน assembler จึงตั้ง UV เป็นช่อง `grass (8,2)`, `dirt (14,1)`, `stone (10,2)`, `wood (10,1)`

## 6. สองโหมดบนแผนที่เดียว

| | PureRace (ค่าเริ่มต้น) | BridgeRaid |
| --- | --- | --- |
| `RiverBarrier_Group` | เปิด — ตลิ่ง + hedge มี collider และ `NavMeshObstacle` (carve) | ปิด |
| `Bridges_Group` | ปิด | เปิด — สะพานเหนือ/ใต้ มี `NavMeshLink` |
| กติกา | ต่างคนต่างอยู่เกาะตัวเอง | ข้ามไปแย่ง `TASK_LOCK` ที่สถานีฝั่งตรงข้ามได้ |

- `TaskRaceModeManager.ApplyMode()` จัดการทั้งสองกลุ่ม และสร้าง/ปรับ obstacle หรือ link ให้อัตโนมัติ
- `NavMeshLink` เป็นของจริงจากแพ็กเกจ **AI Navigation 2.0.16** (ติดตั้งแล้ว, assembly `Unity.AI.Navigation`) — ไม่มี fallback reflection/OffMeshLink แล้ว
- จุดปลาย link คำนวณจาก world bounds ของสะพาน (`EnsureBridgeLink`) ไม่ใช่ค่าคงที่ — สะพานจึงยาวได้ตามสูตรในข้อ 5 โดยไม่ต้องแก้โค้ด link
- `TaskRaceModeManager.Instance` และ event `ModeChanged` ให้ HUD/กติกาแสดงโหมดได้

### 6.1 NavMesh ต่อโหมด (bake ล่วงหน้า)

`TaskRaceSceneBuilder` bake NavMesh **สองชุดต่อซีน** ก่อนเซฟ: `Assets/Scenes/NavMesh/{Scene}_{PureRace,BridgeRaid}.asset`
แล้ว `TaskRaceModeManager.ApplyNavMesh()` สลับชุดตามโหมด (`RemoveData` → `navMeshData = ...` → `AddData`)

- `NavMeshSurface` อยู่บน `TaskRaceMap` (`collectObjects = Children`, `useGeometry = PhysicsColliders`)
- `TagWalkables` ติด `NavMeshModifier` ให้ collider ทุกตัวในแผนที่: เฉพาะที่ชื่อ `Field` และ `Walkable` (พื้น + สะพาน) ถูก bake, ที่เหลือ (ตลิ่ง, hedge, หน้าผา, prop, เกาะ Tripo) ถูก `ignoreFromBuild`
- PureRace ได้พื้นสองเกาะแยกกัน (ไม่มีสะพาน active) — BridgeRaid ได้พื้นสองเกาะ + แผ่นสะพานที่เชื่อมกันจริง
- agent radius ยังเป็นค่า Humanoid ของโปรเจกต์ (0.5 u) จึงกัดขอบพื้นเข้าไป ~0.5 u — นี่คือเหตุผลที่สะพานต้องจมเข้าหาเกาะ 1.2 u ไม่ใช่แค่แตะขอบ
- Bake ใช้เวลาไม่กี่วินาทีและทำใน batch mode ได้ (ข้อ 12) — ไม่ต้อง bake ใน Play mode

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

## 8. Assembler และ asset ที่ต้องเตรียม

`Tools > BOB > Task Race Map Assembler`

**ชื่อ prefab เป้าหมาย** (ช่องในหน้าต่างกำหนดเองได้)

| ช่อง | prefab ที่หาให้อัตโนมัติ (`Assets/Resources/Props`) | GLB ดิบใน OneDrive/Rig | tri หลัง clean |
| --- | --- | --- | --- |
| Island A | `SM_bob_map_island_land` | `floating island 3d model.glb` | 42,640 → 8,000 |
| Island B (ถ้าไม่ใส่ = ใช้ตัว A) | `SM_bob_map_island_land2` | `floating island 3d model2.glb` | 52,416 → 8,000 |
| River | `SM_bob_map_river_channel` | `stylized river water 3d model.glb` | 51,638 → 8,000 |
| Bridge | `SM_bob_map_wooden_bridge` | `wooden footbridge 3d model.glb` | 53,657 → 7,999 |
| Cliff | `SM_bob_map_cliff_waterfall` | `waterfall diorama 3d model.glb` | 57,117 → 7,999 |

โบนัสที่ยังไม่ผูกกับโค้ด: `SM_bob_map_plaza` (`circular cobblestone plaza`, 55,684 → 6,172 tri) ใช้เป็นลานกลางหรือเกาะที่สามได้

**สถานะ asset:** clean/decimate/import **เสร็จแล้ว** — ผ่าน `bob_prop_cleanup.py` ด้วย `--tint` (palette-snapped UV) แล้ว FBX อยู่ใน `Assets/Art/Map`, prefab อยู่ใน `Assets/Resources/Props` (เมนู `Bob > Art > Build Map Prefabs`); ใช้ `M_BOB_CurvedMap.mat` (มี outline pass ในตัว จึงไม่ต้องมี hull child)
ถ้าช่องว่างและเปิด `Use placeholders` หน้าต่างจะสร้างกล่องติดป้ายแทน เพื่อทดสอบเลย์เอาต์/โหมดได้ทันที

**ปุ่มในหน้าต่าง**

1. `Build map skeleton` — สร้าง `TaskRaceMap/{TeamA,TeamB}/{Island,Props}`, river, cliff, `RiverBarrier_Group`, `Bridges_Group`, `Systems` (manager + controller) และ BoxCollider แบน; ถ้าเปิด `Add BobTaskRaceCamera` จะเพิ่มกล้องให้ Main Camera พร้อม target ฮีโร่
2. `Clone Team A layout to Team B` — สำเนาพร็อพ + TaskSpot ใต้ `TeamA/Props` ไป `TeamB/Props` ด้วยการเลื่อน X และตั้ง id ใหม่ `S3_A → S3_B`
3. `PureRace` / `BridgeRaid` — เขียนค่าให้ manager ในฉากทันที
4. `Validate map` — รายงานจำนวนจุด, id ซ้ำ/ว่าง และ reference ของกลุ่มใน manager

## 9. ขั้นตอนตั้งค่าใน Inspector (ทีละขั้น)

1. **สร้างซีนทั้งฉาก** — `Tools > BOB > Build Task Race Scene` (หรือ batch `-executeMethod Bob.EditorTools.TaskRaceSceneBuilder.Build`) สร้าง `Assets/Scenes/TaskRace.unity` ใหม่ทั้งไฟล์: Main Camera + `BobTaskRaceCamera`, ดวงอาทิตย์ + ambient, ฮีโร่ `SK_hero_sweep` (CharacterController + `BobTaskRaceHero`), แผนที่จาก assembler, สถานีไม้ 3 จุดบน Team A แล้ว clone ไป Team B, ผู้อำนวยการรอบ `TaskRaceRound`, NavMesh สองชุด (ข้อ 6.1), โหมด PureRace และแทรกซีนใน Build Settings ลำดับ 2
2. **ปรับด้วยหน้าต่าง assembler** — ถ้าต้องวางเอง/แก้เลย์เอาต์ ใช้ขั้น 3–10 ด้านล่าง
3. **Import asset** — ทำเสร็จแล้ว (ข้อ 8); ถ้าเปลี่ยนโมเดลให้รัน `bob_prop_cleanup.py` แล้ว `Bob > Art > Build Map Prefabs` ใหม่
4. **เปิดหน้าต่าง** — เมนู `Tools > BOB > Task Race Map Assembler`
5. **ลาก prefab** เข้าช่อง Island A/B, River, Bridge, Cliff (หรือกด `Find by name`)
6. **ตั้งเลย์เอาต์** — `Island width` ≈ 12, `Island centres` ≈ 16 (ช่องน้ำ = 4 u), `Bridge yaw` 0, `River yaw` 90
7. กด **Build map skeleton** → ตรวจว่า `TaskRaceMap` มี TeamA/TeamB/Systems ครบ
8. **วางสถานี** — ใต้ `TaskRaceMap/TeamA/Props` วางเคาน์เตอร์ (`Counter` ไม้ + `CounterTop` หิน) + prop ที่เข้าเรื่อง (`espresso_machine`, `bucket_metal_blue`, `basket_clothes`) แล้วเพิ่ม `TaskSpot` ให้ root ของสถานี:
   - `Spot ID` เช่น `S3_A`, `Task Name`, `Category`
   - `Expected Seconds` ตาม doc 16
   - `Ring Anchor` = empty บนพื้นใต้สถานี (ถ้าไม่ตั้ง วงแหวนจะอยู่ที่ origin ของ prop ซึ่งอาจลอย)
   - ปล่อย `Player` ว่างได้ ถ้าฮีโร่มี tag `Player`
   - prop จาก Tripo ต้อง **compose** rotation: `Quaternion.Euler(0, yaw, 0) * prefab.localRotation` (root มี X 270 จาก importer — เขียนทับแล้วโมเดลจะนอนราบ)
9. กด **Clone Team A layout to Team B** → ตรวจ id ฝั่ง B ลงท้าย `_B` และตำแหน่งเลื่อนตาม X เท่านั้น
10. **โหมด** — เลือก `PureRace` หรือ `BridgeRaid` ในหน้าต่าง (หรือใน Inspector ของ `TaskRaceMap/Systems`)
11. **วัสดุโค้ง** — เปิด `Swap map materials to BOB/CurvedWorld` ก่อน Build หรือใช้ `M_BOB_CurvedMap.mat` ที่ตั้งไว้แล้ว (อย่าใช้ flat `_Color` เพราะจะไม่ตรงกับสี palette ของชิ้นอื่น)
12. **NavMesh** — ทำอัตโนมัติในขั้น 1: `TaskRaceSceneBuilder` ติด `NavMeshModifier`, bake สองชุด (PureRace/BridgeRaid) ลง `Assets/Scenes/NavMesh/` แล้วผูกให้ manager (ข้อ 6.1) ถ้าแก้แผนที่ด้วยหน้าต่าง assembler เอง ให้รัน build ซีนซ้ำเพื่อ bake ใหม่
13. **เชื่อม HUD** — ให้ HUD/เกมสมัคร event ของ `TaskSpot` (ดูข้อ 10) แล้วผูก `manager.joystickInput` กับค่าจอยจริง
14. กด **Validate map** และเล่นทดสอบทั้งสองโหมด

## 10. การเชื่อมกับ HUD / เกม

```csharp
void OnEnable()
{
    TaskSpot.AnyOpened += s => OpenMinigameCard(s);        // เปิดการ์ด S28 ของ task
    TaskSpot.AnyClosed += s => CloseMinigameCard(s);
    TaskRaceModeManager.ModeChanged += m => RefreshModeBadge(m);
}

// ทุกเฟรม: ปุ่มหลัก
var near = TaskSpot.ActiveNear;
mainButton.gameObject.SetActive(near != null && !near.IsOpen && !near.Done);
if (near != null) mainLabel.text = "ทำงาน: " + near.TaskName;

// ส่ง progress จากการ์ดกลับเข้าจุดงาน
spot.AddProgress(delta);          // 0..1
spot.Close(completed: true);      // จบงาน
```

`TaskRaceModeManager.joystickInput` เป็น `Func<Vector2>` — ตั้งเป็น `() => joystick.Value` เพื่อให้กฎ "จอย > 70% ปิดการ์ด" ทำงาน
ถ้าไม่ตั้ง จะใช้ `Input.GetAxisRaw` ของคีย์บอร์ดสำหรับทดสอบบนเดสก์ท็อป

## 11. ข้อจำกัดและงานถัดไป

- สถานีในซีนเป็น **ตัวอย่าง** (solo 3 / duo 4 / team 6 จุดต่อเกาะ, เคาน์เตอร์ไม้ + prop) เพื่อทดสอบเลย์เอาต์และความหนาแน่น ไม่ใช่ชุดสถานีจริงตาม doc 16/19
- หน้าตัดของเกาะ B สูงกว่าเกาะ A มาก (12.6 vs 4.8 u จากโมเดล) — พื้นเล่นเท่ากันที่ y = 0 แต่ฐานหินล่างต่างกัน
- เกาะ B (`land2`) มีต้นไม้ใหญ่ในตัวโมเดลที่ยื่นมาทับขอบตะวันออกของ `Field` ในโหมด duo/team — ไม่มี collider (เดินทะลุได้) แต่บังพื้นที่ตากว้าง ถ้าต้องการสนามโล่งต้องเปลี่ยนเกาะ B เป็น prefab เดียวกับ A หรือตัดเนินไม้ออกจาก mesh
- ตัวละคร rig ใหม่ 3 ตัว (`bob_hero_sweep/spray/press` ใน Rig) มี skin แต่ **ไม่มี animation clip** และไม่มี rig ของ bubble — ฮีโร่ในซีนใช้ `SK_hero_sweep` แบบนิ่ง ยังไม่มีแอนิเมชันเดิน
- HUD/กติกายังไม่ได้ผูกกับ event เหล่านี้ในซีนจริง (ข้อ 10 เป็นสัญญาที่ต้องต่อ) — รวมถึง **รายการงาน/แถบน้ำหนัก** ของ `TaskRaceRound` ที่ยังไม่ถูกวาดบนจอ
- `TaskRaceRound` ยังไม่มีคู่แข่ง/คราบฉุกเฉิน T16 และยังไม่รับ seed จาก server — ตอนนี้ทั้งสองเกาะใช้ seed เดียวกันในเครื่องเดียว (ข้อ 14)
- ยังไม่มี server contract ของ Task Race map (`TASK_BEGIN`/`TASK_LOCK` ตาม doc 16) — TASK_LOCK รอบนี้เป็นฝั่ง client

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
`surface under spots: ... y=0.00:Field` ทุกจุด, `BridgeRaid: links=2`, `PureRace: obstacles=2 carving=2`, แล้วได้ภาพ

**NavMesh (ข้อ 6.1)** — build log ต้องมี `navmesh geometry: 4 walkable collider(s), 2 ignored` และ
`navmesh baked: PureRace ... (solo 24 / duo 28 / team 28 tri), BridgeRaid ... (52 / 60 / 64 tri)` — ส่วนต่างคือแผ่นสะพาน (28 / 32 / 36 tri) และต้องไม่เป็น 0
verify log ต้องได้:

```
navmesh BridgeRaid: ... tri
nav BridgeRaid: hero -> Team B spot · path=True status=PathComplete corners=4 length 12.9 u   <- ข้ามได้
probe Bridge_North centre ...: on navmesh, off by 0.00 u                                        <- แผ่นสะพานอยู่บน NavMesh
deck Bridge_North: ... collider BoxCollider x[-5.78,5.78] y[-1.00,0.00] z[1.74,3.66]            <- ขนาดถูก (ยาว ~12, กว้าง ~1.9)
navmesh PureRace: ... tri
nav PureRace: hero -> Team B spot · path=True status=PathPartial ...                            <- ข้ามไม่ได้ (ไม่มีสะพาน)
```

ถ้า `BridgeRaid` ยัง `PathPartial`: ดู `deck ... collider x[...]` ก่อน — ถ้ากว้างกว่าสะพานแปลว่า scale ถูก invert ผิด (ข้อ 5)
ถ้าขนาดถูกแต่ยังไม่เชื่อม ให้เพิ่มระยะจมเข้าหาเกาะ (ค่าคงที่ 1.2 ในสูตร `bridgeTarget`) แล้ว build ซีนใหม่

แล้วได้ภาพ
`Logs/taskrace{-duo,-team}-play.png` (มุมกล้องจริงของฮีโร่), `...-preview.png` (มุมกลาง), `...-wide.png` (ภาพรวมสองเกาะ)

เช็กลิสต์ในเกม:

- เปลี่ยน `curvature` 0 → 0.018 → 0.026 แล้วพื้น/กล้อง/ป้าย/วงแหวนโค้งพร้อมกันโดยไม่มี collider เพี้ยน
- เดินเข้า 2.6 u → ป้ายโผล่; เข้า 1.2 u → วงแหวนไฮไลต์และปุ่มหลักเปลี่ยนชื่อ
- เปิดการ์ด → กล้องซูม 0.85 และเล็งเยื้องขวา 1.2; กด × หรือดันจอย > 70% → การ์ดปิดและ progress คงอยู่ 10 วิ
- PureRace: เดินชนตลิ่งไม่ได้, สะพานปิด; BridgeRaid: ข้ามสะพานไปแตะจุดฝั่งตรงข้ามได้ และโดน TASK_LOCK toast
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
| ฐาน NavMesh ที่ bake (PureRace → BridgeRaid) | 24 → 52 tri | 28 → 60 tri | 28 → 64 tri |

- `islandGap` ต้อง ≥ `islandSize + 3` (assembler บังคับผ่าน `SetLayout`) เพื่อให้มีช่องน้ำพอสำหรับตลิ่ง (PureRace) และสะพาน (BridgeRaid)
- ความกว้างสะพานคงที่ 2 u ทุกโหมด — ยาวขึ้นแต่ไม่หนาขึ้น ยังอ่านเป็นสะพานไม้เดินแคบ; ความยาวคิดจากขอบวงรีของ `Field` (ข้อ 5) ไม่ใช่แค่ความกว้างช่องน้ำ จึงยาวกว่าเส้นผ่านศูนย์กลางช่องราว 3 เท่าและจมเข้าหาเกาะข้างละ 1.2 u
- กล้อง fov 40 สูง 8.5 ถอย 7.5 เห็นแนวกว้าง ~14.7 u ที่ตัวฮีโร่: Solo เห็นเกาะทั้งใบ, Duo เกือบทั้งใบ, Team เห็นราว 4 ใน 5 ของความกว้าง — Team จึงต้องพึ่งการเดิน/สะพานมากที่สุด ซึ่งตรงกับกติกา 2v2/3v3 ที่ต้องแยกกันทำงาน
- ทางเลือกประหยัด: ใช้ 15/20 ทั้ง Duo และ Team แล้วเพิ่มสถานีเป็น 6 จุด (เฟรมกล้องเท่ากันทุกโหมด แต่จุดงานจะแน่นขึ้นเป็น ~3.5 u ระหว่างเคาน์เตอร์)
- ทุกซีนถูกเพิ่มเข้า Build Settings อัตโนมัติ (ต่อจาก `TaskRace.unity`) — index 0 ยังเป็นซีนเกมเดิม ไม่เปลี่ยน
- **จุดเกิดฮีโร่ = จุดในครึ่งใต้ของสนามที่ห่างสถานีมากที่สุด** (`ChooseSpawn`, สแกนกริด 0.5 u) เพราะจุดเดิม `z = -0.2 × islandSize` ไปทับสถานี S6 ของโหมด team (ห่างแค่ 0.75 u ทำให้เริ่มเกมในวง NEAR) — log รายงาน `hero spawn (x,z), nearest station d=...`
- ตรวจทั้งสามซีน: `TaskRaceVerify.Run` / `RunDuo` / `RunTeam` → `Logs/taskrace{,-duo,-team}-{play,preview,wide}.png`

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
- ทั้งสองเกาะได้รายการเดียวกันและเติมพร้อมกัน → การแข่งยังยุติธรรม; `DoneCount` / `DoneWeight` ของฝั่ง A คือตัวเลขบน HUD (ยังไม่ผูก UI)
- ทำครบทุกงาน → `RoundWon` (log `won: 9/9 tasks, weight 15/15`)

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
- ยังไม่ทำ: HUD รายการงาน/แถบน้ำหนัก, คราบฉุกเฉิน T16, server seed contract (`TASK_BEGIN`) — ดูข้อ 11
