using System;
using System.Collections.Generic;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Core.Save;
using Zmywak.Meta.Economy;
using Zmywak.Meta.Stats;

namespace Zmywak.Meta.Progress
{
    /// <summary>
    /// Kuba's life between shifts as plain state and rules: the wallet, the three stats, the calendar, the
    /// weekly bills and the evening's choices. No scene objects and no saving - <see cref="MetaProgressController"/>
    /// wraps it for the game, and the economy simulator test drives it straight through weeks of shifts.
    /// Money is in the currency of the place Kuba lives in (<see cref="Place"/>): euros in Dublin, and after a move
    /// (<see cref="MoveTo"/>) złoty in Kraków or Australian dollars in the Pilbara. The story's sums are written in
    /// euros and arrive in the local money at the year's rate.
    /// </summary>
    public sealed class LifeLedger
    {
        private readonly ExchangeRateService _rates;
        private WeeklyBudgetController _budget;
        private int _lastCallMomDay;
        private int _lastPubDay;
        private int _lastEveningDay;
        private EveningActivity _lastEvening;
        private readonly HashSet<Purchase> _purchases = new HashSet<Purchase>();

        public LifeLedger(LifeRules rules, ExchangeRateService rates)
        {
            Rules = rules ?? LifeRules.Default;
            _rates = rates ?? new ExchangeRateService(null);
            _budget = new WeeklyBudgetController(Rules);
            Wallet = Rules.StartingWallet;
        }

        /// <summary>The rules of the place Kuba lives in.</summary>
        public LifeRules Rules { get; private set; }

        public Place Place => Rules.Place;
        public PlayerStatsController Stats { get; } = new PlayerStatsController();

        /// <summary>The wallet, in the place's money (<see cref="PlaceMoney.Unit"/>).</summary>
        public float Wallet { get; private set; }

        public int DayNumber { get; private set; } = 1;
        public DateTime CurrentDate { get; private set; } = new DateTime(2004, 5, 1);

        /// <summary>Everything sent home so far, as euros at the rate of the day it went.</summary>
        public float SentHomeEuros { get; private set; }

        /// <summary>Everything sent home so far, in the złoty it arrived as.</summary>
        public float SentHomePln { get; private set; }

        /// <summary>Everything Kuba has done at the sink - what promotions are judged on (Career).</summary>
        public CareerRecord WorkRecord { get; private set; } = new CareerRecord();

        /// <summary>His rank at the sink: earned for good, never taken away.</summary>
        public int Rank { get; private set; }

        public bool CalledMomToday => _lastCallMomDay == DayNumber;
        public bool WentToPubToday => _lastPubDay == DayNumber;

        /// <summary>Tonight's plan, once made (None: an evening at home with nothing on, which sleeps a little better).</summary>
        public EveningActivity TonightsActivity => _lastEveningDay == DayNumber ? _lastEvening : EveningActivity.None;

        /// <summary>Whether Kuba owns something bought in the evenings (once each).</summary>
        public bool Has(Purchase purchase) => _purchases.Contains(purchase);

        public IReadOnlyCollection<Purchase> Purchases => _purchases;

        /// <summary>A sum in the place's money, written for the screen: "12.50 EUR", "48.00 zł".</summary>
        public string Format(float amount) => PlaceMoney.Format(amount, Place);

        /// <summary>
        /// The share of this week's bills already run up - one seventh for every day of the week lived so
        /// far. Neither the pub nor a transfer home may touch it. Together with a day's wage covering a
        /// day's bills, that keeps the week's end payable however the evenings are spent.
        /// </summary>
        public float BillsReserve
        {
            get
            {
                if (Rules.DaysPerWeek <= 0)
                {
                    return 0f;
                }

                int daysLivedThisWeek = (DayNumber - 1) % Rules.DaysPerWeek;
                return WeeklyBills * daysLivedThisWeek / Rules.DaysPerWeek;
            }
        }

