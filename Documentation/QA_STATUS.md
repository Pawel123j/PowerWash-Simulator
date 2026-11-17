# QA, stabilność i zakres dowodów

## Aktualizacja 16.10.2026 — TASK-001

Wdrożono transakcyjny zapis konsekwencji dialogów i checkpointy v3. Na kodzie sprzed poprawki nowa regresja Tomka wykazała **160 zamiast 130 EUR po restarcie**. Końcowy kod: **771/771 EditMode** oraz **42/42 celowanych PlayMode**, zero błędów w tych dwóch przebiegach. Wcześniejszy pełny standardowy PlayMode: 147 PASS, 3 FAIL, 13 SKIP; trzy błędy normalizacji pustego checkpointu naprawiono i potwierdzono w powtórce 42 przypadków. Pełnego PlayMode po ostatniej korekcie ani Explicit nie powtarzano.

Środowisko: izolowana kopia, Unity 6000.6.5f1, Windows, Intel UHD Graphics; oryginalne ustawienia 6000.6.3f1 pozostawiono. Wyniki, lista plików, migracja i ograniczenia: [TASK_001_IMPLEMENTATION.md](TASK_001_IMPLEMENTATION.md). Dowody z hashami: `Logs/QA/TASK-001-20261016/manifest.json`. Postęp masterplanu: [MASTERPLAN_PROGRESS.md](MASTERPLAN_PROGRESS.md). Poniższy audyt z 13.10 pozostaje dokumentem historycznym.

Audyt z 13.10.2026. Badany jest bieżący katalog roboczy, także pliki niezatwierdzone w Git, a nie sam HEAD. Ścieżki dowodów są względem katalogu projektu; numery linii dotyczą tej migawki. Oryginalne skrypty, sceny, ustawienia i assety nie były celowo zmieniane. Testy uruchomiono na odrębnej kopii poza repozytorium.

**Granica pewności:** analiza statyczna potwierdza strukturę i konkretne ścieżki wykonania, test automatyczny potwierdza tylko swoje asercje. Brak pomiarów telefonu, audytu słuchowego, ręcznego przejścia wszystkich wariantów fabuły i testów sklepowych oznacza UNKNOWN w tych obszarach. Nie deklarujemy poznania gry w 100%.

## Świeże wyniki — 13.10.2026

| Uruchomienie | PASS | FAIL | SKIP | Start UTC → koniec UTC | Czas suite z XML | Dowód |
|---|---:|---:|---:|---|---:|---|
| EditMode | 761 | 0 | 0 | 2026-10-13 11:08:58Z → 2026-10-13 11:11:28Z | 150.557 s | `audit-editmode.xml` + `audit-editmode-final.log` |
| PlayMode standard | 132 | 0 | 13 | 2026-10-13 20:19:33Z → 2026-10-13 20:31:19Z | 705.259 s | `audit-playmode.xml` + `audit-playmode.log` |
| PlayMode Explicit: tour + monkey | 3 | 0 | 0 | 2026-10-13 20:33:43Z → 2026-10-13 20:44:29Z | 645.848 s | `audit-explicit.xml` + `audit-explicit.log` |

Łącznie **896 wykonanych przypadków PASS, 0 FAIL**: 761 EditMode + 132 zwykłe PlayMode + 3 wybrane Explicit. W EditMode jeden przypadek to pakietowy `AddressableAssets.DocExampleCode.TestStub.RequiredTest`; pozostałe 760 należą do projektu. Standardowy przebieg wykrył 145 PlayMode, w tym 13 Explicit. Osobno wykonano 3 z tych 13, zatem **10 przypadków Explicit pozostało niewykonanych w tym audycie**. Nie doliczamy ich do zaliczonych. Nie wykonano pomiaru procentowego pokrycia linii/gałęzi.

Zielony suite potwierdza wiele modeli, integracji i ścieżek scen. Nie wykrywa automatycznie wszystkich błędów ekonomii i przerwania narracji opisanych w audycie. Nie jest certyfikacją mobile ani wynikiem kompletnego przejścia wszystkich rozdziałów przez człowieka.

## Środowisko, izolacja i nienaruszalność oryginału

