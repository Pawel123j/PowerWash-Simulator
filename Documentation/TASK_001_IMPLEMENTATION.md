# TASK-001 — transakcyjne konsekwencje dialogów

Data: 16.10.2026. Status: **wdrożone i zweryfikowane dla zapisu v3**. Zakres: pierwsza partia masterplanu, CODE-001 oraz batchowanie CODE-007. Bez zmian nagród, kosztów, scenariusza i balansu; bez commitów i push.

## Przyczyna i odtworzenie

`DialogueSession.FinishNode` zapisywał osobno każde słowo, slang i flagę. Następnie zdarzenie outcome uruchamiało kolejny zapis portfela i statystyk. `StoryBeatTrigger` oznaczał beat jako obejrzany dopiero po zamknięciu całego grafu. Zapis nie zawierał miejsca wznowienia ani informacji o rozliczonych węzłach.

Nowy scenowy test odtwarza przerwanie `Dialogue_Ch3_2_Tomek` po `tomek3_repay`, przed zakończeniem `tomek3_end`. Na kodzie sprzed poprawki pierwszy restart dał **160 EUR zamiast 130 EUR**. Test uruchomiono w prawdziwych scenach Boot/MainMenu/MetaHub, na izolowanym zapisie; wynik czerwony znajduje się w `task001-red.xml`.

## Kontrakt zapisu

- Format v3 dodaje `appliedDialogueNodes` i `dialogueCheckpoint`. Stabilną tożsamością grafu kampanii jest autorskie `StoryBeat.beatId`, a węzła — `DialogueNode.nodeId`. Nie zależą od nazwy pliku ani instancji Unity; nie wolno ich zmieniać w opublikowanej treści bez migracji. Klucz ma prefiks długości ID beatu, aby uniknąć kolizji.
- Zakończenie węzła przygotowuje wspólny snapshot: pieniądze, statystyki, słowa, slang, flagi, ID zastosowanego węzła i punkt wznowienia. Oryginalne reguły `LifeLedger` są wykonywane na kopii roboczej. Dopiero udany zapis publikuje oba modele w pamięci i zdarzenia UI.
- Wybrana odpowiedź zapisuje docelowy węzeł przed jego odtworzeniem. Po restarcie nie można ponownie wybrać innej gałęzi już zatwierdzonej odpowiedzi. Koszt/nagroda nadal przypada po przeczytaniu jej węzła, tak jak wcześniej.
- Wznowienie następuje na początku nieukończonego węzła albo przy jego wyborach. Kilka przeczytanych linii może powtórzyć się; zastosowane skutki nie powtarzają się.
- Ostatni węzeł zapisuje także ukończenie beatu i ewentualnie rozdziału. Nie ma osobnej transakcji między ostatnią nagrodą a tymi znacznikami.
- Błąd IO pozostawia całą operację oczekującą, bez częściowej zmiany ekonomii lub narracji w RAM. Ekran rozmowy pokazuje błąd i konsekwencję zamknięcia gry. „Dalej” ponawia tę samą operację, z minimalnym odstępem 0,5 s. Dopiero udany zapis pozwala kontynuować.
- Zmiana/usunięcie slotu unieważnia możliwość zapisu z poprzedniej instancji rozmowy. Inne pola pliku są zachowane przez odczyt i modyfikację istniejącego snapshotu.
- W jednym zakończeniu węzła jest jeden zapis, niezależnie od liczby słów, flag i innych skutków. Osobny zapis wyboru utrwala trasę przed czytaniem odpowiedzi.
- Nieudane sprzątanie pliku tymczasowego po skutecznym kopiowaniu nie zmienia już wyniku zatwierdzonego zapisu na błąd.

## Kompatybilność

Migracja v0/v1/v2 do v3 zachowuje portfel, statystyki, flagi i obejrzane beaty. Nie próbuje odtwarzać historii wypłat z samej flagi. W v2 flaga mogła trafić na dysk zarówno przed wypłatą, jak i razem ze stanem po wypłacie; nie istnieje informacja pozwalająca to rozróżnić.

Gwarancja jednokrotności obejmuje skutki zatwierdzone w formacie v3. **Niedokończona rozmowa odziedziczona z v2 może zawierać dawny skutek bez pokwitowania.** Migracja nie naprawia automatycznie takich historycznych, niejednoznacznych stanów i nie koryguje zgadywaniem salda. Zakończone beaty nadal są pomijane.

## Pliki implementacji

Wszystkie ścieżki poniżej są względem katalogu projektu. Zastane niezacommitowane zmiany zostały zachowane; punkt odniesienia stanowi migawka SHA-256 przed rozpoczęciem pracy, nie HEAD Git.

