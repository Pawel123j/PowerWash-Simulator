using System.Collections.Generic;
using NUnit.Framework;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Kitchen.Shifts;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Progress;

namespace Zmywak.Tests
{
    public class LifeLedgerTests
    {
        private static readonly LifeRules Rules = LifeRules.Default;

        private static ShiftResult Paid(float euros) => new ShiftResult { Wage = euros };

        /// <summary>Lives the first days, until the evenings have the pub and transfers home (MechanicUnlocks).</summary>
        private static LifeLedger PastTheFirstDays(LifeLedger ledger)
        {
            while (!MechanicUnlocks.IsUnlocked(Mechanic.Pub, ledger.DayNumber) || !MechanicUnlocks.IsUnlocked(Mechanic.SendMoney, ledger.DayNumber))
            {
                ledger.CompleteShift(Paid(0f));
            }

            return ledger;
        }

        [Test]
        public void AFreshLife_StartsOnDayOne_WithTheStartingWallet()
        {
            var ledger = new LifeLedger(Rules, null);

            Assert.AreEqual(1, ledger.DayNumber);
            Assert.AreEqual(Rules.StartingWallet, ledger.Wallet);
            Assert.AreEqual(0f, ledger.BillsReserve, "Nothing is owed before the week's first day is lived");
        }

        [Test]
        public void ASaveTheWeekWasNeverWrittenTo_LoadsAsAFreshLife()
        {
            // Seen on the emulator: the story's flags reached the file, the week never did (the app died that first
            // evening) - and continuing gave Kuba 0 EUR and no energy.
            var data = new SaveData { storyFlags = new List<string> { "ch1_hung_up" }, seenStoryBeats = new List<string> { "ch1_arrival" } };
            var ledger = new LifeLedger(Rules, null);
            ledger.LoadFrom(data);

            Assert.AreEqual(1, ledger.DayNumber);
            Assert.AreEqual(Rules.StartingWallet, ledger.Wallet, 1e-4f);
            Assert.AreEqual(100f, ledger.Stats.Energy, 1e-4f, "A new life starts rested, not with no energy");
        }

        [Test]
        public void SkippingYears_MovesTheCalendarForwardOnly_AndTheRentFollowsTheYear()
        {
            var ledger = new LifeLedger(Rules, null);
            ledger.CompleteShift(Paid(40f));
            int day = ledger.DayNumber;
            Assert.AreEqual(Rules.WeeklyBillsIn(2004), ledger.WeeklyBills, 1e-3f);

            Assert.IsTrue(ledger.SkipTo(new System.DateTime(2006, 5, 1)));
            Assert.AreEqual(new System.DateTime(2006, 5, 1), ledger.CurrentDate);
            Assert.AreEqual(day, ledger.DayNumber, "The days lived (and the week's rhythm of bills) carry on");
            Assert.Greater(ledger.WeeklyBills, Rules.WeeklyBillsIn(2004), "A Dublin room costs more at the height of the boom");
            Assert.AreEqual(Rules.WeeklyBillsIn(2006), ledger.WeeklyBills, 1e-3f);

            Assert.IsFalse(ledger.SkipTo(new System.DateTime(2005, 1, 1)), "The calendar never goes back");
            Assert.AreEqual(new System.DateTime(2006, 5, 1), ledger.CurrentDate);

            ledger.CompleteShift(Paid(40f));
            Assert.AreEqual(new System.DateTime(2006, 5, 2), ledger.CurrentDate, "...and runs on a day a shift from there");
        }

        [Test]
        public void TheRent_RisesThroughTheBoom_AndFallsAfterTheBust()
        {
            Assert.Less(Rules.WeeklyRentIn(2004), Rules.WeeklyRentIn(2006));
            Assert.Less(Rules.WeeklyRentIn(2006), Rules.WeeklyRentIn(2008));
            Assert.Greater(Rules.WeeklyRentIn(2008), Rules.WeeklyRentIn(2010));
            Assert.AreEqual(Rules.WeeklyBills, Rules.WeeklyBillsIn(2004), 1e-3f);
        }

