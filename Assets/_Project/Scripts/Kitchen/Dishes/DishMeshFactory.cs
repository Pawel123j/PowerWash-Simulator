using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// Procedural dish meshes, built by spinning a 2D profile around the Y axis. Every shape fits the
    /// same footprint as Unity's built-in Cylinder primitive (radius 0.5, height 2, centred), so the
    /// DishDefinition scales keep meaning the same thing. Two properties matter for gameplay:
    /// - UVs are a top-down planar projection, uv = (x + 0.5, z + 0.5), on every vertex. A point on the
    ///   dish maps to the dirt texture by its local XZ alone, so scrubbing can paint exactly where the
    ///   finger is (no MeshCollider textureCoord lookups needed).
    /// - They are used for convex MeshColliders. The primitive's CapsuleCollider degenerates into a sphere
    ///   on a flat plate (radius 0.45, height 0.12), so plates rolled and piled up like balls.
    /// </summary>
    public static class DishMeshFactory
    {
        private const int Segments = 40;
        private const float HardEdgeDegrees = 40f;

        private static readonly Dictionary<DishShape, Mesh> Cache = new Dictionary<DishShape, Mesh>();

        public static Mesh Cylinder => Get(DishShape.Cylinder);

        public static Mesh Get(DishShape shape)
        {
            if (!Cache.TryGetValue(shape, out Mesh mesh) || mesh == null)
            {
                mesh = BuildLathe(shape.ToString(), Profile(shape), ExteriorFrom(shape));
                Cache[shape] = mesh;
            }

            return mesh;
        }

        /// <summary>
        /// Where the dirt texture keeps a dish's outside (<see cref="BuildLathe"/>): a patch in its corner, outside
        /// the disc the dirt lives on, which <see cref="DishDecor"/> paints and scrubbing never reaches.
        /// </summary>
        public static readonly Rect ExteriorUv = new Rect(0.01f, 0.01f, 0.05f, 0.05f);

        /// <summary>
        /// The ring of the dirt texture a dish's tall walls are projected onto (from, to radius in UV), or null for a
        /// shallow dish. The planar projection squeezes a wall's whole height into this thin ring, so anything that
        /// varies round it is stretched into vertical stripes down the wall (<see cref="DirtCanvas"/> lays tide lines
        /// there instead). A pan's outside has its own patch (<see cref="ExteriorUv"/>); a glass's outside shares the ring.
        /// </summary>
        public static Vector2? WallBand(DishShape shape)
        {
            switch (shape)
            {
                case DishShape.Pan:
                    return new Vector2(0.4f, 0.45f);
                case DishShape.Glass:
                    return new Vector2(0.38f, 0.5f);
                case DishShape.Bowl:
                    return new Vector2(0.36f, 0.48f); // only the steep upper wall: the lower curve is near flat from above
                case DishShape.Pot:
                    return new Vector2(0.42f, 0.47f);
                case DishShape.Mug:
                    return new Vector2(0.38f, 0.43f);
                case DishShape.Roaster:
                    return new Vector2(0.41f, 0.46f);
                default:
                    return null;
            }
        }

        /// <summary>The dish's floor inside, in its local (unscaled) units: x its radius, y its height.</summary>
        public static Vector2 Floor(DishShape shape)
        {
            return Profile(shape)[1];
        }

        /// <summary>The first profile segment that is the dish's outside, or -1 when the planar projection covers all of
        /// it. A pan's tall outer wall under the planar projection sampled the ring at the rim, and the dirt there
        /// ran down it in vertical stripes; a real pan's outside is its own, plainer surface.</summary>
        private static int ExteriorFrom(DishShape shape)
        {
            switch (shape)
            {
                case DishShape.Pan:
                case DishShape.Pot:
                case DishShape.Mug:
                    return 3; // from the rim down the outside wall
                case DishShape.Roaster:
                    return 4; // past the flat lip
                case DishShape.Bowl:
                    return 5;
                default:
                    return -1;
            }
        }

        private static Mesh _leftovers;
        private static Texture2D _leftoversPalette;

        /// <summary>
        /// Scraps of a meal, heaped where the food sat: a few bumpy lumps of different sizes, each coloured from a
        /// small palette texture (sauce, fried bits, potato, a pea), in a unit-sized heap the dish scales to fit.
        /// Built once and shared.
        /// </summary>
        public static Mesh Leftovers => _leftovers != null ? _leftovers : _leftovers = BuildLeftovers();

        /// <summary>The colours <see cref="Leftovers"/> picks from (one texel each).</summary>
        public static Texture2D LeftoversPalette
        {
            get
            {
                if (_leftoversPalette == null)
                {
                    _leftoversPalette = new Texture2D(PaletteColors.Length, 1, TextureFormat.RGBA32, false)
                    {
                        name = "LeftoversPalette",
                        filterMode = FilterMode.Point,
                        wrapMode = TextureWrapMode.Clamp
                    };
                    _leftoversPalette.SetPixels(PaletteColors);
                    _leftoversPalette.Apply(false, true);
                }

                return _leftoversPalette;
            }
        }

        private static readonly Color[] PaletteColors =
        {
            new Color(0.45f, 0.13f, 0.05f), // tomato sauce
            new Color(0.36f, 0.2f, 0.09f),  // fried, browned
            new Color(0.82f, 0.68f, 0.42f), // potato, pasta
            new Color(0.3f, 0.5f, 0.12f),   // a pea
            new Color(0.58f, 0.3f, 0.1f)    // gravy
        };

        private static Mesh BuildLeftovers()
        {
            var random = new System.Random(2004);
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            const int lumps = 7;
            for (int i = 0; i < lumps; i++)
            {
                float angle = (float)random.NextDouble() * Mathf.PI * 2f;
                float distance = i == 0 ? 0f : 0.12f + 0.2f * (float)random.NextDouble();
                float size = i == 0 ? 0.2f : 0.08f + 0.09f * (float)random.NextDouble();
                var centre = new Vector3(Mathf.Cos(angle) * distance, size * 0.45f, Mathf.Sin(angle) * distance);
                int colour = i == lumps - 1 ? 3 : random.Next(0, PaletteColors.Length);
                if (colour == 3 && i != lumps - 1)
                {
                    colour = 0; // one pea is plenty
                }

                var uv = new Vector2((colour + 0.5f) / PaletteColors.Length, 0.5f);
                // A pea is round; everything else is squashed and lumpy.
                float lumpiness = colour == 3 ? 0.03f : 0.28f;
                var squash = colour == 3 ? Vector3.one : new Vector3(1f + 0.4f * (float)random.NextDouble(), 0.55f, 1f + 0.2f * (float)random.NextDouble());
                AddLump(vertices, uvs, triangles, centre, size, squash, lumpiness, uv, random.Next());
            }

            var mesh = new Mesh { name = "DishLeftovers" };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>One closed lump: a squashed sphere pushed in and out by a few waves, with one vertex per pole and no
        /// seam, so RecalculateNormals shades it smoothly all round.</summary>
        private static void AddLump(List<Vector3> vertices, List<Vector2> uvs, List<int> triangles,
            Vector3 centre, float radius, Vector3 squash, float lumpiness, Vector2 uv, int seed)
        {
            const int rings = 7;
            const int sectors = 10;
            var random = new System.Random(seed);
            float phaseA = (float)random.NextDouble() * 6.3f;
            float phaseB = (float)random.NextDouble() * 6.3f;

            Vector3 Point(float polar, float azimuth)
            {
                var direction = new Vector3(Mathf.Sin(polar) * Mathf.Cos(azimuth), Mathf.Cos(polar), Mathf.Sin(polar) * Mathf.Sin(azimuth));
                float bump = 1f + lumpiness * (0.6f * Mathf.Sin(azimuth * 3f + phaseA) * Mathf.Sin(polar * 2f + phaseB) +
                                               0.4f * Mathf.Sin(azimuth * 5f + phaseB + polar * 3f));
                return centre + Vector3.Scale(direction, squash) * (radius * bump);
            }

            int top = vertices.Count;
            vertices.Add(Point(0f, 0f));
            for (int r = 1; r < rings; r++)
            {
                for (int s = 0; s < sectors; s++)
                {
                    vertices.Add(Point(Mathf.PI * r / rings, Mathf.PI * 2f * s / sectors));
                }
            }

            int bottom = vertices.Count;
            vertices.Add(Point(Mathf.PI, 0f));
            for (int i = top; i < vertices.Count; i++)
            {
                uvs.Add(uv);
            }

            int Ring(int r, int s) => top + 1 + (r - 1) * sectors + s % sectors;

            // Clockwise seen from outside (Unity's front faces).
            for (int s = 0; s < sectors; s++)
            {
                triangles.Add(top);
                triangles.Add(Ring(1, s + 1));
                triangles.Add(Ring(1, s));

                for (int r = 1; r < rings - 1; r++)
                {
                    triangles.Add(Ring(r, s));
                    triangles.Add(Ring(r, s + 1));
                    triangles.Add(Ring(r + 1, s + 1));
                    triangles.Add(Ring(r, s));
                    triangles.Add(Ring(r + 1, s + 1));
                    triangles.Add(Ring(r + 1, s));
                }

                triangles.Add(Ring(rings - 1, s));
                triangles.Add(Ring(rings - 1, s + 1));
                triangles.Add(bottom);
            }
        }

        /// <summary>
        /// Non-colliding extras that make a shape read as what it is: the pan's handle, the pot's two loop handles, the
        /// mug's ear. Loops are built at their real size and set under the dish with its scale undone, so a squat pot
        /// does not squash them; they take their colour from the dish's outside (its <see cref="ExteriorUv"/> patch).
        /// </summary>
        public static void AddDetails(GameObject dish, DishShape shape, Material detailMaterial)
        {
            switch (shape)
            {
                case DishShape.Pan:
                    var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    handle.name = "Handle";
                    Object.Destroy(handle.GetComponent<Collider>());
                    handle.transform.SetParent(dish.transform, false);
                    // Local units: the pan body spans x in [-0.5, 0.5] and y in [-1, 1].
                    handle.transform.localPosition = new Vector3(0.88f, 0.55f, 0f);
                    handle.transform.localScale = new Vector3(0.8f, 0.45f, 0.1f);
                    handle.GetComponent<Renderer>().sharedMaterial = detailMaterial;
                    break;

                case DishShape.Pot:
                    // Flat loops sticking out either side, just under the rim.
                    AddLoop(dish, shape, "HandleRight", new Vector2(0.5f, 0.72f), Quaternion.identity);
                    AddLoop(dish, shape, "HandleLeft", new Vector2(-0.5f, 0.72f), Quaternion.Euler(0f, 180f, 0f));
                    break;

                case DishShape.Mug:
                    // The ear: an upright loop on the right.
                    AddLoop(dish, shape, "Ear", new Vector2(0.5f, 0.05f), Quaternion.Euler(90f, 0f, 0f));
                    break;

                case DishShape.Roaster:
                    // Small flat ears at the two ends of the oval, just under the lip.
                    AddLoop(dish, shape, "EarRight", new Vector2(0.5f, 0.75f), Quaternion.identity);
                    AddLoop(dish, shape, "EarLeft", new Vector2(-0.5f, 0.75f), Quaternion.Euler(0f, 180f, 0f));
                    break;
            }
        }

        private static readonly Dictionary<DishShape, Mesh> LoopCache = new Dictionary<DishShape, Mesh>();

        /// <summary>A half ring in real units, centred on the dish's side at (x, height) in its local units.</summary>
        private static void AddLoop(GameObject dish, DishShape shape, string name, Vector2 localSide, Quaternion turn)
        {
            if (!LoopCache.TryGetValue(shape, out Mesh mesh) || mesh == null)
            {
                mesh = shape == DishShape.Mug ? BuildLoop(0.075f, 0.065f, 0.018f)
                    : shape == DishShape.Roaster ? BuildLoop(0.055f, 0.075f, 0.012f)
                    : BuildLoop(0.075f, 0.06f, 0.014f);
                LoopCache[shape] = mesh;
            }

            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = dish.GetComponent<Renderer>().sharedMaterial;
            go.transform.SetParent(dish.transform, false);
            Vector3 scale = dish.transform.localScale;
            go.transform.localPosition = new Vector3(localSide.x, localSide.y, 0f);
            go.transform.localRotation = turn;
            go.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        }

        /// <summary>
        /// Half a ring lying in the XZ plane and bulging towards +x (rotate it upright for a mug's ear): `reach` out from
        /// the dish's side, `span` along it, a tube of `thickness` radius. UVs all on the exterior patch, so the loop
        /// wears the dish's outside colour.
        /// </summary>
        private static Mesh BuildLoop(float reach, float span, float thickness)
        {
            const int along = 16;
            const int around = 10;
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            var uv = new Vector2(ExteriorUv.center.x, ExteriorUv.center.y);
            for (int i = 0; i <= along; i++)
            {
                float t = -Mathf.PI * 0.5f + Mathf.PI * i / along;
                var centre = new Vector3(Mathf.Cos(t) * reach, 0f, Mathf.Sin(t) * span);
                var tangent = new Vector3(-Mathf.Sin(t) * reach, 0f, Mathf.Cos(t) * span).normalized;
                Vector3 outward = Vector3.Cross(Vector3.up, tangent).normalized;
                for (int j = 0; j <= around; j++)
                {
                    float a = Mathf.PI * 2f * j / around;
                    Vector3 n = outward * Mathf.Cos(a) + Vector3.up * Mathf.Sin(a);
                    vertices.Add(centre + n * thickness);
                    normals.Add(n);
                    uvs.Add(uv);
                }
            }

            int row = around + 1;
            for (int i = 0; i < along; i++)
            {
                for (int j = 0; j < around; j++)
                {
                    int a = i * row + j;
                    int b = a + row;
                    triangles.Add(a);
                    triangles.Add(a + 1);
                    triangles.Add(b);
                    triangles.Add(a + 1);
                    triangles.Add(b + 1);
                    triangles.Add(b);
                }
            }

            var mesh = new Mesh { name = "DishLoop" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>(radius, height) points from the top centre, outwards and down, to the bottom centre.</summary>
        private static Vector2[] Profile(DishShape shape)
        {
            switch (shape)
            {
                case DishShape.Plate:
                    return new[]
                    {
                        new Vector2(0f, -0.15f), new Vector2(0.27f, -0.15f), // shallow well
                        new Vector2(0.36f, 0.55f), new Vector2(0.45f, 1f),   // slope up to the rim
                        new Vector2(0.5f, 0.8f),                            // rolled lip
                        new Vector2(0.44f, 0.1f), new Vector2(0.3f, -0.7f), // underside
                        new Vector2(0.28f, -1f), new Vector2(0.22f, -1f),   // foot ring
                        new Vector2(0.2f, -0.8f), new Vector2(0f, -0.8f)
                    };

                case DishShape.Pan:
                    return new[]
                    {
                        new Vector2(0f, -0.55f), new Vector2(0.4f, -0.55f), // cooking surface
                        new Vector2(0.45f, 1f), new Vector2(0.5f, 1f),      // flared wall and its rim
                        new Vector2(0.46f, -1f), new Vector2(0f, -1f)       // outside wall, base
                    };

                case DishShape.Glass:
                    return new[]
                    {
                        new Vector2(0f, -0.8f), new Vector2(0.38f, -0.8f),  // inside bottom
                        new Vector2(0.46f, 1f), new Vector2(0.5f, 1f),      // inner wall, rim
                        new Vector2(0.42f, -1f), new Vector2(0f, -1f)       // tapered outside, base
                    };

                case DishShape.Bowl:
                    return new[]
                    {
                        new Vector2(0f, -0.55f), new Vector2(0.22f, -0.55f), // a small flat floor
                        new Vector2(0.33f, -0.25f), new Vector2(0.42f, 0.3f), new Vector2(0.47f, 0.88f), // the curve up
                        new Vector2(0.5f, 0.98f),                            // rolled rim
                        new Vector2(0.48f, 0.55f), new Vector2(0.4f, -0.25f), new Vector2(0.28f, -0.8f), // outside
                        new Vector2(0.24f, -1f), new Vector2(0.18f, -1f),   // foot ring
                        new Vector2(0.16f, -0.85f), new Vector2(0f, -0.85f)
                    };

                case DishShape.Pot:
                    return new[]
                    {
                        new Vector2(0f, -0.8f), new Vector2(0.43f, -0.8f),  // a wide flat bottom
                        new Vector2(0.46f, 1f), new Vector2(0.5f, 1f),      // straight wall and its rim
                        new Vector2(0.5f, -0.94f), new Vector2(0.46f, -1f), new Vector2(0f, -1f) // outside, rounded base edge
                    };

                case DishShape.Mug:
                    return new[]
                    {
                        new Vector2(0f, -0.72f), new Vector2(0.38f, -0.72f), // inside bottom
                        new Vector2(0.42f, 1f), new Vector2(0.5f, 1f),       // inner wall, thick rim
                        new Vector2(0.5f, -0.95f), new Vector2(0.45f, -1f), new Vector2(0f, -1f) // outside, base
                    };

                case DishShape.Roaster:
                    // Stretched into an oval by its scale: a wide flat bottom, sloping sides, a flat lip all round.
                    return new[]
                    {
                        new Vector2(0f, -0.6f), new Vector2(0.38f, -0.6f),  // bottom
                        new Vector2(0.44f, 0.8f), new Vector2(0.45f, 1f),   // sloping side up to the lip
                        new Vector2(0.5f, 1f),                             // the flat lip
                        new Vector2(0.48f, 0.7f), new Vector2(0.42f, -1f), new Vector2(0f, -1f) // outside, base
                    };

                default:
                    return new[]
                    {
                        new Vector2(0f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(0.5f, -1f), new Vector2(0f, -1f)
                    };
            }
        }

        /// <param name="exteriorFrom">Segments from this one on are the dish's outside: their UVs go to
        /// <see cref="ExteriorUv"/> instead of the planar projection (-1: none).</param>
        private static Mesh BuildLathe(string name, IReadOnlyList<Vector2> profile, int exteriorFrom = -1)
        {
            int segmentCount = profile.Count - 1;

            // Outward normal of each profile segment (walking top-centre -> bottom-centre, the surface
            // is on the left, so outward is the direction rotated +90 degrees).
            var segmentNormals = new Vector2[segmentCount];
            for (int i = 0; i < segmentCount; i++)
            {
                Vector2 dir = (profile[i + 1] - profile[i]).normalized;
                segmentNormals[i] = new Vector2(-dir.y, dir.x);
            }

            // Each profile point becomes one ring of vertices - or two at a hard edge (rim, base), so the
            // edge stays crisp instead of being smoothed into a blob.
            var ringPoints = new List<Vector2>();
            var ringNormals = new List<Vector2>();
            var ringExterior = new List<bool>();
            var segmentStartRing = new int[segmentCount];
            var segmentEndRing = new int[segmentCount];
            bool Exterior(int segment) => exteriorFrom >= 0 && segment >= exteriorFrom;
            for (int p = 0; p < profile.Count; p++)
            {
                bool hasIncoming = p > 0;
                bool hasOutgoing = p < segmentCount;
                if (hasIncoming && hasOutgoing && Vector2.Angle(segmentNormals[p - 1], segmentNormals[p]) < HardEdgeDegrees)
                {
                    ringPoints.Add(profile[p]);
                    ringNormals.Add((segmentNormals[p - 1] + segmentNormals[p]).normalized);
                    ringExterior.Add(Exterior(p));
                    segmentEndRing[p - 1] = ringPoints.Count - 1;
                    segmentStartRing[p] = ringPoints.Count - 1;
                    continue;
                }

                if (hasIncoming)
                {
                    ringPoints.Add(profile[p]);
                    ringNormals.Add(segmentNormals[p - 1]);
                    ringExterior.Add(Exterior(p - 1));
                    segmentEndRing[p - 1] = ringPoints.Count - 1;
                }

                if (hasOutgoing)
                {
                    ringPoints.Add(profile[p]);
                    ringNormals.Add(segmentNormals[p]);
                    ringExterior.Add(Exterior(p));
                    segmentStartRing[p] = ringPoints.Count - 1;
                }
            }

            int ringSize = Segments + 1;
            var vertices = new List<Vector3>(ringPoints.Count * ringSize);
            var normals = new List<Vector3>(ringPoints.Count * ringSize);
            var uvs = new List<Vector2>(ringPoints.Count * ringSize);
            for (int r = 0; r < ringPoints.Count; r++)
            {
                for (int s = 0; s <= Segments; s++)
                {
                    float angle = s * Mathf.PI * 2f / Segments;
                    float cos = Mathf.Cos(angle);
                    float sin = Mathf.Sin(angle);
                    var position = new Vector3(cos * ringPoints[r].x, ringPoints[r].y, sin * ringPoints[r].x);
                    vertices.Add(position);
                    normals.Add(new Vector3(cos * ringNormals[r].x, ringNormals[r].y, sin * ringNormals[r].x).normalized);
                    uvs.Add(ringExterior[r]
                        ? new Vector2(ExteriorUv.x + ExteriorUv.width * s / Segments, ExteriorUv.y + ExteriorUv.height * (position.y + 1f) * 0.5f)
                        : new Vector2(position.x + 0.5f, position.z + 0.5f));
                }
            }

            var triangles = new List<int>(segmentCount * Segments * 6);
            for (int i = 0; i < segmentCount; i++)
            {
                int a = segmentStartRing[i] * ringSize;
                int b = segmentEndRing[i] * ringSize;
                for (int s = 0; s < Segments; s++)
                {
                    // Clockwise seen from the side the normal points to (Unity's front-face winding).
                    triangles.Add(a + s);
                    triangles.Add(a + s + 1);
                    triangles.Add(b + s);
                    triangles.Add(a + s + 1);
                    triangles.Add(b + s + 1);
                    triangles.Add(b + s);
                }
            }

            var mesh = new Mesh { name = $"Dish{name}" };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }
    }
}
