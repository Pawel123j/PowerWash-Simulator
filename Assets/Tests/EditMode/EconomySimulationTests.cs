// Reconstructed on 2026-11-17 by decompiling Library/ScriptAssemblies/Zmywak.Tests.EditMode.dll (built 2026-10-11):
// the original source was lost from disk. Behaviour matches that build; original comments are gone.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Kitchen.Dishes;
using Zmywak.Kitchen.Shifts;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.Story;

namespace Zmywak.Tests
{
    public class EconomySimulationTests
    {
        private enum Strategy
        {
            Frugal,
            Spender,
            SendEverythingHome
        }

        private sealed class Run
        {
            public Strategy Strategy;

            public string Skill;

            public float LowestWalletAfterBills = float.MaxValue;

            public float LowestEnergy = 100f;

            public LifeLedger Ledger;

            public readonly Dictionary<ShiftGrade, int> Grades = new Dictionary<ShiftGrade, int>();
        }

        private sealed class Profile
        {
            public string Name;

            public string Craft;

            public Func<int, (int cleaned, int broken, int fullRacks)> Shift;

            public Func<LifeLedger, int, EveningActivity> Evening;

            public bool BuysThings;

            public float BuyBuffer = 40f;

            public bool SendsEverything;

            public float SendsWeekly;

            public bool Rests = true;
        }

        private sealed class Life
        {
            public Profile Profile;

            public LifeLedger Ledger;

            public float Earned;

            public float Bills;

            public float Evenings;

            public float Bought;

            public float Story;

            public float LowestAfterBills = float.MaxValue;

            public float LowestEnergy = 100f;

            public float EnergySum;

            public float HomesickSum;

            public float HighestHomesick;

            public float StoryHomesick;

            public float IntegrationSum;

            public int TiredDays;

            public int Days;

            public readonly Dictionary<EveningActivity, int> Plans = new Dictionary<EveningActivity, int>();

            public readonly Dictionary<ShiftGrade, int> Grades = new Dictionary<ShiftGrade, int>();
        }

        private const int Weeks = 8;

        private const float PlannedShiftSeconds = 180f;

        private const float EnergyFloor = 20f;

        private const float TriviallyRichWeeksOfBills = 10f;

        private const float MasterySavingsWeeksOfBills = 16f;

        private static readonly string[] ChapterPaths = new string[6] { "Assets/_Project/ScriptableObjects/Chapter_ch1.asset", "Assets/_Project/ScriptableObjects/Chapter_ch2.asset", "Assets/_Project/ScriptableObjects/Chapter_ch3.asset", "Assets/_Project/ScriptableObjects/Chapter_ch4.asset", "Assets/_Project/ScriptableObjects/Chapter_ch5.asset", "Assets/_Project/ScriptableObjects/Chapter_ch6.asset" };

        private static readonly (string name, int cleaned, int broken, int fullRacks)[] Skills = new (string, int, int, int)[3]
        {
            ("novice", 4, 2, 0),
            ("average", 8, 1, 1),
            ("skilled", 13, 0, 2)
        };

        private static readonly Dictionary<string, (float accuracy, float urgentMet)> Craft = new Dictionary<string, (float, float)>
        {
            ["novice"] = (0.6f, 0.3f),
            ["average"] = (0.8f, 0.6f),
            ["skilled"] = (0.95f, 1f)
        };

        private ShiftCatalog _catalog;

        private Dictionary<int, DialogueGraph> _sceneByDay;

        private Dictionary<int, DateTime> _jumpByDay;

        private static readonly DateTime EpilogueStart = new DateTime(2010, 11, 29);

        private const float CarriedOutOfDublin = 300f;

        private const int StoryDays = 35;

        [SetUp]
        public void SetUp()
        {
            _sceneByDay = new Dictionary<int, DialogueGraph>();
            _jumpByDay = new Dictionary<int, DateTime>();
            _catalog = AssetDatabase.LoadAssetAtPath<ShiftCatalog>("Assets/_Project/Resources/ShiftCatalog.asset");
            Assert.IsNotNull(_catalog, "The shift catalog has not been generated (Zmywak > Setup > Run All)");
            string[] chapterPaths = ChapterPaths;
            foreach (string path in chapterPaths)
            {
                StoryChapter chapter = AssetDatabase.LoadAssetAtPath<StoryChapter>(path);
                Assert.IsNotNull(chapter, path + " has not been generated (Zmywak > Setup > Run All)");
                if (chapter.TryGetStartDate(out var starts))
                {
                    _jumpByDay[chapter.Beats[0].day] = starts;
                }
                foreach (StoryBeat beat in chapter.Beats)
                {
                    Assert.IsFalse(_sceneByDay.ContainsKey(beat.day), $"Two scenes on day {beat.day}");
                    _sceneByDay[beat.day] = beat.graph;
                }
            }
        }

