# 04 · AI Prompts สำหรับผลิต Concept / Reference

ใช้กับ ChatGPT (image), Midjourney, Leonardo, Scenario ฯลฯ
**วิธีใช้ให้สไตล์ไม่เพี้ยน:** แนบ `concept/BOB_Asset_Sheet.png` และ `concept/BOB_Game_Art_Board.png` ทุกครั้งเป็นภาพอ้างอิง แล้วต่อด้วย Style Block + prompt ของงานนั้น
ภาพจาก AI ใช้เป็น **reference สำหรับศิลปิน/modeler** เท่านั้น ก่อนใช้ในเกมจริงให้ศิลปินวาด/ปั้นใหม่และตรวจเงื่อนไขลิขสิทธิ์ของเครื่องมือที่ใช้

## Style Block (ใส่ทุก prompt)

```
Style: BOB – Battle of the Broom. 2.5D stylized cartoon mobile game art, chibi proportions
(about 2.3 heads tall), soft 3D volume like a vinyl toy, soft matte plastic materials,
hand-painted gradients, thin dark-brown outlines, soft warm key light from upper left,
light-blue fill light, gentle ambient occlusion, bright clean colors.
Palette: red #FB2F44, orange #FD7713, cream #FEEECF, skin #FADDC1, sky blue #3695F2,
navy #2E4484, deep navy #041E4F.
Keep the exact character designs from the attached BOB reference sheets. Do not redesign.
```

## 1. Character Master Design Sheet (ทำทีละตัว ตามเลย์เอาต์ sheet กวาด)

```
[Style Block]
Create a character design sheet for "<NAME_TH> (<NAME_EN>)" from BOB, using the same layout as the
attached "กวาด (Sweeper)" sheet: hero render with tagline, turnaround (front, side, back, side),
8 facial expressions (happy, wink, determined shout, surprised with "!", crying, effort gritting teeth,
dizzy swirl eyes, joyful closed eyes with sparkles), key poses (idle, walk, run, attack, skill, hit, victory),
skills & effects panel, equipment upgrade panel (3 levels), color palette swatches.
Character: <CHARACTER_BLOCK>
Light blue gradient background panels with navy rounded headers. 16:9.
```

### CHARACTER_BLOCK ต่อตัว

- **ฟอง (Foam)**: `cheerful girl, light-brown hair in two space buns with side-swept bangs, sky-blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holds a big yellow porous sponge, soap bubbles floating around. Role: support / area attack. Skill: squeezes sponge to release a ring wave of water and bubbles.`
- **ถู (Scrub)**: `sturdy cheerful boy wearing a silver metal bucket as a helmet (handle hanging at the side), brown hair peeking out, green t-shirt, cream apron with square pocket, navy pants, brown boots, holds a wooden mop with a fluffy white mop head. Role: defender / control. Skill: sweeps the mop in an arc creating a slippery water line.`
- **สเปรย์ (Spray)**: `confident girl, black bob haircut with straight bangs, aviator goggles with light-blue lenses and purple strap worn on the head, purple t-shirt, cream apron with square pocket, navy shorts, brown boots, holds a pink trigger spray bottle with white nozzle like a blaster. Role: ranged / debuff. Skill: long-range blue mist spray.`
- **ฮีโร่ที่เสนอเพิ่ม (ซัก, รีด, ปัด, ดูด):** CHARACTER_BLOCK อยู่ใน `24_Hero_Roster_Expansion.md` ข้อ 9 (รออนุมัติรายชื่อก่อนสร้างภาพ)
- **กวาด (Sweeper)** (เพิ่มแผ่น ortho): `energetic boy, messy dark-brown hair, red headband tied in a knot on the right with two tails, orange t-shirt, cream apron with square pocket, navy pants, brown boots, wooden broom with golden bristles and red band.`

## 2. Orthographic Modeling Reference (ส่ง modeler / Blender)

```
[Style Block]
Orthographic modeling reference sheet of <NAME_EN> from BOB. Neutral A-pose, arms 30 degrees from body,
no perspective, flat even lighting, no shadows, white background. Front view, left side view, back view
side by side, same scale, aligned on horizontal guide lines at top of head, eyes, chin, waist, knees, soles.
Separate small views of the equipment (<TOOL>) in front and side view at the same scale.
Total height 2.3 heads. Clean lines, no effects, no text except view labels.
```

