# 09 · Leonardo.ai Full Prompts (ก็อปวางได้ทันที)

รวม 172 prompt ครบทุก asset เรียงตามลำดับที่ควรทำ ใช้คู่กับ `08_Leonardo_Production_Pack.md` (วิธีตั้งค่าและเหตุผล)

**ตั้งค่าทุกภาพ:** Prompt Enhance **Off** · Number of images 4 · Style preset 3D Render (หรือ Illustration ถ้าใกล้ concept กว่า ใช้ค่าเดียวตลอด) · จดเลข seed ของภาพที่เลือก
**ถ้าไม่มี Transparency:** สร้างพื้นขาวแล้วกด Remove Background หรือส่งมาให้ Claude ลบ
**ลำดับ:** ทำ `sweep_master.png` ให้ได้ก่อน แล้วจึงทำท่าอื่นของกวาดโดยใช้ master เป็น Character Reference จากนั้นทำตัวต่อไป

ไฟล์ CSV ฉบับเดียวกัน: `09_Leonardo_Full_Prompts.csv` (เปิดใน Excel/Google Sheets ใช้ติ๊กว่าทำแล้ว)


## Hero กวาด

### 1. `sweep_master.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: docs/concept/refs/sweep_ref.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, standing in a confident idle pose holding the tool, smiling, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 2. `sweep_idle.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, relaxed idle pose, tool held at the side, gentle smile, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 3. `sweep_walk_a.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, walking, left foot forward, right arm swinging forward, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 4. `sweep_walk_b.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, walking, right foot forward, left arm swinging forward, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 5. `sweep_run.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, running fast, leaning forward, tool held back, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 6. `sweep_carry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, walking while holding a small cardboard box above the head with both hands, tool slung across the back, effort face, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 7. `sweep_work.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, scrubbing the floor with the tool, focused face, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 8. `sweep_skill.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, dynamic special move pose swinging the tool, determined shout, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 9. `sweep_hit.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, stumbling backwards, dizzy swirl eyes, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 10. `sweep_cheer.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, jumping with both arms up, joyful closed eyes, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 11. `sweep_defeat.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, sitting on the floor crying with tears, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 12. `sweep_back.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, seen from directly behind, standing, back of the head and apron ties visible, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 13. `sweep_side.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, side profile view facing right, standing, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Hero กวาด · สีหน้า

### 14. `sweep_face_happy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, head and shoulders only, front view, big happy smile, open mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 15. `sweep_face_wink.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, head and shoulders only, front view, winking one eye, smiling, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 16. `sweep_face_angry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, head and shoulders only, front view, determined shout, angry eyebrows, open mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 17. `sweep_face_surprised.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, head and shoulders only, front view, surprised, wide eyes, small round mouth, a small red exclamation mark beside the head, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 18. `sweep_face_cry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, head and shoulders only, front view, crying with tears, sad mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 19. `sweep_face_effort.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, head and shoulders only, front view, effort face, eyes squeezed shut, gritting teeth, sweat drop, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 20. `sweep_face_dizzy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, head and shoulders only, front view, dizzy swirl eyes, wobbly mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 21. `sweep_face_joy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/sweep_master.png (High)

Prompt
```
boy named Kwad, messy dark brown hair, red headband knotted on the right side with two tails, orange t-shirt, cream apron with square front pocket, navy pants, brown short boots, holding a wooden broom with golden bristles tied with a red band, head and shoulders only, front view, joyful closed eyes, big smile, small sparkles, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Hero ฟอง

### 22. `bubble_master.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: docs/concept/refs/bubble_ref.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, standing in a confident idle pose holding the tool, smiling, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 23. `bubble_idle.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, relaxed idle pose, tool held at the side, gentle smile, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 24. `bubble_walk_a.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, walking, left foot forward, right arm swinging forward, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 25. `bubble_walk_b.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, walking, right foot forward, left arm swinging forward, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 26. `bubble_run.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, running fast, leaning forward, tool held back, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 27. `bubble_carry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, walking while holding a small cardboard box above the head with both hands, tool slung across the back, effort face, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 28. `bubble_work.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, scrubbing the floor with the tool, focused face, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 29. `bubble_skill.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, dynamic special move pose swinging the tool, determined shout, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 30. `bubble_hit.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, stumbling backwards, dizzy swirl eyes, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 31. `bubble_cheer.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, jumping with both arms up, joyful closed eyes, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 32. `bubble_defeat.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, sitting on the floor crying with tears, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 33. `bubble_back.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, seen from directly behind, standing, back of the head and apron ties visible, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 34. `bubble_side.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, side profile view facing right, standing, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Hero ฟอง · สีหน้า