        [Test]
        public void EightWeeks_EveryStrategyAndSkill_StaysSolventAndRested_AndTheChoicesShow()
        {
            LifeRules rules = LifeRules.Default;
            List<Run> runs = new List<Run>();
            (string, int, int, int)[] skills = Skills;
            foreach ((string, int, int, int) skill in skills)
            {
                Strategy[] array = new Strategy[3]
                {
                    Strategy.Frugal,
                    Strategy.Spender,
                    Strategy.SendEverythingHome
                };
                foreach (Strategy strategy in array)
                {
                    runs.Add(Live(strategy, skill, rules, withStory: true));
                }
            }
            Report(runs, rules);
            foreach (Run run in runs)
            {
                string who = $"{run.Skill} / {run.Strategy}";
                Assert.GreaterOrEqual(run.LowestWalletAfterBills, 0f, who + " ended a week in debt");
                Assert.GreaterOrEqual(run.LowestEnergy, 20f, who + ": energy collapsed");
            }
            float richLimit = 10f * rules.WeeklyBillsIn(runs[0].Ledger.CurrentDate.Year);
            skills = Skills;
            for (int i = 0; i < skills.Length; i++)
            {
                (string name, int, int, int) skill2 = skills[i];
                Run frugal = runs.Find((Run r) => r.Skill == skill2.name && r.Strategy == Strategy.Frugal);
                Run spender = runs.Find((Run r) => r.Skill == skill2.name && r.Strategy == Strategy.Spender);
                Run run2 = runs.Find((Run r) => r.Skill == skill2.name && r.Strategy == Strategy.SendEverythingHome);
                Assert.Less(arg2: (skill2.name == "skilled") ? (richLimit / 10f * 16f) : richLimit, arg1: frugal.Ledger.Wallet, message: skill2.name + ": saving everything makes money meaningless", args: Array.Empty<object>());
                Assert.Greater(frugal.Ledger.Wallet - spender.Ledger.Wallet, 200f, skill2.name + ": the pub costs nothing that shows");
                Run frugalNoStory = Live(Strategy.Frugal, skill2, rules, withStory: false);
                Assert.Greater(Live(Strategy.Spender, skill2, rules, withStory: false).Ledger.Stats.Integration - frugalNoStory.Ledger.Stats.Integration, 30f, skill2.name + ": evenings out do not bring Kuba closer to the crew");
                Assert.Greater(run2.Ledger.SentHomeEuros, 0.5f * frugal.Ledger.Wallet, skill2.name + ": little of what was earned could go home");
                Assert.Less(run2.Ledger.Wallet, frugal.Ledger.Wallet, skill2.name + ": sending money home did not leave the wallet lighter");
            }
            Run noviceFrugal = runs.Find((Run r) => r.Skill == "novice" && r.Strategy == Strategy.Frugal);
            Run run3 = runs.Find((Run r) => r.Skill == "skilled" && r.Strategy == Strategy.Frugal);
            Assert.Greater(run3.Ledger.Wallet - noviceFrugal.Ledger.Wallet, 300f, "Washing well barely pays");
            Run averageFrugal = runs.Find((Run r) => r.Skill == "average" && r.Strategy == Strategy.Frugal);
            Assert.Greater(run3.Ledger.Wallet - averageFrugal.Ledger.Wallet, 500f, "Mastery barely pays over an average night");
            Assert.Greater(run3.Grades.TryGetValue(ShiftGrade.S, out var masterNights) ? masterNights : 0, 28, "A master porter's nights should mostly be S");
            Assert.Greater(noviceFrugal.Grades.TryGetValue(ShiftGrade.D, out var hardNights) ? hardNights : 0, 0, "A novice who breaks two dishes a night should see some D grades");
        }