        /// <summary>What can go home tonight without leaving the bills short - in whole cents, rounded down (a
        /// transfer is not made of float dust: sending "everything" once must leave nothing more to send).</summary>
        public float Sendable => Mathf.Max(0f, Mathf.Floor((Wallet - BillsReserve) * 100f + 0.001f) / 100f);

        /// <summary>Whether Kuba can spend this much of the place's money tonight without touching the bills run up
        /// this week - the rule the pub, a transfer home and a story choice that costs money all keep.</summary>
        public bool CanSpare(float amount) => amount <= 0f || Wallet - amount >= BillsReserve - 0.001f;

        /// <summary><see cref="CanSpare"/> for a sum the story names in euros.</summary>
        public bool CanSpareEuros(float euros) => CanSpare(FromEuros(euros));

        /// <summary>A sum in euros, in the place's money at this year's rate (to the cent).</summary>
        public float FromEuros(float euros) => Mathf.Round(_rates.FromEuros(euros, Place, CurrentDate.Year) * 100f) / 100f;

        /// <summary>Flavour and bookkeeping lines for the hub: "Jesz coś ciepłego?", the week's settlement, ...</summary>
        public event Action<string> OnNotice;

        /// <summary>A week has been settled: wallet before and after the bills.</summary>
        public event Action<float, float> OnWeekSettled;

        public void LoadFrom(SaveData data)
        {
            // Day 0: the week was never written (the story's flags or a tutorial's mark can reach the file first, and a phone
            // that dies that first evening never wrote the rest). It is a new life, then - not one with 0 EUR and no energy.
            if (data.dayNumber <= 0)
            {
                return;
            }

            if ((Place)data.place != Place)
            {
                Settle(LifeRules.For((Place)data.place));
            }

            Wallet = data.walletEuros;
            Stats.LoadFrom(data.energy, data.homesickness, data.integration);
            DayNumber = Mathf.Max(1, data.dayNumber);
            if (DateTime.TryParse(data.currentDateIso, out DateTime parsed))
            {
                CurrentDate = parsed;
            }

            _lastCallMomDay = data.lastCallMomDay;
            _lastPubDay = data.lastPubDay;
            SentHomeEuros = data.sentHomeEuros;
            SentHomePln = data.sentHomePln;
            WeeksInArrears = data.weeksInArrears;
            InHostel = data.inHostel;
            WorkRecord = data.career ?? new CareerRecord();
            Rank = data.rank;
            BreakdownTutorialDone = data.breakdownTutorialDone;
            LastShiftBrokeDown = data.lastShiftBrokeDown;
            _lastEveningDay = data.lastEveningDay;
            _lastEvening = (EveningActivity)data.lastEveningActivity;
            _purchases.Clear();
            if (data.purchases != null)
            {
                foreach (string bought in data.purchases)
                {
                    if (Enum.TryParse(bought, out Purchase purchase))
                    {
                        _purchases.Add(purchase);
                    }
                }
            }
        }

        public void WriteTo(SaveData data)
        {
            data.place = (int)Place;
            data.walletEuros = Wallet;
            data.energy = Stats.Energy;
            data.homesickness = Stats.Homesickness;
            data.integration = Stats.Integration;
            data.dayNumber = DayNumber;
            data.currentDateIso = CurrentDate.ToString("O");
            data.lastCallMomDay = _lastCallMomDay;
            data.lastPubDay = _lastPubDay;
            data.sentHomeEuros = SentHomeEuros;
            data.sentHomePln = SentHomePln;
            data.weeksInArrears = WeeksInArrears;
            data.inHostel = InHostel;
            data.career = WorkRecord;
            data.rank = Rank;
            data.breakdownTutorialDone = BreakdownTutorialDone;
            data.lastShiftBrokeDown = LastShiftBrokeDown;
            data.lastEveningDay = _lastEveningDay;
            data.lastEveningActivity = (int)_lastEvening;
            data.purchases = new List<string>();
            foreach (Purchase purchase in _purchases)
            {
                data.purchases.Add(purchase.ToString());
            }
        }

