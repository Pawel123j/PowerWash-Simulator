using System;
using System.Collections.Generic;
using Zmywak.Core.Data;
using Zmywak.Core.Services;
using Zmywak.Narrative.Progress;
using Zmywak.Narrative.SlangDictionary;

namespace Zmywak.UI.Common
{
    /// <summary>
    /// What the loading screen says while the next scene comes in: a tip for the kitchen on the way to work, one about
    /// life on the way home, now and then a word of slang Kuba has already picked up, or a line about where and when he
    /// is. Short - read in the second the screen is up.
    /// </summary>
    public static class LoadingTips
    {
        public readonly struct Tip
        {
            public Tip(string heading, string text)
            {
                Heading = heading;
                Text = text;
            }

            public string Heading { get; }
            public string Text { get; }
        }

        private static readonly string[] Kitchen =
        {
            "Pełny kosz podbija Flow i daje napiwek. Czasem warto chwilę poczekać ze zmywarką.",
            "Zaschnięta owsianka? Odstaw miskę do moczenia i weź następne naczynie.",
            "Szybki ruch palcem w górę odkłada czyste naczynie na stojak.",
            "Pilne zamówienie liczy się w chwili, gdy naczynie trafi na stojak.",
            "Każdy brud ma swoje narzędzie. Właściwe pulsuje na pasku.",
            "Szklanka nie jest czysta, dopóki nie obejrzysz jej pod światło.",
            "Góra rośnie? Zdejmuj z wierzchu, zanim się zawali.",
            "Zmęczony Kuba szoruje wolniej. Dobry sen to też praca."
        };

        private static readonly string[] Home =
        {
            "Rachunki przychodzą co siedem dni. Pub poczeka, czynsz nie.",
            "Telefon do mamy koi tęsknotę. Ale karta telefoniczna nie jest z gumy.",
            "Wieczór możesz spędzić z ekipą albo odpocząć. Jedno i drugie ma swoją cenę.",
            "Ekipa z kuchni pomaga tym, których zna.",
            "Im lepiej znasz ludzi, tym więcej słów rozumiesz.",
            "Awans przychodzi za całą robotę: naczynia, pełne kosze, zmiany bez stłuczki."
        };

        private static readonly string[] Months =
            { "Styczeń", "Luty", "Marzec", "Kwiecień", "Maj", "Czerwiec", "Lipiec", "Sierpień", "Wrzesień", "Październik", "Listopad", "Grudzień" };

        private static int _turn;

        /// <summary>Every slang entry there is (the hub registers them; the loading screen shows the ones Kuba knows).</summary>
        public static IReadOnlyList<SlangEntry> Glossary { get; set; } = Array.Empty<SlangEntry>();

        /// <summary>The tip for the way to a scene.</summary>
        public static Tip For(string nextScene)
        {
            _turn++;
            if (_turn % 3 == 0 && TryKnownSlang(out Tip slang))
            {
                return slang;
            }

            if (_turn % 4 == 1 && TryFlavour(out Tip flavour))
            {
                return flavour;
            }

            bool kitchen = nextScene == "Kitchen_Shift";
            string[] lines = kitchen ? Kitchen : Home;
            return new Tip(kitchen ? "Przy zlewie" : "Po pracy", lines[_turn % lines.Length]);
        }

        private static bool TryKnownSlang(out Tip tip)
        {
            tip = default;
            if (!ServiceLocator.TryGet(out NarrativeProgressController narrative) || Glossary == null)
            {
                return false;
            }

            var known = new List<SlangEntry>();
            foreach (SlangEntry entry in Glossary)
            {
                if (entry != null && narrative.Slang.IsUnlocked(entry.Id))
                {
                    known.Add(entry);
                }
            }

            if (known.Count == 0)
            {
                return false;
            }

            SlangEntry pick = known[_turn % known.Count];
            tip = new Tip("Słówko z kuchni", $"<b>{pick.Phrase}</b> - {pick.Meaning}");
            return true;
        }

        private static bool TryFlavour(out Tip tip)
        {
            tip = default;
            if (!ServiceLocator.TryGet(out IPlayerCondition condition))
            {
                return false;
            }

            DateTime today = condition.Today;
            string line;
            switch (condition.Place)
            {
                case Place.Krakow:
                    line = "Kraków. Hotelowa kuchnia przy Rynku, tramwaj o świcie i obiad u rodziców w niedzielę.";
                    break;
                case Place.Pilbara:
                    line = "Pilbara. Czerwona ziemia, czterdzieści stopni i tysiąc dwieście posiłków dziennie.";
                    break;
                default:
                    line = today.Year <= 2004 ? "Dublin, 2004. Irlandia otworzyła rynek pracy, a na lotnisku co tydzień ląduje kolejny samolot z Polski."
                        : today.Year <= 2007 ? "Dublin w boomie: dźwigi nad Liffey, kolejki do restauracji i czynsze, które rosną co miesiąc."
                        : today.Year <= 2009 ? "Dublin po krachu. Budowy stoją, a w kuchniach liczy się każdą godzinę."
                        : "Dublin po latach. Ten sam deszcz, inne miasto.";
                    break;
            }

            tip = new Tip($"{Months[today.Month - 1]} {today.Year}", line);
            return true;
        }
    }
}
