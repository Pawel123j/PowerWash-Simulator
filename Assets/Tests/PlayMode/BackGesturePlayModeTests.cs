using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zmywak.Core.Save;
using Zmywak.UI.Common;
using Zmywak.UI.Screens;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// The Android back gesture outside the kitchen (the kitchen's is in KitchenShiftPlayModeTests): it closes the card on top,
    /// and with nothing open it does nothing at all - it must not quit the game from under the player. These go through the
    /// scenes as the setup built them, so a card added to a scene but left off the list of what back closes is caught here.
    /// </summary>
    public class BackGesturePlayModeTests
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

        private static IEnumerator OpenScene(string sceneName)
        {
            Scene previouslyActive = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            var deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu") && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Boot did not hand over to the main menu");
            if (sceneName != "MainMenu")
            {
                SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
                yield return null;
                yield return null;
                Assert.AreEqual(sceneName, SceneManager.GetActiveScene().name);
            }
        }

        private static GameObject[] PanelsOf(BackClosesPanels back) =>
            (GameObject[])typeof(BackClosesPanels).GetField("panels", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(back);

        [UnityTest]
        public IEnumerator InTheMainMenu_Back_ClosesTheCardOnTop_AndWithNoneOpen_AsksBeforeLeavingTheGame()
        {
            SaveSystem.Save(new SaveData { dayNumber = 5 }); // a save: the list of saves is on the menu
            yield return OpenScene("MainMenu");

            var back = Object.FindAnyObjectByType<BackClosesPanels>();
            Assert.IsNotNull(back, "The main menu does not handle the back gesture");
            GameObject[] panels = PanelsOf(back);
            CollectionAssert.AreEquivalent(new[] { "QuitPanel", "SettingsPanel", "SaveSlotsPanel" },
                System.Array.ConvertAll(panels, p => p.name), "A card of the menu is missing from what back closes");

            GameObject.Find("LoadGameButton").GetComponent<Button>().onClick.Invoke();
            GameObject slots = System.Array.Find(panels, p => p.name == "SaveSlotsPanel");
            Assert.IsTrue(slots.activeSelf, "The list of saves did not open");
            back.Back();
            Assert.IsFalse(slots.activeSelf, "Back did not close the list of saves");

            // Nothing open: back asks whether to leave - it never quits from under the player.
            GameObject quit = System.Array.Find(panels, p => p.name == "QuitPanel");
            back.Back();
            Assert.IsTrue(quit.activeSelf, "Back with nothing open did not ask about leaving the game");
            yield return null;
            TestScreenshot.Capture(Camera.main, "43_menu_quit_question.png");
            back.Back();
            Assert.IsFalse(quit.activeSelf, "Back did not close the question");
        }

        [UnityTest]
        public IEnumerator InTheHub_Back_ClosesEachCardTopmostFirst_TheMapIncluded()
        {
            // The story told, so no scene plays over the hub (as in CityMapPlayModeTests).
            SaveSystem.Save(new SaveData
            {
                dayNumber = 40, currentDateIso = "2011-01-08T00:00:00", walletEuros = 120f, energy = 80f,
                storyFlags = new List<string>
                    { "ch1_complete", "ch2_complete", "ch3_complete", "ch4_complete", "ch5_complete", "ch6_complete" }
            });
            yield return OpenScene("MetaHub");

            var back = Object.FindAnyObjectByType<BackClosesPanels>();
            var map = Object.FindAnyObjectByType<CityMapPanel>();
            Assert.IsNotNull(back, "The hub does not handle the back gesture");
            Assert.IsFalse(back.CloseTopmost(), "Back with nothing open did something");

            // The map, the way the player opens it.
            GameObject.Find("MapToggleButton").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(map.IsOpen);
            Assert.IsTrue(back.CloseTopmost());
            Assert.IsFalse(map.IsOpen, "Back did not close the map");

            // Every card the hub has is on the list, and they close one by one from the top.
            GameObject[] panels = PanelsOf(back);
            CollectionAssert.AreEquivalent(new[] { "HistoryPanel", "SettingsPanel", "HubMenuPanel", "ShopPanel", "MapPanel", "SlangPanel" },
                System.Array.ConvertAll(panels, p => p.name), "A card of the hub is missing from what back closes");
            foreach (GameObject panel in panels)
            {
                panel.SetActive(true);
            }

            for (int i = 0; i < panels.Length; i++)
            {
                if (!panels[i].activeInHierarchy)
                {
                    continue; // the scene's history lives in the dialogue, closed here
                }

                Assert.IsTrue(back.CloseTopmost(), $"Back closed nothing with {panels.Length - i} cards still open");
                Assert.IsFalse(panels[i].activeSelf, $"Back did not close {panels[i].name} while it was the topmost open card");
            }

            // With nothing open, back opens the evening's menu - the way back to the main menu.
            GameObject menu = System.Array.Find(panels, p => p.name == "HubMenuPanel");
            back.Back();
            Assert.IsTrue(menu.activeSelf, "Back with the hub showing did not open its menu");
            Assert.IsTrue(GameObject.Find("HubMenuMainMenuButton").GetComponent<Button>().interactable, "No way to the main menu");
            yield return null;
            TestScreenshot.Capture(Camera.main, "44_hub_menu.png");
            GameObject.Find("HubMenuMainMenuButton").GetComponent<Button>().onClick.Invoke();
            var deadline = new PlayDeadline(10f);
            while (SceneManager.GetActiveScene().name != "MainMenu" && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "The hub's menu did not lead back to the main menu");
        }
    }
}