        [Test]
        public void AfterTheEpilogue_EachPlace_PaysItsOwnWay_AndStaysSolvent()
        {
            Dictionary<Place, float> sentHome = new Dictionary<Place, float>();
            StringBuilder table = new StringBuilder("After the epilogue, 8 weeks from 2010-11-29 (wallet in the place's money, sent home in PLN):\n");
            Place[] array = new Place[3]
            {
                Place.Dublin,
                Place.Krakow,
                Place.Pilbara
            };
            foreach (Place place in array)
            {
                List<Run> runs = new List<Run>();
                (string, int, int, int)[] skills = Skills;
                foreach ((string, int, int, int) skill in skills)
                {
                    Strategy[] array2 = new Strategy[3]
                    {
                        Strategy.Frugal,
                        Strategy.Spender,
                        Strategy.SendEverythingHome
                    };
                    foreach (Strategy strategy in array2)
                    {
                        runs.Add(Live(strategy, skill, LifeRules.Default, withStory: false, (LifeLedger ledger) =>
                        {
                            ledger.SkipTo(EpilogueStart);
                            ledger.ApplyStoryOutcome(new StoryOutcome
                            {
                                moneyEuros = 300f - ledger.Wallet
                            });
                            ledger.MoveTo(place);
                        }));
                    }
                }
                LifeRules rules = LifeRules.For(place);
                foreach (Run run in runs)
                {
                    string who = $"{place}: {run.Skill} / {run.Strategy}";
                    Assert.AreEqual(place, run.Ledger.Place, who + " did not move");
                    Assert.GreaterOrEqual(run.LowestWalletAfterBills, 0f, who + " ended a week in debt");
                    Assert.GreaterOrEqual(run.LowestEnergy, 20f, who + ": energy collapsed");
                    table.AppendLine($"{place,-8} {run.Skill,-8} {run.Strategy,-18} {PlaceMoney.Format(run.Ledger.Wallet, place),14} " + $"{run.Ledger.SentHomePln,10:0} PLN  homesickness {run.Ledger.Stats.Homesickness:0}");
                }
                Run frugal = runs.Find((Run r) => r.Skill == "average" && r.Strategy == Strategy.Frugal);
                Run spender = runs.Find((Run r) => r.Skill == "average" && r.Strategy == Strategy.Spender);
                Run sender = runs.Find((Run r) => r.Skill == "average" && r.Strategy == Strategy.SendEverythingHome);
                Assert.Greater(frugal.Ledger.Wallet - spender.Ledger.Wallet, 20f * rules.PubCost, $"{place}: the pub costs nothing that shows");
                Assert.That(7f * ShiftResultCalculator.Calculate(new List<WashStage>(), 8, 1, 180f, 180f, 1, ShiftResultCalculator.HourlyWageOn(EpilogueStart, place), place).Wage / rules.WeeklyBillsIn(2010), Is.InRange(1.5f, 7f), $"{place}: pay and bills are out of proportion");
                sentHome[place] = sender.Ledger.SentHomePln;
            }
            TestContext.WriteLine(table.ToString());
            Debug.Log(table.ToString());
            Assert.Less(sentHome[Place.Krakow], sentHome[Place.Dublin], "Working in Kraków should leave fewer złoty to spare than Dublin");
            Assert.Greater(sentHome[Place.Pilbara], sentHome[Place.Dublin], "The mining boom should pay the most home");
        }

        private Run Live(Strategy strategy, (string name, int cleaned, int broken, int fullRacks) skill, LifeRules rules, bool withStory, Action<LifeLedger> before = null)
        {
            Run run = new Run
            {
                Strategy = strategy,
                Skill = skill.name,
                Ledger = new LifeLedger(rules, null)
            };
            LifeLedger ledger = run.Ledger;
            before?.Invoke(ledger);
            ledger.OnWeekSettled += (float _, float after) =>
            {
                run.LowestWalletAfterBills = Mathf.Min(run.LowestWalletAfterBills, after);
            };
            for (int day = 1; day <= 8 * rules.DaysPerWeek; day++)
            {
                Assert.AreEqual(day, ledger.DayNumber);
                if (withStory && _jumpByDay.TryGetValue(day, out var jump))
                {
                    ledger.SkipTo(jump);
                }
                if (withStory && _sceneByDay.TryGetValue(day, out var scene))
                {
                    PlayCostliest(scene, ledger);
                }
                if (strategy != Strategy.Spender || !ledger.TryGoToPub())
                {
                    if (ledger.Stats.Energy < 60f)
                    {
                        ledger.TryRest();
                    }
                    else
                    {
                        ledger.TryCallMom();
                    }
                }
                if (strategy == Strategy.SendEverythingHome)
                {
                    while (ledger.SendMoneyHome(ledger.Rules.SendHomeStep) > 0f)
                    {
                    }
                }
                run.LowestEnergy = Mathf.Min(run.LowestEnergy, ledger.Stats.Energy);
                bool badDay = day % 5 == 0;
                float strength = Fatigue.ScrubStrength(ledger.Stats.Energy);
                int cleaned = Mathf.RoundToInt((float)Mathf.Max(0, skill.cleaned - (badDay ? 2 : 0)) * strength);
                int broken = skill.broken + (badDay ? 2 : 0);
                SpecialShift special = SpecialShifts.On(ledger.CurrentDate, day, ledger.Place);
                bool trayScrubbed = special == SpecialShift.LasagneTray && skill.name != "novice";
                float hourly = ShiftResultCalculator.HourlyWageOn(ledger.CurrentDate, ledger.Place) + Career.HourlyBonusEuros(ledger.Rank) * PlaceMoney.SmallSums(ledger.Place);
                ShiftDefinition kind = _catalog.Find(ShiftSchedule.ArchetypeFor(day, special));
                int quota = ShiftSchedule.QuotaFor(kind.Quota, ShiftSchedule.PaceFor(day), SpecialShifts.Rules(special).PaceScale);
                int bestFlow = skill.fullRacks * 2 + ((broken == 0) ? 2 : 0);
                int urgent = Mathf.RoundToInt(kind.UrgentOrderChance * 4f);
                ShiftScore score = ShiftScoring.Score(new ShiftPerformance
                {
                    DishesCleaned = cleaned,
                    Quota = quota,
                    DishesBroken = broken,
                    WashAccuracy = Craft[skill.name].accuracy,
                    BestFlow = bestFlow,
                    UrgentOrders = urgent,
                    UrgentOrdersMet = Mathf.RoundToInt((float)urgent * Craft[skill.name].urgentMet)
                });
                run.Grades[score.Grade] = ((!run.Grades.TryGetValue(score.Grade, out var seen)) ? 1 : (seen + 1));
                ShiftResult shift = ShiftResultCalculator.Calculate(new List<WashStage>(), cleaned, broken, 180f, 180f, skill.fullRacks, hourly, ledger.Place, special, quota, score.Grade);
                shift.Wage += SpecialShifts.Bonus(special, broken, pileCollapsed: false, trayScrubbed) * PlaceMoney.SmallSums(ledger.Place);
                shift.InspectionPassed = special != SpecialShift.Inspection || SpecialShifts.Passed(broken, pileCollapsed: false);
                shift.TrayScrubbed = trayScrubbed;
                shift.BestFlow = bestFlow;
                shift.EnergyCost = kind.EnergyCost;
                ledger.CompleteShift(shift);
                run.LowestEnergy = Mathf.Min(run.LowestEnergy, ledger.Stats.Energy);
            }
            return run;
        }