        /// <summary>Kuba has met the dishwasher breaking down (the scripted one, or any): from now on breakdowns come by chance.</summary>
        public bool BreakdownTutorialDone { get; private set; }

        /// <summary>The machine broke down on the last shift: the next one is mostly spared (ShiftSchedule).</summary>
        public bool LastShiftBrokeDown { get; private set; }

        /// <summary>The day's shift: its kind, pace and deal, and what the dishwasher may do (<see cref="ShiftSchedule.For"/>).</summary>
        public ScheduledShift PlanShift(SpecialShift special) =>
            ShiftSchedule.For(DayNumber, special, BreakdownTutorialDone, LastShiftBrokeDown);

        /// <summary>A shift is over: the wage, the tiredness, the night's sleep, the next day - and the
        /// bills, when it was the week's last.</summary>
        public void CompleteShift(ShiftResult result)
        {
            Wallet += result.Wage;
            RecordWork(result);
            LastShiftBrokeDown = result.Breakdowns > 0;
            BreakdownTutorialDone |= LastShiftBrokeDown;
            Stats.ChangeEnergy(-ShiftEnergyCost(result));
            // The night after: deeper the more tired, better after a quiet evening and on a real mattress, worse homesick or
            // in a hostel dorm.
            Stats.ChangeEnergy(Rules.NightsSleep(Stats.Energy, Stats.Homesickness, TonightsActivity == EveningActivity.None,
                Has(Purchase.Mattress), InHostel));
            Stats.ChangeHomesickness(Rules.HomesicknessPerDay);

            DayNumber++;
            CurrentDate = CurrentDate.AddDays(1);

            if (Rules.DaysPerWeek > 0 && (DayNumber - 1) % Rules.DaysPerWeek == 0)
            {
                SettleWeek();
            }
        }

        /// <summary>
        /// What a shift took out of Kuba: its kind's cost (a calm one less, a hard night more - or the plain shift's, for a
        /// result that does not say), for the share of it he worked - walking out early tires him less.
        /// </summary>
        public float ShiftEnergyCost(ShiftResult result)
        {
            float cost = result.EnergyCost > 0f ? result.EnergyCost : Rules.ShiftEnergyCost;
            float share = result.PlannedSeconds > 0f ? Mathf.Clamp01(result.ElapsedSeconds / result.PlannedSeconds) : 1f;
            return cost * Mathf.Lerp(0.4f, 1f, share);
        }

        /// <summary>The shift goes on Kuba's record; a rank earned is announced (and pays from the next shift).</summary>
        private void RecordWork(ShiftResult result)
        {
            WorkRecord.Record(result);
            int earned = Career.RankFor(WorkRecord);
            if (earned <= Rank)
            {
                return;
            }

            Rank = earned;
            string raise = Format(Career.HourlyBonusPerRankEuros * PlaceMoney.SmallSums(Place));
            string boss = Place == Place.Dublin ? "Declan" : "Szef";
            OnNotice?.Invoke(Rank >= Career.KingRank
                ? $"{boss} wiesza nad zlewem tekturową koronę: KRÓL ZMYWAKA. I dorzuca {raise} do każdej godziny."
                : $"Awans: {Career.Title(Rank)}! {boss} dorzuca {raise} do każdej godziny.");
        }

