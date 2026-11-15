// Battle of the Brooms: writes the six StageConfig assets of Resources/Stages from the owner's stage table
// (2026-11-15). Idempotent: an existing asset keeps its GUID and only its values are rewritten.
//   Tools > BOB > Stages > Rebuild Six Stage Assets
//   -executeMethod Bob.EditorTools.BobStageBuilder.BuildAll
using System.IO;
using System.Linq;
using Bob.Rules;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobStageBuilder
    {
        const string Folder = "Assets/Resources/Stages";

        struct Row
        {
            public string file, title;
            public float clock, open;
            public int points, bonus;
            public float fragile;
            public bool twoPass, randomize;
        }

        // The owner's stage table: clocks 120/120/120/120/90/120, bridge 60/60/60/60/45/60, points 100/75/100/100/100/100
        // and the stage 6 bonus of 200.
        static readonly Row[] Rows =
        {
            new Row { file = "Stage1_RentalRoom",     title = "ห้องเช่า",              clock = 120, open = 60, points = 100 },
            new Row { file = "Stage2_NeighbourJobs",  title = "รับงานเพื่อนบ้าน",      clock = 120, open = 60, points = 75 },
            new Row { file = "Stage3_ShopStreet",     title = "ร้านค้าในซอย",          clock = 120, open = 60, points = 100, fragile = 1.5f },
            new Row { file = "Stage4_OldHouse",       title = "บ้านเก่ามีเรื่องเล่า",  clock = 120, open = 60, points = 100, twoPass = true },
            new Row { file = "Stage5_District",       title = "งานใหญ่ของย่าน",        clock = 90,  open = 45, points = 100, randomize = true },
            new Row { file = "Stage6_Market",         title = "ฟื้นตลาดเก่า",          clock = 120, open = 60, points = 100, bonus = 200 },
        };

        [MenuItem("Tools/BOB/Stages/Rebuild Six Stage Assets")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Directory.CreateDirectory(Folder);
                AssetDatabase.Refresh();
            }

            for (int i = 0; i < Rows.Length; i++)
            {
                var row = Rows[i];
                string path = $"{Folder}/{row.file}.asset";
                var stage = AssetDatabase.LoadAssetAtPath<StageConfig>(path);
                if (stage == null)
                {
                    stage = ScriptableObject.CreateInstance<StageConfig>();
                    AssetDatabase.CreateAsset(stage, path);
                }

                var so = new SerializedObject(stage);
                so.FindProperty("order").intValue = i + 1;
                so.FindProperty("title").stringValue = row.title;
                so.FindProperty("clockSeconds").floatValue = row.clock;
                so.FindProperty("bridgeOpenAtRemaining").floatValue = row.open;
                so.FindProperty("pointsPerTask").intValue = row.points;
                so.FindProperty("stageBonus").intValue = row.bonus;
                so.FindProperty("fragileStunSeconds").floatValue = row.fragile;
                so.FindProperty("twoPassStains").boolValue = row.twoPass;
                so.FindProperty("randomizeSpots").boolValue = row.randomize;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(stage);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            foreach (var stage in Resources.LoadAll<StageConfig>("Stages").OrderBy(s => s.Order))
                Debug.Log($"[BobStages] stage {stage.Order} {stage.Title}: {stage.Tag} · {stage.PointsPerTask} แต้ม/งาน" +
                          $"{(stage.StageBonus > 0 ? $" · โบนัส {stage.StageBonus}" : "")}" +
                          $"{(stage.FragileStunSeconds > 0 ? $" · สตัน {stage.FragileStunSeconds:0.#} วิ" : "")}" +
                          $"{(stage.TwoPassStains ? " · คราบสองรอบ" : "")}{(stage.RandomizeSpots ? " · จุดสุ่ม" : "")}");
            Debug.Log($"[BobStages] {Rows.Length} stage asset(s) written under {Folder}.");
        }
    }
}
