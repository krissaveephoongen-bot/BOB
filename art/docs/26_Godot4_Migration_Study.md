# 26 · ศึกษาการย้าย BOB จาก Unity 6 → Godot 4.7 (.NET)

เวอร์ชัน 0.1 · 15 พ.ย. 2026 · สถานะ: ศึกษาความเป็นไปได้ ยังไม่เริ่มย้าย

> **หมายเหตุ 16 พ.ย. 2026:** เอกสารนี้วิเคราะห์สแตกตอนที่แอป Unity ยังใช้ Nakama (`Net/`, `Coop` ฯลฯ) **วันเดียวกันนั้นฝั่ง Unity ถูกเปลี่ยนเป็น pure UGS** (`27_UGS_Multiplayer.md`) — แถวที่พูดถึง `Net/`, Nakama Unity SDK, `BobServer`/`BobOnline` และจำนวนบรรทัดของสองโฟลเดอร์นั้น **เก่าแล้ว** (ไฟล์ถูกลบ); ส่วน `server/` (Nakama + JS + opcode) ยังอยู่และยังใช้เป็นแหล่งกติกาฝั่งเซิร์ฟเวอร์ ถ้าวันหนึ่งจะย้ายจริง ฝั่ง client ที่ต้องพอร์ตคือ UGS 6 สคริปต์ใน `Assets/Scripts/UGS/` ไม่ใช่ Nakama client

> ขอบเขต: วิเคราะห์จากโค้ดจริงใน `unity/` และเอกสาร Godot 4.7 (พ.ย. 2026) เท่านั้น
> เครื่องนี้ยังไม่ได้ติดตั้ง Godot และยังไม่ได้รัน benchmark บนมือถือ — ตัวเลขประสิทธิภาพทุกตัวในเอกสารนี้เป็น "ต้องวัดใน spike" ไม่ใช่ผลวัด

---

## 0. สรุปผู้บริหาร

1. **ย้ายได้ แต่ไม่ใช่ "เปลี่ยน engine แล้วกด build"** — งานหนักไม่ได้อยู่ที่กติกาเกม แต่อยู่ที่ UI ที่สร้างด้วยโค้ดทั้งหมด, shader 11 ตัว และ editor tooling 4.9k บรรทัด
2. **ขอบเขตจริงเล็กกว่าที่เห็น** — เกมที่ ship มี 2 ฉาก: `Game` (lobby/menus) + `TaskRace`; โหมด Co-op ถูก retire แล้ว; `TaskRace_Duo` / `TaskRace_Team` มีอยู่แต่ไม่เข้า APK
3. **ของที่ยกไปได้ทั้งดุ้น** — Nakama server + JS modules + RPC/opcode contract, กติกาที่เป็น pure C# (`Rules/`), art source (PNG/FBX/GLB/WAV/MP3), ฟอนต์ไทย, เอกสารดีไซน์ และ test harness แบบ .NET (`unity-check/`)
4. **ความเสี่ยงอันดับ 1 คือ C# บนมือถือ** — Godot 4.7 ยังระบุว่า C# สำหรับ Android/iOS เป็น **experimental** และ C# export ไป **web ไม่ได้**; โปรเจกต์นี้ตั้งเป้า Android + iOS (doc 03) จึงต้องตัดสินใจเรื่องภาษาให้ชัดก่อนลงมือ
5. **ข้อแนะนำ** — ทำ **spike 10 วันทำการ** บน vertical slice ก่อน (1 เกาะ + HUD + 2 มินิเกม + Nakama + APK) แล้วใช้เกณฑ์ go/no-go ท้ายเอกสาร; ถ้าผ่านค่อยพอร์ตเป็นเฟสโดยให้ Unity เป็น reference ห้ามทำ big-bang และไม่ควรหยุด feature งานปัจจุบันถ้ายังไม่มีเหตุผลบังคับ

---

## 1. สถานะปัจจุบัน (จาก repo)