        private static EveningActivity Sensible(LifeLedger l, int day, float tiredBelow, float callAbove, Func<int, bool> pubNight)
        {
            if (l.Stats.Energy < tiredBelow)
            {
                return EveningActivity.Rest;
            }
            if (l.Stats.Homesickness >= callAbove && !l.CalledMomToday)
            {
                return EveningActivity.CallHome;
            }
            if (l.CrewDinnerOffered)
            {
                return EveningActivity.CrewDinner;
            }
            if (!pubNight(day))
            {
                return EveningActivity.None;
            }
            return EveningActivity.Pub;
        }

        private static Profile[] FiveProfiles()
        {
            return new Profile[5]
            {
                new Profile
                {
                    Name = "very good",
                    Craft = "skilled",
                    BuysThings = true,
                    SendsWeekly = 30f,
                    Shift = (int day) => (cleaned: 13, broken: (day % 5 == 0) ? 1 : 0, fullRacks: 2),
                    Evening = (LifeLedger l, int day) => Sensible(l, day, 64f, 40f, (int d) => d % 3 == 0)
                },
                new Profile
                {
                    Name = "average",
                    Craft = "average",
                    SendsWeekly = 20f,
                    Shift = (int day) => (cleaned: (day % 5 == 0) ? 6 : 8, broken: ((day % 3 == 0) ? 1 : 0) + ((day % 5 == 0) ? 1 : 0), fullRacks: 1),
                    Evening = (LifeLedger l, int day) => Sensible(l, day, 58f, 50f, (int d) => d % 7 == 2 || d % 7 == 5)
                },
                new Profile
                {
                    Name = "weak",
                    Craft = "novice",
                    Shift = (int day) => (cleaned: (day % 5 == 0) ? 3 : 5, broken: (day % 4 != 0) ? (1 + ((day % 3 == 0) ? 1 : 0)) : 0, fullRacks: (day % 3 == 0) ? 1 : 0),
                    Evening = (LifeLedger l, int day) => Sensible(l, day, 62f, 45f, (int d) => false)
                },
                new Profile
                {
                    Name = "party",
                    Craft = "average",
                    Rests = false,
                    Shift = (int day) => (cleaned: (day % 5 == 0) ? 6 : 8, broken: ((day % 3 == 0) ? 1 : 0) + ((day % 5 == 0) ? 1 : 0), fullRacks: 1),
                    Evening = (LifeLedger l, int day) => (!l.CanDo(EveningActivity.Pub, out var _)) ? EveningActivity.CallHome : EveningActivity.Pub
                },
                new Profile
                {
                    Name = "family",
                    Craft = "average",
                    SendsEverything = true,
                    Shift = (int day) => (cleaned: (day % 5 == 0) ? 6 : 8, broken: ((day % 3 == 0) ? 1 : 0) + ((day % 5 == 0) ? 1 : 0), fullRacks: 1),
                    Evening = (LifeLedger l, int day) => Sensible(l, day, 58f, 30f, (int d) => false)
                }
            };
        }

