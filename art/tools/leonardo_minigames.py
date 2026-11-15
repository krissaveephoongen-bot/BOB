#!/usr/bin/env python3
"""Generate the S28 minigame pack with the Leonardo REST API (docs/25_Minigame_Leonardo_Pack.md).

Three groups, because they land in three different places:
  assets  15 transparent prop cutouts -> Unity Resources/Textures/Minigame (used by the card itself)
  refs     9 scene stills              -> art/incoming/minigames/refs (art direction only)
  videos   4 motion clips              -> art/incoming/minigames/videos (motion reference only, never shipped)

Usage:  python3 tools/leonardo_minigames.py dry-run
        python3 tools/leonardo_minigames.py prompts            # paste-ready text for the web app
        python3 tools/leonardo_minigames.py assets --n 2
        python3 tools/leonardo_minigames.py refs
        python3 tools/leonardo_minigames.py videos --only V2
        python3 tools/leonardo_minigames.py pull --id <generationId>   # whatever the web app already made
Auth:   LEONARDO_API_KEY in the environment. Nothing is written outside art/.
        Without credits, `prompts` + the web app + `minigame_pack_to_unity.py` cover the whole pack.
"""
import argparse
import json
import os
import sys
import time
import urllib.request

API = "https://cloud.leonardo.ai/api/rest/v1"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
OUT = os.path.join(ROOT, "incoming", "minigames")
MODEL = os.environ.get("LEONARDO_MODEL", "de7d3faf-762f-48e0-b3b7-9d0ac3a3fcf3")  # Phoenix 1.0
VIDEO_RES = os.environ.get("LEONARDO_VIDEO_RES", "RESOLUTION_720")

STYLE = ("BOB Battle of the Broom mobile game art, 2.5D stylized cartoon, soft 3D volume like a vinyl toy, "
         "smooth matte plastic, hand-painted gradients, thin dark brown outline, warm key light from upper left, "
         "soft light blue fill, gentle ambient occlusion, bright clean colours")
NEG_PROP = ("realistic, photo, photorealistic, text, letters, digits, words, watermark, signature, logo, frame, "
            "border, background scenery, ground shadow, cropped, cut off, blurry, noisy, thick black outline, "
            "anime style, multiple objects, cluttered")
NEG_SCENE = ("realistic, photo, text, letters, words, watermark, logo, frame, border, people, children, "
             "characters, hands, blurry, noisy, thick black outline, anime style")

# name: (prompt, negative). Every prop is blank on purpose: the card draws all text and digits with TMP,
# so an AI banner or a hallucinated "A1" can never end up in the game.
ASSETS = {
    "mini_bin_blue": ("a single open-top recycling bin, bright blue plastic, rounded edges, a blank front label "
                      "plate, empty inside, front view", NEG_PROP),
    "mini_bin_grey": ("a single open-top rubbish bin, grey plastic, rounded edges, a blank front label plate, "
                      "empty inside, front view", NEG_PROP),
    "mini_bin_yellow": ("a single open-top paper recycling bin, warm yellow plastic, rounded edges, a blank front "
                        "label plate, empty inside, front view", NEG_PROP),
    "mini_keypad_panel": ("a blank cream control panel, rounded square, a dark glass display window across the top "
                          "and a three by four grid of empty recessed square key wells, no symbols at all, "
                          "front view", NEG_PROP),
    "mini_key_square": ("one blank ivory square key cap with rounded corners, shallow dish, soft top light, "
                        "front view", NEG_PROP),
    "mini_slip_paper": ("a small torn paper slip with faint blank ruled lines and a bent paperclip, empty, "
                        "three quarter view", NEG_PROP),
    "mini_dial_base": ("a round brass dial face with raised tick marks all around the rim and no numbers in the "
                       "middle, seen from directly above", NEG_PROP),
    "mini_dial_needle": ("a single red enamel pointer needle with a round brass hub, pointing straight up", NEG_PROP),
    "mini_cloth_wet": ("a folded damp cloth, pale blue with a wet sheen and a darker wet patch, three quarter view",
                       NEG_PROP),
    "mini_bottle_pour": ("a cream detergent bottle with a blue cap tilted as if pouring, one small drop leaving the "
                         "lip, three quarter view", NEG_PROP),
    "mini_basin": ("a small round plastic basin, sky blue, empty, three quarter view", NEG_PROP),
    "mini_stain_red": ("a soft round red stain smear with a ragged semi transparent edge, flat, seen from above",
                       NEG_PROP),
    "mini_stain_grey": ("a soft round grey dust stain smear with a ragged semi transparent edge, flat, seen from "
                        "above", NEG_PROP),
    "mini_node_mark": ("a round brass floor marker disc with a raised ring and a small dome, seen from directly "
                       "above", NEG_PROP),
    "mini_trail_dust": ("a dashed trail of dust, crumbs and small leaves, curving in a loose S shape, seen from "
                        "directly above", NEG_PROP),
}

