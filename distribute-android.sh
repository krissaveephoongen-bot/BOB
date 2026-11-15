#!/usr/bin/env bash
# Build the APK and upload it to Firebase App Distribution (project bob-preprod).
#   bash distribute-android.sh              # bump AndroidBundleVersionCode, build, upload
#   bash distribute-android.sh --no-bump    # rebuild + upload at the current versionCode
#   bash distribute-android.sh --upload-only # upload the APK already in Builds/ (e.g. after a login fix)
#   FIREBASE_GROUPS=internal-testers bash distribute-android.sh   # also release to a tester group
#
# Upload path: a service account key at FIREBASE_KEY_PATH (default ./firebase-key.json, gitignored) is uploaded
# through the node:20-alpine image, so no Firebase tooling is installed on this machine. Without the key the
# script falls back to a logged-in local firebase CLI; with neither it stops after the build with a clear message.
# One-time for the Docker path: enable App Distribution API, create the service account key in bob-preprod.
set -euo pipefail

REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
UNITY="${UNITY:-C:/Program Files/Unity/Hub/Editor/6000.6.8f1/Editor/Unity.exe}"
PROJECT="$REPO/unity"
PS="$PROJECT/ProjectSettings/ProjectSettings.asset"
APK="$REPO/Builds/BOB-prototype-v1.apk"

# .env holds no secrets: app id, tester group and the path of the gitignored key.
if [ -f "$REPO/.env" ]; then set -a; . "$REPO/.env"; set +a; fi
APP_ID="${FIREBASE_APP_ID:-1:438462393037:android:76c94b4645660e52e69784}"
GROUPS="${FIREBASE_GROUPS:-${FIREBASE_TESTER_GROUP:-}}"
KEY="${FIREBASE_KEY_PATH:-./firebase-key.json}"
case "$KEY" in /*|[A-Za-z]:*) ;; *) KEY="$REPO/${KEY#./}" ;; esac
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

NOTES="$(git -C "$REPO" log -1 --pretty='%h %s')"
APK_REL="${APK#$REPO/}"
KEY_REL="${KEY#$REPO/}"

if [ -z "$APP_ID" ]; then
  die "FIREBASE_APP_ID is empty - put the bob-preprod android app id in .env"
fi

if [ -f "$KEY" ]; then
  command -v docker >/dev/null || die "service account key found but docker is missing (install Docker Desktop)"
  say "uploading to Firebase App Distribution ($APP_ID) from node:20-alpine"
  # The release notes travel as a positional parameter so quoting survives the container shell.
  docker run --rm \
    -v "$REPO:/work" -w /work \
    -e GOOGLE_APPLICATION_CREDENTIALS="/work/$KEY_REL" \
    node:20-alpine sh -c \
    'npx -y firebase-tools appdistribution:distribute "$1" --app "$2" --project bob-preprod --release-notes "$3" ${4:+--groups "$4"}' \
    _ "$APK_REL" "$APP_ID" "v0.1.0 ($NEXT) $NOTES" "$GROUPS"
elif command -v firebase >/dev/null; then
  say "no service account key - uploading with the logged-in firebase CLI"
  ARGS=(appdistribution:distribute "$APK" --app "$APP_ID" --project bob-preprod
        --release-notes "v0.1.0 ($NEXT) $NOTES")
  [ -n "$GROUPS" ] && ARGS+=(--groups "$GROUPS")
  firebase "${ARGS[@]}"
else
  say "SKIPPED the upload: no $KEY and no firebase CLI"
  say "APK is ready at $APK (versionCode $NEXT)"
  say "to upload: create the bob-preprod service account key at $KEY, or npm i -g firebase-tools && firebase login"
  exit 0
fi

say "done - versionCode $NEXT is live in bob-preprod"
