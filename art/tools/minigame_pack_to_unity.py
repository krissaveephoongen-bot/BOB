#!/usr/bin/env python3
"""Take whatever came out of the Leonardo web app and make Unity-ready sprites.

The owner generates by hand (no API credits), so the only contract is the file name: drop
`mini_bin_blue.png` or `mini_bin_blue (1).png` into art/incoming/minigames/raw/ and this script
keys the backdrop, trims to the subject, caps the size and writes the RGBA PNG where the card
loads it from (Resources/Textures/Minigame/<name>.png).

A PNG that already carries transparency is trusted as-is (a web-app "transparent" export);
everything else is flood-filled from the frame border, the same way raw_vfx_to_unity.py cuts the
RAW VFX.

Usage:  python3 tools/minigame_pack_to_unity.py            # everything in raw/
        python3 tools/minigame_pack_to_unity.py --dry      # list what it would do
Output: unity/Assets/Resources/Textures/Minigame/<name>.png
"""
import argparse
import os
import re
from collections import deque

import numpy as np
from PIL import Image, ImageFilter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
RAW = os.path.join(ROOT, "incoming", "minigames", "raw")
OUT = os.path.join(ROOT, "..", "unity", "Assets", "Resources", "Textures", "Minigame")

MAX_SIDE = 512
NEAR_BG = 26.0    # colour distance that still counts as backdrop
SOFT_EDGE = 1.2   # px of alpha blur, so the cut edge does not stair-step at card size
ALPHA_KEEP = 0.02  # a PNG with more transparent pixels than this already has its own matte

# The names BobMinigameHost asks for. Anything else still converts, but gets a warning.
KNOWN = {
    "mini_bin_blue", "mini_bin_grey", "mini_bin_yellow", "mini_keypad_panel", "mini_key_square",
    "mini_slip_paper", "mini_dial_base", "mini_dial_needle", "mini_cloth_wet", "mini_bottle_pour",
    "mini_basin", "mini_stain_red", "mini_stain_grey", "mini_node_mark", "mini_trail_dust",
}
VIDEO_EXT = {".mp4", ".webm", ".mov"}


def target_name(filename):
    """`mini_bin_blue (1).PNG` -> `mini_bin_blue`. Browser copy suffixes must not reach Unity."""
    stem = os.path.splitext(os.path.basename(filename))[0]
    stem = re.sub(r"\s*\(\d+\)$", "", stem)
    stem = re.sub(r"[-_ ]\d+$", "", stem)
    stem = re.sub(r"[^0-9A-Za-z_-]+", "_", stem.strip()).strip("_").lower()
    return stem


def backdrop(px):
    edge = np.concatenate([px[0], px[-1], px[:, 0], px[:, -1]]).astype(np.float32)
    return np.median(edge, axis=0)


def flood(px, bg):
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


def fit(rgba):
    bbox = rgba.getchannel("A").point(lambda v: 255 if v > 8 else 0).getbbox()
    if bbox:
        pad = 6
        bbox = (max(0, bbox[0] - pad), max(0, bbox[1] - pad),
                min(rgba.width, bbox[2] + pad), min(rgba.height, bbox[3] + pad))
        rgba = rgba.crop(bbox)
    w, h = rgba.size
    if max(w, h) > MAX_SIDE:
        k = MAX_SIDE / max(w, h)
        rgba = rgba.resize((max(1, round(w * k)), max(1, round(h * k))), Image.LANCZOS)
    return rgba


def convert(src, dry):
    name = target_name(src)
    img = Image.open(src)
    note = ""
    if img.mode in ("RGBA", "LA", "PA") or (img.mode == "P" and "transparency" in img.info):
        rgba = img.convert("RGBA")
        alpha = np.asarray(rgba.getchannel("A"))
        kept = float((alpha < 16).mean())
        if kept >= ALPHA_KEEP:
            note = f"alpha kept {100 * kept:.0f}%"
        else:
            rgba, note = key(img.convert("RGB"))
    else:
        rgba, note = key(img.convert("RGB"))

    rgba = fit(rgba)
    if dry:
        print(f"{name:16s} {img.size[0]}x{img.size[1]} -> {rgba.size[0]}x{rgba.size[1]}  {note}")
        return
    os.makedirs(OUT, exist_ok=True)
    dst = os.path.abspath(os.path.join(OUT, name + ".png"))
    rgba.save(dst, optimize=True)
    flag = "" if name in KNOWN else "  <-- not in the 15-name list"
    print(f"{name:16s} {img.size[0]}x{img.size[1]} -> {rgba.size[0]}x{rgba.size[1]}  {note:18s} "
          f"{os.path.getsize(dst) // 1024} KB{flag}")


def key(img):
    px = np.asarray(img)
    mask = flood(px, backdrop(px))
    cleared = 100.0 * mask.mean()
    alpha = np.where(mask, 0, 255).astype(np.uint8)
    alpha_img = Image.fromarray(alpha, "L").filter(ImageFilter.GaussianBlur(SOFT_EDGE))
    rgba = img.convert("RGBA")
    rgba.putalpha(alpha_img)
    note = f"keyed {cleared:.0f}%"
    if cleared < 3.0:
        note += " (bg not uniform?)"
    return rgba, note


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--dry", action="store_true", help="list the work, write nothing")
    a = ap.parse_args()

    if not os.path.isdir(RAW):
        os.makedirs(RAW, exist_ok=True)
        print(f"drop the web-app downloads in {os.path.relpath(RAW, ROOT)} and run again")
        return

    files = sorted(f for f in os.listdir(RAW) if os.path.isfile(os.path.join(RAW, f)))
    images = [f for f in files if os.path.splitext(f)[1].lower() in {".png", ".jpg", ".jpeg", ".webp"}]
    videos = [f for f in files if os.path.splitext(f)[1].lower() in VIDEO_EXT]
    if not images and not videos:
        print(f"nothing in {os.path.relpath(RAW, ROOT)} yet")
        return

    for f in images:
        convert(os.path.join(RAW, f), a.dry)
    for f in videos:
        print(f"{f:16s} motion reference only - never shipped, nothing to convert")
    missing = sorted(KNOWN - {target_name(f) for f in images})
    if missing:
        print("still missing:", ", ".join(missing))


if __name__ == "__main__":
    main()