### 35. `bubble_face_happy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, head and shoulders only, front view, big happy smile, open mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 36. `bubble_face_wink.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, head and shoulders only, front view, winking one eye, smiling, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 37. `bubble_face_angry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, head and shoulders only, front view, determined shout, angry eyebrows, open mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 38. `bubble_face_surprised.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, head and shoulders only, front view, surprised, wide eyes, small round mouth, a small red exclamation mark beside the head, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 39. `bubble_face_cry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, head and shoulders only, front view, crying with tears, sad mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 40. `bubble_face_effort.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, head and shoulders only, front view, effort face, eyes squeezed shut, gritting teeth, sweat drop, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 41. `bubble_face_dizzy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, head and shoulders only, front view, dizzy swirl eyes, wobbly mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 42. `bubble_face_joy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/bubble_master.png (High)

Prompt
```
girl named Fong, light brown hair in two space buns with side swept bangs, sky blue t-shirt, cream apron with square front pocket, navy shorts, brown short boots, holding a big yellow porous sponge, a few soap bubbles, head and shoulders only, front view, joyful closed eyes, big smile, small sparkles, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Hero ถู

### 43. `press_master.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: docs/concept/refs/press_ref.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, standing in a confident idle pose holding the tool, smiling, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 44. `press_idle.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, relaxed idle pose, tool held at the side, gentle smile, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 45. `press_walk_a.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, walking, left foot forward, right arm swinging forward, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 46. `press_walk_b.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, walking, right foot forward, left arm swinging forward, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 47. `press_run.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, running fast, leaning forward, tool held back, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 48. `press_carry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, walking while holding a small cardboard box above the head with both hands, tool slung across the back, effort face, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 49. `press_work.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, scrubbing the floor with the tool, focused face, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 50. `press_skill.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, dynamic special move pose swinging the tool, determined shout, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 51. `press_hit.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, stumbling backwards, dizzy swirl eyes, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 52. `press_cheer.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, jumping with both arms up, joyful closed eyes, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 53. `press_defeat.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, sitting on the floor crying with tears, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 54. `press_back.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, seen from directly behind, standing, back of the head and apron ties visible, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 55. `press_side.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, side profile view facing right, standing, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Hero ถู · สีหน้า

### 56. `press_face_happy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, head and shoulders only, front view, big happy smile, open mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 57. `press_face_wink.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, head and shoulders only, front view, winking one eye, smiling, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 58. `press_face_angry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, head and shoulders only, front view, determined shout, angry eyebrows, open mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 59. `press_face_surprised.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, head and shoulders only, front view, surprised, wide eyes, small round mouth, a small red exclamation mark beside the head, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 60. `press_face_cry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, head and shoulders only, front view, crying with tears, sad mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 61. `press_face_effort.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, head and shoulders only, front view, effort face, eyes squeezed shut, gritting teeth, sweat drop, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 62. `press_face_dizzy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, head and shoulders only, front view, dizzy swirl eyes, wobbly mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 63. `press_face_joy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/press_master.png (High)