        [Test]
        public void TheStorysDays_FiveKindsOfPlayer_FeelThePressure_ButNoneIsBroken()
        {
            List<Life> lives = new List<Life>();
            Profile[] array = FiveProfiles();
            foreach (Profile profile in array)
            {
                lives.Add(LiveTheStory(profile));
            }
            LifeRules rules = LifeRules.Default;
            string text = Table(lives);
            TestContext.WriteLine(text);
            Debug.Log(text);
            Life master = lives.Find((Life l) => l.Profile.Name == "very good");
            Life average = lives.Find((Life l) => l.Profile.Name == "average");
            Life weak = lives.Find((Life l) => l.Profile.Name == "weak");
            Life party = lives.Find((Life l) => l.Profile.Name == "party");
            Life family = lives.Find((Life l) => l.Profile.Name == "family");
            foreach (Life life in lives)
            {
                Assert.GreaterOrEqual(life.LowestAfterBills, 0f, life.Profile.Name + ": a week ended in debt");
                Assert.GreaterOrEqual(life.LowestEnergy, 20f, life.Profile.Name + ": energy collapsed");
                if (life.Profile.Rests)
                {
                    Assert.Less(life.TiredDays, life.Days / 2, life.Profile.Name + ": tired most days - fatigue must not be a permanent state");
                }
            }
            Assert.Greater(lives.FindAll((Life l) => l.Profile.Rests).Sum((Life l) => l.TiredDays), 0, "Nobody is ever tired - energy means nothing");
            float weekOfBills = rules.WeeklyBillsIn(2010);
            Assert.Less(average.Ledger.Wallet, 8f * weekOfBills, "An average porter saves too easily - money stops mattering");
            Assert.Less(weak.LowestAfterBills, 1.5f * rules.WeeklyBills, "The weakest player never feels the rent");
            Assert.Greater(master.Earned - weak.Earned, 400f, "Washing well barely pays");
            Assert.Greater(average.Earned, weak.Earned, "An average porter earns no more than a weak one");
            Assert.GreaterOrEqual(weak.Ledger.Rank, 1, "Even the weakest player is promoted once in five weeks");
            Assert.GreaterOrEqual(master.Ledger.Rank, 2, "A very good porter climbs the ladder");
            Assert.Greater(party.TiredDays, party.Days / 3, "Nights out every evening should tire Kuba out");
            Assert.Less(party.EnergySum / (float)party.Days, average.EnergySum / (float)average.Days, "The pub every night costs nothing in energy");
            Assert.Less(master.TiredDays, party.TiredDays);
            Assert.Greater(family.Ledger.SentHomeEuros, average.Ledger.SentHomeEuros + 200f, "Sending home regularly barely sends anything");
            Assert.Less(family.Ledger.Wallet, average.Ledger.Wallet, "Sending money home did not leave the wallet lighter");
            Assert.GreaterOrEqual(party.Ledger.Stats.Integration, weak.Ledger.Stats.Integration);
            Assert.Less(family.Ledger.Stats.Homesickness, party.Ledger.Stats.Homesickness, "Calling home does nothing for homesickness");
        }

        [Test]
        public void TheStorysDays_TheSpenderAndTheSaver_NeitherBreaksTheStory_AndTheSavingsCostSomething()
        {
            Profile spenderProfile = new Profile
            {
                Name = "spender",
                Craft = "novice",
                BuysThings = true,
                BuyBuffer = 0f,
                SendsEverything = true,
                Rests = false,
                Shift = (int day) => (cleaned: (day % 5 == 0) ? 3 : 5, broken: (day % 4 != 0) ? (1 + ((day % 3 == 0) ? 1 : 0)) : 0, fullRacks: (day % 3 == 0) ? 1 : 0),
                Evening = (LifeLedger l, int day) => (l.Stats.Energy < 30f) ? EveningActivity.Rest : ((!l.CrewDinnerOffered || !l.CanDo(EveningActivity.CrewDinner, out var why)) ? (l.CanDo(EveningActivity.Pub, out why) ? EveningActivity.Pub : EveningActivity.None) : EveningActivity.CrewDinner)
            };
            Profile saverProfile = new Profile
            {
                Name = "saver",
                Craft = "skilled",
                Shift = (int day) => (cleaned: 13, broken: (day % 5 == 0) ? 1 : 0, fullRacks: 2),
                Evening = (LifeLedger l, int day) => (l.Stats.Energy < 50f) ? EveningActivity.Rest : EveningActivity.None
            };
            List<Life> lives = new List<Life>();
            foreach (Profile profile in FiveProfiles().Append(spenderProfile).Append(saverProfile))
            {
                lives.Add(LiveTheStory(profile));
            }
            string text = Table(lives);
            TestContext.WriteLine(text);
            Debug.Log(text);
            Life spender = lives.Find((Life l) => l.Profile.Name == "spender");
            Life saver = lives.Find((Life l) => l.Profile.Name == "saver");
            Life family = lives.Find((Life l) => l.Profile.Name == "family");
            Life party = lives.Find((Life l) => l.Profile.Name == "party");
            Life[] array = new Life[2] { spender, saver };
            foreach (Life life in array)
            {
                Assert.AreEqual(35, life.Days, life.Profile.Name + ": the story did not run to its end");
                Assert.GreaterOrEqual(life.LowestAfterBills, 0f, life.Profile.Name + ": a week ended in debt");
                Assert.GreaterOrEqual(life.LowestEnergy, 20f, life.Profile.Name + ": energy collapsed");
                Assert.IsFalse(life.Ledger.InHostel, life.Profile.Name + ": ended the story in the hostel");
            }
            Assert.Less(spender.Ledger.Wallet, 2f * LifeRules.Default.WeeklyBillsIn(2010), "The spender kept money he was set on spending");
            Assert.Greater(spender.Evenings + spender.Bought, saver.Evenings + saver.Bought, "The spender spent no more than the saver");
            Assert.Greater(spender.IntegrationSum / (float)spender.Days, saver.IntegrationSum / (float)saver.Days, "Nights out did not bring the crew closer");
            foreach (Life life2 in lives.Where((Life l) => l != saver))
            {
                Assert.Greater(saver.Ledger.Wallet, life2.Ledger.Wallet, "The saver ended poorer than " + life2.Profile.Name);
            }
            Assert.Greater(saver.Ledger.Stats.Homesickness, family.Ledger.Stats.Homesickness, "Never calling home cost nothing");
            Assert.Less(saver.IntegrationSum / (float)saver.Days, party.IntegrationSum / (float)party.Days, "Never going out cost nothing with the crew");
        }

