# Backlog uporządkowany

Audyt z 13.10.2026. Badany jest bieżący katalog roboczy, także pliki niezatwierdzone w Git, a nie sam HEAD. Ścieżki dowodów są względem katalogu projektu; numery linii dotyczą tej migawki. Oryginalne skrypty, sceny, ustawienia i assety nie były celowo zmieniane. Testy uruchomiono na odrębnej kopii poza repozytorium.

**Granica pewności:** analiza statyczna potwierdza strukturę i konkretne ścieżki wykonania, test automatyczny potwierdza tylko swoje asercje. Brak pomiarów telefonu, audytu słuchowego, ręcznego przejścia wszystkich wariantów fabuły i testów sklepowych oznacza UNKNOWN w tych obszarach. Nie deklarujemy poznania gry w 100%.

## Zasady realizacji

TASK-001–020 to 20 najważniejszych strumieni pracy w rekomendowanej kolejności rozpoczynania. TASK-021–030 uzupełniają konkretny etap lub są warunkowe/opcjonalne. Zależności i bramki faz mają pierwszeństwo przed numerem zadania; przykładowo optymalizacja z TASK-021 następuje po pomiarach TASK-012, a przed zakończeniem bety TASK-020. Kolejność nie jest kalendarzem ani obietnicą równoległości.

Priorytet opisuje wpływ, trudność zakres techniczny (Small/Medium/Large), bez wyceny godzinowej. To backlog przyszłych zmian — w tym audycie nie wdrożono żadnej z nich. Dowody usterek: `TECHNICAL_AUDIT.md`; pomysły opcjonalne: `GAMEPLAY_REVIEW.md`; wymagane bramki: `DEVELOPMENT_ROADMAP.md`.

W każdym zadaniu implementacyjnym: zachować zastany stan roboczy, ustalić bazową migawkę, nie uruchamiać Run All bez świadomej potrzeby, dołączyć dowód wyniku testu i aktualizację odpowiedniego raportu. Nie dodawać szerokiej refaktoryzacji do małej poprawki.

## TASK-001 — Transakcyjne konsekwencje dialogów

**Aktualizacja 16.10.2026: wdrożone i zweryfikowane dla zapisu v3.** Wspólny zapis skutków, checkpointów i ukończenia beatu/rozdziału; błąd IO zatrzymuje rozmowę z możliwością ponowienia. Końcowe testy: 771/771 EditMode, 42/42 celowanych PlayMode. Szczegóły, historia wcześniejszych niezaliczonych przebiegów i ograniczenie dawnych niedokończonych zapisów v2: [TASK_001_IMPLEMENTATION.md](TASK_001_IMPLEMENTATION.md). Następny strumień: TASK-002. Poniżej zachowano pierwotny kontrakt zadania.

- **Kategoria:** Integralność danych
- **Priorytet:** High
- **Aktualny problem:** CODE-001 i CODE-007: restart między nagrodą a końcem beatu pozwala powtórzyć skutki.
- **Cel:** Każdy skutek wywołany raz, także po awarii.
- **Zakres implementacji:** DialogueSession/StoryBeatTrigger, NarrativeProgress i MetaProgress: wspólny snapshot outcome+flag+checkpoint/applied-ID; stabilne ID graf/węzeł, migracja v2, jawne zachowanie po błędzie dysku. Nie zmieniać scenariusza ani wartości nagród.
- **Powiązane systemy:** SaveData, SaveSystem, fabuła, LifeLedger
- **Zależności:** Brak; najpierw utrwalić test pokazujący ścieżkę ch3 Tomka.
- **Kryteria akceptacji:** Nagroda +30 EUR i +5 integracji oraz koszt wyboru występują najwyżej raz po dowolnej liczbie restartów; cały wybór utrwalony albo cały pending; stary poprawny zapis działa.
- **Testy:** Test integracyjny przerwania przed/po każdej operacji IO; graf tomek3_repay → tomek3_end, wybór z kosztem, przełączenie slotu; pełny EditMode i odpowiednie PlayMode.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 0.

## TASK-002 — Status zapisu, błąd i bezpieczne ponowienie

- **Kategoria:** Zapis / UX
- **Priorytet:** High
- **Aktualny problem:** CODE-002: UI obiecuje zapis mimo false, część kontrolerów ignoruje błąd.
- **Cel:** Uczciwa informacja i odzyskiwalny pending state.
- **Zakres implementacji:** Centralny wynik operacji dla meta/narracji/maszyny i usuwania slotu; wspólny komunikat, retry z ograniczeniem częstotliwości, brak utraty niezapisanego snapshotu; odróżnić zapisane od tylko zmienionego w RAM.
- **Powiązane systemy:** SaveSystem, kontrolery postępu, menu i wynik
- **Zależności:** TASK-001: kontrakt transakcji; projekt UI można przygotować wcześniej.
- **Kryteria akceptacji:** Wymuszony błąd pokazuje czytelny stan; po odzyskaniu zapisu retry utrwala właściwy stan bez duplikacji; nieudane kasowanie nie udaje sukcesu.
- **Testy:** Brak miejsca/no-write, brak katalogu, powrót z tła w trakcie retry, reentry sceny; asercja UI i ponownego odczytu.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 0.

