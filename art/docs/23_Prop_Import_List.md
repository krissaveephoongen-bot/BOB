# 23 · Tripo GLB to BOB: ชื่อที่ตั้ง, สเกล และสถานะนำเข้า

ไฟล์ดิบจาก Tripo อยู่ที่ `C:/Users/Jakgrits/OneDrive - Appworks Co., Ltd/Pictures/BOB` (98 ไฟล์, 5.8 GB).
ทุกไฟล์ Tripo normalize ให้ด้านยาว = 1.0 u จึงไม่มีสเกลจริงติดมา - คอลัมน์ *สูง* คือค่าที่ `bob_prop_cleanup.py` ใช้สเกลตอน clean
อิง `21_AA_Art_Bible.md` ข้อ 3.2 (ฮีโร่ 1.15, ศัตรู 0.7-0.9, บอส 2.2, ชั้นวาง 1.3, พร็อพเล็ก 0.35-0.6).

ขั้นตอน: `bob_thumbs.py` (ระบุตัว) -> `bob_clean_batch.py` + `art/aa/tripo_jobs.json` (clean 95 ตัว) -> `BobPropImporter` ใน Unity

## สถานะ 14 พ.ย. 2026

- clean เสร็จครบ **95/95 ok, 0 skipped, 0 failed** -> `art/aa/props/{SM_,SK_}*.glb|fbx|json` (98 ดิบ, ข้าม 3 ตัวที่ gen เสีย)
- งบ tri: ผ่านทุกตัวยกเว้น `SM_ac_indoor_unit` 1 356 tri (งบ 800) - mesh มี non-manifold 192 edges ทําให้ collapse หยุดที่ ~1 280-1 356 ต่อให้ decimate ซ้ํา; ตัวอื่นต่ํากว่างบหมด (total 656k tri, room class หยุดที่ predecimate 20k)
- บทเรียน pipeline: Tripo export ทํา vertex ซ้ําต่อ face -> edge เป็น non-manifold -> DECIMATE COLLAPSE หยุดที่ ~13% ของเดิม `bob_prop_cleanup.py` จึงต้อง merge ก่อน (`remove_doubles`) แล้วจึง decimate เป็นลูป (`decimate_to`)
- Unity: FBX 85 props -> `Assets/Art/Props` + palette, เมนู **Bob > Art > Build Prop Prefabs** -> `Assets/Resources/Props/SM_*.prefab`; 10 ตัวละคร -> `Assets/Art/Characters`, เมนู **Bob > Art > Build Character Prefabs** -> `Resources/Characters` (ยังไม่มี rig)
- วางในห้องแล้วผ่าน `BobGame.Set.cs > TryProp()`: โซฟา 3 ที่นั่ง, เตียง, ฟูก, ทีวี, โต๊ะเตี้ย, ชั้น S3, ถังขยะ S9, ล็อกเกอร์ S5, โต๊ะกลม/สตูล/เก้าอี้, ของบนเคาน์เตอร์ และของตกแต่งผนัง/มุมห้อง; placeholder ยังอยู่เป็น fallback
- ค่า transform ที่ติดมากับ prefab (สําคัญ): root มี **rotation X = 270** (แปลง Blender Z-up -> Unity Y-up) และ **scale = 100** (ชดเชย FBX cm) — โค้ดที่ตั้ง `localRotation = yaw` หรือ `localScale = 1` ทับจะทําให้โมเดลนอนราบ/หด 100 เท่า ต้องคูณต่อ: `Euler(0,yaw,0) * prefab.localRotation` และ `prefab.localScale * k`
- ทิศหน้า: ที่ yaw 0 โมเดลหันหน้าไป **-X**; yaw 90 -> +Z, yaw 180 -> +X, yaw 270 -> -Z (เข้ากล้อง) `TryProp` จึงใช้ yaw 270 เป็นค่าเริ่มต้น
- ตรวจด้วย `Bob > Run Game Test` (GUI ไม่ใส่ -batchmode เพราะต้องมี Game view) -> `GAME TEST PASSED` และ screenshot `unity/Logs/bob-game-3-*.png`
- **Diorama pass (14 พ.ย. 2026)**: ฉากเล่นเป็นชิ้นงานวางโชว์แล้ว — `BobGame.Diorama.cs` เขียนพื้นผิวทั้งหมดในโค้ด (หญ้า+ดอกเดซี่, deck ไม้ 4 แผ่น, หิน, plinth ไม้, กระดานหมากรุกสีเขียวน้ำทะเล, วอลเปเปอร์ลายจุด) ชิ้นงานเป็นเกาะหญ้าซ้อนชั้นดิน/หินแบบ "เค้กตัด" ที่ strata เป็น **geometry** แต่ละชั้นมี outline ของตัวเอง (texture แถบถี่ ๆ แบบเดิมถูก mip รวมจนกลายเป็นสีน้ำตาลเรียบ อ่านไม่ออกที่ซูมกล้องจริง) ต่อด้วย plinth ไม้สีวอลนัทขอบครีม, สวนต้นไม้/พุ่ม/ดอกไม้/หิน/เห็ด/โคมไฟรอบเกาะ (`DressDioramaRim()` สร้างด้วย primitive เพราะแพ็กไม่มีโมเดลสวน) และท้องฟ้าเขียนเป็น quad `BOB/Unlit` ลูกของกล้อง (แสง key ย้อมไม่ได้) มีเมฆ พระอาทิตย์ และทะเลหมอกใต้ชิ้นงาน
- **กล้องฉากเล่น (14 พ.ย. 2026, รอบสอง)**: เปลี่ยนจาก ortho 45° เป็น **perspective มุมต่ำหน้า** ตาม reference — pitch 30°, fov 32, กล้องนิ่งไม่ตามฮีโร่ (ฮีโร่เดินอยู่ในฉาก) `FitCamera3D` วางช่วงแนวตั้งจากยอดต้นไม้หลังสุดถึงขอบหน้าเกาะให้กินพื้นที่ ~86% ของจอ (ยอดไม้อยู่ใต้แถบบนของ HUD) แล้วปล่อยให้เกาะล้นขอบซ้าย/ขวาออกไป (จอแคบตัดมากกว่าจอกว้าง) ส่วนสวนหน้า/ชั้นหิน/plinth ไหลออกใต้แถวปุ่มล่าง — เฟรมแบบภาพถ่าย diorama ใกล้ ๆ; `FitSky` ปรับขนาด quad ตาม fov ที่ระยะ 70 unit พิกัดห้อง/การแตะไม่เปลี่ยน
- โมเดลบนผนังที่ใช้ผ่าน `TryProp` ใน `BuildRentalRoom()`: `SM_window_arched`, `SM_ac_wall_teal`, `SM_clock_alarm`

