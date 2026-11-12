# 08 · Leonardo.ai Production Pack: สร้าง asset จริงทีละไฟล์

ใช้แทน `07_ChatGPT_Production_Pack.md` ชื่อไฟล์และโฟลเดอร์ `incoming\` ใช้แบบเดียวกัน
> **หมายเหตุ:** ชื่อโมเดล เมนู และฟีเจอร์ของ Leonardo เปลี่ยนบ่อย ชื่อในเอกสารนี้อ้างอิงตามที่รู้ ณ ปลายปี 2026 ถ้าในแอปไม่ตรง ให้หาฟีเจอร์ที่ทำหน้าที่เดียวกัน (โดยเฉพาะ Character Reference, Style Reference, Transparency, Training)

---

## 0. ไฟล์อ้างอิงที่เตรียมไว้ให้แล้ว

`bob-art\docs\concept\refs\` (ตัดจากภาพ concept และขยายแล้ว ใช้อัปโหลดเป็น Image Guidance)

| ไฟล์ | ใช้เป็น |
| --- | --- |
| `sweep_ref.png`, `bubble_ref.png`, `press_ref.png`, `spray_ref.png` | Character Reference ของฮีโร่แต่ละตัว |
| `stain_ref.png`, `grease_ref.png`, `trash_ref.png`, `germ_ref.png`, `boss_ref.png` | Content/Character Reference ของศัตรู |
| `heroes_lineup.png`, `sweep_poses.png` | Style Reference (สไตล์รวม, ท่าทาง) |
| `sweep_turnaround.png`, `sweep_expressions.png` | อ้างอิงมุมหน้า/ข้าง/หลัง และสีหน้า |
| `style_ingame.png`, `style_environments.png` | Style Reference ของฉาก |

---

## 1. ตั้งค่าที่ใช้ทุกครั้ง (Image Generation)

| การตั้งค่า | ค่าแนะนำ | หมายเหตุ |
| --- | --- | --- |
| Model | **Leonardo Phoenix** (รุ่นล่าสุดที่มี) | ทำตาม prompt ดี รองรับ Character Reference; ทางเลือก: Lucid Origin / Flux (สไตล์ cartoon สะอาด) |
| Style / Preset | **3D Render** หรือ **Illustration** | ลองทั้งสองกับ Hero Master แล้วเลือกอันที่ใกล้ concept ที่สุด แล้วใช้ค่านั้นตลอด |
| Contrast | Medium | |
| Prompt Enhance | **Off** | กันไม่ให้ระบบเติมคำจนตัวละครเพี้ยน |
| Image Dimensions | ตัวละคร/ไอเทม/UI **1:1 (1024×1024)**, ฉาก **3:2 (1536×1024)** | |
| Number of Images | 4 | เลือกภาพที่ดีที่สุด |
| **Transparency** | **On** สำหรับตัวละคร ศัตรู ไอเทม พร็อพ UI | ถ้าโมเดลที่เลือกไม่มี toggle นี้ ให้สร้างพื้นขาวล้วน แล้วใช้ **Remove Background** ในเมนูภาพ หรือส่งมาให้ Claude ลบ |
| Seed | จดเลข seed ของ Hero Master ไว้ | ใช้ seed เดิมตอนสั่งท่าอื่นช่วยให้นิ่งขึ้น |
| Upscale | Universal Upscaler 1.5–2× เฉพาะภาพที่ใช้จริง | ประหยัดเครดิต |

### Negative prompt (ใช้กับทุก asset ถ้าโมเดลรองรับ)
```
realistic, photo, photorealistic, adult proportions, tall body, extra fingers, extra limbs, deformed hands,
text, letters, watermark, signature, logo, frame, border, background scenery, ground shadow, cropped,
cut off, blurry, noisy, thick black outline, anime style, different outfit, different hair, multiple characters
```
(ฉากให้ตัด `background scenery` ออก, character sheet ให้ตัด `text, multiple characters` ออก)

### Style suffix (ต่อท้ายทุก prompt)
```
BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall,
soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline,
warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours,
isolated on a transparent background, full body, centered
```

---

## 2. ฮีโร่ (ทำก่อน: กวาด → ฟอง → ถู → สเปรย์)

### Image Guidance สำหรับฮีโร่
1. **Character Reference** = `<id>_ref.png` ความแรง **High** (ถ้าหน้าเพี้ยน) หรือ **Mid** (ถ้าท่าแข็งเกิน)
2. **Style Reference** = `heroes_lineup.png` ความแรง Mid
3. หลังได้ Hero Master แล้ว ให้เปลี่ยน Character Reference เป็นภาพ Master ที่ได้ (นิ่งกว่า concept ที่ขยายมา)

| id | `<HERO>` |
| --- | --- |
| sweep | `boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band` |
| bubble | `girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles` |
| press | `boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head` |
| spray | `girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle` |

**2.1 Hero Master** → `incoming\heroes\<id>_master.png`
```
<HERO>, standing in a confident idle pose holding the tool, smiling, facing three-quarter to the right, <Style suffix>
```

**2.2 ท่าหลัก** (Character Reference = Master) → `incoming\heroes\<id>_<pose>.png`
```
<HERO>, <POSE>, facing three-quarter to the right, <Style suffix>
```
| pose | `<POSE>` |
| --- | --- |
| idle | relaxed idle pose, tool held at the side, gentle smile |
| walk_a | walking, left foot forward, right arm swinging forward |
| walk_b | walking, right foot forward, left arm swinging forward |
| run | running fast, leaning forward, tool held back |
| carry | walking while holding a small box above the head with both hands, tool slung across the back, effort face |
| work | scrubbing the floor with the tool, focused face |
| skill | dynamic special move pose swinging the tool, determined shout |
| hit | stumbling backwards, dizzy face |
| cheer | jumping with both arms up, joyful closed eyes |
| defeat | sitting on the floor crying |
| back | seen from directly behind, standing |
| side | side profile facing right, standing |

> ถ้าท่ายากออกมาไม่ตรง ให้ใช้ **Image Guidance → Pose / Depth / Edge (ControlNet)** โดยใส่ภาพท่าจาก placeholder เดิม (`unity\Assets\Art\Sprites\Characters\<id>\<id>_<pose>.png` เฟรมแรก) หรือ `sweep_poses.png` เป็นตัวกำหนดท่า

**2.3 สีหน้า** (ขนาด 1:1, Character Reference = Master) → `<id>_face_<expr>.png`
```
<HERO>, head and shoulders only, front view, expression: <EXPR>, <Style suffix>
```
`<EXPR>`: happy · wink · determined shout (`angry`) · surprised with a small red exclamation mark · crying with tears (`cry`) · effort gritting teeth · dizzy swirl eyes · joyful closed eyes with sparkles (`joy`)

**2.4 (ทางเลือก) เทรนโมเดลตัวละคร** เมื่อมีภาพที่ดีของตัวเดียวกัน 10–20 ภาพ (Master + ท่า + สีหน้า, พื้นหลังสะอาด)
- เมนู **Training & Datasets / Elements** → สร้าง dataset ต่อ 1 ตัวละคร → เลือกประเภท Character → ตั้ง trigger word เช่น `bobkwad`
- หลังเทรนเสร็จ ใช้ trigger word ใน prompt แทนคำบรรยายยาว ตัวละครจะนิ่งกว่า Character Reference มาก เหมาะกับการสั่งท่าจำนวนมาก
- ตรวจว่าแพ็กเกจของคุณมีสิทธิ์เทรนและเครดิตพอ

---

## 3. ศัตรู

Image Guidance: **Content/Character Reference** = `<id>_ref.png` (Mid–High), Style Reference = `heroes_lineup.png` (Low–Mid)

| id | `<ENEMY>` |
| --- | --- |
| stain | `cute grumpy mud monster, lumpy pile of brown mud shaped like a teardrop blob, darker speckles, small mud splats` |
| grease | `cute grumpy slime monster, glossy semi-translucent purple slime blob with pink highlights, small droplets around it` |
| trash | `cute grumpy trash can monster, grey metal trash can with a lid, dark inside with two big glaring white eyes` |
| germ | `cute grumpy germ monster, small round green body with two antennae and stubby feet` |
| boss | `giant purple slime king boss, glossy, golden crown, stretchy slime arms, droplets orbiting` |
| dust | `cute grumpy dust bunny, fluffy grey dust cloud with loose fluff` |
| rust | `cute grumpy rust monster, jagged orange brown rusty metal chunk with flaky spots` |
| scale | `cute grumpy limescale monster, pale blue hexagonal crystal with facets` |

```
<ENEMY>, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, <POSE>, <Style suffix>
```
| ไฟล์ `incoming\enemies\` | `<POSE>` |
| --- | --- |
| `<id>_idle.png` | idle, slightly squashed |
| `<id>_attack.png` | lunging forward to attack, stretched |
| `<id>_hit.png` | squashed from a hit, eyes shut tight |
| `<id>_defeat.png` | melting into sparkles, dizzy swirl eyes |
| `boss_taunt.png`, `boss_slam.png` | laughing with arms up / slamming both arms down |

---

## 4. ไอเทมและพร็อพ (ทีละชิ้น ได้ผลดีกว่ากริดใน Leonardo)

Style Reference = `heroes_lineup.png` (Mid), Transparency On, 1:1
```
<ITEM>, single game item icon, three-quarter view, soft outline, <Style suffix>
```
ไอเทม → `incoming\items\item_<name>.png`: coin (gold coin with a star) · star · gem (blue gem) · heart (red heart) · stopwatch (orange stopwatch) · magnet (red horseshoe magnet) · bubble (soap bubble) · chest_closed · chest_open (with sparkles) · trophy (gold trophy)

พร็อพ → `incoming\props\<name>.png` (เปลี่ยน `single game item icon` เป็น `single game prop, top-down three-quarter view, camera pitched 40 degrees`): wet_sign · cone · bucket · trash_bag · trash_bin · recycle_bin · toilet_paper · spray_blue · desk · chair · blackboard · locker · window · cafe_table · cafe_chair · food_stall · slide · bench · tree · bush · boss_door · pipe

---

## 5. ฉาก (3:2, Transparency Off)

Style Reference = `style_environments.png` (Mid–High), Content Reference = `style_ingame.png` (Low, ช่วยมุมกล้อง)
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, <ROOM>, large clear walkable
floor area in the centre, no characters, no text, <CLEAN_OR_DIRTY>, BOB Battle of the Broom 2.5D stylized cartoon,
soft lighting, bright colours
```
- `<ROOM>`: school classroom with desks, chairs, blackboard and big windows · school cafeteria with long tables, colourful chairs and a food stall · outdoor school playground with slide, swings, trees, sand and grass · underground boiler room with pipes, stone walls and a glowing purple door · school building exterior with clock tower and trees
- `<CLEAN_OR_DIRTY>`: `clean, sparkling floor, warm sunlight` / `dirty, desaturated, brown mud stains, purple slime puddles, scattered trash and dust`