        private static string Table(List<Life> lives)
        {
            StringBuilder table = new StringBuilder($"The story's {35} days (Dublin, 2004-2010; costliest story choices; money in euros):\n");
            table.AppendLine("player      earned   bills  evenings  bought   story  sent home  wallet  lowest after bills  energy min/avg  tired days  rest/call/pub/dinner/letter/in  rank  homesick end/avg/max (story)  integration end/avg  grades");
            foreach (Life life in lives)
            {
                LifeLedger l = life.Ledger;
                string grades = string.Join(" ", new ShiftGrade[5]
                {
                    ShiftGrade.S,
                    ShiftGrade.A,
                    ShiftGrade.B,
                    ShiftGrade.C,
                    ShiftGrade.D
                }.Select((ShiftGrade g) => $"{g}:{(life.Grades.TryGetValue(g, out var value) ? value : 0)}"));
                table.AppendLine($"{life.Profile.Name,-10} {life.Earned,7:0} {life.Bills,7:0} {life.Evenings,9:0} {life.Bought,7:0} {life.Story,7:0} " + $"{l.SentHomeEuros,10:0} {l.Wallet,7:0} {life.LowestAfterBills,19:0} {life.LowestEnergy,7:0}/{life.EnergySum / (float)life.Days,-6:0} " + $"{life.TiredDays,10} {Plans(life),-30} {Career.Title(l.Rank),-17} " + $"{l.Stats.Homesickness,4:0}/{life.HomesickSum / (float)life.Days:0}/{life.HighestHomesick:0} ({life.StoryHomesick:+0;-0}) " + $"{l.Stats.Integration,12:0}/{life.IntegrationSum / (float)life.Days:0}  {grades}");
            }
            return table.ToString();
        }

        private static string Plans(Life life)
        {
            return $"{Count(EveningActivity.Rest)}/{Count(EveningActivity.CallHome)}/{Count(EveningActivity.Pub)}/" + $"{Count(EveningActivity.CrewDinner)}/{Count(EveningActivity.Letter)}/{Count(EveningActivity.None)}";
            int Count(EveningActivity a)
            {
                if (!life.Plans.TryGetValue(a, out var n))
                {
                    return 0;
                }
                return n;
            }
        }

