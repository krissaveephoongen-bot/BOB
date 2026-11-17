#!/usr/bin/env python3
"""Play-readiness gate for the BOB repo: the checks that catch a build which cannot be played.

Unity is not available in the CI sandbox, so this script never opens the editor. It reads the same files the editor
would: scene YAML, .meta GUIDs, Resources folders, the art manifest, the audio prefab and ProjectSettings. Everything
it needs is in the repository, so a failure here is a real failure before anyone presses Play.

Checks: build scenes + GUIDs; LoadScene scene names; Canvas <-> EventSystem per scene; Resources folders and fonts;
every Resources.Load literal; every script GUID in scenes/prefabs (with a known-external baseline); duplicate .meta
GUIDs; the shipping PlayerSettings; the sprite names the code asks for; the audio clip names. LFS state is a note.

    python3 ready-check/check.py          # or: bash ready-check/check.sh

Exit code 0 = ready, 1 = at least one blocker, 2 = the script could not find the repo.
"""
import glob
import json
import os
import re
import sys

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(REPO, "unity", "Assets")
scene_dir = os.path.join(ASSETS, "Scenes")

blockers = []
notes = []


def fail(msg):
    blockers.append(msg)


def note(msg):
    notes.append(msg)


def read(path):
    with open(path, encoding="utf-8", errors="ignore") as f:
        return f.read()


def guids():
    """GUID -> asset path for every .meta in the project (package assets are not in the repo)."""
    out = {}
    for meta in glob.glob(os.path.join(ASSETS, "**", "*.meta"), recursive=True):
        m = re.search(r"guid: ([0-9a-f]{32})", read(meta)[:400])
        if m:
            out[m.group(1)] = meta[: -len(".meta")]
    return out


def check_build_scenes():
    """Every scene in Build Settings must exist, and its GUID must still match the file."""
    settings = read(os.path.join(REPO, "unity", "ProjectSettings", "EditorBuildSettings.asset"))
    entries = re.findall(r"path: (\S+)\n\s+guid: ([0-9a-f]{32})", settings)
    if not entries:
        fail("EditorBuildSettings lists no scenes at all")
        return []
    names = []
    for path, guid in entries:
        full = os.path.join(REPO, "unity", path)
        if not os.path.exists(full):
            fail("build scene missing on disk: " + path)
            continue
        meta = full + ".meta"
        if not os.path.exists(meta):
            fail("build scene has no .meta (Unity will re-GUID it): " + path)
            continue
        real = re.search(r"guid: ([0-9a-f]{32})", read(meta)[:400])
        if not real or real.group(1) != guid:
            fail("build scene GUID does not match its .meta: " + path)
        names.append(os.path.splitext(os.path.basename(path))[0])
    return names


def check_scene_loads(names):
    """LoadScene("X") and CanStreamedLevelBeLoaded("X") must name a scene that is in Build Settings."""
    want = set()
    for f in glob.glob(os.path.join(ASSETS, "Scripts", "**", "*.cs"), recursive=True):
        src = read(f)
        want |= set(re.findall(r'LoadScene\("([A-Za-z0-9_]+)"', src))
        want |= set(re.findall(r'CanStreamedLevelBeLoaded\("([A-Za-z0-9_]+)"', src))
    for scene in sorted(want - set(names)):
        fail(f'code loads scene "{scene}" but it is not in Build Settings')
    return want


def check_event_system(names):
    """A scene with a Canvas needs an EventSystem, or every uGUI pointer event is dead in a build.

    Each scene names its own creator: the lobby builds one at runtime inside the Game scene (BobGame.World) and the
    Task Race scenes build one in TaskRaceModeManager.EnsureEventSystem. The pairing is per scene on purpose - a
    creator that lives in the other scene does not load with this one, and an earlier version of this check accepted
    either file, so it passed while the Task Race scenes had no EventSystem at all.
    """
    creators = {}
    for f in glob.glob(os.path.join(ASSETS, "Scripts", "**", "*.cs"), recursive=True):
        src = read(f)
        if "EnsureEventSystem" in src or "typeof(EventSystem)" in src:
            creators[os.path.basename(f)] = True
    required = {"Game": "BobGame.World.cs"}
    for scene in names:
        path = os.path.join(scene_dir, scene + ".unity")
        if not os.path.exists(path):
            continue
        src = read(path)
        has_canvas = "Canvas:" in src or "m_RenderMode: 0" in src
        has_system = "EventSystem" in src or "InputModule" in src
        if not has_canvas or has_system:
            continue
        if scene.startswith("TaskRace"):
            owner = "TaskRaceModeManager.cs"
        elif scene in required:
            owner = required[scene]
        else:
            fail(f"{scene} has a Canvas but no EventSystem")
            continue
        if owner not in creators:
            fail(f"{scene} has a Canvas, no EventSystem in the scene, and {owner} does not build one")



