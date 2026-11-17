using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Zmywak.Core.Graphics;
using Debug = UnityEngine.Debug;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Precomputed light, worked out on the development PC's GPU each Run All and only read back on the phone, where it
    /// costs next to nothing - for the kitchen and for Kuba's room (the menu's and the hub's backdrop):
    /// - lightmaps of the light bouncing between surfaces, with contact darkness (ambient occlusion) in the corners;
    /// - in the room, the glow of the city through the window and of the lamp's shade, baked in as light sources;
    /// - a grid of light probes giving what moves (dishes, the hand) the same bounced light;
    /// - a box-projected reflection probe, so glossy things mirror the room where it really is.
    /// Lights stay real-time for what is direct (Mixed, baking only the bounce), so moving things and their shadows
    /// behave as before; a light that changes while playing (the passing car's headlights) stays fully real-time.
    /// The look stays the one <see cref="SceneLighting"/> was tuned to: the bake takes its three ambient colours, and
    /// the probe captures the room against its studio. In the kitchen, open above and in front, the steel still
    /// mirrors that studio's ceiling panel and strips. (Against Unity's default sky the counter came out navy and the
    /// machines white.)
    /// </summary>
    internal static class BakedLighting
    {
        private sealed class Setup
        {
            public string Name;
            public Vector3 RoomCentre;
            public Vector3 RoomSize;
            public float TexelsPerUnit;
            public float OcclusionDistance;
            public List<Vector3> Probes;
        }

        /// <summary>The kitchen: the counter is 6.4 x 8 units round the origin (a unit ~30 cm); dishes rise ~2 above it.</summary>
        public static void PrepareKitchen(Scene scene)
        {
            var probes = new List<Vector3>();
            foreach (float y in new[] { 0.25f, 1.1f, 2.3f })
            {
                for (float x = -3f; x <= 3.01f; x += 1f)
                {
                    for (float z = -3.6f; z <= 3.61f; z += 0.9f)
                    {
                        probes.Add(new Vector3(x, y, z));
                    }
                }
            }

            // Down in the sink's basin (centred at 0.6, -2.3), under the water: a soaking dish.
            for (float x = -0.4f; x <= 1.61f; x += 1f)
            {
                for (float z = -3f; z <= -1.59f; z += 0.7f)
                {
                    probes.Add(new Vector3(x, -0.25f, z));
                }
            }

            Prepare(scene, new Setup
            {
                Name = "Kitchen", RoomCentre = new Vector3(0f, 1.6f, 0.05f), RoomSize = new Vector3(7f, 4.6f, 8.3f),
                TexelsPerUnit = 32f, OcclusionDistance = 0.9f, Probes = probes
            });
        }

        /// <summary>Kuba's room (RoomArt), in metres: 3.4 x 3.6, 2.6 high, round the origin.</summary>
        public static void PrepareRoom(Scene scene)
        {
            var probes = new List<Vector3>();
            foreach (float y in new[] { 0.25f, 0.9f, 1.6f, 2.3f })
            {
                for (float x = -1.45f; x <= 1.46f; x += 0.48f)
                {
                    for (float z = -1.55f; z <= 1.56f; z += 0.52f)
                    {
                        probes.Add(new Vector3(x, y, z));
                    }
                }
            }

            Prepare(scene, new Setup
            {
                Name = "Room", RoomCentre = new Vector3(0f, 1.3f, 0f), RoomSize = new Vector3(3.5f, 2.7f, 3.7f),
                TexelsPerUnit = 48f, OcclusionDistance = 0.35f, Probes = probes
            });
        }

        /// <summary>
        /// Bakes the lightmaps, the light probes and the reflection probe of the saved scene, then saves it with them.
        /// Needs a GPU: a -nographics run leaves the scene unbaked (and says so).
        /// </summary>
        public static void Bake(Scene scene)
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            {
                Debug.LogWarning($"Zmywak: no graphics device (-nographics) - {scene.name}'s light was NOT baked this run.");
                return;
            }

            var timer = Stopwatch.StartNew();
            bool ok = Lightmapping.Bake();
            Debug.Log($"Zmywak: {scene.name} light baked {(ok ? "OK" : "FAILED")} in {timer.Elapsed.TotalSeconds:0} s on " +
                      $"{SystemInfo.graphicsDeviceName}: {LightmapSettings.lightmaps.Length} lightmap(s), " +
                      $"{(LightmapSettings.lightProbes != null ? LightmapSettings.lightProbes.count : 0)} light probes.");
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Gets a built scene ready to bake: its opaque static meshes take part (and are lightmapped), glowing ones light
        /// it, the lights bake their bounce, the environment is the scene's own, and the probes go in. Before saving.
        /// </summary>
        private static void Prepare(Scene scene, Setup setup)
        {
            int baked = 0;
            SceneLighting look = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
                {
                    GameObject go = renderer.gameObject;
                    StaticEditorFlags flags = GameObjectUtility.GetStaticEditorFlags(go);
                    if ((flags & StaticEditorFlags.BatchingStatic) == 0 || !IsOpaque(renderer) || renderer is not MeshRenderer)
                    {
                        continue;
                    }

                    GameObjectUtility.SetStaticEditorFlags(go, flags | StaticEditorFlags.ContributeGI | StaticEditorFlags.ReflectionProbeStatic);
                    foreach (Material material in renderer.sharedMaterials)
                    {
                        if (material.IsKeywordEnabled("_EMISSION"))
                        {
                            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive; // the city's glow, the lamp's shade
                        }
                    }

                    baked++;
                }

                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                {
                    // A light that changes while playing cannot have its bounce baked once and for all.
                    light.lightmapBakeType = light.GetComponent<LightSweep>() != null ? LightmapBakeType.Realtime : LightmapBakeType.Mixed;
                }

                look = look != null ? look : root.GetComponentInChildren<SceneLighting>(true);
            }

            if (look == null)
            {
                Debug.LogWarning($"Zmywak: the {setup.Name.ToLowerInvariant()} has no SceneLighting - its light is baked against Unity's default sky.");
            }
            else
            {
                RenderSettings.ambientMode = AmbientMode.Trilight;
                RenderSettings.ambientSkyColor = look.AmbientSky;
                RenderSettings.ambientEquatorColor = look.AmbientEquator;
                RenderSettings.ambientGroundColor = look.AmbientGround;
                RenderSettings.ambientIntensity = 1f;
                // The sky is only ever "seen" by the reflection probe's bake: the cameras clear to a solid colour, and
                // SceneLighting drops it again when the scene starts.
                RenderSettings.skybox = StudioSky(look, setup.Name);
            }

            var probesGo = new GameObject("LightProbes");
            probesGo.AddComponent<LightProbeGroup>().probePositions = setup.Probes.ToArray();

            var reflectionGo = new GameObject("RoomReflection");
            reflectionGo.transform.position = setup.RoomCentre;
            ReflectionProbe probe = reflectionGo.AddComponent<ReflectionProbe>();
            probe.mode = ReflectionProbeMode.Baked;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox; // the studio, where the room is open
            probe.boxProjection = true;
            probe.size = setup.RoomSize;
            probe.resolution = 256;
            probe.hdr = true;
            probe.importance = 1;
            probe.blendDistance = 0.5f;
            probe.intensity = look != null ? look.ReflectionIntensity : 0.8f; // as bright as SceneLighting's own reflections

            Lightmapping.lightingSettings = EnsureSettings(setup);
            Debug.Log($"Zmywak: {baked} static meshes take part in the {setup.Name.ToLowerInvariant()}'s baked light.");
        }

        private static bool IsOpaque(Renderer renderer)
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null || material.renderQueue >= (int)RenderQueue.GeometryLast)
                {
                    return false; // see-through: soft shadow decals, shading quads, water, glass - not part of the bake
                }
            }

            return true;
        }

        /// <summary>SceneLighting's studio, saved as a cubemap and worn as a skybox for the probe to capture the room against.</summary>
        private static Material StudioSky(SceneLighting look, string name)
        {
            string cubemapPath = $"Assets/_Project/Art/Textures/{name}Studio.cubemap";
            string skyPath = $"Assets/_Project/Art/Materials/{name}StudioSky.mat";
            Cubemap studio = look.BuildStudioCubemap();
            AssetDatabase.CreateAsset(studio, cubemapPath); // replaces last run's

            var sky = AssetDatabase.LoadAssetAtPath<Material>(skyPath);
            if (sky == null)
            {
                sky = new Material(Shader.Find("Skybox/Cubemap"));
                AssetDatabase.CreateAsset(sky, skyPath);
            }

            sky.SetTexture("_Tex", studio);
            sky.SetColor("_Tint", new Color(0.5f, 0.5f, 0.5f, 1f)); // neutral
            sky.SetFloat("_Exposure", 1f);
            EditorUtility.SetDirty(sky);
            return sky;
        }

        private static LightingSettings EnsureSettings(Setup setup)
        {
            string path = $"Assets/_Project/Settings/Rendering/{setup.Name}Lighting.lighting";
            var settings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            if (settings == null)
            {
                settings = new LightingSettings { name = $"{setup.Name}Lighting" };
                AssetDatabase.CreateAsset(settings, path);
            }

            settings.bakedGI = true;
            settings.realtimeGI = false; // (no autoGenerate: Unity 6 bakes only on demand)
            settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
            settings.mixedBakeMode = MixedLightingMode.IndirectOnly; // direct light and shadows stay real-time
            settings.directionalityMode = LightmapsMode.NonDirectional; // one texture per lightmap: cheap on a phone
            settings.lightmapResolution = setup.TexelsPerUnit;
            settings.lightmapPadding = 4;
            settings.lightmapMaxSize = 1024;
            settings.maxBounces = 3;
            settings.directSampleCount = 64;
            settings.indirectSampleCount = 1024; // a GPU affords it: clean bounce without blotches
            settings.environmentSampleCount = 512;
            settings.filteringMode = LightingSettings.FilterMode.Auto; // the best denoiser the GPU has
            settings.ao = true;
            settings.aoMaxDistance = setup.OcclusionDistance;
            settings.aoExponentIndirect = 1.2f;
            settings.aoExponentDirect = 0f;
            EditorUtility.SetDirty(settings);
            return settings;
        }
    }
}
