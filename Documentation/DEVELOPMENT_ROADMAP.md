# Roadmapa rozwoju i bramki gotowości

Audyt z 13.10.2026. Badany jest bieżący katalog roboczy, także pliki niezatwierdzone w Git, a nie sam HEAD. Ścieżki dowodów są względem katalogu projektu; numery linii dotyczą tej migawki. Oryginalne skrypty, sceny, ustawienia i assety nie były celowo zmieniane. Testy uruchomiono na odrębnej kopii poza repozytorium.

**Granica pewności:** analiza statyczna potwierdza strukturę i konkretne ścieżki wykonania, test automatyczny potwierdza tylko swoje asercje. Brak pomiarów telefonu, audytu słuchowego, ręcznego przejścia wszystkich wariantów fabuły i testów sklepowych oznacza UNKNOWN w tych obszarach. Nie deklarujemy poznania gry w 100%.

## Ocena etapu

Najbardziej uzasadniony opis: **rozbudowana gra o zakresie alfa / kandydat do zamkniętych testów beta**, z działającym rdzeniem i znaczącą zawartością, lecz bez dowodu zamknięcia alfy i gotowości wydania. Nazwa „Beta 1.0” oraz bundleVersion=1.0 nie zastępują odbioru. Nie wyliczamy procentu ukończenia z liczby plików lub zielonych testów.

## A–F: warunki ukończenia

| Etap | Ocena | Spełnione wymagania | Niespełnione / UNKNOWN | Największe blokery | Kryterium ukończenia | Niezbędne testy |
|---|---|---|---|---|---|---|
| A — Grywalny prototyp | TAK w zbadanym zakresie desktop Editor | Sceny są połączone; mycie, kosz, maszyny, timer, wynik, meta i zapis mają kod oraz świeże testy integracyjne. | Pełna ręczna akceptacja na docelowym telefonie nie wynika z desktop suite. | Brak mobilnego dowodu nie blokuje nazwania prototypem; blokuje obietnicę gotowości platformowej. | Nowy gracz może przejść od menu do wyniku i powrotu bez debug bypass. | Smoke menu→hub→shift→result→hub, podstawowy input i odczyt zapisu. |
| B — Vertical Slice | ZBUDOWANY; akceptacja mobilna częściowa | Reprezentatywna pętla, grafika, audio, historia, ekonomia i tutorial zintegrowane w 4 scenach. | Odczucie dotyku, czytelność, miks i wydajność na minimum UNKNOWN; błędy przerwania lekcji. | TASK-007/008/011/012/016/022. | Jedna reprezentatywna sesja ma docelową jakość i spełnia uzgodniony profil urządzenia. | Manualny slice, lifecycle, font/safe area, odsłuch, profiler i wynik pierwszego gracza. |
| C — Alpha | ZAKRES ALFA; ukończenie fazy niepotwierdzone | Większość zakładanych systemów i 6 rozdziałów istnieje; 35 grafów strukturalnie spójnych. | Nieustalony formalny feature freeze, niezweryfikowane wszystkie ścieżki, błędy zapisu/karier/tutorialu. | CODE-001/002, TASK-009/014/015 i decyzje o scope. | Wszystkie wymagane funkcje i treści osiągalne; brak blockerów kampanii oraz utraty potwierdzonego zapisu; właściciel zamyka zakres. | Pełne ścieżki do epilogów, reset/slot/upgrade, regresje usterek i kontrola ekonomii. |
| D — Beta | NIEPOTWIERDZONA; kandydat do zamkniętych testów | Duży suite zielony na odizolowanej kopii; istnieją ustawienia dostępności, quality i diagnostyka. | Brak aktualnej macierzy urządzeń, dowodu pełnej stabilności lifecycle, akceptacji UX/balansu i powiązanego builda. | TASK-010–020, profil minimum i naprawa High. | Feature complete zamknięte; wszyscy testerzy używają identyfikowalnego builda; wszystkie High zakończone albo jawnie odraczają wejście do bety. | Macierz urządzeń, pełne kampanie, długi soak, instalacja/update, feedback nowych graczy, ustalona regresja. |
| E — Release Candidate | NIE | Istnieją skrypty builda i podstawowe preflight/CI; to fundament procesu. | Brak zweryfikowanej finalnej paczki/signingu/aktualizacji oraz zamkniętych bramek wydania. | TASK-019/020/027/028; iOS TASK-026 jeśli w scope. | Jeden zamrożony hash paczki spełnia wszystkie wymagane bramki, zero otwartych Critical/High produktowych, znane Low opisane. | Smoke dokładnie paczki kandydata, upgrade save, offline, przerwania, permissions, weryfikacja wymagań dystrybucji. |
| F — Produkcyjne 1.0 | NIE | Produkt ma tożsamość, zasoby, mechaniki, fabułę i podstawę automatyzacji. | Odbiór RC, gotowość dystrybucji/supportu, materiały/licencje, decyzje platformowe i plan hotfix niepotwierdzone. | Niezamknięte bramki wcześniejszych etapów. | Zaakceptowany RC opublikowany w uzgodnionym zakresie, spójne obietnice sklepu, gotowa obsługa problemów i kompatybilność. | Kontrola finalnej dystrybucji, instalacja ze źródła docelowego, sanity po wydaniu, odtworzenie procesu hotfix. |