def check_resources():
    """The assets that are loaded by name at runtime have to exist, or the screen plays empty."""
    for folder, kind, minimum in (
        ("Chapters", "ChapterDefinition assets", 6),
        ("Stages", "StageConfig assets", 6),
    ):
        found = [p for p in glob.glob(os.path.join(ASSETS, "Resources", folder, "*.asset"))]
        if len(found) < minimum:
            fail(f"Resources/{folder} holds {len(found)} asset(s), expected {minimum} ({kind})")
        for p in found:
            if not os.path.exists(p + ".meta"):
                fail(f"Resources asset has no .meta: {os.path.relpath(p, REPO)}")
    for path, what in (
        ("Resources/Audio/BobAudio.prefab", "audio prefab (BobSfx boots from it)"),
        ("Resources/Fonts/TMP/IBMPlexSansThai-Medium SDF.asset", "Thai font asset"),
        ("Resources/Fonts/TMP/Mitr-Medium SDF.asset", "display font asset"),
    ):
        if not os.path.exists(os.path.join(ASSETS, path)):
            fail(f"missing {what}: unity/Assets/{path}")


def check_art_names():
    """Every literal sprite name the code asks the art library for must be in the manifest.

    Names built by concatenation ("sweep" + "_idle") are skipped: only complete, underscore-only literals are checked,
    which is what the audit found to be the source of real typos.
    """
    manifest = json.load(open(os.path.join(ASSETS, "Art", "bob-art-manifest.json"), encoding="utf-8"))
    names = {os.path.basename(s["path"]) for s in manifest["sprites"]}
    used = set()
    for f in glob.glob(os.path.join(ASSETS, "Scripts", "**", "*.cs"), recursive=True):
        src = read(f)
        used |= set(re.findall(r'(?:BobUi\.Img|BobUi\.Panel)\([^;\n]*?"([a-z0-9]+(?:_[a-z0-9]+)+)"', src))
        used |= set(re.findall(r'Art\.(?:Get|Has|Frames)\("([a-z0-9]+(?:_[a-z0-9]+)+)"', src))
    missing = sorted(n for n in used if n not in names)
    for n in missing:
        fail(f'code asks for sprite "{n}" but no manifest entry has it')
    return len(used)


def check_audio_names():
    """Every BobSfx name must be a clip on the audio prefab, except the ones the code synthesises (BobTone)."""
    g2name = {}
    for meta in glob.glob(os.path.join(ASSETS, "**", "*.meta"), recursive=True):
        m = re.search(r"guid: ([0-9a-f]{32})", read(meta)[:400])
        if m:
            base = os.path.basename(meta)[: -len(".meta")]
            g2name[m.group(1)] = os.path.splitext(base)[0]
    prefab = read(os.path.join(ASSETS, "Resources", "Audio", "BobAudio.prefab"))
    block = prefab.split("clips:")[1].split("music:")[0]
    clips = {g2name.get(g, "??" + g) for g in re.findall(r"guid: ([0-9a-f]{32})", block)}
    tone_file = os.path.join(ASSETS, "Scripts", "Art", "BobTone.cs")
    tones = set(re.findall(r'AudioClip\.Create\("([a-z0-9_]+)"', read(tone_file))) if os.path.exists(tone_file) else set()
    used = set()
    for f in glob.glob(os.path.join(ASSETS, "Scripts", "**", "*.cs"), recursive=True):
        src = read(f)
        used |= set(re.findall(r'BobSfx\.(?:Play|Loop)\("([a-z0-9_]+)"', src))
    for n in sorted(used - clips - tones):
        fail(f'BobSfx plays "{n}" but the audio prefab has no such clip and no tone fallback')
    return len(clips), sorted(tones)


