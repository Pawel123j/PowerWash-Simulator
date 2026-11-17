# Audyt techniczny

Audyt z 13.10.2026. Badany jest bieżący katalog roboczy, także pliki niezatwierdzone w Git, a nie sam HEAD. Ścieżki dowodów są względem katalogu projektu; numery linii dotyczą tej migawki. Oryginalne skrypty, sceny, ustawienia i assety nie były celowo zmieniane. Testy uruchomiono na odrębnej kopii poza repozytorium.

**Granica pewności:** analiza statyczna potwierdza strukturę i konkretne ścieżki wykonania, test automatyczny potwierdza tylko swoje asercje. Brak pomiarów telefonu, audytu słuchowego, ręcznego przejścia wszystkich wariantów fabuły i testów sklepowych oznacza UNKNOWN w tych obszarach. Nie deklarujemy poznania gry w 100%.

## Najważniejszy wynik

Nie potwierdzono krytycznego błędu kompilacji obecnych źródeł w dostępnym Unity: 761 testów EditMode przeszło na kopii. Potwierdzono natomiast błędy spójności zapisu fabuły i reguł rozgrywki, których ten zestaw nie wykrywa. Priorytetem są integralność danych i wiarygodność następnego builda. Brak awarii testów nie jest dowodem gotowości do publikacji.

## Rejestr problemów

Każdy problem ma osobno określoną pewność. „Potwierdzona ścieżka statyczna” oznacza dowód w aktualnym kodzie i danych, nie twierdzenie o wykonaniu nowego testu na telefonie. Nie zgłoszono Critical na podstawie samych obaw.

### CODE-001 — Konsekwencje dialogu mogą zostać naliczone ponownie po przerwaniu gry

**ID:** CODE-001

**Priorytet:** High

**Pewność:** Potwierdzona ścieżka statyczna; bez nowego testu odtworzenia w grze

**Lokalizacja:** `Assets/_Project/Scripts/Narrative/Dialogue/DialogueSession.cs:168`; `Assets/_Project/Scripts/Narrative/Progress/NarrativeProgressController.cs:145`; `Assets/_Project/Scripts/Meta/Progress/MetaProgressController.cs:282`; `Assets/_Project/Scripts/UI/Screens/StoryBeatTrigger.cs:126`

**Opis problemu:** FinishNode zapisuje flagi/słowa i wywołuje OutcomeApplied podczas rozmowy. Meta zapisuje pieniądze natychmiast. Beat dostaje MarkBeatSeen dopiero po zakończeniu całego grafu. SaveData nie zawiera aktywnego węzła ani identyfikatorów już rozliczonych outcome.

**Potencjalny skutek:** Duplikacja nagród/kosztów, niespójne wybory; dodatkowo wiele zapisów jednego wyboru tworzy okna częściowego commitowania.

**Dowód:** W grafie `Dialogue_Ch3_2_Tomek.asset` węzeł `tomek3_repay` daje +30 EUR i +5 integracji, po czym przechodzi do `tomek3_end` z kolejną wypowiedzią. Zamknięcie procesu po wypłacie, przed końcem beatu, pozostawia go do ponownego odtworzenia. Flaga ch3_tomek_repaid sama nie blokuje FinishNode ani ponownej nagrody. Podobnie koszty mogą naliczyć się wielokrotnie.

**Proponowane rozwiązanie:** Zaprojektować pojedynczą transakcję wyboru/konsekwencji z trwałym identyfikatorem graph+node lub zapisem sesji. Zapisać outcome i jego zastosowanie atomowo w jednym SaveData; zachować migrację v2. Testować przerwania przed/po każdej granicy.

**Szacowana trudność:** Large

### CODE-002 — Gracz nie dowiaduje się o nieudanym zapisie

**ID:** CODE-002

**Priorytet:** High

**Pewność:** Potwierdzony brak obsługi wyniku operacji