## Kolejność i organizacja

Fazy są bramkami jakości, a nie sztywnym wodospadem. Pomiar urządzenia i prototyp UX powinny rozpocząć się wcześnie; finalny odbiór czeka na stabilną zawartość. Pipeline builda z FAZY 7 jest potrzebny wcześniej do alfy/bety, a jego finalna certyfikacja następuje na końcu. Każde przejście fazy wymaga jawnego dowodu, nie samego zamknięcia zadań.

Nie ma dat i estymacji czasu: nie znamy liczebności zespołu, dostępności urządzeń/Maca, budżetu art/audio ani założeń dystrybucji. Po zatwierdzeniu zakresu właściciel może planować iteracje z backlogu. Podane trudności nie są ekwiwalentem roboczodni.

## FAZA 0 — Krytyczne błędy i stabilizacja

**Cel:** Zapewnić integralność zapisu i prawdziwą bramkę regresji. Nazwa fazy pochodzi z zakresu audytu; nie oznacza znalezienia usterki o priorytecie Critical.

**Dokładne zadania:** TASK-001/002: transakcja i save error; TASK-003/004: runner i dowody; TASK-005/006: poprawne wyniki; TASK-013: bramka CI; TASK-017: referencje.

**Priorytety:** High przed nową zawartością; błędy punktacji Medium wdrożyć razem z małą regresją.

**Zależności:** Kontrakt save przed migracją użytkowników; runner można przygotowywać niezależnie; CI musi korzystać z jego jawnego rezultatu.

**Kryteria akceptacji:** Brak powielenia nagród/kosztów przy restartach, błędy zapisu widoczne i odzyskiwalne, brak farmienia inspekcji, poprawny final stroke; czerwony wymagany test blokuje dystrybucję.

**Testy potwierdzające ukończenie:** Nowe regresje CODE-001–004 + 761 EditMode i zwykły PlayMode po zmianach; osobny raport Explicit; kontrolowane IO failure i czerwony job.

**Ryzyka:** Zmiana zapisu naruszy stare dane, zbyt szybka bramka utrwali niestabilny runner, drobna poprawka rozrośnie się do refaktoryzacji.

**Oczekiwany rezultat:** Wiarygodna baza do pracy nad gameplayem z odtwarzalnym raportem testów.

## FAZA 1 — Ukończenie podstawowych mechanik

**Cel:** Domknąć zachowanie przerwanej gry i procesu uczenia.

**Dokładne zadania:** TASK-007/008: guided dish i pierwsza naprawa; TASK-009: decyzja i implementacja recovery aktywnej zmiany; jawne konsekwencje early exit.

**Priorytety:** High dla pierwszej sesji i lifecycle; reszta Medium.

**Zależności:** FAZA 0: save/zdarzenia bez duplikacji; decyzja właściciela snapshot vs checkpoint; migracja tutorialu.

**Kryteria akceptacji:** Każdy etap nauki można skończyć lub świadomie pominąć; exit nie udaje sukcesu; kill aplikacji ma zgodne z kontraktem wznowienie.

**Testy potwierdzające ukończenie:** Nowy/stary slot, przerwanie każdego kroku, restart procesu, utrata focusu, trzy rodzaje napraw, round-trip aktywnej zmiany.

**Ryzyka:** Pełny snapshot fizyki może być kosztowny; uproszczony checkpoint wymaga jasnej informacji dla gracza.

**Oczekiwany rezultat:** Zdefiniowane i sprawdzone zachowanie wszystkich końców sesji oraz tutorialu.

## FAZA 2 — Integracja i dopracowanie rozgrywki

**Cel:** Sprawdzić połączenia kuchni, życia i fabuły w całej kampanii.

