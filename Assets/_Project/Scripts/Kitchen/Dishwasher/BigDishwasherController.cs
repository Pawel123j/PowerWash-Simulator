using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Dishes;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Kitchen.Dishwasher
{
    /// <summary>
    /// The big dishwasher on the counter: dirty dishes go in straight from the sink, no scrubbing, up to its
    /// capacity; once started it washes for half an hour of real time (<see cref="LongCycleDishwasher"/>) -
    /// through the end of the shift and with the game closed - and the clean load is unloaded in a later shift,
    /// where it counts as washed (and pays its tips) then. The state is saved with the game at every change.
    /// Shows its load through the glass lid: a miniature of every dish in it, dirty, then clean.
    /// </summary>
    public class BigDishwasherController : MonoBehaviour
    {
        /// <summary>Where the time comes from - the real UTC clock; tests turn it forward.</summary>
        public static Func<DateTime> Clock = () => DateTime.UtcNow;

        [SerializeField] private int capacity = 40;
        [SerializeField] private float cycleMinutes = 30f;
        [SerializeField] private WashStation washStation;
        [Tooltip("Every kind of dish the shift can bring, to show the right miniature for each id in the machine.")]
        [SerializeField] private DishDefinition[] knownDishes = Array.Empty<DishDefinition>();

        [Header("Looks")]
        [Tooltip("Parent of the dish miniatures, at the rack inside (its local XZ is the rack's centre).")]
        [SerializeField] private Transform contents;
        [SerializeField] private Vector2 rackSize = new Vector2(1.7f, 1.2f);
        [SerializeField] private int rackColumns = 8;
        [SerializeField] private Renderer statusLight;
        [SerializeField] private Transform progressFill;
        [SerializeField] private float progressFullWidth = 0.6f;
        [SerializeField] private ParticleSystem steam;

        private static readonly Color LightIdle = new Color(0.25f, 0.27f, 0.3f);
        private static readonly Color LightLoaded = new Color(1f, 0.7f, 0.15f);
        private static readonly Color LightRunning = new Color(1f, 0.45f, 0.1f);
        private static readonly Color LightDone = new Color(0.2f, 0.95f, 0.35f);
        private static readonly Color DirtTint = new Color(0.4f, 0.26f, 0.14f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private LongCycleDishwasher _state;
        private readonly List<GameObject> _miniatures = new List<GameObject>();
        private readonly Dictionary<string, Material> _materials = new Dictionary<string, Material>();
        private bool _shownDone;
        private int _shownCount = -1;
        private bool _shownClean;
        private MaterialPropertyBlock _lightBlock;

        public int Capacity => _state.Capacity;
        public int Count => _state.Count;
        public bool CanLoad => _state.CanLoad;
        public bool CanStart => _state.CanStart;
        public bool IsStarted => _state.IsStarted;
        public bool IsRunning => _state.IsRunning(Clock());
        public bool IsDone => _state.IsDone(Clock());
        public TimeSpan Remaining => _state.Remaining(Clock());
        public TimeSpan Cycle => _state.Cycle;

        /// <summary>The dish in the sink, if it can go into the machine now (dirty - a clean one belongs on the rack).</summary>
        public DishController LoadableDish
        {
            get
            {
                DishController dish = washStation != null ? washStation.Occupant : null;
                return dish != null && _state.CanLoad && dish.CurrentStage < WashStage.ReadyForRack ? dish : null;
            }
        }

        /// <summary>A dish went in (the machine's count after it).</summary>
        public event Action<int> OnLoaded;
        public event Action OnStarted;

        /// <summary>The cycle finished while the kitchen was open.</summary>
        public event Action OnFinished;

        /// <summary>The clean load came out: the ids of the dishes in it.</summary>
        public event Action<IReadOnlyList<string>> OnUnloaded;

        private void Awake()
        {
            using var _startup = StartupTimeline.Measure("BigDishwasherController.Awake");
            _state = new LongCycleDishwasher(capacity, TimeSpan.FromMinutes(cycleMinutes));
            _state.ReadFrom(SaveSystem.Load(), Clock());
            _shownDone = _state.IsDone(Clock());
            _lightBlock = new MaterialPropertyBlock();
            RefreshMiniatures();
        }

        private void OnDestroy()
        {
            foreach (Material material in _materials.Values)
            {
                Destroy(material);
            }
        }

        /// <summary>The dish in the sink goes into the machine as it is.</summary>
        public bool TryLoadFromSink()
        {
            DishController dish = LoadableDish;
            if (dish == null || !_state.TryLoad(dish.Definition.Id))
            {
                return false;
            }

            dish.SendInto(contents != null ? SlotPosition(_state.Count - 1) : transform.position);
            Save();
            RefreshMiniatures();
            OnLoaded?.Invoke(_state.Count);
            return true;
        }

        public bool TryStart()
        {
            if (!_state.TryStart(Clock()))
            {
                return false;
            }

            Save();
            OnStarted?.Invoke();
            return true;
        }

        /// <summary>Takes the clean load out (only once the cycle is done); returns how many dishes came out.</summary>
        public int TryUnload()
        {
            IReadOnlyList<string> unloaded = _state.Unload(Clock());
            if (unloaded.Count == 0)
            {
                return 0;
            }

            // The emptied machine and the credit for what came out go to disk in one save: killed right after, the dishes are
            // still owed to the player (the shift's pay clears them), never gone with nothing paid.
            Save(unloaded.Count);
            _shownDone = false;
            RefreshMiniatures();
            OnUnloaded?.Invoke(unloaded);
            return unloaded.Count;
        }

        private void Save(int unloadedNow = 0)
        {
            // Read-modify-write, like the other persistent state: only this machine's fields are touched.
            SaveData data = SaveSystem.Load() ?? new SaveData();
            _state.WriteTo(data);
            data.bigDishwasherPendingDishes += unloadedNow;
            SaveSystem.Save(data);
        }

        private void Update()
        {
            DateTime now = Clock();
            bool done = _state.IsDone(now);
            bool running = _state.IsRunning(now);
            if (done && !_shownDone)
            {
                _shownDone = true;
                RefreshMiniatures(); // they come out clean
                OnFinished?.Invoke();
            }

            if (statusLight != null)
            {
                Color colour = done ? LightDone
                    : running ? Color.Lerp(LightRunning, Color.white, (Mathf.Sin(Time.unscaledTime * 3f) * 0.5f + 0.5f) * 0.2f)
                    : _state.Count > 0 ? LightLoaded
                    : LightIdle;
                statusLight.GetPropertyBlock(_lightBlock);
                _lightBlock.SetColor(BaseColorId, colour);
                _lightBlock.SetColor(EmissionColorId, colour * (colour == LightIdle ? 0.1f : 1.6f));
                statusLight.SetPropertyBlock(_lightBlock);
            }

            if (progressFill != null)
            {
                float progress = _state.Progress01(now);
                progressFill.localScale = new Vector3(Mathf.Max(0.0001f, progress * progressFullWidth), progressFill.localScale.y, progressFill.localScale.z);
                progressFill.localPosition = new Vector3(-progressFullWidth * 0.5f + progress * progressFullWidth * 0.5f,
                    progressFill.localPosition.y, progressFill.localPosition.z);
            }

            if (steam != null)
            {
                if (running && !steam.isPlaying)
                {
                    steam.Play();
                }
                else if (!running && steam.isPlaying)
                {
                    steam.Stop();
                }
            }
        }

        /// <summary>Where the i-th dish lies on the rack inside: filled from the back row forwards, a little askew.</summary>
        private Vector3 SlotPosition(int index)
        {
            int columns = Mathf.Max(1, rackColumns);
            int rows = Mathf.Max(1, Mathf.CeilToInt(_state.Capacity / (float)columns));
            int column = index % columns;
            int row = index / columns;
            float jitterX = (Mathf.PerlinNoise(index * 0.73f, 0.5f) - 0.5f) * 0.03f;
            float jitterZ = (Mathf.PerlinNoise(0.5f, index * 0.61f) - 0.5f) * 0.03f;
            var local = new Vector3(-rackSize.x * 0.5f + (column + 0.5f) * rackSize.x / columns + jitterX, 0f,
                rackSize.y * 0.5f - (row + 0.5f) * rackSize.y / rows + jitterZ);
            return contents.TransformPoint(local);
        }

        /// <summary>One miniature per dish in the machine, dirty until the cycle is done, clean after.</summary>
        private void RefreshMiniatures()
        {
            if (contents == null)
            {
                return;
            }

            bool clean = _state.IsDone(Clock());
            if (_shownCount == _state.Count && clean == _shownClean)
            {
                return;
            }

            foreach (GameObject miniature in _miniatures)
            {
                Destroy(miniature);
            }

            _miniatures.Clear();
            for (int i = 0; i < _state.Count; i++)
            {
                _miniatures.Add(CreateMiniature(_state.Load[i], i, clean));
            }

            _shownCount = _state.Count;
            _shownClean = clean;
        }

        private GameObject CreateMiniature(string id, int index, bool clean)
        {
            DishDefinition definition = Find(id);
            DishShape shape = definition != null ? definition.Shape : DishShape.Plate;
            Vector3 scale = (definition != null ? definition.PlaceholderScale : new Vector3(0.9f, 0.06f, 0.9f)) * MiniatureScale;

            var go = new GameObject($"InMachine_{id}", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(contents, false);
            go.transform.position = SlotPosition(index) + contents.up * scale.y; // its mesh is 2 units tall, centred
            go.transform.localScale = scale;
            go.transform.localRotation = Quaternion.Euler(0f, index * 47f % 360f, 0f);
            go.GetComponent<MeshFilter>().sharedMesh = DishMeshFactory.Get(shape);
            var renderer = go.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = MaterialFor(definition, clean);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; // under the lid, out of the key light
            return go;
        }

        private const float MiniatureScale = 0.22f;

        private Material MaterialFor(DishDefinition definition, bool clean)
        {
            string key = (definition != null ? definition.Id : "?") + (clean ? ":clean" : ":dirty");
            if (!_materials.TryGetValue(key, out Material material) || material == null)
            {
                Color colour = definition != null ? definition.PlaceholderCleanColor : new Color(0.9f, 0.9f, 0.88f);
                colour.a = 1f;
                material = PlaceholderMaterial.Create(clean ? colour : Color.Lerp(colour, DirtTint, 0.5f),
                    clean ? (definition != null ? definition.Smoothness : 0.8f) : 0.3f,
                    definition != null ? definition.Metallic : 0f);
                _materials[key] = material;
            }

            return material;
        }

        private DishDefinition Find(string id)
        {
            foreach (DishDefinition definition in knownDishes)
            {
                if (definition != null && definition.Id == id)
                {
                    return definition;
                }
            }

            return null;
        }
    }
}
