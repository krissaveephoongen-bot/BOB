// Battle of the Brooms: every placeholder sprite by its short name ("sweep_walk", "part_soap", "cart", "ui_button").
// Filled by the menu Bob > Build Game Scene from Assets/Art/bob-art-manifest.json, so game code never needs asset paths.
// Missing art never breaks the game: Get() falls back to a plain white square that callers tint.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Bob.Game
{
    [CreateAssetMenu(menuName = "Bob/Art Library")]
    public class BobArtLibrary : ScriptableObject
    {
        [Serializable] public class Entry { public string name; public Sprite[] frames; public float fps = 10; public bool loop = true; }

        public List<Entry> entries = new List<Entry>();

        Dictionary<string, Entry> map;
        static Sprite white;

        public static Sprite White
        {
            get
            {
                if (white != null) return white;
                var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point };
                var px = new Color32[16]; for (int i = 0; i < 16; i++) px[i] = new Color32(255, 255, 255, 255);
                tex.SetPixels32(px); tex.Apply();
                white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4);
                return white;
            }
        }

        public Entry Find(string name)
        {
            if (map == null)
            {
                map = new Dictionary<string, Entry>();
                foreach (var e in entries) if (e != null && e.frames != null && e.frames.Length > 0) map[e.name] = e;
            }
            return map.TryGetValue(name, out var x) ? x : null;
        }

        public bool Has(string name) => Find(name) != null;
        public Sprite Get(string name) { var e = Find(name); return e != null ? e.frames[0] : White; }
        public Sprite[] Frames(string name) { var e = Find(name); return e != null ? e.frames : new[] { White }; }
    }
}
