"""BOB chibi kit: builds the 8 heroes and 4 enemies from primitives, rigs them
with the shared skeleton from 03_Unity_Spec.md, keys idle / walk / cheer, and
exports GLB (+ FBX for Unity).

No artist needed: every shape follows the concept Art Board / Asset Sheet and
the numbers in art/docs/21_AA_Art_Bible.md (proportions, palette, budgets).

Run (headless, no GPU):
    blender -b -P art/tools/blender/bob_kit.py -- --out art/aa/kit
or with the pip module:
    python -m venv v && v/bin/pip install bpy && v/bin/python art/tools/blender/bob_kit.py --out art/aa/kit

Every mesh is one skinned mesh with ONE material that samples a 256x256
palette texture (T_BOB_Palette.png). Each part's UVs sit on its colour cell,
so all characters and props share one material -> SRP Batcher friendly.
"""
import argparse
import json
import math
import os
import sys

import bpy  # must come first: bpy makes bmesh/mathutils importable
import bmesh
from mathutils import Matrix, Vector, Euler

# --------------------------------------------------------------------------
# Palette (concept Asset Sheet). Order = cell index on the 16x16 grid.
# --------------------------------------------------------------------------
PALETTE = [
    ("skin", "#FADDC1"), ("skin_shadow", "#E9B896"), ("cheek", "#F59A9A"),
    ("eye", "#3A2418"), ("white", "#FFFFFF"), ("mouth", "#C8424A"),
    ("tongue", "#F0727A"), ("brow", "#3B2419"),
    ("cream", "#FEEECF"), ("navy", "#2E4484"), ("shoe", "#5A3A28"),
    ("outline", "#4A2E22"),
    ("sweep_shirt", "#F79A2E"), ("sweep_band", "#FB2F44"), ("sweep_hair", "#4B2E1F"),
    ("bubble_shirt", "#3D8FE0"), ("bubble_hair", "#8A5530"),
    ("press_shirt", "#2FA97A"), ("press_hair", "#4B2E1F"), ("metal", "#B9BEC6"),
    ("metal_dark", "#7F8794"),
    ("spray_shirt", "#8E6BD8"), ("spray_hair", "#2A2228"), ("goggle_lens", "#8FD3F4"),
    ("goggle_strap", "#7A4FC8"), ("spray_pink", "#F79BB0"),
    ("wood", "#A0643A"), ("straw", "#F2B53A"), ("sponge", "#F7D046"),
    ("mop", "#F4F1E8"),
    ("dirt", "#8A5226"), ("dirt_dark", "#5E3518"), ("slime", "#8B4FC8"),
    ("slime_light", "#C79BF0"), ("trash", "#7D8796"), ("trash_dark", "#2A2D38"),
    ("boss", "#7A3FB8"), ("gold", "#F6C431"), ("gold_dark", "#C98A1B"),
    ("fang", "#FFF6E8"),
    # heroes 5-8 (doc 24); appended only, never reorder
    ("wash_shirt", "#F27BAE"), ("wash_hair", "#C08A5A"), ("towel", "#FFF8F0"),
    ("iron_shirt", "#F2C230"), ("iron_handle", "#3D6FD0"),
    ("duster_shirt", "#E5484D"),
    ("vac_shirt", "#9CCB3B"), ("vac_hair", "#C06A30"),
]
PAL_INDEX = {n: i for i, (n, _) in enumerate(PALETTE)}
GRID = 16  # 16x16 cells on a 256 texture (16 px per cell)


def hex_rgb(h):
    h = h.lstrip("#")
    return tuple(int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4))


def cell_uv(name):
    i = PAL_INDEX[name]
    cx, cy = i % GRID, i // GRID
    # centre of the cell; v grows upward in Blender
    return ((cx + 0.5) / GRID, 1.0 - (cy + 0.5) / GRID)


def make_palette_image(path):
    size = 256
    img = bpy.data.images.new("T_BOB_Palette", size, size, alpha=False)
    px = [0.0] * (size * size * 4)
    cell = size // GRID
    for i, (_, hx) in enumerate(PALETTE):
        r, g, b = hex_rgb(hx)
        cx, cy = i % GRID, i // GRID
        for y in range(cell):
            for x in range(cell):
                X = cx * cell + x
                Y = size - 1 - (cy * cell + y)
                o = (Y * size + X) * 4
                px[o:o + 4] = [r, g, b, 1.0]
    img.pixels = px
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    return img


# --------------------------------------------------------------------------
# Scene helpers
# --------------------------------------------------------------------------
def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def _new_obj_from_op(op, **kw):
    op(**kw)
    return bpy.context.active_object


def part(kind, color, bone, loc=(0, 0, 0), rot=(0, 0, 0), scale=(1, 1, 1), **kw):
    """Create a primitive, bake its transform into the mesh, put its UVs on the
    palette cell, and tag it with the bone that drives it (rigid skinning)."""
    if kind == "sphere":
        o = _new_obj_from_op(bpy.ops.mesh.primitive_uv_sphere_add,
                             segments=kw.get("seg", 16), ring_count=kw.get("rings", 10), radius=1)
    elif kind == "cyl":
        o = _new_obj_from_op(bpy.ops.mesh.primitive_cylinder_add,
                             vertices=kw.get("seg", 12), radius=1, depth=1)
    elif kind == "cone":
        o = _new_obj_from_op(bpy.ops.mesh.primitive_cone_add, vertices=kw.get("seg", 12),
                             radius1=kw.get("r1", 1), radius2=kw.get("r2", 0), depth=1)
    elif kind == "torus":
        o = _new_obj_from_op(bpy.ops.mesh.primitive_torus_add, major_radius=1,
                             minor_radius=kw.get("minor", 0.15),
                             major_segments=kw.get("seg", 16), minor_segments=kw.get("mseg", 6))
    elif kind == "box":
        o = _new_obj_from_op(bpy.ops.mesh.primitive_cube_add, size=2)
    else:
        raise ValueError(kind)
    me = o.data
    M = Matrix.Translation(Vector(loc)) @ Euler([math.radians(a) for a in rot]).to_matrix().to_4x4() \
        @ Matrix.Diagonal(Vector(scale)).to_4x4()
    me.transform(M)
    o.matrix_world = Matrix.Identity(4)
    bm = bmesh.new()
    bm.from_mesh(me)
    if kind == "box" and kw.get("bevel", 0) > 0:
        bmesh.ops.bevel(bm, geom=list(bm.edges) + list(bm.verts), offset=kw["bevel"],
                        segments=kw.get("bseg", 2), profile=0.5, affect="EDGES")
    uv = bm.loops.layers.uv.verify()
    u, v = cell_uv(color)
    for f in bm.faces:
        for l in f.loops:
            l[uv].uv = (u, v)
    bm.to_mesh(me)
    bm.free()
    if kw.get("smooth", True):
        me.shade_smooth()
    vg = o.vertex_groups.new(name=bone)
    vg.add(list(range(len(me.vertices))), 1.0, "REPLACE")
    o["bob_color"] = color
    return o


