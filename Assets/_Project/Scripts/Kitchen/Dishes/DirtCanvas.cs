using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// Per-dish paintable dirt: one low-resolution mask per dirt layer (0 = clean, 1 = thickest dirt),
    /// laid out in the dish's planar UV space (see <see cref="DishMeshFactory"/>). Scrubbing removes dirt
    /// only under the brush, so the player has to actually rub the whole dish clean. Plain C# (no Unity
    /// objects) so it can be unit tested; <see cref="DishController"/> turns it into a texture.
    /// </summary>
    public class DirtCanvas
    {
        // 64 looked blocky on a dish enlarged in the sink, and at 96 the frayed edges of the dirt still showed texel steps.
        // 128 was soft on a phone, where a plate in the sink spans ~700 px: 256 draws the spatters, the rim's line and
        // the edges of the dirt sharp. Scrubbing only redraws what changed (see TakeChangedRegion), so it costs no
        // more a frame than 128 did.
        public const int DefaultResolution = 256;

        /// <summary>The resolution the per-texel looks (bead size, relief slope, grain) were tuned at; they scale from it.</summary>
        private const int TunedResolution = 128;

        private const float NoiseFrequency = 4.5f;
        private const float EdgeSoftness = 0.06f;

        private readonly int _resolution;
        private readonly bool[] _inside;
        private readonly int _insideCount;
        private readonly float[][] _layers;
        private readonly float[] _initialSums;
        private readonly float[] _currentSums;

        public int Resolution => _resolution;
        public int LayerCount => _layers.Length;

        /// <summary>Rinsed: beads of water stand on the clean glaze (drawn by <see cref="WriteNormals"/>; dirt hides them).</summary>
        public bool Wet { get; set; }

        private readonly int _seed;
        private readonly Vector2? _wallBand;
        private readonly float[] _wall; // WallWeight per texel, or null without a band
        private float[] _beads;

        /// <summary>How much of the fine grain a texel keeps: on the wall band most of it goes, since anything that varies
        /// round the ring is drawn out into streaks down the wall.</summary>
        private float GrainShare(int texel) => _wall != null ? 1f - 0.8f * _wall[texel] : 1f;

        /// <summary>
        /// 0 (the default) for a glazed dish: the written alpha is gloss. Above 0 the dish is see-through (a glass
        /// on a transparent material) and alpha is opacity instead - this much where it is clean; dirt clouds it
        /// and the reflection held to the light makes it solid enough to read.
        /// </summary>
        public float CleanOpacity { get; set; }

        /// <param name="coverages">Fraction of the dish surface each layer covers when it arrives (0-1).</param>
        /// <param name="seed">Varies the dirt pattern between dishes of the same type.</param>
        /// <param name="circular">Only the disc inscribed in UV space is dish surface (cylinders).</param>
        /// <param name="patterns">Per layer: splotches (default) or thin streaks.</param>
        /// <param name="wallBand">The ring (from, to radius in UV) a tall dish's walls are projected onto, where
        /// splotches become tide lines round the wall (see <see cref="DishMeshFactory.WallBand"/>); null for none.</param>
        public DirtCanvas(IReadOnlyList<float> coverages, int seed, bool circular, int resolution = DefaultResolution,
            IReadOnlyList<DirtPattern> patterns = null, Vector2? wallBand = null)
        {
            _resolution = Mathf.Max(4, resolution);
            _seed = seed;
            _wallBand = wallBand;
            int texelCount = _resolution * _resolution;
            if (wallBand.HasValue)
            {
                _wall = new float[texelCount];
                for (int i = 0; i < texelCount; i++)
                {
                    _wall[i] = WallWeight((TexelCenter(i % _resolution, i / _resolution) - new Vector2(0.5f, 0.5f)).magnitude);
                }
            }

            _inside = new bool[texelCount];
            // A texel's worth of margin so the rim (which the dish's side faces sample) is paintable too.
            float insideRadius = 0.5f + 1f / _resolution;
            for (int y = 0; y < _resolution; y++)
            {
                for (int x = 0; x < _resolution; x++)
                {
                    Vector2 uv = TexelCenter(x, y);
                    bool inside = !circular || (uv - new Vector2(0.5f, 0.5f)).magnitude <= insideRadius;
                    _inside[y * _resolution + x] = inside;
                    if (inside)
                    {
                        _insideCount++;
                    }
                }
            }

            int layerCount = coverages?.Count ?? 0;
            _layers = new float[layerCount][];
            _initialSums = new float[layerCount];
            _currentSums = new float[layerCount];
            var random = new System.Random(seed);
            for (int i = 0; i < layerCount; i++)
            {
                bool streaks = patterns != null && i < patterns.Count && patterns[i] == DirtPattern.Streaks;
                _layers[i] = streaks
                    ? GenerateStreaks(Mathf.Clamp01(coverages[i]), random)
                    : GeneratePattern(Mathf.Clamp01(coverages[i]), random);
                _initialSums[i] = Sum(_layers[i]);
                _currentSums[i] = _initialSums[i];
            }
        }

        // The texels whose dirt changed since TakeChangedRegion last looked (min > max: none).
        private int _changedMinX = int.MaxValue;
        private int _changedMinY = int.MaxValue;
        private int _changedMaxX = -1;
        private int _changedMaxY = -1;

        private void MarkChanged(int minX, int minY, int maxX, int maxY)
        {
            _changedMinX = Mathf.Min(_changedMinX, minX);
            _changedMinY = Mathf.Min(_changedMinY, minY);
            _changedMaxX = Mathf.Max(_changedMaxX, maxX);
            _changedMaxY = Mathf.Max(_changedMaxY, maxY);
        }

        /// <summary>
        /// The texels whose dirt has changed since this was last asked (it then forgets them), for redrawing only those:
        /// a scrub touches a brush-sized patch, not the whole dish. False when nothing changed.
        /// </summary>
        public bool TakeChangedRegion(out RectInt region)
        {
            if (_changedMaxX < 0)
            {
                region = default;
                return false;
            }

            region = new RectInt(_changedMinX, _changedMinY, _changedMaxX - _changedMinX + 1, _changedMaxY - _changedMinY + 1);
            _changedMinX = _changedMinY = int.MaxValue;
            _changedMaxX = _changedMaxY = -1;
            return true;
        }

        public RectInt Whole => new RectInt(0, 0, _resolution, _resolution);

        /// <summary>How much of the layer's original dirt is left, 0-1 (1 = untouched).</summary>
        public float RemainingFraction(int layer)
        {
            return _initialSums[layer] > 0f ? Mathf.Clamp01(_currentSums[layer] / _initialSums[layer]) : 0f;
        }

        /// <summary>
        /// Whether any texel of the layer is thicker than <paramref name="floor"/>: dirt a tool that cannot go below that thickness
        /// (the scraper, the spray: see <see cref="DirtMaskMath.ApplyScrub"/>) can still take off. A little slack, because a texel
        /// scrubbed down to the floor sits at it exactly.
        /// </summary>
        public bool HasDirtAbove(int layer, float floor)
        {
            float[] texels = _layers[layer];
            float limit = floor + 1e-4f;
            for (int i = 0; i < texels.Length; i++)
            {
                if (texels[i] > limit)
                {
                    return true;
                }
            }

            return false;
        }

        public float DirtAt(int layer, Vector2 uv)
        {
            int x = Mathf.Clamp(Mathf.FloorToInt(uv.x * _resolution), 0, _resolution - 1);
            int y = Mathf.Clamp(Mathf.FloorToInt(uv.y * _resolution), 0, _resolution - 1);
            return _layers[layer][y * _resolution + x];
        }

        /// <summary>
        /// Removes dirt under a round brush centred on <paramref name="uv"/>, strongest in the middle.
        /// Each layer comes off at the rate its tool power / dirt difficulty allows. Returns true if any
        /// dirt was actually removed.
        /// </summary>
        /// <param name="floor">How thin the tool can get the dirt (see <see cref="DirtMaskMath.ApplyScrub"/>); 0 = all of it.</param>
        public bool ScrubAt(Vector2 uv, float radius, IReadOnlyList<float> powerPerLayer, IReadOnlyList<float> difficultyPerLayer, float deltaTime,
            float floor = 0f)
        {
            if (radius <= 0f || deltaTime <= 0f)
            {
                return false;
            }

            int minX = Mathf.Max(0, Mathf.FloorToInt((uv.x - radius) * _resolution));
            int maxX = Mathf.Min(_resolution - 1, Mathf.FloorToInt((uv.x + radius) * _resolution));
            int minY = Mathf.Max(0, Mathf.FloorToInt((uv.y - radius) * _resolution));
            int maxY = Mathf.Min(_resolution - 1, Mathf.FloorToInt((uv.y + radius) * _resolution));
            float radiusSqr = radius * radius;
            bool changed = false;

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int index = y * _resolution + x;
                    if (!_inside[index])
                    {
                        continue;
                    }

                    float distSqr = (TexelCenter(x, y) - uv).sqrMagnitude;
                    if (distSqr > radiusSqr)
                    {
                        continue;
                    }

                    float falloff = 1f - distSqr / radiusSqr;
                    for (int layer = 0; layer < _layers.Length; layer++)
                    {
                        float before = _layers[layer][index];
                        if (before <= 0f)
                        {
                            continue;
                        }

                        float after = DirtMaskMath.ApplyScrub(before, powerPerLayer[layer] * falloff, deltaTime, difficultyPerLayer[layer], floor);
                        if (after < before)
                        {
                            _layers[layer][index] = after;
                            _currentSums[layer] -= before - after;
                            changed = true;
                        }
                    }
                }
            }

            if (changed)
            {
                MarkChanged(minX, minY, maxX, maxY);
            }

            return changed;
        }

        public void ClearLayer(int layer)
        {
            System.Array.Clear(_layers[layer], 0, _layers[layer].Length);
            _currentSums[layer] = 0f;
            MarkChanged(0, 0, _resolution - 1, _resolution - 1);
        }

        /// <summary>
        /// Composites the layers over the clean surface colour (first layer drawn on top). Alpha carries
        /// gloss for the dish material: 1 on clean glaze, down to <see cref="DirtGloss"/> under thick dirt.
        /// </summary>
        /// <param name="cleanPerTexel">The clean dish per texel (its alpha is the glaze's gloss); null = <paramref name="clean"/> everywhere.</param>
        /// <param name="hiddenLayers">Layers that only show in the reflection (<paramref name="light"/>); null = none.</param>
        /// <param name="light">The reflection while the dish is held to the light, or null.</param>
        /// <param name="layerGloss">How glossy each layer is at full thickness; null = <see cref="DirtGloss"/> for all.</param>
        /// <param name="region">Only these texels (the rest of <paramref name="pixels"/> is left as it was); null = all.</param>
        public void WriteColors(Color32[] pixels, Color clean, IReadOnlyList<Color> layerColors, IReadOnlyList<Color> cleanPerTexel = null,
            IReadOnlyList<bool> hiddenLayers = null, InspectionLight light = null, IReadOnlyList<float> layerGloss = null,
            RectInt? region = null)
        {
            bool seeThrough = CleanOpacity > 0f;
            RectInt area = Clip(region);
            for (int y = area.yMin; y < area.yMax; y++)
            for (int x = area.xMin; x < area.xMax; x++)
            {
                int i = y * _resolution + x;
                if (i >= pixels.Length)
                {
                    continue;
                }

                Color c = cleanPerTexel != null ? cleanPerTexel[i] : clean;
                c.a = seeThrough ? CleanOpacity : cleanPerTexel != null ? cleanPerTexel[i].a : 1f;

                // Held to the light, the glass reads dim and fairly matte (so the kitchen's key light does not
                // glare over it) with one bright, glossy reflection - which is where the streaks show up.
                float reveal = 0f;
                if (light != null)
                {
                    reveal = light.Intensity(TexelCenter(x, y));
                    c = Color.Lerp(c * InspectionDim, ReflectionSheen, reveal);
                    c.a = seeThrough
                        ? Mathf.Lerp(CleanOpacity, SheenOpacity, reveal)
                        : Mathf.Lerp(InspectionGloss, 1f, reveal);
                }

                for (int layer = _layers.Length - 1; layer >= 0; layer--)
                {
                    float amount = _layers[layer][i];
                    bool hidden = hiddenLayers != null && layer < hiddenLayers.Count && hiddenLayers[layer];
                    if (hidden && reveal <= 0f)
                    {
                        continue; // streaks are invisible outside the reflection
                    }

                    if (amount > 0f)
                    {
                        Color dirt = layerColors[layer];
                        // The dirt colour's alpha is how opaque the layer is at full thickness: a greasy
                        // film shows the dish through it, burnt crust doesn't. Hidden streaks only break
                        // up the reflection, as strongly as it shines on them.
                        float t = Mathf.Min(1f, amount * 1.1f) * dirt.a * (hidden ? reveal : 1f);
                        // Thicker crust reads darker in the middle of a splotch, and no dirt is one flat
                        // colour: a fixed grain, different for each layer, breaks it into specks and clumps,
                        // and the grain read at another offset shifts it warmer and cooler (cooked darker here, paler there).
                        dirt = Color.Lerp(dirt, dirt * 0.7f, amount * amount);
                        float share = GrainShare(i);
                        float grain = 1f + DirtGrain * GrainAt(i, layer) * share;
                        float warmth = WarmthShift * GrainAt(i, layer + 7) * share;
                        dirt.r *= grain * (1f + warmth);
                        dirt.g *= grain;
                        dirt.b *= grain * (1f - warmth);
                        // What the layer pulls the alpha towards: its own gloss on a glaze; on a glass the grime is what you see.
                        float dirtAlpha = seeThrough ? 1f : layerGloss != null && layer < layerGloss.Count ? layerGloss[layer] : DirtGloss;
                        float alpha = Mathf.Lerp(c.a, dirtAlpha, t);
                        c = Color.Lerp(c, dirt, t);
                        c.a = alpha;
                    }
                }

                pixels[i] = c;
            }
        }

        /// <summary>
        /// The dirt as relief, for the dish's normal map (tangent space along the planar UVs; RGB = XYZ, alpha 1, which
        /// URP unpacks the same on phones and in the Editor): each layer stands off the dish by its thickness times
        /// how raised that kind of dirt is (<paramref name="relief"/>), roughened by the same grain as its colour, so
        /// a crust's edge and lumps catch the light and a flat film barely does. Clean glaze is flat - or, once the
        /// dish is <see cref="Wet"/>, beaded with water.
        /// </summary>
        /// <param name="strength">Slope for a full relief step from one texel to the next, at the resolution the looks were
        /// tuned at (128); finer canvases scale it, so the dirt stands up as far off the dish whatever the resolution.</param>
        /// <param name="region">Only the relief of these texels changed (their normals and their neighbours' are redrawn);
        /// null = all.</param>
        public void WriteNormals(Color32[] normals, IReadOnlyList<float> relief, float strength, RectInt? region = null)
        {
            bool all = _height == null || !region.HasValue;
            var height = _height ??= new float[_resolution * _resolution];
            float[] beads = Wet ? Beads() : null;
            RectInt changed = all ? Whole : Clip(region);
            strength *= (float)_resolution / TunedResolution;
            for (int y = changed.yMin; y < changed.yMax; y++)
            for (int x = changed.xMin; x < changed.xMax; x++)
            {
                int i = y * _resolution + x;
                float h = 0f;
                float dirt = 0f;
                for (int layer = 0; layer < _layers.Length; layer++)
                {
                    float amount = _layers[layer][i];
                    dirt += amount;
                    if (amount > 0f && layer < relief.Count)
                    {
                        h += amount * relief[layer] * (1f + ReliefGrain * GrainAt(i, layer) * GrainShare(i));
                    }
                }

                if (beads != null)
                {
                    // Water beads only on bare glaze: under dirt they are soaked up, so they show as it comes off.
                    h += BeadHeight * beads[i] * Mathf.Clamp01(1f - dirt * 4f);
                }

                height[i] = h;
            }

            // A texel's normal comes from its neighbours' heights: one texel round the change is redrawn too.
            RectInt redraw = all ? Whole : Clip(new RectInt(changed.x - 1, changed.y - 1, changed.width + 2, changed.height + 2));
            for (int y = redraw.yMin; y < redraw.yMax; y++)
            {
                int up = Mathf.Min(y + 1, _resolution - 1) * _resolution;
                int down = Mathf.Max(y - 1, 0) * _resolution;
                int row = y * _resolution;
                for (int x = redraw.xMin; x < redraw.xMax; x++)
                {
                    int right = Mathf.Min(x + 1, _resolution - 1);
                    int left = Mathf.Max(x - 1, 0);
                    float dx = (height[row + right] - height[row + left]) * 0.5f * strength;
                    float dy = (height[up + x] - height[down + x]) * 0.5f * strength;
                    float length = Mathf.Sqrt(dx * dx + dy * dy + 1f);
                    normals[row + x] = new Color32(
                        (byte)Mathf.RoundToInt((-dx / length * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((-dy / length * 0.5f + 0.5f) * 255f),
                        (byte)Mathf.RoundToInt((1f / length * 0.5f + 0.5f) * 255f),
                        255);
                }
            }
        }

        private float[] _height;

        /// <summary>Water beads left by the rinse: small domes (1-3 texels) scattered over the dish, the same for a
        /// dish each time (from its seed). Built the first time the dish is wet.</summary>
        private float[] Beads()
        {
            if (_beads != null)
            {
                return _beads;
            }

            _beads = new float[_resolution * _resolution];
            var random = new System.Random(_seed ^ 0x5bd1e995);
            for (int b = 0; b < BeadCount; b++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float fromCentre = 0.46f * Mathf.Sqrt((float)random.NextDouble());
                float cx = (0.5f + Mathf.Cos(angle) * fromCentre) * _resolution;
                float cy = (0.5f + Mathf.Sin(angle) * fromCentre) * _resolution;
                // Mostly small: 1.2-3 texels at the tuned resolution, the same size on the dish at any other.
                float radius = (1.2f + 1.8f * (float)random.NextDouble() * (float)random.NextDouble()) * _resolution / TunedResolution;
                for (int y = Mathf.Max(0, (int)(cy - radius)); y <= Mathf.Min(_resolution - 1, (int)(cy + radius) + 1); y++)
                {
                    for (int x = Mathf.Max(0, (int)(cx - radius)); x <= Mathf.Min(_resolution - 1, (int)(cx + radius) + 1); x++)
                    {
                        float dx = x + 0.5f - cx;
                        float dy = y + 0.5f - cy;
                        float d = (dx * dx + dy * dy) / (radius * radius);
                        if (d < 1f)
                        {
                            int index = y * _resolution + x;
                            _beads[index] = Mathf.Max(_beads[index], Mathf.Sqrt(1f - d));
                        }
                    }
                }
            }

            return _beads;
        }

        private const int BeadCount = 36;
        private const float BeadHeight = 0.65f; // taller beads read as dark pits on steel

        public const float DirtGloss = 0.2f;

        private const float DirtGrain = 0.22f; // how far the grain lightens or darkens dirt
        private const float WarmthShift = 0.12f; // how far the coarse grain pushes dirt warmer or cooler
        private const float ReliefGrain = 0.6f; // how lumpy the grain makes a layer's surface
        private const int GrainSize = 128;

        /// <summary>The grain, one sample a texel: on a finer canvas the specks are finer (spread over the dish at the tuned
        /// resolution's scale instead, each one came out a visible 2x2 block).</summary>
        private float GrainAt(int texel, int layer)
        {
            int gx = (texel % _resolution + layer * 37) % GrainSize;
            int gy = (texel / _resolution + layer * 53) % GrainSize;
            return Grain[gy * GrainSize + gx];
        }

        /// <summary>Fixed grain in [-1, 1]: soft clumps (value noise) with fine specks on top. Deterministic.</summary>
        private static readonly float[] Grain = BuildGrain();

        private static float[] BuildGrain()
        {
            var grain = new float[GrainSize * GrainSize];
            for (int y = 0; y < GrainSize; y++)
            {
                for (int x = 0; x < GrainSize; x++)
                {
                    float clumps = Mathf.PerlinNoise(x * 0.11f + 3.7f, y * 0.11f + 9.1f) * 2f - 1f;
                    uint h = (uint)(x * 374761393 + y * 668265263);
                    h = (h ^ (h >> 13)) * 1274126177u;
                    float speck = ((h ^ (h >> 16)) & 0xffff) / 65535f * 2f - 1f;
                    grain[y * GrainSize + x] = Mathf.Clamp(clumps * 0.7f + speck * 0.45f, -1f, 1f);
                }
            }

            return grain;
        }

        private static readonly Color ReflectionSheen = new Color(0.97f, 0.99f, 1f);
        private const float InspectionDim = 0.55f; // glass brightness away from the reflection
        private const float InspectionGloss = 0.45f; // and its gloss there
        private const float SheenOpacity = 0.85f; // a see-through glass: how solid the reflection reads

        /// <summary>
        /// Thin wavy smears, like water drying on a glass. The dish texture is projected from above, so a
        /// glass's walls are the ring at the edge of the UV disc: strokes run outwards across that ring and
        /// show on the walls as drips running down, and as smears on the base. Each fades out at both ends.
        /// <paramref name="coverage"/> sets how many strokes, not an area.
        /// </summary>
        private float[] GenerateStreaks(float coverage, System.Random random)
        {
            var mask = new float[_resolution * _resolution];
            int strokes = Mathf.Clamp(Mathf.RoundToInt(coverage * 16f), 0, 14);
            for (int s = 0; s < strokes; s++)
            {
                float spotAngle = (float)(random.NextDouble() * Mathf.PI * 2f);
                float spotRadius = 0.32f + 0.1f * (float)random.NextDouble(); // near the rim, where the walls are
                var outwards = new Vector2(Mathf.Cos(spotAngle), Mathf.Sin(spotAngle));
                Vector2 center = new Vector2(0.5f, 0.5f) + outwards * spotRadius;
                float angle = spotAngle + ((float)random.NextDouble() - 0.5f) * 0.5f; // roughly radial
                var along = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
                var across = new Vector2(-along.y, along.x);
                float halfLength = 0.08f + 0.08f * (float)random.NextDouble();
                float width = 0.012f + 0.008f * (float)random.NextDouble();
                float wobbleFrequency = 18f + 10f * (float)random.NextDouble();
                float wobblePhase = (float)random.NextDouble() * Mathf.PI * 2f;
                float thickness = 0.7f + 0.3f * (float)random.NextDouble();

                // Only round the stroke itself (its length, and a few widths and its wobble either side).
                float reach = halfLength + width * 3f + 0.012f;
                int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - reach) * _resolution));
                int x1 = Mathf.Min(_resolution - 1, Mathf.CeilToInt((center.x + reach) * _resolution));
                int y0 = Mathf.Max(0, Mathf.FloorToInt((center.y - reach) * _resolution));
                int y1 = Mathf.Min(_resolution - 1, Mathf.CeilToInt((center.y + reach) * _resolution));
                for (int y = y0; y <= y1; y++)
                {
                    for (int x = x0; x <= x1; x++)
                    {
                        int index = y * _resolution + x;
                        if (!_inside[index])
                        {
                            continue;
                        }

                        Vector2 offset = TexelCenter(x, y) - center;
                        float along01 = Vector2.Dot(offset, along);
                        if (Mathf.Abs(along01) >= halfLength)
                        {
                            continue;
                        }

                        float distance = Vector2.Dot(offset, across) - 0.012f * Mathf.Sin(along01 * wobbleFrequency + wobblePhase);
                        float endFade = Mathf.Clamp01((halfLength - Mathf.Abs(along01)) / (halfLength * 0.35f));
                        float value = Mathf.Exp(-(distance * distance) / (width * width)) * endFade * thickness;
                        mask[index] = Mathf.Max(mask[index], value);
                    }
                }
            }

            // Drop the faint fringes: they would count as dirt nobody can see even in the light.
            for (int i = 0; i < mask.Length; i++)
            {
                if (mask[i] < 0.08f)
                {
                    mask[i] = 0f;
                }
            }

            return mask;
        }

        private float[] GeneratePattern(float coverage, System.Random random)
        {
            var mask = new float[_resolution * _resolution];
            if (coverage <= 0f)
            {
                return mask;
            }

            float offsetX = (float)random.NextDouble() * 1000f;
            float offsetY = (float)random.NextDouble() * 1000f;
            var noise = new float[mask.Length];
            var insideValues = new List<float>(_insideCount);
            for (int y = 0; y < _resolution; y++)
            {
                for (int x = 0; x < _resolution; x++)
                {
                    int index = y * _resolution + x;
                    Vector2 uv = Twirl(TexelCenter(x, y));
                    // Two octaves: big splotches plus some crusty detail.
                    float n = 0.7f * Mathf.PerlinNoise(offsetX + uv.x * NoiseFrequency, offsetY + uv.y * NoiseFrequency)
                            + 0.3f * Mathf.PerlinNoise(offsetX * 0.5f + uv.x * NoiseFrequency * 3f, offsetY * 0.5f + uv.y * NoiseFrequency * 3f);
                    Vector2 fromCentre = uv - new Vector2(0.5f, 0.5f);
                    float r = fromCentre.magnitude;
                    float wall = WallWeight(r);
                    if (wall > 0f)
                    {
                        // On the walls: tide lines round the dish - dirt that varies with height rather than round it,
                        // since the projection stretches every change round the wall into a stripe down it. The
                        // round-the-wall part is noise on a circle, so it closes on itself without a seam.
                        float angle = Mathf.Atan2(fromCentre.y, fromCentre.x);
                        float rings = 0.78f * Mathf.PerlinNoise(offsetX + r * WallRingFrequency, offsetY + 0.37f)
                                      + 0.22f * Mathf.PerlinNoise(offsetX * 0.5f + Mathf.Cos(angle) * 0.8f, offsetY * 0.5f + Mathf.Sin(angle) * 0.8f);
                        n = Mathf.Lerp(n, rings, wall);
                    }

                    // Food sits in the middle and gets pushed out towards the rim, not scattered evenly like camouflage.
                    n += CentreBias * (0.3f - r);
                    // Fine noise frays the edges into specks and spatters: smooth blob outlines read as a printed pattern.
                    // (Not on the walls, where it would be drawn out into streaks.)
                    n += EdgeFray * (1f - wall) * (Mathf.PerlinNoise(offsetY + uv.x * NoiseFrequency * 11f, offsetX + uv.y * NoiseFrequency * 11f) - 0.5f);
                    noise[index] = n;
                    if (_inside[index])
                    {
                        insideValues.Add(n);
                    }
                }
            }

            // Threshold at the (1 - coverage) quantile, so exactly `coverage` of the surface is dirty.
            float threshold = float.NegativeInfinity;
            if (coverage < 1f && insideValues.Count > 0)
            {
                insideValues.Sort();
                threshold = insideValues[Mathf.Clamp(Mathf.FloorToInt((1f - coverage) * insideValues.Count), 0, insideValues.Count - 1)];
            }

            for (int i = 0; i < mask.Length; i++)
            {
                if (!_inside[i])
                {
                    continue;
                }

                float presence = coverage >= 1f ? 1f : Mathf.Clamp01((noise[i] - threshold) / EdgeSoftness + 0.5f);
                // Thicker in the middle of a splotch: reads as a crust rather than a flat decal.
                float thickness = Mathf.Min(1f, 0.7f + 0.3f * noise[i]);
                mask[i] = presence * thickness;
            }

            return mask;
        }

        /// <summary>
        /// Turns the noise's round blobs into arcs, the way food gets pushed round a plate: each point is swung
        /// about the dish's centre by an angle that grows towards the rim, which shears blobs along circles.
        /// </summary>
        private static Vector2 Twirl(Vector2 uv)
        {
            Vector2 d = uv - new Vector2(0.5f, 0.5f);
            float angle = TwirlRadians * d.magnitude * 2f;
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            return new Vector2(0.5f + d.x * cos - d.y * sin, 0.5f + d.x * sin + d.y * cos);
        }

        /// <summary>1 on the wall band, fading to 0 over a couple of texels either side of it; 0 without a band.</summary>
        private float WallWeight(float r)
        {
            if (!_wallBand.HasValue)
            {
                return 0f;
            }

            const float fade = 0.02f;
            Vector2 band = _wallBand.Value;
            return Mathf.Clamp01(Mathf.Min(r - band.x, band.y - r) / fade + 0.5f);
        }

        // Tide lines a few texels wide: finer ones (a period of ~3 texels) turned into moire where the ring crosses the
        // texel grid, which varied round the ring again.
        private const float WallRingFrequency = 14f;

        private const float TwirlRadians = 0.9f; // at the rim; much more and the dirt turned into a candy swirl
        private const float CentreBias = 0.35f;
        private const float EdgeFray = 0.14f;

        private float Sum(float[] values)
        {
            float sum = 0f;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }

            return sum;
        }

        private Vector2 TexelCenter(int x, int y)
        {
            return new Vector2((x + 0.5f) / _resolution, (y + 0.5f) / _resolution);
        }

        /// <summary>The region cut to the canvas; null is the whole canvas.</summary>
        private RectInt Clip(RectInt? region)
        {
            if (!region.HasValue)
            {
                return Whole;
            }

            RectInt r = region.Value;
            int xMin = Mathf.Clamp(r.xMin, 0, _resolution);
            int yMin = Mathf.Clamp(r.yMin, 0, _resolution);
            int xMax = Mathf.Clamp(r.xMax, xMin, _resolution);
            int yMax = Mathf.Clamp(r.yMax, yMin, _resolution);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }
    }
}
