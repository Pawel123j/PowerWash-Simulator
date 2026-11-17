using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Data;
using Zmywak.Core.Events.Channels;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Shifts;
using Zmywak.UI.HUD;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// The special shifts in the kitchen: a wedding brings more dishes faster and the newlyweds' envelope, an
    /// inspection is failed by a single breakage, and on a Sunday the legendary lasagne tray lands on the pile a
    /// quarter of the way in. The card beside the radio says which night it is.
    /// </summary>
    public class SpecialShiftPlayModeTests
    {
        private ShiftResult _result;
        private ShiftCompletedEventChannel _channel;

        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
            ShiftPlan.Special = SpecialShift.None;
            _result = null;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            ShiftPlan.Special = SpecialShift.None;
            if (_channel != null)
            {
                _channel.OnRaised -= OnShiftCompleted;
            }

            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator AWedding_BringsMoreDishesFaster_AndTheNewlywedsPay()
        {
            ShiftController shift = null;
            yield return LoadKitchen(SpecialShift.Wedding, s => shift = s);
            Assert.AreEqual(SpecialShift.Wedding, shift.Special);
            Assert.AreEqual(SpecialShift.None, ShiftPlan.Special, "The kitchen should take the plan, not leave it for the next shift");

            yield return new WaitForSeconds(3.5f);
            Assert.GreaterOrEqual(shift.DishesSpawnedTotal, 5, "A wedding should open with the counter full");
            SpecialShiftBadge badge = Object.FindAnyObjectByType<SpecialShiftBadge>();
            TMP_Text title = badge.transform.Find("SpecialShiftCard/Title").GetComponent<TMP_Text>();
            Assert.IsTrue(title.gameObject.activeInHierarchy, "No card says it is a wedding");
            Assert.AreEqual("Wesele", title.text);
            TestScreenshot.Capture(Camera.main, "36_special_wedding.png");

            shift.EndShiftNow();
            yield return null;
            Assert.IsNotNull(_result);
            Assert.AreEqual(SpecialShift.Wedding, _result.Special);
            Assert.Greater(_result.SpecialBonus, 0f, "The newlyweds' envelope did not come");
            Assert.AreEqual(_result.BaseWage - _result.IdleDocked + _result.Tips - _result.Deductions + _result.SpecialBonus, _result.Wage, 1e-3f);
            Assert.IsFalse(badge.transform.Find("SpecialShiftCard").gameObject.activeSelf, "The card stays up over the result");
        }

        [UnityTest]
        public IEnumerator AnInspection_IsFailedByASingleBreakage()
        {
            ShiftController shift = null;
            yield return LoadKitchen(SpecialShift.Inspection, s => shift = s);
            var deadline = new PlayDeadline(10f);
            DishController dish = null;
            while ((dish = Object.FindAnyObjectByType<DishController>()) == null && !deadline.Passed)
            {
                yield return null;
            }

            Assert.IsNotNull(dish, "No dish came");
            Assert.IsFalse(shift.InspectionFailed, "Failed before anything happened");
            dish.Shatter(dish.transform.position);
            yield return null;
            Assert.IsTrue(shift.InspectionFailed, "A breakage in front of the inspector should fail it");

            shift.EndShiftNow();
            yield return null;
            Assert.IsFalse(_result.InspectionPassed);
            Assert.AreEqual(0f, _result.SpecialBonus, "A failed inspection earns no thanks");
        }

        [UnityTest]
        public IEnumerator OnASunday_TheLegendaryTray_ComesInAQuarterOfTheWay()
        {
            ShiftController shift = null;
            yield return LoadKitchen(SpecialShift.LasagneTray, s => shift = s);
            Assert.IsFalse(shift.TrayArrived, "The tray came in with the opening dishes");

            Time.timeScale = 8f;
            var deadline = new PlayDeadline(30f);
            while (!shift.TrayArrived && !deadline.Passed)
            {
                yield return null;
            }

            Time.timeScale = 1f;
            Assert.IsTrue(shift.TrayArrived, "The tray never came");
            Assert.GreaterOrEqual(shift.Progress01, ShiftController.TrayArrivesAt);
            Assert.AreEqual("lasagne_tray", shift.LegendaryTray.Definition.Id);
            yield return new WaitForSeconds(1.5f); // it lands on the pile
            TestScreenshot.Capture(Camera.main, "37_special_lasagne_tray.png");

            shift.EndShiftNow();
            yield return null;
            Assert.IsFalse(_result.TrayScrubbed);
            Assert.AreEqual(0f, _result.SpecialBonus, "The whip-round is for a tray scrubbed clean");
        }

        private IEnumerator LoadKitchen(SpecialShift special, System.Action<ShiftController> found)
        {
            ShiftPlan.Special = special;
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;
            var shift = Object.FindAnyObjectByType<ShiftController>();
            _channel = (ShiftCompletedEventChannel)typeof(ShiftController)
                .GetField("shiftCompletedChannel", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(shift);
            _channel.OnRaised += OnShiftCompleted;
            found(shift);
        }

        private void OnShiftCompleted(ShiftResult result)
        {
            _result = result;
        }
    }
}
