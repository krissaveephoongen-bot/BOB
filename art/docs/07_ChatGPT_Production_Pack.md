# 07 · ChatGPT Production Pack: สร้าง asset จริงทีละไฟล์

ภาพจากโค้ดใน `unity/Assets/Art/Sprites/` ใช้เป็น **ตัวกำหนดชื่อไฟล์ ขนาด และท่าทาง** เท่านั้น ภาพจริงให้สร้างใน ChatGPT ตามลำดับในเอกสารนี้ แล้วส่งกลับมาให้ Claude ตัด ปรับขนาด ตั้งชื่อ และใส่เข้า Unity

---

## ขั้นที่ 0: ตั้งค่า ChatGPT ครั้งเดียว

1. ใน ChatGPT สร้าง **Project** ชื่อ `BOB Art`
2. อัปโหลด 2 ไฟล์นี้เป็นไฟล์ของ Project: `docs/concept/BOB_Asset_Sheet.png`, `docs/concept/BOB_Game_Art_Board.png`
3. วางข้อความนี้ใน **Project instructions**

```
You are the lead artist for the mobile game "BOB – Battle of the Broom".
Always match the attached BOB_Asset_Sheet.png and BOB_Game_Art_Board.png exactly: same characters,
same proportions (chibi, about 2.3 heads tall), same outfits and colours. Never redesign a character.
Style: 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted
gradients, thin dark-brown outlines, warm key light from upper left, soft light-blue fill, gentle
ambient occlusion, bright clean colours.
Output rules unless told otherwise: ONE subject per image, centered, full body visible, nothing cropped,
transparent background (PNG), no text, no logo, no ground shadow, no frame, square 1024x1024.
Characters face 3/4 to the RIGHT (viewer's right), feet at the bottom of the image with a small margin.
```