**Dokładne zadania:** TASK-010/011: urządzenia i input; TASK-014: macierz ścieżek; TASK-018: authoring; TASK-024: tylko potrzebne ekstrakcje kontrolerów.

**Priorytety:** High dla utraty postępu/softlocka; Medium dla narzędzi i utrzymania.

**Zależności:** FAZA 0–1, dostęp do minimum sprzętowego; pełny test rozdziałów przed nową treścią.

**Kryteria akceptacji:** Wymagane ścieżki do Dublin/Kraków/Pilbara osiągalne, sceny nie dublują usług, sloty nie mieszają danych, input działa przy nakładkach i zmianie focusu.

**Testy potwierdzające ukończenie:** Pełne przejścia, test flag/dni, load/unload/restart, device touch/Back/pause, generator dry-run i smoke wygenerowanych scen w kopii.

**Ryzyka:** Statyczna osiągalność węzłów może ukrywać sprzeczne warunki flag; refaktoryzacja może zmienić kolejność zdarzeń.

**Oczekiwany rezultat:** Zintegrowana alfa z zamkniętym zakresem i kontrolowanym tworzeniem danych.

## FAZA 3 — Rozszerzenie zawartości

**Cel:** Uzupełnić stwierdzone luki w istniejącym zakresie, bez automatycznego dodania nowych systemów.

**Dokładne zadania:** TASK-023: finalne przejścia i teksty; TASK-025: decyzja językowa; TASK-029/030 wyłącznie opcjonalne eksperymenty po dowodzie potrzeby.

**Priorytety:** Medium dla braków kampanii, Low dla nowych pomysłów; nowe funkcje nie blokują 1.0.

**Zależności:** Macierz kampanii i authoring z FAZY 2; decyzja feature freeze; wyniki balansu mogą wymagać pętli zwrotnej.

**Kryteria akceptacji:** Każdy wymagany beat/asset ma właściciela i akceptację; brak ślepych gałęzi/placeholderów w zatwierdzonym zakresie; odrzucone pomysły jawnie poza wydaniem.

**Testy potwierdzające ukończenie:** Walidacja grafów, StoryContentTests, render nowych tekstów i ponowne przejście zmienionych dróg.

**Ryzyka:** Scope creep, więcej kombinacji save/flag, mnożenie kosztu lokalizacji bez potwierdzonej potrzeby.

**Oczekiwany rezultat:** Komplet treści 1.0 w ustalonym zakresie, bez zwiększania długu transakcyjnego.

## FAZA 4 — UI/UX, grafika i audio

**Cel:** Nadać wymaganej treści docelową czytelność i feedback.

**Dokładne zadania:** TASK-016: hierarchia HUD/font/safe area/dostępność; TASK-022: odsłuch i haptyka; spójność menu/wyniku/błędu zapisu; korekta widocznych problemów na rzeczywistym ekranie.

**Priorytety:** High dla niedostępnej akcji/tekstu, Medium dla estetyki i miksu.

**Zależności:** Stabilna pętla FAZY 1–2, docelowe treści FAZY 3; wczesne badania UX mogą ruszyć wcześniej.

**Kryteria akceptacji:** Nowy gracz kończy pierwszą sesję samodzielnie; brak zasłoniętych krytycznych akcji; istotny stan czytelny bez koloru/dźwięku; audio/haptyka zgodna z ustawieniami.

**Testy potwierdzające ukończenie:** Manualna macierz ekranów i fontów, testy UI, nagranie pierwszej sesji, odsłuch urządzenia/słuchawek, kontrola wszystkich nakładek.

**Ryzyka:** Artystyczna zmiana układu może pogorszyć dotyk lub wydajność, screenshot nie ujawnia latency.

**Oczekiwany rezultat:** Czytelny, spójny slice i pełna kampania z akceptacją UX/audio.

## FAZA 5 — Optymalizacja i balans

**Cel:** Spełnić uzgodnione budżety telefonu i uzyskać uczciwe tempo postępu.

**Dokładne zadania:** TASK-012: profile i budżety; TASK-021: pomierzone optymalizacje; TASK-015: ekonomia/awans/early exit; weryfikacja quality presets, rozmiaru builda i termiki.

**Priorytety:** High dla niemożności gry/utraty postępu, Medium dla hotspotów po pomiarze.

**Zależności:** Poprawne reguły FAZY 0–2; finalne reprezentatywne art/treści; minimum sprzętowe i oczekiwany czas kampanii ustalone.