def tri_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


# --------------------------------------------------------------------------
# Skeleton (names = 03_Unity_Spec.md section 3, trimmed for rigid skinning)
# --------------------------------------------------------------------------
BONES = [
    # name, parent, head, tail
    ("Root", None, (0, 0, 0), (0, 0.12, 0)),
    ("Hips", "Root", (0, 0, 0.20), (0, 0, 0.30)),
    ("Spine", "Hips", (0, 0, 0.30), (0, 0, 0.40)),
    ("Chest", "Spine", (0, 0, 0.40), (0, 0, 0.50)),
    ("Neck", "Chest", (0, 0, 0.50), (0, 0, 0.53)),
    ("Head", "Neck", (0, 0, 0.53), (0, 0, 0.95)),
    ("UpperArm_L", "Chest", (0.17, 0, 0.46), (0.22, 0, 0.38)),
    ("LowerArm_L", "UpperArm_L", (0.22, 0, 0.38), (0.25, 0, 0.32)),
    ("Hand_L", "LowerArm_L", (0.25, 0, 0.32), (0.26, 0, 0.27)),
    ("UpperArm_R", "Chest", (-0.17, 0, 0.46), (-0.22, 0, 0.38)),
    ("LowerArm_R", "UpperArm_R", (-0.22, 0, 0.38), (-0.25, 0, 0.32)),
    ("Hand_R", "LowerArm_R", (-0.25, 0, 0.32), (-0.26, 0, 0.27)),
    ("Socket_Tool", "Hand_R", (-0.27, -0.02, 0.29), (-0.27, -0.02, 0.36)),
    ("Apron_01", "Chest", (0, -0.14, 0.42), (0, -0.15, 0.24)),
    ("UpperLeg_L", "Hips", (0.08, 0, 0.20), (0.085, 0, 0.09)),
    ("Foot_L", "UpperLeg_L", (0.085, 0, 0.09), (0.085, -0.08, 0.02)),
    ("UpperLeg_R", "Hips", (-0.08, 0, 0.20), (-0.085, 0, 0.09)),
    ("Foot_R", "UpperLeg_R", (-0.085, 0, 0.09), (-0.085, -0.08, 0.02)),
    ("Socket_Carry", "Root", (0, 0, 1.15), (0, 0, 1.25)),
]


def make_armature(name, scale=1.0, bones=BONES):
    arm = bpy.data.armatures.new(name + "_Rig")
    ob = bpy.data.objects.new(name + "_Rig", arm)
    bpy.context.scene.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    eb = arm.edit_bones
    for n, p, h, t in bones:
        b = eb.new(n)
        b.head = Vector(h) * scale
        b.tail = Vector(t) * scale
        if p:
            b.parent = eb[p]
            b.use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def bind(parts, rig, name):
    bpy.ops.object.select_all(action="DESELECT")
    for p in parts:
        p.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.join()
    mesh = bpy.context.active_object
    mesh.name = "SM_" + name
    mesh.data.name = "SM_" + name
    mod = mesh.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    mesh.parent = rig
    # one material, palette texture
    mesh.data.materials.clear()
    mesh.data.materials.append(bpy.data.materials["M_BOB_Toon"])
    return mesh


def make_material(img):
    m = bpy.data.materials.new("M_BOB_Toon")
    m.use_nodes = True
    nt = m.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.interpolation = "Closest"
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    bsdf.inputs["Roughness"].default_value = 0.75
    return m


