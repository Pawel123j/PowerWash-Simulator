using System.Collections.Generic;

namespace Zmywak.Core.Data
{
    /// <summary>Something the game teaches - at the sink or in the evening - and holds back until its day.</summary>
    public enum Mechanic
    {
        /// <summary>The whole loop on one plate, step by step (the first shift's guided start).</summary>
        GuidedFirstDish,
        /// <summary>The sponge, the steel wool and the dish soap.</summary>
        BasicTools,
        Glasses,
        ChefOrders,
        Soaking,
        Breakdowns,
        SendMoney,
        Pub,
        Scraper,
        UrgentOrders,
        HeavySteel,
        Shower,
        BigDishwasher
    }

    /// <summary>
    /// When each part of the game comes in. Day one is the sink itself, taught on one plate; every day after adds at most
    /// one or two things on top of what the days before practised - the glass and the chef's calls, then the soak and the
    /// first breakdown, then the busier shifts with their tools - so nothing arrives before there is room for it, and the
    /// first week ends with all of it. The kitchen on its own (the editor, the tests) has everything from the start.
    /// </summary>
    public static class MechanicUnlocks
    {
        /// <summary>A day late enough for everything (the kitchen played on its own, outside the calendar).</summary>
        public const int Everything = int.MaxValue;

        private static readonly Dictionary<Mechanic, int> Days = new Dictionary<Mechanic, int>
        {
            [Mechanic.GuidedFirstDish] = 1,
            [Mechanic.BasicTools] = 1,
            [Mechanic.Glasses] = 2,
            [Mechanic.ChefOrders] = 2,
            [Mechanic.SendMoney] = 2,
            [Mechanic.Soaking] = 3,
            [Mechanic.Breakdowns] = 3,
            [Mechanic.Pub] = 3,
            [Mechanic.Scraper] = 4,
            [Mechanic.UrgentOrders] = 4,
            [Mechanic.HeavySteel] = 4,
            [Mechanic.Shower] = 5,
            [Mechanic.BigDishwasher] = 5
        };

        /// <summary>The story day a mechanic comes in.</summary>
        public static int DayOf(Mechanic mechanic) => Days.TryGetValue(mechanic, out int day) ? day : 1;

        public static bool IsUnlocked(Mechanic mechanic, int day) => day >= DayOf(mechanic);

        /// <summary>Whether a kind of dish comes to the sink yet: the glass with the light, the bowl with the soak, the pots and
        /// roasting dishes with the busier shifts (by id; anything else is there from day one).</summary>
        public static bool IsDishUnlocked(string dishId, int day)
        {
            switch (dishId)
            {
                case "glass":
                    return IsUnlocked(Mechanic.Glasses, day);
                case "bowl":
                    return IsUnlocked(Mechanic.Soaking, day);
                case "pot":
                case "roaster":
                    return IsUnlocked(Mechanic.HeavySteel, day);
                default:
                    return true;
            }
        }

        /// <summary>Whether a tool is on the bar yet (by id).</summary>
        public static bool IsToolUnlocked(string toolId, int day)
        {
            switch (toolId)
            {
                case "scraper":
                    return IsUnlocked(Mechanic.Scraper, day);
                case "shower":
                    return IsUnlocked(Mechanic.Shower, day);
                default:
                    return true;
            }
        }

        /// <summary>What a day brings in, one line each, for the hub and the kitchen's first-time cards (empty on most days).</summary>
        public static IReadOnlyList<Mechanic> NewOn(int day)
        {
            var list = new List<Mechanic>();
            foreach (KeyValuePair<Mechanic, int> pair in Days)
            {
                if (pair.Value == day)
                {
                    list.Add(pair.Key);
                }
            }

            return list;
        }

        /// <summary>The day's news as the evening tells it ("Nowe jutro: ...") - only the things the hub and the sink show.</summary>
        public static string Describe(Mechanic mechanic)
        {
            switch (mechanic)
            {
                case Mechanic.Glasses: return "szklanki (sprawdzane pod światło)";
                case Mechanic.ChefOrders: return "zamówienia szefa i Flow";
                case Mechanic.SendMoney: return "przelewy do domu";
                case Mechanic.Soaking: return "moczenie w zlewie";
                case Mechanic.Breakdowns: return "awarie zmywarki";
                case Mechanic.Pub: return "wieczory w pubie";
                case Mechanic.Scraper: return "skrobak";
                case Mechanic.UrgentOrders: return "pilne zamówienia";
                case Mechanic.HeavySteel: return "garnki i brytfanny";
                case Mechanic.Shower: return "prysznic";
                case Mechanic.BigDishwasher: return "duża zmywarka";
                default: return string.Empty;
            }
        }
    }
}
