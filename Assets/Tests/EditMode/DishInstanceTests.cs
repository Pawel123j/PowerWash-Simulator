// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;

namespace Zmywak.Tests
{
    public class DishInstanceTests
    {
        private DirtTypeDefinition _sauce;

        private ToolDefinition _sponge;

        private ToolDefinition _steelWool;

        private DishDefinition _plate;

        [SetUp]
        public void SetUp()
        {
            _sauce = ScriptableObject.CreateInstance<DirtTypeDefinition>();
            Set(_sauce, "id", "dried_sauce");
            Set(_sauce, "scrubDifficulty", 1.2f);
            _sponge = CreateTool("sponge", _sauce);
            _steelWool = CreateTool("steel_wool", null);
            _plate = ScriptableObject.CreateInstance<DishDefinition>();
            SerializedObject serializedObject = new SerializedObject(_plate);
            serializedObject.FindProperty("id").stringValue = "plate";
            SerializedProperty serializedProperty = serializedObject.FindProperty("dirtLayers");
            serializedProperty.arraySize = 1;
            serializedProperty.GetArrayElementAtIndex(0).FindPropertyRelative("dirtType").objectReferenceValue = _sauce;
            serializedProperty.GetArrayElementAtIndex(0).FindPropertyRelative("coverage").floatValue = 0.7f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_plate);
            Object.DestroyImmediate(_sponge);
            Object.DestroyImmediate(_steelWool);
            Object.DestroyImmediate(_sauce);
        }

        [Test]
        public void TheRightTool_IsTheOneThatWorksOnTheDirtLeft()
        {
            DishInstance dish = ScrapedAndRinsed();
            Assert.IsTrue(dish.ToolSuits(_sponge), "The sponge is the tool for dried sauce");
            Assert.IsFalse(dish.ToolSuits(_steelWool), "Steel wool works on nothing on this dish");
            Assert.IsFalse(dish.ToolSuits(null));
            for (int pass = 0; pass < 20; pass++)
            {
                if (dish.Stage != WashStage.AwaitingScrub)
                {
                    break;
                }
                Sweep(dish, _sponge, 0.2f);
            }
            Assert.AreNotEqual(WashStage.AwaitingScrub, dish.Stage, "The dish never came clean");
            Assert.IsFalse(dish.ToolSuits(_sponge), "Nothing is left for any tool to be right for");
        }

        [Test]
        public void ScrubBeforeRinse_DoesNothing()
        {
            DishInstance dish = new DishInstance(_plate, 1);
            dish.Scrape();
            Assert.IsFalse(dish.ScrubAt(_sponge, new Vector2(0.5f, 0.5f), 1f));
            Assert.AreEqual(0.699999988079071, dish.Layers[0].Remaining, 9.999999747378752E-06);
        }

        [Test]
        public void RubbingOnePlace_IsNotEnough()
        {
            DishInstance dish = ScrapedAndRinsed();
            for (int i = 0; i < 200; i++)
            {
                dish.ScrubAt(_sponge, new Vector2(0.5f, 0.5f), 0.05f);
            }
            Assert.AreEqual(WashStage.AwaitingScrub, dish.Stage, "The whole dish has to be rubbed, not one spot");
            Assert.Greater(dish.Layers[0].Remaining, 0f);
        }

        [Test]
        public void SweepingTheWholeDish_CleansIt()
        {
            DishInstance dish = ScrapedAndRinsed();
            for (int pass = 0; pass < 20; pass++)
            {
                if (dish.Stage != WashStage.AwaitingScrub)
                {
                    break;
                }
                Sweep(dish, _sponge, 0.02f);
            }
            Assert.AreEqual(WashStage.ReadyForRack, dish.Stage);
            Assert.AreEqual(0f, dish.Layers[0].Remaining);
            Assert.AreEqual(1.0, dish.Cleanliness01, 9.999999747378752E-06);
        }

        [Test]
        public void WrongTool_CleansFarSlower()
        {
            DishInstance dishInstance = ScrapedAndRinsed();
            DishInstance wrong = ScrapedAndRinsed();
            Sweep(dishInstance, _sponge, 0.02f);
            Sweep(wrong, _steelWool, 0.02f);
            Assert.Less(dishInstance.Layers[0].Remaining, wrong.Layers[0].Remaining);
        }

        [Test]
        public void NearlyCleanLayer_FinishesItself()
        {
            DishInstance dish = ScrapedAndRinsed();
            float lastRemaining = dish.Layers[0].Remaining;
            for (int pass = 0; pass < 200; pass++)
            {
                if (dish.Stage != WashStage.AwaitingScrub)
                {
                    break;
                }
                Sweep(dish, _sponge, 0.002f);
                lastRemaining = dish.Layers[0].Remaining;
                Assert.IsTrue(lastRemaining == 0f || lastRemaining > 0.035f, $"Layer lingered at {lastRemaining}");
            }
            Assert.AreEqual(0f, lastRemaining);
        }

