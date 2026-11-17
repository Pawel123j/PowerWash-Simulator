# ZMYWAK: Zielona Wyspa — raport stanu projektu

**Aktualizacja 16.10.2026:** wykonano pierwszą partię stabilizacji, TASK-001: transakcyjne konsekwencje dialogów i checkpointy v3. Końcowe testy: 771/771 EditMode oraz 42/42 celowanych PlayMode. Pełny raport, lista zmienionych plików i ograniczenia migracji: [TASK_001_IMPLEMENTATION.md](TASK_001_IMPLEMENTATION.md). Masterplan: [1/44 punktów](MASTERPLAN_PROGRESS.md); następne zadanie to TASK-002. Nie oznacza to gotowości do publikacji. Poniższy raport opisuje historyczną migawkę audytu z 13.10.2026.

**Data:** 13 października 2026. **Tryb:** audyt do odczytu z nowymi raportami w Documentation. **Rekomendacja:** stabilizować istniejącą grę i przygotować zamknięte testy; nie deklarować gotowości publikacji 1.0.

Audyt z 13.10.2026. Badany jest bieżący katalog roboczy, także pliki niezatwierdzone w Git, a nie sam HEAD. Ścieżki dowodów są względem katalogu projektu; numery linii dotyczą tej migawki. Oryginalne skrypty, sceny, ustawienia i assety nie były celowo zmieniane. Testy uruchomiono na odrębnej kopii poza repozytorium.

**Granica pewności:** analiza statyczna potwierdza strukturę i konkretne ścieżki wykonania, test automatyczny potwierdza tylko swoje asercje. Brak pomiarów telefonu, audytu słuchowego, ręcznego przejścia wszystkich wariantów fabuły i testów sklepowych oznacza UNKNOWN w tych obszarach. Nie deklarujemy poznania gry w 100%.

## 1. Czym obecnie jest gra i co zbudowano

To portretowy symulator pracy na zmywaku połączony z narracyjną symulacją życia polskiego emigranta. Kuba pracuje, zarabia, uczy się języka, pokrywa koszty życia i podejmuje wybory w historii rozciągniętej od 2004 do 2010 r. Nazwa katalogu „PowerWash Simulator” jest myląca; produkt i dane identyfikują grę jako **ZMYWAK: Zielona Wyspa**.

Zbudowano zintegrowany rdzeń: cztery sceny Boot/MainMenu/MetaHub/Kitchen_Shift, pięć modułów runtime, fizyczne naczynia i lokalne maski brudu, mycie etapami i narzędzia, kosz, dwie maszyny, awarie i naprawy, presję zamówień i czasu, wynik, ekonomię życia, karierę, narrację, sloty, ustawienia, dźwięk oraz proceduralną warstwę wizualną. To rozbudowana gra, której kolejnym problemem jest spójne ukończenie i sprawdzenie całości.

Skala zbadanej migawki: **159 skryptów runtime (26 794 linie), 25 narzędzi Editor, 79 plików C# testów/helperów; razem 263 C# (54 664 linie)**. Zawartość obejmuje 8 typów naczyń, 7 zabrudzeń, 5 narzędzi, 5 zaplanowanych archetypów zmiany, 6 rozdziałów, 35 grafów dialogowych (274 węzły, 465 wypowiedzi, 95 wyborów) i 21 wpisów slangu. Liczby mierzą zakres, nie jakość ani procent ukończenia.

Silnik projektu: Unity **6000.6.3f1**, URP **17.6.0**, Input System **1.20.0**. Główny kierunek to Android; konfiguracja i build tooling obejmują także iOS i Windows. Produkcyjna gotowość każdej platformy wymaga osobnego dowodu.

## 2. Co gracz rzeczywiście może robić

- Wybrać jeden z trzech slotów, wracać do mieszkania, korzystać z ustawień i prowadzić osobną kampanię.
- Wybierać i przesuwać naczynia, usuwać różne zabrudzenia odpowiednią kolejnością działań, płukać, namaczać i szorować; przy szkle sprawdzać pozostałości.
- Układać różne kształty w koszu 24 komórek, obsługiwać maszynę, naprawiać trzy rodzaje awarii; korzystać także z długiego cyklu dużej maszyny.
- Realizować zmiany o zróżnicowanej presji i normach, zamówienia szefa, zmiany specjalne oraz opcjonalny prep z krojeniem.
- Otrzymać wynik S–D, wypłatę, premie i potrącenia, gromadzić statystyki kariery i awansować.
- Zarządzać energią, tęsknotą, integracją, kosztami życia, zakupami i pieniędzmi wysyłanymi do domu; przeżyć konsekwencje długu.
- Czytać dialogi, wybierać odpowiedzi, odblokowywać słowa/slang i kierować historię do dróg zakończenia związanych z Dublinem, Krakowem lub Pilbarą.
- Ustawić głośność, radio, jakość, skalę tekstu, kolory, haptykę i ułatwienia rozgrywki.

