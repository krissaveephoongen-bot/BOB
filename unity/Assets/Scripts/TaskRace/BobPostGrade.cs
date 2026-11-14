// Battle of the Brooms: the Task Race grade (docs 24). A mild built-in-pipeline image effect — vignette, contrast,
// saturation and a cool-shadow / warm-highlight split tone — added by the scene builder to the play camera and by the
// verify pass to its shot cameras.
using UnityEngine;

namespace Bob.TaskRace
{
    [ExecuteAlways, RequireComponent(typeof(Camera))]
    public class BobPostGrade : MonoBehaviour
    {
        // Light-handed: the vignette used to darken the top corners of the sky into a navy band, which read as a
        // backdrop edge rather than a lens effect.
        [Range(0f, 1f)] public float vignette = 0.2f;
        [Range(0.5f, 2f)] public float saturation = 1.06f;
        [Range(0.5f, 2f)] public float contrast = 1.03f;
        public Color shadowTone = new Color(0.94f, 0.97f, 1.06f);
        public Color highlightTone = new Color(1.05f, 1.02f, 0.96f);

        Shader shader;
        Material material;

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
            Graphics.Blit(src, dst, material);
        }

        void OnDisable()
        {
            if (material == null) return;
            if (Application.isPlaying) Destroy(material); else DestroyImmediate(material);
            material = null;
        }
    }
}