- Oryginał deklaruje Unity **6000.6.3f1**. Dostępny i użyty silnik: **6000.6.5f1**, Windows 11 x64. Wynik dotyczy tej wersji patch, bez dowodu równoważności z 6000.6.3f1.
- EditMode: `-batchmode -nographics`. PlayMode i Explicit: `-batchmode` z grafiką D3D11, renderer **Intel UHD Graphics**, według logu. To nie telefon ani historyczny RTX 3070.
- Skopiowano Assets, Packages i ProjectSettings oraz lokalny cache pakietów do niezależnego `audit-project`. Testy wykorzystują istniejący TestSaveSandbox/DirectoryOverride i testowe klucze PlayerPrefs. Nie uruchamiano generatora scen ani builda w oryginalnym katalogu.
- SHA-256 porównano dla **1718 istniejących plików** Assets/Packages/ProjectSettings/Tools/.github i głównych dokumentów: **0 zmienionych, 0 brakujących**. Raporty są nowymi plikami Documentation. Nie wykonano commit/push, usuwania plików ani poprawek gry.
- Unity zmieniło wyłącznie w kopii dwa spośród porównywanych plików: `ProjectSettings/ProjectVersion.txt` oraz `Assets/UniversalRenderPipelineGlobalSettings.asset` (wyczyszczenie serializowanej `m_RuntimeSettings.m_List`). Pozostałe źródła i porównane assety zgodne z baseline. Testy i obrazy należy interpretować jako uruchomienie kopii zaimportowanej przez nowszy patch; nie jako bezwarunkowe potwierdzenie oryginalnych ustawień renderingu w 6000.6.3f1.
- Pierwsza próba miała problem środowiska z IPC/licencją; kolejna trafiła na zajęty projekt kopii. Zakończono wyłącznie zweryfikowany proces audytowej kopii, a następne przebiegi zakończyły się XML. Nie klasyfikujemy prób bez kompletnego wyniku jako niezaliczonych testów gry.

Katalog wszystkich surowych dowodów: `C:\Users\pawel\.codex\visualizations\2026\10\13\01a134db-ba5e-7bc1-8e38-e18919545d18`. Baseline: `audit_baseline.json`; końcowe porównanie: `audit_integrity.json`. Materiały pozostają lokalnymi artefaktami tego audytu poza repozytorium; przeniesienie samej Documentation nie przenosi XML/logów/obrazów. TASK-003 ma uczynić kolejne paczki dowodów przenośnymi i samowystarczalnymi.

## Dokładne polecenia udanych uruchomień

Poniższe komendy PowerShell odpowiadają argumentom zapisanym przez Unity w logach. `-projectPath` wskazuje kopię. Nie używać ścieżki oryginału przy odtwarzaniu audytu do odczytu. Start-Process uruchamiano z WindowStyle Hidden i Wait; w środowisku narzędzi potrzebny był dostęp do procesu licencyjnego poza sandboxem.

```powershell
$auditUnity = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
$auditRoot = 'C:\Users\pawel\.codex\visualizations\2026\10\13\01a134db-ba5e-7bc1-8e38-e18919545d18'

$auditProcess = Start-Process -FilePath $auditUnity -ArgumentList @(
  '-batchmode', '-nographics', '-projectPath', "$auditRoot\audit-project",
  '-runTests', '-testPlatform', 'EditMode',
  '-testResults', "$auditRoot\audit-editmode.xml",
  '-logFile', "$auditRoot\audit-editmode-final.log"
) -WindowStyle Hidden -PassThru -Wait
$auditProcess.ExitCode

$auditProcess = Start-Process -FilePath $auditUnity -ArgumentList @(
  '-batchmode', '-projectPath', "$auditRoot\audit-project",
  '-runTests', '-testPlatform', 'PlayMode',
  '-testResults', "$auditRoot\audit-playmode.xml",
  '-logFile', "$auditRoot\audit-playmode.log"
) -WindowStyle Hidden -PassThru -Wait
$auditProcess.ExitCode

$auditProcess = Start-Process -FilePath $auditUnity -ArgumentList @(
  '-batchmode', '-projectPath', "$auditRoot\audit-project",
  '-runTests', '-testPlatform', 'PlayMode',
  '-testFilter', 'Tour_TheWholeGame_InPictures;PWS022_MonkeyTester_RandomTouchesToolSpamAndPauses_BreakNothing',
  '-testResults', "$auditRoot\audit-explicit.xml",
  '-logFile', "$auditRoot\audit-explicit.log"
) -WindowStyle Hidden -PassThru -Wait
$auditProcess.ExitCode
```

