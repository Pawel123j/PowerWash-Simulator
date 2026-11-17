// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Tests
{
    public class GlassInspectionTests
    {
        private DirtTypeDefinition _grease;

        private DirtTypeDefinition _streaks;

        private ToolDefinition _soak;

        private ToolDefinition _sponge;

        private DishDefinition _glass;

        [SetUp]
        public void SetUp()
        {
            _grease = CreateDirt("grease", DirtPattern.Splotches, hidden: false);
            _streaks = CreateDirt("streaks", DirtPattern.Streaks, hidden: true);
            _soak = CreateTool("soak", _grease);
            _sponge = CreateTool("sponge", _streaks);
            _glass = ScriptableObject.CreateInstance<DishDefinition>();
            SerializedObject serializedObject = new SerializedObject(_glass);
            serializedObject.FindProperty("id").stringValue = "glass";
            SerializedProperty serializedProperty = serializedObject.FindProperty("dirtLayers");
            serializedProperty.arraySize = 2;
            serializedProperty.GetArrayElementAtIndex(0).FindPropertyRelative("dirtType").objectReferenceValue = _grease;
            serializedProperty.GetArrayElementAtIndex(0).FindPropertyRelative("coverage").floatValue = 0.5f;
            serializedProperty.GetArrayElementAtIndex(1).FindPropertyRelative("dirtType").objectReferenceValue = _streaks;
            serializedProperty.GetArrayElementAtIndex(1).FindPropertyRelative("coverage").floatValue = 0.35f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_glass);
            Object.DestroyImmediate(_soak);
            Object.DestroyImmediate(_sponge);
            Object.DestroyImmediate(_grease);
            Object.DestroyImmediate(_streaks);
        }

        [Test]
        public void Streaks_AreThinSmears_OnTheGlass()
        {
            DirtCanvas canvas = new DirtCanvas(new float[1] { 0.35f }, 4, circular: true, 256, new DirtPattern[1] { DirtPattern.Streaks });
            int dirty = 0;
            int total = canvas.Resolution * canvas.Resolution;
            for (int y = 0; y < canvas.Resolution; y++)
            {
                for (int x = 0; x < canvas.Resolution; x++)
                {
                    Vector2 uv = new Vector2(((float)x + 0.5f) / (float)canvas.Resolution, ((float)y + 0.5f) / (float)canvas.Resolution);
                    if (canvas.DirtAt(0, uv) > 0f)
                    {
                        dirty++;
                        Assert.LessOrEqual((uv - new Vector2(0.5f, 0.5f)).magnitude, 0.5f + 1f / (float)canvas.Resolution, "A streak ran off the glass");
                    }
                }
            }
            float arg = (float)dirty / (float)total;
            Assert.Greater(arg, 0.01f, "No streaks at all");
            Assert.Less(arg, 0.2f, "Streaks should be thin lines, not a film over the glass");
        }

        [Test]
        public void Streaks_StayUntouched_WhileGreaseIsOn()
        {
            DishInstance glass = ScrapedAndRinsed();
            Assert.IsFalse(glass.NeedsInspection, "Still greasy: nothing to check against the light yet");
            Sweep(glass, _sponge, 0.05f);
            Assert.Greater(glass.Layers[0].Remaining, 0f);
            Assert.AreEqual(0.3499999940395355, glass.Layers[1].Remaining, 9.999999747378752E-06, "Streaks came off before the grease did");
        }

        [Test]
        public void OnceTheGreaseIsOff_TheGlassNeedsHoldingToTheLight()
        {
            DishInstance glass = ScrapedAndRinsed();
            for (int pass = 0; pass < 20; pass++)
            {
                if (!(glass.Layers[0].Remaining > 0f))
                {
                    break;
                }
                Sweep(glass, _soak, 0.3f);
            }
            Assert.AreEqual(0f, glass.Layers[0].Remaining);
            Assert.IsTrue(glass.NeedsInspection);
            Assert.AreEqual(WashStage.AwaitingScrub, glass.Stage, "A glass with streaks is not ready for the rack");
            for (int i = 0; i < 20; i++)
            {
                if (glass.Stage != WashStage.AwaitingScrub)
                {
                    break;
                }
                Sweep(glass, _sponge, 0.3f);
            }
            Assert.AreEqual(WashStage.ReadyForRack, glass.Stage);
            Assert.IsFalse(glass.NeedsInspection);
        }

        [Test]
        public void Streaks_ShowOnlyInsideTheReflection()
        {
            DirtCanvas canvas = new DirtCanvas(new float[1] { 1f }, 2, circular: true, 256, new DirtPattern[1] { DirtPattern.Streaks });
            Color clean = new Color(0.2f, 0.4f, 0.6f);
            Color streak = new Color(1f, 0f, 0f, 1f);
            bool[] hidden = new bool[1] { true };
            int texels = canvas.Resolution * canvas.Resolution;
            Color32[] plain = new Color32[texels];
            canvas.WriteColors(plain, clean, new Color[1] { streak }, null, hidden);
            Color32[] array = plain;
            for (int i = 0; i < array.Length; i++)
            {
                Color32 pixel = array[i];
                Assert.AreEqual(((Color32)clean).r, pixel.r, "A streak showed up without the light on it");
            }
            InspectionLight light = new InspectionLight();
            Color32[] lit = new Color32[texels];
            canvas.WriteColors(lit, clean, new Color[1] { streak }, null, hidden, light);
            Color32 away = clean * 0.55f;
            int reddestInSpot = 0;
            for (int j = 0; j < texels; j++)
            {
                Vector2 uv = new Vector2(((float)(j % canvas.Resolution) + 0.5f) / (float)canvas.Resolution, ((float)(j / canvas.Resolution) + 0.5f) / (float)canvas.Resolution);
                if (light.Intensity(uv) <= 0f)
                {
                    Assert.AreEqual((int)away.r, (int)lit[j].r, 1.0, "A streak showed outside the reflection");
                    Assert.AreEqual((int)away.g, (int)lit[j].g, 1.0, "A streak showed outside the reflection");
                }
                else if (canvas.DirtAt(0, uv) > 0.5f)
                {
                    reddestInSpot = Mathf.Max(reddestInSpot, lit[j].r - lit[j].g);
                }
            }
            Assert.Greater(reddestInSpot, 50, "No streak showed inside the reflection");
        }

        [Test]
        public void Tilt_RollsTheReflectionTheWayThePhoneTips()
        {
            InspectionLight light = new InspectionLight();
            Vector3 heldAt = new Vector3(0f, -0.7f, -0.7f);
            for (int i = 0; i < 60; i++)
            {
                light.Tilt(heldAt, 1f / 60f);
            }
            Assert.AreEqual(0.5, light.StripeX, 0.0010000000474974513);
            for (int j = 0; j < 60; j++)
            {
                light.Tilt(heldAt + new Vector3(0.2f, 0f, 0f), 1f / 60f);
            }
            Assert.Greater(light.StripeX, 0.65f, "Tipping right should slide the reflection right");
            for (int k = 0; k < 60; k++)
            {
                light.Tilt(heldAt + new Vector3(0f, 0.3f, 0f), 1f / 60f);
            }
            Assert.AreEqual(0.5, light.StripeX, 0.019999999552965164, "Tipping forwards should not slide the reflection");
            for (int l = 0; l < 60; l++)
            {
                light.Tilt(heldAt + new Vector3(3f, 0f, 0f), 1f / 60f);
            }
            Assert.LessOrEqual(light.StripeX, 0.9501f, "The reflection left the glass");
        }

        [Test]
        public void Stripe_LightsAColumnOfTheGlass_WallsIncluded()
        {
            InspectionLight light = new InspectionLight();
            Assert.AreEqual(1.0, light.Intensity(new Vector2(0.5f, 0.5f)), 9.999999747378752E-05, "The base, under the stripe");
            Assert.AreEqual(1.0, light.Intensity(new Vector2(0.5f, 0.02f)), 9.999999747378752E-05, "The front wall, under the stripe");
            Assert.AreEqual(1.0, light.Intensity(new Vector2(0.5f, 0.98f)), 9.999999747378752E-05, "The back wall, under the stripe");
            Assert.AreEqual(0f, light.Intensity(new Vector2(0.601f, 0.5f)), "Past the stripe's edge");
        }

        [Test]
        public void AFingerNudge_SlidesTheReflection_AndTheSweepCarriesOnFromThere()
        {
            InspectionLight light = new InspectionLight();
            light.Nudge(0.2f);
            Assert.AreEqual(0.699999988079071, light.StripeX, 9.999999747378752E-05);
            light.Nudge(5f);
            Assert.AreEqual(0.949999988079071, light.StripeX, 9.999999747378752E-05, "A long drag should stop at the glass's edge");
            light.Nudge(-0.3f);
            float before = light.StripeX;
            light.Drift(0.1f);
            Assert.Less(light.StripeX, before);
            Assert.Less(before - light.StripeX, 0.1f, "The reflection jumped instead of carrying on");
        }

        [Test]
        public void Drift_SweepsAcrossTheGlass_WithoutLeavingIt()
        {
            InspectionLight light = new InspectionLight();
            float min = 1f;
            float max = 0f;
            for (int i = 0; i < 1200; i++)
            {
                light.Drift(1f / 60f);
                Assert.That(light.StripeX, Is.InRange(0.05f, 0.95f));
                min = Mathf.Min(min, light.StripeX);
                max = Mathf.Max(max, light.StripeX);
            }
            Assert.Greater(max - min, 0.7f, "The drifting reflection should cross the glass side to side");
        }

        private DishInstance ScrapedAndRinsed()
        {
            DishInstance glass = new DishInstance(_glass, 9);
            glass.Scrape();
            glass.Rinse(2f);
            Assert.AreEqual(WashStage.AwaitingScrub, glass.Stage);
            return glass;
        }

        private static void Sweep(DishInstance dish, ToolDefinition tool, float deltaTime)
        {
            float step = 0.128f;
            bool leftToRight = true;
            for (float v = 0f; v <= 1f; v += step)
            {
                for (float u = 0f; u <= 1f; u += step)
                {
                    dish.ScrubAt(tool, new Vector2(leftToRight ? u : (1f - u), v), deltaTime);
                }
                leftToRight = !leftToRight;
            }
        }

        private static DirtTypeDefinition CreateDirt(string id, DirtPattern pattern, bool hidden)
        {
            DirtTypeDefinition dirtTypeDefinition = ScriptableObject.CreateInstance<DirtTypeDefinition>();
            SerializedObject serializedObject = new SerializedObject(dirtTypeDefinition);
            serializedObject.FindProperty("id").stringValue = id;
            serializedObject.FindProperty("scrubDifficulty").floatValue = 1f;
            serializedObject.FindProperty("pattern").intValue = (int)pattern;
            serializedObject.FindProperty("showsOnlyInReflection").boolValue = hidden;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return dirtTypeDefinition;
        }

        private static ToolDefinition CreateTool(string id, DirtTypeDefinition effectiveAgainst)
        {
            ToolDefinition toolDefinition = ScriptableObject.CreateInstance<ToolDefinition>();
            SerializedObject serializedObject = new SerializedObject(toolDefinition);
            serializedObject.FindProperty("id").stringValue = id;
            serializedObject.FindProperty("cleanPower").floatValue = 1f;
            serializedObject.FindProperty("ineffectivePower").floatValue = 0.15f;
            SerializedProperty serializedProperty = serializedObject.FindProperty("effectiveAgainst");
            serializedProperty.arraySize = 1;
            serializedProperty.GetArrayElementAtIndex(0).objectReferenceValue = effectiveAgainst;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return toolDefinition;
        }
    }
}
