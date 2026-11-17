using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// The costly per-dish things, kept for the next dish instead of being made and thrown away for every one of a shift's
    /// twenty-odd: the dirt and relief textures and the pixel arrays they are drawn from (a dish leaves its own when it is
    /// destroyed, the next one takes them and redraws every texel before showing them - nothing of the old dirt survives),
    /// and the painted glaze under the dirt, which is the same for every dish of a kind and is worked out once.
    /// </summary>
    public static class DishResourcePool
    {
        private const int KeepPerKind = 6;

        private static readonly Dictionary<(int resolution, bool linear), Stack<Texture2D>> Textures =
            new Dictionary<(int, bool), Stack<Texture2D>>();

        private static readonly Dictionary<int, Stack<Color32[]>> Pixels = new Dictionary<int, Stack<Color32[]>>();
        private static readonly Dictionary<(DishDefinition, int), Color[]> Glazes = new Dictionary<(DishDefinition, int), Color[]>();

        /// <summary>Textures handed out and not yet back, and how many are waiting on the shelf - for tests.</summary>
        public static int TexturesOut { get; private set; }

        public static int TexturesKept
        {
            get
            {
                int kept = 0;
                foreach (Stack<Texture2D> stack in Textures.Values)
                {
                    kept += stack.Count;
                }

                return kept;
            }
        }

        public static Texture2D RentTexture(int resolution, bool linear, string name)
        {
            TexturesOut++;
            if (Textures.TryGetValue((resolution, linear), out Stack<Texture2D> stack))
            {
                while (stack.Count > 0)
                {
                    Texture2D kept = stack.Pop();
                    if (kept != null)
                    {
                        kept.name = name;
                        return kept;
                    }
                }
            }

            return new Texture2D(resolution, resolution, TextureFormat.RGBA32, false, linear)
            {
                name = name,
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear
            };
        }

        public static void Return(Texture2D texture, bool linear)
        {
            if (texture == null)
            {
                return;
            }

            TexturesOut = Mathf.Max(0, TexturesOut - 1);
            var key = (texture.width, linear);
            if (!Textures.TryGetValue(key, out Stack<Texture2D> stack))
            {
                Textures[key] = stack = new Stack<Texture2D>();
            }

            if (stack.Count < KeepPerKind)
            {
                stack.Push(texture);
            }
            else
            {
                Object.Destroy(texture);
            }
        }

        public static Color32[] RentPixels(int count)
        {
            if (Pixels.TryGetValue(count, out Stack<Color32[]> stack) && stack.Count > 0)
            {
                return stack.Pop();
            }

            return new Color32[count];
        }

        public static void Return(Color32[] pixels)
        {
            if (pixels == null)
            {
                return;
            }

            if (!Pixels.TryGetValue(pixels.Length, out Stack<Color32[]> stack))
            {
                Pixels[pixels.Length] = stack = new Stack<Color32[]>();
            }

            if (stack.Count < KeepPerKind * 2)
            {
                stack.Push(pixels);
            }
        }

        /// <summary>The glaze a kind of dish is painted with under its dirt (read-only: shared by every dish of the kind).</summary>
        public static Color[] Glaze(DishDefinition definition, int resolution)
        {
            if (!Glazes.TryGetValue((definition, resolution), out Color[] glaze) || glaze == null)
            {
                using var _glaze = StartupTimeline.Measure("dish: glaze painted (first dish of a kind)");
                glaze = DishDecor.BuildBase(definition, resolution);
                Glazes[(definition, resolution)] = glaze;
            }

            return glaze;
        }

        /// <summary>Forgets everything kept (tests; a new kitchen gets fresh ones as it needs them).</summary>
        public static void Clear()
        {
            foreach (Stack<Texture2D> stack in Textures.Values)
            {
                foreach (Texture2D texture in stack)
                {
                    if (texture != null)
                    {
                        Object.Destroy(texture);
                    }
                }
            }

            Textures.Clear();
            Pixels.Clear();
            Glazes.Clear();
            TexturesOut = 0;
        }
    }
}
