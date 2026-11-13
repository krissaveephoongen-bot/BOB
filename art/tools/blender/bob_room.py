"""BOB room builder: turns a task race layout (doc 19) into a 3D room shell.

The layout data lives in art/docs/ux/BOB_Level_Layouts.html (<script id="bob-layout-logic">),
the same data check_layouts.js checks. This script reads it through node, then builds:

  * the room shell: floor 14x8, back wall 2.4 high with skirting, low cut-away side walls,
    and the flat wall items (window + curtains, poster, TV, door)
  * one placeholder box per prop, at the layout's exact size, named PH_<n>_<kind>.
    When a cleaned Tripo prop exists (SM_prop_<id>.glb in --props, mapping in PROP_FILES)
    it is imported instead, scaled to fit the box and stood on its bottom centre.
  * empties for gameplay points: TS_<slot> (TaskSpot, with its type), SPAWN_P1-4,
    CART_STAND, PAIR_A/B, EMERG_E1-3

Everything uses the one shared material (M_BOB_Toon + T_BOB_Palette.png) with UVs on palette
cells, like bob_kit.py. Only existing palette cells are used, so the palette is not changed.

Coordinates: layout x -> Blender X, layout y (0 = back wall) -> Blender Y = 8 - y, Z up.
After GLB import into Unity: x right, z forward (back wall at z = 8), y up, 1 unit = 1 tile.

Run:
    pip install bpy
    python art/tools/blender/bob_room.py --room rental --out art/aa/rooms --props art/aa/props
Writes SM_room_<id>.glb, SM_room_<id>.json (manifest) and SM_room_<id>_preview.png
(Cycles CPU; game camera: pitch 45 deg, ortho size 4.1, 2340x1080 framing, per 21_AA_Art_Bible.md).
"""
import argparse
import json
import math
import os
import subprocess
import sys

import bpy  # noqa: F401  (must come before bmesh/mathutils)
from mathutils import Vector

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
from bob_kit import PALETTE, PAL_INDEX, cell_uv  # noqa: E402

ROOM_W, ROOM_D = 14.0, 8.0
WALL_H = 2.4
SIDE_H = 0.5
T = 0.15  # wall thickness

# Prop kind -> palette cell for the placeholder box (existing cells only).
KIND_COL = {
    "shelf": "wood", "cabinet": "cream", "sofa": "sweep_shirt", "bed": "goggle_lens",
    "table": "straw", "drawer": "wood", "bin": "trash", "heavy": "spray_pink", "cart": "metal",
    "counter": "cream", "sink": "metal", "rack": "metal", "washer": "white", "stove": "metal_dark",
    "steel": "metal", "pipe": "metal_dark", "desk": "straw",
}
# Placeholder name (room id, prop label) -> cleaned Tripo prop id, imported when the file exists.
# Fill in as props land in art/aa/props (see /mnt/project-files/bob-art/tripo/L1_rental_room_props.md).
PROP_FILES = {
    ("rental", "โซฟา"): "sofa_2seat",
    ("rental", "เตียงเดี่ยว"): "bed_single",
    ("rental", "โต๊ะญี่ปุ่น"): "table_low",
    ("rental", "ตู้ลิ้นชัก"): "drawer_low",
    ("rental", "ครัวเล็ก + ตู้น้ำยา"): "kitchenette",
    ("rental", "ฟูกที่นอนล้ม"): "futon_fallen",
    ("rental", "ชั้นวางชิ้นส่วน"): "parts_shelf",
    ("rental", "ถังแยกขยะ"): "recycle_bin",
    ("rental", "รถเข็น"): "cart",
}


def read_rooms(html):
    js = (
        "const fs=require('fs'),vm=require('vm');"
        "const h=fs.readFileSync(process.argv[1],'utf8');"
        "const m=h.match(/<script id=\"bob-layout-logic\">([\\s\\S]*?)<\\/script>/);"
        "const c={module:{}};vm.createContext(c);vm.runInContext(m[1],c);"
        "process.stdout.write(JSON.stringify(c.module.exports.ROOMS));"
    )
    out = subprocess.run(["node", "-e", js, html], check=True, capture_output=True, text=True)
    return json.loads(out.stdout)


def material(palette_png):
    mat = bpy.data.materials.new("M_BOB_Toon")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = bpy.data.images.load(palette_png)
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.9
    return mat


