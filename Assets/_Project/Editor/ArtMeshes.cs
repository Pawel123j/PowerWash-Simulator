using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Procedural meshes for the set: boxes with rounded edges and corners - what makes steel and plastic
    /// catch a highlight along their edges instead of ending in a hard, unlit line - open-topped ones for tubs,
    /// and inward-facing ones for the inside of a basin seen from above. UVs are planar in metres, so a
    /// texture keeps its scale across pieces of any size. Meshes are saved under Art/Meshes and updated in
    /// place, so scenes keep their references when the setup runs again.
    /// </summary>
    internal static class ArtMeshes
    {
        private const string MeshFolder = "Assets/_Project/Art/Meshes";

        /// <param name="size">Outer size in metres.</param>
        /// <param name="radius">Edge and corner radius (clamped to half the thinnest side).</param>
        /// <param name="segments">Steps across each half of a rounded edge.</param>
        /// <param name="openTop">Leave out the top face (a tub, a basin).</param>
        /// <param name="inward">Faces point inwards: the inside of a basin.</param>
        /// <param name="uvOrigin">Where the box sits in the world, for pieces whose texture must run on from one to the next.</param>
        public static Mesh RoundedBox(string name, Vector3 size, float radius, int segments = 4, bool openTop = false, bool inward = false,
            Vector3 uvOrigin = default)
        {
            Vector3 half = size * 0.5f;
            radius = Mathf.Min(radius, Mathf.Min(half.x, Mathf.Min(half.y, half.z)));
            Vector3 inner = half - Vector3.one * radius;
            float[][] steps = { AxisSteps(half.x, inner.x, segments), AxisSteps(half.y, inner.y, segments), AxisSteps(half.z, inner.z, segments) };

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            for (int axis = 0; axis < 3; axis++)
            {
                for (int sign = -1; sign <= 1; sign += 2)
                {
                    if (openTop && axis == 1 && sign > 0)
                    {
                        continue;
                    }

                    AddFace(axis, sign, half, inner, radius, steps, inward, uvOrigin, vertices, normals, uvs, triangles);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return Save(mesh);
        }

        /// <summary>
        /// A lumpy ball - a wad of steel wool: a UV sphere whose radius wanders with a few octaves of noise, so it
        /// catches light in clumps. UVs wrap around it (for a fibre texture). Radius 1 before scaling by <paramref name="radius"/>.
        /// </summary>
        public static Mesh LumpySphere(string name, float radius, float roughness, int seed)
        {
            const int rings = 18;
            const int segments = 28;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float v = r / (float)rings;
                float theta = v * Mathf.PI;
                for (int s = 0; s <= segments; s++)
                {
                    float u = s / (float)segments;
                    float phi = u * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Sin(theta) * Mathf.Cos(phi), Mathf.Cos(theta), Mathf.Sin(theta) * Mathf.Sin(phi));
                    // Seamless around the sphere: sample noise by direction, not by u.
                    float lumps = Mathf.PerlinNoise(dir.x * 1.7f + seed, dir.y * 1.7f + dir.z * 0.9f + seed * 0.37f) * 0.6f +
                                  Mathf.PerlinNoise(dir.z * 4.1f + seed * 1.3f, dir.x * 3.3f + dir.y * 2.1f) * 0.4f;
                    float bumpy = 1f + (lumps - 0.5f) * 2f * roughness;
                    vertices.Add(dir * (radius * bumpy * (r == 0 || r == rings ? 0.92f : 1f)));
                    uvs.Add(new Vector2(u * 3f, v * 2f));
                }
            }

            int row = segments + 1;
            for (int r = 0; r < rings; r++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = r * row + s;
                    int b = a + 1;
                    int c = a + row + 1;
                    int d = a + row;
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return Save(mesh);
        }

        /// <summary>
        /// A limb or a sleeve: a tube running from z = 0 back to z = -<paramref name="length"/>, its radius following
        /// <paramref name="radii"/> (evenly spaced stations, interpolated smoothly between them) and its cross-section an
        /// ellipse <paramref name="flatten"/> times as tall as it is wide - a forearm is flatter than it is round. The
        /// far end is open (off the screen); the near one too, or rounded off with <paramref name="roundStart"/> where it
        /// would show. UVs in metres round and along it; normals are worked out from the surface itself, so the seam
        /// where the UVs wrap does not show.
        /// </summary>
        public static Mesh Tube(string name, float length, float[] radii, float flatten, bool roundStart = false, int segments = 20)
        {
            float Radius(float t)
            {
                float x = Mathf.Clamp01(t) * (radii.Length - 1);
                int i = Mathf.Min(Mathf.FloorToInt(x), radii.Length - 2);
                float f = x - i;
                return Mathf.Lerp(radii[i], radii[i + 1], f * f * (3f - 2f * f));
            }

            // Stations along it, near end first: (z, radius).
            var stations = new List<Vector2>();
            if (roundStart)
            {
                const int capRings = 4;
                for (int c = capRings; c > 0; c--)
                {
                    float phi = c / (float)capRings * Mathf.PI * 0.5f; // the tip at 90 degrees
                    stations.Add(new Vector2(Mathf.Sin(phi) * radii[0] * 0.7f, Mathf.Cos(phi) * radii[0]));
                }
            }

            int steps = Mathf.Max(8, radii.Length * 6);
            for (int k = 0; k <= steps; k++)
            {
                float t = k / (float)steps;
                stations.Add(new Vector2(-t * length, Radius(t)));
            }

            int row = segments + 1;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            float distance = 0f;
            for (int k = 0; k < stations.Count; k++)
            {
                if (k > 0)
                {
                    distance += (stations[k] - stations[k - 1]).magnitude;
                }

                for (int s = 0; s <= segments; s++)
                {
                    float angle = s * Mathf.PI * 2f / segments;
                    float r = stations[k].y;
                    vertices.Add(new Vector3(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r * flatten, stations[k].x));
                    uvs.Add(new Vector2(angle * r, distance));
                }
            }

            // Normals from the neighbours on the grid. Each difference is normalised before the cross product:
            // Vector3.normalized returns zero below 1e-5, and the product of two millimetre steps is far smaller
            // (the first version of this came out with no normals at all - a black arm).
            var normals = new List<Vector3>(vertices.Count);
            for (int k = 0; k < stations.Count; k++)
            {
                for (int s = 0; s <= segments; s++)
                {
                    Vector3 p = vertices[k * row + s];
                    int next = (s + 1) % segments;
                    int previous = (s - 1 + segments) % segments;
                    Vector3 around = vertices[k * row + next] - vertices[k * row + previous];
                    Vector3 along = vertices[Mathf.Min(k + 1, stations.Count - 1) * row + s] - vertices[Mathf.Max(k - 1, 0) * row + s];
                    if (around.sqrMagnitude < 1e-12f)
                    {
                        normals.Add(Vector3.forward); // the tip of the rounded end
                        continue;
                    }

                    Vector3 normal = Vector3.Cross(along.normalized, around.normalized).normalized;
                    if (Vector3.Dot(normal, new Vector3(p.x, p.y, 0f)) < 0f)
                    {
                        normal = -normal;
                    }

                    normals.Add(normal);
                }
            }

            var triangles = new List<int>();
            for (int k = 0; k < stations.Count - 1; k++)
            {
                for (int s = 0; s < segments; s++)
                {
                    int a = k * row + s;
                    int b = a + row;
                    // Clockwise seen from outside (seen from +x: angle runs up the screen, the length to the left).
                    triangles.Add(a);
                    triangles.Add(b + 1);
                    triangles.Add(a + 1);
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(b + 1);
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return Save(mesh);
        }

        /// <summary>
        /// A blanket thrown over a bed: flat-ish on a <paramref name="width"/> x <paramref name="depth"/> top (with
        /// soft wrinkles), rolling over the edges and hanging <paramref name="hang"/> down each side in loose folds.
        /// The top sits at y = 0, the hem at y = -hang. UVs in metres across the unfolded cloth.
        /// </summary>
        public static Mesh Drape(string name, float width, float depth, float hang, float wrinkles, int seed)
        {
            const int stepsPerMetre = 28;
            float fullW = width + hang * 2f;
            float fullD = depth + hang * 2f;
            int nx = Mathf.Max(8, Mathf.RoundToInt(fullW * stepsPerMetre));
            int nz = Mathf.Max(8, Mathf.RoundToInt(fullD * stepsPerMetre));
            const float roll = 0.05f; // radius the cloth rolls over the mattress edge with

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (int j = 0; j <= nz; j++)
            {
                for (int i = 0; i <= nx; i++)
                {
                    float u = -fullW * 0.5f + fullW * i / nx;
                    float v = -fullD * 0.5f + fullD * j / nz;
                    float outX = Mathf.Max(Mathf.Abs(u) - width * 0.5f, 0f);
                    float outZ = Mathf.Max(Mathf.Abs(v) - depth * 0.5f, 0f);
                    float over = Mathf.Sqrt(outX * outX + outZ * outZ); // how far past the top's edge
                    Vector2 edgeDir = over > 1e-5f ? new Vector2(Mathf.Sign(u) * outX, Mathf.Sign(v) * outZ) / over : Vector2.zero;

                    // Over the edge: a quarter-roll, then straight down.
                    float along = Mathf.Min(over, roll * Mathf.PI * 0.5f);
                    float angle = along / roll;
                    float outwards = roll * Mathf.Sin(angle);
                    float drop = roll * (1f - Mathf.Cos(angle)) + Mathf.Max(0f, over - roll * Mathf.PI * 0.5f);
                    var p = new Vector3(Mathf.Clamp(u, -width * 0.5f, width * 0.5f) + edgeDir.x * outwards, -drop,
                        Mathf.Clamp(v, -depth * 0.5f, depth * 0.5f) + edgeDir.y * outwards);

                    // Wrinkles on top, folds hanging down the sides - both from smooth noise.
                    float n = Mathf.PerlinNoise(u * 2.3f + seed, v * 2.3f + seed * 0.7f) - 0.5f;
                    float n2 = Mathf.PerlinNoise(u * 6.1f + seed * 1.3f, v * 6.1f) - 0.5f;
                    float onTop = over <= 0f ? 1f : 0f;
                    p.y += (n * 0.035f + n2 * 0.012f) * wrinkles * onTop;
                    float fold = Mathf.Sin((u + v) * 11f + n * 6f) * 0.018f * wrinkles * Mathf.Clamp01(over / Mathf.Max(0.01f, hang));
                    p.x += edgeDir.x * fold;
                    p.z += edgeDir.y * fold;
                    vertices.Add(p);
                    uvs.Add(new Vector2(u, v));
                }
            }

            var triangles = new List<int>();
            int row = nx + 1;
            for (int j = 0; j < nz; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    int a = j * row + i;
                    triangles.Add(a);
                    triangles.Add(a + row);
                    triangles.Add(a + row + 1);
                    triangles.Add(a);
                    triangles.Add(a + row + 1);
                    triangles.Add(a + 1);
                }
            }

            return Finish(name, vertices, uvs, triangles);
        }

        /// <summary>A curtain: a hanging panel <paramref name="width"/> wide and <paramref name="height"/> tall (top at y = 0),
        /// in soft vertical folds that open up towards the hem. Faces -Z; draw it double-sided.</summary>
        public static Mesh Curtain(string name, float width, float height, int folds, float depth)
        {
            const int nx = 64;
            const int ny = 24;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            for (int j = 0; j <= ny; j++)
            {
                float t = j / (float)ny; // 0 top, 1 hem
                for (int i = 0; i <= nx; i++)
                {
                    float s = i / (float)nx;
                    float wave = Mathf.Sin(s * folds * Mathf.PI * 2f + t * 0.8f) * depth * (0.6f + 0.4f * t);
                    vertices.Add(new Vector3((s - 0.5f) * width * (0.85f + 0.15f * t), -t * height, wave));
                    uvs.Add(new Vector2(s * width, -t * height));
                }
            }

            var triangles = new List<int>();
            int row = nx + 1;
            for (int j = 0; j < ny; j++)
            {
                for (int i = 0; i < nx; i++)
                {
                    int a = j * row + i;
                    triangles.Add(a);
                    triangles.Add(a + 1);
                    triangles.Add(a + row + 1);
                    triangles.Add(a);
                    triangles.Add(a + row + 1);
                    triangles.Add(a + row);
                }
            }

            return Finish(name, vertices, uvs, triangles);
        }

        /// <summary>An open cone section around Y (a lamp shade): radius <paramref name="bottom"/> at y = 0, <paramref name="top"/> at y = height.
        /// Both sides of the wall are built, so its inside shows too.</summary>
        public static Mesh Frustum(string name, float bottom, float top, float height, int segments = 32)
        {
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            for (int side = 0; side < 2; side++)
            {
                int start = vertices.Count;
                for (int s = 0; s <= segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    vertices.Add(dir * bottom);
                    vertices.Add(dir * top + Vector3.up * height);
                    uvs.Add(new Vector2(s / (float)segments, 0f));
                    uvs.Add(new Vector2(s / (float)segments, 1f));
                }

                for (int s = 0; s < segments; s++)
                {
                    int a = start + s * 2;
                    if (side == 0)
                    {
                        triangles.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
                    }
                    else
                    {
                        triangles.AddRange(new[] { a, a + 3, a + 1, a, a + 2, a + 3 });
                    }
                }
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            // Two sides share positions: split normals by recomputing per side would need separate vertex sets - they are.
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return Save(mesh);
        }

        private static Mesh Finish(string name, List<Vector3> vertices, List<Vector2> uvs, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return Save(mesh);
        }

        /// <summary>A flat quad lying on XZ, facing up, 1 x 1 m, UVs 0-1 (for soft shadows and water).</summary>
        public static Mesh GroundQuad(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(new List<Vector3>
            {
                new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f)
            });
            mesh.SetNormals(new List<Vector3> { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) });
            mesh.SetTriangles(new List<int> { 0, 1, 2, 0, 2, 3 }, 0);
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            return Save(mesh);
        }

        /// <summary>Grid coordinates along one axis: fine steps across each rounded edge, one span across the flat middle.</summary>
        private static float[] AxisSteps(float half, float inner, int segments)
        {
            if (half - inner < 1e-5f)
            {
                return new[] { -half, half }; // no rounding: a plain flat face
            }

            var steps = new List<float>();
            for (int i = 0; i <= segments; i++)
            {
                steps.Add(Mathf.Lerp(-half, -inner, i / (float)segments));
            }

            if (inner > 1e-5f)
            {
                steps.Add(inner);
            }

            for (int i = 1; i <= segments; i++)
            {
                steps.Add(Mathf.Lerp(inner, half, i / (float)segments));
            }

            return steps.ToArray();
        }

        private static void AddFace(int axis, int sign, Vector3 half, Vector3 inner, float radius, float[][] steps, bool inward, Vector3 uvOrigin,
            List<Vector3> vertices, List<Vector3> normals, List<Vector2> uvs, List<int> triangles)
        {
            int uAxis = (axis + 1) % 3;
            int vAxis = (axis + 2) % 3;
            float[] us = steps[uAxis];
            float[] vs = steps[vAxis];
            Vector3 faceNormal = Vector3.zero;
            faceNormal[axis] = sign;
            int start = vertices.Count;

            for (int j = 0; j < vs.Length; j++)
            {
                for (int i = 0; i < us.Length; i++)
                {
                    // A point on the flat face of the outer box, pulled onto the rounded surface: clamp it into the
                    // inner box and push it back out by the radius along the direction it came from.
                    Vector3 flat = Vector3.zero;
                    flat[axis] = sign * half[axis];
                    flat[uAxis] = us[i];
                    flat[vAxis] = vs[j];
                    Vector3 core = new Vector3(Mathf.Clamp(flat.x, -inner.x, inner.x), Mathf.Clamp(flat.y, -inner.y, inner.y), Mathf.Clamp(flat.z, -inner.z, inner.z));
                    Vector3 away = flat - core;
                    Vector3 normal = away.sqrMagnitude > 1e-12f ? away.normalized : faceNormal;
                    vertices.Add(core + normal * radius);
                    normals.Add(inward ? -normal : normal);
                    uvs.Add(FaceUv(axis, flat + uvOrigin));
                }
            }

            Vector3 wanted = inward ? -faceNormal : faceNormal;
            int row = us.Length;
            for (int j = 0; j < vs.Length - 1; j++)
            {
                for (int i = 0; i < us.Length - 1; i++)
                {
                    int a = start + j * row + i;
                    int b = a + 1;
                    int c = a + row + 1;
                    int d = a + row;
                    AddQuad(a, b, c, d, wanted, i, j, us, vs, axis, uAxis, vAxis, triangles);
                }
            }
        }

        /// <summary>
        /// Planar UVs in metres, oriented so a texture's X runs horizontally on every face - along X on the top,
        /// front and back, along Z on the sides: brushed steel's grain then runs along a counter's length.
        /// </summary>
        private static Vector2 FaceUv(int axis, Vector3 p)
        {
            switch (axis)
            {
                case 0: return new Vector2(p.z, p.y);
                case 1: return new Vector2(p.x, p.z);
                default: return new Vector2(p.x, p.y);
            }
        }

        /// <summary>Two triangles, wound so that Unity (clockwise = front) shows them from the wanted side.</summary>
        private static void AddQuad(int a, int b, int c, int d, Vector3 wanted, int i, int j, float[] us, float[] vs, int axis, int uAxis, int vAxis,
            List<int> triangles)
        {
            // Decide the winding on the flat grid (the rounded corner points can coincide).
            Vector3 pa = Vector3.zero, pb = Vector3.zero, pd = Vector3.zero;
            pa[uAxis] = us[i];
            pa[vAxis] = vs[j];
            pb[uAxis] = us[i + 1];
            pb[vAxis] = vs[j];
            pd[uAxis] = us[i];
            pd[vAxis] = vs[j + 1];
            bool flip = Vector3.Dot(Vector3.Cross(pd - pa, pb - pa), wanted) < 0f;
            if (!flip)
            {
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(c);
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(b);
            }
            else
            {
                triangles.Add(a);
                triangles.Add(c);
                triangles.Add(d);
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(c);
            }
        }

        private static Mesh Save(Mesh mesh)
        {
            EnsureFolder(MeshFolder);
            string path = $"{MeshFolder}/{mesh.name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            // A second UV set laid out for the lightmap: the kitchen's static meshes take baked light (KitchenLighting).
            // Unity lays it out in an external tool (UnwrapCL), which once lost its connection and took the editor
            // down with it. The layout may split vertices, so the saved mesh is not comparable with a fresh one; the
            // fresh shape's fingerprint is kept in the asset's import settings instead, and a mesh rebuilt exactly as
            // last time is left as it is - no unwrapping, no rewrite.
            string fingerprint = Fingerprint(mesh);
            AssetImporter importer = existing != null ? AssetImporter.GetAtPath(path) : null;
            if (importer != null && importer.userData == fingerprint && existing.uv2.Length > 0)
            {
                Object.DestroyImmediate(mesh);
                return existing;
            }

            Unwrapping.GenerateSecondaryUVSet(mesh);
            Mesh saved;
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                saved = mesh;
            }
            else
            {
                EditorUtility.CopySerialized(mesh, existing);
                existing.name = mesh.name;
                Object.DestroyImmediate(mesh);
                EditorUtility.SetDirty(existing);
                saved = existing;
            }

            importer = AssetImporter.GetAtPath(path);
            if (importer != null)
            {
                importer.userData = fingerprint;
                EditorUtility.SetDirty(importer);
                AssetDatabase.WriteImportSettingsIfDirty(path);
            }

            return saved;
        }

        /// <summary>A hash of everything the mesh builders produce (positions, normals, UVs, triangles), as text.</summary>
        private static string Fingerprint(Mesh mesh)
        {
            unchecked
            {
                ulong hash = 14695981039346656037UL; // FNV-1a
                void Add(float value)
                {
                    hash = (hash ^ (uint)System.BitConverter.SingleToInt32Bits(value)) * 1099511628211UL;
                }

                foreach (Vector3 v in mesh.vertices) { Add(v.x); Add(v.y); Add(v.z); }
                foreach (Vector3 n in mesh.normals) { Add(n.x); Add(n.y); Add(n.z); }
                foreach (Vector2 uv in mesh.uv) { Add(uv.x); Add(uv.y); }
                foreach (int index in mesh.triangles) { hash = (hash ^ (uint)index) * 1099511628211UL; }
                return $"shape:{hash:x16}";
            }
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
