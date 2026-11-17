using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Zmywak.Core.Settings;
using Zmywak.Kitchen.Dishes;
using Zmywak.UI.Accessibility;

namespace Zmywak.Tests.PlayMode
{
    /// <summary>
    /// Screen shapes and fingers: the kitchen and the hub captured at common phone proportions and a
    /// tablet. At each shape every visible control has to be fully on screen and clear of the others (and of
    /// the kitchen's hint and gauges); every control is checked against the 48 dp minimum touch target on
    /// real phone screens, and the kitchen's working controls to sit where a thumb reaches in portrait.
    /// </summary>
    public class LayoutPlayModeTests
    {
        private static readonly (string name, int width, int height)[] ScreenShapes =
        {
            ("16x9", 1080, 1920),
            ("18x9", 1080, 2160),
            ("19_5x9", 1080, 2340),
            ("20x9", 1080, 2400),
            ("21x9", 1080, 2520),
            ("4x3_tablet", 1536, 2048)
        };

        /// <summary>Phones a control has to be comfortable on: width, height (px) and density (dpi).</summary>
        private static readonly (string name, int width, int height, float dpi)[] Phones =
        {
            ("1080x1920 @ 420 dpi (older 5\")", 1080, 1920, 420f),
            ("1080x2400 @ 400 dpi (typical 6.5\")", 1080, 2400, 400f),
            ("720x1600 @ 270 dpi (budget 6.5\")", 720, 1600, 270f)
        };

        private const float MinTouchTargetDp = 48f;
        private const float ThumbZone = 0.45f; // bottom share of a portrait screen a thumb reaches without shuffling the grip
        private const float Tolerance = 1f; // canvas units: touching edges are fine

        // Kitchen read-outs a control must not sit on (or they on it).
        private static readonly string[] KitchenReadouts = { "StageHint", "RackFill", "PileMeter" };

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
        public IEnumerator Kitchen_AtEveryScreenShape_ControlsFitAndAreBigEnough_AndWithinThumbReach()
        {
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.5f); // a couple of dishes in the pile

            Transform canvas = GameObject.Find("Canvas").transform;
            var problems = new List<string>();
            foreach ((string name, int width, int height) in ScreenShapes)
            {
                TestScreenshot.Capture(Camera.main, $"20_kitchen_{name}.png", width, height,
                    () => problems.AddRange(LayoutProblems(canvas, name, KitchenReadouts)));
            }

            Assert.IsEmpty(problems, "Kitchen layout:\n" + string.Join("\n", problems));
            AssertTouchTargets("Kitchen_Shift");

            foreach (string control in new[] { "Tool_soak", "Tool_sponge", "Tool_steel_wool", "Tool_scraper", "Tool_shower", "StartDishwasherButton" })
            {
                var rect = GameObject.Find(control).GetComponent<RectTransform>();
                float heightShare = CenterHeightShare(rect);
                Assert.Less(heightShare, ThumbZone, $"{control} sits at {heightShare:P0} of the screen height - out of thumb reach");
            }
        }

        /// <summary>
        /// With a scraped dish in the sink, two more buttons come up left of it, stacked over the toolbar: soak it,
        /// or put it in the big dishwasher. They have to fit beside the pile meter and the hint at every shape too.
        /// </summary>
        [UnityTest]
        public IEnumerator Kitchen_WithADishInTheSink_ItsButtonsFitAtEveryScreenShape()
        {
            AccessibilitySettings.RelaxedMode = true;
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.5f);

            DishController dish = Object.FindAnyObjectByType<DishController>();
            Assert.IsNotNull(dish, "No dish in the kitchen");
            Assert.IsTrue(dish.TryClaim(), "The dish could not be taken to the sink");
            // The scrape is a swipe; here it is only the state that matters.
            var instance = (DishInstance)typeof(DishController)
                .GetField("_instance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .GetValue(dish);
            instance.Scrape();
            float deadline = Time.time + 3f;
            while (dish.IsMoving && Time.time < deadline)
            {
                yield return null;
            }

            yield return null;
            Assert.IsTrue(GameObject.Find("SoakButton") != null, "No soak button for the scraped dish in the sink");
            Assert.IsTrue(GameObject.Find("BigDishwasherLoadButton") != null, "No big dishwasher button for the dish in the sink");

            Transform canvas = GameObject.Find("Canvas").transform;
            var problems = new List<string>();
            foreach ((string name, int width, int height) in ScreenShapes)
            {
                TestScreenshot.Capture(Camera.main, $"20_kitchen_sink_buttons_{name}.png", width, height,
                    () => problems.AddRange(LayoutProblems(canvas, name, KitchenReadouts)));
            }

            Assert.IsEmpty(problems, "Kitchen layout with a dish in the sink:\n" + string.Join("\n", problems));
            foreach (string control in new[] { "SoakButton", "BigDishwasherLoadButton" })
            {
                float heightShare = CenterHeightShare(GameObject.Find(control).GetComponent<RectTransform>());
                Assert.Less(heightShare, ThumbZone, $"{control} sits at {heightShare:P0} of the screen height - out of thumb reach");
            }
        }

