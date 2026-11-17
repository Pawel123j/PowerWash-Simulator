using System;

namespace Zmywak.Core.Data
{
    /// <summary>A shift out of the ordinary: the kitchen's busy days and its worst one.</summary>
    public enum SpecialShift
    {
        None,
        Wedding,
        Inspection,
        LasagneTray,
        StPatricks
    }

    /// <summary>What a special shift changes: the pace and the pile, the tips, what a breakage costs, and what it can
    /// pay at the end (in Dublin euros; elsewhere the place's small sums, <see cref="PlaceMoney.SmallSums"/>).</summary>
    public readonly struct SpecialShiftRules
    {
        public SpecialShiftRules(string title, string intro, float paceScale, int extraOpeningDishes, float tipsScale, float breakageScale,
            float bonusEuros)
        {
            Title = title;
            Intro = intro;
            PaceScale = paceScale;
            ExtraOpeningDishes = extraOpeningDishes;
            TipsScale = tipsScale;
            BreakageScale = breakageScale;
            BonusEuros = bonusEuros;
        }

        public string Title { get; }

        /// <summary>What Kuba is told as the shift starts.</summary>
        public string Intro { get; }

        /// <summary>The gaps between dishes, times this: under 1, a busier night.</summary>
        public float PaceScale { get; }

        /// <summary>More dishes waiting on the counter at the start.</summary>
        public int ExtraOpeningDishes { get; }

        public float TipsScale { get; }

        /// <summary>What a breakage is docked, times this.</summary>
        public float BreakageScale { get; }

        /// <summary>The most the shift can pay on top at the end: the newlyweds' envelope, Declan's thanks for a clean
        /// inspection, the room's whip-round for the lasagne tray.</summary>
        public float BonusEuros { get; }
    }

    /// <summary>
    /// When the special shifts come, from the calendar - so the story's weeks meet them as much as the days after it:
    /// a wedding reception every Saturday (not in a mining camp), the legendary lasagne tray every Sunday, an
    /// unannounced health inspection every ninth day, and in Dublin, St. Patrick's Day. The first week is Kuba's to
    /// learn the sink in peace.
    /// </summary>
    public static class SpecialShifts
    {
        /// <summary>Days at the start with plain shifts only.</summary>
        public const int CalmFirstDays = 7;

        public const int InspectionEveryDays = 9;

        private const int InspectionDayOfCycle = 5;

        public static SpecialShift On(DateTime date, int dayNumber, Place place)
        {
            if (place == Place.Dublin && date.Month == 3 && date.Day == 17)
            {
                return SpecialShift.StPatricks;
            }

            if (dayNumber <= CalmFirstDays)
            {
                return SpecialShift.None;
            }

            switch (date.DayOfWeek)
            {
                case DayOfWeek.Saturday:
                    return place == Place.Pilbara ? SpecialShift.None : SpecialShift.Wedding;
                case DayOfWeek.Sunday:
                    return SpecialShift.LasagneTray;
            }

            return dayNumber % InspectionEveryDays == InspectionDayOfCycle ? SpecialShift.Inspection : SpecialShift.None;
        }

        public static SpecialShiftRules Rules(SpecialShift special)
        {
            switch (special)
            {
                case SpecialShift.Wedding:
                    return new SpecialShiftRules("Wesele",
                        "Wesele na sto dwadzieścia osób: talerze i kieliszki falami. Na koniec młodzi zostawiają kuchni kopertę.",
                        paceScale: 0.7f, extraOpeningDishes: 3, tipsScale: 1.5f, breakageScale: 1f, bonusEuros: 15f);
                case SpecialShift.Inspection:
                    return new SpecialShiftRules("Inspekcja sanepidu",
                        "Niezapowiedziana kontrola HSE. Żadnej stłuczki i żadnego zawalenia Góry, a Declan się odwdzięczy.",
                        paceScale: 1f, extraOpeningDishes: 0, tipsScale: 1f, breakageScale: 2f, bonusEuros: 10f);
                case SpecialShift.LasagneTray:
                    return new SpecialShiftRules("Legendarna Taca Lasagne",
                        "Niedziela: lasagne Declana w tacy, której nikt jeszcze nie domył do końca. Domyj ją ręcznie, a sala zrzuci się na napiwek.",
                        paceScale: 1f, extraOpeningDishes: 0, tipsScale: 1f, breakageScale: 1f, bonusEuros: 10f);
                case SpecialShift.StPatricks:
                    return new SpecialShiftRules("Dzień św. Patryka",
                        "Siedemnasty marca: pełna sala, morze szklanek po Guinnessie i podwójne napiwki.",
                        paceScale: 0.6f, extraOpeningDishes: 2, tipsScale: 2f, breakageScale: 1f, bonusEuros: 0f);
                default:
                    return new SpecialShiftRules(string.Empty, string.Empty, 1f, 0, 1f, 1f, 0f);
            }
        }

        /// <summary>The dishes a busy night brings, by id, in the order they come round (null: the shift's own queue).</summary>
        public static string[] Queue(SpecialShift special)
        {
            switch (special)
            {
                case SpecialShift.Wedding:
                    return new[] { "plate", "glass", "plate", "bowl", "glass", "plate", "mug", "glass", "plate", "roaster" };
                case SpecialShift.StPatricks:
                    return new[] { "glass", "glass", "plate", "glass", "mug", "glass", "glass", "pan" };
                default:
                    return null;
            }
        }

        /// <summary>
        /// What the shift pays on top at the end, in Dublin euros: the wedding's envelope always, Declan's thanks only
        /// for an inspection without a breakage or a collapse, the whip-round only for a tray scrubbed clean by hand.
        /// </summary>
        public static float Bonus(SpecialShift special, int dishesBroken, bool pileCollapsed, bool trayScrubbed)
        {
            float bonus = Rules(special).BonusEuros;
            switch (special)
            {
                case SpecialShift.Inspection:
                    return Passed(dishesBroken, pileCollapsed) ? bonus : 0f;
                case SpecialShift.LasagneTray:
                    return trayScrubbed ? bonus : 0f;
                default:
                    return bonus;
            }
        }

        /// <summary>An inspection is passed with nothing broken and the pile never down.</summary>
        public static bool Passed(int dishesBroken, bool pileCollapsed) => dishesBroken == 0 && !pileCollapsed;
    }
}