Prompt
```
boy named Tu, wearing a silver metal bucket as a helmet with the handle hanging at the side, brown hair peeking out, green t-shirt, cream apron with square front pocket, navy pants, brown boots, holding a wooden mop with a fluffy white mop head, head and shoulders only, front view, joyful closed eyes, big smile, small sparkles, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Hero สเปรย์

### 64. `spray_master.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: docs/concept/refs/spray_ref.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, standing in a confident idle pose holding the tool, smiling, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 65. `spray_idle.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, relaxed idle pose, tool held at the side, gentle smile, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 66. `spray_walk_a.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, walking, left foot forward, right arm swinging forward, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 67. `spray_walk_b.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, walking, right foot forward, left arm swinging forward, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 68. `spray_run.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, running fast, leaning forward, tool held back, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 69. `spray_carry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, walking while holding a small cardboard box above the head with both hands, tool slung across the back, effort face, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 70. `spray_work.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, scrubbing the floor with the tool, focused face, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 71. `spray_skill.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, dynamic special move pose swinging the tool, determined shout, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 72. `spray_hit.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, stumbling backwards, dizzy swirl eyes, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 73. `spray_cheer.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, jumping with both arms up, joyful closed eyes, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 74. `spray_defeat.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, sitting on the floor crying with tears, facing three-quarter to the right, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 75. `spray_back.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, seen from directly behind, standing, back of the head and apron ties visible, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 76. `spray_side.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High) + Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, side profile view facing right, standing, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Hero สเปรย์ · สีหน้า

### 77. `spray_face_happy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, head and shoulders only, front view, big happy smile, open mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 78. `spray_face_wink.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, head and shoulders only, front view, winking one eye, smiling, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 79. `spray_face_angry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, head and shoulders only, front view, determined shout, angry eyebrows, open mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 80. `spray_face_surprised.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, head and shoulders only, front view, surprised, wide eyes, small round mouth, a small red exclamation mark beside the head, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 81. `spray_face_cry.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, head and shoulders only, front view, crying with tears, sad mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 82. `spray_face_effort.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, head and shoulders only, front view, effort face, eyes squeezed shut, gritting teeth, sweat drop, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 83. `spray_face_dizzy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, head and shoulders only, front view, dizzy swirl eyes, wobbly mouth, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 84. `spray_face_joy.png`
บันทึกที่ `incoming/heroes/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: incoming/heroes/spray_master.png (High)

Prompt
```
girl named Spray, black bob haircut with straight bangs, aviator goggles with light blue lenses and purple strap on top of the head, purple t-shirt, cream apron with square front pocket, navy shorts, brown boots, holding a pink trigger spray bottle with white nozzle, head and shoulders only, front view, joyful closed eyes, big smile, small sparkles, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, chibi proportions about 2.3 heads tall, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Enemy เจ้าคราบ

### 85. `stain_idle.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy mud monster, lumpy pile of brown mud shaped like a teardrop blob, darker speckles, small mud splats, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, idle, slightly squashed, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 86. `stain_attack.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy mud monster, lumpy pile of brown mud shaped like a teardrop blob, darker speckles, small mud splats, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, lunging forward to attack, stretched, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 87. `stain_hit.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy mud monster, lumpy pile of brown mud shaped like a teardrop blob, darker speckles, small mud splats, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, squashed from a hit, eyes shut tight, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 88. `stain_defeat.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy mud monster, lumpy pile of brown mud shaped like a teardrop blob, darker speckles, small mud splats, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, melting into sparkles, dizzy swirl eyes, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Enemy สไลม์

### 89. `grease_idle.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/grease_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy slime monster, glossy semi-translucent purple slime blob with pink highlights, small droplets around it, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, idle, slightly squashed, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 90. `grease_attack.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/grease_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy slime monster, glossy semi-translucent purple slime blob with pink highlights, small droplets around it, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, lunging forward to attack, stretched, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 91. `grease_hit.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/grease_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy slime monster, glossy semi-translucent purple slime blob with pink highlights, small droplets around it, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, squashed from a hit, eyes shut tight, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 92. `grease_defeat.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/grease_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy slime monster, glossy semi-translucent purple slime blob with pink highlights, small droplets around it, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, melting into sparkles, dizzy swirl eyes, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Enemy ขยะปีศาจ

