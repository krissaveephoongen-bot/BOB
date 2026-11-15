#!/usr/bin/env bash
# Build the APK and upload it to Firebase App Distribution (project bob-preprod).
#   bash distribute-android.sh              # bump AndroidBundleVersionCode, build, upload
#   bash distribute-android.sh --no-bump    # rebuild + upload at the current versionCode
#   bash distribute-android.sh --upload-only # upload the APK already in Builds/ (e.g. after a login fix)
#   FIREBASE_GROUPS=internal-testers bash distribute-android.sh   # also release to a tester group
#
# One-time: npm i -g firebase-tools && firebase login
# (CI instead: export GOOGLE_APPLICATION_CREDENTIALS=/path/to/service-account.json)
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNITY="${UNITY:-C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe}"
PROJECT="$REPO/unity"
PS="$PROJECT/ProjectSettings/ProjectSettings.asset"
APK="$REPO/Builds/BOB-prototype-v1.apk"
APP_ID="${FIREBASE_APP_ID:-1:438462393037:android:76c94b4645660e52e69784}"
GROUPS="${FIREBASE_GROUPS:-}"
BUMP=1
[ "${1:-}" = "--no-bump" ] && BUMP=0
UPLOAD_ONLY=0
[ "${1:-}" = "--upload-only" ] && UPLOAD_ONLY=1

say() { printf '\n== %s\n' "$*"; }
die() { printf '\n!! %s\n' "$*" >&2; exit 1; }

[ -f "$UNITY" ] || die "Unity not found at $UNITY (set UNITY=/path/to/Unity.exe)"
[ -f "$PS" ] || die "missing $PS"

if [ "$UPLOAD_ONLY" = 1 ]; then
  NEXT="$(grep -m1 '^  AndroidBundleVersionCode:' "$PS" | tr -dc '0-9')"
  [ -f "$APK" ] || die "nothing to upload: $APK is missing (run without --upload-only)"
  say "uploading the existing APK at versionCode $NEXT"
else
  # An open Editor holds the project; a batch build would fail half-way and leave the Editor on Android.
  if [ -f "$PROJECT/Temp/UnityLockfile" ]; then
    die "Unity has the project open - close the Editor first (Temp/UnityLockfile exists)"
  fi

  if [ "$BUMP" = 1 ]; then
    CODE="$(grep -m1 '^  AndroidBundleVersionCode:' "$PS" | tr -dc '0-9')"
    [ -n "$CODE" ] || die "AndroidBundleVersionCode not found in $PS"
    NEXT=$((CODE + 1))
    # Replace the digits only, so the file keeps its CRLF line endings.
    sed -i "s/^  AndroidBundleVersionCode: [0-9]*/  AndroidBundleVersionCode: $NEXT/" "$PS"
    say "versionCode $CODE -> $NEXT"
  else
    NEXT="$(grep -m1 '^  AndroidBundleVersionCode:' "$PS" | tr -dc '0-9')"
    say "versionCode kept at $NEXT"
  fi

  say "building $APK"
  "$UNITY" -batchmode -quit -projectPath "$PROJECT" \
    -executeMethod Bob.EditorTools.BobAndroidBuild.BuildApk \
    -logFile "$PROJECT/Logs/apk-build-$NEXT.log" \
    || die "Unity exited non-zero - see unity/Logs/apk-build-$NEXT.log"
  [ -f "$APK" ] || die "build reported success but $APK is missing"
fi

command -v firebase >/dev/null || die "firebase CLI missing: npm i -g firebase-tools && firebase login"

NOTES="$(git -C "$REPO" log -1 --pretty='%h %s')"
ARGS=(appdistribution:distribute "$APK" --app "$APP_ID" --project bob-preprod
      --release-notes "v0.1.0 ($NEXT) $NOTES")
[ -n "$GROUPS" ] && ARGS+=(--groups "$GROUPS")
say "uploading to Firebase App Distribution ($APP_ID)"
firebase "${ARGS[@]}"

say "done - versionCode $NEXT is live in bob-preprod"