def box(name, x0, y0, z0, x1, y1, z1, col, mat, coll):
    """Axis-aligned box in Blender coords with all UVs on one palette cell."""
    me = bpy.data.meshes.new(name)
    v = [(x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
         (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1)]
    f = [(0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)]
    me.from_pydata(v, [], f)
    uv = me.uv_layers.new(name="UVMap")
    u = cell_uv(col)
    for d in uv.data:
        d.uv = u
    me.materials.append(mat)
    o = bpy.data.objects.new(name, me)
    coll.objects.link(o)
    return o


def lay_rect(p):
    """Layout rect (x, y, w, d) -> Blender X/Y extents."""
    return p["x"], ROOM_D - p["y"] - p["d"], p["x"] + p["w"], ROOM_D - p["y"]


def wall_item(p, mat, coll, n):
    x0, x1 = p["x"], p["x"] + p["w"]
    y0, y1 = ROOM_D - 0.03, ROOM_D
    k, t = p["k"], p["t"]
    out = []
    if k == "window":
        out.append(box(f"WALL_{n}_window_frame", x0 - .08, y0 - .01, .92, x1 + .08, y1, 2.08, "white", mat, coll))
        out.append(box(f"WALL_{n}_window_glass", x0, y0 - .03, 1.0, x1, y1, 2.0, "goggle_lens", mat, coll))
        for cx in (x0 - .25, x1 - .05):
            out.append(box(f"WALL_{n}_curtain", cx, y0 - .12, .85, cx + .3, y1, 2.2, "bubble_shirt", mat, coll))
    elif k == "door":
        out.append(box(f"WALL_{n}_door", x0 + .3, y0 - .02, 0, x1 - .3, y1, 2.0, "wood", mat, coll))
    elif "ทีวี" in t:
        out.append(box(f"WALL_{n}_tv", x0 + .1, y0 - .06, 1.1, x1 - .1, y1, 1.85, "trash_dark", mat, coll))
    else:  # poster / notice board
        out.append(box(f"WALL_{n}_poster", x0 + .35, y0 - .02, 1.15, x1 - .35, y1, 1.95, "spray_pink", mat, coll))
    return out


def import_prop(path, coll):
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=path)
    new = [o for o in bpy.data.objects if o not in before]
    for o in new:
        for c in o.users_collection:
            c.objects.unlink(o)
        coll.objects.link(o)
    return new


def fit(objs, x0, y0, x1, y1, h):
    """Scale imported objects uniformly into the box footprint and height, bottom centre on the floor."""
    bpy.context.view_layer.update()
    pts = [o.matrix_world @ Vector(c) for o in objs if o.type == "MESH" for c in o.bound_box]
    lo = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    hi = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    size = hi - lo
    s = min((x1 - x0) / max(size.x, 1e-4), (y1 - y0) / max(size.y, 1e-4), h / max(size.z, 1e-4))
    centre = Vector(((lo.x + hi.x) / 2, (lo.y + hi.y) / 2, lo.z))
    target = Vector(((x0 + x1) / 2, (y0 + y1) / 2, 0))
    roots = [o for o in objs if o.parent is None]
    for o in roots:
        o.location = target + (o.location - centre) * s
        o.scale = o.scale * s


def empty(name, x, y, coll, **props):
    e = bpy.data.objects.new(name, None)
    e.empty_display_type = "PLAIN_AXES"
    e.empty_display_size = 0.3
    e.location = (x, ROOM_D - y, 0)
    for k, v in props.items():
        e[k] = v
    coll.objects.link(e)
    return e


def build(room, palette_png, props_dir):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    mat = material(palette_png)
    coll = bpy.data.collections.new(f"SM_room_{room['id']}")
    bpy.context.scene.collection.children.link(coll)
    shell = []
    # floor, top at z = 0
    shell.append(box("FLOOR", 0, 0, -.1, ROOM_W, ROOM_D, 0, "wood", mat, coll))
    # back wall + skirting, low side walls (cut-away so the 45 deg camera sees the floor)
    shell.append(box("WALL_back", -T, ROOM_D, 0, ROOM_W + T, ROOM_D + T, WALL_H, "cream", mat, coll))
    shell.append(box("WALL_back_skirt", 0, ROOM_D - .03, 0, ROOM_W, ROOM_D, .14, "wood", mat, coll))
    shell.append(box("WALL_left", -T, 0, 0, 0, ROOM_D + T, SIDE_H, "cream", mat, coll))
    shell.append(box("WALL_right", ROOM_W, 0, 0, ROOM_W + T, ROOM_D + T, SIDE_H, "cream", mat, coll))
    manifest = {"room": room["id"], "name": room["name"], "code": room["code"], "units": "1 unit = 1 tile",
                "axes": "Unity: x right, z forward (back wall z=8), y up", "props": [], "points": []}
    for n, p in enumerate(room["props"]):
        if p["h"] <= 0:
            shell += wall_item(p, mat, coll, n)
            continue
        x0, y0, x1, y1 = lay_rect(p)
        pid = PROP_FILES.get((room["id"], p["t"]))
        path = os.path.join(props_dir, f"SM_prop_{pid}.glb") if (pid and props_dir) else None
        src = "placeholder"
        if path and os.path.exists(path):
            fit(import_prop(path, coll), x0, y0, x1, y1, p["h"])
            src = os.path.basename(path)
        else:
            o = box(f"PH_{n}_{p['k']}", x0, y0, 0, x1, y1, p["h"], KIND_COL.get(p["k"], "cream"), mat, coll)
            o["label"] = p["t"]
            o["tripo_id"] = pid or ""
        manifest["props"].append({"label": p["t"], "kind": p["k"], "tripo_id": pid, "source": src,
                                  "unity_min": [x0, 0, y0], "size": [p["w"], p["h"], p["d"]]})
    # merge the static shell into one mesh (one draw call)
    bpy.ops.object.select_all(action="DESELECT")
    for o in shell:
        o.select_set(True)
    bpy.context.view_layer.objects.active = shell[0]
    bpy.ops.object.join()
    shell[0].name = f"SM_room_{room['id']}_shell"
    # gameplay points
    for s in room["slots"]:
        empty(f"TS_{s['id']}", s["x"], s["y"], coll, type=s["type"])
        manifest["points"].append({"id": s["id"], "role": "taskspot", "type": s["type"], "x": s["x"], "z": ROOM_D - s["y"]})
    empty("CART_STAND", room["cart"]["x"], room["cart"]["y"], coll)
    for tag, xy in (("PAIR_A", room["pair"]["a"]), ("PAIR_B", room["pair"]["b"])):
        empty(tag, xy[0], xy[1], coll)
    for i, e in enumerate(room["E"]):
        empty(f"EMERG_E{i + 1}", e[0], e[1], coll)
    for i, s in enumerate(room["spawns"]):
        empty(f"SPAWN_P{i + 1}", s[0], s[1], coll)
    for tag, x, y in [("cart", room["cart"]["x"], room["cart"]["y"]), ("pair_a", *room["pair"]["a"]),
                      ("pair_b", *room["pair"]["b"])] + [(f"emergency_E{i + 1}", *e) for i, e in enumerate(room["E"])] \
            + [(f"spawn_P{i + 1}", *s) for i, s in enumerate(room["spawns"])]:
        manifest["points"].append({"id": tag, "x": x, "z": ROOM_D - y})
    return manifest