        [UnityTest]
        public IEnumerator Hub_AtEveryScreenShape_ControlsFitAndAreBigEnough()
        {
            yield return OpenHubWithoutTheStory();
            Transform safeArea = GameObject.Find("Canvas").transform.Find("SafeArea");
            GameObject settings = safeArea.Find("SettingsPanel").gameObject;
            var problems = new List<string>();
            foreach ((string name, int width, int height) in ScreenShapes)
            {
                TestScreenshot.Capture(Camera.main, $"21_hub_{name}.png", width, height,
                    () => problems.AddRange(LayoutProblems(safeArea, name)));
            }

            // An open panel covers the hub on purpose: its own controls have to fit together.
            settings.SetActive(true);
            yield return null;
            foreach ((string name, int width, int height) in ScreenShapes)
            {
                TestScreenshot.Capture(Camera.main, $"22_hub_settings_{name}.png", width, height,
                    () => problems.AddRange(LayoutProblems(settings.transform, name)));
            }

            settings.SetActive(false);

            Assert.IsEmpty(problems, "Hub layout:\n" + string.Join("\n", problems));
            AssertTouchTargets("MetaHub");
        }

        [UnityTest]
        public IEnumerator Kitchen_AtTheLargestTextSize_TextsGrow_AndStayInsideTheirBoxes()
        {
            SceneManager.LoadScene("Kitchen_Shift", LoadSceneMode.Single);
            yield return new WaitForSeconds(1.5f);

            yield return AssertLargestTextSizeFits("Kitchen_Shift");
        }

        [UnityTest]
        public IEnumerator Hub_AtTheLargestTextSize_TextsGrow_AndStayInsideTheirBoxes()
        {
            yield return OpenHubWithoutTheStory();

            yield return AssertLargestTextSizeFits("MetaHub");
        }

        private const float LargestTextScale = 1.5f; // the settings slider's top

        /// <summary>
        /// Turns the text-size setting to its largest and checks the scene's scaled texts: some visibly grow (the
        /// setting does something), and none - at any of the screen shapes - spills out of its box over its
        /// neighbours.
        /// </summary>
        private static IEnumerator AssertLargestTextSizeFits(string sceneName)
        {
            var before = new Dictionary<TMP_Text, float>();
            foreach (TMP_Text text in ScaledTexts(sceneName))
            {
                text.ForceMeshUpdate();
                before[text] = text.fontSize;
            }

            Assert.IsNotEmpty(before, $"No text in {sceneName} follows the text-size setting");

            AccessibilitySettings.TextScale = LargestTextScale;
            yield return null; // TextScaler applies it in Update
            yield return null;

            var problems = new List<string>();
            int grew = 0;
            foreach (KeyValuePair<TMP_Text, float> pair in before)
            {
                pair.Key.ForceMeshUpdate();
                if (pair.Key.fontSize > pair.Value + 0.5f)
                {
                    grew++;
                }
            }

            foreach ((string name, int width, int height) in ScreenShapes)
            {
                TestScreenshot.Capture(Camera.main, $"23_{sceneName}_largest_text_{name}.png", width, height,
                    () => problems.AddRange(TextOverflows(sceneName, name)));
            }

            Assert.Greater(grew, before.Count / 3, $"The largest text size barely changed {sceneName}: {grew} of {before.Count} texts grew");
            Assert.IsEmpty(problems, $"{sceneName} at the largest text size:\n" + string.Join("\n", problems));        }

        /// <summary>The scene's visible, non-empty texts that follow the text-size setting.</summary>
        private static List<TMP_Text> ScaledTexts(string sceneName)
        {
            var texts = new List<TMP_Text>();
            foreach (TextScaler scaler in Object.FindObjectsByType<TextScaler>(FindObjectsInactive.Exclude))
            {
                var text = scaler.GetComponent<TMP_Text>();
                if (scaler.gameObject.scene.name == sceneName && text.isActiveAndEnabled && !string.IsNullOrEmpty(text.text))
                {
                    texts.Add(text);
                }
            }

            return texts;
        }