**Lokalizacja:** `Assets/_Project/Scripts/Core/Save/SaveSystem.cs:95`; `Assets/_Project/Scripts/Meta/Progress/MetaProgressController.cs:132`; `Assets/_Project/Scripts/Narrative/Progress/NarrativeProgressController.cs:126`; `Assets/_Project/Scripts/Kitchen/Dishwasher/BigDishwasherController.cs:149`; `Assets/_Project/Scripts/UI/Screens/MetaHubScreen.cs:518`

**Opis problemu:** SaveSystem zwraca false i ostrzeżenie tylko w logu. Kontrolery nie pokazują stanu błędu graczowi; narracja i duża maszyna ignorują bool. Meta używa go do pending credit, ale nadal zmienia pamięć i UI. TryDelete także połyka błąd bez zwracania wyniku.

**Potencjalny skutek:** Gracz może wyjść przekonany, że zachował postęp. Po ponownym starcie stan cofa się; odzyskanie wymaga świadomego retry.

**Dowód:** Wypełniony/no-write katalog powoduje false. UI nadal zapewnia, że wszystko zapisuje się na bieżąco, a zmiana przechodzi do kolejnego dnia. Istnieją testy, że awaria dysku nie rzuca wyjątku, ale brak potwierdzenia odzyskania wszystkich danych i komunikatu UI.

**Proponowane rozwiązanie:** Centralny status zapisu + czytelny komunikat i retry. Operacje ekonomiczne/narracyjne zachowują pending state do skutecznego zapisu; kasowanie slotu zwraca rzeczywisty rezultat. Nie blokować głównego wątku w nieskończoność.

**Szacowana trudność:** Medium

### CODE-003 — Poprawny końcowy ruch szorowania jest oceniany jako niewłaściwe narzędzie

**ID:** CODE-003

**Priorytet:** Medium

**Pewność:** Potwierdzona kolejność wywołań

**Lokalizacja:** `Assets/_Project/Scripts/Kitchen/Dishes/DishController.cs:729`; `Assets/_Project/Scripts/Kitchen/Shifts/ShiftController.cs:724`; `Assets/_Project/Scripts/Kitchen/Dishes/DishInstance.cs:104`

**Opis problemu:** ScrubStroke usuwa brud i może ustawić ReadyForRack, potem RaiseScrubbing wywołuje ShiftController.OnScrubbing. Tam ToolSuits sprawdza już pozostały brud. Po usunięciu ostatniej warstwy nie znajduje warstwy >0 i zwraca false.

**Potencjalny skutek:** Zaniżona WashAccuracy i wynik mimo poprawnego gestu. Zwykle mała strata, lecz powtarzana i zależna od czasu klatki.

**Dowód:** Ścieżka: skuteczne narzędzie → ostatni fragment znika → zdarzenie Scrubbing → żadna warstwa nie pozostaje → _wrongToolSeconds += deltaTime. Analogiczna pomyłka może wystąpić przy zmianie rodzaju brudu.

**Proponowane rozwiązanie:** Klasyfikować poprawność przed mutacją maski; przekazać wynik w zdarzeniu albo wynik ScrubStroke. Test ostatniej warstwy i przejścia między warstwami przy kilku deltaTime.

**Szacowana trudność:** Small

### CODE-004 — Kariera zalicza inspekcję po natychmiastowym wyjściu

**ID:** CODE-004

**Priorytet:** Medium

**Pewność:** Potwierdzona ścieżka statyczna

**Lokalizacja:** `Assets/_Project/Scripts/Kitchen/Shifts/ShiftController.cs:750`; `Assets/_Project/Scripts/Kitchen/Shifts/ShiftController.cs:817`; `Assets/_Project/Scripts/Core/Data/Career.cs:41`

**Opis problemu:** InspectionPassed zależy od braku stłuczek i zawalenia, nie czasu pracy. CareerRecord.Record zwiększa inspectionsPassed dla wyniku Inspection+Passed bez sprawdzania elapsed/planned. W tej samej metodzie cleanShifts ma warunek pełnej zmiany.

