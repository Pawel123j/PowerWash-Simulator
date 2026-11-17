# Gameplay, UX i rekomendacje

Audyt z 13.10.2026. Badany jest bieżący katalog roboczy, także pliki niezatwierdzone w Git, a nie sam HEAD. Ścieżki dowodów są względem katalogu projektu; numery linii dotyczą tej migawki. Oryginalne skrypty, sceny, ustawienia i assety nie były celowo zmieniane. Testy uruchomiono na odrębnej kopii poza repozytorium.

**Granica pewności:** analiza statyczna potwierdza strukturę i konkretne ścieżki wykonania, test automatyczny potwierdza tylko swoje asercje. Brak pomiarów telefonu, audytu słuchowego, ręcznego przejścia wszystkich wariantów fabuły i testów sklepowych oznacza UNKNOWN w tych obszarach. Nie deklarujemy poznania gry w 100%.

## Pętla i motywacja

Faktyczna pętla: wybór slotu → mieszkanie i fabuła → decyzje o życiu/wydatkach → opcjonalny prep → 180-sekundowa zmiana → wynik i wypłata → kolejny dzień, rachunki, relacje i dalsza historia. W kuchni: rozpoznać zabrudzenie → wybrać narzędzie i kolejność działań → miejscowo oczyścić → zapakować kosz → uruchomić maszynę/naprawić awarię → obsłużyć następne naczynie i pilne zlecenia. Alternatywny duży cykl przyjmuje wsad do 40 sztuk i trwa 30 minut czasu rzeczywistego.

Dowody: `Assets/_Project/Scripts/Core/Data/ShiftSchedule.cs:1`, `Assets/_Project/Scripts/Kitchen/Dishes/DishInstance.cs:1`, `Assets/_Project/Scripts/Kitchen/Rack/RackLayout.cs:1`, `Assets/_Project/Scripts/Kitchen/Dishwasher/LongCycleDishwasher.cs:1`, `Assets/_Project/Scripts/Meta/Progress/LifeLedger.cs:1`, `Assets/_Project/Scripts/UI/Screens/StoryBeatTrigger.cs:1`. Szczegółowe połączenia i statusy są w `FEATURE_INVENTORY.md`.

**Ocena projektowa:** siłą koncepcji jest związek namacalnej, powtarzalnej pracy z emigracyjną historią. Zmywanie może jednocześnie dawać krótką satysfakcję sensoryczną i znaczenie ekonomiczne. To hipoteza o atrakcyjności, nie rezultat badania rynku ani dowód przewagi nad innymi grami.

## Reguły a doświadczenie gracza