        [Test]
        public void Scraper_TakesTheThickOff_ButLeavesAFilmForAnotherTool()
        {
            ToolDefinition scraper = CreateTool("scraper", _sauce, 2.6f, 1f, 0.3f);
            try
            {
                DishInstance dish = ScrapedAndRinsed();
                for (int pass = 0; pass < 30; pass++)
                {
                    Sweep(dish, scraper, 0.02f);
                }
                float num = dish.Layers[0].Remaining / dish.Layers[0].InitialCoverage;
                Assert.AreEqual(WashStage.AwaitingScrub, dish.Stage, "The scraper alone finished the dish");
                Assert.That(num, Is.InRange(0.1f, 0.6f), "The scraper should get the dirt down to a film, and no further");
                Sweep(dish, scraper, 0.02f);
                Assert.AreEqual(num * dish.Layers[0].InitialCoverage, dish.Layers[0].Remaining, 9.999999974752427E-07, "The scraper went on through its film");
                Assert.IsFalse(dish.ScrubAt(scraper, new Vector2(0.5f, 0.5f), 1f), "Rubbing the film with the scraper should do nothing");
                for (int i = 0; i < 20; i++)
                {
                    if (dish.Stage != WashStage.AwaitingScrub)
                    {
                        break;
                    }
                    Sweep(dish, _sponge, 0.02f);
                }
                Assert.AreEqual(WashStage.ReadyForRack, dish.Stage, "The sponge should finish what the scraper left");
            }
            finally
            {
                Object.DestroyImmediate(scraper);
            }
        }

        [Test]
        public void Scraper_IsQuickerThanTheRightToolAtTheThickOfIt()
        {
            ToolDefinition scraper = CreateTool("scraper", _sauce, 2.6f, 1f, 0.3f);
            try
            {
                DishInstance dishInstance = ScrapedAndRinsed();
                DishInstance sponged = ScrapedAndRinsed();
                Sweep(dishInstance, scraper, 0.01f);
                Sweep(sponged, _sponge, 0.01f);
                Assert.Less(dishInstance.Layers[0].Remaining, sponged.Layers[0].Remaining);
            }
            finally
            {
                Object.DestroyImmediate(scraper);
            }
        }

        [Test]
        public void Spray_CoversMoreOfTheDishInOneSpot_ThanASponge()
        {
            ToolDefinition spray = CreateTool("shower", _sauce, 1f, 1.8f);
            try
            {
                DishInstance dishInstance = ScrapedAndRinsed();
                DishInstance sponged = ScrapedAndRinsed();
                dishInstance.ScrubAt(spray, new Vector2(0.5f, 0.5f), 0.05f);
                sponged.ScrubAt(_sponge, new Vector2(0.5f, 0.5f), 0.05f);
                Assert.Less(dishInstance.Layers[0].Remaining, sponged.Layers[0].Remaining, "The same power over a wider jet should take more dirt off");
            }
            finally
            {
                Object.DestroyImmediate(spray);
            }
        }

        [Test]
        public void Soaking_RinsesTheDish_ButNotBeforeItIsScraped()
        {
            DishInstance dish = new DishInstance(_plate, 3);
            dish.Soak(5f);
            Assert.AreEqual(WashStage.AwaitingScrape, dish.Stage, "Scraps still on it: the soak should wait for the scrape");
            Assert.AreEqual(0f, dish.SoakSeconds);
            dish.Scrape();
            dish.Soak(1.5f);
            Assert.AreEqual(WashStage.AwaitingScrub, dish.Stage, "Lying in the water should rinse it");
        }

        [Test]
        public void Soaking_SoftensTheDirt_UpToAFullSoak()
        {
            DishInstance soaked = ScrapedAndRinsed();
            DishInstance dry = ScrapedAndRinsed();
            soaked.Soak(60f);
            Assert.AreEqual(20.0, soaked.SoakSeconds, 9.999999747378752E-06, "A soak should stop counting once it is full");
            Assert.AreEqual(1.0, soaked.Soak01, 9.999999747378752E-06);
            Assert.AreEqual(0.3499999940395355, soaked.SoakSoftening, 9.999999747378752E-06);
            Sweep(soaked, _sponge, 0.005f);
            Sweep(dry, _sponge, 0.005f);
            Assert.Less(soaked.Layers[0].Remaining, dry.Layers[0].Remaining, "The same rubbing should take more off a soaked dish");
        }

        [Test]
        public void HalfASoak_SoftensHalfAsMuch()
        {
            DishInstance dish = ScrapedAndRinsed();
            dish.Soak(10f);
            Assert.AreEqual(Mathf.Lerp(1f, 0.35f, 0.5f), dish.SoakSoftening, 9.999999747378752E-06);
        }

        private DishInstance ScrapedAndRinsed()
        {
            DishInstance dish = new DishInstance(_plate, 11);
            dish.Scrape();
            dish.Rinse(2f);
            Assert.AreEqual(WashStage.AwaitingScrub, dish.Stage);
            return dish;
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

        private static ToolDefinition CreateTool(string id, DirtTypeDefinition effectiveAgainst, float cleanPower = 1f, float brushRadiusScale = 1f, float residueFloor = 0f)
        {
            ToolDefinition toolDefinition = ScriptableObject.CreateInstance<ToolDefinition>();
            SerializedObject serializedObject = new SerializedObject(toolDefinition);
            serializedObject.FindProperty("id").stringValue = id;
            serializedObject.FindProperty("cleanPower").floatValue = cleanPower;
            serializedObject.FindProperty("ineffectivePower").floatValue = 0.15f;
            serializedObject.FindProperty("brushRadiusScale").floatValue = brushRadiusScale;
            serializedObject.FindProperty("residueFloor").floatValue = residueFloor;
            SerializedProperty list = serializedObject.FindProperty("effectiveAgainst");
            list.arraySize = ((effectiveAgainst != null) ? 1 : 0);
            if (effectiveAgainst != null)
            {
                list.GetArrayElementAtIndex(0).objectReferenceValue = effectiveAgainst;
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            return toolDefinition;
        }

        private static void Set(Object target, string property, object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty prop = serializedObject.FindProperty(property);
            if (!(value is string s))
            {
                if (value is float f)
                {
                    prop.floatValue = f;
                }
            }
            else
            {
                prop.stringValue = s;
            }
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
