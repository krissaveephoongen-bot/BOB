"""Render 3/4-view thumbnails of raw GLB models (for naming Tripo exports).

  blender -b -P art/tools/blender/bob_thumbs.py -- <out_dir> <file.glb> [more.glb ...]
  blender -b -P art/tools/blender/bob_thumbs.py -- <out_dir> --glob <dir>/*.glb

Workbench engine, texture colours, 384 px, transparent background. Fast enough for
2M-tri raw Tripo meshes and needs no GPU compute.
"""
import glob
import os
import sys

import bpy
from mathutils import Vector

SIZE = 384


def parse_argv():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    out = argv[0]
    rest = argv[1:]
    files = []
    for a in rest:
        if a.startswith("--glob"):
            continue
        files.extend(sorted(glob.glob(a)) if any(c in a for c in "*?") else [a])
    return out, files


def scene_bbox():
    pts = []
    for o in bpy.data.objects:
        if o.type != "MESH":
            continue
        for c in o.bound_box:
            pts.append(o.matrix_world @ Vector(c))
    if not pts:
        return None, None
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def render_one(path, out_dir):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x = sc.render.resolution_y = SIZE
    sc.render.film_transparent = True
    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "TEXTURE"
    sh.show_cavity = False
    sh.show_shadows = False

    try:
        bpy.ops.import_scene.gltf(filepath=path)
    except Exception as e:
        print("IMPORT FAILED", path, e)
        return False
    mn, mx = scene_bbox()
    if mn is None:
        print("NO MESH", path)
        return False
    center = (mn + mx) / 2
    radius = (mx - mn).length / 2 or 1.0

    cam_data = bpy.data.cameras.new("cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = radius * 2.1
    cam = bpy.data.objects.new("cam", cam_data)
    sc.collection.objects.link(cam)
    direction = Vector((1.0, -1.6, 0.95)).normalized()
    cam.location = center + direction * (radius * 4)
    cam.rotation_euler = (center - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc.camera = cam

    name = os.path.splitext(os.path.basename(path))[0]
    sc.render.filepath = os.path.join(out_dir, name + ".png")
    bpy.ops.render.render(write_still=True)
    return True


def main():
    out_dir, files = parse_argv()
    os.makedirs(out_dir, exist_ok=True)
    ok = 0
    for i, f in enumerate(files, 1):
        print(f"[{i}/{len(files)}] {os.path.basename(f)}", flush=True)
        if render_one(f, out_dir):
            ok += 1
    print(f"rendered {ok}/{len(files)}")


if __name__ == "__main__":
    main()