| Obszar | Fakt z implementacji | Ocena / brak dowodu | Działanie |
|---|---|---|---|
| Cel krótkiej sesji | Ukończyć zmianę, umyć normę, poprawić jakość i wypłatę | Czy gracz rozumie cel zanim dotknie naczynia — UNKNOWN | Pierwsza sesja z 5–8 nowymi osobami, obserwacja bez podpowiedzi |
| Motywacja długa | Pięć rang, odblokowania, słownictwo, relacje i epilog | Proporcja pracy do tekstu może dzielić odbiorców | Ustalić grupę: relaks z historią czy presja zręcznościowa |
| Wygrana | Wynik zmiany S–D, sukces inspekcji i fabularny epilog | Nie ma jednej sportowej wygranej całej gry | Wyjaśnić, że droga życiowa jest wynikiem wyborów, nie wyłącznie punktów |
| Porażka | Stłuczki, zawalenie, niższa płaca, dług i hostel | Konsekwencje zastępują klasyczny game over; uczciwość przy małym budżecie do zbadania | Test scenariusza ubóstwa i możliwości wyjścia z niego |
| Trudność | Narzędzia i systemy odblokowują się w dniach 1–5; pięć archetypów zmian po 180 s, normy 5/8/9/9/11 | Szybka kumulacja nowych reguł może przeciążyć początkującego | Mierzyć czas do pierwszego samodzielnego mycia i najczęstsze pomyłki |
| Narzędzia | Pięć definicji narzędzi, siedem zabrudzeń, osiem typów naczyń; scrape/rinse/scrub | Wiele reguł może być satysfakcjonujących, jeśli przyczyna blokady jest widoczna | Krótki feedback stanu zamiast kolejnego panelu instrukcji |
| Precyzja | Maska przestrzenna, końcowe 5% automatycznie, szkło z inspekcją, fizyczne odkładanie | Dobry fundament manualnego poczucia pracy; ekran/DPI mogą zmienić komfort | Telefon, różne palce i wielkości ekranu; nie oceniać tylko myszą |
| Kosz i naprawa | Układ 24 komórek i rotacja; trzy typy awarii | Wzbogaca rytm, ale niejasny drop/naprawa może go przerwać | Ocena czasu błędnych prób i czytelności hotspotów |
| Nagroda | Stawka bazowa 6 EUR/h, udział czasu pracy, tipy 0,25 EUR/szt. i 1 EUR/pełny kosz; potrącenia | Wynik ma wiele składowych; CODE-003/004 zniekształcają sprawiedliwość | Najpierw poprawić reguły, potem balansować progi |
| Ryzyko ekonomiczne | Rezerwa rachunków, przelew do domu, zakupy, dług; koszty i nagrody fabularne | Restart fabuły może powielić pieniądze; CODE-001 | Atomowość przed jakimkolwiek pomiarem balansu |
| Prep | Runda 40 s, ocena krojenia, do 6 EUR bonusu | Dodatkowa umiejętność może urozmaicić start, ale względna nagroda wymaga kalibracji | Porównać z zarobkiem zmiany i odsetkiem pomijania |
| Długi cykl | Stan i zakończenie według UTC przechowywane w save | Może osłabić motywację ręcznego mycia albo zachęcić do powrotów; skala UNKNOWN | Symulacja dochodu obu strategii, próba zmiany zegara |
| Zakończenie zmiany | Pauza pozwala zakończyć wcześniej; dzień się przesuwa | CODE-004/005/006: skróty progresji i utrata lekcji | Jawny wynik przerwania, osobny od sukcesu |
| Replayability | Wybory, kilka dróg życiowych, losowane naczynia i zmiany specjalne | To podstawa; brak badania chęci powtórnej kampanii | Najpierw trzy kompletne ścieżki i jakościowe różnice zakończeń |

Próg końcowej rangi obejmuje m.in. 800 naczyń, 60 pełnych koszy, 30 czystych zmian, 2 inspekcje, 2 legendarne blachy i Flow 8 (`Career.cs`). Bez danych z pełnych kampanii nie można uczciwie nazwać tego ani dobrym tempem, ani grindem. Dowód matematyczny progów nie jest dowodem przyjemnej długości gry.

## Wprowadzenie, presja i frustracja

Istnieje prowadzony pierwszy talerz, wskazówki etapów i narzędzi, odblokowania oraz pierwsza scenariuszowa awaria. Luka polega na utracie możliwości dokończenia nauki po wczesnym wyjściu, a nie na braku całego tutorialu. CODE-005 i CODE-006 wymagają spójnej reguły: ukończono, pominięto świadomie albo nadal trzeba zaproponować pomoc.

W kodzie są ułatwienia: relaxed, tool assist i auto dishwasher. Rekomendacja: zachować je jako równoprawne preferencje komfortu i jasno określić wpływ na wynik/rekordy. Nie rekomenduję dodawania walki, pojazdów, multiplayera, otwartego miasta ani online rankingu do obecnego zakresu. Największy zwrot ma dopracowanie istniejącej pętli i jej następstw.

## UX, wizualia, wejście i audio

