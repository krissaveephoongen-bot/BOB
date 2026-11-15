// Battle of the Brooms: lets LINE send the player back into the app (docs 28). Unity's generated AndroidManifest only
// declares the launcher intent, so without this filter Android cannot open the game for bob://line-login and the
// browser is left holding the code. Runs on every Gradle build and skips itself when the filter is already there.
using System.IO;
using UnityEditor.Android;
using UnityEngine;

namespace Bob.EditorTools
{
    public class BobAndroidDeepLinks : IPostGenerateGradleAndroidProject
    {
        public const string Scheme = "bob";
        public const string Host = "line-login"; // keep in sync with BobLineLogin.DefaultRedirect / LINE_REDIRECT_URI

        public int callbackOrder => 10;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
            if (!File.Exists(manifest)) { Debug.LogWarning("[BobDeepLink] no manifest at " + manifest); return; }
            var xml = File.ReadAllText(manifest);
            if (xml.Contains("android:scheme=\"" + Scheme + "\"")) return; // already patched

            // Unity 6 uses UnityPlayerGameActivity; older projects use UnityPlayerActivity. Insert into whichever exists,
            // never into an <activity-alias> or another component.
            var names = new[] { "com.unity3d.player.UnityPlayerGameActivity", "com.unity3d.player.UnityPlayerActivity" };
            int at = -1, close = -1;
            foreach (var n in names)
            {
                at = xml.IndexOf(n);
                if (at < 0) continue;
                close = xml.IndexOf("</activity>", at);
                if (close > 0) break;
            }
            if (at < 0 || close < 0) { Debug.LogWarning("[BobDeepLink] player activity not found; deep link not added"); return; }

            var filter =
                "    <intent-filter>\n" +
                "      <action android:name=\"android.intent.action.VIEW\" />\n" +
                "      <category android:name=\"android.intent.category.DEFAULT\" />\n" +
                "      <category android:name=\"android.intent.category.BROWSABLE\" />\n" +
                "      <data android:scheme=\"" + Scheme + "\" android:host=\"" + Host + "\" />\n" +
                "    </intent-filter>\n  ";
            xml = xml.Insert(close, filter);
            File.WriteAllText(manifest, xml);
            Debug.Log("[BobDeepLink] " + Scheme + "://" + Host + " added to the player activity");
        }
    }
}