Nie użyto `-quit`, aby nie przerwać runnera przed zakończeniem. Decydującym dowodem są końcowe XML i brak failure w ich przypadkach; sam start procesu lub napis Passed w historycznej dokumentacji nie wystarcza.

## Hash wyników i dowodów

| Plik | SHA-256 |
|---|---|
| `audit-editmode.xml` | `020feda79457393477868aa571418157ed019ffa399ff374a29168c5c64d3275` |
| `audit-editmode-final.log` | `68a37d0efa093c682814f34a6a3658db3357b2fae87979de25b989cfba4f3181` |
| `audit-playmode.xml` | `64b6c519e9b2f1a8de757a96ba08e993892941c9115de42f4237de6642eca70a` |
| `audit-playmode.log` | `ffb8638009af059dc3698ddd4949c2ec7cb4b5929a08083bec6f53987ffb32cd` |
| `audit-explicit.xml` | `2be92547f0e25c1ce3333859dcf76651039913402d43c4379d22e2c9a9560e19` |
| `audit-explicit.log` | `8c1597f9535284e26c052bf156cb33ab2a11979c184bff2b29bfea4862e36387` |
| `audit_baseline.json` | `70940d9942a0dffd681b91f99896e5ed750c7a855f66285871ca059577916385` |

## Wyniki poszczególnych fixture

### EditMode

| Fixture | PASS | FAIL | SKIP |
|---|---:|---:|---:|
| TestStub | 1 | 0 | 0 |
| BugfixRegressionTests | 32 | 0 | 0 |
| CareerTests | 6 | 0 | 0 |
| ColorBlindPaletteTests | 3 | 0 | 0 |
| DialogueSessionTests | 9 | 0 | 0 |
| DirtCanvasTests | 14 | 0 | 0 |
| DirtMaskPercentTests | 6 | 0 | 0 |
| DishInstanceTests | 12 | 0 | 0 |
| DishMeshFactoryTests | 3 | 0 | 0 |
| DishPhysicsTests | 5 | 0 | 0 |
| DishwasherRepairTests | 13 | 0 | 0 |
| EconomySimulationTests | 4 | 0 | 0 |
| ExchangeRateServiceTests | 4 | 0 | 0 |
| ExchangeRateTableTests | 2 | 0 | 0 |
| FatigueTests | 3 | 0 | 0 |
| GameplayLogTests | 3 | 0 | 0 |
| GameSoundsTests | 81 | 0 | 0 |
| GlassInspectionTests | 8 | 0 | 0 |
| HandWearTests | 7 | 0 | 0 |
| HapticsTests | 2 | 0 | 0 |
| LifeLedgerTests | 30 | 0 | 0 |
| LongCycleDishwasherTests | 8 | 0 | 0 |
| MechanicUnlocksTests | 6 | 0 | 0 |
| PerformanceGuardTests | 6 | 0 | 0 |
| PlayerStatsControllerTests | 3 | 0 | 0 |
| PolishPluralTests | 35 | 0 | 0 |
| PrepRoundTests | 7 | 0 | 0 |
| ProceduralSoundsTests | 52 | 0 | 0 |
| ProjectConfigurationTests | 6 | 0 | 0 |
| QaAuditTests | 11 | 0 | 0 |
| QaExploratoryTests | 15 | 0 | 0 |
| QualityGovernorTests | 7 | 0 | 0 |
| RackLayoutTests | 10 | 0 | 0 |
| RadioTests | 14 | 0 | 0 |
| RecordedSoundsTests | 57 | 0 | 0 |
| SaveCorruptionTests | 105 | 0 | 0 |
| SaveSystemTests | 15 | 0 | 0 |
| ServiceWavesTests | 4 | 0 | 0 |
| ShiftResultCalculatorTests | 16 | 0 | 0 |
| ShiftScheduleTests | 11 | 0 | 0 |
| ShiftScoringTests | 18 | 0 | 0 |
| ShiftTimerTests | 3 | 0 | 0 |
| ShuffleBagTests | 6 | 0 | 0 |
| SlangCollectionControllerTests | 2 | 0 | 0 |
| SpecialShiftTests | 8 | 0 | 0 |
| StartFromAnySceneTests | 1 | 0 | 0 |
| StartupTimelineTests | 2 | 0 | 0 |
| StoryContentTests("Chapter_ch1.asset") | 12 | 0 | 0 |
| StoryContentTests("Chapter_ch2.asset") | 12 | 0 | 0 |
| StoryContentTests("Chapter_ch3.asset") | 12 | 0 | 0 |
| StoryContentTests("Chapter_ch4.asset") | 12 | 0 | 0 |
| StoryContentTests("Chapter_ch5.asset") | 12 | 0 | 0 |
| StoryContentTests("Chapter_ch6.asset") | 12 | 0 | 0 |
| UiPolishRegressionTests | 2 | 0 | 0 |
| VocabularyTrackerTests | 3 | 0 | 0 |
| WeeklyBudgetControllerTests | 2 | 0 | 0 |
| WordBlurFormatterTests | 6 | 0 | 0 |