| Obszar | Istniejące rozwiązanie / dowód | Ocena i kryterium kolejnej próby |
|---|---|---|
| Menu i nawigacja | MainMenu → sloty → hub; mapa, warstwy paneli, BackGesture, pauza; świeże MenuScenes/BackGesture/UiPolish PASS | Testy potwierdzają wybrane ścieżki, nie zrozumiałość nazw. Back powinien zamykać najwyższą warstwę, a wyjście ujawniać konsekwencje |
| HUD | Czas, norma, Flow, narzędzia, prośby szefa, awaria, wynik | Wiele komunikatów konkuruje o mały ekran. Test hierarchii przy jednoczesnym zamówieniu, nowym narzędziu i awarii |
| Czytelność | TMP, TextScaler, SafeArea, ustawienia skali i paleta daltonizmu | Brak pomiaru czytelności na najmniejszym obsługiwanym telefonie. Największy font, długi polski tekst i notch muszą pozostawić dostęp do akcji |
| Feedback | Maska brudu, pasek postępu, efekty wody, ruch dłoni, dźwięk i haptyka, animacja wypłaty | Odpowiedź wizualna istnieje; latency i odczucie UNKNOWN. Stan powinien być zrozumiały także przy wyciszeniu i ograniczeniu efektów |
| Wynik / przerwanie | ShiftResultScreen, rozbicie wyniku i powrót do mieszkania | Brak jednego klasycznego ekranu porażki nie jest luką koncepcji. Trzeba spójnie wyjaśniać normę, straty i nieukończoną inspekcję |
| Błędy | Log ostrzega o save failure, UI nie przekazuje wiarygodnie tej informacji | CODE-002 przed publikacją; komunikat ma prowadzić do ponowienia bez utraty pending stanu |
| Rozdzielczość | Mobilny portret, okno Windows 450×800, konfiguracje jakości | Nie potwierdzono pełnej używalności landscape/tabletu; formalnie określić obsługiwany zakres |
| Dotyk / mysz / klawiatura | Input System, wspólna obsługa pointer; skróty i Back/Escape | Mysz w Editorze nie dowodzi multitouch, odporności na focus i działania przy DPI telefonu |
| Kontroler | Brak potwierdzonej kompletnej mapy gamepada i remapowania | UNKNOWN / poza potwierdzonym zakresem. Nie obiecywać wsparcia na stronie produktu bez pełnej ścieżki |
| Dostępność | Skala tekstu, colorblind, haptics, efekty, tilt, relaxed, pomoc narzędzi i automatyczna maszyna | Cenny zakres, ale nie audyt zgodności. Brak pełnej obsługi czytnika ekranu i dowodów dla ograniczeń motorycznych |
| Spójność grafiki | Proceduralna kuchnia i pokój, własne materiały, portrety, ręce, światło i postprocess | Historyczne porównania `Logs/grafika_runda2_kuchnia.png` i `grafika_runda2_pokoj.png` pokazują spójny kierunek; nie są akceptacją aktualnego builda |
| Audio | GameAudio, KitchenAudio, banki WAV, proceduralne dźwięki, radio, suwaki master/music/SFX; brak AudioMixer asset | Testy sprawdzają zasoby i strukturę, nie miks słuchowy. Odsłuchać telefon/słuchawki, overlap awarii/szefa/radia, pauzę i przerwania audio |
| Animacja / VFX | Proceduralna dłoń i UI, ParticleSystem, postprocess; brak AnimatorController i VFX Graph | Nie jest to samo w sobie brak — technika pasuje do zakresu. Ocenić czy efekty nie zasłaniają zabrudzeń i celów dotyku |

Historyczne obrazy obejrzano jako materiał odniesienia. Nie zastępują bieżącej sesji ręcznej. Aktualne materiały z testów, jeśli wygenerowane, i zakres ich inspekcji opisuje `QA_STATUS.md`. Bez odsłuchu nie wydano oceny jakości brzmienia.

## Domknięcie kampanii

