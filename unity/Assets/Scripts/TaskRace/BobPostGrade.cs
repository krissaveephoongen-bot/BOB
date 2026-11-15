// Battle of the Brooms: built-in-pipeline post grade for the Task Race scenes (docs 24 section 15.15). Vignette,
// contrast, saturation, a cool-shadow / warm-highlight split tone, exposure and a half-resolution bloom. The project
// has no URP, so this stands in for a post-processing volume: 3 extra quarter-screen draws per frame, no compute.
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public class BobPostGrade : MonoBehaviour
    {
        // Light-handed: the vignette used to darken the top corners of the sky into a navy band, which read as a
        // backdrop edge rather than a lens effect.
        [Range(0f, 1f)] public float vignette = 0.2f;
        [Range(0.5f, 2f)] public float saturation = 1.12f;
        [Range(0.5f, 2f)] public float contrast = 1.03f;
        public Color shadowTone = new Color(0.94f, 0.97f, 1.06f);
        public Color highlightTone = new Color(1.05f, 1.02f, 0.96f);

        [Tooltip("Stops of exposure, applied before the tone curve. Kept near zero: the pastel sky already sits high.")]
        [Range(-1f, 1f)] public float postExposure = 0.03f;

        [Tooltip("Bloom strength. 0 disables the three extra passes entirely.")]
        [Range(0f, 1f)] public float bloom = 0.35f;
        [Tooltip("Luminance above which a pixel blooms: only sparkles and specular highlights, never UI or paper.")]
        [Range(0.6f, 2f)] public float bloomThreshold = 1.05f;

        Shader shader, bloomShader;
        Material material, bloomMaterial;

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (shader == null) shader = Resources.Load<Shader>("Shaders/BobPostGrade") ?? Shader.Find("Hidden/BOB/PostGrade");
            if (shader == null || !shader.isSupported)
            {
                Graphics.Blit(src, dst);
                return;
            }
            if (material == null) material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            material.SetFloat("_Vignette", vignette);
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_Contrast", contrast);
            material.SetColor("_ShadowTone", shadowTone);
            material.SetColor("_HighlightTone", highlightTone);
            material.SetFloat("_Exposure", Mathf.Pow(2f, postExposure));

            if (bloom > 0.0001f && Bloom(src, out var bloomTex))
            {
                material.SetTexture("_BloomTex", bloomTex);
                material.SetFloat("_Bloom", bloom);
                Graphics.Blit(src, dst, material);
                material.SetFloat("_Bloom", 0f);
                RenderTexture.ReleaseTemporary(bloomTex);
                return;
            }

            material.SetFloat("_Bloom", 0f);
            Graphics.Blit(src, dst, material);
        }

        /// <summary>Bright pass at half resolution, then a horizontal and a vertical blur (docs 24 section 15.15).</summary>
        bool Bloom(RenderTexture src, out RenderTexture result)
        {
            result = null;
            if (bloomShader == null) bloomShader = Resources.Load<Shader>("Shaders/BobBloom") ?? Shader.Find("Hidden/BOB/Bloom");
            if (bloomShader == null || !bloomShader.isSupported) return false;
            if (bloomMaterial == null) bloomMaterial = new Material(bloomShader) { hideFlags = HideFlags.HideAndDontSave };

            int w = Mathf.Max(1, src.width / 2), h = Mathf.Max(1, src.height / 2);
            var bright = RenderTexture.GetTemporary(w, h, 0, src.format);
            var blurA = RenderTexture.GetTemporary(w, h, 0, src.format);
            var blurB = RenderTexture.GetTemporary(w, h, 0, src.format);

            // 1.05 only means something over an HDR buffer. On an LDR phone target nothing can exceed white, so the
            // threshold drops just under it: the sparkles, foam and specular highlights bloom, the flat pastels do not.
            bool hdr = src.format == RenderTextureFormat.DefaultHDR || src.format == RenderTextureFormat.ARGBHalf
                       || src.format == RenderTextureFormat.RGB111110Float;
            bloomMaterial.SetFloat("_Threshold", hdr ? bloomThreshold : 0.98f);
            Graphics.Blit(src, bright, bloomMaterial, 0);

            bloomMaterial.SetVector("_BlurDir", new Vector4(1f / w, 0f, 0f, 0f));
            Graphics.Blit(bright, blurA, bloomMaterial, 1);
            bloomMaterial.SetVector("_BlurDir", new Vector4(0f, 1f / h, 0f, 0f));
            Graphics.Blit(blurA, blurB, bloomMaterial, 1);

            RenderTexture.ReleaseTemporary(bright);
            RenderTexture.ReleaseTemporary(blurA);
            result = blurB;
            return true;
        }

        void OnDisable()
        {
            Release(ref material);
            Release(ref bloomMaterial);
        }

        static void Release(ref Material m)
        {
            if (m == null) return;
            if (Application.isPlaying) Destroy(m); else DestroyImmediate(m);
            m = null;
        }
    }
}
