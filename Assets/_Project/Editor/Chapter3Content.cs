using System.Collections.Generic;
using UnityEditor;
using Zmywak.Narrative.Story;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Chapter 3, "Swoi i obcy" (13-18 May 2004, Thursday to Tuesday): the first morning at the chopping board and
    /// Gavin, the sous-chef who times it; Tomek's week, whichever way chapter 2 left him; a Eurovision night on the
    /// house's one couch; referendum posters and "Declan's running a charity", where Kuba says his first English
    /// sentence out loud; letters from the state and from home; and the night a critic comes to dinner. It reads
    /// what chapters 1 and 2 decided: the fake bill, the rent, Tunde, Tomek, the knife, the money sent home.
    /// </summary>
    internal static class Chapter3Content
    {
        public const string ChapterAssetName = "Chapter_ch3.asset";

        public static StoryChapter Create(string folder)
        {
            var beats = new List<StoryBeat>
            {
                StoryScript.Beat("ch3_board", 13, StoryScript.SaveGraph(folder, "Ch3_1_Board", Board())),
                StoryScript.Beat("ch3_tomek", 14, StoryScript.SaveGraph(folder, "Ch3_2_Tomek", Tomek())),
                StoryScript.Beat("ch3_eurovision", 15, StoryScript.SaveGraph(folder, "Ch3_3_Eurovision", Eurovision())),
                StoryScript.Beat("ch3_gavin", 16, StoryScript.SaveGraph(folder, "Ch3_4_Gavin", Gavin())),
                StoryScript.Beat("ch3_letters", 17, StoryScript.SaveGraph(folder, "Ch3_5_Letters", Letters())),
                StoryScript.Beat("ch3_critic", 18, StoryScript.SaveGraph(folder, "Ch3_6_Critic", Critic()), endsChapter: true)
            };

            StoryChapter chapter = VerticalSliceSetup.CreateOrLoad<StoryChapter>($"{folder}/{ChapterAssetName}");
            chapter.SetContent("ch3", "Rozdział 3: Swoi i obcy", beats);
            EditorUtility.SetDirty(chapter);
            return chapter;
        }

        // ------------------------------------------------------------------
        // Day 13 - Thursday, the board
        // ------------------------------------------------------------------

        private static StoryScript.Graph Board()
        {
            var g = new StoryScript.Graph("board_1");
            g.Node("board_1")
                .Narrate("Czwartek, dziewiąta rano. Kuchnia bez gości wygląda inaczej: cicha, jasna, pachnie wybielaczem i cebulą.")
                .En("gavin", "So you're Declan's new project. I'm Gavin. Sous-chef. That means when Declan's not here, I'm God.")
                .Teach("project", "means", "here")
                .Think("Gavin. Zastępca szefa kuchni. Uśmiecha się tak, jak uśmiecha się pies, zanim ugryzie.")
                .BranchIf("ch2_prep_cook", "board_first")
                .Next("board_ask");
            g.Node("board_ask")
                .En("declan", "Well, Kooba? Gavin's been doing prep all week and giving out about it. Made up your mind?")
                .Teach("made", "mind", "week")
                .Slang("slang_giving_out")
                .Think("\"Giving out\": Gavin robi wyrzuty. Całej kuchni, od tygodnia. Przy mnie zacznie od nowa.")
                .Choice("Biorę deskę.", "board_take")
                .Choice("Zostaję przy zlewie.", "board_sink");
            g.Node("board_take")
                .En("declan", "Good man. Apron's on the hook. Gavin, show him the onions.")
                .Teach("apron", "hook", "show")
                .Flag("ch3_prep_cook")
                .Next("board_first");
            g.Node("board_sink")
                .En("declan", "Fair enough. A good man at the sink is worth two at the board.")
                .Teach("worth", "two")
                .En("gavin", "Knew it. They never stay at the board.")
                .Think("Może ma rację. A może po prostu wiem, gdzie jest moje miejsce. Na razie.")
                .Outcome(energy: 5f)
                .Flag("ch3_stays_sink")
                .Next("board_end");
            g.Node("board_first")
                .Narrate("Dziesięć kilo cebuli, pięć kilo marchwi, seler. Gavin stawia obok minutnik i patrzy.")
                .En("gavin", "Brunoise. Three millimetres. My granny cuts faster, and she's been dead since ninety-eight.")
                .Teach("three", "faster", "dead")
                .Flag("ch3_at_board")
                .Choice("Tnę szybko, żeby nadążyć za minutnikiem.", "board_fast")
                .Choice("Tnę powoli, tak jak pokazał Declan.", "board_careful");
            g.Node("board_fast")
                .Narrate("Siódma cebula. Nóż ześlizguje się z kłykcia. Krew na desce, krew na fartuchu.")
                .En("gavin", "Ah, for... Blue plaster. So you'll find it in the soup, not the customer.")
                .Teach("blue", "plaster", "soup", "customer")
                .Think("Niebieski plaster. W kuchni wszystkie są niebieskie, bo nie ma niebieskiego jedzenia. Pierwsza lekcja deski.")
                .Outcome(energy: -10f)
                .Flag("ch3_cut_finger")
                .Next("board_end");
            g.Node("board_careful")
                .Narrate("Minutnik dzwoni trzy razy, zanim Kuba kończy. Gavin przewraca oczami. Potem Declan bierze łyżkę kostek i ogląda je pod lampą.")
                .En("declan", "Three millimetres. Every one. Gavin, look at this.")
                .Teach("every", "look")
                .En("gavin", "...Grand. It's grand.")
                .Think("W ustach Gavina \"grand\" brzmi jak przegrana. Uczciwa wymiana.")
                .Outcome(integration: 5f)
                .Flag("ch3_careful")
                .Next("board_end");
            g.Node("board_end")
                .Think("Wieczorem ręce pachną cebulą mimo trzech myć. Mama mówiła, że pomaga cytryna. Nie mamy cytryny. Mamy cebulę.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 14 - Friday, Tomek's week
        // ------------------------------------------------------------------

        private static StoryScript.Graph Tomek()
        {
            var g = new StoryScript.Graph("tomek3_1");
            g.Node("tomek3_1")
                .Narrate("Piątek. Druga wypłata.")
                .BranchIf("ch2_tomek_at_sink", "tomek3_sink")
                .Next("tomek3_route");
            g.Node("tomek3_route") // no lines: chapter 2 lent him money, or said no
                .BranchIf("ch2_lent_tomek", "tomek3_repay")
                .Next("tomek3_gone");
            g.Node("tomek3_sink")
                .Narrate("Trzeci dzień Tomka przy zlewie. Cztery stłuczone talerze, patelnia w koszu na szklanki i woda na całej podłodze.")
                .En("gavin", "Your man there is a bull in a china shop. Literally. In a china shop.")
                .Teach("bull", "shop", "literally")
                .Slang("slang_your_man")
                .Pl("tomek", "Kuba, te kosze... Na budowie wszystko jest prostokątne. Tu nic do niczego nie pasuje.")
                .Choice("Zostaję po zmianie i uczę Tomka układać kosz.", "tomek3_teach")
                .Choice("Mówię mu, że musi sam. Ja też musiałem.", "tomek3_alone");
            g.Node("tomek3_teach")
                .Narrate("Do jedenastej w nocy: talerze bokiem, patelnie rączką do tyłu, szklanki dnem do góry. Tomek układa kosz jak tynk: powoli, ale potem nic nie odpada.")
                .Pl("tomek", "Wiesz co? Jak to ułożyć na płasko, to jest jak glazura. Kafelek przy kafelku.")
                .Outcome(energy: -10f, integration: 5f)
                .Flag("ch3_taught_tomek")
                .Next("tomek3_end");
            g.Node("tomek3_alone")
                .Pl("tomek", "No tak. Każdy sam.")
                .Think("Powiedziałem mu to, co kiedyś ktoś powiedział mnie. Nie wiem, czy to mądrość, czy tylko łańcuch.")
                .Outcome(homesickness: 5f)
                .Next("tomek3_end");
            g.Node("tomek3_repay")
                .Narrate("Wieczorem Tomek puka do pokoju. W ręce trzy banknoty po dziesięć euro i butelka Żubrówki.")
                .Pl("tomek", "Mówiłem: w piątek. Marek załatwił mi fuchę na Ballymun. Płacą pod stołem, ale płacą. A to na procent.")
                .Think("Trzydzieści euro wróciło. Procent stoi na parapecie. W domu na dziewięć osób starczy na jeden wieczór.")
                .Outcome(money: 30f, integration: 5f)
                .Flag("ch3_tomek_repaid")
                .Next("tomek3_end");
            g.Node("tomek3_gone")
                .Narrate("Materac Tomka jest pusty. Leży na nim kartka.")
                .Pl("tomek", "Przeniosłem się do kumpla na Ballymun. Taniej. Nie gniewam się. Trzymaj się. T.")
                .Think("Nie gniewa się. To gorzej, niż gdyby się gniewał.")
                .Choice("Wysyłam mu SMS-a: \"Sorry, stary. Piwo na mnie.\"", "tomek3_text")
                .Choice("Odkładam kartkę i idę spać.", "tomek3_end");
            g.Node("tomek3_text")
                .Pl("tomek", "Spoko. Trzymam za słowo.")
                .Outcome(homesickness: -5f)
                .Flag("ch3_texted_tomek")
                .Next("tomek3_end");
            g.Node("tomek3_end")
                .Think("Dublin jest mały. Wszyscy w końcu spotykają się przy tym samym zlewie, na tej samej budowie albo w tej samej kolejce na poczcie.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 15 - Saturday 15 May, the Eurovision final
        // ------------------------------------------------------------------

        private static StoryScript.Graph Eurovision()
        {
            var g = new StoryScript.Graph("ev_1");
            g.Node("ev_1")
                .Narrate("Sobota, 15 maja. Finał Eurowizji w Stambule. Cały dom na jednej kanapie przed telewizorem pani Byrne, w który trzeba co godzinę uderzyć z boku.")
                .Pl("marek", "Blue Café! Kuba, Polska śpiewa! Dwa tygodnie w Unii i od razu w finale. To jest znak.")
                .Pl("agnieszka", "Marek, to konkurs piosenki, nie referendum akcesyjne.")
                .Pl("darius", "Litwa nawet nie weszła do finału. Kibicuję Irlandii. Z litości.")
                .Think("Irlandczycy wygrywali ten konkurs siedem razy. Teraz siedzą w pubach i z góry śmieją się ze swojego chłopaka z balladą. Wstyd za to, że już się nie wygrywa: to też chyba narodowe.")
                .Choice("Lepię pierogi dla całego domu. Ruskie, jak u mamy.", "ev_pierogi")
                .Choice("Wymiguję się i idę spać przed północą.", "ev_sleep");
            g.Node("ev_pierogi")
                .Narrate("Mąka, ziemniaki i irlandzki cottage cheese, który udaje twaróg. Dwanaście euro. Agnieszka wałkuje, Darius lepi krzywo, Marek je surowe ciasto.")
                .Pl("mum", "Twarogu nie ma? To weź ten ich serek i odsącz przez gazę. I cebulę zeszklij, Kubuś, nie przypal!")
                .Think("Zeszklona, nie przypalona. Trzy milimetry, jak u Declana. Pierwszy raz od przyjazdu w kuchni pachnie domem.")
                .Outcome(money: -12f, energy: -5f, homesickness: -10f, integration: 5f)
                .Flag("ch3_pierogi")
                .Next("ev_end");
            g.Node("ev_sleep")
                .Think("Przez ścianę słychać, jak Marek liczy punkty po francusku. Douze points. Zasypiam gdzieś przy dwunastym kraju.")
                .Outcome(energy: 10f, homesickness: 5f)
                .Next("ev_end");
            g.Node("ev_end")
                .Narrate("Wygrywa Ukraina: Ruslana i \"Wild Dances\". Polska daleko w tabeli, Irlandia jeszcze dalej.")
                .Pl("marek", "W przyszłym roku. Mówię wam. W przyszłym roku.")
                .Think("Wschód wygrał w Europie, do której właśnie weszliśmy. Może to też znak. Marek na pewno tak uważa.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 16 - Sunday, "a charity"
        // ------------------------------------------------------------------

        private static StoryScript.Graph Gavin()
        {
            var g = new StoryScript.Graph("gv_1");
            g.Node("gv_1")
                .Narrate("Niedziela. W drodze do pracy na każdej latarni plakaty przed referendum 11 czerwca: o obywatelstwie dzieci urodzonych w Irlandii. Na jednym ktoś dopisał flamastrem: IRELAND IS FULL.")
                .Think("Nie wiem, co znaczy \"full\". Wiem, że przez dwa tygodnie wszędzie, gdzie stanąłem, było jeszcze miejsce na jednego zmywaka.")
                .Next("gv_2");
            g.Node("gv_2")
                .Narrate("Brunch, sto porcji. Przy wydawce Gavin stawia talerz tak, że sos pryska na fartuch.")
                .En("gavin", "Half this kitchen can't order a pint in English. Declan's not running a restaurant, he's running a charity.")
                .Teach("half", "order", "pint", "running", "charity", "wash", "cut", "learn", "enough")
                .Slang("slang_giving_out")
                .Think("Gavin znowu \"gives out\". Mówi w powietrze. Ale stoi przy mnie.")
                .Choice("Odpowiadam po angielsku. Krótko.", "gv_answer")
                .Choice("Udaję, że nie zrozumiałem.", "gv_quiet")
                .Choice("Patrzę na Tunde.", "gv_tunde", requiredFlag: "ch2_covered_tunde");
            g.Node("gv_answer")
                .En("kuba", "I wash. I cut. I learn. Enough?")
                .Narrate("Cisza przy wydawce. Siobhán parska śmiechem.")
                .En("siobhan", "He has you there, Gav. That's gas.")
                .Teach("has", "there")
                .Slang("slang_gas")
                .Think("Pierwsze zdanie, które powiedziałem w tej kuchni na głos. Wittgenstein pisał, że granice mojego języka są granicami mojego świata. Mój świat właśnie urósł o cztery słowa.")
                .Outcome(integration: 5f)
                .Flag("ch3_stood_up")
                .Next("gv_end");
            g.Node("gv_quiet")
                .Think("Udaję, że nie rozumiem. Bariera językowa ma swoje zalety: można przez nią nie słyszeć.")
                .Outcome(homesickness: 5f)
                .Next("gv_end");
            g.Node("gv_tunde")
                .Narrate("Tunde odkłada stos czystych talerzy na wydawkę. Powoli, talerz po talerzu.")
                .En("tunde", "Gavin. Who brought you clean plates for the last two hours? The charity?")
                .Teach("brought", "clean", "plates", "hours")
                .En("gavin", "...Ah, give over, the pair of you.")
                .Think("Tunde puszcza do mnie oko. Dwóch zmywaków z dwóch końców świata i jeden wspólny język: talerze.")
                .Outcome(integration: 5f, homesickness: -5f)
                .Flag("ch3_tunde_friend")
                .Next("gv_end");
            g.Node("gv_end")
                .Narrate("Po zmianie, przy tylnym wyjściu. Od marca w Irlandii nie wolno palić w pubach ani w restauracjach, więc cała kuchnia pali na zapleczu, przy kontenerach. Zza drzwi słychać Declana.")
                .En("declan", "I washed pots in Boston for three years, Gavin. Nobody called it charity. They called it work.")
                .Teach("washed", "pots", "years", "called", "work")
                .Narrate("Chwilę później Gavin wychodzi, zapala i bez słowa podsuwa Kubie paczkę.")
                .Think("Nie palę. Biorę jednego. Czasem rozejm wygląda jak papieros, którego się nie chce.")
                .Outcome(homesickness: -5f);
            return g;
        }

        // ------------------------------------------------------------------
        // Day 17 - Monday, two envelopes
        // ------------------------------------------------------------------

        private static StoryScript.Graph Letters()
        {
            var g = new StoryScript.Graph("lt_1");
            g.Node("lt_1")
                .Narrate("Poniedziałek. Na wycieraczce w Phibsborough dwie koperty: urzędowa, z harfą, i druga, z polskimi znaczkami.")
                .BranchIf("ch1_fake_bill", "lt_pps")
                .Next("lt_home");
            g.Node("lt_pps")
                .Narrate("Department of Social and Family Affairs prosi o \"dodatkowe potwierdzenie adresu\". Rachunek z przyszłego miesiąca ktoś jednak przeczytał.")
                .Think("Potrzebuję pisma od właścicielki, że tu mieszkam. Od pani Byrne. Od \"I will, yeah\".")
                .BranchIf("ch2_rent_on_time", "lt_signs")
                .Next("lt_fee");
            g.Node("lt_signs")
                .En("byrne", "You paid on time, love. Course I'll sign. I will, yeah. And this time I mean it.")
                .Teach("course", "sign", "mean")
                .Think("Pierwszy raz \"I will, yeah\" znaczy \"tak\". Czynsz zapłacony na czas to w Dublinie najlepsza rekomendacja.")
                .Outcome(integration: 5f)
                .Flag("ch3_byrne_signed")
                .Next("lt_home");
            g.Node("lt_fee")
                .En("byrne", "A letter? That's paperwork, love. Paperwork is twenty euro.")
                .Teach("letter", "paperwork", "twenty")
                .Think("Dwadzieścia euro za jedno zdanie i podpis. Albo wieczór z Agnieszką nad pismem, które napiszę sam.")
                .Choice("Płacę dwadzieścia euro.", "lt_pay")
                .Choice("Proszę Agnieszkę, żeby pomogła mi napisać pismo.", "lt_agnieszka");
            g.Node("lt_pay")
                .Narrate("Pani Byrne podpisuje, nie czytając. Banknot znika w kieszeni fartucha szybciej niż długopis.")
                .Outcome(money: -20f)
                .Flag("ch3_byrne_signed")
                .Next("lt_home");
            g.Node("lt_agnieszka")
                .Pl("agnieszka", "\"To whom it may concern\". Zawsze tak się zaczyna. Nie, Kuba, \"concern\" przez \"c\". Siadaj, piszemy od nowa.")
                .Teach("concern")
                .Think("Trzy wersje, dwie herbaty i jedno \"Yours faithfully\". Urzędowy angielski jest jak łacina: nikt nim nie mówi, ale każdy musi w nim umieć prosić.")
                .Outcome(energy: -10f, homesickness: -5f)
                .Flag("ch3_wrote_letter")
                .Next("lt_home");
            g.Node("lt_home")
                .Narrate("Koperta od mamy. W środku dyplom magistra filozofii, zwinięty w rulon i owinięty w \"Gazetę Wyborczą\".")
                .Pl("mum", "Kubuś, przyszedł z uczelni. Wysyłam, bo może ci się tam przyda. Ojciec mówi, że w Irlandii też są uniwersytety.")
                .BranchIf("ch2_sent_home", "lt_photo")
                .Next("lt_choice");
            g.Node("lt_photo")
                .Narrate("Pod dyplomem zdjęcie: Ola w białej sukience komunijnej, w wianku, z dwoma zębami mniej niż w zeszłym roku. Na odwrocie krzywymi literami: \"Dziękuję, Kuba!\"")
                .Outcome(homesickness: -10f)
                .Next("lt_choice");
            g.Node("lt_choice")
                .Think("Pięć lat Platona, Kanta i Camusa w rulonie z \"Wyborczej\". Co z tym zrobić w pokoju bez gwoździ?")
                .Choice("Przypinam dyplom nad materacem.", "lt_wall")
                .Choice("Chowam go na dno walizki.", "lt_case");
            g.Node("lt_wall")
                .Think("Wisi krzywo, na pinezkach od Agnieszki. Marek mówi, że to pierwszy dyplom w historii tego domu. Darius salutuje.")
                .Outcome(homesickness: -5f)
                .Flag("ch3_diploma_up");
            g.Node("lt_case")
                .Think("Na dno walizki, pod sweter od mamy. Nie dlatego, że się wstydzę. Dlatego, że tutaj jeszcze na niego nie zarobiłem.")
                .Flag("ch3_diploma_packed");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 18 - Tuesday, a critic at table seven
        // ------------------------------------------------------------------

        private static StoryScript.Graph Critic()
        {
            var g = new StoryScript.Graph("cr_1");
            g.Node("cr_1")
                .Narrate("Wtorek wieczór. Siobhán wpada do kuchni blada jak talerz.")
                .En("siobhan", "Table seven. That's your woman from the Irish Times. The food critic. I'd know her anywhere.")
                .Teach("seven", "critic", "anywhere")
                .Slang("slang_your_man")
                .En("declan", "Right. Nobody panics. Nobody. Gavin, the lamb. Kooba...")
                .Teach("panics", "lamb")
                .BranchIf("ch3_at_board", "cr_board")
                .Next("cr_sink");
            g.Node("cr_board")
                .En("declan", "...brunoise for the soup. Three millimetres. Like Thursday.")
                .Think("Jak w czwartek. Palce zgięte jak pazury, ostrze prowadzi kłykieć. Trzy milimetry.")
                .Next("cr_crash");
            g.Node("cr_sink")
                .En("declan", "...plates. Hot plates, clean plates, all night. You're the engine, Kooba.")
                .Teach("engine", "hot", "night")
                .Think("Silnik. Pierwszy raz ktoś nazwał zmywaka silnikiem. Syzyf by się uśmiechnął.")
                .Next("cr_crash");
            g.Node("cr_crash")
                .Narrate("Za dwadzieścia dziewiąta. Gavin chwyta rondel z sosem bez ścierki i syczy przez zęby. Ręka czerwona, sos na podłodze, jagnięcina dla stolika siedem za cztery minuty.")
                .En("gavin", "Banjaxed. My hand's banjaxed.")
                .Teach("hand")
                .Slang("slang_banjaxed")
                .Choice("Rzucam swoje i pomagam Gavinowi.", "cr_help")
                .Choice("Pilnuję swojej roboty. Declan jest blisko.", "cr_own");
            g.Node("cr_help")
                .Narrate("Zimna woda na rękę Gavina, czysty rondel, ścierka. Kuba podaje, Gavin jedną ręką kończy sos. Trzy minuty pięćdziesiąt sekund.")
                .En("gavin", "...Cheers, Kooba. Don't tell anyone.")
                .Teach("cheers", "tell", "anyone")
                .Outcome(energy: -5f)
                .Flag("ch3_helped_gavin")
                .Next("cr_end");
            g.Node("cr_own")
                .Narrate("Declan sam łapie rondel. Klnie po irlandzku, którego Kuba nie zna, ale rozumie każde słowo. Jagnięcina wychodzi minutę za późno.")
                .Think("Zrobiłem swoje. Czy to wystarczy? W kuchni chyba nigdy nie wystarcza.")
                .Next("cr_end");
            g.Node("cr_end")
                .Narrate("Czwartek. Recenzja w \"The Irish Times\": \"a quietly confident kitchen\", cztery gwiazdki na pięć. Declan przypina wycinek nad wydawką, obok grafiku.")
                .En("declan", "Four stars. The whole city's eating out these days, Kooba. Money everywhere. Watch this place grow.")
                .Teach("stars", "city", "eating", "money", "grow")
                .En("gavin", "Not bad. For a charity.")
                .Think("Gavin się uśmiecha. Nie do mnie, ale też nie przeciwko mnie. Trzy tygodnie temu nie znałem tu nikogo. Dziś znam ludzi, którzy prawie wiedzą, jak mam na imię.")
                .Outcome(homesickness: -5f)
                .Narrate("Koniec Rozdziału 3: Swoi i obcy.");
            return g;
        }
    }
}