**Potencjalny skutek:** Obejście wymagań awansu i niewiarygodny licznik inspekcji.

**Dowód:** Początek inspekcji, pauza, EndShiftEarly: brak stłuczek → InspectionPassed true → trwały rekord +1. Mnożnik udziału czasu ogranicza pieniądze specjalne, ale nie rekord kariery.

**Proponowane rozwiązanie:** Jednoznacznie określić zaliczenie inspekcji: np. pełny wymagany czas i brak błędów. Przekazać rozstrzygnięcie do płacy, UI i kariery spójnie.

**Szacowana trudność:** Small

### CODE-005 — Przerwana pierwsza lekcja nie wraca w następnym dniu

**ID:** CODE-005

**Priorytet:** Medium

**Pewność:** Potwierdzona ścieżka statyczna; decyzja UX potrzebna

**Lokalizacja:** `Assets/_Project/Scripts/Kitchen/Shifts/ShiftController.cs:330`; `Assets/_Project/Scripts/Kitchen/Shifts/ShiftController.cs:750`; `Assets/_Project/Scripts/Meta/Progress/LifeLedger.cs:214`

**Opis problemu:** TutorialActive wymaga Day == 1 oraz braku znacznika lekcji. Pauza pozwala skończyć zmianę, gdy trwa TutorialHold; CompleteShift przesuwa dzień. W dniu 2 znacznik może nadal nie istnieć, lecz warunek dnia wyklucza lekcję.

**Potencjalny skutek:** Gracz może ominąć jedyną prowadzoną naukę nie rozumiejąc konsekwencji, bez prostego powrotu.

**Dowód:** Na świeżym slocie wejść do pierwszej kuchni, otworzyć pauzę i zakończyć przed myciem pierwszego talerza. Następne wejście ma plan dnia 2.

**Proponowane rozwiązanie:** Umożliwić powrót do niedokończonej lekcji lub jawny przycisk pominięcia i ponowienia. Zdefiniować, czy przerwana lekcja zużywa dzień.

**Szacowana trudność:** Medium

### CODE-006 — Sam fakt awarii kończy obowiązek pierwszej naprawy

**ID:** CODE-006

**Priorytet:** Medium

**Pewność:** Potwierdzona niezgodność implementacji z deklaracją nauki

**Lokalizacja:** `Assets/_Project/Scripts/Meta/Progress/LifeLedger.cs:206`; `Assets/_Project/Scripts/Core/Data/ShiftSchedule.cs:93`; `Assets/_Project/Scripts/Kitchen/Dishwasher/DishwasherController.cs:109`

**Opis problemu:** LifeLedger ustawia BreakdownTutorialDone na podstawie result.Breakdowns > 0. To licznik wystąpień awarii; nie ma w ShiftResult odpowiedniego licznika ukończonych napraw.

**Potencjalny skutek:** Nauka naprawy może nigdy się nie odbyć; późniejsze awarie już losowe.

**Dowód:** Wyjście podczas pierwszej, nadal nierozwiązanej awarii ustawia flagę i usuwa gwarancję scenariuszowej awarii następnego dnia. README mówi o pierwszej naprawie, a nie samej awarii.

**Proponowane rozwiązanie:** Oddzielić poznanie awarii od ukończenia naprawy; zapisać flagę przy OnRepaired i przenieść ją do trwałego stanu. Uzgodnić intencję projektową przed zmianą.

**Szacowana trudność:** Small

### CODE-007 — Wielokrotny synchroniczny zapis w jednej klatce dialogu

**ID:** CODE-007

**Priorytet:** Medium

**Pewność:** Potwierdzona konstrukcja; wpływ wymaga pomiaru

**Lokalizacja:** `Assets/_Project/Scripts/Narrative/Progress/NarrativeProgressController.cs:129`; `Assets/_Project/Scripts/Core/Save/SaveSystem.cs:109`

