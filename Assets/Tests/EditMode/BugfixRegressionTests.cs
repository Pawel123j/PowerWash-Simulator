using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Dishes;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;

namespace Zmywak.Tests
{
    /// <summary>
    /// Regression tests for the bugs the QA pass found (QA_REPORT.md, PWS-xxx). They run with the regular suite, as do the
    /// matching probes (QaExploratoryTests, QaAuditTests, QaExploratoryPlayModeTests; only the long ones there are Explicit).
    /// </summary>
    [Category("QARegression")]
    public class BugfixRegressionTests
    {
        private static readonly LifeRules Rules = LifeRules.Default;
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ZmywakRegression_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_dir);
            SaveSystem.DirectoryOverride = _dir;
            SaveSystem.ResetSlot();
        }

        [TearDown]
        public void TearDown()
        {
            SaveSystem.DirectoryOverride = null;
            SaveSystem.ResetSlot();
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        // ---- PWS-001: a story moment must not wipe a debt ---------------------------------------------------------

        /// <summary>A life whose wallet is exactly <paramref name="wallet"/> (a loaded save), on day 5 in Dublin.</summary>
        private static LifeLedger WithWallet(float wallet)
        {
            var ledger = new LifeLedger(Rules, null);
            ledger.LoadFrom(new SaveData { dayNumber = 5, walletEuros = wallet, energy = 80f, currentDateIso = "2004-05-05T00:00:00.0000000" });
            Assert.AreEqual(wallet, ledger.Wallet, "Setup: the wallet is not what was loaded");
            return ledger;
        }

        private static float After(LifeLedger ledger, float euros)
        {
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = euros });
            return ledger.Wallet;
        }

        [TestCase(-155f, 1f, -154f)]
        [TestCase(-155f, -1f, -156f)]
        [TestCase(-155f, 50f, -105f)]
        [TestCase(-1000000f, 1f, -999999f)]
        [TestCase(-1000000f, -1f, -1000001f)]
        [TestCase(-155f, 0f, -155f)]
        public void PWS001_InDebt_AStoryMoment_ChangesTheDebtByExactlyItsAmount(float wallet, float euros, float expected)
        {
            Assert.AreEqual(expected, After(WithWallet(wallet), euros), 1e-3f);
        }

        [TestCase(-10f, 50f, 40f)] // a gain big enough pays the debt off and leaves money
        [TestCase(-10f, 10f, 0f)] // exactly paid off
        [TestCase(-0.01f, 1f, 0.99f)]
        public void PWS001_AGainThatPaysTheDebtOff_CarriesOnPastZero(float wallet, float euros, float expected)
        {
            Assert.AreEqual(expected, After(WithWallet(wallet), euros), 1e-3f);
        }

        [TestCase(20f, 30f, 50f)]
        [TestCase(20f, -5f, 15f)]
        [TestCase(20f, -20f, 0f)]
        [TestCase(20f, -500f, 0f)] // a loss still cannot take more than the wallet holds
        [TestCase(5f, -20f, 0f)] // ...and does not start a debt from a positive wallet
        [TestCase(0f, -5f, 0f)] // ...nor from an empty one
        [TestCase(0f, 5f, 5f)]
        public void PWS001_WithMoneyInTheWallet_TheLossStillStopsAtZero(float wallet, float euros, float expected)
        {
            Assert.AreEqual(expected, After(WithWallet(wallet), euros), 1e-3f);
        }

        [Test]
        public void PWS001_TheNotice_TellsWhatChanged_NotTheDebtWipedAway()
        {
            LifeLedger ledger = WithWallet(-155f);
            var notices = new List<string>();
            ledger.OnNotice += notices.Add;

            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = 1f });

            Assert.AreEqual(1, notices.Count);
            StringAssert.DoesNotContain("155", notices[0]);
            StringAssert.Contains("1", notices[0]);
        }

        [Test]
        public void PWS001_AWalletThatChangedInDebt_SurvivesASaveAndALoad()
        {
            LifeLedger ledger = WithWallet(-155f);
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = 1f, energy = -5f });

            var data = new SaveData();
            ledger.WriteTo(data);
            SaveSystem.Save(data);
            var reloaded = new LifeLedger(Rules, null);
            reloaded.LoadFrom(SaveSystem.Load());

            Assert.AreEqual(-154f, reloaded.Wallet, 1e-3f);
            Assert.AreEqual(ledger.Stats.Energy, reloaded.Stats.Energy, 1e-3f);
        }

        [Test]
        public void PWS001_AnEmptyOutcome_ChangesNothing()
        {
            LifeLedger ledger = WithWallet(-155f);
            ledger.ApplyStoryOutcome(default);
            Assert.AreEqual(-155f, ledger.Wallet);
        }

        // ---- PWS-020: a tool that cannot take off what is left is not the tool for it ----------------------------------

        private static List<T> Assets<T>() where T : UnityEngine.Object
        {
            var list = new List<T>();
            foreach (string guid in UnityEditor.AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }

        private static ToolDefinition MakeTool(string id, float residueFloor, params DirtTypeDefinition[] effectiveAgainst)
        {
            var tool = ScriptableObject.CreateInstance<ToolDefinition>();
            var so = new UnityEditor.SerializedObject(tool);
            so.FindProperty("id").stringValue = id;
            UnityEditor.SerializedProperty list = so.FindProperty("effectiveAgainst");
            list.arraySize = effectiveAgainst.Length;
            for (int i = 0; i < effectiveAgainst.Length; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = effectiveAgainst[i];
            }

            so.FindProperty("cleanPower").floatValue = 2f;
            so.FindProperty("ineffectivePower").floatValue = 0.1f;
            so.FindProperty("brushRadiusScale").floatValue = 1f;
            so.FindProperty("residueFloor").floatValue = residueFloor;
            so.ApplyModifiedPropertiesWithoutUndo();
            return tool;
        }

        private static DishDefinition MakeDish(DirtTypeDefinition dirt, float coverage = 0.7f)
        {
            var dish = ScriptableObject.CreateInstance<DishDefinition>();
            var so = new UnityEditor.SerializedObject(dish);
            so.FindProperty("id").stringValue = "qa_" + dirt.name;
            UnityEditor.SerializedProperty layers = so.FindProperty("dirtLayers");
            layers.arraySize = 1;
            layers.GetArrayElementAtIndex(0).FindPropertyRelative("dirtType").objectReferenceValue = dirt;
            layers.GetArrayElementAtIndex(0).FindPropertyRelative("coverage").floatValue = coverage;
            so.ApplyModifiedPropertiesWithoutUndo();
            return dish;
        }

        private static DishInstance ScrapedAndRinsed(DishDefinition definition, int resolution = 64)
        {
            var dish = new DishInstance(definition, 7, true, resolution);
            dish.Scrape();
            dish.Rinse(2f);
            return dish;
        }

        [Test]
        public void PWS020_AToolThatLeavesAFilm_IsNotTheToolForTheFilm_ForEveryTypeOfDirt()
        {
            List<DirtTypeDefinition> types = Assets<DirtTypeDefinition>();
            Assert.GreaterOrEqual(types.Count, 7, "Setup: the dirt types were not found");
            var problems = new List<string>();
            foreach (DirtTypeDefinition type in types)
            {
                DishDefinition definition = MakeDish(type);
                ToolDefinition film = MakeTool("film", 0.3f, type);
                ToolDefinition finisher = MakeTool("finisher", 0f, type);
                try
                {
                    DishInstance dish = ScrapedAndRinsed(definition);
                    if (!dish.ToolSuits(film) || !dish.ToolSuits(finisher))
                    {
                        problems.Add($"{type.name}: on a fresh dish the tools do not both suit");
                        continue;
                    }

                    if (!QaScrubHelper.Exhaust(dish, film) || dish.Stage != WashStage.AwaitingScrub)
                    {
                        problems.Add($"{type.name}: the film tool did not settle at its film (stage {dish.Stage}) - nothing to test on");
                        continue;
                    }

                    if (dish.ToolSuits(film))
                    {
                        problems.Add($"{type.name}: the film tool takes nothing more off (thickest dirt {QaScrubHelper.ThickestEffective(dish, film):0.000}) but still 'suits'");
                    }

                    if (!dish.ToolSuits(finisher))
                    {
                        problems.Add($"{type.name}: the finishing tool does not suit what the film tool left");
                    }

                    QaScrubHelper.Exhaust(dish, finisher);
                    if (dish.Stage != WashStage.ReadyForRack)
                    {
                        problems.Add($"{type.name}: the finishing tool did not finish the dish ({dish.Stage})");
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(definition);
                    UnityEngine.Object.DestroyImmediate(film);
                    UnityEngine.Object.DestroyImmediate(finisher);
                }
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void PWS020_TheRealScraperAndSpray_OnTheRealDishes_SuitWhileTheyCanWork_AndNotAfter()
        {
            List<ToolDefinition> tools = Assets<ToolDefinition>().FindAll(t => t.ResidueFloor > 0f);
            Assert.GreaterOrEqual(tools.Count, 2, "Setup: the scraper and the spray were not found");
            var problems = new List<string>();
            int exhausted = 0;
            foreach (DishDefinition definition in Assets<DishDefinition>())
            {
                foreach (ToolDefinition tool in tools)
                {
                    DishInstance dish = ScrapedAndRinsed(definition);
                    bool effectiveHere = false;
                    foreach (DirtLayer layer in definition.DirtLayers)
                    {
                        effectiveHere |= tool.IsEffectiveAgainst(layer.dirtType);
                    }

                    // Fresh: nothing about the normal flow changes - it suits exactly where it is effective.
                    if (dish.ToolSuits(tool) != effectiveHere)
                    {
                        problems.Add($"{definition.Id} / {tool.Id}: fresh, ToolSuits is {dish.ToolSuits(tool)} but the tool is{(effectiveHere ? "" : " not")} effective here");
                    }

                    if (!effectiveHere)
                    {
                        continue;
                    }

                    if (!QaScrubHelper.Exhaust(dish, tool))
                    {
                        problems.Add($"{definition.Id} / {tool.Id}: never settled at its film - nothing to test on");
                        continue;
                    }

                    if (dish.Stage != WashStage.AwaitingScrub)
                    {
                        continue; // this tool came the dish clean: no film left to be wrong about
                    }

                    exhausted++;
                    if (dish.ToolSuits(tool))
                    {
                        problems.Add($"{definition.Id} / {tool.Id}: scraped down to the film (thickest {QaScrubHelper.ThickestEffective(dish, tool):0.000}, floor {tool.ResidueFloor}), it still 'suits'");
                    }
                }
            }

            Assert.Greater(exhausted, 0, "Setup: no real dish was ever left at a film, so this test saw nothing");
            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [TestCase(0f)]
        [TestCase(0.05f)]
        [TestCase(0.12f)]
        [TestCase(0.3f)]
        [TestCase(0.5f)]
        [TestCase(0.9f)]
        [TestCase(0.999f)]
        [TestCase(1f)]
        public void PWS020_AnEffectiveTool_SuitsExactlyWhileItCanStillThinTheDirt(float residueFloor)
        {
            DirtTypeDefinition type = Assets<DirtTypeDefinition>().Find(t => t.name.Contains("burnt"));
            Assert.IsNotNull(type, "Setup: no burnt food dirt type");
            DishDefinition definition = MakeDish(type);
            ToolDefinition tool = MakeTool("floor_" + residueFloor, residueFloor, type);
            try
            {
                DishInstance dish = ScrapedAndRinsed(definition);
                // At every stage of the scrub, the dish says "suits" exactly when some dirt it works on is thicker than the floor -
                // judged by reading the dirt itself, not by asking the code under test.
                for (int pass = 0; pass < 200 && dish.Stage == WashStage.AwaitingScrub; pass++)
                {
                    bool canStillThin = QaScrubHelper.ThickestEffective(dish, tool) > residueFloor + QaScrubHelper.ExhaustedSlack;
                    Assert.AreEqual(canStillThin, dish.ToolSuits(tool),
                        $"floor {residueFloor}, pass {pass}: the tool can{(canStillThin ? "" : "not")} thin the dirt any more, ToolSuits says {dish.ToolSuits(tool)}");
                    if (!canStillThin)
                    {
                        return; // settled at its film: the question is answered
                    }

                    QaScrubHelper.Pass(dish, tool);
                }

                Assert.AreEqual(WashStage.ReadyForRack, dish.Stage, $"floor {residueFloor}: neither settled nor clean after 200 passes");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(tool);
            }
        }

        [Test]
        public void PWS020_AskingWhichToolSuits_EveryFrame_IsCached_NotAScanOfTheWholeDish()
        {
            DirtTypeDefinition type = Assets<DirtTypeDefinition>().Find(t => t.name.Contains("burnt"));
            DishDefinition definition = MakeDish(type);
            ToolDefinition film = MakeTool("film", 0.3f, type);
            try
            {
                DishInstance dish = ScrapedAndRinsed(definition, 256);
                Assert.IsTrue(QaScrubHelper.Exhaust(dish, film), "Setup: the film tool did not settle");
                Assert.IsFalse(dish.ToolSuits(film), "Setup: the settled film tool still suits");
                var clock = System.Diagnostics.Stopwatch.StartNew();
                bool any = false;
                for (int i = 0; i < 100000; i++)
                {
                    any |= dish.ToolSuits(film);
                }

                clock.Stop();
                Assert.IsFalse(any);
                Assert.Less(clock.ElapsedMilliseconds, 400, "100 000 questions took " + clock.ElapsedMilliseconds + " ms: ToolSuits scans the dish every time");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(film);
            }
        }

        // ---- PWS-004: a deleted slot must not be written back by the life still in memory -------------------------

        [Test]
        public void PWS004_TheLifeGeneration_GoesUpOnlyWhenTheSlotInPlayIsDeleted_OrAnotherIsPicked()
        {
            int start = SaveSystem.LifeGeneration;

            SaveSystem.DeleteSlot(1); // not the slot in play (0): the life in memory is untouched
            Assert.AreEqual(start, SaveSystem.LifeGeneration, "Deleting another slot must not silence the life in play");

            SaveSystem.DeleteSlot(SaveSystem.Slot);
            Assert.AreEqual(start + 1, SaveSystem.LifeGeneration, "Deleting the slot in play must");

            SaveSystem.DeleteSave();
            Assert.AreEqual(start + 2, SaveSystem.LifeGeneration);

            SaveSystem.Slot = 2;
            Assert.AreEqual(start + 3, SaveSystem.LifeGeneration, "Picking a slot must");
        }

        [Test]
        public void PWS004_DeletingASlot_StillRemovesItsFilesAndLeavesTheOthers()
        {
            SaveSystem.Save(new SaveData { dayNumber = 3 });
            SaveSystem.Save(new SaveData { dayNumber = 4 }); // a backup too
            SaveSystem.Slot = 1;
            SaveSystem.Save(new SaveData { dayNumber = 9 });
            SaveSystem.Slot = 0;

            SaveSystem.DeleteSlot(0);

            Assert.IsFalse(SaveSystem.HasSave(0));
            Assert.IsTrue(SaveSystem.HasSave(1));
            Assert.AreEqual(9, SaveSystem.Peek(1).dayNumber);
        }
    }
}