## 3. Expression Sheet (ใช้ทำ face atlas)

```
[Style Block]
Face expression sheet of <NAME_EN> from BOB, head only, front view, 4x2 grid, identical head angle and size
in every cell, plain white background: happy, wink, determined shout, surprised "!", crying, effort gritting teeth,
dizzy swirl eyes, joyful closed eyes. Eyes and mouth clearly separated for texture swapping.
```

## 4. Enemy Sheet

```
[Style Block]
Enemy design sheet for BOB: "<ENEMY_TH> (<ENEMY_EN>)". <ENEMY_BLOCK>
Show: front, 3/4, back; idle, attack, hit, defeat poses; size comparison next to the chibi hero "กวาด";
color swatches. Cute-but-grumpy: big white eyes, thick angry brows, toothy grin. Not scary for kids.
```

- **เจ้าคราบ (Dirt Monster)**: `a lumpy pile of brown mud shaped like a teardrop blob, darker speckles, angry eyes, open mouth with small teeth, leaves small mud splats.`
- **สไลม์ (Slime)**: `glossy semi-translucent purple slime blob with pink highlights, small droplets splashing around it, sticky puddle under it.`
- **ขยะปีศาจ (Trash Monster)**: `grey metal trash can with a lid, dark inside with two big white glaring eyes and a frown, lid flaps open when attacking.`
- **เชื้อโรค (Germ)**: `small round green germ with two antennae and stubby feet, mischievous grin.`
- **บอสคราบยักษ์ (Boss)**: `giant glossy purple slime king, 2.5x hero height, golden crown, stretchy slime arms, droplets orbiting; show 3 phases (full, cracked, shrinking).`

## 5. Environment (Dirty / Clean)

```
[Style Block]
Isometric-like 3/4 top-down orthographic game level for BOB, camera pitched 45 degrees, landscape 16:9:
<ROOM>. Two versions side by side: LEFT dirty (desaturated, brown mud stains, purple slime puddles,
scattered trash, dust), RIGHT clean (bright, sparkling floor, warm sunlight from windows).
Leave a clear walkable floor area in the center for 4 characters and enemies. No characters.
```

`<ROOM>` = `a school classroom with wooden desks, chairs, blackboard, big windows` · `a school cafeteria with long tables, colorful chairs, food stall with striped awning` · `an outdoor school playground with slide, swings, trees, sand and grass` · `an underground boiler room with pipes, stone walls and a glowing purple boss door`

## 6. Props / Items / UI Icons

```
[Style Block]
Game item icon set for BOB, each item centered on its own transparent background, 3/4 view, same lighting,
thick soft outline, consistent size, grid 6x3: gold coin with star, yellow star, blue gem, red heart,
orange stopwatch, horseshoe magnet, yellow sponge, blue spray bottle, blue bucket, mop, yellow wet-floor sign,
orange traffic cone, black trash bag, blue recycle bin, toilet paper roll, soap bubble.
```

```
[Style Block]
Mobile game UI kit for BOB: big glossy yellow-gold PLAY button, blue SETTINGS and SHOP buttons, round
pause button, circular character portrait frame, green HP bar with navy frame, coin and star counters,
large round blue skill button, small round equipment buttons, character select cards in orange/purple/green/blue,
stage card with 3 stars, upgrade panel with HP/ATK/DEF bars. Rounded shapes, navy outlines, cream panels.
```

## 7. เคล็ดลับให้ได้ผลตรงกันทุกภาพ

1. แนบ Asset Sheet ทุกครั้ง และพูดว่า "Do not redesign"
2. ทำทีละตัว ทีละ sheet อย่าขอ 4 ตัวในภาพเดียว (รายละเอียดจะเพี้ยน)
3. ถ้าสีเพี้ยน ให้ระบุ HEX ซ้ำในส่วน CHARACTER_BLOCK
4. ถ้ารายละเอียดเฉพาะหาย (ปมผ้าคาดหัว, หูถัง, สายแว่น) ให้แก้ทีละจุดด้วย inpainting แทนการสร้างใหม่ทั้งภาพ
5. ภาพ ortho ให้ปิดเอฟเฟกต์และพื้นหลังทั้งหมด modeler ต้องการเส้นชัด ไม่ใช่ภาพสวย
