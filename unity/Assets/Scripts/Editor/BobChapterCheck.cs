// Battle of the Brooms: the six chapters of docs 24 section 15.1 live in Resources/Chapters. This menu prints them in
// order (the same order S13 shows) and reports ladder problems, so a bad asset is caught in the editor instead of a
// missing stage on a phone.
//   -executeMethod Bob.EditorTools.BobChapterCheck.Run
using Bob.Rules;
using UnityEditor;
using UnityEngine;

namespace Bob.EditorTools
{
    public static class BobChapterCheck
    {
        [MenuItem("Bob/Chapters/Check Ladder")]
        public static void Run()
        {
            var all = BobChapters.All;
            foreach (var c in all)
                Debug.Log($"[Chapters] {c.Order}. {c.Title} · {c.Place} · {c.Tag} · {c.RoundTime:0} s · band {c.Band} · " +
                          $"{c.Backdrop} · cards: {string.Join(" / ", c.RuleCards)}");

            var problems = BobChapters.Validate();
            if (problems.Count == 0) { Debug.Log($"[Chapters] {all.Count} chapters, ladder ok (chapter 1 at level 1)."); return; }
            foreach (var p in problems) Debug.LogError("[Chapters] " + p);
        }
    }
}
