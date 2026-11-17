using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Rack;
using Zmywak.Kitchen.Shifts;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;

namespace Zmywak.Tests
{
    /// <summary>
    /// QA audits of the game's data and a fuzz of the life ledger: what no scene test would ever stumble on. A failing
    /// one is a bug found (QA_REPORT.md); they run with the regular suite.
    /// </summary>
    [Category("QA")]
    public class QaAuditTests
    {
        private static readonly LifeRules Rules = LifeRules.Default;
        private string _dir;

        [SetUp]
        public void SetUp()
        {
            _dir = Path.Combine(Path.GetTempPath(), "ZmywakQaAudit_" + Guid.NewGuid().ToString("N"));
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

        private static List<T> AllAssets<T>() where T : UnityEngine.Object
        {
            var list = new List<T>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null)
                {
                    list.Add(asset);
                }
            }

            return list;
        }

        // ---- Data audit ------------------------------------------------------------------------------------------

        [Test]
        public void PWS015_EveryDish_FitsAnEmptyRack_AndHasValidData()
        {
            List<DishDefinition> dishes = AllAssets<DishDefinition>();
            Assert.GreaterOrEqual(dishes.Count, 8, "Setup: dish assets not found");
            var problems = new List<string>();
            var ids = new HashSet<string>();
            foreach (DishDefinition dish in dishes)
            {
                if (string.IsNullOrEmpty(dish.Id) || !ids.Add(dish.Id)) problems.Add($"{dish.name}: empty or duplicate id '{dish.Id}'");
                if (dish.DirtLayers.Count == 0) problems.Add($"{dish.name}: no dirt layers");
                foreach (DirtLayer layer in dish.DirtLayers)
                {
                    if (layer.dirtType == null) problems.Add($"{dish.name}: a dirt layer without a dirt type");
                    if (layer.coverage <= 0f) problems.Add($"{dish.name}: a dirt layer with coverage {layer.coverage}");
                }

                if (dish.RackFootprint.x < 1 || dish.RackFootprint.y < 1) problems.Add($"{dish.name}: footprint {dish.RackFootprint}");
                if (!new RackLayout(6, 4).HasRoomFor(dish.RackFootprint)) problems.Add($"{dish.name}: footprint {dish.RackFootprint} does not fit an EMPTY 6x4 rack");
                if (dish.MassKg <= 0f) problems.Add($"{dish.name}: mass {dish.MassKg}");
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void PWS016_EveryDayOfTheDeal_HasAnUnlockedToolThatWorksOnEveryDirtOnEveryDish()
        {
            List<DishDefinition> dishes = AllAssets<DishDefinition>();
            List<ToolDefinition> tools = AllAssets<ToolDefinition>();
            Assert.GreaterOrEqual(tools.Count, 5, "Setup: tool assets not found");
            var problems = new List<string>();
            for (int day = 1; day <= 8; day++)
            {
                foreach (DishDefinition dish in dishes)
                {
                    if (!MechanicUnlocks.IsDishUnlocked(dish.Id, day))
                    {
                        continue;
                    }

                    foreach (DirtLayer layer in dish.DirtLayers)
                    {
                        bool covered = tools.Exists(t => MechanicUnlocks.IsToolUnlocked(t.Id, day) && t.IsEffectiveAgainst(layer.dirtType));
                        if (!covered)
                        {
                            problems.Add($"day {day}: {dish.Id} carries {layer.dirtType?.name}, which no unlocked tool is effective against");
                            continue;
                        }

                        bool noFilm = tools.Exists(t => MechanicUnlocks.IsToolUnlocked(t.Id, day) && t.IsEffectiveAgainst(layer.dirtType) && t.ResidueFloor <= 0f);
                        if (!noFilm)
                        {
                            problems.Add($"day {day}: {dish.Id} / {layer.dirtType?.name}: only residue-floor tools are effective - a film is left for another tool");
                        }
                    }
                }
            }

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        /// <summary>Seconds of boustrophedon scrubbing until the dish is ready for the rack (-1: never within the limit).</summary>
        private static float SecondsToClean(DishDefinition definition, ToolDefinition tool, float limit = 400f)
        {
            var dish = new DishInstance(definition, 7, true, 64);
            dish.Scrape();
            dish.Rinse(2f);
            const float dt = 0.02f;
            float t = 0f;
            float y = 0.12f;
            int dir = 1;
            var pos = new Vector2(0.12f, y);
            while (dish.Stage == WashStage.AwaitingScrub && t < limit)
            {
                var next = new Vector2(Mathf.Clamp(pos.x + dir * 0.06f, 0.1f, 0.9f), y);
                dish.ScrubStroke(tool, pos, next, dt);
                pos = next;
                t += dt;
                if (pos.x >= 0.9f || pos.x <= 0.1f)
                {
                    dir = -dir;
                    y += 0.08f;
                    if (y > 0.9f)
                    {
                        y = 0.12f;
                    }

                    pos = new Vector2(pos.x, y);
                }
            }

            return dish.Stage == WashStage.AwaitingScrub ? -1f : t;
        }

        [Test]
        public void PWS017_EveryDish_CanBeScrubbedClean_WithEveryToolThatLeavesNoFilm()
        {
            var table = new System.Text.StringBuilder("seconds of steady scrubbing to clean (-1 = stuck):\n");
            var stuck = new List<string>();
            List<ToolDefinition> tools = AllAssets<ToolDefinition>();
            foreach (DishDefinition dish in AllAssets<DishDefinition>())
            {
                table.Append(dish.Id.PadRight(14));
                foreach (ToolDefinition tool in tools)
                {
                    float seconds = SecondsToClean(dish, tool);
                    table.Append($" {tool.Id}={seconds:0.#}");
                    if (seconds < 0f && tool.ResidueFloor <= 0f)
                    {
                        stuck.Add($"{dish.Id} never came clean with {tool.Id}");
                    }
                }

                table.Append('\n');
            }

            Debug.Log("QA PWS-017 " + table);
            Assert.IsEmpty(stuck, string.Join("\n", stuck));
        }

        [Test]
        public void PWS020_ARuinedToolInHand_IsNotCalledSuitable_OnceItCanTakeNothingMoreOff([Values("scraper", "shower")] string toolId)
        {
            ToolDefinition tool = AllAssets<ToolDefinition>().Find(t => t.Id == toolId);
            Assert.IsNotNull(tool);
            var lies = new List<string>();
            foreach (DishDefinition definition in AllAssets<DishDefinition>())
            {
                var dish = new DishInstance(definition, 7, true, 64);
                dish.Scrape();
                dish.Rinse(2f);
                // The tool takes off everything it can: swept over every texel until nothing it works on is thicker than its floor
                // (read straight from the dirt). The first version of this probe swept only part of the dish and judged "nothing
                // more to take off" from one stroke in the middle; the assertion below is the same, the setup is exact now.
                bool settled = QaScrubHelper.Exhaust(dish, tool);
                if (dish.Stage != WashStage.AwaitingScrub)
                {
                    continue; // this tool finished the dish: nothing stuck
                }

                if (!settled)
                {
                    lies.Add($"{definition.Id}: {toolId} never settled - the probe saw nothing");
                    continue;
                }

                if (dish.ToolSuits(tool))
                {
                    lies.Add($"{definition.Id}: {toolId} can take nothing more off, yet the dish says it is the tool for what is left (no 'wrong tool' hint)");
                }
            }

            Assert.IsEmpty(lies, string.Join("\n", lies));
        }

        // ---- Fuzz ------------------------------------------------------------------------------------------------

        [Test]
        public void PWS014_RandomLives_KeepTheirInvariants_AndSurviveASaveRoundTrip([Values(1, 2, 3, 4, 5, 6)] int seed)
        {
            var rng = new System.Random(seed);
            var ledger = new LifeLedger(Rules, null);
            var purchases = (Purchase[])Enum.GetValues(typeof(Purchase));
            for (int step = 0; step < 3000; step++)
            {
                int dayBefore = ledger.DayNumber;
                float walletBefore = ledger.Wallet;
                Place placeBefore = ledger.Place;
                string op;
                bool spends = false;
                switch (rng.Next(13))
                {
                    case 0:
                    case 1:
                    case 2:
                        op = "shift";
                        ledger.CompleteShift(new ShiftResult
                        {
                            Wage = rng.Next(0, 70), ElapsedSeconds = rng.Next(1, 180), PlannedSeconds = 180f, Breakdowns = rng.Next(0, 2),
                            DishesCleaned = rng.Next(0, 30), BestFlow = rng.Next(0, 9)
                        });
                        break;
                    case 3: op = "mom"; ledger.TryCallMom(); break;
                    case 4: op = "pub"; spends = true; ledger.TryGoToPub(); break;
                    case 5: op = "rest"; ledger.TryRest(); break;
                    case 6: op = "dinner"; spends = true; ledger.TryCrewDinner(); break;
                    case 7: op = "letter"; ledger.TryWriteLetter(); break;
                    case 8: op = "buy"; spends = true; ledger.TryBuy(purchases[rng.Next(purchases.Length)]); break;
                    case 9: op = "send"; spends = true; ledger.SendMoneyHome((float)rng.NextDouble() * 90f); break;
                    case 10:
                        op = "story";
                        ledger.ApplyStoryOutcome(new StoryOutcome { energy = rng.Next(-30, 30), homesickness = rng.Next(-30, 30), integration = rng.Next(-10, 20) });
                        break;
                    case 11:
                        op = "skip";
                        if (rng.Next(40) == 0)
                        {
                            ledger.SkipTo(ledger.CurrentDate.AddDays(rng.Next(1, 800)));
                        }

                        break;
                    default:
                        op = "move";
                        if (rng.Next(60) == 0)
                        {
                            ledger.MoveTo((Place)rng.Next(3));
                        }

                        break;
                }

                string where = $"seed {seed} step {step} after {op}";
                Assert.IsFalse(float.IsNaN(ledger.Wallet) || float.IsInfinity(ledger.Wallet), $"{where}: wallet {ledger.Wallet}");
                Assert.That(ledger.Stats.Energy, Is.InRange(0f, 100f), where);
                Assert.That(ledger.Stats.Homesickness, Is.InRange(0f, 100f), where);
                Assert.That(ledger.Stats.Integration, Is.InRange(0f, 100f), where);
                Assert.GreaterOrEqual(ledger.DayNumber, dayBefore, where);
                Assert.GreaterOrEqual(ledger.WeeksInArrears, 0, where);
                Assert.GreaterOrEqual(ledger.BillsReserve, 0f, where);
                Assert.GreaterOrEqual(ledger.SentHomeEuros, 0f, where);
                if (spends && ledger.Place == placeBefore && ledger.Wallet < walletBefore - 0.001f)
                {
                    Assert.GreaterOrEqual(ledger.Wallet, ledger.BillsReserve - 0.011f,
                        $"{where}: spent into the bills' share ({walletBefore:0.00} -> {ledger.Wallet:0.00}, reserve {ledger.BillsReserve:0.00})");
                }

                if (step % 40 == 0)
                {
                    AssertRoundTrips(ledger, where);
                }
            }
        }

        private static void AssertRoundTrips(LifeLedger a, string where)
        {
            var data = new SaveData();
            a.WriteTo(data);
            SaveSystem.Save(data);
            SaveData loaded = SaveSystem.Load();
            var b = new LifeLedger(Rules, null);
            b.LoadFrom(loaded);

            Assert.AreEqual(a.Place, b.Place, where + ": place");
            Assert.AreEqual(a.Wallet, b.Wallet, 0.02f, where + ": wallet");
            Assert.AreEqual(a.Stats.Energy, b.Stats.Energy, 0.02f, where + ": energy");
            Assert.AreEqual(a.Stats.Homesickness, b.Stats.Homesickness, 0.02f, where + ": homesickness");
            Assert.AreEqual(a.Stats.Integration, b.Stats.Integration, 0.02f, where + ": integration");
            Assert.AreEqual(a.DayNumber, b.DayNumber, where + ": day");
            Assert.AreEqual(a.CurrentDate.Date, b.CurrentDate.Date, where + ": date");
            Assert.AreEqual(a.SentHomeEuros, b.SentHomeEuros, 0.02f, where + ": sent home EUR");
            Assert.AreEqual(a.SentHomePln, b.SentHomePln, 0.02f, where + ": sent home PLN");
            Assert.AreEqual(a.WeeksInArrears, b.WeeksInArrears, where + ": arrears");
            Assert.AreEqual(a.InHostel, b.InHostel, where + ": hostel");
            Assert.AreEqual(a.Rank, b.Rank, where + ": rank");
            Assert.AreEqual(a.TonightsActivity, b.TonightsActivity, where + ": tonight's plan");
            Assert.AreEqual(a.CalledMomToday, b.CalledMomToday, where + ": called mom");
            Assert.AreEqual(a.WentToPubToday, b.WentToPubToday, where + ": pub today");
            Assert.AreEqual(a.LastShiftBrokeDown, b.LastShiftBrokeDown, where + ": last shift broke down");
            Assert.AreEqual(a.BreakdownTutorialDone, b.BreakdownTutorialDone, where + ": breakdown tutorial");
            CollectionAssert.AreEquivalent(a.Purchases, b.Purchases, where + ": purchases");
            Assert.AreEqual(a.WorkRecord.shifts, b.WorkRecord.shifts, where + ": career shifts");
            Assert.AreEqual(a.WorkRecord.dishesWashed, b.WorkRecord.dishesWashed, where + ": career dishes");
        }
    }
}