Połączenia tych funkcji sprawdzono w kodzie/scenach i istniejących testach. Świeży autoplay pokonał dwie zmiany i odłożył 18 naczyń; próbki jego obrazów potwierdzają menu, dialog, mycie, wynik i kolejny dzień. Nie potwierdza to wszystkich wariantów każdej mechaniki ani trzech pełnych kampanii.

## 3. Co jest ukończone, a co niedokończone

**Zweryfikowane w ograniczonym zakresie:** modele reguł mycia, układu kosza, napraw, czasu i wyliczeń, parsowanie/grafy narracji, zapis/odczyt i scenariusze korupcji, liczne ścieżki UI i integracji scen. Ich opis i konkretny zakres COMPLETE znajdują się w [inwentaryzacji](FEATURE_INVENTORY.md). Zielony test jednostkowy nie zmienia automatycznie całego systemu w COMPLETE.

**BROKEN w konkretnych ścieżkach:** skutki fabuły po przerwaniu, wiarygodność komunikatu o zapisie, klasyfikacja ostatniego ruchu mycia, zaliczenie inspekcji, powrót do pierwszej lekcji i uznanie pierwszej naprawy. Szczegóły poniżej i w [audycie technicznym](TECHNICAL_AUDIT.md).

**PARTIAL/UNKNOWN na poziomie produktu:** niezawodność mobilnego lifecycle i inputu, pełna kampania/osiągalność semantyczna, rzeczywisty balans, wydajność i termika minimum sprzętowego, jakość odsłuchowa, dostępność oraz finalny build. Aktywna zwykła zmiana nie jest odtwarzana ze snapshotu — konieczna decyzja o oczekiwanym sposobie wznowienia. Po epilogu nadal pojawia się „Ciąg dalszy nastąpi” i można dalej pracować; produktowe domknięcie kampanii wymaga świadomego określenia i odbioru (TASK-014).

Nie ma potrzeby traktowania braku pojazdów, walki, AI przeciwników, wyścigów lub multiplayera jako nieukończenia tej gry. Brak AnimatorController, prefabów czy Addressables także nie jest samodzielnym dowodem braku funkcji: dużo zawartości jest generowane i obsługiwane proceduralnie. Utrudnia to jednak utrzymanie bez jasnej reguły authoringu.

## 4. Najważniejsze problemy — kolejność ryzyka

Nie znaleziono podstaw do zgłoszenia **Critical**. Problemy są potwierdzone statycznie lub jako brak procesu; nie przedstawiam ich jako nowych runtime crashy.

| Priorytet | ID | Problem i konsekwencja | Pierwsze działanie |
|---|---|---|---|
| High | CODE-001 | Skutek dialogu zapisuje się przed zakończeniem beatu; restart może naliczyć pieniądze/koszt ponownie. Konkretny przykład: zwrot 30 EUR przez Tomka. | Transakcja, trwałe ID zastosowanego skutku/checkpoint i regresja przerwań |
| High | CODE-002 | Zapis może zwrócić false, a UI nadal zapewnia o zachowaniu postępu; ryzyko nieświadomej utraty danych. | Status błędu, pending snapshot i bezpieczny retry |
| High | BUILD-001 | Android może się zbudować bez wymaganego sukcesu PlayMode. | Wprowadzić bramkę przed dystrybucją |
| High | QA-001 | Część runnerów i historycznych dowodów znajduje się poza repo i jest niedostępna. | Runner i manifest wyników w repo |
| Medium | CODE-003 | Poprawny ostatni ruch jest oceniany po usunięciu brudu jako złe narzędzie. | Ocena na stanie sprzed mutacji |
| Medium | CODE-004 | Natychmiastowe zakończenie inspekcji zalicza sukces kariery. | Jawny warunek ukończenia wspólny dla wyniku i awansu |
| Medium | CODE-005 | Przerwana lekcja pierwszego talerza nie powraca w dniu 2. | Completion flag, jawny skip i ponowienie |
| Medium | CODE-006 | Samo wystąpienie awarii oznacza w save zakończenie pierwszej naprawy. | Rozróżnić awarię i faktyczne naprawienie |
| Medium | CODE-007 | Jeden węzeł dialogu może wykonywać wiele pełnych synchronicznych zapisów. | Batchowanie w transakcji; wpływ wydajności mierzyć na telefonie |
| Low | ASSET-001 | Nieobecny GUID domyślnego TMP sprite asset. Nie wykazano blokera zwykłego tekstu. | Świadomie przypisać asset albo usunąć niepotrzebną referencję podczas późniejszej naprawy |

