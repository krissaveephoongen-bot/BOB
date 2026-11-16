#!/usr/bin/env python3
"""Play-readiness gate for the BOB repo: the checks that catch a build which cannot be played.

Unity is not available in the CI sandbox, so this script never opens the editor. It reads the same files the editor
would: scene YAML, .meta GUIDs, Resources folders, the art manifest and the audio prefab. Everything it needs is in the
repository, so a failure here is a real failure before anyone presses Play.

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
    art = check_art_names()
    clips, tones = check_audio_names()
    report_lfs()

    print(f"scenes: {', '.join(names) if names else '(none)'}")
    print(f"art: {art} sprite name(s) checked, audio: {clips} clip(s), tone fallback: {', '.join(tones) or '(none)'}")
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
