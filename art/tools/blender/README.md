# Blender tools (no artist pipeline)

Spec: `art/docs/21_AA_Art_Bible.md` section 9. Tested with Blender 5.3 as the pip module (`pip install bpy`, Python 3.13), headless, no GPU.

| Script | What it does | Output |
| --- | --- | --- |
| `bob_kit.py` | Builds the 4 heroes (sweep, bubble, press, spray) and 4 enemies (dirt, slime, trash, boss_slime) from primitives, rigs them (shared skeleton, rigid skinning), keys idle/walk/cheer (heroes) and idle/caught (enemies), one material + palette texture | `SK_<id>.glb`, `SK_<id>.fbx`, `T_BOB_Palette.png`, `kit_report.json` |
| `bob_room.py` | Builds a task race room from the layout in `art/docs/ux/BOB_Level_Layouts.html` (doc 19): floor, back wall, low side walls, wall items, one placeholder box per prop at its layout size (swapped for `art/aa/props/SM_prop_<id>.glb` when it exists, see `PROP_FILES`), and named empties for TaskSpots, spawns, cart, pair and emergency points. Needs `node` to read the layout. Preview renders with Cycles on CPU at the game camera (45°, ortho 4.1) | `art/aa/rooms/SM_room_<id>.glb`, `.json`, `_preview.png` |
| `bob_prop_cleanup.py` | Cleans a raw AI image-to-3D or CC0 mesh: join, merge, drop floating bits, decimate to budget, scale to game height, pivot bottom centre, snap colours to the palette | `SM_<name>.glb`, `.fbx`, `.json` |

```bash
# with Blender installed
blender -b -P art/tools/blender/bob_kit.py -- --out art/aa/kit
blender -b -P art/tools/blender/bob_room.py -- --room all   # rental, condo, kitchen
blender -b -P art/tools/blender/bob_prop_cleanup.py -- raw/bucket.glb --name prop_bucket --class prop_m --height 0.45 --out art/aa/props

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
