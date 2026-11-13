# 03 · Unity Production Specification

Unity 6 LTS · URP · Target: Android (min API 26, OpenGL ES 3.0 / Vulkan) + iOS · 60 fps บนเครื่องกลาง, 30 fps บนเครื่องต่ำ (2 GB RAM)

> **อัปเดต 13 พ.ย. 2026:** ข้อเสนอ AA ใน `21_AA_Art_Bible.md` แก้ข้อ 1 (กล้อง 45° size 4.1), ข้อ 2 (งบ + palette texture เดียว), ข้อ 5 (material library, เส้นขอบเป็นพิกเซลบนจอ) และเพิ่มชุดแสง โมเดล blockout ที่ทำตามสเปกนี้อยู่ใน `art/aa/kit/` (Generic rig, ชื่อ bone ตามข้อ 3)

## 1. สรุปสเปก

| รายการ | ค่า |
| --- | --- |
| Character | 3D Stylized, toon shader + outline |
| Camera | Orthographic, rotation X = 40–45°, size 4.5 (landscape gameplay) / 6.0 (portrait เมนู) |
| Rendering | URP Forward, MSAA 2x (ปิดบนเครื่องต่ำ), HDR ปิด, Shadow: ปิดกับตัวละคร ใช้ blob shadow |
| Animation | **Generic Rig** ใช้ skeleton เดียวกันทุก Hero (ไม่ใช้ Humanoid เพราะสัดส่วน chibi retarget แล้วแขนทะลุตัว) |
| Materials | Stylized: toon ramp + outline (ไม่ใช้ PBR เต็ม) |
| Texture | Hero 1024², ศัตรู/อุปกรณ์ 512², พร็อพ 256–512² (atlas), UI Sprite Atlas 2048² |
| Effects | Particle System (Shuriken) + flipbook sprite; ไม่ใช้ VFX Graph |
| Optimization | LOD 2 ระดับ, SRP Batcher, GPU Instancing กับพร็อพซ้ำ, static batching ฉาก |

## 2. Poly & Texture Budget

| ประเภท | Triangles | Texture | Material ต่อชิ้น | Bones |
| --- | --- | --- | --- | --- |
| Hero (LOD0) | 4,000–6,000 | 1024² 1 ชุด (Albedo + ramp mask) + face 512² | 2 (body, face) | ≤ 32 |
| Hero (LOD1, ระยะไกล/co-op 4 คน) | 2,000 | ใช้ร่วม | 2 | ≤ 32 |
| อุปกรณ์ประจำตัว | 300–800 | อยู่ใน atlas ของ Hero | 0 (แชร์) | – |
| ศัตรู | 1,500–3,000 | 512² | 1 | 0–8 |
| บอส | 8,000–10,000 | 1024² | 2 | ≤ 40 |
| ชิ้นส่วน 15 ชิ้น | 150–500 ต่อชิ้น | Atlas 1024² รวมทุกชิ้น | 1 (แชร์) | – |
| ห้อง 1 ห้อง (รวม) | ≤ 40,000 | Atlas 2048² 1–2 แผ่น | ≤ 4 | – |
| **งบรวมบนจอ** | **≤ 100k tris, ≤ 80 draw calls (batches)** | | | |

Texture format: ASTC 6×6 (Android/iOS), Mip-map เปิดกับ 3D, ปิดกับ UI

## 3. Shared Hero Skeleton

```
Root
 └ Hips
    ├ Spine ─ Chest ─ Neck ─ Head ─ (HeadTop, Hair_01..04, Accessory)
    │            ├ Shoulder_L ─ UpperArm_L ─ LowerArm_L ─ Hand_L ─ (Finger_L, Thumb_L)
    │            ├ Shoulder_R ─ UpperArm_R ─ LowerArm_R ─ Hand_R ─ (Finger_R, Thumb_R, Socket_Tool)
    │            └ Apron_01 (ผ้ากันเปื้อนแกว่ง)
    ├ UpperLeg_L ─ LowerLeg_L ─ Foot_L
    └ UpperLeg_R ─ LowerLeg_R ─ Foot_R
 Socket_Carry (ลูกของ Root สูง 1.15)
```
- รวม ~28 bones, influence ≤ 2 ต่อ vertex (ประหยัด skinning บนมือถือ)
- ทุก Hero ยึดตำแหน่ง joint เดียวกัน ±2 cm → อนิเมชัน 13 ท่าร่วมใช้ได้ทุกตัว
- ผม/หมวก/แว่น เป็น child ของ `Head` หรือใช้ Hair bones (secondary motion ด้วย script spring เบาๆ ไม่ใช้ physics)

## 4. Animation List & Animator

