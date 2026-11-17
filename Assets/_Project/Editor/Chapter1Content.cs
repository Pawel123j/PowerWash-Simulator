using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Zmywak.Core.Data;
using Zmywak.Narrative.Dialogue;
using Zmywak.Narrative.Progress;
using Zmywak.Narrative.Story;

namespace Zmywak.EditorTools
{
    /// <summary>
    /// Chapter 1, "Przyjazd" (May 2004): six scenes played at home, one per day. Kuba lands in Dublin to a
    /// construction job that does not exist, ends up at Declan's sink in Temple Bar, shares a house with
    /// eight other people, and gets stuck in the PPS - bank - address loop until someone (or something)
    /// breaks it. English lines are blurred where Kuba does not know the words yet; each scene teaches a few.
    /// </summary>
    internal static class Chapter1Content
    {
        public const string ChapterAssetName = "Chapter_ch1.asset";

        public static StoryChapter Create(string folder)
        {
            var beats = new List<StoryBeat>
            {
                StoryScript.Beat(NarrativeProgressController.ArrivalBeatId, 1, StoryScript.SaveGraph(folder, "Ch1_1_Arrival", Arrival())),
                StoryScript.Beat("ch1_house", 2, StoryScript.SaveGraph(folder, "Ch1_2_House", House())),
                StoryScript.Beat("ch1_pps_office", 3, StoryScript.SaveGraph(folder, "Ch1_3_PpsOffice", PpsOffice())),
                StoryScript.Beat("ch1_bank", 4, StoryScript.SaveGraph(folder, "Ch1_4_Bank", Bank())),
                StoryScript.Beat("ch1_way_out", 5, StoryScript.SaveGraph(folder, "Ch1_5_WayOut", WayOut())),
                StoryScript.Beat("ch1_number", 6, StoryScript.SaveGraph(folder, "Ch1_6_Number", Number()), endsChapter: true)
            };

            StoryChapter chapter = VerticalSliceSetup.CreateOrLoad<StoryChapter>($"{folder}/{ChapterAssetName}");
            chapter.SetContent("ch1", "Rozdział 1: Przyjazd", beats);
            EditorUtility.SetDirty(chapter);
            return chapter;
        }

        // ------------------------------------------------------------------
        // Day 1 - Dublin Airport, the agency, the hostel, Declan
        // ------------------------------------------------------------------

