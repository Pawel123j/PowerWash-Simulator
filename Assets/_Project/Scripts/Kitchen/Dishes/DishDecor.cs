using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// The clean look of each dish, painted into the same planar-UV texture the dirt goes on: restaurant
    /// china with a green band near the rim, a steel pan with circular brushing marks, a soup bowl glazed cobalt
    /// outside with a double blue line inside, a bottle-green mug, a brushed steel pot. Without it a clean
    /// dish was a flat, featureless disc - and the moment it comes clean is the reward. Alpha is gloss on a
    /// glazed dish (1 = full shine).
    /// </summary>
    public static class DishDecor
    {
        private static readonly Color RimBand = new Color(0.14f, 0.44f, 0.3f);

        public static Color[] BuildBase(DishDefinition definition, int resolution)
        {
            var colors = new Color[resolution * resolution];
            Color clean = definition.PlaceholderCleanColor;
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    var uv = new Vector2((x + 0.5f) / resolution, (y + 0.5f) / resolution);
                    colors[y * resolution + x] = Decorate(definition.Shape, clean, uv);
                }
            }

            return colors;
        }

        private static Color Decorate(DishShape shape, Color clean, Vector2 uv)
        {
            float r = (uv - new Vector2(0.5f, 0.5f)).magnitude;
            switch (shape)
            {
                case DishShape.Plate:
                    // Band on the rim slope, softened over about a texel on each side.
                    float band = Mathf.Clamp01(1f - Mathf.Abs(r - 0.418f) / 0.018f) * 1.4f;
                    Color plate = Color.Lerp(clean, RimBand, Mathf.Clamp01(band));
                    // The well is a touch warmer than the rim, like glazed china catching the light.
                    return r < 0.3f ? plate * new Color(1f, 0.99f, 0.97f, 1f) : plate;

                case DishShape.Pan:
                    if (r > 0.56f)
                    {
                        // The outside (DishMeshFactory.ExteriorUv): duller steel with a bronze heat tint from the
                        // burners, less glossy (alpha is gloss) than the polished inside.
                        Color outside = clean * new Color(1.05f, 0.95f, 0.82f);
                        outside.a = 0.72f;
                        return outside;
                    }

                    float rings = 0.94f + 0.06f * Mathf.Sin(r * 260f) + 0.03f * Mathf.Sin(r * 71f);
                    Color pan = clean * rings;
                    pan.a = 1f;
                    // Darker, seasoned cooking surface in the middle.
                    return r < 0.4f ? Color.Lerp(pan, pan * 0.8f, 0.5f) : pan;

                case DishShape.Bowl:
                    if (r > 0.56f)
                    {
                        return Glaze(BowlOutside); // the outside (and the foot): cobalt glaze
                    }

                    // White inside with a double blue line just under the rim, as on a diner's soup bowl.
                    float lines = Mathf.Clamp01(1f - Mathf.Abs(r - 0.455f) / 0.004f) + Mathf.Clamp01(1f - Mathf.Abs(r - 0.443f) / 0.0025f);
                    return Color.Lerp(clean, BowlOutside, Mathf.Clamp01(lines) * 0.85f);

                case DishShape.Mug:
                    // A bottle-green glazed mug, white inside.
                    return r > 0.56f ? Glaze(MugOutside) : clean;

                case DishShape.Pot:
                    if (r > 0.56f)
                    {
                        Color side = clean * 1.04f; // brushed steel outside, a little brighter than the worked inside
                        side.a = 0.8f;
                        return side;
                    }

                    float brushing = 0.95f + 0.05f * Mathf.Sin(r * 300f) + 0.025f * Mathf.Sin(r * 83f);
                    Color pot = clean * brushing;
                    pot.a = 1f;
                    return r < 0.43f ? Color.Lerp(pot, pot * 0.86f, 0.4f) : pot; // the bottom, dulled by the flame

                case DishShape.Roaster:
                    // Granite-ware: navy enamel flecked with white, a shade lighter inside where the oven has dulled it.
                    // (The outside's patch is a dozen texels across - too few to fleck - so it is plain navy.)
                    float fleck = r > 0.56f ? 0f : Fleck(uv);
                    Color enamel = Color.Lerp(clean, RoasterFleck, fleck);
                    enamel = r > 0.56f ? enamel : Color.Lerp(enamel, enamel * 1.25f, 0.35f);
                    enamel.a = 0.92f;
                    return enamel;

                default:
                    return clean;
            }
        }

        private static readonly Color RoasterFleck = new Color(0.86f, 0.88f, 0.9f);

        /// <summary>White flecks scattered over enamel: 0 mostly, up to 1 on a fleck (a fixed hash, the same every time).</summary>
        private static float Fleck(Vector2 uv)
        {
            Vector2 cell = uv * 90f;
            int cx = Mathf.FloorToInt(cell.x);
            int cy = Mathf.FloorToInt(cell.y);
            uint h = (uint)(cx * 374761393 + cy * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            float pick = ((h ^ (h >> 16)) & 0xffff) / 65535f;
            if (pick < 0.82f)
            {
                return 0f;
            }

            var centre = new Vector2(cx + 0.5f, cy + 0.5f);
            return Mathf.Clamp01(1f - (cell - centre).magnitude * 2.2f) * 0.85f;
        }

        private static readonly Color BowlOutside = new Color(0.13f, 0.27f, 0.58f);
        private static readonly Color MugOutside = new Color(0.08f, 0.33f, 0.21f);

        private static Color Glaze(Color colour)
        {
            colour.a = 1f; // full gloss
            return colour;
        }
    }
}