## TASK-003 — Repozytoryjny runner QA i manifest dowodów

- **Kategoria:** Narzędzia / QA
- **Priorytet:** High
- **Aktualny problem:** QA-001: final_regression.ps1 i część dawnych dowodów są pod nieobecną ścieżką E:\Dev.
- **Cel:** Nowy agent odtwarza wynik jednym opisanym poleceniem.
- **Zakres implementacji:** Runner z parametrem Unity i katalogiem wyników; EditMode, PlayMode, jawne grupy Explicit; timestamp UTC, wersja silnika/pakietów, identyfikator snapshotu, hashe XML/builda; kody wyjścia i brak cichego pominięcia raportu.
- **Powiązane systemy:** Tools, CI, QA dokumentacja
- **Zależności:** Brak; nie uruchamiać generatora scen jako setupu testów.
- **Kryteria akceptacji:** Świeża kopia uruchamia suite; brak XML lub niezaliczony test daje niezerowy exit; jawna lista pominiętych testów i ograniczeń grafiki; bez danych prywatnego slotu.
- **Testy:** Uruchomienie pozytywne i kontrolowany nieistniejący filtr/błędna ścieżka; sprawdzenie manifestu i odtworzenia.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 0.

## TASK-004 — Utrwalić regresję i objąć Explicit selektorami

- **Kategoria:** QA integracyjne
- **Priorytet:** High
- **Aktualny problem:** Zieleń zwykłego suite pomija 13 przypadków Explicit; stare wyniki nie są wiarygodnym opisem bieżącej paczki.
- **Cel:** Powtarzalne uruchamianie przypadków długich i graficznych oraz kontrola ich faktycznego zakresu.
- **Zakres implementacji:** Skatalogować 13 przypadków; osobne profile smoke/art/monkey/soak; tour ma asercję tylko zdjęć — dodać kryteria przejścia gdy używany jako test gry; diagnozować niestabilność bez usuwania asercji.
- **Powiązane systemy:** Assets/Tests/PlayMode, DevAutoplay, runner
- **Zależności:** TASK-003.
- **Kryteria akceptacji:** Każdy test ma właściciela, cel i profil; manifest rozróżnia assertion test i art capture; każdy nowy bug z audytu ma ukierunkowaną regresję przy naprawie.
- **Testy:** Pełne 761 EditMode i zwykłe PlayMode po zmianach; osobno Explicit; powtórka tylko przypadków z wykazaną niestabilnością.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 0.

## TASK-005 — Klasyfikować poprawność gestu przed myciem

- **Kategoria:** Gameplay / punktacja
- **Priorytet:** Medium
- **Aktualny problem:** CODE-003: usunięcie ostatniej warstwy zmienia odpowiedź ToolSuits przed naliczeniem accuracy.
- **Cel:** Prawidłowy ruch nie zwiększa czasu złego narzędzia.
- **Zakres implementacji:** DishController/DishInstance zwraca lub publikuje poprawność dla właśnie wykonanego ruchu; ShiftController nie odczytuje do tego nowego stanu; zachować dotychczasowy wzór wyniku.
- **Powiązane systemy:** ScrubStroke, zdarzenie Scrubbing, ShiftScoring
- **Zależności:** TASK-003 zalecane do dowodów; można implementować niezależnie od save.
- **Kryteria akceptacji:** Ostatni poprawny stroke daje 0 przyrostu wrongToolSeconds; błędne narzędzie nadal nalicza karę; zmiana warstwy jest poprawnie sklasyfikowana.
- **Testy:** Ostatni pixel/auto-finish, kilka warstw/narzędzi, dt 1/30 i 1/60, scenowy final stroke z kontrolą wyniku.
- **Szacowana trudność:** Small
- **Sugerowana faza realizacji:** FAZA 0.

## TASK-006 — Ujednolicić zaliczenie inspekcji