Osobny rejestr obejmuje **8 ryzyk**, a nie udowodnionych awarii: resume aktywnej kuchni, błędy ładowania scen, duplikaty globalnych usług, cache pracownika prewarm, granice walidacji save, zegar UTC, modyfikowanie URP przez quality oraz brak dowodów device/release. Nie należy przedstawiać ich jako wykrytych crashy.

## 5. Technika i dług

Pięć modułów ma czytelny kierunek zależności bez cyklu własnych asmdef. Modele są w dużej części testowalne. Zespolenie wejścia, fizyki, renderingu i scoringu w dużych kontrolerach oraz globalne usługi utrudniają dalsze zmiany. Priorytetem jest właściciel zapisu i granice zdarzeń; szerokie przepisanie architektury nie jest wymagane do każdej poprawki.

Dane/sceny są jednocześnie wynikiem generatorów i plikami edytowalnymi. Trzeba ustalić źródło prawdy i sprawdzić deterministyczność generowania w kopii. Skan nie znalazł brakujących skryptów w scenach ani duplikatów GUID. Kandydaci bez referencji nie są automatycznie zbędni: występują Resources i ustawienia projektu.

Dziesięć obszarów pomiaru wydajności obejmuje maski tekstur/Upload, alokacje, URP/shadows, raycasty i fizykę, startup, pooling/miniatury, rozmiar paczki i termikę. **Brak aktualnych pomiarów telefonu.** W raporcie nie ma wymyślonych FPS ani RAM. Istniejące limity 30/60 FPS są konfiguracją, nie uzyskanym wynikiem.

## 6. Stabilność i testy

| Zestaw wykonany na kopii | PASS | FAIL | SKIP w danym przebiegu |
|---|---:|---:|---:|
| EditMode | 761 | 0 | 0 |
| PlayMode standard | 132 | 0 | 13 Explicit |
| Wybrane Explicit: tour i 2 seedy monkey | 3 | 0 | 0 |

**896 wykonanych przypadków przeszło**, w tym 1 pakietowy test stub; **10 spośród 13 Explicit nadal nie uruchomiono**. Nie było niezaliczonego testu w zakończonych przebiegach. Nie zmierzono flake rate ani pokrycia linii. Błędy wykazane przez analizę kodu nie zostały naprawione w ramach audytu.

Wyniki uzyskano na **Unity 6000.6.5f1**, nie zadeklarowanym 6000.6.3f1. Silnik zmienił w kopii ProjectVersion oraz serializowaną listę runtime settings URP. To ogranicza przenoszenie wyników grafiki na oryginalny patch. Wszystkie 1718 kontrolowanych oryginalnych plików zachowały SHA-256. Szczegółowe polecenia, fixture, wyniki Explicit, logi, hashe i zakres inspekcji obrazów: [QA_STATUS.md](QA_STATUS.md).

Nie uruchomiono nowego builda mobilnego ani pełnej ręcznej kampanii; nie odsłuchano miksu i nie wykonano profiler capture na urządzeniu. Historyczne APK, Windows build i wyniki desktop soak pozostają materiałem pomocniczym, nie potwierdzeniem bieżącego release.

## 7. Gotowość produkcyjna i droga do 1.0

**Ocena: zakres alfa, kandydat do zamkniętych testów beta; gotowość wydania niepotwierdzona.** Grywalny prototyp i reprezentatywny slice istnieją. Aby uznać alfę za ukończoną, należy formalnie zamknąć scope, zabezpieczyć zapis i udowodnić ukończenie wszystkich wymaganych dróg. Beta wymaga także urządzeń, UX, ekonomii i wydajności; RC — jednej zidentyfikowanej paczki, update/recovery oraz zamkniętych bramek dystrybucji.

Do 1.0 brakuje przede wszystkim niezawodności i dowodów jakości, nie kolejnych dużych mechanik. Osiem faz, kryteria A–F i zależności są w [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md). Brak arbitralnych dat: zasoby zespołu, platformy i minimalny sprzęt nie są ustalone.

## 8. Największy potencjał

**Ocena projektowa:** warto rozwijać poczucie manualnego mycia, różnicę między typami brudu, czytelny feedback oraz znaczenie codziennej pracy dla życia Kuby. Narracja o emigracji, nauka języka i materialne wybory mogą nadać charakter krótkim sesjom. Hipotezę atrakcyjności trzeba sprawdzić na grupie docelowej; nie jest to wynik analizy rynku.

