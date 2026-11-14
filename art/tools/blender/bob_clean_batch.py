"""Run bob_prop_cleanup.py over a list of models in one Blender session.

  blender -b -P art/tools/blender/bob_clean_batch.py -- <jobs.json> <out_dir>

jobs.json: [{"src": "raw/x.glb", "name": "prop_bed_single", "class": "room", "height": 0.55}, ...]
Each job is exactly the CLI form of bob_prop_cleanup (join, decimate, scale, pivot, palette snap),
so one Blender start-up covers the whole batch. Failures are collected and printed at the end.
"""
import json
import os
import sys
import traceback

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import bob_prop_cleanup as cleanup  # noqa: E402


def main():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else sys.argv[1:]
    jobs_path, out_dir = argv[0], argv[1]
    jobs = json.load(open(jobs_path))
    out_dir = os.path.abspath(out_dir)
    skipped, failed = [], []
    for i, j in enumerate(jobs, 1):
        if not os.path.exists(j["src"]):
            skipped.append((j["name"], "missing " + j["src"]))
            continue
        sys.argv = ["bob_prop_cleanup", "--", j["src"], "--name", j["name"], "--class", j.get("class", "prop_m"),
                    "--prefix", j.get("prefix", "SM_"), "--height", str(j["height"]),
                    "--predecimate", str(j.get("predecimate", 0)), "--tint", j.get("tint", "none"),
                    "--out", out_dir]
        try:
            print(f"[{i}/{len(jobs)}] {j['name']} <- {os.path.basename(j['src'])}", flush=True)
            cleanup.main()
        except SystemExit as e:
            failed.append((j["name"], str(e)))
        except Exception:
            failed.append((j["name"], traceback.format_exc().splitlines()[-1]))
        sys.stdout.flush()
    print(f"done: {len(jobs) - len(failed) - len(skipped)} ok, {len(skipped)} skipped, {len(failed)} failed")
    for n, why in skipped + failed:
        print("  FAIL", n, "-", why)


if __name__ == "__main__":
    main()
