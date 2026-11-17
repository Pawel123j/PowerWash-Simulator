using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Graphics;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>Loads the non-Kitchen scenes and captures a screenshot of each for a visual sanity check
    /// (camera framing, missing objects, unreadable UI, pink/missing shaders).</summary>
    public class MenuScenesPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter(); // fresh-save state: deterministic screenshots, real save untouched
        }

        [TearDown]
        public void TearDown()
        {
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator BootScene_Loads_WithoutErrors()
        {
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Camera camera = Camera.main;
            if (camera != null)
            {
                TestScreenshot.Capture(camera, "06_boot.png");
            }
        }

        [UnityTest]
        public IEnumerator MainMenuScene_Loads_AndRenders()
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Camera camera = Camera.main;
            Assert.IsNotNull(camera, "MainMenu scene has no main camera");
            TestScreenshot.Capture(camera, "07_main_menu.png");
        }

        [UnityTest]
        public IEnumerator MetaHubScene_Loads_AndRenders()
        {
            // Through Boot first, like the real game: the hub reads wallet/stats/intro state from the
            // persistent progress controllers Boot creates (here backed by the sandboxed fresh save).
            // The previous test may have left a MainMenu scene active - compare scene handles so that one
            // isn't mistaken for the MainMenu that Boot's state machine loads.
            Scene previouslyActive = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            var deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu")
                   && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("MainMenu", SceneManager.GetActiveScene().name, "Boot did not hand over to MainMenu");

            SceneManager.LoadScene("MetaHub", LoadSceneMode.Single);
            deadline = new PlayDeadline(10f);
            while (SceneManager.GetActiveScene().name != "MetaHub" && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("MetaHub", SceneManager.GetActiveScene().name, "MetaHub did not load");
            yield return null; // let Start() fill in the hub readouts and trigger the intro

            Camera camera = Camera.main;
            Assert.IsNotNull(camera, "MetaHub scene has no main camera");
            TestScreenshot.Capture(camera, "08_meta_hub.png");

            // On a fresh save the first Chapter 1 scene covers the hub (StoryBeatTrigger hides the hub actions).
            // Swap them for a second capture of the hub layout itself. (StoryPlayModeTests plays the scenes.)
            GameObject dialoguePanel = GameObject.Find("DialoguePanel");
            if (dialoguePanel != null)
            {
                dialoguePanel.SetActive(false);
                Transform actions = dialoguePanel.transform.parent.Find("Actions");
                if (actions != null)
                {
                    actions.gameObject.SetActive(true);
                }

                yield return null;
                TestScreenshot.Capture(camera, "09_meta_hub_without_dialogue.png");
            }
        }

        /// <summary>
        /// Not a check - a picture for judging the art: Kuba's room behind the main menu, at a tall phone's shape,
        /// as a car's headlights cross the ceiling. Explicit, so a normal run skips it (Logs/Screenshots/art_room_car.png).
        /// </summary>
        [UnityTest, Explicit]
        public IEnumerator ArtPreview_Room_AsACarGoesBy()
        {
            SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
            yield return null;

            var sweep = Object.FindAnyObjectByType<LightSweep>();
            Assert.IsNotNull(sweep, "No passing car in the room");
            var deadline = new PlayDeadline(30f);
            while (sweep.Progress < 0.45f && !deadline.Passed) // halfway across: its brightest
            {
                yield return null;
            }

            Assert.GreaterOrEqual(sweep.Progress, 0.45f, "No car went by within 30 s");
            TestScreenshot.Capture(Camera.main, "art_room_car.png", 1080, 2340);
        }
    }
}