# --------------------------------------------------------------------------
# Hero base body (H = 1.0 without hair/hat; head 0.44 H)
# --------------------------------------------------------------------------
def hero_body(shirt, hair):
    P = []
    # shoes + legs
    for s, side in ((1, "L"), (-1, "R")):
        P.append(part("box", "shoe", "Foot_" + side, (0.085 * s, -0.025, 0.045), scale=(0.062, 0.09, 0.045), bevel=0.03, bseg=2))
        P.append(part("cyl", "navy", "UpperLeg_" + side, (0.08 * s, 0, 0.14), scale=(0.05, 0.05, 0.12), seg=10))
    # hips / shorts
    P.append(part("sphere", "navy", "Hips", (0, 0, 0.22), scale=(0.15, 0.12, 0.07), seg=14, rings=8))
    # torso (shirt)
    P.append(part("sphere", shirt, "Spine", (0, 0, 0.34), scale=(0.17, 0.135, 0.16), seg=16, rings=10))
    P.append(part("cyl", shirt, "Chest", (0, 0, 0.45), scale=(0.13, 0.11, 0.06), seg=14))
    # apron + pocket + straps
    P.append(part("box", "cream", "Apron_01", (0, -0.125, 0.32), rot=(-6, 0, 0), scale=(0.115, 0.018, 0.12), bevel=0.012, bseg=2))
    P.append(part("box", "cream", "Apron_01", (0, -0.148, 0.29), rot=(-6, 0, 0), scale=(0.05, 0.008, 0.035), bevel=0.006, bseg=1))
    for s in (1, -1):
        P.append(part("box", "cream", "Chest", (0.075 * s, -0.08, 0.45), rot=(-20, 0, 0), scale=(0.018, 0.05, 0.012), bevel=0.004, bseg=1))
    # arms
    for s, side in ((1, "L"), (-1, "R")):
        P.append(part("sphere", shirt, "UpperArm_" + side, (0.175 * s, 0, 0.445), scale=(0.065, 0.065, 0.06), seg=12, rings=8))
        P.append(part("cyl", "skin", "LowerArm_" + side, (0.225 * s, 0, 0.375), rot=(0, -25 * s, 0), scale=(0.038, 0.038, 0.07), seg=10))
        P.append(part("sphere", "skin", "Hand_" + side, (0.255 * s, -0.01, 0.30), scale=(0.055, 0.05, 0.055), seg=12, rings=8))
    # head
    P.append(part("sphere", "skin", "Head", (0, 0, 0.72), scale=(0.25, 0.23, 0.22), seg=24, rings=16))
    P.append(part("sphere", "skin", "Head", (0, -0.02, 0.555), scale=(0.07, 0.06, 0.05), seg=10, rings=6))  # neck
    for s in (1, -1):
        P.append(part("sphere", "skin", "Head", (0.245 * s, 0.0, 0.70), scale=(0.035, 0.03, 0.045), seg=10, rings=6))  # ears
    # face (preview only; production face = flipbook atlas, see 21_AA_Art_Bible 6.3)
    for s in (1, -1):
        P.append(part("sphere", "eye", "Head", (0.085 * s, -0.212, 0.70), scale=(0.038, 0.02, 0.05), seg=12, rings=8))
        P.append(part("sphere", "white", "Head", (0.072 * s, -0.228, 0.718), scale=(0.013, 0.008, 0.015), seg=8, rings=5))
        P.append(part("sphere", "white", "Head", (0.096 * s, -0.226, 0.684), scale=(0.006, 0.006, 0.007), seg=6, rings=4))
        P.append(part("sphere", "cheek", "Head", (0.15 * s, -0.19, 0.645), rot=(0, 0, 30 * s), scale=(0.04, 0.012, 0.022), seg=10, rings=6))
        P.append(part("box", "brow", "Head", (0.085 * s, -0.215, 0.775), rot=(0, 8 * s, 0), scale=(0.035, 0.01, 0.009), bevel=0.004, bseg=1))
    P.append(part("sphere", "mouth", "Head", (0, -0.218, 0.625), scale=(0.035, 0.012, 0.022), seg=10, rings=6))
    P.append(part("sphere", "tongue", "Head", (0, -0.226, 0.615), scale=(0.018, 0.006, 0.009), seg=8, rings=4))
    # hair cap (back + top), fringe
    P.append(part("sphere", hair, "Head", (0, 0.025, 0.775), scale=(0.262, 0.245, 0.2), seg=20, rings=12))
    return P


def fringe(hair, n=5, z=0.83, spread=0.17, depth=-0.17, size=0.06):
    P = []
    for i in range(n):
        t = (i / (n - 1)) * 2 - 1
        P.append(part("sphere", hair, "Head", (t * spread, depth + abs(t) * 0.05, z - abs(t) * 0.02),
                      rot=(0, 0, t * 20), scale=(size, size * 0.8, size * 0.9), seg=10, rings=6))
    return P


# --------------------------------------------------------------------------
# Heroes
# --------------------------------------------------------------------------
def hero_sweep():
    P = hero_body("sweep_shirt", "sweep_hair") + fringe("sweep_hair")
    # spiky messy top
    for i, (x, y) in enumerate([(-0.1, 0.0), (0.05, -0.05), (0.12, 0.05), (-0.02, 0.1)]):
        P.append(part("cone", "sweep_hair", "Head", (x, y, 0.975), rot=(15 * (y > 0) - 10, 20 * x * 5, 0), scale=(0.065, 0.065, 0.12), seg=8))
    # headband + knot + two tails (right side of character = -X)
    P.append(part("torus", "sweep_band", "Head", (0, 0.0, 0.82), rot=(-8, 0, 0), scale=(0.262, 0.242, 0.32), minor=0.12, seg=24, mseg=6))
    P.append(part("sphere", "sweep_band", "Head", (-0.255, 0.05, 0.83), scale=(0.045, 0.04, 0.04), seg=10, rings=6))
    # AA silhouette fix: tails 2x longer and flying out sideways so they read from above
    P.append(part("box", "sweep_band", "Head", (-0.36, 0.12, 0.79), rot=(20, 30, -55), scale=(0.03, 0.012, 0.13), bevel=0.01, bseg=1))
    P.append(part("box", "sweep_band", "Head", (-0.35, 0.16, 0.88), rot=(-15, 40, -80), scale=(0.028, 0.012, 0.12), bevel=0.01, bseg=1))
    # broom in right hand (diagonal silhouette)
    P.append(part("cyl", "wood", "Socket_Tool", (-0.28, -0.03, 0.42), rot=(0, 18, 0), scale=(0.018, 0.018, 0.42), seg=8))
    P.append(part("cyl", "sweep_band", "Socket_Tool", (-0.21, -0.03, 0.645), rot=(0, 18, 0), scale=(0.028, 0.028, 0.025), seg=10))
    P.append(part("cone", "straw", "Socket_Tool", (-0.185, -0.03, 0.73), rot=(0, 18 + 180, 0), r1=1, r2=0.45, scale=(0.08, 0.04, 0.17), seg=12))
    return P