## ชุด Rig ใหม่ 14 พ.ย. 2026 (clean + import แล้ว)

ไฟล์อยู่ใน `C:/Users/Jakgrits/OneDrive - Appworks Co., Ltd/Pictures/BOB/Rig` — **ทุกไฟล์ normalize ด้านยาว = 2.0 u** (ชุดก่อนหน้าคือ 1.0) จึงต้องหารครึ่งก่อนใช้สเกลจากตารางเดิม
ผ่าน `bob_prop_cleanup.py` ด้วย `art/aa/map_jobs.json` แล้ว **ครบ 6/6** (tri ไม่เกินงบ map 8,000) FBX อยู่ที่ `unity/Assets/Art/Map`, prefab อยู่ที่ `unity/Assets/Resources/Props/SM_bob_map_*.prefab` (เมนู `Bob > Art > Build Map Prefabs`)

| ไฟล์ | prefab | tri ดิบ → clean | ใช้ในซีน `TaskRace.unity` |
| --- | --- | --- | --- |
| `floating island 3d model.glb` | `SM_bob_map_island_land` | 42,640 → 8,000 | เกาะ A |
| `floating island 3d model2.glb` | `SM_bob_map_island_land2` | 52,416 → 8,000 | เกาะ B (ไม่ mirror กับ A; เลื่อน X) |
| `stylized river water 3d model.glb` | `SM_bob_map_river_channel` | 51,638 → 8,000 | ช่องน้ำระหว่างเกาะ, top -0.15 |
| `wooden footbridge 3d model.glb` | `SM_bob_map_wooden_bridge` | 53,657 → 7,999 | 2 ตัว (เหนือ/ใต้), กว้าง 2 u, top y = 0 |
| `waterfall diorama 3d model.glb` | `SM_bob_map_cliff_waterfall` | 57,117 → 7,999 | ฉากหลังเหนือแม่น้ำ (z +9.2); ตัวโมเดลมีรางน้ำไม้ยื่นมาถึงน้ำ |
| `circular cobblestone plaza 3d model.glb` | `SM_bob_map_plaza` | 55,684 → 6,172 | ยังไม่ผูก (ลานกลาง/เกาะที่สาม) |