| หัวข้อ | ค่า |
| --- | --- |
| Engine | Unity 6000.6.8f1, Built-in Render Pipeline (ไม่ใช่ URP ตามที่ doc 00 เขียนไว้) |
| เป้าหมาย | Android ARM64 + IL2CPP, min API 26, landscape; doc 03 ระบุ iOS ด้วย |
| APK ล่าสุด | `Builds/BOB-prototype-v1.apk` = 67.6 MB (debug-signed) |
| ฉากที่เข้า build | `Assets/Scenes/Game.unity`, `Assets/Scenes/TaskRace.unity` |
| ฉากที่มีแต่ไม่เข้า build | `TaskRace_Duo`, `TaskRace_Team`, `Coop` (retired), `SmokeTest` |
| ขนาดฉาก TaskRace | 388 GameObject, 39 PrefabInstance, 179 MonoBehaviour (Duo 392/38/184, Team 402/38/194) |
| โค้ด | 100 ไฟล์ `.cs` = 22,911 บรรทัด |
| Runtime code | 18,001 บรรทัด (ตัด `Editor/` 4,910 ออก) |
| Asset | 878 MB (ในนั้น `TripoRaw/` 776 MB เป็น raw ที่ไม่ต้องย้าย; `Art/` 57 MB, `Audio/` 11 MB, `Resources/` 21 MB, `Scenes/` 8.8 MB) |
| Model / sprite / prefab | FBX 109, GLB 13, PNG 262, prefab 110, animation/controller asset 95 |
| Shader ที่เขียนเอง | 11 ตัว + `BobCurvedWorld.hlsl` (ดู §3) |
| Audio | WAV 38, MP3 4 |
| ฟอนต์ | Mitr + IBM Plex Sans Thai (TMP SDF; ต้นทางเป็น TTF) |
| Server | Nakama 3.22 + Postgres 16 ใน Docker; `server/match.js` ~30.7 KB; opcode 1–21 |
| Firebase | ใช้เฉพาะ **App Distribution** ใน `distribute-android.sh` ไม่มี Firebase SDK ในเกม |
| Analytics | วิ่งผ่าน Nakama RPC `bob_log_event` ไม่ใช่ SDK ภายนอก |
| วิดีโอ | เลือกแล้ว 7 จาก 13 คลิป Hailuo (35.4 MB) วางเป็นชื่อ runtime ที่ `unity/Assets/Video` — รอ Unity transcode 720p/no-audio และยังไม่ commit; ที่เหลือเก็บที่ `Leonardo/Video` |

### 1.1 คลิปวิดีโอที่เพิ่งเพิ่ม (15 พ.ย. 2026)

- 13 ไฟล์ MP4 (Hailuo AI), 1344×768 (7:4), 24 fps, 6.6 s/คลิป, 3.8–10.7 MB/ไฟล์, รวม 74 MB; อยู่ที่ `unity/Assets/Leonardo/Video` และยังไม่ถูก track
- เนื้อหา: เกาะลอย/อารีล 2 คลิป, ห้อง cutaway 1 คลิป, twin-island trailer shot 1 คลิป, และ close-up 9 ตัว (เครื่องซักผ้า, เครื่องกาแฟ, ลูกบิดประตู, trash chute, ชั้นกระจก, ถังแยกขยะ, เคาน์เตอร์ครัว, อ่างล้างจาน, ไม้กวาดใต้โต๊ะ)
- Unity: importer เฝ้าแค่ `Assets/Video/` → คลิปใน `Leonardo/Video` จะถูก import แบบ default และ `Resources.Load` มองไม่เห็น; 15 พ.ย. 2026 เลือก 7 คลิปและ transcode แล้ว — ไฟล์จริงอยู่ที่ `unity/Assets/Resources/Video` (720p, ไม่มี audio) และติดไปกับ APK 78.6 MB แล้ว
- ข้อควรระวัง: 1344×768 (1.750) → importer 1280×720 (1.778) บีบแนวนอน ~1.6%; คลิป AI 6.6 s อาจไม่ loop เนียน; 74 MB ถ้าใส่หมดจะทำให้ APK โตจาก 67.6 MB; **ตัดสินใจแล้ว 16 พ.ย. 2026:** `.gitattributes` ครอบ `*.mp4` และของมีเดียอื่นผ่าน Git LFS — คลิป 7 ไฟล์ที่ใช้จริง (`Resources/Video`) และต้นฉบับทั้ง 13 ไฟล์ (`Leonardo/Video`) commit ผ่าน LFS แล้ว
- Godot: native `VideoStreamPlayer` อ่านได้แค่ Ogg Theora → ต้อง transcode คลิปที่จะใช้เป็น `.ogv` (หรือใช้ third-party native video plugin); งานนี้ ~1–2 วันสำหรับ 7 คลิปที่เลือก

**คลิปที่เลือกและผูกเข้าเกมแล้ว (15 พ.ย. 2026)**

| runtime name | คลิป | ใช้ที่ | ขนาด |
| --- | --- | --- | ---: |
| `Video/menu_loop` | cutaway wooden room | S13 + S11 | 4.0 MB |
| `Video/tutorial_T05` | three sorting bins | แยกขยะ | 4.1 MB |
| `Video/tutorial_T07` | kitchen sink | ล้างจาน | 7.2 MB |
| `Video/tutorial_T08` | glass display shelf | เช็ดกระจก | 4.8 MB |
| `Video/tutorial_T10` | broom under table | ไล่ฝุ่นใต้โต๊ะ | 4.6 MB |
| `Video/tutorial_T11` | espresso machine | เติมน้ำยา | 4.8 MB |
| `Video/tutorial_T15` | washing machine | จำลำดับเครื่องซักผ้า | 4.1 MB |