def hero_bubble():
    P = hero_body("bubble_shirt", "bubble_hair") + fringe("bubble_hair", n=4, spread=0.16)
    for s in (1, -1):  # space buns: the silhouette key
        P.append(part("sphere", "bubble_hair", "Head", (0.17 * s, 0.05, 0.94), scale=(0.095, 0.09, 0.09), seg=14, rings=8))
    # big sponge held in left hand
    P.append(part("box", "sponge", "Hand_L", (0.26, -0.09, 0.33), rot=(10, 0, 15), scale=(0.11, 0.07, 0.075), bevel=0.03, bseg=2))
    for i, (x, z) in enumerate([(0.22, 0.36), (0.29, 0.31), (0.25, 0.29), (0.31, 0.37)]):
        P.append(part("sphere", "dirt_dark" if False else "straw", "Hand_L", (x, -0.162, z), scale=(0.012, 0.006, 0.012), seg=6, rings=4))
    return P


def hero_press():
    P = hero_body("press_shirt", "press_hair") + fringe("press_hair", n=4, z=0.8, size=0.055)
    # metal bucket worn as a hat (trapezoid silhouette), tilted
    P.append(part("cone", "metal", "Head", (0.0, 0.0, 0.96), rot=(-6, 8, 0), r1=0.80, r2=1.0, scale=(0.24, 0.24, 0.22), seg=20))
    P.append(part("torus", "metal_dark", "Head", (0.0, 0.0, 1.07), rot=(-6, 8, 0), scale=(0.24, 0.24, 0.24), minor=0.07, seg=20, mseg=5))
    P.append(part("torus", "metal_dark", "Head", (0.0, 0.0, 0.9), rot=(-6, 8, 0), scale=(0.2, 0.2, 0.2), minor=0.06, seg=20, mseg=5))
    P.append(part("torus", "metal_dark", "Head", (0.215, -0.02, 0.86), rot=(0, 90, 0), scale=(0.08, 0.08, 0.08), minor=0.08, seg=12, mseg=4))
    # mop in right hand
    P.append(part("cyl", "wood", "Socket_Tool", (-0.28, -0.03, 0.42), rot=(0, 10, 0), scale=(0.018, 0.018, 0.46), seg=8))
    for i in range(7):
        a = i / 7 * math.tau
        P.append(part("cyl", "mop", "Socket_Tool", (-0.345 + 0.05 * math.cos(a), -0.03 + 0.05 * math.sin(a), -0.0),
                      rot=(15 * math.sin(a), 15 * math.cos(a), 0), scale=(0.022, 0.022, 0.08), seg=6))
    P.append(part("sphere", "mop", "Socket_Tool", (-0.345, -0.03, 0.06), scale=(0.07, 0.07, 0.04), seg=10, rings=6))
    return P


def hero_spray():
    P = hero_body("spray_shirt", "spray_hair")
    # black bob: wider shell down to the jaw + straight fringe
    P.append(part("sphere", "spray_hair", "Head", (0, 0.03, 0.72), scale=(0.275, 0.255, 0.25), seg=20, rings=12))
    P.append(part("box", "spray_hair", "Head", (0, -0.17, 0.81), rot=(-20, 0, 0), scale=(0.2, 0.04, 0.05), bevel=0.03, bseg=2))
    # goggles on the forehead: strap + 2 lenses (the silhouette key)
    for s in (1, -1):  # flared bob ends at the jaw (sides only, the face stays clear)
        P.append(part("sphere", "spray_hair", "Head", (0.235 * s, 0.05, 0.6), rot=(0, 0, 0), scale=(0.075, 0.15, 0.09), seg=12, rings=8))
    # AA silhouette fix: goggles sit on the crown, lenses face up-forward, so the game camera sees them
    P.append(part("torus", "goggle_strap", "Head", (0, 0.02, 0.88), rot=(-28, 0, 0), scale=(0.275, 0.255, 0.3), minor=0.12, seg=24, mseg=5))
    for s in (1, -1):
        P.append(part("cyl", "metal_dark", "Head", (0.085 * s, -0.16, 0.95), rot=(38, 0, 0), scale=(0.075, 0.075, 0.035), seg=14))
        P.append(part("cyl", "goggle_lens", "Head", (0.085 * s, -0.175, 0.965), rot=(38, 0, 0), scale=(0.06, 0.06, 0.014), seg=14))
    # pink spray bottle in right hand
    P.append(part("cyl", "spray_pink", "Hand_R", (-0.29, -0.08, 0.33), rot=(-10, 0, 0), scale=(0.045, 0.045, 0.075), seg=12))
    P.append(part("box", "white", "Hand_R", (-0.29, -0.11, 0.42), rot=(-10, 0, 0), scale=(0.03, 0.05, 0.025), bevel=0.01, bseg=1))
    P.append(part("box", "white", "Hand_R", (-0.29, -0.13, 0.385), rot=(-30, 0, 0), scale=(0.012, 0.012, 0.03), bevel=0.004, bseg=1))
    return P


# --- heroes 5-8 (art/docs/24_Hero_Roster_Expansion.md section 5) ---------
def hero_wash():
    """ซัก Wash: tall swirled towel turban + 3 clothespins, giant clothespin tool."""
    P = hero_body("wash_shirt", "wash_hair") + fringe("wash_hair", n=4, z=0.8, size=0.055)
    # towel turban: stacked rings shrinking upward + tip (tallest head in the team)
    for i, (z, r) in enumerate(((0.9, 0.25), (0.98, 0.21), (1.05, 0.16), (1.11, 0.1))):
        P.append(part("torus", "towel", "Head", (0.01 * i, 0.01 * i, z), rot=(-6, 4 * i, 25 * i),
                      scale=(r, r * 0.95, r * 0.9), minor=0.32, seg=14, mseg=5))
    P.append(part("cone", "towel", "Head", (0.04, 0.04, 1.16), rot=(-10, 15, 0), scale=(0.07, 0.07, 0.09), seg=10))
    P.append(part("torus", "wash_shirt", "Head", (0, 0, 0.94), rot=(-6, 0, 0), scale=(0.235, 0.225, 0.2), minor=0.06, seg=18, mseg=4))
    # three yellow clothespins clipped on the front of the towel
    for k in (-1, 0, 1):
        P.append(part("box", "gold", "Head", (0.09 * k, -0.2 + 0.02 * abs(k), 0.99 - 0.02 * abs(k)), rot=(-20, 0, -10 * k),
                      scale=(0.016, 0.014, 0.05), bevel=0.006, bseg=1))
    # ponytail at the back
    P.append(part("sphere", "wash_hair", "Head", (0, 0.26, 0.68), rot=(30, 0, 0), scale=(0.07, 0.06, 0.12), seg=12, rings=8))
    # giant clothespin held like pincers (right hand)
    for s in (1, -1):
        P.append(part("box", "gold", "Socket_Tool", (-0.3, -0.04 + 0.025 * s, 0.42), rot=(6 * s, 25, 0),
                      scale=(0.018, 0.016, 0.17), bevel=0.008, bseg=1))
    P.append(part("cyl", "metal", "Socket_Tool", (-0.31, -0.04, 0.38), rot=(90, 0, 0), scale=(0.03, 0.03, 0.07), seg=10))
    return P


