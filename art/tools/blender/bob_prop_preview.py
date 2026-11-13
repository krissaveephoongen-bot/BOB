"""BOB prop preview: renders one or more .glb files side by side at the game camera
angle (close to the 45 deg game camera, Art Bible 21 section 3) so a raw AI mesh can be compared with the
cleaned SM_ prop. Cycles on CPU, so it runs with the pip bpy module in the cloud.

  python art/tools/blender/bob_prop_preview.py raw.glb art/aa/props/SM_prop_bucket.glb --out art/docs/aa/aa_prop_bucket.png
"""
import argparse
import math
import os
import sys

import bpy
from mathutils import Vector


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    ap = argparse.ArgumentParser()
    ap.add_argument("src", nargs="+")
    ap.add_argument("--out", required=True)
    ap.add_argument("--size", type=int, default=640, help="pixels per model")
    ap.add_argument("--samples", type=int, default=24)
    a = ap.parse_args(argv)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    n = len(a.src)
    for i, path in enumerate(a.src):
        before = set(bpy.data.objects)
        bpy.ops.import_scene.gltf(filepath=os.path.abspath(path))
        objs = [o for o in bpy.data.objects if o not in before]
        meshes = [o for o in objs if o.type == "MESH"]
        pts = [o.matrix_world @ Vector(c) for o in meshes for c in o.bound_box]
        mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
        mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
        s = 1.0 / max(1e-6, mx.z - mn.z)  # normalise every model to height 1
        root = bpy.data.objects.new("root%d" % i, None)
        bpy.context.scene.collection.objects.link(root)
        for o in objs:
            if o.parent is None:
                o.parent = root
        root.scale = (s, s, s)
        root.location = Vector(((i - (n - 1) / 2) * 1.5, 0, 0)) - Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z)) * s
        for m in meshes:  # palette texture must not blur
            for slot in m.material_slots:
                if slot.material and slot.material.use_nodes:
                    for nd in slot.material.node_tree.nodes:
                        if nd.type == "BSDF_PRINCIPLED":
                            nd.inputs["Roughness"].default_value = 0.8

    sc = bpy.context.scene
    bpy.ops.mesh.primitive_plane_add(size=40)
    floor = bpy.context.active_object
    fm = bpy.data.materials.new("floor")
    fm.diffuse_color = (0.93, 0.88, 0.80, 1)
    fm.use_nodes = True
    fm.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = (0.93, 0.88, 0.80, 1)
    floor.data.materials.append(fm)

    cam = bpy.data.objects.new("cam", bpy.data.cameras.new("cam"))
    sc.collection.objects.link(cam)
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 1.5 * n + 0.2
    d = 6
    cam.location = (0, -d * math.cos(math.radians(35)), 0.5 + d * math.sin(math.radians(35)))  # a little lower than the 45 deg game camera so the sides read
    look = Vector((0, 0, 0.5)) - Vector(cam.location)
    cam.rotation_euler = look.to_track_quat("-Z", "Y").to_euler()
    cam.location.x = 0
    sc.camera = cam

    def light(kind, loc, energy, col):
        L = bpy.data.objects.new(kind, bpy.data.lights.new(kind, "SUN"))
        L.data.energy = energy
        L.data.color = col
        L.rotation_euler = (Vector((0, 0, 0)) - Vector(loc)).to_track_quat("-Z", "Y").to_euler()
        sc.collection.objects.link(L)
    light("key", (-3, -3, 5), 3.0, (1.0, 0.95, 0.85))   # warm key, upper left
    light("fill", (4, -2, 2), 1.0, (0.8, 0.9, 1.0))     # cool fill
    w = bpy.data.worlds.new("w")
    w.use_nodes = True
    w.node_tree.nodes["Background"].inputs[0].default_value = (0.9, 0.9, 0.95, 1)
    w.node_tree.nodes["Background"].inputs[1].default_value = 0.6
    sc.world = w

    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = a.samples
    sc.cycles.use_denoising = False
    sc.view_settings.view_transform = "Standard"
    sc.render.resolution_x = a.size * n
    sc.render.resolution_y = int(a.size * 1.1)
    sc.render.filepath = os.path.abspath(a.out)
    sc.render.image_settings.file_format = "PNG"
    bpy.ops.render.render(write_still=True)
    print("saved", a.out)


if __name__ == "__main__":
    main()