Najpierw dopracować istniejące systemy i całą kampanię. Opcjonalne ćwiczenia, dziennik i warianty wyzwań rozważać po badaniu potrzeb. [GAMEPLAY_REVIEW.md](GAMEPLAY_REVIEW.md) zawiera dokładnie 35 ocenionych propozycji: 10 usprawnień, 10 nowych funkcji, 5 UX, 5 technicznych i 5 replayability. Nie są one automatycznie zakresem 1.0.

## 9. Dwadzieścia najważniejszych następnych zadań

Rekomendowana kolejność rozpoczynania. Konkretne zależności mogą wymagać równoległego przygotowania lub wykonania zadań uzupełniających; szczegółowe kontrakty są w [PRIORITIZED_BACKLOG.md](PRIORITIZED_BACKLOG.md).

| Kolejność | Zadanie | Oczekiwany wynik |
|---:|---|---|
| 1 | TASK-001 — Transakcyjne konsekwencje dialogów | Każdy skutek wywołany raz, także po awarii. |
| 2 | TASK-002 — Status zapisu, błąd i bezpieczne ponowienie | Uczciwa informacja i odzyskiwalny pending state. |
| 3 | TASK-003 — Repozytoryjny runner QA i manifest dowodów | Nowy agent odtwarza wynik jednym opisanym poleceniem. |
| 4 | TASK-004 — Utrwalić regresję i objąć Explicit selektorami | Powtarzalne uruchamianie przypadków długich i graficznych oraz kontrola ich faktycznego zakresu. |
| 5 | TASK-005 — Klasyfikować poprawność gestu przed myciem | Prawidłowy ruch nie zwiększa czasu złego narzędzia. |
| 6 | TASK-006 — Ujednolicić zaliczenie inspekcji | Sukces wymaga uzgodnionego warunku ukończenia. |
| 7 | TASK-007 — Dokończenie i ponowienie pierwszej lekcji | Pierwszy kontakt nie pozbawia gracza nauki. |
| 8 | TASK-008 — Oznaczać ukończenie pierwszej naprawy | Pierwsza naprawa ma rzeczywisty warunek ukończenia. |
| 9 | TASK-009 — Polityka i obsługa przerwanej zmiany | Gracz rozumie i zachowuje uzgodniony postęp po ubiciu aplikacji. |
| 10 | TASK-010 — Mobilne lifecycle i odzyskiwanie danych | Integralność każdej krytycznej granicy na wspieranym OS. |
| 11 | TASK-011 — Gesty, DPI i fizyka na telefonie | Powtarzalny chwyt, mycie i odkładanie bez przypadkowych akcji. |
| 12 | TASK-012 — Profil mobilny i budżety wydajności | Udokumentowane p95/p99 frame time, pamięć i czas wejścia do kuchni dla konkretnego builda. |
| 13 | TASK-013 — PlayMode jako warunek dystrybucji APK | Nie publikować artefaktu przeznaczonego dla testerów po czerwonym wymaganym teście gry. |
| 14 | TASK-014 — Macierz całej kampanii i zakończeń | Każda wspierana droga prowadzi do sensownego, osiągalnego finału. |
| 15 | TASK-015 — Ekonomia, awanse i tempo kampanii | Uczciwy, zrozumiały rytm bez exploitu i niezamierzonej pułapki długu. |
| 16 | TASK-016 — Przegląd UX i dostępności pierwszej sesji | Nowy gracz sam kończy pierwszą zmianę i rozumie wypłatę. |
| 17 | TASK-017 — Naprawić referencję TMP i preflight assetów | Brak nieuzasadnionych brakujących referencji w kandydacie. |
| 18 | TASK-018 — Jednoznaczne źródło danych i scen | Kolejny autor nie nadpisuje ręcznych poprawek przez odtworzenie scen. |
| 19 | TASK-019 — Kandydat builda z jednoznacznym pochodzeniem | Podpisany lub testowy kandydat powiązany z raportami i konfiguracją. |
| 20 | TASK-020 — Zamknięta alfa/beta z bramkami akceptacji | Jawna decyzja o wejściu do RC poparta testami i znanymi ograniczeniami. |

## 10. Pierwszy konkretny krok

Rozpocząć **TASK-001**: odtworzyć przerwanie grafu `Dialogue_Ch3_2_Tomek` między `tomek3_repay` a `tomek3_end`, zapisać regresję pokazującą podwójne +30 EUR, następnie zaprojektować atomowy zapis konsekwencji z trwałym identyfikatorem zastosowania i kompatybilnością istniejących slotów. Kryterium: dowolna liczba restartów nie powiela nagrody ani kosztu. To propozycja kolejnego zadania programistycznego — audyt nie wprowadził takiego testu ani naprawy.