def hero_iron():
    """รีด Press (id iron): iron-shaped silver helmet (shield from above) + steam iron."""
    P = hero_body("iron_shirt", "spray_hair") + fringe("spray_hair", n=6, z=0.82, spread=0.18, size=0.05)
    # iron helmet: rounded back + pointed front = shield seen from the game camera
    P.append(part("sphere", "metal", "Head", (0, 0.03, 0.97), scale=(0.25, 0.25, 0.08), seg=16, rings=8))
    P.append(part("cone", "metal", "Head", (0, -0.17, 0.97), rot=(90, 0, 0), r1=1, r2=0.15, scale=(0.2, 0.08, 0.22), seg=10))
    P.append(part("box", "metal_dark", "Head", (0, 0, 0.92), scale=(0.25, 0.27, 0.012), bevel=0.01, bseg=1))
    # curved handle on top + red status light
    P.append(part("torus", "iron_handle", "Head", (0, 0.02, 1.06), rot=(0, 90, 0), scale=(0.11, 0.08, 0.08), minor=0.18, seg=16, mseg=5))
    P.append(part("sphere", "sweep_band", "Head", (0.12, -0.12, 1.03), scale=(0.022, 0.022, 0.022), seg=8, rings=6))
    # steam iron held like a shield (left hand)
    P.append(part("sphere", "metal", "Hand_L", (0.3, -0.11, 0.32), rot=(0, 90, 0), scale=(0.05, 0.1, 0.075), seg=10, rings=6))
    P.append(part("cone", "metal", "Hand_L", (0.3, -0.11, 0.42), rot=(0, 90, 0), r1=1, r2=0.2, scale=(0.05, 0.07, 0.09), seg=12))
    P.append(part("torus", "iron_handle", "Hand_L", (0.32, -0.11, 0.35), rot=(0, 0, 0), scale=(0.05, 0.05, 0.08), minor=0.25, seg=12, mseg=4))
    for k in range(3):  # steam puffs
        P.append(part("sphere", "white", "Hand_L", (0.33 + 0.03 * k, -0.2 - 0.03 * k, 0.4 + 0.06 * k), scale=(0.03, 0.03, 0.03), seg=8, rings=6))
    return P


def hero_duster():
    """ปัด Duster: 5-spike crest (star from above) with red tips, rainbow feather duster."""
    P = hero_body("duster_shirt", "sweep_hair") + fringe("sweep_hair", n=5, size=0.055)
    for i in range(5):
        a = i / 5 * math.tau + math.pi / 2
        x, y = 0.12 * math.cos(a), 0.12 * math.sin(a)
        tilt_x, tilt_y = -28 * math.sin(a), 28 * math.cos(a)
        P.append(part("cone", "sweep_hair", "Head", (x, y, 1.0), rot=(tilt_x, tilt_y, 0), scale=(0.075, 0.075, 0.2), seg=8))
        P.append(part("cone", "sweep_band", "Head", (x * 1.75, y * 1.75, 1.15), rot=(tilt_x, tilt_y, 0), scale=(0.035, 0.035, 0.06), seg=8))
    P.append(part("cone", "sweep_hair", "Head", (0, 0, 1.03), scale=(0.08, 0.08, 0.18), seg=8))
    P.append(part("cone", "sweep_band", "Head", (0, 0, 1.15), scale=(0.04, 0.04, 0.06), seg=8))
    # feather duster: long handle + round rainbow pom above the shoulder
    P.append(part("cyl", "wood", "Socket_Tool", (-0.3, -0.03, 0.5), rot=(0, 14, 0), scale=(0.016, 0.016, 0.5), seg=8))
    pom = [("sweep_band", (0, 0, 0)), ("gold", (0.05, 0.03, 0.04)), ("bubble_shirt", (-0.05, 0.02, 0.05)),
           ("press_shirt", (0.0, -0.05, 0.06)), ("sweep_band", (0.03, 0.04, 0.1)), ("gold", (-0.03, -0.03, 0.11))]
    for c, (dx, dy, dz) in pom:
        P.append(part("sphere", c, "Socket_Tool", (-0.21 + dx, -0.03 + dy, 0.78 + dz), scale=(0.09, 0.09, 0.085), seg=10, rings=7))
    return P