- **ไม่มี rig ของ bubble** ในชุดนี้ — ใช้ prefab จาก `art/aa/kit` ต่อ
- งบ tri ต่อชิ้น ≤ 8,000 บันทึกใน `03_Unity_Spec.md` แล้ว (รวม 6 ชิ้น ≤ 48,000)
- **เกาะเป็นชุดประกอบ** (surface + ก้อนหิน + ต้นไม้/ของลอย 338 / 42 ชิ้นต่อกัน) — bounds top จึงเป็นยอดไม้ ไม่ใช่พื้น: assembler ใช้ `AlignGround` (percentile 0.6 ของพื้น) แล้วปู slab `Field` แบนที่ y = 0 เป็นระนาบเล่น
- ตัวละคร rig (`bob_hero_sweep/spray/press_tripo`) มี skin แต่ **ไม่มี animation clip** และ **ยังไม่ผ่าน cleanup/import** — ซีนทดสอบใช้ `Resources/Characters/SK_hero_sweep` (mesh นิ่ง 1.15 u) ก่อน; ต้องมี AnimatorController ก่อนใช้แทน sprite

## ตัวละคร (SK_, ยังไม่มี rig)

| ไฟล์ (uuid = `tripo_convert_<uuid>.glb`) | ชื่อในเกม | class | สูง (u) |
| --- | --- | --- | --- |
| bob_boss_stain_giant_tripo | SK_boss_stain | boss | 2.2 |
| 1afe831f-8456-44ba-af0d-909ba897365f | SK_hero_sweep | char | 1.15 |
| 1c824107-f95c-4610-925a-d69493059cb4 | SK_enemy_slime_dark | char | 0.75 |
| 2cc1e0ad-537b-4bf6-a8d1-4421863377dd | SK_hero_spray | char | 1.15 |
| 4e0a9f40-cabe-411e-a22d-3623d3adfbf5 | SK_enemy_slime_mud | char | 0.8 |
| 8838f1b4-b292-4df1-810b-dbf94c814c54 | SK_enemy_slime_dust | char | 0.9 |
| cf003d15-e05e-4dbd-9f5b-a62676b054c0 | SK_hero_press | char | 1.15 |
| cf07386e-85b3-4494-88d0-2fda7e61615d | SK_enemy_slime_purple | char | 0.75 |
| eac52d09-a0ae-4761-942e-49d0c7102447 | SK_hero_bubble | char | 1.15 |
| ecc46427-0339-403d-b066-422184325189 | SK_enemy_slime_green | char | 0.75 |

## เฟอร์นิเจอร์ / ฉาก (room)