ทำ clean ก่อน แล้วสร้าง dirty โดยใช้ภาพ clean เป็น **Image to Image / Content Reference ความแรงสูง** หรือใช้ **Canvas (inpaint)** ระบายคราบเพิ่มบนภาพ clean จะได้ห้องเดียวกันเป๊ะ
ไฟล์ → `incoming\backgrounds\bg_<room>.png`, `bg_<room>_dirty.png` (room = classroom, cafeteria, playground, boiler, school)

---

## 6. UI และโลโก้ (1:1, Transparency On, ทีละชิ้น)

```
<UI>, mobile game UI element, glossy, rounded, navy outline, no text, <Style suffix>
```
`<UI>` → ไฟล์ `incoming\ui\`: big yellow gold rounded button (`ui_button_play`) · blue rounded button (`ui_button_blue`) · large round blue skill button (`ui_button_skill`) · round portrait frame (`ui_portrait_frame`) · red ribbon banner (`ui_ribbon`) · cream panel with navy header (`ui_panel_blue`) · skill icons: golden broom swipe / bubble dome / frozen stopwatch / crosshair target (`icon_skill_sweep|bubble|press|spray`)

โลโก้: AI มักเขียนตัวอักษรผิด แนะนำให้สร้าง **พื้นโลโก้ไม่มีตัวอักษร** (red ribbon banner over a round yellow and blue badge with crossed broom and mop) แล้วให้ Claude หรือนักออกแบบใส่คำว่า BOB / BATTLE OF THE BROOM ด้วยฟอนต์จริง

---

## 7. ส่งกลับมาให้ Claude

วางไฟล์ใน `SweapBattle\bob-art\incoming\<หมวด>\` ตามชื่อข้างบน แล้วบอกในเธรด Claude จะลบพื้นหลังที่เหลือ ตัด ปรับขนาด (ตัวละคร 256 px/เฟรม เท้าที่จุดหมุน) ประกอบเป็น sprite sheet ชื่อเดิม อัปเดต manifest และเข้า Unity ให้

## 8. เงื่อนไขการใช้งานเชิงพาณิชย์

- สิทธิ์การใช้ภาพเชิงพาณิชย์และความเป็นส่วนตัวของภาพ (Private mode) ขึ้นกับ **แพ็กเกจ Leonardo ที่ใช้** แผนฟรีมักทำให้ภาพเป็นสาธารณะและมีข้อจำกัดมากกว่า ให้ตรวจ Terms of Service และหน้า Pricing ปัจจุบันก่อนใช้ในเกมที่ขาย
- อย่าอัปโหลดภาพของบุคคลอื่นหรือเกมอื่นเป็น reference ใช้เฉพาะภาพ concept ของ BOB เอง
- ภาพ ref1–ref5 ที่ได้มาจากเว็บภายนอกห้ามใช้เป็น Image Guidance
