using System;
using System.Globalization;
using UnityEngine;
using Zmywak.Core.Data;

namespace Zmywak.Core.Save
{
    /// <summary>
    /// What makes a file a save, and what is made of one that is odd. <see cref="IsReadable"/> turns away what is not a game save at
    /// all (an empty object, another program's JSON, numbers no float holds) so the backup is used instead; <see cref="Repair"/>
    /// puts what is left back in the range the game can run on (a day count that would wrap, a date <c>AddDays</c> would choke on,
    /// a place that does not exist). Nothing here guesses at values: a value that cannot be told is the field's default.
    /// </summary>
    public static class SaveValidation
    {
        public const int MaxDay = 100000;
        public const float MaxMoney = 1e7f;
        public const int MaxPendingDishes = 1000;
        public const int EarliestYear = 1990;
        public const int LatestYear = 2200;

        /// <summary>
        /// A game save names its version or its day (everything the game writes does, and so did the first saves, which had a day);
        /// anything else - {}, {"foo":1} - is not one. And every number in it has to be a number.
        /// </summary>
        public static bool IsReadable(SaveData data, out string problem)
        {
            if (data == null)
            {
                problem = "nothing in it";
                return false;
            }

            if (data.version <= 0 && data.dayNumber <= 0)
            {
                problem = "it names neither a version nor a day, so it is not a game save";
                return false;
            }

            string bad = FirstNonFinite(data);
            if (bad != null)
            {
                problem = $"{bad} is not a number";
                return false;
            }

            problem = null;
            return true;
        }

        /// <summary>Puts the numbers, the place and the dates of a readable save back in range (see the constants).</summary>
        public static void Repair(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            // Unity serializes inline classes by value: a null checkpoint may round-trip as an empty object.
            // Neither an empty nor a partial record identifies a conversation to resume.
            if (data.dialogueCheckpoint != null &&
                (string.IsNullOrWhiteSpace(data.dialogueCheckpoint.beatId) ||
                 string.IsNullOrWhiteSpace(data.dialogueCheckpoint.nodeId)))
            {
                data.dialogueCheckpoint = null;
            }

            data.dayNumber = Mathf.Clamp(data.dayNumber, 0, MaxDay);
            if (!Enum.IsDefined(typeof(Place), data.place))
            {
                data.place = (int)Place.Dublin;
            }

            data.walletEuros = Mathf.Clamp(data.walletEuros, -MaxMoney, MaxMoney);
            data.sentHomeEuros = Mathf.Clamp(data.sentHomeEuros, 0f, MaxMoney);
            data.sentHomePln = Mathf.Clamp(data.sentHomePln, 0f, MaxMoney);
            data.energy = Mathf.Clamp(data.energy, 0f, 100f);
            data.homesickness = Mathf.Clamp(data.homesickness, 0f, 100f);
            data.integration = Mathf.Clamp(data.integration, 0f, 100f);
            data.weeksInArrears = Mathf.Max(0, data.weeksInArrears);
            data.bigDishwasherPendingDishes = Mathf.Clamp(data.bigDishwasherPendingDishes, 0, MaxPendingDishes);
            if (data.bigDishwasherStartedUtcTicks < 0L || data.bigDishwasherStartedUtcTicks > DateTime.MaxValue.Ticks)
            {
                data.bigDishwasherStartedUtcTicks = 0L; // no clock holds it: the machine was never started
            }

            // A date the calendar cannot run on (the end of time: the next day throws; the start: no rates, no rent) is no date:
            // the life keeps the calendar it starts with.
            if (!string.IsNullOrEmpty(data.currentDateIso) &&
                DateTime.TryParse(data.currentDateIso, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTime date) &&
                (date.Year < EarliestYear || date.Year > LatestYear))
            {
                data.currentDateIso = null;
            }
        }

        /// <summary>What goes into a file: every number a number. (An in-memory NaN is a bug elsewhere; it must not make the save unreadable.)</summary>
        public static void MakeWritable(SaveData data)
        {
            data.walletEuros = Finite(data.walletEuros);
            data.energy = Finite(data.energy);
            data.homesickness = Finite(data.homesickness);
            data.integration = Finite(data.integration);
            data.sentHomeEuros = Finite(data.sentHomeEuros);
            data.sentHomePln = Finite(data.sentHomePln);
        }

        private static float Finite(float value) => float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;

        private static string FirstNonFinite(SaveData d)
        {
            if (!IsFinite(d.walletEuros)) return nameof(d.walletEuros);
            if (!IsFinite(d.energy)) return nameof(d.energy);
            if (!IsFinite(d.homesickness)) return nameof(d.homesickness);
            if (!IsFinite(d.integration)) return nameof(d.integration);
            if (!IsFinite(d.sentHomeEuros)) return nameof(d.sentHomeEuros);
            if (!IsFinite(d.sentHomePln)) return nameof(d.sentHomePln);
            return null;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
