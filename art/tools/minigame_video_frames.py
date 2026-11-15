#!/usr/bin/env python3
"""Turn the owner's Among Us gameplay clips into stills we can read motion timing from.

There is no ffmpeg on this machine, so frames come out of OpenCV. For every clip in
art/incoming/minigames/videos/ this writes:

  frames/<clip>/f000.jpg ...   evenly spaced stills (default 12, or one every --every seconds)
  frames/<clip>_sheet.jpg      those stills on one contact sheet, labelled with their timecode

The stills are references only - nothing is imported into Unity. Timing work (how long a wire
drag takes, how fast the keypad cursor steps) reads the per-frame times printed here.

Usage:  /c/Python313/python art/tools/minigame_video_frames.py
        /c/Python313/python art/tools/minigame_video_frames.py --every 0.25
        /c/Python313/python art/tools/minigame_video_frames.py wire keypad     # name filter
"""
import glob
import os
import sys

import cv2
import numpy as np

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "incoming", "minigames", "videos")
OUT = os.path.join(SRC, "frames")

VIDEO_EXT = (".mp4", ".webm", ".mov", ".m4v", ".avi")
TILE_W = 480
COLS = 4


def label(img, text):
    bar = max(18, img.shape[0] // 14)
    cv2.rectangle(img, (0, 0), (img.shape[1], bar), (24, 24, 28), -1)
    cv2.putText(img, text, (6, bar - 5), cv2.FONT_HERSHEY_SIMPLEX, 0.45, (255, 255, 255), 1, cv2.LINE_AA)
    return img


def sample_times(fps, frames, every):
    duration = frames / fps if fps > 0 else 0.0
    if every and every > 0:
        n = max(1, int(duration / every))
        return [min(i * every, max(0.0, duration - 1.0 / max(fps, 1.0))) for i in range(n + 1)]
    n = 12
    return [duration * (i + 0.5) / n for i in range(n)]


def sheet(stills, dst):
    tiles = []
    for t, img in stills:
        k = TILE_W / img.shape[1]
        img = cv2.resize(img, (TILE_W, max(1, int(img.shape[0] * k))), interpolation=cv2.INTER_AREA)
        tiles.append(label(img, f"{t:5.2f}s"))
    if not tiles:
        return
    rows = (len(tiles) + COLS - 1) // COLS
    h = max(t.shape[0] for t in tiles)
    w = TILE_W
    canvas = np.full((rows * h, COLS * w, 3), 18, np.uint8)
    for i, t in enumerate(tiles):
        r, c = divmod(i, COLS)
        canvas[r * h:r * h + t.shape[0], c * w:c * w + t.shape[1]] = t
    cv2.imwrite(dst, canvas, [cv2.IMWRITE_JPEG_QUALITY, 92])


def run(path, every):
    cap = cv2.VideoCapture(path)
    if not cap.isOpened():
        print(f"{os.path.basename(path):28s} cannot open")
        return None
    fps = cap.get(cv2.CAP_PROP_FPS) or 30.0
    frames = int(cap.get(cv2.CAP_PROP_FRAME_COUNT) or 0)
    w = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH) or 0)
    h = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT) or 0)
    times = sample_times(fps, frames, every)

    stem = os.path.splitext(os.path.basename(path))[0]
    folder = os.path.join(OUT, stem)
    os.makedirs(folder, exist_ok=True)
    kept = []
    for i, t in enumerate(times):
        cap.set(cv2.CAP_PROP_POS_MSEC, t * 1000.0)
        ok, img = cap.read()
        if not ok:
            continue
        dst = os.path.join(folder, f"f{i:03d}.jpg")
        cv2.imwrite(dst, img, [cv2.IMWRITE_JPEG_QUALITY, 90])
        kept.append((t, img))
    cap.release()

    sheet(kept, os.path.join(OUT, stem + "_sheet.jpg"))
    dur = frames / fps if fps else 0.0
    print(f"{stem:28s} {w}x{h}  {fps:5.2f} fps  {dur:6.2f} s  -> {len(kept)} stills")
    return stem, kept


def main():
    mkv = [a for a in sys.argv[1:] if not a.startswith("--")]
    every = 0.0
    for i, a in enumerate(sys.argv[1:]):
        if a == "--every" and i + 2 <= len(sys.argv) - 1:
            every = float(sys.argv[i + 2])

    files = [p for p in sorted(glob.glob(os.path.join(SRC, "*"))) if p.lower().endswith(VIDEO_EXT)]
    if mkv:
        files = [p for p in files if any(k.lower() in os.path.basename(p).lower() for k in mkv)]
    if not files:
        print(f"no clips in {SRC}")
        print("drop them as  <minigame>.mp4  (wire, keypad, pour, digits, trash, sort, align, key, path)")
        return

    os.makedirs(OUT, exist_ok=True)
    for p in files:
        run(p, every)
    print(f"contact sheets in {OUT}")


if __name__ == "__main__":
    main()
