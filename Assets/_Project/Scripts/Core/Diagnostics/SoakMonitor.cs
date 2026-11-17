using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using Zmywak.Core.Graphics;
using Zmywak.Core.Telemetry;

namespace Zmywak.Core.Diagnostics
{
    /// <summary>
    /// For the soak test: every <see cref="SampleSeconds"/> of real time, how the game has been running - frame
    /// rate, the slowest frame, memory, live textures and meshes (a leak shows as a count that keeps climbing
    /// shift after shift), the battery and the quality tier. Each sample is one "[Soak]" line in the log
    /// (logcat on a phone) and one "type":"soak" line in the local <see cref="GameplayLog"/>. Temperature comes
    /// from the phone itself, read over adb by Tools/phone_soak.ps1.
    /// </summary>
    public class SoakMonitor : MonoBehaviour
    {
        public const float SampleSeconds = 30f;

        // Clearly under 30 fps: a frame held to a 30 fps cap lasts 33.4 ms and is not slow.
        private const float SlowFrameSeconds = 1.1f / 30f;

        private float _startedAt;
        private float _windowStartedAt;
        private int _frames;
        private int _slowFrames;
        private float _worstFrame;

        public int Samples { get; private set; }
        public SoakLogEntry First { get; private set; }
        public SoakLogEntry Last { get; private set; }

        private void OnEnable()
        {
            _startedAt = Time.realtimeSinceStartup;
            _windowStartedAt = _startedAt;
        }

        private void Update()
        {
            float frame = Time.unscaledDeltaTime;
            _frames++;
            _worstFrame = Mathf.Max(_worstFrame, frame);
            if (frame > SlowFrameSeconds)
            {
                _slowFrames++;
            }

            if (Time.realtimeSinceStartup - _windowStartedAt >= SampleSeconds)
            {
                Sample();
            }
        }

        /// <summary>Records the window so far now (also called when the soak ends).</summary>
        public SoakLogEntry Sample()
        {
            float now = Time.realtimeSinceStartup;
            float window = Mathf.Max(1e-3f, now - _windowStartedAt);
            var quality = FindAnyObjectByType<QualityManager>();
            var entry = new SoakLogEntry
            {
                minute = (now - _startedAt) / 60f,
                scene = SceneManager.GetActiveScene().name,
                averageFps = _frames / window,
                worstFrameMs = _worstFrame * 1000f,
                slowFrameShare = _frames > 0 ? (float)_slowFrames / _frames : 0f,
                allocatedMB = Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f),
                reservedMB = Profiler.GetTotalReservedMemoryLong() / (1024f * 1024f),
                managedMB = System.GC.GetTotalMemory(false) / (1024f * 1024f),
                textures = Resources.FindObjectsOfTypeAll<Texture>().Length,
                meshes = Resources.FindObjectsOfTypeAll<Mesh>().Length,
                batteryLevel = SystemInfo.batteryLevel,
                batteryStatus = SystemInfo.batteryStatus.ToString(),
                qualityTier = quality != null ? quality.CurrentTier.ToString() : string.Empty
            };

            GameplayLog.Append(entry);
            Debug.Log($"[Soak] {entry.minute:0.0} min {entry.scene}: {entry.averageFps:0.0} fps, worst {entry.worstFrameMs:0} ms, " +
                      $"slow {entry.slowFrameShare:P0}, {entry.allocatedMB:0} MB allocated ({entry.managedMB:0} MB managed), " +
                      $"{entry.textures} textures, {entry.meshes} meshes, battery {entry.batteryLevel:P0} {entry.batteryStatus}, {entry.qualityTier}");

            First ??= entry;
            Last = entry;
            Samples++;
            _windowStartedAt = now;
            _frames = 0;
            _slowFrames = 0;
            _worstFrame = 0f;
            return entry;
        }
    }
}