**Kryteria akceptacji:** Ślady pokazują spełnienie przyjętych limitów; brak narastania pamięci w uzgodnionym scenariuszu; wszystkie profile ekonomii mają wyjaśnioną drogę do końca.

**Testy potwierdzające ukończenie:** Profiler CPU/GPU/GC, cold/warm startup, 30–60 min sesja, build report, symulacje ekonomii i pełne przejścia słabym/sprawnym graczem.

**Ryzyka:** Optymalizacja w ciemno pogorszy złożoność; zbyt szybki awans unieważni zawartość, zbyt wolny wydłuży grind.

**Oczekiwany rezultat:** Budżety i balans udokumentowane na konkretnym buildzie i urządzeniu.

## FAZA 6 — Testy Alpha i Beta

**Cel:** Zamknąć produktowe niewiadome z udziałem grupy docelowej.

**Dokładne zadania:** TASK-020: zamknięte testy i triage; TASK-027: upgrade/recovery; pełna regresja, matryca platform i znanych ograniczeń.

**Priorytety:** High: zapis, uruchomienie, ukończenie gry, kluczowy input; reszta według wpływu.

**Zależności:** Alfa wymaga zamknięcia zakresu; beta wyników FAZY 4–5; identyfikowalny build z procesu TASK-019.

**Kryteria akceptacji:** Zero otwartych Critical/High blokujących produkt; wszystkie wymagane kampanie i urządzenia mają wyniki; każdy problem ma reprodukcję/status; jawna decyzja przejścia do RC.

**Testy potwierdzające ukończenie:** Pełny suite i wymagane Explicit, realne kill/lock/update, długa sesja, nowe i stare zapisy, manualna akceptacja nowych graczy.

**Ryzyka:** Mała grupa nie reprezentuje urządzeń/umiejętności; poprawki bety wprowadzą nowe bugi bez selektywnej regresji.

**Oczekiwany rezultat:** Zaakceptowana beta i zamrożony zakres finalnej paczki.

## FAZA 7 — Przygotowanie gry do wydania

**Cel:** Odebrać dokładnie tę paczkę, którą można bezpiecznie dystrybuować.

**Dokładne zadania:** TASK-019: odtwarzalny build/manifest; TASK-026 jeśli iOS; TASK-028: RC, aktualne wymagania dystrybucji, materiały, prawa, support/hotfix.

**Priorytety:** High dla wymagań platformy, integralności i zgodności obietnic; kosmetyka według triage.

**Zależności:** FAZA 6 zaakceptowana; konta/klucze i platformy ustalone; pipeline builda przygotować wcześniej, odbiór finalny dopiero teraz.

**Kryteria akceptacji:** Finalny hash ma komplet raportów i akceptację; instalacja/update działa; opis produktu odpowiada rzeczywistemu scope; właściciel podejmuje osobną decyzję o publikacji.

**Testy potwierdzające ukończenie:** Smoke signed/release paczki, czysta instalacja, update poprzedniej, offline, urządzenia minimalne, kontrola nieobecności debug autoplay, weryfikacja wymagań oficjalnych.

**Ryzyka:** Różnica release vs Editor, utrata symboli, niezgodny podpis/upgrade, wymagania sklepu zmienione od historycznego CI.

**Oczekiwany rezultat:** Release Candidate gotowy do publikacji, a po autoryzowanej dystrybucji kontrolowane 1.0.

## Decyzje właściciela przed planowaniem terminów

1. Android jako jedyna platforma 1.0 czy także iOS/Windows; wskazać minimum sprzętowe i zakres ekranów.
2. Pełna kampania 6 rozdziałów i 3 dróg jako scope czy mniejsza, jawnie opisana pierwsza wersja.
3. Przerwana zmiana: pełny snapshot, checkpoint czy uczciwe rozliczenie częściowe; oczekiwany wpływ na datę gry.
4. Główny profil: satysfakcja/relaks czy presja; znaczenie ułatwień dla wyników i awansów.
5. Oczekiwany czas kampanii i akceptowana trudność ekonomiczna, języki i zakres art/audio.
6. Model dystrybucji i zakres danych diagnostycznych, zespół odpowiedzialny za wsparcie i urządzenia testowe.

Rekomendacja bez rozszerzania scope: zacząć od TASK-001 i runnera TASK-003, po naprawie zapisu sprawdzić pełną kampanię oraz pierwszy kontakt na telefonie. Pomysły opcjonalne N02–N10/R02–R05 nie powinny opóźniać integralności danych i bazowej używalności.
