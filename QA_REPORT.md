# Raport QA — ZMYWAK (Unity 6.6, Android first)

Data testu: 2026-10-08. Tester: Claude (Sonnet 5.5), na zlecenie Pawła.

> **Stan po poprawkach:** sekcje 3–9 opisują audyt tak, jak go zrobiłem. Błędy PWS-001, 003, 004, 020, 025 i 026 zostały potem poprawione i potwierdzone testami (sekcja 10, tabela i pełna regresja; oceny po poprawkach w sekcji 11). **Druga runda stabilizacji** (sekcja 12) zamknęła PWS-002, 007, 008, 009, 013, 018, 021, 027, 028, ustabilizowała test szefa kuchni i dodała diagnostykę PWS-029; **gotowość do wydania i końcowa rekomendacja są w sekcji 13 („Release Candidate Readiness”)**. Opisy błędów w sekcji 3 zostawiłem w brzmieniu z audytu (to historia); aktualny status każdego jest w tabelach sekcji 10 i 12.

## 0. Najpierw: co faktycznie testowałem

Zlecenie było napisane pod **PowerWash Simulator** (myjka ciśnieniowa, dysze, drabina, co-op). Ten projekt to **ZMYWAK**, mobilna gra o zmywaniu naczyń. Folder nazywa się „PowerWash Simulator” tylko z nazwy. Przetestowałem więc Zmywaka i przełożyłem kategorie z promptu na to, co w tej grze istnieje:

| Kategoria z promptu | Jak potraktowana w Zmywaku |
|---|---|
| Myjka, dysze, detergenty, zasięg | Narzędzia zmywania: Płyn, Gąbka, Druciak, Skrobak, Prysznic; brud na naczyniu; przetestowane |
| Procent czystości, „ostatni piksel” | Maska brudu `DirtCanvas`, 5% auto-finish, próg resztkowy narzędzi; przetestowane |
| Kolizje, drabina, wyjście poza mapę | Fizyka stosu naczyń, spadanie z blatu, rzut na suszarkę; część przetestowana (małpa-tester), reszta z kodu |
| Mapy i obiekty | Naczynia, suszarka, zmywarki, hub, mapa miasta; audyt danych |
| UI/UX | Menu, HUD, hub, ustawienia, wynik zmiany; przegląd 78 zrzutów ekranu |
| Zapisy | `SaveSystem` (3 sloty, backup, atomowa wymiana); przetestowane dokładnie |
| Ekonomia | `LifeLedger`, wypłaty, zakupy, rachunki; fuzz + sondy |
| Sterowanie | **Tylko dotyk** (+ Esc/Back). Brak kontrolera i zmiany przypisania: **N/D** |
| Wydajność | Soak w Edytorze na RTX 3070; **brak pomiarów na telefonie** |
| Multiplayer / co-op | **N/D** (gra jednoosobowa, brak sieci) |

**Metoda:** (1) pełny zestaw testów projektu jako stan wyjściowy, (2) czytanie kodu z nastawieniem „jak to zepsuć”, (3) **35 własnych przypadków testowych-sond** (zielone i czerwone), w tym fuzz ekonomii (18 000 losowych kroków z rundą zapisu), audyt danych, symulacja szorowania i **małpa-tester** (ponad 1300 losowych dotknięć, przeciągnięć, zmian narzędzi i pauz przez prawdziwy wirtualny kursor), (4) przegląd zrzutów ekranu.

**Czego NIE sprawdziłem:** żadnego fizycznego telefonu, emulatora w tej sesji, TalkBack, dłuższej niż 10 minut sesji, połączeń przychodzących, pełnej pamięci telefonu, innych ustawień regionalnych niż pl-PL. FPS-y z Edytora na RTX 3070 **nie mówią nic o budżetowym telefonie**.

## 1. Środowisko

- Platforma: Windows 11, Unity Editor 6000.6.3f1 (kopia robocza `E:\Dev\Zmywak`), RTX 3070, URP, jakość High.
- Wersja gry: gałąź `nowe-naczynia`, commit `ad12a39` + niezatwierdzone zmiany etapów 1–3 i Bety 1.0.
- Rozdzielczość: zrzuty 1080×1920, 1080×2160, 1080×2340 i 1080×2520 (proporcje 16:9, 18:9, 19,5:9, 20:9, 21:9, tablet 4:3); testy w trybie wsadowym: okno 640×480.

## 2. Stan wyjściowy (przed moimi zmianami)

| Zestaw | Wynik |
|---|---|
| EditMode | **559 / 559 zielone** |
| PlayMode | **71 zielonych, 0 czerwonych**, 10 pominiętych (`Explicit`) |
| Podgląd dań (`ArtPreview_EachDish…`, Explicit) | **1 czerwony:** „The rack never made room for the roaster”. Najpewniej to scenariusz testu, a nie błąd gry: komentarze w kodzie mówią, że drugi cykl zmywarki w samodzielnej kuchni zawsze się psuje, a podgląd czeka na wolne miejsce w koszu. Nie sprawdzałem tego bezpośrednio, więc traktuję to jako niepotwierdzoną hipotezę. |

## 3. Raporty błędów

Numer błędu = znacznik `PWS-xxx` w testach-sondach (`Assets/Tests/EditMode/QaExploratoryTests.cs`, `QaAuditTests.cs`, `Assets/Tests/PlayMode/QaExploratoryPlayModeTests.cs`). Numery bez błędu to sondy, które wyszły **zielone** (patrz sekcja 5).

---

### PWS-001 — Zdarzenie fabularne z kwotą kasuje dług

- **Severity:** High
- **Priority:** P2
- **Środowisko:** Unity 6000.6.3f1 Editor, Windows 11, EditMode (logika bez sceny).
- **Mapa / misja:** Hub, każdy beat fabularny z kwotą (`StoryOutcome.moneyEuros`): rozdz. 1–6, np. +30 (rozdz. 3), +40 (rozdz. 4), +50 (rozdz. 6) i wymuszone koszty −20.
- **Warunki wstępne:** Portfel poniżej zera, czyli gracz nie zapłacił rachunków tygodnia (zaległość).
- **Kroki do reprodukcji:**
  1. Przeżyć tydzień bez zarobków, aż portfel spadnie poniżej zera (w teście: −155,00 EUR po pierwszym tygodniu).
  2. Wywołać dowolny beat z kwotą, np. `ApplyStoryOutcome(moneyEuros = −1)` albo `+1`.
  3. Odczytać portfel.
- **Oczekiwany rezultat:** −155 → −156 (strata) albo −154 (zysk). Dług zostaje.
- **Rzeczywisty rezultat:** Portfel = **0,00 EUR** w obu przypadkach. Dług znika, a hub pokaże „+155,00 EUR”.
- **Reprodukowalność:** 5/5 (deterministyczne).
- **Dowody:** `PWS001_AStoryMoment_WhileInDebt_DoesNotWipeTheDebt(-1)` i `(+1)`: „In debt (-155,00), a story moment worth -1 EUR left the wallet at 0,00 instead of -156,00”.
- **Dodatkowe uwagi:** Przyczyna to `Wallet = Mathf.Max(0f, Wallet + FromEuros(...))` w [LifeLedger.cs:673](Assets/_Project/Scripts/Meta/Progress/LifeLedger.cs:673). Zabezpieczenie „portfel nie spada poniżej zera” miało chronić przed stratą, a działa też w drugą stronę na istniejącym długu. Poprawka: obciąć tylko wtedy, gdy portfel *był* nieujemny, np. `Wallet = before >= 0 ? Max(0, before + delta) : before + delta`. Komunikat „+155,00 EUR” bierze się stąd, że hub liczy zmianę jako różnicę portfela przed i po, więc po poprawce też będzie poprawny.

---

### PWS-003 — Zejście do tła na pustym menu tworzy „widmowy” zapis

- **Severity:** Medium
- **Priority:** P3
- **Środowisko:** Unity Editor, PlayMode, prawdziwe sceny Boot → MainMenu.
- **Mapa / misja:** Menu główne, pierwsze uruchomienie.
- **Warunki wstępne:** Świeża instalacja, brak pliku zapisu, gracz jeszcze nie nacisnął „Rozpocznij grę”.
- **Kroki do reprodukcji:**
  1. Uruchomić grę, zatrzymać się na menu głównym.
  2. Wysłać aplikację w tło (powiadomienie, rozmowa, przełączenie aplikacji).
  3. Wrócić do menu.
- **Oczekiwany rezultat:** Dalej „Rozpocznij grę”, bez zapisu. Komentarz w kodzie (`EnsureOnDisk`) mówi wprost: „Not at boot: a first-time player who never started a game must not find one to continue”.
- **Rzeczywisty rezultat:** Powstaje plik `zmywak_save.json` (dzień 1, portfel startowy). Menu pokazuje „Kontynuuj”, „Wczytaj zapis” i „Nowa gra”.
- **Reprodukowalność:** 5/5.
- **Dowody:** `PWS003_BackgroundingTheMenuOnAFirstStart_DoesNotMintASave`: „A save file appeared although the player never started a game”.
- **Dodatkowe uwagi:** `MetaProgressController.OnApplicationPause/OnApplicationFocus` wołają `Save()` bezwarunkowo ([MetaProgressController.cs:85–100](Assets/_Project/Scripts/Meta/Progress/MetaProgressController.cs:85)), a `NarrativeProgressController` robi to samo. Na Androidzie wystarczy pociągnąć rolety powiadomień. Poprawka: zapisywać tylko po tym, jak życie zostało rozpoczęte (flaga ustawiana w hubie / `EnsureOnDisk`).

---

### PWS-004 — Usunięty slot wraca po zejściu aplikacji do tła

- **Severity:** Medium
- **Priority:** P3
- **Środowisko:** Unity Editor, PlayMode, prawdziwe sceny.
- **Mapa / misja:** Menu główne → hub → menu → zapisy.
- **Warunki wstępne:** Gra zaczęta z zapisu (slot 1), gracz w hubie.
- **Kroki do reprodukcji:**
  1. Z huba wejść w „Menu”, wrócić do menu głównego. Usługi w tle **nie są restartowane** (`machine.ChangeState<MainMenuState>()`).
  2. „Wczytaj zapis” → „Usuń” przy zapisie 1 → „Na pewno?”. Zapis znika.
  3. Wysłać aplikację w tło.