def hero_vac():
    """ดูด Vac: big yellow ear-muffs, backpack vacuum with a hose to the hand."""
    P = hero_body("vac_shirt", "vac_hair") + fringe("vac_hair", n=4, size=0.065)
    for i, (x, y) in enumerate([(-0.08, 0.02), (0.07, -0.03), (0.0, 0.1)]):  # messy tufts
        P.append(part("sphere", "vac_hair", "Head", (x, y, 0.95), scale=(0.07, 0.07, 0.06), seg=8, rings=5))
    # ear-muff band over the crown (arc of beads) + two big cups
    for i in range(7):
        a = math.pi * (0.12 + 0.76 * i / 6)
        P.append(part("sphere", "gold_dark", "Head", (0.275 * math.cos(a), 0.0, 0.72 + 0.29 * math.sin(a)),
                      scale=(0.034, 0.038, 0.034), seg=6, rings=4))
    for s in (1, -1):
        P.append(part("cyl", "gold", "Head", (0.27 * s, 0.0, 0.72), rot=(0, 90, 0), scale=(0.1, 0.1, 0.06), seg=12))
        P.append(part("sphere", "gold", "Head", (0.31 * s, 0.0, 0.72), scale=(0.035, 0.09, 0.09), seg=10, rings=6))
    # backpack vacuum (only hero with a load on the back)
    P.append(part("cyl", "trash", "Chest", (0, 0.2, 0.36), scale=(0.1, 0.1, 0.2), seg=12))
    P.append(part("sphere", "trash", "Chest", (0, 0.2, 0.46), scale=(0.1, 0.1, 0.05), seg=12, rings=5))
    P.append(part("torus", "vac_shirt", "Chest", (0, 0.2, 0.36), scale=(0.1, 0.1, 0.1), minor=0.12, seg=12, mseg=4))
    # hose: bead chain from tank top over the right shoulder down to the nozzle
    pts = [(0.0, 0.2, 0.5), (-0.08, 0.16, 0.56), (-0.16, 0.08, 0.56), (-0.22, 0.0, 0.52), (-0.28, -0.05, 0.46),
           (-0.31, -0.08, 0.39), (-0.31, -0.1, 0.33)]
    for x, y, z in pts:
        P.append(part("sphere", "metal_dark", "Chest", (x, y, z), scale=(0.03, 0.03, 0.03), seg=6, rings=4))
    P.append(part("cyl", "metal", "Socket_Tool", (-0.3, -0.11, 0.24), rot=(-15, 0, 0), scale=(0.02, 0.02, 0.1), seg=8))
    P.append(part("box", "trash_dark", "Socket_Tool", (-0.3, -0.13, 0.13), rot=(-15, 0, 0), scale=(0.06, 0.03, 0.02), bevel=0.01, bseg=1))
    return P


# --------------------------------------------------------------------------
# Enemies (no limbs; squash & stretch happens in code/BobSquash)
# --------------------------------------------------------------------------
ENEMY_BONES = [
    ("Root", None, (0, 0, 0), (0, 0.12, 0)),
    ("Body", "Root", (0, 0, 0.02), (0, 0, 0.5)),
    ("Top", "Body", (0, 0, 0.5), (0, 0, 0.8)),
]


def angry_face(z, y, sx=0.1, eye=0.07, P=None, bone="Body", teeth=True):
    for s in (1, -1):
        P.append(part("sphere", "white", bone, (sx * s, y, z), scale=(eye, eye * 0.5, eye * 1.1), seg=14, rings=8))
        P.append(part("sphere", "eye", bone, (sx * s * 0.9, y - eye * 0.42, z - eye * 0.1), scale=(eye * 0.45, eye * 0.2, eye * 0.55), seg=10, rings=6))
        P.append(part("box", "brow", bone, (sx * s * 1.05, y - 0.01, z + eye * 1.25), rot=(0, -25 * s, 0), scale=(eye * 0.95, 0.015, eye * 0.22), bevel=0.008, bseg=1))
    P.append(part("sphere", "trash_dark", bone, (0, y + 0.01, z - eye * 2.0), scale=(eye * 1.5, eye * 0.4, eye * 0.6), seg=12, rings=6))
    if teeth:
        for s in (1, -1):
            P.append(part("cone", "fang", bone, (0.045 * s, y - 0.012, z - eye * 1.6), rot=(180, 0, 0), scale=(0.022, 0.012, 0.035), seg=6))
    return P


def enemy_dirt():
    P = []
    lumps = [((0, 0, 0.2), (0.32, 0.28, 0.22)), ((0, 0.02, 0.4), (0.24, 0.22, 0.18)),
             ((0.02, 0.02, 0.55), (0.14, 0.13, 0.11)), ((0.22, 0.02, 0.15), (0.14, 0.13, 0.12)),
             ((-0.23, 0.0, 0.14), (0.15, 0.13, 0.11))]
    for l, s in lumps:
        P.append(part("sphere", "dirt", "Body", l, scale=s, seg=16, rings=10))
    for l in [(0.18, -0.22, 0.3), (-0.2, -0.2, 0.22), (0.05, -0.19, 0.5)]:
        P.append(part("sphere", "dirt_dark", "Body", l, scale=(0.04, 0.012, 0.03), seg=8, rings=5))
    angry_face(0.36, -0.235, P=P)
    return P


def enemy_slime():
    P = [part("sphere", "slime", "Body", (0, 0, 0.2), scale=(0.33, 0.3, 0.22), seg=18, rings=10),
         part("cone", "slime", "Top", (0, 0, 0.45), r1=1, r2=0.0, scale=(0.22, 0.2, 0.3), seg=16),
         part("sphere", "slime", "Body", (0, 0, 0.36), scale=(0.24, 0.22, 0.16), seg=16, rings=8)]
    for l, s in [((-0.15, -0.2, 0.42), (0.04, 0.015, 0.06)), ((-0.08, -0.22, 0.47), (0.015, 0.01, 0.015))]:
        P.append(part("sphere", "slime_light", "Body", l, scale=s, seg=8, rings=5))
    for l in [(0.42, -0.1, 0.05), (-0.4, 0.05, 0.05), (0.3, 0.25, 0.04)]:
        P.append(part("sphere", "slime", "Root", l, scale=(0.05, 0.05, 0.035), seg=10, rings=6))
    angry_face(0.3, -0.26, sx=0.09, eye=0.06, P=P)
    return P