- **Kategoria:** Gameplay / kariera
- **Priorytet:** Medium
- **Aktualny problem:** CODE-004: natychmiastowy koniec inspekcji zwiększa licznik sukcesów kariery.
- **Cel:** Sukces wymaga uzgodnionego warunku ukończenia.
- **Zakres implementacji:** Wspólna reguła w wyniku zmiany, używana przez Career, bonus i UI; odróżnić przerwanie od porażki i pełnego sukcesu; zapisać decyzję designerską.
- **Powiązane systemy:** ShiftController, ShiftResultCalculator, Career, wynik
- **Zależności:** Decyzja: pełny czas czy jawny cel inspekcji; domyślna rekomendacja pełny czas i brak błędów.
- **Kryteria akceptacji:** 0-sekundowa i przerwana inspekcja nie zwiększa inspectionsPassed; pełna poprawna zwiększa raz; stłuczka/zawalenie uniemożliwia sukces.
- **Testy:** Macierz early/full × clean/broken/collapse, round-trip save i ponowna publikacja tego samego wyniku.
- **Szacowana trudność:** Small
- **Sugerowana faza realizacji:** FAZA 0.

## TASK-007 — Dokończenie i ponowienie pierwszej lekcji

- **Kategoria:** Tutorial / UX
- **Priorytet:** High
- **Aktualny problem:** CODE-005: wyjście w dniu 1 przesuwa dzień i trwale omija guided lesson.
- **Cel:** Pierwszy kontakt nie pozbawia gracza nauki.
- **Zakres implementacji:** Tutorial uzależnić od znacznika ukończenia, z regułą ponowienia po przerwaniu; jawny przycisk pominięcia i powrotu do pomocy; wyjście informuje o konsekwencjach.
- **Powiązane systemy:** ShiftController, TutorialProgress, menu pauzy, Meta
- **Zależności:** TASK-002; uzgodnić świadomy skip i wpływ na dzień.
- **Kryteria akceptacji:** Przerwanie na każdym etapie lekcji daje określony powrót; ukończenie zapisywane raz; doświadczony gracz może świadomie pominąć bez softlocka.
- **Testy:** Nowy slot, wyjście/pause/reload przed szorowaniem, w trakcie, przed rack; dzień 2 z brakującą flagą; skip i stary save.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 1.

## TASK-008 — Oznaczać ukończenie pierwszej naprawy

- **Kategoria:** Tutorial / stan
- **Priorytet:** Medium
- **Aktualny problem:** CODE-006: Breakdowns > 0 kończy tutorial bez naprawy.
- **Cel:** Pierwsza naprawa ma rzeczywisty warunek ukończenia.
- **Zakres implementacji:** Osobna informacja Repaired w wyniku/stanie; OnRepaired kończy obowiązek; migracja istniejącej flagi według opisanej bezpiecznej reguły.
- **Powiązane systemy:** DishwasherRepair, DishwasherController, ShiftResult, LifeLedger
- **Zależności:** TASK-001 kontrakt zapisu; TASK-007 zasady tutorialu.
- **Kryteria akceptacji:** Wyjście przy aktywnej pierwszej awarii nie oznacza ukończenia; poprawna naprawa kończy obowiązek; kolejne dni nie duplikują nauki po sukcesie.
- **Testy:** Trzy rodzaje awarii, wyjście przed/po zakończeniu, ponowna scena i zapis starszej wersji.
- **Szacowana trudność:** Small
- **Sugerowana faza realizacji:** FAZA 1.

## TASK-009 — Polityka i obsługa przerwanej zmiany

- **Kategoria:** Mobile / lifecycle
- **Priorytet:** High
- **Aktualny problem:** RISK-001: normalna aktywna kuchnia nie ma snapshotu mimo długich mobilnych sesji.
- **Cel:** Gracz rozumie i zachowuje uzgodniony postęp po ubiciu aplikacji.
- **Zakres implementacji:** Najpierw decyzja snapshot vs checkpoint/rozliczenie; następnie wersjonowany stan czasu, naczyń, kosza, awarii, szefa i pending rewards lub dokładny kontrakt uproszczonego wariantu; wyświetlić ograniczenia.
- **Powiązane systemy:** SaveData, ShiftController, fizyka, zmywarki, tutorial
- **Zależności:** TASK-001/002; wymagana decyzja właściciela o oczekiwanym wznowieniu.
- **Kryteria akceptacji:** Restart procesu odtwarza uzgodniony stan bez farmienia wypłat/inspekcji; jasno opisane wyjątki; snapshot z uszkodzonym rekordem daje bezpieczny recovery.
- **Testy:** Kill na etapach scrape/rinse/rack/cycle/repair/result, pause→kill, zmiana slotu i zegara; realny telefon.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 1.

## TASK-010 — Mobilne lifecycle i odzyskiwanie danych

