using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Meta.Economy
{
    /// <summary>
    /// The numbers Kuba's life between shifts runs on, in one place: the game plays by them and the economy
    /// simulator test (EconomySimulationTests) lives weeks by exactly the same ones, so a change here that bankrupts a
    /// careful player or makes money meaningless fails a test. One set per place, every sum in that place's money
    /// (<see cref="PlaceMoney"/>): Dublin from 2004 (<see cref="Default"/>) - a bed in a shared room, a bus to the
    /// restaurant, a phone card to call home - and, after chapter 5's choice, Kraków or the Pilbara.
    /// </summary>
    public sealed class LifeRules
    {
        /// <summary>Dublin, where it all happens.</summary>
        public static readonly LifeRules Default = new LifeRules();

        /// <summary>
        /// Kraków from late 2010, in złoty: a room in a flat in Kazimierz (about 780 zł a month), a monthly MPK tram
        /// ticket (89 zł), a mobile; beer with the hotel's kitchen round the corner. Home is a train ride away, so
        /// homesickness grows slowly - the parents are a phone call and a Sunday away.
        /// </summary>
        public static readonly LifeRules Krakow = new LifeRules
        {
            Place = Place.Krakow,
            WeeklyRent = 180f,
            WeeklyFood = 120f,
            WeeklyPhoneCard = 15f,
            WeeklyTransport = 22f,
            BillsDescription = "pokój na Kazimierzu, jedzenie, bilet MPK i telefon",
            HomesicknessPerDay = 1f,
            PubCost = 40f,
            PubName = "Piwo z ekipą na Kazimierzu",
            PubNotice = "Piwo z hotelową kuchnią w knajpie na Kazimierzu. Integracja rośnie.",
            SendHomeStep = 100f,
            SendHomeTo = "rodzicom",
            LateFee = 40f,
            HostelExtra = 100f,
            CrewDinnerCost = 20f,
            CrewDinnerName = "Obiad u Ołeha",
            MattressCost = 120f,
            DictionaryCost = 30f,
            PhoneCost = 160f
        };

        /// <summary>
        /// The Pilbara from late 2010, in Australian dollars: the camp feeds and beds Kuba on his weeks on, so the
        /// bills are a room in a Perth share house for the weeks off, food and the drive to the airport, and the
        /// internet for Skype home. The pay is the mining boom's; the homesickness is the other side of the world's.
        /// </summary>
        public static readonly LifeRules Pilbara = new LifeRules
        {
            Place = Place.Pilbara,
            WeeklyRent = 150f,
            WeeklyFood = 50f,
            WeeklyPhoneCard = 20f,
            WeeklyTransport = 30f,
            BillsDescription = "pokój w Perth na tygodnie wolne, jedzenie, dojazdy i internet na Skype'a",
            HomesicknessPerDay = 4f,
            PubCost = 30f,
            PubName = "Wieczór w kantynie obozu",
            PubNotice = "Wieczór w obozowej kantynie z Gavinem i ekipą z kopalni. Integracja rośnie.",
            SendHomeStep = 50f,
            SendHomeTo = "do domu",
            LateFee = 15f,
            HostelExtra = 40f,
            CrewDinnerCost = 8f,
            CrewDinnerName = "Grill z Liamem przy kontenerach",
            MattressCost = 45f,
            DictionaryCost = 12f,
            PhoneCost = 70f
        };

        /// <summary>The rules of a place.</summary>
        public static LifeRules For(Place place)
        {
            switch (place)
            {
                case Place.Krakow:
                    return Krakow;
                case Place.Pilbara:
                    return Pilbara;
                default:
                    return Default;
            }
        }

        public Place Place { get; private set; } = Place.Dublin;

        public int DaysPerWeek { get; } = 7;
        /// <summary>What Kuba lands in Dublin with (the game starts there).</summary>
        public float StartingWallet { get; } = 20f;

        // The week's bills, all due at the end of every seventh day.
        public float WeeklyRent { get; private set; } = 80f;
        public float WeeklyFood { get; private set; } = 50f;
        public float WeeklyPhoneCard { get; private set; } = 15f;
        public float WeeklyTransport { get; private set; } = 20f;
        public float WeeklyBills => WeeklyRent + WeeklyFood + WeeklyPhoneCard + WeeklyTransport;

        /// <summary>What the week's bills are, for the settlement notice.</summary>
        public string BillsDescription { get; private set; } = "czynsz, jedzenie, bilet i karta telefoniczna";

        /// <summary>
        /// The week's rent in a given year, in the place's money. A bed in a shared Dublin room follows the boom and the
        /// bust: a third dearer by 2008, cheaper after the crash, then climbing again from 2013 as the city filled up
        /// (2004's is <see cref="WeeklyRent"/>). A room in Kazimierz creeps up a few złoty a year; a Perth room follows
        /// the mining boom up to 2012 and eases off after it. Kuba only lives in either from late 2010.
        /// </summary>
        public float WeeklyRentIn(int year)
        {
            switch (Place)
            {
                case Place.Krakow:
                    return year <= 2010 ? WeeklyRent : year == 2011 ? 185f : year == 2012 ? 190f : 195f;
                case Place.Pilbara:
                    return year <= 2010 ? WeeklyRent : year == 2011 ? 165f : year == 2012 ? 180f : year == 2013 ? 175f : 160f;
            }

            if (year <= 2004)
            {
                return WeeklyRent;
            }

            switch (year)
            {
                case 2005: return 85f;
                case 2006: return 92f;
                case 2007: return 100f;
                case 2008: return 104f;
                case 2009: return 94f;
                case 2010: return 86f;
                case 2011: return 84f;
                case 2012: return 86f;
                case 2013: return 92f;
                case 2014: return 100f;
                default: return 110f;
            }
        }

        /// <summary>The week's bills in a given year: the rent of that year, the rest as at the start.</summary>
        public float WeeklyBillsIn(int year) => WeeklyRentIn(year) + WeeklyFood + WeeklyPhoneCard + WeeklyTransport;

        // A shift takes energy - a plain one this much; each kind of shift says what it takes (ShiftResult.EnergyCost), a calm
        // one less, a hard night more - and the night gives back the more, the more tired Kuba went to bed (and more again
        // when he falls into bed exhausted). A quiet evening at home adds a little; the evening's plan moves it more. Evenings
        // mixed between rest, a call home and the odd night out keep him around seventy - a run of rushes or a hard night
        // drags him under sixty (tired: slower at the sink) for a day or two; the pub every night keeps him in the forties;
        // one evening's rest brings back a dozen.
        public float ShiftEnergyCost { get; } = 24f;
        public float SleepBaseRecovery { get; } = 8f;
        public float SleepRecoveryPerMissingPoint { get; } = 0.25f;

        /// <summary>Exhausted at bedtime (below this), he sleeps like a log: more back for every point under it.</summary>
        public float DeepSleepBelow { get; } = 35f;
        public float DeepSleepPerPoint { get; } = 0.3f;

        /// <summary>An evening with no plans: a little more sleep's worth.</summary>
        public float QuietEveningBonus { get; } = 3f;

        public float HomesicknessPerDay { get; private set; } = 3f;

        // Homesickness, felt but never crippling: very homesick, Kuba lies awake; settled in, he sleeps a bit better.
        public float HomesickSleepThreshold { get; } = 70f;
        public float HomesickSleepPenalty { get; } = 4f;
        public float SettledThreshold { get; } = 25f;
        public float SettledSleepBonus { get; } = 2f;

        /// <summary>This homesick, the pub only half reaches him: he sits there thinking of home.</summary>
        public float HomesickPubThreshold { get; } = 85f;

        // Evenings: one plan an evening - rest, a call home from the phone box on the corner, the pub, the crew's dinner, a
        // letter. (With a mobile of his own, the call home no longer takes the evening.)
        public float CallMomHomesicknessRelief { get; } = 15f;
        public float CallMomEnergyRelief { get; } = 3f;
        public float RestEnergy { get; } = 12f;
        public float PubCost { get; private set; } = 10f;
        public string PubName { get; private set; } = "Pub z ekipą";
        public string PubNotice { get; private set; } = "Wieczór w pubie z ekipą z kuchni. Integracja rośnie.";
        public float PubIntegrationGain { get; } = 10f;
        public float PubEnergyCost { get; } = 6f;
        public float PubHomesicknessRelief { get; } = 0f; // a night out takes his mind off things, not the ache

        /// <summary>The crew asks Kuba round for dinner once he is one of them (from this Integration), every few days.</summary>
        public float CrewDinnerIntegration { get; } = 35f;
        public int CrewDinnerEveryDays { get; } = 3;
        public float CrewDinnerCost { get; private set; } = 5f;
        public string CrewDinnerName { get; private set; } = "Kolacja z ekipą";
        public float CrewDinnerEnergy { get; } = 4f;
        public float CrewDinnerHomesicknessRelief { get; } = 4f;
        public float CrewDinnerIntegrationGain { get; } = 5f;

        /// <summary>Homesick enough, Kuba sits down to write home: it costs nothing and eases it.</summary>
        public float LetterThreshold { get; } = 45f;
        public float LetterRelief { get; } = 10f;

        // A few things worth buying in a life like Kuba's, once each, in the place's money: a real mattress (sleep), a
        // pocket dictionary (words), a pay-as-you-go mobile (calls home that do more).
        public float MattressCost { get; private set; } = 30f;
        public float MattressSleepBonus { get; } = 3f;
        public float DictionaryCost { get; private set; } = 8f;
        public float PhoneCost { get; private set; } = 45f;
        public float PhoneHomesicknessBonus { get; } = 5f;
        public float PhoneEnergyBonus { get; } = 2f;

        /// <summary>What one press of "send money home" transfers.</summary>
        public float SendHomeStep { get; private set; } = 20f;

        /// <summary>Who the money goes to, on the button: "do domu", or the parents from Kraków.</summary>
        public string SendHomeTo { get; private set; } = "do domu";

        // Debt. A week that ends short of the bills is arrears: a late fee, a bad night and worry. Stay in
        // arrears for WeeksBeforeHostel weeks and the landlord puts the suitcase out - a hostel bed costs more
        // and sleeps worse, until a week ends paid up again. (The evening rules keep a normal game out of this;
        // it is for a player who keeps walking out of shifts early.)
        public float LateFee { get; private set; } = 10f;
        public float ArrearsEnergyHit { get; } = 10f;
        public float ArrearsHomesickness { get; } = 8f;
        public int WeeksBeforeHostel { get; } = 3;
        public float HostelExtra { get; private set; } = 25f;
        public float HostelSleepPenalty { get; } = 4f;

        public float SleepRecovery(float energyAtBedtime)
        {
            return SleepBaseRecovery + SleepRecoveryPerMissingPoint * (100f - energyAtBedtime) +
                   DeepSleepPerPoint * Mathf.Max(0f, DeepSleepBelow - energyAtBedtime);
        }

        /// <summary>
        /// The night after a shift: the more tired, the more it gives back; a quiet evening and a real mattress add to it,
        /// lying awake homesick or a noisy hostel dorm take from it, and being settled in helps a little.
        /// </summary>
        public float NightsSleep(float energyAtBedtime, float homesickness, bool quietEvening, bool mattress, bool hostel)
        {
            float sleep = SleepRecovery(energyAtBedtime);
            if (quietEvening)
            {
                sleep += QuietEveningBonus;
            }

            if (mattress)
            {
                sleep += MattressSleepBonus;
            }

            if (hostel)
            {
                sleep -= HostelSleepPenalty;
            }

            if (homesickness >= HomesickSleepThreshold)
            {
                sleep -= HomesickSleepPenalty;
            }
            else if (homesickness <= SettledThreshold)
            {
                sleep += SettledSleepBonus;
            }

            return sleep;
        }
    }
}