def enemy_trash():
    P = [part("cone", "trash", "Body", (0, 0, 0.28), r1=0.82, r2=1.0, scale=(0.26, 0.26, 0.5), seg=18)]
    for z in (0.12, 0.3, 0.46):
        P.append(part("torus", "metal_dark", "Body", (0, 0, z), scale=(0.23 + z * 0.06, 0.23 + z * 0.06, 0.2), minor=0.06, seg=18, mseg=4))
    # dark opening + lid (lid lifts on Top bone)
    P.append(part("cyl", "trash_dark", "Body", (0, -0.02, 0.53), rot=(12, 0, 0), scale=(0.25, 0.25, 0.02), seg=18))
    P.append(part("cyl", "trash", "Top", (0, 0.04, 0.62), rot=(22, 0, 0), scale=(0.29, 0.29, 0.03), seg=18))
    P.append(part("torus", "metal_dark", "Top", (0, 0.06, 0.67), rot=(22 + 90, 0, 0), scale=(0.06, 0.06, 0.06), minor=0.2, seg=10, mseg=4))
    for s in (1, -1):  # eyes peeking from the dark opening
        P.append(part("sphere", "white", "Body", (0.075 * s, -0.17, 0.53), scale=(0.06, 0.03, 0.05), seg=12, rings=6))
        P.append(part("sphere", "eye", "Body", (0.06 * s, -0.195, 0.52), scale=(0.025, 0.01, 0.028), seg=8, rings=5))
    return P


def enemy_boss():
    # one wide dome + off-centre bump + drips; stacked tiers read as the wrong object (review 20 sec. 3)
    P = [part("sphere", "boss", "Body", (0, 0, 0.62), scale=(1.0, 0.9, 0.85), seg=28, rings=16),
         part("sphere", "boss", "Top", (0.25, 0.05, 1.32), scale=(0.38, 0.36, 0.32), seg=18, rings=10)]
    for i in range(7):
        a = i / 7 * math.tau
        P.append(part("sphere", "boss", "Body", (0.92 * math.cos(a), 0.82 * math.sin(a), 0.12), scale=(0.2, 0.2, 0.14), seg=12, rings=6))
    for l, s in [((-0.45, -0.6, 1.1), (0.12, 0.04, 0.18)), ((-0.3, -0.65, 1.25), (0.05, 0.02, 0.05))]:
        P.append(part("sphere", "slime_light", "Body", l, scale=s, seg=10, rings=6))
    # crown
    P.append(part("cyl", "gold", "Top", (0.25, 0.05, 1.66), rot=(0, 12, 0), scale=(0.27, 0.27, 0.09), seg=20))
    for i in range(5):
        a = i / 5 * math.tau + 0.3
        P.append(part("cone", "gold", "Top", (0.25 + 0.23 * math.cos(a), 0.05 + 0.23 * math.sin(a), 1.8 - 0.05 * math.cos(a)), scale=(0.07, 0.07, 0.12), seg=8))
        P.append(part("sphere", "sweep_band" if i % 2 else "goggle_lens", "Top", (0.25 + 0.27 * math.cos(a), 0.05 + 0.27 * math.sin(a), 1.66), scale=(0.035, 0.02, 0.035), seg=8, rings=5))
    angry_face(0.95, -0.8, sx=0.26, eye=0.16, P=P)
    for l in [(1.1, -0.3, 0.08), (-1.05, 0.2, 0.08), (0.7, 0.8, 0.06), (-0.6, -0.9, 0.06)]:
        P.append(part("sphere", "boss", "Root", l, scale=(0.12, 0.12, 0.08), seg=10, rings=6))
    return P


ENEMY_BOSS_BONES = [
    ("Root", None, (0, 0, 0), (0, 0.3, 0)),
    ("Body", "Root", (0, 0, 0.05), (0, 0, 1.2)),
    ("Top", "Body", (0, 0, 1.2), (0, 0, 1.9)),
]


# --------------------------------------------------------------------------
# Animation: keyed from code, following the 12 principles we can afford:
# anticipation, squash/stretch, overlap (head lags the body by 2 frames),
# arcs, slow in/out (Bezier), secondary action (apron/tool).
# --------------------------------------------------------------------------
def key(rig, bone, frame, rot=None, loc=None, scl=None):
    pb = rig.pose.bones[bone]
    pb.rotation_mode = "XYZ"
    if rot is not None:
        pb.rotation_euler = [math.radians(a) for a in rot]
        pb.keyframe_insert("rotation_euler", frame=frame)
    if loc is not None:
        pb.location = loc
        pb.keyframe_insert("location", frame=frame)
    if scl is not None:
        pb.scale = scl
        pb.keyframe_insert("scale", frame=frame)


def new_action(rig, name):
    if rig.animation_data is None:
        rig.animation_data_create()
    act = bpy.data.actions.new(name)
    act.use_fake_user = True
    rig.animation_data.action = act
    for pb in rig.pose.bones:
        pb.rotation_mode = "XYZ"
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)
        pb.scale = (1, 1, 1)
    return act


def push_nla(rig, act, start=0):
    tr = rig.animation_data.nla_tracks.new()
    tr.name = act.name
    tr.strips.new(act.name, start, act)
    rig.animation_data.action = None