- เลือก cutaway room เป็นเมนูเพราะ first/last frame diff = `3.4` (trailer `53.9`, aerial `57.6`, sink `52.8` จะเห็นรอยตัดทุก 6.6 วิ); การ์ดสอนถือแค่ 4 วิ จึงใช้ close-up ที่ seam สูงได้
- ทั้ง 7 ไฟล์ transcode เป็น 720p/no-audio แล้วใน `Resources/Video` (+12.8 MB ใน APK); Godot port ยังต้องแปลงเป็น `.ogv` 7 ไฟล์ และควรลด bitrate เพราะคลิปสอนแสดงในกรอบ 240×135 เท่านั้น
- อีก 6 คลิปที่ยังไม่ผูก (island cutaway, aerial fly-by, twin-island trailer, trash chute, door lock, kitchen counter) ใช้ต่อกับ T01/T02/T04/T06/T09/T12/T13 ได้ในรอบถัดไป

**สิ่งที่ยังไม่เสร็จและกระทบขอบเขตพอร์ต:** การ์ด S28 ยังเป็น 2D uGUI (3D prop stage ยังไม่ต่อ), T16 emergency stain, server seed contract, AI เดินจริง (คู่แข่งเป็น pace model) — ถ้าย้ายตอนนี้ งานเหล่านี้จะถูกทำบน Godot แทน Unity

---

## 2. โค้ด: อะไรยกได้ อะไรต้องเขียนใหม่

| โฟลเดอร์ | ไฟล์ | บรรทัด | สถานะพอร์ต | หมายเหตุ |
| --- | ---: | ---: | --- | --- |
| `Rules/` | 7 | 841 | **A — ใช้ซ้ำเกือบทั้งหมด** | 3 ไฟล์เป็น pure C#; `ChapterDefinition` / `StageConfig` เป็น ScriptableObject → เปลี่ยนเป็น `Resource` (.tres) |
| `Net/` | 2 | 360 | **B — แปลง adapter** | `unity-check/` พิสูจน์แล้วว่า Nakama .NET client ใช้ได้นอก Unity; ตัด `UnityWebRequestAdapter` |
| `Game/` | 16 | 5,204 | **C — เขียนใหม่บางส่วน** | lobby/menus/HUD/old solo room; UI สร้างด้วยโค้ด |
| `TaskRace/` | 31 | 9,175 | **C — เขียนใหม่** | หัวใจเกม: mode manager, task spot, round director, HUD, มินิเกม, rival |
| `Editor/` | 24 | 4,910 | **C/D — สร้างใหม่หรือทิ้ง** | scene builder/import/verify; บางตัวมีค่าเป็น regression test |
| `Coop/` | 5 | 1,343 | **D — ไม่ต้องย้าย** | โหมด retired |
| `Modular/` | 7 | 537 | **C — บางส่วนอาจตัดทิ้ง** | ของโหมดเก่า (cart / room) |
| `UI/`, `Art/` | 7 | 466 | **C — เขียนใหม่** | theme/hold button/tween/sfx/sprite animator |
| `Dev/` | 1 | 75 | **B** | smoke test → headless runner |
| **รวม** | **100** | **22,911** | | ต้องแตะจริง ~15.5k runtime + 4.9k editor |

### 2.1 ไฟล์ใหญ่ที่สุดและปลายทางใน Godot

| Unity file (บรรทัด) | ปลายทางฝั่ง Godot |
| --- | --- |
| `TaskRace/BobMinigameHost.cs` (1,363) | `Control` + `_GuiInput` ต่อการ์ดแต่ละ mechanic |
| `TaskRace/BobTaskRaceHud.cs` (1,264) | `CanvasLayer` + `Control`/container + theme |
| `TaskRace/TaskRaceModeManager.cs` (939) | `Node` + group/gate collider + timer (ไม่ต้องมี NavMesh agent) |
| `Editor/BobMapAssemblerEditor.cs` (873) | `EditorScript` / `@tool` หรือ rebuild ฉากครั้งเดียว |
| `Game/BobGame.Hud.cs` (794) | `Control` + theme/containers |
| `TaskRace/TaskSpot.cs` (781) | `Node3D` + `Area3D`/raycast |
| `Game/BobGame.World.cs` (590) | `Node3D` + วัสดุ/`MultiMeshInstance3D` |
| `TaskRace/BobMinigameStage.cs` (582) | `SubViewport` + `SubViewportContainer` (ถ้าทำ 3D stage) |
| `Editor/StorySeriesCheck.cs` (574) | headless test runner |
| `Game/BobGame.Diorama.cs` (570) | `Node3D` + `GPUParticles3D`/`CPUParticles3D` |
| `Game/BobGame.Tasks.cs` (544) | `Node` + state machine |
| `Editor/TaskRaceVerify.cs` (524) | headless runner + screenshot |

