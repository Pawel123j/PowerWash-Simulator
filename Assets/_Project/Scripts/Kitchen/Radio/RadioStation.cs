using Zmywak.Core.Data;

namespace Zmywak.Kitchen.Radio
{
    /// <summary>Where the kitchen radio's dial is: three stations, or off.</summary>
    public enum RadioStation
    {
        Off,
        Trad,
        Pop,
        News
    }

    /// <summary>The stations on the dial where Kuba lives: an Irish trad session in Dublin, folk on Polish radio in
    /// Kraków, the bush's folk station in the Pilbara - and pop and the news everywhere.</summary>
    public static class RadioStations
    {
        /// <summary>What the dial shows in Dublin.</summary>
        public static string Label(RadioStation station) => Label(station, Place.Dublin);

        /// <summary>What the dial shows.</summary>
        public static string Label(RadioStation station, Place place)
        {
            switch (station)
            {
                case RadioStation.Trad:
                    return place == Place.Krakow ? "FM 101.0 · Folk" : place == Place.Pilbara ? "FM 99.7 · Bush" : "FM 96.2 · Trad";
                case RadioStation.Pop:
                    return place == Place.Pilbara ? "FM 92.9 · Hits" : "FM 101.4 · Pop";
                case RadioStation.News:
                    return place == Place.Pilbara ? "AM 702 · News" : "FM 88.5 · Wiadomości";
                default:
                    return "Radio wyłączone";
            }
        }

        /// <summary>What the dial says under the station on a music station in Dublin.</summary>
        public static string Programme(RadioStation station) => Programme(station, Place.Dublin);

        /// <summary>What the dial says under the station on a music station (the news reads its headlines instead).</summary>
        public static string Programme(RadioStation station, Place place)
        {
            switch (station)
            {
                case RadioStation.Trad:
                    return place == Place.Krakow ? "Muzyka z gór i z pubów"
                        : place == Place.Pilbara ? "Skrzypce przy ognisku"
                        : "Sesja w pubie: jigi i reele na żywo";
                case RadioStation.Pop:
                    return "Popołudnie z hitami";
                case RadioStation.News:
                    return "Wiadomości";
                default:
                    return "Stuknij, żeby włączyć";
            }
        }

        /// <summary>The next turn of the dial: Trad, Pop, News, off, and round again.</summary>
        public static RadioStation Next(RadioStation station)
        {
            switch (station)
            {
                case RadioStation.Trad: return RadioStation.Pop;
                case RadioStation.Pop: return RadioStation.News;
                case RadioStation.News: return RadioStation.Off;
                default: return RadioStation.Trad;
            }
        }
    }
}
