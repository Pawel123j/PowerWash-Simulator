using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// What the Blender scripts in Tools/Blender model, read back from the JSON they leave in Art/Source and turned into
    /// mesh and texture assets. The JSON is committed, so nobody needs Blender to build the game; without it, the
    /// game falls back to what it generates itself.
    /// </summary>
    internal static class BlenderArt
    {
        private const string SourceFolder = "Assets/_Project/Art/Source";
        private const string MeshFolder = "Assets/_Project/Art/Meshes";
        private const string PalettePath = "Assets/_Project/Art/Textures/LeftoversPalette_Food.png";

        // The palette texture: a column per food, each this many texels wide (sampled at its middle, so neighbours
        // never bleed in), and rows from occluded to lit.
        private const int ColumnWidth = 8;
        private const int ShadeRows = 32;

        [System.Serializable]
        private class FoodSource
        {
            public float[] vertices;
            public float[] normals;
            public float[] uv;
            public int[] triangles;
            public float[] palette; // r, g, b per food
        }

        /// <summary>
        /// The scraps on a dish of this id (Tools/Blender/leftovers.py: LeftoversPlate.json and so on) as a mesh, with
        /// the palette it is coloured from; false (both null) when there is no such file.
        /// </summary>
        public static bool Leftovers(string dishId, out Mesh mesh, out Texture2D palette)
        {
            mesh = null;
            palette = null;
            string name = "Leftovers" + char.ToUpperInvariant(dishId[0]) + dishId.Substring(1);
            string source = $"{SourceFolder}/{name}.json";
            if (!File.Exists(source))
            {
                return false;
            }

            var food = JsonUtility.FromJson<FoodSource>(File.ReadAllText(source));
            int count = food.vertices.Length / 3;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            for (int i = 0; i < count; i++)
            {
                vertices[i] = new Vector3(food.vertices[i * 3], food.vertices[i * 3 + 1], food.vertices[i * 3 + 2]);
                normals[i] = new Vector3(food.normals[i * 3], food.normals[i * 3 + 1], food.normals[i * 3 + 2]);
                // Kept off the very top and bottom rows: bilinear filtering there would reach past the texture's edge.
                uv[i] = new Vector2(food.uv[i * 2], Mathf.Lerp(0.5f / ShadeRows, 1f - 0.5f / ShadeRows, food.uv[i * 2 + 1]));
            }

            var built = new Mesh { name = name };
            built.indexFormat = count > 65535 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            built.vertices = vertices;
            built.normals = normals;
            built.uv = uv;
            built.triangles = food.triangles;
            built.RecalculateBounds();
            Directory.CreateDirectory(MeshFolder);
            mesh = KitchenArt.SaveMesh(built, $"{MeshFolder}/{name}.asset");
            palette = EnsurePalette(food.palette);
            return true;
        }

        /// <summary>
        /// Each food's colour from deep in a crevice (dark, a touch richer) to where the light falls full on it. Written
        /// only when it differs from what is there, so an unchanged palette leaves git alone.
        /// </summary>
        private static Texture2D EnsurePalette(float[] colours)
        {
            int foods = colours.Length / 3;
            var texture = new Texture2D(foods * ColumnWidth, ShadeRows, TextureFormat.RGBA32, false);
            var pixels = new Color[texture.width * texture.height];
            for (int f = 0; f < foods; f++)
            {
                var food = new Color(colours[f * 3], colours[f * 3 + 1], colours[f * 3 + 2]);
                float grey = food.grayscale;
                for (int row = 0; row < ShadeRows; row++)
                {
                    float shade = row / (ShadeRows - 1f);
                    // In the dark the colour deepens (more saturated); in full light it stays well under the food's
                    // own colour, as the dishes' do - the key light and the sink lamp together blew it out to cream.
                    Color rich = Color.LerpUnclamped(new Color(grey, grey, grey), food, 1.1f);
                    Color c = Color.Lerp(rich * 0.2f, food * 0.68f, Mathf.SmoothStep(0f, 1f, shade));
                    c.a = 1f;
                    for (int x = 0; x < ColumnWidth; x++)
                    {
                        pixels[row * texture.width + f * ColumnWidth + x] = c;
                    }
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false);
            byte[] png = texture.EncodeToPNG();
            Object.DestroyImmediate(texture);
            Directory.CreateDirectory(Path.GetDirectoryName(PalettePath));
            if (!File.Exists(PalettePath) || !SameBytes(File.ReadAllBytes(PalettePath), png))
            {
                File.WriteAllBytes(PalettePath, png);
                AssetDatabase.ImportAsset(PalettePath);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(PalettePath);
            if (importer.mipmapEnabled || importer.wrapMode != TextureWrapMode.Clamp || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.mipmapEnabled = false; // mipmaps would blend the columns together on a distant dish
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed; // tiny, and block compression smears the columns
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(PalettePath);
        }

        private static bool SameBytes(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
            {
                return false;
            }

            for (int i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
