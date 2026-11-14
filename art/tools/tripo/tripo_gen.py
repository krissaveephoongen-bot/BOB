"""BOB x Tripo: create one AI prop through the Tripo API (21_AA_Art_Bible.md section 9.3, route B).

Auth: never pass the key on the command line or store it in the repo.
  * Cloud session: add a Network secret (host api.tripo3d.ai, header Authorization,
    value "Bearer <key>"); the proxy adds the header and this script sends none.
  * Local machine: set the environment variable TRIPO_API_KEY.

Usage (stdlib only, Python 3.9+):
  python art/tools/tripo/tripo_gen.py --spec art/tools/tripo/specs/prop_bucket.json --out art/aa/props/raw
  python art/tools/tripo/tripo_gen.py --balance          # show credit balance only

Writes <out>/<name>_tripo.glb and <out>/<name>_tripo.json (task id, params, credits
before/after, model URL host). The raw .glb then goes through
  python art/tools/blender/bob_prop_cleanup.py <out>/<name>_tripo.glb --name <name> --class <class> --height <h>
"""
import argparse
import json
import mimetypes
import os
import sys
import time
import urllib.parse
import urllib.request
import uuid

API = "https://api.tripo3d.ai/v2/openapi"


def headers(extra=None):
    h = {"User-Agent": "bob-tripo/1.0"}
    key = os.environ.get("TRIPO_API_KEY", "").strip()
    if key:
        h["Authorization"] = "Bearer " + key
    if extra:
        h.update(extra)
    return h


def call(method, path, body=None, raw=None, ctype=None):
    data = raw
    extra = {}
    if body is not None:
        data = json.dumps(body).encode()
        extra["Content-Type"] = "application/json"
    if ctype:
        extra["Content-Type"] = ctype
    req = urllib.request.Request(API + path, data=data, method=method, headers=headers(extra))
    try:
        with urllib.request.urlopen(req, timeout=120) as r:
            res = json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        msg = e.read().decode(errors="replace")[:500]
        sys.exit(f"Tripo {method} {path} -> HTTP {e.code}: {msg}")
    if res.get("code", 0) != 0:
        sys.exit(f"Tripo {method} {path} -> {res}")
    return res.get("data", {})


def balance():
    d = call("GET", "/user/balance")
    return {"balance": d.get("balance"), "frozen": d.get("frozen")}


def upload(path):
    boundary = uuid.uuid4().hex
    mime = mimetypes.guess_type(path)[0] or "image/png"
    with open(path, "rb") as f:
        payload = f.read()
    if payload.startswith(b"version https://git-lfs"):
        sys.exit(f"{path} is a Git LFS pointer: run 'git lfs pull' first")
    body = (f"--{boundary}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"{os.path.basename(path)}\"\r\n"
            f"Content-Type: {mime}\r\n\r\n").encode() + payload + f"\r\n--{boundary}--\r\n".encode()
    d = call("POST", "/upload", raw=body, ctype=f"multipart/form-data; boundary={boundary}")
    return d["image_token"], ("jpg" if mime == "image/jpeg" else "png")


def wait(task_id, every=5, limit=900):
    t0 = time.time()
    while True:
        d = call("GET", f"/task/{task_id}")
        st = d.get("status")
        print(f"  {int(time.time() - t0):4d}s  {st}  {d.get('progress', '')}", flush=True)
        if st == "success":
            return d
        if st in ("failed", "cancelled", "unknown", "banned", "expired"):
            sys.exit(f"task {task_id} ended: {st}")
        if time.time() - t0 > limit:
            sys.exit(f"task {task_id} still {st} after {limit}s")
        time.sleep(every)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--spec", help="JSON spec (see specs/prop_bucket.json)")
    ap.add_argument("--out", default="art/aa/props/raw")
    ap.add_argument("--balance", action="store_true")
    a = ap.parse_args()

    if a.balance or not a.spec:
        print(json.dumps(balance()))
        return

    spec = json.load(open(a.spec, encoding="utf-8"))
    os.makedirs(a.out, exist_ok=True)
    before = balance()
    print("credits before:", before)

    task = dict(spec["tripo"])
    ref = spec.get("reference_image")
    if ref:
        token, ftype = upload(ref)
        task["type"] = "image_to_model"
        task["file"] = {"type": ftype, "file_token": token}
    else:
        task["type"] = "text_to_model"
        task["prompt"] = spec["prompt"]
        if spec.get("negative_prompt"):
            task["negative_prompt"] = spec["negative_prompt"]

    tid = call("POST", "/task", body=task)["task_id"]
    print("task:", tid)
    d = wait(tid)
    out = d.get("output", {})
    url = out.get("pbr_model") or out.get("model") or out.get("base_model")
    if not url:
        sys.exit(f"no model URL in output keys {list(out)}")
    glb = os.path.join(a.out, f"{spec['name']}_tripo.glb")
    try:
        urllib.request.urlretrieve(url, glb)
    except Exception as e:  # download host may need to be allowed in the cloud network policy
        sys.exit(f"download failed from host {urllib.parse.urlparse(url).hostname}: {e}")
    if out.get("rendered_image"):
        try:
            urllib.request.urlretrieve(out["rendered_image"], os.path.join(a.out, f"{spec['name']}_tripo_render.webp"))
        except Exception:
            pass
    after = balance()
    log = {
        "name": spec["name"], "task_id": tid, "task": {k: v for k, v in task.items() if k != "file"},
        "reference_image": ref, "credits_before": before, "credits_after": after,
        "credits_used": (before["balance"] - after["balance"]) if isinstance(before.get("balance"), (int, float)) and isinstance(after.get("balance"), (int, float)) else None,
        "model_host": urllib.parse.urlparse(url).hostname, "created": time.strftime("%Y-%m-%d %H:%M:%S"),
    }
    json.dump(log, open(os.path.join(a.out, f"{spec['name']}_tripo.json"), "w", encoding="utf-8"), indent=1, ensure_ascii=False)
    print("saved", glb, "credits used:", log["credits_used"])


if __name__ == "__main__":
    main()