        private void SettleWeek()
        {
            float before = Wallet;
            Wallet -= WeeklyBills;
            OnNotice?.Invoke($"Koniec tygodnia: {Rules.BillsDescription} kosztowały {Format(WeeklyBills)}.");

            if (Wallet < 0f)
            {
                // Arrears: a late fee on top, a bad night, worry - and the landlord's patience runs down.
                WeeksInArrears++;
                Wallet -= Rules.LateFee;
                Stats.ChangeEnergy(-Rules.ArrearsEnergyHit);
                Stats.ChangeHomesickness(Rules.ArrearsHomesickness);
                if (!InHostel && WeeksInArrears >= Rules.WeeksBeforeHostel)
                {
                    InHostel = true;
                    OnNotice?.Invoke($"Właściciel wystawił walizkę za drzwi. Kuba śpi w hostelu: drożej (+{Format(Rules.HostelExtra)} na tydzień) " +
                                     "i głośno. Wróci do pokoju, gdy tydzień skończy się bez długu.");
                }
                else if (!InHostel && WeeksInArrears == Rules.WeeksBeforeHostel - 1)
                {
                    OnNotice?.Invoke($"Właściciel: \"Jeszcze tydzień zaległości i szukasz nowego lokum.\" Dług: {Format(-Wallet)}.");
                }
                else
                {
                    OnNotice?.Invoke($"Zabrakło na rachunki. Dług {Format(-Wallet)} (z karą {Format(Rules.LateFee)}) - Kuba źle śpi.");
                }
            }
            else
            {
                if (InHostel)
                {
                    InHostel = false;
                    OnNotice?.Invoke("Długi spłacone - Kuba wraca z hostelu do pokoju.");
                }

                WeeksInArrears = 0;
            }

            OnWeekSettled?.Invoke(before, Wallet);
        }

        /// <summary>This week's bills: the year's (the rent follows the boom and the bust), plus the hostel's dearer
        /// bed while Kuba is in one.</summary>
        public float WeeklyBills => _budget.TotalWeeklyExpensesIn(CurrentDate.Year) + (InHostel ? Rules.HostelExtra : 0f);

        /// <summary>
        /// Time passes between chapters: the calendar moves on to a later date (never back), while the days lived and
        /// the week's rhythm of bills carry on as they were - the years skipped are told, not played. Returns whether
        /// the date moved.
        /// </summary>
        public bool SkipTo(DateTime date)
        {
            if (date.Date <= CurrentDate.Date)
            {
                return false;
            }

            CurrentDate = date.Date;
            return true;
        }

        /// <summary>
        /// Kuba moves: from now on the place's rules and money. The wallet is changed into the new currency at the
        /// year's rate (a debt too), and a new landlord means a clean slate - no arrears, no hostel. Returns whether
        /// he moved (not if he already lives there).
        /// </summary>
        public bool MoveTo(Place place)
        {
            if (place == Place)
            {
                return false;
            }

            Place from = Place;
            float before = Wallet;
            float euros = _rates.ToEuros(Wallet, from, CurrentDate.Year);
            Settle(LifeRules.For(place));
            Wallet = Mathf.Round(_rates.FromEuros(euros, place, CurrentDate.Year) * 100f) / 100f;
            WeeksInArrears = 0;
            InHostel = false;
            OnNotice?.Invoke(from == Place.Dublin || place == Place.Dublin
                ? $"Przeprowadzka. Portfel: {PlaceMoney.Format(before, from)} to teraz {Format(Wallet)}."
                : $"Przeprowadzka. Portfel: {Format(Wallet)}.");
            return true;
        }

        private void Settle(LifeRules rules)
        {
            Rules = rules;
            _budget = new WeeklyBudgetController(rules);
        }

        /// <summary>Weeks in a row that ended short of the bills (0 = paid up).</summary>
        public int WeeksInArrears { get; private set; }

        /// <summary>Evicted after too long in arrears: dearer, noisier, until a week ends paid up.</summary>
        public bool InHostel { get; private set; }

        /// <summary>
        /// A call home: from the phone box on the corner it takes the evening (one plan an evening); from a mobile of his own,
        /// it fits in beside whatever else the evening holds. Once a day either way.
        /// </summary>
        public bool TryCallMom()
        {
            if (CalledMomToday)
            {
                OnNotice?.Invoke("Mama już dziś słyszała twój głos. Karta telefoniczna nie jest z gumy.");
                return false;
            }

            bool mobile = Has(Purchase.Phone);
            if (!mobile && TonightsActivity != EveningActivity.None)
            {
                OnNotice?.Invoke(PlansMade() + " Do budki pójdzie jutro.");
                return false;
            }

            _lastCallMomDay = DayNumber;
            if (!mobile)
            {
                PlanEvening(EveningActivity.CallHome);
            }

            Stats.ChangeHomesickness(-(Rules.CallMomHomesicknessRelief + (mobile ? Rules.PhoneHomesicknessBonus : 0f)));
            Stats.ChangeEnergy(Rules.CallMomEnergyRelief + (mobile ? Rules.PhoneEnergyBonus : 0f));
            OnNotice?.Invoke(mobile
                ? "Z komórki, z łóżka, bez kolejki do budki. Mama: \"Jesz coś ciepłego?\""
                : "Mama: \"Jesz coś ciepłego?\"");
            return true;
        }

