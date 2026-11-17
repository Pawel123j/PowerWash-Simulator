using System;

namespace Zmywak.Core.Data
{
    /// <summary>
    /// The Polish noun after a number: one (1: "naczynie"), few (2-4 and the ones that end so, but not 12-14: "22 naczynia"), many
    /// (everything else, 0 and 5-21 included: "naczyń"). A count in a sentence ("Nowy rekord: 22 naczynia") cannot use one fixed form.
    /// </summary>
    public static class PolishPlural
    {
        public static string Form(int count, string one, string few, string many)
        {
            long n = Math.Abs((long)count);
            if (n == 1)
            {
                return one;
            }

            long lastDigit = n % 10;
            long lastTwoDigits = n % 100;
            return lastDigit >= 2 && lastDigit <= 4 && !(lastTwoDigits >= 12 && lastTwoDigits <= 14) ? few : many;
        }

        /// <summary>"22 naczynia".</summary>
        public static string Count(int count, string one, string few, string many) => $"{count} {Form(count, one, few, many)}";

        /// <summary>The dishes: "1 naczynie", "3 naczynia", "5 naczyń".</summary>
        public static string Dishes(int count) => Count(count, "naczynie", "naczynia", "naczyń");
    }
}