| ไฟล์ (uuid = `tripo_convert_<uuid>.glb`) | ชื่อในเกม | class | สูง (u) |
| --- | --- | --- | --- |
| bob_prop_bed_single_tripo | SM_bed_single_blue | room | 0.55 |
| 2072cc56-bf1d-47fa-9833-fd60af1d3e76 | SM_shelf_rack_metal | room | 1.3 |
| 26ce0aab-f783-4e43-987a-065899dd4e14 | SM_kitchen_unit_upper | room | 0.9 |
| 2923ad19-2b49-474f-938c-dca7ed008a27 | SM_door_metal | room | 2.0 |
| 36046c02-4c7e-4e1b-aa37-2dafaeca4dfd | SM_table_low_wood | room | 0.35 |
| 42212267-cd7c-4988-ac10-7134f7c3a749 | SM_pipes_valve | room | 1.0 |
| 487470a1-1e8b-4d06-b407-5348c57af885 | SM_sofa_3seat_tan | room | 0.85 |
| 540fe6d5-931e-4877-843a-a8dccc786c1a | SM_window_curtain | room | 1.4 |
| 5be038d2-84b7-4ea3-9fa7-c4ac6caea1d1 | SM_door_wood_open | room | 2.0 |
| 5f321553-7a94-4425-8a1c-c855e469cade | SM_sofa_2seat_orange | room | 0.8 |
| 615057bf-d216-4e67-9381-471003a4224a | SM_screen_mural | room | 1.4 |
| 674708b0-94c3-4819-b092-59c1821ff264 | SM_fence_wood | room | 1.0 |
| 76cbc377-e3a1-4ae9-9498-95fc813000b5 | SM_water_dispenser | room | 1.0 |
| 7ca2f5c6-a38d-4b4d-b5af-c4665632a2d0 | SM_table_round_cafe | room | 0.65 |
| 866daa53-aac1-41d0-81c1-0799fe4f6ddc | SM_sink_cabinet | room | 0.85 |
| 9371af3e-e253-4a3a-9d90-882c0ed29152 | SM_kitchen_unit_wood | room | 0.9 |
| 962a918a-e43e-44a0-8a89-f79d6886aa34 | SM_stall_awning | room | 2.0 |
| af3fba60-81ff-46ba-897d-8de7fc42ffec | SM_lockers_teal | room | 1.8 |
| afd48964-71c4-4541-979a-92ffb43feaa9 | SM_bakery_case | room | 1.2 |
| b05cd33d-bf4d-48ca-973d-daa6b38f35da | SM_street_lamp | room | 2.6 |
| b1b959fe-586c-4326-92fd-7b5f4028f65e | SM_chest_wood | room | 0.55 |
| b904565f-301e-492c-b2fe-7a8ca701fee3 | SM_chair_bentwood | room | 0.85 |
| c77889b9-a3da-45cb-be55-04ed62fc8432 | SM_kiosk_wood | room | 1.5 |
| d0955ac6-dbf9-468a-be77-bab3dd254e25 | SM_futon_rolled | room | 0.3 |
| d1317581-65aa-4da3-a70b-bf66402a2cc7 | SM_window_arched | room | 1.3 |
| d78eedaa-7245-42f8-aede-c290a3816ef1 | SM_dresser_cream | room | 1.0 |
| dc71244e-b854-4fc6-abbe-322e9d33375b | SM_washing_machine | room | 0.85 |
| ea0954c0-b79f-475e-8e64-94370c7f638e | SM_slide_kids | room | 1.5 |
| f82bb372-a74e-46d7-9e6f-8995f49497fc | SM_wardrobe_color | room | 1.7 |
| fcf88a5c-76d0-4510-813e-f42407dd3201 | SM_swing_kids | room | 1.8 |
| fd5de428-e055-404f-9882-8bcfa07dd317 | SM_bed_single_wood | room | 0.55 |

## พร็อพกลาง (prop_m)