| Clip | Frames (30 fps) | Loop | หมายเหตุ |
| --- | --- | --- | --- |
| attack_1/2/3 (Action) | 12–18 | – | combo 3 จังหวะ, event `OnHitFrame`, `OnFxSpawn` |
| dodge (Action) | 14 | – | กลิ้ง/สไลด์ |
| idle | 60 | ✓ | หายใจ + กะพริบตา (face flipbook) |
| idle_fidget_<hero> | 90 | – | เล่นสุ่มทุก 6–10 วินาที |
| walk / run | 24 / 16 | ✓ | root motion ปิด, ความเร็วจากโค้ด |
| carry_idle / carry_walk | 60 / 24 | ✓ | แขนชูเหนือหัว ถือของที่ `Socket_Carry` |
| pickup / place / throw | 12 / 14 / 12 | – | event `OnPartAttach`, `OnPartRelease` |
| work | 20 | ✓ | ขัด/ถู ใช้อุปกรณ์ประจำตัว |
| skill_<hero> | 40–60 | – | event `OnSkillFx` |
| hit | 14 | – | สะดุด/ตกใจ (ปฏิกิริยาเสีย) |
| victory / defeat / cheer | 60 / 50 / 40 | victory ✓ | |

Animator Controller: `AC_Hero_Base` (ร่วม) + **Animator Override Controller** ต่อ Hero (แทนแค่ `idle_fidget` และ `skill`)

Layers: `Base` (locomotion blend tree speed 0→walk→run) · `Upper` (avatar mask ลำตัวบน: carry/work) · `Face` (ควบคุม flipbook ผ่าน script `BobFace`)

Parameters: `speed` (float), `carrying` (bool), `working` (bool), triggers `pickup`, `place`, `throw`, `skill`, `hit`, `victory`, `defeat`, `cheer` (ชื่อ trigger ตรงกับระบบ placeholder เดิม)

## 5. Shader & Material (URP)

**`BOB/ToonLit`** (Shader Graph หรือ HLSL):
- Ramp 2 ขั้น จาก texture ramp 256×4 (แถวละ Hero) → shadow สีตามตาราง Color Script ไม่ใช่ดำ
- Rim light ขาว อ่อน (ใช้กับของที่หยิบได้ ปรับด้วย `_RimStrength`)
- `_DirtAmount` 0–1: ผสม albedo กับ dirt overlay + ลด saturation (ใช้กับห้องและพร็อพ → ทำ Dirty/Clean variant ได้ด้วย material เดียว)
- รับ AO จาก vertex color R

**`BOB/Outline`**: inverted hull, ความหนา `_OutlineWidth` 0.012 (ปรับตาม camera size ให้คงที่บนจอ), สี `#2B2D42`
- ใช้ Renderer Feature "Render Objects" pass ที่ 2 แทนการทำ material ซ้ำ

**ศัตรู**: `BOB/ToonLit` + ตัวแปร Grease `_Specular` สูง, Scale ใช้ fresnel ใสๆ (ไม่ใช้ transparency จริงเพื่อเลี่ยง overdraw)

## 6. โครงสร้างโฟลเดอร์ Unity

```
Assets/
  Art/
    Characters/
      Shared/        SK_Hero.fbx, AC_Hero_Base.controller, Anim/*.fbx, Ramp_Hero.png
      Sweep/         SM_Sweep.fbx, T_Sweep_Albedo.png, T_Sweep_Face.png, M_Sweep.mat, AOC_Sweep.overrideController, PF_Sweep.prefab
      Bubble/ Spray/ Press/   (โครงเดียวกัน)
    Enemies/<id>/    SM_, T_, M_, Anim/, PF_
    Props/
      Parts/         SM_Part_<id>.fbx (15), T_Parts_Atlas.png
      Cart/ Stations/ Tools/
    Environments/<Room>/   Meshes/, Textures/, Materials/, PF_Room_<Room>.prefab
    VFX/             Textures/, Materials/, PF_FX_<name>.prefab
    UI/              Sprites/, Atlas/SA_UI.spriteatlasv2, Fonts/
    Sprites/         (placeholder 2D เดิม — คงไว้จนกว่างานจริงแทนครบ)
  Shaders/           BOB_ToonLit.shadergraph, BOB_Outline.shader
  Scripts/           (เดิม) Rules/, Net/, Art/ + BobFace.cs, BobHeroView.cs
```

## 7. Naming Convention