- **Oczekiwany rezultat:** Slot pozostaje pusty.
- **Rzeczywisty rezultat:** Slot 1 odtwarza się z dniem i portfelem usuniętego życia. Gracz, który właśnie skasował zapis, zobaczy go znów na liście.
- **Reprodukowalność:** 5/5.
- **Dowody:** `PWS004_ADeletedSlot_StaysDeleted_WhenTheAppGoesToTheBackground`: „The deleted life came back by itself when the app was backgrounded”.
- **Dodatkowe uwagi:** Żywy `MetaProgressController` (a także `NarrativeProgressController`) nadal trzyma stan skasowanego życia w pamięci i `Save()` zapisuje go do aktualnego slotu (`SaveSystem.Slot`). `MainMenuScreen.Delete` nie informuje kontrolerów. Nie sprawdzałem, czy odtworzony zapis jest kompletny (flagi fabuły trzyma drugi kontroler, więc prawdopodobnie tak). Poprawka: przy usuwaniu aktywnego slotu zrestartować usługi (`StartFromAnyScene.RestartInto`) albo wyczyścić stan kontrolerów. Slot wraca tylko do czasu, aż gracz naciśnie „Nowa gra” lub „Kontynuuj” (wtedy usługi są tworzone od nowa).

---

### PWS-020 — Brak wskazówki „zły przyrząd”, gdy skrobak/prysznic nie ma już czego zetrzeć

- **Severity:** Medium
- **Priority:** P3
- **Środowisko:** Unity Editor, EditMode, prawdziwe assety (`Dish_*`, `Tool_*`).
- **Mapa / misja:** Kuchnia, od dnia 4 (skrobak) i 5 (prysznic).
- **Warunki wstępne:** Wybrany skrobak (próg resztkowy 0,3) lub prysznic (0,12).
- **Kroki do reprodukcji:**
  1. Wziąć dowolne gar/miskę/brytfannę, przetrzeć skrobakiem cały obszar (skrobak zostawia film).
  2. Szorować dalej skrobakiem.
- **Oczekiwany rezultat:** Skoro skrobak nic już nie zetrze, przycisk właściwego narzędzia (Gąbka/Druciak) pulsuje jako podpowiedź.
- **Rzeczywisty rezultat:** `DishInstance.ToolSuits(skrobak)` nadal zwraca `true`, więc `WrongToolInHand` jest fałszywe, podpowiedź się nie pojawia, a szorowanie nic nie zmienia. Dotyczy 5 z 8 naczyń przy skrobaku i 7 z 8 przy prysznicu. „Łatwiejszy zlew” (auto-wybór) też nie przełączy narzędzia.
- **Reprodukowalność:** 5/5.
- **Dowody:** `PWS020_ARuinedToolInHand_IsNotCalledSuitable_OnceItCanTakeNothingMoreOff("scraper"/"shower")`.
- **Dodatkowe uwagi:** [DishInstance.cs:103](Assets/_Project/Scripts/Kitchen/Dishes/DishInstance.cs:103): `ToolSuits` sprawdza tylko, czy narzędzie jest skuteczne wobec brudu z `Remaining > 0`, ale nie uwzględnia `tool.ResidueFloor`. `SuitingTool` poprawnie preferuje narzędzia bez progu, więc wystarczy, żeby `ToolSuits` mówiło „nie pasuje”, gdy `Remaining <= ResidueFloor`.

---

### PWS-018 — Mylący opis przełącznika dźwięku

- **Severity:** Low
- **Priority:** P3
- **Środowisko:** Edytor, zrzut `22_hub_settings_18x9.png`.
- **Mapa / misja:** Ustawienia → „Dźwięk i ekran”.
- **Warunki wstępne:** Dźwięk włączony (suwaki na 100% / 80%).
- **Kroki do reprodukcji:** 1. Otworzyć ustawienia w hubie. 2. Spojrzeć na pierwszy przełącznik.
- **Oczekiwany rezultat:** Etykieta zgodna ze stanem.
- **Rzeczywisty rezultat:** Zaznaczone pole z napisem „Dźwięk (wyłączony: zupełna cisza)”, czyli tekst brzmi jak aktualny stan (wyłączony), a dźwięk gra.
- **Reprodukowalność:** 5/5.
- **Dowody:** Zrzut; kod: stały napis w `VerticalSliceSetup.Stage2.cs:49`, pole = `AccessibilitySettings.SoundEnabled`.
- **Dodatkowe uwagi:** Prawdopodobnie miało znaczyć „odznaczone = pełna cisza”. Prosta poprawka: „Dźwięk” i podpis pod spodem.

---

### PWS-002 — Poprawny JSON bez pól (np. `{}`) ukrywa dobry backup

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** EditMode.
- **Mapa / misja:** Każdy start gry z zapisu.
- **Warunki wstępne:** Główny plik zapisu zawiera tylko `{}`.
- **Kroki do reprodukcji:** 1. Zapisać dwa razy (powstaje backup). 2. Podmienić główny plik na `{}`. 3. `SaveSystem.Load()`.
- **Oczekiwany rezultat:** Wczytany backup (dzień 7).
- **Rzeczywisty rezultat:** Pusty `SaveData` z `dayNumber = 0`, czyli nowe życie, a dobry backup leży nieużyty.
- **Reprodukowalność:** 5/5.
- **Dowody:** `PWS002_AGarbageMainSave_FallsBackToTheBackup("{}")`. Przypadki `null`, `[]`, uciętego JSON-a i bajtów zerowych oraz ucięcie pliku w 60 różnych miejscach przechodzą poprawnie.
- **Dodatkowe uwagi:** `TryRead` uznaje za poprawny każdy obiekt JSON. Dodać warunek np. `version > 0 || dayNumber > 0`. Mało prawdopodobne w praktyce (zapis atomowy), więc P4.

---

### PWS-007 — Uszkodzony zapis dużej zmywarki wywala wczytanie kuchni

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** EditMode.
- **Mapa / misja:** Kuchnia (wczytanie sceny).
- **Warunki wstępne:** `bigDishwasherStartedUtcTicks` poza zakresem `DateTime` (uszkodzony lub ręcznie edytowany plik).
- **Kroki do reprodukcji:** 1. Zapisać `bigDishwasherStartedUtcTicks = long.MaxValue`. 2. Wczytać kuchnię (`BigDishwasherController.Awake` → `ReadFrom`).
- **Oczekiwany rezultat:** Wartość odrzucona, maszyna pusta.
- **Rzeczywisty rezultat:** `ArgumentOutOfRangeException` w [LongCycleDishwasher.cs:123](Assets/_Project/Scripts/Kitchen/Dishwasher/LongCycleDishwasher.cs:123); scena kuchni nie wstaje poprawnie.
- **Reprodukowalność:** 5/5.
- **Dowody:** `PWS007_ABigDishwasherSaveWithAbsurdTicks_DoesNotThrowOnLoad` (obie wartości).
- **Dodatkowe uwagi:** Poprawka: `ticks` w `[DateTime.MinValue.Ticks, DateTime.MaxValue.Ticks]` albo `try/catch`.

---

### PWS-008 — NaN / nieskończoność w zapisie przechodzą do portfela i statystyk

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** EditMode.
- **Mapa / misja:** Każdy start z zapisu.
- **Warunki wstępne:** Zapis z `walletEuros = NaN` (uszkodzony lub edytowany).
- **Kroki do reprodukcji:** 1. Zapisać `NaN`. 2. `LifeLedger.LoadFrom`.
- **Oczekiwany rezultat:** Wartość zastąpiona sensowną (np. portfel startowy), statystyki w [0,100].
- **Rzeczywisty rezultat:** `Wallet is NaN`. `Mathf.Clamp(NaN)` zwraca NaN, więc statystyki też mogą zostać NaN.
- **Reprodukowalność:** 5/5.
- **Dowody:** `PWS008_ASaveWithNaNOrInfiniteNumbers_LoadsAsASaneLife`.
- **Dodatkowe uwagi:** Poprawka: sprawdzić `float.IsFinite` w `LoadFrom` i `PlayerStatsController.LoadFrom`.

---

### PWS-009 — Starsza wersja gry „obniża” wersję nowszego zapisu

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** EditMode.
- **Mapa / misja:** Dotyczy wszystkich zapisów po cofnięciu aplikacji (rollback).
- **Warunki wstępne:** Plik z `version` większą niż `SaveMigrations.CurrentVersion`.
- **Kroki do reprodukcji:** 1. Wczytać taki zapis. 2. Zapisać go z powrotem.
- **Oczekiwany rezultat:** Wersja i nieznane pola zachowane albo zapis odrzucony.
- **Rzeczywisty rezultat:** `version` ustawiona na 2, nieznane pola przepadają (`JsonUtility`).
- **Reprodukowalność:** 5/5.
- **Dowody:** `PWS009_ASaveFromANewerGame_IsNotDowngradedByASave`.
- **Dodatkowe uwagi:** Na Androidzie rollback to rzadkość; przy kopii OneDrive / Auto Backup między wersjami jest realne.

---

### PWS-013 — Etykieta „Góra” wystaje poza lewą krawędź ekranu

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** Edytor, zrzuty `23_Kitchen_Shift_largest_text_*.png`, `17_kitchen_paused.png`.
- **Mapa / misja:** Kuchnia, wskaźnik stosu naczyń.
- **Warunki wstępne:** Wskaźnik widoczny; największy rozmiar tekstu pogarsza sprawę.
- **Kroki do reprodukcji:** 1. Wejść do kuchni. 2. Spojrzeć na lewą krawędź przy wskaźniku.
- **Oczekiwany rezultat:** Cały napis „Góra” na ekranie.
- **Rzeczywisty rezultat:** „G” przy samej krawędzi, ucięte.
- **Reprodukowalność:** 5/5.
- **Dowody:** Zrzuty; kod: etykieta 90 px wyśrodkowana na wskaźniku szerokim 46 px o `x = 10` (`VerticalSliceSetup.cs:2143–2152`), czyli ok. −12 px poza ekranem.
- **Dodatkowe uwagi:** Przesunąć wskaźnik albo wyrównać etykietę do lewej / zawęzić.

---

### PWS-027 — Drobne usterki układu i prześwity warstw

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** Edytor, zrzuty 1080×1920–2520.
- **Mapa / misja:** HUD kuchni, hub, zapisy, mapa, wynik zmiany.
- **Kroki do reprodukcji:** Otworzyć ekrany z listy poniżej (największy tekst = ustawienie dostępności).
- **Rzeczywisty rezultat (wszystko 5/5):**
  1. Największy tekst: „Umyte: 0 / 8” nachodzi na cienki pasek postępu (`23_Kitchen_Shift_largest_text_16x9.png`).
  2. Hub, największy tekst: linia „Do awansu: …· czyste zmiany 0/1” zawija się tak, że **separator „·” zaczyna drugą linię**; nagłówek „Wieczór - wybierz jedno (albo nic: spokojny sen):” leży na jasnym dywanie bez podkładu i słabo się czyta (`23_MetaHub_largest_text_21x9.png`).
  3. Panele (zapisy, mapa, ustawienia, wynik zmiany) są półprzezroczyste: spod nich przebijają napisy menu i HUD („ZMYWAK / Zielona Wyspa” zderza się z nagłówkiem „Zapisy”, „Wczytaj zapis”/„Ustawienia” pod panelem, „04:05”, „Umyte”, nazwy narzędzi pod wynikiem) (`40_save_slots.png`, `23_shift_result.png`, `39_city_map.png`).