        private Life LiveTheStory(Profile profile)
        {
            LifeRules rules = LifeRules.Default;
            Life life = new Life
            {
                Profile = profile,
                Ledger = new LifeLedger(rules, null)
            };
            LifeLedger ledger = life.Ledger;
            ledger.OnWeekSettled += (float num3, float after) =>
            {
                life.Bills += num3 - after;
                life.LowestAfterBills = Mathf.Min(life.LowestAfterBills, after);
            };
            for (int day = 1; day <= 35; day++)
            {
                Assert.AreEqual(day, ledger.DayNumber);
                if (_jumpByDay.TryGetValue(day, out var jump))
                {
                    ledger.SkipTo(jump);
                }
                float before = ledger.Wallet;
                float homesickBefore = ledger.Stats.Homesickness;
                if (_sceneByDay.TryGetValue(day, out var scene))
                {
                    PlayCostliest(scene, ledger);
                }
                life.Story += before - ledger.Wallet;
                life.StoryHomesick += ledger.Stats.Homesickness - homesickBefore;
                before = ledger.Wallet;
                if (profile.BuysThings)
                {
                    Purchase[] array = new Purchase[3]
                    {
                        Purchase.Mattress,
                        Purchase.Dictionary,
                        Purchase.Phone
                    };
                    foreach (Purchase purchase in array)
                    {
                        if (!ledger.Has(purchase) && ledger.Sendable > ledger.PriceOf(purchase) + profile.BuyBuffer)
                        {
                            ledger.TryBuy(purchase);
                        }
                    }
                }
                life.Bought += before - ledger.Wallet;
                before = ledger.Wallet;
                if (ledger.Has(Purchase.Phone) && ledger.Stats.Homesickness >= 30f)
                {
                    ledger.TryCallMom();
                }
                EveningActivity plan = profile.Evening(ledger, day);
                bool done = plan switch
                {
                    EveningActivity.Rest => ledger.TryRest(),
                    EveningActivity.CallHome => ledger.TryCallMom(),
                    EveningActivity.Pub => ledger.TryGoToPub(),
                    EveningActivity.CrewDinner => ledger.TryCrewDinner(),
                    EveningActivity.Letter => ledger.TryWriteLetter(),
                    _ => false,
                };
                if (!done && plan != EveningActivity.None && ledger.LetterOffered && ledger.TonightsActivity == EveningActivity.None && profile.Rests)
                {
                    plan = (ledger.TryWriteLetter() ? EveningActivity.Letter : plan);
                }
                EveningActivity tonight = ledger.TonightsActivity;
                life.Plans[tonight] = ((!life.Plans.TryGetValue(tonight, out var nights)) ? 1 : (nights + 1));
                life.Evenings += before - ledger.Wallet;
                if (profile.SendsEverything)
                {
                    while (ledger.SendMoneyHome(ledger.Rules.SendHomeStep) > 0f)
                    {
                    }
                }
                else if (profile.SendsWeekly > 0f && day % 7 == 6)
                {
                    ledger.SendMoneyHome(profile.SendsWeekly);
                }
                float energy = ledger.Stats.Energy;
                life.HomesickSum += ledger.Stats.Homesickness;
                life.IntegrationSum += ledger.Stats.Integration;
                life.HighestHomesick = Mathf.Max(life.HighestHomesick, ledger.Stats.Homesickness);
                life.LowestEnergy = Mathf.Min(life.LowestEnergy, energy);
                life.EnergySum += energy;
                life.Days++;
                if (Fatigue.IsTired(energy))
                {
                    life.TiredDays++;
                }
                ShiftResult shift = Work(profile.Craft, profile.Shift(day), day, ledger, life.Grades);
                life.Earned += shift.Wage;
                ledger.CompleteShift(shift);
                life.LowestEnergy = Mathf.Min(life.LowestEnergy, ledger.Stats.Energy);
            }
            return life;
        }

        private ShiftResult Work(string craft, (int cleaned, int broken, int fullRacks) today, int day, LifeLedger ledger, Dictionary<ShiftGrade, int> grades)
        {
            (string, int, int, int) skill = (craft, today.cleaned, today.broken, today.fullRacks);
            float strength = Fatigue.ScrubStrength(ledger.Stats.Energy);
            int cleaned = Mathf.RoundToInt((float)skill.Item2 * strength);
            int broken = skill.Item3;
            SpecialShift special = SpecialShifts.On(ledger.CurrentDate, day, ledger.Place);
            bool trayScrubbed = special == SpecialShift.LasagneTray && skill.Item1 != "novice";
            float hourly = ShiftResultCalculator.HourlyWageOn(ledger.CurrentDate, ledger.Place) + Career.HourlyBonusEuros(ledger.Rank) * PlaceMoney.SmallSums(ledger.Place);
            ShiftDefinition kind = _catalog.Find(ShiftSchedule.ArchetypeFor(day, special));
            int quota = ShiftSchedule.QuotaFor(kind.Quota, ShiftSchedule.PaceFor(day), SpecialShifts.Rules(special).PaceScale);
            int bestFlow = skill.Item4 * 2 + ((broken == 0) ? 2 : 0);
            int urgent = (MechanicUnlocks.IsUnlocked(Mechanic.UrgentOrders, day) ? Mathf.RoundToInt(kind.UrgentOrderChance * 4f) : 0);
            ShiftScore score = ShiftScoring.Score(new ShiftPerformance
            {
                DishesCleaned = cleaned,
                Quota = quota,
                DishesBroken = broken,
                WashAccuracy = Craft[skill.Item1].accuracy,
                BestFlow = bestFlow,
                UrgentOrders = urgent,
                UrgentOrdersMet = Mathf.RoundToInt((float)urgent * Craft[skill.Item1].urgentMet)
            });
            grades[score.Grade] = ((!grades.TryGetValue(score.Grade, out var seen)) ? 1 : (seen + 1));
            ShiftResult shiftResult = ShiftResultCalculator.Calculate(new List<WashStage>(), cleaned, broken, 180f, 180f, skill.Item4, hourly, ledger.Place, special, quota, score.Grade);
            shiftResult.Wage += SpecialShifts.Bonus(special, broken, pileCollapsed: false, trayScrubbed) * PlaceMoney.SmallSums(ledger.Place);
            shiftResult.InspectionPassed = special != SpecialShift.Inspection || SpecialShifts.Passed(broken, pileCollapsed: false);
            shiftResult.TrayScrubbed = trayScrubbed;
            shiftResult.BestFlow = bestFlow;
            shiftResult.Score = score;
            shiftResult.EnergyCost = kind.EnergyCost;
            return shiftResult;
        }

