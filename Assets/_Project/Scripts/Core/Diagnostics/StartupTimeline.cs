using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using Zmywak.Core.GameState;
using Debug = UnityEngine.Debug;

namespace Zmywak.Core.Diagnostics
{
    /// <summary>
    /// Where the time goes when a scene starts (PWS-029: a hitch of 120-390 ms at the start of every shift). For the Editor and development
    /// builds only; in a release build every call is a no-op. A window opens when a scene begins to load (<see cref="Open"/>, called by
    /// <see cref="SceneLoader"/>) and lasts <see cref="WindowSeconds"/>; inside it the pieces of work that matter are measured
    /// (<see cref="Measure"/>: <c>using var _ = StartupTimeline.Measure("name");</c>, and <see cref="Mark"/> for the stretches of a coroutine),
    /// every frame's length is kept, and at its end one report says: the frames and the worst one, the garbage collections and the memory it
    /// cost, each named piece's count / total / worst, and for every frame slower than 30 fps the pieces that ran in it and what they do not
    /// explain (the engine's own work: rendering, physics, scene activation). Each piece is also a Profiler sample of the same name, so a
    /// development build connected to the Unity Profiler shows them on the timeline; on a phone without the Profiler the report is one
    /// "[Startup]" block in logcat: <c>adb logcat -s Unity | findstr Startup</c>.
    /// </summary>
    public static class StartupTimeline
    {
        public const float WindowSeconds = 8f;
        public const float SpikeMilliseconds = 33.4f;
        private const int MaxEntries = 1024;
        private const int ReportedNames = 16;
        private const int ReportedSpikes = 8;

        /// <summary>One measured piece.</summary>
        public struct Entry
        {
            public string Name;
            public int Frame;
            public float StartMs; // since the window opened
            public float Ms;
            public int Depth; // 0: not inside another measured piece
        }

        /// <summary>A frame slower than <see cref="SpikeMilliseconds"/>.</summary>
        public struct Spike
        {
            public int Frame;
            public float Ms;
            public long GcAllocBytes; // -1: not known
        }

        public readonly struct Scope : IDisposable
        {
            private readonly string _name;
            private readonly long _start;
            private readonly int _depth;

            internal Scope(string name)
            {
                _name = name;
                _start = Stopwatch.GetTimestamp();
                _depth = EnterScope(name);
            }

            public void Dispose()
            {
                if (_name != null)
                {
                    ExitScope(_name, _start, _depth);
                }
            }
        }

        private static readonly List<Entry> EntryList = new List<Entry>(MaxEntries);
        private static readonly List<Spike> SpikeList = new List<Spike>();
        private static int _mainThread;
        private static int _depth;
        private static long _openedAt;
        private static float _openedRealtime;
        private static string _label;
        private static int _frames;
        private static float _worstMs;
        private static int _worstFrame;
        private static int _gc0, _gc1, _gc2;
        private static long _memory;
        private static ProfilerRecorder _gcAlloc;
        private static StartupTimelineDriver _driver;

        /// <summary>False in a release build (and anywhere a test turns it off): nothing is measured.</summary>
        public static bool Enabled { get; set; }

        public static bool IsOpen { get; private set; }

        /// <summary>The last finished report ("" before the first).</summary>
        public static string LastReport { get; private set; } = string.Empty;

        /// <summary>The label of the window that is open, or the last one.</summary>
        public static string Label => _label;

        public static IReadOnlyList<Entry> Entries => EntryList;
        public static IReadOnlyList<Spike> Spikes => SpikeList;
        public static int Frames => _frames;
        public static float WorstFrameMs => _worstMs;

        /// <summary>A finished report, as it was logged.</summary>
        public static event Action<string> Reported;

        /// <summary>The clock for <see cref="Mark"/>.</summary>
        public static long Now => Stopwatch.GetTimestamp();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Init()
        {
            _mainThread = Thread.CurrentThread.ManagedThreadId;
            Enabled = Application.isEditor || Debug.isDebugBuild;
        }