### PlayMode standard

| Fixture | PASS | FAIL | SKIP |
|---|---:|---:|---:|
| AppLifecyclePlayModeTests | 2 | 0 | 0 |
| BackGesturePlayModeTests | 2 | 0 | 0 |
| BugfixRegressionPlayModeTests | 14 | 0 | 0 |
| CityMapPlayModeTests | 2 | 0 | 0 |
| GameTourPlayModeTests | 0 | 0 | 1 |
| HandWearPlayModeTests | 1 | 0 | 0 |
| KitchenRadioPlayModeTests | 1 | 0 | 0 |
| KitchenShiftPlayModeTests | 34 | 0 | 6 |
| LayoutPlayModeTests | 5 | 0 | 0 |
| MenuScenesPlayModeTests | 3 | 0 | 1 |
| PrepBoardPlayModeTests | 2 | 0 | 0 |
| ProgressSaveRoundTripTests | 1 | 0 | 0 |
| QaExploratoryPlayModeTests | 8 | 0 | 3 |
| RoomDressingPlayModeTests | 2 | 0 | 0 |
| SaveCorruptionPlayModeTests | 9 | 0 | 0 |
| SaveSlotsPlayModeTests | 4 | 0 | 0 |
| ScenesAndSettingsPlayModeTests | 4 | 0 | 0 |
| SoakPlayModeTests | 1 | 0 | 1 |
| SpecialShiftPlayModeTests | 3 | 0 | 0 |
| StartupTimelinePlayModeTests | 3 | 0 | 0 |
| StoryPlayModeTests | 8 | 0 | 0 |
| UiPolishPlayModeTests | 22 | 0 | 0 |
| WordBlurPlayModeTests | 1 | 0 | 1 |

### PlayMode Explicit: tour + monkey

| Fixture | PASS | FAIL | SKIP |
|---|---:|---:|---:|
| GameTourPlayModeTests | 1 | 0 | 0 |
| QaExploratoryPlayModeTests | 2 | 0 | 0 |

## 13 przypadków Explicit: jawny stan

`Explicit` oznacza uruchamianie na żądanie, nie samo w sobie niestabilność. Część to podglądy artystyczne, część długie lub losowe scenariusze. Zwykłe `-runTests PlayMode` ich nie pokrywa.

