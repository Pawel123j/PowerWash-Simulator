using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Particle textures (soft puff, soap bubble, sparkle, ring, ceramic shard) drawn in code and the URP
    /// "Particles/Unlit" materials that use them - alpha-blended for foam, crumbs, shards, steam, and the
    /// "clean!" stars and ring (additive light vanishes against a white plate), additive for water glints. Materials are set up through URP's own ShaderGUI helpers,
    /// exactly as the material inspector would, so blend state and keywords are consistent.
    /// </summary>
    internal static class FxArt
    {
        private const string TextureFolder = "Assets/_Project/Art/Textures";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";
        private const int Size = 64;

        public static Material Soft => Load("Fx_Soft");
        public static Material Bubble => Load("Fx_Bubble");
        public static Material Glow => Load("Fx_Glow");
        public static Material Sparkle => Load("Fx_Sparkle");
        public static Material Shard => Load("Fx_Shard");
        public static Material Ring => Load("Fx_Ring");

        public static void EnsureAssets()
        {
            Texture2D soft = EnsureTexture("Fx_SoftCircle", (x, y) =>
            {
                float r = Radius(x, y);
                float a = Mathf.Clamp01(1f - r);
                return new Color(1f, 1f, 1f, a * a * (3f - 2f * a));
            });

            Texture2D bubble = EnsureTexture("Fx_Bubble", (x, y) =>
            {
                float r = Radius(x, y);
                if (r > 1f)
                {
                    return new Color(1f, 1f, 1f, 0f);
                }

                // Thin bright rim, faint film inside, a highlight top-left.
                float rim = Mathf.Clamp01(1f - Mathf.Abs(r - 0.88f) / 0.12f);
                float film = 0.18f;
                var p = new Vector2(x, y) / (Size - 1f) * 2f - Vector2.one;
                float highlight = Mathf.Clamp01(1f - (p - new Vector2(-0.35f, 0.4f)).magnitude / 0.28f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(Mathf.Max(film, rim * 0.95f) + highlight * 0.9f));
            });

            Texture2D sparkle = EnsureTexture("Fx_Sparkle", (x, y) =>
            {
                var p = new Vector2(x, y) / (Size - 1f) * 2f - Vector2.one;
                float r = p.magnitude;
                // Four-point star: thin rays along the axes over a small glow.
                float rays = Mathf.Max(Mathf.Clamp01(1f - Mathf.Abs(p.x) * 14f) * (1f - Mathf.Abs(p.y)),
                                       Mathf.Clamp01(1f - Mathf.Abs(p.y) * 14f) * (1f - Mathf.Abs(p.x)));
                float glow = Mathf.Clamp01(1f - r * 2.2f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(rays + glow * glow));
            });

            Texture2D ring = EnsureTexture("Fx_Ring", (x, y) =>
            {
                float r = Radius(x, y);
                // A crisp band near the edge with a faint wash inside, so the ripple reads as a surface wave.
                float band = Mathf.Clamp01(1f - Mathf.Abs(r - 0.86f) / 0.1f);
                float wash = r < 0.86f ? 0.12f * r : 0f;
                return new Color(1f, 1f, 1f, Mathf.Clamp01(band * band * (3f - 2f * band) + wash));
            });

            Texture2D shard = EnsureTexture("Fx_Shard", (x, y) =>
            {
                var p = new Vector2(x, y) / (Size - 1f);
                // An irregular triangle, lighter towards one edge like a glazed fragment.
                bool inside = Edge(p, new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.3f)) >= 0f &&
                              Edge(p, new Vector2(0.9f, 0.3f), new Vector2(0.35f, 0.92f)) >= 0f &&
                              Edge(p, new Vector2(0.35f, 0.92f), new Vector2(0.1f, 0.15f)) >= 0f;
                if (!inside)
                {
                    return new Color(1f, 1f, 1f, 0f);
                }

                float shade = 0.75f + 0.25f * p.y;
                return new Color(shade, shade, shade, 1f);
            });

            EnsureParticleMaterial("Fx_Soft", soft, additive: false);
            EnsureParticleMaterial("Fx_Bubble", bubble, additive: false);
            EnsureParticleMaterial("Fx_Glow", soft, additive: true);
            EnsureParticleMaterial("Fx_Sparkle", sparkle, additive: false);
            EnsureParticleMaterial("Fx_Ring", ring, additive: false);
            EnsureParticleMaterial("Fx_Shard", shard, additive: false);
            AssetDatabase.SaveAssets();
        }

        private static float Radius(int x, int y)
        {
            var p = new Vector2(x, y) / (Size - 1f) * 2f - Vector2.one;
            return p.magnitude;
        }

        /// <summary>Which side of the edge a→b point p lies on (positive = left, counter-clockwise).</summary>
        private static float Edge(Vector2 p, Vector2 a, Vector2 b)
        {
            return (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        }

        private static Texture2D EnsureTexture(string name, System.Func<int, int, Color> pixel)
        {
            string path = $"{TextureFolder}/{name}.png";
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(TextureFolder);
                var texture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
                var pixels = new Color[Size * Size];
                for (int y = 0; y < Size; y++)
                {
                    for (int x = 0; x < Size; x++)
                    {
                        pixels[y * Size + x] = pixel(x, y);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (!importer.alphaIsTransparency || importer.wrapMode != TextureWrapMode.Clamp)
            {
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.mipmapEnabled = true;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static void EnsureParticleMaterial(string name, Texture2D texture, bool additive)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Directory.CreateDirectory(MaterialFolder);
                material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                AssetDatabase.CreateAsset(material, path);
            }

            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f); // transparent
            material.SetFloat("_Blend", additive ? 2f : 0f); // 2 = additive, 0 = alpha
            BaseShaderGUI.SetMaterialKeywords(material, null, ParticleGUI.SetMaterialKeywords);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            EditorUtility.SetDirty(material);
        }

        private static Material Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/{name}.mat");
        }
    }
}
