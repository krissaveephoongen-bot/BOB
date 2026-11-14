"""BOB prop cleanup: turns a raw AI image-to-3D mesh (TRELLIS / Tripo / Meshy,
or a CC0 kit model) into a BOB-ready prop.

Steps (21_AA_Art_Bible.md section 9.3):
 1. import .glb/.gltf/.obj/.fbx, join all meshes, merge by distance, drop loose bits
 2. decimate to the tri budget for its class (prop_s / prop_m / prop_hand / room)
 3. scale to the real game height, pivot = bottom centre, rest on Y=0
 4. palette snap: every face takes the nearest BOB palette colour (CIE Lab) of the
    texture under it, and its UVs move onto that palette cell -> the prop uses the
    same single material + T_BOB_Palette as heroes and kit props (one batch)
 5. export SM_<name>.glb (+ .fbx) and a JSON report (tris before/after, colours used)

Run:
  blender -b -P art/tools/blender/bob_prop_cleanup.py -- raw/bucket.glb --name prop_bucket --class prop_m --height 0.45
  (or with the pip bpy module: python bob_prop_cleanup.py raw/bucket.glb --name ...)

Limits: palette snap removes painted detail by design (the look is flat colour +
toon ramp). A prop that needs a decal (label, logo) keeps it as a separate quad.
"""
import argparse
import json
import math
import os
import sys

import bpy  # must come first
import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from bob_kit import PALETTE, cell_uv, hex_rgb, make_palette_image  # noqa: E402

TRI_BUDGET = {"prop_s": 300, "prop_m": 800, "prop_hand": 1500, "room": 40000, "part": 500,
              "char": 6000, "boss": 10000, "map": 8000}
# props may only snap to these (no skin / face colours on furniture)
PROP_COLOURS = [n for n, _ in PALETTE if n not in ("skin", "skin_shadow", "cheek", "eye", "mouth", "tongue", "brow")]


def srgb_to_lab(c):
    def lin(u):
        return u / 12.92 if u <= 0.04045 else ((u + 0.055) / 1.055) ** 2.4
    r, g, b = (lin(x) for x in c)
    X = (0.4124 * r + 0.3576 * g + 0.1805 * b) / 0.95047
    Y = (0.2126 * r + 0.7152 * g + 0.0722 * b)
    Z = (0.0193 * r + 0.1192 * g + 0.9505 * b) / 1.08883

    def f(t):
        return t ** (1 / 3) if t > 0.008856 else 7.787 * t + 16 / 116
    fx, fy, fz = f(X), f(Y), f(Z)
    return (116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz))


PAL_LAB = {n: srgb_to_lab(hex_rgb(h)) for n, h in PALETTE if n in PROP_COLOURS}


def nearest(rgb):
    L = srgb_to_lab(rgb)
    return min(PAL_LAB, key=lambda n: sum((a - b) ** 2 for a, b in zip(L, PAL_LAB[n])))


def tint_colour(scheme, face, z0, z1):
    """Untextured map pieces (Tripo exports geometry only): paint palette cells by face normal and
    normalised height instead of sampling a missing texture."""
    c = face.calc_center_median()
    h = (c.z - z0) / max(1e-6, z1 - z0)
    nz = face.normal.z
    if scheme == "terrain":
        if nz > 0.55: return "grass"
        if h > 0.72: return "grass_dark"
        if h > 0.30: return "dirt"
        return "metal_dark"
    if scheme == "water":
        if nz > 0.5: return "water"
        if h > 0.72: return "goggle_lens"
        return "water_dark"
    if scheme == "wood":
        return "wood" if h > 0.55 else "shoe"
    if scheme == "stone":
        if h < 0.2: return "metal_dark"
        return "stone" if (int(c.x * 4) + int(c.y * 4)) % 2 == 0 else "metal"
    if scheme == "rock":
        if nz > 0.5: return "stone"
        if h < 0.35: return "metal_dark"
        return "metal"
    return None


def import_any(path):
    ext = os.path.splitext(path)[1].lower()
    before = set(bpy.data.objects)
    if ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=path)
    elif ext == ".obj":
        bpy.ops.wm.obj_import(filepath=path)
    elif ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    else:
        raise SystemExit("unsupported: " + ext)
    return [o for o in bpy.data.objects if o not in before and o.type == "MESH"]


def first_image(obj):
    for slot in obj.material_slots:
        m = slot.material
        if m and m.use_nodes:
            for n in m.node_tree.nodes:
                if n.type == "TEX_IMAGE" and n.image:
                    return n.image
    return None


