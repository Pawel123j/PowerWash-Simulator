// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using Zmywak.Core.Data;

namespace Zmywak.Tests
{
    public class ShiftScoringTests
    {
        private static ShiftPerformance Shift(int cleaned, int quota = 8, int broken = 0, float accuracy = 1f, int flow = 0, int urgent = 0, int urgentMet = 0, int collapses = 0, int drops = 0)
        {
            return new ShiftPerformance
            {
                DishesCleaned = cleaned,
                Quota = quota,
                DishesBroken = broken,
                WashAccuracy = accuracy,
                BestFlow = flow,
                UrgentOrders = urgent,
                UrgentOrdersMet = urgentMet,
                PileCollapses = collapses,
                RackDropsMissed = drops
            };
        }

        [Test]
        public void MeetingTheQuota_WithoutBreakingAnything_IsAtLeastAB_EvenForAClumsyBeginner()
        {
            ShiftScore beginner = ShiftScoring.Score(Shift(5, 5, 0, 0f));
            Assert.GreaterOrEqual(beginner.Grade, ShiftGrade.B, $"Score {beginner.Total}");
        }

        [Test]
        public void TheWorstShift_IsAD_NotLower_AndTheScoreNeverGoesNegative()
        {
            ShiftScore disaster = ShiftScoring.Score(Shift(0, 8, 30, 0f, 0, 5, 0, 9, 20));
            Assert.AreEqual(0, disaster.Total);
            Assert.AreEqual(ShiftGrade.D, disaster.Grade);
        }

        [Test]
        public void GoodWashing_IsClearlyRewarded()
        {
            ShiftScore average = ShiftScoring.Score(Shift(8, 8, 1, 0.8f, 2, 2, 1));
            ShiftScore skilled = ShiftScoring.Score(Shift(12, 8, 0, 0.95f, 4, 2, 2));
            ShiftScore novice = ShiftScoring.Score(Shift(4, 8, 2, 0.6f, 1, 1));
            Assert.AreEqual(ShiftGrade.S, skilled.Grade, $"Skilled: {skilled.Total}");
            Assert.That(average.Grade, Is.EqualTo(ShiftGrade.B).Or.EqualTo(ShiftGrade.C), $"Average: {average.Total}");
            Assert.AreEqual(ShiftGrade.D, novice.Grade, $"Novice: {novice.Total}");
            Assert.Greater(skilled.Total - novice.Total, 50);
        }

        [Test]
        public void BeatingTheQuota_EarnsTheLastPoints_AndMorePastThatEarnsNothing()
        {
            ShiftScore met = ShiftScoring.Score(Shift(8, 8, 0, 0.8f, 2));
            ShiftScore beaten = ShiftScoring.Score(Shift(10, 8, 0, 0.8f, 2));
            ShiftScore farBeyond = ShiftScoring.Score(Shift(30, 8, 0, 0.8f, 2));
            Assert.AreEqual(50.0, met.Work, 0.0010000000474974513);
            Assert.AreEqual(60.0, beaten.Work, 0.0010000000474974513, "A quarter over the quota earns it all");
            Assert.AreEqual(beaten.Work, farBeyond.Work, 0.0010000000474974513);
            Assert.AreEqual(25.0, ShiftScoring.Score(Shift(4)).Work, 0.0010000000474974513, "Half the quota, half the points");
        }

        [Test]
        public void Breakages_AndACollapsedPile_AreTakenOff_ButCapped()
        {
            Assert.AreEqual((float)ShiftScoring.Score(Shift(8)).Total - 8f, ShiftScoring.Score(Shift(8, 8, 1)).Total, 1.0);
            Assert.AreEqual(24.0, ShiftScoring.Score(Shift(8, 8, 10)).Penalties, 0.0010000000474974513);
            Assert.AreEqual(36.0, ShiftScoring.Score(Shift(8, 8, 10, 1f, 0, 0, 0, 10)).Penalties, 0.0010000000474974513);
        }

        [Test]
        public void UrgentOrders_CountWhenThereAreAny_AndAShiftWithoutThemLosesNothing()
        {
            Assert.AreEqual(15.0, ShiftScoring.Score(Shift(8)).Orders, 0.0010000000474974513);
            Assert.AreEqual(15.0, ShiftScoring.Score(Shift(8, 8, 0, 1f, 0, 3, 3)).Orders, 0.0010000000474974513);
            Assert.AreEqual(5.0, ShiftScoring.Score(Shift(8, 8, 0, 1f, 0, 3, 1)).Orders, 0.0010000000474974513);
            Assert.AreEqual(0.0, ShiftScoring.Score(Shift(8, 8, 0, 1f, 0, 2)).Orders, 0.0010000000474974513);
        }

        [Test]
        public void Care_IsTheRightToolForTheDirt_AndDishesPutOnTheRackNotBesideIt()
        {
            Assert.AreEqual(15.0, ShiftScoring.Score(Shift(8)).Care, 0.0010000000474974513);
            Assert.AreEqual(7.5, ShiftScoring.Score(Shift(8, 8, 0, 0.5f)).Care, 0.0010000000474974513);
            Assert.AreEqual(11.0, ShiftScoring.Score(Shift(8, 8, 0, 1f, 0, 0, 0, 0, 2)).Care, 0.0010000000474974513);
            Assert.AreEqual(0.0, ShiftScoring.Score(Shift(8, 8, 0, 0f, 0, 0, 0, 0, 5)).Care, 0.0010000000474974513);
        }

        [TestCase(100, ShiftGrade.S)]
        [TestCase(90, ShiftGrade.S)]
        [TestCase(89, ShiftGrade.A)]
        [TestCase(75, ShiftGrade.A)]
        [TestCase(74, ShiftGrade.B)]
        [TestCase(60, ShiftGrade.B)]
        [TestCase(59, ShiftGrade.C)]
        [TestCase(40, ShiftGrade.C)]
        [TestCase(39, ShiftGrade.D)]
        [TestCase(0, ShiftGrade.D)]
        public void Grades_FollowTheThresholds(int total, ShiftGrade grade)
        {
            Assert.AreEqual(grade, ShiftScoring.GradeFor(total));
        }

        [Test]
        public void ClockingOutEarly_AsksForThatShareOfTheQuota()
        {
            Assert.AreEqual(8, ShiftScoring.EffectiveQuota(8, 1f));
            Assert.AreEqual(4, ShiftScoring.EffectiveQuota(8, 0.5f));
            Assert.AreEqual(1, ShiftScoring.EffectiveQuota(8, 0.01f), "At least one dish");
            Assert.AreEqual(0, ShiftScoring.EffectiveQuota(0, 1f), "No quota stays no quota");
        }
    }
}