- **Kategoria:** QA urządzeń
- **Priorytet:** High
- **Aktualny problem:** Testy Editor nie potwierdzają force-stop, przerwań systemowych i zachowania pamięci telefonu.
- **Cel:** Integralność każdej krytycznej granicy na wspieranym OS.
- **Zakres implementacji:** Macierz focus/pause/lock/kill/restart; log końca zapisu; maszynowy długi cykl, wybór fabularny, slot delete i upgrade; do runnera raport bez prywatnych danych.
- **Powiązane systemy:** Zapis, Boot, trwałe serwisy, audio, input
- **Zależności:** TASK-001/002/009; wybrane urządzenia i systemy.
- **Kryteria akceptacji:** Na przyjętej macierzy brak utraty potwierdzonego stanu i podwójnego wyniku; powrót odtwarza audio/input; błędy udokumentowane i zamknięte.
- **Testy:** Co najmniej telefon minimum i typowy telefon; przerwania przed/po atomowym replace; stary save po instalacji update.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 2.

## TASK-011 — Gesty, DPI i fizyka na telefonie

- **Kategoria:** Sterowanie / QA
- **Priorytet:** High
- **Aktualny problem:** Brak świeżego dowodu dla dotyku, różnych DPI i utraty focusu; mysz w batchmode nie zastępuje palca.
- **Cel:** Powtarzalny chwyt, mycie i odkładanie bez przypadkowych akcji.
- **Zakres implementacji:** Sprawdzić progi tap/hold/drag, raycasty, szklankę, obrót kosza, edge i multitouch; kalibrować tylko po pomiarze; zachować wspólny model pointer.
- **Powiązane systemy:** GestureRecognizer, DishController, Rack, PrepBoard
- **Zależności:** TASK-005/007/008; ustalony zakres ekranów.
- **Kryteria akceptacji:** Każda wspierana rozdzielczość pozwala wykonać wszystkie etapy; input nad UI nie myje; focus/Back nie zostawia przytrzymanego gestu.
- **Testy:** Macierz DPI/safe area, palec/mysz, aktywne nakładki, zrzut palca z ekranu, random touch; logi bez danych osobowych.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 2.

## TASK-012 — Profil mobilny i budżety wydajności

- **Kategoria:** Performance
- **Priorytet:** High
- **Aktualny problem:** PERF-01–10: brak aktualnego profilu urządzenia i budżetów, desktop soak nie wystarcza.
- **Cel:** Udokumentowane p95/p99 frame time, pamięć i czas wejścia do kuchni dla konkretnego builda.
- **Zakres implementacji:** Profiler release/development w kontrolowanym scenariuszu; CPU/GPU/GC/drawcalls/shadow/physics/startup/memory/build size, 30–60 min termiki; określić budżety wraz z minimum sprzętowym.
- **Powiązane systemy:** DirtCanvas, URP, generator naczyń, QualityGovernor, startup
- **Zależności:** TASK-003 manifest; wybór minimum sprzętowego; bez zmian kodu przed baseline.
- **Kryteria akceptacji:** Każdy pomiar ma urządzenie, OS, engine, preset, hash builda, scenariusz i ślad; hotspoty uszeregowane wg wpływu; limity FPS odróżnione od wyniku.
- **Testy:** Minimum i środek docelowej półki; pierwsze/rozgrzane wejście, pełny kosz, awaria, duży wsad, długi soak.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 5.

## TASK-013 — PlayMode jako warunek dystrybucji APK

- **Kategoria:** CI / build
- **Priorytet:** High
- **Aktualny problem:** BUILD-001: job Android zależy wyłącznie od EditMode.
- **Cel:** Nie publikować artefaktu przeznaczonego dla testerów po czerwonym wymaganym teście gry.
- **Zakres implementacji:** Dodać zależność lub osobną bramkę stable PlayMode na właściwym środowisku; jeśli Linux problematyczny, osobny runner z grafiką; jawne oddzielenie build-only od approved-for-test.
- **Powiązane systemy:** Workflow Android, runner, artefakty
- **Zależności:** TASK-003/004.
- **Kryteria akceptacji:** Wymuszona porażka obowiązkowego PlayMode blokuje etap dystrybucji; cache i raporty nie maskują failure; pominięcie suite widoczne.
- **Testy:** Kontrolowany czerwony test na gałęzi technicznej i pozytywny przebieg; anulowanie/brak XML/niedostępność runnera.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 0.

## TASK-014 — Macierz całej kampanii i zakończeń

- **Kategoria:** Narracja / integracja
- **Priorytet:** High
- **Aktualny problem:** 35 grafów jest strukturalnie spójnych, lecz brak pełnego dowodu wszystkich ścieżek gracza i flag.
- **Cel:** Każda wspierana droga prowadzi do sensownego, osiągalnego finału.
- **Zakres implementacji:** Macierz 6 rozdziałów/35 beatów, zależności flag/daty, nowe i stare sloty; kompletne ścieżki Dublin, Kraków, Pilbara; restart w kluczowych wyborach i przegląd językowy; zastąpić niejednoznaczne „Ciąg dalszy nastąpi” po finalnym chapter uzgodnionym domknięciem i regułą dalszej pracy.
- **Powiązane systemy:** StoryBeatTrigger, ChapterDefinition, DialogueGraph, LifeLedger
- **Zależności:** TASK-001/002/006/007.
- **Kryteria akceptacji:** Każdy beat ma warunki startu i wyjścia; nieosiągalne gałęzie usunięte/uzasadnione; trzy drogi dochodzą do właściwego epilogu bez debug bypass.
- **Testy:** Automat modelowy flag + scenowy reprezentant każdej ścieżki + ręczne pełne przejścia; nie ograniczać do reachability grafu.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 2.

