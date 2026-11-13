// Battle of the Brooms: turns the FBX heroes in Assets/Art/Heroes into prefabs under Resources/Heroes (menu Bob > Art > Build Hero Prefabs).
// Each prefab gets the clips idle/walk/cheer, an AnimatorController with float Speed and bool Carry, the shared palette and the toon material.
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobHeroImporter
    {
        const string Dir = "Assets/Art/Heroes/";

        [MenuItem("Bob/Art/Build Hero Prefabs")]
        public static void Build()
        {
            Directory.CreateDirectory("Assets/Resources/Heroes");
            var pal = AssetDatabase.LoadAssetAtPath<Texture2D>(Dir + "T_BOB_Palette.png");
            if (pal != null)
            {
                var ti = (TextureImporter)AssetImporter.GetAtPath(Dir + "T_BOB_Palette.png");
                ti.filterMode = FilterMode.Point; ti.textureCompression = TextureImporterCompression.Uncompressed; ti.mipmapEnabled = false; ti.SaveAndReimport();
            }
            foreach (var id in new[] { "sweep", "bubble", "spray", "press" }) BuildOne(id, pal);
            foreach (var id in new[] { "dirt", "slime", "trash", "boss_slime" }) BuildOne(id, pal);
            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        }

        static void BuildOne(string id, Texture2D pal)
        {
            string fbx = Dir + "SK_" + id + ".fbx";
            var imp = (ModelImporter)AssetImporter.GetAtPath(fbx);
            if (imp == null) { Debug.LogError("Bob: missing " + fbx); return; }
            imp.animationType = ModelImporterAnimationType.Generic; imp.importAnimation = true;
            imp.materialImportMode = ModelImporterMaterialImportMode.None;
            imp.SaveAndReimport();
            var clips = AssetDatabase.LoadAllAssetsAtPath(fbx).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview")).ToList();
            AnimationClip Find(string n) => clips.FirstOrDefault(c => c.name.ToLower().Contains(n));
            var idle = Find("idle"); var walk = Find("walk"); var cheer = Find("cheer");
            var path = "Assets/Resources/Heroes/SK_" + id + ".controller";
            AssetDatabase.DeleteAsset(path);
            var ac = AnimatorController.CreateAnimatorControllerAtPath(path);
            ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
            ac.AddParameter("Carry", AnimatorControllerParameterType.Bool);
            var sm = ac.layers[0].stateMachine;
            var sIdle = sm.AddState("idle"); sIdle.motion = idle;
            sm.defaultState = sIdle;
            if (walk != null) {
            var sWalk = sm.AddState("walk"); sWalk.motion = walk;
            var t1 = sIdle.AddTransition(sWalk); t1.hasExitTime = false; t1.duration = 0.1f; t1.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
            var t2 = sWalk.AddTransition(sIdle); t2.hasExitTime = false; t2.duration = 0.1f; t2.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed"); }
            if (cheer != null) { var sC = sm.AddState("cheer"); sC.motion = cheer; }
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(fbx);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(model);
            var anim = inst.GetComponent<Animator>(); if (anim == null) anim = inst.AddComponent<Animator>();
            anim.runtimeAnimatorController = ac; anim.applyRootMotion = false;
            var mat = new Material(Shader.Find("BOB/ToonLit")) { color = Color.white };
            if (pal != null) mat.mainTexture = pal;
            string matPath = "Assets/Resources/Heroes/M_SK_" + id + ".mat"; AssetDatabase.DeleteAsset(matPath); AssetDatabase.CreateAsset(mat, matPath);
            foreach (var r in inst.GetComponentsInChildren<Renderer>()) r.sharedMaterials = r.sharedMaterials.Select(_ => mat).ToArray();
            PrefabUtility.SaveAsPrefabAsset(inst, "Assets/Resources/Heroes/SK_" + id + ".prefab");
            Object.DestroyImmediate(inst);
            Debug.Log($"Bob: hero SK_{id}: clips {string.Join(",", clips.Select(c => c.name))}");
        }
    }
}
