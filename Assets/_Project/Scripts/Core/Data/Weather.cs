using System;

namespace Zmywak.Core.Data
{
    /// <summary>What it is doing outside Kuba's window tonight.</summary>
    public enum WeatherKind
    {
        Clear,
        Cloudy,
        Drizzle,
        Rain,
        Fog,
        Snow,
        Heat,
        Storm
    }

    /// <summary>
    /// The weather by the date and the place, the same every time the day comes round (a hash of the date, not a dice):
    /// Dublin's rain more days than not and drizzle on many more, its rare clear night, fog in the autumn - and the
    /// real big freeze of 2010, snow from 26 November to 28 December. Kraków's winters snow; the Pilbara is dry heat,
    /// with the odd storm in the cyclone season (January to March).
    /// </summary>
    public static class Weather
    {
        public static WeatherKind On(DateTime date, Place place)
        {
            int roll = Roll(date);
            switch (place)
            {
                case Place.Krakow:
                    if (date.Month == 12 || date.Month <= 2)
                    {
                        return roll < 45 ? WeatherKind.Snow : roll < 70 ? WeatherKind.Cloudy : roll < 85 ? WeatherKind.Fog : WeatherKind.Clear;
                    }

                    return roll < 30 ? WeatherKind.Rain : roll < 60 ? WeatherKind.Cloudy : WeatherKind.Clear;
                case Place.Pilbara:
                    if (date.Month <= 3 && roll < 15)
                    {
                        return WeatherKind.Storm;
                    }

                    return roll < 70 ? WeatherKind.Heat : WeatherKind.Clear;
            }

            if (date >= new DateTime(2010, 11, 26) && date <= new DateTime(2010, 12, 28))
            {
                return WeatherKind.Snow; // the big freeze
            }

            if (date.Month >= 10 && date.Month <= 11 && roll < 12)
            {
                return WeatherKind.Fog;
            }

            return roll < 40 ? WeatherKind.Rain : roll < 65 ? WeatherKind.Drizzle : roll < 88 ? WeatherKind.Cloudy : WeatherKind.Clear;
        }

        /// <summary>A word for the hub, after the date.</summary>
        public static string Word(WeatherKind weather)
        {
            switch (weather)
            {
                case WeatherKind.Clear: return "pogodnie";
                case WeatherKind.Cloudy: return "pochmurno";
                case WeatherKind.Drizzle: return "mżawka";
                case WeatherKind.Rain: return "deszcz";
                case WeatherKind.Fog: return "mgła";
                case WeatherKind.Snow: return "śnieg";
                case WeatherKind.Heat: return "upał";
                default: return "burza";
            }
        }

        private static int Roll(DateTime date)
        {
            unchecked
            {
                int h = date.Year * 397 ^ date.DayOfYear * 7919;
                h ^= h >> 13;
                h *= 0x5bd1e995;
                h ^= h >> 15;
                return (h & 0x7fffffff) % 100;
            }
        }
    }
}