### 93. `trash_idle.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/trash_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy trash can monster, grey metal trash can with a lid, dark inside with two big glaring white eyes, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, idle, slightly squashed, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 94. `trash_attack.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/trash_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy trash can monster, grey metal trash can with a lid, dark inside with two big glaring white eyes, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, lunging forward to attack, stretched, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 95. `trash_hit.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/trash_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy trash can monster, grey metal trash can with a lid, dark inside with two big glaring white eyes, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, squashed from a hit, eyes shut tight, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 96. `trash_defeat.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/trash_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy trash can monster, grey metal trash can with a lid, dark inside with two big glaring white eyes, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, melting into sparkles, dizzy swirl eyes, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Enemy เชื้อโรค

### 97. `germ_idle.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/germ_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy germ monster, small round green body with two antennae and stubby feet, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, idle, slightly squashed, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 98. `germ_attack.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/germ_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy germ monster, small round green body with two antennae and stubby feet, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, lunging forward to attack, stretched, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 99. `germ_hit.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/germ_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy germ monster, small round green body with two antennae and stubby feet, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, squashed from a hit, eyes shut tight, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 100. `germ_defeat.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/germ_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy germ monster, small round green body with two antennae and stubby feet, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, melting into sparkles, dizzy swirl eyes, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Enemy บอสคราบยักษ์

### 101. `boss_idle.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/boss_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
giant purple slime king boss, glossy, golden crown, stretchy slime arms, droplets orbiting, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, idle, slightly squashed, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 102. `boss_attack.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/boss_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
giant purple slime king boss, glossy, golden crown, stretchy slime arms, droplets orbiting, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, lunging forward to attack, stretched, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 103. `boss_hit.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/boss_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
giant purple slime king boss, glossy, golden crown, stretchy slime arms, droplets orbiting, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, squashed from a hit, eyes shut tight, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 104. `boss_defeat.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/boss_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
giant purple slime king boss, glossy, golden crown, stretchy slime arms, droplets orbiting, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, melting into sparkles, dizzy swirl eyes, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 105. `boss_taunt.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/boss_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
giant purple slime king boss, glossy, golden crown, stretchy slime arms, droplets orbiting, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, laughing with slime arms raised, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 106. `boss_slam.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/boss_ref.png (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
giant purple slime king boss, glossy, golden crown, stretchy slime arms, droplets orbiting, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, slamming both slime arms down on the floor, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Enemy ฝุ่น

### 107. `dust_idle.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy dust bunny monster, fluffy grey dust cloud with loose fluff, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, idle, slightly squashed, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 108. `dust_attack.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy dust bunny monster, fluffy grey dust cloud with loose fluff, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, lunging forward to attack, stretched, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 109. `dust_hit.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy dust bunny monster, fluffy grey dust cloud with loose fluff, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, squashed from a hit, eyes shut tight, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 110. `dust_defeat.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy dust bunny monster, fluffy grey dust cloud with loose fluff, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, melting into sparkles, dizzy swirl eyes, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Enemy สนิม

### 111. `rust_idle.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy rust monster, jagged orange brown rusty metal chunk with flaky spots, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, idle, slightly squashed, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 112. `rust_attack.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy rust monster, jagged orange brown rusty metal chunk with flaky spots, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, lunging forward to attack, stretched, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 113. `rust_hit.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy rust monster, jagged orange brown rusty metal chunk with flaky spots, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, squashed from a hit, eyes shut tight, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 114. `rust_defeat.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy rust monster, jagged orange brown rusty metal chunk with flaky spots, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, melting into sparkles, dizzy swirl eyes, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Enemy หินปูน

### 115. `scale_idle.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy limescale monster, pale blue hexagonal crystal with facets, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, idle, slightly squashed, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 116. `scale_attack.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy limescale monster, pale blue hexagonal crystal with facets, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, lunging forward to attack, stretched, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 117. `scale_hit.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy limescale monster, pale blue hexagonal crystal with facets, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, squashed from a hit, eyes shut tight, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```

### 118. `scale_defeat.png`
บันทึกที่ `incoming/enemies/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Character Reference: refs/stain_ref.png (style only) (Mid-High) + Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cute grumpy limescale monster, pale blue hexagonal crystal with facets, big white eyes, thick angry eyebrows, toothy grin, not scary for kids, melting into sparkles, dizzy swirl eyes, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, full body, centered, nothing cropped
```
Negative prompt
```
multiple characters, character lineup, grid, collage, text, letters, words, logo, watermark, signature, frame, border, background scenery, ground shadow, realistic, photo, adult proportions, tall body, extra fingers, extra limbs, deformed hands, cropped, cut off, blurry, thick black outline, anime style, different outfit, different hair
```


## Item

### 119. `item_coin.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
gold coin with a star emblem, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 120. `item_star.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
shiny yellow star, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 121. `item_gem.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
faceted blue gem, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 122. `item_heart.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
glossy red heart, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 123. `item_stopwatch.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
orange stopwatch, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 124. `item_magnet.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
red horseshoe magnet with silver tips, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 125. `item_bubble.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
clear soap bubble with a rainbow sheen, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 126. `item_chest_closed.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
closed wooden treasure chest with gold trim, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 127. `item_chest_open.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
open wooden treasure chest with gold coins and sparkles, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 128. `item_trophy.png`
บันทึกที่ `incoming/items/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Mid)

Prompt
```
gold trophy cup with a star, single game item icon, three-quarter view, soft outline, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```


## Prop

### 129. `wet_sign.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
yellow wet floor caution sign, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 130. `cone.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
orange traffic cone with a white stripe, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 131. `bucket.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
blue plastic bucket with water, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 132. `trash_bag.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
tied black trash bag, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 133. `trash_bin.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
grey metal trash bin, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 134. `recycle_bin.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
blue recycle bin with recycle symbol, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 135. `toilet_paper.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
roll of toilet paper, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 136. `spray_blue.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
blue cleaning spray bottle, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 137. `desk.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
wooden school desk, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 138. `chair.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
wooden school chair, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 139. `blackboard.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
green blackboard on a wooden frame, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 140. `locker.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
row of three colourful school lockers red orange blue, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 141. `window.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
white framed school window, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 142. `cafe_table.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
long cafeteria table with a food tray, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 143. `cafe_chair.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
red plastic cafeteria chair, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 144. `food_stall.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
school food stall with a red and white striped awning, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 145. `slide.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
playground slide with blue ladder and orange slide, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 146. `bench.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
wooden park bench, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 147. `tree.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
round green cartoon tree, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 148. `bush.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
green bush with small flowers, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 149. `boss_door.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
stone arch doorway with a glowing purple door, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```

### 150. `pipe.png`
บันทึกที่ `incoming/props/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/style_environments.png (Mid)