- **Oczekiwany rezultat:** Czytelne, nienakładające się elementy.
- **Reprodukowalność:** 5/5.
- **Dowody:** Wymienione zrzuty w `E:\Dev\shots\final_regular`.
- **Dodatkowe uwagi:** Bezpieczniej zrobić tło modalne nieprzezroczyste lub użyć jednolitej zasłony pod panelem.

---

### PWS-028 — Wynik zmiany pokazuje „Norma: brakło 7” obok „Premia za normę”

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** Edytor, zrzut `23_shift_result.png`.
- **Mapa / misja:** Ekran wyniku po wcześniejszym wyjściu ze zmiany.
- **Warunki wstępne:** Zmiana zakończona wcześniej niż po czasie, część normy zaliczona proporcjonalnie.
- **Kroki do reprodukcji:** 1. Umyć 1 naczynie. 2. „Kończę zmianę” w pauzie.
- **Oczekiwany rezultat:** Spójny komunikat o normie.
- **Rzeczywisty rezultat:** „Norma: brakło 7” (pełna norma 8), a w wypłacie „Premia za normę (część zmiany) +0,66 EUR”.
- **Reprodukowalność:** 5/5.
- **Dowody:** Zrzut; kod: `ShiftResultCalculator` liczy premię od normy za przepracowaną część, ekran pokazuje normę całej zmiany.
- **Dodatkowe uwagi:** Pokazać w wierszu normy tę samą część (np. „1 / 1 za 22 s”) albo dopisać, że norma jest liczona proporcjonalnie.

---

### PWS-021 — Błędna polska odmiana „naczyń” dla liczb 22–24, 32–34, …

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** Analiza kodu.
- **Mapa / misja:** Ekran wyniku, rekord zmiany.
- **Warunki wstępne:** Nowy rekord dokładnie 22–24, 32–34 itd. naczyń na zmianie.
- **Kroki do reprodukcji:** Pobić rekord wynikiem 22.
- **Oczekiwany rezultat:** „22 naczynia”.
- **Rzeczywisty rezultat:** „Nowy rekord: 22 naczyń na zmianie!” ([ShiftResultScreen.cs:295](Assets/_Project/Scripts/UI/HUD/ShiftResultScreen.cs:295)).
- **Reprodukowalność:** 100% przy takich liczbach (nie uruchamiałem, wynika z kodu).
- **Dowody:** Kod. Inne napisy z „naczyń/naczynia” (norma 5–12, pomoc ekipy = 2) są w dozwolonych zakresach.
- **Dodatkowe uwagi:** Mała funkcja odmiany (1 / 2–4 poza 12–14 / reszta).

---

### PWS-025 — Wyjęcie dużej zmywarki zapisuje się przed wypłatą (utrata przy zabiciu aplikacji)

- **Severity:** Low
- **Priority:** P4
- **Środowisko:** Analiza kodu (nie uruchamiałem).
- **Mapa / misja:** Kuchnia, duża zmywarka (od dnia 5).
- **Warunki wstępne:** Zakończony 30-minutowy cykl, 40 naczyń w maszynie.
- **Kroki do reprodukcji:** 1. Wyjąć załadunek. 2. Zabić aplikację w trakcie tej samej zmiany.
- **Oczekiwany rezultat:** Albo kredyt za naczynia został wypłacony, albo załadunek nadal czeka w maszynie.
- **Rzeczywisty rezultat:** `TryUnload` zapisuje pustą maszynę od razu ([BigDishwasherController.cs:130](Assets/_Project/Scripts/Kitchen/Dishwasher/BigDishwasherController.cs:130)), ale kredyt (`_dishesCleanedTotal`) wpływa do wypłaty dopiero w `EndShift`. Zabicie aplikacji gubi i naczynia, i pół godziny realnego czekania.
- **Reprodukowalność:** 100% z kodu; rzadka ścieżka.
- **Dowody:** Kod; test `KilledMidShift…` potwierdza, że niedokończona zmiana ginie.
- **Dodatkowe uwagi:** Odłożyć zapis „pusta maszyna” do końca zmiany albo zapisać także zarobiony kredyt.

---

### PWS-026 — Narzędzie soak (autogra) przestało działać po menu zapisów

- **Severity:** Medium (proces QA; gracze nie są dotknięci)
- **Priority:** P3
- **Środowisko:** Unity Editor, PlayMode; dotyczy też `Tools/phone_soak.ps1` na telefonie.
- **Mapa / misja:** Menu główne → Play (świeża gra).
- **Warunki wstępne:** `SoakPlayModeTests.Soak_ShiftAfterShift…` albo intent `zmywak_autoplay`.
- **Kroki do reprodukcji:** 1. Uruchomić soak. 2. Autogra naciska „Nowa gra / Kontynuuj”.
- **Oczekiwany rezultat:** Autogra gra zmiana po zmianie przez 15 minut.
- **Rzeczywisty rezultat:** Po naciśnięciu Play `StartFromAnyScene.RestartInto` niszczy wszystkie obiekty gry bez `IKeepAcrossRestart`, **w tym `DevAutoplay` i `SoakMonitor`**. Gra stoi w hubie, 0 próbek, test pada po 18 minutach: „The soak never finished”.
- **Reprodukowalność:** 5/5 (1 pełny przebieg, 18 minut).
- **Dowody:** `Logs/qa_soak.log`: „Soak, 15 min: 0 shifts, 0 dishes racked”; kod: `DevAutoplay : MonoBehaviour` bez interfejsu (tylko `GameAudio`, `SceneLoader`, `LoadingScreen` go mają). Autogra uruchomiona **z huba** działa (patrz sekcja 5).
- **Dodatkowe uwagi:** Poprawka jednoliniowa: `DevAutoplay : MonoBehaviour, IKeepAcrossRestart`. Skutek uboczny: od etapu zapisów nikt nie ma świeżych danych o długiej sesji z telefonu.

---

### PWS-029 — Zacięcie 120–364 ms na początku każdej zmiany (do potwierdzenia na telefonie)

- **Severity:** Low (na RTX 3070); na budżetowym telefonie może być znacznie gorzej
- **Priority:** P4 (P3, jeśli potwierdzi się na urządzeniu)
- **Środowisko:** Unity Editor, RTX 3070, okno 640×480, jakość High, soak 10 minut.
- **Mapa / misja:** Kuchnia, start zmiany 1, 2 i 3.
- **Warunki wstępne:** Autogra z huba (`PWS024`), 3 zmiany z rzędu.
- **Kroki do reprodukcji:** 1. Rozpocząć zmianę. 2. Obserwować najgorszą klatkę w pierwszym oknie próbkowania (30 s).
- **Oczekiwany rezultat:** Brak klatek dłuższych niż ~50 ms w trakcie gry.
- **Rzeczywisty rezultat:** Najgorsza klatka 364 ms (zmiana 1), 120 ms (zmiana 2), 128 ms (zmiana 3); średnie FPS w tych oknach 45 / 49 / 53 zamiast 60. Poza tym stałe 60 FPS i najgorsza klatka 19–29 ms.
- **Reprodukowalność:** 3/3 zmiany w jednym przebiegu.
- **Dowody:** Tabela soaka `PWS024` (minuty 0,5; 4,0; 7,5).
- **Dodatkowe uwagi:** To może być aktywacja sceny pod ekranem ładowania (niewidoczna dla gracza) albo generowanie masek brudu / tworzenie tekstur na starcie. Rozstrzygnie pomiar na telefonie z profilerem. Jednocześnie licznik tekstur rośnie o ok. 3 na zmianę (406 → 415 w 10 minut, w limicie testu < 40), więc przy wielogodzinnej sesji warto to obserwować.

---

## 4. Lista wszystkich błędów

| ID | Tytuł | Severity | Priority |
|---|---|---|---|
| PWS-001 | Zdarzenie fabularne z kwotą kasuje dług | High | P2 |
| PWS-003 | Zejście do tła na pustym menu tworzy widmowy zapis | Medium | P3 |
| PWS-004 | Usunięty slot wraca po zejściu do tła | Medium | P3 |
| PWS-020 | Brak wskazówki „zły przyrząd” przy skrobaku/prysznicu na filmie | Medium | P3 |
| PWS-026 | Narzędzie soak niszczy się przy restarcie gry | Medium (QA) | P3 |
| PWS-018 | Mylący opis przełącznika dźwięku | Low | P3 |
| PWS-002 | `{}` jako zapis ukrywa dobry backup | Low | P4 |
| PWS-007 | Absurdalne ticks zmywarki wywalają wczytanie kuchni | Low | P4 |
| PWS-008 | NaN/∞ z zapisu przechodzi do portfela i statystyk | Low | P4 |
| PWS-009 | Starsza gra obniża wersję nowszego zapisu | Low | P4 |
| PWS-013 | Etykieta „Góra” ucięta przy krawędzi | Low | P4 |
| PWS-021 | Odmiana „22 naczyń” | Low | P4 |
| PWS-025 | Wyjęcie dużej zmywarki zapisane przed wypłatą | Low | P4 |
| PWS-027 | Nakładanie się elementów i prześwity paneli | Low | P4 |
| PWS-028 | „Norma: brakło 7” obok „Premia za normę” | Low | P4 |
| PWS-029 | Zacięcie 120–364 ms na starcie zmiany (do potwierdzenia na telefonie) | Low | P4 |

**Nie są błędami, tylko uwagi projektowe** (do decyzji, nic nie jest zepsute):

- **U1: dni można przeskakiwać bez kosztu energii.** „Menu główne” w pauzie rozlicza zmianę z zerową pracą i przesuwa dzień. Po 60 takich dniach: energia 79, tęsknota 100, portfel **−1505 EUR**, hostel, 8 tygodni zaległości. Brak stanu przegranej i brak limitu długu (sonda `PWS005`).
- **U2: duża zmywarka liczy czas systemowy (UTC).** Przestawienie zegara urządzenia kończy cykl od razu. Cofnięcie zegara jest obsłużone poprawnie.
- **U3: balans narzędzi.** Przy systematycznym szorowaniu: Płyn (domyślne narzędzie) czyści miskę w ~165 s, brytfannę ~129 s, lasagne ~122 s, patelnię ~108 s przy zmianie 180 s. Gąbka lub druciak: 25–66 s. Skrobak i prysznic zostawiają film z założenia. Wymaga tutorialu, który jest.
- **U4: dryf `FullIntensityRubSpeed` i `RubSpeedThreshold` w pikselach.** Próg 40 px/s i 1500 px/s zależą od rozdzielczości, nie od cali ani dp. Na tablecie 4K „pełna intensywność” będzie inna niż na telefonie.

