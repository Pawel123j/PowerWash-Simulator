using System;
using System.Collections.Generic;
using UnityEditor;
using Zmywak.Narrative.Story;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Chapter 5, "Krach" (29 September - 4 October 2008): the calendar jumps to the week the Irish boom broke - two
    /// weeks after Lehman Brothers, the night the government guaranteed every euro in its banks. The glass restaurant
    /// closes; the flats in Navan were never built; Tomek takes the bus home to build stadiums for Euro 2012; Declan
    /// has a list with no names on it yet; Kuba's father rings for the first time in four years; and Kuba has to
    /// choose: stay, go home, or - with Gavin - go further still. Chapter 4's choices come back here.
    /// </summary>
    internal static class Chapter5Content
    {
        public const string ChapterAssetName = "Chapter_ch5.asset";

        /// <summary>The Monday before the bank guarantee.</summary>
        public static readonly DateTime StartDate = new DateTime(2008, 9, 29);

        public static StoryChapter Create(string folder)
        {
            var beats = new List<StoryBeat>
            {
                StoryScript.Beat("ch5_empty_room", 25, StoryScript.SaveGraph(folder, "Ch5_1_EmptyRoom", EmptyRoom(), fluent: true)),
                StoryScript.Beat("ch5_guarantee", 26, StoryScript.SaveGraph(folder, "Ch5_2_Guarantee", Guarantee(), fluent: true)),
                StoryScript.Beat("ch5_tomek", 27, StoryScript.SaveGraph(folder, "Ch5_3_Tomek", Tomek(), fluent: true)),
                StoryScript.Beat("ch5_list", 28, StoryScript.SaveGraph(folder, "Ch5_4_List", Cuts(), fluent: true)),
                StoryScript.Beat("ch5_dad", 29, StoryScript.SaveGraph(folder, "Ch5_5_Dad", Dad(), fluent: true)),
                StoryScript.Beat("ch5_choice", 30, StoryScript.SaveGraph(folder, "Ch5_6_Choice", Decision(), fluent: true), endsChapter: true)
            };

            StoryChapter chapter = VerticalSliceSetup.CreateOrLoad<StoryChapter>($"{folder}/{ChapterAssetName}");
            chapter.SetContent("ch5", "Rozdział 5: Krach", beats, StartDate);
            EditorUtility.SetDirty(chapter);
            return chapter;
        }

        // ------------------------------------------------------------------
        // Day 25 - Monday 29 September 2008, an empty room
        // ------------------------------------------------------------------

        private static StoryScript.Graph EmptyRoom()
        {
            var g = new StoryScript.Graph("er_1");
            g.Node("er_1")
                .Narrate("29 września 2008. Dwa tygodnie temu w Ameryce upadł bank, którego nazwy nikt w kuchni nie umiał wymówić. Dziś radio mówi już tylko o irlandzkich bankach i o tym, jak lecą ich akcje.")
                .BranchIf("ch4_with_gavin", "er_glass")
                .Next("er_old");
            g.Node("er_glass")
                .Narrate("Szklana restauracja nad Liffey, poniedziałek, ósma wieczorem. Dwa stoliki zajęte. Deweloper w różowej koszuli nie zapłacił rachunku z sierpnia: trzy tysiące euro.")
                .En("gavin", "Glass walls, Kooba. The whole city can see there's nobody inside. The bank rang. We close on Friday. Half my lads will be on the dole by Christmas.")
                .Teach("glass", "nobody", "inside", "close", "friday", "christmas")
                .Slang("slang_dole")
                .Think("Szklane ściany. Myślałem, że są dla widowni. Teraz widownia patrzy na pustą salę.")
                .Flag("ch5_glass_closed")
                .Next("er_end");
            g.Node("er_old")
                .Narrate("Stara restauracja Declana, poniedziałek, ósma wieczorem. Pół sali pustej. Szklana, nad Liffey, stoi zamknięta od tygodnia, z kartką od banku na drzwiach.")
                .En("declan", "It's fierce quiet. Remember what I told you? Nothing in this country lasts. I hate being right, Kooba.")
                .Teach("quiet", "remember", "hate", "right")
                .Slang("slang_fierce")
                .Next("er_end");
            g.Node("er_end")
                .Think("W kuchni cicho. Nawet zmywarka szumi jakby ciszej, żeby nie zwracać na siebie uwagi.")
                .Choice("Zostaję po zamknięciu i myję wszystko jeszcze raz, za darmo.", "er_stay")
                .Choice("Idę do domu wcześniej. Nie ma co robić.", "er_home");
            g.Node("er_stay")
                .Think("Myję czyste talerze. Głupie? Może. Ale dopóki coś myję, ta kuchnia jeszcze działa.")
                .Outcome(energy: -10f, integration: 5f)
                .Flag("ch5_stayed_late");
            g.Node("er_home")
                .Think("Pierwszy raz od czterech lat wychodzę przed północą. Dublin o dziesiątej wieczorem wygląda jak miasto, w którym ktoś wyłączył muzykę.")
                .Outcome(energy: 5f, homesickness: 5f);
            return g;
        }

        // ------------------------------------------------------------------
        // Day 26 - Tuesday 30 September, the guarantee
        // ------------------------------------------------------------------

        private static StoryScript.Graph Guarantee()
        {
            var g = new StoryScript.Graph("gu_1");
            g.Node("gu_1")
                .Narrate("Wtorek rano. Radio w kuchni: w nocy rząd zagwarantował wszystkie depozyty w irlandzkich bankach. Wszystkie. Ktoś przy zlewie pyta, ile to jest \"wszystkie\". Nikt nie wie.")
                .En("declan", "Brutal. The whole country's guaranteed, and nobody knows what it means.")
                .Teach("whole", "guaranteed", "means")
                .Slang("slang_brutal")
                .Narrate("Po południu przychodzi Marek. Bez teczki, bez folderów, w tej samej kurtce co cztery lata temu.")
                .Pl("marek", "Navan. Deweloper zbankrutował. Fundamenty i dziura w ziemi, Kuba. Wpłaciłem trzydzieści tysięcy. Kredyt na samochód i oszczędności mamy.")
                .BranchIf("ch4_booked_flat", "gu_booked")
                .Next("gu_route");
            g.Node("gu_route") // no lines: Kuba either thought too long, or said no
                .BranchIf("ch4_no_flat", "gu_refused")
                .Next("gu_waited");
            g.Node("gu_booked")
                .Think("Moja rezerwacja: pięćdziesiąt euro. Śmieszne pieniądze przy trzydziestu tysiącach Marka. Ale to było moje pięćdziesiąt euro w murach, których nigdy nie było.")
                .Outcome(homesickness: 5f)
                .Next("gu_ask");
            g.Node("gu_refused")
                .Think("Odmówiłem dwa lata temu. Declan miał rację, jego ojciec miał rację, tylko Marek nie. Nie czuję satysfakcji. Czuję zimno.")
                .Next("gu_ask");
            g.Node("gu_waited")
                .Think("Powiedziałem, że muszę to przemyśleć. Myślałem tak długo, że zdążyło upaść. Pierwszy raz filozofia mnie uratowała.")
                .Next("gu_ask");
            g.Node("gu_ask")
                .Pl("marek", "Kuba... pożycz mi coś. Pięćdziesiąt. Na czynsz. Oddam, jak... no, oddam.")
                .Choice("Pożyczam mu pięćdziesiąt euro.", "gu_lend")
                .Choice("Mówię, że sam nie mam.", "gu_no");
            g.Node("gu_lend")
                .Pl("marek", "Dzięki. Ty jeden nigdy nie miałeś \"notions\", filozofie.")
                .Outcome(money: -50f, integration: 5f)
                .Flag("ch5_lent_marek");
            g.Node("gu_no")
                .Pl("marek", "Jasne. Teraz każdy liczy.")
                .Think("Dwa lata temu Marek liczył do czterystu tysięcy. Dziś do pięćdziesięciu.")
                .Outcome(homesickness: 5f);
            return g;
        }

        // ------------------------------------------------------------------
        // Day 27 - Wednesday 1 October, Tomek goes home
        // ------------------------------------------------------------------

        private static StoryScript.Graph Tomek()
        {
            var g = new StoryScript.Graph("tk_1");
            g.Node("tk_1")
                .Narrate("Środa. Budowy w całym kraju stają jedna po drugiej. Tomek dzwoni o szóstej rano.")
                .Pl("tomek", "Zamknęli Navan. I Droghedę. I wszystko. Wracam do Kielc, Kuba. W Polsce budują stadiony na Euro dwa tysiące dwanaście, szukają ludzi. Autobus w sobotę z Busárasu.")
                .Think("Tomek przyjechał tu po pracę na budowie. Wyjeżdża z tego samego powodu, tylko w drugą stronę.")
                .BranchIf("ch3_taught_tomek", "tk_rack")
                .Next("tk_choice");
            g.Node("tk_rack")
                .Pl("tomek", "Pamiętasz, jak mnie uczyłeś układać kosz? Kafelek przy kafelku. W Kielcach opowiem, że zmywałem w najlepszej kuchni w Dublinie.")
                .Next("tk_choice");
            g.Node("tk_choice")
                .Choice("Daję mu trzydzieści euro na drogę.", "tk_money")
                .Choice("Mówię, żeby został. Jeszcze się odbije.", "tk_stay")
                .Choice("Pytam, czy w Polsce znajdzie się praca dla kucharza.", "tk_ask");
            g.Node("tk_money")
                .Pl("tomek", "Kuba, ty i te twoje pieniądze na drogę. ...Dzięki. Oddam w Kielcach. Piwem.")
                .Outcome(money: -30f, homesickness: -5f)
                .Flag("ch5_saw_tomek_off");
            g.Node("tk_stay")
                .Pl("tomek", "Tu się nic nie odbije przez pięć lat. Mam trzydzieści cztery lata, Kuba. Nie mam pięciu lat do stracenia.")
                .Think("Nie mam odpowiedzi. Mam tylko zdanie, które sam chciałbym usłyszeć.")
                .Outcome(homesickness: 5f);
            g.Node("tk_ask")
                .Pl("tomek", "Dla kucharza? W Krakowie stawiają hotele na Euro. Jak masz papier, wezmą cię od ręki.")
                .Think("Papier. Mój dyplom z filozofii wisi albo leży. O innym papierze nie pomyślałem.")
                .Flag("ch5_asked_about_poland")
                .BranchIf("ch4_cookery_school", "tk_cert");
            g.Node("tk_cert")
                .Think("Mam drugi papier. Certyfikat z Cathal Brugha Street: rok wieczorów po dwunastu godzinach w kuchni. Pierwszy raz myślę o nim po polsku.")
                .Flag("ch5_krakow_job");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 28 - Thursday 2 October, a list with no names yet
        // ------------------------------------------------------------------

        private static StoryScript.Graph Cuts()
        {
            var g = new StoryScript.Graph("li_1");
            g.Node("li_1")
                .Narrate("Czwartek. Declan zbiera kuchnię przy wydawce. W ręku kartka.")
                .En("declan", "I won't dress it up. We're down forty percent. I can keep this kitchen open if two people go. Or if everyone takes fewer hours.")
                .Teach("dress", "forty", "percent", "fewer")
                .Think("Dwie osoby. Na kartce nie ma nazwisk. Jeszcze.")
                .BranchIf("ch4_mentor_bartek", "li_bartek")
                .Next("li_choice");
            g.Node("li_bartek")
                .Narrate("Bartek stoi przy zlewie, przy którym Kuba uczył go zeskrobywać resztki. Patrzy w podłogę.")
                .Pl("bartek", "Ja jestem ostatni, to ja wylecę pierwszy. Taka jest zasada, prawda?")
                .Next("li_choice");
            g.Node("li_choice")
                .Choice("Oddaję swoje soboty, żeby nikt nie musiał odchodzić.", "li_share")
                .Choice("Milczę. Każdy ma swoje rachunki.", "li_quiet")
                .Choice("Mówię Declanowi, że Bartek jest lepszy, niż ja byłem na początku.", "li_vouch", requiredFlag: "ch4_mentor_bartek");
            g.Node("li_share")
                .Narrate("Po Kubie rękę podnosi drugi kucharz, potem kelnerka. Declan skreśla sobie pensję na październik.")
                .En("declan", "Ah, sure look. Nobody goes. Not this month.")
                .Teach("month", "goes")
                .Slang("slang_sure_look")
                .Think("Czterdzieści euro mniej w tygodniu. W zamian nikt nie stoi jutro z reklamówką pod drzwiami.")
                .Outcome(money: -40f, energy: 10f, integration: 5f)
                .Flag("ch5_shared_hours");
            g.Node("li_quiet")
                .Narrate("W piątek na grafiku brakuje dwóch nazwisk.")
                .Think("Każdy ma swoje rachunki. Powtarzam to sobie przez całą zmianę. Z każdym razem brzmi gorzej.")
                .Outcome(homesickness: 5f)
                .Flag("ch5_kept_quiet");
            g.Node("li_vouch")
                .En("declan", "Better than you were? Kooba, you broke four plates your first week.")
                .Teach("broke", "first")
                .Think("Trzy. Ale nie poprawiam go.")
                .En("declan", "...Fine. Bartek stays. He's your responsibility. Again.")
                .Outcome(integration: 5f)
                .Flag("ch5_kept_bartek");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 29 - Friday 3 October, Dad rings
        // ------------------------------------------------------------------

        private static StoryScript.Graph Dad()
        {
            var g = new StoryScript.Graph("da_1");
            g.Node("da_1")
                .Narrate("Piątek wieczór. Telefon z Polski. Tym razem dzwoni tata. Tata nie dzwoni nigdy.")
                .Pl("dad", "Kuba. Mama się martwi. W telewizji mówią, że u was kryzys. Euro tanie jak barszcz, złotówka mocna. Kraków szuka kucharzy na Euro dwa tysiące dwanaście.")
                .Think("Tata mówi pełnymi zdaniami. To znaczy, że ćwiczył.")
                .BranchIf("ch4_promised_visit", "da_visit")
                .Next("da_2");
            g.Node("da_visit")
                .Pl("dad", "Jak przyjechałeś wtedy w sierpniu, mama potem tydzień płakała w kuchni. Mówiła, że od cebuli.")
                .Next("da_2");
            g.Node("da_2")
                .Pl("dad", "Nie mówię, żebyś wracał. Mówię, że pokój jest. I półka na ten twój dyplom.")
                .BranchIf("ch4_dublin_home", "da_home")
                .Next("da_choice");
            g.Node("da_home")
                .Think("Dwa lata temu powiedziałem mamie, że Dublin to mój dom. Tata to pamięta. Tata wszystko pamięta i nic nie mówi.")
                .Next("da_choice");
            g.Node("da_choice")
                .Choice("Mówię, że się zastanowię. Naprawdę.", "da_think")
                .Choice("Mówię, że zostaję. Tu jest moje życie.", "da_stay")
                .Choice("Pytam tatę, jak się czuje.", "da_ask");
            g.Node("da_think")
                .Pl("dad", "No. To się zastanów. Tylko nie za długo, bo mama zdąży wszystko poprzestawiać.")
                .Outcome(homesickness: -5f)
                .Flag("ch5_considering");
            g.Node("da_stay")
                .Pl("dad", "Rozumiem. Ja w twoim wieku też nikogo nie słuchałem. Dlatego jestem tu, gdzie jestem.")
                .Think("Nie wiem, czy to pochwała, czy ostrzeżenie. U taty zwykle jedno i drugie.")
                .Outcome(homesickness: 5f)
                .Flag("ch5_told_dad_staying");
            g.Node("da_ask")
                .Pl("dad", "Ja? Dobrze. Starzeję się, ale dobrze. Wracaj albo nie wracaj, ale dzwoń częściej.")
                .Think("Pierwszy raz od czterech lat rozmawiamy o czymś innym niż pieniądze i pogoda.")
                .Outcome(homesickness: -10f)
                .Flag("ch5_asked_dad");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 30 - Saturday 4 October, the choice
        // ------------------------------------------------------------------

        private static StoryScript.Graph Decision()
        {
            var g = new StoryScript.Graph("ch_1");
            g.Node("ch_1")
                .Narrate("Sobota, siódma rano. Busáras. Autobus do Kielc przez Holyhead i pół Europy stoi z włączonym silnikiem.")
                .BranchIf("ch5_saw_tomek_off", "ch_tomek")
                .Next("ch_river");
            g.Node("ch_tomek")
                .Pl("tomek", "Kanapki zjem do Holyhead. Piwo w Kielcach. Trzymam za słowo, Kuba.")
                .Next("ch_river");
            g.Node("ch_river")
                .Narrate("Kuba wraca wzdłuż Liffey. Dźwigi stoją. Nikt ich już nie liczy i nikt nimi nie obraca.")
                .Think("Cztery lata. Zlew, deska, dwie restauracje, jeden krach. I pytanie, które zadają mi tata, mama, Tomek i ta cisza w kuchni: co dalej?")
                .BranchIf("ch4_with_gavin", "ch_gavin")
                .Next("ch_declan");
            g.Node("ch_gavin")
                .En("gavin", "Perth, Kooba. Western Australia. Mines, money, and they're crying out for chefs. I fly in November. Come with me.")
                .Teach("mines", "crying", "fly", "november")
                .Next("ch_decide");
            g.Node("ch_declan")
                .En("declan", "Whatever you decide, Kooba, decide it yourself. Not the banks, not the radio. You.")
                .Teach("decide", "yourself", "banks")
                .Next("ch_decide");
            g.Node("ch_decide")
                .Think("Heraklit: nie wchodzi się dwa razy do tej samej rzeki. Ani do tego samego kraju. Ani do tego samego zlewu.")
                .Choice("Zostaję w Dublinie. Przetrwam to razem z nimi.", "ch_stay")
                .Choice("Wracam do Polski.", "ch_home")
                .Choice("Lecę z Gavinem do Australii.", "ch_perth", requiredFlag: "ch4_with_gavin");
            g.Node("ch_stay")
                .Think("Zostaję. Nie dlatego, że tu łatwiej. Dlatego, że tu jestem kimś, kogo znają z imienia. Prawie.")
                .Outcome(integration: 10f, homesickness: 5f)
                .Flag("ch5_stays")
                .Next("ch_end");
            g.Node("ch_home")
                .Flag("ch5_goes_home")
                .BranchIf("ch5_krakow_job", "ch_krakow")
                .Next("ch_home_plain");
            g.Node("ch_krakow")
                .Think("Kraków. Hotel przy Rynku, zastępca szefa kuchni, z certyfikatem z Cathal Brugha Street. Wyjechałem jako filozof, wracam jako kucharz.")
                .Outcome(homesickness: -20f)
                .Next("ch_end");
            g.Node("ch_home_plain")
                .Think("Wracam. Bez planu. Z dyplomem z filozofii i rękami, które umieją zmywać i kroić. Na początek wystarczy.")
                .Outcome(homesickness: -20f)
                .Next("ch_end");
            g.Node("ch_perth")
                .En("gavin", "Deadly. Bring your plasters. And sun cream.")
                .Teach("cream", "sun")
                .Think("Australia. Jeszcze dalej od domu. Albo jeszcze bliżej siebie. Syzyf nigdy nie mógł zmienić góry. Ja mogę.")
                .Outcome(homesickness: 10f)
                .Flag("ch5_goes_to_perth")
                .Next("ch_end");
            g.Node("ch_end")
                .Narrate("Koniec Rozdziału 5: Krach.");
            return g;
        }
    }
}