        [Test]
        public void TheWeeksBills_ArePaid_AfterTheSeventhShift()
        {
            var ledger = new LifeLedger(Rules, null);
            var settled = new List<(float before, float after)>();
            ledger.OnWeekSettled += (before, after) => settled.Add((before, after));

            for (int i = 0; i < Rules.DaysPerWeek - 1; i++)
            {
                ledger.CompleteShift(Paid(40f));
            }

            Assert.IsEmpty(settled);
            ledger.CompleteShift(Paid(40f));

            Assert.AreEqual(1, settled.Count);
            Assert.AreEqual(Rules.DaysPerWeek + 1, ledger.DayNumber);
            Assert.AreEqual(Rules.StartingWallet + 7 * 40f - Rules.WeeklyBills, ledger.Wallet, 1e-3f);
        }

        [Test]
        public void TheBillsRunUp_ASeventhADay()
        {
            var ledger = new LifeLedger(Rules, null);
            for (int day = 1; day <= Rules.DaysPerWeek; day++)
            {
                Assert.AreEqual(Rules.WeeklyBills * (day - 1) / Rules.DaysPerWeek, ledger.BillsReserve, 1e-3f, $"day {day}");
                ledger.CompleteShift(Paid(40f));
            }

            Assert.AreEqual(0f, ledger.BillsReserve, 1e-3f, "Paid: a new week owes nothing yet");
        }

        [Test]
        public void SleepGivesBackMore_TheMoreTiredKubaWentToBed()
        {
            var rested = new LifeLedger(Rules, null);
            rested.CompleteShift(Paid(0f));
            float restedGain = rested.Stats.Energy - (100f - Rules.ShiftEnergyCost);

            var tired = new LifeLedger(Rules, null);
            tired.ApplyStoryOutcome(new StoryOutcome { energy = -50f });
            tired.CompleteShift(Paid(0f));
            float tiredGain = tired.Stats.Energy - (50f - Rules.ShiftEnergyCost);

            Assert.Greater(tiredGain, restedGain);
        }

        [Test]
        public void WeeksOfShifts_WithThePubEveryNight_WearKubaDown_ButLevelOffInsteadOfFlatteningHim()
        {
            var ledger = new LifeLedger(Rules, null);
            for (int i = 0; i < 60; i++)
            {
                ledger.TryGoToPub();
                ledger.CompleteShift(Paid(40f));
            }

            Assert.Less(ledger.Stats.Energy, 60f, "The pub every night should leave Kuba tired at the sink");
            Assert.Greater(ledger.Stats.Energy, 25f, "...but it levels off instead of flattening him");
        }

        [Test]
        public void AQuietEvening_OrARestfulOne_BringsKubaBack()
        {
            LifeLedger party = PastTheFirstDays(new LifeLedger(Rules, null));
            LifeLedger resting = PastTheFirstDays(new LifeLedger(Rules, null));
            LifeLedger quiet = PastTheFirstDays(new LifeLedger(Rules, null));
            for (int i = 0; i < 14; i++)
            {
                party.TryGoToPub();
                party.CompleteShift(Paid(60f));
                resting.TryRest();
                resting.CompleteShift(Paid(60f));
                quiet.CompleteShift(Paid(60f));
            }

            Assert.Greater(resting.Stats.Energy, quiet.Stats.Energy, "Resting gives more than doing nothing");
            Assert.Greater(quiet.Stats.Energy, party.Stats.Energy, "A quiet evening gives more than the pub");
            Assert.GreaterOrEqual(quiet.Stats.Energy, Fatigue.RestedEnergy, "Plain shifts and quiet evenings keep Kuba rested");

            // One evening's rest after the party weeks is felt the next day.
            float tired = party.Stats.Energy;
            Assert.IsTrue(party.TryRest());
            Assert.AreEqual(tired + Rules.RestEnergy, party.Stats.Energy, 1e-3f);
        }