4. ทุกครั้งที่ได้ภาพที่ชอบ ให้กด **ดาวน์โหลด PNG** แล้วตั้งชื่อตามตารางด้านล่าง วางไว้ที่ `SweapBattle\bob-art\incoming\<หมวด>\`

> เคล็ดลับความสม่ำเสมอ: ทำ **Hero Master** ของแต่ละตัวก่อน (ข้อ 1.1) แล้วทุก prompt ถัดไปของตัวนั้นให้ **แนบภาพ Master ที่ได้** ไปด้วย และพิมพ์ "same character as the attached image"
> ถ้ารายละเอียดเพี้ยน (ปมผ้าคาดหัว, หูถัง, สายแว่น) ใช้ปุ่มแก้ไขภาพ (เลือกพื้นที่) แก้เฉพาะจุด ไม่ต้องสร้างใหม่ทั้งภาพ

---

## ขั้นที่ 1: ฮีโร่ 4 ตัว (P0 ทำก่อน)

### ข้อมูลตัวละคร (ใช้แทน `<HERO>` ใน prompt)

| id | `<HERO>` |
| --- | --- |
| sweep | `กวาด (Sweeper): energetic boy, messy dark-brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with a square front pocket, navy pants, brown short boots, wooden broom with golden bristles tied with a red band.` |
| bubble | `ฟอง (Foam): cheerful girl, light-brown hair in two space buns with side-swept bangs, sky-blue t-shirt, cream apron with a square front pocket, navy shorts, brown short boots, holds a big yellow porous sponge, a few soap bubbles.` |
| press | `ถู (Scrub): sturdy friendly boy wearing a silver metal bucket as a helmet (handle hanging at the side), brown hair peeking out, green t-shirt, cream apron with a square front pocket, navy pants, brown boots, wooden mop with a fluffy white mop head.` |
| spray | `สเปรย์ (Spray): confident girl, black bob haircut with straight bangs, aviator goggles with light-blue lenses and a purple strap worn on top of the head, purple t-shirt, cream apron with a square front pocket, navy shorts, brown boots, pink trigger spray bottle with a white nozzle.` |

### 1.1 Hero Master (1 ภาพต่อตัว)
```
<HERO>
Full-body hero render, standing in a confident idle pose holding the tool, smiling.
```
บันทึกเป็น `incoming/heroes/<id>_master.png`

### 1.2 ท่าหลัก (แนบ Master ทุกครั้ง, 1 ภาพต่อท่า)
```
Same character as the attached image. <POSE>. Same outfit, colours and proportions. Full body.
```

| ไฟล์ | `<POSE>` |
| --- | --- |
| `<id>_idle.png` | relaxed idle pose, tool held at the side, gentle smile |
| `<id>_walk_a.png` | walking, left foot forward, right arm swinging forward |
| `<id>_walk_b.png` | walking, right foot forward, left arm swinging forward |
| `<id>_run.png` | running fast, leaning forward, tool held back |
| `<id>_carry.png` | walking while holding a small box above the head with both hands, tool slung across the back, effort face |
| `<id>_work.png` | scrubbing / using the tool on the floor, focused face |
| `<id>_skill.png` | dynamic special-move pose with the tool, determined shout (no effects) |
| `<id>_hit.png` | stumbling backwards, dizzy face |
| `<id>_cheer.png` | jumping with both arms up, joyful closed eyes |
| `<id>_defeat.png` | sitting on the floor, sad crying face |
| `<id>_back.png` | seen from directly behind, standing |
| `<id>_side.png` | seen from the side (profile facing right), standing |

### 1.3 สีหน้า (แนบ Master)
```
Same character as the attached image. Head and shoulders only, front view, expression: <EXPR>.
```
`<EXPR>` = happy · wink · determined shout · surprised with a small red "!" · crying with tears · effort gritting teeth · dizzy swirl eyes · joyful closed eyes with sparkles
บันทึกเป็น `<id>_face_happy.png`, `<id>_face_wink.png`, `<id>_face_angry.png`, `<id>_face_surprised.png`, `<id>_face_cry.png`, `<id>_face_effort.png`, `<id>_face_dizzy.png`, `<id>_face_joy.png`

### 1.4 Character Sheet (เอกสาร ไม่เข้าเกม, ไม่ต้องพื้นใส)
แนบ Master + `docs/sheets/sheet_<id>.png` (เลย์เอาต์อ้างอิง)
```
Create a character design sheet for the attached character using the same layout as the attached
"กวาด (Sweeper)" sheet in BOB_Asset_Sheet.png: hero render with tagline, turnaround (front, side, back, side),
8 expressions, key poses (idle, walk, run, carry, work, skill, hit, victory), equipment, colour palette.
Light-blue panels with navy rounded headers. Landscape 1536x1024. Text allowed.
```

---

## ขั้นที่ 2: ศัตรูและบอส

| id | `<ENEMY>` |
| --- | --- |
| stain | `เจ้าคราบ Dirt Monster: lumpy pile of brown mud shaped like a teardrop blob, darker speckles, small mud splats around it.` |
| grease | `สไลม์ Slime: glossy semi-translucent purple slime blob with pink highlights and small droplets around it.` |
| trash | `ขยะปีศาจ Trash Monster: grey metal trash can with a lid, dark inside with two big glaring white eyes.` |
| germ | `เชื้อโรค Germ: small round green germ with two antennae and stubby feet.` |
| boss | `บอสคราบยักษ์ Slime King: giant glossy purple slime with a golden crown, stretchy slime arms, droplets orbiting.` |
| dust | `Dust bunny: fluffy grey dust cloud with loose fluff bits.` |
| rust | `Rust: jagged orange-brown rusty metal chunk with flaky spots.` |
| scale | `Limescale: pale blue hexagonal crystal with facets.` |

```
<ENEMY> Cute-but-grumpy: big white eyes, thick angry eyebrows, toothy grin. Not scary for kids.
Pose: <POSE>.
```

| ไฟล์ | `<POSE>` |
| --- | --- |
| `<id>_idle.png` | idle, slightly squashed, angry face |
| `<id>_attack.png` | lunging forward to attack, stretched |
| `<id>_hit.png` | squashed from a hit, eyes shut tight, white flash |
| `<id>_defeat.png` | melting / dissolving into sparkles, dizzy swirl eyes |

บอสเพิ่ม `boss_taunt.png` (laughing, arms up), `boss_slam.png` (slamming both arms down)

---

## ขั้นที่ 3: ไอเทมและพร็อพ (ทำเป็นชุดได้ แล้วให้ Claude ตัดแยก)

```
Game item icon set for BOB on a transparent background, a 4x4 grid with generous spacing, each item centered
in its cell, same 3/4 view, same lighting, soft outline, no text:
gold coin with a star, yellow star, blue gem, red heart, orange stopwatch, horseshoe magnet, soap bubble,
treasure chest closed, treasure chest open with sparkles, gold trophy, yellow wet-floor sign, orange traffic cone,
blue bucket, black trash bag, grey trash bin, blue recycle bin.
```
บันทึกเป็น `incoming/items/items_grid_1.png` (Claude จะตัดเป็น `item_coin.png` ... ตามชื่อในตาราง 06)

```
School furniture set for BOB on a transparent background, 4x3 grid, generous spacing, same 3/4 top-down view
(camera pitched 40 degrees), no text: classroom desk, classroom chair, green blackboard on a wooden frame,
3 colourful lockers, school window, long cafeteria table with a tray, red cafeteria chair, food stall with a red and
white striped awning, playground slide, park bench, round green tree, flowering bush.
```
บันทึกเป็น `incoming/props/props_grid_1.png`

---

## ขั้นที่ 4: ฉาก (ไม่ต้องพื้นใส, 1536x1024)

```
Top-down 3/4 orthographic game level for BOB, camera pitched 45 degrees, landscape: <ROOM>.
Leave a large clear walkable floor area in the centre for characters and a cleaning cart. No characters, no text.
Version: <CLEAN_OR_DIRTY>.
```
`<ROOM>` = a school classroom with desks, chairs, a blackboard and big windows · a school cafeteria with long tables, colourful chairs and a food stall · an outdoor school playground with a slide, swings, trees, sand and grass · an underground boiler room with pipes, stone walls and a glowing purple door
`<CLEAN_OR_DIRTY>` = CLEAN: bright, sparkling, sunlight · DIRTY: same room and same layout, desaturated, brown mud stains, purple slime puddles, scattered trash and dust

ให้ทำ CLEAN ก่อน แล้วแนบภาพ CLEAN ตอนสั่ง DIRTY พร้อมพิมพ์ "exactly the same room and camera as the attached image" เพื่อให้สองภาพซ้อนกันได้
ไฟล์: `bg_classroom.png`, `bg_classroom_dirty.png`, `bg_cafeteria.png`, ... , `bg_boiler_dirty.png`

---

## ขั้นที่ 5: UI

```
Mobile game UI kit for BOB on a transparent background, separated pieces with spacing, no text on buttons:
big glossy yellow-gold rounded button, glossy blue rounded button, large round blue skill button,
small round cream button, round portrait frame, red ribbon banner, cream panel with a navy header,
green HP bar with navy frame and its empty background, 4 round skill icons (golden broom swipe, bubble dome,
frozen stopwatch, crosshair target).
```
`incoming/ui/ui_kit_1.png`

โลโก้ (พื้นใส, 1536x1024):
```
Game logo "BOB" with subtitle "BATTLE OF THE BROOM": big cream letters with dark outline on a red ribbon banner,
over a round yellow and blue badge with a crossed broom and mop, exactly like the logo in the attached art board.
```
`incoming/ui/logo_bob.png`

---

## ส่งกลับมาให้ Claude

วางไฟล์ไว้ใน `SweapBattle\bob-art\incoming\` แล้วบอกในเธรดนี้ Claude จะ
1. ลบพื้นหลังที่ยังไม่ใส ตัดภาพจากกริด
2. ปรับขนาดให้ตรงกับ placeholder (ตัวละคร 256 px ต่อเฟรม เท้าอยู่ที่จุดหมุน, ไอคอน 192 px, ฉาก 1536x1024)
3. ประกอบท่าเป็น sprite sheet ตามชื่อไฟล์เดิม แล้วอัปเดต `bob-art-manifest.json` เพื่อให้ Unity import ได้ทันที
4. เทียบกับ concept และแจ้งตัวที่สีหรือทรงเพี้ยน

### ข้อจำกัดที่ควรรู้
- ChatGPT สร้างอนิเมชันหลายเฟรมที่ต่อเนื่องกันไม่ได้ดี จึงสั่งเป็น **ท่าหลักทีละภาพ** แล้วขยับใน Unity ด้วย bob/squash (`BobTween`) หรือ Unity 2D Animation (bone) ถ้าจะลื่นแบบเกมจริงต้องให้นักอนิเมตทำต่อ
- ถ้าจะไปทาง 3D ตาม `03_Unity_Spec.md` ภาพชุดนี้ (Master, back, side) คือ reference ให้ modeler
- ตรวจเงื่อนไขการใช้ภาพจาก ChatGPT เชิงพาณิชย์ของบัญชีที่ใช้ก่อนออกเกม
