using System;
using System.Collections.Generic;
using UnityEditor;
using Zmywak.Narrative.Story;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Chapter 4, "Celtycki Tygrys" (1-6 May 2006): two years on - the calendar jumps there as the chapter begins.
    /// Kuba runs a station or the sink; a boy from Białystok walks in the way Kuba did; Marek sells flats off the
    /// plans (banks lend everyone everything); the złoty is strong and Mum asks whether he will come home; Declan
    /// offers half the fees for cookery school; the second restaurant opens to champagne and a five-hundred-euro
    /// tip; and Declan, over whiskey, says that nothing in this country ever lasts.
    /// </summary>
    internal static class Chapter4Content
    {
        public const string ChapterAssetName = "Chapter_ch4.asset";

        /// <summary>Two years to the day after Kuba landed.</summary>
        public static readonly DateTime StartDate = new DateTime(2006, 5, 1);

        public static StoryChapter Create(string folder)
        {
            var beats = new List<StoryBeat>
            {
                StoryScript.Beat("ch4_anniversary", 19, StoryScript.SaveGraph(folder, "Ch4_1_Anniversary", Anniversary(), fluent: true)),
                StoryScript.Beat("ch4_flats", 20, StoryScript.SaveGraph(folder, "Ch4_2_Flats", Flats(), fluent: true)),
                StoryScript.Beat("ch4_zloty", 21, StoryScript.SaveGraph(folder, "Ch4_3_Zloty", Zloty(), fluent: true)),
                StoryScript.Beat("ch4_school", 22, StoryScript.SaveGraph(folder, "Ch4_4_School", School(), fluent: true)),
                StoryScript.Beat("ch4_opening", 23, StoryScript.SaveGraph(folder, "Ch4_5_Opening", Opening(), fluent: true)),
                StoryScript.Beat("ch4_cranes", 24, StoryScript.SaveGraph(folder, "Ch4_6_Cranes", Cranes(), fluent: true), endsChapter: true)
            };

            StoryChapter chapter = VerticalSliceSetup.CreateOrLoad<StoryChapter>($"{folder}/{ChapterAssetName}");
            chapter.SetContent("ch4", "Rozdział 4: Celtycki Tygrys", beats, StartDate);
            EditorUtility.SetDirty(chapter);
            return chapter;
        }

        // ------------------------------------------------------------------
        // Day 19 - Monday 1 May 2006, two anniversaries
        // ------------------------------------------------------------------

        private static StoryScript.Graph Anniversary()
        {
            var g = new StoryScript.Graph("an_1");
            g.Node("an_1")
                .Narrate("1 maja 2006. Dwa lata temu, co do dnia, Kuba wylądował w Dublinie z dwudziestoma euro i biletem w jedną stronę. Polska jest w Unii od dwóch lat. Irlandia jest w czymś, co gazety nazywają Celtyckim Tygrysem.")
                .Think("Nad miastem więcej dźwigów niż kościołów. W kuchni więcej polskiego niż angielskiego. Dwa lata temu nie wiedziałem, co to \"grand\". Dziś mówię tak przez sen.")
                .BranchIf("ch3_at_board", "an_board")
                .Next("an_sink");
            g.Node("an_board")
                .Narrate("Kuba prowadzi dziś przystawki i zupy: własna stacja, dwóch ludzi pod sobą, nóż, którego nikt inny nie dotyka.")
                .En("declan", "Two years, Kooba. Started at my sink, now you run my starters. Don't let it go to your head.")
                .Teach("starters", "head", "run", "started")
                .Next("an_bartek");
            g.Node("an_sink")
                .Narrate("Kuba jest dziś głównym zmywakiem: trzech ludzi przy zlewie, grafik na drzwiach, zamówienia chemii. Mówią na niego \"szef zlewu\" i nie zawsze żartem.")
                .En("declan", "Two years, Kooba. Best sink in Dublin, and I've seen a few. Still don't want the board?")
                .Teach("best", "seen", "still")
                .Think("Nie chcę. Albo jeszcze nie chcę. Przy zlewie wiem, kim jestem.")
                .Next("an_bartek");
            g.Node("an_bartek")
                .Narrate("W drzwiach zaplecza stoi chłopak w za dużej kurtce, z reklamówką. Biały jak talerz.")
                .Pl("bartek", "Dzień dobry... Pan Kuba? Marek mówił, że pan tu pracuje. Agencja w Białymstoku wzięła ode mnie trzysta złotych. Pracy nie ma, adresu nie ma, nic nie ma.")
                .Think("Lotnisko, pierwszy maja, dwadzieścia euro. Jakbym patrzył w lustro sprzed dwóch lat.")
                .Choice("Biorę Bartka pod skrzydła. Najpierw zlew, potem zobaczymy.", "an_mentor")
                .Choice("Daję mu numer do Marka i adres polskiego sklepu z ogłoszeniami.", "an_address")
                .Choice("Proszę Declana, żeby dał mu szansę, jak kiedyś mnie.", "an_letter", requiredFlag: "ch1_declan_letter");
            g.Node("an_mentor")
                .Narrate("Kuba pokazuje Bartkowi wszystko, czego sam uczył się dwa lata: jak zeskrobać, jak spłukać, jak ułożyć kosz, gdzie leżą niebieskie plastry.")
                .Pl("bartek", "A ten szef, ten duży... On krzyczy, bo jest zły?")
                .Think("Krzyczy, bo jest Declanem. Tego też się nauczysz.")
                .Outcome(energy: -5f, homesickness: -5f)
                .Flag("ch4_mentor_bartek");
            g.Node("an_address")
                .Pl("bartek", "Dziękuję. Naprawdę.")
                .Think("Dałem mu to, co sam dostałem na początku od Marka: adres i numer. Dużo czy mało? Mnie wtedy wystarczyło.")
                .Flag("ch4_sent_bartek_on");
            g.Node("an_letter")
                .En("declan", "Another one? Kooba, you'll have the whole of Poland in my kitchen. ...Fine. Monday, seven. He's your responsibility.")
                .Teach("another", "whole", "responsibility")
                .Think("Każdy kiedyś od tego zaczynał. Dwa lata temu ktoś napisał list za mnie. Dziś ja go piszę.")
                .Outcome(integration: 5f)
                .Flag("ch4_mentor_bartek", "ch4_vouched_bartek");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 20 - Tuesday, flats off the plans
        // ------------------------------------------------------------------

        private static StoryScript.Graph Flats()
        {
            var g = new StoryScript.Graph("fl_1");
            g.Node("fl_1")
                .Narrate("Wtorek wieczór. Marek przychodzi do pubu z teczką kolorowych folderów: \"Riverside Gardens, Navan. Apartamenty od 285 000 euro\".")
                .Pl("marek", "Kuba, słuchaj. Banki dają sto procent kredytu. Nawet nam. Rezerwacja to pięćdziesiąt euro, resztę kredyt. Za dwa lata to będzie warte czterysta tysięcy. Ceny tylko rosną, to jest Irlandia!")
                .En("declan", "Property? Ah, Marek has notions. Everyone in this country has notions these days.")
                .Teach("property", "everyone", "country", "days")
                .Slang("slang_notions")
                .Think("Dwieście osiemdziesiąt pięć tysięcy. W złotówkach nie umiem nawet tego przeczytać. Heraklit mówił, że wszystko płynie. Ceny też? W którą stronę?")
                .BranchIf("ch2_tomek_at_sink", "fl_tomek")
                .Next("fl_choice");
            g.Node("fl_tomek")
                .Pl("tomek", "Rzuciłem zlew rok temu. Brygadzista w Navan, tysiąc euro tygodniowo, pod stołem. Te mieszkania to ja stawiam, Kuba. Ściany jak z papieru, ale sprzedają się, zanim wyschnie tynk.")
                .Think("Tomek mówi to z dumą. I z czymś jeszcze, czego nie umiem nazwać.")
                .Next("fl_choice");
            g.Node("fl_choice")
                .Think("Pięćdziesiąt euro dziś. Trzysta tysięcy kiedyś. Gdzieś pomiędzy jest moje życie.")
                .Choice("Wpłacam pięćdziesiąt euro rezerwacji.", "fl_book")
                .Choice("Mówię, że muszę to przemyśleć.", "fl_think")
                .Choice("Odmawiam. Nie kupuję czegoś, czego jeszcze nie ma.", "fl_no");
            g.Node("fl_book")
                .Narrate("Formularz, podpis, pięćdziesiąt euro w kopercie. Marek klepie Kubę po plecach tak, że Guinness ląduje na folderze.")
                .Pl("marek", "Witaj w klubie właścicieli! Za rok sprzedasz z zyskiem i kupisz dwa.")
                .Think("Jestem właścicielem pięćdziesięciu euro w mieszkaniu, którego nie ma, w mieście, w którym nigdy nie byłem.")
                .Outcome(money: -50f, homesickness: -5f)
                .Flag("ch4_booked_flat");
            g.Node("fl_think")
                .Pl("marek", "Myśl, myśl. Tylko szybko, bo do piątku wykupią Irlandczycy.")
                .Think("Kant pisał, że oświecenie to wyjście z niedojrzałości. Nie pisał, co zrobić z folderem dewelopera.")
                .Flag("ch4_flat_maybe");
            g.Node("fl_no")
                .Pl("marek", "Twoja strata, filozofie. Za pięć lat pogadamy.")
                .En("declan", "Good man. My da bought land in seventy-nine. Lost it in eighty-three.")
                .Teach("bought", "land", "lost")
                .Think("Declan nie wygląda na kogoś, kto żartuje.")
                .Outcome(integration: 5f)
                .Flag("ch4_no_flat");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 21 - Wednesday, a strong złoty
        // ------------------------------------------------------------------

        private static StoryScript.Graph Zloty()
        {
            var g = new StoryScript.Graph("zl_1");
            g.Node("zl_1")
                .Narrate("Środa. Telefon do domu z komórki: karta za pięć euro to przeszłość, Kuba ma abonament.")
                .Pl("mum", "Kubuś, przelew doszedł, dziękujemy. Tylko jakoś mniej tych złotówek niż kiedyś. Ojciec mówi, że złoty jest mocny. Ja tam nie wiem, czy mocny, ale na mniej starcza.")
                .Think("Dwa lata temu euro było warte cztery pięćdziesiąt. Dziś niecałe cztery. Pracuję tyle samo, a do domu dochodzi mniej. Ekonomia jest jak dublińska pogoda: nikt jej nie wybiera, wszyscy na nią narzekają.")
                .Pl("mum", "A wiesz, że u ojca w zakładzie dali podwyżkę? I że Kowalscy od sąsiadów wrócili z Londynu i otworzyli warsztat? Może byś... no wiesz. Tu też teraz jest praca.")
                .BranchIf("ch3_diploma_up", "zl_diploma")
                .Next("zl_choice");
            g.Node("zl_diploma")
                .Pl("mum", "I ten dyplom dalej wisi na pinezkach? Marek mi mówił. Synku, magister filozofii przy garach...")
                .Think("Mama mówi to bez wyrzutu. Z tym, co po polsku nazywa się \"no ale\".")
                .Next("zl_choice");
            g.Node("zl_choice")
                .Choice("Obiecuję, że przylecę w sierpniu. Bilet do Krakowa kosztuje mniej niż obiad w Temple Bar.", "zl_visit")
                .Choice("Mówię, że Dublin to teraz mój dom.", "zl_home")
                .Choice("Zmieniam temat na pogodę.", "zl_weather");
            g.Node("zl_visit")
                .Pl("mum", "W sierpniu? Kubuś! Ojciec będzie udawał, że mu wszystko jedno. Upiekę sernik.")
                .Outcome(homesickness: -10f)
                .Flag("ch4_promised_visit");
            g.Node("zl_home")
                .Narrate("Po drugiej stronie długa cisza. Potem szelest, jakby mama zakryła słuchawkę dłonią.")
                .Pl("mum", "No... dobrze. Jak ci tam dobrze, to dobrze.")
                .Think("Powiedziałem to pierwszy raz na głos. Nie wiem, czy to prawda. Wiem, że zabolało nas oboje.")
                .Outcome(homesickness: 5f, integration: 5f)
                .Flag("ch4_dublin_home");
            g.Node("zl_weather")
                .Pl("mum", "U was pada? U nas też. Widzisz, nie ma różnicy. To wracaj.")
                .Think("Mama wygrywa każdą rozmowę o pogodzie.")
                .Outcome(homesickness: 5f);
            return g;
        }

        // ------------------------------------------------------------------
        // Day 22 - Thursday, Cathal Brugha Street
        // ------------------------------------------------------------------

        private static StoryScript.Graph School()
        {
            var g = new StoryScript.Graph("sc_1");
            g.Node("sc_1")
                .Narrate("Czwartek. Declan kładzie na desce ulotkę: Dublin Institute of Technology, szkoła kulinarna przy Cathal Brugha Street. Kursy wieczorowe.")
                .En("declan", "Half the chefs in this city went through Cathal Brugha Street. A proper certificate, Kooba. I'll pay half. You pay half, so you'll turn up.")
                .Teach("certificate", "half", "pay", "proper")
                .Think("Połowa wpisowego to osiemdziesiąt euro. Dwa wieczory w tygodniu przez rok, po dwunastu godzinach w kuchni.")
                .BranchIf("ch3_stays_sink", "sc_sink")
                .Next("sc_choice");
            g.Node("sc_sink")
                .En("declan", "Even for a man who likes his sink. A cert is a cert.")
                .Next("sc_choice");
            g.Node("sc_choice")
                .Choice("Zapisuję się. Osiemdziesiąt euro.", "sc_yes")
                .Choice("Dziękuję, ale nie teraz.", "sc_no");
            g.Node("sc_yes")
                .Narrate("Formularz po angielsku, w trzech egzemplarzach. Kuba wypełnia go sam, bez Agnieszki. W rubryce \"Previous education\" wpisuje: Master of Arts, Philosophy.")
                .Think("Pierwszy raz od dwóch lat ktoś pyta o mój dyplom. W formularzu na kurs gotowania.")
                .Outcome(money: -80f, energy: -5f)
                .Flag("ch4_cookery_school");
            g.Node("sc_no")
                .En("declan", "Your call. The offer stands till September.")
                .Teach("call", "offer", "stands")
                .Think("Dwanaście godzin w kuchni i jeszcze szkoła. Może za rok. Za rok zawsze jest więcej czasu. Przynajmniej teraz tak się wydaje.")
                .Outcome(energy: 5f)
                .Flag("ch4_school_later");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 23 - Friday, the second restaurant opens
        // ------------------------------------------------------------------

        private static StoryScript.Graph Opening()
        {
            var g = new StoryScript.Graph("op_1");
            g.Node("op_1")
                .Narrate("Piątek. Declan otwiera drugą restaurację, nad Liffey, w nowym szklanym budynku z widokiem na dźwigi. Na otwarciu pół miasta: deweloperzy, bankierzy, ktoś z telewizji.")
                .En("gavin", "Look at them. Champagne at four hundred a bottle, and not one of them ever washed a pot.")
                .Teach("champagne", "bottle", "pot")
                .Think("Gavin jest szefem kuchni w nowym miejscu. Ma własny nóż z Japonii i te same niebieskie plastry co wszyscy.")
                .Narrate("Koło północy deweloper w różowej koszuli wchodzi do kuchni z plikiem banknotów.")
                .En("gavin", "Your man just tipped the kitchen five hundred. Five hundred! Go way outta that. He's a gaff in Dalkey with a helicopter pad, they say.")
                .Teach("tipped", "five", "hundred", "helicopter")
                .Slang("slang_go_way", "slang_gaff")
                .Think("Pięćset euro na dwanaście osób. Czterdzieści dla mnie. Dwa lata temu tyle zarabiałem przez cały dzień.")
                .Outcome(money: 40f)
                .BranchIf("ch3_helped_gavin", "op_owed")
                .Next("op_offer");
            g.Node("op_owed")
                .En("gavin", "Kooba. You saved my hand and my lamb that night with the critic. I don't forget.")
                .Teach("saved", "forget")
                .Next("op_offer");
            g.Node("op_offer")
                .En("gavin", "Come with me to the new place. Better money, glass walls, the whole city watching. What do you say?")
                .Teach("better", "walls", "watching", "say")
                .Think("Szklane ściany. Wszyscy będą widzieć, jak pracujemy. Declan mówi, że w takich kuchniach kucharze zaczynają gotować dla widowni, a nie dla gości.")
                .Choice("Idę z Gavinem do nowego miejsca.", "op_go")
                .Choice("Zostaję u Declana.", "op_stay");
            g.Node("op_go")
                .En("gavin", "Deadly. Monday. Bring your plasters.")
                .Think("Declan ściska mi rękę tak, jak ściska się rękę synowi, który wyjeżdża. Mocno i krótko.")
                .Outcome(integration: 5f)
                .Flag("ch4_with_gavin");
            g.Node("op_stay")
                .En("gavin", "Loyal. Declan's lucky. ...Don't tell him I said that.")
                .Teach("loyal", "lucky")
                .Think("Zostaję tam, gdzie przyjęli mnie z dwudziestoma euro w kieszeni. Może to sentyment. Może rozsądek. W Dublinie czasem to jedno i to samo.")
                .Flag("ch4_with_declan");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 24 - Saturday, cranes over the Liffey
        // ------------------------------------------------------------------

        private static StoryScript.Graph Cranes()
        {
            var g = new StoryScript.Graph("cn_1");
            g.Node("cn_1")
                .Narrate("Sobota rano. Kuba idzie do pracy wzdłuż Liffey. Nad miastem tyle dźwigów, że ktoś w radiu żartował, że to narodowy ptak Irlandii.")
                .BranchIf("ch3_tunde_friend", "cn_tunde")
                .Next("cn_alone");
            g.Node("cn_tunde")
                .Narrate("Tunde idzie obok. Dwa lata temu stłukł stos talerzy na brunchu. Dziś studiuje wieczorowo księgowość i pisze do Lagos maile o irlandzkich podatkach.")
                .En("tunde", "My uncle says Nigeria will be rich one day, like here. I tell him: here was poor too. Fifty years ago they were all on the boats.")
                .Teach("uncle", "rich", "poor", "boats")
                .Think("Tunde ma rację. Każdy kiedyś od tego zaczynał. Irlandczycy też.")
                .Next("cn_evening");
            g.Node("cn_alone")
                .Think("Liczę dźwigi do dwudziestu i przestaję. Dwa lata temu myłem talerze ludziom, którzy budowali to miasto. Dziś myję talerze ludziom, którzy je sprzedają.")
                .Next("cn_evening");
            g.Node("cn_evening")
                .Narrate("Wieczorem, po zamknięciu. Declan nalewa dwie whiskey i jedną przesuwa po stali do Kuby.")
                .En("declan", "Enjoy it, Kooba. All of it. My father used to say nothing in this country ever lasts, except the rain and the begrudgers.")
                .Teach("enjoy", "lasts", "except", "rain")
                .Slang("slang_begrudgers")
                .BranchIf("ch4_booked_flat", "cn_flat")
                .Next("cn_last");
            g.Node("cn_flat")
                .Think("W portfelu potwierdzenie rezerwacji z Navan. Pięćdziesiąt euro w murach, których jeszcze nie ma. Declan mówi \"nic nie trwa\", a ja myślę o kredycie na trzydzieści lat.")
                .Next("cn_last");
            g.Node("cn_last")
                .Think("Wypijam. Pali jak Żubrówka Tomka. Za oknem dźwig obraca się powoli, jakby sprawdzał, czy wszyscy są jeszcze na miejscu.")
                .Outcome(homesickness: -5f)
                .Narrate("Koniec Rozdziału 4: Celtycki Tygrys.");
            return g;
        }
    }
}