**Opis problemu:** Każde LearnWord, UnlockSlang i SetFlag zapisuje cały JSON; outcome meta robi kolejny odczyt i zapis. Wszystko dzieje się w głównym wątku.

**Potencjalny skutek:** Możliwe zacięcia i nadmierne operacje dyskowe na wolnym telefonie; nie przypisujemy im niezmierzonej liczby ms.

**Dowód:** FinishNode iteruje po listach nagród. Z każdego elementu idzie read-modify-write; nie ma batchowania całego węzła. Jest to także przyczyna długu transakcyjnego CODE-001.

**Proponowane rozwiązanie:** Batchowanie transakcji po naprawie CODE-001; profilować czas JSON, odczytu, replace i UI. Nie przenosić Unity API do pracownika bez rozdzielenia danych.

**Szacowana trudność:** Medium

### ASSET-001 — Brak domyślnego sprite asset TMP

**ID:** ASSET-001

**Priorytet:** Low

**Pewność:** Potwierdzone brakujące GUID; efekt wizualny nieodtworzony

**Lokalizacja:** `Assets/TextMesh Pro/Resources/TMP Settings.asset:39`

**Opis problemu:** m_defaultSpriteAsset wskazuje GUID c41005c129ba4d66911b75229fd70b45, którego nie ma w Assets ani lokalnym PackageCache.

**Potencjalny skutek:** Fallback sprite/emoji może być pusty; nie jest to dowód, że podstawowy polski tekst nie działa.

**Dowód:** Zbudowano mapę wszystkich .meta i referencji. Pozostałe niezerowe GUID mają rozwiązaną ścieżkę albo są wbudowanymi identyfikatorami Unity.

**Proponowane rozwiązanie:** Sprawdzić, czy gra używa sprite fallback, następnie przypisać poprawny asset albo świadomie usunąć zbędne odwołanie. Testy tekstu i symboli na urządzeniu.

**Szacowana trudność:** Small

### BUILD-001 — Paczka Android nie czeka na wynik PlayMode

**ID:** BUILD-001

**Priorytet:** High

**Pewność:** Potwierdzona luka bramki CI

**Lokalizacja:** `.github/workflows/android.yml` — job android, needs: editmode-tests

**Opis problemu:** Workflow jawnie uruchamia PlayMode w osobnym jobie, ale APK zależy tylko od EditMode. Komentarz tłumaczy brak udowodnionej niezawodności software renderingu.

**Potencjalny skutek:** Możliwość dystrybucji artefaktu z popsutym sterowaniem/sceną mimo zielonego zestawu jednostkowego.

**Dowód:** Awaria scenowego joba nie zatrzymuje budowy APK. Sam fakt napisania workflow nie dowodzi jego uruchomienia na zdalnym serwerze.

**Proponowane rozwiązanie:** Ustabilizować scenowy zestaw na obsługiwanym runnerze i dodać zależność dla kandydatów wydania; oddzielić eksperymentalny APK od release gate.

**Szacowana trudność:** Medium

### QA-001 — Główny skrypt wcześniejszej regresji i część dowodów są poza repo

**ID:** QA-001

**Priorytet:** High

**Pewność:** Potwierdzona luka odtwarzalności

**Lokalizacja:** `QA_REPORT.md:596`; `E:/Dev/scripts/final_regression.ps1`

**Opis problemu:** Raport odwołuje się do E:/Dev/scripts i E:/Dev/…; sprawdzane final_regression.ps1 i final_regression.txt są niedostępne w tym środowisku.

**Potencjalny skutek:** Następny agent nie ma kompletnej procedury i pochodzenia wyników; nie wolno sumować starych rezultatów jako jednego bieżącego przebiegu.

**Dowód:** Bieżące źródła mają testy; brak zewnętrznego runnera nie oznacza braku testów. Świeży EditMode odtworzono bez tego skryptu. Nie można jednak zweryfikować wszystkich dawnych powtórzeń i screenshotów z deklarowanego miejsca.

