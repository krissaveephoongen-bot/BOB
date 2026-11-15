// Writes the six chapter assets of docs 24 section 15.1 into Resources/Chapters. Re-running updates the fields in place,
// so the scene builder's "run once" habit also works here; nothing in a scene points at these assets by GUID.
using System.Linq;
using Bob.Game;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobChapterBuilder
    {
        const string Folder = "Assets/Resources/Chapters";

        [MenuItem("Bob/Data/Build Chapter Definitions")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources")) AssetDatabase.CreateFolder("Assets", "Resources");
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Resources", "Chapters");

            // id, file, title, unlock level, band, backdrop, blurb (docs 24 sections 15.1 and 11 of the GDD)
            var rows = new[]
            {
                ("ch01", "ch01_rental_room", "ห้องเช่า", 1, 1, "lobby_rental_room", "ห้องเดียวขนาดเล็ก ของทุกอย่างอยู่ใกล้ · 1 เงื่อนไข เวลาเหลือเฟือ"),
                ("ch02", "ch02_neighbour_jobs", "รับงานเพื่อนบ้าน", 6, 1, "lobby_condo", "คอนโด 2 ห้อง ประตูแคบ · 2 เงื่อนไข + แหล่งแรง"),
                ("ch03", "ch03_soi_shops", "ร้านค้าในซอย", 13, 2, "lobby_coffee_shop", "ร้านกาแฟ 3 เคาน์เตอร์ · วิธีใช้ ผิวบอบบาง"),
                ("ch04", "ch04_old_house", "บ้านเก่ามีเรื่องเล่า", 21, 2, "lobby_kitchen", "บ้านไม้ 2 ชั้น มีบันได · 3 เงื่อนไข คราบซ้อน"),
                ("ch05", "ch05_neighbourhood", "งานใหญ่ของย่าน", 31, 3, "lobby_coffee_shop", "โรงเรียนทางเดินยาว · เวลาสั้นลง ชิ้นส่วนสุ่ม"),
                ("ch06", "ch06_old_market", "ฟื้นตลาดเก่า", 41, 3, "lobby_kitchen", "ตลาดหลายโซน · การ์ดกติกาพิเศษ รวมทุกกลไก"),
            };

            foreach (var r in rows)
            {
                string path = $"{Folder}/{r.Item2}.asset";
                var chapter = AssetDatabase.LoadAssetAtPath<ChapterDefinition>(path);
                if (chapter == null)
                {
                    chapter = ScriptableObject.CreateInstance<ChapterDefinition>();
                    AssetDatabase.CreateAsset(chapter, path);
                }
                var so = new SerializedObject(chapter);
                so.FindProperty("id").stringValue = r.Item1;
                so.FindProperty("title").stringValue = r.Item3;
                so.FindProperty("unlockLevel").intValue = r.Item4;
                so.FindProperty("difficulty").intValue = r.Item5;
                so.FindProperty("backdrop").stringValue = r.Item6;
                so.FindProperty("blurb").stringValue = r.Item7;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
            Debug.Log($"Bob: {rows.Length} chapter definitions in {Folder} ({string.Join(", ", rows.Select(r => r.Item1 + " L" + r.Item4 + " D" + r.Item5))})");
        }
    }
}
