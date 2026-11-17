using UnityEngine;
using UnityEngine.Rendering;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Core.Graphics
{
    /// <summary>
    /// Ambient light and reflections set from code on scene start: the fallback (Unity's default sky) tinted
    /// every placeholder surface teal. A three-colour ambient plus a small HDR "studio" cubemap for reflections
    /// gives glossy china and the steel pan something warm to reflect. In the kitchen, whose light is baked
    /// (KitchenLighting), the bake uses the same three colours and its reflection probe captures the room against
    /// this same studio; this then only covers what lies outside the probe.
    /// </summary>
    public class SceneLighting : MonoBehaviour
    {
        [SerializeField] private Color skyColor = new Color(0.78f, 0.8f, 0.86f);
        [SerializeField] private Color equatorColor = new Color(0.62f, 0.58f, 0.54f);
        [SerializeField] private Color groundColor = new Color(0.3f, 0.27f, 0.24f);
        [SerializeField] private float ambientIntensity = 1f;
        [SerializeField] private float reflectionIntensity = 0.8f;

        [Header("Reflected lights (what steel, glaze and water shine with)")]
        [SerializeField] private Color panelColor = new Color(1f, 0.96f, 0.88f);
        [SerializeField] private float panelIntensity = 0.35f;
        [SerializeField] private Color stripColor = new Color(1f, 0.98f, 0.94f);
        [SerializeField] private float stripIntensity = 1.8f;
        [SerializeField] private Color windowColor = new Color(0.86f, 0.92f, 1f);
        [SerializeField] private float windowIntensity = 1.5f;

        private const int CubemapSize = 128;
        private Cubemap _reflection;

        // What the editor's light bake (in a scene that has one) takes over, so the baked light matches this.
        public Color AmbientSky => skyColor * ambientIntensity;
        public Color AmbientEquator => equatorColor * ambientIntensity;
        public Color AmbientGround => groundColor * ambientIntensity;
        public float ReflectionIntensity => reflectionIntensity;

        /// <summary>The studio around the scene as a new cubemap - for a reflection probe to be baked against.</summary>
        public Cubemap BuildStudioCubemap() => BuildGradientCubemap();

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("SceneLighting.Awake");
            Apply();
        }

        private void OnDestroy()
        {
            if (_reflection != null)
            {
                Destroy(_reflection);
            }
        }

        public void Apply()
        {
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = skyColor * ambientIntensity;
            RenderSettings.ambientEquatorColor = equatorColor * ambientIntensity;
            RenderSettings.ambientGroundColor = groundColor * ambientIntensity;

            // Lit shaders read ambient from the spherical-harmonics probe, which is normally produced by a
            // lighting bake - build it directly from the three colours instead.
            var probe = new SphericalHarmonicsL2();
            probe.AddAmbientLight(equatorColor * ambientIntensity);
            probe.AddDirectionalLight(Vector3.up, (skyColor - equatorColor) * ambientIntensity, 1.2f);
            probe.AddDirectionalLight(Vector3.down, (groundColor - equatorColor) * ambientIntensity, 1.2f);
            RenderSettings.ambientProbe = probe;

            if (_reflection == null)
            {
                _reflection = BuildGradientCubemap();
            }

            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = _reflection;
            RenderSettings.reflectionIntensity = reflectionIntensity;
        }

        /// <summary>
        /// A small "studio" around the kitchen for reflections, in HDR: the room's gradient, a soft ceiling
        /// panel, fluorescent strips and a window. The kitchen camera looks down at 55 degrees through a long
        /// lens, so the flat counter reflects directions within about 12 degrees of "up and towards the wall":
        /// the panel sits there, the strips cross it as bands of sheen, and the brushed steel's normal map spreads
        /// them into streaks. Rounded edges, the tap, the rack's wires and the dishes' glaze look in every other
        /// direction and catch the strips and the window. Mipmapped, so rougher surfaces see it blurred, as they should.
        /// </summary>
        private Cubemap BuildGradientCubemap()
        {
            var cubemap = new Cubemap(CubemapSize, TextureFormat.RGBAHalf, true) { name = "SceneLightingReflection" };
            var pixels = new Color[CubemapSize * CubemapSize];
            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < CubemapSize; y++)
                {
                    for (int x = 0; x < CubemapSize; x++)
                    {
                        Vector3 dir = FaceDirection((CubemapFace)face, (x + 0.5f) / CubemapSize * 2f - 1f, (y + 0.5f) / CubemapSize * 2f - 1f);
                        pixels[y * CubemapSize + x] = Studio(dir);
                    }
                }

                cubemap.SetPixels(pixels, (CubemapFace)face);
            }

            cubemap.Apply(true);
            return cubemap;
        }

        private Color Studio(Vector3 dir)
        {
            Color room = dir.y >= 0f
                ? Color.Lerp(equatorColor, skyColor * 1.2f, Mathf.Pow(dir.y, 0.6f))
                : Color.Lerp(equatorColor, groundColor, Mathf.Pow(-dir.y, 0.6f));

            float elevation = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
            float azimuth = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg; // 0 = towards the tiled wall, 90 = right

            // Ceiling softbox: a gentle glow around what the counter mirrors (the camera looks down at 55 degrees).
            float panel = Band(elevation, 44f, 70f, 7f) * Band(azimuth, -38f, 38f, 12f);
            // Fluorescent strips across the ceiling, thin and bright - two either side of the counter's mirror
            // direction, which the brushing tilts the reflection onto in streaks, and two further out for rounded edges.
            float strips = (Band(elevation, 49f, 50.5f, 1f) + Band(elevation, 60f, 61.5f, 1f) +
                            Band(elevation, 73f, 76f, 1.2f) + Band(elevation, 29f, 31.5f, 1.2f)) * Band(azimuth, -55f, 55f, 6f);
            // A window on the right, low: daylight glints on the left-facing sides of things.
            float window = Band(elevation, 6f, 34f, 4f) * Band(azimuth, 62f, 112f, 5f);

            return room + panelColor * (panel * panelIntensity) + stripColor * (strips * stripIntensity) + windowColor * (window * windowIntensity);
        }

        /// <summary>1 inside [from, to], fading to 0 over <paramref name="soft"/> outside it.</summary>
        private static float Band(float value, float from, float to, float soft)
        {
            float inside = Mathf.Min(value - from, to - value);
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(inside / soft + 0.5f));
        }

        private static Vector3 FaceDirection(CubemapFace face, float u, float v)
        {
            switch (face)
            {
                case CubemapFace.PositiveX: return new Vector3(1f, -v, -u).normalized;
                case CubemapFace.NegativeX: return new Vector3(-1f, -v, u).normalized;
                case CubemapFace.PositiveY: return new Vector3(u, 1f, v).normalized;
                case CubemapFace.NegativeY: return new Vector3(u, -1f, -v).normalized;
                case CubemapFace.PositiveZ: return new Vector3(u, -v, 1f).normalized;
                default: return new Vector3(-u, -v, -1f).normalized;
            }
        }
    }
}