| Prefix | ประเภท | ตัวอย่าง |
| --- | --- | --- |
| `SK_` | Skeletal mesh | `SK_Hero.fbx` |
| `SM_` | Static / skinned mesh รายตัว | `SM_Sweep.fbx`, `SM_Part_vinegar.fbx` |
| `T_` | Texture (+ suffix `_Albedo`, `_Mask`, `_Face`) | `T_Bubble_Albedo.png` |
| `M_` | Material | `M_Grease.mat` |
| `A_` | Animation clip | `A_Hero_carry_walk.anim` |
| `AC_` / `AOC_` | Animator / Override | `AOC_Spray.overrideController` |
| `PF_` | Prefab | `PF_Press.prefab`, `PF_FX_fizz.prefab` |
| `SA_` | Sprite Atlas | `SA_UI` |

id ตัวละคร/ศัตรู/ชิ้นส่วน **ใช้ตัวพิมพ์เล็กตาม `BobRules.cs` เสมอ** (`sweep`, `grease`, `vinegar`) เพื่อให้โค้ดโหลด asset ด้วย id ได้ตรง

## 8. FBX Export (Blender → Unity)

- Scale 1 unit = 1 m, Apply Transform, Forward `-Z`, Up `Y`, "Apply Unit" เปิด
- Pivot: ตัวละคร/ศัตรู = กึ่งกลางเท้าบนพื้น (Y=0); ชิ้นส่วน = กึ่งกลางด้านล่าง (สำหรับวางซ้อน)
- อนิเมชัน export แยกไฟล์ละท่า `SK_Hero@walk.fbx` หรือรวม NLA track ไฟล์เดียว
- Import: Rig = Generic, Avatar = Copy From `SK_Hero`, Mesh Compression Medium, Read/Write ปิด, Anim Compression Optimal

## 9. Prefab Structure (Hero)

```
PF_Sweep
 ├ Model (SM_Sweep + Animator + BobFace)
 │   └ ... Socket_Tool → PF_Tool_Broom
 ├ BlobShadow (quad + M_BlobShadow)
 ├ FX_Anchor
 └ components: BobHeroView (ผูก id "sweep", เล่น trigger ตามสถานะเกม)
```

## 10. Co-op / Performance Checklist

- 4 Heroes + 3 ศัตรู + รถเข็น 2 คัน บนจอพร้อมกัน ต้องอยู่ใน 100k tris / 80 batches
- ใช้ LOD1 กับ Hero ของผู้เล่นอื่นเมื่อเครื่องเป็น Low tier
- Particle ต่อ effect ≤ 30 อนุภาค, ทั้งจอ ≤ 200
- ทดสอบบนเครื่องจริงรุ่นต่ำ (เช่น Android 3 GB RAM) ตั้งแต่ M3 ห้ามรอจนจบ
- Addressables แยก group ตามห้อง/Chapter เพื่อลดขนาดดาวน์โหลดแรก (เป้า APK ≤ 150 MB)

## 11. ข้อเพิ่มจาก Art Bible AA (13 พ.ย. 2026, ข้อเสนอ)

- **Palette texture เดียว:** `T_BOB_Palette.png` 256² (ช่อง 16×16, ลำดับตาม `PALETTE` ใน `art/tools/blender/bob_kit.py`) Import: Filter Point, ไม่มี mip, ASTC 4×4 ทุกโมเดลจาก kit, AI และ CC0 ใช้ `M_BOB_Toon` ตัวเดียว → SRP Batcher รวม batch ได้
- **Rig ใน kit:** 19 bone (ชุดย่อยของข้อ 3: Root, Hips, Spine, Chest, Neck, Head, UpperArm/LowerArm/Hand ซ้ายขวา, Socket_Tool, Apron_01, UpperLeg/Foot ซ้ายขวา, Socket_Carry) skin แบบ rigid 1 influence ต่อ vertex; ศัตรู 3 bone (Root, Body, Top)
- **Outline:** ความหนาคำนวณเป็นพิกเซลบนจอ (2.5 px ที่ 1080p ตัวละคร, 2 px ฉาก) แทน 0.008–0.010 unit ซึ่งเหลือราว 1 px ที่ ortho 4.1
- **กล้อง:** pitch 45°, ortho size 4.1, ฮีโร่ scale 1.15 (วัดได้ 164 px ที่ 1080p)
- **ชุดแสง:** `PF_Light_Room` (key `#FFF4E0`, ambient gradient `#DDEBFA`/`#8F86BF`) ทุกห้อง
- **นำเข้าไฟล์จากสคริปต์:** `art/aa/kit/SK_<id>.fbx` (หรือ `.glb` ผ่าน glTFast) Rig = Generic, Avatar = Create From This Model (ฮีโร่ตัวแรก) แล้ว Copy From สำหรับตัวที่เหลือ อนิเมชันอยู่ใน take ชื่อ idle / walk / cheer
