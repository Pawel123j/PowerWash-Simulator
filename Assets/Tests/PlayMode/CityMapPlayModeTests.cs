using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.UI.Screens;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>The hub's map: Dublin with the places of Kuba's life on it, a pin that sends him to work.</summary>
    public class CityMapPlayModeTests
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

        [Test]
        public void NoTwoPinsYouCanTap_Overlap()
        {
            // The map is drawn 900x1200; a pin's touch target is 130 across (48 dp on a dense 5-inch phone).
            foreach (Place place in new[] { Place.Dublin, Place.Krakow, Place.Pilbara })
            {
                MapPin[] pins = CityMap.Pins(place);
                for (int a = 0; a < pins.Length; a++)
                {
                    for (int b = a + 1; b < pins.Length; b++)
                    {
                        if (pins[a].Action == MapAction.None || pins[b].Action == MapAction.None)
                        {
                            continue;
                        }

                        float dx = Mathf.Abs(pins[a].U - pins[b].U) * 900f;
                        float dy = Mathf.Abs(pins[a].V - pins[b].V) * 1200f;
                        Assert.IsTrue(dx >= 130f || dy >= 130f, $"{place}: '{pins[a].Label}' and '{pins[b].Label}' share a touch target");
                    }
                }
            }
        }

        [UnityTest]
        public IEnumerator TheMap_ShowsTheCity_AndAPinGoesToWork()
        {
            // The story told, so no scene plays over the hub.
            SaveSystem.Save(new SaveData
            {
                dayNumber = 40, currentDateIso = "2011-01-08T00:00:00", walletEuros = 120f, energy = 80f,
                storyFlags = new System.Collections.Generic.List<string>
                    { "ch1_complete", "ch2_complete", "ch3_complete", "ch4_complete", "ch5_complete", "ch6_complete" }
            });
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

            CityMapPanel map = Object.FindAnyObjectByType<CityMapPanel>();
            Assert.IsNotNull(map, "The hub has no map");
            GameObject.Find("MapToggleButton").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(map.IsOpen);
            Assert.AreEqual(CityMap.Pins(Place.Dublin).Length, map.PinObjects.Count);
            yield return null;
            TestScreenshot.Capture(Camera.main, "39_city_map.png");

            map.Tap(MapAction.Work);
            deadline = new PlayDeadline(10f);
            while (SceneManager.GetActiveScene().name != "Kitchen_Shift" && !deadline.Passed)
            {
                yield return null;
            }

            Assert.AreEqual("Kitchen_Shift", SceneManager.GetActiveScene().name, "The restaurant's pin did not send Kuba to work");
        }
    }
}