def resources_index():
    """Every path Resources.Load can resolve to: folders, file names, and names without their extension."""
    root = os.path.join(ASSETS, "Resources")
    idx = set()
    for dirpath, _dirnames, filenames in os.walk(root):
        rel = os.path.relpath(dirpath, root).replace(os.sep, "/")
        idx.add(rel)
        for name in filenames:
            if name.endswith(".meta"):
                continue
            joined = name if rel == "." else rel + "/" + name
            idx.add(joined)
            idx.add(os.path.splitext(joined)[0])
    return idx


# Literals that are allowed to miss, with the reason. Keep this list short and honest: everything else must resolve.
OPTIONAL_LOADS = {
    "BobUiTheme": "optional: BobUiTheme.Current falls back to the built-in palette when the asset is absent",
}


def check_resources_loads():
    """A Resources.Load path that does not resolve returns null: the screen plays empty, with no error in the console.

    Only complete string literals are checked; paths built by concatenation ("Heroes/SK_" + id) are covered by their
    own gates (art manifest names, Resources/Stages counts).
    """
    idx = resources_index()
    used = set()
    for f in glob.glob(os.path.join(ASSETS, "Scripts", "**", "*.cs"), recursive=True):
        used |= set(re.findall(r'Resources\.Load(?:All)?(?:<[^>]+>)?\("([^"]+)"\)', read(f)))
    for path in sorted(used):
        if path in idx:
            continue
        if path in OPTIONAL_LOADS:
            note(f'Resources.Load("{path}") is absent by design - {OPTIONAL_LOADS[path]}')
            continue
        fail(f'Resources.Load("{path}") resolves to nothing under Assets/Resources')
    return len(used)


def check_scripts():
    """A scene or prefab that points at a script with no .cs must be caught before Play: Unity shows Missing (Mono Script).

    Unity writes `m_Script: {fileID: 0}` for a component whose script it cannot find - that is always a blocker. A GUID
    that resolves outside Assets belongs to a Unity package (TMP, uGUI), so the known-external set lives in
    ready-check/external-scripts.txt: a *new* unresolved GUID means a project script was deleted or renamed.
    """
    known = set()
    base = os.path.join(os.path.dirname(os.path.abspath(__file__)), "external-scripts.txt")
    if os.path.exists(base):
        known = {l.strip() for l in read(base).splitlines() if l.strip() and not l.startswith("#")}
    path_of = {}
    for meta in glob.glob(os.path.join(ASSETS, "**", "*.meta"), recursive=True):
        m = re.search(r"guid: ([0-9a-f]{32})", read(meta)[:400])
        if m:
            path_of[m.group(1)] = meta[: -len(".meta")]
    seen, external = set(), set()
    for f in glob.glob(os.path.join(ASSETS, "**", "*.unity"), recursive=True) + glob.glob(
        os.path.join(ASSETS, "**", "*.prefab"), recursive=True
    ):
        src = read(f)
        short = os.path.relpath(f, REPO)
        if "m_Script: {fileID: 0}" in src:
            fail(f"{short} has a component with a missing script (m_Script: fileID 0)")
        for guid in re.findall(r"m_Script: \{fileID: \d+, guid: ([0-9a-f]{32})", src):
            seen.add(guid)
            if guid not in path_of:
                external.add(guid)
            elif not path_of[guid].endswith(".cs"):
                fail(f"{short} points at {os.path.relpath(path_of[guid], REPO)} as a script, but it is not a .cs")
    for guid in sorted(external - known):
        fail(f"unresolved script GUID {guid}: no .cs in Assets and not in ready-check/external-scripts.txt")
    return len(seen), len(external)


def check_duplicate_guids():
    """Two assets with the same GUID make Unity bind the wrong file - the classic silent asset swap."""
    seen = {}
    dups = 0
    for meta in glob.glob(os.path.join(ASSETS, "**", "*.meta"), recursive=True):
        m = re.search(r"guid: ([0-9a-f]{32})", read(meta)[:400])
        if not m:
            continue
        guid = m.group(1)
        if guid in seen:
            dups += 1
            fail(f"duplicate GUID {guid}: {os.path.relpath(seen[guid], REPO)} and {os.path.relpath(meta, REPO)}")
        else:
            seen[guid] = meta
    return len(seen), dups