## TASK-015 — Ekonomia, awanse i tempo kampanii

- **Kategoria:** Game design / balans
- **Priorytet:** High
- **Aktualny problem:** Niezweryfikowana cała długość progresji, wpływ prep/dużej maszyny oraz early exit na rachunki.
- **Cel:** Uczciwy, zrozumiały rytm bez exploitu i niezamierzonej pułapki długu.
- **Zakres implementacji:** Zdefiniować profile początkujący/sprawny/relaxed; policzyć dochód i wydatki, rangę 800 naczyń itd.; stroić istniejące wartości; ujawnić konsekwencje w UI; opisać cel długości.
- **Powiązane systemy:** LifeLedger, Career, ShiftScoring, Prep, duża zmywarka
- **Zależności:** TASK-001/005/006/008/014.
- **Kryteria akceptacji:** Każdy profil ma wykres zasobów i uzasadnioną drogę awansu; early exit nie daje darmowego sukcesu; możliwe wyjście z długu zgodne z intencją.
- **Testy:** Symulacje deterministyczne, strategie pesymalne/optymalne, 0-dish i spam zakończenia, pełne kampanie testowe.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 5.

## TASK-016 — Przegląd UX i dostępności pierwszej sesji

- **Kategoria:** UX / UI
- **Priorytet:** High
- **Aktualny problem:** Bogaty HUD i ustawienia nie mają pełnej walidacji dotykowej oraz czytelności na minimum.
- **Cel:** Nowy gracz sam kończy pierwszą zmianę i rozumie wypłatę.
- **Zakres implementacji:** Ustalić hierarchię informacji, SafeArea, największy font, kontrast/kolor, back stack, tooltips i preview konsekwencji wyjścia; poprawki oparte na obserwacji, zachować istniejące ułatwienia.
- **Powiązane systemy:** Menu, HUD, tutorial, wynik, ustawienia
- **Zależności:** TASK-002/007/011; badanie nowych graczy.
- **Kryteria akceptacji:** Brak uciętych kluczowych tekstów/akcji na wspieranych ekranach; najważniejsze stany rozpoznawalne bez koloru i bez dźwięku; kompletna pierwsza sesja bez prowadzenia.
- **Testy:** LayoutPlayMode/UiPolish + ręczne font max/safe area/relaxed/wyciszenie; obserwacja zadaniowa z zapisaniem błędnych prób.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 4.

## TASK-017 — Naprawić referencję TMP i preflight assetów

- **Kategoria:** Assets / walidacja
- **Priorytet:** Medium
- **Aktualny problem:** ASSET-001: defaultSpriteAsset wskazuje nieobecny GUID; skan zerowych odwołań nie dowodzi śmieci.
- **Cel:** Brak nieuzasadnionych brakujących referencji w kandydacie.
- **Zakres implementacji:** Wybrać używany sprite asset lub jawne null gdy niepotrzebny; walidator scen, m_Script, GUID, Resources, BuildSettings i danych; dla 14 kandydatów sprawdzić referencje po stringu/config przed decyzją.
- **Powiązane systemy:** TMP Settings, BuildPreflight, Assets
- **Zależności:** TASK-003; uzgodnić czy sprite tags są wspierane.
- **Kryteria akceptacji:** 0 brakujących wymaganych referencji; przykładowe TMP sprite/fallback działa lub formalnie wyłączone; żadnych automatycznych skasowań zasobów po samym skanie.
- **Testy:** Preflight świeżego checkoutu; import pustego Library w kopii; render długich polskich tekstów i ewentualnych sprite tags.
- **Szacowana trudność:** Small
- **Sugerowana faza realizacji:** FAZA 0.

## TASK-018 — Jednoznaczne źródło danych i scen

