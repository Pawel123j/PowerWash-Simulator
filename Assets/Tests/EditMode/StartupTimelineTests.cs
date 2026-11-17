using NUnit.Framework;
using Zmywak.Core.Diagnostics;

namespace Zmywak.Tests
{
    /// <summary>The measuring does nothing where it has no frames to hang on to (EditMode: no play, no main-thread id, no window).</summary>
    [Category("QARegression")]
    public class StartupTimelineTests
    {
        [SetUp]
        public void SetUp()
        {
            StartupTimeline.Reset();
        }

        [Test]
        public void OutsidePlayMode_MeasuringIsANoOp()
        {
            Assert.DoesNotThrow(() =>
            {
                using (StartupTimeline.Measure("edit mode piece"))
                {
                }

                StartupTimeline.Mark("edit mode mark", StartupTimeline.Now);
                StartupTimeline.Open("edit mode");
            });

            Assert.AreEqual(0, StartupTimeline.Entries.Count);
            Assert.IsFalse(StartupTimeline.IsOpen);
            Assert.AreEqual(string.Empty, StartupTimeline.LastReport);
        }

        [Test]
        public void ClosingAWindowThatWasNeverOpened_IsHarmless()
        {
            Assert.DoesNotThrow(() => StartupTimeline.Close());
            Assert.AreEqual(string.Empty, StartupTimeline.LastReport);
        }
    }
}
