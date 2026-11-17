using System;
using System.Collections.Generic;
using Zmywak.Core.Data;

namespace Zmywak.Kitchen.Radio
{
    /// <summary>
    /// The news on the kitchen radio, by the date in the game: Ireland opening its jobs to the new EU on the day Kuba
    /// lands, the smoking ban and the Eurovision of May 2004; the boom's house prices and the World Cup in 2006; the
    /// bank guarantee and the building sites stopping in the autumn of 2008; the rescue, the cuts and the snow at the
    /// end of 2010 - and, after the move, Polish radio in Kraków (Euro 2012, Germany opening its jobs in 2011) or
    /// Australian in the Pilbara (record iron ore, the Queensland floods, the mining tax). Headlines are in Polish -
    /// Kuba's ear translates them.
    /// </summary>
    public static class RadioNews
    {
        private static readonly (DateTime from, string[] headlines)[] Bulletins =
        {
            (new DateTime(2004, 5, 1), new[]
            {
                "Dziesięć nowych krajów w Unii. Irlandia od pierwszego dnia otwiera dla nich rynek pracy.",
                "Od marca zakaz palenia w pubach i restauracjach. Właściciele liczą straty, kucharze palą przy kontenerach.",
                "W sobotę finał Eurowizji w Stambule. Polska i Irlandia liczą na ballady.",
                "Przed referendum o obywatelstwie: plakaty na każdej latarni.",
                "Pogoda: przelotne deszcze, potem deszcz. Wieczorem przejaśnienia, potem deszcz."
            }),
            (new DateTime(2005, 1, 1), new[]
            {
                "Ceny domów rosną kolejny kwartał z rzędu.",
                "Od maja płaca minimalna 7,65 euro za godzinę.",
                "Coraz więcej polskich sklepów w Dublinie."
            }),
            (new DateTime(2006, 1, 1), new[]
            {
                "Ceny domów w Dublinie znowu w górę. Banki dają kredyt na sto procent.",
                "Spis powszechny: w Irlandii mieszka coraz więcej Polaków.",
                "Mundial w Niemczech: Polska zagra w grupie z Niemcami i Ekwadorem.",
                "Nad Dublinem więcej dźwigów niż kiedykolwiek."
            }),
            (new DateTime(2007, 1, 1), new[]
            {
                "Płaca minimalna rośnie do 8,30 euro.",
                "Ekonomiści ostrzegają: ceny domów przestają rosnąć.",
                "Budownictwo zatrudnia rekordową liczbę ludzi."
            }),
            (new DateTime(2008, 9, 15), new[]
            {
                "Rząd gwarantuje wszystkie depozyty w irlandzkich bankach.",
                "Akcje banków lecą na łeb na szyję.",
                "Budowy stają w całym kraju. Tysiące ludzi bez pracy.",
                "Coraz więcej Polaków wraca do domu - autobusy do Polski pełne."
            }),
            (new DateTime(2009, 1, 1), new[]
            {
                "Recesja: bezrobocie najwyższe od lat.",
                "Młodzi Irlandczycy znowu wyjeżdżają: do Australii, Kanady, Londynu."
            }),
            (new DateTime(2010, 11, 21), new[]
            {
                "Irlandia przyjmuje 85 miliardów euro pomocy z Unii i MFW.",
                "Budżet oszczędnościowy w grudniu: cięcia w całym kraju.",
                "Śnieg i mróz paraliżują Dublin. Lotnisko zamknięte.",
                "Polska szykuje się do Euro 2012: stadiony prawie gotowe."
            })
        };

        // Kraków from late 2010: Polish radio.
        private static readonly (DateTime from, string[] headlines)[] KrakowBulletins =
        {
            (new DateTime(2010, 11, 21), new[]
            {
                "Irlandia bierze pomoc z Unii i MFW. Polacy w Dublinie: wracać czy czekać?",
                "Na Rynku Głównym rusza jarmark bożonarodzeniowy.",
                "Stadiony na Euro 2012 prawie gotowe. W Krakowie trenować będą reprezentacje.",
                "Mróz i śnieg: korki na zakopiance."
            }),
            (new DateTime(2011, 1, 1), new[]
            {
                "Od maja Niemcy i Austria otwierają rynek pracy dla Polaków.",
                "Beatyfikacja Jana Pawła II w Rzymie: tysiące pielgrzymów z Krakowa.",
                "Od lipca Polska przewodniczy Radzie Unii Europejskiej.",
                "Wybory parlamentarne w październiku."
            }),
            (new DateTime(2012, 1, 1), new[]
            {
                "Euro 2012: Irlandia zagra w Poznaniu i Gdańsku. Kibice z Dublina już rezerwują noclegi.",
                "Mecz otwarcia w Warszawie 8 czerwca.",
                "Płaca minimalna od stycznia 1500 złotych."
            })
        };

        // The Pilbara from late 2010: Australian radio, in Kuba's Polish.
        private static readonly (DateTime from, string[] headlines)[] PilbaraBulletins =
        {
            (new DateTime(2010, 11, 21), new[]
            {
                "Ruda żelaza po rekordowej cenie. Kopalnie w Pilbarze szukają ludzi.",
                "Powodzie w Queensland: woda zalewa miasta na wschodzie.",
                "Ashes: Anglia gra z Australią w krykieta. Pół obozu nie śpi.",
                "Upał: w Port Hedland 45 stopni w cieniu."
            }),
            (new DateTime(2011, 2, 1), new[]
            {
                "Cyklon Yasi uderza w Queensland.",
                "Rząd zapowiada podatek węglowy. Górnicy protestują.",
                "Do Perth przylatuje coraz więcej Irlandczyków. Lotnisko pęka w szwach."
            }),
            (new DateTime(2012, 7, 1), new[]
            {
                "Od lipca podatek od zysków górniczych.",
                "Cena rudy żelaza spada. Pierwsze kopalnie wstrzymują rekrutację.",
                "Czynsze w Perth najwyższe w historii."
            })
        };

        /// <summary>The headlines on the air on this date in Dublin (the latest bulletin that has begun).</summary>
        public static IReadOnlyList<string> For(DateTime date) => For(date, Place.Dublin);

        /// <summary>The headlines on the air on this date where Kuba lives: Irish, Polish or Australian news.</summary>
        public static IReadOnlyList<string> For(DateTime date, Place place)
        {
            (DateTime from, string[] headlines)[] bulletins = place == Place.Krakow ? KrakowBulletins
                : place == Place.Pilbara ? PilbaraBulletins
                : Bulletins;
            string[] headlines = bulletins[0].headlines;
            foreach ((DateTime from, string[] lines) in bulletins)
            {
                if (date >= from)
                {
                    headlines = lines;
                }
            }

            return headlines;
        }
    }
}