        private static void Report(List<Run> runs, LifeRules rules)
        {
            StringBuilder table = new StringBuilder();
            table.AppendLine($"Economy after {8} weeks, ending {runs[0].Ledger.CurrentDate:yyyy-MM-dd} (bills {rules.WeeklyBills:0} EUR/week in 2004, " + $"{rules.WeeklyBillsIn(runs[0].Ledger.CurrentDate.Year):0} at the end; costliest story choices):");
            table.AppendLine("skill    strategy            wallet  sent home  lowest after bills  lowest energy  integration  homesickness  grades");
            foreach (Run run in runs)
            {
                string grades = string.Join(" ", new ShiftGrade[5]
                {
                    ShiftGrade.S,
                    ShiftGrade.A,
                    ShiftGrade.B,
                    ShiftGrade.C,
                    ShiftGrade.D
                }.Select((ShiftGrade g) => $"{g}:{(run.Grades.TryGetValue(g, out var value) ? value : 0)}"));
                table.AppendLine($"{run.Skill,-8} {run.Strategy,-18} {run.Ledger.Wallet,7:0} {run.Ledger.SentHomeEuros,10:0} " + $"{run.LowestWalletAfterBills,19:0} {run.LowestEnergy,14:0} {run.Ledger.Stats.Integration,12:0} {run.Ledger.Stats.Homesickness,13:0}  {grades}");
            }
            TestContext.WriteLine(table.ToString());
            Debug.Log(table.ToString());
        }

        private static void PlayCostliest(DialogueGraph graph, LifeLedger ledger)
        {
            string nodeId = graph.StartNodeId;
            for (int step = 0; step < 100; step++)
            {
                if (string.IsNullOrEmpty(nodeId))
                {
                    break;
                }
                DialogueNode node = graph.FindNode(nodeId);
                if (node == null)
                {
                    break;
                }
                ledger.ApplyStoryOutcome(node.outcome);
                List<string> ways = new List<string>();
                foreach (DialogueChoice choice in node.choices)
                {
                    if (ledger.CanSpareEuros(DialogueSession.CostOf(graph, choice)))
                    {
                        ways.Add(choice.targetNodeId);
                    }
                }
                if (node.choices.Count == 0)
                {
                    ways.Add(node.branchNodeId);
                    ways.Add(node.nextNodeId);
                }
                nodeId = null;
                foreach (string way in ways)
                {
                    if (!string.IsNullOrEmpty(way) && (nodeId == null || Costlier(graph, way, nodeId)))
                    {
                        nodeId = way;
                    }
                }
            }
        }

        private static bool Costlier(DialogueGraph graph, string a, string b)
        {
            float moneyA = WorstPath(graph, a, (DialogueNode node) => node.outcome.moneyEuros);
            float moneyB = WorstPath(graph, b, (DialogueNode node) => node.outcome.moneyEuros);
            if (!Mathf.Approximately(moneyA, moneyB))
            {
                return moneyA < moneyB;
            }
            return WorstPath(graph, a, (DialogueNode node) => node.outcome.energy) < WorstPath(graph, b, (DialogueNode node) => node.outcome.energy);
        }

        private static float WorstPath(DialogueGraph graph, string fromNodeId, Func<DialogueNode, float> value)
        {
            Dictionary<string, float> memo = new Dictionary<string, float>();
            return Worst(fromNodeId);
            float Worst(string nodeId)
            {
                if (string.IsNullOrEmpty(nodeId))
                {
                    return 0f;
                }
                if (memo.TryGetValue(nodeId, out var known))
                {
                    return known;
                }
                DialogueNode node = graph.FindNode(nodeId);
                if (node == null)
                {
                    return 0f;
                }
                List<string> next = new List<string> { node.nextNodeId, node.branchNodeId };
                foreach (DialogueChoice choice in node.choices)
                {
                    next.Add(choice.targetNodeId);
                }
                float worstNext = 0f;
                bool any = false;
                foreach (string id in next)
                {
                    if (!string.IsNullOrEmpty(id))
                    {
                        float candidate = Worst(id);
                        worstNext = (any ? Mathf.Min(worstNext, candidate) : candidate);
                        any = true;
                    }
                }
                float total = value(node) + worstNext;
                memo[nodeId] = total;
                return total;
            }
        }
    }
}