## 5. Co przeszło (dowody stabilności)

- **Fuzz ekonomii:** 6 ziaren × 3000 losowych działań (zmiany, pub, odpoczynek, kolacja, list, zakupy, przelewy, skoki w czasie, przeprowadzki). Niezmienniki trzymają: portfel skończony, statystyki 0–100, dni rosną, wydatki nie wchodzą w rezerwę na rachunki. Co 40 kroków pełna runda zapis → odczyt porównuje **20 pól** i wszystko się zgadza.
- **Zapis:** ucięcie głównego pliku w 60 miejscach zawsze wraca do dobrego backupu; zero-bajtowy, `null`, `[]`, uciety JSON i bajty zerowe też; zapis przez kopiowanie (gdy `File.Replace` odmówi) zachowuje dobry backup; brak pliku `.tmp` po zapisie.
- **Dane:** każde naczynie mieści się na pustym koszu 6×4; w dniach 1–8 każdy brud na każdym odblokowanym naczyniu ma odblokowane skuteczne narzędzie bez progu; żadne naczynie nie utyka przy żadnym narzędziu bez progu resztkowego.
- **Małpa-tester (2 przebiegi po ~110 s przy 3× prędkości, 658 i 680 akcji):** losowe dotyk/przeciągnięcie/szorowanie/przytrzymanie, zmiany narzędzi (do 7 w jednej klatce), pauzy i Back. **Zero błędów w logu, zero wyjątków, zero naczyń w NaN / poza światem**, ekran wyniku pokazał się.
- **Menu pauzy:** podwójne „Tak” w pytaniu o menu rozlicza zmianę **raz**, nie zostawia gry zamrożonej ani wyciszonej; 300 przełączeń pauza/wznów/Back/utrata fokusu kończy się działającym zegarem.
- **Koniec zmiany pod kartą naprawy zmywarki:** ekran wyniku jest na wierzchu, przycisk „Wróć do domu” dotykalny, karta naprawy nie zasłania.
- **Świeża gra:** z huba do kuchni w ~15 s samym dotykaniem tego, co na ekranie (dialog, potem „Idź do pracy”).
- **Zdarzenia:** wszystkie subskrypcje `+=` na zdarzeniach mają odpowiadające `-=` (skan całego kodu), więc brak wycieków delegatów do trwałych kontrolerów.
- **Dziennik rozgrywki:** limit 1 MB z rotacją.
- **Soak 10 minut (autogra z huba, 3 zmiany, 27 naczyń na suszarce, Edytor/RTX 3070):** FPS 60 przez cały czas poza oknami startu zmiany (patrz PWS-029); pamięć łączna 400 → 402 MB (bez trendu), pamięć zarządzana 30–47 MB z regularnym spadaniem (GC działa), tekstury 406 → 415, siatki 61 → 81 ze skokiem do 122 i powrotem do 63; zero błędów w logu. Dziesięć minut to mało na wnioski o „kilku godzinach grania”.

## 6. TOP 5 najpoważniejszych problemów

1. **PWS-001:** dług znika po każdym beacie fabularnym z kwotą (psuje centralny system rachunków i zaległości).
2. **PWS-004:** usunięty zapis wraca po zejściu do tła.
3. **PWS-003:** każdy nowy gracz, który zejdzie do tła na menu, dostaje „Kontynuuj” z dniem 1.
4. **PWS-026:** soak (w tym na telefonie) nie działa od etapu zapisów, więc długa sesja jest nieprzetestowana.
5. **PWS-020:** gra nie podpowiada zmiany narzędzia, gdy skrobak/prysznic dobrnął do filmu.

## 7. Oceny

| Obszar | Ocena | Uzasadnienie |
|---|---|---|
| **Stabilność** | **8 / 10** | 559 + 71 testów zielonych, małpa-tester bez jednego błędu, fuzz 18 000 kroków czysty, soak 10 min bez wycieków i błędów, zapis odporny na ucięcie. Odejmuję za: zapisy w tle (PWS-003/004), brak tolerancji na uszkodzone wartości (PWS-007/008) i zepsute narzędzie do długich sesji (PWS-026). |
| **UX** | **7 / 10** | Dopracowane przepływy, tutorial, 6 proporcji ekranu bez rozjechania głównych elementów, pauza i Back działają. Odejmuję za: mylący przełącznik dźwięku (PWS-018), brak podpowiedzi narzędzia (PWS-020), nakładanie się elementów przy największym tekście i prześwity paneli (PWS-013/027), widmowy „Kontynuuj” u nowego gracza (PWS-003). |
| **Wydajność** | **7 / 10 (wstępna)** | Na RTX 3070 w Edytorze: stałe 60 FPS i płaska pamięć. Odejmuję za zacięcie na starcie zmiany (PWS-029) i za to, że **nie mam żadnych pomiarów z telefonu**, więc to ocena desktopowa, nie mobilna. |

## 7a. Rekomendacja

**PASS WITH ISSUES.** Do zamkniętych testów z ludźmi gra się nadaje. Przed publikacją trzeba poprawić PWS-001 (ekonomia), PWS-003 i PWS-004 (zapisy), a przed jakąkolwiek oceną wydajności zrobić pomiar na prawdziwym telefonie po naprawie PWS-026. Nic z tego, co znalazłem, nie powoduje awarii na zwykłej ścieżce gry. Jedyna ścieżka z utratą nagrody (PWS-025, z analizy kodu, nie z uruchomienia) wymaga zabicia aplikacji w środku zmiany po wyjęciu załadunku dużej zmywarki.

## 8. Obszary wymagające dalszych testów

- **Prawdziwe telefony**, w tym budżetowy (2–3 GB RAM): FPS, nagrzewanie, czas ładowania scen, rozmiar tekstur po kilku godzinach. Do tego soak po poprawce PWS-026.
- **Przerwania systemowe:** połączenie przychodzące, pełna pamięć telefonu, tryb oszczędzania baterii, zabicie aplikacji w trakcie zapisu (poza testem kopii zapasowej).
- **Dotyk wielopalcowy** i gesty brzegowe Androida (nawigacja gestami nad paskiem narzędzi): `DishInputRouter` używa jednego wskaźnika.
- **Kopia zapasowa / synchronizacja** katalogu zapisu (Auto Backup, OneDrive): rollback wersji (PWS-009), zapisy z innego urządzenia.
- **Zegar i strefa czasowa** przy dużej zmywarce; zmiana języka systemu (inne separatory dziesiętne, `GameCulture`).
- **Dostępność:** TalkBack, tryb dla daltonistów na urządzeniu, największy tekst na wszystkich proporcjach po poprawce PWS-013/PWS-027.
- **Balans pierwszej doby** (znany jako „wymaga playtestu”): pierwszy tydzień przy niskich wynikach, bo w zrzucie testowym (1 z 8 naczyń) wypłata za zmianę to 4,86 EUR, a z mojej sondy wynika, że rachunki pierwszego tygodnia to ok. 175 EUR (dług −155 po tygodniu bez zarobków przy portfelu 20 EUR).
- **Pozostałe 10 testów `Explicit`** projektu (m.in. podgląd dań, który jest teraz czerwony przez kolejność awarii zmywarki).

## 9. Jak powtórzyć moje sondy

> **Od drugiej rundy** (sekcja 12) szybkie sondy **nie są już `Explicit`**: biegną ze zwykłym zestawem (kategorie `QA` i `QARegression`), więc każdy czerwony jest od razu widoczny. `Explicit` zostały tylko dwie długie sondy (`PWS022` małpa-tester, `PWS024` soak 10 min), pełny soak 15 min i galerie obrazkowe (`ArtPreview_*`, `Tour_*`); uzasadnienie w sekcji 12.6. Poniższy opis (`[Explicit]`, „czerwone sondy”) dotyczy stanu z audytu.

W audycie wszystkie sondy były oznaczone `[Explicit]`, więc **nie wpływały na zwykły zielony przebieg**. Przykłady (PowerShell):

```powershell
# EditMode: ekonomia, zapisy, audyt, symulacja szorowania
& 'E:\Dev\scripts\unity.ps1' -Name qa_edit -UnityArgs @('-nographics','-runTests','-testPlatform','EditMode','-testFilter','Zmywak.Tests.QaExploratoryTests','-testResults','E:\Dev\Zmywak\Logs\qa_edit.xml')
& 'E:\Dev\scripts\unity.ps1' -Name qa_audit -UnityArgs @('-nographics','-runTests','-testPlatform','EditMode','-testFilter','Zmywak.Tests.QaAuditTests','-testResults','E:\Dev\Zmywak\Logs\qa_audit.xml')
# PlayMode: prawdziwe sceny, małpa-tester, soak z huba
& 'E:\Dev\scripts\unity.ps1' -NoSync -Name qa_play -UnityArgs @('-runTests','-testPlatform','PlayMode','-testFilter','Zmywak.Tests.PlayMode.QaExploratoryPlayModeTests','-testResults','E:\Dev\Zmywak\Logs\qa_play.xml')
```

Czerwone sondy (to są błędy do poprawy): `PWS001`, `PWS002("{}")`, `PWS003`, `PWS004`, `PWS007`, `PWS008`, `PWS009`, `PWS020`. Po poprawce każda powinna zazielenieć.

## 10. Poprawki i regresja (2026-10-08, po audycie)

Sześć błędów poprawionych pojedynczo w zleconej kolejności, każdy: sonda czerwona przed → root cause → minimalna poprawka → sonda i testy regresji zielone. Nic nie zostało zatwierdzone w git. Sondy QA zostały (nic nie usunąłem); jedyne zmiany w sondach to: usunięcie przestarzałego wywołania `FindObjectsByType(..., FindObjectsSortMode)` (ostrzeżenie kompilatora) i **przygotowanie** sondy PWS-020 (opis niżej).

