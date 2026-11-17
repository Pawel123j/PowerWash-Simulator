using System.Collections;
using System.Diagnostics;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Zmywak.Core.Diagnostics;
using Zmywak.Core.GameState;
using Debug = UnityEngine.Debug;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// PWS-029: the start of a shift measured piece by piece. The Editor numbers are for the PC; the point is that the same report,
    /// named the same way, comes out of a development build on a phone (QA_REPORT.md says how to read it).
    /// </summary>
    [Category("QARegression")]
    public class StartupTimelinePlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
            StartupTimeline.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            StartupTimeline.Reset();
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        [Timeout(120_000)]
        public IEnumerator TheStartOfAShift_IsMeasuredPieceByPiece_AndTheReportNamesWhereTheTimeWent()
        {
            Assert.IsTrue(StartupTimeline.Enabled, "Measuring is off in the Editor");
            string report = null;
            void OnReport(string r) => report = r;
            StartupTimeline.Reported += OnReport;
            try
            {
                Zmywak.Core.Settings.AccessibilitySettings.RelaxedMode = true;
                SceneLoader.Load("Kitchen_Shift"); // the game's own way in: it opens the measuring window
                var deadline = new PlayDeadline(40f);
                while (report == null && !deadline.Passed)
                {
                    yield return null;
                }
            }
            finally
            {
                StartupTimeline.Reported -= OnReport;
            }

            Assert.IsNotNull(report, "The window never closed, so there is no report");
            TestContext.WriteLine(report);

            StringAssert.Contains("[Startup] Kitchen_Shift", report);
            string[] pieces =
            {
                "scene: loading in the background", "scene: activation", "scene: first frame",
                "ShiftController.Start", "shift: PrewarmDirt", "shift: SpawnDish",
                "dish: Create", "dish: body mesh and convex collider", "dish: DishInstance", "dish: dirt texture setup",
                "dish: redraw dirt colours (whole texture)", "KitchenAudio.Awake", "ShiftHUD.OnEnable"
            };
            // Every piece was measured (the report itself lists the sixteen that took longest, so it is the entries that are checked)...
            foreach (string piece in pieces)
            {
                bool measured = false;
                foreach (StartupTimeline.Entry entry in StartupTimeline.Entries)
                {
                    measured |= entry.Name.StartsWith(piece);
                }

                Assert.IsTrue(measured, $"Nothing named '{piece}' was measured");
            }

            // ...and the report says where the time went: the biggest pieces by name, with their count, total and longest.
            StringAssert.Contains("scene: activation", report);
            StringAssert.Contains("pieces (count, total ms, longest ms", report);
            StringAssert.Contains("GC gen0", report);
            StringAssert.Contains("managed memory", report);
            Assert.Greater(StartupTimeline.Frames, 30, "Hardly any frames were counted in 8 seconds");
            Assert.Greater(StartupTimeline.Entries.Count, 20);
            foreach (StartupTimeline.Entry entry in StartupTimeline.Entries)
            {
                Assert.GreaterOrEqual(entry.Ms, 0f, entry.Name);
                Assert.GreaterOrEqual(entry.StartMs, 0f, entry.Name);
                Assert.GreaterOrEqual(entry.Depth, 0, entry.Name);
            }

            // Every slow frame is in the report, with what ran in it (or "rest" when only the engine did).
            foreach (StartupTimeline.Spike spike in StartupTimeline.Spikes)
            {
                Assert.Greater(spike.Ms, StartupTimeline.SpikeMilliseconds);
            }

            if (StartupTimeline.Spikes.Count > 0)
            {
                StringAssert.Contains("slow frames", report);
                StringAssert.Contains(" rest ", report);
            }

            // a second scene starts a window of its own
            Assert.IsFalse(StartupTimeline.IsOpen);
        }

        [Test]
        public void MeasuringCostsNextToNothing_OpenOrShut_AndNeverOverflows()
        {
            Assert.IsTrue(StartupTimeline.Enabled);
            var clock = Stopwatch.StartNew();
            for (int i = 0; i < 20000; i++)
            {
                using var _ = StartupTimeline.Measure("cost: window shut");
            }

            clock.Stop();
            Assert.Less(clock.ElapsedMilliseconds, 250, "20 000 measurements with the window shut took " + clock.ElapsedMilliseconds + " ms");
            Assert.AreEqual(0, StartupTimeline.Entries.Count, "Pieces were kept with no window open");

            StartupTimeline.Open("cost test");
            clock.Restart();
            for (int i = 0; i < 20000; i++)
            {
                using var _ = StartupTimeline.Measure("cost: window open");
            }

            clock.Stop();
            Assert.Less(clock.ElapsedMilliseconds, 400, "20 000 measurements with the window open took " + clock.ElapsedMilliseconds + " ms");
            Assert.LessOrEqual(StartupTimeline.Entries.Count, 1024, "The list of pieces is not capped");
            StartupTimeline.Close();
            Assert.IsFalse(StartupTimeline.IsOpen);
            StringAssert.Contains("[Startup] cost test", StartupTimeline.LastReport);
        }

        [Test]
        public void Nested_Pieces_KnowTheirDepth_AndTheNameIsKept()
        {
            StartupTimeline.Open("nesting");
            using (StartupTimeline.Measure("outer"))
            {
                using (StartupTimeline.Measure("inner"))
                {
                }
            }

            using (StartupTimeline.Measure("after"))
            {
            }

            var seen = new System.Collections.Generic.Dictionary<string, int>();
            foreach (StartupTimeline.Entry e in StartupTimeline.Entries)
            {
                seen[e.Name] = e.Depth;
            }

            Assert.AreEqual(0, seen["outer"]);
            Assert.AreEqual(1, seen["inner"]);
            Assert.AreEqual(0, seen["after"], "The depth was not given back after the nested piece");
            StartupTimeline.Close();
        }
    }
}
