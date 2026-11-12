#!/usr/bin/env bash
# Regenerates every placeholder asset and the viewer data. Run from the bob-art folder.
#   needs: python3 + numpy, node + playwright (chromium)
set -euo pipefail
cd "$(dirname "$0")/.."
python3 tools/make_art.py
node tools/render_png.js
python3 tools/make_sfx.py
python3 - <<'PY'
import json
art = json.load(open("unity/Assets/Art/bob-art-manifest.json"))
aud = json.load(open("unity/Assets/Audio/bob-audio-list.json"))
open("prototype/data.js", "w").write("window.BOB_ART=" + json.dumps(art) + ";\nwindow.BOB_AUDIO=" + json.dumps(aud) + ";\n")
print("prototype/data.js written")
PY