---

## 3. แผนที่ Unity → Godot 4.7

| Unity | Godot 4.7 | ระดับงาน | หมายเหตุเฉพาะโปรเจกต์นี้ |
| --- | --- | --- | --- |
| `MonoBehaviour` + `Update` | `Node` + `_Process(double)` / `_PhysicsProcess` | ต่ำ | 50 ครั้งในโค้ด; เวลาส่วนใหญ่ใช้ `Time.*` 205 ครั้ง → ตรวจ pause semantics |
| Coroutine (`IEnumerator` 33) | `async/await` + `ToSignal` / `Tween` | กลาง | มี `Task` 1,050 refs อยู่แล้ว; ระวัง continuation thread และ lifetime |
| uGUI + TMP (RectTransform 164, TMP_Text 57) | `Control` + `Theme` + `FontFile` | **สูง** | UI สร้างด้วยโค้ดทั้งหมด (`BobUi.cs` เป็น factory) ต้องมี UI kit ฝั่ง Godot |
| `CanvasScaler` 1920×1080 match height | project viewport 1920×1080 + stretch `canvas_items` + aspect `expand` | กลาง | ต้องเทียบ 16:9 / 19.5:9 / 4:3 และ safe area |
| `SafeArea` component | `DisplayServer.GetDisplaySafeArea()` | ต่ำ | มีใน 4.7 |
| `Sprite` + `Animator` flipbook | `AnimatedSprite3D` + `SpriteFrames` | กลาง | สร้าง `SpriteFrames` จาก `bob-art-manifest.json`; ตัด Animator controller ได้ |
| FBX/GLB prop | native import (FBX ผ่าน ufbx ตั้งแต่ 4.3) | กลาง | 110 prefab ต้องประกอบ scene ใหม่; เก็บ palette UV |
| `CharacterController` | `CharacterBody3D` + `move_and_slide()` | ต่ำ | ฮีโร่เดินแบน X–Z อยู่แล้ว (`BobTaskRaceHero`) |
| `NavMeshSurface/Link/Obstacle` | `NavigationRegion3D` / `NavigationLink3D` / `NavigationObstacle3D` | กลาง | **เกมเพลย์ไม่ได้ใช้ NavMeshAgent** — ฮีโร่เดินเอง บอทใช้ waypoint; ใช้ collider/gate แทนได้ และคง path check ไว้เป็น test |
| `RenderTexture` (41 refs) | `SubViewport` + `TextureRect` | กลาง | ใช้กับการ์ด 3D stage, วิดีโอ, tutorial; ระวังต้นทุน viewport บนมือถือ |
| `VideoPlayer` | `VideoStreamPlayer` + `VideoStreamTheora` | ต่ำ–กลาง | native รองรับ **Ogg Theora เท่านั้น**; มี 13 คลิป MP4 แล้ว → ต้อง transcode เฉพาะที่ใช้ (~1–2 วัน) |
| `AudioSource` (8) | `AudioStreamPlayer` / `AudioStreamPlayer3D` | ต่ำ | WAV/MP3 รองรับ |
| `ParticleSystem` (41) | `GPUParticles3D` / `CPUParticles3D` | กลาง | 6 ระบบ < 200 live → `CPUParticles3D` พอ |
| `OnRenderImage` + `BobPostGrade`/`BobBloom` | `WorldEnvironment` (Glow, Exposure, Adjustment) เป็นหลัก; `CompositorEffect` เฉพาะ pass พิเศษ | กลาง | Mobile renderer รองรับ Glow + CompositorEffect; Compatibility ไม่รองรับ |
| `PlayerPrefs` (85 refs, 18 keys) | `ConfigFile` ที่ `user://` | ต่ำ | คง key เดิม (`bob.coins`, `bob.owned`, …) |
| `ScriptableObject` | `Resource` + `.tres` | ต่ำ | `ChapterDefinition`, `StageConfig`, `BobArtLibrary` |
| `UnityEditor` tools (4,910) | `@tool` / `EditorScript` / `EditorPlugin` / `EditorImportPlugin` | **สูง** | scene builders คือ source of truth ของฉาก; ทางลัดคือ export static geometry เป็น glTF แล้ว rebuild เฉพาะ gameplay node |
| Nakama Unity SDK | Nakama .NET client + adapter | กลาง | `BobServer` มี branch ที่ไม่ใช้ Unity อยู่แล้ว; socket callback ต้อง marshal มาหา main thread |
| `-executeMethod` verify | headless scene + CLI + golden screenshot | กลาง | ต้องพอร์ต `TaskRaceVerify`/`StoryCycle` checks ก่อน เพื่อไม่ให้ regression หลุด |
| Android build script | export preset (arm64) + Firebase CLI | กลาง | C# Android ต้อง .NET 9+; LAN HTTP ต้องเปิด cleartext traffic ใน Android manifest |

