using System;
using System.Collections.Generic;
using UnityEditor;
using Zmywak.Core.Data;
using Zmywak.Narrative.Story;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// The epilogue, "Listopad 2010" (29 November - 3 December 2010): the calendar jumps to the day after Ireland took
    /// the EU and IMF rescue. Every scene goes three ways, by chapter 5's choice - Kuba running Declan's kitchen in
    /// Dublin, cooking in a hotel off the Kraków market square, or feeding a mining camp in Western Australia with
    /// Gavin - and comes back to the same things: news from the others, a new boy at the sink who is now Kuba's to
    /// teach, and one last plate.
    /// </summary>
    internal static class Chapter6Content
    {
        public const string ChapterAssetName = "Chapter_ch6.asset";

        /// <summary>The Monday after the bailout.</summary>
        public static readonly DateTime StartDate = new DateTime(2010, 11, 29);

        public static StoryChapter Create(string folder)
        {
            var beats = new List<StoryBeat>
            {
                StoryScript.Beat("ch6_where", 31, StoryScript.SaveGraph(folder, "Ch6_1_Where", Where(), fluent: true)),
                StoryScript.Beat("ch6_news", 32, StoryScript.SaveGraph(folder, "Ch6_2_News", News(), fluent: true)),
                StoryScript.Beat("ch6_others", 33, StoryScript.SaveGraph(folder, "Ch6_3_Others", Others(), fluent: true)),
                StoryScript.Beat("ch6_new_kid", 34, StoryScript.SaveGraph(folder, "Ch6_4_NewKid", NewKid(), fluent: true)),
                StoryScript.Beat("ch6_last_plate", 35, StoryScript.SaveGraph(folder, "Ch6_5_LastPlate", LastPlate(), fluent: true), endsChapter: true)
            };

            StoryChapter chapter = VerticalSliceSetup.CreateOrLoad<StoryChapter>($"{folder}/{ChapterAssetName}");
            // Chapter 5's choice, two years on: in the Pilbara with Gavin, or home in Kraków (staying is staying in Dublin).
            var moves = new List<StoryRelocation>
            {
                new StoryRelocation { ifFlag = "ch5_goes_to_perth", place = Place.Pilbara },
                new StoryRelocation { ifFlag = "ch5_goes_home", place = Place.Krakow }
            };
            chapter.SetContent("ch6", "Epilog: Listopad 2010", beats, StartDate, moves);
            EditorUtility.SetDirty(chapter);
            return chapter;
        }

        /// <summary>Two nodes without lines that send the scene where Kuba went in chapter 5: Dublin if he stayed,
        /// Perth if he flew with Gavin, otherwise home to Poland.</summary>
        private static void Route(StoryScript.Graph g, string start, string dublin, string perth, string poland)
        {
            g.Node(start).BranchIf("ch5_stays", dublin).Next(start + "_away");
            g.Node(start + "_away").BranchIf("ch5_goes_to_perth", perth).Next(poland);
        }

        // ------------------------------------------------------------------
        // Day 31 - Monday 29 November 2010, where Kuba is
        // ------------------------------------------------------------------

        private static StoryScript.Graph Where()
        {
            var g = new StoryScript.Graph("wh");
            Route(g, "wh", "wh_dublin", "wh_perth", "wh_poland");

            g.Node("wh_dublin")
                .Narrate("29 listopada 2010. Wczoraj Irlandia przyjęła pomoc Unii Europejskiej i Międzynarodowego Funduszu Walutowego: osiemdziesiąt pięć miliardów euro. W radiu co drugie słowo to \"bailout\".")
                .Narrate("Restauracja Declana przetrwała. Krótsze menu, tańsze wino, ten sam zlew. Kuba prowadzi kuchnię od otwarcia do zamknięcia.")
                .En("declan", "Sixty-two, Kooba. My knees are older than this country's debt. Will you take the kitchen? Properly. Your name on the door.")
                .Think("Moje nazwisko na drzwiach. \"Kooba\", przez dwa \"o\", jak na pierwszej kopercie z wypłatą.")
                .Choice("Biorę. Z twoim nazwiskiem obok.", "wh_take")
                .Choice("Jeszcze nie. Daj mi rok.", "wh_year");
            g.Node("wh_take")
                .En("declan", "Two names on the door. Grand. Mine first, though. For now.")
                .Think("Każdy kiedyś od tego zaczynał. Ja zacząłem od dwudziestu euro i zlewu.")
                .Outcome(integration: 10f)
                .Flag("ch6_name_on_the_door");
            g.Node("wh_year")
                .En("declan", "A year. You're the only man in this country asking for more time instead of more money.")
                .Think("Czas to jedyna rzecz, której banki jeszcze nie zagwarantowały.")
                .Flag("ch6_asked_for_a_year");

            g.Node("wh_perth")
                .Narrate("29 listopada 2010. Pilbara, Australia Zachodnia. Obóz górniczy na czterysta osób, czterdzieści stopni w cieniu, którego nie ma.")
                .Narrate("Kuba i Gavin prowadzą kuchnię: trzy zmiany, tysiąc dwieście posiłków dziennie, pensja, o jakiej w Dublinie nikomu się nie śniło.")
                .En("gavin", "Kooba. It's forty degrees and I'd give a week's wages for one day of Dublin rain. Is that mad?")
                .Think("Gavin tęskni za deszczem. Ja tęsknię... nie wiem już, za którym miejscem. Chyba za kuchnią, w której wszyscy znali moje imię.")
                .Choice("Mówię mu, że to normalne. Ja tęsknię za dwoma krajami naraz.", "wh_two")
                .Choice("Proponuję, żebyśmy na święta polecieli do Dublina.", "wh_christmas");
            g.Node("wh_two")
                .En("gavin", "Two countries. Jaysus. You're richer than me, so.")
                .Outcome(homesickness: -5f, integration: 5f)
                .Flag("ch6_two_homes");
            g.Node("wh_christmas")
                .En("gavin", "Dublin for Christmas... Declan would cry. Then he'd put us to work.")
                .Outcome(homesickness: -5f)
                .Flag("ch6_christmas_in_dublin");

            g.Node("wh_poland")
                .Narrate("29 listopada 2010. Kraków. Na Rynku rozstawiają jarmark bożonarodzeniowy, a w hotelowej kuchni przy Rynku od szóstej rano pachnie barszczem i rosołem.")
                .BranchIf("ch5_krakow_job", "wh_sous")
                .Next("wh_climbed");
            g.Node("wh_sous")
                .Narrate("Kuba jest tu zastępcą szefa kuchni od dwóch lat. Certyfikat z Cathal Brugha Street wisi w biurze szefa, obok menu na Euro 2012.")
                .Think("W Dublinie byłem \"Kooba\". Tu jestem \"panem Jakubem\". Nie wiem, który z nich lepiej kroi cebulę.")
                .Next("wh_mum");
            g.Node("wh_climbed")
                .Narrate("Kuba zaczynał tu od zlewu, jak w Dublinie. Po roku dostał deskę, po drugim całą zmianę. Szef kuchni mówi na niego \"Irlandczyk\".")
                .Think("W Dublinie byłem Polakiem. W Krakowie jestem Irlandczykiem. Wszędzie trochę stąd i trochę z daleka.")
                .Next("wh_mum");
            g.Node("wh_mum")
                .Pl("mum", "Kubuś, przyjedziesz na Wigilię? Tata już kupił karpia. Żywego. Pływa w wannie i ma na imię Declan.")
                .Choice("Przyjeżdżam. I zrobię pierogi jak w Dublinie.", "wh_wigilia")
                .Choice("Mamo, w Wigilię hotel jest pełny. Przyjadę w drugi dzień świąt.", "wh_later");
            g.Node("wh_wigilia")
                .Think("Karp o imieniu Declan. Tata ma poczucie humoru, którego nikt w rodzinie nie podejrzewał.")
                .Outcome(homesickness: -10f)
                .Flag("ch6_christmas_eve_home");
            g.Node("wh_later")
                .Pl("mum", "No trudno. Karp poczeka. Declan jest cierpliwy.")
                .Outcome(homesickness: 5f);
            return g;
        }

        // ------------------------------------------------------------------
        // Day 32 - Tuesday, news from far away
        // ------------------------------------------------------------------

        private static StoryScript.Graph News()
        {
            var g = new StoryScript.Graph("nw");
            Route(g, "nw", "nw_dublin", "nw_perth", "nw_poland");

            g.Node("nw_dublin")
                .Narrate("Wtorek. Na adres restauracji przychodzi pocztówka z Wrocławia: stadion w budowie, dźwigi, ten sam kask co kiedyś.")
                .Pl("tomek", "Kuba! Stawiam stadion na Euro. Tu dźwigi jeszcze się kręcą. Piwo w Kielcach ciągle czeka. T.")
                .Think("U nas dźwigi stoją, u nich się kręcą. Jakby ktoś przeniósł je przez całą Europę.")
                .Choice("Wysyłam Tomkowi irlandzki szalik na Euro i paczkę irlandzkiej herbaty.", "nw_scarf")
                .Choice("Odpisuję mu kartką z widokiem na Liffey.", "nw_card");
            g.Node("nw_scarf")
                .Think("Dwadzieścia pięć euro z przesyłką. Irlandia nie zagra na Euro na stadionie Tomka, ale szalik na tym stadionie będzie.")
                .Outcome(money: -25f, homesickness: -5f)
                .Flag("ch6_sent_a_parcel");
            g.Node("nw_card")
                .Think("Piszę trzy zdania. Na kartce z Dublina zawsze mieszczą się trzy zdania i deszcz.");

            g.Node("nw_perth")
                .Narrate("Wtorek wieczór. Skype z Polską: mama w okienku, za nią tata z gazetą.")
                .Pl("mum", "Kubuś, ty jesteś cały czerwony! Smarujesz się czymś? I jecie tam w ogóle coś normalnego?")
                .Pl("dad", "Ile ci płacą? ...Tyle? Na tydzień? Ja w zakładzie tyle przez...")
                .Think("Tata nie kończy zdania. Pierwszy raz widzę, że jest ze mnie dumny i trochę na mnie zły. I że to jedno i to samo.")
                .Choice("Wysyłam rodzicom pieniądze na święta.", "nw_gift")
                .Choice("Obiecuję, że w przyszłym roku przylecę na dłużej.", "nw_promise");
            g.Node("nw_gift")
                .Think("Trzydzieści sześć dolarów to w złotówkach prawie sto złotych. W Dublinie godzina przy zlewie była warta trzydzieści złotych, tu prawie dziewięćdziesiąt. Ekonomia ma poczucie humoru.")
                .Outcome(money: -25f, homesickness: -5f)
                .Flag("ch6_sent_a_parcel");
            g.Node("nw_promise")
                .Pl("dad", "Obiecujesz. Ty zawsze obiecujesz. ...Dobra. Trzymam cię za słowo.")
                .Outcome(homesickness: 5f);

            g.Node("nw_poland")
                .Narrate("Wtorek. Mail z Dublina, po angielsku, z błędami, których Declan nie poprawia od czterdziestu lat.")
                .En("declan", "Kooba. Still standing. Smaller menu, same sink. The new lads send regards. The country's broke and the rain is fine. Come and visit.")
                .Think("\"The rain is fine\". Tęsknię za deszczem. Kto by pomyślał.")
                .Choice("Wysyłam Declanowi paczkę: kabanosy, Żubrówka, krówki.", "nw_parcel")
                .Choice("Odpisuję, że przyjadę latem.", "nw_reply");
            g.Node("nw_parcel")
                .Think("Sto złotych z przesyłką. Krówki dla kuchni, Żubrówka dla Declana, kabanosy dla tych, którzy przeżyją Żubrówkę.")
                .Outcome(money: -25f, homesickness: -5f)
                .Flag("ch6_sent_a_parcel");
            g.Node("nw_reply")
                .Think("Lato w Dublinie. Czyli deszcz, tylko cieplejszy.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 33 - Wednesday, where everyone else ended up
        // ------------------------------------------------------------------

        private static StoryScript.Graph Others()
        {
            var g = new StoryScript.Graph("ot_1");
            g.Node("ot_1")
                .Narrate("Środa. Wieści przychodzą jak zawsze: telefonem, mailem, przez znajomych znajomych.")
                .BranchIf("ch5_lent_marek", "ot_marek_pays")
                .Next("ot_marek");
            g.Node("ot_marek_pays")
                .Pl("marek", "Kuba, oddaję te pięćdziesiąt euro z dwa tysiące ósmego. Z odsetkami w postaci przeprosin. Jeżdżę taksówką po Dublinie. Na apartament za mało, na życie wystarczy. Bez \"notions\".")
                .Outcome(money: 50f)
                .Flag("ch6_marek_paid_back")
                .Next("ot_tunde");
            g.Node("ot_marek")
                .Narrate("Marek jeździ taksówką po Dublinie i opowiada pasażerom, że był kiedyś deweloperem. Nikt mu nie wierzy, a on się z tego śmieje.")
                .Next("ot_tunde");
            g.Node("ot_tunde")
                .BranchIf("ch3_tunde_friend", "ot_tunde_numbers")
                .Next("ot_bartek");
            g.Node("ot_tunde_numbers")
                .Narrate("Tunde skończył księgowość. Pracuje w firmie, która liczy dla rządu, ile Irlandia jest winna. Mówi, że to jak zmywanie: nigdy się nie kończy, ale ktoś musi to robić.")
                .En("tunde", "Same plates, Kooba. Bigger numbers.")
                .Next("ot_bartek");
            g.Node("ot_bartek")
                .BranchIf("ch5_kept_bartek", "ot_bartek_cooks")
                .Next("ot_end");
            g.Node("ot_bartek_cooks")
                .Narrate("Bartek gotuje na zmianie, którą kiedyś prowadził Kuba. Ma własne niebieskie plastry i własnego nowego chłopaka przy zlewie.")
                .Pl("bartek", "Panie Kuba... to znaczy, Kuba. Dziękuję. Za tamten czwartek.")
                .Next("ot_end");
            g.Node("ot_end")
                .Think("Każdy z nas wylądował gdzie indziej. Ale wszyscy zaczynaliśmy przy tym samym zlewie, z tym samym strachem i tymi samymi niebieskimi plastrami.")
                .Choice("Zakładam grupę na Facebooku: \"Zlew u Declana, 2004\".", "ot_group")
                .Choice("Przepisuję wszystkie numery do nowego telefonu, żeby żaden nie zginął.", "ot_numbers");
            g.Node("ot_group")
                .Think("Pierwszy dołącza Marek. Drugi Tomek, z Wrocławia. Trzeci, po godzinie, Gavin, z komentarzem: \"Notions.\"")
                .Outcome(integration: 5f)
                .Flag("ch6_sink_group");
            g.Node("ot_numbers")
                .Think("Marek, Tomek, Agnieszka, Darius, Siobhán, Tunde, Declan. Siedem numerów. Siedem lat. Prawie.")
                .Outcome(homesickness: -5f);
            return g;
        }

        // ------------------------------------------------------------------
        // Day 34 - Thursday, a new boy at the sink
        // ------------------------------------------------------------------

        private static StoryScript.Graph NewKid()
        {
            var g = new StoryScript.Graph("nk");
            Route(g, "nk", "nk_dublin", "nk_perth", "nk_poland");

            g.Node("nk_dublin")
                .Narrate("Czwartek. W drzwiach zaplecza stoi chłopak z plecakiem, opalony nie na irlandzką pogodę. Thiago z Goiânii, student angielskiego, pierwszy dzień na zmywaku.")
                .En("thiago", "Sorry... I am Thiago. The school say you need a man for the wash. My English is... little.")
                .Next("nk_choice");
            g.Node("nk_perth")
                .Narrate("Czwartek. Z samolotu dla zmiany przylatuje nowy pomocnik do zmywaka. Piegowaty, spalony słońcem po jednym dniu, z akcentem z Cork.")
                .En("liam", "Liam. From Cork. There's no work at home, so here I am. Is it true you were a dishwasher in Dublin?")
                .Think("Irlandczyk ucieka przed kryzysem na drugi koniec świata i trafia do zlewu Polaka. Dwadzieścia lat temu byłoby to nie do pomyślenia. Sześć lat temu też.")
                .Next("nk_choice");
            g.Node("nk_poland")
                .Narrate("Czwartek. Do hotelowej kuchni przychodzi nowy pomocnik: Ołeh ze Lwowa, dwadzieścia dwa lata, studiował budownictwo. Po polsku mówi lepiej niż Kuba po angielsku sześć lat temu.")
                .Pl("oleh", "Dzień dobry. Agencja mówiła, że jest praca na zmywaku. Ja... szybko się uczę.")
                .Think("Agencja. Zmywak. \"Szybko się uczę\". Ktoś kiedyś powiedział to samo w Dublinie. Ja.")
                .Next("nk_choice");
            g.Node("nk_choice")
                .Think("Teraz to ja jestem Declanem. Duży, zmęczony, z niebieskimi plastrami w kieszeni.")
                .Choice("Pokazuję mu wszystko od początku: zeskrob, spłucz, szoruj, układaj kosz.", "nk_teach")
                .Choice("Krzyczę jak Declan. Ktoś musi.", "nk_shout")
                .Choice("Daję mu swój numer i mówię: \"Każdy kiedyś od tego zaczynał\".", "nk_words");
            g.Node("nk_teach")
                .Narrate("Talerz, patelnia, szklanka pod światło. Kosz kafelek przy kafelku. Gdzie leżą niebieskie plastry.")
                .Think("Te same ruchy co sześć lat temu, tylko teraz to ja je pokazuję. Ręce pamiętają lepiej niż głowa.")
                .Outcome(energy: -5f, integration: 5f)
                .Flag("ch6_taught_the_new_boy");
            g.Node("nk_shout")
                .Narrate("\"Plates! I need plates, not philosophy!\" - wychodzi Kubie samo, z akcentem Declana. Cała kuchnia wybucha śmiechem. Nowy też, choć nie wie dlaczego.")
                .Think("Declan krzyczał, bo był Declanem. Ja krzyczę, bo nauczyłem się od Declana. Tak przechodzą tradycje.")
                .Outcome(homesickness: -5f)
                .Flag("ch6_shouted_like_declan");
            g.Node("nk_words")
                .Think("Powiedziałem to po polsku, po angielsku i rękami. Zrozumiał we wszystkich trzech językach.")
                .Outcome(integration: 5f)
                .Flag("ch6_passed_it_on");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 35 - Friday, the last plate
        // ------------------------------------------------------------------

        private static StoryScript.Graph LastPlate()
        {
            var g = new StoryScript.Graph("lp");
            Route(g, "lp", "lp_dublin", "lp_perth", "lp_poland");

            g.Node("lp_dublin")
                .Narrate("Piątek, po zamknięciu. Pusta kuchnia nad Liffey, deszcz o szybę, ten sam od sześciu lat.")
                .Next("lp_plate");
            g.Node("lp_perth")
                .Narrate("Piątek, po ostatniej zmianie. Nad obozem gwiazdy, których nie ma na irlandzkim ani polskim niebie. Gavin śpi.")
                .Next("lp_plate");
            g.Node("lp_poland")
                .Narrate("Piątek, po zamknięciu. Za oknem hotelowej kuchni świecą się budy jarmarku, a z Mariackiej wieży ktoś gra hejnał.")
                .Next("lp_plate");
            g.Node("lp_plate")
                .Narrate("Na stali został jeden talerz. Kuba mógłby zostawić go zmywakowi. Bierze gąbkę.")
                .Think("Zeskrobać. Spłukać. Szorować, aż zapiszczy. Pod światło.")
                .Think("Camus pisał, że trzeba wyobrażać sobie Syzyfa szczęśliwym. Przez sześć lat myślałem, że to o kamieniu. To nie o kamieniu. To o tym, że ktoś go codziennie podnosi i nie przestaje być sobą.")
                .Choice("Odkładam talerz na stojak i gaszę światło.", "lp_rack")
                .Choice("Dzwonię do domu. Wszystko jedno, który dom odbierze.", "lp_call");
            g.Node("lp_rack")
                .Think("Czysty. Jeden talerz z miliona. Wyobrażam sobie Syzyfa szczęśliwym. Tym razem naprawdę.")
                .Outcome(homesickness: -5f)
                .Next("lp_end");
            g.Node("lp_call")
                .Narrate("Długi sygnał. Ktoś odbiera.")
                .Think("Nie powiem, kto. To już nieważne, w którym kraju. Ważne, że ktoś odebrał.")
                .Outcome(homesickness: -10f)
                .Flag("ch6_called_home")
                .Next("lp_end");
            g.Node("lp_end")
                .Narrate("Koniec. Dziękujemy, że zmywałeś z Kubą.");
            return g;
        }
    }
}