| Bug | Przed | Po | Root cause | Zmienione pliki (produkcyjne) | Regression | Status |
|---|---|---|---|---|---|---|
| **PWS-001** dług kasowany przez beat fabularny | sonda czerwona: −155 ±1 → 0,00 EUR | sonda zielona; 19 przypadków regresji (dług ±1, 0, ±50, −1 000 000, przejście przez zero, dodatnie saldo, zapis→wczytanie, treść komunikatu) | `LifeLedger.ApplyStoryOutcome`: `Wallet = Max(0, Wallet + kwota)` obcinało też portfel już ujemny | `LifeLedger.cs` | EditMode 591/591 (w tym symulacja ekonomii); PlayMode fabuła/zapis 11/11 | **FIXED** |
| **PWS-004** usunięty slot wraca | sonda scenowa czerwona | sonda zielona; 4 testy PlayMode + 2 EditMode (usunięcie slotu w grze, usunięcie *innego* slotu nie wycisza życia, zmiana slotu, restart w skasowany slot) | `MetaProgressController.Save` i `NarrativeProgressController.Save` zapisywały życie z pamięci pod `SaveSystem.Slot` przy pauzie/fokusie; `DeleteSlot` nie informował kontrolerów, a `Load() ?? new SaveData()` tworzyło plik od zera | `SaveSystem.cs` (licznik `LifeGeneration`), `MetaProgressController.cs`, `NarrativeProgressController.cs` | sloty, lifecycle, menu, plecy, fabuła: 32/32 | **FIXED** |
| **PWS-003** widmowy zapis na pustym menu | sonda scenowa czerwona | sonda zielona; 5 testów regresji (pauza/fokus/„quit”/zniszczenie nie tworzą pliku; istniejące życie nadal zapisuje się w tle; hub i akcje zaczynają życie) | te same callbacki lifecycle zapisywały bezwarunkowo | `MetaProgressController.cs`, `NarrativeProgressController.cs` | j.w. | **FIXED** (zapisy już utworzone przez starsze wersje nie są sprzątane) |
| **PWS-025** utrata nagrody po wyjęciu z dużej zmywarki | wcześniej tylko z analizy kodu; teraz **wykonana sonda czerwona** (3 talerze znikają) | sonda zielona; 3 testy regresji (zabicie dwa razy z rzędu; wypłacona zmiana kasuje kredyt, następna nie liczy go drugi raz, przez Boot z prawdziwym `MetaProgressController`; wyjęcie po końcu zmiany) | `BigDishwasherController.TryUnload` zapisywał pustą maszynę od razu, a kredyt trafiał do wypłaty dopiero w `EndShift` | `SaveData.cs` (pole `bigDishwasherPendingDishes`), `BigDishwasherController.cs`, `ShiftController.cs`, `MetaProgressController.cs` | kuchnia (w tym test dużej zmywarki) i lifecycle zielone | **FIXED** (zabicie aplikacji symulowane ponownym wczytaniem sceny, nie prawdziwym zabiciem procesu) |
| **PWS-020** brak podpowiedzi „zły przyrząd” | sonda czerwona (z uwagą o metodzie, niżej); po poprawieniu przygotowania 12 testów czerwonych z twardymi liczbami, np. „bowl / scraper … thickest 0,300, floor 0,3, still 'suits'” | sonda zielona; 11 przypadków regresji (7 rodzajów brudu, prawdziwy skrobak i prysznic na wszystkich naczyniach, 8 wartości progu od 0 do 1, cache) + test end-to-end: patelnia szorowana skrobakiem do filmu → `WrongToolInHand`, `Suggested` bez progu, dokończenie podpowiedzianym narzędziem; **czerwony bez poprawki, zielony z nią** | `DishInstance.ToolSuits` sprawdzało tylko skuteczność wobec rodzaju brudu, nie czy narzędzie może jeszcze coś zdjąć (próg `ResidueFloor`) | `DishInstance.cs`, `DirtCanvas.cs` (`HasDirtAbove`, wynik w cache, unieważniany przy ruchu brudu) | kuchnia + tour + autoplay: 36/37 (jedyny czerwony to znany `Explicit` podgląd dań) | **FIXED** (skutek uboczny: szorowanie skrobakiem po filmie liczy się teraz jako „zły przyrząd” w celności zmiany) |
| **PWS-026** soak niszczył się przy „Nowa gra” | sonda czerwona („The autoplay was destroyed…”); wcześniej pełny soak 18 min bez ani jednej zmiany | sonda i test regresji zielone; **pełny soak 15 min od Boot przez menu przeszedł**: 4 zmiany, 40 naczyń, 1 naprawa | `StartFromAnyScene.Restart` niszczy korzenie `DontDestroyOnLoad` bez `IKeepAcrossRestart`, a `DevAutoplay` (z `SoakMonitor`) go nie miał | `DevAutoplay.cs` (jeden interfejs markerowy; kod tylko deweloperski) | autoplay 1 min i soak z huba zielone | **FIXED** |

**Uwaga o metodzie PWS-020 (poprawka mojego błędu z audytu).** Oryginalna sonda uznawała narzędzie za „bezużyteczne” po jednym pociągnięciu w środku tarczy, a jej przemiatacz zostawiał nieobjętą „czapkę” u góry tarczy (pętla `y += 0.08f` kończyła na 0,82), więc część naczyń nie była wyczerpana i wniosek bywał fałszywy. Asercja zostaje ta sama (wyczerpane narzędzie nie może „pasować”), zmieniłem **przygotowanie**: przemiatanie każdej tekstury i niezależne kryterium wyczerpania (czytam brud przez `DirtCanvas.DirtAt`, nie przez testowany kod). Dopiero z tym przygotowaniem sonda jest rzetelnym dowodem, a czerwień sprzed poprawki potwierdziłem, neutralizując poprawkę w kopii roboczej.

**Decyzja projektowa w PWS-001.** Strata fabularna *przy istniejącym długu* zwiększa dług o kwotę (zwykła suma), a nie jest „darmowa”; dodatnia kwota go spłaca, także poza zero. Strata przy dodatnim lub zerowym portfelu nadal zatrzymuje się na zerze („czego nie ma, tego nie zapłaci”) i nie tworzy długu. Jeżeli wolisz, żeby fabuła nigdy nie zwiększała długu, to jedna linia do zmiany, ale sonda `PWS001(-1)` zakłada zwykłą sumę.

### Pełna regresja po poprawkach

| Zestaw | Wynik |
|---|---|
| EditMode, wszystkie zwykłe testy | **591 / 591** (stan wyjściowy 559; +32 testy regresji) |
| PlayMode, wszystkie zwykłe testy | **85 zielonych, 0 czerwonych** (stan wyjściowy 71; +14 testów regresji), 21 pominiętych `Explicit` |
| Sondy EditMode (fuzz ekonomii 6 × 3000 kroków z rundą zapisu, audyty danych, symulacja szorowania, uszkodzone zapisy) | 21 / 26 zielonych; **5 czerwonych to wyłącznie PWS-002 (`{}`), PWS-007 ×2, PWS-008, PWS-009**, czyli odporność na uszkodzony plik, której nie zlecono poprawiać |
| Sondy PlayMode (małpa-tester 2 ziarna: 658 i 680 akcji, spam pauzy, podwójne „Tak”, karta naprawy, świeża gra, PWS-003/004/025/026) | **10 / 10** |
| Soak 15 min od Boot przez menu | **przeszedł**: ~60 FPS, pamięć łączna 399 → 401 MB (bez trendu), tekstury 406 → 416, siatki 59 → 79; najgorsze klatki 140–392 ms w oknach startu zmiany (PWS-029 pozostaje otwarty, wymaga pomiaru na telefonie) |
| Nowe ostrzeżenia lub wyjątki w logach względem stanu wyjściowego | **brak** (EditMode i PlayMode) |

**Dwie rzeczy, które nie wynikają z poprawek:**
- `KitchenShiftPlayModeTests.FulfillingChefOrder_ExtendsFlowStreak` jest **niestabilny sam z siebie** (czerwony zdarza się także z wyłączoną moją zmianą: przebiegi naprzemienne dały 1 czerwony na 5 bez poprawki PWS-025 i 0 na 5 z nią; ta sama sygnatura „Dish_pan … grease=0,06”). Szoruje w czasie rzeczywistym z limitem 30 s losowo zamówione naczynie. W stanie wyjściowym przechodził przypadkiem. Nie ruszałem go (poza zakresem).
- `ArtPreview_EachDish_DirtyScrubbedAndClean` (`Explicit`) pozostaje czerwony tak jak w stanie wyjściowym.

## 11. Oceny po poprawkach

| Obszar | Przed | Po | Uzasadnienie |
|---|---|---|---|
| **Stabilność** | 8 | **9 / 10** | Znikły trzy błędy integralności danych i ekonomii (dług, widmowy i wskrzeszany zapis, utrata nagrody). Pełna regresja, małpa-tester i soak 15 min czyste. Odejmuję za otwartą odporność na uszkodzony plik (PWS-002/007/008/009), niestabilny test kuchni i to, że zabicie procesu symulowałem, a nie robiłem naprawdę. |
| **UX** | 7 | **7,5 / 10** | Wskazówka narzędzia działa, nowy gracz nie widzi „Kontynuuj”. Otwarte: mylący przełącznik dźwięku (PWS-018), układ przy największym tekście i prześwity paneli (PWS-013/027), odmiana „naczyń” (PWS-021), komunikat o normie (PWS-028). |
| **Wydajność** | 7 | **7 / 10 (wstępna)** | Pełny soak 15 min stabilny, ale nadal zero pomiarów z telefonu, a zacięcia 140–392 ms na starcie zmiany (PWS-029) czekają na sprawdzenie na urządzeniu. |

**Rekomendacja: PASS WITH ISSUES** (bliżej PASS dla zamkniętych testów z ludźmi). Przed publikacją: pomiar na prawdziwym telefonie (PWS-029), przegląd tekstów i układu (PWS-013/018/021/027/028), decyzja, czy uodparniać wczytywanie na uszkodzone pliki (PWS-002/007/008/009).

*(Wszystkie trzy pozycje tej listy zostały zrealizowane w drugiej rundzie poza pomiarem na telefonie; stan na koniec: sekcje 12–13.)*

## 12. Druga runda stabilizacji (2026-10-08)

Zasady rundy: bez nowych funkcji, bez commitów, sond QA nie usuwałem, żadnego czerwonego testu nie schowałem przez `Ignore`/`Explicit`, oczekiwań testów nie zmieniałem po to, żeby zazielenić wynik. Każdą poprawkę zaczynałem od czerwonego testu, a kończyłem uruchomieniem sondy PWS, testów `SaveSystem` i całego EditMode.

### 12.1 Etap 1 — odporność zapisu (PWS-002, 007, 008, 009)

**Prześledzona ścieżka:** zapis (`SaveSystem.Save`) → plik → kopia (`.bak`, `.tmp`) → odczyt (`TryRead`) → walidacja → rezerwa (backup) → inicjalizacja kontrolerów (`LifeLedger.LoadFrom`, `BigDishwasherController.Awake`, menu główne). Zapis atomowy i wymiana plików były już poprawne (ucięcie w 60 miejscach, kopia przy odmowie `File.Replace`); luki były w **odczycie i walidacji**:

1. `JsonUtility` przyjmuje każdy obiekt JSON (`{}`, `{"foo":1}`) i wypełnia domyślne wartości, a `TryRead` sprawdzał tylko „nie null”. Taki plik zasłaniał dobry backup, a `HasSave` (istnienie plików) pokazywało w menu „Kontynuuj” także dla samego wraku.
2. Liczby bez zakresu: `NaN`/`∞`/`1e39`, `dayNumber` przekręcający licznik, daty, na których kalendarz się wywraca, miejsce spoza enuma, ticks poza `DateTime`.
3. `Save` przepisywał `version` na bieżącą i gubił zapis nowszej gry bez śladu.