**Proponowane rozwiązanie:** Przenieść parametryzowany runner, filtry Explicit, zapis manifestu i publikację wyników do repo; instrukcja bez zależności od dysku E.

**Szacowana trudność:** Medium

## Ryzyka, których nie należy przedstawiać jako odtworzonych awarii

| ID | Ryzyko i dowód | Wpływ | Sposób weryfikacji |
|---|---|---|---|
| RISK-001 | SaveData nie zapisuje aktywnej kuchni: timer, stos, naczynia, szef, normalna zmywarka i bieżący wynik są stanem sceny | High: po OS kill możliwa utrata pracy trwającej zmiany; funkcja resume MISSING, polityka do decyzji | Kill w różnych fazach na telefonie, sprawdzić powrót i komunikat; zwykła pauza nie odtwarza tego |
| RISK-002 | SceneLoader.LoadRoutine nie ma try/finally obejmującego LoadSceneAsync i callback Loaded | Medium: wyjątek przy złej nazwie sceny/subskrybencie może pozostawić IsLoading/overlay; w bieżących Build Settings wszystkie nazwy istnieją | Fault injection na kopii z błędną nazwą; nie zgłaszamy normalnego ładowania jako BROKEN |
| RISK-003 | ServiceLocator.Register nadpisuje wpis, Unregister usuwa po typie; bootstrap nie broni się przed duplikacją | Medium przy rozbudowie/additive load; brak dowodu normalnego podwójnego Boot | Test celowo podwójnego bootstrapu; później jawne ownership usług |
| RISK-004 | DirtPrewarm.Clear czyści słownik, nie anuluje Task; fallback może równolegle liczyć drugą maskę | Medium CPU/RAM przy szybkim przechodzeniu scen; brak dowodu wyścigu zapisów gry | Zliczyć pending jobs/miss/hit i pamięć przy 50 przejściach |
| RISK-005 | SaveValidation naprawia wybrane pola, ale nie wszystkie zagnieżdżone liczniki kariery/enum aktywności; rozmiar JSON/list nie ma jawnego limitu | Medium dla ekstremalnie uszkodzonych/importowanych zapisów; nie twierdzimy, że obecne UI generuje takie dane | Fuzz struktury i duże listy, limity, porównanie stanu przed/po |
| RISK-006 | Duża maszyna opiera się na DateTime.UtcNow; fallback dla dat daleko w przyszłości nie usuwa zależności od zegara | Low/Medium, możliwy skip czasu lub przedłużenie cyklu w grze offline | Cofnięcie/przesunięcie zegara, restart; decyzja czy anti-cheat w ogóle potrzebny |
| RISK-007 | Zmiana jakości mutuje aktywny URP asset i renderer feature, a nie jego kopię | Medium dla pracy w Editorze, utrwalania niezamierzonych ustawień; w playerze to wspólny stan runtime | Enter/exit Play z diffem Settings w bezpiecznej kopii; rozważyć runtime clone |
| RISK-008 | Słabe dane o buildzie docelowym, AAB podpisie, iOS i produkcyjnych shader variants | High gotowości wydania, UNKNOWN platformowy | Powtarzalny build z manifestem i smoke każdej deklarowanej platformy |

## Architekturę sprawdzono w następujących obszarach

