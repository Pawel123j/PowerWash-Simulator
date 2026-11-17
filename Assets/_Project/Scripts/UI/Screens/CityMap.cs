using Zmywak.Core.Data;

namespace Zmywak.UI.Screens
{
    /// <summary>What tapping a place on the map does: the hub's own actions, or nothing (a landmark).</summary>
    public enum MapAction
    {
        None,
        Home,
        Work,
        Pub,
        CallHome,
        SendMoney
    }

    /// <summary>A place on the map: its name, where it is (0-1 across and up the picture) and what it is for.</summary>
    public readonly struct MapPin
    {
        public MapPin(string label, float u, float v, MapAction action)
        {
            Label = label;
            U = u;
            V = v;
            Action = action;
        }

        public string Label { get; }
        public float U { get; }
        public float V { get; }
        public MapAction Action { get; }
    }

    /// <summary>
    /// The places of Kuba's life on the hub's map (CityMapPanel), wherever he lives: in Dublin his room in Rathmines,
    /// Declan's in Temple Bar, the pub on Camden Street, the internet café and phones on Parnell Street, the money
    /// transfer on O'Connell Street, DIT and Busáras; in Kraków a room in Kazimierz and the hotel on the Rynek; in the
    /// Pilbara his donga, the camp kitchen and the wet mess. The pictures behind them are painted to match (UiArt).
    /// </summary>
    public static class CityMap
    {
        public static string Title(Place place)
        {
            switch (place)
            {
                case Place.Krakow: return "Kraków";
                case Place.Pilbara: return "Obóz w Pilbarze";
                default: return "Dublin";
            }
        }

        public static MapPin[] Pins(Place place)
        {
            switch (place)
            {
                case Place.Krakow:
                    return new[]
                    {
                        new MapPin("Pokój na Kazimierzu", 0.56f, 0.4f, MapAction.Home),
                        new MapPin("Hotel przy Rynku", 0.5f, 0.63f, MapAction.Work),
                        new MapPin("Knajpa na Kazimierzu", 0.72f, 0.46f, MapAction.Pub),
                        new MapPin("Kawiarenka internetowa", 0.34f, 0.74f, MapAction.CallHome),
                        new MapPin("Poczta Główna", 0.66f, 0.58f, MapAction.SendMoney),
                        new MapPin("Wawel", 0.42f, 0.42f, MapAction.None)
                    };
                case Place.Pilbara:
                    return new[]
                    {
                        new MapPin("Twoja donga", 0.4f, 0.44f, MapAction.Home),
                        new MapPin("Kuchnia obozu", 0.56f, 0.56f, MapAction.Work),
                        new MapPin("Wet mess", 0.66f, 0.44f, MapAction.Pub),
                        new MapPin("Internet (Skype)", 0.5f, 0.3f, MapAction.CallHome),
                        new MapPin("Biuro obozu", 0.38f, 0.6f, MapAction.SendMoney),
                        new MapPin("Lądowisko (do Perth)", 0.26f, 0.82f, MapAction.None),
                        new MapPin("Kopalnia", 0.78f, 0.2f, MapAction.None)
                    };
                default:
                    return new[]
                    {
                        new MapPin("Pokój w Rathmines", 0.48f, 0.16f, MapAction.Home),
                        new MapPin("Restauracja Declana", 0.5f, 0.52f, MapAction.Work),
                        new MapPin("Pub na Camden Street", 0.46f, 0.36f, MapAction.Pub),
                        new MapPin("Parnell Street: kafejka, telefony", 0.44f, 0.72f, MapAction.CallHome),
                        new MapPin("Przekazy: O'Connell Street", 0.62f, 0.64f, MapAction.SendMoney),
                        new MapPin("DIT", 0.64f, 0.78f, MapAction.None),
                        new MapPin("Busáras", 0.8f, 0.7f, MapAction.None)
                    };
            }
        }
    }
}
