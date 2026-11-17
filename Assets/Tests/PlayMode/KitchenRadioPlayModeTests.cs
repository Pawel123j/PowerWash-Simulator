using System.Collections;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Radio;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>The radio on the kitchen wall: on the trad station to begin with, a tap turns the dial round, the news
    /// reads the day's headlines, and where the dial was left is where it is next shift.</summary>
    public class KitchenRadioPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter(); // settings (the radio's station among them) under test keys, from defaults
        }

        [TearDown]
        public void TearDown()
        {
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        [UnityTest]
        public IEnumerator TheRadio_StartsOnTrad_ATapTurnsTheDial_AndTheChoiceStays()
        {
            yield return LoadKitchen();
            var radio = Object.FindAnyObjectByType<KitchenRadio>();
            Assert.IsNotNull(radio, "The kitchen has no radio");
            Assert.AreEqual(RadioStation.Trad, radio.Station);
            Assert.IsTrue(radio.IsOn);

            Button dial = FindButton("RadioButton");
            TMP_Text station = dial.transform.Find("Dial/Station").GetComponent<TMP_Text>();
            StringAssert.Contains("Trad", station.text);

            dial.onClick.Invoke();
            Assert.AreEqual(RadioStation.Pop, radio.Station);
            Assert.AreEqual(RadioStations.Programme(RadioStation.Pop), radio.Caption);

            dial.onClick.Invoke();
            Assert.AreEqual(RadioStation.News, radio.Station);
            CollectionAssert.Contains(RadioNews.For(new System.DateTime(2004, 5, 1)), radio.Caption,
                "The news should read the headlines of the day (a kitchen on its own is on 1 May 2004)");
            yield return new WaitForSecondsRealtime(1f); // the headline crawls
            TestScreenshot.Capture(Camera.main, "35_kitchen_radio.png");

            dial.onClick.Invoke();
            Assert.AreEqual(RadioStation.Off, radio.Station);
            Assert.IsFalse(radio.IsOn);
            StringAssert.Contains("wyłączone", station.text);
            Assert.AreEqual((int)RadioStation.Off, AccessibilitySettings.RadioStation);

            // Next shift: still off.
            yield return LoadKitchen();
            Assert.AreEqual(RadioStation.Off, Object.FindAnyObjectByType<KitchenRadio>().Station, "The dial did not stay where it was left");
        }

        private static IEnumerator LoadKitchen()
        {
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        private static Button FindButton(string name)
        {
            foreach (Button button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button.name == name)
                {
                    return button;
                }
            }

            Assert.Fail($"No {name}");
            return null;
        }
    }
}