        private static StoryScript.Graph Arrival()
        {
            var g = new StoryScript.Graph("arrival_1");
            g.Node("arrival_1")
                .Narrate("1 maja 2004. Od dziś Polska jest w Unii Europejskiej. Lotnisko w Dublinie, hala przylotów.")
                .Think("Pan z agencji \"Emerald Recruitment\" miał czekać z kartką \"KUBA - BUDOWA\". Czekam drugą godzinę. Kartki nie ma. Pana też nie.")
                .Think("Dzwonię na numer z ulotki. Po czterech sygnałach ktoś odbiera.")
                .Next("arrival_2");
            g.Node("arrival_2")
                .En("agent", "Emerald Recruitment, Gerry speaking. Ah, the Polish lad! Small problem, the job is gone. But for fifty euro I can get you on a site next week.")
                .Teach("job", "problem", "euro", "week")
                .Think("Fifty euro. Mam dwadzieścia. I bilet w jedną stronę.")
                .Choice("Oddaję ostatnie dwadzieścia euro. A nuż.", "arrival_pay")
                .Choice("Rozłączam się.", "arrival_hang_up");
            g.Node("arrival_pay")
                .En("agent", "Twenty? Grand, grand. I'll ring you, so.")
                .Think("Nie zadzwoni. Wiem to, zanim skończy mówić.")
                .Outcome(money: -20f, homesickness: 5f)
                .Flag("ch1_paid_agency")
                .Slang("slang_so")
                .Next("arrival_hostel");
            g.Node("arrival_hang_up")
                .Think("Rozłączam się. Pięć lat filozofii nauczyło mnie wątpić. Pierwszy raz do czegoś się to przydało.")
                .Flag("ch1_refused_agency")
                .Next("arrival_hostel");
            g.Node("arrival_hostel")
                .Narrate("Hostel przy O'Connell Street. Łóżko piętrowe, osiem osób w pokoju. Jedna z nich mówi po polsku.")
                .Pl("marek", "Emerald? Mnie też wzięli na Emerald. Budowa, co? Słuchaj, w Temple Bar szukają chłopaka na zmywak. Płacą w piątek.")
                .Think("Zmywak. Pięć lat filozofii, magisterka o Camusie. No cóż, Syzyf też pracował fizycznie.")
                .Pl("marek", "Jutro o siódmej. Pytaj o Declana. I się nie spóźnij, on krzyczy.")
                .Next("arrival_kitchen");
            g.Node("arrival_kitchen")
                .Think("Restauracja w Temple Bar. Kuchnia pachnie smażonym boczkiem i strachem.")
                .En("declan", "You're the new fella, yeah? Grand. Name's Declan, I run this kitchen.")
                .Teach("grand", "kitchen")
                .Next("arrival_grand");
            g.Node("arrival_grand")
                .Think("Zrozumiałem może połowę. Ale \"grand\" mówi chyba co drugie zdanie - musi znaczyć coś dobrego.")
                .Slang("slang_grand")
                .Next("arrival_sink");
            g.Node("arrival_sink")
                .En("declan", "Right, plates first. Everyone starts at the sink. I did too, back in Boston.")
                .Teach("plates", "sink", "started", "boston")
                .Next("arrival_questions");
            g.Node("arrival_questions")
                .En("declan", "Any questions?")
                .Choice("Kiwam głową.", "arrival_nod")
                .Choice("Pytam, czy może mówić wolniej.", "arrival_slower");
            g.Node("arrival_nod")
                .En("declan", "Good man. We all started here, remember that.")
                .Teach("good", "man", "remember")
                .Next("arrival_end");
            g.Node("arrival_slower")
                .En("declan", "Ha! Fair play for asking. We all started here, remember that.")
                .Teach("fair", "asking", "remember")
                .Slang("slang_fair_play")
                .Outcome(integration: 5f)
                .Next("arrival_end");
            g.Node("arrival_end")
                .Think("\"Każdy kiedyś od tego zaczynał.\" Może i tak. Na razie liczy się tylko Góra brudnych naczyń.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 2 - the house of nine
        // ------------------------------------------------------------------

        private static StoryScript.Graph House()
        {
            var g = new StoryScript.Graph("house_1");
            g.Node("house_1")
                .Narrate("Marek załatwił pokój. Właściwie: miejsce. Dom w Phibsborough, dwa piętra, jedna łazienka, dziewięć osób.")
                .Pl("marek", "To jest Tomek, śpi po nocce, nie budzić. To Agnieszka, studiuje angielski. Na górze Darius i jego brat, z Kowna. Reszta zmienia się co tydzień.")
                .Pl("agnieszka", "Cześć! Jak chcesz, mogę cię wieczorami uczyć słówek. W zamian zmywasz po mnie kubki. Żartuję. Trochę.")
                .En("darius", "What's the craic, man? Welcome to the madhouse.")
                .Teach("welcome", "man")
                .Slang("slang_craic")
                .Next("house_landlady");
            g.Node("house_landlady")
                .En("byrne", "You're the new one? Rent is on Friday. Cash. And no new names on the lease, love, I don't want trouble.")
                .Teach("rent", "friday", "cash", "new", "names")
                .Think("Pytam łamanym angielskim, czy mimo wszystko wpisze mnie do umowy.")
                .En("byrne", "Put you on the lease? I will, yeah.")
                .Think("Powiedziała \"tak\". Marek parska śmiechem. Okazuje się, że w Irlandii \"I will, yeah\" znaczy \"po moim trupie\".")
                .Slang("slang_i_will_yeah")
                .Flag("ch1_no_lease")
                .Next("house_bed");
            g.Node("house_bed")
                .Think("Wolnego łóżka nie ma. Jest kanapa w salonie albo pół materaca u Tomka - za dopłatą dziesięć euro.")
                .Choice("Śpię na kanapie w salonie.", "house_couch")
                .Choice("Dopłacam dziesięć euro za materac.", "house_mattress");
            g.Node("house_couch")
                .Think("Salon to także kuchnia, korytarz i kino. O drugiej w nocy ktoś smaży jajecznicę.")
                .Outcome(energy: -10f)
                .Flag("ch1_couch")
                .Next("house_end");
            g.Node("house_mattress")
                .Think("Tomek chrapie jak betoniarka na wolnych obrotach. Ale to przynajmniej materac.")
                .Outcome(money: -10f, energy: 5f)
                .Flag("ch1_mattress")
                .Next("house_end");
            g.Node("house_end")
                .Pl("agnieszka", "I tak w tym domu nikt nie śpi. Witaj w Dublinie!")
                .Think("Dziewięć osób, jedna łazienka i ani jednego nazwiska na umowie. Coś mi mówi, że to się jeszcze zemści.")
                .Outcome(integration: 5f);
            return g;
        }

        // ------------------------------------------------------------------
        // Day 3 - the PPS office
        // ------------------------------------------------------------------

        private static StoryScript.Graph PpsOffice()
        {
            var g = new StoryScript.Graph("pps_1");
            g.Node("pps_1")
                .Narrate("Urząd, okienko od numerów PPS. Kolejka od drzwi. Numerek 247, na tablicy 112.")
                .Think("Marek tłumaczy: bez numeru PPS nie ma podatków, bez podatków nie ma legalnej pracy, a bez legalnej pracy Declan płaci pod stołem i w każdej chwili może przestać.")
                .Next("pps_clerk");
            g.Node("pps_clerk")
                .En("clerk", "PPS number? Grand. I'll need your passport and a proof of address. A bill or a bank statement with your name on it.")
                .Teach("number", "passport", "address", "bill", "bank", "name")
                .Think("Rachunek z moim nazwiskiem. W domu, w którym moje nazwisko nie istnieje.")
                .En("clerk", "No address, no PPS, I'm afraid. Next!")
                .Teach("afraid", "next")
                .Next("pps_end");
            g.Node("pps_end")
                .Think("Dobrze. Rachunku nie mam, ale bank może wydać wyciąg. Jutro bank.")
                .Outcome(energy: -5f);
            return g;
        }

        // ------------------------------------------------------------------
        // Day 4 - the bank, and the loop closes
        // ------------------------------------------------------------------

        private static StoryScript.Graph Bank()
        {
            var g = new StoryScript.Graph("bank_1");
            g.Node("bank_1")
                .Narrate("Bank przy College Green. Marmury, cisza i pan w krawacie, który uśmiecha się jak ktoś, kto zaraz odmówi.")
                .En("banker", "An account? Certainly. I just need your PPS number and a proof of address.")
                .Teach("account", "certainly", "need")
                .Think("Na PPS potrzebuję adresu. Adres, czyli wyciąg, daje konto. Na konto potrzebuję PPS.")
                .En("banker", "Without them, I'm afraid there is nothing I can do.")
                .Think("Pętla. Domknięta tak porządnie, że Kafka by się wzruszył.")
                .Choice("Proszę o rozmowę z kierownikiem.", "bank_manager")
                .Choice("Wychodzę, zanim powiem coś głupiego.", "bank_leave");
            g.Node("bank_manager")
                .En("banker", "The manager will tell you the same, sir. Rules are rules.")
                .Think("Kierownik powiedziałby to samo, tylko dłużej.")
                .Next("bank_sisyphus");
            g.Node("bank_leave")
                .Think("Wychodzę. Na schodach gołąb patrzy na mnie jak na kogoś bez numeru PPS.")
                .Next("bank_sisyphus");
            g.Node("bank_sisyphus")
                .Think("Syzyf przynajmniej wiedział, gdzie jest jego góra. Camus kazał wyobrażać go sobie szczęśliwym. Spróbujmy: jestem szczęśliwy. Nie działa.")
                .Think("Ale Camus pisał też, że samo wspinanie się wystarczy. Declan wspinał się w Bostonie. Ciekawe, kto mu wtedy pomógł.")
                .Flag("ch1_sisyphus")
                .Outcome(homesickness: 5f)
                .Next("bank_end");
            g.Node("bank_end")
                .Think("Wieczorem do pracy. Talerze przynajmniej nie wymagają zaświadczeń.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 5 - two ways out
        // ------------------------------------------------------------------

        private static StoryScript.Graph WayOut()
        {
            var g = new StoryScript.Graph("way_1");
            g.Node("way_1")
                .Narrate("Po zmianie. Kuba siedzi na skrzynce po pomidorach za restauracją i gapi się na formularz PPS.")
                .Pl("marek", "Słuchaj, znam gościa. Za dwadzieścia euro zrobi ci rachunek za prąd na twoje nazwisko. Wygląda jak prawdziwy, nikt nie sprawdza.")
                .En("declan", "What's that face? Ah, the PPS. Everyone has that problem. Give me the form.")
                .Teach("face", "form")
                .En("declan", "I'll write you a letter. You work here, you get your letters here. The office knows me.")
                .Teach("letter", "work", "office", "knows")
                .Think("Dwie drogi. Gość Marka jest szybki. Declan jest... Declanem.")
                .Choice("Biorę list od Declana.", "way_letter")
                .Choice("Płacę gościowi Marka za rachunek.", "way_fake_bill")
                .Choice("(Myśl o Syzyfie) Pytam Declana, kto jemu pomógł w Bostonie.", "way_boston", requiredFlag: "ch1_sisyphus");
            g.Node("way_letter")
                .En("declan", "Good man. Now go, table six wants plates, not paperwork.")
                .Outcome(integration: 10f)
                .Flag("ch1_declan_letter")
                .Next("way_end");
            g.Node("way_fake_bill")
                .Pl("marek", "Mądry wybór. Jutro będziesz miał najładniejszy rachunek w Dublinie.")
                .Think("Czuję się, jakbym podpisywał coś, czego nie przeczytałem.")
                .Outcome(money: -20f)
                .Flag("ch1_fake_bill")
                .Next("way_end");
            g.Node("way_boston")
                .En("declan", "Boston, eighty-six. Same story, different stamps. An old cook wrote me a letter. So now I write letters, son. That's how it works.")
                .Teach("story", "different", "stamps", "old", "cook", "son", "works")
                .Think("Każdy kiedyś od tego zaczynał. Nawet Declan. Zwłaszcza Declan.")
                .Outcome(integration: 15f, homesickness: -5f)
                .Flag("ch1_declan_letter", "ch1_declan_story")
                .Next("way_end");
            g.Node("way_end")
                .Think("Jutro znowu urząd. Numerek 247, podejście drugie.");
            return g;
        }

        // ------------------------------------------------------------------
        // Day 6 - the number, the account, a jar from home
        // ------------------------------------------------------------------

        private static StoryScript.Graph Number()
        {
            var g = new StoryScript.Graph("number_1");
            g.Node("number_1")
                .Narrate("Ta sama kolejka, ta sama urzędniczka, ten sam numerek. Inny papier w ręku.")
                .BranchIf("ch1_fake_bill", "number_fake")
                .Next("number_letter");
            g.Node("number_letter")
                .En("clerk", "A letter from Declan O'Sullivan? He sends me a new one every month. Grand, here's your number.")
                .Teach("letter", "month", "here's", "your")
                .Think("Siedem cyfr i litera. Żadna liczba jeszcze mnie tak nie wzruszyła.")
                .Next("number_loop_breaks");
            g.Node("number_fake")
                .En("clerk", "An electricity bill... dated next month? Hm. Well. It has your name on it. Here's your number.")
                .Teach("electricity", "month", "here's", "your")
                .Think("Rachunek z datą z przyszłego miesiąca. Gość Marka ma fantazję. Urzędniczka ma litość. Albo kolejkę do trzysetki.")
                .Flag("ch1_clerk_noticed")
                .Next("number_loop_breaks");
            g.Node("number_loop_breaks")
                .Narrate("Z numerem PPS bank otwiera konto w dziesięć minut. Z kontem pani Byrne wzdycha i wpisuje Kubę do umowy. Pętla pęka.")
                .Next("number_mum");
            g.Node("number_mum")
                .Pl("mum", "Kubuś? Wysłałam paczkę. Słoik bigosu, słoik ogórków i skarpetki. Jesz coś ciepłego?")
                .Think("Bigos w kuchni na dziewięć osób. Znika w siedem minut. Mój rekord zmywaka to wciąż dłużej.")
                .Outcome(energy: 15f, homesickness: -10f)
                .Next("number_end");
            g.Node("number_end")
                .Think("Pierwszy tydzień w Dublinie: oszukany, bez łóżka, za to z numerem PPS i słoikiem bigosu. Brudne talerze. Czyste marzenia.")
                .Narrate("Koniec Rozdziału 1: Przyjazd.");
            return g;
        }
    }
}