| Obszar | Obserwacja | Rekomendacja |
|---|---|---|
| Granice modułów | Pięć asmdef runtime, Kitchen/Meta/Narrative nie zależą wzajemnie; UI integruje je przez Core | Zachować podział; interfejsy transakcji i usług zamiast dodawania zależności do UI |
| Stan gry | 4 stany, persistent controllers, restart slotu i LifeGeneration | Rozdzielić trwały stan od widoków, określić save ownership |
| NullReference | Wiele serializowanych referencji z ochroną, brak zerowych m_Script; pola null bywają opcjonalne | Nie oznaczać każdego null jako błędu. Dodać preflight wymaganych pól, nie zastępować poprawności samym ?. |
| Sceny | Async load, kolejka jednego następnego żądania, brakujące nazwy sprawdzane preflight | Przy następnych stanach dodać rollback ładowania i powiązanie state enter z zakończeniem load |
| Eventy | W głównych ścieżkach widoczne pary subscribe/unsubscribe; globalne KitchenFeedbackEvents i event channel SO | Test reload/slot switch, zwłaszcza gdy powstają nowe singletony; nie stwierdzono udowodnionego wycieku eventów |
| Wyjątki | Zapis obsługuje typowe błędy dysku, haptyka degradację; UI nie dostaje rezultatu zapisu | CODE-002 zamiast ogólnego try/catch wszystkiego |
| Wątki | Jedyny znaleziony Task.Run w runtime to DirtPrewarm; przekazuje głównie zwykłe dane | Zweryfikować wszystkie użyte API maski w IL2CPP i zamykanie zadań |
| Update | 43 metody Update, 1 FixedUpdate, 5 LateUpdate w indeksie runtime | Same liczby nie są dowodem wolnej gry; mierzyć aktywne obiekty i koszt CPU |
| Fizyka | FixedUpdate DishController stabilizuje obiekty; ustawienia solvera per naczynie 12/4 | Profilować liczbę aktywnych bodies/contacts zamiast bezpodstawnie przenosić wszystko do FixedUpdate |
| UI | TextScaler reaguje na event; HUD ma cache części wartości, nadal rozbudowany polling | Mierzyć Canvas rebuild i unikać niezmienionych zapisów właściwości |
| Alokacje | ChefOrderController.FindObjectsByType przy wyborze i co sekundę; IsTappableFrom tworzy tablicę próbek | Rejestr żywych naczyń i cache tylko jeśli profiler potwierdzi istotność |
| Nieużywany kod | Nie wykryto pewnego kompletnego porzuconego subsystemu; są pola pomocnicze/dawne ścieżki i API wyłącznie dla testów | Analiza call graph musi uwzględniać Unity messages, reflection, generatory i resource loads |
| Duplikacja | UI budowane proceduralnie w obszernych generatorach; read-modify-write powtarza się w kilku kontrolerach | Najpierw wspólna transakcja, następnie komponenty authoringu i wspólne kontrolki |
| Rozmiar klas | DishController 1200, ShiftHUD 1033, ShiftController 826, MetaHubScreen 739, LifeLedger 708 linii | Refaktoryzować małymi kontraktami po testach, nie przepisywać gry przed walidacją |

`Cleanliness01` używa LINQ/ToList (`Assets/_Project/Scripts/Kitchen/Dishes/DishInstance.cs:89`), ale brak dowodu jego użycia w gorącej ścieżce produktu. Nie przypisujemy mu automatycznie istotnego kosztu GC. Stosowane pule mają limit per rodzaj, a materiały naczyń są niszczone w OnDestroy; nie wykryto na tej podstawie potwierdzonego wycieku pamięci.

## Sceny, zasoby i pipeline

Przejrzano wszystkie 8 plików scen, ich typy komponentów, GUID oraz lokalne fileID; cztery sceny produkcyjne mają poprawne nazwy i kolejność w Build Settings. Brak prefabów i wariantów; naczynia i część FX powstają przez Create/AddComponent. Brak AnimatorController/AnimationClip; ruch ręki, przejścia paneli, naczynia i efekty realizuje kod. Brak VFX Graph i AudioMixer assetów; jest ParticleSystem i ręczny miks AudioSource. Nie oznacza to brakującego dźwięku/animacji.

Kuchnia ma zapisanych 175 MeshRenderer, 175 MeshFilter, 2 ParticleSystem, 2 Light i 1 Camera; MainMenu 104 rendererów, MetaHub 104. Są także obiekty tworzone runtime. To **nie są** liczby draw calls, aktywnych obiektów ani koszt GPU. Nie stwierdzono liczbowo złego batchingu. URP ma SRP Batcher włączony, dynamic batching wyłączony, HDR oraz cienie głównego światła 2048. QualityManager steruje render scale 0,75/0,9/1, MSAA 1/2/4, shadow distance 26/28/30 i SSAO tylko High. Low ustawia limit 30 FPS, pozostałe 60 w kuchni; menu/pauza 30. Nie jest to pomiar osiąganej wydajności.

