using System.Collections.Generic;
using UnityEditor;
using Zmywak.Narrative.Story;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Chapter 2, "Pierwsza wypłata" (7-12 May 2004, Friday to Wednesday - day N is the Nth of May): the first
    /// envelope and what it has to cover, Siobhán and the language nobody teaches at school, a Sunday brunch that goes
    /// wrong, a Monday night in the pub where everyone buys a round, Tomek losing his site job, and Declan asking
    /// whether Kuba can hold a knife - the first step up from the sink. Declan's letter from chapter 1 carries over.
    /// </summary>
    internal static class Chapter2Content
    {
        public const string ChapterAssetName = "Chapter_ch2.asset";

        public static StoryChapter Create(string folder)
        {
            var beats = new List<StoryBeat>
            {
                StoryScript.Beat("ch2_payday", 7, StoryScript.SaveGraph(folder, "Ch2_1_Payday", Payday())),
                StoryScript.Beat("ch2_siobhan", 8, StoryScript.SaveGraph(folder, "Ch2_2_Siobhan", Siobhan())),
                StoryScript.Beat("ch2_brunch", 9, StoryScript.SaveGraph(folder, "Ch2_3_Brunch", Brunch())),
                StoryScript.Beat("ch2_pub", 10, StoryScript.SaveGraph(folder, "Ch2_4_Pub", Pub())),
                StoryScript.Beat("ch2_tomek", 11, StoryScript.SaveGraph(folder, "Ch2_5_Tomek", Tomek())),
                StoryScript.Beat("ch2_knife", 12, StoryScript.SaveGraph(folder, "Ch2_6_Knife", Knife()), endsChapter: true)
            };

            StoryChapter chapter = VerticalSliceSetup.CreateOrLoad<StoryChapter>($"{folder}/{ChapterAssetName}");
            chapter.SetContent("ch2", "Rozdział 2: Pierwsza wypłata", beats);
            EditorUtility.SetDirty(chapter);
            return chapter;
        }

        // ------------------------------------------------------------------
        // Day 7 - Friday, the envelope
        // ------------------------------------------------------------------

        private static StoryScript.Graph Payday()
        {
            var g = new StoryScript.Graph("pay_1");
            g.Node("pay_1")
                .Narrate("Piątek, po zamknięciu kuchni. Declan stoi przy zlewie z plikiem brązowych kopert i wywołuje nazwiska jak na apelu.")
                .En("declan", "Kooba! There you are. First week's wages. Count it, I'm not a bank.")
                .Teach("wages", "count", "first", "week")
                .Think("Koperta. Prawdziwe pieniądze za prawdziwą pracę. Przeliczone na złotówki brzmią jak wygrana w totka, w Dublinie jak tydzień czynszu i kilka obiadów.")
                .Next("pay_choice");
            g.Node("pay_choice")
                .Think("Czynsz u pani Byrne jest dziś. Mama pytała, czy dołożę się do komunii Oli. A moje trampki przy zlewie ślizgają się jak łyżwy.")
                .Choice("Najpierw czynsz. Pani Byrne nie lubi czekać.", "pay_rent")
                .Choice("Wysyłam pięćdziesiąt euro do domu.", "pay_home")
                .Choice("Kupuję porządne buty kuchenne, antypoślizgowe.", "pay_shoes");
            g.Node("pay_rent")
                .En("byrne", "Friday and on time? Well now. You're a rare one, love.")
                .Teach("time", "rare", "love")
                .Think("Pani Byrne prawie się uśmiechnęła. W tym domu to jak zaćmienie słońca.")
                .Outcome(integration: 5f)
                .Flag("ch2_rent_on_time")
                .Next("pay_end");
            g.Node("pay_home")
                .Narrate("Poczta przy O'Connell Street: formularz przelewu, kolejka, pani za szybą z długopisem na sznurku. Wieczorem dzwoni mama.")
                .Pl("mum", "Kubuś, przyszło! Ola będzie miała sukienkę jak księżniczka. Ojciec mówi, żebyś sobie nie odejmował od ust.")
                .Think("Pięćdziesiąt euro. Dla mamy to pół pensji. Dla mnie trzy dni bez kanapki z boczkiem. Uczciwa wymiana.")
                .Outcome(money: -50f, homesickness: -10f)
                .Flag("ch2_sent_home")
                .Next("pay_end");
            g.Node("pay_shoes")
                .Narrate("Sklep z odzieżą roboczą na Capel Street. Czarne, ciężkie, z podeszwą jak opony traktora.")
                .Think("Czterdzieści euro. Ale na mokrej podłodze stoję teraz jak przyspawany. Pierwsza rzecz w Dublinie, która jest naprawdę moja.")
                .Outcome(money: -40f, energy: 10f)
                .Flag("ch2_good_shoes")
                .Next("pay_end");
            g.Node("pay_end")
                .Think("Koperta chudnie szybciej, niż się wypełniała. Za to jestem w niej zapisany jako \"Kooba\". To już prawie nazwisko.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 8 - Siobhán, and the words they do not teach at school
        // ------------------------------------------------------------------

        private static StoryScript.Graph Siobhan()
        {
            var g = new StoryScript.Graph("sio_1");
            g.Node("sio_1")
                .Narrate("Sobota, wieczór, pełna sala. Przez okienko podawcze co chwilę wpada głowa w rudych lokach i woła zamówienia szybciej, niż Kuba je rozumie.")
                .En("siobhan", "Two plates for table four, and where's the jacks key? Some eejit locked it again!")
                .Teach("table", "key", "locked", "again")
                .Think("\"Jacks\". Nie mam pojęcia, co to jest. Ale mina mówi, że coś pilnego.")
                .Choice("Podaję jej klucz z haczyka nad zlewem.", "sio_key")
                .Choice("Pytam, co to jest \"jacks\".", "sio_ask");
            g.Node("sio_key")
                .En("siobhan", "Ah, you're a star! The jacks is the toilet, by the way. You'll learn.")
                .Teach("star", "toilet", "learn")
                .Think("Strzał w ciemno. Trafiony. Filozofia nazywa to intuicją, ja nazywam to haczykiem z napisem WC.")
                .Slang("slang_jacks")
                .Outcome(integration: 5f)
                .Next("sio_invite");
            g.Node("sio_ask")
                .En("siobhan", "The jacks? The toilet, pet! And an eejit is... well, whoever locked it. Not you, I hope.")
                .Teach("toilet", "hope")
                .Think("Dwa słowa w jednym zdaniu. Agnieszka będzie zazdrosna, że uczę się szybciej w kuchni niż ona na kursie.")
                .Slang("slang_jacks", "slang_eejit")
                .Outcome(integration: 5f)
                .Next("sio_invite");
            g.Node("sio_invite")
                .En("siobhan", "Mondays are dead, so after close on Monday we're all going to Mulligan's. The whole kitchen. You coming?")
                .Teach("dead", "going", "close", "whole", "coming")
                .Think("W poniedziałek po zamknięciu do pubu. Cała kuchnia. W kopercie zostało, ile zostało.")
                .Choice("Idę. Raz się żyje.", "sio_yes")
                .Choice("Wymiguję się. Muszę się wyspać.", "sio_no");
            g.Node("sio_yes")
                .En("siobhan", "Deadly! Bring your wallet, it's your round after mine.")
                .Teach("wallet", "round")
                .Slang("slang_deadly")
                .Flag("ch2_pub_yes")
                .Next("sio_end");
            g.Node("sio_no")
                .En("siobhan", "Ah, you're no craic at all. Next time, so.")
                .Teach("time")
                .Flag("ch2_pub_no")
                .Next("sio_end");
            g.Node("sio_end")
                .Think("W szkole uczyli mnie \"How do you do\". Nikt tu tak nie mówi. Tu mówi się \"what's the craic\" i \"you're a star\". Podręczniki kłamały.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 9 - Sunday brunch
        // ------------------------------------------------------------------

        private static StoryScript.Graph Brunch()
        {
            var g = new StoryScript.Graph("brunch_1");
            g.Node("brunch_1")
                .Narrate("Niedziela, jedenasta. Sto dwadzieścia porcji \"full Irish\": jajka, bekon, kiełbaski, black pudding, fasolka. Talerze wracają szybciej, niż Kuba je myje.")
                .En("declan", "Plates, Kooba! I need plates, not philosophy!")
                .Teach("need")
                .Think("Skąd wie o filozofii? Marek. Oczywiście, że Marek.")
                .BranchIf("ch2_good_shoes", "brunch_steady")
                .Next("brunch_slip");
            g.Node("brunch_steady")
                .Think("W nowych butach stoję na mokrej podłodze jak przyspawany. Jedyna stabilna rzecz w tej kuchni to ja. Czterdzieści euro dobrze wydane.")
                .Outcome(energy: 5f)
                .Next("brunch_crash");
            g.Node("brunch_slip")
                .Think("Trampki jadą po mokrych kafelkach jak po lodzie. Dwa razy prawie ląduję w zlewie razem z talerzami.")
                .Outcome(energy: -5f)
                .Next("brunch_crash");
            g.Node("brunch_crash")
                .Narrate("Nowy pomocnik, chłopak z Lagos, pierwszy dzień, niesie za wysoki stos. Talerze lecą na posadzkę. Cała kuchnia zamiera.")
                .En("declan", "Who did that?! Who?!")
                .Teach("who")
                .Think("Chłopak jest biały jak talerz. Patrzy na mnie. Ja patrzę na skorupy.")
                .Choice("Biorę to na siebie.", "brunch_cover")
                .Choice("Milczę i zbieram skorupy.", "brunch_quiet");
            g.Node("brunch_cover")
                .En("declan", "You? Hm. Then sweep it up and move. We'll talk later.")
                .Teach("sweep", "move", "talk", "later")
                .Think("Chłopak z Lagos ma na imię Tunde. Po zmianie przynosi mi kubek herbaty z trzema łyżeczkami cukru. Po irlandzku znaczy to \"dziękuję\". Po nigeryjsku chyba też.")
                .Outcome(integration: 10f, energy: -5f)
                .Flag("ch2_covered_tunde")
                .Next("brunch_end");
            g.Node("brunch_quiet")
                .En("declan", "Nobody? Grand. Then everybody stays late tonight.")
                .Teach("everybody", "stays", "late", "tonight")
                .Think("Zostajemy wszyscy do pierwszej. Tunde zmywa ze mną w milczeniu. Myślę o Camusie: \"Dżuma\", ludzie, którzy robią, co trzeba. Ja nie zrobiłem nic. To też jest wybór.")
                .Outcome(energy: -15f)
                .Next("brunch_end");
            g.Node("brunch_end")
                .Think("Sto dwadzieścia porcji. Każdy talerz przeszedł przez moje ręce. Gdyby za to dawali punkty, byłbym w czołówce ligi.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 10 - Monday night at Mulligan's (or a quiet night in)
        // ------------------------------------------------------------------

        private static StoryScript.Graph Pub()
        {
            var g = new StoryScript.Graph("pub_1");
            g.Node("pub_1")
                .Narrate("Poniedziałek, wieczór po zamknięciu kuchni. W poniedziałki w restauracji pusto, więc kuchnia wychodzi na miasto.")
                .BranchIf("ch2_pub_yes", "pub_mulligans")
                .Next("pub_home");
            g.Node("pub_mulligans")
                .Narrate("Mulligan's. Ciemne drewno, lepka podłoga, telewizor z powtórką meczu. Cała kuchnia przy jednym stole, nawet Declan, choć udaje, że przyszedł tylko na jedno.")
                .En("siobhan", "Right, rules. I get a round, then you, then him. Nobody leaves till everyone's bought one. That's Ireland.")
                .Teach("rules", "round", "nobody", "leaves", "everyone")
                .Think("Siedem osób, siedem rund. Siedem razy po pięć euro. Matematyka jest bezlitosna, ale Guinness łagodzi ból.")
                .Next("pub_round");
            g.Node("pub_round")
                .Think("Moja kolej. Barman patrzy wyczekująco.")
                .Choice("Stawiam rundę dla wszystkich.", "pub_buy")
                .Choice("Zamawiam tylko dla siebie i udaję, że nie znam zasad.", "pub_cheap")
                .Choice("Mówię wprost, że mnie nie stać.", "pub_honest");
            g.Node("pub_buy")
                .En("declan", "Good man, Kooba. A round from the sink! Now we know you're one of us.")
                .Teach("one", "us", "know")
                .Think("Trzydzieści pięć euro. Za to Declan po raz pierwszy wymówił moje imię bez \"the new fella\" w środku.")
                .Outcome(money: -35f, integration: 10f, energy: -10f)
                .Flag("ch2_bought_round")
                .Next("pub_song");
            g.Node("pub_cheap")
                .En("siobhan", "Ah, no. No, no, no. That's not how it works, pet.")
                .Teach("how", "works")
                .Think("Cały stół patrzy na mnie, jakbym nadepnął komuś na psa. Kupuję rundę. Za te same pieniądze, tylko ze wstydem w gratisie.")
                .Outcome(money: -35f, energy: -10f)
                .Next("pub_song");
            g.Node("pub_honest")
                .En("siobhan", "Ah, sure look, you're grand. Get the next one when you're rich, so.")
                .Teach("sure", "look", "next", "rich")
                .Think("W Polsce spaliłbym się ze wstydu. Tu nikt nie robi z tego sprawy. Może dlatego, że każdy przy tym stole kiedyś nie miał.")
                .Outcome(integration: 5f, energy: -10f)
                .Next("pub_song");
            g.Node("pub_song")
                .Narrate("Koło północy ktoś zaczyna śpiewać \"The Fields of Athenry\". Śpiewa cały pub. Kuba nie zna słów, ale zna to uczucie: tęsknotę za miejscem, z którego się wyjechało.")
                .En("siobhan", "You're sound, Kooba, d'you know that? Sound. It's the best thing we call anyone.")
                .Teach("best", "call", "anyone")
                .Think("Irlandczycy wyjeżdżali za chlebem przez dwieście lat. Może dlatego tak łatwo nas tu przyjmują. Wiedzą, jak to jest.")
                .Slang("slang_sound")
                .Outcome(homesickness: -5f)
                .Next("pub_end");
            g.Node("pub_home")
                .Narrate("Wieczór w domu w Phibsborough. Agnieszka przy kuchennym stole z fiszkami.")
                .Pl("agnieszka", "Siobhán? To ta ruda kelnerka? Zaprosiła cię, a ty siedzisz tu ze mną i fiszkami? Kuba, filozofie, ty naprawdę nie umiesz żyć.")
                .Think("Za to umiem spać. Osiem godzin. Pierwszy raz od przyjazdu.")
                .Outcome(energy: 15f, homesickness: 5f)
                .Next("pub_end");
            g.Node("pub_end")
                .Think("Przed snem widzę Tomka w kuchni nad kalkulatorem. Liczy coś, czego nie chce mi pokazać. Mówi tylko, że na budowie \"coś się kroi\".");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 11 - Tomek
        // ------------------------------------------------------------------

        private static StoryScript.Graph Tomek()
        {
            var g = new StoryScript.Graph("tomek_1");
            g.Node("tomek_1")
                .Narrate("Wtorek rano. Tomek siedzi na schodach przed domem z kaskiem na kolanach. Nie poszedł na budowę. Budowa poszła bez niego.")
                .Pl("tomek", "Zwolnili pół ekipy. Bez słowa, po prostu nie ma nazwiska na liście. Czynsz w piątek, a ja mam dwadzieścia euro i kask.")
                .Think("Znam tę minę. Widziałem ją w lustrze na lotnisku pierwszego maja.")
                .Choice("Pożyczam mu trzydzieści euro.", "tomek_lend")
                .Choice("Pytam Declana, czy nie potrzebuje kogoś na zmywak.", "tomek_declan", requiredFlag: "ch1_declan_letter")
                .Choice("Mówię, że sam ledwo wiążę koniec z końcem.", "tomek_no");
            g.Node("tomek_lend")
                .Pl("tomek", "Oddam w przyszły piątek. Słowo. A jak nie oddam, to możesz spać na moim pół materaca do końca roku.")
                .Think("Trzydzieści euro mniej. Ale w domu na dziewięć osób dług wdzięczności to waluta twardsza niż euro.")
                .Outcome(money: -30f, integration: 5f)
                .Flag("ch2_lent_tomek")
                .Next("tomek_end");
            g.Node("tomek_declan")
                .En("declan", "Another Polish lad? If he works like you, send him in. Tomorrow, seven. And tell him I shout.")
                .Teach("another", "send", "tomorrow", "shout")
                .Think("Każdy kiedyś od tego zaczynał. Teraz ja piszę \"list\". Ten sam, który Declan dostał w Bostonie. Tak to działa.")
                .Pl("tomek", "Zmywak? Ja, majster od tynków? ...A wiesz co, biorę. Lepsza mokra robota niż żadna.")
                .Outcome(integration: 10f, homesickness: -5f)
                .Flag("ch2_tomek_at_sink")
                .Next("tomek_end");
            g.Node("tomek_no")
                .Pl("tomek", "Jasne. Rozumiem. Każdy ma swoje.")
                .Think("Rozumie. I właśnie dlatego jest mi głupio przez cały dzień.")
                .Outcome(homesickness: 5f)
                .Next("tomek_end");
            g.Node("tomek_end")
                .Think("Dublin to miasto, w którym połowa ludzi kogoś zna, a druga połowa kogoś szuka. Ja już jestem w tej pierwszej połowie. Ledwo.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 12 - can you hold a knife?
        // ------------------------------------------------------------------

        private static StoryScript.Graph Knife()
        {
            var g = new StoryScript.Graph("knife_1");
            g.Node("knife_1")
                .Narrate("Środa, przed otwarciem. Declan staje przy zlewie z nożem szefa kuchni w ręce i workiem cebuli pod pachą.")
                .En("declan", "Our prep cook is gone back to Cork. Can you hold a knife, Kooba?")
                .Teach("prep", "cook", "gone", "hold", "knife")
                .Think("Czy umiem trzymać nóż. Pięć lat filozofii i nikt mnie o to nie zapytał.")
                .BranchIf("ch2_covered_tunde", "knife_tunde")
                .Next("knife_try");
            g.Node("knife_tunde")
                .En("declan", "And about Sunday. Tunde told me. You're sound, Kooba. That's why I'm asking you.")
                .Teach("told", "asking")
                .Think("\"Sound\". Po minie Declana poznaję, że to komplement. Chyba największy, jaki ma na stanie.")
                .Slang("slang_sound")
                .Outcome(integration: 5f)
                .Next("knife_try");
            g.Node("knife_try")
                .Narrate("Deska, cebula, nóż. Declan pokazuje raz: palce zgięte jak pazury, ostrze prowadzi kłykieć. Dziesięć cebul później Kuba płacze, ale nie ze smutku.")
                .En("declan", "Not bad. Not good either. Two hours of prep before every shift, the sink after. Same money for now. Deal?")
                .Teach("bad", "either", "hours", "every", "money", "deal")
                .Choice("Biorę. Od zlewu do deski - to jest kariera.", "knife_yes")
                .Choice("Proszę o tydzień do namysłu.", "knife_wait");
            g.Node("knife_yes")
                .En("declan", "Good man. Tomorrow, nine. Bring your own plasters.")
                .Teach("plasters")
                .Think("Syzyf dostał nowy kamień. Mniejszy, ostrzejszy i pachnący cebulą. Wyobrażam go sobie szczęśliwym. Tym razem działa.")
                .Outcome(integration: 5f, energy: -5f)
                .Flag("ch2_prep_cook")
                .Next("knife_end");
            g.Node("knife_wait")
                .En("declan", "A week? Fair enough. The onions will wait. I won't, so don't be long.")
                .Teach("fair", "enough", "wait", "long")
                .Think("Nie wiem, dlaczego się waham. Może dlatego, że przy zlewie wiem, kim jestem. A za deską nie wiem jeszcze nic.")
                .Flag("ch2_prep_later")
                .Next("knife_end");
            g.Node("knife_end")
                .Narrate("Wieczorem budka telefoniczna na rogu Phibsborough Road. Karta za pięć euro, dwanaście minut do Polski.")
                .Pl("mum", "Kubuś, chcą cię na kucharza? Mój syn, kucharz w Dublinie! Ojciec mówi, że w rodzinie zawsze był talent do noża. Po dziadku rzeźniku.")
                .Think("Drugi tydzień w Dublinie: pierwsza koperta, pierwsze irlandzkie słowa, pierwsza cebula. Brudne talerze dalej czekają. Ale ktoś zobaczył, że umiem coś więcej.")
                .Outcome(homesickness: -10f)
                .Narrate("Koniec Rozdziału 2: Pierwsza wypłata.");
            return g;
        }
    }
}