def preview(path):
    sc = bpy.context.scene
    # Cycles on CPU so it renders headless with no GPU (Workbench/EEVEE need one)
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 24
    sc.cycles.use_denoising = False
    sc.view_settings.view_transform = "Standard"
    sun = bpy.data.lights.new("SUN", "SUN")
    sun.energy = 3.2
    sun.angle = math.radians(8)
    so = bpy.data.objects.new("SUN", sun)
    so.rotation_euler = (math.radians(40), math.radians(-18), math.radians(-25))
    sc.collection.objects.link(so)
    sc.render.resolution_x, sc.render.resolution_y = 2340, 1080
    sc.render.resolution_percentage = 50
    world = bpy.data.worlds.new("W")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.75, 0.72, 0.68, 1)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.9
    sc.world = world
    cam = bpy.data.cameras.new("CAM")
    cam.type = "ORTHO"
    cam.ortho_scale = 2 * 4.1 * 2340 / 1080  # Unity ortho size 4.1 (half height) on 2340x1080
    co = bpy.data.objects.new("CAM", cam)
    pitch = math.radians(45)
    target = Vector((ROOM_W / 2, ROOM_D / 2 + .55, 0.5))
    d = Vector((0, math.cos(pitch), -math.sin(pitch)))
    co.location = target - d * 30
    co.rotation_euler = (math.pi / 2 - pitch, 0, 0)
    sc.collection.objects.link(co)
    sc.camera = co
    sc.render.filepath = path
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(co)
    bpy.data.objects.remove(so)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    root = os.path.normpath(os.path.join(HERE, "..", "..", ".."))
    ap = argparse.ArgumentParser()
    ap.add_argument("--room", default="rental")
    ap.add_argument("--out", default=os.path.join(root, "art", "aa", "rooms"))
    ap.add_argument("--props", default=os.path.join(root, "art", "aa", "props"))
    ap.add_argument("--layout", default=os.path.join(root, "art", "docs", "ux", "BOB_Level_Layouts.html"))
    ap.add_argument("--palette", default=os.path.join(root, "art", "aa", "kit", "T_BOB_Palette.png"))
    ap.add_argument("--no-preview", action="store_true")
    a = ap.parse_args(argv)
    rooms = {r["id"]: r for r in read_rooms(a.layout)}
    ids = list(rooms) if a.room == "all" else [a.room]
    os.makedirs(a.out, exist_ok=True)
    for rid in ids:
        man = build(rooms[rid], a.palette, a.props)
        base = os.path.join(a.out, f"SM_room_{rid}")
        if not a.no_preview:
            preview(base + "_preview.png")
        bpy.ops.export_scene.gltf(filepath=base + ".glb", export_format="GLB", export_extras=True,
                                  export_yup=True, export_apply=True)
        with open(base + ".json", "w", encoding="utf-8") as f:
            json.dump(man, f, ensure_ascii=False, indent=1)
        placed = sum(1 for p in man["props"] if p["source"] != "placeholder")
        print(f"{rid}: {len(man['props'])} props ({placed} from Tripo), {len(man['points'])} points -> {base}.glb")


if __name__ == "__main__":
    main()
