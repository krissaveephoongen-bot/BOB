#!/usr/bin/env bash
# Play-readiness gate (see check.py). Same style as unity-typecheck/check.sh: prints one PASSED/FAILED line.
set -u
here="$(cd "$(dirname "$0")" && pwd)"
exec python3 "$here/check.py" "$@"
