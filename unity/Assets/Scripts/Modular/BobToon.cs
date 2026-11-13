// Battle of the Brooms: helpers for the toon look (Art Bible 21): one lit material with a 3-step ramp and a rim, plus an
// inverted-hull outline. Materials are kept in Assets/Resources/Materials so a player build never strips their shaders.
// Use Toon(colour) for anything that stands in the 3D room and Outline(go) to give it the dark line; 3D heroes use the same two calls.
using UnityEngine;

namespace Bob.Modular
{
    public static class BobToon
    {
        static Material toonBase, outlineBase;
        public static readonly Color LineColor = new Color(0.29f, 0.18f, 0.133f, 1f), MetalLine = new Color(0.243f, 0.263f, 0.298f, 1f);

        static Material ToonBase { get { if (toonBase == null) toonBase = Resources.Load<Material>("Materials/M_BOB_Toon"); return toonBase; } }
        static Material OutlineBase { get { if (outlineBase == null) outlineBase = Resources.Load<Material>("Materials/M_BOB_Outline"); return outlineBase; } }

        /// <summary>A new toon material in `colour`, optionally with a palette texture. Falls back to the standard shader if the asset is missing.</summary>
        public static Material Toon(Color colour, Texture tex = null, float dirt = 0f)
        {
            var b = ToonBase;
            var m = b != null ? new Material(b) : new Material(Shader.Find("Standard"));
            m.color = colour; if (tex != null) m.mainTexture = tex;
            if (m.HasProperty("_DirtAmount")) m.SetFloat("_DirtAmount", dirt);
            return m;
        }

        /// <summary>Add a constant-width outline to a renderer's mesh (a child copy drawn with the hull shader).</summary>
        public static GameObject Outline(GameObject target, float pixels = 2.5f, Color? colour = null)
        {
            var mf = target.GetComponent<MeshFilter>(); var ob = OutlineBase;
            if (mf == null || ob == null) return null;
            var go = new GameObject("Outline"); go.transform.SetParent(target.transform, false);
            go.AddComponent<MeshFilter>().sharedMesh = mf.sharedMesh;
            var mr = go.AddComponent<MeshRenderer>();
            var m = new Material(ob); m.SetFloat("_Pixels", pixels); m.SetColor("_OutlineColor", colour ?? LineColor);
            mr.sharedMaterial = m; mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; mr.receiveShadows = false;
            return go;
        }

        /// <summary>A primitive shape with the toon material (and an outline) in one call.</summary>
        public static GameObject Shape(PrimitiveType type, Transform parent, string name, Vector3 localPos, Vector3 scale, Color colour, bool outline = true, float outlinePx = 2f)
        {
            var g = GameObject.CreatePrimitive(type); g.name = name; g.transform.SetParent(parent, false);
            g.transform.localPosition = localPos; g.transform.localScale = scale;
            var col = g.GetComponent<Collider>(); if (col != null) Object.Destroy(col);
            g.GetComponent<MeshRenderer>().sharedMaterial = Toon(colour);
            if (outline) Outline(g, outlinePx);
            return g;
        }
    }
}
