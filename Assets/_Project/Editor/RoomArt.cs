using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Zmywak.Core.Graphics;
using Zmywak.UI.Screens;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Kuba's rented room in Dublin, 2004, as the hub's backdrop - a real 3D room behind the hub's cards instead of
    /// a flat illustration: wooden floor, tired wallpaper, a sash window with rain running down it and the city at
    /// night behind (the same view as the old illustration, painted by UiArt), a narrow bed under a checked duvet,
    /// the suitcase he came with, a desk under the window with a lamp, Mum's jar, a mug and the phone. Warm lamp
    /// light against cold streetlight from the window - the mood grading (homesickness cools, belonging warms)
    /// plays over it. All generated here, sized in real metres, no colliders (nothing in it is touched).
    /// </summary>
    internal static class RoomArt
    {
        private const float Width = 3.4f;   // x: -1.7 .. 1.7
        private const float Depth = 3.6f;   // z: -1.8 .. 1.8, the window wall at the back (+z)
        private const float Height = 2.6f;
        private const float BackZ = 1.8f;
        private const float WallThickness = 0.14f;

        // The window opening in the back wall.
        private const float WindowLeft = -0.25f;
        private const float WindowRight = 1.05f;
        private const float WindowBottom = 0.98f;
        private const float WindowTop = 2.18f;

        private static Material _floor;
        private static Material _wallpaper;
        private static Material _paint;
        private static Material _glass;
        private static Material _rain;
        private static Material _snow;
        private static Material _fog;
        private static Texture2D _krakowView;
        private static Texture2D _pilbaraView;
        private static Material _view;
        private static Material _pine;
        private static Material _duvet;
        private static Material _linen;
        private static Material _curtain;
        private static Material _suitcase;
        private static Material _jacket;
        private static Material _rug;
        private static Material _jarGlass;
        private static Material _bigos;
        private static Material _lid;
        private static Material _phone;
        private static Material _phoneScreen;
        private static Material _mug;
        private static Material _lampMetal;
        private static Material _lampGlow;
        private static Material _paper;
        private static Material _calendar;
        private static Material _photo;
        private static Material _bunkMetal;
        private static Material _blanket;
        private static Material _locker;
        private static Material _rucksack;

        public static void EnsureAssets()
        {
            Texture2D planks = KitchenArt.EnsureTexture("Room_Planks_Albedo", 512, 512, false, TextureWrapMode.Repeat, PlankAlbedo);
            Texture2D planksNormal = KitchenArt.EnsureTexture("Room_Planks_Normal", 512, 512, true, TextureWrapMode.Repeat, PlankNormal);
            Texture2D wallpaper = KitchenArt.EnsureTexture("Room_Wallpaper", 256, 256, false, TextureWrapMode.Repeat, WallpaperAlbedo);
            Texture2D grain = KitchenArt.EnsureTexture("Room_WoodGrain", 256, 256, false, TextureWrapMode.Repeat, WoodGrain);
            Texture2D check = KitchenArt.EnsureTexture("Room_DuvetCheck", 256, 256, false, TextureWrapMode.Repeat, DuvetCheck);
            Texture2D weave = KitchenArt.EnsureTexture("Weave_Normal", 128, 128, true, TextureWrapMode.Repeat, KitchenArt.GenerateWeaveNormal);
            Texture2D rugPattern = KitchenArt.EnsureTexture("Room_Rug", 256, 256, false, TextureWrapMode.Repeat, RugPattern);
            Texture2D gingham = KitchenArt.EnsureTexture("Room_Gingham", 128, 128, false, TextureWrapMode.Repeat, Gingham);
            Texture2D rain = KitchenArt.EnsureTexture("Room_RainStreaks", 256, 512, false, TextureWrapMode.Repeat, RainStreaks);
            Texture2D view = KitchenArt.EnsureTexture("Room_DublinNight", 512, 512, false, TextureWrapMode.Clamp, DublinNight);
            // After the move (WindowWeather swaps them in): Kraków's and the Pilbara's night; and snow for the window.
            _krakowView = KitchenArt.EnsureTexture("Room_KrakowNight", 512, 512, false, TextureWrapMode.Clamp, KrakowNight);
            _pilbaraView = KitchenArt.EnsureTexture("Room_PilbaraNight", 512, 512, false, TextureWrapMode.Clamp, PilbaraNight);
            Texture2D snowflakes = KitchenArt.EnsureTexture("Room_Snowflakes", 256, 512, false, TextureWrapMode.Repeat, Snowflakes);
            Texture2D calendar = KitchenArt.EnsureTexture("Room_Calendar", 256, 256, false, TextureWrapMode.Clamp, CalendarPage);
            Texture2D photo = KitchenArt.EnsureTexture("Room_Photo", 128, 128, false, TextureWrapMode.Clamp, FamilyPhoto);

            _floor = KitchenArt.EnsureMaterial("Room_Floor", new Color(0.78f, 0.6f, 0.42f), 0.42f, 0f, planks, new Vector2(0.9f, 0.9f), planksNormal, 0.6f);
            _wallpaper = KitchenArt.EnsureMaterial("Room_Wallpaper", new Color(0.72f, 0.74f, 0.66f), 0.12f, 0f, wallpaper, new Vector2(2.2f, 2.2f));
            _paint = KitchenArt.EnsureMaterial("Room_WhitePaint", new Color(0.9f, 0.89f, 0.85f), 0.55f, 0f);
            _pine = KitchenArt.EnsureMaterial("Room_Pine", new Color(0.8f, 0.6f, 0.38f), 0.5f, 0f, grain, new Vector2(1.5f, 1.5f));
            _duvet = KitchenArt.EnsureMaterial("Room_Duvet", new Color(1f, 1f, 1f), 0.08f, 0f, check, new Vector2(1.6f, 1.6f), weave, 0.5f);
            _linen = KitchenArt.EnsureMaterial("Room_Linen", new Color(0.93f, 0.92f, 0.89f), 0.1f, 0f, null, new Vector2(12f, 12f), weave, 0.6f);
            _curtain = KitchenArt.EnsureMaterial("Room_Curtain", new Color(0.55f, 0.18f, 0.16f), 0.12f, 0f, null, new Vector2(10f, 10f), weave, 0.6f);
            _curtain.SetFloat("_Cull", (float)CullMode.Off); // seen from both sides
            _curtain.doubleSidedGI = true;
            _suitcase = KitchenArt.EnsureMaterial("Room_Suitcase", new Color(0.36f, 0.07f, 0.08f), 0.55f, 0f);
            // Washed-out denim: a navy jacket in that light was just a black shape.
            _jacket = KitchenArt.EnsureMaterial("Room_Jacket", new Color(0.26f, 0.35f, 0.5f), 0.12f, 0f, null, new Vector2(10f, 10f), weave, 0.5f);
            _rug = KitchenArt.EnsureMaterial("Room_Rug", new Color(1f, 1f, 1f), 0.05f, 0f, rugPattern, new Vector2(0.7f, 0.7f), weave, 0.7f);
            _bigos = KitchenArt.EnsureMaterial("Room_Bigos", new Color(0.38f, 0.22f, 0.1f), 0.45f, 0f);
            _lid = KitchenArt.EnsureMaterial("Room_JarLid", new Color(1f, 1f, 1f), 0.3f, 0f, gingham, new Vector2(3f, 1f));
            _phone = KitchenArt.EnsureMaterial("Room_Phone", new Color(0.06f, 0.06f, 0.07f), 0.85f, 0.2f);
            _phoneScreen = KitchenArt.EnsureMaterial("Room_PhoneScreen", new Color(0.05f, 0.1f, 0.14f), 0.9f, 0f,
                emission: new Color(0.25f, 0.5f, 0.6f) * 0.8f);
            _mug = KitchenArt.EnsureMaterial("Room_Mug", new Color(0.9f, 0.86f, 0.78f), 0.8f, 0f);
            _lampMetal = KitchenArt.EnsureMaterial("Room_LampMetal", new Color(0.1f, 0.24f, 0.19f), 0.62f, 0.6f);
            _lampGlow = KitchenArt.EnsureMaterial("Room_LampGlow", new Color(1f, 0.85f, 0.6f), 0.5f, 0f, emission: new Color(1f, 0.78f, 0.5f) * 4f);
            _paper = KitchenArt.EnsureMaterial("Room_Paper", new Color(0.93f, 0.91f, 0.85f), 0.15f, 0f);
            _calendar = KitchenArt.EnsureMaterial("Room_Calendar", new Color(1f, 1f, 1f), 0.15f, 0f, calendar, Vector2.one);
            _photo = KitchenArt.EnsureMaterial("Room_Photo", new Color(1f, 1f, 1f), 0.35f, 0f, photo, Vector2.one);
            // The hostel dormitory (RoomDressing): painted steel bunks and lockers, grey wool blankets, a stranger's rucksack.
            _bunkMetal = KitchenArt.EnsureMaterial("Room_BunkMetal", new Color(0.36f, 0.4f, 0.44f), 0.45f, 0.5f);
            _blanket = KitchenArt.EnsureMaterial("Room_HostelBlanket", new Color(0.4f, 0.41f, 0.44f), 0.06f, 0f, null, new Vector2(10f, 10f), weave, 0.8f);
            _locker = KitchenArt.EnsureMaterial("Room_Locker", new Color(0.3f, 0.43f, 0.5f), 0.5f, 0.35f);
            _rucksack = KitchenArt.EnsureMaterial("Room_Rucksack", new Color(0.72f, 0.28f, 0.12f), 0.25f, 0f, null, new Vector2(10f, 10f), weave, 0.6f);

            _glass = TransparentLit("Room_WindowGlass", new Color(0.75f, 0.82f, 0.9f, 0.12f), 0.97f);
            _jarGlass = TransparentLit("Room_JarGlass", new Color(0.85f, 0.9f, 0.88f, 0.22f), 0.95f);
            _rain = KitchenArt.EnsureUnlitTransparent("Room_Rain", rain, new Color(0.8f, 0.88f, 1f, 0.55f), new Vector2(1.2f, 1f));
            _snow = KitchenArt.EnsureUnlitTransparent("Room_Snow", snowflakes, new Color(1f, 1f, 1f, 0.85f), new Vector2(1.4f, 1f));
            _fog = KitchenArt.EnsureUnlitTransparent("Room_Fog", null, new Color(0.56f, 0.58f, 0.62f, 0.6f), Vector2.one);
            _view = KitchenArt.EnsureMaterial("Room_View", Color.black, 0f, 0f, null, Vector2.one, emission: Color.white);
            _view.SetTexture("_EmissionMap", view);
            _view.SetColor("_EmissionColor", new Color(1.15f, 1.15f, 1.2f)); // a touch over 1: the lit windows bloom
        }

        /// <summary>Builds the room under <paramref name="parent"/> and returns the camera looking into it.</summary>
        public static Camera Build(Transform parent)
        {
            var room = new GameObject("KubasRoom").transform;
            room.SetParent(parent, false);

            BuildShell(room);
            BuildWindow(room);
            BuildBed(room);
            BuildDesk(room);
            BuildDetails(room);
            KitchenArt.MakeStatic(room);

            // The hostel, after the room is marked static: lit by the probes, shown instead of his bed when he is in one.
            GameObject hostel = BuildHostel(room);
            hostel.SetActive(false);
            var dressing = room.gameObject.AddComponent<RoomDressing>();
            var so = new UnityEditor.SerializedObject(dressing);
            SerializedArray(so.FindProperty("home"), room.Find("HomeBed").gameObject, room.Find("HomePhoto").gameObject, room.Find("Calendar").gameObject);
            so.FindProperty("hostel").objectReferenceValue = hostel;
            so.ApplyModifiedPropertiesWithoutUndo();

            BuildLights(parent);
            return BuildCamera(parent);
        }

        private static void BuildShell(Transform room)
        {
            Box("Floor", room, new Vector3(0f, -0.02f, 0f), new Vector3(Width, 0.04f, Depth), 0f, _floor, worldUv: true);
            Box("Ceiling", room, new Vector3(0f, Height + 0.02f, 0f), new Vector3(Width, 0.04f, Depth), 0f, _paint);
            Box("WallLeft", room, new Vector3(-Width * 0.5f - WallThickness * 0.5f, Height * 0.5f, 0f), new Vector3(WallThickness, Height, Depth), 0f, _wallpaper, worldUv: true);
            Box("WallRight", room, new Vector3(Width * 0.5f + WallThickness * 0.5f, Height * 0.5f, 0f), new Vector3(WallThickness, Height, Depth), 0f, _wallpaper, worldUv: true);

            // The back wall, in four pieces around the window.
            float z = BackZ + WallThickness * 0.5f;
            WallPiece("WallBackLeft", room, -Width * 0.5f, WindowLeft, 0f, Height, z);
            WallPiece("WallBackRight", room, WindowRight, Width * 0.5f, 0f, Height, z);
            WallPiece("WallBackBelow", room, WindowLeft, WindowRight, 0f, WindowBottom, z);
            WallPiece("WallBackAbove", room, WindowLeft, WindowRight, WindowTop, Height, z);

            // Skirting boards.
            Box("SkirtingBack", room, new Vector3(0f, 0.06f, BackZ - 0.01f), new Vector3(Width, 0.12f, 0.02f), 0.004f, _paint);
            Box("SkirtingLeft", room, new Vector3(-Width * 0.5f + 0.01f, 0.06f, 0f), new Vector3(0.02f, 0.12f, Depth), 0.004f, _paint);
            Box("SkirtingRight", room, new Vector3(Width * 0.5f - 0.01f, 0.06f, 0f), new Vector3(0.02f, 0.12f, Depth), 0.004f, _paint);

            // A worn rug in the middle.
            Box("Rug", room, new Vector3(0.25f, 0.004f, 0.25f), new Vector3(1.5f, 0.008f, 1f), 0.003f, _rug);
        }

        private static void WallPiece(string name, Transform room, float xMin, float xMax, float yMin, float yMax, float z)
        {
            var size = new Vector3(xMax - xMin, yMax - yMin, WallThickness);
            Box(name, room, new Vector3((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f, z), size, 0f, _wallpaper, worldUv: true);
        }

        /// <summary>A white sash window: reveal, frame, a mullion and a transom, glass with rain running down it,
        /// and the city at night a little way behind (it shifts against the frame as the view moves).</summary>
        private static void BuildWindow(Transform room)
        {
            float cx = (WindowLeft + WindowRight) * 0.5f;
            float cy = (WindowBottom + WindowTop) * 0.5f;
            float w = WindowRight - WindowLeft;
            float h = WindowTop - WindowBottom;
            float z = BackZ + WallThickness * 0.5f;

            // Reveal: the wall's thickness around the opening.
            Box("RevealLeft", room, new Vector3(WindowLeft + 0.01f, cy, z), new Vector3(0.02f, h, WallThickness), 0f, _paint);
            Box("RevealRight", room, new Vector3(WindowRight - 0.01f, cy, z), new Vector3(0.02f, h, WallThickness), 0f, _paint);
            Box("RevealTop", room, new Vector3(cx, WindowTop - 0.01f, z), new Vector3(w, 0.02f, WallThickness), 0f, _paint);
            Box("Sill", room, new Vector3(cx, WindowBottom - 0.015f, BackZ - 0.06f), new Vector3(w + 0.16f, 0.035f, 0.26f), 0.01f, _paint);

            // Frame, mullion and transom (like the old illustration's), set back in the opening.
            float fz = BackZ + WallThickness * 0.65f;
            const float bar = 0.055f;
            Box("FrameLeft", room, new Vector3(WindowLeft + bar * 0.5f + 0.02f, cy, fz), new Vector3(bar, h, 0.06f), 0.008f, _paint);
            Box("FrameRight", room, new Vector3(WindowRight - bar * 0.5f - 0.02f, cy, fz), new Vector3(bar, h, 0.06f), 0.008f, _paint);
            Box("FrameTop", room, new Vector3(cx, WindowTop - bar * 0.5f - 0.02f, fz), new Vector3(w, bar, 0.06f), 0.008f, _paint);
            Box("FrameBottom", room, new Vector3(cx, WindowBottom + bar * 0.5f + 0.01f, fz), new Vector3(w, bar, 0.06f), 0.008f, _paint);
            Box("Mullion", room, new Vector3(cx, cy, fz), new Vector3(0.035f, h, 0.045f), 0.006f, _paint);
            Box("Transom", room, new Vector3(cx, WindowBottom + h * 0.62f, fz), new Vector3(w, 0.035f, 0.045f), 0.006f, _paint);

            // Glass, the rain running down its outside, and the city behind. None of them casts a shadow: they
            // would block the streetlight coming in through the window.
            GameObject glass = Quad("Glass", room, new Vector3(cx, cy, fz + 0.012f), new Vector2(w - 0.04f, h - 0.04f), _glass);
            GameObject rain = Quad("Rain", room, new Vector3(cx, cy, fz + 0.02f), new Vector2(w - 0.04f, h - 0.04f), _rain);
            var drops = rain.AddComponent<TextureScroller>();
            SetScroll(drops, new Vector2(0f, 0.09f));
            GameObject city = Quad("City", room, new Vector3(cx + 0.1f, cy + 0.05f, BackZ + 1.1f), new Vector2(2.6f, 2.6f), _view);
            NoShadows(glass);
            NoShadows(rain);
            NoShadows(city);

            // The other weathers (WindowWeather picks one by the date): snow drifting past outside, fog over the city.
            GameObject snow = Quad("Snow", room, new Vector3(cx, cy, fz + 0.3f), new Vector2(w - 0.04f, h - 0.04f), _snow);
            var flakes = snow.AddComponent<TextureScroller>();
            SetScroll(flakes, new Vector2(0.012f, 0.035f));
            GameObject fog = Quad("Fog", room, new Vector3(cx, cy, fz + 0.6f), new Vector2(w + 0.4f, h + 0.4f), _fog);
            NoShadows(snow);
            NoShadows(fog);
            snow.SetActive(false);
            fog.SetActive(false);

            var weather = room.gameObject.AddComponent<WindowWeather>();
            var so = new UnityEditor.SerializedObject(weather);
            so.FindProperty("rain").objectReferenceValue = rain.GetComponent<Renderer>();
            so.FindProperty("snow").objectReferenceValue = snow.GetComponent<Renderer>();
            so.FindProperty("fog").objectReferenceValue = fog.GetComponent<Renderer>();
            so.FindProperty("city").objectReferenceValue = city.GetComponent<Renderer>();
            so.FindProperty("krakowView").objectReferenceValue = _krakowView;
            so.FindProperty("pilbaraView").objectReferenceValue = _pilbaraView;
            so.ApplyModifiedPropertiesWithoutUndo();

            // A curtain rail and a curtain pulled to the left.
            Cylinder("CurtainRail", room, new Vector3(cx, WindowTop + 0.12f, BackZ - 0.08f), new Vector3(0.022f, w * 0.5f + 0.3f, 0.022f), Quaternion.Euler(0f, 0f, 90f), _lampMetal);
            Mesh curtainMesh = ArtMeshes.Curtain("RoomCurtain", 0.55f, WindowTop + 0.1f - 0.05f, 5, 0.035f);
            KitchenArt.Visual("Curtain", room, new Vector3(WindowLeft - 0.12f, WindowTop + 0.1f, BackZ - 0.1f), curtainMesh, _curtain);
            GameObject curtainRight = KitchenArt.Visual("CurtainRight", room, new Vector3(WindowRight + 0.14f, WindowTop + 0.1f, BackZ - 0.1f), curtainMesh, _curtain);
            curtainRight.transform.localScale = new Vector3(0.7f, 1f, 1f);
        }

        /// <summary>A narrow pine bed along the left wall, a mattress, a checked duvet thrown over it and a pillow.</summary>
        private static void BuildBed(Transform room)
        {
            // His own bed: hidden in the hostel (RoomDressing), so it is a group of its own - static like the rest.
            var bed = new GameObject("HomeBed").transform;
            bed.SetParent(room, false);
            const float x = BedX;
            const float bedWidth = 0.92f;
            const float zFoot = BedFootZ;
            const float zHead = BackZ - 0.04f;
            float length = zHead - zFoot;
            float zMid = (zFoot + zHead) * 0.5f;

            Box("BedFrame", bed, new Vector3(x, 0.2f, zMid), new Vector3(bedWidth + 0.06f, 0.2f, length), 0.02f, _pine);
            for (int corner = 0; corner < 4; corner++)
            {
                float lx = x + (corner % 2 == 0 ? -1f : 1f) * (bedWidth * 0.5f);
                float lz = corner < 2 ? zFoot + 0.04f : zHead - 0.04f;
                Box("BedLeg", bed, new Vector3(lx, 0.05f, lz), new Vector3(0.06f, 0.1f, 0.06f), 0.01f, _pine);
            }

            Box("Headboard", bed, new Vector3(x, 0.55f, zHead - 0.02f), new Vector3(bedWidth + 0.08f, 0.9f, 0.04f), 0.015f, _pine);
            Box("Mattress", bed, new Vector3(x, 0.39f, zMid - 0.01f), new Vector3(bedWidth, 0.18f, length - 0.06f), 0.05f, _linen);

            GameObject duvet = KitchenArt.Visual("Duvet", bed, new Vector3(x, 0.5f, zMid - 0.18f),
                ArtMeshes.Drape("RoomDuvet", bedWidth - 0.04f, length - 0.45f, 0.22f, 1f, 17), _duvet);
            duvet.transform.rotation = Quaternion.Euler(0f, 1.5f, 0f);
            GameObject pillow = Box("Pillow", bed, new Vector3(x + 0.04f, 0.54f, zHead - 0.3f), new Vector3(0.62f, 0.12f, 0.38f), 0.055f, _linen);
            pillow.transform.rotation = Quaternion.Euler(4f, -6f, 2f);
            KitchenArt.MakeStatic(bed);
        }

        private const float BedX = -1.23f;
        private const float BedFootZ = -0.35f;

        /// <summary>
        /// The hostel dormitory Kuba sleeps in after the eviction (RoomDressing shows it instead of his bed): a two-tier
        /// steel bunk where his bed was, the top one taken - a lump under the blanket and a stranger's rucksack - a
        /// ladder at the foot, a locker against the back wall with a padlock, and the house rules pinned above it. Not
        /// static: built after the room is marked, so the lightmaps never count on it and the light probes light it.
        /// </summary>
        private static GameObject BuildHostel(Transform room)
        {
            var dorm = new GameObject("HostelDorm").transform;
            dorm.SetParent(room, false);
            const float x = BedX;
            const float width = 0.9f;
            const float zFoot = BedFootZ;
            const float zHead = BackZ - 0.06f;
            const float postTop = 1.78f;
            float length = zHead - zFoot;
            float zMid = (zFoot + zHead) * 0.5f;

            // Four square steel posts, and at each tier a frame with a mattress on it.
            for (int corner = 0; corner < 4; corner++)
            {
                float px = x + (corner % 2 == 0 ? -1f : 1f) * (width * 0.5f);
                float pz = corner < 2 ? zFoot : zHead;
                Box("BunkPost", dorm, new Vector3(px, postTop * 0.5f, pz), new Vector3(0.045f, postTop, 0.045f), 0.008f, _bunkMetal);
            }

            foreach ((string tier, float y) in new[] { ("Lower", 0.3f), ("Upper", 1.28f) })
            {
                Box($"Bunk{tier}RailLeft", dorm, new Vector3(x - width * 0.5f, y, zMid), new Vector3(0.04f, 0.06f, length), 0.006f, _bunkMetal);
                Box($"Bunk{tier}RailRight", dorm, new Vector3(x + width * 0.5f, y, zMid), new Vector3(0.04f, 0.06f, length), 0.006f, _bunkMetal);
                Box($"Bunk{tier}Mattress", dorm, new Vector3(x, y + 0.1f, zMid), new Vector3(width - 0.06f, 0.14f, length - 0.06f), 0.045f, _linen);
            }

            Box("BunkUpperGuard", dorm, new Vector3(x + width * 0.5f, 1.52f, zMid + 0.25f), new Vector3(0.035f, 0.035f, length - 0.6f), 0.006f, _bunkMetal);

            // Kuba's bunk, made; the top one slept in by somebody who came in late and is not up yet.
            GameObject lower = KitchenArt.Visual("BunkLowerBlanket", dorm, new Vector3(x, 0.47f, zMid - 0.2f),
                ArtMeshes.Drape("HostelBlanketLower", width - 0.1f, length - 0.5f, 0.12f, 0.6f, 23), _blanket);
            lower.transform.rotation = Quaternion.Euler(0f, -1f, 0f);
            Box("BunkLowerPillow", dorm, new Vector3(x, 0.5f, zHead - 0.28f), new Vector3(0.56f, 0.1f, 0.34f), 0.05f, _linen);
            GameObject upper = KitchenArt.Visual("BunkUpperBlanket", dorm, new Vector3(x, 1.47f, zMid - 0.05f),
                ArtMeshes.Drape("HostelBlanketUpper", width - 0.1f, length - 0.3f, 0.12f, 1.6f, 41), _blanket);
            upper.transform.rotation = Quaternion.Euler(0f, 3f, 0f);
            GameObject rucksack = Box("Rucksack", dorm, new Vector3(x + 0.18f, 1.6f, zFoot + 0.3f), new Vector3(0.34f, 0.46f, 0.22f), 0.08f, _rucksack);
            rucksack.transform.rotation = Quaternion.Euler(-12f, 25f, 8f);

            // The ladder up, at the foot on the room's side.
            float lx = x + width * 0.5f + 0.03f;
            Box("LadderRailFront", dorm, new Vector3(lx, 0.8f, zFoot + 0.02f), new Vector3(0.03f, 1.6f, 0.03f), 0.006f, _bunkMetal);
            Box("LadderRailBack", dorm, new Vector3(lx, 0.8f, zFoot + 0.34f), new Vector3(0.03f, 1.6f, 0.03f), 0.006f, _bunkMetal);
            for (int rung = 1; rung <= 4; rung++)
            {
                Box("LadderRung", dorm, new Vector3(lx, rung * 0.3f, zFoot + 0.18f), new Vector3(0.03f, 0.025f, 0.32f), 0.006f, _bunkMetal);
            }

            // A locker against the back wall, where his calendar hung: vents, a number, a padlock.
            var lockerAt = new Vector3(-0.52f, 0.9f, BackZ - 0.25f);
            Box("Locker", dorm, lockerAt, new Vector3(0.42f, 1.8f, 0.46f), 0.012f, _locker);
            for (int vent = 0; vent < 4; vent++)
            {
                Box("LockerVent", dorm, lockerAt + new Vector3(0f, 0.7f - vent * 0.05f, -0.232f), new Vector3(0.22f, 0.015f, 0.006f), 0.002f, _phone);
            }

            Quad("LockerNumber", dorm, lockerAt + new Vector3(0f, 0.5f, -0.235f), new Vector2(0.1f, 0.06f), _paper);
            Box("Padlock", dorm, lockerAt + new Vector3(0.15f, 0.05f, -0.245f), new Vector3(0.04f, 0.05f, 0.02f), 0.006f, _lampMetal);

            // The house rules, pinned up over the locker.
            GameObject rules = Quad("HouseRules", dorm, new Vector3(-0.52f, 2.08f, BackZ - 0.005f), new Vector2(0.4f, 0.3f), _paper);
            rules.transform.rotation = Quaternion.Euler(0f, 0f, 1.5f);
            var text = new GameObject("HouseRulesText", typeof(RectTransform)).AddComponent<TMPro.TextMeshPro>();
            text.transform.SetParent(dorm, false);
            text.transform.SetPositionAndRotation(new Vector3(-0.52f, 2.08f, BackZ - 0.012f), Quaternion.Euler(0f, 0f, 1.5f));
            text.rectTransform.sizeDelta = new Vector2(0.36f, 0.26f);
            text.fontSize = 0.32f;
            text.alignment = TMPro.TextAlignmentOptions.Center;
            text.color = new Color(0.15f, 0.12f, 0.1f);
            text.text = "<b>HOUSE RULES</b>\nNo food in the dorms\nQuiet after 11 pm\nCheck-out 10 am";
            return dorm.gameObject;
        }

        /// <summary>A pine desk under the window with a lamp, Mum's jar, a mug, the phone and a few letters.</summary>
        private static void BuildDesk(Transform room)
        {
            const float top = 0.74f;
            float cx = (WindowLeft + WindowRight) * 0.5f;
            const float cz = BackZ - 0.33f;
            Box("DeskTop", room, new Vector3(cx, top - 0.015f, cz), new Vector3(1.25f, 0.03f, 0.58f), 0.006f, _pine);
            for (int corner = 0; corner < 4; corner++)
            {
                float lx = cx + (corner % 2 == 0 ? -0.58f : 0.58f);
                float lz = cz + (corner < 2 ? -0.25f : 0.25f);
                Box("DeskLeg", room, new Vector3(lx, (top - 0.03f) * 0.5f, lz), new Vector3(0.045f, top - 0.03f, 0.045f), 0.008f, _pine);
            }

            Box("Apron", room, new Vector3(cx, top - 0.08f, cz - 0.27f), new Vector3(1.15f, 0.09f, 0.02f), 0.004f, _pine);

            // The lamp, at the right end.
            Vector3 lampBase = new Vector3(cx + 0.46f, top, cz + 0.12f);
            Cylinder("LampBase", room, lampBase + new Vector3(0f, 0.012f, 0f), new Vector3(0.14f, 0.012f, 0.14f), Quaternion.identity, _lampMetal);
            Cylinder("LampStem", room, lampBase + new Vector3(0f, 0.2f, 0f), new Vector3(0.018f, 0.19f, 0.018f), Quaternion.identity, _lampMetal);
            Cylinder("LampArm", room, lampBase + new Vector3(-0.1f, 0.4f, -0.05f), new Vector3(0.016f, 0.13f, 0.016f), Quaternion.Euler(20f, 0f, 60f), _lampMetal);
            Vector3 shade = lampBase + new Vector3(-0.2f, 0.43f, -0.1f);
            GameObject shadeGo = KitchenArt.Visual("LampShade", room, shade, ArtMeshes.Frustum("LampShade", 0.11f, 0.045f, 0.12f), _lampMetal);
            shadeGo.transform.rotation = Quaternion.Euler(12f, 0f, -18f);
            GameObject bulb = Sphere("LampBulb", room, shade + new Vector3(0f, 0.03f, 0f), 0.045f, _lampGlow);
            bulb.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;

            // Everything on the desk has its own spot (desk centre cx, cz; left to right: mug, letters, jar
            // behind them, phone, the lamp at the right end) - nothing sits inside anything else.
            // Mum's jar of bigos: glass, the stew inside, a gingham lid.
            Vector3 jar = new Vector3(cx - 0.12f, top, cz + 0.16f);
            Cylinder("JarBigos", room, jar + new Vector3(0f, 0.05f, 0f), new Vector3(0.08f, 0.05f, 0.08f), Quaternion.identity, _bigos);
            GameObject jarGlass = Cylinder("JarGlass", room, jar + new Vector3(0f, 0.068f, 0f), new Vector3(0.092f, 0.068f, 0.092f), Quaternion.identity, _jarGlass);
            NoShadows(jarGlass);
            Cylinder("JarLid", room, jar + new Vector3(0f, 0.142f, 0f), new Vector3(0.1f, 0.012f, 0.1f), Quaternion.identity, _lid);

            // A mug, the phone, and the letters from the office.
            Vector3 mug = new Vector3(cx - 0.5f, top, cz - 0.02f);
            Cylinder("Mug", room, mug + new Vector3(0f, 0.05f, 0f), new Vector3(0.085f, 0.05f, 0.085f), Quaternion.identity, _mug);
            Box("MugHandle", room, mug + new Vector3(0.055f, 0.05f, 0f), new Vector3(0.018f, 0.06f, 0.012f), 0.006f, _mug);
            BuildMugSteam(room, mug + new Vector3(0f, 0.1f, 0f));
            GameObject phone = Box("Phone", room, new Vector3(cx + 0.2f, top + 0.006f, cz - 0.12f), new Vector3(0.05f, 0.012f, 0.105f), 0.006f, _phone);
            phone.transform.rotation = Quaternion.Euler(0f, 23f, 0f);
            GameObject screen = Quad("PhoneScreen", phone.transform, phone.transform.position + new Vector3(0f, 0.0065f, 0f), new Vector2(0.04f, 0.05f), _phoneScreen);
            screen.transform.rotation = phone.transform.rotation * Quaternion.Euler(90f, 0f, 0f);
            NoShadows(screen);
            for (int i = 0; i < 3; i++)
            {
                GameObject letter = Box("Letter", room, new Vector3(cx - 0.22f + i * 0.02f, top + 0.002f + i * 0.0015f, cz - 0.1f + i * 0.01f),
                    new Vector3(0.21f, 0.002f, 0.297f), 0f, _paper);
                letter.transform.rotation = Quaternion.Euler(0f, -8f + i * 11f, 0f);
            }
        }

        /// <summary>Tea still hot in the mug: a few faint wisps curling up and fading (ten soft particles at most).</summary>
        private static void BuildMugSteam(Transform room, Vector3 top)
        {
            var go = new GameObject("MugSteam", typeof(ParticleSystem));
            go.transform.SetParent(room, true);
            go.transform.position = top;
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // emit upwards

            var ps = go.GetComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true;
            main.prewarm = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.03f, 0.06f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.09f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(1f, 0.97f, 0.92f, 0.24f);
            main.maxParticles = 10;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = 3.5f;

            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.03f;

            ParticleSystem.ColorOverLifetimeModule colour = ps.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.25f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;

            ParticleSystem.SizeOverLifetimeModule grow = ps.sizeOverLifetime;
            grow.enabled = true;
            grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.7f, 1f, 2.2f));

            ParticleSystem.NoiseModule noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.02f;
            noise.frequency = 1.5f;
            noise.scrollSpeed = 0.3f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = FxArt.Soft;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        /// <summary>
        /// A pine chair pulled out from the desk and turned a little, as if he has just got up, with his jacket
        /// thrown over its back - the foreground the room lacked, and a silhouette against the lamp.
        /// </summary>
        private static void BuildChair(Transform room)
        {
            var chair = new GameObject("Chair").transform;
            chair.SetParent(room, false);
            // Half tucked under the desk, off to the side of the lamp: any further out and it hid the desk behind the
            // menu's title and button.
            var o = new Vector3((WindowLeft + WindowRight) * 0.5f - 0.3f, 0f, BackZ - 0.72f);
            chair.position = o;

            const float seat = 0.45f;
            const float half = 0.2f;
            Box("ChairSeat", chair, o + new Vector3(0f, seat - 0.02f, 0f), new Vector3(0.42f, 0.04f, 0.42f), 0.01f, _pine);
            for (int corner = 0; corner < 4; corner++)
            {
                float lx = corner % 2 == 0 ? -half + 0.02f : half - 0.02f;
                float lz = corner < 2 ? -half + 0.02f : half - 0.02f;
                // The back legs run on up as the posts of the backrest (the back is on -z, towards the camera).
                float top = corner < 2 ? 0.88f : seat - 0.04f;
                Box("ChairLeg", chair, o + new Vector3(lx, top * 0.5f, lz), new Vector3(0.035f, top, 0.035f), 0.006f, _pine);
            }

            Box("ChairTopRail", chair, o + new Vector3(0f, 0.84f, -half + 0.02f), new Vector3(0.42f, 0.075f, 0.028f), 0.01f, _pine);
            Box("ChairSlat", chair, o + new Vector3(0f, 0.67f, -half + 0.02f), new Vector3(0.38f, 0.04f, 0.022f), 0.008f, _pine);

            GameObject jacket = KitchenArt.Visual("Jacket", chair, o + new Vector3(0.02f, 0.885f, -half + 0.02f),
                ArtMeshes.Drape("RoomJacket", 0.4f, 0.04f, 0.3f, 0.7f, 5), _jacket);
            jacket.transform.rotation = Quaternion.Euler(0f, 4f, 0f);

            chair.rotation = Quaternion.Euler(0f, 16f, 0f);
            KitchenArt.MakeStatic(chair);
        }

        /// <summary>The suitcase he came with, a calendar and a photo from home.</summary>
        private static void BuildDetails(Transform room)
        {
            BuildChair(room);
            GameObject suitcase = Box("Suitcase", room, new Vector3(-0.42f, 0.34f, 0.35f), new Vector3(0.46f, 0.64f, 0.22f), 0.035f, _suitcase);
            suitcase.transform.rotation = Quaternion.Euler(0f, 28f, 0f);
            GameObject handle = Box("SuitcaseHandle", suitcase.transform, suitcase.transform.position + new Vector3(0f, 0.34f, 0f), new Vector3(0.14f, 0.025f, 0.03f), 0.012f, _phone);
            handle.transform.rotation = suitcase.transform.rotation;
            GameObject tag = Box("LuggageTag", suitcase.transform, suitcase.transform.position + new Vector3(0.06f, 0.26f, -0.12f), new Vector3(0.06f, 0.09f, 0.003f), 0f, _paper);
            tag.transform.rotation = suitcase.transform.rotation * Quaternion.Euler(0f, 0f, 12f);

            GameObject calendar = Quad("Calendar", room, new Vector3(-0.75f, 1.55f, BackZ - 0.005f), new Vector2(0.34f, 0.46f), _calendar);
            calendar.transform.rotation = Quaternion.Euler(0f, 0f, -1.5f);

            // A photo from home above the bed, in a pine frame (its own group: the upper bunk of a hostel would cut
            // through it, so RoomDressing hides it there).
            var photoGroup = new GameObject("HomePhoto").transform;
            photoGroup.SetParent(room, false);
            var frameCentre = new Vector3(-Width * 0.5f + 0.012f, 1.45f, 0.9f);
            Box("PhotoFrame", photoGroup, frameCentre, new Vector3(0.02f, 0.26f, 0.34f), 0.004f, _pine);
            GameObject photo = Quad("Photo", photoGroup, frameCentre + new Vector3(0.012f, 0f, 0f), new Vector2(0.28f, 0.2f), _photo);
            photo.transform.rotation = Quaternion.Euler(0f, -90f, 0f);
            KitchenArt.MakeStatic(photoGroup);
        }

        /// <summary>The desk lamp's warm pool of light against the streetlight's cold wash through the window.</summary>
        private static void BuildLights(Transform parent)
        {
            float cx = (WindowLeft + WindowRight) * 0.5f;
            var lamp = new GameObject("DeskLamp", typeof(Light));
            lamp.transform.SetParent(parent, false);
            lamp.transform.position = new Vector3(cx + 0.26f, 1.12f, BackZ - 0.43f);
            lamp.transform.rotation = Quaternion.Euler(80f, 0f, -18f);
            Light spot = lamp.GetComponent<Light>();
            spot.type = LightType.Spot;
            spot.color = new Color(1f, 0.76f, 0.48f);
            spot.intensity = 2.8f; // at 5 the desk top and the wall under the window burnt out to white
            spot.range = 4f;
            spot.spotAngle = 118f;
            spot.innerSpotAngle = 60f;
            spot.shadows = LightShadows.Soft;
            spot.shadowStrength = 0.85f;

            var glow = new GameObject("LampGlow", typeof(Light));
            glow.transform.SetParent(parent, false);
            glow.transform.position = new Vector3(cx + 0.26f, 1.2f, BackZ - 0.45f);
            Light fill = glow.GetComponent<Light>();
            fill.type = LightType.Point;
            fill.color = new Color(1f, 0.72f, 0.45f);
            fill.intensity = 0.9f;
            fill.range = 4.5f;
            fill.shadows = LightShadows.None;

            var street = new GameObject("Streetlight", typeof(Light));
            street.transform.SetParent(parent, false);
            street.transform.rotation = Quaternion.Euler(32f, 196f, 0f); // in through the window, down across the bed
            Light moon = street.GetComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = new Color(0.55f, 0.66f, 0.95f);
            moon.intensity = 0.45f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.9f;

            // A car in the street below now and then: its headlights come up through the window and slide across
            // the ceiling. No shadows, and switched off between cars.
            var car = new GameObject("PassingCar", typeof(Light));
            car.transform.SetParent(parent, false);
            Light headlights = car.GetComponent<Light>();
            headlights.type = LightType.Spot;
            headlights.color = new Color(1f, 0.93f, 0.8f);
            headlights.range = 6f;
            headlights.spotAngle = 55f;
            headlights.innerSpotAngle = 20f;
            headlights.shadows = LightShadows.None;
            var sweep = new UnityEditor.SerializedObject(car.AddComponent<LightSweep>());
            sweep.FindProperty("from").vector3Value = new Vector3(WindowLeft - 1f, WindowBottom - 0.15f, BackZ + 0.7f);
            sweep.FindProperty("to").vector3Value = new Vector3(WindowRight + 1f, WindowBottom - 0.15f, BackZ + 0.7f);
            sweep.FindProperty("aimAt").vector3Value = new Vector3(WindowLeft - 0.6f, Height, 0.2f);
            sweep.FindProperty("peakIntensity").floatValue = 9f; // ~3 m from the ceiling it lights: dimmer did not show
            sweep.ApplyModifiedPropertiesWithoutUndo();

            var ambience = new GameObject("RoomLighting", typeof(SceneLighting));
            ambience.transform.SetParent(parent, false);
            var so = new UnityEditor.SerializedObject(ambience.GetComponent<SceneLighting>());
            so.FindProperty("skyColor").colorValue = new Color(0.2f, 0.23f, 0.33f);
            so.FindProperty("equatorColor").colorValue = new Color(0.2f, 0.17f, 0.15f);
            so.FindProperty("groundColor").colorValue = new Color(0.1f, 0.08f, 0.07f);
            so.FindProperty("reflectionIntensity").floatValue = 0.6f;
            so.FindProperty("panelIntensity").floatValue = 0f;
            so.FindProperty("stripIntensity").floatValue = 0f;
            so.FindProperty("windowIntensity").floatValue = 0.9f;
            so.FindProperty("windowColor").colorValue = new Color(0.45f, 0.55f, 0.85f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Camera BuildCamera(Transform parent)
        {
            var go = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            go.tag = "MainCamera";
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(1.28f, 1.52f, -1.62f);
            go.transform.LookAt(new Vector3(-0.12f, 0.98f, 1.15f));
            Camera camera = go.GetComponent<Camera>();
            camera.orthographic = false;
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 12f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.03f, 0.04f);
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            return camera;
        }

        // ------------------------------------------------------------------
        // Pieces
        // ------------------------------------------------------------------

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, float radius, Material material, bool worldUv = false)
        {
            string meshName = worldUv
                ? $"Room_{name}"
                : $"Box_{size.x:0.####}x{size.y:0.####}x{size.z:0.####}_r{radius:0.####}".Replace(',', '.');
            Mesh mesh = ArtMeshes.RoundedBox(meshName, size, radius, radius > 0.02f ? 4 : 2, uvOrigin: worldUv ? position : default);
            return KitchenArt.Visual(name, parent, position, mesh, material);
        }

        /// <summary>A vertical quad facing -Z (towards the room's camera), or turned by its caller.</summary>
        private static GameObject Quad(string name, Transform parent, Vector3 position, Vector2 size, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 position, Vector3 scale, Quaternion rotation, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.SetPositionAndRotation(position, rotation);
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static GameObject Sphere(string name, Transform parent, Vector3 position, float diameter, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, true);
            go.transform.position = position;
            go.transform.localScale = Vector3.one * diameter;
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go;
        }

        private static void SerializedArray(UnityEditor.SerializedProperty array, params Object[] items)
        {
            array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static void NoShadows(GameObject go)
        {
            var renderer = go.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static void SetScroll(TextureScroller scroller, Vector2 speed)
        {
            var so = new UnityEditor.SerializedObject(scroller);
            so.FindProperty("speed").vector2Value = speed;
            so.FindProperty("swayAmount").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Material TransparentLit(string name, Color color, float smoothness)
        {
            Material material = KitchenArt.EnsureMaterial(name, color, smoothness, 0f);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            UnityEditor.BaseShaderGUI.SetMaterialKeywords(material, UnityEditor.Rendering.Universal.ShaderGUI.LitGUI.SetMaterialKeywords);
            UnityEditor.BaseShaderGUI.SetupMaterialBlendMode(material);
            UnityEditor.EditorUtility.SetDirty(material);
            return material;
        }

        // ------------------------------------------------------------------
        // Textures
        // ------------------------------------------------------------------

        /// <summary>Oak boards 1/8 of the texture wide, each its own length, shade and grain, with dark joints.</summary>
        private static float PlankHeight(int x, int y, int w, int h, out float shade, out float grainLine)
        {
            const int boards = 8;
            int boardW = w / boards;
            int board = x / boardW;
            float offset = KitchenArt.Hash01(board * 7919) * h;
            int length = h / 2;
            float along = (y + offset) % h;
            int piece = Mathf.FloorToInt((y + offset) / length);
            shade = 0.82f + KitchenArt.Hash01(board * 104729 + piece * 31) * 0.3f;
            float u = x % boardW / (float)boardW;
            grainLine = Mathf.PerlinNoise(u * 3f + board * 5.3f, along / h * 40f + piece * 2.1f);
            bool joint = x % boardW < 2 || (along % length) < 2;
            return joint ? 0f : 1f;
        }

        private static Color PlankAlbedo(int x, int y, int w, int h)
        {
            float height = PlankHeight(x, y, w, h, out float shade, out float grain);
            if (height <= 0f)
            {
                return new Color(0.22f, 0.15f, 0.1f);
            }

            float v = shade * (0.85f + grain * 0.22f);
            return new Color(v, v * 0.93f, v * 0.85f);
        }

        private static Color PlankNormal(int x, int y, int w, int h)
        {
            float left = PlankHeight((x - 1 + w) % w, y, w, h, out _, out float gl);
            float right = PlankHeight((x + 1) % w, y, w, h, out _, out float gr);
            float down = PlankHeight(x, (y - 1 + h) % h, w, h, out _, out float gd);
            float up = PlankHeight(x, (y + 1) % h, w, h, out _, out float gu);
            return KitchenArt.EncodeNormal((left - right) * 0.8f + (gl - gr) * 0.4f, (down - up) * 0.8f + (gd - gu) * 0.4f);
        }

        /// <summary>Faded 1970s wallpaper: soft stripes with a small repeating sprig between.</summary>
        private static Color WallpaperAlbedo(int x, int y, int w, int h)
        {
            float stripe = Mathf.Sin(x / (float)w * Mathf.PI * 8f) * 0.5f + 0.5f;
            float sprigX = (x % 32) - 16f;
            float sprigY = (y % 48) - 24f + ((x / 32) % 2) * 12f;
            float sprig = Mathf.Clamp01(1f - Mathf.Sqrt(sprigX * sprigX + sprigY * sprigY * 0.4f) / 6f);
            float stain = KitchenArt.TileableNoise(x, y, w, h, 3f, 41f);
            float v = 0.9f + stripe * 0.06f - sprig * 0.18f - (stain - 0.5f) * 0.08f;
            return new Color(v * 0.97f, v, v * 0.9f);
        }

        private static Color WoodGrain(int x, int y, int w, int h)
        {
            float u = x / (float)w;
            float v = y / (float)h;
            float rings = Mathf.Sin((v * 30f + Mathf.PerlinNoise(u * 4f, v * 2f) * 6f) * Mathf.PI) * 0.5f + 0.5f;
            float g = 0.82f + rings * 0.14f + (Mathf.PerlinNoise(u * 60f, v * 3f) - 0.5f) * 0.08f;
            return new Color(g, g * 0.92f, g * 0.82f);
        }

        /// <summary>A navy and cream checked duvet cover.</summary>
        private static Color DuvetCheck(int x, int y, int w, int h)
        {
            bool bandX = (x / 32) % 2 == 0;
            bool bandY = (y / 32) % 2 == 0;
            var cream = new Color(0.9f, 0.87f, 0.78f);
            var navy = new Color(0.16f, 0.22f, 0.4f);
            Color c = bandX && bandY ? navy : bandX || bandY ? Color.Lerp(cream, navy, 0.45f) : cream;
            float thread = KitchenArt.Hash01(x * 73856093 ^ y * 19349663) * 0.06f;
            return c * (0.97f - thread);
        }

        /// <summary>A worn rug: a border, stripes, a faded middle.</summary>
        private static Color RugPattern(int x, int y, int w, int h)
        {
            float u = x / (float)w;
            float v = y / (float)h;
            float edge = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            var red = new Color(0.55f, 0.15f, 0.12f);
            var cream = new Color(0.82f, 0.74f, 0.6f);
            var blue = new Color(0.18f, 0.25f, 0.38f);
            Color c = edge < 0.06f ? blue : edge < 0.1f ? cream : (Mathf.FloorToInt(v * 14f) % 2 == 0 ? red : Color.Lerp(red, cream, 0.35f));
            float wear = KitchenArt.TileableNoise(x, y, w, h, 2f, 7f);
            return Color.Lerp(c, cream, Mathf.Clamp01((wear - 0.55f) * 1.5f));
        }

        private static Color Gingham(int x, int y, int w, int h)
        {
            bool a = (x / 16) % 2 == 0;
            bool b = (y / 16) % 2 == 0;
            var red = new Color(0.78f, 0.12f, 0.12f);
            return a && b ? red : a || b ? Color.Lerp(red, Color.white, 0.55f) : Color.white;
        }

        /// <summary>Rain running down glass: thin streaks with drops at their heads (alpha).</summary>
        private static Color RainStreaks(int x, int y, int w, int h)
        {
            float best = 0f;
            for (int s = 0; s < 3; s++)
            {
                int column = (x + s * 37) / 9;
                float cx = column * 9 - s * 37 + 4 + KitchenArt.Hash01(column * 131 + s) * 3f;
                float start = KitchenArt.Hash01(column * 7919 + s * 17) * h;
                float length = 40f + KitchenArt.Hash01(column * 104729 + s) * 120f;
                float along = (y - start + h) % h;
                if (along > length)
                {
                    continue;
                }

                float dx = Mathf.Abs(x - cx - Mathf.Sin(along * 0.05f) * 1.5f);
                float line = Mathf.Clamp01(1f - dx / 1.2f) * (0.35f + 0.65f * along / length);
                float drop = along > length - 6f ? Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + (along - length + 3f) * (along - length + 3f)) / 3f) : 0f;
                best = Mathf.Max(best, Mathf.Max(line * 0.6f, drop));
            }

            return new Color(1f, 1f, 1f, best);
        }

        private static Color DublinNight(int x, int y, int w, int h)
        {
            if (_dublinView == null || _dublinView.Length != w * h)
            {
                _dublinView = UiArt.PaintDublinView(w, new System.Random(2004));
            }

            return _dublinView[y * w + x];
        }

        private static Color[] _dublinView;
        private static Color[] _krakowNight;
        private static Color[] _pilbaraNight;

        private static Color KrakowNight(int x, int y, int w, int h)
        {
            if (_krakowNight == null || _krakowNight.Length != w * h)
            {
                _krakowNight = UiArt.PaintKrakowView(w, new System.Random(2010));
            }

            return _krakowNight[y * w + x];
        }

        private static Color PilbaraNight(int x, int y, int w, int h)
        {
            if (_pilbaraNight == null || _pilbaraNight.Length != w * h)
            {
                _pilbaraNight = UiArt.PaintPilbaraView(w, new System.Random(2011));
            }

            return _pilbaraNight[y * w + x];
        }

        /// <summary>Snowflakes for the window: soft white dots of a few sizes, scattered, sparse.</summary>
        private static Color Snowflakes(int x, int y, int w, int h)
        {
            float best = 0f;
            const int cell = 16;
            int cx = x / cell;
            int cy = y / cell;
            for (int oy = -1; oy <= 1; oy++)
            {
                for (int ox = -1; ox <= 1; ox++)
                {
                    int gx = (cx + ox + w / cell) % (w / cell);
                    int gy = (cy + oy + h / cell) % (h / cell);
                    int seed = gx * 7919 + gy * 104729;
                    if (KitchenArt.Hash01(seed) > 0.55f)
                    {
                        continue;
                    }

                    float fx = (cx + ox) * cell + KitchenArt.Hash01(seed + 1) * cell;
                    float fy = (cy + oy) * cell + KitchenArt.Hash01(seed + 2) * cell;
                    float radius = 1.2f + KitchenArt.Hash01(seed + 3) * 2.2f;
                    float d = Mathf.Sqrt((x - fx) * (x - fx) + (y - fy) * (y - fy));
                    best = Mathf.Max(best, Mathf.Clamp01(1f - d / radius));
                }
            }

            return new Color(1f, 1f, 1f, best);
        }

        /// <summary>A wall calendar for May 2004: a picture on top, the month's grid below, the 1st circled.</summary>
        private static Color CalendarPage(int x, int y, int w, int h)
        {
            var paper = new Color(0.95f, 0.94f, 0.9f);
            float u = x / (float)w;
            float v = y / (float)h;
            if (v > 0.6f)
            {
                // A picture of the Tatras.
                float ridge = 0.72f + Mathf.PerlinNoise(u * 6f, 1f) * 0.18f;
                return v > ridge ? Color.Lerp(new Color(0.5f, 0.7f, 0.9f), new Color(0.8f, 0.88f, 0.95f), v) : new Color(0.25f, 0.35f, 0.3f) * (0.8f + v * 0.3f);
            }

            if (v > 0.52f)
            {
                return new Color(0.7f, 0.15f, 0.12f); // the month's red band
            }

            int col = Mathf.FloorToInt(u * 7f);
            int row = Mathf.FloorToInt((0.52f - v) / 0.52f * 6f);
            float cu = u * 7f - col;
            float cv = (0.52f - v) / 0.52f * 6f - row;
            bool line = cu < 0.04f || cv < 0.05f;
            bool numeral = cu > 0.25f && cu < 0.6f && cv > 0.25f && cv < 0.7f && (row * 7 + col) % 3 != 1;
            bool firstOfMay = row == 0 && col == 5 && Mathf.Abs(Mathf.Sqrt((cu - 0.45f) * (cu - 0.45f) + (cv - 0.48f) * (cv - 0.48f)) - 0.32f) < 0.05f;
            if (firstOfMay)
            {
                return new Color(0.8f, 0.1f, 0.1f);
            }

            return line ? new Color(0.7f, 0.68f, 0.64f) : numeral ? new Color(0.3f, 0.3f, 0.32f) : paper;
        }

        /// <summary>A family snapshot, soft and warm: figures against a garden (the kind that fades on a wall).</summary>
        private static Color FamilyPhoto(int x, int y, int w, int h)
        {
            float u = x / (float)w;
            float v = y / (float)h;
            Color c = Color.Lerp(new Color(0.35f, 0.5f, 0.3f), new Color(0.75f, 0.8f, 0.85f), v);
            for (int person = 0; person < 3; person++)
            {
                float px = 0.3f + person * 0.2f;
                float head = Mathf.Sqrt((u - px) * (u - px) + (v - 0.62f + person * 0.03f) * (v - 0.62f + person * 0.03f));
                bool body = Mathf.Abs(u - px) < 0.07f && v < 0.56f - person * 0.03f;
                if (head < 0.06f || body)
                {
                    c = body ? new Color(0.45f + person * 0.15f, 0.3f, 0.25f + person * 0.1f) : new Color(0.85f, 0.7f, 0.58f);
                }
            }

            float border = Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v));
            return border < 0.05f ? new Color(0.94f, 0.92f, 0.86f) : Color.Lerp(c, new Color(0.9f, 0.8f, 0.6f), 0.25f);
        }
    }
}
