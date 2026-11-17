using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Save;
using Zmywak.UI.Screens;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// The room behind the hub is the one Kuba sleeps in: his own, or - evicted after weeks in arrears - a hostel
    /// dormitory with bunks and lockers.
    /// </summary>
    public class RoomDressingPlayModeTests
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
        public IEnumerator AtHome_HisOwnBed()
        {
            yield return BootThenHub();
            RoomDressing room = Object.FindAnyObjectByType<RoomDressing>();
            Assert.IsNotNull(room, "The hub's room has no RoomDressing");
            Assert.IsFalse(room.ShowsHostel);
            Assert.IsNotNull(GameObject.Find("HomeBed"), "His bed is not there");
        }

        [UnityTest]
        public IEnumerator InTheHostel_ADormitory()
        {
            SaveSystem.Save(new SaveData
            {
                dayNumber = 30, currentDateIso = "2004-05-30T00:00:00", walletEuros = -40f, weeksInArrears = 3, inHostel = true, energy = 60f,
                // The story told, so no scene plays over the room.
                storyFlags = new System.Collections.Generic.List<string>
                    { "ch1_complete", "ch2_complete", "ch3_complete", "ch4_complete", "ch5_complete", "ch6_complete" }
            });
            yield return BootThenHub();

            RoomDressing room = Object.FindAnyObjectByType<RoomDressing>();
            Assert.IsTrue(room.ShowsHostel, "Evicted, Kuba still sleeps in his own room");
            Assert.IsNull(GameObject.Find("HomeBed"), "His own bed in a hostel dormitory");
            Assert.IsNotNull(GameObject.Find("BunkUpperBlanket"));
            yield return null;
            TestScreenshot.Capture(Camera.main, "38_hostel_dorm.png");
        }

        private static IEnumerator BootThenHub()
        {
            Scene previouslyActive = SceneManager.GetActiveScene(); // a MainMenu left by the test before is not Boot's
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            var deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu") && !deadline.Passed)
            {
                yield return null;
            }

            SceneManager.LoadScene("MetaHub", LoadSceneMode.Single);
            yield return null;
            yield return null;
            Assert.AreEqual("MetaHub", SceneManager.GetActiveScene().name);
        }
    }
}
