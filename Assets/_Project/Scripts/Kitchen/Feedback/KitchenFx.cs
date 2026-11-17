using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Graphics;
using Zmywak.Core.Haptics;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Dishwasher;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Feedback
{
    /// <summary>
    /// Particles and vibration for everything that happens at the sink: foam (tinted with the grime coming
    /// off), sparks and crust chips under the finger, the spray's jets, bubbles over a soaking dish, the tap's water stream and splash
    /// while rinsing, food flicked off
    /// on a scrape, a sparkle when a dish comes clean, shards when one smashes, steam from a running
    /// dishwasher. Particle systems are built here in code (world space, emitted by hand), only the
    /// materials and anchor points come from the scene.
    /// </summary>
    public class KitchenFx : MonoBehaviour
    {
        private const float FoamPerSecond = 55f;
        private const float WaterPerSecond = 80f;
        private const float SplashPerSecond = 35f;
        private const float SteamPerSecond = 5f;
        private const float WaterFlightSeconds = 0.3f;
        private const float SprayPerSecond = 260f;
        private const float SprayFlightSeconds = 0.12f;
        private const float SprayReach = 0.16f;
        private const float ChipsPerSecond = 30f;
        private const float SoakBubblesPerSecond = 9f;

        [SerializeField] private Material softMaterial;
        [SerializeField] private Material bubbleMaterial;
        [SerializeField] private Material glowMaterial;
        [SerializeField] private Material sparkleMaterial;
        [SerializeField] private Material shardMaterial;
        [SerializeField] private Material ringMaterial;
        [SerializeField] private Transform tapSpout;
        [SerializeField] private Transform steamOrigin;
        [SerializeField] private DishwasherController dishwasher;
        [Tooltip("Kuba's hand: while it holds the spray on a dish, water jets from its nozzle.")]
        [SerializeField] private GlovedHand hand;
        [Tooltip("The sink: bubbles rise over a dish soaking in it.")]
        [SerializeField] private WashStation washStation;

        private ParticleSystem _foam;
        private ParticleSystem _flecks;
        private ParticleSystem _sparks;
        private ParticleSystem _water;
        private ParticleSystem _splash;
        private ParticleSystem _sparkles;
        private ParticleSystem _ring;
        private ParticleSystem _shards;
        private ParticleSystem _dust;
        private ParticleSystem _steam;

        private float _foamBudget;
        private float _waterBudget;
        private float _splashBudget;
        private float _steamBudget;
        private float _sprayBudget;
        private float _soakBudget;

        public int LiveParticleCount =>
            Count(_foam) + Count(_flecks) + Count(_sparks) + Count(_water) + Count(_splash) +
            Count(_sparkles) + Count(_ring) + Count(_shards) + Count(_dust) + Count(_steam);

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("KitchenFx.Awake");
            _foam = Build("Foam", bubbleMaterial, gravity: -0.03f, grow: 1.4f);
            _flecks = Build("GrimeFlecks", softMaterial, gravity: 0.6f, grow: 0.8f);
            _sparks = Build("Sparks", glowMaterial, gravity: 0.8f, grow: 0.5f);
            _water = Build("WaterStream", glowMaterial, gravity: 1f, grow: 1f, stretched: true);
            _splash = Build("Splash", glowMaterial, gravity: 1f, grow: 0.7f);
            _sparkles = Build("Sparkles", sparkleMaterial, gravity: 0f, grow: 0f, pulse: true);
            _ring = Build("CleanRing", ringMaterial, gravity: 0f, grow: 1.9f, flat: true);
            _shards = Build("Shards", shardMaterial, gravity: 1.4f, grow: 1f);
            _dust = Build("Dust", softMaterial, gravity: -0.02f, grow: 2.2f);
            _steam = Build("Steam", softMaterial, gravity: -0.04f, grow: 2.5f);
        }

        private void OnEnable()
        {
            KitchenFeedbackEvents.Scraped += OnScraped;
            KitchenFeedbackEvents.Rinsing += OnRinsing;
            KitchenFeedbackEvents.Scrubbing += OnScrubbing;
            KitchenFeedbackEvents.Cleaned += OnCleaned;
            KitchenFeedbackEvents.Soaked += OnSoaked;
            KitchenFeedbackEvents.Racked += OnRacked;
            KitchenFeedbackEvents.Broken += OnBroken;
            KitchenFeedbackEvents.UrgentOrder += OnUrgentOrder;
            KitchenFeedbackEvents.LayerCleared += OnLayerCleared;
            if (dishwasher != null)
            {
                dishwasher.OnBrokeDown += OnDishwasherBrokeDown;
                dishwasher.OnRepaired += OnDishwasherRepaired;
            }
        }

        private void OnDisable()
        {
            KitchenFeedbackEvents.Scraped -= OnScraped;
            KitchenFeedbackEvents.Rinsing -= OnRinsing;
            KitchenFeedbackEvents.Scrubbing -= OnScrubbing;
            KitchenFeedbackEvents.Cleaned -= OnCleaned;
            KitchenFeedbackEvents.Soaked -= OnSoaked;
            KitchenFeedbackEvents.Racked -= OnRacked;
            KitchenFeedbackEvents.Broken -= OnBroken;
            KitchenFeedbackEvents.UrgentOrder -= OnUrgentOrder;
            KitchenFeedbackEvents.LayerCleared -= OnLayerCleared;
            if (dishwasher != null)
            {
                dishwasher.OnBrokeDown -= OnDishwasherBrokeDown;
                dishwasher.OnRepaired -= OnDishwasherRepaired;
            }
        }

        // Felt while the dirt is really coming off: a faint tick now and then (never with the wrong tool, never a buzz).
        private const float ScrubTickSeconds = 0.42f;
        private float _scrubTick;

        /// <summary>A hard layer (a crust, dried porridge, baked cheese) is gone: a firmer pulse and a little burst.</summary>
        private void OnLayerCleared(DishController dish, DirtTypeDefinition dirt)
        {
            HapticsService.Pulse(dirt != null && dirt.ScrubDifficulty >= 1.5f ? HapticPulse.Medium : HapticPulse.Light);
            Bounds bounds = dish.GetComponent<Renderer>().bounds;
            for (int i = 0; i < 6; i++)
            {
                Vector2 p = Random.insideUnitCircle * Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.6f;
                Emit(_sparkles, new Vector3(bounds.center.x + p.x, bounds.max.y + 0.05f, bounds.center.z + p.y), Vector3.up * 0.1f,
                    Random.Range(0.12f, 0.22f), Random.Range(0.35f, 0.6f), new Color(1f, 0.9f, 0.55f), Random.Range(0f, 90f));
            }
        }

        private void Update()
        {
            if (dishwasher != null && dishwasher.IsRunning && !dishwasher.IsBroken && steamOrigin != null)
            {
                _steamBudget += SteamPerSecond * Time.deltaTime;
                while (_steamBudget >= 1f)
                {
                    _steamBudget -= 1f;
                    Vector3 pos = steamOrigin.position + new Vector3(Random.Range(-0.45f, 0.45f), 0f, Random.Range(-0.35f, 0.35f));
                    Emit(_steam, pos, new Vector3(Random.Range(-0.05f, 0.05f), Random.Range(0.25f, 0.5f), 0f),
                        Random.Range(0.35f, 0.6f), Random.Range(2.5f, 3.5f), new Color(1f, 1f, 1f, 0.16f));
                }
            }

            Transform nozzle = hand != null ? hand.SprayNozzle : null;
            if (nozzle != null)
            {
                Spray(nozzle.position, hand.Contact);
            }

            DishController soaking = washStation != null ? washStation.Soaking : null;
            if (soaking != null && !soaking.IsMoving)
            {
                SoakBubbles(soaking);
            }
        }

        /// <summary>Bubbles breaking on the water over a soaking dish - fewer as it soaks through.</summary>
        private void SoakBubbles(DishController dish)
        {
            Bounds bounds = dish.GetComponent<Renderer>().bounds;
            float radius = Mathf.Min(bounds.extents.x, bounds.extents.z) * 0.8f;
            float surface = washStation.WaterLevelY + 0.01f;
            _soakBudget += SoakBubblesPerSecond * (1f - 0.6f * dish.SoakProgress01) * Time.deltaTime;
            while (_soakBudget >= 1f)
            {
                _soakBudget -= 1f;
                Vector2 p = Random.insideUnitCircle * radius;
                Emit(_foam, new Vector3(bounds.center.x + p.x, surface, bounds.center.z + p.y),
                    new Vector3(Random.Range(-0.03f, 0.03f), Random.Range(0.03f, 0.08f), Random.Range(-0.03f, 0.03f)),
                    Random.Range(0.05f, 0.11f), Random.Range(0.7f, 1.2f), new Color(0.9f, 0.96f, 1f, 0.8f));
            }
        }

        /// <summary>A splash as the dish goes into the water.</summary>
        private void OnSoaked(DishController dish)
        {
            Vector3 centre = washStation != null ? washStation.BasinFloor : dish.transform.position;
            float surface = washStation != null ? washStation.WaterLevelY + 0.02f : centre.y;
            for (int i = 0; i < 22; i++)
            {
                Vector2 dir = Random.insideUnitCircle;
                Emit(_splash, new Vector3(centre.x + dir.x * 0.5f, surface, centre.z + dir.y * 0.4f),
                    new Vector3(dir.x * 1.5f, Random.Range(1.2f, 2.4f), dir.y * 1.5f),
                    Random.Range(0.04f, 0.08f), Random.Range(0.35f, 0.55f), new Color(0.82f, 0.93f, 1f, 0.75f));
            }

            HapticsService.Pulse(HapticPulse.Light);
        }

        /// <summary>The spray's jets: a cone of fast droplets from the nozzle onto the dish around the finger, splashing back up.</summary>
        private void Spray(Vector3 start, Vector3 target)
        {
            float g = Physics.gravity.magnitude;
            _sprayBudget += SprayPerSecond * Time.deltaTime;
            while (_sprayBudget >= 1f)
            {
                _sprayBudget -= 1f;
                Vector2 spread = Random.insideUnitCircle * SprayReach;
                Vector3 landing = target + new Vector3(spread.x, 0f, spread.y);
                Vector3 velocity = (landing - start) / SprayFlightSeconds + Vector3.up * (0.5f * g * SprayFlightSeconds);
                Emit(_water, start, velocity, Random.Range(0.045f, 0.075f), SprayFlightSeconds + 0.015f, new Color(0.8f, 0.93f, 1f, 0.6f));

                if (Random.value < 0.3f)
                {
                    Vector2 dir = Random.insideUnitCircle;
                    Emit(_splash, landing + Vector3.up * 0.02f, new Vector3(dir.x * 1.6f, Random.Range(0.6f, 1.5f), dir.y * 1.6f),
                        Random.Range(0.03f, 0.055f), Random.Range(0.25f, 0.4f), new Color(0.82f, 0.93f, 1f, 0.65f));
                }
            }
        }

        // ------------------------------------------------------------------
        // Reactions
        // ------------------------------------------------------------------

        private void OnScraped(DishController dish)
        {
            Vector3 top = DishTop(dish);
            var leftovers = new Color(0.36f, 0.2f, 0.08f);
            for (int i = 0; i < 16; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                var velocity = new Vector3(dir.x * Random.Range(1.2f, 2.8f), Random.Range(1f, 2.2f), dir.y * Random.Range(1.2f, 2.8f));
                Color c = Color.Lerp(leftovers, new Color(0.62f, 0.2f, 0.1f), Random.value);
                Emit(_flecks, top, velocity, Random.Range(0.06f, 0.12f), Random.Range(0.6f, 0.9f), c);
            }

            HapticsService.Pulse(HapticPulse.Light);
        }

        private void OnRinsing(DishController dish)
        {
            Vector3 target = DishTop(dish);
            if (tapSpout != null)
            {
                // Aim each droplet so gravity carries it from the spout onto the middle of the dish.
                Vector3 start = tapSpout.position;
                float g = Physics.gravity.magnitude;
                Vector3 velocity = (target - start) / WaterFlightSeconds + Vector3.up * (0.5f * g * WaterFlightSeconds);
                _waterBudget += WaterPerSecond * Time.deltaTime;
                while (_waterBudget >= 1f)
                {
                    _waterBudget -= 1f;
                    Vector3 jitter = Random.insideUnitSphere * 0.15f;
                    Emit(_water, start, velocity + jitter, Random.Range(0.07f, 0.11f), WaterFlightSeconds + 0.02f,
                        new Color(0.75f, 0.9f, 1f, 0.55f));
                }
            }

            _splashBudget += SplashPerSecond * Time.deltaTime;
            while (_splashBudget >= 1f)
            {
                _splashBudget -= 1f;
                Vector2 dir = Random.insideUnitCircle;
                Emit(_splash, target + new Vector3(dir.x, 0f, dir.y) * 0.25f,
                    new Vector3(dir.x * 1.2f, Random.Range(0.8f, 1.8f), dir.y * 1.2f),
                    Random.Range(0.04f, 0.07f), Random.Range(0.3f, 0.45f), new Color(0.8f, 0.92f, 1f, 0.7f));
            }
        }

        private void OnScrubbing(DishController dish, Vector3 point, ToolDefinition tool, float intensity)
        {
            if (dish != null && dish.ToolSuits(tool) && intensity > 0.2f)
            {
                _scrubTick += Time.deltaTime;
                if (_scrubTick >= ScrubTickSeconds)
                {
                    _scrubTick = 0f;
                    HapticsService.Pulse(HapticPulse.Tick);
                }
            }

            string id = tool != null ? tool.Id : string.Empty;
            bool steelWool = id == "steel_wool";
            bool soak = id == "soak";
            Color grime = TopDirtColor(dish);

            if (id == "shower")
            {
                // The water is the spray's own (Update); here only the dirt it washes off, running away in drops.
                if (Random.value < 12f * Time.deltaTime)
                {
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    Emit(_flecks, point + new Vector3(dir.x, 0.2f, dir.y) * 0.08f, new Vector3(dir.x * 1.4f, Random.Range(0.4f, 0.9f), dir.y * 1.4f),
                        Random.Range(0.03f, 0.05f), Random.Range(0.3f, 0.5f), grime);
                }

                return;
            }

            if (id == "scraper")
            {
                // Crust chipping off the blade's edge - flicked ahead of it, away from the camera - and no lather.
                _foamBudget += ChipsPerSecond * (0.3f + intensity * 0.7f) * Time.deltaTime;
                while (_foamBudget >= 1f)
                {
                    _foamBudget -= 1f;
                    Vector2 spread = Random.insideUnitCircle * 0.06f;
                    Color chip = Color.Lerp(grime, grime * 0.55f, Random.value);
                    chip.a = 1f;
                    Emit(_flecks, point + new Vector3(spread.x, 0.03f, spread.y),
                        new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(0.6f, 1.3f), Random.Range(0.3f, 1.1f)),
                        Random.Range(0.04f, 0.08f), Random.Range(0.45f, 0.7f), chip, Random.Range(0f, 360f));
                }

                return;
            }

            _foamBudget += FoamPerSecond * (0.3f + intensity * 0.7f) * Time.deltaTime;
            while (_foamBudget >= 1f)
            {
                _foamBudget -= 1f;
                Vector2 spread = Random.insideUnitCircle * 0.14f;
                Vector3 pos = point + new Vector3(spread.x, 0.02f, spread.y);
                var velocity = new Vector3(Random.Range(-0.15f, 0.15f), Random.Range(0.1f, 0.35f), Random.Range(-0.15f, 0.15f));

                if (Random.value < 0.3f)
                {
                    // Grime coming off: flecks in the colour of the dirt being scrubbed.
                    Emit(_flecks, pos, velocity * 3f + Vector3.up * 0.4f, Random.Range(0.03f, 0.06f), Random.Range(0.4f, 0.7f), grime);
                    continue;
                }

                if (steelWool)
                {
                    Emit(_sparks, pos, velocity * 4f + Vector3.up * 0.6f, Random.Range(0.025f, 0.045f), Random.Range(0.25f, 0.4f),
                        new Color(1f, 0.85f, 0.55f, 0.9f));
                    continue;
                }

                Color foam = soak ? new Color(0.8f, 0.92f, 1f, 0.85f) : new Color(1f, 1f, 1f, 0.9f);
                Emit(_foam, pos, velocity, Random.Range(0.07f, 0.16f), Random.Range(0.6f, 1.1f), foam);
            }
        }

        private void OnCleaned(DishController dish)
        {
            Bounds bounds = dish.GetComponent<Renderer>().bounds;
            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 0.8f;
            for (int i = 0; i < 14; i++)
            {
                Vector2 p = Random.insideUnitCircle * radius;
                Vector3 pos = new Vector3(bounds.center.x + p.x, bounds.max.y + 0.05f, bounds.center.z + p.y);
                // Warm gold rather than white: the stars have to read on a white plate as well as on steel.
                Color c = Color.Lerp(new Color(1f, 0.8f, 0.25f), new Color(1f, 0.93f, 0.6f), Random.value);
                Emit(_sparkles, pos, Vector3.up * Random.Range(0.05f, 0.2f), Random.Range(0.18f, 0.36f), Random.Range(0.5f, 0.9f), c,
                    Random.Range(0f, 90f));
            }

            // A ripple spreading over the dish surface, flat so it follows the dish in perspective.
            Emit(_ring, new Vector3(bounds.center.x, bounds.max.y + 0.03f, bounds.center.z), Vector3.zero, radius * 2.2f, 0.5f,
                new Color(0.55f, 1f, 0.7f, 0.9f));

            HapticsService.Pulse(HapticPulse.Medium);
        }

        private void OnRacked(DishController dish, Vector3 position)
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 dir = Random.insideUnitCircle;
                Emit(_splash, position + Vector3.up * 0.1f, new Vector3(dir.x, Random.Range(0.8f, 1.5f), dir.y),
                    Random.Range(0.03f, 0.05f), Random.Range(0.3f, 0.45f), new Color(0.8f, 0.92f, 1f, 0.7f));
            }

            HapticsService.Pulse(HapticPulse.Light);
        }

        private void OnDishwasherBrokeDown(DishwasherFault fault)
        {
            if (steamOrigin == null)
            {
                return;
            }

            // A dark cough of smoke out of the vent: it has stopped, and not in a good way.
            for (int i = 0; i < 14; i++)
            {
                Vector3 pos = steamOrigin.position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.25f, 0.25f));
                float shade = Random.Range(0.25f, 0.4f);
                Emit(_steam, pos, new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0.6f, 1.2f), Random.Range(-0.2f, 0.2f)),
                    Random.Range(0.4f, 0.7f), Random.Range(1.4f, 2.2f), new Color(shade, shade, shade, 0.55f));
            }

            HapticsService.Pulse(HapticPulse.Heavy);
            CameraShake.On(Camera.main)?.Shake(0.05f, 0.35f); // the machine bangs to a stop
        }

        private void OnDishwasherRepaired()
        {
            if (steamOrigin != null)
            {
                // A clean white puff as it starts up again.
                for (int i = 0; i < 10; i++)
                {
                    Vector3 pos = steamOrigin.position + new Vector3(Random.Range(-0.4f, 0.4f), 0f, Random.Range(-0.3f, 0.3f));
                    Emit(_steam, pos, new Vector3(Random.Range(-0.2f, 0.2f), Random.Range(0.5f, 0.9f), 0f),
                        Random.Range(0.35f, 0.55f), Random.Range(1.2f, 1.8f), new Color(1f, 1f, 1f, 0.35f));
                }
            }

            HapticsService.Pulse(HapticPulse.Medium);
        }

        private void OnBroken(Vector3 position)
        {
            for (int i = 0; i < 18; i++)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                var velocity = new Vector3(dir.x * Random.Range(1f, 3f), Random.Range(2f, 4f), dir.y * Random.Range(1f, 3f));
                float shade = Random.Range(0.85f, 1f);
                Emit(_shards, position + Vector3.up * 0.2f, velocity, Random.Range(0.07f, 0.15f), Random.Range(0.9f, 1.4f),
                    new Color(shade, shade, shade * 0.96f, 1f), Random.Range(0f, 360f));
            }

            for (int i = 0; i < 6; i++)
            {
                Emit(_dust, position + Random.insideUnitSphere * 0.2f, Random.insideUnitSphere * 0.3f + Vector3.up * 0.2f,
                    Random.Range(0.3f, 0.5f), Random.Range(0.7f, 1.1f), new Color(0.85f, 0.83f, 0.8f, 0.35f));
            }

            HapticsService.Pulse(HapticPulse.Heavy);
            CameraShake.On(Camera.main)?.Shake(0.07f, 0.3f); // a plate smashing jolts the view, a little
        }

        /// <summary>An urgent order: felt as well as heard, the eyes being on the sink.</summary>
        private void OnUrgentOrder(DishDefinition dish)
        {
            HapticsService.Pulse(HapticPulse.Medium);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static Vector3 DishTop(DishController dish)
        {
            Bounds bounds = dish.GetComponent<Renderer>().bounds;
            return new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
        }

        private static Color TopDirtColor(DishController dish)
        {
            foreach (DirtLayerState layer in dish.DirtLayers)
            {
                if (layer.Type != null && layer.Remaining > 0f)
                {
                    Color c = ColorBlindPalette.Adjust(layer.Type.PlaceholderDirtyColor, layer.Type.Id);
                    c.a = 1f;
                    return c;
                }
            }

            return new Color(0.5f, 0.4f, 0.3f);
        }

        private static void Emit(ParticleSystem system, Vector3 position, Vector3 velocity, float size, float lifetime, Color color, float rotation = 0f)
        {
            var emit = new ParticleSystem.EmitParams
            {
                position = position,
                velocity = velocity,
                startSize = size,
                startLifetime = lifetime,
                startColor = color,
                rotation = rotation,
                applyShapeToPosition = false
            };
            system.Emit(emit, 1);
        }

        private static int Count(ParticleSystem system) => system != null ? system.particleCount : 0;

        /// <summary>
        /// A hand-emitted, world-space system: no emission or shape module, particles only come from Emit.
        /// <paramref name="grow"/> is the size at end of life relative to birth (1 = constant);
        /// <paramref name="pulse"/> swells then shrinks instead (sparkles). Everything fades out.
        /// </summary>
        private ParticleSystem Build(string label, Material material, float gravity, float grow, bool stretched = false, bool pulse = false,
            bool flat = false)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform, false);
            var system = go.AddComponent<ParticleSystem>();
            system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            ParticleSystem.MainModule main = system.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = gravity;
            main.maxParticles = 400;
            main.startSpeed = 0f;

            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = system.shape;
            shape.enabled = false;

            ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
            size.enabled = true;
            size.size = pulse
                ? new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(new Keyframe(0f, 0.3f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f)))
                : new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, grow));

            ParticleSystem.ColorOverLifetimeModule colour = system.colorOverLifetime;
            colour.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            colour.color = fade;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.renderMode = stretched ? ParticleSystemRenderMode.Stretch
                : flat ? ParticleSystemRenderMode.HorizontalBillboard
                : ParticleSystemRenderMode.Billboard;
            if (stretched)
            {
                renderer.velocityScale = 0.05f;
                renderer.lengthScale = 1f;
            }

            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            system.Play();
            return system;
        }
    }
}
