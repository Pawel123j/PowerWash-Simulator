using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// The stylized 3D portraits of the people Kuba meets, rendered by Tools/Blender/portraits.py into Art/Portraits
    /// (Portrait_&lt;speaker id&gt;.png, committed - nobody needs Blender to build the game). Imported here as UI sprites
    /// for the dialogue box; without them the dialogue simply shows no faces.
    /// </summary>
    internal static class PortraitArt
    {
        public const string Folder = "Assets/_Project/Art/Portraits";
        private const string Prefix = "Portrait_";

        /// <summary>Every portrait there is, by speaker id, imported as a sprite.</summary>
        public static Dictionary<string, Sprite> Load()
        {
            var byId = new Dictionary<string, Sprite>();
            if (!AssetDatabase.IsValidFolder(Folder))
            {
                Debug.LogWarning($"Zmywak: no portraits in {Folder} (Tools/Blender/portraits.py) - the dialogue shows no faces.");
                return byId;
            }

            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { Folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string name = Path.GetFileNameWithoutExtension(path);
                if (!name.StartsWith(Prefix))
                {
                    continue;
                }

                ImportAsSprite(path);
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite != null)
                {
                    byId[name.Substring(Prefix.Length)] = sprite;
                }
            }

            return byId;
        }

        private static void ImportAsSprite(string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            bool changed = importer.textureType != TextureImporterType.Sprite
                           || importer.spriteImportMode != SpriteImportMode.Single
                           || !importer.alphaIsTransparency
                           || importer.mipmapEnabled
                           || importer.maxTextureSize != 512
                           || importer.textureCompression != TextureImporterCompression.CompressedHQ;
            if (!changed)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true; // the edge of a bust against the dimmed hub, without a dark fringe
            importer.mipmapEnabled = false;      // always drawn at about its own size
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.CompressedHQ; // faces band badly at low quality
            importer.SaveAndReimport();
        }
    }
}