        /// <summary>Measures the rest of the block it is declared in.</summary>
        public static Scope Measure(string name) => Enabled && Thread.CurrentThread.ManagedThreadId == _mainThread ? new Scope(name) : default;

        /// <summary>Records a stretch that began at <paramref name="startTimestamp"/> (<see cref="Now"/>) and ends now - for what a coroutine waits through.</summary>
        public static void Mark(string name, long startTimestamp)
        {
            if (IsOpen && Enabled && Thread.CurrentThread.ManagedThreadId == _mainThread)
            {
                Record(name, startTimestamp, 0);
            }
        }

        /// <summary>Starts a window: a scene is about to load, or has just started.</summary>
        public static void Open(string label)
        {
            if (!Enabled || Thread.CurrentThread.ManagedThreadId != _mainThread)
            {
                return;
            }

            _label = label;
            EntryList.Clear();
            SpikeList.Clear();
            _frames = 0;
            _worstMs = 0f;
            _worstFrame = 0;
            _depth = 0;
            _openedAt = Stopwatch.GetTimestamp();
            _openedRealtime = Time.realtimeSinceStartup;
            _gc0 = GC.CollectionCount(0);
            _gc1 = GC.CollectionCount(1);
            _gc2 = GC.CollectionCount(2);
            _memory = GC.GetTotalMemory(false);
            if (!_gcAlloc.Valid)
            {
                _gcAlloc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            }

            if (_driver == null)
            {
                var go = new GameObject("StartupTimeline") { hideFlags = HideFlags.HideAndDontSave };
                UnityEngine.Object.DontDestroyOnLoad(go);
                _driver = go.AddComponent<StartupTimelineDriver>();
            }

            IsOpen = true;
        }

        /// <summary>Ends the window now and makes the report (also called by the driver when the time is up).</summary>
        public static string Close()
        {
            if (!IsOpen)
            {
                return LastReport;
            }

            IsOpen = false;
            LastReport = BuildReport();
            Debug.Log(LastReport);
            Reported?.Invoke(LastReport);
            return LastReport;
        }

        /// <summary>Forgets everything and stops (tests).</summary>
        public static void Reset()
        {
            IsOpen = false;
            EntryList.Clear();
            SpikeList.Clear();
            LastReport = string.Empty;
            _depth = 0;
        }

        internal static void OnFrame(float unscaledDeltaSeconds, int frameCount)
        {
            if (!IsOpen)
            {
                return;
            }

            // The delta at the start of this frame is how long the one before it took.
            int finished = frameCount - 1;
            float ms = unscaledDeltaSeconds * 1000f;
            _frames++;
            if (ms > _worstMs)
            {
                _worstMs = ms;
                _worstFrame = finished;
            }

            if (ms > SpikeMilliseconds && _frames > 1)
            {
                SpikeList.Add(new Spike { Frame = finished, Ms = ms, GcAllocBytes = _gcAlloc.Valid ? _gcAlloc.LastValue : -1L });
            }

            if (Time.realtimeSinceStartup - _openedRealtime >= WindowSeconds)
            {
                Close();
            }
        }

        private static int EnterScope(string name)
        {
            Profiler.BeginSample(name);
            return _depth++;
        }

        private static void ExitScope(string name, long start, int depth)
        {
            Profiler.EndSample();
            _depth = depth;
            if (IsOpen)
            {
                Record(name, start, depth);
            }
        }

        private static void Record(string name, long start, int depth)
        {
            if (EntryList.Count >= MaxEntries)
            {
                return;
            }

            long end = Stopwatch.GetTimestamp();
            double toMs = 1000.0 / Stopwatch.Frequency;
            EntryList.Add(new Entry
            {
                Name = name,
                Frame = Time.frameCount,
                StartMs = (float)((start - _openedAt) * toMs),
                Ms = (float)((end - start) * toMs),
                Depth = depth
            });
        }