### 3.1 Shader ที่ต้องพอร์ต (11 ตัว)

`BOB/CurvedWorld` (outline + toon + shadow caster ที่ดัด vertex), `BOB/ToonLit`, `BOB/Outline`, `BOB/Dressing`, `BOB/GroundAO`, `BOB/CloudSea`, `BOB/SkyNDC`, `BOB/Unlit`, `BOB/UiShine`, `Hidden/BOB/Bloom`, `Hidden/BOB/PostGrade` + `BobCurvedWorld.hlsl`

- สูตรความโค้งเป็น engine-agnostic: `worldPos.y -= _BobCurvature * _CurvatureScale * dot(d, d)` โดย `d = worldPos.xz - camera.xz` → ย้ายเข้า `vertex()` ได้ตรง ๆ
- Outline ใช้ `next_pass` + inverted hull ได้; แต่ **เงา** ของ vertex ที่ดัดแล้วเป็นจุดที่ต้องทดลองก่อน (Godot ไม่มี ShadowCaster pass แบบ HLSL ให้เขียนตรง ๆ)
- `SkyNDC` เปลี่ยนเป็น `ProceduralSkyMaterial`/sky shader หรือ fullscreen layer; `UiShine` เปลี่ยนเป็น CanvasItem shader
- PostGrade/Bloom มีโอกาสใช้ของ built-in แทนได้มาก (Glow/Threshold/Exposure/Saturation) → ลดงานเขียน shader ลง

---

## 4. สิ่งที่ยกไปได้เลย (ไม่ต้องเขียนใหม่)