- **Kategoria:** Narzędzia / authoring
- **Priorytet:** Medium
- **Aktualny problem:** DEBT-03: generatory i wygenerowane assety/sceny mogą rozjechać się przy Run All.
- **Cel:** Kolejny autor nie nadpisuje ręcznych poprawek przez odtworzenie scen.
- **Zakres implementacji:** Opisać właściciela każdego typu danych; deterministyczny dry-run/diff generatorów, idempotencja w kopii, walidacja grafów i obowiązkowych controller refs; instrukcja aktualizacji zawartości.
- **Powiązane systemy:** VerticalSliceSetup, Stage2, Art/Story generators, BuildPreflight
- **Zależności:** TASK-017, kopia/snapshot źródeł.
- **Kryteria akceptacji:** Dwa kolejne uruchomienia w kopii dają równoważny wynik poza jawnie zmiennymi metadanymi; zmiana ręczna ma opisane zachowanie; brak niespodziewanego resetu settings.
- **Testy:** Hash/diff przed/po, otwarcie 4 scen i EditMode/PlayMode smoke po generowaniu.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 2.

## TASK-019 — Kandydat builda z jednoznacznym pochodzeniem

- **Kategoria:** Build / release engineering
- **Priorytet:** High
- **Aktualny problem:** Istniejący APK i Windows nie mają dowodu zgodności z obecną migawką.
- **Cel:** Podpisany lub testowy kandydat powiązany z raportami i konfiguracją.
- **Zakres implementacji:** Pin engine/packages, wybór systemu i platformy; build na czystej kopii z manifestem/hashami, symbole i logi; audyt CREDITS/źródeł audio/portretów, permissions i faktycznie użytych pakietów; bez publikacji sklepowej w ramach samego zadania.
- **Powiązane systemy:** Windows/Android/iOS build scripts, CREDITS, CI
- **Zależności:** TASK-003/010/012/013/017; konta i klucze tylko gdy właściciel udostępni.
- **Kryteria akceptacji:** Paczka instaluje się, uruchamia i przechodzi smoke; manifest wskazuje engine, konfigurację, źródła i wynik QA; brak dev autoplay w release; kwestie licencji mają źródła/rozstrzygnięcie.
- **Testy:** Clean build/install/update/uninstall na macierzy; podpis i weryfikacja wymagań sklepu według aktualnych oficjalnych źródeł.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 7.

## TASK-020 — Zamknięta alfa/beta z bramkami akceptacji

- **Kategoria:** Produkcja / QA
- **Priorytet:** High
- **Aktualny problem:** Brak obecnego dowodu niezawodnej pełnej kampanii i realnego komfortu grupy docelowej.
- **Cel:** Jawna decyzja o wejściu do RC poparta testami i znanymi ograniczeniami.
- **Zakres implementacji:** Sesje pierwszego kontaktu, kompletne kampanie, zgłoszenia z wersją, triage i regresja; alfa zamyka scope, beta stabilność/UX/balans; mierzyć problemy bez wymyślania celu retencji.
- **Powiązane systemy:** Cały produkt, backlog, buildy
- **Zależności:** TASK-001–019 w odpowiednim zakresie oraz wymagane zadania faz 4–5.
- **Kryteria akceptacji:** Zero otwartych Critical/High wpływających na zapis, progresję i uruchomienie; jawne odchylenia; wszystkie wymagane ścieżki i urządzenia z dowodem; decyzja RC udokumentowana.
- **Testy:** Checklisty z DEVELOPMENT_ROADMAP, pełna kampania, dłuższe sesje, install/update, regresja każdego naprawionego buga.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 6.

## TASK-021 — Optymalizować wyłącznie zmierzone hotspoty

- **Kategoria:** Performance
- **Priorytet:** Medium
- **Aktualny problem:** Koszt masek, raycastów, miniatur i URP jest ryzykiem, nie zmierzonym budżetem.
- **Cel:** Spełnić przyjęte budżety minimum sprzętowego.
- **Zakres implementacji:** Wybrać 1–3 dominujące hotspoty z TASK-012; warianty patch upload/odświeżania miniatur/cachingu zapytań tylko wg pomiarów; A/B tych samych scenariuszy, zachowanie jakości i deterministyczności.
- **Powiązane systemy:** DirtCanvas, BigDishwasher, ChefOrders, URP
- **Zależności:** TASK-012; TASK-018 przy zmianach generatora.
- **Kryteria akceptacji:** Poprawa widoczna w śladach tego samego urządzenia; brak regresji fizyki, czyszczenia i UI; koszt pamięci jawny.
- **Testy:** Profiler przed/po, długi soak, pełny suite zmienionych obszarów; wizualne porównanie wyników.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 5.

## TASK-022 — Odsłuch i odporność warstwy audio