| Przypadek | Standard suite | Oddzielne uruchomienie w audycie | Znaczenie |
|---|---|---|---|
| `GameTourPlayModeTests.Tour_TheWholeGame_InPictures` | SKIP Explicit | Passed | Automatyczne zdjęcia; asercja wymaga jedynie shot > 0 |
| `KitchenShiftPlayModeTests.ArtPreview_BigDishwasher_Full` | SKIP Explicit | NIEWYKONANY | Podgląd, nie pełny test gry |
| `KitchenShiftPlayModeTests.ArtPreview_EachDish_DirtyScrubbedAndClean` | SKIP Explicit | NIEWYKONANY | Podgląd, nie pełny test gry |
| `KitchenShiftPlayModeTests.ArtPreview_HandsFreshAndWorn` | SKIP Explicit | NIEWYKONANY | Podgląd, nie pełny test gry |
| `KitchenShiftPlayModeTests.ArtPreview_Leftovers` | SKIP Explicit | NIEWYKONANY | Podgląd, nie pełny test gry |
| `KitchenShiftPlayModeTests.ArtPreview_LegendaryTray` | SKIP Explicit | NIEWYKONANY | Podgląd, nie pełny test gry |
| `KitchenShiftPlayModeTests.ArtPreview_ScraperAndSpray` | SKIP Explicit | NIEWYKONANY | Podgląd, nie pełny test gry |
| `MenuScenesPlayModeTests.ArtPreview_Room_AsACarGoesBy` | SKIP Explicit | NIEWYKONANY | Podgląd, nie pełny test gry |
| `QaExploratoryPlayModeTests.PWS022_MonkeyTester_RandomTouchesToolSpamAndPauses_BreakNothing(11)` | SKIP Explicit | Passed | Losowe interakcje i pauza; seed stały |
| `QaExploratoryPlayModeTests.PWS022_MonkeyTester_RandomTouchesToolSpamAndPauses_BreakNothing(22)` | SKIP Explicit | Passed | Losowe interakcje i pauza; seed stały |
| `QaExploratoryPlayModeTests.PWS024_TenMinuteSoak_StartedFromTheHub_ShiftAfterShift` | SKIP Explicit | NIEWYKONANY | Długi soak niewykonany ponownie |
| `SoakPlayModeTests.Soak_ShiftAfterShift_WithoutErrorsLeaksOrSlowdown` | SKIP Explicit | NIEWYKONANY | Długi soak niewykonany ponownie |
| `WordBlurPlayModeTests.ArtPreview_BlurStrengths` | SKIP Explicit | NIEWYKONANY | Podgląd, nie pełny test gry |

## Bieżący tour i inspekcja wizualna

`GameTourPlayModeTests` wygenerował **115 obrazów** w `audit-project/Logs/Screenshots/tour_*.png`. Log raportuje **2 zmiany, 18 naczyń w koszu, 0 napraw, last problem: none**. Autoplay korzysta z istniejącej ścieżki inputu; to dodatkowy dowód połączenia scen i wczesnej pętli, nie ręczny playtest ani pełna kampania. Sam test tour asertuje tylko dodatnią liczbę zdjęć — jego zielony status nie gwarantuje zaliczenia zmiany lub naprawy. Liczniki tour są odrębnym dowodem z logu.

Obejrzano 7 próbek: `tour_000_MainMenu.png`, `tour_002_MetaHub.png`, `tour_008_MetaHub.png`, `tour_014_Kitchen_Shift.png`, `tour_028_Kitchen_Shift.png`, `tour_060_MetaHub.png`, `tour_112_Kitchen_Shift.png`. Potwierdzają menu, portrety i zamazywanie angielskich słów, mycie z widoczną dłonią, pracującą maszynę, następny dzień oraz wynik z powrotem do domu. Obraz 112 pokazuje S, 95/100, 8/5 naczyń i 57,90 EUR; są to wartości konkretnej automatycznej sesji, nie ocena balansu.

W tych próbkach nie znaleziono udowodnionego blokera układu. Nie potwierdzono jednak czytelności małego tekstu na fizycznym telefonie. Wieloelementowy HUD i szczegółowy wynik wymagają badania opisanym zadaniem TASK-016. Niepełne litery/przejściowo niewidoczne etykiety na pojedynczym ujęciu w trakcie animacji nie zostały pochopnie uznane za usterkę.

**Ograniczenie renderu:** TestScreenshot renderuje kamerę do 1080×2340 i czasowo przełącza Overlay Canvas do Screen Space Camera; wówczas postprocessing może zabarwiać UI, a helper usuwa część warstwy fading. Nie jest to zrzut natywnego framebufferu telefonu. Nie używać tych obrazów do oceny końcowego kontrastu bez device capture.

Dodatkowo obejrzano historyczne porównania kuchni i pokoju z `Logs/grafika_runda2_*.png`. Nie podnoszą one statusu dzisiejszej zgodności graficznej. Audio nie odsłuchiwano.

## Infrastruktura i rzeczywiste pokrycie