| ไฟล์ (uuid = `tripo_convert_<uuid>.glb`) | ชื่อในเกม | class | สูง (u) |
| --- | --- | --- | --- |
| bob_prop_bucket_tripo | SM_bucket_blue | prop_m | 0.45 |
| 0ecd124a-6909-4559-b8df-650e93153ee5 | SM_rocks_grass | prop_m | 0.5 |
| 0eedc264-8fab-4ef4-8ac6-c54973e556de | SM_espresso_machine | prop_m | 0.5 |
| 12e717df-fa83-4968-9ea3-0aee9234c714 | SM_cctv_camera | prop_m | 0.28 |
| 2a8a60a7-43e3-4a0a-aa14-93bae976a1b0 | SM_pot_ceramic_gold | prop_m | 0.7 |
| 2f253a0b-4143-4a83-a60a-77cf2a69ab3c | SM_cone_traffic | prop_m | 0.5 |
| 313044a7-0c8e-4887-82f3-b2de9e8c39e4 | SM_jerrycan_blue | prop_m | 0.45 |
| 3661fa30-0479-4c15-965e-5fc95430ee13 | SM_meter_box | prop_m | 0.5 |
| 3b86ab9e-4ffd-4b26-9f1b-930d53509a2e | SM_bin_recycle_blue | prop_m | 1.0 |
| 3d8fec71-7345-4c7f-a350-6c4e80d335e2 | SM_griddle_stove | prop_m | 0.5 |
| 425c39fb-0178-4848-b9aa-9fe3c7e2a30d | SM_board_menu | prop_m | 0.8 |
| 44f3fe53-0cf8-4759-a51c-f894cc04cc96 | SM_ac_indoor_unit | prop_m | 0.4 |
| 4641edbf-cf0d-4142-9f96-2ab7528c710f | SM_breaker_box | prop_m | 0.45 |
| 467a0170-586b-4695-9422-13a3edd8bc6c | SM_gas_cylinder_red | prop_m | 0.6 |
| 4c2b380f-afe6-4eda-9da5-4fa01b3d1561 | SM_chalkboard_easel | prop_m | 1.2 |
| 50698ccb-dfc0-4db8-a583-d6f32495fc85 | SM_shoe_rack | prop_m | 0.55 |
| 595ad2c4-6b94-4ee1-9523-cd152233d72e | SM_trash_bag_black | prop_m | 0.5 |
| 5b341d7a-35fd-4885-896e-c7cb73dffc15 | SM_plinth_stone **(ข้าม)** | prop_m - plain plinth, no use yet | 1.3 |
| 5f81c18f-7afa-49c4-99a9-5b63169cfea7 | SM_sack_coffee | prop_m | 0.45 |
| 64fc864a-da8a-4fa9-b6d4-d0d8240fa090 | SM_pipe_fittings **(ข้าม)** | prop_m - loose pipe couplers, no use yet | 0.3 |
| 667208fd-8538-49ae-b271-b9f270eb13e3 | SM_bin_popup_blue | prop_m | 0.5 |
| 671635f4-6001-474a-baba-c38ac4c9e74c | SM_shelf_wall_books | prop_m | 0.7 |
| 6ef51361-46f8-4ccf-a216-4ff680d3db0e | SM_stool_red | prop_m | 0.45 |
| 757eb359-d81f-4b78-ba72-fb9aa6164d62 | SM_bin_trash_grey | prop_m | 1.0 |
| 7cdc46d5-b6b4-494c-9843-16d4c0b9e3c8 | SM_ac_wall_teal | prop_m | 0.45 |
| 7dd5fae6-e21a-462c-bccb-3df4c7a27c4b | SM_stanchion | prop_m | 1.0 |
| 80009009-9481-4edb-ad50-c95bbf8f47e0 | SM_radiator_red | prop_m | 0.6 |
| 8da42e49-2544-4d84-b576-0e4c2d02dba1 | SM_utensil_rack | prop_m | 0.6 |
| 94effc93-06f2-4efc-98cc-5c85c1f509f9 | SM_pot_on_burner | prop_m | 0.5 |
| 9d14d780-3e5b-4eee-8640-936c8ddc53f4 | SM_hose_reel | prop_m | 0.45 |
| a881ece9-3ecd-4288-9bd0-da3288012a28 | SM_crate_oranges | prop_m | 0.4 |
| ac00ecb1-793d-44eb-bc84-1dc6c9cc169a | SM_basket_clothes | prop_m | 0.45 |
| af7cbdfe-2cca-4c9f-a27f-e638076352ec | SM_heater_wall | prop_m | 0.3 |
| b55f456f-e116-409c-9976-1b8c315f728b | SM_sign_wet_floor | prop_m | 0.6 |
| bd23a918-15fd-4652-b4b4-6352039975da | SM_bucket_metal_blue | prop_m | 0.4 |
| c37b995b-dd36-4a13-bb83-a4eec2f0984f | SM_parking_block | prop_m | 0.25 |
| c5d1233f-4e2b-45ed-82bc-a662cd72eb7e | SM_pot_blue | prop_m | 0.28 |
| ce3ca72d-ddb3-425b-a692-c789d685ac25 | SM_plant_pot | prop_m | 0.8 |
| d4502bdc-4eec-4d0f-97af-87de2d63333e | SM_gas_cylinder_red_b | prop_m | 0.6 |
| daa2ebdb-38d8-43d4-8e73-4043c021ae01 | SM_fire_extinguisher | prop_m | 0.6 |
| dbe641aa-f5fc-4070-870a-2e5ca852c118 | SM_polisher_head_yellow | prop_m | 0.2 |
| e476cc32-0c30-4e47-9105-90741b25c035 | SM_sign_stand_wood | prop_m | 1.2 |
| e567d4c8-2d59-40ec-9610-74e19cbd8fbe | SM_ring_blocks **(ข้าม)** | prop_m - abstract ring, looks like a failed generation | 0.6 |
| eff11c95-f763-46e5-9ce5-0a0d66ca59d8 | SM_tv_flat | prop_m | 0.6 |
| f10b9b81-2f77-4ff8-a57d-bbaa8f48c0ee | SM_industrial_fan | prop_m | 0.6 |
| f8de3daa-3669-428d-83d3-65b6c19608ef | SM_dish_rack_blue | prop_m | 0.35 |
| fa1ca082-5d67-4747-b732-7cd4e7b9e089 | SM_bush_green | prop_m | 0.6 |