        /// <summary>Scaled texts whose rendered lines run past their own box (as laid out right now).</summary>
        private static List<string> TextOverflows(string sceneName, string shape)
        {
            var problems = new List<string>();
            var drawn = new List<(TMP_Text text, Rect rect)>();
            foreach (TMP_Text text in ScaledTexts(sceneName))
            {
                text.ForceMeshUpdate();
                Bounds bounds = text.textBounds;
                Rect box = text.rectTransform.rect;
                if (bounds.size.x > box.width + TextTolerance || bounds.size.y > box.height + TextTolerance)
                {
                    problems.Add($"[{shape}] {text.name} \"{text.text}\" at {text.fontSize:0} pt needs {bounds.size.x:0}x{bounds.size.y:0} in a {box.width:0}x{box.height:0} box");
                }

                if (bounds.size.x > 0f && !string.IsNullOrWhiteSpace(text.text))
                {
                    // Where the letters actually are, in world space, a tolerance in from each edge.
                    Vector3 min = text.rectTransform.TransformPoint(bounds.min);
                    Vector3 max = text.rectTransform.TransformPoint(bounds.max);
                    float inset = TextTolerance * text.rectTransform.lossyScale.x;
                    drawn.Add((text, Rect.MinMaxRect(min.x + inset, min.y + inset, max.x - inset, max.y - inset)));
                }
            }

            // Two texts each inside its own box can still run into each other when the boxes overlap (the hub's date and
            // wallet did at the largest size): the letters themselves must not touch.
            for (int i = 0; i < drawn.Count; i++)
            {
                for (int j = i + 1; j < drawn.Count; j++)
                {
                    if (drawn[i].rect.Overlaps(drawn[j].rect))
                    {
                        problems.Add($"[{shape}] \"{drawn[i].text.text}\" runs into \"{drawn[j].text.text}\"");
                    }
                }
            }

            return problems;
        }

        private const float TextTolerance = 2f; // canvas units

        private static IEnumerator OpenHubWithoutTheStory()
        {
            yield return BootToMainMenu();
            SceneManager.LoadScene("MetaHub", LoadSceneMode.Single);
            yield return null;
            yield return null;

            // Today's story scene covers the hub; look at the hub itself.
            GameObject dialogue = GameObject.Find("DialoguePanel");
            if (dialogue != null)
            {
                dialogue.SetActive(false);
                dialogue.transform.parent.Find("Actions")?.gameObject.SetActive(true);
            }

            yield return null;
        }

        // ------------------------------------------------------------------

        /// <summary>
        /// Visible controls under <paramref name="root"/> that stick out past the screen's edges, overlap each
        /// other, or overlap one of the named read-outs - as laid out right now (inside a capture, at its size).
        /// </summary>
        private static List<string> LayoutProblems(Transform root, string shape, params string[] readoutNames)
        {
            var problems = new List<string>();
            var canvasRect = (RectTransform)root.GetComponentInParent<Canvas>().rootCanvas.transform;
            Rect screen = canvasRect.rect;

            var controls = new List<(Selectable control, Rect rect)>();
            foreach (Selectable control in root.GetComponentsInChildren<Selectable>(false))
            {
                controls.Add((control, RectOnCanvas((RectTransform)control.transform, canvasRect)));
            }

            foreach ((Selectable control, Rect rect) in controls)
            {
                if (rect.xMin < screen.xMin - Tolerance || rect.xMax > screen.xMax + Tolerance ||
                    rect.yMin < screen.yMin - Tolerance || rect.yMax > screen.yMax + Tolerance)
                {
                    problems.Add($"[{shape}] {control.name} runs off the screen ({Describe(rect)} on a {screen.width:0}x{screen.height:0} canvas)");
                }
            }

            for (int i = 0; i < controls.Count; i++)
            {
                for (int j = i + 1; j < controls.Count; j++)
                {
                    Transform a = controls[i].control.transform;
                    Transform b = controls[j].control.transform;
                    if (!a.IsChildOf(b) && !b.IsChildOf(a) && Overlap(controls[i].rect, controls[j].rect))
                    {
                        problems.Add($"[{shape}] {a.name} overlaps {b.name}");
                    }
                }
            }

            foreach (string readoutName in readoutNames)
            {
                Transform readout = FindDeep(root, readoutName);
                if (readout == null || !readout.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Rect readoutRect = RectOnCanvas((RectTransform)readout, canvasRect);
                foreach ((Selectable control, Rect rect) in controls)
                {
                    if (Overlap(rect, readoutRect))
                    {
                        problems.Add($"[{shape}] {control.name} overlaps {readoutName}");
                    }
                }
            }

            return problems;
        }

        private static Rect RectOnCanvas(RectTransform rect, RectTransform canvasRect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector2 min = canvasRect.InverseTransformPoint(corners[0]);
            Vector2 max = canvasRect.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(min.x, max.x), Mathf.Min(min.y, max.y), Mathf.Max(min.x, max.x), Mathf.Max(min.y, max.y));
        }

