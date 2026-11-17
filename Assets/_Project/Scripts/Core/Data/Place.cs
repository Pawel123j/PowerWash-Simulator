using Zmywak.Core.Settings;

namespace Zmywak.Core.Data
{
    /// <summary>
    /// Where Kuba lives and works. Dublin for the whole story; chapter 5's choice sends him on for the epilogue - home
    /// to Kraków, or with Gavin to a mining camp in the Pilbara (Western Australia, flying in and out of Perth).
    /// </summary>
    public enum Place
    {
        Dublin = 0,
        Krakow = 1,
        Pilbara = 2
    }

    /// <summary>The money of each place: its currency, and what the small sums of a shift come to in it.</summary>
    public static class PlaceMoney
    {
        /// <summary>How an amount is written on screen: "EUR", "zł", "AUD".</summary>
        public static string Unit(Place place)
        {
            switch (place)
            {
                case Place.Krakow:
                    return "zł";
                case Place.Pilbara:
                    return "AUD";
                default:
                    return "EUR";
            }
        }

        /// <summary>ISO code of the place's currency.</summary>
        public static string Currency(Place place)
        {
            switch (place)
            {
                case Place.Krakow:
                    return "PLN";
                case Place.Pilbara:
                    return "AUD";
                default:
                    return "EUR";
            }
        }

        /// <summary>"12,50 EUR", "48,00 zł" - written the Polish way whatever the phone's language.</summary>
        public static string Format(float amount, Place place)
        {
            return $"{amount.ToString("0.00", GameCulture.Polish)} {Unit(place)}";
        }

        /// <summary>
        /// What a Dublin euro of small money - a tip for a clean dish, a breakage docked, the prep bonus - comes to in
        /// this place's money: a few złoty in Kraków, a dollar and a half in the camp.
        /// </summary>
        public static float SmallSums(Place place)
        {
            switch (place)
            {
                case Place.Krakow:
                    return 4f;
                case Place.Pilbara:
                    return 1.5f;
                default:
                    return 1f;
            }
        }
    }
}