| Plik | Zmiana |
|---|---|
| `Assets/_Project/Scripts/Core/Save/SaveData.cs` | Pokwitowania węzłów i checkpoint |
| `Assets/_Project/Scripts/Core/Save/SaveMigrations.cs` | Migracja do v3 |
| `Assets/_Project/Scripts/Core/Save/SaveValidation.cs` | Normalizacja pustego/niekompletnego checkpointu po serializacji Unity |
| `Assets/_Project/Scripts/Core/Save/SaveSystem.cs` | Bezpieczny wynik sprzątania; punkty symulacji przerwania tylko w Editorze |
| `Assets/_Project/Scripts/Meta/Progress/MetaProgressController.cs` | Przygotowanie i przyjęcie snapshotu ekonomii |
| `Assets/_Project/Scripts/Narrative/Progress/NarrativeProgressController.cs` | Wspólny snapshot narracji, publikacja po zatwierdzeniu |
| `Assets/_Project/Scripts/Narrative/Dialogue/IDialoguePersistence.cs` | Kontrakt zatwierdzania i wznowienia |
| `Assets/_Project/Scripts/Narrative/Dialogue/DialogueSession.cs` | Checkpointy, zatrzymanie po błędzie, zapis wyborów |
| `Assets/_Project/Scripts/Narrative/Dialogue/DialogueRunner.cs` | Integracja zapisu i ograniczone częstotliwością ponowienie |
| `Assets/_Project/Scripts/UI/Screens/StoryDialogueTransaction.cs` | Połączenie ekonomii, narracji i zakończenia beatu w jednym zapisie |
| `Assets/_Project/Scripts/UI/Screens/StoryBeatTrigger.cs` | Uruchomienie transakcyjnej rozmowy |
| `Assets/_Project/Scripts/UI/Screens/DialogueScreen.cs` | Widoczny błąd zapisu i powrót po ponowieniu |
| `Assets/Tests/EditMode/DialogueCheckpointTests.cs` | Migracje, trzy sloty, zatrzymanie sesji po błędzie |
| `Assets/Tests/PlayMode/DialogueTransactionIoPlayModeTests.cs` | Rzeczywiste IO, granice przerwania, koszt, flaga, słowa, retry, sloty, obserwatorzy |
| `Assets/Tests/PlayMode/DialogueTransactionPlayModeTests.cs` | Regresja Tomka przez restart scen i komunikat UI |

Nowe skrypty mają dołączone pliki `.meta`.

Dokumentacja: ten raport, `MASTERPLAN_PROGRESS.md` oraz aktualizacje `QA_STATUS.md`, `PRIORITIZED_BACKLOG.md` i `PROJECT_STATUS_REPORT.md`. Zmieniono **10 istniejących plików C#**, dodano **2 pliki runtime i 3 pliki testów** (z pięcioma `.meta`). Sceny, grafy, wartości ekonomii, pozostałe istniejące testy i oryginalne ustawienia projektu nie zostały zmienione.

## Walidacja

Środowisko: Windows, Unity **6000.6.5f1**, odizolowana kopia projektu; oryginał deklaruje **6000.6.3f1**. EditMode bez grafiki, PlayMode z grafiką. Nie uruchamiano generatora scen Run All ani builda Androida.

| Przebieg | PASS | FAIL | SKIP | Znaczenie |
|---|---:|---:|---:|---|
| `task001-red.xml` | 0 | 1 | 0 | Oczekiwana regresja na kodzie sprzed poprawki: 160 zamiast 130 EUR |
| `task001-editmode.xml` | 764 | 2 | 0 | Pierwsza kopia nie zawierała `CREDITS.md`, wymaganego przez dwa testy |
| `task001-final-EditMode.xml` | 766 | 0 | 0 | Pełny EditMode po uzupełnieniu kopii |
| `task001-final-PlayMode.xml` | 147 | 3 | 13 | Pełny standardowy PlayMode wykrył trzy nowe asercje pustego checkpointu |
| **`task001-verified-EditMode.xml`** | **771** | **0** | **0** | Pełny EditMode końcowego kodu, 55,06 s |
| **`task001-verified-PlayMode.xml`** | **42** | **0** | **0** | Końcowa regresja transakcji, fabuły, slotów, korupcji zapisu, round-trip i lifecycle, 90,12 s |

Trzy błędy pełnego PlayMode wynikały z serializacji Unity: pusty obiekt `DialogueCheckpoint` wracał zamiast `null`. `SaveValidation` normalizuje pusty/niekompletny rekord, zachowując pokwitowania i pieniądze. Dodano cztery testy tego przypadku oraz test sesji bez stanu fabuły. Wszystkie trzy niezaliczone przypadki zostały wykonane ponownie i przeszły. Nie osłabiano asercji. **Pełnego standardowego PlayMode po tej ostatniej korekcie nie powtarzano**; uruchomiono 42 powiązane przypadki. 13 `Explicit` pozostaje poza zakresem tej partii.