        public bool TryGoToPub()
        {
            if (WentToPubToday)
            {
                OnNotice?.Invoke("Na dziś wystarczy. Jutro zmiana.");
                return false;
            }

            if (!MechanicUnlocks.IsUnlocked(Mechanic.Pub, DayNumber))
            {
                OnNotice?.Invoke("Ekipa jeszcze cię nie zna. Na piwo cię zaproszą, jak trochę popracujesz.");
                return false;
            }

            if (TonightsActivity != EveningActivity.None)
            {
                OnNotice?.Invoke(PlansMade());
                return false;
            }

            if (Wallet < Rules.PubCost)
            {
                OnNotice?.Invoke("Za mało pieniędzy na piwo z ekipą.");
                return false;
            }

            if (!CanSpare(Rules.PubCost))
            {
                OnNotice?.Invoke($"Kuba nie przepije czynszu: {Format(BillsReserve)} czeka na rachunki tygodnia.");
                return false;
            }

            _lastPubDay = DayNumber;
            PlanEvening(EveningActivity.Pub);
            Wallet -= Rules.PubCost;
            // Very homesick, he sits there thinking of home: half the evening reaches him.
            bool faraway = Stats.Homesickness >= Rules.HomesickPubThreshold;
            Stats.ChangeIntegration(Rules.PubIntegrationGain * (faraway ? 0.5f : 1f));
            Stats.ChangeEnergy(-Rules.PubEnergyCost);
            Stats.ChangeHomesickness(-Rules.PubHomesicknessRelief);
            OnNotice?.Invoke(faraway ? "Kuba siedzi w pubie z ekipą, ale myślami jest w domu." : Rules.PubNotice);
            return true;
        }

        private void PlanEvening(EveningActivity activity)
        {
            _lastEveningDay = DayNumber;
            _lastEvening = activity;
        }

        private string PlansMade()
        {
            switch (TonightsActivity)
            {
                case EveningActivity.Rest:
                    return "Dziś wieczór na odpoczynek. Plany na jutro.";
                case EveningActivity.Pub:
                    return "Dziś już był pub.";
                case EveningActivity.CrewDinner:
                    return "Dziś kolacja z ekipą. Na resztę nie ma już wieczoru.";
                case EveningActivity.Letter:
                    return "Dziś wieczór przy liście do domu.";
                case EveningActivity.CallHome:
                    return "Dziś wieczór przy telefonie do domu.";
                default:
                    return string.Empty;
            }
        }

