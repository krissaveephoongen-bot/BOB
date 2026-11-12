#!/usr/bin/env python3
"""Generate BOB art with the Leonardo.ai REST API from docs/09_Leonardo_Full_Prompts.csv.

Usage:  python3 tools/leonardo_gen.py 1          # one entry by number
        python3 tools/leonardo_gen.py 2-14 --n 2 # a range, 2 variants each
Auth:   LEONARDO_API_KEY env var, or an Authorization header injected by the network proxy.
Output: incoming/<group>/<file> (variants: name_v2.png ...). Character/Style refs are read from the
        image_guidance column (paths relative to bob-art/); missing refs are skipped with a warning.
"""
import argparse, csv, json, os, sys, time, urllib.request, uuid

API = "https://cloud.leonardo.ai/api/rest/v1"
ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
MODEL = os.environ.get("LEONARDO_MODEL", "de7d3faf-762f-48e0-b3b7-9d0ac3a3fcf3")  # Phoenix 1.0
CHAR_REF = int(os.environ.get("LEONARDO_CHAR_PREPROC", "397"))   # Character Reference (Phoenix)
STYLE_REF = int(os.environ.get("LEONARDO_STYLE_PREPROC", "166"))  # Style Reference (Phoenix)
SIZES = {"1:1": (1024, 1024), "3:2": (1536, 1024)}


def req(method, path, body=None):
    url = path if path.startswith("http") else API + path
    data = json.dumps(body).encode() if body is not None else None
    r = urllib.request.Request(url, data=data, method=method)
    r.add_header("accept", "application/json")
    if data is not None:
        r.add_header("content-type", "application/json")
    key = os.environ.get("LEONARDO_API_KEY")
    if key:
        r.add_header("authorization", "Bearer " + key)
    try:
        with urllib.request.urlopen(r, timeout=120) as f:
            return json.loads(f.read() or b"{}")
    except urllib.error.HTTPError as e:
        sys.exit(f"HTTP {e.code} {path}: {e.read().decode()[:500]}")


_uploaded = {}
def upload(path):
    if path in _uploaded:
        return _uploaded[path]
    j = req("POST", "/init-image", {"extension": "png"})["uploadInitImage"]
    fields = json.loads(j["fields"])
    boundary = uuid.uuid4().hex
    parts = []
    for k, v in fields.items():
        parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="{k}"\r\n\r\n{v}\r\n'.encode())
    parts.append(f'--{boundary}\r\nContent-Disposition: form-data; name="file"; filename="ref.png"\r\n'
                 f'Content-Type: image/png\r\n\r\n'.encode() + open(path, "rb").read() + b"\r\n")
    parts.append(f"--{boundary}--\r\n".encode())
    r = urllib.request.Request(j["url"], data=b"".join(parts), method="POST")
    r.add_header("content-type", "multipart/form-data; boundary=" + boundary)
    urllib.request.urlopen(r, timeout=120).read()
    _uploaded[path] = j["id"]
    return j["id"]


def controlnets(guidance):
    out = []
    for item in guidance.split(";"):
        item = item.strip()
        if ":" not in item:
            continue
        kind, rest = item.split(":", 1)
        path = rest.split("(")[0].strip()
        strength = rest.split("(")[1].rstrip(") ").strip() if "(" in rest else "Mid"
        full = os.path.join(ROOT, path)
        if not os.path.exists(full):
            print(f"  ! ref not found, skipped: {path}")
            continue
        pre = CHAR_REF if "Character" in kind else STYLE_REF
        out.append({"initImageId": upload(full), "initImageType": "UPLOADED",
                    "preprocessorId": pre, "strengthType": strength})
    return out


def generate(row, n):
    ratio = "3:2" if "3:2" in row["settings"] else "1:1"
    w, h = SIZES[ratio]
    body = {"modelId": MODEL, "prompt": row["prompt"], "negative_prompt": row["negative_prompt"],
            "width": w, "height": h, "num_images": n, "alchemy": True, "contrast": 3.5,
            "enhancePrompt": False}
    if "Transparency On" in row["settings"]:
        body["transparency"] = "foreground_only"
    cn = controlnets(row["image_guidance"])
    if cn:
        body["controlnets"] = cn
    gid = req("POST", "/generations", body)["sdGenerationJob"]["generationId"]
    for _ in range(90):
        time.sleep(4)
        g = req("GET", f"/generations/{gid}")["generations_by_pk"]
        if g["status"] == "COMPLETE":
            break
        if g["status"] == "FAILED":
            sys.exit(f"generation failed: {row['file']}")
    else:
        sys.exit("timeout")
    outdir = os.path.join(ROOT, row["save_to"])
    os.makedirs(outdir, exist_ok=True)
    saved = []
    for i, img in enumerate(g["generated_images"]):
        name = row["file"] if i == 0 else row["file"].replace(".png", f"_v{i + 1}.png")
        dst = os.path.join(outdir, name)
        urllib.request.urlretrieve(img["url"], dst)
        saved.append(dst)
    return saved


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("which", help="number or range, e.g. 1 or 2-14")
    ap.add_argument("--n", type=int, default=1, help="variants per prompt")
    a = ap.parse_args()
    lo, _, hi = a.which.partition("-")
    lo, hi = int(lo), int(hi or lo)
    rows = list(csv.DictReader(open(os.path.join(ROOT, "docs/09_Leonardo_Full_Prompts.csv"), encoding="utf-8-sig")))
    for row in rows:
        if lo <= int(row["#"]) <= hi:
            print(f"#{row['#']} {row['file']}")
            for p in generate(row, a.n):
                print("  saved", os.path.relpath(p, ROOT))


if __name__ == "__main__":
    main()