52 pliki C# EditMode i 27 PlayMode obejmują również helpery; liczba plików nie odpowiada liczbie metod/przypadków. 6 instancji StoryContentTests po 12 testów daje 72 przypadki walidacji rozdziałów. Są testy wzorów ekonomii, masek i narzędzi, kosza, awarii, kariery, prep, zapisów/korupcji, słownictwa, audio, jakości, konfiguracji i wejść do scen. PlayMode obejmuje menu, hub, kuchnię, sloty, RoundTrip, fabułę, UI, mapę, radio, input, lifecycle i autoplay. Większość wykorzystuje rzeczywiste sceny, część skraca drogę przez ustawienie stanu/reflection — należy czytać asercje przy ocenie zakresu.

Skan 35 grafów wykazał 274 węzły, 465 wypowiedzi i 95 wyborów, bez lokalnych brakujących celów, powtórzonych ID ani nieosiągalnych strukturalnie węzłów. Nie dowodzi osiągalności semantycznej wszystkich kombinacji flag, dni i ekonomii. Skan 8 scen nie znalazł brakujących m_Script ani brakujących lokalnych fileID; 4 sceny są w buildzie. Jeden brakujący zewnętrzny GUID w TMP Settings opisuje ASSET-001.

Workflow Android uruchamia testy, ale job budowania Androida zależy od EditMode, bez obowiązkowego powodzenia PlayMode. Komentarz o problemach grafiki Linux jest wyjaśnieniem decyzji, nie spełnieniem bramki release. TASK-013 ma zdefiniować stabilny wymagany przebieg na właściwym runnerze.

## Potwierdzone błędy i awarie

W świeżych, zakończonych przebiegach **nie ma niezaliczonego testu ani potwierdzonej awarii procesu gry**. Ostrzeżenia o zapisie w nieistniejącym testowym katalogu pochodzą ze scenariuszy SaveCorruption; ich oczekiwany log nie jest nowym crashem. Ostrzeżenia klienta licencji nie unieważniły końcowych XML. Raport alokacji przy shutdownie Unity nie dowodzi sam w sobie wycieku gry.

Błędy zidentyfikowane w aktualnej ścieżce kodu pozostają ważne mimo zielonych testów:

| ID | Problem | Pewność / brakujący dowód | Przyszła regresja |
|---|---|---|---|
| CODE-001 | Powtórne skutki dialogu po restarcie w połowie beatu | Statyczny łańcuch i konkretne dane Tomka; nie dopisywano nowego testu w audycie | Restart przed/po outcome + mark seen, single reward i koszt |
| CODE-002 | Nieudany zapis niewidoczny dla gracza | Wynik Save false i brak propagacji do UI; błąd IO ćwiczony w suite | UI status + retry + zachowany stan, także kasowanie slotu |
| CODE-003 | Ocena poprawnego gestu po usunięciu warstwy | Potwierdzona kolejność ScrubStroke → RaiseScrubbing → ToolSuits | Ostatni fragment i przejście między warstwami |
| CODE-004 | Natychmiastowe wyjście zalicza inspekcję | Potwierdzona reguła wyniku i Career.Record | Early/full × clean/broken, trwały licznik |
| CODE-005 | Przerwana pierwsza lekcja nie powraca dnia 2 | Warunek Day==1 oraz increment dnia po exit | Przerwanie na każdym kroku, jawny skip i retry |
| CODE-006 | Sama awaria kończy obowiązek nauki naprawy | Flagę ustawia Breakdowns, nie Repairs | Wyjście z otwartą awarią kontra OnRepaired |
| CODE-007 | Wiele synchronicznych zapisów w jednym węźle | Konstrukcja potwierdzona; koszt na telefonie UNKNOWN | Liczba IO na transakcję i pomiar czasu |

Pozostałe problemy: ASSET-001 (referencja TMP), BUILD-001 (CI), QA-001 (zewnętrzny runner/dowody). Szczegółowe lokalizacje, skutki i rozwiązania: `TECHNICAL_AUDIT.md`. Nie ma potwierdzonego Critical; High dotyczy integralności i gotowości procesu.

## Historia, pomijanie i niestabilność

Dotychczasowy `QA_REPORT.md` deklaruje 761 EditMode oraz 128 PlayMode +13 Explicit, dodatkowe testy i 15-minutowy soak. To dokument historyczny z innym środowiskiem; nie przenosimy jego wartości RAM/FPS na obecny build. Ścieżki `E:\Dev\scripts\final_regression.ps1` i `E:\Dev\final_regression.txt` nie były dostępne. Obecny standardowy suite wykrył 132 wykonane PlayMode, nie 128.

