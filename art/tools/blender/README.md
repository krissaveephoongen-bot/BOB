# Blender tools (no artist pipeline)

Spec: `art/docs/21_AA_Art_Bible.md` section 9. Tested with Blender 5.3 as the pip module (`pip install bpy`, Python 3.13), headless, no GPU.

| Script | What it does | Output |
| --- | --- | --- |
| `bob_kit.py` | Builds the 8 heroes (sweep, bubble, press, spray, wash, iron, duster, vac) and 4 enemies (dirt, slime, trash, boss_slime) from primitives, rigs them (shared skeleton, rigid skinning), keys idle/walk/cheer (heroes) and idle/caught (enemies), one material + palette texture | `SK_<id>.glb`, `SK_<id>.fbx`, `T_BOB_Palette.png`, `kit_report.json` |
| `bob_prop_cleanup.py` | Cleans a raw AI image-to-3D or CC0 mesh: join, merge, drop floating bits, decimate to budget, scale to game height, pivot bottom centre, snap colours to the palette | `SM_<name>.glb`, `.fbx`, `.json` |
| `bob_prop_preview.py` | Renders raw and cleaned `.glb` side by side near the game camera angle (Cycles CPU) | one `.png` |
| `../tripo/tripo_gen.py` | Creates a prop through the Tripo API from a spec in `../tripo/specs/` (needs API credits, which are separate from the Tripo web plan credits) | `<name>_tripo.glb` + credit log `.json` |

```bash
# with Blender installed
blender -b -P art/tools/blender/bob_kit.py -- --out art/aa/kit
blender -b -P art/tools/blender/bob_prop_cleanup.py -- raw/bucket.glb --name prop_bucket --class prop_m --height 0.45 --out art/aa/props

# AI prop: limit the snap to the 2-4 concept colours of the prop (ignores baked lighting, removes speckles)
python art/tools/blender/bob_prop_cleanup.py art/aa/raw/prop_bucket_tripo_20261113.glb --name prop_bucket --class prop_m --height 0.45 --colours bubble_shirt,shoe
python art/tools/blender/bob_prop_preview.py art/aa/raw/prop_bucket_tripo_20261113.glb art/aa/props/SM_prop_bucket.glb --out art/docs/aa/aa_prop_bucket.png

# or without Blender
python3 -m venv .bpy && .bpy/bin/pip install bpy
.bpy/bin/python art/tools/blender/bob_kit.py --out art/aa/kit
```

After `bob_kit.py`, refresh the web preview (`art/docs/ux/BOB_AA_Art_Bible.html` reads `BOB_AA_Kit_Models.js`):

```bash
python3 - <<'E'
import base64, glob, os, json
out = {os.path.basename(f)[3:-4]: base64.b64encode(open(f, 'rb').read()).decode()
       for f in sorted(glob.glob('art/aa/kit/SK_*.glb'))}
open('art/docs/ux/BOB_AA_Kit_Models.js', 'w').write(
    "// generated from art/aa/kit/*.glb (art/tools/blender/bob_kit.py), base64\n"
    "window.BOB_KIT=" + json.dumps(out) + ";\nwindow.BOB_KIT_REPORT=" + open('art/aa/kit/kit_report.json').read() + ";\n")
E
```

Palette cells: the order of `PALETTE` in `bob_kit.py` is the cell order on the 16x16 grid. Add colours at the end only, never reorder (UVs point at cells).

The kit models are blockouts: faces are geometry for preview; production faces are a flipbook texture (Art Bible 6.3).

Characters from Tripo (heroes, enemies) keep their painted texture instead of the palette snap:

```
python art/tools/blender/bob_prop_cleanup.py art/aa/raw/hero_sweep_tripo_20261113.glb --name hero_sweep_tripo --class hero --height 1.15 --keep-texture --prefix SK_ --out art/aa/chars
```

`--min-island 0.08` drops a stray image frame that came with the model. `bob_prop_preview.py --fit width` frames long furniture by its widest side.

`--rot-z <deg>` turns a prop so its front faces -Y. The pip `bpy` module cannot read meshopt-compressed GLB (files made by gltfpack): decode first with `@gltf-transform/core` + `meshoptimizer` (read, dispose `EXT_meshopt_compression`, write).