def anim_hero(rig):
    # idle: 60 f breathing; head lags chest (overlap)
    a = new_action(rig, "idle")
    for f, k in ((0, 0), (30, 1), (60, 0)):
        key(rig, "Hips", f, loc=(0, 0, -0.008 * k))
        key(rig, "Chest", f, scl=(1 + 0.02 * k, 1 + 0.02 * k, 1 - 0.015 * k))
        key(rig, "Head", f + (2 if f < 60 else 0), rot=(3 * k, 0, 2 * k))
        key(rig, "UpperArm_L", f, rot=(0, -4 * k, 0))
        key(rig, "UpperArm_R", f, rot=(0, 4 * k, 0))
    push_nla(rig, a)
    # walk: 24 f; contact frames 0 and 12 are the low points (squash)
    a = new_action(rig, "walk")
    for f in range(0, 25, 3):
        ph = f / 24 * math.tau
        sw = math.sin(ph)
        key(rig, "UpperLeg_L", f, rot=(32 * sw, 0, 0))
        key(rig, "UpperLeg_R", f, rot=(-32 * sw, 0, 0))
        key(rig, "Foot_L", f, rot=(max(0, -25 * sw), 0, 0))
        key(rig, "Foot_R", f, rot=(max(0, 25 * sw), 0, 0))
        key(rig, "UpperArm_L", f, rot=(-28 * sw, 0, 0))
        key(rig, "UpperArm_R", f, rot=(28 * sw, 0, 0))
        bob = abs(math.cos(ph))  # 1 at contact
        key(rig, "Hips", f, loc=(0, 0, 0.03 * (1 - bob)), rot=(0, 0, 6 * sw))
        key(rig, "Spine", f, rot=(-7, 0, -4 * sw), scl=(1 + 0.03 * bob, 1, 1 - 0.03 * bob))
        key(rig, "Head", f, rot=(4 * math.sin(ph - 0.6), 0, 3 * math.sin(ph - 0.6)))
        key(rig, "Apron_01", f, rot=(-8 * abs(sw), 0, 0))
    push_nla(rig, a)
    # cheer: anticipation squat -> jump -> overshoot -> settle (40 f)
    a = new_action(rig, "cheer")
    seq = [(0, 0, 0, 0), (8, -0.06, 1, 0.3), (14, 0.16, -1, 1.0), (20, 0.18, -0.5, 1.0),
           (28, -0.03, 0.6, 0.6), (34, 0.01, -0.1, 0.2), (40, 0, 0, 0)]
    for f, h, sq, arm in seq:
        key(rig, "Hips", f, loc=(0, 0, h))
        key(rig, "Spine", f, scl=(1 + 0.08 * sq, 1 + 0.08 * sq, 1 - 0.1 * sq))
        key(rig, "UpperArm_L", f, rot=(0, -150 * arm, 0))
        key(rig, "UpperArm_R", f, rot=(0, 150 * arm, 0))
        key(rig, "Head", min(40, f + 2), rot=(-8 * arm, 0, 0))
    push_nla(rig, a)


def anim_enemy(rig, big=False):
    s = 1.6 if big else 1.0
    a = new_action(rig, "idle")
    for f, k in ((0, 0), (15, 1), (30, 0), (45, -1), (60, 0)):
        key(rig, "Body", f, scl=(1 + 0.06 * k * s, 1 + 0.06 * k * s, 1 - 0.08 * k * s))
        key(rig, "Top", f + 3 if f < 60 else 60, rot=(0, 6 * k, 0))
    push_nla(rig, a)
    # "caught" (A4 in spec 16): squash hard, stretch up, shrink
    a = new_action(rig, "caught")
    for f, sx, sz in ((0, 1, 1), (4, 1.3, 0.7), (10, 0.8, 1.3), (16, 1.1, 0.9), (24, 0.6, 0.6), (30, 0.4, 0.4)):
        key(rig, "Body", f, scl=(sx, sx, sz))
    push_nla(rig, a)


# --------------------------------------------------------------------------
BUDGET = {"hero": (4000, 6000), "enemy": (1500, 3000), "boss": (8000, 10000)}


def build(name, kind, parts_fn, bones=BONES, anim=anim_hero, bscale=1.0):
    parts = parts_fn()
    rig = make_armature(name, bscale, bones)
    mesh = bind(parts, rig, name)
    anim(rig)
    tris = tri_count(mesh)
    return rig, mesh, tris


def export(rig, mesh, out, name):
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    mesh.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.gltf(filepath=os.path.join(out, name + ".glb"), use_selection=True,
                              export_format="GLB", export_animations=True,
                              export_animation_mode="NLA_TRACKS", export_skins=True,
                              export_yup=True, export_apply=False)
    try:
        bpy.ops.export_scene.fbx(filepath=os.path.join(out, name + ".fbx"), use_selection=True,
                                 apply_unit_scale=True, axis_forward="-Z", axis_up="Y",
                                 add_leaf_bones=False, bake_anim_use_nla_strips=True,
                                 bake_anim_use_all_actions=False, path_mode="COPY", embed_textures=False)
    except Exception as e:  # FBX is a convenience; GLB is the master
        print("FBX export skipped:", e)


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="art/aa/kit")
    args = ap.parse_args(argv)
    out = os.path.abspath(args.out)
    os.makedirs(out, exist_ok=True)

    jobs = [
        ("sweep", "hero", hero_sweep, BONES, anim_hero, 1.0),
        ("bubble", "hero", hero_bubble, BONES, anim_hero, 1.0),
        ("press", "hero", hero_press, BONES, anim_hero, 1.0),
        ("spray", "hero", hero_spray, BONES, anim_hero, 1.0),
        ("wash", "hero", hero_wash, BONES, anim_hero, 1.0),
        ("iron", "hero", hero_iron, BONES, anim_hero, 1.0),
        ("duster", "hero", hero_duster, BONES, anim_hero, 1.0),
        ("vac", "hero", hero_vac, BONES, anim_hero, 1.0),
        ("dirt", "enemy", enemy_dirt, ENEMY_BONES, anim_enemy, 1.0),
        ("slime", "enemy", enemy_slime, ENEMY_BONES, anim_enemy, 1.0),
        ("trash", "enemy", enemy_trash, ENEMY_BONES, anim_enemy, 1.0),
        ("boss_slime", "boss", enemy_boss, ENEMY_BOSS_BONES, lambda r: anim_enemy(r, True), 1.0),
    ]
    report = []
    for name, kind, fn, bones, anim, bs in jobs:
        reset()
        img = make_palette_image(os.path.join(out, "T_BOB_Palette.png"))
        make_material(img)
        rig, mesh, tris = build(name, kind, fn, bones, anim, bs)
        lo, hi = BUDGET[kind]
        export(rig, mesh, out, "SK_" + name)
        report.append({"id": name, "kind": kind, "tris": tris, "budget": [lo, hi],
                       "ok": tris <= hi, "bones": len(bones), "materials": 1,
                       "anims": [t.name for t in rig.animation_data.nla_tracks]})
        print(f"{name:11s} {kind:6s} tris={tris:5d} budget<={hi} {'OK' if tris <= hi else 'OVER'}")
    with open(os.path.join(out, "kit_report.json"), "w") as f:
        json.dump(report, f, indent=1)


if __name__ == "__main__":
    main()
