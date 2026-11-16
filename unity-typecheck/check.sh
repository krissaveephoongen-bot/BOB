#!/usr/bin/env bash
# Type-checks every Unity script without the Editor and without a Unity licence.
#
# The public reference assemblies are Unity 2021.3/2018.1, so Unity 6-only APIs
# (FindObjectsInactive overloads, EditorApplication.EnterPlaymode, PrefabUtility
# SaveAsPrefabAsset, ModelImporter.materialImportMode, ...) cannot resolve here.
# baseline-errors.txt lists those known gaps per file + error kind. The gate is:
#   - no NEW error kind appears
#   - the build must not fail for a non-C# reason
# When a known gap is removed, the script prints a note and still passes; run it
# with `--update` to rewrite the baseline after an intentional change.
#
# Run from anywhere: bash unity-typecheck/check.sh
set -uo pipefail
cd "$(dirname "$0")" || exit 2

BASE=baseline-errors.txt
LOG=build.log
NORM=build-errors.txt

dotnet build BobTypeCheck.csproj -nologo -v q >"$LOG" 2>&1

grep -E "error CS[0-9]+" "$LOG" \
  | sed -E 's#\\#/#g; s#^.*/unity/Assets/#unity/Assets/#; s#\([0-9]+,[0-9]+\)##; s# \[.*$##' \
  | sort -u >"$NORM"

if [ "${1:-}" = "--update" ]; then
  cp "$NORM" "$BASE"
  echo "typecheck baseline updated: $(wc -l <"$BASE") known kind(s)"
  exit 0
fi

if grep -qE "error (MSB|NU)[0-9]+" "$LOG"; then
  echo "FAIL: the typecheck project did not build; see $LOG"
  grep -E "error (MSB|NU)[0-9]+" "$LOG" | head -10
  exit 1
fi

if [ ! -s "$NORM" ] && [ -s "$BASE" ]; then
  echo "FAIL: the build produced no C# errors at all but the project did not pass; see $LOG"
  tail -20 "$LOG"
  exit 1
fi

new=$(comm -13 "$BASE" "$NORM")
fixed=$(comm -23 "$BASE" "$NORM")

if [ -n "$fixed" ]; then
  echo "note: known gap(s) no longer present — update the baseline if intentional:"
  echo "$fixed" | sed 's/^/  - /'
fi

if [ -n "$new" ]; then
  echo "FAIL: new C# error kind(s) not in $BASE:"
  echo "$new" | sed 's/^/  + /'
  echo "full log: $(pwd)/$LOG"
  exit 1
fi

echo "typecheck ok: $(wc -l <"$BASE") known Unity-6 gap kind(s), no new errors"