## พร็อพเล็ก / หยิบได้ (prop_s, prop_hand)

| ไฟล์ (uuid = `tripo_convert_<uuid>.glb`) | ชื่อในเกม | class | สูง (u) |
| --- | --- | --- | --- |
| 4e3967fd-564c-4b8d-a30f-9db1947806dd | SM_hose_coil | prop_hand | 0.25 |
| 551013b5-bc80-417f-8177-6ffe413ae5e7 | SM_gloves_pair | prop_hand | 0.28 |
| 558f5920-dc84-411f-84f0-b01c727dfd46 | SM_brush_pair_yellow | prop_hand | 0.18 |
| 590dcdb0-54d7-471d-a626-2b755338e16d | SM_detergent_pump | prop_hand | 0.32 |
| 6af0beea-dba3-47ab-8650-0f4201835315 | SM_clock_alarm | prop_s | 0.3 |
| 6cc8be52-76ca-424d-905d-8fa6a98c9f14 | SM_plates_stack | prop_hand | 0.14 |
| 7c699820-3a2e-45a3-b83c-01c0665be2a2 | SM_toilet_paper | prop_s | 0.13 |
| a6b6cd5e-62cc-49b5-8e90-964a26006501 | SM_brush_scrub_wood | prop_hand | 0.15 |
| b3afd3a9-b50e-4945-a946-975774fa3c81 | SM_spray_bottle | prop_hand | 0.3 |
| b3c11304-0db3-47ea-b094-6ed87f6e5b72 | SM_spray_bottle_label | prop_hand | 0.3 |