Po ostatnim rozdziale StoryBeatTrigger.HandleDialogueEnded używa komunikatu „Ciąg dalszy nastąpi”, ponieważ nie ma następnego chapter, a hub pozwala dalej pracować. Istnieją epilog i odmienne drogi życiowe, lecz nie ma wyraźnego podsumowania całej kampanii. To luka odbioru produktu względem proponowanego 1.0, nie dowód crasha. TASK-014 powinien określić i zweryfikować finał oraz świadome przejście do dalszej swobodnej pracy lub zakończenia sesji. Nie trzeba w tym celu dopisywać kolejnego rozdziału.

## Lista pomysłów i punktacja

Poniżej dokładnie 10 usprawnień, 10 potencjalnych nowych funkcji, 5 UX, 5 technicznych i 5 replayability. **Wszystkie oceny są rekomendacją projektową**, nie wynikiem badań. Wartość i zgodność: więcej = lepiej. Koszt i ryzyko: więcej = trudniej. Skala 1–10 jest względna, bez przeliczenia na dni. Wymagane poprawki mają pierwszeństwo; opcjonalne funkcje nie są warunkiem ukończenia obecnej gry.

### Usprawnienie

| ID | Pomysł i zakres | Wartość | Koszt | Ryzyko | Zgodność | Zależności | Priorytet / charakter |
|---|---|---:|---:|---:|---:|---|---|
| U01 | **Rozliczenie dialogu tylko raz.** Nagroda, koszt, flaga i wznowienie stanowią jedną operację; CODE-001. | 10 | 7 | 6 | 10 | TASK-001 | High / wymagane |
| U02 | **Uczciwa ocena ostatniego ruchu.** Ocena narzędzia ze stanu przed usunięciem brudu; CODE-003. | 8 | 2 | 2 | 10 | TASK-005 | Medium / wymagane |
| U03 | **Spójne zaliczenie inspekcji.** Wcześniejsze wyjście nie daje pełnego sukcesu zawodowego; CODE-004. | 8 | 2 | 2 | 10 | TASK-006 | Medium / wymagane |
| U04 | **Powrót do pierwszej lekcji.** Nauka zależna od ukończenia, z jawnym pominięciem i ponowieniem; CODE-005. | 9 | 4 | 3 | 10 | TASK-007 | High / wymagane przed szerszym testem |
| U05 | **Pierwsza naprawa naprawdę ukończona.** Osobne znaczniki zobaczenia awarii i wykonania naprawy; CODE-006. | 8 | 2 | 2 | 10 | TASK-008 | Medium / wymagane |
| U06 | **Lepsza diagnoza nieudanego mycia.** Krótki sygnał: zły przyrząd, najpierw płukanie, trzeba namoczyć; oparty na istniejącym modelu. | 8 | 4 | 3 | 10 | TASK-005, TASK-011 | Medium / po badaniu graczy |
| U07 | **Widoczny efekt nauki języka.** Porównać czytelność wcześniejszych i nowych słów, zachować sens scen także przy zamazaniu. | 8 | 3 | 2 | 10 | TASK-014, TASK-016 | Medium / rekomendacja |
| U08 | **Czytelny koszt następnego tygodnia.** Ujednolicić preview rachunków, przelewu i zakupu wokół istniejącej rezerwy. | 9 | 4 | 3 | 10 | TASK-015 | High / rekomendacja |
| U09 | **Wyważona duża zmywarka.** Zmierzona korzyść kontra ręczne mycie i czas 30 minut; wyjaśnienie zamrożonych pieniędzy. | 7 | 4 | 4 | 9 | TASK-015 | Medium / rekomendacja |
| U10 | **Lepsze tempo odblokowań.** Skalibrować dni 1–5, presję szefa i awarie na nowych graczach; nie dodawać kolejnych obowiązków. | 9 | 5 | 3 | 10 | TASK-007, TASK-015, TASK-020 | High / rekomendacja |

### Nowa funkcja

