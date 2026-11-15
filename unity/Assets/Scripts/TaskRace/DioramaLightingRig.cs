// Battle of the Brooms: the warm pastel daylight of the diorama (docs 24 section 15.15). One golden-cream sun with
// soft shadows and a bright sky/equator/ground ambient ramp, so no corner goes dark or muddy on a phone screen.
// DioramaAmbient adds this to the Systems object; the values are applied in code, so a scene rebuild cannot lose them
// and leaving the scene puts the previous lighting back for the lobby.
using UnityEngine;
using UnityEngine.Rendering;

namespace Bob.TaskRace
{
    [ExecuteAlways]
    public class DioramaLightingRig : MonoBehaviour
    {
        [Tooltip("Warm golden-cream sunlight (#FFF3DC).")]
        public Color sunColour = new Color(1f, 0.953f, 0.863f);
        public float sunIntensity = 1.12f;
        [Tooltip("Under 0.5 keeps shadowed corners readable instead of navy.")]
        [Range(0f, 1f)] public float shadowStrength = 0.45f;

        public Color sky = new Color(0.98f, 0.95f, 0.88f);
        public Color equator = new Color(1f, 0.93f, 0.84f);
        public Color ground = new Color(0.74f, 0.64f, 0.54f);

        bool saved;
        AmbientMode wasMode;
        Color wasSky, wasEquator, wasGround;
        bool wasFog;
        Light wasRenderSun, wasSun;
        Color wasSunColour;
        float wasSunIntensity, wasShadowStrength;
        LightShadows wasShadows;

        void OnEnable() => Apply();

        public void Apply()
        {
            var sun = RenderSettings.sun != null ? RenderSettings.sun : FindSun();
            Save(sun);
            if (sun != null)
            {
                RenderSettings.sun = sun;
                sun.color = Lin(sunColour);
                sun.intensity = sunIntensity;
                sun.shadowStrength = shadowStrength;
                sun.shadows = LightShadows.Soft;
            }

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = Lin(sky);
            RenderSettings.ambientEquatorColor = Lin(equator);
            RenderSettings.ambientGroundColor = Lin(ground);
            RenderSettings.fog = false;
        }

        void Save(Light sun)
        {
            if (saved) return;
            saved = true;
            wasMode = RenderSettings.ambientMode;
            wasSky = RenderSettings.ambientSkyColor;
            wasEquator = RenderSettings.ambientEquatorColor;
            wasGround = RenderSettings.ambientGroundColor;
            wasFog = RenderSettings.fog;
            wasRenderSun = RenderSettings.sun;
            wasSun = sun;
            if (sun != null)
            {
                wasSunColour = sun.color;
                wasSunIntensity = sun.intensity;
                wasShadowStrength = sun.shadowStrength;
                wasShadows = sun.shadows;
            }
        }

        /// <summary>Scene unload (or the editor verify tearing the preview down) restores the lighting it found.</summary>
        void OnDisable()
        {
            if (!saved) return;
            saved = false;
            RenderSettings.ambientMode = wasMode;
            RenderSettings.ambientSkyColor = wasSky;
            RenderSettings.ambientEquatorColor = wasEquator;
            RenderSettings.ambientGroundColor = wasGround;
            RenderSettings.fog = wasFog;
            RenderSettings.sun = wasRenderSun;
            if (wasSun != null)
            {
                wasSun.color = wasSunColour;
                wasSun.intensity = wasSunIntensity;
                wasSun.shadowStrength = wasShadowStrength;
                wasSun.shadows = wasShadows;
            }
        }

        static Light FindSun()
        {
            Light best = null;
            foreach (var l in Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude))
            {
                if (l.type != LightType.Directional) continue;
                if (best == null || l.intensity > best.intensity) best = l;
            }
            return best;
        }

        static Color Lin(Color c) => QualitySettings.activeColorSpace == ColorSpace.Linear ? c.linear : c;
    }
}