# One still per Among Us archetype, restyled to our tasks. These are for the eye, not for the import.
REFS = {
    "ref_wire_trail": ("a wooden village floor seen from a high three quarter angle, a long dashed dusty trail "
                       "zig-zagging across it between round brass floor marks, a wooden broom with golden bristles "
                       "resting at the first mark, wide landscape view", NEG_SCENE),
    "ref_sort_bins": ("three open bins in a row on a wooden floor, blue, grey and yellow, each with a blank front "
                      "label plate, a scatter of small junk cards floating above them waiting to be sorted, "
                      "three quarter view, wide landscape view", NEG_SCENE),
    "ref_wring_insert": ("an old wooden mangle wringer with a turned handle, a damp cloth being fed into the roller "
                         "slot, a dashed arrow showing the roller turning, on a wooden table, wide landscape view",
                         NEG_SCENE),
    "ref_dial_align": ("a round brass dial with a red needle and a raised dashed target notch on the rim, the needle "
                       "just short of the notch, seen from directly above on a wooden table, wide landscape view",
                       NEG_SCENE),
    "ref_soak_select": ("a washing basin on a tiled floor with a detergent bottle pouring into it, a gauge on the "
                        "bottle nearly empty, and three stains on the floor ahead, one glow red and two dull grey, "
                        "wide landscape view", NEG_SCENE),
    "ref_fill_band": ("a tall detergent bottle with a vertical transparent gauge and a green target band, blue "
                      "liquid rising inside, on a wooden counter, wide landscape view", NEG_SCENE),
    "ref_keypad": ("a washing machine control panel with a dark display, a blank paper slip clipped beside it and "
                   "twelve large square keys, the last one green, on a tiled wall, wide landscape view", NEG_SCENE),
    "ref_washer_pads": ("the front of a washing machine with four big round buttons in blue, green, yellow and "
                        "orange, each a different shape, on a tiled wall, wide landscape view", NEG_SCENE),
    "ref_mop_path": ("a wet tiled floor with a foamy jagged dashed path from the top of the frame to the bottom, a "
                     "mop resting at the start with soap bubbles, high three quarter view, wide landscape view",
                     NEG_SCENE),
}

# Motion clips. Each one is generated from its anchor still, so they show the exact motion the card should feel like.
VIDEOS = {
    "V1_trail": ("ref_wire_trail", "the broom slides along the dashed trail from the first floor mark to the last, "
                                   "dust clearing behind it, then the trail flashes red and resets to dust"),
    "V2_sort": ("ref_sort_bins", "one junk card lifts from the scatter and flies into the middle bin, the bin pops "
                                 "like a soft toy and a gold star bursts open"),
    "V3_keypad": ("ref_keypad", "a finger presses one square key, the key sinks, a digit appears on the dark display, "
                                "then the green key lights up"),
    "V4_fill": ("ref_fill_band", "blue liquid rises smoothly inside the gauge, reaches the green band, the band "
                                 "blinks softly and a small splash pops at the rim"),
}


def req(method, path, body=None):
    url = path if path.startswith("http") else API + path
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(url, data=data, method=method)
    r.add_header("accept", "application/json")
    if data is not None:
        r.add_header("content-type", "application/json")
    key = os.environ.get("LEONARDO_API_KEY")
    if not key:
        sys.exit("LEONARDO_API_KEY is not set. Export it for one command: LEONARDO_API_KEY=... python3 tools/leonardo_minigames.py ...")
    r.add_header("authorization", "Bearer " + key)
    try:
        with urllib.request.urlopen(r, timeout=120) as f:
            return json.loads(f.read() or b"{}")
    except urllib.error.HTTPError as e:
        sys.exit(f"HTTP {e.code} {path}: {e.read().decode()[:600]}")


def ids_path():
    return os.path.join(OUT, "_ids.json")


def load_ids():
    try:
        with open(ids_path(), encoding="utf-8") as f:
            return json.load(f)
    except FileNotFoundError:
        return {}


def save_ids(ids):
    os.makedirs(OUT, exist_ok=True)
    with open(ids_path(), "w", encoding="utf-8") as f:
        json.dump(ids, f, indent=2)


def poll(generation_id, key="motionMP4URL", tries=150):
    for _ in range(tries):
        time.sleep(4)
        g = req("GET", f"/generations/{generation_id}")["generations_by_pk"]
        if g["status"] == "COMPLETE":
            return g
        if g["status"] == "FAILED":
            sys.exit(f"generation failed: {generation_id}")
    sys.exit(f"timeout: {generation_id}")


def download(url, dst):
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    urllib.request.urlretrieve(url, dst)
    return dst