**Macierz przypadków** (`SaveCorruptionTests`, EditMode, przypadki parametryczne dla pliku głównego i backupu): pusty plik, zero bajtów, same białe znaki i BOM, ucięty JSON, niepoprawny JSON, `{}`, brakujące pola, dodatkowe pola, złe typy (tekst w polu liczbowym), skrajne liczby, `NaN`/`∞`, stara i nowsza wersja, uszkodzony główny przy dobrym backupie, dobry główny przy uszkodzonym backupie, **oba uszkodzone**. Do tego 9 testów scenowych (`SaveCorruptionPlayModeTests`: menu z wrakiem w slocie obok dobrego slotu, skrajny ale czytelny zapis przechodzi menu → hub → zmianę bez błędu, kuchnia z absurdalnym czasem startu dużej zmywarki).

| Bug | Przed | Po | Root cause | Zmienione pliki (produkcyjne) | Regression | Status |
|---|---|---|---|---|---|---|
| **PWS-002** `{}` / cudzy JSON ukrywa backup | macierz: **76 z 135 czerwonych** (59 zielonych); PlayMode 4 z 9 czerwone | macierz **135 / 135**; PlayMode **9 / 9** | `TryRead` uznawał każdy obiekt za zapis; `HasSave` liczył pliki, nie czytelność | `SaveValidation.cs` (nowy, `IsReadable`), `SaveSystem.cs` (`TryRead`, `HasReadableSave`), `MainMenuScreen.cs` (2 miejsca) | cała macierz, w tym „oba uszkodzone” → `Load()` zwraca `null` (nowa gra, ostrzeżenie w logu), menu nie oferuje „Kontynuuj” dla wraku, slot jest wolny dla nowej gry | **FIXED** |
| **PWS-007** absurdalne ticks zmywarki | `ArgumentOutOfRangeException` przy wczytaniu kuchni | `ReadFrom` nie rzuca; czas spoza zakresu = maszyna nie była uruchomiona, czas z przyszłości (> 7 dni) = start „teraz” | `new DateTime(ticks)` bez sprawdzenia zakresu; ticks z przyszłości blokowały maszynę na lata | `LongCycleDishwasher.cs` (`ReadFrom(data, nowUtc)`), `BigDishwasherController.cs`, `SaveValidation.Repair` | PlayMode: kuchnia z czasem „nieistniejącym na zegarze” i „z przyszłości” wstaje z załadunkiem w maszynie | **FIXED** |
| **PWS-008** NaN / ∞ w zapisie | `Wallet is NaN`, statystyki poza [0, 100] | plik z nieskończonością jest nieczytelny (idzie backup); wartości skrajne wracają do zakresu (`MaxMoney` ±1e7, dzień ≤ 100 000, statystyki 0–100, rok 1990–2200, miejsce z enuma, ≤ 1000 oczekujących naczyń); `Save` nie zapisuje nie-liczb | `Mathf.Clamp(NaN)` zwraca NaN; brak walidacji zakresów przy wczytaniu i przy zapisie | `SaveValidation.cs` (`Repair`, `MakeWritable`), `SaveSystem.cs` | macierz „skrajne liczby”, test scenowy „skrajny ale czytelny zapis” | **FIXED** |
| **PWS-009** starsza gra obniża wersję zapisu | `version` nadpisana na 2, nieznane pola przepadają | `version` nigdy nie maleje; zapis nowszej gry przed pierwszym nadpisaniem trafia do `*.newer` (jednorazowo, kasowany z slotem) | `Save` ustawiał `version = CurrentVersion` | `SaveSystem.cs` (`Save`, `SwapIn`, `KeepNewerGamesSave`, `DeleteSlot`) | macierz „nowsza wersja”, sonda PWS-009 | **FIXED** (patrz uwaga niżej) |

**Co to znaczy w praktyce / ograniczenia:** (1) Zapis uszkodzony **po obu stronach** daje nowe życie, nie awarię; zachowanie jest przewidywalne i udokumentowane testem, ale nic nie próbuje „naprawiać” pól, których nie da się odczytać (wartość nieznana = wartość domyślna pola). (2) Przy PWS-009 plik główny nadal nie przenosi pól, których starsza gra nie zna (`JsonUtility` je gubi); oryginał ratuje dopiero kopia `*.newer`. Pełne przenoszenie nieznanych pól wymagałoby zmiany serializera, więc tego nie robiłem (nowa funkcja). (3) Zakresy (`MaxMoney`, `MaxDay`, lata 1990–2200) to moje założenia o „rozsądnych” wartościach, nie wymaganie z projektu; jeśli gra kiedyś ma dopuszczać więcej, stałe są w jednym miejscu (`SaveValidation`).

### 12.2 Etap 2 — test `FulfillingChefOrder_ExtendsFlowStreak` (niestabilny)

**To był problem testu, nie produktu.** Test czekał, aż szef (losowo, spośród wszystkiego w zasięgu) zamówi jakieś naczynie, i próbował je umyć w czasie rzeczywistym w budżecie 30 s. Zamówienie bywa talerzem (kilka–kilkanaście sekund szorowania) albo brytfanną/patelnią (ponad minutę), więc wynik zależał od rzutu kością szefa. Zmierzyłem to zamiast zgadywać (przebieg `s2_diag`); nie znalazłem zależności od FPS, kolejności testów ani pozostałości po poprzednim teście (`TestSaveSandbox` czyści stan), a ten sam kod produkcyjny jest wywoływany w obu wersjach.

**Poprawka (tylko w teście):** test sam zamawia talerz publicznym wejściem zmiany `ChefOrderController.Issue` (tym samym kodem, którym szef zamawia), wyłącza losowe zamówienia na czas testu (`OrdersOn = false`), a **cała reszta zostaje bez zmian**: mycie, kosz, zmywarka i końcowa asercja `Assert.AreEqual(1, flow.CurrentStreak, …)`. Żeby nie utracić pokrycia „szef sam woła”, dodałem test `ChefOrder_IsCalledOnItsOwn_ForADishThatIsInReach` (szef woła tylko o naczynie, które da się jeszcze umyć). Rozgrywki nie zmieniałem.

**Wynik:** zmieniony test **20 / 20** (20 osobnych uruchomień Unity, `repeat_test.ps1`), test „szef sam woła” **10 / 10**; w pełnych przebiegach PlayMode (po każdym etapie) bez ani jednego czerwonego. W rundzie pierwszej ten test dawał ~1 czerwony na 5 przebiegów (sygnatura „Dish_pan … grease=0,06”).

### 12.3 Etap 3 — pełna regresja automatyczna

Skrypt `E:\Dev\scripts\final_regression.ps1` (kroki A–E, podsumowanie w `E:\Dev\final_regression.txt`). Po tej rundzie sondy QA są zwykłymi testami, więc kroki A i C obejmują je razem z całą resztą.

| Krok | Zestaw | Wynik |
|---|---|---|
| A | EditMode, każdy test niebędący `Explicit` (w tym fuzz ekonomii 6 × 3000 kroków z rundą zapisu, audyty danych, macierz uszkodzeń zapisu, `SaveSystem`, polskie liczby mnogie, UI, oś czasu startu) | **761 / 761**, 0 pominiętych |
| B | Klasy QA osobno: `QaExploratoryTests`, `QaAuditTests`, `SaveCorruptionTests`, `SaveSystemTests`, `BugfixRegressionTests`, `PolishPluralTests`, `UiPolishRegressionTests`, `StartupTimelineTests` | **217 / 217** |
| C | PlayMode, każdy test niebędący `Explicit` (sondy QA, cykl życia aplikacji, zapis → wczytanie, sloty, regresje, oś czasu startu, UI) | **128 zielonych, 0 czerwonych**, 13 pominiętych (`Explicit`, sekcja 12.6) z 141 |
| D | Małpa-tester, 2 ziarna (11 i 22), po 110 s przy 3× | **2 / 2** po poprawce harnessu opisanej niżej: ziarno 11: 2157 akcji, ziarno 22: 2115 akcji, 0 problemów (żadne naczynie nie wypada poza świat ani nie ma NaN w pozycji, ekran wyniku się pokazuje) |
| E | Soak 15 min od Boot przez menu (autogra, Edytor, RTX 3070, okno 640×480) | **przeszedł**: 4 zmiany, 41 naczyń na suszarce, 1 naprawa zmywarki; 60 FPS poza oknami startu zmiany; pamięć łączna **393 → 397 MB** (bez trendu), pamięć zarządzana 30–61 MB z regularnym spadaniem (GC działa), tekstury 405 → 415 (ok. +2,5 na zmianę, w limicie testu), siatki 59–177 (skoki przejściowe, wracają do ~63–78); zero wyjątków w logu |
| — | Nowe ostrzeżenia lub wyjątki w logach względem stanu wyjściowego | **0 wyjątków, 0 nowych błędów.** Nowe wpisy to wyłącznie ostrzeżenia `SaveSystem: could not read zmywak_save.json…` z testów, które celowo psują pliki zapisu (EditMode 11 rodzajów, PlayMode 2) |

**Potwierdzenie na ostatecznym kodzie.** Po kroku C zmieniłem jeszcze tylko pliki testów (poprawka harnessu małpy, diagnostyka i scenariusz galerii `ArtPreview_EachDish…`, dwa komentarze); scena `MetaHub` z poprawką linii rozdziału była już w krokach A–C. Mimo to jeszcze raz uruchomiłem cały zwykły zestaw (`E:\Dev\final_regression_recheck.txt`, 14:01–14:14): **EditMode 761 / 761, PlayMode 128 zielonych, 0 czerwonych, 13 pominiętych `Explicit`**. Kroki D i E (małpa, soak) biegły na tym samym kodzie produkcyjnym, a zmieniły się po nich wyłącznie komentarze i scenariusz galerii.

**Uwaga o wydajności w tym soaku (PWS-029 zostaje otwarty):** najgorsze klatki w oknach startu zmiany to **609 ms** (pierwsza zmiana sesji), 135, 135, 158 i 150 ms; w pierwszej rundzie było 140–392 ms. Nie rozstrzygam, czy różnica to szum Edytora, zimny start (pierwsza zmiana w sesji), czy narzut samego pomiaru: oś czasu startu (`StartupTimeline`) jest w Edytorze włączona, a ja nie porównywałem tych przebiegów w kontrolowanych warunkach. Poza oknami startu: 60 FPS i najgorsza klatka 20–41 ms. To nadal pomiar desktopowy, nie telefonu.