| ID | Pomysł i zakres | Wartość | Koszt | Ryzyko | Zgodność | Zależności | Priorytet / charakter |
|---|---|---:|---:|---:|---:|---|---|
| N01 | **Wznowienie przerwanej zmiany.** Snapshot lub uzgodniony checkpoint po ubiciu aplikacji; stan obecnej kuchni nie jest dziś zapisywany. | 10 | 8 | 7 | 10 | TASK-009, TASK-010 | High / wymaga decyzji zakresu |
| N02 | **Swobodny trening narzędzi.** Mała bezkarna próba na wybranym naczyniu, bez zmiany ekonomii kampanii. | 8 | 5 | 4 | 10 | TASK-007, TASK-030 | Medium / opcjonalne |
| N03 | **Krótki dziennik zdarzeń fabularnych.** Odtworzenie poznanych faktów i decyzji, bez spoilowania dalszych rozdziałów. | 7 | 5 | 3 | 10 | TASK-001, TASK-014 | Low / opcjonalne |
| N04 | **Przegląd otrzymanej wypłaty.** Historia ostatnich zmian z rozbiciem normy, tipów i potrąceń, wykraczająca poza ekran wyniku. | 7 | 4 | 3 | 9 | TASK-001, TASK-015 | Low / opcjonalne |
| N05 | **Ręczne profile dostępności.** Zestawy ułatwień zapisane jako nazwana konfiguracja, zamiast każdorazowego ustawiania przełączników. | 6 | 4 | 2 | 9 | TASK-016 | Low / opcjonalne |
| N06 | **Konfigurowalny układ dla dominującej ręki.** Alternatywne strony panelu narzędzi, tylko jeśli potwierdzi to badanie z telefonem. | 8 | 7 | 6 | 9 | TASK-011, TASK-016 | Medium / opcjonalne |
| N07 | **Galeria poznanych zakończeń.** Lokalne podsumowanie odblokowanych epilogów bez nagradzania farmienia slotów. | 6 | 4 | 3 | 9 | TASK-014, TASK-029 | Low / po 1.0 |
| N08 | **Wyzwania mistrzowskie poza karierą.** Ograniczona paczka autorskich zmian z własnym celem i niezależnym rekordem. | 7 | 6 | 4 | 8 | TASK-015, TASK-029 | Low / po 1.0 |
| N09 | **Eksport dobrowolnego zgłoszenia błędu.** Lokalna paczka wersji/logu bez prywatnego zapisu, udostępniana dopiero przez gracza. | 6 | 5 | 4 | 9 | TASK-003, TASK-019 | Medium / opcjonalne dla bety |
| N10 | **Spokojny tryb obejrzenia scen narracyjnych.** Replay poznanych rozmów w odseparowanej sesji bez nagród i zmian kampanii. | 6 | 6 | 5 | 9 | TASK-001, TASK-014 | Low / po 1.0 |

### UX

| ID | Pomysł i zakres | Wartość | Koszt | Ryzyko | Zgodność | Zależności | Priorytet / charakter |
|---|---|---:|---:|---:|---:|---|---|
| X01 | **Prawdziwy stan zapisu.** Zapisano / zapis w toku / błąd + ponów, w jednym spójnym miejscu; CODE-002. | 10 | 4 | 3 | 10 | TASK-002 | High / wymagane |
| X02 | **Hierarchia HUD i skalowanie.** Jeden główny cel, czytelny czas i feedback narzędzia; testy tekstu i safe area. | 9 | 5 | 3 | 10 | TASK-016 | High / rekomendacja |
| X03 | **Wyjaśnienie zakończenia zmiany.** Przed potwierdzeniem pokazać wpływ na dzień, wypłatę, inspekcję i tutorial. | 9 | 3 | 2 | 10 | TASK-006, TASK-007, TASK-015 | High / rekomendacja |
| X04 | **Czytelność bez koloru i dźwięku.** Stan brudu, blokady i pilne zamówienie mają kształt/tekst oraz opcjonalną haptykę. | 8 | 5 | 3 | 10 | TASK-016, TASK-022 | High / rekomendacja |
| X05 | **Spójny powrót z nakładek.** Back zamyka najwyższą warstwę bez przypadkowej zmiany dnia; wspólny słownik przycisków. | 8 | 3 | 3 | 10 | TASK-004, TASK-016 | Medium / rekomendacja |