def check_player_settings():
    """The build settings the game is shipped with: a wrong one makes an APK that installs but cannot be played.

    These are pinned to what the code expects, not to taste: the package name is the distributed app id, the
    orientation must stay landscape because every screen is authored 1920x1080, and activeInputHandler must stay 0
    while TaskRaceModeManager builds a legacy StandaloneInputModule.
    """
    src = read(os.path.join(REPO, "unity", "ProjectSettings", "ProjectSettings.asset"))

    def one(pattern, what):
        m = re.search(pattern, src, re.MULTILINE)
        if not m:
            fail(f"ProjectSettings is missing {what}")
            return None
        return m.group(1).strip()

    product = one(r"^  productName: (.+)$", "productName")
    company = one(r"^  companyName: (.+)$", "companyName")
    if product is not None and not product:
        fail("productName is empty - the launcher shows a blank app name")
    if company is not None and not company:
        fail("companyName is empty")
    app_id = one(r"^    Android: (.+)$", "the Android application identifier")
    if app_id is not None and app_id != "com.bob.battleofthebrooms":
        fail(f"Android application identifier is {app_id}, expected com.bob.battleofthebrooms")
    code = one(r"^  AndroidBundleVersionCode: (\d+)$", "AndroidBundleVersionCode")
    if code is not None and int(code) < 1:
        fail("AndroidBundleVersionCode is 0 - Google Play rejects it")
    min_sdk = one(r"^  AndroidMinSdkVersion: (\d+)$", "AndroidMinSdkVersion")
    if min_sdk is not None and int(min_sdk) < 26:
        fail(f"AndroidMinSdkVersion is {min_sdk}, expected 26 or newer")
    orientation = one(r"^  defaultScreenOrientation: (\d+)$", "defaultScreenOrientation")
    if orientation is not None and orientation not in ("3", "4"):
        fail(f"defaultScreenOrientation is {orientation}; the game is authored landscape only (3 or 4)")
    for key in ("allowedAutorotateToPortrait", "allowedAutorotateToPortraitUpsideDown"):
        v = one(r"^  " + key + r": (\d+)$", key)
        if v is not None and v != "0":
            fail(f"{key} is {v}; portrait would letterbox every screen")
    input_handler = one(r"^  activeInputHandler: (\d+)$", "activeInputHandler")
    if input_handler is not None and input_handler != "0":
        fail(
            "activeInputHandler is "
            + input_handler
            + "; the scenes build a legacy StandaloneInputModule, so pointer input dies unless this stays 0"
        )
    version = one(r"^  bundleVersion: (.+)$", "bundleVersion") or "?"
    return f'{app_id} · {version} ({code}) · minSdk {min_sdk} · landscape · legacy input'


def report_lfs():
    """A wav/glb committed wholesale (not as an LFS pointer) only warns on checkout; a pointer with no object breaks it."""
    pointers, plain = 0, 0
    for ext in ("*.wav", "*.glb", "*.png"):
        for path in glob.glob(os.path.join(ASSETS, "**", ext), recursive=True):
            with open(path, "rb") as f:
                head = f.read(64)
            if head.startswith(b"version https://git-lfs.github.com/spec"):
                pointers += 1
            else:
                plain += 1
    note(f"LFS: {pointers} pointer(s), {plain} plain binary file(s) under unity/Assets (plain ones still work; they only warn)")


def main():
    if not os.path.isdir(ASSETS):
        print("ready-check: cannot find unity/Assets next to this script", file=sys.stderr)
        return 2

    names = check_build_scenes()
    check_scene_loads(names)
    check_event_system(names)
    check_resources()
    loads = check_resources_loads()
    scripts, external = check_scripts()
    metas, dups = check_duplicate_guids()
    settings = check_player_settings()
    art = check_art_names()
    clips, tones = check_audio_names()
    report_lfs()

    print(f"scenes: {', '.join(names) if names else '(none)'}")
    print(f"art: {art} sprite name(s) checked, audio: {clips} clip(s), tone fallback: {', '.join(tones) or '(none)'}")
    print(f"loads: {loads} Resources.Load path(s) · scripts: {scripts} guid(s) ({external} from packages) · metas: {metas} ({dups} duplicate(s))")
    print(f"settings: {settings}")
    for n in notes:
        print("note: " + n)
    if blockers:
        print("\nREADY-CHECK FAILED:")
        for b in blockers:
            print("  - " + b)
        return 1
    print("READY-CHECK PASSED")
    return 0


if __name__ == "__main__":
    sys.exit(main())