        /// <summary>Whether an evening plan is on offer tonight - and if not, why (a few words for the hub's button).</summary>
        public bool CanDo(EveningActivity activity, out string why)
        {
            why = null;
            if (activity == EveningActivity.CallHome && Has(Purchase.Phone))
            {
                why = CalledMomToday ? "zrobione" : null;
                return !CalledMomToday;
            }

            if (TonightsActivity != EveningActivity.None)
            {
                why = TonightsActivity == activity ? "zrobione" : "inne plany";
                return false;
            }

            switch (activity)
            {
                case EveningActivity.Rest:
                    return true;
                case EveningActivity.CallHome:
                    if (CalledMomToday)
                    {
                        why = "zrobione";
                        return false;
                    }

                    return true;
                case EveningActivity.Pub:
                    if (!MechanicUnlocks.IsUnlocked(Mechanic.Pub, DayNumber))
                    {
                        why = "od dnia " + MechanicUnlocks.DayOf(Mechanic.Pub); // a no-break space: the day never wraps alone
                        return false;
                    }

                    if (Wallet < Rules.PubCost || !CanSpare(Rules.PubCost))
                    {
                        why = BillsMoney;
                        return false;
                    }

                    return true;
                case EveningActivity.CrewDinner:
                    if (!CrewDinnerOffered)
                    {
                        why = "nie dziś";
                        return false;
                    }

                    if (Wallet < Rules.CrewDinnerCost || !CanSpare(Rules.CrewDinnerCost))
                    {
                        why = BillsMoney;
                        return false;
                    }

                    return true;
                case EveningActivity.Letter:
                    if (!LetterOffered)
                    {
                        why = "nie trzeba";
                        return false;
                    }

                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Why a plan that costs money is not on: the week's bills come first.</summary>
        public const string BillsMoney = "pieniądze na rachunki";

        /// <summary>An evening off: Kuba stays in and rests (more energy for the next shift).</summary>
        public bool TryRest()
        {
            if (!CanDo(EveningActivity.Rest, out _))
            {
                OnNotice?.Invoke(PlansMade());
                return false;
            }

            PlanEvening(EveningActivity.Rest);
            Stats.ChangeEnergy(Rules.RestEnergy);
            OnNotice?.Invoke(Stats.Homesickness >= Rules.HomesickSleepThreshold
                ? "Wieczór w łóżku. Odpoczywa ciało, myśli krążą wokół domu."
                : "Wieczór w łóżku z herbatą. Jutro będzie lżej.");
            return true;
        }

        /// <summary>The crew asks Kuba round, every few days, once he is one of them (Integration).</summary>
        public bool CrewDinnerOffered => Stats.Integration >= Rules.CrewDinnerIntegration && Rules.CrewDinnerEveryDays > 0 &&
                                         DayNumber % Rules.CrewDinnerEveryDays == 0;

        public bool TryCrewDinner()
        {
            if (!CanDo(EveningActivity.CrewDinner, out string why))
            {
                OnNotice?.Invoke(why == BillsMoney
                    ? $"Kuba nie ma na składkę: {Format(BillsReserve)} czeka na rachunki tygodnia."
                    : TonightsActivity != EveningActivity.None ? PlansMade() : "Dziś nikt nie gotuje dla ekipy.");
                return false;
            }

            PlanEvening(EveningActivity.CrewDinner);
            Wallet -= Rules.CrewDinnerCost;
            Stats.ChangeEnergy(Rules.CrewDinnerEnergy);
            Stats.ChangeHomesickness(-Rules.CrewDinnerHomesicknessRelief);
            Stats.ChangeIntegration(Rules.CrewDinnerIntegrationGain);
            OnNotice?.Invoke("Ciepły obiad, stół pełen ludzi i nikt nie pyta, skąd jesteś. Tylko czy zjesz dokładkę.");
            return true;
        }

        /// <summary>Homesick enough, Kuba writes home instead of going anywhere.</summary>
        public bool LetterOffered => Stats.Homesickness >= Rules.LetterThreshold;

        public bool TryWriteLetter()
        {
            if (!CanDo(EveningActivity.Letter, out _))
            {
                OnNotice?.Invoke(TonightsActivity != EveningActivity.None ? PlansMade() : "Dziś nie ma o czym pisać.");
                return false;
            }

            PlanEvening(EveningActivity.Letter);
            Stats.ChangeHomesickness(-Rules.LetterRelief);
            OnNotice?.Invoke("Trzy strony do domu. O deszczu, o Declanie, o tym, że wszystko dobrze. Prawie wszystko.");
            return true;
        }

        /// <summary>What something costs here, in the place's money.</summary>
        public float PriceOf(Purchase purchase)
        {
            switch (purchase)
            {
                case Purchase.Mattress:
                    return Rules.MattressCost;
                case Purchase.Dictionary:
                    return Rules.DictionaryCost;
                default:
                    return Rules.PhoneCost;
            }
        }

        /// <summary>Buys something once - never with the money the bills need.</summary>
        public bool TryBuy(Purchase purchase)
        {
            if (Has(purchase))
            {
                return false;
            }

            float price = PriceOf(purchase);
            if (Wallet < price || !CanSpare(price))
            {
                OnNotice?.Invoke($"Na to trzeba odłożyć: {Format(BillsReserve)} czeka na rachunki tygodnia.");
                return false;
            }

            Wallet -= price;
            _purchases.Add(purchase);
            switch (purchase)
            {
                case Purchase.Mattress:
                    OnNotice?.Invoke("Prawdziwy materac zamiast kanapy. Plecy dziękują od pierwszej nocy.");
                    break;
                case Purchase.Dictionary:
                    OnNotice?.Invoke("Kieszonkowy słownik angielsko-polski. Kuba czyta go w autobusie.");
                    break;
                default:
                    OnNotice?.Invoke("Komórka na kartę. Do mamy można teraz zadzwonić z łóżka.");
                    break;
            }

            return true;
        }

        /// <summary>
        /// Sends up to <paramref name="amount"/> (the place's money) home - but never the bills run up so far this
        /// week: what the landlord and the shop are owed stays in the wallet. Returns the złoty that arrive.
        /// </summary>
        public float SendMoneyHome(float amount)
        {
            if (!MechanicUnlocks.IsUnlocked(Mechanic.SendMoney, DayNumber))
            {
                OnNotice?.Invoke("Najpierw trzeba coś zarobić. Przekazy do domu - od jutra.");
                return 0f;
            }

            amount = Mathf.Clamp(amount, 0f, Sendable);
            if (amount < 0.01f)
            {
                OnNotice?.Invoke($"Kuba odkłada {Format(BillsReserve)} na rachunki tygodnia. Wyśle to, co zostanie ponad nie.");
                return 0f;
            }

            int year = CurrentDate.Year;
            Wallet -= amount;
            float pln = _rates.ToPln(amount, Place, year);
            SentHomeEuros += _rates.ToEuros(amount, Place, year);
            SentHomePln += pln;
            OnNotice?.Invoke(Place == Place.Krakow
                ? $"Przelano {Format(amount)} {Rules.SendHomeTo}."
                : $"Przelano {Format(amount)} ({pln:0.00} PLN po kursie {year}: {_rates.ToPln(1f, Place, year):0.00}).");
            return pln;
        }

        /// <summary>
        /// What a story moment does to Kuba's week: money in or out - named in euros, paid in the place's money -
        /// (the wallet never goes below zero: what he cannot pay, he cannot pay) and nudges to his stats.
        /// </summary>
        public void ApplyStoryOutcome(StoryOutcome outcome)
        {
            if (outcome.IsEmpty)
            {
                return;
            }

            if (outcome.moneyEuros != 0f)
            {
                float before = Wallet;
                // A loss cannot take more than the wallet holds, but a debt is not the wallet's to forgive: with the wallet
                // already in the red the sum is plain - a gain pays the debt down (past zero, if it is big enough), a loss adds to it.
                float sum = before + FromEuros(outcome.moneyEuros);
                Wallet = before >= 0f ? Mathf.Max(0f, sum) : sum;
                float change = Wallet - before;
                if (change != 0f)
                {
                    OnNotice?.Invoke(change > 0f ? $"+{Format(change)}" : Format(change));
                }
            }

            Stats.ChangeEnergy(outcome.energy);
            Stats.ChangeHomesickness(outcome.homesickness);
            Stats.ChangeIntegration(outcome.integration);
        }
    }

    /// <summary>One evening's plan (one an evening); a call home and a transfer come on top of it.</summary>
    public enum EveningActivity
    {
        None,
        Rest,
        Pub,
        CrewDinner,
        Letter,
        CallHome
    }

    /// <summary>The few things worth buying in Kuba's life, once each.</summary>
    public enum Purchase
    {
        Mattress,
        Dictionary,
        Phone
    }
}