Dodano łącznie **28 przypadków regresyjnych**: 10 EditMode i 18 PlayMode. Końcowy EditMode obejmuje również jeden istniejący test stub pakietu Unity. Nie mierzono pokrycia procentowego ani wydajności telefonu.

Dowody skopiowano do `Logs/QA/TASK-001-20261016/` w projekcie: XML, logi, test czerwony, bazowy i końcowy manifest źródeł oraz `manifest.json` z wynikami i SHA-256. Ten folder jest ignorowany przez Git; nie stanowi jeszcze repozytoryjnego runnera z TASK-003. Pierwotny katalog roboczy dowodów: `C:\Users\pawel\.codex\visualizations\2026\10\16\01a14443-78d9-75a1-b9e4-cc19a8d72d34`.

Porównanie kodu i assetów kopii z oryginałem wykazało wyłącznie dwie różnice importu: `ProjectSettings/ProjectVersion.txt` i `Assets/UniversalRenderPipelineGlobalSettings.asset`. Kod C#, testy i grafy odpowiadają źródłom projektu. Renderer PlayMode: **Intel UHD Graphics**, a nie telefon. Dowody odnoszą się do testowanego snapshotu źródeł w Editorze; nie utworzono ani nie certyfikowano APK.

### Odtworzenie testów

Użyto kopii `Assets`, `Packages`, `ProjectSettings`, dokumentów głównego katalogu (w szczególności `CREDITS.md`, wymaganego przez istniejące testy) i lokalnego `Library/PackageCache`. Nie kopiowano zapisów gracza. Testy korzystają z `DirectoryOverride`/`TestSaveSandbox`.

Przykład uruchomienia pełnego zestawu na przygotowanej kopii (PowerShell; ustawić ścieżki do posiadanego Unity i kopii):

```powershell
$qaUnity = 'C:\Program Files\Unity\Hub\Editor\6000.6.5f1\Editor\Unity.exe'
$qaRoot = 'C:\Users\pawel\.codex\visualizations\2026\10\16\01a14443-78d9-75a1-b9e4-cc19a8d72d34'
$qaProject = Join-Path $qaRoot 'test-project'
foreach ($platform in @('EditMode', 'PlayMode')) {
    $qaArgs = @('-batchmode', '-projectPath', $qaProject, '-runTests',
        '-testPlatform', $platform, '-testResults', "$qaRoot\recheck-$platform.xml",
        '-logFile', "$qaRoot\recheck-$platform.log")
    if ($platform -eq 'EditMode') { $qaArgs += '-nographics' }
    $qaProcess = Start-Process $qaUnity -ArgumentList $qaArgs -WindowStyle Hidden -PassThru -Wait
    if ($qaProcess.ExitCode -ne 0) { throw "Unity ${platform}: exit $($qaProcess.ExitCode)" }
    [xml]$qaXml = Get-Content "$qaRoot\recheck-$platform.xml" -Raw
    if ($qaXml.'test-run'.result -ne 'Passed' -or [int]$qaXml.'test-run'.passed -eq 0) {
        throw 'Brak poprawnego wyniku testów'
    }
}
```

Nie dodawać `-quit` do Unity Test Runnera. Zwykły PlayMode pomija testy `Explicit`; nie są wliczane do zaliczonych. Do celowanego powtórzenia nowych regresji użyć `-testFilter 'DialogueTransactionIoPlayModeTests;DialogueTransactionPlayModeTests'` w PlayMode, a `DialogueCheckpointTests;DialogueSessionTests` w EditMode. Uruchomienie testu czerwonego wymaga źródeł sprzed poprawki i zachowanej wersji testu `task001-red-test.cs`; obecny kod powinien go zaliczać.

## Granice i kolejny krok

Testy IO symulują śmierć procesu wyjątkiem na granicy operacji plikowej, odrzuceniem kontrolerów i ponownym odczytem z dysku. To nie jest fizyczne odłączenie zasilania ani Android force-stop. Kopiowanie awaryjne nadal może cofnąć do poprzedniego spójnego backupu; utrata obu plików nie jest rozwiązywana przez dziennik skutków.

Obsługa błędu i ponowienia dotyczy tutaj dialogów. Pełny **TASK-002** pozostaje następny: centralny status zapisu także dla wypłat zmian, dużej zmywarki i kasowania slotów, wraz z odzyskiwaniem oczekującego stanu. Pozostałe etapy masterplanu, w tym urządzenia i testy nowych graczy, nie są oznaczane jako wykonane na podstawie tych testów.