        private static bool Overlap(Rect a, Rect b)
        {
            return Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > Tolerance &&
                   Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > Tolerance;
        }

        private static string Describe(Rect rect) => $"x {rect.xMin:0}..{rect.xMax:0}, y {rect.yMin:0}..{rect.yMax:0}";

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == name)
                {
                    return child;
                }
            }

            return null;
        }

        /// <summary>Every control of the scene - including those on closed panels - is at least 48 dp both ways on each phone.</summary>
        private static void AssertTouchTargets(string sceneName)
        {
            var tooSmall = new List<string>();
            foreach (Selectable control in Object.FindObjectsByType<Selectable>(FindObjectsInactive.Include))
            {
                if (control.gameObject.scene.name != sceneName)
                {
                    continue;
                }

                Canvas canvas = control.GetComponentInParent<Canvas>(true).rootCanvas;
                Vector2 size = ReferenceSize(control, canvas);
                foreach ((string phone, int width, int height, float dpi) in Phones)
                {
                    float dpPerUnit = CanvasScale(canvas.GetComponent<CanvasScaler>(), width, height) * 160f / dpi;
                    float smallestDp = Mathf.Min(size.x, size.y) * dpPerUnit;
                    if (smallestDp < MinTouchTargetDp - 0.5f)
                    {
                        tooSmall.Add($"{control.name} {size.x:0}x{size.y:0} = {smallestDp:0} dp on {phone}");
                        break;
                    }
                }
            }

            Assert.IsEmpty(tooSmall, $"Touch targets under {MinTouchTargetDp} dp in {sceneName}:\n" + string.Join("\n", tooSmall));
        }

        /// <summary>The control's size in the canvas's reference units (what the layout is authored in).</summary>
        private static Vector2 ReferenceSize(Selectable control, Canvas canvas)
        {
            var rect = (RectTransform)control.transform;
            Vector3 relativeScale = new Vector3(
                rect.lossyScale.x / canvas.transform.lossyScale.x,
                rect.lossyScale.y / canvas.transform.lossyScale.y, 1f);
            return Vector2.Scale(rect.rect.size, relativeScale);
        }

        /// <summary>How many screen pixels one reference unit becomes on a screen of this size - the same
        /// sum the canvas's CanvasScaler does, so the check follows whatever the scenes are set to.</summary>
        private static float CanvasScale(CanvasScaler scaler, int width, int height)
        {
            Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode, $"{scaler.name}: expected to scale with the screen");
            Vector2 reference = scaler.referenceResolution;
            switch (scaler.screenMatchMode)
            {
                case CanvasScaler.ScreenMatchMode.Expand:
                    return Mathf.Min(width / reference.x, height / reference.y);
                case CanvasScaler.ScreenMatchMode.Shrink:
                    return Mathf.Max(width / reference.x, height / reference.y);
                default:
                    float logWidth = Mathf.Log(width / reference.x, 2f);
                    float logHeight = Mathf.Log(height / reference.y, 2f);
                    return Mathf.Pow(2f, Mathf.Lerp(logWidth, logHeight, scaler.matchWidthOrHeight));
            }
        }

        /// <summary>Where the control's centre is, as a share of the screen height from the bottom.</summary>
        private static float CenterHeightShare(RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>().rootCanvas;
            var canvasRect = (RectTransform)canvas.transform;
            Vector3 local = canvasRect.InverseTransformPoint(rect.TransformPoint(rect.rect.center));
            return (local.y - canvasRect.rect.yMin) / canvasRect.rect.height;
        }

        private static IEnumerator BootToMainMenu()
        {
            Scene previouslyActive = SceneManager.GetActiveScene();
            SceneManager.LoadScene("Boot", LoadSceneMode.Single);
            var deadline = new PlayDeadline(10f);
            while ((SceneManager.GetActiveScene() == previouslyActive || SceneManager.GetActiveScene().name != "MainMenu")
                   && !deadline.Passed)
            {
                yield return null;
            }
        }
    }
}
