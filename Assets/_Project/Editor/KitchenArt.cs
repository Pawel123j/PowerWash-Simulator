using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Rendering.Universal.ShaderGUI;
using UnityEngine;
using Zmywak.Core.Graphics;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Input;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Procedural art for the kitchen, aiming at stylised realism: what reads as "real" on a phone is light
    /// catching rounded edges, brushed steel, water and soft contact shadows rather than polygon counts. Textures
    /// are generated into PNGs (subway tiles, brushed steel and its normal map, speckled plastic, water ripples,
    /// soap foam, a soft shadow), meshes into Art/Meshes (rounded boxes, see <see cref="ArtMeshes"/>), materials
    /// saved as assets - and the set built from them: tiled wall, a steel counter with a real recessed sink full
    /// of soapy water, a plastic crate for the pile, a wire rack and the dishwasher. Everything is deterministic,
    /// so re-running the setup regenerates identical files.
    /// Physics is kept apart from looks: the counter and the crate collide through plain invisible boxes exactly
    /// where the old ones were, and no decoration has a collider (it must never block a tap).
    /// </summary>
    internal static class KitchenArt
    {
        private const string TextureFolder = "Assets/_Project/Art/Textures";
        private const string MaterialFolder = "Assets/_Project/Art/Materials";

        // The sink sits in a hole in the counter: its station's centre and the basin's size.
        private static readonly Vector3 SinkCentre = new Vector3(0.6f, 0f, -2.3f);
        private static readonly Vector2 BasinSize = new Vector2(2.6f, 1.9f);
        private const float BasinDepth = 0.42f;
        private const float WaterLevel = -0.14f;

        private static Material _steel;
        private static Material _steelDark;
        private static Material _basin;
        private static Material _chrome;
        private static Material _tiles;
        private static Material _paper;
        private static Material _crate;
        private static Material _rackTray;
        private static Material _wire;
        private static Material _slot;
        private static Material _panelBlack;
        private static Material _led;
        private static Material _progress;
        private static Material _shadow;
        private static Material _shadowLight;
        private static Material _water;
        private static Material _foam;
        private static Material _glove;
        private static Material _gloveCuff;
        private static Material _sleeve;
        private static Material _skin;
        private static Material _wallShade;
        private static Material _machineInterior;
        private static Material _machineGlass;
        private static Material _soapBottle;
        private static Material _capPlastic;
        private static Material _teaTowel;
        private static Material _spongeFoam;
        private static Material _scourPad;
        private static Material _steelWool;
        private static Material _brushPlastic;
        private static Material _sprayFace;
        private static Material _sprayWater;
        private static Material _bristles;

        public static Material SlotMaterial => _slot;

        /// <summary>Creates (or refreshes) the generated textures and the kitchen materials.</summary>
        public static void EnsureAssets()
        {
            EnsureFolder(TextureFolder);
            EnsureFolder(MaterialFolder);

            Texture2D tiles = EnsureTexture("SubwayTiles_Albedo", 512, 512, false, TextureWrapMode.Repeat, GenerateTileAlbedo);
            Texture2D tilesNormal = EnsureTexture("SubwayTiles_Normal", 512, 512, true, TextureWrapMode.Repeat, GenerateTileNormal);
            Texture2D steel = EnsureTexture("BrushedSteel_Albedo", 512, 512, false, TextureWrapMode.Repeat, GenerateBrushedSteel);
            Texture2D steelNormal = EnsureTexture("BrushedSteel_Normal", 512, 512, true, TextureWrapMode.Repeat, GenerateBrushedSteelNormal);
            Texture2D steelFinish = EnsureTexture("BrushedSteel_Finish", 512, 512, false, TextureWrapMode.Repeat, GenerateSteelFinish, data: true);
            Texture2D plastic = EnsureTexture("Plastic_Albedo", 128, 128, false, TextureWrapMode.Repeat, GeneratePlastic);
            Texture2D waterNormal = EnsureTexture("Water_Normal", 256, 256, true, TextureWrapMode.Repeat, GenerateWaterNormal);
            Texture2D foam = EnsureTexture("SoapFoam", 256, 256, false, TextureWrapMode.Repeat, GenerateFoam);
            Texture2D softShadow = EnsureTexture("SoftShadow", 128, 128, false, TextureWrapMode.Clamp, GenerateSoftShadow);

            // Set pieces are ArtMeshes with UVs in metres: tiling = repeats per metre.
            _tiles = EnsureMaterial("Tiles", new Color(1f, 1f, 1f), 0.78f, 0f, tiles, new Vector2(5.8f, 5.8f), tilesNormal, 0.9f);
            // A little darker than before the finish map: white plates need something to stand out against.
            _steel = EnsureMaterial("Steel", new Color(0.8f, 0.81f, 0.83f), 0.84f, 0.85f, steel, new Vector2(0.45f, 0.45f), steelNormal, 0.12f,
                metallicGloss: steelFinish);
            _steelDark = EnsureMaterial("SteelDark", new Color(0.6f, 0.62f, 0.65f), 0.86f, 0.85f, steel, new Vector2(0.6f, 0.6f), steelNormal, 0.15f,
                metallicGloss: steelFinish);
            // The basin's inside: darker and duller than the counter, so it reads as depth under the water.
            _basin = EnsureMaterial("SinkBasin", new Color(0.42f, 0.44f, 0.46f), 0.66f, 0.7f, steel, new Vector2(0.6f, 0.6f), steelNormal, 0.12f,
                metallicGloss: steelFinish);
            _chrome = EnsureMaterial("Chrome", new Color(0.92f, 0.92f, 0.94f), 0.94f, 1f);
            Texture2D ticket = EnsureTexture("OrderTicket", 128, 192, false, TextureWrapMode.Clamp, GenerateOrderTicket);
            _paper = EnsureMaterial("TicketPaper", Color.white, 0.1f, 0f, ticket, Vector2.one);
            Texture2D wallShade = EnsureTexture("WallShade", 8, 128, false, TextureWrapMode.Clamp, GenerateWallShade);
            _wallShade = EnsureUnlitTransparent("WallShade", wallShade, new Color(0.02f, 0.02f, 0.03f, 0.55f), Vector2.one);
            // A grey-blue bus tub, darker than the steel around it so the dirty dishes in it stand out.
            _crate = EnsureMaterial("PileTub", new Color(0.24f, 0.31f, 0.38f), 0.45f, 0f, plastic, new Vector2(2.5f, 2.5f));
            _rackTray = EnsureMaterial("RackTray", new Color(0.19f, 0.22f, 0.26f), 0.4f, 0f, plastic, new Vector2(3f, 3f));
            _wire = EnsureMaterial("RackWire", new Color(0.86f, 0.87f, 0.89f), 0.86f, 0.9f);
            _slot = EnsureMaterial("RackSlot", new Color(0.15f, 0.18f, 0.21f), 0.35f, 0f); // quiet until a drag lights a cell up
            _panelBlack = EnsureMaterial("PanelBlack", new Color(0.035f, 0.04f, 0.045f), 0.94f, 0f);
            _led = EnsureMaterial("StatusLight", new Color(0.2f, 0.9f, 0.35f), 0.8f, 0f, emission: new Color(0.2f, 0.9f, 0.35f) * 1.2f);
            _progress = EnsureMaterial("ProgressFill", new Color(1f, 0.6f, 0.1f), 0.8f, 0f, emission: new Color(1f, 0.6f, 0.1f) * 1.6f);
            _shadow = EnsureUnlitTransparent("SoftShadow", softShadow, new Color(0.05f, 0.04f, 0.03f, 0.55f), Vector2.one);
            _shadowLight = EnsureUnlitTransparent("SoftShadowLight", softShadow, new Color(0.05f, 0.04f, 0.03f, 0.28f), Vector2.one);
            // Foam as a soft veil on the water; fully opaque bubbles glittered like snow under the sink lamp.
            _foam = EnsureUnlitTransparent("SoapFoam", foam, new Color(0.92f, 0.94f, 0.95f, 0.5f), new Vector2(1.1f, 1.1f));
            _water = EnsureWater(waterNormal);
            _foam.renderQueue = _water.renderQueue + 1; // always over the water it floats on

            // The hand and what it holds (the rig is built in real metres: tiling per metre is high).
            Texture2D gloveNormal = EnsureTexture("Glove_Normal", 128, 128, true, TextureWrapMode.Repeat, GenerateGloveNormal);
            Texture2D weaveNormal = EnsureTexture("Weave_Normal", 128, 128, true, TextureWrapMode.Repeat, GenerateWeaveNormal);
            Texture2D spongeAlbedo = EnsureTexture("Sponge_Albedo", 256, 256, false, TextureWrapMode.Repeat, GenerateSpongeAlbedo);
            Texture2D spongeNormal = EnsureTexture("Sponge_Normal", 256, 256, true, TextureWrapMode.Repeat, GenerateSpongeNormal);
            Texture2D fibreNormal = EnsureTexture("Fibre_Normal", 256, 256, true, TextureWrapMode.Repeat, GenerateFibreNormal);
            // The arm is drawn by Zmywak/Hand: lit like URP/Lit, with the shift's wear on top (HandWear).
            _glove = EnsureHandMaterial("NitrileGlove", HandPart.Glove, new Color(0.17f, 0.37f, 0.8f), 0.6f, gloveNormal, new Vector2(5f, 5f), 0.2f);
            _gloveCuff = EnsureHandMaterial("NitrileGloveCuff", HandPart.Cuff, new Color(0.17f, 0.37f, 0.8f), 0.6f, gloveNormal, new Vector2(5f, 5f), 0.2f);
            // Chef's whites: a touch below white, or the lit side blows out next to the saturated glove.
            // Coarse enough weave to show at a hand's size on screen; a finer one sparkled like static.
            _sleeve = EnsureHandMaterial("ChefSleeve", HandPart.Sleeve, new Color(0.84f, 0.84f, 0.82f), 0.12f, weaveNormal, new Vector2(10f, 10f), 0.4f);
            _skin = EnsureHandMaterial("Skin", HandPart.Skin, new Color(0.86f, 0.64f, 0.53f), 0.36f, null, Vector2.one, 1f);
            _spongeFoam = EnsureMaterial("SpongeFoam", new Color(1f, 0.84f, 0.3f), 0.08f, 0f, spongeAlbedo, new Vector2(22f, 22f), spongeNormal, 0.9f);
            _scourPad = EnsureMaterial("ScourPad", new Color(0.1f, 0.42f, 0.19f), 0.06f, 0f, null, new Vector2(30f, 30f), fibreNormal, 1f);
            _steelWool = EnsureMaterial("SteelWool", new Color(0.74f, 0.74f, 0.77f), 0.5f, 1f, null, new Vector2(3f, 3f), fibreNormal, 1f);
            _brushPlastic = EnsureMaterial("BrushPlastic", new Color(0.8f, 0.14f, 0.12f), 0.66f, 0f);
            _sprayFace = EnsureMaterial("SprayFace", new Color(0.17f, 0.18f, 0.2f), 0.55f, 0.4f);
            // Unlit: a glossy lit cone mirrored the bright kitchen and read as milk, not water. Streaks run along it
            // (UVs in metres: ~2 cm round per tile, 5 cm along) and flow down it (TextureScroller).
            Texture2D sprayStreaks = EnsureTexture("SprayStreaks", 64, 64, false, TextureWrapMode.Repeat, GenerateSprayStreaks);
            _sprayWater = EnsureUnlitTransparent("SprayWater", sprayStreaks, new Color(0.74f, 0.89f, 1f, 0.62f), new Vector2(50f, 20f));

            // Things by the sink.
            _soapBottle = EnsureMaterial("SoapBottle", new Color(0.12f, 0.55f, 0.24f), 0.82f, 0f);
            _capPlastic = EnsureMaterial("CapPlastic", new Color(0.92f, 0.92f, 0.9f), 0.6f, 0f);
            // The big dishwasher: a dark, wet tub seen through a smoked-glass lid.
            _machineInterior = EnsureMaterial("BigDishwasherTub", new Color(0.13f, 0.15f, 0.17f), 0.7f, 0.5f, steel, new Vector2(0.6f, 0.6f), steelNormal, 0.2f);
            _machineGlass = EnsureTransparentLit("BigDishwasherLid", new Color(0.1f, 0.13f, 0.15f, 0.42f), 0.95f);
            Texture2D teaTowel = EnsureTexture("TeaTowel_Albedo", 128, 128, false, TextureWrapMode.Repeat, GenerateTeaTowel);
            _teaTowel = EnsureMaterial("TeaTowel", Color.white, 0.08f, 0f, teaTowel, new Vector2(1.6f, 1.6f), weaveNormal, 0.5f);
            _bristles = EnsureMaterial("Bristles", new Color(0.94f, 0.92f, 0.86f), 0.22f, 0f, null, new Vector2(4f, 4f), fibreNormal, 0.8f);

            AssetDatabase.SaveAssets();
        }

        // ------------------------------------------------------------------
        // Set dressing
        // ------------------------------------------------------------------

        public static void BuildRoom(Transform parent)
        {
            var room = new GameObject("Room").transform;
            room.SetParent(parent);

            // The physics floor for the pile, where it always was: an invisible box.
            ColliderBox("Counter", room, new Vector3(0f, -0.25f, 0f), new Vector3(6.4f, 0.5f, 8f));

            // The steel top, in four pieces around the sink's hole.
            float holeLeft = SinkCentre.x - BasinSize.x * 0.5f;
            float holeRight = SinkCentre.x + BasinSize.x * 0.5f;
            float holeFront = SinkCentre.z - BasinSize.y * 0.5f;
            float holeBack = SinkCentre.z + BasinSize.y * 0.5f;
            CounterPiece("CounterLeft", room, -3.2f, holeLeft, -4f, 4f);
            CounterPiece("CounterRight", room, holeRight, 3.2f, -4f, 4f);
            CounterPiece("CounterBack", room, holeLeft, holeRight, holeBack, 4f);
            CounterPiece("CounterFront", room, holeLeft, holeRight, -4f, holeFront);
            Solid("CounterFace", room, new Vector3(0f, -0.3f, -3.97f), new Vector3(6.4f, 0.5f, 0.06f), 0.01f, _steel);
            GameObject edge = Cylinder("CounterEdge", room, new Vector3(0f, -0.06f, -4f), new Vector3(0.14f, 3.2f, 0.14f), _steel);
            edge.transform.rotation = Quaternion.Euler(0f, 0f, 90f);

            // Cabinets under the counter fill the bottom of a tall phone screen.
            Solid("Cabinets", room, new Vector3(0f, -2.2f, -3.95f), new Vector3(6.4f, 3.4f, 0.1f), 0.02f, _steelDark);
            for (int i = -1; i <= 1; i += 2)
            {
                Solid("CabinetSeam", room, new Vector3(i * 1.07f, -2.2f, -4.01f), new Vector3(0.03f, 3.4f, 0.02f), 0.005f, _panelBlack);
                Solid("CabinetHandle", room, new Vector3(i * 1.07f - i * 0.18f, -0.85f, -4.05f), new Vector3(0.05f, 0.5f, 0.05f), 0.024f, _chrome);
            }

            // Tiled wall behind the counter, with a steel upstand where the two meet and a soft shadow in the corner.
            GameObject wall = Box("Wall", room, new Vector3(0f, 3.5f, 4.1f), new Vector3(7f, 7f, 0.2f), _tiles);
            // After the move the tiles are another kitchen's: a Kraków hotel's cream, a Pilbara camp's steel grey.
            var dressing = room.gameObject.AddComponent<KitchenPlaceDressing>();
            var dressingSo = new SerializedObject(dressing);
            dressingSo.FindProperty("walls").arraySize = 1;
            dressingSo.FindProperty("walls").GetArrayElementAtIndex(0).objectReferenceValue = wall.GetComponent<Renderer>();
            dressingSo.ApplyModifiedPropertiesWithoutUndo();
            Solid("Upstand", room, new Vector3(0f, 0.1f, 3.97f), new Vector3(6.4f, 0.2f, 0.06f), 0.025f, _steel);
            SoftShadow("CornerShadow", room, new Vector3(0f, 0f, 3.9f), new Vector2(6.8f, 0.4f), 0.5f);
            // The wall darkens upwards, away from the lamp over the counter: it gives the room depth, and the
            // chef's yellow banner and the labels over the wall stand out against it.
            GameObject shade = GameObject.CreatePrimitive(PrimitiveType.Quad);
            shade.name = "WallShade";
            Object.DestroyImmediate(shade.GetComponent<Collider>());
            shade.transform.SetParent(room, true);
            shade.transform.position = new Vector3(0f, 2.1f, 3.985f); // the part of the wall a phone shows: ~4 m
            shade.transform.localScale = new Vector3(7f, 3.8f, 1f);
            var shadeRenderer = shade.GetComponent<Renderer>();
            shadeRenderer.sharedMaterial = _wallShade;
            shadeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            shadeRenderer.receiveShadows = false;

            // Ticket rail - where the chef's orders hang.
            Cylinder("TicketRail", room, new Vector3(1.1f, 1.15f, 3.93f), new Vector3(0.05f, 1.6f, 0.05f), _chrome)
                .transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            float[] ticketX = { 0.1f, 0.75f, 1.45f, 2.2f };
            for (int i = 0; i < ticketX.Length; i++)
            {
                GameObject ticket = Box("Ticket", room, new Vector3(ticketX[i], 0.85f, 3.9f), new Vector3(0.36f, 0.55f, 0.01f), _paper);
                ticket.transform.rotation = Quaternion.Euler(0f, 0f, (i % 2 == 0 ? 1f : -1f) * (3f + i * 1.5f));
            }

            MakeStatic(room);
        }

        /// <summary>
        /// A lamp hanging over the sink. A directional light and the ambient light the whole flat counter alike, and
        /// under the long-lens camera it mirrors almost the same direction everywhere, so the steel came out one
        /// even grey from edge to edge; a point light's angle changes across it, which draws a soft pool of sheen on
        /// the steel around the sink, a highlight on the water and a rim of light on the dish being washed. No
        /// shadows: a phone pays for every shadow map, and the key light already casts them.
        /// </summary>
        public static void BuildWorkLight(Transform parent)
        {
            var go = new GameObject("SinkLamp", typeof(Light));
            go.transform.SetParent(parent, false);
            // Behind and above the sink: the counter mirrors it just in front of the basin (the camera looks down at 55 degrees).
            go.transform.position = new Vector3(0.4f, 3.2f, 0.1f);
            Light lamp = go.GetComponent<Light>();
            lamp.type = LightType.Point;
            lamp.color = new Color(1f, 0.92f, 0.8f);
            lamp.intensity = 5f;
            lamp.range = 8f;
            lamp.shadows = LightShadows.None;
            lamp.renderMode = LightRenderMode.ForcePixel;
        }

        /// <summary>A plastic crate the pile sits in - keeps dishes from sliding off the counter.</summary>
        public static void BuildPileTub(Transform parent, Vector3 centre)
        {
            var tub = new GameObject("PileTub").transform;
            tub.SetParent(parent);
            tub.position = centre;
            const float width = 1.7f;
            const float depth = 1.5f;
            const float wallHeight = 0.3f;
            const float wall = 0.06f;

            // Physics: the same five boxes as before, invisible.
            ColliderBox("Floor", tub, centre + new Vector3(0f, 0.02f, 0f), new Vector3(width, 0.04f, depth));
            ColliderBox("WallFront", tub, centre + new Vector3(0f, wallHeight * 0.5f, -depth * 0.5f), new Vector3(width, wallHeight, wall));
            ColliderBox("WallBack", tub, centre + new Vector3(0f, wallHeight * 0.5f, depth * 0.5f), new Vector3(width, wallHeight, wall));
            ColliderBox("WallLeft", tub, centre + new Vector3(-width * 0.5f, wallHeight * 0.5f, 0f), new Vector3(wall, wallHeight, depth));
            ColliderBox("WallRight", tub, centre + new Vector3(width * 0.5f, wallHeight * 0.5f, 0f), new Vector3(wall, wallHeight, depth));

            // Looks: a rounded outer shell, the inside seen from above, and a lip over the gap between them.
            Visual("Shell", tub, centre + new Vector3(0f, wallHeight * 0.5f + 0.005f, 0f),
                ArtMeshes.RoundedBox("CrateShell", new Vector3(width + wall, wallHeight + 0.01f, depth + wall), 0.08f, 4, openTop: true), _crate);
            Visual("Inside", tub, centre + new Vector3(0f, wallHeight * 0.5f + 0.02f, 0f),
                ArtMeshes.RoundedBox("CrateInside", new Vector3(width - wall, wallHeight, depth - wall), 0.06f, 4, openTop: true, inward: true), _crate);
            Visual("LipFront", tub, centre + new Vector3(0f, wallHeight + 0.01f, -depth * 0.5f),
                ArtMeshes.RoundedBox("CrateLipX", new Vector3(width + wall, 0.035f, wall + 0.01f), 0.017f, 3), _crate);
            Visual("LipBack", tub, centre + new Vector3(0f, wallHeight + 0.01f, depth * 0.5f), ArtMeshes.RoundedBox("CrateLipX", new Vector3(width + wall, 0.035f, wall + 0.01f), 0.017f, 3), _crate);
            Visual("LipLeft", tub, centre + new Vector3(-width * 0.5f, wallHeight + 0.01f, 0f),
                ArtMeshes.RoundedBox("CrateLipZ", new Vector3(wall + 0.01f, 0.035f, depth + wall), 0.017f, 3), _crate);
            Visual("LipRight", tub, centre + new Vector3(width * 0.5f, wallHeight + 0.01f, 0f), ArtMeshes.RoundedBox("CrateLipZ", new Vector3(wall + 0.01f, 0.035f, depth + wall), 0.017f, 3), _crate);
            // A bus tub's moulding: vertical ribs down the long sides and a hand-hold slot in each end.
            for (int rib = -2; rib <= 2; rib++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Visual("Rib", tub, centre + new Vector3(rib * 0.3f, wallHeight * 0.45f, side * (depth + wall) * 0.5f + side * 0.008f),
                        ArtMeshes.RoundedBox("CrateRib", new Vector3(0.05f, wallHeight * 0.78f, 0.02f), 0.01f, 2), _crate);
                }
            }

            foreach (float side in new[] { -1f, 1f })
            {
                Visual("HandHold", tub, centre + new Vector3(side * ((width + wall) * 0.5f + 0.002f), wallHeight * 0.74f, 0f),
                    ArtMeshes.RoundedBox("CrateHandHold", new Vector3(0.012f, 0.06f, 0.36f), 0.006f, 2), _panelBlack);
            }

            SoftShadow("Shadow", tub, centre + new Vector3(0.05f, 0f, -0.05f), new Vector2(width + 0.5f, depth + 0.5f));

            MakeStatic(tub);
        }

        /// <summary>A recessed stainless basin with soapy water, a rim, a drain and a tap in the back-right corner
        /// (out of the way of the dish being washed).</summary>
        public static Transform BuildSink(Transform station)
        {
            var sink = new GameObject("Sink").transform;
            sink.SetParent(station, false);
            Vector3 o = station.position;

            Visual("Basin", sink, o + new Vector3(0f, -BasinDepth * 0.5f + 0.001f, 0f),
                ArtMeshes.RoundedBox("SinkBasin", new Vector3(BasinSize.x, BasinDepth, BasinSize.y), 0.16f, 5, openTop: true, inward: true), _basin);
            Cylinder("Drain", sink, o + new Vector3(0f, -BasinDepth + 0.006f, 0f), new Vector3(0.26f, 0.004f, 0.26f), _chrome);

            float rimX = BasinSize.x * 0.5f + 0.05f;
            float rimZ = BasinSize.y * 0.5f + 0.05f;
            Visual("RimFront", sink, o + new Vector3(0f, 0.02f, -rimZ), ArtMeshes.RoundedBox("SinkRimX", new Vector3(BasinSize.x + 0.2f, 0.05f, 0.1f), 0.024f, 3), _steel);
            Visual("RimBack", sink, o + new Vector3(0f, 0.02f, rimZ), ArtMeshes.RoundedBox("SinkRimX", new Vector3(BasinSize.x + 0.2f, 0.05f, 0.1f), 0.024f, 3), _steel);
            Visual("RimLeft", sink, o + new Vector3(-rimX, 0.02f, 0f), ArtMeshes.RoundedBox("SinkRimZ", new Vector3(0.1f, 0.05f, BasinSize.y + 0.2f), 0.024f, 3), _steel);
            Visual("RimRight", sink, o + new Vector3(rimX, 0.02f, 0f), ArtMeshes.RoundedBox("SinkRimZ", new Vector3(0.1f, 0.05f, BasinSize.y + 0.2f), 0.024f, 3), _steel);

            // Soapy water: a rippling, reflective surface, and foam drifting on it.
            GameObject water = Visual("Water", sink, o + new Vector3(0f, WaterLevel, 0f), ArtMeshes.GroundQuad("GroundQuad"), _water);
            water.transform.localScale = new Vector3(BasinSize.x - 0.06f, 1f, BasinSize.y - 0.06f);
            TextureScroller ripples = water.AddComponent<TextureScroller>();
            SetScroll(ripples, new Vector2(0.018f, 0.011f));
            GameObject foam = Visual("Foam", sink, o + new Vector3(0f, WaterLevel + 0.004f, 0f), ArtMeshes.GroundQuad("GroundQuad"), _foam);
            foam.transform.localScale = new Vector3(BasinSize.x - 0.1f, 1f, BasinSize.y - 0.1f);
            TextureScroller drift = foam.AddComponent<TextureScroller>();
            SetScroll(drift, new Vector2(-0.006f, 0.004f));

            BuildSteam(sink, o + new Vector3(0f, WaterLevel + 0.03f, 0f), new Vector2(BasinSize.x * 0.8f, BasinSize.y * 0.7f));

            Vector3 tap = o + new Vector3(1.15f, 0f, 0.92f);
            Cylinder("TapBase", sink, tap + new Vector3(0f, 0.08f, 0f), new Vector3(0.16f, 0.08f, 0.16f), _chrome);
            Cylinder("TapNeck", sink, tap + new Vector3(0f, 0.55f, 0f), new Vector3(0.08f, 0.45f, 0.08f), _chrome);
            Cylinder("TapSpout", sink, tap + new Vector3(0f, 1.0f, -0.2f), new Vector3(0.07f, 0.22f, 0.07f), _chrome)
                .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            GameObject tapHead = Cylinder("TapHead", sink, tap + new Vector3(0f, 0.95f, -0.42f), new Vector3(0.1f, 0.06f, 0.1f), _chrome);
            SoftShadow("TapShadow", sink, tap + new Vector3(0f, 0.052f, 0.02f), new Vector2(0.34f, 0.34f));

            BuildSinkProps(sink, o);

            foreach (Transform child in sink)
            {
                if (child.GetComponent<TextureScroller>() == null)
                {
                    SetStatic(child.gameObject);
                }
            }

            // Where the water comes out: just under the tap head, not part of the static batch.
            var spout = new GameObject("WaterOrigin").transform;
            spout.SetParent(station, false);
            spout.position = tapHead.transform.position + Vector3.down * 0.08f;
            return spout;
        }

        /// <summary>
        /// What lives beside a porter's sink, on the bare steel either side of it: a bottle of green washing-up
        /// liquid by the tap, and a folded tea towel on the counter to the left. Decoration only - no colliders, out of
        /// the way of the dishes and the hand.
        /// </summary>
        private static void BuildSinkProps(Transform sink, Vector3 o)
        {
            // The bottle: a flattened tube, its rounded top a shoulder and neck, standing on the counter (the open
            // bottom end is hidden against it), with a white flip cap.
            Vector3 bottle = o + new Vector3(BasinSize.x * 0.5f + 0.42f, 0f, BasinSize.y * 0.5f - 0.05f);
            GameObject body = Visual("SoapBottle", sink, bottle + new Vector3(0f, 0.78f, 0f),
                ArtMeshes.Tube("SoapBottle", 0.78f, new[] { 0.035f, 0.06f, 0.11f, 0.125f, 0.125f, 0.125f, 0.12f }, 0.62f, roundStart: true), _soapBottle);
            body.transform.rotation = Quaternion.Euler(-90f, 0f, 0f) * Quaternion.Euler(0f, 0f, 20f);
            GameObject cap = Cylinder("SoapCap", sink, bottle + new Vector3(0f, 0.81f, 0f), new Vector3(0.075f, 0.03f, 0.075f), _capPlastic);
            cap.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
            // Glossy plastic round the edge mirrored the baked probe's lamp-lit steel and went pale mint; the studio
            // (SceneLighting) keeps it the green of washing-up liquid.
            body.GetComponent<Renderer>().reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            SoftShadow("SoapShadow", sink, bottle, new Vector2(0.5f, 0.4f));

            // The tea towel, folded in four, lying a little askew.
            Vector3 towel = o + new Vector3(-BasinSize.x * 0.5f - 0.75f, 0f, -0.35f);
            GameObject cloth = Visual("TeaTowel", sink, towel + new Vector3(0f, 0.02f, 0f),
                ArtMeshes.RoundedBox("TeaTowel", new Vector3(0.62f, 0.04f, 0.44f), 0.018f, 3), _teaTowel);
            cloth.transform.rotation = Quaternion.Euler(0f, 11f, 0f);
            SoftShadow("TowelShadow", sink, towel, new Vector2(0.85f, 0.65f), 0.5f);
        }

        /// <summary>
        /// The big dishwasher: a steel cabinet beside the sink with a smoked-glass lid, through which the rack inside
        /// shows every dish loaded (the controller lays out a miniature of each), a strip on the front with the
        /// status light and the cycle's progress, a handle on the lid, and steam at the lid's seam while it washes.
        /// No colliders. Origin at the counter under its centre; 1.5 x 1.2, 0.62 high.
        /// </summary>
        public static void BuildBigDishwasher(Transform root, BigDishwasherController controller)
        {
            var body = new GameObject("Body").transform;
            body.SetParent(root, false);
            Vector3 o = root.position;
            const float width = 1.5f;
            const float depth = 1.2f;
            const float top = 0.6f;

            // Open-topped, or its top would hide the tub inside; the rim covers the gap between the two.
            Visual("Cabinet", body, o + new Vector3(0f, 0.29f, 0f),
                ArtMeshes.RoundedBox("BigDishwasherShell", new Vector3(width, 0.58f, depth), 0.05f, 4, openTop: true), _steel);
            // The rim round the lid, the tub inside, and the rack the dishes stand on.
            Solid("RimFront", body, o + new Vector3(0f, top, -depth * 0.5f + 0.05f), new Vector3(width, 0.05f, 0.1f), 0.02f, _steelDark);
            Solid("RimBack", body, o + new Vector3(0f, top, depth * 0.5f - 0.05f), new Vector3(width, 0.05f, 0.1f), 0.02f, _steelDark);
            Solid("RimLeft", body, o + new Vector3(-width * 0.5f + 0.05f, top, 0f), new Vector3(0.1f, 0.05f, depth), 0.02f, _steelDark);
            Solid("RimRight", body, o + new Vector3(width * 0.5f - 0.05f, top, 0f), new Vector3(0.1f, 0.05f, depth), 0.02f, _steelDark);
            Visual("Tub", body, o + new Vector3(0f, top - 0.2f, 0f),
                ArtMeshes.RoundedBox("BigDishwasherTub", new Vector3(width - 0.2f, 0.4f, depth - 0.2f), 0.05f, 3, openTop: true, inward: true), _machineInterior);
            for (int k = 0; k <= 6; k++)
            {
                Cylinder("RackWire", body, o + new Vector3(-0.6f + k * 0.2f, 0.24f, 0f), new Vector3(0.016f, 0.48f, 0.016f), _wire)
                    .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            for (int k = 0; k <= 4; k++)
            {
                Cylinder("RackWire", body, o + new Vector3(0f, 0.25f, -0.44f + k * 0.22f), new Vector3(0.016f, 0.64f, 0.016f), _wire)
                    .transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }

            // The lid: smoked glass, and its handle along the front.
            GameObject glass = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glass.name = "Lid";
            Object.DestroyImmediate(glass.GetComponent<Collider>());
            glass.transform.SetParent(body, true);
            glass.transform.position = o + new Vector3(0f, top + 0.02f, 0f);
            glass.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            glass.transform.localScale = new Vector3(width - 0.18f, depth - 0.18f, 1f);
            var glassRenderer = glass.GetComponent<Renderer>();
            glassRenderer.sharedMaterial = _machineGlass;
            glassRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Solid("LidHandle", body, o + new Vector3(0f, top + 0.05f, -depth * 0.5f + 0.03f), new Vector3(1.0f, 0.035f, 0.04f), 0.017f, _chrome);

            Solid("ControlStrip", body, o + new Vector3(0f, 0.44f, -depth * 0.5f - 0.005f), new Vector3(1.3f, 0.13f, 0.02f), 0.008f, _panelBlack);
            SoftShadow("Shadow", body, o + new Vector3(0.06f, 0f, -0.05f), new Vector2(width + 0.5f, depth + 0.5f));
            MakeStatic(body);

            GameObject light = Box("StatusLight", root, o + new Vector3(0.52f, 0.44f, -depth * 0.5f - 0.02f), new Vector3(0.07f, 0.07f, 0.02f), _led);
            const float barWidth = 0.7f;
            var progressRoot = new GameObject("Progress").transform;
            progressRoot.SetParent(root, false);
            progressRoot.position = o + new Vector3(-0.12f, 0.44f, -depth * 0.5f - 0.021f);
            GameObject fill = Box("Fill", progressRoot, progressRoot.position, new Vector3(0.0001f, 0.035f, 0.01f), _progress);

            var contents = new GameObject("Contents").transform;
            contents.SetParent(root, false);
            contents.position = o + new Vector3(0f, 0.26f, 0f);

            ParticleSystem steam = BuildSteam(root, o + new Vector3(0f, top + 0.04f, depth * 0.5f - 0.1f), new Vector2(width * 0.8f, 0.15f));
            ParticleSystem.MainModule main = steam.main;
            main.playOnAwake = false;
            main.prewarm = false;
            steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var so = new SerializedObject(controller);
            so.FindProperty("contents").objectReferenceValue = contents;
            so.FindProperty("rackSize").vector2Value = new Vector2(width - 0.26f, depth - 0.26f);
            so.FindProperty("statusLight").objectReferenceValue = light.GetComponent<Renderer>();
            so.FindProperty("progressFill").objectReferenceValue = fill.transform;
            so.FindProperty("progressFullWidth").floatValue = barWidth;
            so.FindProperty("steam").objectReferenceValue = steam;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Steam off the hot water: a few slow, faint wisps drifting up and fading - enough to read "hot" at a
        /// glance, cheap enough for a phone (a couple of dozen soft particles at most).
        /// </summary>
        private static ParticleSystem BuildSteam(Transform parent, Vector3 position, Vector2 area)
        {
            var go = new GameObject("SinkSteam", typeof(ParticleSystem));
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // emit upwards

            var ps = go.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 4.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.7f, 1.3f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(1f, 1f, 1f, 0.075f);
            main.maxParticles = 24;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 4.5f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area.x, area.y, 0.01f);

            ParticleSystem.ColorOverLifetimeModule colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;

            ParticleSystem.SizeOverLifetimeModule grow = ps.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.5f));

            ParticleSystem.RotationOverLifetimeModule spin = ps.rotationOverLifetime;
            spin.enabled = true;
            spin.z = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.08f;
            noise.frequency = 0.4f;
            noise.scrollSpeed = 0.2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = FxArt.Soft;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return ps;
        }

        /// <summary>Plastic tray, round wires every half cell and a wire rim (cells 0.45 m, 6 x 4; origin at the rack's centre).</summary>
        public static void BuildRackFrame(Transform rackGrid)
        {
            var frame = new GameObject("RackFrame").transform;
            frame.SetParent(rackGrid, false);
            Vector3 o = rackGrid.position;
            // Top at 0.035: the cell markers sit just above it (coplanar quads z-fight).
            Visual("Tray", frame, o + new Vector3(0f, 0.017f, 0f), ArtMeshes.RoundedBox("RackTray", new Vector3(2.9f, 0.036f, 2.0f), 0.018f, 3), _rackTray);
            SoftShadow("Shadow", frame, o + new Vector3(0.04f, 0f, -0.04f), new Vector2(3.4f, 2.5f));

            for (int k = 0; k <= 8; k++)
            {
                Cylinder("WireX", frame, o + new Vector3(0f, 0.047f, -0.9f + k * 0.225f), new Vector3(0.024f, 1.4f, 0.024f), _wire)
                    .transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            }

            for (int k = 0; k <= 12; k++)
            {
                Cylinder("WireZ", frame, o + new Vector3(-1.35f + k * 0.225f, 0.06f, 0f), new Vector3(0.024f, 0.95f, 0.024f), _wire)
                    .transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            }

            Visual("RimFront", frame, o + new Vector3(0f, 0.09f, -1.0f), ArtMeshes.RoundedBox("RackRimX", new Vector3(2.93f, 0.14f, 0.035f), 0.017f, 3), _wire);
            Visual("RimBack", frame, o + new Vector3(0f, 0.09f, 1.0f), ArtMeshes.RoundedBox("RackRimX", new Vector3(2.93f, 0.14f, 0.035f), 0.017f, 3), _wire);
            Visual("RimLeft", frame, o + new Vector3(-1.45f, 0.09f, 0f), ArtMeshes.RoundedBox("RackRimZ", new Vector3(0.035f, 0.14f, 2.03f), 0.017f, 3), _wire);
            Visual("RimRight", frame, o + new Vector3(1.45f, 0.09f, 0f), ArtMeshes.RoundedBox("RackRimZ", new Vector3(0.035f, 0.14f, 2.03f), 0.017f, 3), _wire);

            MakeStatic(frame);
        }

        /// <summary>Steel cabinet with rounded edges, a recessed door, handle and a black-glass control strip
        /// carrying the status light and the cycle progress bar.</summary>
        public static void BuildDishwasher(Transform dishwasherRoot, DishwasherController controller)
        {
            var body = new GameObject("Body").transform;
            body.SetParent(dishwasherRoot, false);
            Vector3 o = dishwasherRoot.position;
            Solid("Cabinet", body, o + new Vector3(0f, 0.45f, 0f), new Vector3(1.3f, 0.9f, 1.25f), 0.06f, _steel);
            Solid("Lid", body, o + new Vector3(0f, 0.925f, 0f), new Vector3(1.36f, 0.05f, 1.31f), 0.024f, _steelDark);
            Solid("Door", body, o + new Vector3(0f, 0.4f, -0.628f), new Vector3(1.18f, 0.66f, 0.03f), 0.015f, _steelDark);
            Solid("Handle", body, o + new Vector3(0f, 0.7f, -0.67f), new Vector3(0.8f, 0.05f, 0.05f), 0.024f, _chrome);
            Solid("ControlStrip", body, o + new Vector3(0f, 0.83f, -0.638f), new Vector3(1.2f, 0.1f, 0.02f), 0.009f, _panelBlack);
            // A commercial machine's details: two chrome dials on the strip, a louvred kick plate under the door and
            // a dark gap round the door - enough for it to read as a machine rather than a steel box.
            for (int dial = 0; dial < 2; dial++)
            {
                GameObject knob = Cylinder("Dial", body, o + new Vector3(-0.5f + dial * 0.13f, 0.83f, -0.655f), new Vector3(0.07f, 0.012f, 0.07f), _chrome);
                knob.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
                Solid("DialMark", body, o + new Vector3(-0.5f + dial * 0.13f, 0.845f, -0.668f), new Vector3(0.008f, 0.025f, 0.004f), 0f, _panelBlack);
            }

            Solid("KickPlate", body, o + new Vector3(0f, 0.035f, -0.63f), new Vector3(1.18f, 0.07f, 0.02f), 0.005f, _panelBlack);
            for (int slot = 0; slot < 9; slot++)
            {
                Solid("Louvre", body, o + new Vector3(-0.44f + slot * 0.11f, 0.035f, -0.643f), new Vector3(0.07f, 0.012f, 0.012f), 0.004f, _steelDark);
            }

            Solid("DoorGap", body, o + new Vector3(0f, 0.74f, -0.626f), new Vector3(1.2f, 0.012f, 0.02f), 0f, _panelBlack);
            SoftShadow("Shadow", body, o + new Vector3(0.08f, 0f, -0.06f), new Vector2(1.85f, 1.8f));
            MakeStatic(body);

            GameObject light = Box("StatusLight", dishwasherRoot, o + new Vector3(0.48f, 0.83f, -0.652f), new Vector3(0.07f, 0.07f, 0.02f), _led);
            const float barWidth = 0.7f;
            var progressRoot = new GameObject("Progress").transform;
            progressRoot.SetParent(dishwasherRoot, false);
            progressRoot.position = o + new Vector3(-0.12f, 0.83f, -0.653f);
            GameObject fill = Box("Fill", progressRoot, progressRoot.position, new Vector3(0.0001f, 0.03f, 0.01f), _progress);

            DishwasherVisuals visuals = dishwasherRoot.gameObject.AddComponent<DishwasherVisuals>();
            var so = new SerializedObject(visuals);
            so.FindProperty("dishwasher").objectReferenceValue = controller;
            so.FindProperty("statusLight").objectReferenceValue = light.GetComponent<Renderer>();
            so.FindProperty("progressFill").objectReferenceValue = fill.transform;
            so.FindProperty("progressFullWidth").floatValue = barWidth;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ------------------------------------------------------------------
        // The hand
        // ------------------------------------------------------------------

        private const float PalmY = 0.056f; // real metres above the contact point

        /// <summary>
        /// A right hand in a blue nitrile glove with a white chef's sleeve, palm down, fingers pointing away from
        /// the camera, built in real metres around the contact point (GlovedHand scales it into the scene), and the
        /// things it can hold. No colliders anywhere.
        /// </summary>
        public static GlovedHand BuildHand(Transform parent, DishInputRouter router, WashStation station)
        {
            var root = new GameObject("GlovedHand");
            root.transform.SetParent(parent, false);

            // What it holds, sitting on the contact point.
            var tools = new GameObject("Tools").transform;
            tools.SetParent(root.transform, false);

            var sponge = new GameObject("Sponge").transform;
            sponge.SetParent(tools, false);
            Part("Foam", sponge, new Vector3(0f, 0.025f, 0.012f), ArtMeshes.RoundedBox("SpongeFoam", new Vector3(0.095f, 0.03f, 0.065f), 0.007f, 3), _spongeFoam);
            Part("ScourPad", sponge, new Vector3(0f, 0.005f, 0.012f), ArtMeshes.RoundedBox("SpongePad", new Vector3(0.095f, 0.01f, 0.065f), 0.003f, 2), _scourPad);

            var wool = new GameObject("SteelWool").transform;
            wool.SetParent(tools, false);
            Part("Wad", wool, new Vector3(0f, 0.03f, 0.016f), ArtMeshes.LumpySphere("SteelWool", 0.034f, 0.28f, 7), _steelWool);

            var brush = new GameObject("Brush").transform;
            brush.SetParent(tools, false);
            PartPrimitive(PrimitiveType.Cylinder, "Bristles", brush, new Vector3(0f, 0.007f, 0f), Quaternion.identity, new Vector3(0.046f, 0.007f, 0.046f), _bristles);
            PartPrimitive(PrimitiveType.Cylinder, "Head", brush, new Vector3(0f, 0.02f, 0f), Quaternion.identity, new Vector3(0.05f, 0.006f, 0.05f), _brushPlastic);
            PartPrimitive(PrimitiveType.Capsule, "Handle", brush, new Vector3(0f, 0.048f, -0.08f), Quaternion.Euler(90f + 13f, 0f, 0f),
                new Vector3(0.018f, 0.075f, 0.018f), _brushPlastic);

            // A wide steel blade, its edge on the contact point and tilted up towards a red handle held like the brush's.
            var scraper = new GameObject("Scraper").transform;
            scraper.SetParent(tools, false);
            PartPrimitive(PrimitiveType.Cube, "Blade", scraper, new Vector3(0f, 0.009f, 0f), Quaternion.Euler(20f, 0f, 0f),
                new Vector3(0.085f, 0.0025f, 0.05f), _chrome);
            PartPrimitive(PrimitiveType.Cube, "Holder", scraper, new Vector3(0f, 0.018f, -0.024f), Quaternion.Euler(20f, 0f, 0f),
                new Vector3(0.092f, 0.012f, 0.02f), _brushPlastic);
            PartPrimitive(PrimitiveType.Capsule, "Handle", scraper, new Vector3(0f, 0.04f, -0.09f), Quaternion.Euler(90f + 18f, 0f, 0f),
                new Vector3(0.02f, 0.07f, 0.02f), _brushPlastic);

            // A pre-rinse spray head held back from the dish like a pistol and aimed forwards and down at the contact
            // point, so that from the camera above the water is seen crossing the gap rather than hidden under the hand;
            // a grip runs back from it under the palm, the hose out of its end. Its axis is `aim` (down 0.6, forward 0.8).
            var shower = new GameObject("Shower").transform;
            shower.SetParent(tools, false);
            var aim = new Vector3(0f, -0.6f, 0.8f);
            Quaternion along = Quaternion.Euler(126.87f, 0f, 0f); // turns a primitive's +Y onto `aim`
            Vector3 face = -aim * 0.11f;
            PartPrimitive(PrimitiveType.Cylinder, "Head", shower, face - aim * 0.024f, along, new Vector3(0.056f, 0.024f, 0.056f), _chrome);
            PartPrimitive(PrimitiveType.Cylinder, "Face", shower, face + aim * 0.001f, along, new Vector3(0.046f, 0.002f, 0.046f), _sprayFace);
            var back = new Vector3(0f, 0.35f, -0.94f); // up and back towards the camera, under the palm
            Quaternion gripAlong = Quaternion.Euler(110.5f, 0f, 0f);
            Vector3 gripStart = face - aim * 0.048f;
            PartPrimitive(PrimitiveType.Capsule, "Grip", shower, gripStart + back * 0.055f, gripAlong, new Vector3(0.024f, 0.055f, 0.024f), _chrome);
            PartPrimitive(PrimitiveType.Capsule, "Lever", shower, gripStart + back * 0.05f + new Vector3(0f, 0.016f, 0f), gripAlong,
                new Vector3(0.012f, 0.045f, 0.012f), _brushPlastic);
            PartPrimitive(PrimitiveType.Capsule, "Hose", shower, gripStart + back * 0.23f, gripAlong, new Vector3(0.017f, 0.12f, 0.017f), _sprayFace);
            var nozzle = new GameObject("Nozzle").transform;
            nozzle.SetParent(shower, false);
            nozzle.localPosition = face + aim * 0.004f;
            // The water itself, a glassy cone from the nozzle widening onto the dish (the droplets alone flicker away
            // at a low frame rate); it runs from the contact point back up to the face.
            var jet = new GameObject("SprayJet", typeof(MeshFilter), typeof(MeshRenderer));
            jet.transform.SetParent(shower, false);
            jet.transform.localRotation = Quaternion.LookRotation(aim);
            jet.GetComponent<MeshFilter>().sharedMesh = ArtMeshes.Tube("SprayJet", 0.106f, new[] { 0.03f, 0.022f, 0.014f, 0.0085f }, 1f, segments: 14);
            jet.GetComponent<MeshRenderer>().sharedMaterial = _sprayWater;
            jet.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            SetScroll(jet.AddComponent<TextureScroller>(), new Vector2(0f, 14f)); // down the jet, nozzle to dish

            // The hand itself: the glove modelled in Blender (Tools/Blender/glove.py) skinned to the joints below, or -
            // without that file - a rounded box and capsules standing in for it.
            var hand = new GameObject("Hand").transform;
            hand.SetParent(root.transform, false);
            bool modelledGlove = File.Exists(GloveSourcePath);
            if (!modelledGlove)
            {
                Part("Palm", hand, new Vector3(0f, PalmY, -0.02f), ArtMeshes.RoundedBox("GlovePalm", new Vector3(0.084f, 0.028f, 0.095f), 0.013f, 4), _glove);
            }

            var fingerJoints = new Transform[12];
            float[] fingerX = { -0.029f, -0.0095f, 0.0095f, 0.028f };
            float[] fingerYaw = { -3f, 0f, 2f, 6f };
            float[] radius = { 0.0085f, 0.009f, 0.0085f, 0.0075f };
            float[][] lengths =
            {
                new[] { 0.042f, 0.025f, 0.021f },
                new[] { 0.046f, 0.028f, 0.023f },
                new[] { 0.043f, 0.026f, 0.022f },
                new[] { 0.034f, 0.02f, 0.019f }
            };
            for (int f = 0; f < 4; f++)
            {
                Transform joint = Joint($"Finger{f}_1", hand, new Vector3(fingerX[f], PalmY - 0.002f, 0.0265f), Quaternion.Euler(0f, fingerYaw[f], 0f));
                for (int j = 0; j < 3; j++)
                {
                    fingerJoints[f * 3 + j] = joint;
                    if (!modelledGlove)
                    {
                        Phalanx(joint, lengths[f][j], radius[f] * (1f - j * 0.06f));
                    }

                    if (j < 2)
                    {
                        joint = Joint($"Finger{f}_{j + 2}", joint, new Vector3(0f, 0f, lengths[f][j] - radius[f] * 0.6f), Quaternion.identity);
                    }
                }
            }

            var thumbJoints = new Transform[3];
            Transform thumb = Joint("Thumb_0", hand, new Vector3(-0.034f, PalmY - 0.006f, -0.048f), Quaternion.Euler(22f, 0f, -20f));
            float[] thumbLengths = { 0.036f, 0.03f, 0.025f };
            for (int j = 0; j < 3; j++)
            {
                thumbJoints[j] = thumb;
                if (!modelledGlove)
                {
                    Phalanx(thumb, thumbLengths[j], 0.0105f * (1f - j * 0.06f));
                }

                if (j < 2)
                {
                    thumb = Joint($"Thumb_{j + 1}", thumb, new Vector3(0f, 0f, thumbLengths[j] - 0.006f), Quaternion.identity);
                }
            }

            if (modelledGlove)
            {
                BuildGlove(hand, fingerJoints, thumbJoints);
            }

            // The forearm, rising towards the camera as it goes back: the glove's cuff flaring over the wrist, bare
            // skin tapering out towards the elbow, and the chef's-jacket sleeve rolled up above it. (A white capsule
            // standing in for all of it read as a sausage lying across the sink.) It runs on under the toolbar.
            Transform forearm = Joint("Forearm", hand, new Vector3(0f, PalmY - 0.002f, -0.05f), Quaternion.Euler(16f, 0f, 0f));
            Part("Arm", forearm, Vector3.zero, ArtMeshes.Tube("HandForearm", 0.34f, new[] { 0.029f, 0.031f, 0.035f, 0.039f, 0.041f }, 0.78f), _skin);
            const float cuffStart = 0.012f;
            const float cuffLength = 0.09f;
            Part("Cuff", forearm, new Vector3(0f, 0f, cuffStart),
                ArtMeshes.Tube("GloveCuff", cuffLength, new[] { 0.033f, 0.0345f, 0.037f, 0.039f }, 0.8f, roundStart: true), _gloveCuff);
            // Where the bare arm comes out of the cuff, in the arm's own space: the wet band and the flush start there.
            _skin.SetFloat("_WristZ", cuffStart - cuffLength);
            EditorUtility.SetDirty(_skin);
            Part("SleeveRoll", forearm, new Vector3(0f, 0f, -0.215f),
                ArtMeshes.Tube("SleeveRoll", 0.075f, new[] { 0.042f, 0.05f, 0.052f, 0.049f, 0.046f }, 0.82f), _sleeve);
            Part("Sleeve", forearm, new Vector3(0f, 0f, -0.28f), ArtMeshes.Tube("Sleeve", 0.45f, new[] { 0.046f, 0.05f, 0.053f, 0.055f }, 0.85f), _sleeve);

            GlovedHand component = root.AddComponent<GlovedHand>();
            root.AddComponent<HandWear>(); // what the shift does to the arm: drives Zmywak/Hand and the hand's tremor
            var so = new SerializedObject(component);
            so.FindProperty("router").objectReferenceValue = router;
            so.FindProperty("washStation").objectReferenceValue = station;
            so.FindProperty("hand").objectReferenceValue = hand;
            SerializedProperty fingers = so.FindProperty("fingerJoints");
            fingers.arraySize = fingerJoints.Length;
            for (int i = 0; i < fingerJoints.Length; i++)
            {
                fingers.GetArrayElementAtIndex(i).objectReferenceValue = fingerJoints[i];
            }

            SerializedProperty thumbs = so.FindProperty("thumbJoints");
            thumbs.arraySize = thumbJoints.Length;
            for (int i = 0; i < thumbJoints.Length; i++)
            {
                thumbs.GetArrayElementAtIndex(i).objectReferenceValue = thumbJoints[i];
            }

            so.FindProperty("sponge").objectReferenceValue = sponge.gameObject;
            so.FindProperty("steelWool").objectReferenceValue = wool.gameObject;
            so.FindProperty("brush").objectReferenceValue = brush.gameObject;
            so.FindProperty("scraper").objectReferenceValue = scraper.gameObject;
            so.FindProperty("shower").objectReferenceValue = shower.gameObject;
            so.FindProperty("sprayNozzle").objectReferenceValue = nozzle;
            so.FindProperty("sprayJet").objectReferenceValue = jet;
            so.ApplyModifiedPropertiesWithoutUndo();
            return component;
        }

        /// <summary>The glove as modelled in Blender: run Tools/Blender/glove.py to write it (see its header).</summary>
        private const string GloveSourcePath = "Assets/_Project/Art/Source/GloveHand.json";
        private const string GloveMeshPath = "Assets/_Project/Art/Meshes/GloveHand.asset";

        // How far either side of a joint the glove blends from one bone to the next: a bent knuckle stays round.
        private const float GloveJointBlend = 0.006f;

        [System.Serializable]
        private class GloveSource
        {
            public float[] vertices;
            public float[] normals;
            public int[] triangles;
            public int[] parts; // 0 palm, 1-4 fingers (index to little), 5 thumb
        }

        /// <summary>
        /// Skins Blender's glove to the rig: the back of the hand to the hand itself; along each finger and the thumb,
        /// each stretch to the joint it hangs from, fading from one to the next across every joint, and from the hand
        /// into the first joint where the finger leaves the palm. The joints' rest pose here is the pose the glove was
        /// modelled in, so every curl GlovedHand already applies bends it.
        /// </summary>
        private static void BuildGlove(Transform hand, Transform[] fingerJoints, Transform[] thumbJoints)
        {
            var source = JsonUtility.FromJson<GloveSource>(File.ReadAllText(GloveSourcePath));
            int count = source.parts.Length;
            var vertices = new Vector3[count];
            var normals = new Vector3[count];
            var uv = new Vector2[count];
            var rest = new List<Vector4>(count); // Zmywak/Hand's wear: the rest position, and how far out along a finger
            var weights = new BoneWeight[count];

            // Bones: the hand, the twelve finger joints, the three thumb joints.
            var bones = new Transform[1 + fingerJoints.Length + thumbJoints.Length];
            bones[0] = hand;
            fingerJoints.CopyTo(bones, 1);
            thumbJoints.CopyTo(bones, 1 + fingerJoints.Length);

            for (int i = 0; i < count; i++)
            {
                Vector3 p = vertices[i] = new Vector3(source.vertices[i * 3], source.vertices[i * 3 + 1], source.vertices[i * 3 + 2]);
                Vector3 n = normals[i] = new Vector3(source.normals[i * 3], source.normals[i * 3 + 1], source.normals[i * 3 + 2]);

                // Box-mapped for the nitrile's fine grain (the material tiles it): no seam shows through a noise.
                Vector3 a = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
                uv[i] = (a.y >= a.x && a.y >= a.z ? new Vector2(p.x, p.z) : a.x >= a.z ? new Vector2(p.z, p.y) : new Vector2(p.x, p.y)) / 0.05f;

                int part = source.parts[i];
                float tip = 0f;
                if (part == 0)
                {
                    weights[i] = new BoneWeight { boneIndex0 = 0, weight0 = 1f };
                }
                else if (part <= 4)
                {
                    int first = (part - 1) * 3;
                    weights[i] = ChainWeight(hand, p, fingerJoints[first], fingerJoints[first + 1], fingerJoints[first + 2], 1 + first);
                    tip = AlongChain(hand, p, fingerJoints[first], fingerJoints[first + 2]);
                }
                else
                {
                    weights[i] = ChainWeight(hand, p, thumbJoints[0], thumbJoints[1], thumbJoints[2], 1 + fingerJoints.Length);
                    tip = AlongChain(hand, p, thumbJoints[0], thumbJoints[2]);
                }

                rest.Add(new Vector4(p.x, p.y, p.z, tip));
            }

            var glove = new GameObject("Glove", typeof(SkinnedMeshRenderer));
            glove.transform.SetParent(hand, false);
            var bindposes = new Matrix4x4[bones.Length];
            for (int b = 0; b < bones.Length; b++)
            {
                bindposes[b] = bones[b].worldToLocalMatrix * glove.transform.localToWorldMatrix;
            }

            var mesh = new Mesh { name = "GloveHand" };
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.uv = uv;
            mesh.triangles = source.triangles;
            mesh.boneWeights = weights;
            mesh.bindposes = bindposes;
            mesh.RecalculateTangents(); // the nitrile normal map
            mesh.RecalculateBounds();
            mesh.SetUVs(2, rest);
            mesh = SaveMesh(mesh, GloveMeshPath);
            if (!mesh.HasVertexAttribute(UnityEngine.Rendering.VertexAttribute.TexCoord2))
            {
                // Copying a mesh over the saved one has been seen to drop a third UV channel.
                Debug.Log("Zmywak: glove rest UVs lost when saving - setting them on the asset");
                mesh.SetUVs(2, rest);
                EditorUtility.SetDirty(mesh);
            }

            var skinned = glove.GetComponent<SkinnedMeshRenderer>();
            skinned.sharedMesh = mesh;
            skinned.bones = bones;
            skinned.rootBone = hand;
            skinned.sharedMaterial = _glove;
            skinned.quality = SkinQuality.Bone2;
            // Curled fingers reach outside the flat hand's box.
            Bounds bounds = mesh.bounds;
            bounds.Expand(0.08f);
            skinned.localBounds = bounds;
        }

        /// <summary>
        /// How far out along a finger or the thumb a point lies: 0 where it leaves the hand, 1 at the tip (taken as
        /// the last joint plus a fingertip's length).
        /// </summary>
        private static float AlongChain(Transform hand, Vector3 point, Transform first, Transform last)
        {
            Vector3 origin = hand.InverseTransformPoint(first.position);
            float t = Vector3.Dot(point - origin, hand.InverseTransformDirection(first.forward));
            float length = Vector3.Distance(origin, hand.InverseTransformPoint(last.position)) + 0.022f;
            return Mathf.Clamp01(t / length);
        }

        /// <summary>
        /// Weight along a finger or the thumb: how far the point lies along the chain from its first joint decides
        /// which stretch carries it, blended over <see cref="GloveJointBlend"/> either side of each joint.
        /// </summary>
        private static BoneWeight ChainWeight(Transform hand, Vector3 point, Transform j0, Transform j1, Transform j2, int firstBone)
        {
            Vector3 origin = hand.InverseTransformPoint(j0.position);
            Vector3 along = hand.InverseTransformDirection(j0.forward);
            float t = Vector3.Dot(point - origin, along);
            float t1 = Vector3.Distance(origin, hand.InverseTransformPoint(j1.position));
            float t2 = Vector3.Distance(origin, hand.InverseTransformPoint(j2.position));

            float s0 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-GloveJointBlend, GloveJointBlend, t));
            float s1 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(t1 - GloveJointBlend, t1 + GloveJointBlend, t));
            float s2 = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(t2 - GloveJointBlend, t2 + GloveJointBlend, t));
            float handWeight = 1f - s0;
            float w0 = s0 * (1f - s1);
            float w1 = s0 * s1 * (1f - s2);
            float w2 = s0 * s1 * s2;

            // At most two bones ever share a point (the blends do not overlap): keep the two heaviest.
            var candidates = new[] { (0, handWeight), (firstBone, w0), (firstBone + 1, w1), (firstBone + 2, w2) };
            System.Array.Sort(candidates, (x, y) => y.Item2.CompareTo(x.Item2));
            float total = candidates[0].Item2 + candidates[1].Item2;
            return new BoneWeight
            {
                boneIndex0 = candidates[0].Item1,
                weight0 = candidates[0].Item2 / total,
                boneIndex1 = candidates[1].Item1,
                weight1 = candidates[1].Item2 / total
            };
        }

        /// <summary>Saves a mesh as an asset (replacing last run's in place, so references to it hold).</summary>
        internal static Mesh SaveMesh(Mesh mesh, string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null)
            {
                AssetDatabase.CreateAsset(mesh, path);
                return mesh;
            }

            EditorUtility.CopySerialized(mesh, existing);
            existing.name = mesh.name;
            Object.DestroyImmediate(mesh);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        private static Transform Joint(string name, Transform parent, Vector3 localPosition, Quaternion localRotation)
        {
            var joint = new GameObject(name).transform;
            joint.SetParent(parent, false);
            joint.localPosition = localPosition;
            joint.localRotation = localRotation;
            return joint;
        }

        /// <summary>One finger bone: a capsule lying along the joint's +Z, <paramref name="length"/> long including its round ends.</summary>
        private static void Phalanx(Transform joint, float length, float radius)
        {
            PartPrimitive(PrimitiveType.Capsule, "Bone", joint, new Vector3(0f, 0f, length * 0.5f), Quaternion.Euler(90f, 0f, 0f),
                new Vector3(radius * 2f, length * 0.5f, radius * 2f), _glove);
        }

        internal static void Part(string name, Transform parent, Vector3 localPosition, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        internal static void PartPrimitive(PrimitiveType type, string name, Transform parent, Vector3 localPosition, Quaternion localRotation, Vector3 scale,
            Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = localRotation;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
        }

        // ------------------------------------------------------------------
        // Pieces
        // ------------------------------------------------------------------

        /// <summary>A flat-edged slab: rounded edges where two pieces meet caught the light differently and drew seams.</summary>
        private static void CounterPiece(string name, Transform parent, float xMin, float xMax, float zMin, float zMax)
        {
            var size = new Vector3(xMax - xMin, 0.12f, zMax - zMin);
            var centre = new Vector3((xMin + xMax) * 0.5f, -0.06f, (zMin + zMax) * 0.5f);
            // UVs from world positions: the brushing runs on from one piece to the next without a seam.
            Visual(name, parent, centre, ArtMeshes.RoundedBox($"Counter_{name}", size, 0f, 2, uvOrigin: centre), _steel);
        }

        /// <summary>A rounded box of this size (a mesh per distinct shape, shared between identical pieces).</summary>
        private static GameObject Solid(string name, Transform parent, Vector3 worldPosition, Vector3 size, float radius, Material material)
        {
            string meshName = $"Box_{size.x:0.###}x{size.y:0.###}x{size.z:0.###}_r{radius:0.###}".Replace(',', '.');
            return Visual(name, parent, worldPosition, ArtMeshes.RoundedBox(meshName, size, radius, radius > 0.02f ? 4 : 2), material);
        }

        internal static GameObject Visual(string name, Transform parent, Vector3 worldPosition, Mesh mesh, Material material)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            go.GetComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static void ColliderBox(string name, Transform parent, Vector3 worldPosition, Vector3 size)
        {
            var go = new GameObject(name, typeof(BoxCollider));
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;
            go.GetComponent<BoxCollider>().size = size;
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        /// <summary>A soft dark patch on the counter under an object: the ambient occlusion no light bake gives us.</summary>
        internal static void SoftShadow(string name, Transform parent, Vector3 groundPosition, Vector2 size, float strength = 1f)
        {
            GameObject shadow = Visual(name, parent, groundPosition + new Vector3(0f, 0.004f, 0f), ArtMeshes.GroundQuad("GroundQuad"),
                strength < 1f ? _shadowLight : _shadow);
            shadow.transform.localScale = new Vector3(size.x, 1f, size.y);
            var renderer = shadow.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        private static void SetScroll(TextureScroller scroller, Vector2 speed)
        {
            var so = new SerializedObject(scroller);
            so.FindProperty("speed").vector2Value = speed;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void MakeStatic(Transform root)
        {
            foreach (Transform child in root)
            {
                SetStatic(child.gameObject);
            }
        }

        // ------------------------------------------------------------------
        // Textures
        // ------------------------------------------------------------------

        private const int TileW = 128;
        private const int TileH = 64;
        private const float Grout = 3f;
        private const float Bevel = 9f;

        private static float TileHeight(int x, int y, out int tileId)
        {
            int row = y / TileH;
            int shifted = x + (row % 2 == 1 ? TileW / 2 : 0);
            int col = shifted / TileW;
            int lx = shifted % TileW;
            int ly = y % TileH;
            tileId = row * 31 + col * 17;
            float d = Mathf.Min(Mathf.Min(lx, TileW - 1 - lx), Mathf.Min(ly, TileH - 1 - ly));
            if (d < Grout)
            {
                return 0f;
            }

            float t = Mathf.Clamp01((d - Grout) / Bevel);
            return 0.35f + 0.65f * t * t * (3f - 2f * t);
        }

        private static Color GenerateTileAlbedo(int x, int y, int w, int h)
        {
            float height = TileHeight(x, y, out int tileId);
            if (height <= 0f)
            {
                return new Color(0.6f, 0.58f, 0.55f);
            }

            float variation = ((tileId * 9301 + 49297) % 233280) / 233280f; // cheap per-tile hash
            float shade = 0.95f + variation * 0.04f - (1f - height) * 0.08f;
            return new Color(0.97f * shade, 0.96f * shade, 0.92f * shade);
        }

        private static Color GenerateTileNormal(int x, int y, int w, int h)
        {
            float left = TileHeight((x - 1 + w) % w, y, out _);
            float right = TileHeight((x + 1) % w, y, out _);
            float down = TileHeight(x, (y - 1 + h) % h, out _);
            float up = TileHeight(x, (y + 1) % h, out _);
            return EncodeNormal((left - right) * 2.5f, (down - up) * 2.5f);
        }

        /// <summary>
        /// Brushed steel, at about one texel per screen pixel on a phone (512 texels per 2.2 m): fine scratch lines
        /// along X that start and stop (an earlier version ran every line the whole width, which read as a TV's
        /// scanlines), a faint cloudiness, and the lighter rings dried water drops leave.
        /// </summary>
        private static Color GenerateBrushedSteel(int x, int y, int w, int h)
        {
            float u = (x + 0.5f) / w;
            float v = (y + 0.5f) / h;
            float cloud = PeriodicNoise(u, v, 4, 4, 5) * 0.6f + PeriodicNoise(u, v, 9, 9, 6) * 0.4f;
            float value = 0.72f + (BrushedHeight(u, v) - 0.5f) * 0.02f + (cloud - 0.5f) * 0.05f + WaterSpots(x, y, w, h) * 0.04f;
            return new Color(value, value, value * 1.015f);
        }

        /// <summary>The brushing as relief: long scratch lines of three lengths and depths, so a highlight breaks into
        /// fine streaks along the grain the way real brushed steel catches a light. (Short, strong ones read as fur.)</summary>
        private static float BrushedHeight(float u, float v)
        {
            return PeriodicNoise(u, v, 3, 256, 1) * 0.45f + PeriodicNoise(u, v, 7, 512, 2) * 0.35f + PeriodicNoise(u, v, 2, 128, 3) * 0.2f;
        }

        private static Color GenerateBrushedSteelNormal(int x, int y, int w, int h)
        {
            float du = 1f / w;
            float dv = 1f / h;
            float u = (x + 0.5f) * du;
            float v = (y + 0.5f) * dv;
            return EncodeNormal((BrushedHeight(u - du, v) - BrushedHeight(u + du, v)) * 0.6f,
                (BrushedHeight(u, v - dv) - BrushedHeight(u, v + dv)) * 1.2f);
        }

        /// <summary>
        /// The steel's metallic-gloss map (R metallic, A gloss): where a kitchen's steel is duller than the rest -
        /// soft smears from hands and cloths, arcs where it was wiped in circles, dried water drops - and the
        /// brushing's own faint shimmer. A flat counter seen through a long lens mirrors nearly the same direction
        /// everywhere, so it is largely these patches, not the reflection, that keep the steel from reading as flat grey.
        /// </summary>
        private static Color GenerateSteelFinish(int x, int y, int w, int h)
        {
            float u = (x + 0.5f) / w;
            float v = (y + 0.5f) / h;
            float smear = Smooth(0.52f, 0.85f, PeriodicNoise(u, v, 3, 3, 21) * 0.6f + PeriodicNoise(u, v, 8, 8, 22) * 0.4f);
            float gloss = 0.95f + (BrushedHeight(u, v) - 0.5f) * 0.05f - smear * 0.12f - WipeArcs(u, v) * 0.05f - WaterSpots(x, y, w, h) * 0.25f;
            const float metallic = 0.85f;
            return new Color(metallic, metallic, metallic, Mathf.Clamp01(gloss));
        }

        /// <summary>Arcs a cloth leaves when steel is wiped in circles: parts of a few rings of fine concentric lines.</summary>
        private static float WipeArcs(float u, float v)
        {
            float result = 0f;
            for (int k = 0; k < 4; k++)
            {
                // The nearest copy of each centre, so arcs run across the tile's edges without a seam.
                float dx = Mathf.Repeat(u - Hash01(k * 7919 + 11) + 0.5f, 1f) - 0.5f;
                float dy = Mathf.Repeat(v - Hash01(k * 104729 + 13) + 0.5f, 1f) - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float middle = 0.12f + Hash01(k * 31 + 5) * 0.12f;
                float band = Smooth(middle - 0.09f, middle - 0.03f, r) * (1f - Smooth(middle + 0.03f, middle + 0.09f, r));
                float sweep = Smooth(-0.2f, 0.6f, Mathf.Sin(Mathf.Atan2(dy, dx) + k * 1.7f)); // only part of each circle
                float lines = 0.5f + 0.5f * Mathf.Sin(r * 700f + PeriodicNoise(u, v, 16, 16, 40 + k) * 3f);
                result = Mathf.Max(result, band * sweep * lines);
            }

            return result;
        }

        /// <summary>Dried water drops, 1-3 cm across: a faint film inside and a ring at the edge, where the water shrank to.
        /// Scattered on a jittered grid that wraps, so the texture still tiles.</summary>
        private static float WaterSpots(int x, int y, int w, int h)
        {
            const int cells = 8;
            int cellW = w / cells;
            int cellH = h / cells;
            int cx = x / cellW;
            int cy = y / cellH;
            float best = 0f;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int gx = cx + ox;
                    int gy = cy + oy;
                    int wx = (gx % cells + cells) % cells;
                    int wy = (gy % cells + cells) % cells;
                    for (int s = 0; s < 2; s++)
                    {
                        int id = (wx * 31 + wy) * 7 + s;
                        if (Hash01(id * 977 + 3) > 0.2f)
                        {
                            continue; // a few drops here and there; more read as bubbles all over
                        }

                        float px = (gx + Hash01(id * 7919 + 1)) * cellW;
                        float py = (gy + Hash01(id * 104729 + 2)) * cellH;
                        float radius = 2.5f + Hash01(id * 31337 + 4) * 5f;
                        float d = Mathf.Sqrt((x - px) * (x - px) + (y - py) * (y - py)) / radius;
                        if (d > 1.3f)
                        {
                            continue;
                        }

                        float ring = Mathf.Exp(-Mathf.Pow((d - 0.92f) / 0.13f, 2f));
                        best = Mathf.Max(best, Mathf.Max(ring, d < 0.92f ? 0.3f : 0f));
                    }
                }
            }

            return best;
        }

        /// <summary>
        /// Smooth value noise on a lattice of <paramref name="cellsX"/> by <paramref name="cellsY"/> cells that wraps at
        /// the texture's edges, so it tiles; unequal counts stretch it into streaks.
        /// </summary>
        internal static float PeriodicNoise(float u, float v, int cellsX, int cellsY, int seed)
        {
            float x = u * cellsX;
            float y = v * cellsY;
            int x0 = Mathf.FloorToInt(x);
            int y0 = Mathf.FloorToInt(y);
            float fx = x - x0;
            float fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);
            float bottom = Mathf.Lerp(Lattice(x0, y0, cellsX, cellsY, seed), Lattice(x0 + 1, y0, cellsX, cellsY, seed), fx);
            float top = Mathf.Lerp(Lattice(x0, y0 + 1, cellsX, cellsY, seed), Lattice(x0 + 1, y0 + 1, cellsX, cellsY, seed), fx);
            return Mathf.Lerp(bottom, top, fy);
        }

        private static float Lattice(int x, int y, int cellsX, int cellsY, int seed)
        {
            x = (x % cellsX + cellsX) % cellsX;
            y = (y % cellsY + cellsY) % cellsY;
            return Hash01((x * 73856093) ^ (y * 19349663) ^ (seed * 83492791));
        }

        private static Color GeneratePlastic(int x, int y, int w, int h)
        {
            float speck = Hash01(x * 73856093 ^ y * 19349663);
            float cloud = Mathf.PerlinNoise(x / (float)w * 4f + 13f, y / (float)h * 4f + 7f);
            float v = 0.93f + (cloud - 0.5f) * 0.08f + (speck > 0.985f ? -0.12f : 0f);
            return new Color(v, v, v);
        }

        /// <summary>Tileable ripples: a few sine waves whose periods divide the texture, so it repeats seamlessly.</summary>
        private static float WaterHeight(int x, int y, int w, int h)
        {
            float u = x / (float)w * Mathf.PI * 2f;
            float v = y / (float)h * Mathf.PI * 2f;
            return Mathf.Sin(u * 3f + v * 2f) * 0.35f + Mathf.Sin(u * -2f + v * 5f + 1.3f) * 0.25f +
                   Mathf.Sin(u * 7f + v * -3f + 2.1f) * 0.15f + Mathf.Sin(u * 11f + v * 9f + 0.7f) * 0.08f;
        }

        /// <summary>
        /// The spray's water: four soft streaks across the tile (round the jet), each broken into beads along its
        /// length at its own whole number of beads per tile, so it repeats seamlessly both ways; a faint film between.
        /// </summary>
        private static Color GenerateSprayStreaks(int x, int y, int w, int h)
        {
            float u = x / (float)w;
            float v = y / (float)h;
            float[] centres = { 0.12f, 0.37f, 0.6f, 0.84f };
            float[] widths = { 0.05f, 0.035f, 0.06f, 0.04f };
            float alpha = 0.12f;
            for (int i = 0; i < centres.Length; i++)
            {
                float d = (Mathf.Abs(Mathf.Repeat(u - centres[i] + 0.5f, 1f) - 0.5f)) / widths[i];
                float beads = 0.55f + 0.45f * Mathf.Sin((v * (2 + i) + centres[i]) * Mathf.PI * 2f);
                alpha = Mathf.Max(alpha, Mathf.Exp(-d * d) * beads);
            }

            return new Color(1f, 1f, 1f, alpha);
        }

        private static Color GenerateWaterNormal(int x, int y, int w, int h)
        {
            float left = WaterHeight((x - 1 + w) % w, y, w, h);
            float right = WaterHeight((x + 1) % w, y, w, h);
            float down = WaterHeight(x, (y - 1 + h) % h, w, h);
            float up = WaterHeight(x, (y + 1) % h, w, h);
            return EncodeNormal((left - right) * 4f, (down - up) * 4f);
        }

        /// <summary>Soap foam: clusters of bubbles (tileable), bright where they gather, clear water between.</summary>
        private static Color GenerateFoam(int x, int y, int w, int h)
        {
            float coverage = TileableNoise(x, y, w, h, 3f, 11f) * 0.65f + TileableNoise(x, y, w, h, 7f, 29f) * 0.35f;
            float clusters = Smooth(0.6f, 0.76f, coverage);
            // Bubbles: a cell grid with a round bubble in each cell, seen where the clusters are.
            const int cells = 32;
            float cx = x * cells / (float)w;
            float cy = y * cells / (float)h;
            int ix = Mathf.FloorToInt(cx);
            int iy = Mathf.FloorToInt(cy);
            float r = 0.25f + Hash01(ix * 92821 + iy * 68917) * 0.2f;
            float dx = cx - ix - 0.5f;
            float dy = cy - iy - 0.5f;
            float bubble = 1f - Smooth(r - 0.12f, r, Mathf.Sqrt(dx * dx + dy * dy));
            float alpha = clusters * (0.55f + 0.45f * bubble);
            return new Color(1f, 1f, 1f, alpha);
        }

        internal static float TileableNoise(int x, int y, int w, int h, float scale, float seed)
        {
            // Perlin sampled on a torus: blend four shifted copies so the edges meet.
            float u = x / (float)w;
            float v = y / (float)h;
            float a = Mathf.PerlinNoise(u * scale + seed, v * scale + seed);
            float b = Mathf.PerlinNoise((u - 1f) * scale + seed, v * scale + seed);
            float c = Mathf.PerlinNoise(u * scale + seed, (v - 1f) * scale + seed);
            float d = Mathf.PerlinNoise((u - 1f) * scale + seed, (v - 1f) * scale + seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
        }

        /// <summary>Nitrile's faint grain: small smooth bumps.</summary>
        private static float GloveHeight(int x, int y, int w, int h)
        {
            return TileableNoise(x, y, w, h, 24f, 5f) * 0.7f + Hash01(x * 92821 + y * 68917) * 0.3f;
        }

        private static Color GenerateGloveNormal(int x, int y, int w, int h)
        {
            return EncodeNormal((GloveHeight((x - 1 + w) % w, y, w, h) - GloveHeight((x + 1) % w, y, w, h)) * 1.5f,
                (GloveHeight(x, (y - 1 + h) % h, w, h) - GloveHeight(x, (y + 1) % h, w, h)) * 1.5f);
        }

        /// <summary>Cotton twill: threads going over and under each other.</summary>
        private static float WeaveHeight(int x, int y)
        {
            const int thread = 4;
            bool warpOnTop = ((x / thread) + (y / thread)) % 2 == 0;
            float across = Mathf.Sin((warpOnTop ? x : y) % thread / (float)thread * Mathf.PI);
            return (warpOnTop ? 0.6f : 0.4f) + across * 0.4f;
        }

        internal static Color GenerateWeaveNormal(int x, int y, int w, int h)
        {
            return EncodeNormal((WeaveHeight((x - 1 + w) % w, y) - WeaveHeight((x + 1) % w, y)) * 1.2f,
                (WeaveHeight(x, (y - 1 + h) % h) - WeaveHeight(x, (y + 1) % h)) * 1.2f);
        }

        /// <summary>Foam: open pores of various sizes scattered through it.</summary>
        private static float SpongeHeight(int x, int y, int w, int h)
        {
            const int cells = 18;
            float cx = x * cells / (float)w;
            float cy = y * cells / (float)h;
            float nearest = 9f;
            int ix = Mathf.FloorToInt(cx);
            int iy = Mathf.FloorToInt(cy);
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int gx = (ix + ox + cells) % cells;
                    int gy = (iy + oy + cells) % cells;
                    float px = ix + ox + Hash01(gx * 7919 + gy * 104729);
                    float py = iy + oy + Hash01(gx * 15731 + gy * 789221);
                    float r = 0.18f + Hash01(gx * 31337 + gy * 271) * 0.22f;
                    float d = Mathf.Sqrt((cx - px) * (cx - px) + (cy - py) * (cy - py)) / r;
                    nearest = Mathf.Min(nearest, d);
                }
            }

            return Smooth(0.6f, 1.1f, nearest);
        }

        private static Color GenerateSpongeAlbedo(int x, int y, int w, int h)
        {
            float v = 0.72f + 0.28f * SpongeHeight(x, y, w, h);
            return new Color(v, v, v);
        }

        private static Color GenerateSpongeNormal(int x, int y, int w, int h)
        {
            return EncodeNormal((SpongeHeight((x - 1 + w) % w, y, w, h) - SpongeHeight((x + 1) % w, y, w, h)) * 2f,
                (SpongeHeight(x, (y - 1 + h) % h, w, h) - SpongeHeight(x, (y + 1) % h, w, h)) * 2f);
        }

        /// <summary>Fibres: noise stretched hard in two crossing directions (steel wool, the scouring pad, bristles).</summary>
        private static float FibreHeight(int x, int y, int w, int h)
        {
            float u = x / (float)w;
            float v = y / (float)h;
            float a = Mathf.PerlinNoise(u * 3f + 17f, v * 60f + 3f);
            float b = Mathf.PerlinNoise((u + v) * 45f + 7f, (u - v) * 2.5f + 11f);
            return Mathf.Max(a, b * 0.9f);
        }

        private static Color GenerateFibreNormal(int x, int y, int w, int h)
        {
            return EncodeNormal((FibreHeight((x - 1 + w) % w, y, w, h) - FibreHeight((x + 1) % w, y, w, h)) * 3f,
                (FibreHeight(x, (y - 1 + h) % h, w, h) - FibreHeight(x, (y + 1) % h, w, h)) * 3f);
        }

        /// <summary>A cotton tea towel: cream with a band of red stripes, tiling (it is folded, so any cut of it will do).</summary>
        private static Color GenerateTeaTowel(int x, int y, int w, int h)
        {
            float u = (x + 0.5f) / w;
            float v = (y + 0.5f) / h;
            var cream = new Color(0.93f, 0.91f, 0.85f);
            var red = new Color(0.7f, 0.12f, 0.1f);
            bool wide = u > 0.12f && u < 0.22f;
            bool thin = (u > 0.27f && u < 0.3f) || (u > 0.04f && u < 0.07f);
            bool across = v > 0.46f && v < 0.49f;
            Color c = wide || thin ? red : across ? Color.Lerp(cream, red, 0.6f) : cream;
            float thread = Hash01(x * 73856093 ^ y * 19349663) * 0.05f;
            return c * (0.97f - thread);
        }

        /// <summary>Black with alpha rising up the texture: nothing just above the counter, the full shade at the top.</summary>
        private static Color GenerateWallShade(int x, int y, int w, int h)
        {
            float v = (y + 0.5f) / h;
            float alpha = Smooth(0.12f, 0.95f, v);
            return new Color(0f, 0f, 0f, alpha * (0.6f + 0.4f * alpha));
        }

        /// <summary>
        /// A kitchen order ticket: thermal paper with a printed header bar and table number, a few lines of the
        /// waiter's handwriting in blue biro, a chef's tick in red, and a torn edge where it came off the pad.
        /// </summary>
        private static Color GenerateOrderTicket(int x, int y, int w, int h)
        {
            // Painted turned half round: the tickets are Unity cubes, whose face towards the camera maps the texture
            // upside down.
            x = w - 1 - x;
            y = h - 1 - y;
            float u = (x + 0.5f) / w;
            float v = (y + 0.5f) / h;
            var paper = new Color(0.97f, 0.96f, 0.91f);
            var print = new Color(0.16f, 0.16f, 0.17f);
            var biro = new Color(0.14f, 0.2f, 0.45f);

            // The torn top edge: a ragged line of shadow.
            float tear = 0.965f + 0.02f * Mathf.PerlinNoise(u * 18f, 3.1f);
            if (v > tear)
            {
                return paper * 0.82f;
            }

            // Printed header: a bar with the table number knocked out of it, and a printed rule under it.
            if (v > 0.84f && v < 0.92f && u > 0.1f && u < 0.9f)
            {
                bool numeral = u > 0.66f && u < 0.84f && v > 0.855f && v < 0.905f && (x / 3 + y / 3) % 2 == 0;
                return numeral ? paper : print;
            }

            if (v > 0.8f && v < 0.81f && u > 0.1f && u < 0.9f)
            {
                return Color.Lerp(paper, print, 0.6f);
            }

            // Handwriting: wavy strokes of different lengths on five lines, broken into "words".
            for (int line = 0; line < 5; line++)
            {
                float baseline = 0.7f - line * 0.12f;
                float length = 0.45f + 0.4f * Hash01(line * 7919 + 17);
                if (u < 0.12f || u > 0.12f + length)
                {
                    continue;
                }

                float word = Mathf.Repeat(u * 7f + Hash01(line * 31) * 3f, 1f);
                if (word > 0.82f)
                {
                    continue; // the gap between words
                }

                float wave = Mathf.Sin(u * 95f + line * 1.7f) * 0.018f + Mathf.Sin(u * 37f + line) * 0.008f;
                if (Mathf.Abs(v - baseline - wave) < 0.009f)
                {
                    return biro;
                }
            }

            // The chef's tick against the first line: done.
            float tickU = u - 0.88f;
            float tickV = v - 0.68f;
            bool shortStroke = tickU > -0.05f && tickU < 0f && Mathf.Abs(tickV + tickU * 0.9f) < 0.012f;
            bool longStroke = tickU >= 0f && tickU < 0.07f && Mathf.Abs(tickV - tickU * 1.4f) < 0.012f;
            if (shortStroke || longStroke)
            {
                return new Color(0.75f, 0.1f, 0.08f);
            }

            // Thermal paper greys a little towards the bottom where hands have held it.
            return paper * (0.96f + 0.04f * v);
        }

        /// <summary>White, with alpha fading from solid in the middle to nothing at the edges of a rounded rectangle.</summary>
        private static Color GenerateSoftShadow(int x, int y, int w, int h)
        {
            float u = (x + 0.5f) / w * 2f - 1f;
            float v = (y + 0.5f) / h * 2f - 1f;
            const float inner = 0.35f;
            float dx = Mathf.Max(Mathf.Abs(u) - inner, 0f);
            float dy = Mathf.Max(Mathf.Abs(v) - inner, 0f);
            float distance = Mathf.Sqrt(dx * dx + dy * dy) / (1f - inner);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, distance);
            return new Color(1f, 1f, 1f, alpha * alpha);
        }

        /// <summary>Shader-style smoothstep: 0 below <paramref name="edge0"/>, 1 above <paramref name="edge1"/>, smooth
        /// between (Mathf.SmoothStep interpolates between its first two arguments instead).</summary>
        internal static float Smooth(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        internal static Color EncodeNormal(float dx, float dy)
        {
            var n = new Vector3(dx, dy, 1f).normalized;
            return new Color(n.x * 0.5f + 0.5f, n.y * 0.5f + 0.5f, n.z * 0.5f + 0.5f, 1f);
        }

        internal static float Hash01(int n)
        {
            n = (n << 13) ^ n;
            int m = (n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff;
            return m / 2147483647f;
        }

        /// <param name="data">Numbers rather than colours (a metallic-gloss map): imported without the sRGB curve.</param>
        internal static Texture2D EnsureTexture(string name, int width, int height, bool normalMap, TextureWrapMode wrap,
            System.Func<int, int, int, int, Color> pixel, bool data = false)
        {
            string path = $"{TextureFolder}/{name}.png";
            bool linear = normalMap || data;
            if (!File.Exists(path))
            {
                var texture = new Texture2D(width, height, TextureFormat.RGBA32, false, linear);
                var pixels = new Color[width * height];
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        pixels[y * width + x] = pixel(x, y, width, height);
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply(false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.ImportAsset(path);
            }

            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            TextureImporterType wantedType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != wantedType || importer.wrapMode != wrap || !importer.mipmapEnabled || importer.sRGBTexture == linear)
            {
                importer.textureType = wantedType;
                importer.wrapMode = wrap;
                importer.mipmapEnabled = true;
                importer.anisoLevel = 4;
                importer.alphaIsTransparency = !linear;
                importer.sRGBTexture = !linear;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        // ------------------------------------------------------------------
        // Materials and primitives
        // ------------------------------------------------------------------

        /// <param name="metallicGloss">R metallic, A gloss (times <paramref name="smoothness"/>); replaces <paramref name="metallic"/>.</param>
        internal static Material EnsureMaterial(string name, Color color, float smoothness, float metallic,
            Texture2D baseMap = null, Vector2? tiling = null, Texture2D normalMap = null, float normalScale = 1f, Color? emission = null,
            Texture2D metallicGloss = null)
        {
            Material material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", metallic);
            material.SetTexture("_BaseMap", baseMap);
            material.SetTextureScale("_BaseMap", tiling ?? Vector2.one);
            material.SetTexture("_MetallicGlossMap", metallicGloss);
            material.SetFloat("_SmoothnessTextureChannel", 0f); // gloss from the metallic map's alpha
            if (metallicGloss != null)
            {
                material.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                material.DisableKeyword("_METALLICSPECGLOSSMAP");
            }

            if (normalMap != null)
            {
                material.SetTexture("_BumpMap", normalMap);
                material.SetFloat("_BumpScale", normalScale);
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.SetTexture("_BumpMap", null);
                material.DisableKeyword("_NORMALMAP");
            }

            if (emission.HasValue)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission.Value);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private enum HandPart
        {
            Glove = 0,
            Skin = 1,
            Sleeve = 2,
            Cuff = 3 // the glove's cuff: a rigid tube, not the skinned glove
        }

        private const string HandShader = "Zmywak/Hand";

        /// <summary>A part of Kuba's arm on Zmywak/Hand (Assets/_Project/Shaders/Hand.shader).</summary>
        private static Material EnsureHandMaterial(string name, HandPart part, Color color, float smoothness, Texture2D normalMap,
            Vector2 tiling, float normalScale)
        {
            if (Shader.Find(HandShader) == null)
            {
                throw new System.InvalidOperationException($"Shader '{HandShader}' not found - is Assets/_Project/Shaders/Hand.shader in the project?");
            }

            Material material = LoadOrCreate(name, HandShader);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetTexture("_BaseMap", null);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetFloat("_HandPart", (float)part);
            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_BumpScale", normalScale);
            if (normalMap != null)
            {
                material.EnableKeyword("_NORMALMAP");
            }
            else
            {
                material.DisableKeyword("_NORMALMAP");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>Soapy water: see-through, glossy, rippled by a scrolling normal map (no custom shader needed).</summary>
        private static Material EnsureWater(Texture2D normalMap)
        {
            Material material = LoadOrCreate("SinkWater", "Universal Render Pipeline/Lit");
            // Murky, soapy dishwater rather than clear: the basin shows through dimly.
            material.SetColor("_BaseColor", new Color(0.25f, 0.32f, 0.32f, 0.68f));
            // Not quite a mirror: at 0.96 the lamp over the sink broke into glitter on every ripple, and at 0.9 its
            // glints still crowded the middle of the basin.
            material.SetFloat("_Smoothness", 0.86f);
            material.SetFloat("_Metallic", 0f);
            material.SetTexture("_BaseMap", null);
            // Broad, gentle swell: small, steep ripples caught the strip lights as white flecks all over.
            material.SetTextureScale("_BaseMap", new Vector2(1.3f, 1.3f));
            material.SetTexture("_BumpMap", normalMap);
            material.SetFloat("_BumpScale", 0.22f);
            material.SetFloat("_Surface", 1f); // transparent
            material.SetFloat("_Blend", 0f);   // alpha
            material.SetFloat("_ReceiveShadows", 1f);
            BaseShaderGUI.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        /// <summary>See-through, glossy URP/Lit (glass, a lid): the colour's alpha is how much of what is behind it shows less.</summary>
        internal static Material EnsureTransparentLit(string name, Color color, float smoothness)
        {
            Material material = LoadOrCreate(name, "Universal Render Pipeline/Lit");
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);
            material.SetTexture("_BaseMap", null);
            material.SetFloat("_Surface", 1f); // transparent
            material.SetFloat("_Blend", 0f);   // alpha
            material.SetFloat("_BlendModePreserveSpecular", 1f); // reflections stay whole over the see-through body
            BaseShaderGUI.SetMaterialKeywords(material, LitGUI.SetMaterialKeywords);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        internal static Material EnsureUnlitTransparent(string name, Texture2D texture, Color color, Vector2 tiling)
        {
            Material material = LoadOrCreate(name, "Universal Render Pipeline/Unlit");
            material.SetTexture("_BaseMap", texture);
            material.SetTextureScale("_BaseMap", tiling);
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            BaseShaderGUI.SetMaterialKeywords(material);
            BaseShaderGUI.SetupMaterialBlendMode(material);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material LoadOrCreate(string name, string shader)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader.name != shader)
            {
                material.shader = Shader.Find(shader);
            }

            return material;
        }

        private static GameObject Box(string name, Transform parent, Vector3 worldPosition, Vector3 size, Material material)
        {
            return Primitive(PrimitiveType.Cube, name, parent, worldPosition, size, material);
        }

        /// <summary>Unity cylinder: scale.y is half its height.</summary>
        private static GameObject Cylinder(string name, Transform parent, Vector3 worldPosition, Vector3 scale, Material material)
        {
            return Primitive(PrimitiveType.Cylinder, name, parent, worldPosition, scale, material);
        }

        private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Vector3 worldPosition, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, true);
            go.transform.position = worldPosition;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        internal static void SetStatic(GameObject go)
        {
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
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