### Techniczne

| ID | Pomysł i zakres | Wartość | Koszt | Ryzyko | Zgodność | Zależności | Priorytet / charakter |
|---|---|---:|---:|---:|---:|---|---|
| T01 | **Transakcyjny właściciel SaveData.** Spójny snapshot, kontrolowany retry, migracja i idempotencja skutków. | 10 | 8 | 6 | 10 | TASK-001, TASK-002 | High / wymagane |
| T02 | **Powtarzalny runner QA w repo.** Jawne selektory Explicit, XML/logi i manifest silnika/revizji/paczki. | 9 | 4 | 3 | 10 | TASK-003, TASK-004 | High / wymagane |
| T03 | **Blokada wydania po czerwonym PlayMode.** Osobny status stabilnego smoke albo pełny job przed dystrybucją APK. | 9 | 3 | 3 | 10 | TASK-013 | High / wymagane |
| T04 | **Budżety telefonu poparte pomiarem.** Frame time, startup, GC, RAM, termika i build size na wybranym minimum. | 9 | 5 | 2 | 10 | TASK-012, TASK-021 | High / wymagane do release |
| T05 | **Walidacja authoringu przed generowaniem.** Jedno źródło prawdy, deterministyczny dry-run/diff, referencje i grafy. | 8 | 6 | 4 | 10 | TASK-017, TASK-018 | Medium / wymagane przed rozbudową |

### Replay

| ID | Pomysł i zakres | Wartość | Koszt | Ryzyko | Zgodność | Zależności | Priorytet / charakter |
|---|---|---:|---:|---:|---:|---|---|
| R01 | **Różne życiowe priorytety.** Wykorzystać istniejące zakończenia i relacje, jasno różnicować konsekwencje, bez sztucznego grindu. | 8 | 5 | 4 | 10 | TASK-014, TASK-015 | Medium / rekomendacja |
| R02 | **Opcjonalne cele mistrzostwa.** Dobrowolne cele jakości i czystych zmian, oddzielone od konieczności awansu. | 7 | 4 | 3 | 9 | TASK-005, TASK-006, TASK-029 | Low / opcjonalne |
| R03 | **Nowe zestawy ustalonego seeda.** Porównywalne lokalne wyzwania ze znanym układem naczyń; bez serwera i leaderboardu. | 6 | 5 | 4 | 8 | TASK-011, TASK-029 | Low / po 1.0 |
| R04 | **Mała pula wariantów istniejących zdarzeń.** Zmiana zamówień i kombinacji brudu zamiast kolejnego pełnego systemu. | 7 | 4 | 3 | 10 | TASK-018, TASK-023 | Medium / po balansie |
| R05 | **Opcjonalne ograniczenia drugiej kampanii.** Dla chętnych jawnie trudniejsze normy/inna pomoc, bez ukrytej zmiany normalnej gry. | 6 | 5 | 4 | 8 | TASK-015, TASK-029 | Low / po 1.0 |

## Rekomendowana kolejność decyzji

Najpierw zagwarantować zapis i uczciwe reguły, potem sprawdzić pierwszą godzinę gry oraz pełną kampanię, następnie balansować i mierzyć telefon. Nowe treści wprowadzać dopiero po przejściu tych bramek. Właściciel powinien wybrać platformę i minimalne urządzenie, zakres kampanii 1.0, sposób traktowania przerwanej zmiany, profil presji/relaksu oraz języki. Brak odpowiedzi nie uzasadnia automatycznego dodania wszystkich 35 pomysłów do wydania.
