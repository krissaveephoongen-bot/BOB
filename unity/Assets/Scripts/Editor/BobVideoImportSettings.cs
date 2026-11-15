// Battle of the Brooms: one import rule for any video the owner drops in (docs 24 section 15.15). Every clip under
// Assets/Video/ is transcoded to H264 at 720p with no audio track, then moved to Resources/Video/ where the runtime can
// find it - Resources.Load is the only way a menu can reach a clip without a serialized reference.
#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Video;

namespace Bob.EditorTools
{
    public class BobVideoImportSettings : AssetPostprocessor
    {
        public const string Drop = "Assets/Video/";
        public const string Runtime = "Assets/Resources/Video/";

        static readonly HashSet<string> attempted = new HashSet<string>();

        void OnPreprocessAsset()
        {
            if (!IsClip(assetPath)) return;
            if (!assetPath.StartsWith(Drop) && !assetPath.StartsWith(Runtime)) return;
            if (!(assetImporter is VideoClipImporter importer)) return;

            // A menu or tutorial clip must never step on the BGM: the audio track is not imported at all.
            importer.importAudio = false;
            importer.sRGBClip = true;

            var target = importer.defaultTargetSettings;
            target.enableTranscoding = true;
            target.codec = VideoCodec.H264;
            target.bitrateMode = VideoBitrateMode.Medium;
            target.resizeMode = VideoResizeMode.CustomSize;
            target.customWidth = 1280;
            target.customHeight = 720;
            importer.defaultTargetSettings = target;
        }

        static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            foreach (var path in imported)
            {
                if (!path.StartsWith(Drop) || !IsClip(path) || !attempted.Add(path)) continue;
                // Deferred: moving an asset inside the import callback is not allowed.
                string from = path;
                EditorApplication.delayCall += () => Move(from);
            }
        }

        static void Move(string from) => MoveOne(from);

        /// <summary>
        /// Batchmode quits before the deferred delayCall above runs, so a dropped clip is imported but never reaches
        /// Resources. Build methods call this first: it moves every clip still sitting in Assets/Video.
        /// </summary>
        public static int MovePending()
        {
            if (!AssetDatabase.IsValidFolder(Drop.TrimEnd('/'))) return 0;
            int moved = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:VideoClip", new[] { Drop.TrimEnd('/') }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsClip(path) && MoveOne(path)) moved++;
            }
            if (moved > 0) AssetDatabase.SaveAssets();
            return moved;
        }

        static bool MoveOne(string from)
        {
            if (AssetDatabase.LoadAssetAtPath<VideoClip>(from) == null) return false;
            string to = Runtime + Path.GetFileName(from);
            if (AssetDatabase.LoadAssetAtPath<VideoClip>(to) != null)
            {
                Debug.LogWarning($"[BobVideo] {to} already exists: {from} stays put and will not load at runtime.");
                return false;
            }
            EnsureFolder(Runtime);
            string error = AssetDatabase.MoveAsset(from, to);
            if (string.IsNullOrEmpty(error))
            {
                Debug.Log($"[BobVideo] {from} -> {to} (H264, 720p, no audio). MenuBackgroundVideo finds it as \"Video/{Path.GetFileNameWithoutExtension(to)}\".");
                return true;
            }
            Debug.LogWarning($"[BobVideo] could not move {from} to {to}: {error}. Move it by hand, or Resources.Load cannot see it.");
            return false;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        static bool IsClip(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".mp4" || ext == ".mov" || ext == ".m4v" || ext == ".webm" || ext == ".avi";
        }
    }
}
#endif
