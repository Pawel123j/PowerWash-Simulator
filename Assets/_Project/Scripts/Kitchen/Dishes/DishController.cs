using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Collections;
using Zmywak.Core.Data;
using Zmywak.Core.Graphics;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Feedback;
using Zmywak.Kitchen.Input;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Tools;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Dishes
{
    /// <summary>
    /// Visual + interaction shell around a <see cref="DishInstance"/>. Falls into the pile as a physics
    /// body when spawned; the first touch claims it: it leaves pile physics for good (kinematic) and is
    /// lifted onto the <see cref="WashStation"/> (the sink), enlarged, where it responds to
    /// scrape/rinse/scrub gestures from <see cref="DishInputRouter"/> until it is dragged onto the rack.
    /// </summary>
    [RequireComponent(typeof(Renderer))]
    [RequireComponent(typeof(Rigidbody))]
    public class DishController : MonoBehaviour
    {
        private const float RubSpeedThreshold = 40f; // pixels/sec of movement required to count as scrubbing
        private const float FullIntensityRubSpeed = 1500f; // pixels/sec that counts as scrubbing flat out (foam, sound)
        private const float CounterSurfaceY = 0f; // world Y of the counter/rack/dishwasher plane
        private const float MoveSharpness = 14f; // exponential smoothing rate for the lift to the sink

        /// <summary>
        /// How far from the sink (across the counter, world units) a clean dish must be let go of to count as a drop that
        /// missed the rack. Closer, it was a tap, or the finger still held from the rinse that just finished it - that
        /// cost the player care points for nothing. The sink and the rack are about 4.5 apart; a dish in the sink is
        /// about 1.2 across its half.
        /// </summary>
        public const float MissedDropDistance = 1.5f;
        private const float SettleSpeedSqr = 0.04f; // (0.2 m/s)^2
        private const float SettleGraceSeconds = 1f; // freshly spawned dishes are still falling
        private const float AudibleImpactSpeed = 0.8f; // m/s - slower contacts are the pile settling, not clatter
        private const float ImpactCooldown = 0.12f;
        private const float SlowRollSpeedSqr = 0.09f; // (0.3 m/s)^2
        private const float SlowSpinSqr = 4f; // (2 rad/s)^2
        private const float InspectionSmoothness = 0.5f; // a see-through glass held to the light: the key light must not glare over it
        private const float BaseAngularDamping = 3f;
        private const float RollingAngularDamping = 10f; // while rolling slowly: ~20% of the spin lost per physics step
        private const float ManualSteerHoldSeconds = 2f; // after a drag, the reflection stays put this long
        private const float SoakScale = 1.5f; // drawn smaller lying in the water than held up in the hands

        // Stacking thin plates on pans needs more solver work than PhysX's default; dishes that start out overlapping
        // (a new one dropped into a crowded pile) are pushed apart gently instead of popping across the kitchen.
        private const int SolverIterations = 12;
        private const int SolverVelocityIterations = 4;
        private const float MaxDepenetrationSpeed = 3f;

        private static readonly int BumpMap = Shader.PropertyToID("_BumpMap");
        private static readonly int BumpScale = Shader.PropertyToID("_BumpScale");

        /// <summary>How wide the heap of scraps is on a plate or a pan, in scene units.</summary>
        private const float LeftoversWidth = 0.3f;
        private const float ModelledLeftoversWidth = 0.4f;

        /// <summary>Slope of the dirt's relief in the dish's normal map (see <see cref="DirtCanvas.WriteNormals"/>).</summary>
        private const float DirtReliefStrength = 2.2f;

        [SerializeField] private Transform leftoversMarker;
        [SerializeField] private RackGrid rackGrid;
        [SerializeField] private WashStation washStation;

        private DishInstance _instance;
        private Renderer _renderer;
        private Collider _collider;
        private Rigidbody _body;
        private Camera _camera;
        private Texture2D _dirtTexture;
        private Texture2D _dirtNormals; // null on a see-through glass
        private Color32[] _pixels;
        private Color32[] _normalPixels;
        private Color[] _layerColors;
        private float[] _layerGloss;
        private float[] _layerRelief;
        private Color[] _baseColors;
        private bool _textureDirty;
        // Redraw the whole texture (and its relief) next refresh, not just the patch the brush changed.
        private bool _redrawAll = true;
        private bool _hasLastScrubUv;
        private Vector2 _lastScrubUv;
        private Vector3 _baseScale;
        private Vector3 _targetPosition;
        private Vector3 _targetScale;
        private Quaternion _targetRotation = Quaternion.identity;
        private bool _smoothMove;
        private bool _dragging;
        private bool _finishedDuringGesture;
        private float _spawnTime;
        private float _lastImpactTime = float.NegativeInfinity;
        private bool _landed;    // has come to rest in the pile at least once - only then can a knock break it
        private bool _shattered;
        private readonly List<Vector3> _contactNormals = new List<Vector3>(4);
        private readonly InspectionLight _light = new InspectionLight();
        private bool _inspecting;
        private float _lastSteerTime = float.NegativeInfinity;
        private bool[] _hiddenLayers;

        public WashStage CurrentStage => _instance.Stage;
        public DishDefinition Definition => _instance.Definition;

        /// <summary>It hit something hard enough to break (see <see cref="DishPhysics"/>) and is gone.</summary>
        public event System.Action<DishController> Shattered;

        /// <summary>Has settled in the pile at least once since it arrived: from then on a hard knock can break it.</summary>
        public bool HasLanded => _landed;

        /// <summary>How much bigger than in the pile the dish is drawn right now (2.2x held in the sink, easing between).</summary>
        public float ScaleFactor => _baseScale.x > 0f ? transform.localScale.x / _baseScale.x : 1f;

        public Collider Collider => _collider;
        public IReadOnlyList<DirtLayerState> DirtLayers => _instance.Layers;
        public float DirtAmountAt(int layer, Vector2 uv) => _instance.Canvas.DirtAt(layer, uv);
        public bool IsClaimed { get; private set; }

        /// <summary>Lying in the sink's water (see <see cref="TrySoak"/>).</summary>
        public bool IsSoaking { get; private set; }

        /// <summary>How far its soak has got, 0-1 - kept after it comes out.</summary>
        public float SoakProgress01 => _instance.Soak01;

        /// <summary>
        /// It can go down into the water: held in the sink and still, its scraps off and not yet clean (nor a glass
        /// up to the light), and nothing else soaking.
        /// </summary>
        public bool CanSoak =>
            IsClaimed && !IsSoaking && !IsMoving && !_dragging && !_inspecting && washStation != null &&
            washStation.Occupant == this && washStation.Soaking == null &&
            (_instance.Stage == WashStage.AwaitingRinse || _instance.Stage == WashStage.AwaitingScrub);

        /// <summary>Only hidden streaks are left: the glass needs holding to the light (see <see cref="IsInspecting"/>).</summary>
        public bool NeedsInspection => _instance.NeedsInspection;

        /// <summary>Whether a tool works on the dirt being washed now (<see cref="DishInstance.ToolSuits"/>).</summary>
        public bool ToolSuits(ToolDefinition tool) => _instance.ToolSuits(tool);

        /// <summary>
        /// The tool to pick for the dirt in front of Kuba now: of those given, the first that works on it and takes it right
        /// off (the scraper and the spray leave a film) - or, if none does, the first that works on it at all. Null if none.
        /// </summary>
        public ToolDefinition SuitingTool(IReadOnlyList<ToolDefinition> tools)
        {
            if (tools == null || _instance.Stage != WashStage.AwaitingScrub)
            {
                return null;
            }

            ToolDefinition partial = null;
            foreach (ToolDefinition tool in tools)
            {
                if (tool == null || !_instance.ToolSuits(tool))
                {
                    continue;
                }

                if (tool.ResidueFloor <= 0f)
                {
                    return tool;
                }

                partial ??= tool;
            }

            return partial;
        }

        /// <summary>The glass is in the sink being checked against the light right now.</summary>
        public bool IsInspecting => _inspecting;

        /// <summary>Where the reflection's stripe is across the dish (UV x), while inspecting.</summary>
        public float InspectionStripeX => _light.StripeX;

        /// <summary>The phone's tilt moves the reflection (sensor present and tilting allowed); otherwise a finger does.</summary>
        public static bool TiltSteersReflection =>
            !SuspendTilt && AccessibilitySettings.TiltEnabled && Accelerometer.current != null && Accelerometer.current.enabled;

        /// <summary>How hard Kuba works this shift (1 rested, less when tired - see Shifts.Fatigue): scrubbing and rinsing go
        /// at this pace. The working tool still follows the finger precisely. Set at the shift's start.</summary>
        public static float ScrubStrength { get; set; } = 1f;

        /// <summary>The guided plate's lesson is on the dirt and the tool to pick for it: rubbing does nothing yet, so the
        /// plate cannot be scrubbed clean before the lesson has shown which tool to use (KitchenTutorial).</summary>
        public static bool ScrubHeld { get; set; }

        /// <summary>For the development autoplay only: a phone lying on the table cannot tilt, so the reflection
        /// sweeps by itself, as without a sensor. Not a setting - nothing saves it.</summary>
        public static bool SuspendTilt { get; set; }

        /// <summary>
        /// A finger dragging beside the glass slides the reflection - the way to check it on a phone without a
        /// tilt sensor, or with tilting switched off. Left alone for a moment, the reflection sweeps by itself again.
        /// </summary>
        public void SteerInspectionLight(float uvDelta)
        {
            if (!_inspecting || TiltSteersReflection)
            {
                return;
            }

            _light.Nudge(uvDelta);
            _lastSteerTime = Time.unscaledTime;
            _textureDirty = true;
        }

        /// <summary>True while the dish is still gliding to (or back to) the sink.</summary>
        public bool IsMoving => _smoothMove &&
                                ((transform.position - _targetPosition).sqrMagnitude > 1e-4f ||
                                 (transform.localScale - _targetScale).sqrMagnitude > 1e-4f ||
                                 Quaternion.Angle(transform.rotation, _targetRotation) > 0.1f);

        /// <summary>At rest in the pile: not claimed, not moving, and not a dish that is still falling in.</summary>
        public bool IsSettledInPile => !IsClaimed && Time.time - _spawnTime > SettleGraceSeconds &&
                                       _body.linearVelocity.sqrMagnitude < SettleSpeedSqr;

        /// <summary>World height of the dish's highest point - what makes the pile "tall".</summary>
        public float TopY => _renderer.bounds.max.y;

        /// <summary>
        /// Whether a tap could reach this dish: some part of it is the first thing a ray from the camera hits,
        /// the same test the input router makes. A dish buried under others in the pile is not.
        /// </summary>
        public bool IsTappableFrom(Camera view)
        {
            if (view == null || _collider == null)
            {
                return false;
            }

            Bounds bounds = _collider.bounds;
            Vector3 e = bounds.extents * 0.6f;
            Vector3 center = bounds.center;
            Vector3[] probes =
            {
                center, center + new Vector3(0f, bounds.extents.y, 0f),
                center + new Vector3(e.x, 0f, 0f), center - new Vector3(e.x, 0f, 0f),
                center + new Vector3(0f, 0f, e.z), center - new Vector3(0f, 0f, e.z)
            };

            foreach (Vector3 probe in probes)
            {
                Ray ray = view.ScreenPointToRay(view.WorldToScreenPoint(probe));
                if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.collider.GetComponentInParent<DishController>() == this)
                {
                    return true;
                }
            }

            return false;
        }

        public static DishController Create(DishDefinition definition, Vector3 position, Transform parent,
            RackGrid rackGrid, WashStation washStation = null)
        {
            using var _create = StartupTimeline.Measure("dish: Create");
            GameObject go = CreateBody(definition);
            go.name = $"Dish_{definition.Id}";
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = definition.PlaceholderScale;
            var body = go.GetComponent<Renderer>();
            if (definition.SeeThrough)
            {
                // Light goes through a glass: no solid shadow under it.
                body.sharedMaterial = PlaceholderMaterial.CreateGlass(definition.Smoothness);
                body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
            {
                body.sharedMaterial = PlaceholderMaterial.CreateDish(definition.Smoothness, definition.Metallic);
            }

            // Dishes mirror SceneLighting's studio, as their glaze and steel were tuned to, not the kitchen's baked
            // probe: from up in the hands, the probe showed a pan's sides the lamp-lit counter close by, and its dark
            // steel came out white as a plastic bowl.
            body.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;

            DishMeshFactory.AddDetails(go, definition.Shape, PlaceholderMaterial.Create(new Color(0.12f, 0.12f, 0.13f), 0.3f));

            // Scraps of the meal heaped on the dish's floor, the same real size on every dish (and smaller in a glass):
            // the heap's scale undoes the dish's own squashed one, so the lumps stay round.
            var leftovers = new GameObject("LeftoversMarker", typeof(MeshFilter), typeof(MeshRenderer));
            // What was eaten off this kind of dish (modelled in Blender), or generic lumps.
            bool modelled = definition.LeftoversMesh != null && definition.LeftoversPalette != null;
            leftovers.GetComponent<MeshFilter>().sharedMesh = modelled ? definition.LeftoversMesh : DishMeshFactory.Leftovers;
            leftovers.transform.SetParent(go.transform, false);
            Vector3 dishScale = definition.PlaceholderScale;
            Vector2 floor = DishMeshFactory.Floor(definition.Shape);
            // A modelled meal is a little bigger than the lumps were: a few pieces of pasta read as a meal at 12 cm, not 9.
            float heap = Mathf.Min(modelled ? ModelledLeftoversWidth : LeftoversWidth, floor.x * 2f * dishScale.x * 0.75f);
            leftovers.transform.localScale = new Vector3(heap / dishScale.x, heap / dishScale.y, heap / dishScale.z);
            leftovers.transform.localPosition = new Vector3(0f, floor.y, 0f);
            leftovers.transform.localRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            var leftoversRenderer = leftovers.GetComponent<MeshRenderer>();
            leftoversRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // too small to matter; saves a shadow draw
            // Cooked food is mostly matte: at 0.55 the sink lamp's highlight bleached the tops of the modelled pieces.
            Material food = PlaceholderMaterial.Create(Color.white, modelled ? 0.3f : 0.55f);
            food.mainTexture = modelled ? definition.LeftoversPalette : DishMeshFactory.LeftoversPalette;
            leftoversRenderer.sharedMaterial = food;

            var controller = go.AddComponent<DishController>();
            controller.leftoversMarker = leftovers.transform;
            controller.rackGrid = rackGrid;
            controller.washStation = washStation;
            controller.Init(definition);
            return controller;
        }

        private static GameObject CreateBody(DishDefinition definition)
        {
            using var _body = StartupTimeline.Measure("dish: body mesh and convex collider");
            if (definition.PlaceholderPrimitive != PrimitiveType.Cylinder)
            {
                return GameObject.CreatePrimitive(definition.PlaceholderPrimitive);
            }

            Mesh mesh = DishMeshFactory.Get(definition.Shape);
            var go = new GameObject();
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
            var meshCollider = go.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
            meshCollider.convex = true; // required for a non-kinematic Rigidbody
            return go;
        }

        private void Init(DishDefinition definition)
        {
            bool circular = definition.PlaceholderPrimitive == PrimitiveType.Cylinder;
            // The dirt drawn at the tier's sharpness (Low a quarter of the texels), made ahead on a worker thread when it is ready
            // - and the next one of the kind is started at once.
            int resolution = DeviceProfile.DirtResolution(QualityManager.ActiveTier);
            DirtCanvas ready;
            using (StartupTimeline.Measure("dish: take prewarmed dirt")) ready = DirtPrewarm.Take(definition, resolution);
            using (StartupTimeline.Measure(ready != null ? "dish: DishInstance (dirt was ready)" : "dish: DishInstance (dirt made on the main thread)"))
            {
                _instance = ready != null
                    ? new DishInstance(definition, ready)
                    : new DishInstance(definition, Random.Range(int.MinValue, int.MaxValue), circular, resolution);
            }

            using (StartupTimeline.Measure("dish: start the next dirt on the worker")) DirtPrewarm.Prepare(definition, resolution);
            _renderer = GetComponent<Renderer>();
            _collider = GetComponent<Collider>();
            _body = GetComponent<Rigidbody>();
            _body.isKinematic = false;
            _body.interpolation = RigidbodyInterpolation.Interpolate;
            // Real cylinders (glasses on their side, pans) otherwise roll across the counter for ages - one
            // glass rolled 2 m and another off the counter - and a dish that won't hold still is hard to tap.
            // Grippier material, some damping, and letting slow bodies fall asleep sooner (default
            // threshold 0.005) settle the pile quickly; touching bodies still wake when the pile changes.
            _body.angularDamping = BaseAngularDamping;
            _body.linearDamping = 0.2f;
            _body.sleepThreshold = 0.05f;
            _body.mass = Mathf.Max(0.05f, definition.MassKg); // a pot outweighs a glass in the pile
            _body.solverIterations = SolverIterations;
            _body.solverVelocityIterations = SolverVelocityIterations;
            _body.maxDepenetrationVelocity = MaxDepenetrationSpeed;
            // Speculative contacts: a thin plate thrown off a collapsing pile must not tunnel through the counter or a
            // pan, and unlike ContinuousDynamic this mode is allowed once the dish turns kinematic in the hands.
            _body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            _collider.sharedMaterial = DishPhysics.SurfaceMaterial(definition.Material);
            _camera = Camera.main;
            _baseScale = transform.localScale;
            _spawnTime = Time.time;
            CreateDirtTexture();
            RefreshVisual();
        }

        private void CreateDirtTexture()
        {
            using var _texture = StartupTimeline.Measure("dish: dirt texture setup (rent, glaze, relief)");
            int resolution = _instance.Canvas.Resolution;
            _instance.Canvas.CleanOpacity = Definition.CleanOpacity; // a glass's texture alpha is opacity, not gloss
            // Textures and pixel arrays come from the pool (a dish before this one may have used them: every texel is redrawn
            // before they are seen); the glaze under the dirt is the same for every dish of the kind.
            _dirtTexture = DishResourcePool.RentTexture(resolution, false, $"{name}_Dirt");
            _pixels = DishResourcePool.RentPixels(resolution * resolution);
            _baseColors = DishResourcePool.Glaze(Definition, resolution);

            _hiddenLayers = new bool[_instance.Layers.Count];
            for (int i = 0; i < _hiddenLayers.Length; i++)
            {
                _hiddenLayers[i] = _instance.IsHidden(i);
            }

            _layerColors = new Color[_instance.Layers.Count];
            _layerGloss = new float[_layerColors.Length];
            _layerRelief = new float[_layerColors.Length];
            for (int i = 0; i < _layerColors.Length; i++)
            {
                DirtTypeDefinition type = _instance.Layers[i].Type;
                Color color = type != null
                    ? ColorBlindPalette.Adjust(type.PlaceholderDirtyColor, type.Id)
                    : new Color(0.45f, 0.32f, 0.14f);
                // Alpha is the layer's opacity; the colour-blind palette swaps hue, not how see-through it is.
                color.a = type != null ? type.PlaceholderDirtyColor.a : 1f;
                _layerColors[i] = color;
                _layerGloss[i] = type != null ? type.Gloss : DirtCanvas.DirtGloss;
                _layerRelief[i] = type != null ? type.Relief : 0.5f;
            }

            Material material = _renderer.sharedMaterial; // this dish's own instance (PlaceholderMaterial.Create)
            material.mainTexture = _dirtTexture;
            material.color = Color.white; // the texture carries the colour
            if (!Definition.SeeThrough)
            {
                // The dirt's relief: crusts and smears catch the light at their edges (the template has _NORMALMAP on).
                _dirtNormals = DishResourcePool.RentTexture(resolution, true, $"{name}_DirtRelief");
                _normalPixels = DishResourcePool.RentPixels(resolution * resolution);
                material.SetTexture(BumpMap, _dirtNormals);
                material.SetFloat(BumpScale, 1f);
            }

            _textureDirty = true;
        }

        /// <summary>
        /// Called when the player touches this dish. The first successful claim takes it out of pile physics
        /// for good and lifts it onto the sink. Returns false (dish stays in the pile) while another dish is
        /// still occupying the sink.
        /// </summary>
        public bool TryClaim()
        {
            if (IsSoaking)
            {
                return TryTakeOutOfSoak(); // touching a soaking dish lifts it back out, when the hands are free
            }

            if (IsClaimed)
            {
                return true;
            }

            if (washStation != null && !washStation.TryOccupy(this))
            {
                return false;
            }

            IsClaimed = true;
            transform.rotation = Quaternion.identity;
            _body.isKinematic = true;
            // From here on the dish is moved by writing transform.position directly (drag, rack snap).
            // With interpolation left on, the Rigidbody keeps writing its interpolated physics pose back
            // into the transform, which can overwrite a drag position a frame later - so on release
            // RackGrid.TryPlace intermittently saw a stale position and the drop onto the rack failed.
            _body.interpolation = RigidbodyInterpolation.None;
            // A convex hull seals the opening of bowls, pans and glasses. Once held, the body is
            // kinematic and can use the actual concave surface, so the finger hits the visible dirt.
            if (_collider is MeshCollider surface)
            {
                surface.convex = false;
            }

            if (washStation != null)
            {
                MoveToSink();
            }

            KitchenFeedbackEvents.RaiseClaimed(this);
            return true;
        }

        /// <summary>
        /// A pile dish hits something. Hard enough, glass or china breaks (<see cref="DishPhysics"/>) - but only once it
        /// has settled in the pile, and not when the other one is a newcomer still dropping in: a dish arriving on the
        /// pile is put there, not thrown, and nobody pays for a waiter's aim. Otherwise it clatters, or rings if it is
        /// steel (rate-limited per dish so a settling stack doesn't buzz).
        /// </summary>
        private void OnCollisionEnter(Collision collision)
        {
            if (IsClaimed || _shattered || collision.contactCount == 0)
            {
                return;
            }

            _contactNormals.Clear();
            for (int i = 0; i < collision.contactCount && i < 4; i++)
            {
                _contactNormals.Add(collision.GetContact(i).normal);
            }

            float knock = DishPhysics.NormalImpactSpeed(collision.relativeVelocity, _contactNormals);
            Vector3 point = collision.GetContact(0).point;
            if (CanBreakAgainst(collision) && DishPhysics.Breaks(Definition.Material, knock))
            {
                Shatter(point);
                return;
            }

            float speed = collision.relativeVelocity.magnitude;
            if (speed < AudibleImpactSpeed || Time.time - _lastImpactTime < ImpactCooldown)
            {
                return;
            }

            _lastImpactTime = Time.time;
            KitchenFeedbackEvents.RaiseImpact(point, speed, Definition.Material);
        }

        private bool CanBreakAgainst(Collision collision)
        {
            if (!_landed || AccessibilitySettings.RelaxedMode)
            {
                return false;
            }

            DishController other = collision.rigidbody != null ? collision.rigidbody.GetComponent<DishController>() : null;
            return other == null || other._landed || other.IsClaimed;
        }

        /// <summary>Breaks it where it struck: shards and the crash, and it is gone from the pile.</summary>
        public void Shatter(Vector3 point)
        {
            if (_shattered || IsClaimed)
            {
                return;
            }

            _shattered = true;
            KitchenFeedbackEvents.RaiseBroken(point);
            Shattered?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>Shoves a pile dish (the pile giving way): it wakes and moves off at this speed and spin.</summary>
        public void Knock(Vector3 velocity, Vector3 spin)
        {
            if (IsClaimed || _body.isKinematic)
            {
                return;
            }

            // Knocked off the pile, it was in the pile - even if the collapse came the very frame it settled, before
            // FixedUpdate could note it: a dish thrown off the top lands as hard as any other.
            _landed = true;
            _body.WakeUp();
            _body.linearVelocity = velocity;
            _body.angularVelocity = spin;
        }

        /// <summary>Puts a pile dish that fell off the counter back above the pile - steel that hit the floor, or any
        /// dish in relaxed mode. It drops in like a newcomer.</summary>
        public void ReturnToPile(Vector3 position)
        {
            if (IsClaimed)
            {
                return;
            }

            _body.linearVelocity = Vector3.zero;
            _body.angularVelocity = Vector3.zero;
            _body.position = position;
            transform.position = position;
            _spawnTime = Time.time;
            _landed = false;
        }

        public void ConfirmRacked()
        {
            _instance.MarkRacked();
            _smoothMove = false;
            transform.localScale = _baseScale;
            if (washStation != null)
            {
                washStation.Release(this);
            }

            RefreshVisual();
            KitchenFeedbackEvents.RaiseRacked(this, transform.position);
        }

        public void ConfirmDone()
        {
            _instance.MarkDone();
        }

        private const float FlightSeconds = 0.2f;
        private float _flight = -1f;
        private Vector3 _flightFrom;
        private Vector3 _flightTo;
        private Quaternion _flightFromRotation;
        private Quaternion _flightToRotation;
        private Vector3 _flightFromScale;
        private Vector3 _flightToScale;

        /// <summary>Already placed on the rack: it is drawn flying in from where it was let go, in an arc, over a moment.</summary>
        public void FlyFrom(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _flightTo = transform.position;
            _flightToRotation = transform.rotation;
            _flightToScale = transform.localScale;
            _flightFrom = position;
            _flightFromRotation = rotation;
            _flightFromScale = scale;
            _flight = 0f;
            transform.SetPositionAndRotation(position, rotation);
            transform.localScale = scale;
        }

        private bool UpdateFlight()
        {
            if (_flight < 0f)
            {
                return false;
            }

            _flight = Mathf.Min(1f, _flight + Time.deltaTime / FlightSeconds);
            float t = 1f - (1f - _flight) * (1f - _flight);
            Vector3 position = Vector3.Lerp(_flightFrom, _flightTo, t) + Vector3.up * (Mathf.Sin(_flight * Mathf.PI) * 0.6f);
            transform.SetPositionAndRotation(position, Quaternion.Slerp(_flightFromRotation, _flightToRotation, t));
            transform.localScale = Vector3.Lerp(_flightFromScale, _flightToScale, t);
            if (_flight >= 1f)
            {
                transform.SetPositionAndRotation(_flightTo, _flightToRotation);
                transform.localScale = _flightToScale;
                _flight = -1f;
            }

            return true;
        }

        /// <summary>
        /// Into the big dishwasher, dirty as it is: off the sink, out of reach of any finger, gliding down to its
        /// place inside and gone - the machine keeps the dish's id, not the object (it may wash for longer than the
        /// scene lives).
        /// </summary>
        public void SendInto(Vector3 place)
        {
            if (washStation != null)
            {
                washStation.Release(this);
            }

            if (_collider != null)
            {
                _collider.enabled = false; // no tap can catch it on the way
            }

            if (_body != null && !_body.isKinematic)
            {
                // Lifted off the pile (a friend from the crew takes it): out of physics, moved by hand from here.
                _body.isKinematic = true;
                _body.interpolation = RigidbodyInterpolation.None;
            }

            if (leftoversMarker != null)
            {
                leftoversMarker.gameObject.SetActive(false);
            }

            _dragging = false;
            _targetPosition = place;
            _targetScale = _baseScale * 0.22f; // the machine's miniature of it
            _targetRotation = transform.rotation;
            _smoothMove = true;
            Destroy(gameObject, SendIntoSeconds);
        }

        private const float SendIntoSeconds = 0.45f;

        /// <summary>
        /// Down into the sink's water to soak, freeing the hands for the next dish: it lies on the bottom under the
        /// held spot (a tall glass on its side), rinsed by the water and its dirt softening the longer it stays.
        /// </summary>
        public bool TrySoak()
        {
            if (!CanSoak || !washStation.TrySoak(this))
            {
                return false;
            }

            IsSoaking = true;
            _hasLastScrubUv = false;
            bool tall = _baseScale.y * 2f > _baseScale.x;
            // Resting on the floor: the mesh is two units high, centred, so half its height (or a glass's radius) up.
            float rest = (tall ? _baseScale.x * 0.5f : _baseScale.y) * SoakScale;
            _targetPosition = washStation.BasinFloor + Vector3.up * (rest + 0.01f);
            _targetScale = _baseScale * SoakScale;
            _targetRotation = tall ? Quaternion.Euler(0f, 25f, 90f) : Quaternion.Euler(4f, 0f, -3f);
            _smoothMove = true;
            KitchenFeedbackEvents.RaiseSoaked(this);
            return true;
        }

        /// <summary>Back up out of the water into the hands - only when they are free (nothing held in the sink).</summary>
        public bool TryTakeOutOfSoak()
        {
            if (!IsSoaking || washStation == null || !washStation.TryOccupy(this))
            {
                return false;
            }

            washStation.ReleaseSoak(this);
            IsSoaking = false;
            MoveToSink();
            RefreshVisual();
            KitchenFeedbackEvents.RaiseClaimed(this);
            return true;
        }

        public void OnGestureUpdate(TouchGestureRecognizer recognizer, Vector2 screenPos)
        {
            WashStage before = _instance.Stage;
            switch (_instance.Stage)
            {
                case WashStage.AwaitingRinse:
                    _instance.Rinse(Time.deltaTime * ScrubStrength); // a tired hand holds the dish under the tap longer
                    KitchenFeedbackEvents.RaiseRinsing(this);
                    if (_instance.Stage == WashStage.ReadyForRack)
                    {
                        KitchenFeedbackEvents.RaiseCleaned(this); // nothing stuck on it - the rinse did it
                    }
                    break;

                case WashStage.AwaitingScrub:
                    bool onDish = TryGetWashContact(screenPos, out Vector2 uv, out Vector3 surfacePoint);
                    if (onDish && recognizer.PointerSpeed > RubSpeedThreshold && !ScrubHeld)
                    {
                        ToolDefinition tool = ActiveToolSelector.Instance != null ? ActiveToolSelector.Instance.CurrentTool : null;
                        Vector2 from = _hasLastScrubUv ? _lastScrubUv :
                            TryGetWashContact(recognizer.PreviousPosition, out Vector2 startUv, out _) ? startUv : uv;
                        int dirtyBefore = DirtyLayers(out _);
                        if (_instance.ScrubStroke(tool, from, uv, Time.deltaTime * ScrubStrength))
                        {
                            if (_instance.Stage == WashStage.AwaitingScrub && DirtyLayers(out DirtTypeDefinition gone) < dirtyBefore)
                            {
                                KitchenFeedbackEvents.RaiseLayerCleared(this, gone); // one kind of dirt done, another to go
                            }

                            _textureDirty = true;
                            float intensity = Mathf.Clamp01(recognizer.PointerSpeed / FullIntensityRubSpeed);
                            KitchenFeedbackEvents.RaiseScrubbing(this, surfacePoint, tool, intensity);
                        }

                        if (_instance.Stage == WashStage.ReadyForRack)
                        {
                            KitchenFeedbackEvents.RaiseCleaned(this); // the "squeak of a clean plate" moment
                        }
                    }

                    // Stroke continues from here next frame; lifting off the dish breaks the stroke.
                    _hasLastScrubUv = onDish;
                    _lastScrubUv = uv;
                    break;

                case WashStage.ReadyForRack:
                    // Finishing a rub must not grab and shrink the dish under the same finger.
                    // Lift first; the next gesture carries the clean dish to the rack.
                    if (_finishedDuringGesture)
                    {
                        break;
                    }
                    if (_camera != null)
                    {
                        // Follows the finger 1:1 (no smoothing) so the drop lands exactly where it looks
                        // like it will; only the shrink back to normal size is eased.
                        _dragging = true;
                        _smoothMove = false;
                        transform.position = ScreenToWorldOnPlane(screenPos, CounterSurfaceY);
                        if (rackGrid != null)
                        {
                            // Light up the cells it would cover, and turn it the way it would go in - a pan's
                            // handle visibly swings round when it only fits the gap sideways.
                            RackPlacement? preview = rackGrid.PreviewDrop(this, transform.position);
                            Quaternion turn = preview.HasValue ? RackGrid.RotationFor(preview.Value) : Quaternion.identity;
                            transform.rotation = Quaternion.Slerp(transform.rotation, turn, 1f - Mathf.Exp(-MoveSharpness * Time.deltaTime));
                        }
                    }
                    break;
            }

            if (before != WashStage.ReadyForRack && _instance.Stage == WashStage.ReadyForRack)
            {
                _finishedDuringGesture = true;
            }
            RefreshVisual();
        }

        /// <summary>How many layers still have dirt on them - and the last one found clean (what just came off, when the count drops).</summary>
        private int DirtyLayers(out DirtTypeDefinition lastClean)
        {
            int dirty = 0;
            lastClean = null;
            foreach (DirtLayerState layer in _instance.Layers)
            {
                if (layer.Remaining > 0f)
                {
                    dirty++;
                }
                else
                {
                    lastClean = layer.Type;
                }
            }

            return dirty;
        }

        public void OnGestureEnd(TouchGestureRecognizer recognizer, Vector2 screenPos)
        {
            _hasLastScrubUv = false;
            if (_instance.Stage == WashStage.AwaitingScrape && recognizer.SwipeDetectedThisFrame)
            {
                _instance.Scrape();
                KitchenFeedbackEvents.RaiseScraped(this);
            }
            else if (_instance.Stage == WashStage.ReadyForRack && !_finishedDuringGesture)
            {
                _dragging = false;
                if (rackGrid != null)
                {
                    rackGrid.ClearPreview();
                }

                bool placed = rackGrid != null && rackGrid.TryPlace(this, transform.position);
                if (!placed && rackGrid != null && recognizer.FlickedUp)
                {
                    // Flicked up towards the rack: it flies into the spot it packs into best.
                    placed = rackGrid.TryPlaceBest(this);
                }

                if (!placed && washStation != null)
                {
                    Vector3 fromSink = transform.position - washStation.HeldPosition;
                    bool carriedAway = new Vector2(fromSink.x, fromSink.z).magnitude > MissedDropDistance;
                    transform.rotation = Quaternion.identity;
                    MoveToSink(); // missed the rack: spring back to the sink instead of lying around
                    if (carriedAway)
                    {
                        KitchenFeedbackEvents.RaiseDropMissed(this);
                    }
                }
            }

            _finishedDuringGesture = false;
            RefreshVisual();
        }

        private void MoveToSink()
        {
            _targetPosition = washStation.HeldPosition;
            _targetScale = _baseScale * washStation.HeldScaleMultiplier;
            // Present the opening to the player: a tall pot's near wall otherwise hides its bottom,
            // making real surface picking correct but leaving some dirt impossible to reach.
            _targetRotation = _camera != null
                ? Quaternion.FromToRotation(Vector3.up, (_camera.transform.position - _targetPosition).normalized)
                : Quaternion.identity;
            _smoothMove = true;
        }

        /// <summary>
        /// PhysX has no rolling resistance: a glass on its side kept rolling slowly across the flat counter -
        /// in one test run to the far side of the sink, never settling enough to be picked up. Slow, loose
        /// dishes lose their spin quickly instead, like real friction; falling and tumbling ones (faster) are
        /// left alone.
        /// </summary>
        private void FixedUpdate()
        {
            if (IsClaimed || _body.isKinematic)
            {
                return;
            }

            if (!_landed && IsSettledInPile)
            {
                _landed = true; // in the pile now: from here on it can be knocked over, and broken
            }

            // Rolling resistance by switching the damping, never by scaling the velocity: writing angularVelocity
            // wakes the body, so every step kept each resting dish awake - the pile never fell asleep, jittered
            // on (a plate on a pan crept at 2 cm/s for good) and cost the phone physics work for nothing.
            bool rollingSlowly = _body.linearVelocity.sqrMagnitude < SlowRollSpeedSqr && _body.angularVelocity.sqrMagnitude < SlowSpinSqr;
            float damping = rollingSlowly ? RollingAngularDamping : BaseAngularDamping;
            if (_body.angularDamping != damping)
            {
                _body.angularDamping = damping;
            }
        }

        private void Update()
        {
            if (UpdateFlight())
            {
                return;
            }

            UpdateInspection();
            UpdateSoak();

            if (_dragging)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, _baseScale, 1f - Mathf.Exp(-MoveSharpness * Time.deltaTime));
                return;
            }

            if (!_smoothMove)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-MoveSharpness * Time.deltaTime);
            transform.position = Vector3.Lerp(transform.position, _targetPosition, t);
            transform.localScale = Vector3.Lerp(transform.localScale, _targetScale, t);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, t);
            if (!IsMoving)
            {
                transform.position = _targetPosition;
                transform.localScale = _targetScale;
                transform.rotation = _targetRotation;
            }
        }

        /// <summary>Lying in the water: it rinses and softens (and a dish with nothing stuck on it comes clean).</summary>
        private void UpdateSoak()
        {
            if (!IsSoaking)
            {
                return;
            }

            WashStage before = _instance.Stage;
            _instance.Soak(Time.deltaTime);
            if (_instance.Stage != before)
            {
                RefreshVisual(); // wet now
                if (_instance.Stage == WashStage.ReadyForRack)
                {
                    KitchenFeedbackEvents.RaiseCleaned(this);
                }
            }
        }

        /// <summary>
        /// Holding a glass to the light: once only hidden streaks are left on the dish in the sink, a reflection
        /// plays over it - steered by tilting the phone when the tilt sensor is there (and allowed), drifting
        /// by itself otherwise - and the streaks show wherever it passes.
        /// </summary>
        private void UpdateInspection()
        {
            bool inspect = IsClaimed && !IsSoaking && !IsMoving && !_dragging && _instance.NeedsInspection;
            if (inspect != _inspecting)
            {
                _inspecting = inspect;
                _light.Reset();
                SetTiltSensor(inspect);
                _textureDirty = true;
                _redrawAll = true; // the reflection comes or goes over the whole glass
                if (Definition.SeeThrough)
                {
                    // On a see-through glass the texture's alpha is opacity, so it cannot dull the key light's
                    // glare for the inspection - the material does, and gives the gloss back afterwards.
                    _renderer.sharedMaterial.SetFloat("_Smoothness", inspect ? InspectionSmoothness : Definition.Smoothness);
                }

                if (inspect)
                {
                    KitchenFeedbackEvents.RaiseHeldToLight(this);
                }
                else
                {
                    RefreshVisual(); // drop the reflection from the texture
                }
            }

            if (!_inspecting)
            {
                return;
            }

            if (TiltSteersReflection)
            {
                _light.Tilt(Accelerometer.current.acceleration.ReadValue(), Time.deltaTime);
            }
            else if (Time.unscaledTime - _lastSteerTime > ManualSteerHoldSeconds)
            {
                _light.Drift(Time.deltaTime); // nobody steering: it sweeps by itself
            }

            // The moving reflection means redrawing the whole glass: every other frame is smooth enough and halves the work.
            if ((Time.frameCount & 1) == 0)
            {
                _textureDirty = true;
            }

            RefreshVisual();
        }

        /// <summary>Sensors cost battery on a phone: the accelerometer only runs while a glass is held to the light.</summary>
        private static void SetTiltSensor(bool on)
        {
            Accelerometer tilt = Accelerometer.current;
            if (tilt == null)
            {
                return;
            }

            if (on && AccessibilitySettings.TiltEnabled && !tilt.enabled)
            {
                InputSystem.EnableDevice(tilt);
            }
            // Only a real sensor is switched off again; one added by a test (or a remote) is left as it was.
            else if (!on && tilt.enabled && tilt.native)
            {
                InputSystem.DisableDevice(tilt);
            }
        }

        private void OnDestroy()
        {
            if (_inspecting)
            {
                SetTiltSensor(false);
            }

            if (washStation != null)
            {
                washStation.Release(this);
                washStation.ReleaseSoak(this);
            }

            // Back to the pool for the next dish (the material using them is destroyed below).
            DishResourcePool.Return(_dirtTexture, false);
            DishResourcePool.Return(_dirtNormals, true);
            DishResourcePool.Return(_pixels);
            DishResourcePool.Return(_normalPixels);
            _dirtTexture = null;
            _dirtNormals = null;
            _pixels = null;
            _normalPixels = null;

            // Every material on the dish (body, leftovers, handle) was created for it alone.
            foreach (Renderer part in GetComponentsInChildren<Renderer>(true))
            {
                if (part.sharedMaterial != null)
                {
                    Destroy(part.sharedMaterial);
                }
            }
        }

        /// <summary>
        /// The visible wash surface under the finger, with a small screen-space allowance at its edge.
        /// The same contact drives scrubbing, touch acquisition and the tool in the hand.
        /// </summary>
        public bool TryGetWashContact(Vector2 screenPos, out Vector2 uv, out Vector3 worldPoint)
        {
            uv = default;
            worldPoint = default;
            if (_camera == null || _collider == null || !_collider.enabled)
            {
                return false;
            }

            if (TryRaycastWashSurface(screenPos, out uv, out worldPoint))
            {
                return true;
            }

            // Only the held dish gets an allowance: it never reaches into the pile or steals a UI tap.
            if (!IsClaimed || IsSoaking || IsMoving)
            {
                return false;
            }

            Vector2 centre = _camera.WorldToScreenPoint(_collider.bounds.center);
            Vector2 towardsCentre = (centre - screenPos).normalized;
            float padding = Mathf.Clamp(Mathf.Min(_camera.pixelWidth, _camera.pixelHeight) * 0.025f, 8f, 28f);
            for (int step = 1; step <= 4; step++)
            {
                if (TryRaycastWashSurface(screenPos + towardsCentre * (padding * step / 4f), out uv, out worldPoint))
                {
                    return true;
                }
            }

            return false;
        }

        private bool TryRaycastWashSurface(Vector2 screenPos, out Vector2 uv, out Vector3 worldPoint)
        {
            uv = default;
            worldPoint = default;
            if (!_collider.Raycast(_camera.ScreenPointToRay(screenPos), out RaycastHit hit, 100f))
            {
                return false;
            }

            worldPoint = hit.point;
            if (_collider is MeshCollider mesh && !mesh.convex)
            {
                uv = hit.textureCoord;
                // The decorated outside has its own UV patch and carries no washable dirt.
                return !DishMeshFactory.ExteriorUv.Contains(uv);
            }

            Vector3 local = transform.InverseTransformPoint(hit.point);
            uv = new Vector2(local.x + 0.5f, local.z + 0.5f);
            return true;
        }

        private Vector3 ScreenToWorldOnPlane(Vector2 screenPos, float planeY)
        {
            Ray ray = _camera.ScreenPointToRay(screenPos);
            var plane = new Plane(Vector3.up, new Vector3(0f, planeY, 0f));
            return plane.Raycast(ray, out float enter) ? ray.GetPoint(enter) : ray.origin;
        }

        /// <summary>
        /// Into the texture's own memory, only the rows of the patch that changed (a brush stroke is a few hundred texels of
        /// sixty-five thousand), then up to the GPU - instead of copying the whole array through SetPixels32 every frame.
        /// </summary>
        private static void Upload(Texture2D texture, Color32[] pixels, RectInt? patch)
        {
            NativeArray<Color32> raw = texture.GetRawTextureData<Color32>();
            if (!patch.HasValue || raw.Length != pixels.Length)
            {
                if (raw.Length == pixels.Length)
                {
                    raw.CopyFrom(pixels);
                }
                else
                {
                    texture.SetPixels32(pixels);
                }
            }
            else
            {
                RectInt p = Grow(patch.Value, 0, texture.width); // within the texture
                int width = texture.width;
                for (int y = p.yMin; y < p.yMax; y++)
                {
                    NativeArray<Color32>.Copy(pixels, y * width + p.xMin, raw, y * width + p.xMin, p.width);
                }
            }

            texture.Apply(false);
        }

        private static RectInt Grow(RectInt patch, int by, int resolution)
        {
            int xMin = Mathf.Max(0, patch.xMin - by), yMin = Mathf.Max(0, patch.yMin - by);
            int xMax = Mathf.Min(resolution, patch.xMax + by), yMax = Mathf.Min(resolution, patch.yMax + by);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private void RefreshVisual()
        {
            if (leftoversMarker != null)
            {
                leftoversMarker.gameObject.SetActive(_instance.Stage == WashStage.AwaitingScrape);
            }

            // Rinsed: from here on the clean glaze is beaded with water (drawn into the relief map).
            bool wet = _instance.Stage >= WashStage.AwaitingScrub;
            if (wet != _instance.Canvas.Wet)
            {
                _instance.Canvas.Wet = wet;
                _textureDirty = true;
                _redrawAll = true; // water beads all over
            }

            if (!_textureDirty || _dirtTexture == null)
            {
                return;
            }

            // A scrub changes a brush-sized patch: only that is worked out again (the canvas tracks it). The whole dish
            // is redrawn when it first appears, gets wet, or is held to the light (the reflection moves over all of it).
            DirtCanvas canvas = _instance.Canvas;
            bool changed = canvas.TakeChangedRegion(out RectInt patch);
            bool all = _redrawAll;
            if (all || _inspecting || changed)
            {
                bool whole = all || _inspecting;
                using var _colours = StartupTimeline.Measure(whole ? "dish: redraw dirt colours (whole texture)" : "dish: redraw dirt colours (patch)");
                canvas.WriteColors(_pixels, Definition.PlaceholderCleanColor, _layerColors, _baseColors, _hiddenLayers,
                    _inspecting ? _light : null, _layerGloss, whole ? (RectInt?)null : patch);
                Upload(_dirtTexture, _pixels, whole ? (RectInt?)null : patch);
            }

            // The relief only moves with the dirt (and the water beads), never with the reflection.
            if (_dirtNormals != null && (all || changed))
            {
                using var _relief = StartupTimeline.Measure(all ? "dish: redraw relief (whole texture)" : "dish: redraw relief (patch)");
                canvas.WriteNormals(_normalPixels, _layerRelief, DirtReliefStrength, all ? (RectInt?)null : patch);
                // The relief's redraw reaches a texel past the patch (its slopes look at the neighbours).
                Upload(_dirtNormals, _normalPixels, all ? (RectInt?)null : Grow(patch, 2, canvas.Resolution));
            }

            _textureDirty = false;
            _redrawAll = false;
        }
    }
}