def sample(img, pixels, u, v):
    w, h = img.size
    x = min(w - 1, max(0, int((u % 1.0) * w)))
    y = min(h - 1, max(0, int((v % 1.0) * h)))
    o = (y * w + x) * 4
    return pixels[o:o + 3]


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    ap = argparse.ArgumentParser()
    ap.add_argument("src")
    ap.add_argument("--name", required=True)
    ap.add_argument("--prefix", default="SM_",
                    help="asset prefix: SM_ props (default), SK_ characters that later go through a rig")
    ap.add_argument("--class", dest="cls", default="prop_m", choices=TRI_BUDGET)
    ap.add_argument("--height", type=float, required=True, help="game height in metres (1 tile = 1 m)")
    ap.add_argument("--predecimate", type=int, default=0,
                    help="decimate to this many tris right after import, before the island pass "
                         "(raw Tripo meshes come in at ~2M tris and the island walk is slow on them)")
    ap.add_argument("--out", default="art/aa/props")
    ap.add_argument("--tint", default="none",
                    help="for models with no texture/colour data (map pieces): paint palette cells by "
                         "face normal and height. none | terrain | water | wood | stone | rock")
    a = ap.parse_args(argv)
    os.makedirs(a.out, exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    objs = import_any(os.path.abspath(a.src))
    if not objs:
        raise SystemExit("no mesh in " + a.src)
    img = first_image(objs[0])
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    ob = bpy.context.active_object
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    tris_in = sum(len(p.vertices) - 2 for p in ob.data.polygons)

    # 1. clean. Merge first: Tripo exports repeat vertices per face, which leaves non-manifold edges
    # and makes the decimate modifier stop at ~13 % of the original size.
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bm.to_mesh(ob.data)
    bm.free()

    # 1b. optional early decimate (raw Tripo meshes are ~2M tris; the island walk below is Python)
    def decimate_to(target):
        for _ in range(6):
            cur = sum(len(p.vertices) - 2 for p in ob.data.polygons)
            if cur <= target:
                return cur
            mod = ob.modifiers.new("dec", "DECIMATE")
            mod.decimate_type = "COLLAPSE"
            mod.ratio = max(1e-5, target / cur)
            with bpy.context.temp_override(object=ob, active_object=ob, selected_objects=[ob]):
                bpy.ops.object.modifier_apply(modifier=mod.name)
        return sum(len(p.vertices) - 2 for p in ob.data.polygons)

    if a.predecimate and tris_in > a.predecimate:
        decimate_to(a.predecimate)

    # drop islands under 1% of the faces (floating AI noise)
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    islands, seen = [], set()
    for f in bm.faces:
        if f.index in seen:
            continue
        stack, isl = [f], []
        seen.add(f.index)
        while stack:
            g = stack.pop()
            isl.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen:
                        seen.add(h.index)
                        stack.append(h)
        islands.append(isl)
    total = len(bm.faces)
    kill = [f for isl in islands if len(isl) < 0.01 * total for f in isl]
    if kill and len(kill) < total:
        bmesh.ops.delete(bm, geom=kill, context="FACES")
    bm.to_mesh(ob.data)
    bm.free()

    # 2. decimate to budget
    budget = TRI_BUDGET[a.cls]
    cur = sum(len(p.vertices) - 2 for p in ob.data.polygons)
    if cur > budget:
        mod = ob.modifiers.new("dec", "DECIMATE")
        mod.ratio = budget / cur
        with bpy.context.temp_override(object=ob):
            bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.shade_smooth()

    # 3. scale + pivot bottom centre
    vs = [v.co for v in ob.data.vertices]
    mn = Vector((min(v.x for v in vs), min(v.y for v in vs), min(v.z for v in vs)))
    mx = Vector((max(v.x for v in vs), max(v.y for v in vs), max(v.z for v in vs)))
    s = a.height / max(1e-6, (mx.z - mn.z))
    off = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    for v in ob.data.vertices:
        v.co = (v.co - off) * s

    # 4. palette snap per face
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    uv = bm.loops.layers.uv.verify()
    bm.normal_update()
    px = list(img.pixels) if img else None
    z0 = min(v.co.z for v in bm.verts)
    z1 = max(v.co.z for v in bm.verts)
    used = {}
    for f in bm.faces:
        n = tint_colour(a.tint, f, z0, z1) if a.tint != "none" else None
        if n is None:
            if px:
                u = sum(l[uv].uv.x for l in f.loops) / len(f.loops)
                v = sum(l[uv].uv.y for l in f.loops) / len(f.loops)
                rgb = sample(img, px, u, v)
            else:
                m = me.materials[f.material_index] if me.materials else None
                rgb = tuple(m.diffuse_color[:3]) if m else (0.7, 0.7, 0.7)
            n = nearest(rgb)
        used[n] = used.get(n, 0) + 1
        cu, cv = cell_uv(n)
        for l in f.loops:
            l[uv].uv = (cu, cv)
    bm.to_mesh(me)
    bm.free()
    pal = make_palette_image(os.path.join(a.out, "T_BOB_Palette.png"))
    mat = bpy.data.materials.new("M_BOB_Toon")
    mat.use_nodes = True
    t = mat.node_tree.nodes.new("ShaderNodeTexImage")
    t.image = pal
    t.interpolation = "Closest"
    mat.node_tree.links.new(t.outputs["Color"], mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"])
    me.materials.clear()
    me.materials.append(mat)
    ob.name = me.name = a.prefix + a.name

    tris_out = sum(len(p.vertices) - 2 for p in me.polygons)
    base = os.path.join(a.out, a.prefix + a.name)
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.ops.export_scene.gltf(filepath=base + ".glb", use_selection=True, export_format="GLB")
    try:
        bpy.ops.export_scene.fbx(filepath=base + ".fbx", use_selection=True, axis_forward="-Z", axis_up="Y",
                                 apply_unit_scale=True)
    except Exception as e:
        print("FBX skipped:", e)
    rep = {"name": a.name, "class": a.cls, "tris_in": tris_in, "tris_out": tris_out, "budget": budget,
           "height_m": a.height, "colours": dict(sorted(used.items(), key=lambda kv: -kv[1]))}
    with open(base + ".json", "w") as f:
        json.dump(rep, f, indent=1)
    print(json.dumps(rep))


if __name__ == "__main__":
    main()
