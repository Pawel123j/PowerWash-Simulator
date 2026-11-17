using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Kitchen.Prep;
using Zmywak.Kitchen.Shifts;
using Zmywak.Narrative.Progress;
using Zmywak.UI.HUD;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// The board before the shift, for a Kuba who took the knife: the hub sends him to prep first, the kitchen waits
    /// for it (no clock, no dishes), and what even prep earns is paid with the shift.
    /// </summary>
    public class PrepBoardPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
            ShiftPlan.PrepBeforeShift = false;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            ShiftPlan.PrepBeforeShift = false;
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator OnTheBoard_TheShiftWaitsForThePrep_AndPaysForIt()
        {
            ShiftPlan.PrepBeforeShift = true;
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var shift = Object.FindAnyObjectByType<ShiftController>();
            var panel = Object.FindAnyObjectByType<PrepBoardPanel>();
            Assert.IsFalse(ShiftPlan.PrepBeforeShift, "The kitchen should take the plan, not leave it lying for the next shift");
            Assert.IsTrue(shift.AwaitingPrep, "The shift did not wait for the board");
            yield return null;
            Assert.IsTrue(panel.IsOpen, "The board's card did not open");

            float clock = shift.RemainingSeconds;
            yield return new WaitForSeconds(1.5f);
            Assert.AreEqual(0, shift.DishesSpawnedTotal, "Dishes came in while Kuba was still at the board");
            Assert.AreEqual(clock, shift.RemainingSeconds, 1e-3f, "The shift clock ran during the prep");

            ShiftResult result = null;
            var channel = (ShiftCompletedEventChannel)typeof(ShiftController)
                .GetField("shiftCompletedChannel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shift);
            System.Action<ShiftResult> onCompleted = r => result = r;
            channel.OnRaised += onCompleted;
            try
            {
                // Cut whenever the vegetable reaches the mark, a little faster than life.
                Time.timeScale = 3f;
                bool shot = false;
                var deadline = new PlayDeadline(60f);
                while (!panel.Round.IsFinished && !deadline.Passed)
                {
                    if (panel.Round.FedMm >= PrepRound.TargetSliceMm)
                    {
                        panel.TryCut();
                    }

                    if (!shot && panel.Round.Slices >= 10 && panel.Round.FedMm > 1.5f)
                    {
                        TestScreenshot.Capture(Camera.main, "33_prep_board.png");
                        shot = true;
                    }

                    yield return null;
                }

                Time.timeScale = 1f;
                Assert.IsTrue(panel.Round.IsFinished, "The prep never finished");
                Assert.Greater(panel.Round.Evenness, 0.9f, "Cutting at the mark should give even cubes");
                yield return null;
                TestScreenshot.Capture(Camera.main, "34_prep_result.png");

                float bonus = panel.Round.BonusEuros;
                Assert.Greater(bonus, 0f);
                panel.Continue();
                Assert.IsFalse(panel.IsOpen);
                Assert.IsFalse(shift.AwaitingPrep, "\"Do zlewu\" did not start the shift");

                deadline = new PlayDeadline(10f);
                while (shift.DishesSpawnedTotal == 0 && !deadline.Passed)
                {
                    yield return null;
                }

                Assert.Greater(shift.DishesSpawnedTotal, 0, "No dishes came once the prep was done");
                yield return new WaitForSeconds(0.3f);
                Assert.Less(shift.RemainingSeconds, clock, "The shift clock did not start after the prep");

                shift.EndShiftNow();
                yield return null;
                Assert.IsNotNull(result, "Ending the shift reported nothing");
                Assert.AreEqual(bonus, result.PrepBonus, 1e-3f, "The board's bonus did not reach the pay");
                Assert.AreEqual(result.BaseWage - result.IdleDocked + result.Tips - result.Deductions + bonus, result.Wage, 1e-3f);
            }
            finally
            {
                channel.OnRaised -= onCompleted;
            }
        }

        [UnityTest]
        public IEnumerator FromTheHub_KubaWhoTookTheBoard_GoesToPrepFirst()
        {
            Scene previous = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            var deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previous || SceneManager.GetActiveScene().name != "MainMenu") && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Boot did not hand over to MainMenu");
            Object.FindAnyObjectByType<NarrativeProgressController>().SetFlag("ch3_at_board");

            SceneManager.LoadScene("MetaHub", LoadSceneMode.Single);
            yield return null;
            yield return null;
            Button work = null;
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button.name == "GoToWorkButton")
                {
                    work = button;
                }
            }

            Assert.IsNotNull(work, "The hub has no work button");
            StringAssert.Contains("deska", work.GetComponentInChildren<TMP_Text>(true).text, "The work button should say the board comes first");
            work.onClick.Invoke();

            deadline = new PlayDeadline(10f);
            while (SceneManager.GetActiveScene().name != "Kitchen_Shift" && !deadline.Passed)
            {
                yield return null;
            }

            yield return null;
            yield return null;
            Assert.AreEqual("Kitchen_Shift", SceneManager.GetActiveScene().name);
            Assert.IsTrue(Object.FindAnyObjectByType<ShiftController>().AwaitingPrep, "The shift did not open at the board");
            Assert.IsTrue(Object.FindAnyObjectByType<PrepBoardPanel>().IsOpen);
        }
    }
}