        [Test]
        public void OneEvening_OnePlan_RestThePubTheCrewOrALetter()
        {
            LifeLedger ledger = PastTheFirstDays(new LifeLedger(Rules, null));
            ledger.CompleteShift(Paid(200f));
            Assert.IsTrue(ledger.TryRest());
            Assert.AreEqual(EveningActivity.Rest, ledger.TonightsActivity);
            Assert.IsFalse(ledger.TryGoToPub(), "Rested and then out to the pub the same evening");
            Assert.IsFalse(ledger.CanDo(EveningActivity.Pub, out string why));
            Assert.AreEqual("inne plany", why);
            Assert.IsFalse(ledger.TryCallMom(), "The phone box is another evening's plan");

            ledger.CompleteShift(Paid(40f));
            Assert.AreEqual(EveningActivity.None, ledger.TonightsActivity, "A new evening, a new plan");
            Assert.IsTrue(ledger.TryGoToPub());

            // With a mobile of his own, the call home fits in beside the evening's plan.
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = 200f });
            Assert.IsTrue(ledger.TryBuy(Purchase.Phone));
            Assert.IsTrue(ledger.TryCallMom(), "A mobile's call takes no evening");
            Assert.AreEqual(EveningActivity.Pub, ledger.TonightsActivity);
        }

        [Test]
        public void ThePubAndTransfersHome_ComeAfterTheFirstDays()
        {
            var ledger = new LifeLedger(Rules, null);
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = 200f });
            Assert.IsFalse(ledger.TryGoToPub(), "The pub on the first evening");
            Assert.AreEqual(0f, ledger.SendMoneyHome(20f), "A transfer home before the first wage");
            PastTheFirstDays(ledger);
            Assert.IsTrue(ledger.TryGoToPub());
            Assert.Greater(ledger.SendMoneyHome(20f), 0f);
        }

        [Test]
        public void Homesickness_IsFelt_InSleepAndInThePub_ButAlwaysHasAWayBack()
        {
            LifeLedger homesick = PastTheFirstDays(new LifeLedger(Rules, null));
            LifeLedger settled = PastTheFirstDays(new LifeLedger(Rules, null));
            homesick.ApplyStoryOutcome(new StoryOutcome { homesickness = 90f, energy = -40f });
            settled.ApplyStoryOutcome(new StoryOutcome { homesickness = -90f, energy = -40f });
            homesick.CompleteShift(Paid(40f));
            settled.CompleteShift(Paid(40f));
            Assert.Greater(settled.Stats.Energy, homesick.Stats.Energy, "Lying awake homesick sleeps no worse than settled in");

            // Very homesick, the pub only half reaches him.
            homesick.ApplyStoryOutcome(new StoryOutcome { homesickness = 100f, moneyEuros = 50f });
            float integration = homesick.Stats.Integration;
            Assert.IsTrue(homesick.TryGoToPub());
            Assert.AreEqual(integration + Rules.PubIntegrationGain * 0.5f, homesick.Stats.Integration, 1e-3f);

            // And a letter home is on offer, and eases it.
            homesick.CompleteShift(Paid(40f));
            Assert.IsTrue(homesick.LetterOffered);
            float before = homesick.Stats.Homesickness;
            Assert.IsTrue(homesick.TryWriteLetter());
            Assert.AreEqual(before - Rules.LetterRelief, homesick.Stats.Homesickness, 1e-3f);
        }

        [Test]
        public void TheCrew_AsksKubaRound_OnceHeIsOneOfThem()
        {
            LifeLedger ledger = PastTheFirstDays(new LifeLedger(Rules, null));
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = 100f });
            while (ledger.DayNumber % Rules.CrewDinnerEveryDays != 0)
            {
                ledger.CompleteShift(Paid(40f));
            }

            Assert.IsFalse(ledger.CrewDinnerOffered, "A newcomer is not asked round yet");
            ledger.ApplyStoryOutcome(new StoryOutcome { integration = Rules.CrewDinnerIntegration });
            Assert.IsTrue(ledger.CrewDinnerOffered);
            float wallet = ledger.Wallet;
            float homesick = ledger.Stats.Homesickness;
            Assert.IsTrue(ledger.TryCrewDinner());
            Assert.AreEqual(wallet - Rules.CrewDinnerCost, ledger.Wallet, 1e-3f);
            Assert.Less(ledger.Stats.Homesickness, homesick);
        }

        [Test]
        public void ThingsBought_OnceEach_NeverWithTheRent_AndTheyHelp()
        {
            LifeLedger ledger = PastTheFirstDays(new LifeLedger(Rules, null));
            Assert.IsFalse(ledger.TryBuy(Purchase.Phone), "A mobile with twenty euros in the wallet");
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = 200f });
            Assert.IsTrue(ledger.TryBuy(Purchase.Mattress));
            Assert.IsFalse(ledger.TryBuy(Purchase.Mattress), "Bought twice");

            LifeLedger plain = PastTheFirstDays(new LifeLedger(Rules, null));
            ledger.ApplyStoryOutcome(new StoryOutcome { energy = -50f });
            plain.ApplyStoryOutcome(new StoryOutcome { energy = -50f });
            ledger.CompleteShift(Paid(0f));
            plain.CompleteShift(Paid(0f));
            Assert.AreEqual(plain.Stats.Energy + Rules.MattressSleepBonus, ledger.Stats.Energy, 1e-3f, "The mattress sleeps better");

            Assert.IsTrue(ledger.TryBuy(Purchase.Phone));
            var data = new SaveData();
            ledger.WriteTo(data);
            var reloaded = new LifeLedger(Rules, null);
            reloaded.LoadFrom(data);
            Assert.IsTrue(reloaded.Has(Purchase.Mattress) && reloaded.Has(Purchase.Phone) && !reloaded.Has(Purchase.Dictionary));
        }

        [Test]
        public void AHardNight_TakesMoreOutOfKuba_ThanACalmOne_AndWalkingOutEarlyLess()
        {
            var calm = new LifeLedger(Rules, null);
            var hard = new LifeLedger(Rules, null);
            var early = new LifeLedger(Rules, null);
            calm.CompleteShift(new ShiftResult { EnergyCost = 16f, PlannedSeconds = 180f, ElapsedSeconds = 180f });
            hard.CompleteShift(new ShiftResult { EnergyCost = 30f, PlannedSeconds = 180f, ElapsedSeconds = 180f });
            early.CompleteShift(new ShiftResult { EnergyCost = 30f, PlannedSeconds = 180f, ElapsedSeconds = 30f });
            Assert.Greater(calm.Stats.Energy, hard.Stats.Energy);
            Assert.Greater(early.Stats.Energy, hard.Stats.Energy);
        }

        [Test]
        public void MomAndThePub_OnceAnEvening()
        {
            LifeLedger ledger = PastTheFirstDays(new LifeLedger(Rules, null));
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = 200f }); // the pub's money, above the week's bills

            Assert.IsTrue(ledger.TryCallMom());
            Assert.IsFalse(ledger.TryCallMom(), "A second call the same evening");
            Assert.IsFalse(ledger.TryGoToPub(), "The call from the phone box took the evening");

            ledger.CompleteShift(Paid(40f));
            Assert.IsTrue(ledger.TryGoToPub(), "A new evening");
            Assert.IsFalse(ledger.TryGoToPub(), "A second night out the same evening");
            Assert.IsFalse(ledger.TryCallMom(), "The pub took the evening");

            ledger.CompleteShift(Paid(40f));
            Assert.IsTrue(ledger.TryCallMom(), "A new day, a new call");
        }

        [Test]
        public void ThePub_NeverDrinksTheBillsAlreadyRunUp()
        {
            var ledger = new LifeLedger(Rules, null);
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = -Rules.StartingWallet });
            for (int i = 0; i < 5; i++)
            {
                ledger.CompleteShift(Paid(Rules.WeeklyBills / Rules.DaysPerWeek)); // just covering the days
            }

            float wallet = ledger.Wallet;
            Assert.Less(wallet - Rules.PubCost, ledger.BillsReserve);
            Assert.IsFalse(ledger.TryGoToPub());
            Assert.AreEqual(wallet, ledger.Wallet);
        }

        [Test]
        public void CanSpare_IsWhatIsLeftAboveTheBillsAlreadyRunUp()
        {
            var ledger = new LifeLedger(Rules, null);
            for (int i = 0; i < 3; i++)
            {
                ledger.CompleteShift(Paid(40f));
            }

            float spare = ledger.Wallet - ledger.BillsReserve;
            Assert.Greater(spare, 1f);
            Assert.IsTrue(ledger.CanSpare(spare - 0.5f));
            Assert.IsTrue(ledger.CanSpare(spare));
            Assert.IsFalse(ledger.CanSpare(spare + 0.5f), "A story choice may not spend the rent");
            Assert.IsTrue(ledger.CanSpare(0f), "Free choices are always affordable");
        }

        [Test]
        public void SendingMoneyHome_LeavesTheBillsAlreadyRunUp()
        {
            var ledger = new LifeLedger(Rules, null);
            for (int i = 0; i < 3; i++)
            {
                ledger.CompleteShift(Paid(60f));
            }

            float reserve = ledger.BillsReserve;
            float sendable = ledger.Wallet - reserve;
            Assert.Greater(sendable, 0f);

            float pln = ledger.SendMoneyHome(10000f);

            // Transfers go in whole cents, so "everything" is exact to the cent.
            Assert.AreEqual(reserve, ledger.Wallet, 0.01f);
            Assert.AreEqual(sendable, ledger.SentHomeEuros, 0.01f);
            Assert.AreEqual(ledger.SentHomeEuros * 4f, pln, 1e-3f, "No exchange table -> flat 4 PLN/EUR");
            Assert.AreEqual(0f, ledger.SendMoneyHome(20f), "Nothing more to send");
            Assert.GreaterOrEqual(ledger.Wallet, reserve - 1e-4f, "The bills were touched");
        }

        /// <summary>Lives whole weeks on a wage too small for the bills (a player walking out of every shift early).</summary>
        private static void LiveWeeks(LifeLedger ledger, int weeks, float dailyWage)
        {
            for (int i = 0; i < weeks * Rules.DaysPerWeek; i++)
            {
                ledger.CompleteShift(Paid(dailyWage));
            }
        }

        [Test]
        public void AWeekThatCannotPayTheBills_IsArrears_WithALateFee_AndABadNight()
        {
            var ledger = new LifeLedger(Rules, null);
            var settled = new List<(float before, float after)>();
            ledger.OnWeekSettled += (before, after) => settled.Add((before, after));

            LiveWeeks(ledger, 1, 10f);

            Assert.AreEqual(1, ledger.WeeksInArrears);
            Assert.IsFalse(ledger.InHostel);
            float expected = Rules.StartingWallet + 7 * 10f - Rules.WeeklyBills - Rules.LateFee;
            Assert.AreEqual(expected, ledger.Wallet, 1e-3f);
            Assert.AreEqual(expected, settled[0].after, 1e-3f, "The week's end reports the debt with its fee");
            Assert.Greater(ledger.Stats.Homesickness, 30f + 7 * Rules.HomesicknessPerDay, "Debt weighs on him");
        }

        [Test]
        public void ThreeWeeksInArrears_PutKubaInAHostel_ThatCostsMore_AndSleepsWorse()
        {
            var ledger = new LifeLedger(Rules, null);
            LiveWeeks(ledger, Rules.WeeksBeforeHostel - 1, 10f);
            Assert.IsFalse(ledger.InHostel, "Warned first");

            LiveWeeks(ledger, 1, 10f);
            Assert.IsTrue(ledger.InHostel);
            Assert.AreEqual(Rules.WeeklyBills + Rules.HostelExtra, ledger.WeeklyBills, 1e-3f);

            // The same night's sleep gives back less in a dorm than in the room.
            var room = new LifeLedger(Rules, null);
            room.ApplyStoryOutcome(new Zmywak.Core.Data.StoryOutcome { energy = ledger.Stats.Energy - 100f });
            float roomBefore = room.Stats.Energy;
            float hostelBefore = ledger.Stats.Energy;
            room.CompleteShift(Paid(10f));
            ledger.CompleteShift(Paid(10f));
            Assert.Less(ledger.Stats.Energy - hostelBefore, room.Stats.Energy - roomBefore);
        }

        [Test]
        public void AWeekThatEndsPaidUp_EndsTheArrears_AndTheHostel()
        {
            var ledger = new LifeLedger(Rules, null);
            LiveWeeks(ledger, Rules.WeeksBeforeHostel, 10f);
            Assert.IsTrue(ledger.InHostel);

            // Back at full shifts: enough to clear the debt and the dearer week.
            LiveWeeks(ledger, 2, 90f);

            Assert.GreaterOrEqual(ledger.Wallet, 0f);
            Assert.AreEqual(0, ledger.WeeksInArrears);
            Assert.IsFalse(ledger.InHostel, "Paid up: back in a room");
            Assert.AreEqual(Rules.WeeklyBills, ledger.WeeklyBills, 1e-3f);
        }

        [Test]
        public void Arrears_AndTheHostel_SurviveASaveAndLoad()
        {
            var ledger = new LifeLedger(Rules, null);
            LiveWeeks(ledger, Rules.WeeksBeforeHostel, 10f);

            var data = new SaveData();
            ledger.WriteTo(data);
            var reloaded = new LifeLedger(Rules, null);
            reloaded.LoadFrom(data);

            Assert.AreEqual(ledger.WeeksInArrears, reloaded.WeeksInArrears);
            Assert.IsTrue(reloaded.InHostel);
            Assert.AreEqual(ledger.Wallet, reloaded.Wallet, 1e-4f);
        }

        [Test]
        public void TheStoryCannotTakeMoreThanTheWalletHolds()
        {
            var ledger = new LifeLedger(Rules, null);
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = -500f });

            Assert.AreEqual(0f, ledger.Wallet);
        }

        [Test]
        public void TheEveningsChoices_AndWhatWentHome_SurviveASaveAndLoad()
        {
            LifeLedger ledger = PastTheFirstDays(new LifeLedger(Rules, null));
            ledger.CompleteShift(Paid(100f));
            ledger.TryGoToPub();
            ledger.SendMoneyHome(20f);

            var data = new SaveData();
            ledger.WriteTo(data);
            var reloaded = new LifeLedger(Rules, null);
            reloaded.LoadFrom(data);

            Assert.AreEqual(ledger.Wallet, reloaded.Wallet, 1e-4f);
            Assert.AreEqual(ledger.DayNumber, reloaded.DayNumber);
            Assert.AreEqual(ledger.CurrentDate, reloaded.CurrentDate);
            Assert.AreEqual(ledger.SentHomeEuros, reloaded.SentHomeEuros, 1e-4f);
            Assert.AreEqual(ledger.SentHomePln, reloaded.SentHomePln, 1e-4f);
            Assert.IsFalse(reloaded.TryCallMom(), "Restarting the app gives the evening back for a call home");
            Assert.IsTrue(reloaded.WentToPubToday, "Restarting the app is not a second night out");
            Assert.AreEqual(EveningActivity.Pub, reloaded.TonightsActivity, "Restarting the app is not a second evening");
        }

        // ------------------------------------------------------------------
        // After the epilogue: Kraków and the Pilbara (no rate table: 4 złoty to the euro, ECB averages for the dollar)
        // ------------------------------------------------------------------

        private static readonly System.DateTime Bailout = new System.DateTime(2010, 11, 29);

        private static LifeLedger MovedTo(Place place, float euros)
        {
            LifeLedger ledger = PastTheFirstDays(new LifeLedger(Rules, null)); // years in: the pub and transfers long since open
            ledger.SkipTo(Bailout);
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = euros - ledger.Wallet });
            Assert.IsTrue(ledger.MoveTo(place));
            return ledger;
        }

        [Test]
        public void Moving_ChangesTheWalletIntoTheNewMoney_AtTheYearsRate()
        {
            Assert.AreEqual(400f, MovedTo(Place.Krakow, 100f).Wallet, 1e-3f, "100 EUR in złoty, 2010");
            Assert.AreEqual(144f, MovedTo(Place.Pilbara, 100f).Wallet, 1e-3f, "100 EUR in Australian dollars, 2010");

            LifeLedger krakow = MovedTo(Place.Krakow, 100f);
            Assert.IsFalse(krakow.MoveTo(Place.Krakow), "Moving to where he lives is no move");
            Assert.AreEqual(400f, krakow.Wallet, 1e-3f);
            StringAssert.EndsWith("zł", krakow.Format(krakow.Wallet));
        }

        [Test]
        public void InKrakow_TheBillsThePubAndTheWageAreInZloty()
        {
            LifeLedger ledger = MovedTo(Place.Krakow, 100f);

            Assert.AreSame(LifeRules.Krakow, ledger.Rules);
            Assert.AreEqual(LifeRules.Krakow.WeeklyBills, ledger.WeeklyBills, 1e-3f);
            Assert.Less(ledger.WeeklyBills / 4f, LifeRules.Default.WeeklyBillsIn(2010), "Living in Kraków costs less than in Dublin, in euros");
            Assert.IsTrue(ledger.TryGoToPub());
            Assert.AreEqual(400f - LifeRules.Krakow.PubCost, ledger.Wallet, 1e-3f);
        }

        [Test]
        public void SendingHome_FromKrakow_ArrivesZlotyForZloty_AndFromThePilbaraAtTheRate()
        {
            LifeLedger krakow = MovedTo(Place.Krakow, 100f);
            Assert.AreEqual(100f, krakow.SendMoneyHome(100f), 1e-3f);
            Assert.AreEqual(25f, krakow.SentHomeEuros, 1e-3f, "Kept as euros of the day, too");

            LifeLedger pilbara = MovedTo(Place.Pilbara, 100f);
            Assert.AreEqual(50f / 1.44f * 4f, pilbara.SendMoneyHome(50f), 1e-2f);
        }

        [Test]
        public void TheStorysEuros_ArriveInTheMoneyOfThePlace()
        {
            LifeLedger ledger = MovedTo(Place.Pilbara, 100f);
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = 50f }); // Marek pays back his fifty euros
            Assert.AreEqual(144f + 72f, ledger.Wallet, 1e-3f);

            ledger.CompleteShift(Paid(0f)); // a day of the week lived: a seventh of the bills is set aside
            float spare = ledger.Wallet - ledger.BillsReserve;
            Assert.IsTrue(ledger.CanSpareEuros(spare / 1.44f - 1f));
            Assert.IsFalse(ledger.CanSpareEuros(spare / 1.44f + 1f), "A story choice in euros may not spend the rent in dollars either");
        }

        [Test]
        public void ANewLandlord_IsACleanSlate_AndThePlaceSurvivesASaveAndLoad()
        {
            var ledger = new LifeLedger(Rules, null);
            ledger.ApplyStoryOutcome(new StoryOutcome { moneyEuros = -Rules.StartingWallet });
            for (int day = 0; day < 7; day++)
            {
                ledger.CompleteShift(Paid(0f));
            }

            Assert.AreEqual(1, ledger.WeeksInArrears);
            ledger.SkipTo(Bailout);
            ledger.MoveTo(Place.Krakow);
            Assert.AreEqual(0, ledger.WeeksInArrears, "A move leaves the Dublin landlord behind");

            var data = new SaveData();
            ledger.WriteTo(data);
            var reloaded = new LifeLedger(Rules, null);
            reloaded.LoadFrom(data);
            Assert.AreEqual(Place.Krakow, reloaded.Place);
            Assert.AreSame(LifeRules.Krakow, reloaded.Rules);
            Assert.AreEqual(ledger.Wallet, reloaded.Wallet, 1e-4f);
        }
    }
}