Physics: grawitacja (0,-9,81,0), nominalny Fixed Timestep około 0,02 s, max delta 0,3333 s, AutoSyncTransforms wyłączony, wszystkie bity globalnej collision matrix włączone. Naczynia ustawiają własne parametry. Brak wydzielonych filtrów warstw w głównym Physics.Raycast (max distance 100); istotność zależy od profilerowego kosztu zapytań.

Skan referencji tylko wewnątrz Assets wskazał **14 kandydatów bez przychodzących GUID**, ale większość to fałszywe alarmy: URP jest w ProjectSettings, TMP settings i banki/placeholdery/katalog ładowane z Resources. Pozostają do sprawdzenia np. Character_kuba.asset, Glow.png i niektóre alternatywne materiały TMP. Nie zalecamy ich usuwać bez BuildReport, skanu string loads i sprawdzenia generatorów. Nieużywane assety nie zostały usunięte.

Brak unresolved GUID skryptów nie potwierdza ważności każdego fileID wewnątrz importowanego assetu, poprawności lightmap po rebake ani kompatybilności Hand/Dish/Glass shaderów z docelowym GPU. To zadania testów renderowania i asset validation w Unity. Archiwalne obrazy `Logs/grafika_runda2_kuchnia.png` i `grafika_runda2_pokoj.png` pokazują etap oprawy, nie bieżący release; oceny aktualnego layoutu trzeba oprzeć na nowych capture.

## Wydajność — hipotezy i plan pomiarów

| ID / obszar | Konkretna ścieżka | Przewidywany wpływ (bez fikcyjnych wyników) | Pomiar i warunek decyzji |
|---|---|---|---|
| PERF-01 CPU startu | Shader/resource first use, DirtCanvas/DishDecor, Start/Awake kuchni, audio synth | Możliwe hitches przy pierwszym naczyniu/scenie | StartupTimeline + CPU Timeline, cold i warm start, 10 powtórzeń per telefon |
| PERF-02 CPU/GPU mycia | DishController.RefreshVisual/Upload, WriteColors/WriteNormals, Texture.Apply | CPU zależny od rozdzielczości i GPU upload; inspekcja odrysowuje całą mapę | Profile w 128/192/256, rozdzielić czas obliczeń i upload, koszt klatki p95/p99 |
| PERF-03 I/O | CODE-007 wielokrotne zapisy i lokalny log | Potencjalne przerwy na słabym storage | Znaczniki File IO/JSON, najdłuższy węzeł z nagrodami; batch dopiero z zachowaniem trwałości |
| PERF-04 RAM/GC | maski każdej warstwy, tablice bazowe/glaze, cache tekstur i muzyki | Rosnący working set po pierwszym użyciu typów/quality | Memory Profiler snapshots: menu, szczyt stosu, po 20 zmianach, po zmianie slotu i jakości |
| PERF-05 fizyka/raycast | kontakty stosu, wybór naczyń szefa, Pointer raycast | CPU przy zawaleniu i pełnym stosie | Physics Profiler, aktywne bodies, contacts, koszt selection/CanStillBeFilled; uwzględnić bufor retry |
| PERF-06 GPU/draw calls | stal, szkło, HDR, SSAO High, cienie, ręka, particles | Fill rate/overdraw i przerwania batchy na mobile | Frame Debugger i GPU profiler urządzenia; overdraw szczytu piany i szkła; zweryfikować SRP batches |
| PERF-07 UI | duże scenowe Canvasy, HUD/Settings/TextScaler, teksty dialogów | Rebuild Canvas/layout i alokacje stringów | UI profiler podczas mycia, wyniku i max tekstu; cache z zachowaniem aktualności danych |
| PERF-08 pooling | BigDishwasherController.RefreshMiniatures niszczy i odtwarza całość przy zmianie count | Przy pełnym wsadzie burst alokacji; nie wiemy czy odczuwalny | Profil dołożenia 1. i 40. naczynia; incremental refresh/pool jeśli uzasadnione |
| PERF-09 build size | Localization/Addressables w lockfile, Resources, banki audio i portrety | Rozmiar instalacji/download i pamięć zasobów | BuildReport release, porównać used assets i managed stripping; nie usuwać na podstawie samego manifestu |
| PERF-10 termika | governor, 30/60 caps, brak pomiarów energii/thermal | Możliwe pogorszenie po kilkunastu minutach | 30–60 min ręcznej gry i autoplay na słabym/średnim telefonie; bateria, temperatury, frame pacing |

