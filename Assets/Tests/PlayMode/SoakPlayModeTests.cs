using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Telemetry;
using Zmywak.UI.Dev;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// The development autoplay that drives the soak test on a phone. A short run in the kitchen keeps it
    /// working as the game changes; the long one (explicit: <c>-testFilter Soak</c>) is the soak itself in the
    /// Editor - many shifts and evenings in a row, watched for errors, climbing memory and piling-up objects.
    /// The autoplay feeds its own virtual mouse to the real Input System (no test fixture), so the settings
    /// let input through while the batch-mode Editor has no focus.
    /// </summary>
    public class SoakPlayModeTests
    {
        private InputSettings.BackgroundBehavior _savedBackground;
        private InputSettings.EditorInputBehaviorInPlayMode _savedEditorInput;

        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
            // The two settings changed for the test and put back after it (as GameTourPlayModeTests does). Swapping the
            // whole settings object broke with Input System 1.20 (Unity 6.6): the object kept to restore could be gone by
            // the teardown, and the settings setter now refuses null.
            _savedBackground = InputSystem.settings.backgroundBehavior;
            _savedEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        }

        [TearDown]
        public void TearDown()
        {
            if (DevAutoplay.Instance != null)
            {
                Object.Destroy(DevAutoplay.Instance.gameObject);
            }

            InputSystem.settings.backgroundBehavior = _savedBackground;
            InputSystem.settings.editorInputBehaviorInPlayMode = _savedEditorInput;
            Time.timeScale = 1f;
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator Autoplay_WashesAndRacksDishes_ThroughTheRealInputPath()
        {
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;

            DevAutoplay autoplay = DevAutoplay.Begin(1f);
            var deadline = new PlayDeadline(90f);
            while (!autoplay.Finished && !deadline.Passed)
            {
                yield return null;
            }

            Assert.IsTrue(autoplay.Finished, "Autoplay never finished its minute");
            Assert.IsNull(autoplay.LastProblem);
            Assert.GreaterOrEqual(autoplay.DishesRacked, 1, "A minute in the kitchen and not one dish on the rack");
            Assert.GreaterOrEqual(autoplay.Monitor.Samples, 1);
            Assert.IsNotEmpty(GameplayLog.Read<SoakLogEntry>("soak"), "No soak sample in the gameplay diary");
        }

        [UnityTest, Explicit("The soak itself: 15 minutes (ZMYWAK_SOAK_MINUTES). Run with -testFilter Soak."), Category("Soak")]
        [Timeout(3_600_000)]
        public IEnumerator Soak_ShiftAfterShift_WithoutErrorsLeaksOrSlowdown()
        {
            float minutes = 15f;
            string fromEnvironment = System.Environment.GetEnvironmentVariable("ZMYWAK_SOAK_MINUTES");
            if (!string.IsNullOrEmpty(fromEnvironment))
            {
                minutes = float.Parse(fromEnvironment, System.Globalization.CultureInfo.InvariantCulture);
            }

            // From the very start, as a player would: boot, menu, the hub's evening, then work.
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null;
            DevAutoplay autoplay = DevAutoplay.Begin(minutes);
            var deadline = new PlayDeadline(minutes * 60f + 180f);
            while (!autoplay.Finished && !deadline.Passed)
            {
                yield return null;
            }

            List<SoakLogEntry> samples = GameplayLog.Read<SoakLogEntry>("soak");
            var table = new StringBuilder($"Soak, {minutes:0} min: {autoplay.ShiftsPlayed} shifts, {autoplay.DishesRacked} dishes racked, {autoplay.RepairsDone} repairs\n");
            table.AppendLine("minute  scene          fps   worst ms  slow   alloc MB  managed MB  textures  meshes");
            foreach (SoakLogEntry s in samples)
            {
                table.AppendLine($"{s.minute,6:0.0}  {s.scene,-13} {s.averageFps,5:0} {s.worstFrameMs,9:0} {s.slowFrameShare,5:P0} {s.allocatedMB,9:0} {s.managedMB,11:0} {s.textures,9} {s.meshes,7}");
            }

            List<ShiftLogEntry> shifts = GameplayLog.Read<ShiftLogEntry>("shift");
            int breakdowns = 0;
            int repairs = 0;
            table.AppendLine("shift  minutes  arrived  cleaned  broken  cycles  breakdowns  repairs  fps");
            for (int i = 0; i < shifts.Count; i++)
            {
                ShiftLogEntry s = shifts[i];
                breakdowns += s.breakdowns;
                repairs += s.repairs.Count;
                table.AppendLine($"{i + 1,5}  {s.elapsedSeconds / 60f,7:0.0}  {s.dishesArrived,7}  {s.dishesCleaned,7}  {s.dishesBroken,6}  " +
                                 $"{s.dishwasherCycles,6}  {s.breakdowns,10}  {s.repairs.Count,7}  {s.averageFps,3:0}");
            }

            TestContext.WriteLine(table.ToString());
            Debug.Log(table.ToString());

            Assert.IsTrue(autoplay.Finished, "The soak never finished");
            Assert.AreEqual(autoplay.ShiftsPlayed, shifts.Count, "Every finished shift is in the gameplay diary");
            // Every shift breaks the machine on its second cycle; one striking just before a shift ends may stay unfixed.
            Assert.Greater(breakdowns, 0, "The machine never broke down - the soak did not reach a second cycle");
            Assert.Greater(repairs, 0, "The machine broke down and was never repaired");
            Assert.GreaterOrEqual(repairs, breakdowns - shifts.Count, "Breakdowns stayed unrepaired");
            Assert.IsNull(autoplay.LastProblem);
            Assert.GreaterOrEqual(autoplay.ShiftsPlayed, Mathf.FloorToInt(minutes / 5f), "Too few shifts for the time played");
            Assert.Greater(autoplay.DishesRacked, autoplay.ShiftsPlayed, "The shifts went by without washing");

            // Leaks: compare like with like - the first and last samples taken in the kitchen.
            SoakLogEntry firstKitchen = samples.Find(s => s.scene == "Kitchen_Shift");
            SoakLogEntry lastKitchen = samples.FindLast(s => s.scene == "Kitchen_Shift");
            Assert.IsNotNull(firstKitchen, "No sample was taken in the kitchen");
            Assert.Less(lastKitchen.allocatedMB - firstKitchen.allocatedMB, 64f, "Memory keeps climbing shift after shift");
            Assert.Less(lastKitchen.textures - firstKitchen.textures, 40, "Textures pile up shift after shift");
            Assert.Less(lastKitchen.meshes - firstKitchen.meshes, 40, "Meshes pile up shift after shift");
        }
    }
}