def generate_image(name, prompt, negative, save_to, n, transparent):
    w = h = 1024
    body = {"modelId": MODEL, "prompt": prompt + ", " + STYLE, "negative_prompt": negative,
            "width": w, "height": h, "num_images": n, "alchemy": True, "contrast": 3.5, "enhancePrompt": False}
    if transparent:
        body["transparency"] = "foreground_only"
    job = req("POST", "/generations", body)["sdGenerationJob"]["generationId"]
    g = poll(job)
    saved = []
    for i, img in enumerate(g["generated_images"]):
        ext = ".png" if transparent else ".jpg"
        file = name + ext if i == 0 else f"{name}_v{i + 1}{ext}"
        saved.append(download(img["url"], os.path.join(OUT, save_to, file)))
        if i == 0:
            ids = load_ids()
            ids[name] = {"imageId": img["id"], "generationId": job}
            save_ids(ids)
    return saved


def generate_video(name, anchor, prompt):
    ids = load_ids()
    if anchor not in ids:
        sys.exit(f"anchor still {anchor} has no image id yet: run `refs` (or `refs --only {anchor}`) first")
    body = {
        "imageType": "GENERATED",
        "imageId": ids[anchor]["imageId"],
        "prompt": prompt + ", " + STYLE,
        "resolution": VIDEO_RES,
        "frameInterpolation": True,
    }
    res = req("POST", "/generations-image-to-video", body)
    job = (res.get("generationsByPk") or res.get("sdGenerationJob") or {}).get("id") or res.get("generationId")
    if not job:
        sys.exit(f"unexpected video response: {json.dumps(res)[:400]}")
    g = poll(job)
    images = g.get("generated_images") or []
    url = images[0].get("motionMP4URL") if images else None
    if not url:
        sys.exit(f"video finished without motionMP4URL: {json.dumps(images)[:400]}")
    return [download(url, os.path.join(OUT, "videos", name + ".mp4"))]


def print_prompts(only):
    """The same text the API run would send, so the web app produces the same pack by hand."""
    groups = [("assets", ASSETS), ("refs", REFS)]
    for group, table in groups:
        for name, (prompt, neg) in table.items():
            if only and name not in only:
                continue
            print(f"=== {name}  [{group}]")
            print(prompt + ", " + STYLE)
            print("-- negative --")
            print(neg)
            print()
    for name, (anchor, prompt) in VIDEOS.items():
        if only and name not in only:
            continue
        print(f"=== {name}  [videos -> image to motion from {anchor}]")
        print(prompt + ", " + STYLE)
        print()


def pull(generation_id):
    """Download an existing generation: the stills and the motion clip, when the API already holds one."""
    g = req("GET", f"/generations/{generation_id}")["generations_by_pk"]
    folder = os.path.join(OUT, "pull", generation_id)
    saved = []
    for i, img in enumerate(g.get("generated_images") or []):
        if img.get("url"):
            saved.append(download(img["url"], os.path.join(folder, f"{i:02d}.jpg")))
        if img.get("motionMP4URL"):
            saved.append(download(img["motionMP4URL"], os.path.join(folder, f"{i:02d}.mp4")))
    if not saved:
        sys.exit(f"generation {generation_id} has nothing to download (status {g.get('status')})")
    return saved


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("group", choices=["assets", "refs", "videos", "pull", "prompts", "dry-run"])
    ap.add_argument("--n", type=int, default=0, help="variants per prompt (default 2 assets, 1 refs)")
    ap.add_argument("--only", default="", help="one name, or a comma separated list")
    ap.add_argument("--id", default="", help="generation id for `pull`")
    a = ap.parse_args()
    only = [s for s in a.only.split(",") if s]

    if a.group == "prompts":
        print_prompts(only)
        return

    if a.group == "dry-run":
        n_assets = a.n or 2
        print(f"assets {len(ASSETS)} prompts x {n_assets} = {len(ASSETS) * n_assets} images")
        print(f"refs   {len(REFS)} prompts x 1 = {len(REFS)} images")
        print(f"videos {len(VIDEOS)} clips (needs the refs first for their image ids)")
        print(f"total  {len(ASSETS) * n_assets + len(REFS)} images + {len(VIDEOS)} clips")
        print("(Phoenix 1.0 / Fast is about 2-4 credits per image; check the account before a full run)")
        return

    if a.group == "pull":
        if not a.id:
            sys.exit("pull needs --id <generationId>, the uuid the Leonardo web app shows for the job")
        for p in pull(a.id):
            print("  saved", os.path.relpath(p, ROOT))
        return

    if a.group == "assets":
        n = a.n or 2
        for name, (prompt, neg) in ASSETS.items():
            if only and name not in only:
                continue
            print(name)
            for p in generate_image(name, prompt, neg, "assets", n, transparent=True):
                print("  saved", os.path.relpath(p, ROOT))
        return

    if a.group == "refs":
        n = a.n or 1
        for name, (prompt, neg) in REFS.items():
            if only and name not in only:
                continue
            print(name)
            for p in generate_image(name, prompt, neg, "refs", n, transparent=False):
                print("  saved", os.path.relpath(p, ROOT))
        return

    for name, (anchor, prompt) in VIDEOS.items():
        if only and name not in only:
            continue
        print(name, "from", anchor)
        for p in generate_video(name, anchor, prompt):
            print("  saved", os.path.relpath(p, ROOT))


if __name__ == "__main__":
    main()
