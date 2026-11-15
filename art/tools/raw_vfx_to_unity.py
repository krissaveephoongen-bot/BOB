#!/usr/bin/env python3
"""Cut the dark studio backdrop off the Leonardo RAW VFX and drop RGBA PNGs into Unity.

The RAW set (docs/10) was generated on a near-black navy card, so a flood fill from the border is enough:
everything reachable from the edge that stays within a small colour distance of the backdrop becomes
transparent, and the subject keeps its own dark shading because it is not connected to the edge.

Usage:  python3 tools/raw_vfx_to_unity.py            # all listed effects
        python3 tools/raw_vfx_to_unity.py fx_dust    # one effect
Output: unity/Assets/Resources/Textures/Minigame/<name>.png (cropped, max 512 px, alpha)
"""
import os
import sys
from collections import deque

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, "leonardo_raw", "vfx")
OUT = os.path.join(ROOT, "..", "unity", "Assets", "Resources", "Textures", "Minigame")

# <name>: <raw file>. One variant each; the others in the set stay on disk for future swaps.
EFFECTS = {
    "fx_hitstar": "fx_hitstar_0.jpg",
    "fx_sparkle": "fx_sparkle_0.jpg",
    "fx_foam": "fx_foam_0.jpg",
    "fx_dust": "fx_dust_0.jpg",
    "fx_splash": "fx_splash_0.jpg",
    "fx_slimesplat": "fx_slimesplat_0.jpg",
}

MAX_SIDE = 512
NEAR_BG = 26.0   # colour distance that still counts as backdrop
SOFT_EDGE = 1.2  # px of alpha blur, so the cut edge does not stair-step at card size


def backdrop(px):
    """Median of the outermost ring: the card colour, ignoring any subject that touches an edge."""
    edge = np.concatenate([px[0], px[-1], px[:, 0], px[:, -1]]).astype(np.float32)
    return np.median(edge, axis=0)


def flood(px, bg):
    """True where a pixel is both near the backdrop colour and connected to the frame."""
    h, w, _ = px.shape
    near = np.linalg.norm(px.astype(np.float32) - bg, axis=2) < NEAR_BG
    seen = np.zeros((h, w), bool)
    q = deque()
    for x in range(w):
        for y in (0, h - 1):
            if near[y, x] and not seen[y, x]:
                seen[y, x] = True
                q.append((y, x))
    for y in range(h):
        for x in (0, w - 1):
            if near[y, x] and not seen[y, x]:
                seen[y, x] = True
                q.append((y, x))
    while q:
        y, x = q.popleft()
        for ny, nx in ((y - 1, x), (y + 1, x), (y, x - 1), (y, x + 1)):
            if 0 <= ny < h and 0 <= nx < w and near[ny, nx] and not seen[ny, nx]:
                seen[ny, nx] = True
                q.append((ny, nx))
    return seen


def cut(name, raw_name):
    src = os.path.join(RAW, raw_name)
    img = Image.open(src).convert("RGB")
    px = np.asarray(img)
    bg_mask = flood(px, backdrop(px))

    alpha = np.where(bg_mask, 0, 255).astype(np.uint8)
    alpha_img = Image.fromarray(alpha, "L").filter(ImageFilter.GaussianBlur(SOFT_EDGE))

    rgba = img.convert("RGBA")
    rgba.putalpha(alpha_img)
    bbox = alpha_img.point(lambda v: 255 if v > 8 else 0).getbbox()
    if bbox:
        pad = 6
        bbox = (max(0, bbox[0] - pad), max(0, bbox[1] - pad),
                min(rgba.width, bbox[2] + pad), min(rgba.height, bbox[3] + pad))
        rgba = rgba.crop(bbox)

    w, h = rgba.size
    if max(w, h) > MAX_SIDE:
        k = MAX_SIDE / max(w, h)
        rgba = rgba.resize((max(1, round(w * k)), max(1, round(h * k))), Image.LANCZOS)

    os.makedirs(OUT, exist_ok=True)
    dst = os.path.abspath(os.path.join(OUT, name + ".png"))
    rgba.save(dst, optimize=True)
    cleared = 100.0 * bg_mask.mean()
    print(f"{name:14s} {img.size[0]}x{img.size[1]} -> {rgba.size[0]}x{rgba.size[1]}  "
          f"cut {cleared:5.1f}%  {os.path.getsize(dst) // 1024} KB")


def main():
    wanted = sys.argv[1:] or list(EFFECTS)
    for name in wanted:
        if name not in EFFECTS:
            sys.exit(f"unknown effect {name}; pick from {', '.join(EFFECTS)}")
        cut(name, EFFECTS[name])


if __name__ == "__main__":
    main()
