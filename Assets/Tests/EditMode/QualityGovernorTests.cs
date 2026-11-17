// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using NUnit.Framework;
using Zmywak.Core.Graphics;

namespace Zmywak.Tests
{
    public class QualityGovernorTests
    {
        private static List<(float time, QualityTier tier)> Play(QualityGovernor governor, float seconds, Func<QualityTier, float> frameAt, float startTime = 0f)
        {
            List<(float, QualityTier)> changes = new List<(float, QualityTier)>();
            float time = startTime;
            while (time < startTime + seconds)
            {
                float frame = frameAt(governor.Tier);
                time += frame;
                if (governor.Frame(frame))
                {
                    changes.Add((time, governor.Tier));
                }
            }
            return changes;
        }

        private static float AtTheCap(QualityTier tier)
        {
            return 1f / (float)QualityGovernor.TargetFrameRate(tier);
        }

        [Test]
        public void OneHitch_DoesNotLowerTheQuality()
        {
            QualityGovernor governor = new QualityGovernor(QualityTier.High, QualityTier.High);
            Play(governor, 10f, AtTheCap);
            Assert.IsFalse(governor.Frame(1f), "A one-second frame (a scene load, a GC pause)");
            Assert.IsEmpty(Play(governor, 20f, AtTheCap));
            Assert.AreEqual(QualityTier.High, governor.Tier);
        }

        [Test]
        public void ARunOfHitches_ShorterThanHalfAWindow_DoesNotLowerItEither()
        {
            QualityGovernor governor = new QualityGovernor(QualityTier.High, QualityTier.High);
            int frame = 0;
            Assert.IsEmpty(Play(governor, 30f, (QualityTier tier) => (++frame % 10 != 0) ? (1f / 60f) : 0.12f));
        }

        [Test]
        public void SustainedSlowFrames_StepDownOneTierAtATime_AfterTwoSlowWindows()
        {
            List<(float, QualityTier)> changes = Play(new QualityGovernor(QualityTier.High, QualityTier.High), 30f, (QualityTier tier) => 0.03f);
            Assert.AreEqual(2, changes.Count, "High -> Medium -> Low, then it holds");
            Assert.AreEqual(QualityTier.Medium, changes[0].Item2);
            Assert.AreEqual(QualityTier.Low, changes[1].Item2);
            Assert.That(changes[0].Item1, Is.InRange(5.9f, 6.1f), "Two slow windows, not one");
        }

        [Test]
        public void BelowItsStartingTier_ItTriesAgain_AndStaysUpWhenTheDeviceCopes()
        {
            QualityGovernor governor = new QualityGovernor(QualityTier.High, QualityTier.High);
            List<(float, QualityTier)> down = Play(governor, 7f, (QualityTier tier) => 0.03f);
            Assert.AreEqual(1, down.Count);
            Assert.AreEqual(QualityTier.Medium, governor.Tier);
            List<(float, QualityTier)> changes = Play(governor, 120f, AtTheCap, 7f);
            Assert.AreEqual(1, changes.Count);
            Assert.AreEqual(QualityTier.High, changes[0].Item2);
            Assert.That(changes[0].Item1 - down[0].Item1, Is.InRange(59.9f, 63.1f));
        }

        [Test]
        public void AtLowsFrameCap_ItIsNotStuck_AsTheOldAverageWas()
        {
            QualityGovernor governor = new QualityGovernor(QualityTier.Low, QualityTier.High);
            List<(float, QualityTier)> changes = Play(governor, 150f, AtTheCap);
            Assert.AreEqual(QualityTier.High, governor.Tier, "30 fps at Low is Low doing its job, not a slow device");
            Assert.AreEqual(2, changes.Count);
        }

        [Test]
        public void ATryThatFails_ComesBackDown_AndTheNextWaitsTwiceAsLong()
        {
            QualityGovernor governor = new QualityGovernor(QualityTier.Low, QualityTier.Medium);
            List<(float time, QualityTier tier)> list = Play(governor, 500f, (QualityTier qualityTier) => (qualityTier != QualityTier.Low) ? 0.025f : (1f / 30f));
            List<float> tries = new List<float>();
            foreach (var (time, tier) in list)
            {
                if (tier == QualityTier.Medium)
                {
                    tries.Add(time);
                }
                else
                {
                    Assert.AreEqual(QualityTier.Low, tier);
                }
            }
            Assert.GreaterOrEqual(tries.Count, 3, "Keeps trying, now and then");
            float firstGap = tries[1] - tries[0];
            Assert.Greater(tries[2] - tries[1], firstGap * 1.6f, "Each failed try waits about twice as long");
            Assert.AreEqual(QualityTier.Low, governor.Tier);
        }

        [Test]
        public void TheWaitNeverGrowsPastEightMinutes()
        {
            QualityGovernor governor = new QualityGovernor(QualityTier.Low, QualityTier.Medium);
            Play(governor, 3600f, (QualityTier tier) => (tier != QualityTier.Low) ? 0.025f : (1f / 30f));
            Assert.LessOrEqual(governor.RetryAfterSeconds, 480f);
            Assert.AreEqual(480f, governor.RetryAfterSeconds);
        }
    }
}
