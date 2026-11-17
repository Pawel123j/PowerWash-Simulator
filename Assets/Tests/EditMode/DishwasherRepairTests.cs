using System;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Kitchen.Dishwasher;

namespace Zmywak.Tests
{
    public class DishwasherRepairTests
    {
        [TestCase(DishwasherFault.CloggedFilter, typeof(CloggedFilterRepair))]
        [TestCase(DishwasherFault.StuckSprayArm, typeof(StuckSprayArmRepair))]
        [TestCase(DishwasherFault.TrippedReset, typeof(TrippedResetRepair))]
        public void Create_GivesTheRepairForTheFault_NotYetDone(DishwasherFault fault, Type expected)
        {
            DishwasherRepair repair = DishwasherRepair.Create(fault, seed: 3);

            Assert.IsInstanceOf(expected, repair);
            Assert.AreEqual(fault, repair.Fault);
            Assert.AreEqual(0f, repair.Progress01);
            Assert.IsFalse(repair.IsDone);
        }

        [Test]
        public void Filter_ScrapsSitInsideTheFilter_AndCanBeFound()
        {
            var filter = new CloggedFilterRepair(seed: 11);

            Assert.Greater(filter.ScrapCount, 2);
            for (int i = 0; i < filter.ScrapCount; i++)
            {
                Vector2 position = filter.ScrapPosition(i);
                Assert.Less(position.magnitude + filter.ScrapSize(i), 0.9f, $"Scrap {i} hangs over the rim");
                Assert.AreNotEqual(-1, filter.ScrapAt(position), $"Scrap {i} cannot be grabbed at its own centre");
            }

            Assert.AreEqual(-1, filter.ScrapAt(new Vector2(0.95f, 0.95f)), "Found a scrap outside the filter");
        }

        [Test]
        public void Filter_ShortNudgeLeavesTheScrap_AFlickClearsIt()
        {
            var filter = new CloggedFilterRepair(seed: 5);
            int steps = 0;
            filter.Stepped += () => steps++;

            Assert.IsFalse(filter.Flick(0, Vector2.right * CloggedFilterRepair.FlickDistance * 0.5f));
            Assert.IsFalse(filter.IsCleared(0));

            Assert.IsTrue(filter.Flick(0, Vector2.right * CloggedFilterRepair.FlickDistance));
            Assert.IsTrue(filter.IsCleared(0));
            Assert.AreEqual(1f / filter.ScrapCount, filter.Progress01, 1e-5f);
            Assert.AreEqual(1, steps);

            Assert.IsFalse(filter.Flick(0, Vector2.up), "A scrap came off twice");
            Assert.AreNotEqual(0, filter.ScrapAt(filter.ScrapPosition(0)), "A cleared scrap can still be grabbed");
        }

        [Test]
        public void Filter_IsFixedOnceEveryScrapIsOff()
        {
            var filter = new CloggedFilterRepair(seed: 8);
            for (int i = 0; i < filter.ScrapCount; i++)
            {
                Assert.IsFalse(filter.IsDone);
                filter.Flick(i, Vector2.left);
            }

            Assert.IsTrue(filter.IsDone);
        }

        [Test]
        public void SprayArm_IsStiffAtFirst_AndLoosensAsItTurns()
        {
            var arm = new StuckSprayArmRepair();

            arm.Turn(90f);
            Assert.Less(arm.ArmAngle, 60f, "A seized arm should not follow the finger one to one");
            float firstFollow = arm.ArmAngle / 90f;

            for (int i = 0; i < 6; i++)
            {
                arm.Turn(90f);
            }

            float before = arm.ArmAngle;
            arm.Turn(90f);
            Assert.Greater((arm.ArmAngle - before) / 90f, firstFollow, "The arm did not loosen up");
        }

        [Test]
        public void SprayArm_RockingBackAndForthFreesItToo()
        {
            var arm = new StuckSprayArmRepair();
            int steps = 0;
            arm.Stepped += () => steps++;

            for (int i = 0; i < 200 && !arm.IsDone; i++)
            {
                arm.Turn(i % 2 == 0 ? 60f : -60f);
            }

            Assert.IsTrue(arm.IsDone);
            Assert.AreEqual(Mathf.RoundToInt(StuckSprayArmRepair.DegreesToFree / 90f), steps, "One ratchet click per quarter turn");
        }

        [Test]
        public void Reset_SpringsBackWhenLetGoEarly_AndStaysInOnceHome()
        {
            var reset = new TrippedResetRepair();
            int steps = 0;
            reset.Stepped += () => steps++;

            reset.Hold(TrippedResetRepair.HoldSeconds * 0.6f);
            Assert.IsTrue(reset.IsHeld);
            float partway = reset.Progress01;
            reset.Release(0.1f);
            Assert.IsFalse(reset.IsHeld);
            Assert.Less(reset.Progress01, partway, "Letting go should lose ground");

            for (int i = 0; i < 100 && !reset.IsDone; i++)
            {
                reset.Hold(0.05f);
            }

            Assert.IsTrue(reset.IsDone);
            Assert.AreEqual(1, steps, "The breaker should click in exactly once");
            reset.Release(1f);
            Assert.IsTrue(reset.IsDone, "A reset breaker popped back out");
        }

        [Test]
        public void Schedule_ScriptedCycleAlwaysBreaks_FirstCycleNeverByChance()
        {
            var schedule = new DishwasherBreakdownSchedule(scriptedCycle: 2, chancePerCycle: 1f, maxBreakdowns: 5);

            Assert.IsFalse(schedule.WillBreak(1, roll01: 0f), "The very first cycle broke down by chance");
            Assert.IsTrue(schedule.WillBreak(2, roll01: 0.99f), "The scripted cycle did not break");
            Assert.IsTrue(schedule.WillBreak(3, roll01: 0.5f));
        }

        [Test]
        public void Schedule_ChanceAndCapAreRespected()
        {
            var schedule = new DishwasherBreakdownSchedule(scriptedCycle: 0, chancePerCycle: 0.2f, maxBreakdowns: 2);

            Assert.IsFalse(schedule.WillBreak(2, roll01: 0.5f));
            Assert.IsTrue(schedule.WillBreak(3, roll01: 0.1f));
            Assert.IsTrue(schedule.WillBreak(4, roll01: 0.1f));
            Assert.IsFalse(schedule.WillBreak(5, roll01: 0f), "More breakdowns than the shift allows");
            Assert.AreEqual(2, schedule.Breakdowns);
        }

        [Test]
        public void Schedule_KeepsBreakdownsApart_SoTheyNeverComeInAString()
        {
            var schedule = new DishwasherBreakdownSchedule(scriptedCycle: 0, chancePerCycle: 1f, maxBreakdowns: 5, minCyclesBetween: 3);

            Assert.IsTrue(schedule.WillBreak(2, roll01: 0f));
            for (int cycle = 3; cycle <= 5; cycle++)
            {
                Assert.IsFalse(schedule.WillBreak(cycle, roll01: 0f), $"Cycle {cycle} broke down right after the last breakdown");
            }

            Assert.IsTrue(schedule.WillBreak(6, roll01: 0f), "Three clean cycles later it may break again");
            Assert.AreEqual(2, schedule.Breakdowns);
        }

        [Test]
        public void Schedule_NeverMeansNever()
        {
            for (int cycle = 1; cycle < 20; cycle++)
            {
                Assert.IsFalse(DishwasherBreakdownSchedule.Never.WillBreak(cycle, 0f));
            }
        }
    }
}