- **Kategoria:** Audio / UX
- **Priorytet:** Medium
- **Aktualny problem:** Testy banków dźwiękowych nie potwierdzają balansu, nakładania i przerwań audio.
- **Cel:** Czytelna informacja bez męczącego miksu.
- **Zakres implementacji:** Odsłuch na głośniku i słuchawkach, master/music/SFX, radio+szef+awaria, limity równoległych źródeł, fade/pause/resume; haptyka osobno na Android/iOS zgodnie ze scope.
- **Powiązane systemy:** GameAudio, KitchenAudio, GameSoundBank, radio, haptics
- **Zależności:** TASK-010/016; materiały źródłowe i prawa TASK-019.
- **Kryteria akceptacji:** Brak dominujących/ucinanych ważnych sygnałów; mute utrzymany między scenami; background i powrót zachowują intencję; wyniki odsłuchu opisane.
- **Testy:** Manualne nagranie/odsłuch scenariuszy, suwak0/max, przełączenie wyjścia audio i przerwanie systemowe.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 4.

## TASK-023 — Domknąć zawartość w zatwierdzonym zakresie

- **Kategoria:** Content / design
- **Priorytet:** Medium
- **Aktualny problem:** Kompletna struktura 6 rozdziałów nie dowodzi braku dziur w tempie i czytelności scen.
- **Cel:** Każdy wymagany rozdział i wariant ma finalne dane.
- **Zakres implementacji:** Na podstawie TASK-014/015 uzupełnić konkretne brakujące przejścia, warianty i teksty; nie dodawać nowych systemów ani kolejnego rozdziału bez scope; katalog assetów i akceptacja spójności.
- **Powiązane systemy:** Dialogi, chapters, shift catalog, materiały i portrety
- **Zależności:** TASK-014/015/018, zatwierdzony zakres 1.0.
- **Kryteria akceptacji:** Wszystkie braki macierzy rozwiązane lub formalnie usunięte ze scope; zero orphan targetów; wszystkie nowe teksty przeszły korektę i render.
- **Testy:** StoryContentTests, graph validator, pełne ścieżki dotkniętych beatów, screen na minimalnym ekranie.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 3.

## TASK-024 — Stopniowo rozdzielić największe kontrolery

- **Kategoria:** Architektura
- **Priorytet:** Medium
- **Aktualny problem:** DishController/ShiftController/HUD łączą wiele odpowiedzialności; drobna zmiana dotyka kilku systemów.
- **Cel:** Mniejsza powierzchnia regresji przy przyszłej rozbudowie.
- **Zakres implementacji:** Wydzielać tylko granice potrzebne konkretnym zadaniom: ocena gestu, wynik zmiany, koordynacja save; jawne lifecycle/subscription; bez przepisywania całej gry czy wprowadzania DI frameworka na zapas.
- **Powiązane systemy:** Kitchen controllers, MetaHub, ServiceLocator
- **Zależności:** TASK-003/004 oraz regresje zmienianej funkcji.
- **Kryteria akceptacji:** Zachowanie przed/po równoważne; każda ekstrakcja ma właściciela i test kontraktu; brak nowej globalnej zależności i podwójnych subskrypcji.
- **Testy:** Load/unload/restart 4 scen, domain reload zgodny z ustawieniami, zdarzenia dokładnie raz; testy funkcji.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 2.

## TASK-025 — Zdecydować zakres języków i obsłużyć teksty

- **Kategoria:** Content / lokalizacja
- **Priorytet:** Low
- **Aktualny problem:** Pakiet Localization jest zainstalowany, lecz własne teksty pozostają w C# i grafach.
- **Cel:** Spójny plan językowy bez niepotrzebnej migracji.
- **Zakres implementacji:** Jeśli PL only: korekta i standard formatowania; jeśli wiele języków: stabilne ID/tabele i reguły angielskich słów fabularnych, bez niszczenia mechaniki nauki języka.
- **Powiązane systemy:** Narrative, UI, PolishPlural, WordBlur, Localization
- **Zależności:** Decyzja właściciela, TASK-014/016/018.
- **Kryteria akceptacji:** Scope językowy zapisany; teksty zgodne z nim, poprawne polskie liczby i format pieniędzy; drugi język tylko po kompletnym przejściu.
- **Testy:** Pseudo-localization dla wielu języków, długie frazy, plural, unknown words i render maksymalnego fontu.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 3.

## TASK-026 — Zweryfikować iOS, jeśli należy do wydania

- **Kategoria:** Platforma
- **Priorytet:** High warunkowy
- **Aktualny problem:** Istnieje eksport Xcode i natywna haptyka, brak aktualnego podpisanego builda i device QA.
- **Cel:** Uzasadniona decyzja wspieramy/odraczamy iOS.
- **Zakres implementacji:** Mac/Xcode, signing/provisioning, build release, safe area i lifecycle, plugin haptics; aktualne wymagania platformy sprawdzić przed buildem; nie nazywać samego eksportu wsparciem produkcyjnym.
- **Powiązane systemy:** IosBuildScript, ZmywakHaptics.mm, lifecycle
- **Zależności:** Decyzja platformowa i dostęp do Mac/urządzenia; TASK-010/019.
- **Kryteria akceptacji:** Albo kompletny dowód instalacji/testów na macierzy iOS, albo jawne wyłączenie z zakresu 1.0.
- **Testy:** Install/update, pause/kill, silent mode, haptyka off/on, notch i słaby wspierany model.
- **Szacowana trudność:** Large
- **Sugerowana faza realizacji:** FAZA 7.