Aktualne Upload kopiuje zmieniony region do bufora CPU przez NativeArray.Copy, następnie wywołuje Texture2D.Apply. README mówi o wysyłaniu tylko zmienionych wierszy na GPU, ale sam ten kod nie dowodzi częściowego transferu GPU; taki wniosek wymaga pomiaru/API backendu. Nie podajemy arbitralnych FPS, RAM ani draw calls. Wartości ze starego QA_REPORT (m.in. zacięcia startu) pozostają dowodem historycznym na innym środowisku, nie wynikiem tego audytu telefonu.

## Rejestr długu technicznego

| ID | Dług | Wpływ na rozwój | Priorytet / zależność |
|---|---|---|---|
| DEBT-01 | Współdzielony save bez transakcji i jawnego właściciela | Każdy nowy reward/quest zwiększa liczbę okien niespójności | High, CODE-001/002 przed nową fabułą |
| DEBT-02 | Długie klasy kuchni/HUD/huba | Trudne izolowanie zmian gestu, shaderów i punktów | Medium, po zabezpieczeniu testami integracji |
| DEBT-03 | Jednocześnie generatory i wygenerowane sceny/dane jako źródło prawdy | Run All może odtworzyć inne ustawienia lub zetrzeć ręczną poprawkę | High, preflight dry-run/diff i instrukcja authoringu |
| DEBT-04 | Testy zależne od scen, czasu/fizyki i część Explicit poza CI | Pozorna zieleń i regresje touch/layout | High, runner i stabilny smoke |
| DEBT-05 | Historyczna dokumentacja z wieloma nakładającymi się etapami i zewnętrznymi ścieżkami | Nowy agent nie wie co jest aktualne | Medium, niniejsza dokumentacja i manifest następnej paczki |
| DEBT-06 | Niezatwierdzone zmiany, LFS i OneDrive | Brak jednoznacznej rewizji artefaktu; konflikty/cache/Gradle opisane w README | High dla release, snapshot/hashes; żadnego commit/push w audycie |
| DEBT-07 | Teksty w C# i grafach mimo pakietu Localization | Koszt dalszych języków i korekty nazw/formatów | Low przy jednym języku; decyzja zakresu przed migracją |
| DEBT-08 | Device QA i budżety nieudokumentowane na bieżącej paczce | Trudno ocenić cenę dodania kolejnych efektów/typów naczyń | High przed rozbudową grafiki |
| DEBT-09 | Magic numbers progów gestów i trwałe statyki | Nowe rozdzielczości/urządzenia lub lifecycle mogą zmienić zachowanie | Medium; test DPI i restartów |
| DEBT-10 | Krzyżowanie historii i progresji datą/dniem/flagami | Zmiany kolejności beatów i migracje mogą rozjechać harmonogram | Medium; macierz całej kampanii i restartów |

Nie wprowadzono napraw, refaktoryzacji, zmian konfiguracji, usuwania assetów ani commitów/push. Proponowane działania mają własne zadania w `PRIORITIZED_BACKLOG.md`.
