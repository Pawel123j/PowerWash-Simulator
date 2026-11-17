using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Procedurally drawn UI sprites (signed-distance shapes, antialiased): a 9-sliceable rounded
    /// rectangle for every panel and button, a circle, the tool icons and a background gradient.
    /// Generated into PNGs once; loaded fresh on every access, because sprites loaded before a scene
    /// builder's EditorSceneManager.NewScene() call end up serialised as null.
    /// </summary>
    internal static class UiArt
    {
        private const string Folder = "Assets/_Project/Art/UI";
        private const int RoundedSize = 128;
        private const int RoundedRadius = 40;

        public static Sprite Rounded => Load("RoundedRect");
        public static Sprite Circle => Load("Circle");
        public static Sprite Vignette => Load("Vignette");
        public static Sprite Scrim => Load("Scrim");

        /// <summary>A ring, for pointing at something (the tutorial's marker).</summary>
        public static Sprite Ring => Load("Ring");

        /// <summary>A rounded arrow pointing down.</summary>
        public static Sprite Arrow => Load("Arrow");

        /// <summary>A pointing finger seen from above, for showing a gesture.</summary>
        public static Sprite Finger => Load("Finger");

        /// <summary>A soft round glow (white, tinted at runtime).</summary>
        public static Sprite Glow => Load("Glow");

        // The minigames' props, shaded as if lit from above like the kitchen around them (no flat shapes on a card): the
        // prep board's wood, steel, vegetable, knife and hand; the dishwasher's tub, filter, holes, scraps, arm and button.
        // "White" ones carry only light and shade and are tinted by their Image's colour (the runtime recolours some).

        /// <summary>The chopping board side on: pale wood with grain on top, its front edge darker (untinted).</summary>
        public static Sprite WoodBoard => Load("Prop_WoodBoard");

        /// <summary>Brushed steel counter, in the board's shadow along its top (untinted).</summary>
        public static Sprite SteelCounter => Load("Prop_SteelCounter");

        /// <summary>A vegetable lying on the board: a rounded log, lit from above (white, 9-sliced).</summary>
        public static Sprite VegetableBody => Load("Prop_Vegetable");

        /// <summary>A cut piece: a lit top face and a shaded side (white, 9-sliced).</summary>
        public static Sprite CutPiece => Load("Prop_Piece");

        /// <summary>A round handle or collar, shaded across its width (white).</summary>
        public static Sprite Cylinder => Load("Prop_Cylinder");

        /// <summary>A knife blade side on: a bright ground edge, the flat, a darker spine (untinted).</summary>
        public static Sprite BladeSteel => Load("Prop_Blade");

        /// <summary>The hand holding the vegetable, side on: fingertips tucked into a claw (untinted).</summary>
        public static Sprite Claw => Load("Prop_Claw");

        /// <summary>The dishwasher's tub seen from above: dark inside, a lit rim (untinted).</summary>
        public static Sprite Basin => Load("Prop_Basin");

        /// <summary>The filter plate: lathe-turned steel with a bevelled rim (untinted; the holes are drawn over it).</summary>
        public static Sprite FilterSteel => Load("Prop_Filter");

        /// <summary>A hole in steel: dark, its lower inner wall catching the light (untinted).</summary>
        public static Sprite Hole => Load("Prop_Hole");

        /// <summary>A lumpy food scrap with a wet highlight (white).</summary>
        public static Sprite Scrap => Load("Prop_Scrap");

        /// <summary>A domed button or cap (white).</summary>
        public static Sprite Dome => Load("Prop_Dome");

        /// <summary>A round steel bar with rounded ends, shaded across it (white, 9-sliced).</summary>
        public static Sprite Bar => Load("Prop_Bar");

        public static Sprite ToolIcon(string toolId)
        {
            switch (toolId)
            {
                case "sponge": return Load("Icon_Sponge");
                case "steel_wool": return Load("Icon_SteelWool");
                case "soak": return Load("Icon_Soap");
                case "scraper": return Load("Icon_Scraper");
                case "shower": return Load("Icon_Shower");
                default: return null;
            }
        }

        public static void EnsureAssets()
        {
            EnsureFolder(Folder);
            Ensure("RoundedRect", RoundedSize, RoundedSize, RoundedRadius, (x, y) =>
                Coverage(RoundedBoxSdf(new Vector2(x, y), new Vector2(RoundedSize, RoundedSize) * 0.5f, RoundedRadius), Color.white));
            Ensure("Circle", 128, 128, 0, (x, y) =>
                Coverage((new Vector2(x, y) - new Vector2(64f, 64f)).magnitude - 62f, Color.white));
            Ensure("Icon_Sponge", 128, 128, 0, DrawSponge);
            Ensure("Icon_SteelWool", 128, 128, 0, DrawSteelWool);
            Ensure("Icon_Soap", 128, 128, 0, DrawSoapBottle);
            Ensure("Icon_Scraper", 128, 128, 0, DrawScraper);
            Ensure("Icon_Shower", 128, 128, 0, DrawShower);
            Ensure("Ring", 128, 128, 0, (x, y) =>
            {
                float d = (new Vector2(x, y) - new Vector2(64f, 64f)).magnitude;
                return Coverage(Mathf.Abs(d - 54f) - 7f, Color.white);
            });
            Ensure("Arrow", 128, 160, 0, DrawArrow);
            Ensure("Finger", 128, 128, 0, DrawFinger);
            Ensure("Glow", 128, 128, 0, (x, y) =>
            {
                float d = (new Vector2(x, y) - new Vector2(64f, 64f)).magnitude / 64f;
                float a = Mathf.Clamp01(1f - d);
                return new Color(1f, 1f, 1f, a * a);
            });
            EnsureMaps();
            Ensure("Prop_WoodBoard", 512, 64, 0, DrawWoodBoard);
            Ensure("Prop_SteelCounter", 256, 128, 0, DrawSteelCounter);
            Ensure("Prop_Vegetable", 128, 128, 40, DrawVegetable);
            Ensure("Prop_Piece", 32, 32, 10, DrawPiece);
            Ensure("Prop_Cylinder", 32, 64, 0, DrawCylinder);
            Ensure("Prop_Blade", 32, 128, 0, DrawBlade);
            Ensure("Prop_Claw", 192, 96, 0, DrawClaw);
            Ensure("Prop_Basin", 256, 256, 0, DrawBasin);
            Ensure("Prop_Filter", 256, 256, 0, DrawFilter);
            Ensure("Prop_Hole", 32, 32, 0, DrawHole);
            Ensure("Prop_Scrap", 128, 128, 0, DrawScrap);
            Ensure("Prop_Dome", 128, 128, 0, DrawDome);
            Ensure("Prop_Bar", 128, 64, 28, DrawBar);
            Ensure("Scrim", 8, 256, 0, (x, y) =>
            {
                // Over a 3D scene: dark behind a title at the top and buttons at the bottom, clear between.
                float v = y / 255f;
                float top = Mathf.Clamp01((v - 0.62f) / 0.38f);
                float bottom = Mathf.Clamp01((0.4f - v) / 0.4f);
                return new Color(0.02f, 0.03f, 0.04f, Mathf.Max(top * top * 0.85f, bottom * bottom * 0.9f));
            });
            Ensure("Vignette", 256, 256, 0, (x, y) =>
            {
                // Transparent middle, opaque towards the edges and corners (tinted at runtime).
                var p = new Vector2(x / 255f, y / 255f) * 2f - Vector2.one;
                float edge = Mathf.Max(Mathf.Abs(p.x), Mathf.Abs(p.y)) * 0.7f + p.magnitude * 0.3f;
                float a = Mathf.Clamp01((edge - 0.55f) / 0.45f);
                return new Color(1f, 1f, 1f, a * a);
            });
            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------
        // The view out of Kuba's window: Dublin at dusk
        // ------------------------------------------------------------------

        /// <summary>The city - sky fading from night blue to a sodium-orange horizon, terraces with lit windows,
        /// the spire and the Liffey reflecting it all - behind the glass of Kuba's 3D window (RoomArt). Seeded,
        /// so it is identical every run.</summary>
        internal static Color[] PaintDublinView(int size, System.Random random)
        {
            var px = new Color[size * size];
            int horizon = (int)(size * 0.3f); // river below, city above

            // Sky gradient; river as a darker, slightly blue reflection of it.
            for (int y = 0; y < size; y++)
            {
                float t = Mathf.InverseLerp(horizon, size, y);
                Color sky = Color.Lerp(new Color(0.93f, 0.52f, 0.36f), new Color(0.1f, 0.12f, 0.28f), Mathf.Pow(t, 0.7f));
                Color river = Color.Lerp(new Color(0.08f, 0.1f, 0.18f), new Color(0.35f, 0.25f, 0.3f), y / (float)horizon);
                for (int x = 0; x < size; x++)
                {
                    px[y * size + x] = y >= horizon ? sky : river;
                }
            }

            // Terrace skyline with lit windows, drawn back to front in two rows.
            for (int layer = 0; layer < 2; layer++)
            {
                Color wall = layer == 0 ? new Color(0.2f, 0.17f, 0.25f) : new Color(0.07f, 0.07f, 0.11f);
                int x = -random.Next(0, 30);
                while (x < size)
                {
                    int width = random.Next(38, 78);
                    int height = random.Next(layer == 0 ? 110 : 60, layer == 0 ? 220 : 150);
                    FillRect(px, size, x, horizon, width, height, wall);
                    if (layer == 1 && random.NextDouble() < 0.25)
                    {
                        // A chimney pot or two.
                        FillRect(px, size, x + width / 3, horizon + height, 8, 14, wall);
                    }

                    for (int wy = horizon + 14; wy < horizon + height - 12; wy += 22)
                    {
                        for (int wx = x + 8; wx < x + width - 10; wx += 16)
                        {
                            if (random.NextDouble() < (layer == 0 ? 0.2 : 0.32))
                            {
                                Color lamp = Color.Lerp(new Color(1f, 0.78f, 0.42f), new Color(1f, 0.92f, 0.7f), (float)random.NextDouble());
                                FillRect(px, size, wx, wy, 8, 11, lamp);
                                // Its wobbly reflection in the river.
                                for (int r = 0; r < 26; r += 3)
                                {
                                    int ry = horizon - 6 - (wy - horizon) / 3 - r;
                                    FillRect(px, size, wx + random.Next(-2, 3), ry, 6, 2, lamp * new Color(1f, 1f, 1f, 0.5f), true);
                                }
                            }
                        }
                    }

                    x += width + random.Next(0, 6);
                }
            }

            // A church spire on the skyline.
            for (int s = 0; s < 150; s++)
            {
                int half = Mathf.Max(1, (150 - s) / 9);
                FillRect(px, size, 360 - half, horizon + 150 + s, half * 2, 1, new Color(0.07f, 0.07f, 0.11f));
            }

            FillRect(px, size, 342, horizon, 36, 150, new Color(0.07f, 0.07f, 0.11f));
            return px;
        }

        // ------------------------------------------------------------------
        // The hub's maps (CityMapPanel): stylised, on old paper, the pins' places (CityMap) painted to match
        // ------------------------------------------------------------------

        public const int MapWidth = 600;
        public const int MapHeight = 800;

        public static Sprite MapOf(Zmywak.Core.Data.Place place) => Load($"Map_{place}");

        private static void EnsureMaps()
        {
            foreach (Zmywak.Core.Data.Place place in new[] { Zmywak.Core.Data.Place.Dublin, Zmywak.Core.Data.Place.Krakow, Zmywak.Core.Data.Place.Pilbara })
            {
                Color[] px = place == Zmywak.Core.Data.Place.Krakow ? PaintKrakowMap()
                    : place == Zmywak.Core.Data.Place.Pilbara ? PaintPilbaraMap()
                    : PaintDublinMap();
                Ensure($"Map_{place}", MapWidth, MapHeight, 0, (x, y) => px[y * MapWidth + x]);
            }
        }

        private static Color Paper(int x, int y)
        {
            float grain = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.05f + Mathf.PerlinNoise(x * 0.4f, y * 0.4f) * 0.025f;
            return new Color(0.92f - grain, 0.88f - grain, 0.78f - grain);
        }

        /// <summary>Paints a band along a polyline (u, v in 0-1), width in pixels.</summary>
        private static void Band(Color[] px, Vector2[] path, float width, Color colour)
        {
            for (int i = 0; i < path.Length - 1; i++)
            {
                Vector2 a = new Vector2(path[i].x * MapWidth, path[i].y * MapHeight);
                Vector2 b = new Vector2(path[i + 1].x * MapWidth, path[i + 1].y * MapHeight);
                int steps = Mathf.CeilToInt(Vector2.Distance(a, b));
                for (int s = 0; s <= steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / (float)Mathf.Max(1, steps));
                    Disc(px, p, width * 0.5f, colour);
                }
            }
        }

        private static void Disc(Color[] px, Vector2 centre, float radius, Color colour)
        {
            for (int y = Mathf.FloorToInt(centre.y - radius); y <= Mathf.CeilToInt(centre.y + radius); y++)
            {
                for (int x = Mathf.FloorToInt(centre.x - radius); x <= Mathf.CeilToInt(centre.x + radius); x++)
                {
                    if (x < 0 || y < 0 || x >= MapWidth || y >= MapHeight)
                    {
                        continue;
                    }

                    float edge = Mathf.Clamp01(radius + 0.5f - Vector2.Distance(new Vector2(x, y), centre));
                    px[y * MapWidth + x] = Color.Lerp(px[y * MapWidth + x], colour, edge * colour.a);
                }
            }
        }

        /// <summary>A street grid, broken and bent a little, in a box of the map.</summary>
        private static void Streets(Color[] px, System.Random random, Rect area, float spacing, Color colour)
        {
            for (float v = area.yMin; v < area.yMax; v += spacing * (0.7f + (float)random.NextDouble() * 0.6f))
            {
                float bend = ((float)random.NextDouble() - 0.5f) * 0.04f;
                Band(px, new[] { new Vector2(area.xMin, v), new Vector2(area.center.x, v + bend), new Vector2(area.xMax, v) }, 2.5f, colour);
            }

            for (float u = area.xMin; u < area.xMax; u += spacing * (0.7f + (float)random.NextDouble() * 0.6f))
            {
                float bend = ((float)random.NextDouble() - 0.5f) * 0.04f;
                Band(px, new[] { new Vector2(u, area.yMin), new Vector2(u + bend, area.center.y), new Vector2(u, area.yMax) }, 2.5f, colour);
            }
        }

        private static Color[] PaintDublinMap()
        {
            var px = new Color[MapWidth * MapHeight];
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    px[y * MapWidth + x] = Paper(x, y);
                }
            }

            var random = new System.Random(1916);
            var water = new Color(0.5f, 0.66f, 0.78f);
            var park = new Color(0.55f, 0.7f, 0.45f);
            var street = new Color(1f, 0.98f, 0.92f, 0.85f);
            Streets(px, random, new Rect(0.05f, 0.05f, 0.75f, 0.9f), 0.05f, street);

            // Dublin Bay on the right, the Liffey across the middle into it, the two canals ringing the centre.
            for (int y = 0; y < MapHeight; y++)
            {
                float coast = 0.84f + Mathf.Sin(y * 0.012f) * 0.03f + (y / (float)MapHeight - 0.5f) * 0.1f;
                for (int x = (int)(coast * MapWidth); x < MapWidth; x++)
                {
                    px[y * MapWidth + x] = water;
                }
            }

            Band(px, new[] { new Vector2(0f, 0.56f), new Vector2(0.2f, 0.55f), new Vector2(0.4f, 0.58f), new Vector2(0.6f, 0.57f), new Vector2(0.88f, 0.6f) }, 14f, water);
            Band(px, new[] { new Vector2(0.18f, 0.24f), new Vector2(0.4f, 0.27f), new Vector2(0.62f, 0.3f), new Vector2(0.8f, 0.42f), new Vector2(0.86f, 0.5f) }, 5f, water);
            Band(px, new[] { new Vector2(0.15f, 0.86f), new Vector2(0.4f, 0.84f), new Vector2(0.65f, 0.82f), new Vector2(0.86f, 0.7f) }, 5f, water);

            // Phoenix Park to the west, St Stephen's Green south of the river.
            Disc(px, new Vector2(0.1f * MapWidth, 0.68f * MapHeight), 70f, park);
            Disc(px, new Vector2(0.17f * MapWidth, 0.72f * MapHeight), 50f, park);
            for (int y = (int)(0.4f * MapHeight); y < 0.45f * MapHeight; y++)
            {
                for (int x = (int)(0.55f * MapWidth); x < 0.62f * MapWidth; x++)
                {
                    px[y * MapWidth + x] = park;
                }
            }

            return px;
        }

        private static Color[] PaintKrakowMap()
        {
            var px = new Color[MapWidth * MapHeight];
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    px[y * MapWidth + x] = Paper(x, y);
                }
            }

            var random = new System.Random(1257);
            var water = new Color(0.5f, 0.66f, 0.78f);
            var park = new Color(0.55f, 0.7f, 0.45f);
            Streets(px, random, new Rect(0.05f, 0.08f, 0.9f, 0.88f), 0.06f, new Color(1f, 0.98f, 0.92f, 0.85f));

            // The Vistula looping past Wawel and Kazimierz in the south.
            Band(px, new[] { new Vector2(0f, 0.3f), new Vector2(0.25f, 0.33f), new Vector2(0.38f, 0.38f), new Vector2(0.48f, 0.32f), new Vector2(0.7f, 0.27f), new Vector2(1f, 0.36f) }, 22f, water);

            // The Planty: a ring of green round the Old Town, the Rynek's square in the middle of it.
            var centre = new Vector2(0.5f * MapWidth, 0.63f * MapHeight);
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y * 0.85f), new Vector2(centre.x, centre.y * 0.85f));
                    if (d > 120f && d < 138f)
                    {
                        px[y * MapWidth + x] = park;
                    }
                }
            }

            for (int y = (int)(centre.y - 22); y < centre.y + 22; y++)
            {
                for (int x = (int)(centre.x - 22); x < centre.x + 22; x++)
                {
                    px[y * MapWidth + x] = new Color(0.86f, 0.78f, 0.66f);
                }
            }

            // Wawel on its hill.
            Disc(px, new Vector2(0.42f * MapWidth, 0.42f * MapHeight), 28f, new Color(0.75f, 0.68f, 0.55f));
            return px;
        }

        private static Color[] PaintPilbaraMap()
        {
            var px = new Color[MapWidth * MapHeight];
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    float grain = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 0.08f;
                    px[y * MapWidth + x] = new Color(0.78f - grain, 0.45f - grain, 0.3f - grain);
                }
            }

            var dirt = new Color(0.86f, 0.62f, 0.45f);
            var roof = new Color(0.86f, 0.86f, 0.82f);
            // The haul road from the mine to the camp, the airstrip off it.
            Band(px, new[] { new Vector2(0.85f, 0.15f), new Vector2(0.65f, 0.35f), new Vector2(0.5f, 0.5f), new Vector2(0.3f, 0.75f) }, 10f, dirt);
            Band(px, new[] { new Vector2(0.1f, 0.85f), new Vector2(0.45f, 0.8f) }, 20f, new Color(0.55f, 0.55f, 0.55f));

            // The mine: a pit in terraces.
            for (int ring = 0; ring < 5; ring++)
            {
                Disc(px, new Vector2(0.8f * MapWidth, 0.16f * MapHeight), 90f - ring * 16f, ring % 2 == 0 ? new Color(0.6f, 0.32f, 0.22f) : new Color(0.7f, 0.4f, 0.27f));
            }

            // The camp: rows of dongas, the kitchen and the wet mess bigger.
            for (int row = 0; row < 6; row++)
            {
                for (int col = 0; col < 5; col++)
                {
                    int x0 = (int)(0.32f * MapWidth) + col * 26;
                    int y0 = (int)(0.38f * MapHeight) + row * 22;
                    for (int y = y0; y < y0 + 12; y++)
                    {
                        for (int x = x0; x < x0 + 20; x++)
                        {
                            px[y * MapWidth + x] = roof;
                        }
                    }
                }
            }

            return px;
        }

        /// <summary>The view after a move to Kraków: the Vistula below, a dusk sky, tenements with lit windows, Wawel on
        /// its hill to the left and St Mary's two unequal towers. Seeded, so it is identical every run.</summary>
        internal static Color[] PaintKrakowView(int size, System.Random random)
        {
            var px = new Color[size * size];
            int horizon = (int)(size * 0.28f);
            var dark = new Color(0.07f, 0.07f, 0.1f);
            for (int y = 0; y < size; y++)
            {
                float t = Mathf.InverseLerp(horizon, size, y);
                Color sky = Color.Lerp(new Color(0.85f, 0.58f, 0.5f), new Color(0.1f, 0.13f, 0.3f), Mathf.Pow(t, 0.65f));
                Color river = Color.Lerp(new Color(0.07f, 0.09f, 0.16f), new Color(0.3f, 0.25f, 0.32f), y / (float)horizon);
                for (int x = 0; x < size; x++)
                {
                    px[y * size + x] = y >= horizon ? sky : river;
                }
            }

            // Wawel: a hill with the castle's long walls and a dome on it, on the left.
            for (int x = 0; x < size * 0.4f; x++)
            {
                int hill = (int)(Mathf.Sin(Mathf.Clamp01(x / (size * 0.4f)) * Mathf.PI) * 60f);
                FillRect(px, size, x, horizon, 1, hill + 70, dark);
            }

            FillRect(px, size, 60, horizon + 110, 90, 40, dark);
            for (int s = 0; s < 26; s++)
            {
                FillRect(px, size, 105 - (26 - s) / 2, horizon + 150 + s, 26 - s, 1, dark);
            }

            // Tenements along the river, windows lit.
            int tx = (int)(size * 0.38f);
            while (tx < size)
            {
                int width = random.Next(30, 60);
                int height = random.Next(70, 130);
                FillRect(px, size, tx, horizon, width, height, new Color(0.12f, 0.1f, 0.15f));
                for (int wy = horizon + 12; wy < horizon + height - 10; wy += 20)
                {
                    for (int wx = tx + 6; wx < tx + width - 8; wx += 14)
                    {
                        if (random.NextDouble() < 0.3)
                        {
                            FillRect(px, size, wx, wy, 7, 10, Color.Lerp(new Color(1f, 0.75f, 0.4f), new Color(1f, 0.9f, 0.65f), (float)random.NextDouble()));
                        }
                    }
                }

                tx += width + random.Next(0, 5);
            }

            // St Mary's: two towers, the taller with its spire and crown.
            FillRect(px, size, 380, horizon, 26, 230, dark);
            FillRect(px, size, 414, horizon, 24, 190, dark);
            for (int s = 0; s < 70; s++)
            {
                int half = Mathf.Max(1, (70 - s) / 6);
                FillRect(px, size, 393 - half, horizon + 230 + s, half * 2, 1, dark);
            }

            return px;
        }

        /// <summary>The view from a room in the Pilbara: a night sky thick with stars over flat red earth, a mine's
        /// headframe and the camp's lights on the horizon. Seeded, so it is identical every run.</summary>
        internal static Color[] PaintPilbaraView(int size, System.Random random)
        {
            var px = new Color[size * size];
            int horizon = (int)(size * 0.3f);
            for (int y = 0; y < size; y++)
            {
                float t = Mathf.InverseLerp(horizon, size, y);
                Color sky = Color.Lerp(new Color(0.42f, 0.2f, 0.18f), new Color(0.02f, 0.03f, 0.1f), Mathf.Pow(t, 0.45f));
                Color earth = Color.Lerp(new Color(0.12f, 0.05f, 0.03f), new Color(0.3f, 0.12f, 0.07f), y / (float)horizon);
                for (int x = 0; x < size; x++)
                {
                    px[y * size + x] = y >= horizon ? sky : earth;
                }
            }

            for (int star = 0; star < 900; star++)
            {
                int sx = random.Next(0, size);
                int sy = random.Next(horizon + 20, size);
                float bright = 0.4f + (float)random.NextDouble() * 0.6f;
                FillRect(px, size, sx, sy, 1, 1, new Color(bright, bright, bright * 0.95f, 1f));
            }

            // A low ridge, the mine's headframe and the camp's lights.
            var dark = new Color(0.05f, 0.03f, 0.04f);
            for (int x = 0; x < size; x++)
            {
                int ridge = (int)(Mathf.PerlinNoise(x * 0.01f, 3f) * 26f);
                FillRect(px, size, x, horizon, 1, ridge, dark);
            }

            FillRect(px, size, 300, horizon, 6, 120, dark);
            FillRect(px, size, 340, horizon, 6, 120, dark);
            for (int s = 0; s < 120; s += 20)
            {
                FillRect(px, size, 300, horizon + s, 46, 3, dark);
            }

            for (int light = 0; light < 40; light++)
            {
                int lx = random.Next(20, size - 20);
                FillRect(px, size, lx, horizon + random.Next(2, 10), 3, 2, new Color(1f, 0.85f, 0.5f));
            }

            return px;
        }

        private static void FillRect(Color[] px, int size, int x0, int y0, int width, int height, Color color, bool blend = false)
        {
            for (int y = y0; y < y0 + height; y++)
            {
                for (int x = x0; x < x0 + width; x++)
                {
                    if (blend)
                    {
                        Blend(px, size, x, y, color);
                    }
                    else if (x >= 0 && y >= 0 && x < size && y < size)
                    {
                        px[y * size + x] = color;
                    }
                }
            }
        }

        private static void Blend(Color[] px, int size, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= size || y >= size)
            {
                return;
            }

            Color below = px[y * size + x];
            Color mixed = Color.Lerp(below, color, color.a);
            mixed.a = below.a;
            px[y * size + x] = mixed;
        }

        // ------------------------------------------------------------------
        // Icons
        // ------------------------------------------------------------------

        private static Color DrawSponge(int x, int y)
        {
            var p = new Vector2(x, y);
            // Yellow foam block with a green scourer layer on top and a few pores.
            float foam = RoundedBoxSdf(p - new Vector2(64f, 52f), new Vector2(48f, 26f), 12f);
            float scourer = RoundedBoxSdf(p - new Vector2(64f, 86f), new Vector2(48f, 12f), 8f);
            Color c = Clear;
            c = Over(c, Coverage(foam, new Color(1f, 0.83f, 0.25f)));
            c = Over(c, Coverage(scourer, new Color(0.25f, 0.65f, 0.35f)));
            Vector2[] pores = { new Vector2(40, 48), new Vector2(62, 38), new Vector2(86, 52), new Vector2(52, 64), new Vector2(76, 66) };
            foreach (Vector2 pore in pores)
            {
                c = Over(c, Coverage((p - pore).magnitude - 5f, new Color(0.85f, 0.62f, 0.12f)) * (foam < 0f ? 1f : 0f));
            }

            return c;
        }

        private static Color DrawSteelWool(int x, int y)
        {
            var p = new Vector2(x, y) - new Vector2(64f, 64f);
            float r = p.magnitude;
            float ball = r - 50f;
            if (ball > 1f)
            {
                return Clear;
            }

            // Tangled wire: interfering wavy rings give a scribbly steel-wool look.
            float angle = Mathf.Atan2(p.y, p.x);
            float strands = Mathf.Abs(Mathf.Sin(r * 0.55f + Mathf.Sin(angle * 5f) * 2.2f)) *
                            Mathf.Abs(Mathf.Sin(r * 0.3f - Mathf.Sin(angle * 3f + 1f) * 3f));
            float shade = 0.45f + 0.5f * strands - r / 50f * 0.15f;
            return Coverage(ball, new Color(shade, shade, shade * 1.05f));
        }

        private static Color DrawSoapBottle(int x, int y)
        {
            var p = new Vector2(x, y);
            // Green washing-up liquid bottle with a white flip cap, a label and two bubbles beside it.
            float body = RoundedBoxSdf(p - new Vector2(56f, 46f), new Vector2(28f, 38f), 12f);
            float shoulder = RoundedBoxSdf(p - new Vector2(56f, 86f), new Vector2(18f, 8f), 6f);
            float cap = RoundedBoxSdf(p - new Vector2(56f, 100f), new Vector2(9f, 9f), 3f);
            float label = RoundedBoxSdf(p - new Vector2(56f, 44f), new Vector2(20f, 16f), 4f);
            Color c = Clear;
            c = Over(c, Coverage(Mathf.Min(body, shoulder), new Color(0.3f, 0.75f, 0.35f)));
            c = Over(c, Coverage(cap, new Color(0.95f, 0.96f, 0.97f)));
            c = Over(c, Coverage(label, new Color(1f, 0.93f, 0.55f)));
            c = Over(c, Coverage(RoundedBoxSdf(p - new Vector2(40f, 54f), new Vector2(3f, 18f), 3f), new Color(1f, 1f, 1f, 0.45f)));
            c = Over(c, Coverage(Mathf.Abs((p - new Vector2(100f, 86f)).magnitude - 12f) - 2.5f, new Color(0.7f, 0.9f, 1f)));
            c = Over(c, Coverage(Mathf.Abs((p - new Vector2(106f, 56f)).magnitude - 7f) - 2f, new Color(0.7f, 0.9f, 1f)));
            return c;
        }

        private static Color DrawScraper(int x, int y)
        {
            var p = new Vector2(x, y);
            // A wide steel blade on a red handle, tilted like it is pushing crust off a pan.
            Vector2 q = Rotate(p - new Vector2(64f, 64f), -35f);
            float handle = RoundedBoxSdf(q - new Vector2(0f, 26f), new Vector2(11f, 30f), 9f);
            float ferrule = RoundedBoxSdf(q - new Vector2(0f, -6f), new Vector2(14f, 5f), 2f);
            // The blade widens towards its sharp edge.
            Vector2 b = q - new Vector2(0f, -28f);
            float halfWidth = 22f + (-b.y + 18f) * 0.45f;
            float blade = Mathf.Max(Mathf.Abs(b.x) - halfWidth, Mathf.Abs(b.y) - 18f);
            Color c = Clear;
            c = Over(c, Coverage(blade, Color.Lerp(new Color(0.62f, 0.66f, 0.7f), new Color(0.88f, 0.9f, 0.93f), Mathf.InverseLerp(18f, -18f, b.y))));
            c = Over(c, Coverage(ferrule, new Color(0.5f, 0.52f, 0.56f)));
            c = Over(c, Coverage(handle, new Color(0.85f, 0.25f, 0.2f)));
            c = Over(c, Coverage(RoundedBoxSdf(q - new Vector2(-4f, 30f), new Vector2(2.5f, 18f), 2.5f), new Color(1f, 1f, 1f, 0.35f)));
            return c;
        }

        private static Color DrawShower(int x, int y)
        {
            var p = new Vector2(x, y);
            // A pre-rinse spray head from above-right with a fan of water jets coming out of its face.
            Vector2 head = new Vector2(84f, 88f);
            float hose = RoundedBoxSdf(Rotate(p - new Vector2(104f, 108f), 45f), new Vector2(7f, 18f), 7f);
            float body = (p - head).magnitude - 22f;
            float face = (p - (head + new Vector2(-6f, -6f))).magnitude - 15f;
            Color c = Clear;
            // Jets: thin rays from the face downwards to the left, each breaking into drops.
            Vector2 origin = head + new Vector2(-12f, -12f);
            Vector2 d = p - origin;
            float distance = d.magnitude;
            if (distance > 6f && distance < 90f && d.y < 0f)
            {
                float angle = Mathf.Atan2(-d.y, -d.x) * Mathf.Rad2Deg; // 0 = straight left, 90 = straight down
                for (int jet = 0; jet < 5; jet++)
                {
                    float jetAngle = 22f + jet * 12f;
                    float across = Mathf.Abs(Mathf.DeltaAngle(angle, jetAngle)) * Mathf.Deg2Rad * distance;
                    bool drop = Mathf.Repeat(distance + jet * 7f, 16f) < 9f || distance < 40f;
                    if (drop)
                    {
                        c = Over(c, Coverage(across - 2.6f, new Color(0.35f, 0.7f, 1f, Mathf.InverseLerp(90f, 50f, distance))));
                    }
                }
            }

            c = Over(c, Coverage(hose, new Color(0.3f, 0.32f, 0.36f)));
            c = Over(c, Coverage(body, new Color(0.72f, 0.75f, 0.8f)));
            c = Over(c, Coverage(face, new Color(0.45f, 0.48f, 0.53f)));
            c = Over(c, Coverage((p - (head + new Vector2(6f, 8f))).magnitude - 5f, new Color(1f, 1f, 1f, 0.6f)));
            return c;
        }

        /// <summary>A fat rounded arrow pointing down, white with a dark rim (it reads over a white plate and dark steel).</summary>
        private static Color DrawArrow(int x, int y)
        {
            var p = new Vector2(x, y);
            // The head: a triangle at the bottom; the shaft: a rounded bar above it.
            float shaft = RoundedBoxSdf(p - new Vector2(64f, 112f), new Vector2(18f, 40f), 14f);
            Vector2 q = p - new Vector2(64f, 22f);
            float head = Mathf.Max(Mathf.Max(-q.y, q.y - 62f), Mathf.Abs(q.x) * 0.85f - q.y * 0.85f * 0.62f - 2f);
            float sdf = Mathf.Min(shaft, head - 6f);
            Color c = Coverage(sdf - 5f, new Color(0.08f, 0.09f, 0.1f, 0.8f));
            return Over(c, Coverage(sdf, Color.white));
        }

        /// <summary>A fingertip pressing down, seen from above: the nail, the finger, the start of the hand.</summary>
        private static Color DrawFinger(int x, int y)
        {
            var p = new Vector2(x, y);
            float finger = RoundedBoxSdf(p - new Vector2(64f, 70f), new Vector2(20f, 44f), 20f);
            float hand = RoundedBoxSdf(p - new Vector2(64f, 120f), new Vector2(40f, 22f), 18f);
            float body = Mathf.Min(finger, hand);
            Color c = Coverage(body - 4f, new Color(0.08f, 0.09f, 0.1f, 0.7f));
            c = Over(c, Coverage(body, new Color(0.98f, 0.86f, 0.75f)));
            c = Over(c, Coverage(RoundedBoxSdf(p - new Vector2(64f, 40f), new Vector2(11f, 12f), 9f), new Color(1f, 0.95f, 0.92f)));
            // A press ring under the tip.
            float ring = Mathf.Abs((p - new Vector2(64f, 26f)).magnitude - 20f) - 3f;
            return Over(Coverage(ring, new Color(1f, 1f, 1f, 0.7f)), c);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(r);
            float sin = Mathf.Sin(r);
            return new Vector2(v.x * cos - v.y * sin, v.x * sin + v.y * cos);
        }

        // ------------------------------------------------------------------
        // Minigame props
        // ------------------------------------------------------------------

        private static Color DrawWoodBoard(int x, int y)
        {
            float v = y / 63f; // 0 the front edge's bottom, 1 the far side of the top
            float wave = Noise1(x / 70f, 3) * 6f + Noise1(x / 23f, 7) * 1.5f;
            float grain = Mathf.Pow(Mathf.Abs(Mathf.Sin((y + wave) * 0.9f)), 6f); // thin dark lines along the board
            float fleck = Hash(x / 3, y) * 0.06f;
            Color c = Color.Lerp(new Color(0.8f, 0.6f, 0.37f), new Color(0.56f, 0.38f, 0.2f), grain * 0.55f + fleck);
            if (v < 0.3f)
            {
                c = Shade(c, 0.6f + v * 0.3f); // the front edge, out of the light
            }
            else
            {
                c = Color.Lerp(c, Color.white, Mathf.Clamp01((v - 0.9f) / 0.1f) * 0.15f);
            }

            if (Mathf.Abs(v - 0.3f) < 0.025f)
            {
                c = Color.Lerp(c, new Color(0.92f, 0.76f, 0.55f), 0.55f); // the arris catches the light
            }

            c.a = Mathf.Clamp01(0.5f - RoundedBoxSdf(new Vector2(x - 255.5f, y - 31.5f), new Vector2(256f, 32f), 10f));
            return c;
        }

        private static Color DrawSteelCounter(int x, int y)
        {
            float v = y / 127f;
            float brush = Noise1(y * 1.7f + Noise1(x / 40f, 5) * 3f, 9);
            float shade = 0.5f + 0.12f * brush + 0.1f * v;
            float underBoard = Mathf.Clamp01((v - 0.78f) / 0.22f);
            shade *= 1f - 0.5f * underBoard * underBoard; // the board's shadow along the top
            if (v < 0.12f)
            {
                shade *= 0.7f; // the counter's front lip
            }
            else if (v < 0.14f)
            {
                shade += 0.2f; // its lit edge
            }

            var c = new Color(shade, shade * 1.02f, shade * 1.06f);
            c.a = Mathf.Clamp01(0.5f - RoundedBoxSdf(new Vector2(x - 127.5f, y - 63.5f), new Vector2(128f, 64f), 14f));
            return c;
        }

        private static Color DrawVegetable(int x, int y)
        {
            var p = new Vector2(x - 63.5f, y - 63.5f);
            float sdf = RoundedBoxSdf(p, new Vector2(63f, 62f), 36f);
            float n = p.y / 62f; // -1 underneath .. 1 on top
            float round = Mathf.Sqrt(Mathf.Max(0f, 1f - n * n));
            float shade = 0.55f + 0.45f * (round * 0.75f + n * 0.3f);
            shade += Mathf.Exp(-Mathf.Pow((n - 0.55f) / 0.13f, 2f)) * 0.22f; // a sheen along the top
            shade += 0.035f * Mathf.Sin(y * 0.8f + Noise1(x / 30f, 2) * 4f); // faint streaks along the skin
            float end = Mathf.Clamp01((Mathf.Abs(p.x) - 28f) / 36f);
            shade *= 1f - 0.22f * end * end; // the rounded ends turn away from the light
            return Coverage(sdf, Shade(Color.white, shade));
        }

        private static Color DrawPiece(int x, int y)
        {
            float v = y / 31f;
            float shade = v > 0.62f ? 1f : 0.8f - (0.62f - v) * 0.35f; // a lit top face, a shaded side
            return Coverage(RoundedBoxSdf(new Vector2(x - 15.5f, y - 15.5f), new Vector2(16f, 16f), 7f), Shade(Color.white, shade));
        }

        private static Color DrawCylinder(int x, int y)
        {
            float n = (x - 15.5f) / 16f;
            float round = Mathf.Sqrt(Mathf.Max(0f, 1f - n * n));
            float shade = 0.5f + 0.5f * (round * 0.85f - n * 0.2f) + Mathf.Exp(-Mathf.Pow((n + 0.35f) / 0.15f, 2f)) * 0.25f;
            return Coverage(RoundedBoxSdf(new Vector2(x - 15.5f, y - 31.5f), new Vector2(16f, 32f), 6f), Shade(Color.white, shade));
        }

        private static Color DrawBlade(int x, int y)
        {
            float u = x / 31f; // 0 the cutting edge .. 1 the spine
            float shade = Mathf.Lerp(0.82f, 0.6f, u);
            if (x < 6)
            {
                shade = 0.97f - x * 0.02f; // the ground bevel along the edge
            }

            shade += Mathf.Exp(-Mathf.Pow((x * 0.6f - y * 0.25f + 20f) / 6f, 2f)) * 0.14f; // a long reflection
            var c = new Color(shade, shade * 1.01f, shade * 1.04f);
            return Coverage(RoundedBoxSdf(new Vector2(x - 15.5f, y - 63.5f), new Vector2(16f, 64f), 3f), c);
        }

        private static Color DrawClaw(int x, int y)
        {
            var p = new Vector2(x, y);
            float palm = RoundedBoxSdf(p - new Vector2(128f, 48f), new Vector2(60f, 30f), 26f);
            float[] fingerY = { 22f, 48f, 74f };
            var fingers = new float[3];
            float body = palm;
            for (int i = 0; i < 3; i++)
            {
                fingers[i] = RoundedBoxSdf(p - new Vector2(60f, fingerY[i]), new Vector2(38f, 13f), 13f);
                body = Mathf.Min(body, fingers[i]);
            }

            float v = y / 95f;
            Color c = Shade(new Color(0.9f, 0.68f, 0.54f), 0.8f + 0.3f * v);
            // Where a finger lies over the palm or its neighbour: a soft crease.
            for (int i = 0; i < 3; i++)
            {
                if (Mathf.Abs(fingers[i]) < 1.4f && body < -1.4f)
                {
                    c = Shade(c, 0.78f);
                }

                float nail = RoundedBoxSdf(p - new Vector2(28f, fingerY[i] + 3f), new Vector2(6f, 5f), 4f);
                c = Over(c, Coverage(nail, new Color(1f, 0.86f, 0.8f, 0.9f)));
                float knuckle = (p - new Vector2(90f, fingerY[i] + 5f)).magnitude;
                c = Color.Lerp(c, Color.white, Mathf.Clamp01(1f - knuckle / 10f) * 0.18f);
            }

            Color outline = Coverage(body - 1.5f, new Color(0.45f, 0.28f, 0.2f));
            return Over(outline, Coverage(body, c));
        }

        private static Color DrawBasin(int x, int y)
        {
            var p = new Vector2(x - 127.5f, y - 127.5f);
            float r = p.magnitude / 127.5f;
            float shade = 0.12f + 0.07f * r - 0.05f * Mathf.Clamp01((r - 0.82f) / 0.1f);
            shade += Mathf.Exp(-Mathf.Pow((r - 0.95f) / 0.025f, 2f)) * 0.32f * (0.6f + 0.4f * p.y / 127.5f); // the rim, lit from above
            var c = new Color(shade, shade + 0.01f, shade + 0.02f);
            return Coverage(p.magnitude - 126.5f, c);
        }

        private static Color DrawFilter(int x, int y)
        {
            var p = new Vector2(x - 127.5f, y - 127.5f);
            float r = p.magnitude;
            float lathe = Mathf.Sin(r * 1.9f) * 0.025f * Noise1(r * 0.3f, 13);
            float shade = 0.58f + lathe + 0.12f * (p.y - p.x) / 255f;
            float rim = Mathf.Clamp01((r - 118f) / 8f);
            shade += rim * 0.18f * Mathf.Clamp(p.y / 40f, -1f, 1f); // a bevelled rim: lit above, shadowed below, turning smoothly
            var c = new Color(shade, shade * 1.02f, shade * 1.05f);
            return Coverage(r - 126.5f, c);
        }

        private static Color DrawHole(int x, int y)
        {
            var p = new Vector2(x - 15.5f, y - 15.5f);
            float r = p.magnitude / 15f;
            float shade = 0.07f + 0.12f * Mathf.Clamp01(-p.y / 15f) * Mathf.Clamp01((r - 0.5f) / 0.4f); // the lower wall lit
            shade += Mathf.Exp(-Mathf.Pow((r - 0.94f) / 0.06f, 2f)) * (p.y < 0f ? 0.25f : 0.08f);
            return Coverage(p.magnitude - 15f, new Color(shade, shade, shade + 0.01f));
        }

        private static Color DrawScrap(int x, int y)
        {
            var p = new Vector2(x - 63.5f, y - 63.5f);
            float a = Mathf.Atan2(p.y, p.x);
            float radius = 50f + 5f * Mathf.Sin(a * 3f + 1.3f) + 3f * Mathf.Sin(a * 5f + 0.4f) + 2f * Mathf.Sin(a * 9f + 2f);
            Vector2 n = p / radius;
            float shade = 0.66f + 0.34f * Vector2.Dot(n, new Vector2(-0.5f, 0.6f)) - 0.18f * Mathf.Clamp01(n.magnitude - 0.75f) * 4f;
            shade -= Hash(x / 4, y / 4) * 0.12f; // crumbs and texture
            shade += Mathf.Exp(-(p - new Vector2(-18f, 22f)).sqrMagnitude / 90f) * 0.45f; // a wet highlight
            return Coverage(p.magnitude - radius, Shade(Color.white, shade));
        }

        private static Color DrawDome(int x, int y)
        {
            var p = new Vector2(x - 63.5f, y - 63.5f);
            Vector2 n = p / 60f;
            float z = Mathf.Sqrt(Mathf.Max(0f, 1f - n.sqrMagnitude));
            var normal = new Vector3(n.x, n.y, z);
            float shade = 0.5f + 0.5f * Vector3.Dot(normal, new Vector3(-0.4f, 0.6f, 0.7f).normalized);
            shade += Mathf.Exp(-(n - new Vector2(-0.3f, 0.35f)).sqrMagnitude / 0.03f) * 0.5f; // the shine
            shade *= n.magnitude > 0.92f ? 0.75f : 1f; // the rim
            return Coverage(p.magnitude - 60f, Shade(Color.white, shade));
        }

        private static Color DrawBar(int x, int y)
        {
            float n = (y - 31.5f) / 30f;
            float round = Mathf.Sqrt(Mathf.Max(0f, 1f - n * n));
            float shade = 0.55f + 0.45f * (round * 0.8f + n * 0.3f) + Mathf.Exp(-Mathf.Pow((n - 0.45f) / 0.12f, 2f)) * 0.25f;
            return Coverage(RoundedBoxSdf(new Vector2(x - 63.5f, y - 31.5f), new Vector2(64f, 30f), 28f), Shade(Color.white, shade));
        }

        /// <summary>A colour made lighter or darker, its alpha kept (clamped to white).</summary>
        private static Color Shade(Color c, float by)
        {
            return new Color(Mathf.Clamp01(c.r * by), Mathf.Clamp01(c.g * by), Mathf.Clamp01(c.b * by), c.a);
        }

        private static float Hash(int x, int y)
        {
            unchecked
            {
                int h = x * 374761393 + y * 668265263;
                h = (h ^ (h >> 13)) * 1274126177;
                return ((h ^ (h >> 16)) & 0xffff) / 65535f;
            }
        }

        /// <summary>Smooth 1D value noise, 0-1 (a different curve per seed).</summary>
        private static float Noise1(float x, int seed)
        {
            int i = Mathf.FloorToInt(x);
            float f = x - i;
            f = f * f * (3f - 2f * f);
            return Mathf.Lerp(Hash(i, seed), Hash(i + 1, seed), f);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static readonly Color Clear = new Color(1f, 1f, 1f, 0f);

        private static float RoundedBoxSdf(Vector2 p, Vector2 halfSize, float radius)
        {
            // p relative to the box centre (or absolute for the full-texture rounded rect).
            Vector2 d = new Vector2(Mathf.Abs(p.x), Mathf.Abs(p.y)) - (halfSize - new Vector2(radius, radius));
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - radius;
        }

        /// <summary>Colour with alpha from a signed distance (negative inside), 1 px antialiasing.</summary>
        private static Color Coverage(float signedDistance, Color color)
        {
            color.a *= Mathf.Clamp01(0.5f - signedDistance);
            return color;
        }

        private static Color Over(Color below, Color above)
        {
            float a = above.a + below.a * (1f - above.a);
            if (a <= 0f)
            {
                return Clear;
            }

            Color c = (above * above.a + below * below.a * (1f - above.a)) / a;
            c.a = a;
            return c;
        }

        private static void Ensure(string name, int width, int height, int border, System.Func<int, int, Color> pixel)
        {
            string path = $"{Folder}/{name}.png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                var pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // The rounded rect's SDF takes coordinates relative to its centre.
                        pixels[y * width + x] = name == "RoundedRect"
                            ? pixel(x - width / 2, y - height / 2)
                            : pixel(x, y);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            ConfigureSprite(path, border);
        }

        private static void ConfigureSprite(string path, int border)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.spriteBorder = new Vector4(border, border, border, border);
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; // required for 9-slicing
                importer.SetTextureSettings(settings);
                importer.SaveAndReimport();
            }
        }

        private static Sprite Load(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Sprite>($"{Folder}/{name}.png");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