1. **Server ทั้งก้อน** — Nakama + Postgres + `server/modules/*.js` + RPC + matchmaking + opcode 1–21; Godot เป็นแค่ client ใหม่
2. **กติกา/ตัวเลข** — `Rules/BobRules.cs`, `BobMatch.cs`, `BobTaskRaceRules.cs`, ตาราง chapter/stage; port เป็น C# เดิมได้
3. **Networking logic** — `Net/BobServer.cs` + `BobOnline.cs` ใช้ .NET client path เดียวกับ `unity-check/` (net8.0 + `NakamaClient 3.*`)
4. **Art source** — PNG 262 (sprite sheet + UI), FBX 109, GLB 13, WAV/MP3; manifest `bob-art-manifest.json` บอก frame/fps/pivot/border ครบ → เขียน importer ฝั่ง Godot ได้
5. **ฟอนต์ไทย** — Mitr + IBM Plex Sans Thai (TTF) ใช้ `FontFile` + Thai shaping ของ TextServer Advanced; ดีกว่าสถานะปัจจุบันที่บางจอใช้ฟอนต์ OS
6. **เอกสารและ UX spec** — หน้าจอ 30+ จอ, ขนาด u/dp, สี, เสียง, safe area ใช้เป็น acceptance criteria ได้ทันที
7. **Test corpus** — `unity-check/` (spot check + เทียบ C# กับ server 25 seeds) และ log/ภาพ verify ใช้เป็น baseline

---

## 5. สิ่งที่ต้องเขียนใหม่ (เรียงตามความเสี่ยง)

1. **UI ทั้งเกม** — `BobUi` 254 บรรทัดเป็นแค่ factory; HUD 3 ไฟล์ใหญ่ (794 + 1,264 + 1,363) สร้าง layout ในโค้ด ต้องสร้าง Godot UI kit (theme, nine-patch, container, focus/touch, safe area)
2. **HUD + มินิเกม S28** — 8 mechanic (trace/scrub/tap/sort/hold/fill/sequence/timed) + 3D stage; logic โอนได้ แต่ input/raycast/animation ต้องเขียนใหม่
3. **Scene/tooling** — 110 prefab + 388 node + builder 4.9k; ทางเลือกคือ (a) พอร์ต builder (ได้ repeatability) หรือ (b) export static geometry → glTF แล้วประกอบ gameplay ด้วยมือ (เร็วกว่าแต่เสีย pipeline)
4. **Shader/lighting/VFX parity** — 11 shader + แสง 4 ชั้น + เงาโค้ง + bloom; ต้องมี golden capture เทียบ Unity
5. **Save/settings** — key 18 ตัว + volume/vibrate/reduce motion/tutorial; ใช้ `ConfigFile` ง่าย แต่ต้องมี migration
6. **Build/CI** — export preset, signing, version code bump สำหรับ Firebase App Distribution, cleartext HTTP, device matrix

---

## 6. ความเสี่ยง (เรียงตามผลกระทบ)

| # | ความเสี่ยง | หลักฐาน | ผลกระทบ | มาตรการ |
| --- | --- | --- | --- | --- |
| R1 | C# บนมือถือยัง experimental; C# export web ไม่ได้ | Godot 4.7 docs: mobile experimental, Android ต้อง .NET 9+, web ไม่รองรับ C# | ถ้า iOS/Web เป็นเป้าจริง อาจต้องเปลี่ยนเป็น GDScript = rewrite เกือบทั้งหมด | ตัดสินภาษา/แพลตฟอร์มก่อน; spike APK + iOS build (ถ้ามี Mac) |
| R2 | Visual parity (shader/เงา/แสง) | doc 24: เกมอาศัย curved world + toon + shadow caster + bloom + sky NDC | เกมอาจ "หน้าตาเปลี่ยน" และเสียเวลา tune | พอร์ต curved world + toon 1 วัสดุก่อน; วัดภาพ side-by-side; งบ 20–30% ของเวลา |
| R3 | UI rewrite | UI สร้างด้วย uGUI/TMP ในโค้ด; 164 RectTransform, 57 TMP_Text, 3 HUD ไฟล์ใหญ่ | UI เพี้ยน/ใช้เวลาเกิน | สร้าง UI kit + screenshot test ที่ 1920×1080 และ 2400×1080 |
| R4 | Editor tooling/scene source | 4,910 บรรทัด UnityEditor; 110 prefab; scene builder 873 + 455 + 446 | ถ้าไม่พอร์ต builder จะ rebuild ฉากยาก | เก็บ Unity ไว้เป็น reference; ลอง glTF export; พอร์ตเฉพาะ importer/verify |
| R5 | Performance บนเครื่อง 2 GB | เป้า 30–60 fps; 4 SubViewport, 400 node, bloom, particles | เครื่องล่างอาจตก | วัดตั้งแต่ spike; ลด viewport/particle; ใช้ MultiMesh; resolution scaling |
| R6 | Nakama C# socket/threading | Unity ใช้ `useMainThread`; นอก Unity ใช้ `WebSocketStdlibAdapter` | event race/crash | ใช้ `CallDeferred`/queue ระบายใน `_Process`; ทดสอบกับ server จริง |
| R7 | ฟอนต์/ข้อความไทย | ปัจจุบันยังมี legacy Text ปน; TMP SDF หลายจอ | ข้อความตัด/เพี้ยน | bundle TTF + theme; ทดสอบ Thai shaping/line break/safe area |
| R8 | Test/CI คนละระบบ | verify ปัจจุบันพึ่ง `-executeMethod` + ภาพใน `Logs/` | regression หลุด | พอร์ต headless runner + golden image ก่อนพอร์ตเกมเพลย์ |
| R9 | ความรู้ทีม/ระบบนิเวศ | โปรเจกต์ผูก Unity 6 + Nakama Unity SDK | งานช้าช่วงแรก | spike + คู่มือสั้น + คง server/กติกาเดิม |
| R10 | คลิปวิดีโอ MP4 ใช้กับ Godot native ไม่ได้ | มี 13 คลิป MP4 รวม 74 MB; core รองรับแค่ Theora | ต้อง transcode/คัดคลิป; เสี่ยง APK โต | transcode `.ogv` เฉพาะคลิปที่ใช้, เลือก 1 เมนู + tutorial ที่จำเป็น, วัดขนาดจริง |

---

## 7. ประมาณงาน

สมมติฐาน: 1 dev ที่รู้ Unity/C# และมี AI ช่วย, งานขนานได้บางส่วน, ไม่รวมทำคอนเทนต์ใหม่, ไม่รวม iOS ถ้าไม่มี Mac

| เฟส | งาน | สัปดาห์ |
| --- | --- | ---: |
| 0 | Spike (§8) | 2 |
| 1 | Project skeleton + asset import pipeline (FBX/PNG/audio/SpriteFrames/Resource) | 2–3 |
| 2 | World/camera/hero/controls (1 เกาะ, curved world, tap/joystick) | 2–3 |
| 3 | Task systems: `TaskSpot`, `TaskRaceRound`, `TaskRaceModeManager`, rival/bot | 3–4 |
| 4 | HUD + S28 มินิเกม 8 mechanic + pause/result/chapter card | 4–5 |
| 5 | Lobby/menus/chapter select/shop/save | 2–3 |
| 6 | Online Nakama (adapter, socket, match flow, analytics) | 1–2 |
| 7 | Shader/lighting/VFX parity | 2–3 (ขนาน) |
| 8 | Android build + device QA + perf + distribution + transcode `.ogv` ที่ใช้จริง | 2–3 |
| 9 | Verification harness + golden captures | 1–2 (ขนาน) |
| | **รวมแบบทำขนาน** | **12–18** |
| | **ถ้าทำทีละเฟส** | **19–28** |

เทียบทางเลือก GDScript: ต้องเขียนใหม่ทั้ง 15.5k runtime (ไม่ได้ reuse C#) → **+4–8 สัปดาห์** แต่ได้ mobile ที่ stable กว่า, official Nakama GDScript SDK, และ web export ได้

---

## 8. Spike 10 วันทำการ (ต้องผ่านก่อนตัดสินใจ)

เป้าหมาย: พิสูจน์ 5 อย่างที่เสี่ยงที่สุด โดยไม่แตะ repo Unity

| วัน | สิ่งที่ทำ | ผ่านเมื่อ |
| --- | --- | --- |
| 1–2 | ติดตั้ง Godot 4.7.2 .NET; สร้าง project; import FBX 10 ชิ้น + hero sheet 1 ตัว + ฟอนต์ไทย | import ได้, วัสดุ/palette ใช้ได้, ฟอนต์ไทยแสดงถูก |
| 3–4 | 1 เกาะ 50 prop + curved world + toon + outline + แสง/เงา | ภาพ side-by-side กับ Unity ต่างไม่เกินที่ยอมรับ; 60 fps desktop |
| 5–6 | `CharacterBody3D` + joystick + tap-to-walk + camera follow | เดิน/แตะ/ชนขอบได้เท่า Unity |
| 7–8 | HUD (เวลา/รายการงาน) + การ์ด 2 mechanic (tap, sort) + SubViewport stage | เล่นจบ 1 งาน, ข้อความไทยไม่เพี้ยน |
| 9 | Nakama sign-in/join/progress กับ server Docker | ได้ progress จาก server จริง; offline fallback ทำงาน |
| 10 | Export APK C# → เครื่องจริง | 30 fps บนเครื่อง 2 GB, ไม่ crash 30 นาที, วัด RAM/ขนาด APK |

**Go/No-Go**

- ผ่าน: 30 fps เครื่องล่าง, APK ≤ ~100 MB, visual parity ยอมรับได้, งาน 10 วันจบตามแผน → ไปเฟส 1
- ไม่ผ่าน: C# Android รันไม่ได้/ช้าเกิน, shader ใช้เกิน 4 สัปดาห์, หรือประเมินรวม > 1.5 เท่าของตาราง → คง Unity หรือเปลี่ยนไป GDScript/โปรเจกต์ใหม่
- ต้องบันทึกผลลงเอกสารนี้ก่อนเริ่มเฟสถัดไป

---

## 9. ทางเลือกและคำแนะนำ

| ทางเลือก | ต้นทุน | ความเสี่ยง | เหมาะเมื่อ |
| --- | --- | --- | --- |
| **A. คง Unity ต่อ** | 0 | licence/แพลตฟอร์มผูกกับ Unity | ต้อง ship เร็ว, ยังมี feature ค้าง, ทีมถนัด Unity |
| **B. พอร์ต Godot + C#** | 12–18 สัปดาห์ | มือถือ experimental, web ไม่ได้, shader/UI | ต้องการเปิดโค้ด/ลดค่า licence แต่ยังอยู่ Android/iOS และรับความเสี่ยงได้ |
| **C. พอร์ต Godot + GDScript** | 16–24 สัปดาห์ | rewrite เยอะ, performance ต้องวัด | ต้องรองรับ web ด้วย หรือไม่ต้องการ .NET บนมือถือ |
| **D. Hybrid: คง BOB บน Unity + เริ่มโปรเจกต์ใหม่บน Godot** | ต่ำ | ไม่ได้ย้ายเกมนี้ | เป้าหมายคือ "อยากเรียน/อยากใช้ Godot" มากกว่าอยากย้ายเกมที่เกือบเสร็จ |

**คำแนะนำตามเหตุผลที่อยากย้าย**

- ถ้าเหตุผลคือ **licence/ค่าใช้จ่าย/การควบคุมแพลตฟอร์ม**: ทางเลือก B สมเหตุสมผล แต่ต้องผ่าน spike และควรย้ายหลัง content lock; ระหว่างนี้คง Unity เป็น source of truth
- ถ้าเหตุผลคือ **อยากเรียน Godot**: ทางเลือก D — ทำ prototype เล็กแยก (1 เกาะ + 1 มินิเกม + Nakama) ไม่ต้องแบก 23k บรรทัด
- ถ้าต้องมี **iOS และ/หรือ Web**: เลือก C (GDScript) ตั้งแต่ต้น; C# จะตันเรื่อง web และเสี่ยงบน iOS
- ถ้ามี **เดดไลน์ใกล้**: ทางเลือก A; การย้ายตอนนี้จะทำให้ฟีเจอร์ที่กำลังทำ (S13, series, ambient) หยุดหรือต้องทำซ้ำ

---

## 10. ถ้าตัดสินใจไป: หลักการทำ

1. **Tag/แช่แข็ง Unity เป็น reference** — เก็บ commit ที่ parity ไว้; ภาพ/ค่าตัวเลขทุกจอใช้เทียบ
2. **เริ่มจาก TaskRace ไม่ใช่ lobby** — เป็นหัวใจและมี test/ภาพ baseline เยอะ; lobby ตามมาทีหลัง
3. **พอร์ต test ก่อนพอร์ตเกม** — `unity-check/` + verify headless + golden screenshot; ไม่งั้นจะไม่รู้ว่า parity จริง
4. **หนึ่งฉาก หนึ่งระบบ** — อย่าแปลงทั้งโปรเจกต์พร้อมกัน; ปิด feature flag ฝั่ง Godot
5. **Server ห้ามแตะ** — คง Nakama/JS/opcode; ถ้าจำเป็นต้องแก้ contract ให้ทำพร้อมกันทั้งสอง client
6. **วัดภาพและ fps ทุก milestone** — ใช้กล้อง/มุม/ขนาดเดียวกับ Unity; เก็บ Log ไว้ใน `Logs/`
7. **แยก branch/folder** — เช่น `godot/` ไม่ปนกับ `unity/`; ห้ามให้ tooling ฝั่งหนึ่งไปแก้ asset ของอีกฝั่ง

---

## 11. คำถามที่ต้องตอบก่อนเริ่ม

1. เหตุผลหลักที่อยากย้ายคืออะไร: licence/ค่าใช้จ่าย, ควบคุมแพลตฟอร์ม, อยากเรียน, หรือต้องการ iOS/Web?
2. เป้าหมายแพลตฟอร์มจริงคือ Android อย่างเดียว หรือต้อง iOS/Web ด้วย?
3. ยอมรับ C# ที่ยัง experimental บนมือถือ หรือต้องการ GDScript?
4. มีเดดไลน์/feature freeze ที่ต้อง ship เวอร์ชัน Unity ก่อนหรือไม่?
5. ยอมรับ visual drift ได้แค่ไหน (เช่น สี/เงา/ขอบ อาจต่างจาก Unity เล็กน้อย)?

---

## 12. แหล่งอ้างอิง

- [Godot 4.7.2 stable download (.NET edition)](https://godotengine.org/download/windows/)
- [Godot 4.7 — C# basics (platform support / .NET requirements)](https://docs.godotengine.org/en/4.7/tutorials/scripting/c_sharp/c_sharp_basics.html)
- [Godot 4.7 — Exporting for Android (C# experimental)](https://docs.godotengine.org/en/4.7/tutorials/export/exporting_for_android.html)
- [Godot 4.7 — Exporting for iOS (C# experimental, macOS/Xcode)](https://docs.godotengine.org/en/4.7/tutorials/export/exporting_for_ios.html)
- [Godot 4.6 — Playing videos (native = Ogg Theora only)](https://docs.godotengine.org/en/4.6/tutorials/animation/playing_videos.html)
- [Godot 4.7 — Renderers (Mobile รองรับ Glow/CompositorEffect)](https://docs.godotengine.org/en/4.7/tutorials/rendering/renderers.html)
- [Godot 4.7 release notes (VirtualJoystick, mobile)](https://godotengine.org/releases/4.7/)
- [Godot 4.3 — improved ufbx importer](https://godotengine.org/article/introducing-the-improved-ufbx-importer-in-godot-4-3/)
- [Nakama Godot 4 client guide (official, GDScript; C# ใช้ .NET client)](https://heroiclabs.com/docs/nakama/client-libraries/godot/)
- [Nakama Godot SDK repository](https://github.com/heroiclabs/nakama-godot)
- [Godot DisplayServer — get_display_safe_area](https://docs.godotengine.org/en/4.7/classes/class_displayserver.html)
