using UnityEngine;

namespace Zmywak.UI.Common
{
    /// <summary>
    /// The two shapes UI made at runtime needs (the loading screen lives in no scene, so it has no sprite assets): a
    /// 9-sliceable rounded rectangle and a soft round glow, drawn once into small textures.
    /// </summary>
    public static class RuntimeSprites
    {
        private static Sprite _rounded;
        private static Sprite _glow;

        public static Sprite Rounded
        {
            get
            {
                if (_rounded == null)
                {
                    const int size = 64;
                    const float radius = 22f;
                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "RuntimeRounded" };
                    var pixels = new Color32[size * size];
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - new Vector2(size, size) * 0.5f;
                            Vector2 q = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - new Vector2(size * 0.5f - radius, size * 0.5f - radius);
                            float d = new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - radius;
                            byte a = (byte)Mathf.RoundToInt(Mathf.Clamp01(0.5f - d) * 255f);
                            pixels[y * size + x] = new Color32(255, 255, 255, a);
                        }
                    }

                    texture.SetPixels32(pixels);
                    texture.Apply(false, true);
                    _rounded = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect,
                        new Vector4(radius, radius, radius, radius));
                }

                return _rounded;
            }
        }

        public static Sprite Glow
        {
            get
            {
                if (_glow == null)
                {
                    const int size = 64;
                    var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "RuntimeGlow" };
                    var pixels = new Color32[size * size];
                    for (int y = 0; y < size; y++)
                    {
                        for (int x = 0; x < size; x++)
                        {
                            float d = (new Vector2(x + 0.5f, y + 0.5f) - new Vector2(size, size) * 0.5f).magnitude / (size * 0.5f);
                            float a = Mathf.Clamp01(1f - d);
                            pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * a * 255f));
                        }
                    }

                    texture.SetPixels32(pixels);
                    texture.Apply(false, true);
                    _glow = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
                }

                return _glow;
            }
        }
    }
}