        private static string BuildReport()
        {
            var sb = new StringBuilder(2048);
            float seconds = Time.realtimeSinceStartup - _openedRealtime;
            long memoryDelta = GC.GetTotalMemory(false) - _memory;
            sb.AppendLine($"[Startup] {_label}: window {seconds:0.0} s, {_frames} frames, worst {_worstMs:0} ms (frame {_worstFrame}), " +
                          $"{SpikeList.Count} frames over {SpikeMilliseconds:0} ms | GC gen0 +{GC.CollectionCount(0) - _gc0} gen1 +{GC.CollectionCount(1) - _gc1} " +
                          $"gen2 +{GC.CollectionCount(2) - _gc2} | managed memory {memoryDelta / 1048576f:+0.0;-0.0} MB");

            // Each named piece: how often, how long in all, the longest.
            var byName = new Dictionary<string, (int n, float total, float max, int depth)>();
            foreach (Entry e in EntryList)
            {
                byName.TryGetValue(e.Name, out (int n, float total, float max, int depth) s);
                byName[e.Name] = (s.n + 1, s.total + e.Ms, Mathf.Max(s.max, e.Ms), e.Depth);
            }

            var names = new List<KeyValuePair<string, (int n, float total, float max, int depth)>>(byName);
            names.Sort((a, b) => b.Value.total.CompareTo(a.Value.total));
            sb.AppendLine("  pieces (count, total ms, longest ms; '>' = inside another piece):");
            for (int i = 0; i < names.Count && i < ReportedNames; i++)
            {
                var (n, total, max, depth) = names[i].Value;
                sb.AppendLine($"   {(depth > 0 ? ">" : " ")} {names[i].Key,-46} x{n,-3} {total,8:0.0} {max,8:0.0}");
            }

            // The frames that were slow, worst first: what ran in each, and what is left over.
            var spikes = new List<Spike>(SpikeList);
            spikes.Sort((a, b) => b.Ms.CompareTo(a.Ms));
            if (spikes.Count > 0)
            {
                sb.AppendLine("  slow frames (worst first; 'rest' = what the measured pieces do not explain: rendering, physics, scene activation):");
            }

            for (int i = 0; i < spikes.Count && i < ReportedSpikes; i++)
            {
                Spike spike = spikes[i];
                var inFrame = new Dictionary<string, (int n, float ms)>();
                float attributed = 0f;
                foreach (Entry e in EntryList)
                {
                    if (e.Frame != spike.Frame || e.Depth != 0)
                    {
                        continue;
                    }

                    inFrame.TryGetValue(e.Name, out (int n, float ms) s);
                    inFrame[e.Name] = (s.n + 1, s.ms + e.Ms);
                    attributed += e.Ms;
                }

                string alloc = spike.GcAllocBytes >= 0 ? $", {spike.GcAllocBytes / 1024f:0} KB allocated" : string.Empty;
                sb.Append($"   f{spike.Frame} {spike.Ms:0} ms{alloc}: ");
                foreach (KeyValuePair<string, (int n, float ms)> pair in inFrame)
                {
                    sb.Append($"{pair.Key}{(pair.Value.n > 1 ? " x" + pair.Value.n : string.Empty)} {pair.Value.ms:0.0} | ");
                }

                sb.AppendLine($"rest {Mathf.Max(0f, spike.Ms - attributed):0} ms");
            }

            return sb.ToString();
        }
    }

    /// <summary>Gives <see cref="StartupTimeline"/> the length of every frame and closes its window on time.</summary>
    internal sealed class StartupTimelineDriver : MonoBehaviour, IKeepAcrossRestart
    {
        private void Update()
        {
            StartupTimeline.OnFrame(Time.unscaledDeltaTime, Time.frameCount);
        }
    }
}
