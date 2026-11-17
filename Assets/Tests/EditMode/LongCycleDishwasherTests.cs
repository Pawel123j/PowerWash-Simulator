// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using NUnit.Framework;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Dishwasher;

namespace Zmywak.Tests
{
    public class LongCycleDishwasherTests
    {
        private static readonly DateTime Start = new DateTime(2026, 10, 1, 18, 0, 0, DateTimeKind.Utc);

        private static readonly TimeSpan HalfAnHour = TimeSpan.FromMinutes(30.0);

        [Test]
        public void TakesDishesUpToItsCapacity_AndNoneOnceStarted()
        {
            LongCycleDishwasher machine = new LongCycleDishwasher(3, HalfAnHour);
            Assert.IsTrue(machine.TryLoad("plate"));
            Assert.IsTrue(machine.TryLoad("pan"));
            Assert.IsTrue(machine.TryLoad("glass"));
            Assert.IsFalse(machine.TryLoad("plate"), "A full machine took another dish");
            Assert.AreEqual(3, machine.Count);
            LongCycleDishwasher longCycleDishwasher = new LongCycleDishwasher(3, HalfAnHour);
            longCycleDishwasher.TryLoad("plate");
            Assert.IsTrue(longCycleDishwasher.TryStart(Start));
            Assert.IsFalse(longCycleDishwasher.TryLoad("pan"), "A dish went into a running machine");
        }

        [Test]
        public void AnEmptyMachine_DoesNotStart()
        {
            LongCycleDishwasher longCycleDishwasher = new LongCycleDishwasher(40, HalfAnHour);
            Assert.IsFalse(longCycleDishwasher.TryStart(Start));
            Assert.IsFalse(longCycleDishwasher.IsStarted);
        }

        [Test]
        public void WashesForHalfAnHourOfRealTime()
        {
            LongCycleDishwasher machine = new LongCycleDishwasher(40, HalfAnHour);
            machine.TryLoad("plate");
            machine.TryStart(Start);
            DateTime almost = Start + HalfAnHour - TimeSpan.FromSeconds(1.0);
            Assert.IsTrue(machine.IsRunning(almost));
            Assert.IsFalse(machine.IsDone(almost));
            Assert.AreEqual(1.0, machine.Remaining(almost).TotalSeconds, 1E-06);
            Assert.AreEqual(0, machine.Unload(almost).Count, "It unloaded before the cycle was over");
            Assert.IsTrue(machine.IsDone(Start + HalfAnHour));
            Assert.IsFalse(machine.IsRunning(Start + HalfAnHour));
            Assert.AreEqual(TimeSpan.Zero, machine.Remaining(Start + HalfAnHour));
        }

        [Test]
        public void UnloadingGivesTheWholeLoad_AndEmptiesTheMachineForTheNext()
        {
            LongCycleDishwasher machine = new LongCycleDishwasher(40, HalfAnHour);
            machine.TryLoad("plate");
            machine.TryLoad("glass");
            machine.TryStart(Start);
            CollectionAssert.AreEqual(new string[2] { "plate", "glass" }, machine.Unload(Start + TimeSpan.FromHours(2.0)));
            Assert.AreEqual(0, machine.Count);
            Assert.IsFalse(machine.IsStarted);
            Assert.IsTrue(machine.TryLoad("pan"), "The emptied machine would not take the next load");
        }

        [Test]
        public void KeepsWashing_WhileTheGameIsClosed()
        {
            LongCycleDishwasher longCycleDishwasher = new LongCycleDishwasher(40, HalfAnHour);
            longCycleDishwasher.TryLoad("plate");
            longCycleDishwasher.TryLoad("pan");
            longCycleDishwasher.TryStart(Start);
            SaveData save = new SaveData();
            longCycleDishwasher.WriteTo(save);
            LongCycleDishwasher reopened = new LongCycleDishwasher(40, HalfAnHour);
            reopened.ReadFrom(save);
            Assert.AreEqual(2, reopened.Count);
            Assert.IsTrue(reopened.IsRunning(Start + TimeSpan.FromMinutes(10.0)));
            Assert.IsTrue(reopened.IsDone(Start + TimeSpan.FromMinutes(40.0)));
            Assert.AreEqual(2, reopened.Unload(Start + TimeSpan.FromMinutes(40.0)).Count);
        }

        [Test]
        public void ALoadWaitingToStart_SurvivesASave_StillUnstarted()
        {
            LongCycleDishwasher longCycleDishwasher = new LongCycleDishwasher(40, HalfAnHour);
            longCycleDishwasher.TryLoad("plate");
            SaveData save = new SaveData();
            longCycleDishwasher.WriteTo(save);
            LongCycleDishwasher reopened = new LongCycleDishwasher(40, HalfAnHour);
            reopened.ReadFrom(save);
            Assert.AreEqual(1, reopened.Count);
            Assert.IsFalse(reopened.IsStarted);
            Assert.IsTrue(reopened.CanLoad);
        }

        [Test]
        public void AClockSetBack_DoesNotFinishTheCycle()
        {
            LongCycleDishwasher machine = new LongCycleDishwasher(40, HalfAnHour);
            machine.TryLoad("plate");
            machine.TryStart(Start);
            DateTime earlier = Start - TimeSpan.FromHours(3.0);
            Assert.IsTrue(machine.IsRunning(earlier));
            Assert.IsFalse(machine.IsDone(earlier));
            Assert.AreEqual(HalfAnHour, machine.Remaining(earlier));
        }

        [Test]
        public void AnOldSave_WithoutTheMachine_LoadsItEmpty()
        {
            LongCycleDishwasher machine = new LongCycleDishwasher(40, HalfAnHour);
            machine.ReadFrom(new SaveData
            {
                bigDishwasherLoad = null
            });
            Assert.AreEqual(0, machine.Count);
            Assert.IsFalse(machine.IsStarted);
        }
    }
}