**Znalezisko tego etapu: małpa-tester zawisł (a nie produkt).** Pierwszy przebieg kroku D zatrzymał się na ziarnie 22 po ok. 8 minutach bez żadnego wpisu w logu, z jednym wątkiem na 100% CPU. Zamiast zgadywać, dodałem do sondy tętno diagnostyczne (jedna linia co 5 s: liczba akcji, `timeScale`, stan pauzy) i powtórzyłem: przy akcji 510 gra była **zapauzowana** (`timeScale 0`, `paused True`), a licznik akcji stał w miejscu. Przyczyna: gra była w pauzie (zwykłym, poprawnym stanie; najpewniej od losowego dotyku w przycisk ⏸, ale tego heartbeat nie rozstrzyga), a małpa czekała przez `WaitForSeconds`, które liczy czas skalowany, więc przy `timeScale = 0` nigdy się nie kończy; 110-sekundowy `PlayDeadline` sprawdzany jest dopiero na początku pętli, więc nie mógł zadziałać, a wątek główny kręcił bezczynne klatki (stąd 100% CPU i cisza w logu). To **błąd harnessu sondy, nie produktu**: pauza zachowuje się zgodnie z projektem.

**Poprawka (tylko w sondzie, dwie linie + stała):** czekania małpy są teraz w czasie rzeczywistym (`WaitForSecondsRealtime`, ta sama długość w czasie gry podzielona przez prędkość 3×), a w pauzie małpa dalej dotyka ekranu i może ją zdjąć. **Nie zmieniłem żadnej asercji ani sekwencji losowań** (te same wywołania `rng`). W pierwszej rundzie oba ziarna przeszły (po ~660 akcji), więc ta kombinacja (pauza, a zaraz po niej czekanie w czasie skalowanym) po prostu się nie zdarzyła; w tej rundzie wywołało ją ziarno 22 (przebieg gry różni się od poprzedniego, bo kod gry też się zmienił). Tętno zostaje w sondzie. Po poprawce: 2 / 2 zielone, a jedno tętno zarejestrowało grę w pauzie, z której małpa wyszła.

**Ograniczenie tej oceny:** losowe dotyki w czasie pauzy robi tylko małpa-tester; pozostałe testy wywołują pauzę jawnie i zdejmują ją w tym samym teście. Nie przeglądałem jednak każdego z ok. 25 użyć `WaitForSeconds` w pozostałych testach PlayMode jeden po drugim; żaden pełny przebieg tej i poprzedniej rundy (poza tym jednym przypadkiem małpy) nie wykazał podobnego zastoju.


### 12.4 Etap 4 — PWS-029: diagnostyka startu zmiany (bez optymalizacji)

**Nie optymalizowałem niczego na ślepo.** Dodałem `StartupTimeline` (`Assets/_Project/Scripts/Core/Diagnostics/StartupTimeline.cs`), narzędzie pomiarowe, które da się włączyć na urządzeniu. Każde ładowanie sceny przez `SceneLoader` otwiera 8-sekundowe okno; w oknie mierzone są (zegar `Stopwatch` + próbka `Profiler.BeginSample` o tej samej nazwie, więc widać je też na osi czasu Profilera):

| Co | Gdzie w raporcie |
|---|---|
| ładowanie sceny | `scene: loading in the background (to 90%)`, `scene: activation` (Awake/OnEnable), `scene: first frame` (Start) |
| inicjalizacja kontrolera zmiany | `ShiftController.Start` z podetapami |
| generowanie naczyń | `shift: SpawnDish`, `dish: Create`, `dish: body mesh and convex collider` |
| tekstury brudu | `dish: DishInstance (dirt made on the main thread)`, `dirt texture setup`, `redraw dirt colours / relief`, `glaze painted` |
| ładowanie assetów | `DishResourcePool`, `Resources` |
| UI | `ShiftHUD.OnEnable` i pozostałe `Awake`/`Start` kuchni |
| dźwięk | `KitchenAudio.Awake`, `audio: radio programme synthesised (first use)`, `ProceduralSounds`, `GameSynth` |
| pierwszy zapis / odczyt | `save: write`, `save: read` |
| GC i pamięć | liczba odśmiecań gen0/1/2 i zmiana pamięci zarządzanej w oknie; ilość zaalokowana w każdej wolnej klatce |

Na końcu okna powstaje jeden blok `[Startup]`: liczba klatek i najgorsza, kawałki z licznością / sumą / maksimum, a dla **każdej klatki wolniejszej niż 30 FPS** to, co się w niej wykonało, plus `rest` (czego zmierzone kawałki nie tłumaczą: render, fizyka, aktywacja sceny) i KB zaalokowane w tej klatce. **Działa w Edytorze i w buildzie developerskim, nie w release.** Z telefonu: `adb logcat -s Unity | Select-String "Startup"` (README, sekcja o `[Startup]`).

**Pierwszy odczyt (Edytor, RTX 3070, tryb wsadowy; to nie jest pomiar telefonu):** najgorsza klatka okna startu kuchni **488 ms** (klatka 23), reszta okna czysta (1 klatka > 33 ms). Z `scene: activation` 485 ms składają się głównie: `KitchenRadio.Start` 154 ms (synteza programu radia przy pierwszym użyciu, 152 ms), `ShiftController.Start` 115 ms (w tym pierwsze naczynia: `dish: Create` 122 ms na dwa naczynia, z czego brud generowany na wątku głównym ok. 38 ms), `SceneLighting.Awake` 52 ms, `KitchenAudio.Awake` 16 ms. Oprócz tego ładowanie w tle do 90% to 138 ms, a pierwsza klatka (`Start`) 63 ms.

**Wniosek ostrożnie:** w Edytorze największe pojedyncze kawałki to synteza radia, pierwsze naczynia i oświetlenie. To jest **hipoteza do sprawdzenia na telefonie**, bo (a) radio liczy się „przy pierwszym użyciu”, więc nie musi powtarzać się w kolejnych zmianach, a w soaku kolejne zmiany też miały zacięcia 120–128 ms, których to nie tłumaczy; (b) czasy na telefonie będą inne co do wielkości i proporcji. **PWS-029 zostaje otwarty** do odczytu bloku `[Startup]` z prawdziwego urządzenia. Testy: `StartupTimelineTests` (EditMode, liczenie okien, rozbijanie klatek, zagnieżdżenia) i `StartupTimelinePlayModeTests` (raport z prawdziwego startu kuchni nazywa oczekiwane kawałki).

### 12.5 Etap 5 — UI/UX (zrzuty BEFORE / AFTER)

Zrzuty: `E:\Dev\shots\qa_ui\before` (stan z audytu) i `E:\Dev\shots\qa_ui\after` (po poprawkach; te same nazwy). Każdą zmianę robiłem osobno: test czerwony → poprawka → test zielony → zrzut. Zmiany scen wprowadziłem narzędziem `UiPolishScenePatch` (bez „Run All”, które wypiekłoby światło od nowa i zmieniło identyfikatory we wszystkich scenach); te same wartości buduje `VerticalSliceSetup`, więc późniejszy „Run All” da ten sam wynik.

| ID | Problem | BEFORE | AFTER | Poprawka | Test |
|---|---|---|---|---|---|
| **PWS-013** | „Góra” ucięta przy lewej krawędzi | `23_Kitchen_Shift_largest_text_16x9.png`: „G” obcięte | `13_pile_meter_word_1.5x.png`: napis zaczyna się na krawędzi paska i mieści w ekranie | etykieta kotwiczona do lewej krawędzi wskaźnika zamiast wyśrodkowana na 46 px | `PWS013_…` (przy tekście 1× i 1,5×, box i szerokość tekstu w obszarze HUD) |
| **PWS-018** | zaznaczone pole „Dźwięk (wyłączony: zupełna cisza)” | `22_hub_settings_*.png` | `22_hub_settings_16x9.png`: „Dźwięk włączony” | jeden napis zgodny z tym, co znaczy pole | `PWS018_…` we wszystkich 3 scenach z ustawieniami |
| **PWS-021** | „22 naczyń” zamiast „22 naczynia” | tekst (czerwone testy: 7 z 13) | zielone | `PolishPlural` (1 / 2–4 poza 12–14 / reszta) w ekranie wyniku (rekord, pomoc ekipy), plakietce zmian specjalnych i komunikacie ekipy w HUD | `PolishPluralTests` + 12 przypadków PlayMode |
| **PWS-027b** | separator „·” zaczynał drugą linię „Do awansu” | tekst (test czerwony) | `08_meta_hub.png` | niełamliwa spacja przed „·” w `Career.NextGoal` | `UiPolishRegressionTests` |
| **PWS-027c** | prześwity paneli (HUD, tytuły i napisy menu pod kartami) | `22_hub_settings_16x9.png`, `23_shift_result.png`, `40_save_slots.png` | te same nazwy w `after` | tło modalne 0,70 → 0,92, ekran wyniku → 0,94; menu pauzy zostaje lżejsze celowo (kuchnia ma przez nie prześwitywać); linia rozdziału w hubie leży pod tłem modalnym jak reszta nagłówka (wychwycone przy porównaniu zrzutów: była złota na czarnym tle) | `PWS027_…` (3 sceny) i `PWS027_TheHubsChapterLine_…` |
| **PWS-028** | „Norma: brakło 7” obok „Premia za normę (część zmiany)” | `23_shift_result.png` | `23_shift_result.png`: „zrobiona za część zmiany” | wiersz normy liczony od części przepracowanej zmiany, tak jak premia (sam tekst: kalkulator wypłaty nie był ruszany) | `PWS028_…` (5 przypadków: wcześniejsze wyjście, pełna zmiana, brak normy) |

**Czego nie poprawiłem i dlaczego:**
- **PWS-027a „Umyte: 0 / 8 nachodzi na pasek” — wycofuję.** Po obejrzeniu zrzutów w pełnej skali tekst leży nad paskiem, nie na nim; to był mój błąd oceny, nic tu nie ma do poprawienia.
- **PWS-027d: nagłówek „Wieczór — wybierz jedno…” na jasnym dywanie słabo się czyta (zostaje otwarte).** To decyzja artystyczna (podkład pod tekst, kolor czy tło sceny), a zasada rundy mówi: bez zmian projektu bez potrzeby. Czytelność jest „słabsza”, nie „zepsuta”.
- **Uwaga kosmetyczna:** w podpowiedzi „Do awansu: naczynia 0/40 · kosze 0/2 · czyste zmiany 0/1” przy domyślnym tekście samo „0/1” spada do drugiej linii. To zwykłe zawijanie, poza zgłoszonym błędem (separator na początku linii jest naprawiony), więc zostawiam.
- Przy porównaniu BEFORE/AFTER kwoty na ekranie wyniku różnią się (4,86 → 4,08 EUR), bo to zmiana rozegrana w teście z innym czasem; kalkulator wypłaty nie był zmieniany w tej rundzie.

