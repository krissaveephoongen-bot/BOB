// Battle of the Brooms: import settings + animation builder for the generated placeholder art.
// Reads Assets/Art/bob-art-manifest.json (written by tools/make_art.py) so every PNG is sliced, pivoted and 9-sliced
// automatically. Menu: Bob > Art > Build Animation Clips / Build Audio Prefab / Reimport Art.
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Bob.Art.EditorTools
{
    [Serializable] public class ArtSprite
    {
        public string path, group, owner, anim, family;
        public int w, h, frames = 1;
        public float fps = 10;
        public bool loop = true, tile, still; // still = a sheet of separate poses (e.g. faces), not an animation
        public float[] pivot, border;
    }

    [Serializable] public class ArtManifest { public int scale; public List<ArtSprite> sprites; }

    public class BobArtImporter : AssetPostprocessor
    {
        public const string ArtRoot = "Assets/Art/";
        public const string ManifestPath = ArtRoot + "bob-art-manifest.json";
        public const float PixelsPerUnit = 256f; // one 256 px frame = 1 world unit

        static ArtManifest cache;
        public static ArtManifest Manifest
        {
            get
            {
                if (cache != null) return cache;
                var p = Path.Combine(Directory.GetCurrentDirectory(), ManifestPath);
                if (File.Exists(p)) cache = JsonUtility.FromJson<ArtManifest>(File.ReadAllText(p));
                return cache;
            }
        }
        public static void ClearCache() => cache = null;

        public static ArtSprite Find(string assetPath)
        {
            if (!assetPath.StartsWith(ArtRoot + "Sprites/") || !assetPath.EndsWith(".png")) return null;
            var rel = assetPath.Substring(ArtRoot.Length, assetPath.Length - ArtRoot.Length - 4);
            return Manifest?.sprites?.FirstOrDefault(x => x.path == rel);
        }

        void OnPreprocessTexture()
        {
            var s = Find(assetPath);
            if (s == null) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Sprite;
            ti.spritePixelsPerUnit = PixelsPerUnit;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.filterMode = FilterMode.Bilinear;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Compressed;
            ti.wrapMode = s.tile ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            var pivot = s.pivot != null && s.pivot.Length == 2 ? new Vector2(s.pivot[0], s.pivot[1]) : new Vector2(0.5f, 0.5f);
            var border = s.border != null && s.border.Length == 4 ? new Vector4(s.border[0], s.border[1], s.border[2], s.border[3]) : Vector4.zero;

            if (s.frames <= 1)
            {
                ti.spriteImportMode = SpriteImportMode.Single;
                var set = new TextureImporterSettings();
                ti.ReadTextureSettings(set);
                set.spriteAlignment = (int)SpriteAlignment.Custom;
                set.spritePivot = pivot;
                set.spriteBorder = border;
                set.spriteMeshType = (s.tile || border != Vector4.zero) ? SpriteMeshType.FullRect : SpriteMeshType.Tight;
                ti.SetTextureSettings(set);
                return;
            }

            ti.spriteImportMode = SpriteImportMode.Multiple;
            var baseName = Path.GetFileNameWithoutExtension(assetPath);
            var metas = new SpriteMetaData[s.frames];
            for (int i = 0; i < s.frames; i++)
                metas[i] = new SpriteMetaData
                {
                    name = baseName + "_" + i,
                    rect = new Rect(i * s.w, 0, s.w, s.h),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = pivot,
                };
#pragma warning disable 618 // spritesheet is marked obsolete in newer Unity but still works; swap to ISpriteEditorDataProvider if it is removed.
            ti.spritesheet = metas;
#pragma warning restore 618
        }

        void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            bool music = assetPath.Contains("/Music/");
            ai.forceToMono = true;
            ai.loadInBackground = music;
            var st = ai.defaultSampleSettings;
            st.loadType = music ? AudioClipLoadType.Streaming : AudioClipLoadType.DecompressOnLoad;
            st.compressionFormat = AudioCompressionFormat.Vorbis;
            st.quality = music ? 0.6f : 0.8f;
            ai.defaultSampleSettings = st;
        }
    }

    public static class BobArtMenu
    {
        [MenuItem("Bob/Art/Reimport Art")]
        public static void Reimport()
        {
            BobArtImporter.ClearCache();
            AssetDatabase.ImportAsset(BobArtImporter.ArtRoot + "Sprites", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset("Assets/Audio", ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("Bob/Art/Build Animation Clips")]
        public static void BuildClips()
        {
            BobArtImporter.ClearCache();
            var m = BobArtImporter.Manifest;
            if (m == null) { Debug.LogError("Bob: no " + BobArtImporter.ManifestPath); return; }

            var perOwner = new Dictionary<string, List<KeyValuePair<ArtSprite, AnimationClip>>>();
            int made = 0;
            foreach (var s in m.sprites.Where(x => x.frames > 1 && !x.still))
            {
                var png = BobArtImporter.ArtRoot + s.path + ".png";
                var frames = AssetDatabase.LoadAllAssetsAtPath(png).OfType<Sprite>()
                    .OrderBy(sp => int.Parse(sp.name.Substring(sp.name.LastIndexOf('_') + 1))).ToArray();
                if (frames.Length == 0) { Debug.LogWarning("Bob: no sliced sprites in " + png + " (run Bob > Art > Reimport Art)"); continue; }

                var clip = new AnimationClip { frameRate = s.fps };
                var keys = new ObjectReferenceKeyframe[frames.Length + 1];
                for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / s.fps, value = frames[i] };
                keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / s.fps, value = s.loop ? frames[0] : frames[frames.Length - 1] };
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
                var cs = AnimationUtility.GetAnimationClipSettings(clip);
                cs.loopTime = s.loop;
                AnimationUtility.SetAnimationClipSettings(clip, cs);

                var outPath = BobArtImporter.ArtRoot + "Animations/" + s.path.Substring("Sprites/".Length) + ".anim";
                Directory.CreateDirectory(Path.GetDirectoryName(outPath));
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
                if (existing != null) { EditorUtility.CopySerialized(clip, existing); clip = existing; }
                else AssetDatabase.CreateAsset(clip, outPath);
                made++;

                if (!string.IsNullOrEmpty(s.owner) && !string.IsNullOrEmpty(s.anim))
                {
                    var dir = Path.GetDirectoryName(outPath).Replace('\\', '/');
                    var key = dir + "/" + s.owner + ".controller";
                    if (!perOwner.TryGetValue(key, out var list)) perOwner[key] = list = new List<KeyValuePair<ArtSprite, AnimationClip>>();
                    list.Add(new KeyValuePair<ArtSprite, AnimationClip>(s, clip));
                }
            }

            // One Animator Controller per character / enemy. States are named after the animation (idle, walk, hit...).
            // Play them from code with animator.Play("walk"), or fire the trigger of the same name for one-shots.
            foreach (var kv in perOwner)
            {
                AssetDatabase.DeleteAsset(kv.Key); // regenerated each time; make your own controller if you hand-edit transitions
                var ctrl = AnimatorController.CreateAnimatorControllerAtPath(kv.Key);
                var sm = ctrl.layers[0].stateMachine;
                var states = new Dictionary<string, AnimatorState>();
                foreach (var p in kv.Value)
                {
                    var st = sm.AddState(p.Key.anim);
                    st.motion = p.Value;
                    states[p.Key.anim] = st;
                }
                if (states.TryGetValue("idle", out var idle))
                {
                    sm.defaultState = idle;
                    foreach (var p in kv.Value.Where(p => !p.Key.loop))
                    {
                        ctrl.AddParameter(p.Key.anim, AnimatorControllerParameterType.Trigger);
                        var into = sm.AddAnyStateTransition(states[p.Key.anim]);
                        into.AddCondition(AnimatorConditionMode.If, 0, p.Key.anim);
                        into.duration = 0; into.canTransitionToSelf = false;
                        if (p.Key.anim == "defeat") continue; // stays on the last frame
                        var back = states[p.Key.anim].AddTransition(idle);
                        back.hasExitTime = true; back.exitTime = 1f; back.duration = 0;
                    }
                }
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"Bob: built {made} animation clips and {perOwner.Count} animator controllers under {BobArtImporter.ArtRoot}Animations");
        }

        [MenuItem("Bob/Art/Build Audio Prefab")]
        public static void BuildAudioPrefab()
        {
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/SFX" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).Where(c => c != null).ToArray();
            var music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/game_the_fun_run.mp3")
                ?? AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/music_placeholder_loop.wav");
            var tracks = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio/Music" })
                .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))).Where(c => c != null).ToArray();
            var go = new GameObject("BobAudio");
            var sfx = go.AddComponent<Bob.Art.BobSfx>();
            sfx.clips = clips;
            sfx.music = music; sfx.tracks = tracks;
            var path = BobArtImporter.ArtRoot + "BobAudio.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            Debug.Log($"Bob: {path} with {clips.Length} sound effects. Drop it in your first scene.");
        }
    }
}
#endif