Prompt
```
grey metal pipe segment with joints, single game prop, top-down three-quarter view, camera pitched 40 degrees, BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, soft light blue fill, gentle ambient occlusion, bright clean colours, single subject, isolated on a transparent background, centered, nothing cropped
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline
```


## Background

### 151. `bg_classroom.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Style Reference: refs/style_environments.png (High) + Content Reference: refs/style_ingame.png (Low)

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, school classroom with wooden desks, chairs, a green blackboard and big windows, large clear walkable floor area in the centre, no characters, no text, clean, sparkling floor, warm sunlight, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 152. `bg_classroom_dirty.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Image to Image / Content Reference: incoming/backgrounds/bg_classroom.png (High) — same room

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, school classroom with wooden desks, chairs, a green blackboard and big windows, large clear walkable floor area in the centre, no characters, no text, dirty version of the same room, desaturated, brown mud stains, purple slime puddles, scattered trash and dust, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 153. `bg_cafeteria.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Style Reference: refs/style_environments.png (High) + Content Reference: refs/style_ingame.png (Low)

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, school cafeteria with long tables, colourful chairs and a food stall with a striped awning, large clear walkable floor area in the centre, no characters, no text, clean, sparkling floor, warm sunlight, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 154. `bg_cafeteria_dirty.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Image to Image / Content Reference: incoming/backgrounds/bg_cafeteria.png (High) — same room

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, school cafeteria with long tables, colourful chairs and a food stall with a striped awning, large clear walkable floor area in the centre, no characters, no text, dirty version of the same room, desaturated, brown mud stains, purple slime puddles, scattered trash and dust, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 155. `bg_playground.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Style Reference: refs/style_environments.png (High) + Content Reference: refs/style_ingame.png (Low)

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, outdoor school playground with a slide, swings, trees, sand and grass, large clear walkable floor area in the centre, no characters, no text, clean, sparkling floor, warm sunlight, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 156. `bg_playground_dirty.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Image to Image / Content Reference: incoming/backgrounds/bg_playground.png (High) — same room

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, outdoor school playground with a slide, swings, trees, sand and grass, large clear walkable floor area in the centre, no characters, no text, dirty version of the same room, desaturated, brown mud stains, purple slime puddles, scattered trash and dust, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 157. `bg_boiler.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Style Reference: refs/style_environments.png (High) + Content Reference: refs/style_ingame.png (Low)

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, underground boiler room with pipes, stone walls and a glowing purple door, large clear walkable floor area in the centre, no characters, no text, clean, sparkling floor, warm sunlight, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 158. `bg_boiler_dirty.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Image to Image / Content Reference: incoming/backgrounds/bg_boiler.png (High) — same room

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, underground boiler room with pipes, stone walls and a glowing purple door, large clear walkable floor area in the centre, no characters, no text, dirty version of the same room, desaturated, brown mud stains, purple slime puddles, scattered trash and dust, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 159. `bg_school.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Style Reference: refs/style_environments.png (High) + Content Reference: refs/style_ingame.png (Low)

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, school building exterior with a clock on the roof, trees and a path, large clear walkable floor area in the centre, no characters, no text, clean, sparkling floor, warm sunlight, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```

### 160. `bg_school_dirty.png`
บันทึกที่ `incoming/backgrounds/` · Phoenix · 3:2 · Transparency Off  
Image Guidance: Image to Image / Content Reference: incoming/backgrounds/bg_school.png (High) — same room

Prompt
```
top-down three-quarter orthographic mobile game level, camera pitched 45 degrees, school building exterior with a clock on the roof, trees and a path, large clear walkable floor area in the centre, no characters, no text, dirty version of the same room, desaturated, brown mud stains, purple slime puddles, scattered trash and dust, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, hand-painted gradients, soft lighting, bright colours
```
Negative prompt
```
characters, people, animals, text, letters, logo, watermark, frame, border, realistic, photo, blurry, fisheye, distorted perspective, cropped furniture
```


## UI

### 161. `ui_button_play.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
big glossy yellow gold rounded rectangle button, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 162. `ui_button_blue.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
glossy blue rounded rectangle button, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 163. `ui_button_skill.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
large round glossy blue skill button, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 164. `ui_button_round.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
small round cream button, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 165. `ui_portrait_frame.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
round character portrait frame, white and navy ring, empty centre, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 166. `ui_ribbon.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
red ribbon banner with folded ends, empty, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 167. `ui_panel_blue.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
cream rounded panel with a navy header bar, empty, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 168. `icon_skill_sweep.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
round orange skill icon with a golden broom swipe arc, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 169. `icon_skill_bubble.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
round blue skill icon with a bubble dome, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 170. `icon_skill_press.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
round green skill icon with a frozen stopwatch, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 171. `icon_skill_spray.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
round purple skill icon with a crosshair target, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```

### 172. `logo_bob_base.png`
บันทึกที่ `incoming/ui/` · Phoenix · 1:1 · Transparency On  
Image Guidance: Style Reference: refs/heroes_lineup.png (Low)

Prompt
```
game logo background without any letters: a red ribbon banner over a round yellow and blue badge with a crossed broom and mop, mobile game UI element, glossy, rounded, navy outline, no text, BOB Battle of the Broom 2.5D stylized cartoon, soft 3D volume, bright clean colours, isolated on a transparent background, centered
```
Negative prompt
```
multiple objects, grid, collage, character, person, text, letters, logo, watermark, frame, border, background scenery, realistic, photo, cropped, blurry, thick black outline, letters, numbers
```