### 12.6 Testy `Explicit`: co zostało i dlaczego

Żadnego testu nie dopisałem do `Explicit` w tej rundzie; przeciwnie, 22 szybkie sondy QA (9 + 5 + 8) przestały być `Explicit` i biegną w każdym zwykłym przebiegu. W EditMode **0** pominiętych. W PlayMode pozostało **13** pominiętych z 141; każdy był uruchomiony ręcznie, a wynik jest poniżej:

| Test(y) | Dlaczego `Explicit` | Wynik w tej rundzie |
|---|---|---|
| `PWS022_MonkeyTester_…` (ziarna 11 i 22), 2 testy | długość: 2 × 110 s | **2 / 2 zielone** po poprawce harnessu (sekcja 12.3), po ~2100 akcji, 0 problemów |
| `Soak_ShiftAfterShift_WithoutErrorsLeaksOrSlowdown` (15 min z Boot) | długość: 15 min | **zielony** (sekcja 12.3, krok E) |
| `PWS024_TenMinuteSoak_StartedFromTheHub_ShiftAfterShift` | długość: 10 min | **nie uruchamiałem w tej rundzie**; jego rolę (soak) pełni dłuższy `Soak_…` od Boot przez menu, który przeszedł |
| `ArtPreview_*` (8: duża zmywarka, wszystkie naczynia, ręce, resztki, legendarna taca, skrobak i prysznic, pokój z samochodem, rozmycie słów) | to **galerie obrazków do oceny wzrokiem**, a nie testy („Not a check - pictures for judging the art”): każda zapisuje obrazy do `Logs/Screenshots` i trwa 1–2 minuty; były `Explicit` od początku projektu | **8 / 8 zielonych**, w tym `ArtPreview_EachDish_DirtyScrubbedAndClean`, który był **czerwony od stanu wyjściowego** (patrz niżej) |
| `Tour_TheWholeGame_InPictures` | galeria: cała gra ze zrzutami co kilka sekund (limit 20 min) | **zielony**: 434 s, 117 zrzutów w `Logs/Screenshots` |

**Czerwony `ArtPreview_EachDish…` — przyczyna ustalona i usunięta.** W audycie miałem tylko hipotezę („drugi cykl zmywarki się psuje”). Zbadałem ją: diagnostyka (log stanu zmywarki i kosza) pokazała, że zmywarka **się nie psuła** (`broken False`, `breakdowns 0`), tylko stała bezczynnie z pełnym koszem (5 naczyń, 18 / 24 pól), a przycisk „Uruchom zmywarkę” był aktywny. Powód to **zamierzone zachowanie gry**: pełny kosz sam odlicza do startu, a pierwsze dotknięcie przycisku tylko zatrzymuje odliczanie („Dobrze, dołóż jeszcze coś…”), dopiero drugie uruchamia cykl (`ShiftHUD.OnStartDishwasherClicked`). Galeria klikała raz, więc maszyna nigdy nie ruszała, a test czekał na miejsce w koszu, którego nie mogło być. **Poprawka tylko w scenariuszu testu:** po pierwszym dotknięciu, jeśli cykl nie ruszył, drugie (jak zrobiłby gracz); oczekiwanie („kosz robi miejsce na brytfannę”) bez zmian. Wynik: czerwony → zielony; w logu zostaje linia stanu zmywarki. W produkcie nic nie zmieniałem.

## 13. Release Candidate Readiness

Stan na koniec drugiej rundy stabilizacji (2026-10-08). Każdy wiersz ma wynik z konkretnego przebiegu opisanego w sekcjach 10–12; niczego nie oznaczam jako zielone bez potwierdzenia testem.

| Obszar | Wynik | Status |
|---|---|---|
| **EditMode** | **761 / 761** zielonych, 0 pominiętych (przebieg A i powtórzenie na ostatecznym kodzie) | **PASS** |
| **PlayMode** | **128 zielonych, 0 czerwonych**, 13 pominiętych `Explicit` (12 z nich uruchomiłem osobno i przeszły; `PWS024` nie, bo jego rolę pełni dłuższy soak; szczegóły w 12.6) | **PASS** |
| **QA probes** | EditMode klasy QA, korupcji, `SaveSystem`, regresji, polskich liczb mnogich, UI i osi czasu: **217 / 217**; sondy PlayMode (cykl życia, zapis → wczytanie, sloty, świeża gra, PWS-003/004/025/026) zielone w kroku C | **PASS** |
| **Save corruption** | macierz `SaveCorruptionTests` (pusty, zero bajtów, ucięty, zły JSON, brak i nadmiar pól, złe typy, skrajne liczby, `NaN`/`∞`, stara i nowsza wersja, uszkodzony główny / backup / **oba**) + 9 testów scenowych + ucięcie w 60 miejscach: wszystko zielone; PWS-002/007/008/009 **FIXED** | **PASS**, z dwoma zastrzeżeniami: zabicie procesu w trakcie zapisu było **symulowane** (ponowne wczytanie), nie robione na urządzeniu; nieznane pola nowszego zapisu są zachowane tylko w kopii `*.newer` |
| **Monkey test** | 2 ziarna (11 i 22), po ok. 2100 losowych akcji, 0 problemów, ekran wyniku się pokazuje | **PASS** (po poprawce harnessu sondy, sekcja 12.3; to nie był błąd produktu) |
| **Soak** | 15 min od Boot przez menu: 4 zmiany, 41 naczyń, 1 naprawa; pamięć 393 → 397 MB, bez wyjątków | **PASS** (Edytor, RTX 3070; 15 minut to nadal nie „kilka godzin grania”) |
| **Performance PC** | 60 FPS poza oknami startu zmiany; pamięć płaska; najgorsze klatki **w oknach startu zmiany 135–609 ms** (PWS-029) | **PASS WITH ISSUES** |
| **Performance Device** | **NOT TESTED.** Żadnego pomiaru z telefonu ani emulatora w tej rundzie. Narzędzie do pomiaru jest gotowe (`[Startup]`, sekcja 12.4) | **NOT TESTED** |
| **UI/UX** | PWS-013, 018, 021, 027 (b, c), 028 poprawione, BEFORE / AFTER w `E:\Dev\shots\qa_ui`; otwarte: PWS-027d (kontrast nagłówka wieczoru na dywanie, decyzja artystyczna), samotne „0/1” w podpowiedzi awansu; nie sprawdzałem na urządzeniu ani z TalkBack | **PASS WITH ISSUES** |

### Oceny

| Obszar | Sekcja 11 (po rundzie 1) | Teraz | Uzasadnienie |
|---|---|---|---|
| **Stability** | 9 | **9 / 10** | Zielone: pełny EditMode i PlayMode, małpa, soak 15 min, fuzz ekonomii. Odejmuję za brak jakiegokolwiek uruchomienia na telefonie (GPU, pamięć, przerwania systemowe) i za to, że sondy działają w Edytorze. Test szefa kuchni jest już deterministyczny (20 / 20). |
| **UX** | 7,5 | **8 / 10** | Poprawione sześć drobnych usterek (etykieta „Góra”, przełącznik dźwięku, odmiana „naczynia”, prześwity paneli, separator w podpowiedzi, wiersz normy). Odejmuję za otwarte PWS-027d, brak testu na urządzeniu i z TalkBack oraz za to, że ocena układu opiera się na zrzutach z Edytora. |
| **Performance** | 7 (wstępna) | **7 / 10 (wstępna)** | Bez zmian: pomiary wyłącznie desktopowe, zacięcia 135–609 ms przy starcie zmiany nadal niewyjaśnione na urządzeniu. Jest już czym je zmierzyć (`StartupTimeline`), ale pomiaru nie ma. |
| **Data safety** | (nie oceniane osobno) | **9 / 10** | Znikły trzy luki integralności z rundy 1 (dług, widmowy i wskrzeszany zapis, utrata nagrody) i cztery z rundy 2 (uszkodzony / pusty / obcy plik, `NaN` i wartości skrajne, absurdalny czas zmywarki, obniżanie wersji). Odejmuję za zabicie procesu tylko symulowane i za zakresy wartości (`SaveValidation`), które są moimi założeniami, a nie wymaganiem z projektu. |

### Rekomendacja: **PASS WITH ISSUES**

**Nie przyznaję RELEASE CANDIDATE ani PASS**, bo **Performance Device = NOT TESTED** i nie ma żadnego przebiegu na telefonie. Wszystko, co dało się potwierdzić automatycznie, jest potwierdzone; brakuje tego, czego automat w Edytorze nie pokaże. Do zamkniętych testów z ludźmi gra nadaje się według mnie już teraz (żaden znany błąd nie psuje zwykłej ścieżki gry ani zapisu).

**Co trzeba zrobić, żeby przejść do RELEASE CANDIDATE:**
1. **Pomiar na prawdziwym telefonie** (najlepiej budżetowym, 2–3 GB RAM): blok `[Startup]` ze startu zmiany (`adb logcat -s Unity | Select-String "Startup"`, build developerski), plus soak 15+ minut. To zamyka PWS-029 albo nadaje mu realną wagę.
2. **Decyzja co do PWS-027d** (nagłówek wieczoru na jasnym dywanie): podkład czy zmiana koloru.
3. **Przegląd na urządzeniu:** największy tekst na wszystkich proporcjach, TalkBack, tryb dla daltonistów, przerwania systemowe (połączenie, zabicie aplikacji w trakcie zapisu).
4. Ten sam komplet (`E:\Dev\scripts\final_regression.ps1`, ok. 50 min) na buildzie przeznaczonym do wydania; w tej rundzie nie budowałem APK ani nie sprawdzałem, czy się buduje.

**Czego nie ruszałem i co warto wiedzieć:** U1–U4 z sekcji 4 (uwagi projektowe: przeskakiwanie dni bez kosztu, zegar systemowy, balans narzędzi, progi w pikselach) są dalej otwarte jako decyzje projektowe. Wszystkie zmiany są **niezatwierdzone w git** (zgodnie z poleceniem).

### Jak powtórzyć pełną regresję

```powershell
& 'E:\Dev\scripts\final_regression.ps1'   # EditMode, klasy QA, PlayMode, małpa, soak 15 min; podsumowanie w E:\Dev\final_regression.txt
& 'E:\Dev\scripts\repeat_test.ps1' -Filter 'Zmywak.Tests.PlayMode.KitchenShiftPlayModeTests.FulfillingChefOrder_ExtendsFlowStreak' -Times 20
& 'E:\Dev\scripts\run_each.ps1' -Names @('<pełna nazwa testu>', ...)   # po jednym uruchomieniu Unity na test (u mnie jeden -testFilter z kilkoma pełnymi nazwami testów Explicit uruchomił tylko pierwszy)
```