Równolegle organizacyjnie przygotować TASK-003, aby kolejny agent mógł odtworzyć wyniki bez prywatnego katalogu E:\Dev. Nie zaczynać od nowego rodzaju naczynia lub ogólnej refaktoryzacji.

## 11. Decyzje właściciela

| Decyzja | Dlaczego potrzebna | Rekomendacja do zatwierdzenia |
|---|---|---|
| Platformy i minimum sprzętowe | Definiują realne kryteria wydajności, inputu i koszt QA | Android jako pierwszy odbiór; iOS/Windows tylko z osobnym dowodem |
| Scope kampanii 1.0 | Liczba plików nie definiuje feature complete | Domknąć już istniejące 6 rozdziałów i ustalone zakończenia przed nową mechaniką |
| Przerwana zmiana | Obecnie nie ma odtworzenia aktywnej kuchni | Wybrać snapshot lub jawny checkpoint; nie pozostawiać przypadkowej utraty postępu |
| Presja kontra relaks | Wpływa na normy, awanse, presję szefa i ułatwienia | Ustalić profil główny i przewidywalne zasady relaxed/assist |
| Docelowa długość i ekonomia | Bez tego nie ma mierzalnego celu balansu | Ustalić oczekiwane tempo awansu i margines błędów początkującego |
| Języki i jakość treści/audio | Pakiet Localization nie oznacza gotowej lokalizacji | Najpierw pełna korekta i odbiór podstawowego języka |
| Dystrybucja, zespół, urządzenia i wsparcie | Warunki roadmapy i procesu release | Wskazać właścicieli bramek i zasoby; dopiero potem planować terminy |

## 12. Pakiet przekazania kolejnemu agentowi

1. [GAME_OVERVIEW.md](GAME_OVERVIEW.md) — tożsamość, konfiguracja, architektura, sceny, katalogi i build.
2. [FEATURE_INVENTORY.md](FEATURE_INVENTORY.md) — 51 grup systemów oraz indeks wszystkich 263 C#; status, połączenia i dowody.
3. [TECHNICAL_AUDIT.md](TECHNICAL_AUDIT.md) — 10 problemów, 8 osobnych ryzyk, 10 planów pomiaru i 10 pozycji długu.
4. [GAMEPLAY_REVIEW.md](GAMEPLAY_REVIEW.md) — pętla, balans, UX/audio i 35 ocenionych propozycji.
5. [QA_STATUS.md](QA_STATUS.md) — świeże testy, jawne pominięcia, dokładne komendy, artefakty i granice pewności.
6. [DEVELOPMENT_ROADMAP.md](DEVELOPMENT_ROADMAP.md) — 8 faz i bramki prototyp→1.0.
7. [PRIORITIZED_BACKLOG.md](PRIORITIZED_BACKLOG.md) — 30 zadań z zakresem, zależnościami, akceptacją i testami.
8. [PROJECT_STATUS_REPORT.md](PROJECT_STATUS_REPORT.md) — niniejsza decyzja produktowa i kolejność działań.

Raport odzwierciedla migawkę lokalnego working tree, która już przed audytem zawierała liczne zmiany. Przed implementacją kolejny agent powinien sprawdzić różnice od tej migawki; numery linii i wnioski mogą się zdezaktualizować po zmianie kodu. Indeks plików nie jest twierdzeniem o formalnym dowodzie poprawności każdej linii i kombinacji rozgrywki.

## EXECUTIVE SUMMARY

ZMYWAK ma już rozbudowaną, połączoną rozgrywkę: pracę w kuchni, rozwój postaci, gospodarowanie pieniędzmi i wielorozdziałową historię. Testy potwierdzają szeroki zakres działania, a automatyczna sesja przechodzi między mieszkaniem i zmianami. To solidna baza do zamkniętych testów, lecz nie ma jeszcze wystarczających dowodów gotowości do publikacji.

Najpilniejsze są błędy zapisu i reguł postępu oraz sprawdzenie gry na docelowych telefonach. Największy potencjał leży w dopracowaniu połączenia satysfakcjonującego mycia z historią emigracji. Następna praca powinna zabezpieczyć skutki dialogów przed wielokrotnym naliczeniem, a potem domknąć tutorial, przerwane sesje, kampanię i jakość mobilną. Audyt niczego w grze nie zmienił; przygotował konkretne zadania i kryteria, które pozwalają tę pracę kontynuować.