## TASK-027 — Macierz migracji i recovery zapisów

- **Kategoria:** QA / kompatybilność
- **Priorytet:** High
- **Aktualny problem:** Testy korupcji istnieją, lecz brak pełnej dowodowej macierzy aktualizacji między dystrybuowanymi buildami.
- **Cel:** Nowa paczka zachowuje postęp legalnych starszych wersji.
- **Zakres implementacji:** Zanonimizowane fixtures wersji, backup/tmp/main, braki pól, skrajne listy/counters, duży cykl UTC i sloty; zdefiniować co robimy z nieobsługiwaną przyszłą wersją.
- **Powiązane systemy:** SaveValidation, SaveMigration, SaveSystem
- **Zależności:** TASK-001/002/009/019; znane wersje dystrybuowane użytkownikom.
- **Kryteria akceptacji:** Każda wspierana wersja migracji ma fixture i wynik; brak cichego resetu poprawnego save; recovery wyjaśnia skutek i nie miesza slotów.
- **Testy:** Macierz snapshotów × upgrade/downgrade policy × corruption; wartości brzegowe/liczby niefinitywne jeśli format na nie pozwala; próby install update na telefonie.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 6.

## TASK-028 — Lista wydania i kontrola RC

- **Kategoria:** Release
- **Priorytet:** High
- **Aktualny problem:** Nie ma zatwierdzonego RC z zamkniętymi bramkami produktu i dystrybucji.
- **Cel:** Kontrolowane wydanie 1.0 z odtwarzalnym kandydatem.
- **Zakres implementacji:** Freeze, znane problemy, numer builda, materiały sklepu zgodne z faktami, prawa do zasobów, privacy zgodne z faktycznymi danymi, crash support i plan hotfix/rollback; publikacja jako osobno autoryzowana czynność.
- **Powiązane systemy:** Produkcja, CI, CREDITS, dystrybucja
- **Zależności:** TASK-019/020/022/027 i TASK-026 jeśli iOS w scope.
- **Kryteria akceptacji:** Jedna paczka o znanym hash spełnia bramki RC; podpis, assets i opis zatwierdzone; brak otwartych blockerów; plan reakcji przetestowany.
- **Testy:** Smoke finalnej paczki, czysta instalacja/upgrade, offline, łączność i permissions, wymagania sklepowe wg aktualnych oficjalnych źródeł.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 7.

## TASK-029 — Ocenić dodatki replayability po kampanii

- **Kategoria:** Design / opcjonalne
- **Priorytet:** Low
- **Aktualny problem:** Różne zakończenia istnieją, lecz chęć ponownego przejścia nie jest zmierzona.
- **Cel:** Wybrać najwyżej mały dodatek potwierdzony zainteresowaniem graczy.
- **Zakres implementacji:** Porównać R01–R05/N07–N08 z feedbackiem; ewentualny prototyp odseparowany od ekonomii save i scope wydania; bez serwera.
- **Powiązane systemy:** Career, shifts, epilogi
- **Zależności:** TASK-014/015/020; nie blokuje 1.0.
- **Kryteria akceptacji:** Decyzja odrzucamy albo jeden mały eksperyment z jasnym celem; brak obowiązkowego grindu w bazowej kampanii.
- **Testy:** Test zainteresowania i próba scenariusza; regresja zapisów/awansów gdy dodatek zostanie wybrany.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 3.

## TASK-030 — Ocenić bezkarne ćwiczenie narzędzi

- **Kategoria:** Design / opcjonalne
- **Priorytet:** Low
- **Aktualny problem:** Tutorial może wystarczyć; nie potwierdzono potrzeby osobnego treningu.
- **Cel:** Pomóc wyłącznie tam, gdzie testerzy nadal nie rozumieją narzędzi.
- **Zakres implementacji:** Po poprawieniu tutorialu zmierzyć pomyłki; jeśli nadal istotne, mała odseparowana sesja z istniejącymi naczyniami i feedbackiem, bez wypłat ani zmian kampanii.
- **Powiązane systemy:** Tutorial, DishController, UI
- **Zależności:** TASK-007/011/016/020; nie blokuje 1.0.
- **Kryteria akceptacji:** Albo udokumentowana rezygnacja, albo trening poprawia samodzielność i nie wpływa na save ekonomii.
- **Testy:** Porównanie wykonania przed/po ćwiczeniu; wyjście, reset, slot switch i brak nagród.
- **Szacowana trudność:** Medium
- **Sugerowana faza realizacji:** FAZA 3.
