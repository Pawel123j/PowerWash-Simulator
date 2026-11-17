using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zmywak.Core.Save;
using Zmywak.Core.Services;
using Zmywak.Meta.Progress;
using Zmywak.UI.Screens;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// The main menu's save slots: each says where that life stands, a fresh slot starts a new life without touching
    /// the others, and a slot is deleted on a second tap.
    /// </summary>
    public class SaveSlotsPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
        }

        [TearDown]
        public void TearDown()
        {
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator ASecondSlot_StartsANewLife_AndTheFirstKeepsItsOwn()
        {
            SaveSystem.Save(new SaveData { dayNumber = 30, currentDateIso = "2008-10-04T00:00:00", walletEuros = 312.5f, energy = 70f });
            yield return Boot();

            GameObject.Find("LoadGameButton").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(GameObject.Find("SaveSlotsPanel") != null, "Loading did not open the save slots");
            string first = GameObject.Find("SlotButton1").GetComponentInChildren<TMP_Text>().text;
            StringAssert.Contains("dzień 30", first);
            StringAssert.Contains("04.10.2008", first);
            StringAssert.Contains("pusty", GameObject.Find("SlotButton2").GetComponentInChildren<TMP_Text>().text);
            Assert.IsNull(GameObject.Find("DeleteSlotButton2"), "An empty slot has nothing to delete");
            yield return null;
            TestScreenshot.Capture(Camera.main, "40_save_slots.png");

            Object.FindAnyObjectByType<MainMenuScreen>().Play(1);
            var deadline = new PlayDeadline(10f);
            while (SceneManager.GetActiveScene().name != "MetaHub" && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("MetaHub", SceneManager.GetActiveScene().name, "Picking a slot did not go to the hub");
            yield return null;
            Assert.AreEqual(1, SaveSystem.Slot);
            Assert.IsTrue(ServiceLocator.TryGet(out MetaProgressController progress));
            Assert.AreEqual(1, progress.DayNumber, "A fresh slot is a fresh life");
            Assert.AreEqual(30, SaveSystem.Peek(0).dayNumber, "The first slot was not touched");
        }

        [UnityTest]
        public IEnumerator ASlot_IsDeletedOnTheSecondTap()
        {
            SaveSystem.Save(new SaveData { dayNumber = 12 });
            yield return Boot();
            var menu = Object.FindAnyObjectByType<MainMenuScreen>();
            GameObject.Find("LoadGameButton").GetComponent<Button>().onClick.Invoke();

            menu.Delete(0);
            Assert.IsTrue(SaveSystem.HasSave(0), "The first tap only asks");
            StringAssert.Contains("Na pewno", GameObject.Find("DeleteSlotButton1").GetComponentInChildren<TMP_Text>().text);
            menu.Delete(0);
            Assert.IsFalse(SaveSystem.HasSave(0));
            StringAssert.Contains("pusty", GameObject.Find("SlotButton1").GetComponentInChildren<TMP_Text>().text);
        }

        [UnityTest]
        public IEnumerator AFirstStart_OffersOneWayIn_AndANewGameNeedsNoSlotPicked()
        {
            yield return Boot();
            Assert.IsNull(GameObject.Find("MenuContinueButton"), "Nothing to continue on a first start");
            Assert.IsNull(GameObject.Find("LoadGameButton"), "No saves to load on a first start");
            Button start = GameObject.Find("NewGameButton").GetComponent<Button>();
            StringAssert.Contains("Rozpocznij", start.GetComponentInChildren<TMP_Text>().text);
            // The settings close up right under the one way in, not past two empty places.
            var settings = (RectTransform)GameObject.Find("MenuSettingsButton").transform;
            Assert.Less(((RectTransform)start.transform).anchoredPosition.y - settings.anchoredPosition.y, 300f,
                "A gap of two missing buttons between starting and the settings");
            yield return null;
            TestScreenshot.Capture(Camera.main, "41_menu_first_start.png");

            start.onClick.Invoke();
            var deadline = new PlayDeadline(10f);
            while (SceneManager.GetActiveScene().name != "MetaHub" && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("MetaHub", SceneManager.GetActiveScene().name, "A new game did not go to the evening");
            Assert.AreEqual(0, SaveSystem.Slot, "A first game goes into the first slot, no questions asked");
        }

        [UnityTest]
        public IEnumerator WithALifeInProgress_ContinueComesFirst_AndPlaysTheLastSave()
        {
            SaveSystem.Save(new SaveData { dayNumber = 9, currentDateIso = "2004-05-09T00:00:00", walletEuros = 80f, energy = 70f });
            yield return Boot();
            Button continueButton = GameObject.Find("MenuContinueButton")?.GetComponent<Button>();
            Assert.IsNotNull(continueButton, "No CONTINUE with a save on the disk");
            StringAssert.Contains("Dzień 9", GameObject.Find("ContinueDetails").GetComponent<TMP_Text>().text);
            Assert.IsNotNull(GameObject.Find("LoadGameButton"), "The saves can still be picked");
            yield return null;
            // Where the life stands is read on its own: no other line of the menu written over it.
            Rect details = WorldRect(GameObject.Find("ContinueDetails").GetComponent<RectTransform>());
            foreach (TMP_Text other in Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Exclude))
            {
                if (other.gameObject.name != "ContinueDetails" && !string.IsNullOrEmpty(other.text) &&
                    other.GetComponentInParent<Button>() == null && other.canvas == continueButton.GetComponentInParent<Canvas>())
                {
                    Assert.IsFalse(details.Overlaps(WorldRect(other.rectTransform)), $"\"{other.text}\" is written over where the life stands");
                }
            }

            TestScreenshot.Capture(Camera.main, "42_menu_continue.png");

            continueButton.onClick.Invoke();
            var deadline = new PlayDeadline(10f);
            while (SceneManager.GetActiveScene().name != "MetaHub" && !deadline.Passed)
            {
                yield return null;
            }

            yield return null;
            Assert.IsTrue(ServiceLocator.TryGet(out MetaProgressController progress));
            Assert.AreEqual(9, progress.DayNumber, "CONTINUE did not pick up the life where it was");
        }

        private static Rect WorldRect(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        private static IEnumerator Boot()
        {
            Scene previouslyActive = SceneManager.GetActiveScene(); // a MainMenu left by the test before is not Boot's
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            var deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu") && !deadline.Passed)
            {
                yield return null;
            }

            yield return null;
            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name);
        }
    }
}