Najświeższe lokalne archiwalne XML według czasu zapisanego wewnątrz pliku (nie mtime OneDrive):

| Plik | Start UTC | PASS | FAIL | SKIP |
|---|---|---:|---:|---:|
| `Logs/washing-kitchen-regression.xml` | 2026-10-11 07:10:28Z | 40 | 0 | 0 |
| `Logs/washing-controls.xml` | 2026-10-11 07:07:17Z | 4 | 0 | 0 |
| `Logs/playmode2.xml` | 2026-09-30 11:32:57Z | 25 | 0 | 1 |
| `Logs/playmode.xml` | 2026-09-30 11:27:07Z | 25 | 0 | 1 |
| `Logs/glass3.xml` | 2026-09-30 11:25:12Z | 3 | 0 | 0 |
| `Logs/glass2.xml` | 2026-09-30 11:23:26Z | 3 | 0 | 0 |
| `Logs/glass1.xml` | 2026-09-30 11:21:45Z | 3 | 0 | 0 |
| `Logs/editmode.xml` | 2026-09-27 09:39:17Z | 167 | 0 | 0 |

Starsze logi zawierają błędy testów fizyki/inputu i późniejsze przebiegi zielone. Bez tej samej migawki i powtarzania nie przypisujemy im statusu „aktualnie zepsute”. Nie wyznaczono flake rate: jeden świeży standard suite oraz jeden wybrany Explicit nie wystarczą. Ryzyka niestabilności: czas rzeczywisty, ładowanie scen, frame rate, wirtualny pointer, ustawienia grafiki i długie scenariusze. Izolowanie przyczyny ma poprzedzać zmianę timeoutu czy usuwanie asercji.

## Brakujące dowody i strategia QA

| Priorytet | Luka | Scenariusz / kryterium | Zadanie |
|---|---|---|---|
| High | Przerwanie narracji i atomowość | Nagroda/koszt/flag/checkpoint najwyżej raz; dysk może odmówić w każdym kroku | TASK-001/002 |
| High | Realne mobile lifecycle | Kill/lock/notification/focus podczas mycia, wyniku, dialogu, dużego cyklu i save | TASK-009/010 |
| High | Wszystkie drogi kampanii | Kombinacje flag i dni, brak softlocka, trzy poprawne epilogi | TASK-014 |
| High | Urządzenia i dotyk | Minimum i typowy telefon, safe area/DPI, pointer nad UI i multitouch | TASK-011/016 |
| High | Migracje paczek | Instalacja update nad wcześniejszym buildem; 3 sloty, backup/recovery | TASK-027 |
| High | Profil i dłuższa sesja | CPU/GPU/GC/startup, termika i pamięć, 30–60 min na urządzeniu | TASK-012/021 |
| Medium | Reguły wyników/tutorialu | Final stroke, early inspection, first repair, dzień 2 bez tutorial flag | TASK-005–008 |
| Medium | Zegar i duża zmywarka | Cofnięcie/przesunięcie UTC, restart, gotowy wsad i wypłata dokładnie raz | TASK-009/015/027 |
| Medium | UI/audio percepcyjne | Maksymalny font, muted/colorblind, overlap audio, feedback i komfort | TASK-016/022 |
| High dla release | Finalna paczka | Czysta instalacja, upgrade, offline, signing i platform preflight | TASK-019/028 |

Zalecany rytm: szybkie testy modelu i walidacja danych przy zmianie; scenowy smoke dla kuchni/menu/zapisu w obowiązkowym CI; zestaw dłuższy/Explicit i urządzenia przed kandydatem; pełna macierz kampanii, upgrade i soak przed RC. Częstotliwość dopasować do zasobów, bez ukrywania SKIP. Nie dodawać testów powtarzających implementację bez asercji zachowania użytkownika.

Audyt nie wykonywał nowego builda Android/iOS/Windows ani formalnego profilowania. Aktualne wymogi sklepów, licencje wszystkich zasobów, podpisy i deklaracje prywatności wymagają oddzielnej weryfikacji na etapie release; nie uznano ich tu ani za spełnione, ani naruszone.
