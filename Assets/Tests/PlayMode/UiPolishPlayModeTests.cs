using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Zmywak.Core.Data;
using Zmywak.UI.HUD;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>The text of the result screen after the QA fixes: PWS-021 (a count and its noun) and PWS-028 (the quota row of an early exit).</summary>
    [Category("QARegression")]
    public class UiPolishPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            TestSaveSandbox.Enter();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            TestSaveSandbox.DestroyPersistentObjects();
            TestSaveSandbox.Exit();
        }

        private static IEnumerator ResultScreenOf(System.Action<ShiftResultScreen> then)
        {
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return null;
            yield return null;
            var screen = Object.FindAnyObjectByType<ShiftResultScreen>(FindObjectsInactive.Include);
            Assert.IsNotNull(screen, "The kitchen has no result screen");
            then(screen);
            yield return null;
        }

        private static string Shown(ShiftResultScreen screen, ShiftResult result, CareerRecord before = null)
        {
            screen.Show(result, before);
            screen.Complete();
            return screen.Describe();
        }

        // ---- the scenes themselves (PWS-013, 018, 027): what UiPolishScenePatch / the builder put into them -------------------------------

        private static IEnumerator Load(string scene)
        {
            SceneManager.LoadScene(scene, LoadSceneMode.Single);
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator PWS013_ThePileMetersWord_StartsAtTheMetersEdge_AndFitsOnTheScreen([Values(1f, 1.5f)] float textScale)
        {
            Zmywak.Core.Settings.AccessibilitySettings.TextScale = textScale;
            yield return Load("Kitchen_Shift");

            Transform meter = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == "PileMeter") meter = t;
            }

            Assert.IsNotNull(meter, "The kitchen has no pile meter");
            var label = meter.Find("Label") as RectTransform;
            var text = label.GetComponent<TMPro.TMP_Text>();
            Assert.AreEqual("Góra", text.text);

            // Its box and its letters lie inside the HUD's own area, the left edge included (it began 12 units outside).
            var area = (RectTransform)meter.GetComponentInParent<Canvas>().rootCanvas.transform;
            var areaCorners = new Vector3[4];
            var boxCorners = new Vector3[4];
            area.GetWorldCorners(areaCorners);
            label.GetWorldCorners(boxCorners);
            Assert.GreaterOrEqual(boxCorners[0].x, areaCorners[0].x - 0.01f, $"The word's box starts {areaCorners[0].x - boxCorners[0].x:0.0} left of the screen");
            Assert.LessOrEqual(boxCorners[2].x, areaCorners[2].x + 0.01f);
            text.ForceMeshUpdate();
            Assert.LessOrEqual(text.preferredWidth, label.rect.width + 0.5f, "The word is wider than its box at this text size");
            Assert.AreEqual(TMPro.TextAlignmentOptions.BottomLeft, text.alignment);

            // For the eye: the meter only fades in once the pile grows, so for the picture it is shown whatever the dishes are.
            meter.GetComponent<CanvasGroup>().alpha = 1f;
            TestScreenshot.Capture(Camera.main, $"13_pile_meter_word_{textScale.ToString(System.Globalization.CultureInfo.InvariantCulture)}x.png");
        }

        [UnityTest]
        public IEnumerator PWS018_TheSoundSwitch_SaysWhatATickMeans_InEverySceneThatHasSettings([Values("MainMenu", "MetaHub", "Kitchen_Shift")] string scene)
        {
            yield return Load(scene);

            int found = 0;
            foreach (UnityEngine.UI.Toggle toggle in Object.FindObjectsByType<UnityEngine.UI.Toggle>(FindObjectsInactive.Include))
            {
                if (toggle.name != "SoundToggle")
                {
                    continue;
                }

                found++;
                string label = toggle.GetComponentInChildren<TMPro.TMP_Text>(true).text;
                Assert.AreEqual("Dźwięk włączony", label);
                StringAssert.DoesNotContain("wyłączony: ", label, "The label reads as a state, and the opposite one");
            }

            Assert.AreEqual(1, found, $"{scene} should have one sound switch");
        }

        [UnityTest]
        public IEnumerator PWS027_EveryModalCard_SitsOnABackdropDarkEnoughToHideWhatIsBehindIt([Values("MainMenu", "MetaHub", "Kitchen_Shift")] string scene)
        {
            yield return Load(scene);

            int modals = 0;
            foreach (Zmywak.UI.Common.PanelTransition modal in Object.FindObjectsByType<Zmywak.UI.Common.PanelTransition>(FindObjectsInactive.Include))
            {
                var dim = modal.GetComponent<UnityEngine.UI.Image>();
                if (dim == null || modal.name == "PauseMenu")
                {
                    continue; // the pause menu is meant to show the kitchen through it
                }

                modals++;
                Assert.GreaterOrEqual(dim.color.a, 0.9f, $"{scene}/{modal.name}: the backdrop lets {(1f - dim.color.a) * 100f:0}% of the screen behind it show");
            }

            foreach (ShiftResultScreen result in Object.FindObjectsByType<ShiftResultScreen>(FindObjectsInactive.Include))
            {
                Assert.GreaterOrEqual(result.GetComponent<UnityEngine.UI.Image>().color.a, 0.93f, "The result screen shows the kitchen's HUD round its card");
            }

            Assert.Greater(modals, 0, $"{scene} has no modal card to check");
        }

        [UnityTest]
        public IEnumerator PWS027_TheHubsChapterLine_IsDrawnUnderEveryModalBackdrop()
        {
            yield return Load("MetaHub");

            Transform chapter = null;
            foreach (Transform t in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include))
            {
                if (t.name == "ChapterText") chapter = t;
            }

            Assert.IsNotNull(chapter, "The hub has no chapter line");
            int modals = 0;
            foreach (Zmywak.UI.Common.PanelTransition modal in Object.FindObjectsByType<Zmywak.UI.Common.PanelTransition>(FindObjectsInactive.Include))
            {
                if (modal.transform.parent != chapter.parent)
                {
                    continue;
                }

                modals++;
                Assert.Less(chapter.GetSiblingIndex(), modal.transform.GetSiblingIndex(),
                    $"The chapter line is drawn over {modal.name}'s backdrop and shines through it");
            }

            Assert.Greater(modals, 0, "The hub has no modal next to the chapter line to compare with");
        }

        [UnityTest]
        public IEnumerator PWS028_TheQuotaRow_OfAShiftClockedOutEarly_IsAboutTheShareWorked_LikeTheBonusBelowIt()
        {
            string met = null, missed = null, wholeMissed = null, wholeMet = null, none = null;
            yield return ResultScreenOf(screen =>
            {
                var cutShort = new ShiftResult { Quota = 8, DishesCleaned = 1, ElapsedSeconds = 20f, PlannedSeconds = 180f, QuotaBonus = 0.66f };
                met = Shown(screen, cutShort);
                missed = Shown(screen, new ShiftResult { Quota = 8, DishesCleaned = 0, ElapsedSeconds = 20f, PlannedSeconds = 180f });
                wholeMissed = Shown(screen, new ShiftResult { Quota = 8, DishesCleaned = 5, ElapsedSeconds = 180f, PlannedSeconds = 180f });
                wholeMet = Shown(screen, new ShiftResult { Quota = 8, DishesCleaned = 9, ElapsedSeconds = 180f, PlannedSeconds = 180f });
                none = Shown(screen, new ShiftResult { Quota = 0, DishesCleaned = 3, ElapsedSeconds = 180f, PlannedSeconds = 180f });
            });

            StringAssert.Contains("Norma zrobiona za część zmiany", met, "1 dish in 20 of 180 seconds meets a quota of 8 for that share");
            StringAssert.DoesNotContain("brakło 7", met, "The whole night's quota is not what the early exit is judged by");
            StringAssert.Contains("Norma brakło 1 za część zmiany", missed);
            StringAssert.Contains("Norma brakło 3", wholeMissed);
            StringAssert.DoesNotContain("część zmiany", wholeMissed, "A whole shift is judged by the whole quota");
            StringAssert.Contains("Norma zrobiona", wholeMet);
            StringAssert.DoesNotContain("część zmiany", wholeMet);
            StringAssert.Contains("Norma -", none);
        }

        [UnityTest]
        public IEnumerator PWS021_ANewRecordOfDishes_IsWrittenInThePolishFormOfItsCount([Values(22, 23, 24, 25, 32, 40, 21, 12)] int dishes)
        {
            string text = null;
            yield return ResultScreenOf(screen =>
            {
                // a record of dishes, not of score, flow or anything else
                var before = new CareerRecord { shifts = 3, mostDishes = 5, bestScore = 100, bestFlow = 50 };
                text = Shown(screen, new ShiftResult { Quota = 8, DishesCleaned = dishes, ElapsedSeconds = 180f, PlannedSeconds = 180f }, before);
            });

            bool few = dishes % 10 >= 2 && dishes % 10 <= 4 && !(dishes % 100 >= 12 && dishes % 100 <= 14);
            StringAssert.Contains($"Nowy rekord: {dishes} {(few ? "naczynia" : "naczyń")} na zmianie!", text);
        }

        [UnityTest]
        public IEnumerator PWS021_TheCrewsHelp_IsWrittenInThePolishFormOfItsCount([Values(1, 2, 5, 22)] int helped)
        {
            string text = null;
            yield return ResultScreenOf(screen =>
            {
                text = Shown(screen, new ShiftResult { Quota = 8, DishesCleaned = 8, CrewHelped = helped, ElapsedSeconds = 180f, PlannedSeconds = 180f });
            });

            StringAssert.Contains($"Pomoc ekipy +{PolishPlural.Dishes(helped)}", text);
        }
    }
}
