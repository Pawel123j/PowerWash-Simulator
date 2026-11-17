# ZMYWAK: Zielona Wyspa (Kitchen Porter)

Etap 1 — vertical slice: pełna pętla zmywania (scrape → rinse → scrub → rack → zmywarka), 3 typy brudu, jedna zmiana, placeholder art.
Etap 2 — presja: fizyczna Góra naczyń (kolizje, zawalenie), zamówienia szefa, tłuczenie, seria Flow, placeholder wypłaty.
Etap 3 — życie: portfel EUR, staty (Energia/Tęsknota/Integracja), tydzień z czynszem/jedzeniem/telefonem, kurs EUR/PLN, zapis gry, hub domowy między zmianami.
Etap 4 — fabuła: własny system dialogowy data-driven, bariera językowa (nieznane angielskie słowa są rozmyte), słownik slangu, **Rozdział 1: Przyjazd** (sześć scen, wybory z konsekwencjami), a po nim **Rozdział 2: Pierwsza wypłata**, **Rozdział 3: Swoi i obcy**, **Rozdział 4: Celtycki Tygrys** (dwa lata później) , **Rozdział 5: Krach** (jesień 2008) i **Epilog: Listopad 2010** w trzech wersjach (Dublin, Kraków albo Perth); po sześć kolejnych scen (epilog pięć), wcześniejsze wybory wracają.
Etap 5 — polish: profile jakości (Low/Medium/High + auto-dostrajanie), color grading nastroju (zimno↔ciepło wg Tęsknoty/Integracji), dostępność (skala tekstu, tryb dla daltonistów, haptyka, tryb relaks). Etap 7 (po CLI): **malowane maski brudu** (brud schodzi tam, gdzie pocierasz), **zlew** z powiększonym naczyniem, proceduralna grafika kuchni (kafle, stal, zlew z kranem, druciany stojak, zmywarka z lampką i paskiem postępu, kuweta na Górę), naczynia z profilu (talerz z zielonym paskiem, patelnia z rączką, szklanka), oświetlenie z miękkimi cieniami, tonemapping/bloom/winieta, nowe UI (ikony narzędzi, miernik Góry, karty), ilustracja Dublina w menu i hubie. Etap 8: **soczystość** — cząsteczki (piana, strumień wody, okruchy brudu, iskry druciaka, złote gwiazdki i pierścień „czysto!", odłamki przy stłuczeniu, para ze zmywarki), **dźwięki syntetyzowane kodem** (woda, szorowanie gąbką/druciakiem, pisk czystego talerza, brzęk, tłuczenie, szum zmywarki, dzwonek końca cyklu), podświetlenie pola stojaka pod przeciąganym naczyniem, podskok licznika i dymek „Flow ×N!", czerwony błysk przy stłuczeniu, wibracje w trzech siłach. Potem: tempo zmiany (naczynia napływają coraz szybciej), uczciwy szef i **awaria zmywarki z minigrą naprawy** oraz **układanie stojaka jak łamigłówka** (pełny kosz = Flow i premia) i **sprawdzanie szklanek pod światło** (przechylanie telefonu). Ręce nadal odłożone.
Etap 6 — wydanie (część kodowa): konfiguracja PlayerSettings pod Android/iOS (orientacja, IL2CPP, ARM64, AAB), nakładka FPS do testów na telefonie. Same buildy i testy na realnym urządzeniu musisz wykonać sam — wyjaśnienie dlaczego w sekcji 8.
Oprawa kuchni (27.09.2026) — **stylizowany realizm**, jak rekomenduje plan (efekt dają światło, odbicia i materiały, nie liczba wielokątów), nadal w całości generowany kodem, bez płatnych assetów: odbicia z HDR-owego „studia” (panel i jarzeniówki pod sufitem, okno — `SceneLighting`), szczotkowana stal z mapą normalnych i ciągłym wzorem na całym blacie, zaokrąglone krawędzie brył (`ArtMeshes.RoundedBox`), **zlew z prawdziwą wnęką i falującą wodą z pianą** (`TextureScroller`), plastikowa skrzynka na Górę, druciany kosz na ciemnej tacy, miękkie cienie kontaktowe pod przedmiotami i ziarno w brudzie na naczyniach (`DirtCanvas`). Liczby oprawy są w `Assets/_Project/Editor/KitchenArt.cs` (materiały, tekstury, bryły) i `Scripts/Core/Graphics/SceneLighting.cs` (światła w odbiciach); po zmianie — „Run All”. Kolizje to osobne niewidoczne prostopadłościany dokładnie tam, gdzie wcześniej, więc wygląd można zmieniać bez ruszania fizyki.
Druga runda oprawy (30.09.2026), oceniana na zrzutach 1080×2340 renderowanych na karcie graficznej, nadal w całości z kodu i z myślą o telefonie:
- **Stalowy blat:** szczotkowanie z krótszych, przerywanych rys w 512 px. Wcześniej każda rysa szła przez całą szerokość i wyglądała jak linie telewizora. Dodatkowo mapa metaliczności i połysku (`BrushedSteel_Finish`) ze smugami po dłoniach i ścierce, łukami po wycieraniu i zaschniętymi kroplami.
- **Kamera z długim obiektywem zamiast ortograficznej:** perspektywa 24° z ~23,5 m, w tym samym kadrze na wysokości blatu. `CameraWidthFitter`, dawniej `OrthographicWidthFitter`, pilnuje szerokości na wysokich ekranach także dla perspektywy.
  - Bliższe rzeczy są trochę większe: zlew i myte naczynie rosną, stojak lekko maleje.
  - Widać ścianki zlewu i skrzynki, boki zmywarki i wysokość Góry.
  - Stal odbija w każdym miejscu trochę inny fragment sufitu.
  - Zasięg cieni w `QualityManager` przestawiony pod nową odległość kamery.
- **Lampa nad zlewem** (`KitchenArt.BuildWorkLight`, światło punktowe bez cieni): płaski blat wszędzie odbijał prawie to samo i był jednolicie szary. Lampa daje plamę blasku przy zlewie, odbłysk na wodzie i światło na myte naczynie.
- **Woda w zlewie** jest mętna i mydlana, z szerszymi, łagodniejszymi falami. Drobne, strome zmarszczki łapały świetlówki jako białe drobinki na całej wodzie.
- **Ściana z kafli ciemnieje ku górze**, więc żółty baner szefa i napisy lepiej się odcinają. Bloczki zamówień na szynie mają nadruk: nagłówek z numerem stolika, odręczne linijki i czerwony „ptaszek” szefa. Przy zlewie stoi zielona butelka płynu do naczyń, obok leży złożona ściereczka. Skrzynka na Górę ma żebrowanie i wycięcia na uchwyty.
- **Ścianki patelni i szklanki bez pasów:** rzut tekstury z góry ściska całą wysokość ścianki w cienki pierścień, więc każda zmiana brudu dookoła pierścienia była pionowym pasem na ściance. Na tym pierścieniu (`DishMeshFactory.WallBand`) brud układa się teraz w poziome zacieki dookoła naczynia, bez drobnego ziarna. Tłuszcz jest ciemniejszy i mniej błyszczący: na metalicznej patelni błyszczący bursztyn wyglądał jak złota folia.
- **Mokre naczynia:** po spłukaniu na czystej polewie stoją krople wody (`DirtCanvas.Wet`, wypukłości w tej samej mapie normalnych co brud). Pod brudem ich nie ma, więc pojawiają się w miarę szorowania. Na stali łapią światło lampy, na białym talerzu są subtelne.
- **Pokój Kuby (menu i hub):**
  - przy biurku stoi krzesło z przewieszoną kurtką;
  - nad kubkiem unosi się para;
  - co 7-16 s reflektory przejeżdżającego samochodu wpadają przez okno i przesuwają się po suficie (`LightSweep`: jedno światło bez cieni, wyłączone między samochodami);
  - zrzut z takiej chwili robi jawny test `MenuScenesPlayModeTests.ArtPreview_Room_AsACarGoesBy` (`art_room_car.png`).
- **Brud ma rzeźbę i połysk:**
  - razem z kolorem malowana jest mapa normalnych (`DirtCanvas.WriteNormals`); przypalenizna jest gruba i grudkowata, sos ma trochę grubości, tłuszcz to płaski film;
  - każdy rodzaj brudu ma własny połysk (`DirtTypeDefinition.gloss`/`relief`): tłuszcz błyszczy, przypalenizna jest matowa;
  - plamy skupiają się na środku naczynia, są lekko rozmazane po łuku i mają postrzępione brzegi z drobnymi rozbryzgami, zamiast wzoru „moro”;
  - sos jest ceglasty, tłuszcz bursztynowy;
  - tekstura brudu ma 128×128 zamiast 96×96, bo na powiększonym naczyniu widać było schodki.
- **Resztki jedzenia** to kupka grudek w kilku kolorach (sos, przysmażone, ziemniak, groszek; `DishMeshFactory.Leftovers`) zamiast brązowej kuli.
- **Patelnia** ma zewnętrzną ściankę w osobnym miejscu tekstury (`DishMeshFactory.ExteriorUv`): stal z brązowym nalotem od palnika zamiast pionowych pasów brudu.
- **Naczynia na stojaku leżą na drutach.** Wcześniej zapadały się do połowy w tacę i wyglądały jak puste pierścienie.
- **Ręka:** zamiast białej kapsuły przedramię z rur o zmiennym przekroju (`ArtMeshes.Tube`): mankiet rękawiczki, goła skóra, podwinięty rękaw kitla.
- **Drobne:** zmywarka ma pokrętła, kratkę na cokole i szczelinę drzwi; skrzynka na Górę jest ciemniejsza, żeby brudne naczynia się odcinały; piana na wodzie nie iskrzy; lampka w pokoju Kuby nie przepala biurka.

Koszt na telefonie jest mały: jedno dodatkowe światło per-pixel, jedna tekstura więcej na naczynie, liczona tylko podczas szorowania, i trzy tekstury stali po 512 px. Do oceny wyglądu służy test `ArtPreview_EachDish_DirtyScrubbedAndClean` (oznaczony `[Explicit]`, więc zwykły przebieg go pomija): kuchnia oraz każde naczynie brudne, w trakcie szorowania, pod światłem i czyste, jako `Logs/Screenshots/art_*.png`. Uruchomisz go przez `-testFilter Zmywak.Tests.PlayMode.KitchenShiftPlayModeTests.ArtPreview_EachDish_DirtyScrubbedAndClean`. Generator zapisuje teksturę tylko wtedy, gdy nie ma jej pliku. Po zmianie wzoru trzeba więc usunąć stary `.png` (zostaw `.meta`), a „Run All” wygeneruje go od nowa.

## Sterowanie myciem (10.10.2026)

- Naczynie w zlewie obraca otwór do gracza, tak aby dało się trafić także w dno garnka, kubka i szklanki.
- Po podniesieniu naczynia trafienia korzystają z jego rzeczywistej powierzchni i UV. Wypukła bryła kolizji stosu nie tworzy już niewidzialnej „pokrywki”, która przesuwała miejsce czyszczenia.
- Przy brzegu jest niewielka tolerancja dotyku, zależna od wielkości ekranu. Narzędzie i czyszczenie używają tego samego punktu kontaktu, bez opóźnienia ręki podczas szorowania; przyciski zachowują pierwszeństwo.
- Pierwszy szybki ruch czyści całą przebytą drogę, również od miejsca przyłożenia palca.
- Domycie nie zamienia trwającego szorowania w przeciąganie. Puść palec lub przycisk myszy, chwyć czyste naczynie ponownie i przeciągnij albo rzuć na stojak.

Regresje: `KitchenShiftPlayModeTests.WashingAim_*` (dno siedmiu typów naczyń, tolerancja brzegu i UI, pierwszy ruch, przejście od mycia do odkładania).

Weryfikacja 11.10.2026: **40/40 testów `KitchenShiftPlayModeTests` zaliczonych** w Unity 6000.6.5f1, w tym nowe testy celowania, pełne mycie, szklanki pod światło, moczenie, stojak, samouczek i podglądy graficzne. Wyniki: `Logs/washing-kitchen-regression.xml`.

Wersja Windows z poprawkami zbudowana 13.10.2026: `Builds/Windows/Zmywak.exe` (128 MB razem z plikami gry). Log: `Logs/washing-windows-build-retry.log`.

## Beta 1.0 (7.10.2026): stan po etapach 1–3 planu

Feature freeze. Wersja do testów zewnętrznych, gdy tylko powstanie APK (moduł Android Build Support, patrz 8.0). Co jest w grze i gdzie to sprawdzić:

- **Zmiana w kuchni:**
  - Pięć rodzajów zmian (`Resources/ShiftCatalog.asset`), dobieranych na każdy dzień fabuły przez `ShiftSchedule`: spokojne dni 1–3, potem lunch, wieczorny rush, od rozdziału 4 ciężkie noce. Tempo zaciska się do 0,85 do dnia 35.
  - Naczynia rozdaje `ShuffleBag` z ziarnem dnia: ten sam dzień idzie tak samo, nigdy trzy takie same z rzędu.
  - Fale serwisu (`ServiceWaves`) przyspieszają i zwalniają napływ naczyń.
  - Pilne zamówienia szefa: zamawiane tylko spośród naczyń w zasięgu, zaliczane w chwili odłożenia. Gdy jedyne zamówione naczynie się stłucze, szef odwołuje zamówienie bez kary.
  - Awarie zmywarki: pierwsza scenariuszowa (dzień 3, do pierwszej naprawy), potem losowe z odstępem i rzadsze dzień po awarii.
- **Ocena i wypłata:**
  - `ShiftScoring` (0–100, S–D): norma i nadwyżka, właściwe narzędzie, odkładanie na stojak, Flow, pilne zamówienia, minus stłuczki i zawalona Góra.
  - `ShiftResultCalculator`: stawka godzinowa, premia za normę i za ocenę A/S, napiwki, potrącenia za stłuczki (do 30%).
  - Nowe w Beta: potrącenie za zmianę „przy zlewie bez roboty”. Poniżej ¼ normy szef obcina do połowy stawki, więc nic nierobienie (21 €) nie zbliża się do świetnej zmiany (ok. 62 €).
- **Pierwsze dni:**
  - `MechanicUnlocks` wprowadza mechaniki po jednej, dwie dziennie przez pierwszy tydzień.
  - Pierwsza zmiana to prowadzony talerz w 8 krokach (`KitchenTutorial`): kuchnia stoi, dopóki nie skończy się lekcja, a lekcja zapisuje się w slocie.
- **Życie i ekonomia:** energia, tęsknota i integracja mają skutki w grze (sen, wydajność szorowania, pub, pomoc ekipy w kuchni, kolacje, czytelność angielskiego). Symulacja 35 dni dla 7 typów gracza: `EconomySimulationTests`.
- **Zapis:** atomowa podmiana pliku z backupem. Uszkodzony plik główny nigdy nie zastępuje dobrego backupu. Do tego 3 sloty, migracje i zapis przy pauzie/minimalizacji.
- **Wydajność (Beta):**
  - Profil urządzenia wybiera startową jakość (`DeviceProfile`), a wybór w ustawieniach ją nadpisuje (Grafika: auto/niska/średnia/wysoka).
  - Kuchnia działa w 60 FPS (Low: 30), menu, mieszkanie, dialogi i pauza w 30.
  - Maski brudu mają rozdzielczość zależną od jakości (128/192/256) i liczą się z wyprzedzeniem na wątku roboczym (`DirtPrewarm`).
  - Tekstury i tablice pikseli krążą w puli (`DishResourcePool`), a na GPU trafiają tylko zmienione wiersze.
  - HUD i UI zapisują do Canvasu tylko zmiany.

### Do podmiany przez człowieka (placeholdery)

Gra jest kompletna i grywalna, ale część oprawy powstała w kodzie zamiast od artysty lub dźwiękowca. Każdą z tych rzeczy można podmienić bez ruszania logiki. Podmiana to ten sam plik albo ta sama nazwa w generatorze.

| Co | Gdzie jest teraz | Jak podmienić |
|---|---|---|
| Muzyka menu i cichy szum pokoju | `GameSynth` → `Audio/Game/MenuMusic_1.wav`, `AmbRoom_1.wav` | nagranie w tym samym miejscu (mono 44,1 kHz, pętla bez kliknięcia), potem Zmywak → Audio → Bake Game Sounds |
| Dżingle i odgłosy wyniku: oceny, awans, liczenie pieniędzy, Flow, pełny kosz, złe narzędzie, „blip” dialogu | `GameSynth` → `Audio/Game/*_1.wav` (lista w `CREDITS.md`) | jak wyżej, możliwe kilka wersji `_1`, `_2`… |
| Muzyka radia w kuchni | syntetyzowana w `RadioMusic` | nagrania stacji w miejsce syntezy (`KitchenRadio`) |
| Modele 3D kuchni, naczyń, pokoju i ręki | generowane kodem: `KitchenArt`, `RoomArt`, `DishMeshFactory`, `ArtMeshes` | modele w miejsce brył z kodu; kolizje to osobne niewidoczne bryły |
| Ikony narzędzi, grafiki samouczka i rekwizyty minigier (deska, nóż, dłoń, filtr, ramię spryskiwacza, przycisk RESET) | proceduralne PNG w `Art/UI` (`UiArt`, nazwy `Icon_*`, `Prop_*`) | narysowany PNG pod tą samą nazwą (generator nie nadpisze istniejącego pliku) |
| Portrety postaci | stylizowane rendery 3D z `Tools/Blender/portraits.py` (`PortraitArt`), źródła w `Tools/Blender/Characters` | edycja `.blend` lub generatora; PNG w `Art/Portraits` pod tymi samymi nazwami, z zachowaniem `.meta` |
| Ikona aplikacji i ekran startowy | domyślne Unity | przed wydaniem sklepowym (poza zakresem Beta 1.0) |

## 1. Wymagania

- **Unity Hub** + **Unity 6** — projekt jest zweryfikowany (kompilacja, testy, build Androida z CLI) na `6000.6.3f1` (Unity 6.6, zapisane w `ProjectSettings/ProjectVersion.txt`; do 7.10.2026 projekt był na `6000.5.5f1`), z modułem **Android Build Support** (OpenJDK + Android SDK & NDK).
- **Uwaga na OneDrive:** ten folder jest zsynchronizowany z OneDrive. Unity będzie tu tworzyć duży, szybko zmieniający się folder `Library/` (cache importu) — OneDrive będzie próbował go synchronizować, co potrafi znacząco spowolnić edytor i czasem powodować konflikty plików. **Zalecenie:** kliknij prawym na folder `Library` (pojawi się po pierwszym otwarciu projektu) → *Always keep on this device* wyłącz / albo dodaj `Library`, `Temp`, `obj` do wykluczeń synchronizacji OneDrive (Ustawienia OneDrive → Sync and backup → Advanced settings). Sam projekt (kod, assety) i tak wersjonujemy przez git, więc OneDrive nie jest tu potrzebny jako backup.
  - **Potwierdzony problem z buildem Androida:** OneDrive oznacza zsynchronizowane foldery jako tylko-do-odczytu, a Gradle (Java) na Windows nie potrafi usunąć takiego folderu — każdy build po pierwszym padał w `:unityLibrary:mergeDebugNativeLibs` z `AccessDeniedException`. `AndroidBuildScript` zdejmuje ten atrybut w `Library/Bee/Android/Prj` przed buildem (obejście), ale **najlepiej przenieść projekt poza OneDrive** (np. `C:\Dev\Zmywak`).

## 2. Pierwsze otwarcie projektu

1. Unity Hub → **Add project from disk** → wskaż ten folder.
2. Otwórz projekt. Unity zaimportuje pakiety z `Packages/manifest.json` (Input System, Localization, URP, TextMeshPro, Test Framework) — wymaga internetu przy pierwszym imporcie.
3. TMP Essentials są już zaimportowane i w repo (`Assets/TextMesh Pro`) — jeśli jednak Unity o nie zapyta, kliknij **Import**.
4. **Active Input Handling** (Edit → Project Settings → Player) musi być ustawione na **„Input System Package (New)"** — robi to automatycznie krok „Run All" (sekcja 3). Jeśli masz otwarty edytor w chwili tej zmiany, zrestartuj go. Bez tego dotyk/mysz w kuchni i przyciski UI nie działają (na telefonie w ogóle — testy automatyczne tego nie wykryją, bo wstrzykują zdarzenia z pominięciem backendu).

## 3. Wygeneruj zawartość i sceny (jednym kliknięciem)

Cały kontent (ScriptableObjecty: typy brudu, narzędzia, dania, definicja zmiany, tabela kursu EUR/PLN, graf dialogowy, wpisy slangu) i sceny (`Boot`, `MainMenu`, `MetaHub`, `Kitchen_Shift`) są budowane automatycznie przez skrypt edytorowy — nie ma tu ręcznie pisanych plików `.unity`/`.asset`, więc nie ma ryzyka uszkodzonych referencji.

W pasku menu Unity: **Zmywak → Setup → Run All (1-8)**.

To po kolei: importuje TMP Essentials (jeśli ich brak), tworzy i przypisuje assety URP (`Assets/_Project/Settings/Rendering/Zmywak_URP.asset` + renderer — bez tego Unity renderuje wbudowanym pipeline'em i wszystko jest różowe), tworzy szablon materiału `Assets/_Project/Resources/PlaceholderLit.mat`, tworzy assety kuchenne, meta i narracyjne w `Assets/_Project/ScriptableObjects`, buduje cztery sceny w `Assets/_Project/Scenes`, rejestruje je w Build Settings, sprawdza że wszystko powstało i otwiera scenę `Boot`.

(Każdy krok da się też odpalić osobno z tego samego menu, np. żeby przebudować tylko scenę kuchni po zmianie layoutu.)

## 4. Test w edytorze

1. Upewnij się, że otwarta jest scena `Assets/_Project/Scenes/Boot.unity`.
2. Wciśnij **Play**.
3. Ekran główny → **„Rozpocznij grę"** → trafiasz do **domu (MetaHub)**, gdzie gra **Rozdział 1: Przyjazd** (maj 2004): **jedna scena każdego wieczoru**, po powrocie ze zmiany (nagłówek huba pokazuje rozdział):
   - **Dzień 1 - przyjazd:** lotnisko, agencja „Emerald Recruitment" nie istnieje, a Gerry przez telefon chce jeszcze „opłaty" (**oddać ostatnie 20 €** czy się rozłączyć), hostel i Marek, potem pierwsze spotkanie z Declanem („Każdy kiedyś od tego zaczynał").
   - **Dzień 2 - dom na dziewięć osób:** Marek, Agnieszka, Darius, pani Byrne, która „I will, yeah" wpisze Kubę do umowy (po irlandzku: nie wpisze). **Kanapa** (mniej energii) czy **materac za 10 €**.
   - **Dzień 3 - urząd:** numer PPS wymaga adresu. **Dzień 4 - bank:** konto wymaga PPS i adresu, pętla się zamyka; monolog Kuby o Syzyfie.
   - **Dzień 5 - wyjście:** **list od Declana** (+Integracja) czy **podrobiony rachunek od znajomego Marka** (20 €). Monolog z dnia 4 odblokowuje trzecią opcję: zapytać Declana, kto jemu pomógł w Bostonie. Tak działają monologi filozofa z briefu.
   - **Dzień 6 - numer:** urzędniczka reaguje inaczej na list i inaczej na „rachunek z przyszłego miesiąca"; potem konto, umowa, paczka od mamy (słoik bigosu, +Energia, -Tęsknota) i **koniec rozdziału**.
   - Angielskie kwestie są po angielsku, a nieznane Kubie słowa **rozmyte** (`WordBlurFormatter`): słowo stoi na swoim miejscu, z długością i zarysem (litery zastąpione „zaślepkami” tej samej wysokości: wydłużenia w górę i w dół, wielkie litery, wąskie litery), złożone osobną czcionką `LiberationSans SDF - Blur` z szerokim polem odległości (40 pt, margines 20 px) i maksymalnie zmiękczonymi krawędziami, więc wygląda jak tekst poza ostrością i nie da się go odczytać; każda scena uczy kilku słów i odblokowuje slang („Grand", „Fair play", „What's the craic?", „I will, yeah", „..., so." w **„Słowniku slangu"**). Wybory zmieniają portfel i staty oraz zapisują **flagi fabularne**, do których nawiąże dalsza historia. Pomijając hub (idąc od razu do pracy), zaległe sceny i tak zagrają po kolei.
   - **Rozdział 2: Pierwsza wypłata** zaczyna się dzień po końcu Rozdziału 1 (dzień N to N maja 2004, więc od piątku 7 do środy 12 maja). Hub ogłasza: „Rozdział 1: Przyjazd - koniec. Od jutra Rozdział 2: Pierwsza wypłata.”:
     - **Dzień 7 - koperta:** pierwsza tygodniówka od Declana. Na co pójdzie: **czynsz na czas** (+Integracja, pani Byrne prawie się uśmiecha), **50 € do domu** na komunię siostry (-Tęsknota) albo **buty antypoślizgowe za 40 €** (+Energia, przydadzą się w niedzielę).
     - **Dzień 8 - Siobhán:** ruda kelnerka woła o klucz do „jacks” (slang: toaleta) i od „eejit”. Zaprasza całą kuchnię do pubu w poniedziałek: iść czy się wyspać.
     - **Dzień 9 - niedzielny brunch:** sto dwadzieścia porcji „full Irish”. W butach z dnia 7 Kuba stoi pewnie, w trampkach się ślizga. Nowy pomocnik Tunde tłucze stos talerzy: **wziąć to na siebie** (+Integracja, Tunde to zapamięta) czy milczeć (cała kuchnia zostaje do pierwszej).
     - **Dzień 10 - Mulligan's:** jeśli Kuba się zgodził, w pubie każdy stawia rundę: **postawić** (35 €, najwięcej Integracji), udawać, że nie zna zasad (kosztuje tyle samo, plus wstyd), albo **przyznać, że go nie stać** (za darmo; Siobhán: „Get the next one when you're rich”). Na koniec śpiewa się „The Fields of Athenry”. Jeśli nie, wieczór z Agnieszką i fiszkami (+Energia).
     - **Dzień 11 - Tomek:** współlokatora zwolnili z budowy. **Pożyczyć mu 30 €**, odmówić albo, **tylko z listem od Declana z Rozdziału 1**, zapytać Declana, czy nie weźmie go na zmywak.
     - **Dzień 12 - nóż:** kucharz od przygotowania wrócił do Cork, a Declan pyta, czy Kuba umie trzymać nóż. Jeśli w niedzielę wziął winę za Tunde, Declan o tym wie („You're sound”). Kuba może zgodzić się od razu albo poprosić o tydzień do namysłu. Wieczorem telefon do mamy i **koniec rozdziału**.
     - **Drogie wybory nie ruszają czynszu.** W scenie obowiązuje ta sama reguła co przy pubie i przelewach w hubie: wybór, który zabiera pieniądze, da się kliknąć tylko wtedy, gdy Kuba ma je ponad rachunki narosłe w tym tygodniu (`LifeLedger.CanSpare`). Inaczej przycisk jest wyszarzony z dopiskiem „nie stać cię - to pieniądze na rachunki”. Bieda widocznie zamyka drogi. Każdy wybór ma darmową opcję, np. w pubie „Mówię wprost, że mnie nie stać.” (pilnuje tego `StoryContentTests`).
     - Nowy slang: „jacks”, „eejit”, „deadly”, „sound”. Treść: `Assets/_Project/Editor/Chapter2Content.cs`. Wspólne klocki do pisania scen (`Graph`, `Node`, `Beat`) są w `StoryScript.cs`. Hub (`StoryBeatTrigger`) ma listę rozdziałów w kolejności opowiadania. Opowiadany jest pierwszy nieukończony, a po ostatnim nagłówek pokazuje „(ukończony)”.
   - **Rozdział 3: Swoi i obcy** (czwartek 13 – wtorek 18 maja) czyta to, co postanowiły dwa poprzednie rozdziały:
     - **Dzień 13 - deska:** pierwszy poranek przy krojeniu. Gavin, zastępca szefa kuchni, stawia obok minutnik. Kroić szybko (rozcięty palec i niebieski plaster) czy dokładnie, jak pokazał Declan? Kto w Rozdziale 2 poprosił o tydzień do namysłu, wybiera teraz między deską a zlewem.
     - **Dzień 14 - tydzień Tomka:** jeśli trafił na zmywak, tłucze talerze, a Kuba może zostać po zmianie i nauczyć go układać kosz. Jeśli pożyczył 30 €, oddaje je (+30 €) razem z butelką Żubrówki. Jeśli Kuba mu odmówił, Tomek się wyprowadził i zostawił kartkę.
     - **Dzień 15 - finał Eurowizji** (sobota 15 maja 2004, Stambuł): cały dom na jednej kanapie, Polska śpiewa Blue Café, Darius kibicuje Irlandii „z litości”. Kuba może ulepić ruskie pierogi dla wszystkich (12 €, mama przez telefon radzi, czym zastąpić twaróg) albo iść spać.
     - **Dzień 16 - „organizacja charytatywna”:** na latarniach plakaty przed referendum o obywatelstwie z 11 czerwca. Przy wydawce Gavin rzuca, że Declan prowadzi organizację charytatywną, a nie restaurację. Kuba może **pierwszy raz odpowiedzieć na głos po angielsku** („I wash. I cut. I learn. Enough?”), udać, że nie rozumie, albo, jeśli w Rozdziale 2 wziął winę za Tunde, spojrzeć na Tunde, który staje po jego stronie. Wieczorem Declan za drzwiami: w Bostonie nikt nie nazywał jego zmywania dobroczynnością.
     - **Dzień 17 - dwie koperty:** kto w Rozdziale 1 podrobił rachunek, dostaje pismo z urzędu. Pani Byrne podpisze potwierdzenie adresu za darmo, jeśli czynsz był płacony na czas („I will, yeah” pierwszy raz znaczy „tak”). Inaczej chce 20 € albo Kuba pisze pismo z Agnieszką. Od mamy przychodzi dyplom magistra filozofii, a jeśli Kuba wysłał pieniądze, także zdjęcie Oli z komunii.
     - **Dzień 18 - krytyczka:** recenzentka z „The Irish Times” przy stoliku 7. Kuba kroi albo „jest silnikiem” przy zlewie. Gavin parzy rękę i Kuba może rzucić swoją robotę, żeby mu pomóc. Recenzja: cztery gwiazdki. Declan: „Całe miasto chodzi teraz do restauracji”, czyli zapowiedź Celtyckiego Tygrysa. **Koniec rozdziału.**
     - Nowy slang: „giving out”, „your man / your woman”, „That's gas!”, „banjaxed”. Nowi mówiący z portretami: Gavin i Tunde, a Kuba po raz pierwszy mówi na głos (mówca `kuba`, bez „myśli”). Treść: `Assets/_Project/Editor/Chapter3Content.cs`.
   - **Rozdział 4: Celtycki Tygrys** zaczyna się **dwa lata później**. Gdy rusza jego pierwsza scena, kalendarz przeskakuje na 1 maja 2006, dokładnie w drugą rocznicę przylotu Kuby, a hub ogłasza: „Czas płynie. Jest 01.05.2006.”. Licznik przeżytych dni i rytm tygodniowych rachunków biegną dalej jak wcześniej, a pominięte lata są opowiedziane, nie rozegrane (`StoryChapter.startsOn`, `LifeLedger.SkipTo`):
     - **Dzień 19 - dwie rocznice:** Kuba prowadzi przystawki albo jest „szefem zlewu”, zależnie od Rozdziału 3. W drzwiach zaplecza staje Bartek z Białegostoku, oszukany przez agencję jak kiedyś Kuba. Można go wziąć pod skrzydła, dać mu numer do Marka albo, z listem od Declana z Rozdziału 1, poprosić Declana o szansę dla niego.
     - **Dzień 20 - „z planów”:** Marek z folderem apartamentów w Navan od 285 000 €, bo „banki dają sto procent, ceny tylko rosną”. Kuba może wpłacić **50 € rezerwacji**, odłożyć decyzję albo odmówić (Declan: „Mój ojciec kupił ziemię w 79. Stracił ją w 83.”). Tomek, jeśli trafił na zmywak, jest już brygadzistą na tej budowie.
     - **Dzień 21 - mocny złoty:** mama zauważa, że przelew starcza na mniej (euro spadło z 4,53 do 3,90 zł) i że w Polsce też jest już praca. Kuba może obiecać przylot w sierpniu, powiedzieć, że Dublin to jego dom, albo zmienić temat.
     - **Dzień 22 - Cathal Brugha Street:** Declan płaci połowę kursu w szkole kulinarnej DIT, a Kuba może dołożyć **80 €** i się zapisać.
     - **Dzień 23 - otwarcie:** druga restauracja Declana w szklanym budynku nad Liffey. Szampan za 400 €, deweloper zostawia kuchni 500 € napiwku (**+40 €** dla Kuby). Gavin zaprasza Kubę do nowego lokalu i pamięta, jeśli Kuba uratował mu rękę przy krytyczce.
     - **Dzień 24 - dźwigi:** „narodowy ptak Irlandii”, spacer z Tunde, jeśli są przyjaciółmi („pięćdziesiąt lat temu to oni siedzieli na statkach”), i whiskey z Declanem: „nic w tym kraju nie trwa, poza deszczem i zawistnikami”. **Koniec rozdziału.**
     - Nowy slang: „notions”, „gaff”, „Go way outta that!”, „begrudgers”. Nowa postać z portretem: Bartek. Treść: `Assets/_Project/Editor/Chapter4Content.cs`.
   - **Rozdział 5: Krach** przeskakuje na **poniedziałek 29 września 2008**: dwa tygodnie po upadku Lehman Brothers i dzień przed nocą, w której irlandzki rząd zagwarantował wszystkie depozyty w swoich bankach. Wraca tu to, co postanowił Rozdział 4:
     - **Dzień 25 - pusta sala:** jeśli Kuba poszedł z Gavinem, szklana restauracja zamyka się w piątek („połowa moich ludzi będzie na zasiłku przed świętami”). Jeśli został u Declana, w starej kuchni jest „fierce quiet”. Można zostać po zamknięciu i myć za darmo albo wyjść przed północą.
     - **Dzień 26 - gwarancja:** deweloper z Navan zbankrutował: „fundamenty i dziura w ziemi”. Marek stracił 30 tysięcy. Kto wpłacił rezerwację, stracił swoje 50 €, kto odmówił, czuje zimno, a kto „musiał przemyśleć”, myślał tak długo, że zdążyło upaść. Marek prosi o 50 € na czynsz.
     - **Dzień 27 - autobus do Kielc:** budowy stają, Tomek wraca do Polski, gdzie budują stadiony na Euro 2012. Kuba może dać mu 30 € na drogę, namawiać go, żeby został, albo zapytać o pracę dla kucharza w Polsce. Z certyfikatem z DIT czeka na niego hotel w Krakowie.
     - **Dzień 28 - lista:** Declan musi zwolnić dwie osoby albo wszyscy wezmą mniej godzin. Kuba może oddać swoje soboty (−40 €), milczeć albo, jeśli zaopiekował się Bartkiem, wstawić się za nim.
     - **Dzień 29 - tata dzwoni:** pierwszy raz od czterech lat. „Pokój jest. I półka na ten twój dyplom.” Nowy portret: tata z wąsami i słuchawką.
     - **Dzień 30 - wybór:** dworzec Busáras, autobus do Kielc, nieruchome dźwigi. **Zostać w Dublinie, wrócić do Polski** (z certyfikatem do hotelu w Krakowie, bez niego „bez planu”) **albo**, jeśli Kuba poszedł z Gavinem, **polecieć z nim do Perth**, gdzie kopalnie szukają kucharzy. Flagi `ch5_stays` / `ch5_goes_home` / `ch5_goes_to_perth` czekają na epilog. **Koniec rozdziału.**
     - Nowy slang: „on the dole”, „fierce”, „brutal”, „Ah, sure look”. Treść: `Assets/_Project/Editor/Chapter5Content.cs`.
   - **Epilog: Listopad 2010** przeskakuje na **poniedziałek 29 listopada 2010**, dzień po tym, jak Irlandia przyjęła 85 miliardów euro pomocy z Unii i MFW. Każda z pięciu scen idzie jedną z **trzech dróg**, zależnie od wyboru z Rozdziału 5:
     - **Dublin:** Declan („moje kolana są starsze niż dług tego kraju”) proponuje Kubie kuchnię z jego nazwiskiem na drzwiach.
     - **Kraków:** z certyfikatem z DIT zastępca szefa kuchni w hotelu przy Rynku, bez niego od zlewu do zmiany, i „Irlandczyk” dla kucharzy. Mama pyta o Wigilię, a karp w wannie ma na imię Declan.
     - **Perth:** obóz górniczy w Pilbarze, 40 stopni, 1200 posiłków dziennie, a Gavin tęskni za dublińskim deszczem.
     - Dalej, w każdej drodze:
       - Wieści z daleka: pocztówka od Tomka spod stadionu we Wrocławiu, Skype z rodzicami albo mail od Declana („the country's broke and the rain is fine”). Można wysłać paczkę (25 €, w Krakowie 100 zł, w Australii 36 AUD).
       - Gdzie wylądowali inni: Marek jeździ taksówką i oddaje pożyczone w kryzysie 50 €, Tunde liczy dług Irlandii („same plates, bigger numbers”), a Bartek gotuje na zmianie Kuby.
       - **Nowy chłopak przy zlewie**, teraz ucznia ma Kuba: Thiago z Brazylii w Dublinie, Ołeh ze Lwowa w Krakowie albo Liam z Cork w Australii. Kuba może go wszystkiego nauczyć, krzyczeć jak Declan albo powiedzieć „każdy kiedyś od tego zaczynał”.
       - **Ostatni talerz** i Camus: „trzeba wyobrażać sobie Syzyfa szczęśliwym”. **Koniec gry.**
     - Treść: `Assets/_Project/Editor/Chapter6Content.cs`. Trzy nowe portrety: Thiago, Ołeh, Liam.
     - **Życie po epilogu, w miejscowych pieniądzach** (od 2.10.2026; `Place`, `LifeRules.For`, `LifeLedger.MoveTo`). Razem ze skokiem kalendarza do listopada 2010 Kuba przeprowadza się tam, dokąd zabrał go wybór z Rozdziału 5 (`StoryChapter` → przeprowadzki: `ch5_goes_to_perth` → Pilbara, `ch5_goes_home` → Kraków). Portfel zamienia się na nową walutę po średnim kursie NBP z 2010 roku (3,99 zł albo 1,44 AUD za euro), a nowy właściciel mieszkania oznacza czyste konto: bez zaległości i bez hostelu. Od tej chwili hub, wypłata i rachunki liczą w miejscowych pieniądzach.
       - **Dublin** zostaje jak był: euro, irlandzka płaca minimalna 8,65 €/h, pokój za 86 € na tydzień.
       - **Kraków (zł):** hotelowa kuchnia przy Rynku płaci 18 zł/h, czyli ok. 3000 zł brutto miesięcznie przy płacy minimalnej 1317 zł.
         - Rachunki tygodnia to 337 zł: pokój na Kazimierzu 180, jedzenie 120, bilet MPK 22 (miesięczny kosztował 89 zł) i telefon 15.
         - Piwo z ekipą kosztuje 40 zł. Przelew (100 zł naraz) trafia do rodziców złotówka w złotówkę.
         - Tęsknota rośnie o 1 dziennie zamiast 2, bo dom jest pociąg dalej.
       - **Pilbara (AUD):** kuchnia obozu górniczego płaci 32 AUD/h.
         - Obóz daje łóżko i jedzenie, więc rachunki to 250 AUD: pokój w Perth na tygodnie wolne 150, jedzenie 50, dojazdy 30 i internet na Skype'a 20.
         - Wieczór w kantynie kosztuje 30 AUD. Przelew to 50 AUD i dociera jako ok. 139 zł.
         - Tęsknota rośnie o 3 dziennie, bo to drugi koniec świata.
       - Drobne kwoty zmiany (napiwki, potrącenia za stłuczki, premia za deskę) to „dublińskie euro” razy `PlaceMoney.SmallSums`: ×4 w Krakowie, ×1,5 w Pilbarze.
       - Kwoty w scenach są zapisane w euro i przychodzą w miejscowej walucie po kursie roku: 50 € od Marka to ok. 200 zł albo 72 AUD. Paczka z Krakowa kosztuje więc 100 zł, a prezent z Australii 36 AUD, i tak mówią o tym teksty scen.
   - **Po latach bariera językowa znika:** rozdziały z datą startu (4, 5 i epilog) są „płynne”. Każda scena zaczyna się od węzła, który uczy Kubę wszystkich angielskich słów, jakie w niej padną (`StoryScript.SaveGraph(..., fluent: true)`), więc nic nie jest zamazane. Rozdziały 1–3, z pierwszych tygodni, zostają z barierą, bo o nią w nich chodzi. Pilnuje tego `StoryContentTests`.
   - **Rok w grze ma znaczenie:**
     - Płaca to irlandzka płaca minimalna z danego dnia: 7,00 € za godzinę od lutego 2004, 7,65 € od maja 2005, 8,30 € od stycznia 2007 i 8,65 € od lipca 2007 (`ShiftResultCalculator.HourlyWageOn`; kuchnia czyta datę przez `IPlayerCondition.Today`).
     - Czynsz za łóżko w dzielonym pokoju rośnie w boomie i spada po krachu: 80 € tygodniowo w 2004, 92 € w 2006, 104 € w 2008, 86 € w 2010 (`LifeRules.WeeklyRentIn`).
     - Kurs złotego do przelewów był zależny od roku już wcześniej.
4. Po dialogu widzisz normalny hub: dzień/datę, portfel (start: 20 EUR), staty (Energia/Tęsknota/Integracja) i akcje:
   - **„Zadzwoń do mamy"** — obniża Tęsknotę, trochę podnosi Energię, pokazuje „Jesz coś ciepłego?". **Raz na wieczór** (potem przycisk gaśnie do następnego dnia; restart aplikacji tego nie obchodzi).
   - **„Pub z ekipą (-10 EUR)"** — kosztuje 10 EUR, podnosi Integrację, kosztuje trochę Energii. Raz na wieczór i **nie za pieniądze na rachunki**: jeśli po piwie w portfelu zostałoby mniej, niż narosło już rachunków w tym tygodniu (165 EUR × dni tygodnia, które minęły / 7), Kuba odmawia („Kuba nie przepije czynszu").
   - **„Wyślij 20 EUR do domu"** — przelicza po kursie z bieżącego roku w grze (średnie roczne NBP z dziennych kursów tabeli A, 2004-2012, dla euro i dolara australijskiego, pobrane z api.nbp.pl) i pokazuje ile PLN dotarło. Tak samo nie ruszy pieniędzy na narosłe rachunki: wyśle najwyżej nadwyżkę ponad nie (pierwszego dnia tygodnia wszystko, siódmego — ponad 141 EUR).
   - **„Słownik slangu"** — pokazuje odblokowane dotąd zwroty z wyjaśnieniem po polsku.
   - **„Idź do pracy"** → scena kuchni.
5. W kuchni (układ pionowy pod telefon: u góry stojak (siatka 6×4) i zmywarka, pośrodku po lewej kuweta z Górą naczyń, na dole zlew — tam, gdzie leży kciuk; kamera pod kątem, żeby wysokość Góry była widoczna):
   - Naczynia **spadają fizycznie** do kuwety i układają się w Górę. Zmiana zaczyna się od **2 naczyń** czekających w kuwecie, potem przychodzi jedno co **18 s**, a odstęp równo kurczy się do **9 s** pod koniec zmiany; kolejka typów się powtarza, więc roboty nie brakuje do ostatniej sekundy (`ShiftDefinition`: `openingDishes`, `spawnIntervalStartSeconds`/`EndSeconds`, `repeatQueue`). Nowicjusz (~30 s na naczynie) poczuje rosnącą Górę w drugiej połowie zmiany, wprawny zmywak (~15 s) nadąża prawie do końca. Wcześniej całe 7 naczyń spadało w pierwszych 36 s i Góra zawalała się, zanim dało się umyć drugie. Pasek **„Góra"** przy lewej krawędzi pokazuje, jak blisko jest zawalenia (zielony → żółty → czerwony, miga tuż przed progiem). Szkło i porcelana, które spadną z blatu, tłuką się, a stal wraca na Górę (patrz „Góra i fizyka naczyń”).
   - Dotknięcie naczynia w Górze przenosi je **do zlewu**, powiększone ~2×, żeby było wygodnie szorować palcem. W zlewie mieści się jedno naczynie naraz — kolejne da się wziąć dopiero po odłożeniu poprzedniego na stojak. Podpowiedź nad narzędziami mówi, co zrobić dalej i które narzędzie działa na pozostały brud.
   - **Talerz** (brudny od sosu) — potrzebuje narzędzia **Gąbka**.
   - **Patelnia** (przypalone + tłuszcz) — dwie warstwy brudu: **Druciak**, potem **Płyn**.
   - **Miska** (zaschnięta owsianka) — najtwardszy brud w kuchni (trudność 3,2, sos ma 1,2). Na sucho **Gąbka** albo **Druciak** idą bardzo wolno; po **moczeniu** w zlewie zostaje jedna trzecia tej trudności. Podpowiedź sama to podsuwa („Zaschnięte! Odstaw do moczenia…”). Biała w środku z podwójną niebieską linią pod brzegiem, z zewnątrz kobaltowa.
   - **Kubek** (osad z kawy) — cienki brązowy film, który na ściankach układa się w obrączki: **Płyn** albo **Gąbka**. Butelkowo zielony z uchem, w środku biały, na stojaku zajmuje jedno pole.
   - **Garnek** (przypalone + tłuszcz) — stalowy, z dwoma uchwytami. Największe naczynie: **3×3** pola stojaka, więc przy pełnym stojaku trzeba najpierw puścić zmywarkę.
   - Nowe naczynia wchodzą do zmiany po pierwszych trzech (talerz, patelnia, szklanka), na których uczą podpowiedzi. W trybie dla daltonistów owsianka jest purpurowa, a kawa niebieskozielona (paleta Okabe-Ito).
   - **Narzędzia** (pasek na dole, od lewej): **Płyn** (płyn do naczyń na szczotce, na tłuszcz), **Gąbka** (sos, zacieki), **Druciak** (przypalone) oraz dwa szybsze, ale niedokładne:
     - **Skrobak** — ok. 2,5× szybszy od druciaka na przypalone, ale zostawia cienką warstwę (ok. 30% grubości), którą trzeba dokończyć druciakiem. Odpryski skorupy, niższy zgrzyt niż druciak.
     - **Prysznic** — słuchawka ciśnieniowa: strumień ok. 1,8× szerszy niż gąbka, dobry na sos i tłuszcz, słaby na przypalone. Spłukuje gruby brud, ale zostawia ok. 12% warstwy do dokończenia gąbką albo płynem. Woda leci, póki palec trzyma prysznic na naczyniu.
     - Przy skrobaku i prysznicu podpowiedź mówi, czym dokończyć („Skrobak zdejmie wierzch. Dokończ: Przypalone jedzenie → Druciak").
     - W danych: `ToolDefinition.brushRadiusScale` (szerokość) i `residueFloor` (do jakiej grubości narzędzie schodzi; 0 = do czysta). Autoplay i testy wybierają tylko narzędzia, które czyszczą do zera.
   - **Radio na ścianie** (od 2.10.2026; `KitchenRadio`, `RadioMusic`, `RadioNews`, `RadioWidget`). Mały przenośny odbiornik pod miejscem na baner szefa: kratka głośnika, antena, lampka i podświetlona skala. Stuknięcie przekręca gałkę: **Trad → Pop → Wiadomości → wyłączone**, a wybór zostaje na następną zmianę (w ustawieniach gracza).
     - **Muzyka syntetyzowana w kodzie**, bez plików (w przeciwieństwie do dźwięków kuchni, które są nagraniami CC0). Każda stacja to pętla bez trzasku na styku.
       - Trad: jig w D w metrum 6/8 (116 ćwierćnut z kropką), skrzypcowa melodia z vibrato nad burdonem D–A i bodhránem.
       - Pop: akordy I–V–vi–IV w G na elektrycznym pianinie FM, bas, stopa, werbel i hi-hat, 100 BPM.
       - Wiadomości: dżingiel, a potem głos spikera z filtrowanego szumu, rozpoznawalny jako głos, ale nie jako słowa.
       - Wszystko przechodzi przez filtr „małego głośnika”: bez basu (poniżej ~200 Hz), bez góry (powyżej ~4 kHz), z lekkim przesterem. Gra cicho pod kuchnią i milknie z wyłączonym dźwiękiem.
     - **Wiadomości zależą od daty w grze** i przewijają się w okienku skali:
       - 2004: Unia otwiera rynek pracy, zakaz palenia, finał Eurowizji, referendum;
       - 2006: ceny domów, mundial, dźwigi;
       - jesień 2008: gwarancja bankowa, budowy stają, Polacy wracają;
       - koniec 2010: 85 mld z UE i MFW, cięcia, śnieg.
     - **Po przeprowadzce radio jest miejscowe** (`RadioNews.For(data, miejsce)`, `RadioStations.Label`).
       - Kraków: stacje „Folk”, „Pop” i polskie wiadomości: jarmark na Rynku, otwarcie niemieckiego i austriackiego rynku pracy w maju 2011, beatyfikacja, prezydencja Polski w UE, Euro 2012 z Irlandią w Poznaniu i Gdańsku.
       - Pilbara: „Bush”, „Hits” i australijskie „News”: rekordowa ruda żelaza, powodzie w Queensland, cyklon Yasi, podatek górniczy od lipca 2012.
     - Odsłuch poza grą: **Zmywak → Dev → Export radio WAVs** zapisuje stacje do `Logs/Radio/*.wav`.
   - **Deska przed zmianą** (od 2.10.2026; `PrepRound`, `PrepBoardPanel`). Jeśli Kuba wziął nóż Declana w Rozdziale 2 albo deskę w Rozdziale 3, każda zmiana zaczyna się od przygotowania, jak zapowiedział Declan: „dwie godziny przed każdą zmianą, potem zlew”. Przycisk w hubie mówi wtedy „Idź do pracy: deska i zlew”. Kto w Rozdziale 3 został przy zlewie, zmywa jak dawniej.
     - **Zasada:** ręka prowadząca pcha warzywo (cebula, marchew, seler) pod nóż w stałym tempie, a każde stuknięcie w kartę opuszcza nóż i odcina to, co przeszło za ostrze. Zielona kreska 3 mm za ostrzem pokazuje, kiedy ciąć.
     - **Kostki:** cięcie co pół sekundy daje równą kostkę 3 mm (±1 mm). Spóźnione cięcie daje za grubą, wczesne za cienką. Cięcie dużo za wcześnie, poniżej 1,2 mm, trafia w pazur: plaster na palcu i 2,5 s przerwy. Kto nie tnie wcale, temu warzywo przejeżdża pod nożem kawałem po 9 mm. Podwójne stuknięcie, gdy nóż jeszcze się podnosi, nic nie robi.
     - **Zmęczenie:** zmęczony Kuba pcha nierówno, więc cięcie „na pamięć” co pół sekundy zawodzi częściej, ale patrzenie na kreskę dalej działa.
     - **Nagroda:** do 6 € premii (równość × ukończenie, minus 1 € za każdy plaster, do pół euro), doliczanej do wypłaty jako „deska”. Na koniec werdykt Declana albo Gavina, od „Trzy milimetry. Każdy.” po „Moja babcia kroi szybciej…”.
     - **Kuchnia czeka:** podczas deski nie ma zegara ani naczyń. Zmiana rusza po „Do zlewu”. Pauza zatrzymuje także deskę. Plan przechodzi z huba do kuchni przez `ShiftPlan`, który kuchnia zabiera raz, więc kuchnia odpalona sama (testy, edytor) ma zwykłą zmianę. Dźwięk cięcia („Chop”) to nagranie noża o deskę, a wibracje są przy każdym cięciu (mocniej przy palcach).
   - **Moczenie w zlewie:** z naczyniem w zlewie (po zeskrobaniu resztek) przycisk **„Odstaw do moczenia”** nad przyciskiem dużej zmywarki opuszcza je na dno zlewu, pod wodę, a ręce są wolne na następne naczynie z Góry.
     - Woda płucze naczynie, a im dłużej leży (do **20 s**), tym łatwiej schodzi brud: po pełnym moczeniu każdy brud szoruje się z **35%** trudności. Nad naczyniem pękają bąbelki, wejście do wody robi plusk.
     - Moczy się jedno naczynie naraz, pod miejscem, w którym trzymasz naczynie do mycia. Gdy ręce są zajęte, przycisk pokazuje tylko postęp („Moczy się · 45%”).
     - Gdy ręce są wolne, **„Wyjmij z wody”** albo **dotknięcie naczynia w wodzie** podnosi je z powrotem do mycia. Blat ma jeden collider także nad otworem zlewu, więc `DishInputRouter` szuka moczonego naczynia osobnym promieniem.
     - Model: `DishInstance.Soak` (`FullSoakSeconds`, `SoakedDifficulty`, testy w `DishInstanceTests`); scena: `WashStation.Soaking`, `DishController.TrySoak` / `TryTakeOutOfSoak`, `SoakPanel`.
   - **Szklanka** (tłuszcz) — przezroczysta: czyste szkło przepuszcza to, co za nim, a tłuszcz je mętnieje (materiał `PlaceholderLitGlass`: alfa tekstury to krycie, nie połysk). **Płyn**, a potem **pod światło**: gdy tłuszcz zejdzie, szklanka nie jest jeszcze czysta. Podpowiedź woła „Pod światło!", szklanka dzwoni, a po niej przesuwa się jasne odbicie lampy. Na telefonie sterujesz nim **przechylając telefon** (pozycja z chwili rozpoczęcia sprawdzania liczy się jako poziom, więc działa niezależnie od tego, jak trzymasz telefon); bez akcelerometru albo z wyłączonym przechylaniem odbicie wędruje po szklance samo, a **przeciągnięcie palcem obok szklanki** (nie po niej) przesuwa je tam, gdzie chcesz — po puszczeniu odbicie płynie dalej w tę samą stronę. W Edytorze działa to myszką. **Zacieki** (cienkie smugi po schnięciu) widać tylko w tym odbiciu, jako matowe pasy łamiące blask; zetrzyj je **Gąbką**. Dopóki na szklance jest tłuszcz, zacieków nie ma czym zetrzeć, bo powstają dopiero przy schnięciu. Zmywak nie ominie więc sprawdzenia, szorując wszystko naraz.
   - Kolejność na każdym naczyniu: **swipe** (usuń resztki) → **przytrzymaj** (spłucz, ~1s) → **szoruj** — brud schodzi tylko tam, gdzie przesuwasz palec, więc trzeba przejechać całe naczynie wybranym narzędziem (wybrane jest podświetlone na zielono); ostatnie ~5% warstwy znika samo → **przeciągnij** naczynie na wolne miejsce na stojaku (chybione upuszczenie odsyła je z powrotem do zlewu).
   - **Stojak to siatka 6×4 komórek** i każde naczynie zajmuje tyle, ile naprawdę ma: szklanka 1 komórkę, talerz 2×2, patelnia z rączką 4×2. Podczas przeciągania świecą na zielono dokładnie te komórki, na które naczynie spadnie po puszczeniu; patelnia sama obraca się o 90° (rączka do tyłu), jeśli bokiem pasuje bliżej palca, np. w wąską szczelinę 2×4. Luźno porozrzucane talerze potrafią zostawić pół stojaka wolnego, a mimo to bez miejsca na patelnię. Nad stojakiem licznik **„Kosz: 12/24"**; gdy nic już się nie mieści, podpowiedź każe uruchomić zmywarkę.
   - Gdy stojak ma choć jedno naczynie, przycisk **„Uruchom zmywarkę"** robi się aktywny (~20s cyklu; lampka na zmywarce świeci na pomarańczowo, pasek na panelu się zapełnia). Cały wkład **wjeżdża do maszyny**, a stojak od razu jest wolny na następny kosz; po cyklu naczynia liczą się jako umyte. **Pełny kosz** (co najmniej 18 z 24 komórek) przy starcie cyklu podbija Flow (dymek „Pełny kosz!"), dzwoni i daje 1 EUR napiwku (wynik zmiany pokazuje „Pełne kosze"). Stąd decyzja: odpalić zmywarkę od razu czy dopakować kosz, gdy Góra rośnie.
   - **Duża zmywarka** (na blacie po prawej, między Górą a zlewem):
     - Brudne naczynie ze zlewu, przed szorowaniem, wkładasz do niej przyciskiem **„Do dużej zmywarki (12/40)”** nad dolnym panelem. Mieści **40 naczyń** i widać je przez przydymioną szybę.
     - **„Uruchom”** na karcie obok maszyny startuje cykl, który trwa **30 minut prawdziwego czasu** i biegnie dalej po końcu zmiany, wieczorem w domu i przy zamkniętej grze. Zapisuje się z grą (`SaveData.bigDishwasherLoad`, `bigDishwasherStartedUtcTicks`, godzina startu w UTC).
     - Po czasie lampka robi się zielona, a **„Wypakuj”** w kolejnej zmianie dolicza czyste naczynia jako umyte w tej zmianie (z napiwkami), też do „Umyte: X / Y” i wyniku zmiany.
     - Na czas cyklu maszyna nie przyjmuje naczyń.
     - Wybór: szorowanie od razu daje Flow i zamówienia szefa, a duża zmywarka oszczędza pracę, ale płaci dopiero za pół godziny.
     - Model: `LongCycleDishwasher` (czysty C#, testy w `LongCycleDishwasherTests`); scena: `BigDishwasherController` i `BigDishwasherPanel`.
     - Dotyk zaczęty na przycisku interfejsu nie chwyta już naczynia pod nim (`DishInputRouter.IsOverControl`), bo karta leży nad blatem.
   - **Awaria zmywarki:** w połowie cyklu maszyna potrafi stanąć — łupnięcie, kłąb ciemnego dymu, lampka mruga na czerwono, pasek postępu staje, a przycisk zmienia się w pulsujące czerwone **„Napraw zmywarkę!"**. Zegar zmiany leci dalej, a Góra rośnie. Przycisk otwiera kartę z widokiem do środka maszyny; każda z trzech usterek naprawia się jednym z gestów zlewu: **zapchany filtr** — złap każdą resztkę i odrzuć ją machnięciem, **zablokowane ramię spryskiwacza** — kręć palcem wokół środka (ramię najpierw stawia opór, potem się rozluźnia; bujanie w przód i w tył też działa), **wywalony bezpiecznik** — przytrzymaj RESET przez 1,5 s (puszczony za wcześnie odskakuje). Po naprawie maszyna rusza z buczeniem, a cykl kończy się od miejsca, w którym stanął. Kartę można zamknąć („Wróć do zlewu") i dokończyć naczynie; postęp naprawy zostaje. Pierwsza zmiana ma awarię zawsze na **2. cyklu** (żeby każdy ją poznał), potem każdy cykl ma 20% szans, maks. 2 na zmianę; pierwszy cykl nigdy nie psuje się losowo (`ShiftDefinition`: `scriptedBreakdownCycle`, `breakdownChancePerCycle`, `maxBreakdowns`).
   - **Góra i fizyka naczyń** (od 1.10.2026; `DishPhysics`, `DishPileController`):
     - **Każde naczynie ma materiał i wagę.** Talerz to porcelana 0,55 kg, miska 0,45, kubek 0,35, szklanka to szkło 0,25 kg, a patelnia, garnek i emaliowana brytfanna to stal: 1,3, 1,6 i 1,2 kg. Ciężki garnek spycha szklankę, nie odwrotnie. Stal ślizga się łatwiej i odbija trochę mocniej, a porcelana trzyma się stosu.
     - **Tłucze się od uderzenia, nie z losowania.** Szkło pęka przy uderzeniu od 3,2 jednostki na sekundę (liczy się prędkość wzdłuż normalnej kontaktu, więc szuranie po blacie nie tłucze), porcelana od 3,8. Upadek ze szczytu Góry (~1,4) to ok. 5,2, więc tłucze jedno i drugie, ale ześlizgnięcie się o pół jednostki w dół stosu już nie. **Stal się nie tłucze**, tylko dzwoni brzękiem metalu („Clang”), a szkło brzmi wyżej niż porcelana. Stłuczka rozbija naczynie w miejscu uderzenia.
     - **Bez kary za nie swoje:** świeżo wrzucone naczynie nie tłucze się przy pierwszym lądowaniu i nie tłucze tego, w co spada. Kelner je kładzie, nie rzuca. Dopiero naczynie, które raz osiadło w stosie, może zostać strącone i pęknąć.
     - **Zawalenie jest fizyczne:** gdy Góra urośnie do 1,4, nie znikają już losowe naczynia, tylko jej wierzch (wszystko w górnych 0,45) zostaje zepchnięty na zewnątrz, lekko w górę i z obrotem. Fizyka decyduje o reszcie: co spadnie twardo na blat, pęka, co wyląduje na stosie albo jest ze stali, przetrwa. Potrącenie to nadal 3 EUR za stłuczone naczynie, a między zawaleniami są ~3 s przerwy.
     - **Upadek z blatu:** szkło i porcelana rozbijają się na podłodze, a stalowa patelnia z brzękiem wraca na Górę (do ponownego mycia, bez kary). W trybie relaks nic się nie tłucze i nic się nie zawala.
     - **Stabilniejszy stos:** 12 iteracji solvera na naczynie (cienkie talerze na patelniach nie drgają), ciągłe spekulatywne wykrywanie kolizji (talerz zrzucony z Góry nie przenika przez blat) i łagodne rozpychanie nachodzących na siebie brył (maks. 3 jednostki/s, zamiast „wystrzeliwania” nowego naczynia ze środka stosu).
     - Dziennik zmiany liczy zawalenia osobno od stłuczek (`DishPileController.OnCollapsed`).
   - **Rodzaje zmian, ocena i wypłata** (ETAP 1, od 6.10.2026; `ShiftSchedule`, `ShiftCatalog`, `ShuffleBag`, `ServiceWaves`, `ShiftScoring`, `ShiftResultCalculator`, `ChefOrderController`). Kolejne dni nie są już tą samą zmianą.
     - **Pięć rodzajów zmian** (`Assets/_Project/ScriptableObjects/Shift_*.asset`, katalog w `Resources/ShiftCatalog.asset`):

       | Rodzaj | Naczyń na zmianę (tempo 1 / 0,85) | Norma | Fale | Pilne zamówienia | Szansa awarii na cykl |
       |---|---|---|---|---|---|
       | Spokojny początek | 11 / 12 | 5 / 6 | jedna łagodna | brak | 5% |
       | Zwykła zmiana | 16 / 18 | 8 / 9 | dwie równe | 25% zamówień | 10% |
       | Lunch | 18 / 21 | 9 / 11 | wielka na początku, mała później | 35% | 10% |
       | Kolacja | 18 / 21 | 9 / 11 | mała, potem wielka pod koniec | 35% | 12% |
       | Ciężki wieczór | 22 / 26 | 11 / 12 | trzy naloty, garnki i brytfanny | 50% | 18% (do 2 na zmianę) |

     - **Harmonogram** (`ShiftSchedule`, decyduje dzień fabuły): dni 1–3 spokojne; 4–7 zwykłe z pierwszym lunchem; 8–18 zwykłe, lunch i kolacja na zmianę; 19–30 dochodzą ciężkie wieczory; od 31 przeważają rushe i ciężkie wieczory. Tempo (odstępy między naczyniami) zaciska się od dnia 8 do ×0,85 w dniu 35, a norma rośnie razem z nim (maks. 12). Wesele i Dzień św. Patryka wypadają na kolację, niedzielna taca na lunch, inspekcja na zwykłą zmianę (żeby jej nie zdradzić). Przycisk w hubie pokazuje rodzaj zmiany i normę; w kuchni karta obok radia mówi, co to za wieczór, potem pokazuje normę i fazę ruchu.
     - **Kolejność naczyń** (`ShuffleBag`): miks zmiany to wagi (ile czego na rundę). Każda runda ma dokładnie ten skład, w kolejności tasowanej ziarnem dnia, nigdy więcej niż dwa naczynia tego samego rodzaju z rzędu. Pierwszy dzień zaczyna się od talerza, patelni i szklanki, na których uczą podpowiedzi. Kuchnia uruchomiona sama (edytor, testy) gra stałą zmianę `vertical_slice` jak dotąd.
     - **Fale serwisu** (`ServiceWaves`): w środku fali naczynia przychodzą do 1,9× szybciej, z płynnym narastaniem i wygasaniem. Fale przyspieszają też zamówienia szefa, a w szczycie pilne są 1,5× częstsze. HUD woła: „Sala się zapełnia...”, potem okrzyk fali („Lunch: sala pełna!”), potem „Trochę oddechu.”
     - **Pilne zamówienia:** czerwony, pulsujący baner „PILNE: SZKLANKA NA STOJAK! (30s)”, dzwonek i wibracja. Szef bierze naczynie prosto ze stojaka, więc liczy się w chwili odłożenia czystego naczynia do kosza, bez cyklu zmywarki. Na to jest 30 s. Gracz decyduje: domyć naczynie w rękach czy odstawić je do moczenia i zająć się priorytetem. Nigdy dwa pilne z rzędu. Przegapione zeruje Flow i kosztuje punkty.
     - **Awarie zmywarki:** pierwsza jest samouczkowa. Drugi cykl pierwszej zmiany od dnia 2 psuje się zawsze, dopóki Kuba raz nie naprawi maszyny. Potem już tylko losowo, z szansą zależną od rodzaju zmiany, nigdy w pierwszym cyklu i z co najmniej trzema czystymi cyklami między awariami. Dzień po awarii szansa spada do 35%. Pamięta to zapis (`breakdownTutorialDone`, `lastShiftBrokeDown`).
     - **Wynik (0–100) i ocena S/A/B/C/D** (`ShiftScoring`):
       - **Praca, do 50 pkt:** umyte naczynia względem normy. Norma przebita o ćwierć daje jeszcze do 10 pkt.
       - **Staranność, do 15 pkt:** udział szorowania właściwym narzędziem do brudu, minus 2 pkt za każde odłożenie obok stojaka.
       - **Flow, do 10 pkt:** pełna pula od serii 4.
       - **Pilne zamówienia, do 15 pkt:** proporcjonalnie do zrealizowanych; zmiana bez pilnych dostaje pełną pulę.
       - **Kary:** −8 pkt za stłuczkę (maks. −24), −6 za zawalenie Góry (maks. −12).
       - **Progi:** S od 90, A od 75, B od 60, C od 40, niżej D. Ocena zawsze liczy pełną normę zmiany, więc wyjście przed czasem ją obniża. Premia pieniężna za normę liczy się za przepracowaną część.
       - Zmiana z normą i bez stłuczki to co najmniej B; gorzej niż D być nie może.
     - **Wypłata** (`ShiftResultCalculator`):
       - **Stawka:** płaca minimalna × 6 h, jak dotąd. Nigdy nie spada przez słabą grę.
       - **Premie:** norma +1 h stawki, ocena S +1 h, ocena A +0,5 h, plus napiwki jak dotąd (0,25 € za naczynie, 1 € za pełny kosz).
       - **Stłuczki:** 3 € każda (×2 na inspekcji), ale najwyżej 30% stawki. Wcześniej katastrofa mogła zjeść całą dniówkę.
       - Wszystkie premie liczone są za przepracowaną część zmiany i w pieniądzach miejsca.
       - Przykład z 2004: słaba zmiana ok. 37 €, zwykła z normą ok. 49 €, mistrzowska ok. 61 €.
       - Karta wyniku pokazuje dużą literę oceny, punkty, komentarz, normę, pilne zamówienia, staranność i rozbicie wypłaty.
     - **Symulator ekonomii** (`EconomySimulationTests`) liczy teraz normę i ocenę każdego dnia. Wprawny gracz ma 45 z 56 nocy na S, przeciętny głównie B/C, a słaby, tłukący dwa naczynia na zmianę, głównie D, ale nikt nie wpada w długi.
       - Limit oszczędności dla przeciętnego gracza zostaje 10 tygodni rachunków.
       - Dla mistrza podniosłem go do 16. To świadoma decyzja: nagroda za dobrą grę musi być widoczna w portfelu.
   - **Zmiany specjalne** (od 2.10.2026; `SpecialShifts`, `ShiftController`, `SpecialShiftBadge`). Kalendarz w grze decyduje, która zmiana jest wyjątkowa, więc trafiają się w tygodniach fabuły i po niej. Pierwszy tydzień jest spokojny, żeby Kuba nauczył się zlewu. Obok radia karta w kolorze zmiany przez kilka sekund mówi, co się dzieje, a potem pokazuje postęp. Przycisk w hubie zapowiada zmianę, poza inspekcją, bo ta jest niezapowiedziana.
     - **Wesele (soboty, nie w obozie górniczym):** sto dwadzieścia osób, talerze i kieliszki falami. Trzy naczynia więcej na start, odstępy krótsze o 30%, napiwki ×1,5, a na koniec koperta od młodych: 15 €.
     - **Inspekcja sanepidu (co dziewiąty dzień, w dzień roboczy):** stłuczka kosztuje podwójnie. Zmiana bez stłuczki i bez zawalenia Góry daje 10 € od Declana; po pierwszej wpadce karta robi się czerwona: „Kontrola niezaliczona”.
     - **Legendarna Taca Lasagne (niedziele):** po ćwierci zmiany na Górę spada stara stalowa taca Declana: większa od brytfanny (4×3 na stojaku), z warstwami zapieczonego sera, przypalenizny i tłuszczu. Domyta ręcznie daje zrzutkę sali, 10 €. Wrzucona do dużej zmywarki — nic.
     - **Dzień św. Patryka (17 marca, tylko w Dublinie):** najtłoczniejszy wieczór roku: dwa naczynia więcej na start, odstępy krótsze o 40%, prawie same szklanki po Guinnessie i podwójne napiwki. W kalendarzu fabuły nie wypada, więc spotka tylko Kubę, który został w Dublinie, w marcu 2011.
     - Premie są w „dublińskich euro” (×4 w Krakowie, ×1,5 w Pilbarze) i liczą się za przepracowaną część zmiany: wyjście w pierwszej minucie inspekcji nic nie daje. Ekran wyniku ma o nich osobną linijkę. Symulator ekonomii liczy zmiany specjalne tak samo: kopertę zawsze, inspekcję tylko bez stłuczki, tacę każdemu poza nowicjuszem.
     - Podgląd: `SpecialShiftPlayModeTests` robi zrzuty `36_special_wedding.png` i `37_special_lasagne_tray.png`, a jawny test `ArtPreview_LegendaryTray` pokazuje tacę na Górze i w zlewie.
   - **Zamówienie szefa:** żółty baner na górze ekranu („Szef woła: PATELNIE! (54s)") pojawia się co 20-35s — umyj i wrzuć do zmywarki naczynie tego typu w oknie czasowym, żeby podbić serię **Flow** (lewy górny róg). Okno = cykl zmywarki (20 s) + 35 s na reakcję (`ChefOrderController.reactionSeconds`, do strojenia) — wcześniej wynosiło tyle samo co cykl, więc zamówienia praktycznie nie dało się spełnić. Zignorowane zamówienie lub stłuczenie zeruje Flow. Szef woła tylko o to, z czym da się coś zrobić od razu: naczynie w zlewie albo leżące odkryte na Górze (ten sam test promienia z kamery co dotyk); jeśli nic takiego nie ma, pyta ponownie po 4 s. Wcześniej losował z całej zmiany, więc potrafił zawołać o szklankę, której jeszcze nie było, albo o jedyny talerz przygnieciony patelnią.
   - **Feedback (Etap 8):** zdrapanie resztek sypie okruchami w kolorze brudu, płukanie puszcza strumień z kranu z rozbryzgiem, szorowanie pieni się (druciak dodatkowo sypie iskrami) — z dźwiękiem wody i szorowania, którego głośność idzie za ruchem palca. Domyte naczynie piszczy, błyska złotymi gwiazdkami i pierścieniem, przy przeciąganiu wolne pole stojaka pod palcem świeci na zielono, odłożenie brzęka. Stłuczenie = odłamki, huk i czerwony błysk ekranu; pracująca zmywarka szumi i paruje, a na koniec cyklu dzwoni. Wszystko to słuchacze statycznych zdarzeń `KitchenFeedbackEvents` (`Kitchen/Feedback/`) — rozgrywka nie wie o efektach.
   - **Pauza:** okrągły przycisk z dwiema kreskami w górnym pasku, systemowy gest „wstecz" Androida albo przerwanie (połączenie, przełączenie aplikacji) zatrzymują zegar, fizykę i dźwięk. Karta „Przerwa": **Wracam do zlewu** albo **Kończę zmianę** (wypłata za to, co już zrobione). „Wstecz" przy otwartej karcie naprawy zamyka najpierw ją; w hubie „wstecz" zamyka ustawienia albo słownik slangu.
   - Timer w lewym górnym rogu odlicza 3 minuty. Po jego zejściu do zera pojawia się ekran wyniku (umyte / stłuczone / niedokończone / pełne kosze i **wypłata rozpisana**: stawka za godziny + napiwki − potrącenia) z przyciskiem **„Wróć do domu"**.
   - **Wypłata** (`ShiftResultCalculator`): 3 minuty zmiany to 6-godzinny wieczór za irlandzką płacę minimalną z dnia w grze (7,00 EUR/h w 2004, czyli **42 EUR za pełną zmianę**; 7,65 EUR/h od maja 2005) — wcześniejsze „Kończę zmianę” płaci proporcjonalnie do przepracowanego czasu. Do tego **napiwki** z sali: 0,25 EUR za każde umyte naczynie i 1 EUR za pełny kosz; **potrącenie 3 EUR** za każde stłuczone (najwyżej do wysokości dniówki — zmiana może nic nie dać, ale nie kosztuje).
6. Z powrotem w MetaHub: portfel powinien wzrosnąć o wypłatę, Energia lekko spaść (zmiana −20, noc snu oddaje 10 + ¼ tego, czego brakuje do 100 — im bardziej zmęczony, tym więcej odpoczywa), Tęsknota lekko wzrosnąć (+2 dziennie), dzień się zwiększyć. **Co 7 dni** potrącają się rachunki tygodnia — w 2004 **165 EUR**: pokój 80, jedzenie 50, bilet 20, karta telefoniczna 15 (pokój drożeje razem z boomem, patrz Rozdział 4) — i pokazuje się podsumowanie.
   - **Zmęczenie czuć w kuchni:** poniżej 60 Energii Kuba szoruje i płucze wolniej, a ręka na ekranie wolniej nadąża za palcem — przy 0 z 70% siły (`Fatigue`: liniowo od 60 w dół). Hub pisze wtedy „Energia (zmęczony)", a w kuchni podpowiedź mówi, że Kuba jest zmęczony. Pub i ciężkie sceny kosztują więc coś w następnej zmianie; telefon do mamy i sen oddają siły.
   - **Zmęczenie widać na rękach** (od 2.10.2026; shader `Zmywak/Hand` w `Assets/_Project/Shaders/`, `HandWear`, `HandWearModel`). Rękawiczka, gołe przedramię i rękaw kitla są rysowane własnym shaderem URP. Oświetla je tak samo jak URP/Lit (cienie, sondy światła, SSAO, mgła), ale na wierzch rysuje to, co zmiana robi z ręką. Wszystko jest proceduralne (szum w metrach w przestrzeni samej części, wypukłości z pochodnych ekranowych), bez dodatkowych tekstur.
     - **Zmęczenie** to głównie energia, z którą Kuba przyszedł, plus to, ile zmiany już minęło. Daje pot (połysk i kropelki na skórze, ciemniejsze, mokre plamy na nitrylu od środka) i żyły na przedramieniu. Kompletnie wykończona ręka lekko drży (8–11 Hz); wypoczęty Kuba nie drży nigdy.
     - **Gorąca woda i szorowanie:** skóra czerwienieje plamami, najmocniej przy nadgarstku. Mocne szorowanie rozgrzewa ją w kilka sekund, a stygnie wolniej.
     - **Czas przy zlewie:** od mankietu w górę ręki pełznie mokry pas z pomarszczoną skórą, koniuszki rękawiczki ścierają się do matowego i jaśniejszego, a podwinięty rękaw łapie szare plamy od wody.
     - **Lata pracy na mokro:** od pierwszej zmiany w maju 2004 przez cztery lata przy nadgarstku pojawiają się suche, szorstkie placki.
     - Tylko wygląd: gra nic z tego nie odczytuje. Podgląd: jawny test `ArtPreview_HandsFreshAndWorn` (`art_hands_a_fresh.png`, `art_hands_b_hard_shift_2008.png`, `art_hands_c_everything.png`).
   - **Drabinka awansów i Król Zmywaka** (od 5.10.2026; `Career`, `CareerRecord`, `LifeLedger.Rank`). Każda zmiana trafia do rekordu kariery: umyte naczynia, pełne kosze, zmiany do końca bez stłuczki, zaliczone inspekcje, domyte Legendarne Tace i najdłuższa seria Flow. Rangi: **Nowy na zmywaku → Pomywacz → Starszy pomywacz → Szef zmywaka → Król Zmywaka**.
     - Pomywacz: 40 naczyń, 2 pełne kosze i 1 czysta zmiana. Starszy pomywacz: 150 naczyń, 10 koszy, 5 czystych zmian i seria Flow 3. Szef zmywaka: 400 naczyń, 30 koszy, 15 czystych zmian, inspekcja, taca i Flow 5. **Król Zmywaka:** 800 naczyń, 60 koszy, 30 czystych zmian, dwie inspekcje, dwie tace i Flow 8.
     - Każda ranga dodaje do stawki **0,35 € za godzinę** (w Krakowie ×4, w Pilbarze ×1,5). Awans ogłasza Declan (poza Dublinem szef), a Króla wita tekturowa korona nad zlewem. Rangi nie da się stracić.
     - Hub pokazuje rangę i procent do następnej pod portfelem. Wieczór otwiera lista tego, czego jeszcze brakuje („Do awansu: naczynia 120/150 · kosze 8/10 · Flow 2/3”). Symulator ekonomii liczy awanse tak samo.
   - **Pogoda za oknem** (od 5.10.2026; `Weather`, `WindowWeather`). Zależy od daty i miejsca, ale każdego dnia jest ta sama.
     - Dublin: deszcz w 40% wieczorów, mżawka w kolejnych 25%, jesienią czasem mgła, rzadko pogodnie. Do tego prawdziwy atak zimy z 2010 roku: śnieg od 26 listopada do 28 grudnia.
     - Kraków zimą: śnieg i mgła. Pilbara: upał, a od stycznia do marca burze.
     - Okno pokazuje strugi deszczu (mżawka słabiej, burza mocniej), padający śnieg albo mgłę, w której gasną światła miasta. Po przeprowadzce za szybą jest nocny Kraków (Wisła, Wawel, wieże Mariackiego) albo Pilbara (gwiazdy, czerwona ziemia, wieża szybowa i światła obozu). Hub dopisuje pogodę do daty.
   - **Mapa miasta** (przycisk „Mapa” w hubie, `CityMapPanel`, `CityMap`): malowana mapa miejsca, w którym Kuba mieszka, z pinezkami miejsc jego życia.
     - Dublin: Liffey, zatoka, kanały, Phoenix Park i St Stephen's Green. Pinezki: pokój w Rathmines, restauracja Declana, pub na Camden Street, kafejka i telefony na Parnell Street, przekazy na O'Connell Street, DIT i Busáras.
     - Kraków: Wisła, Planty wokół Starego Miasta, Rynek i Wawel. Pinezki: pokój na Kazimierzu, hotel przy Rynku, knajpa, kawiarenka internetowa i Poczta Główna.
     - Pilbara: czerwona ziemia, rzędy kontenerów obozu, droga i lądowisko, odkrywka. Pinezki: donga, kuchnia obozu, wet mess, internet, biuro.
     - Stuknięcie w pinezkę robi to samo co przycisk w hubie (praca, pub, telefon do domu, przelew). Zabytki pokazują tylko nazwę.
   - **Kuchnia po przeprowadzce** (`KitchenPlaceDressing`): kafle za zlewem mają kolor miejsca, kremowy w hotelu w Krakowie i stalowoszary w kuchni obozu w Pilbarze.
   - **Ostatni wsad przy zamykaniu:** na koniec zmiany zmywarka (jeśli nie jest zepsuta) myje to, co stoi w stojaku i co jeszcze jest w środku. Te naczynia liczą się jako umyte, a nie „niedokończone”.
   - **Start z dowolnej sceny** (`StartFromAnyScene`): Play w edytorze na MainMenu, MetaHub albo Kitchen_Shift przechodzi najpierw przez `Boot` (tworzy trwałe usługi) i wraca do tej sceny.
   - **Długi mają skutki:** tydzień zakończony na minusie to zaległość — **10 EUR kary**, gorsza noc (−10 Energii) i zmartwienie (+8 Tęsknoty); portfel w hubie pokazuje wtedy na czerwono „Dług: X EUR". Po 2. tygodniu zaległości właściciel ostrzega, po **3. tygodniu z rzędu** Kuba ląduje w **hostelu**: rachunki tygodnia +25 EUR i głośny dormitorium (sen oddaje 4 Energii mniej). Pokój za hubem zamienia się wtedy w dormitorium (`RoomDressing`): metalowe łóżko piętrowe z cudzym plecakiem na górze, drabinka, szafka z kłódką i regulamin „HOUSE RULES” na ścianie. Pierwszy tydzień zakończony bez długu kończy zaległości i wraca do pokoju. (Przy normalnej grze to się nie zdarza — reguły wieczorów pilnują rachunków; to dla gracza, który codziennie kończy zmianę po minucie.)

   Wszystkie te liczby są w jednym miejscu: `Assets/_Project/Scripts/Meta/Economy/LifeRules.cs` (np. `DaysPerWeek` obniżone do 2 przyspiesza ręczny test rozliczenia; symulator ekonomii — sekcja 5 — sprawdzi, czy zmiana nie psuje balansu).
7. Zamknij Play i wejdź ponownie — stan (portfel/staty/dzień/**poznane słowa, slang, flagi fabularne i obejrzane sceny**) powinien się wczytać z zapisu (`%USERPROFILE%\AppData\LocalLow\Zielona Wyspa Games\ZMYWAK\zmywak_save.json` na Windows). Obejrzana scena nie odpali się drugi raz.
8. **Ustawienia** (przycisk w hubie): suwak **rozmiaru tekstu** (dialogi, odczyty w hubie, liczniki i komunikaty w kuchni, karty wyniku/naprawy/pauzy i napisy na przyciskach; każdy tekst rośnie tak daleko, jak mieści jego pole — przy 1,5× nic nie wychodzi na sąsiadów, pilnuje tego `LayoutPlayModeTests`), **tryb dla daltonistów** (kolory 3 typów brudu zmieniają się na paletę niebieski/pomarańczowy/czarny zamiast brąz/czerwień/czerń — sprawdź w kuchni po włączeniu), **wibracje** (Android: lekki puls przy zdrapaniu resztek i odłożeniu na stojak, średni gdy naczynie jest czyste, mocny przy stłuczeniu — w Edytorze nic nie poczujesz, to normalne), **dźwięk** (wycisza wszystkie dźwięki kuchni), **przechylanie telefonu** (sterowanie odbiciem przy sprawdzaniu szklanek; wyłączone = odbicie przesuwa się samo, np. przy graniu na leżąco), **tryb relaks** (wydłuża zmianę o 50% i całkowicie wyłącza zawalanie Góry — widać po starcie kolejnej zmiany).
9. **Color grading nastroju**: gdy Tęsknota jest wysoka a Integracja niska, obraz powinien lekko zbłękitnieć/odbarwić się; im więcej Integracji względem Tęsknoty, tym cieplejszy odcień. Efekt jest subtelny i globalny (widoczny we wszystkich scenach) — najłatwiej zauważalny po kilku „Zadzwoń do mamy"/"Pub" z rzędu vs. kilku zmianach bez żadnej z tych akcji.
10. W edytorze mysz zastępuje dotyk (Input System ujednolica `Pointer`), więc testowanie myszką jest w pełni miarodajne dla logiki gestów.

## 5. Testy automatyczne

Z edytora: **Window → General → Test Runner → EditMode / PlayMode → Run All.** Z linii komend: sekcja 9.

**PlayMode** (`Assets/Tests/PlayMode`) ładuje prawdziwe sceny i steruje nimi symulowanym wskaźnikiem (Input System `InputTestFixture`): pełna pętla scrape → rinse → szorowanie całej powierzchni → przeciągnięcie na stojak → zmywarka → umyte naczynie; zlew mieści jedno naczynie; przeciążona Góra (słupek z talerzy) fizycznie się osuwa i tłucze naczynia, a stłuczenia obniżają wypłatę; talerz podrzucony i spadający na blat pęka, a tak samo podrzucona patelnia tylko dzwoni (i jest cięższa); spełnione zamówienie szefa podbija Flow; każdy krok mycia wysyła zdarzenia feedbacku, a w scenie żyją wtedy cząsteczki i grają dźwięki; zmiana zaczyna się od kilku naczyń, a kolejka się powtarza; każdą z trzech usterek zmywarki da się naprawić gestami na karcie naprawy, a awaria w trakcie cyklu go wstrzymuje, blokuje dotyk kuchni pod kartą i po naprawie cykl się kończy; naczynie zajmuje na stojaku tyle komórek, ile ma, a start zmywarki zabiera wkład do maszyny i zwalnia stojak; szklanka z zaciekami nie jest czysta, dopóki nie przejdzie sprawdzenia pod światło, a przechylenie (symulowany akcelerometr) przesuwa odbicie, bez czujnika zaś robi to przeciągnięcie palcem obok szklanki; **układ ekranu** (`LayoutPlayModeTests`) — kuchnia, hub i Ustawienia w proporcjach 16:9, 18:9, 19.5:9, 20:9, 21:9 i 4:3 (tablet): żaden widoczny przycisk nie wychodzi poza ekran ani nie nachodzi na inny (w kuchni także na podpowiedź, licznik kosza i miernik Góry), każdy przycisk ma co najmniej **48 dp** w obu wymiarach na trzech typowych telefonach (1080×1920 @ 420 dpi, 1080×2400 @ 400 dpi, 720×1600 @ 270 dpi — skala liczona z ustawień `CanvasScaler` scen), a narzędzia i „Uruchom zmywarkę” leżą w dolnych 45% ekranu, w zasięgu kciuka; postęp Meta+Narrative (z flagami fabularnymi) przeżywa ponowne wczytanie; nieruszana Góra zasypia (fizyka nie pracuje na darmo); zakończona zmiana trafia do lokalnego dziennika rozgrywki z czasami każdego kroku; **autoplay** przez minutę w kuchni myje i odkłada naczynia prawdziwą ścieżką wejścia (`SoakPlayModeTests` — tam też 15-minutowy soak, uruchamiany osobno); Rozdział 1 gra w hubie scenę dziennie aż do końca rozdziału, a podrobiony rachunek zmienia scenę w urzędzie; dzień później zaczyna się Rozdział 2, a po nim Rozdział 3. List od Declana z Rozdziału 1 otwiera dodatkową opcję przy Tomku. Inne życie (podrobiony rachunek, pożyczka dla Tomka, osłonięty Tunde) zmienia Rozdział 3: Tomek oddaje 30 €, Tunde staje obok Kuby, a pani Byrne podpisuje za darmo. Rozdział 4 przestawia kalendarz na 1 maja 2006 (czynsz z 2006, dalej dzień po dniu), napiwek dewelopera trafia do portfela, a bez listu od Declana nie da się poręczyć za Bartka (`StoryPlayModeTests`); sceny Boot/MainMenu/MetaHub się ładują. Przy okazji zapisują zrzuty ekranu 1080×1920 (razem z UI) do `Logs/Screenshots/` — do szybkiej kontroli wizualnej.

**Symulator ekonomii** (`EconomySimulationTests`, EditMode) przeżywa **8 tygodni** na prawdziwych regułach (`LifeRules` + `ShiftResultCalculator`) za trzech graczy — oszczędny (dzwoni do mamy, nie chodzi do pubu, odkłada), rozrzutny (pub każdego wieczoru, na który go stać) i „wszystko do domu" (wysyła co wieczór każde euro ponad narosłe rachunki) — każdy jako nowicjusz (4 umyte, 2 stłuczone na zmianę), średniak (8/1, pełny kosz) i wprawny (13/0, dwa pełne kosze), z gorszym dniem co piąty dzień, ze zmianami specjalnymi z kalendarza (wesela, inspekcje, taca) i z **najdroższym wyborem, na jaki Kubę stać, w każdej scenie Rozdziałów 1-5 i epilogu** (wyliczanym z grafów dialogów, więc nowa droga scena też trafi do symulacji), z przeskokami tam, gdzie zaczynają się Rozdziały 4 (2006), 5 (2008) i epilog (2010), więc płaca i czynsz idą za rokiem. Sprawdza, że nikt nie kończy tygodnia na minusie, Energia nie spada poniżej 20, oszczędny wprawny nie uzbiera 10 tygodni rachunków (pieniądze wciąż coś znaczą), pub widać w portfelu i w Integracji (tę drugą mierzy przebieg bez fabuły, bo każdy rozdział też zbliża Kubę do ekipy, a Integracja kończy się na 100), przelewy do domu realnie wychodzą z portfela, a dobra robota w kuchni się opłaca. Wynik (w wyjściu testu; stan na tę wersję - 8 tygodni kończy się w Boże Narodzenie 2010, przy rachunkach 171 € tygodniowo; po całej historii Integracja każdego dochodzi do 100, dlatego wpływ pubu mierzy przebieg bez fabuły):

| gracz | strategia | portfel po 8 tyg. | wysłane do domu | najniżej po rachunkach | najniższa Energia | Integracja |
|---|---|---|---|---|---|---|
| nowicjusz | oszczędny | 722 € | 0 | 18 € | 72 | 100 |
| nowicjusz | rozrzutny | 372 € | 0 | 18 € | 55 | 100 |
| nowicjusz | wszystko do domu | 22 € | 1052 € | 7 € | 72 | 100 |
| średniak | oszczędny | 981 € | 0 | 43 € | 72 | 100 |
| średniak | rozrzutny | 571 € | 0 | 43 € | 53 | 100 |
| średniak | wszystko do domu | 27 € | 1339 € | 15 € | 72 | 100 |
| wprawny | oszczędny | 1335 € | 0 | 79 € | 72 | 100 |
| wprawny | rozrzutny | 795 € | 0 | 29 € | 54 | 100 |
| wprawny | wszystko do domu | 33 € | 1638 € | 24 € | 72 | 100 |

**Po epilogu** (`AfterTheEpilogue_EachPlace_PaysItsOwnWay_AndStaysSolvent`) symulator żyje jeszcze 8 tygodni od 29.11.2010 w każdym z trzech miejsc, bez fabuły, za tych samych dziewięciu graczy, z 300 € wyniesionymi z Dublina.
- Nikt nie kończy tygodnia na minusie ani nie traci sił, a pub zostawia ślad w portfelu.
- Tygodniowa wypłata średniaka pokrywa rachunki od 1,5 do 7 razy.
- Złotówki wysłane do domu mówią, czemu w 2004 wszyscy wyjeżdżali: z Krakowa wraca ich mniej niż z Dublina, a najwięcej z kopalni.

| średniak, 8 tygodni po epilogu | oszczędny | rozrzutny | „wszystko do domu”: dotarło |
|---|---|---|---|
| Dublin | 1937 € | 1377 € | 7598 zł |
| Kraków | 4946 zł | 2706 zł | 4846 zł |
| Pilbara | 9159 AUD | 7479 AUD | 25 629 zł |

**Automatyczna jakość** (`QualityGovernorTests`, EditMode): pojedyncza przycinka ani seria drobnych szarpnięć nie obniżają jakości; długie spowolnienie schodzi o jeden poziom naraz po dwóch wolnych oknach; po ochłodzeniu wraca wyżej; na Low (limit 30 FPS) nie zostaje uwięziona — to był błąd starej wersji, liczącej średnią: soak w Edytorze pokazał spadek do Low po jednej klatce 1 s na stałe; nieudana próba powrotu wydłuża przerwę przed następną dwukrotnie, najwyżej do 8 minut.

**EditMode** sprawdzają matematykę czyszczenia (`DirtMaskMath`), maski brudu i szorowanie pędzlem (`DirtCanvasTests`, `DishInstanceTests`), zapis/odczyt (`SaveSystemTests`), przeliczenie EUR→PLN (`ExchangeRateServiceTests`), wyliczanie wyniku zmiany i wypłaty (`ShiftResultCalculator`), timer (`ShiftTimer`), staty gracza (`PlayerStatsControllerTests`), rozliczenie tygodnia (`WeeklyBudgetControllerTests`), tabelę kursu (`ExchangeRateTableTests`), rozmywanie nieznanych słów (`WordBlurFormatterTests`: słowo zostaje rozmyte na swoim miejscu, zarys zostaje, liter nie da się odczytać, interpunkcja zostaje ostra), śledzenie słownictwa (`VocabularyTrackerTests`), kolekcję slangu (`SlangCollectionControllerTests`), paletę dla daltonistów (`ColorBlindPaletteTests`), rodzaje zmian i harmonogram (`ShiftScheduleTests`: dni 1–3 spokojne, ciężkie wieczory dopiero od rozdziału czwartego, tempo zaciska się z fabułą, presja rośnie z rozdziału na rozdział, normy w zasięgu nowicjusza i do pobicia przez wprawnego, pierwsza awaria samouczkowa, potem rzadsze), wynik i ocena (`ShiftScoringTests`), tasowanie naczyń (`ShuffleBagTests`: skład każdej rundy, ten sam dzień tak samo, nigdy trzy z rzędu), fale serwisu (`ServiceWavesTests`), ustawienia projektu (`ProjectConfigurationTests`: tylko pion, sceny w Build Settings), generator dźwięków (`ProceduralSoundsTests`: słyszalne, bez przesterowania, pętle bez kliknięcia na styku, jednorazowe dźwięki wygasają zamiast się urywać), nagrania kuchni (`RecordedSoundsTests`: każdy dźwięk ma nagranie, pliki to mono 44,1 kHz, pętle bez kliknięcia, jednorazowe wygasają, ta sama wersja nie gra dwa razy z rzędu, każdy plik jest w `CREDITS.md`), radio (`RadioTests`: każda stacja słyszalna, bez przesteru, zapętla się bez trzasku i brzmi co do próbki tak samo, mały głośnik nie przenosi basu, gałka kręci się w kółko, wiadomości idą za rokiem), ślady zmiany na rękach (`HandWearTests`: wypoczęty Kuba zaczyna ze świeżymi rękami, zmęczenie to głównie stan na wejściu, drży tylko wykończony, woda wsiąka przez zmianę, szorowanie rozgrzewa skórę, lata zostawiają ślad, shader się kompiluje i rysuje całe ramię, rękawiczka zna swoją pozę spoczynkową i koniuszki palców), zmiany specjalne (`SpecialShiftTests`: pierwszy tydzień spokojny, soboty to wesela, niedziele taca, inspekcja co dziewiąty dzień tylko w dni robocze, Dzień św. Patryka tylko w Dublinie, tygodnie fabuły trafiają na wesele, inspekcję i tacę, a premie, napiwki i potrącenia idą za zmianą), deskę (`PrepRoundTests`: krojenie przy kresce daje same równe kostki i pełną premię, spóźnione grube, wczesne cienkie, dużo za wcześnie plaster i przerwa, podwójne stuknięcie nic nie robi, bez krojenia kawały, zmęczona ręka myli rytm), fizykę materiałów (`DishPhysicsTests`: stal się nie tłucze, szkło szybciej niż porcelana, upadek ze szczytu Góry tłucze, ześlizgnięcie się w dół stosu nie, liczy się uderzenie wzdłuż normalnej, stal ślizga się łatwiej), naprawy zmywarki i harmonogram awarii (`DishwasherRepairTests`) , układanie stojaka (`RackLayoutTests`: zajęte komórki, obracanie patelni w wąską szczelinę, ciasne vs luźne upakowanie) , logikę dialogów i harmonogram scen (`DialogueSessionTests`), poprawność treści każdego rozdziału (`StoryContentTests`: każde przejście prowadzi do istniejącego węzła, żadna scena nie zapętla się ani nie urywa, każdy mówiący ma nazwę, slang jest w słowniku, wymagane flagi ustawia wcześniejsza scena, także z poprzedniego rozdziału, rozdział zaczyna się dzień po końcu poprzedniego, a identyfikatory scen się nie powtarzają) oraz sprawdzanie szklanek (`GlassInspectionTests`: zacieki to cienkie smugi, nie schodzą przed tłuszczem, widać je tylko w odbiciu, przechylenie przesuwa odbicie w stronę przechyłu, samoczynne odbicie nie ucieka ze szklanki, pchnięcie palcem przesuwa odbicie i zatrzymuje je na krawędzi) — bez potrzeby uruchamiania sceny. Zamówienia szefa/Flow/zapis/dialogi/jakość-grafiki/color-grading jako całość (z UI i renderowaniem) wciąż warto sprawdzić ręcznie w Play Mode.

## 6. Znane ograniczenia (celowe uproszczenia)

**Wypiekane oświetlenie kuchni i pokoju Kuby** (`BakedLighting`, od 1.10.2026): Run All liczy na GPU komputera (Progressive GPU Lightmapper) odbite światło i cienie kontaktowe (AO) do lightmap, siatkę light probe'ów dla ruchomych rzeczy (naczynia, ręka) i box-projected reflection probe. Telefon tylko czyta gotowe tekstury.
- Pokój Kuby (tło menu i ekranu wieczoru, `MainMenu` i `MetaHub`) dostaje światło lampki odbite od ścian, sufitu i podłogi. Ściana z łóżkiem i dywan nie toną już w czerni. Świecące powierzchnie (nocne miasto za oknem, klosz lampki) są wypiekane jako źródła światła (`BakedEmissive`).
- Reflektory przejeżdżającego samochodu (`LightSweep`) zostają w pełni real-time, bo zmieniają się w trakcie gry.
- Światła są Mixed (Baked Indirect): bezpośrednie światło i cienie zostają real-time, więc naczynia i ręka ruszają się jak wcześniej.
- Wypiekanie bierze kolory otoczenia z `SceneLighting`, a sonda fotografuje kuchnię na tle jego „studia” (panel, świetlówki, okno). Kuchnia nie ma sufitu ani przedniej ściany, więc z domyślnym niebem Unity blat wychodził granatowy, a maszyny białe.
- Same naczynia i butelka płynu odbijają nadal studio, nie sondę: patelnia trzymana w zlewie dostawała w odbiciu jasny blat z bliska i robiła się biała jak plastikowa miska.
- Wygenerowane siatki dostają drugi zestaw UV pod lightmapę (`ArtMeshes.Save`). Rozkład robi zewnętrzne narzędzie Unity (`UnwrapCL.exe`), które raz zgubiło połączenie i wywróciło edytor. Dlatego odcisk kształtu siedzi w ustawieniach importu siatki (`userData`), a niezmieniona siatka nie jest rozkładana drugi raz.

**Dlaczego Etap 5 jest tylko częściowy:** duża część brief'u na ten etap (ilustrowane portrety, prawdziwe dźwięki kuchni/radia, tekstury brudu na `RenderTexture`) wymaga albo gotowych assetów graficznych/audio, których nie potrafię wyprodukować, albo interaktywnego bake'u/iteracji w edytorze Unity, których nie mogę stąd uruchomić ani zweryfikować. Zaimplementowałem to, co da się w pełni i bezpiecznie zrobić kodem — reszta czeka na assety i pracę bezpośrednio w edytorze.

- **Cała grafika jest proceduralna** (siatki toczone z profilu, tekstury i ikony generowane kodem do `Assets/_Project/Art/`) — spójna i lekka, ale to nadal placeholder w porównaniu z ręcznie robionymi modelami i teksturami. Maski brudu to tekstura 128×128 na CPU (bez `RenderTexture`), co wystarcza do jednego szorowanego naczynia naraz.
- **Dźwięki kuchni to nagrania CC0** (od 5.10.2026). Pochodzą z paczek Kenneya i rubberducka, lista źródeł jest w `CREDITS.md`.
  - **Co gra:** woda z kranu, szorowanie gąbką i drucianką, buczenie zmywarki, pisk czystego talerza, stuk porcelany i szkła, tłuczenie, brzęk garnków, nóż o deskę, plusk, dzwonek, bzyczek awarii i pstryknięcie bezpiecznika.
  - **Kilka wersji jednego dźwięku:** stuk talerza ma ich pięć, tłuczenie cztery. `KitchenSounds` nie gra tej samej wersji dwa razy z rzędu, więc pełen kosz nie terkocze jak karabin.
  - **Skąd biorą się pliki:** `RecordedSounds.Bake` robi z nagrań pliki WAV w `Assets/_Project/Audio/Kitchen`: mono, przycięte, z wygaszeniem i poziomem wyrównanym do syntetycznych odpowiedników. Pętle są zszyte przenikaniem.
  - **Bank:** Run All zbiera pliki w `KitchenSoundBank` (`Resources`).
  - **Zapas:** syntetyczne `ProceduralSounds` zostają na wypadek, gdyby zabrakło nagrania, i jako wzorzec głośności.
  - **Radio** nadal gra trzy syntetyzowane stacje (patrz „Radio na ścianie”).
  - Nagrania dobierałem po pomiarach i spektrogramach, nie na ucho. Ich brzmienie trzeba ocenić samemu.
- **Rękawiczka jest wymodelowana w Blenderze** (`Tools/Blender/glove.py`, od 1.10.2026), skryptem, na tym samym szkielecie co rig dłoni w `KitchenArt.BuildHand`. Dłoń to spłaszczona rurka zwężająca się ku nadgarstkowi, a palce i kciuk to gładkie rurki (modyfikatory Skin + Subdivision) z kostkami i zaokrąglonymi opuszkami. Ma ok. 3,3 tys. wierzchołków, mniej niż dawne 16 kapsuł.
  - Unity (`KitchenArt.BuildGlove`) czyta `Assets/_Project/Art/Source/GloveHand.json` i podpina siatkę jako `SkinnedMeshRenderer` pod istniejące stawy. Każdy odcinek palca idzie za swoim stawem, z płynnym przejściem ±6 mm przez każde zgięcie, więc wszystkie pozy `GlovedHand` działają bez zmian.
  - Bez tego pliku ręka wraca do dawnych brył (zaokrąglony prostopadłościan i kapsuły).
  - Odtworzenie rękawiczki (Blender 4 lub 5, bez otwierania okna):
    ```powershell
    & 'C:\Program Files\Blender Foundation\Blender 5.2\blender.exe' --background --factory-startup --python Tools\Blender\glove.py -- Assets\_Project\Art\Source\GloveHand.json --preview glove.png
    ```
  - Przedramię, mankiet i rękaw to dalej rury generowane w Unity. Całe ramię rysuje shader `Zmywak/Hand` (patrz „Zmęczenie widać na rękach”).
- **Resztki jedzenia też są z Blendera** (`Tools/Blender/leftovers.py`): każde naczynie przychodzi z tym, co z niego jedzono.
  - **Talerz:** penne (z rowkami, ścięte skośnie, od spodu w sosie) w plamie sosu pomidorowego i listek bazylii.
  - **Patelnia:** kostki smażonych ziemniaków z przypieczonymi krawędziami, plasterki kiełbasy i paski cebuli.
  - **Szklanka:** plasterek cytryny.
  - **Miska:** grudka owsianki z rodzynkami i jej rozmaz. **Kubek:** wyciśnięta torebka herbaty z nitką i zawieszką. **Garnek:** gulasz przywarty do dna - sos, wołowina, ziemniaki, marchewka.
  - Cienie we wgłębieniach i między kawałkami (AO) wypieka Cycles na karcie graficznej (OptiX na RTX). Trafiają do UV jako wiersz palety (od cienia do światła), a kolumna to rodzaj jedzenia.
  - Unity (`BlenderArt.Leftovers`) robi z `Art/Source/Leftovers*.json` siatki i paletę `LeftoversPalette_Food.png`, a Run All przypina je do definicji naczyń (`DishDefinition.leftoversMesh`). Bez tych plików wracają dawne kolorowe grudki.
  - Odtworzenie: `blender --background --factory-startup --python Tools\Blender\leftovers.py -- Assets\_Project\Art\Source --preview leftovers.png`.
- **Portrety postaci w dialogach są z Blendera** (`Tools/Blender/portraits.py`, odświeżenie 10.10.2026). Wszystkie 19 popiersi mają spójny, stylizowany wygląd 3D: ciągłe siatki twarzy, modelowane powieki, tęczówki z odbiciami, delikatny kolor policzków, pasma włosów oraz ubrania z kołnierzami, szwami i dodatkami. Edytowalne sceny są w `Tools/Blender/Characters`, porównanie i plansza obsady w `Tools/Blender/Previews`, a instrukcja odtworzenia w `Tools/Blender/README.md`. Każda postać zachowuje swój znak rozpoznawczy:
  - Kuba: bluza z kapturem, pasek plecaka, zmęczone powieki.
  - Declan: czapka i bluza kucharska, siwa broda.
  - Marek: dres z paskami.
  - Agnieszka: okulary i sweter.
  - Darius: czapka i broda.
  - Gerry: garnitur, komórka przy uchu.
  - Pani Byrne: siwa trwała, perły, okulary na łańcuszku.
  - Urzędniczka: smycz z identyfikatorem.
  - Pan z banku: okulary i krawat.
  - Mama: słuchawka telefonu ze sznurem.
  - Siobhán: rude loki i piegi.
  - Tomek: kask i kamizelka odblaskowa.
  - Gavin: czarny zaczes w górę, kolczyk, skrzywiona mina, bluza kucharska.
  - Tunde: krótkie, matowe kręcone włosy, biały T-shirt i fartuch pomocnika kuchni.
  - Bartek: szeroko otwarte, przestraszone oczy, uniesione brwi, bordowa bluza.
  - Tata: siwe włosy, sumiaste wąsy, sweter i słuchawka telefonu.
  - W epilogu: Thiago (czarne fale, zielona koszulka, uśmiech), Ołeh (jasny jeżyk, dres) i Liam z Cork (rude włosy, piegi, koszulka w czerwieni Cork).
  - Dalsza korekta obsady: zróżnicowane proporcje twarzy, matowe fryzury, tęczówki zasłaniane przez powieki oraz dopasowane do tułowia paski, fartuchy i kamizelki. Kardigany mają wykończenie z dzianiny, a kask integralne żebra skorupy. `Previews/BeforeAfter.png` pokazuje porównanie sześciu postaci z poprzednią wersją.
  - Miękkie światło studyjne: ciepły klucz, chłodne wypełnienie i kontur. Render: Cycles, 96 próbek, 512×512 RGBA z przezroczystym tłem, **Khronos PBR Neutral**. Generator wybiera OptiX/CUDA, jeśli dostępne, a w przeciwnym przypadku CPU.
  - W grze portret mówiącego stoi z prawej nad oknem dialogu, zwrócony w stronę tekstu, a jego dół chowa się za oknem. Przy zmianie mówiącego wsuwa się z przenikaniem, przy narratorze znika, a przy wyborze pokazuje Kubę, bo to jego decyzja (`DialogueScreen`, `SpeakerNames.PortraitId`). Run All importuje PNG-i jako sprite'y (`PortraitArt`).
  - Pliki leżą w repozytorium (`Assets/_Project/Art/Portraits/Portrait_<id>.png`), więc do zbudowania gry Blender nie jest potrzebny. `StoryContentTests` pilnuje, żeby każdy mówiący w treści miał portret.
  - Odtworzenie: `blender --background --factory-startup --python Tools\Blender\portraits.py -- Logs\PortraitRefine\final --blend-dir Tools\Blender\Characters` (opcje: `--only kuba,declan`, `--samples 128`). Po kontroli skopiuj PNG do `Assets/_Project/Art/Portraits`, pozostawiając istniejące `.meta`; nie trzeba przebudowywać scen przez Run All.
- **Haptyka:** na Androidzie trzy siły pulsu (12/28/70 ms przez systemowy `Vibrator`, awaryjnie `Handheld.Vibrate`). Na iOS lekki, średni i mocny impuls Taptic Engine przez małą natywną wtyczkę (`Assets/Plugins/iOS/ZmywakHaptics.mm`, `UIImpactFeedbackGenerator`, iPhone 7 i nowsze). Wtyczkę kompiluje dopiero Xcode na Macu, więc na telefonie nie była sprawdzona.
- **Profile jakości** (`QualityManager`) modyfikują w runtime właściwości jednego assetu URP (`Zmywak_URP.asset`, tworzonego przez „Run All": render scale, cienie, MSAA), zamiast podmieniać między osobnymi plikami `.asset` URP — jedna para pipeline + renderer do utrzymania.
- Progi Góry (wysokość zawalenia, ile się tłucze) to pierwsze przybliżenie — do dostrojenia po realnym playteście.
- **Maska brudu jest liczona na procesorze** (128×128 na naczynie razem z jej mapą normalnych, szorowanie tylko pod palcem), nie na RenderTexture z AsyncGPUReadback, jak zakładał plan — świadoma decyzja (27.09.2026): soak w Edytorze trzyma 60 FPS, więc przepisanie na GPU ma sens dopiero, jeśli pomiar na telefonie (sekcja 8.4) pokaże, że szorowanie zjada klatki.
- Po epilogu (dzień 35) historia się kończy i gra toczy się dalej jako same zmiany i wieczory tam, gdzie Kuba zamieszkał (patrz „Życie po epilogu”). Płace i czynsze idą dalej za historią. W Dublinie płaca minimalna spada do 7,65 € od lutego do lipca 2011, a od 2016 rośnie (9,15 → 10,10 €); czynsze spadają do 2012 i rosną od 2013. W Krakowie stawka rośnie o złotówkę rocznie do 21 zł. W Pilbarze szczyt boomu przypada na 2012 (36 AUD), potem stawka spada. Kuchnia ma kolor miejsca (patrz „Kuchnia po przeprowadzce”), ale wciąż ten sam układ co u Declana.
- **MetaHub i menu mają za tłem pokój Kuby w 3D** (`RoomArt`: łóżko, biurko z lampką, słoik bigosu, okno z pogodą dnia i nocnym miastem) oraz mapę miasta z miejscami jego życia (patrz „Mapa miasta”).
- **Rozdziały 1-5 to po sześć scen w hubie, a epilog to pięć** (generowanych w `Assets/_Project/Editor/Chapter1Content.cs` … `Chapter6Content.cs`). Kolejne historie (np. Wigilia u rodziców albo kuchnia z nazwiskiem Kuby) to kolejna zawartość na tym samym silniku (`StoryChapter` + `DialogueGraph`), dopisywana jako kolejny element listy rozdziałów w `StoryBeatTrigger`. Rozdziały czytają flagi poprzednich (Rozdział 3 m.in. `ch1_fake_bill`, `ch2_rent_on_time`, `ch2_covered_tunde`, `ch2_lent_tomek`), a Rozdział 4 czyta flagi Rozdziału 3 (`ch3_at_board`, `ch3_helped_gavin`, `ch3_tunde_friend`, `ch3_diploma_up`). Rozdział 5 czyta flagi Rozdziału 4 (`ch4_booked_flat`, `ch4_cookery_school`, `ch4_with_gavin`, `ch4_mentor_bartek`, `ch4_promised_visit`). Epilog idzie drogą wybraną na końcu Rozdziału 5 (`ch5_stays`, `ch5_goes_home`, `ch5_goes_to_perth`). Praca przy desce nie zmienia też jeszcze samej zmiany w kuchni.
- **Skalowanie tekstu rośnie w granicach pól** — teksty mają auto-rozmiar, a ustawienie podnosi jego sufit, więc tekst w ciasnym polu (np. licznik czasu) urośnie mniej niż 1,5×. Pola nie powiększają się same; jeśli na telefonie coś będzie wciąż za małe, trzeba poszerzyć konkretne pole w układzie. Tytuły ekranów i dekoracyjne napisy (menu główne, „RESET", „Flow ×2!") się nie skalują — są już duże.
- **Trzy sloty zapisu** (`SaveSystem.Slot`, `MainMenuScreen`): „Rozpocznij grę” w menu otwiera listę zapisów. Każdy pokazuje dzień, datę, miejsce, portfel i rozdział albo napis „pusty - nowa gra”. Wybrany slot uruchamia grę od nowa przez `Boot` (trwałe usługi czytają ten slot) i przenosi do hubu, a gra zapamiętuje go na następne uruchomienie. „Usuń” kasuje slot po drugim stuknięciu („Na pewno?”). Pierwszy slot to dawny plik `zmywak_save.json`, więc stare zapisy działają dalej. Każdy slot jest **odporny na zabicie aplikacji**: nowy stan trafia najpierw do pliku tymczasowego i dopiero potem podmienia zapis, a poprzedni dobry zapis zostaje jako kopia (`zmywak_save.json.bak`), z której gra się wczyta, gdy główny plik jest pusty albo uszkodzony. Zapis ma numer wersji i migracje (`SaveMigrations`), więc starsze zapisy się aktualizują. Wszystko zapisuje się przy każdej zmianie i dodatkowo przy zwinięciu aplikacji. **Przerwana zmiana przepada:** zabicie gry w trakcie zmiany nie liczy dnia ani wypłaty, ale nic sprzed zmiany nie ginie. Dwa trwałe kontrolery (`MetaProgressController`, `NarrativeProgressController`) współdzielą ten sam plik metodą odczyt-modyfikuj-zapis, więc żaden nie nadpisuje danych drugiego.

## 7. Struktura

Pełny opis w [`C:\Users\pawel\.claude\plans\piped-noodling-iverson.md`](C:\Users\pawel\.claude\plans\piped-noodling-iverson.md) (zaakceptowany plan architektury). Skrót: `Assets/_Project/Scripts/{Core,Kitchen,Meta,Narrative,UI}` — moduły jako osobne assembly definitions, `Assets/_Project/Editor` — generator sceny/contentu, `Assets/Tests/EditMode` — testy.

## 8. Wydanie: Android i iOS

**Testowy APK (development build) i projekt Xcode da się zbudować z linii komend — patrz sekcja 9.** Poniżej ręczna ścieżka przez edytor oraz to, co zostaje po Twojej stronie: podpis (keystore), tożsamość aplikacji i kompilacja iOS na Macu.

### 8.0 Testowy APK na telefon: jedna komenda

Na razie nie przygotowujemy wydania, tylko **stabilny APK do testów na telefonie** (bez keystore'a, AAB i sklepów).

**Potrzebne:** Unity w wersji projektu (`6000.6.3f1`) z modułem **Android Build Support** (Unity Hub → Installs → ta wersja → koło zębate → Add modules: Android Build Support razem z OpenJDK i Android SDK & NDK Tools; ok. 2,4 GB, a Windows raz pyta o uprawnienia administratora, więc trzeba kliknąć „Tak”) oraz projekt **niezałączony** w Edytorze w tym czasie.

```powershell
powershell -ExecutionPolicy Bypass -File Tools\build_android.ps1            # zbuduj
powershell -ExecutionPolicy Bypass -File Tools\build_android.ps1 -Install   # zbuduj, zainstaluj na telefonie i uruchom
```

**Dwie pułapki Windowsa, które skrypt obchodzi sam** (sprawdzone 7.10.2026 przy pierwszym buildzie na Unity 6.6):
- **Polskie znaki w folderze użytkownika** (`C:\Users\Paweł`). Java/Gradle wykłada się na nich w folderze TEMP („Unable to establish loopback connection”) i w cache'u Gradle („ClassNotFoundException” w kroku NDK prefab). Na czas buildu TEMP i katalog domowy Gradle są przenoszone do `Library\BuildTemp` i `Library\GradleHome` projektu.
- **Antywirus skanujący HTTPS** (Norton Web/Mail Shield). Podpisuje połączenia własnym certyfikatem, któremu ufa Windows, ale nie Java z Unity, więc Gradle nie pobierał bibliotek Androida („PKIX path building failed”). Hak `GradleWindowsTrustStore` (`AndroidBuildScript.cs`) każe Gradle sprawdzać certyfikaty magazynem Windows. Weryfikacja zostaje włączona, a buildy w chmurze nie są dotknięte.

Skrypt sam znajduje Unity (także w miejscu ustawionym w Unity Hub), sprawdza moduł Android jeszcze przed długim buildem, uruchamia `AndroidBuildScript.BuildAndroid` i melduje ścieżkę APK (`Builds\Android\Zmywak-dev.apk`, log w `Logs\build_android.log`). Gdy się nie uda, pokazuje pierwsze błędy z logu. Gdy projekt leży w ścieżce z polską literą (np. użytkownik `Paweł`) albo w OneDrive z plikami „tylko w chmurze”, zbuduj lokalną kopię: `-Project E:\Dev\Zmywak`.

**Na emulatorze, bez telefonu.** Unity 6.6 buduje na Androida tylko ARM (arm64-v8a, armeabi-v7a), a emulator na PC to x86_64, który kod ARM tłumaczy. Tłumacz 64-bitowego ARM w obrazach Androida (`ndk_translation`) nie zna instrukcji `SEVL`, na której Unity 6.6 kręci blokady wątków, więc build ARM64 pada na emulatorze od razu (SIGILL; sprawdzone na Androidzie 11 i 14). Emulator dostaje więc osobny build 32-bitowy:

```powershell
powershell -ExecutionPolicy Bypass -File Tools\build_android.ps1 -Project E:\Dev\Zmywak -Emulator   # -> Builds\Android\Zmywak-dev-emulator.apk (ARMv7)
```

Do tego potrzebny jest obraz, który tłumaczy 32-bitowy ARM. Sprawdzony jest **Android 11 (API 30), x86_64, Google APIs**; na desktopie to AVD `zmywak_arm` (Pixel 5). Emulator i adb nie znoszą polskiej litery w folderze użytkownika. Uruchamiaj je z krótkimi ścieżkami 8.3 w `ANDROID_SDK_ROOT`, `ANDROID_HOME`, `ANDROID_USER_HOME`, `ANDROID_AVD_HOME` i `ANDROID_EMULATOR_HOME`, bo inaczej emulator wisi na starcie, a adb widzi urządzenie jako „offline”. Używaj też jednego adb, tego z SDK (`%LOCALAPPDATA%\Android\Sdk\platform-tools`): adb z Unity jest w innej wersji i zabija serwer tamtego. `adb shell input tap` trwa krócej niż klatka przy 30 FPS, a gra czyta dotyk raz na klatkę, więc talerze i deskę stukaj przez `input swipe x y x y 150`. Telefony i testerzy dostają zwykły build ARM64, a projekt po buildzie emulatorowym wraca do ARM64.

**Co jest w tym buildzie:** development build (logi i profiler), IL2CPP, ARM64, minimalne Android API 26, **orientacja tylko pionowa**. Pion jest zapisany w prawdziwym `ProjectSettings/ProjectSettings.asset` (`defaultScreenOrientation: 0`), wymusza go Run All i sam build, a `ProjectConfigurationTests` pilnuje zapisanej wartości, razem z Build Settings (4 sceny, Boot pierwszy).

**Sprawdzone na emulatorze 7.10.2026** (Android 11, Pixel 5, build ARMv7 na Unity 6.6, język systemu angielski) to cała pętla dnia: menu, scena fabularna z rozmyciem nieznanych słów i wyborem, kuchnia (talerz do zlewu, zeskrobanie, płukanie, gąbka, kosz, zmywarka), karta wyniku z wypłatą, powrót do domu i dzień 2. Sprawdzone też: minimalizacja i powrót, „wstecz” zamykający ustawienia, zapis po zabiciu aplikacji, przerwany samouczek (wraca od początku) i „Nowa gra” przy istniejącym zapisie (idzie do wolnego slotu). Wyszły przy tym błędy, które są już poprawione:
- **Pasek nawigacji nad narzędziami i zgubiony pierwszy dotyk.** Na Androidzie 11 okno gubiło ustawienie „paski chowane, odsłaniane przesunięciem” (także przy ładowaniu każdej sceny). System wracał wtedy do „pokaż paski przy dotyku”: pierwsze stuknięcie po starcie zjadał na pokazanie paska, więc „Kontynuuj” trzeba było stukać dwa razy, a przyciski systemowe zostawały na stałe nad paskiem narzędzi w kuchni. `AndroidSystemBars` pyta okno o to ustawienie wprost po starcie, po każdej scenie i po powrocie do gry. Sprawdzone w `dumpsys window`: okno ma flagę `BEHAVIOR_CONTROLLED` jeszcze przed pierwszym dotykiem.
- **Kwoty po angielsku.** Telefon po angielsku pisał „20.00 EUR” i „awans 0 %”. `GameCulture` ustawia polski zapis liczb na każdym telefonie.
- **Przypadkowy wybór i podwójne liczenie.** Szybkie stukanie w „Dalej” mogło trafić w wybór, który właśnie się pojawił; nowe wybory ignorują dotyk przez 0,4 s. Do tego zmywarka kończąca cykl po zamknięciu zmiany liczyła swój wsad drugi raz w liczniku HUD-u.
- **Utracony wieczór.** „Dalej” leży nad „Idź do pracy”, więc szybkie stukanie przez ostatnie linijki sceny wysyłało Kubę od razu do pracy. Akcje huba wracają teraz przez 0,5 s i dopiero wtedy przyjmują dotyk (`StoryBeatTrigger`).
- **Plakietka przy wyborze.** Nad twarzą Kuby stało imię ostatniego rozmówcy („Declan”). Teraz plakietka mówi „Kuba (myśli)”, a linijka, która doprowadziła do wyboru, ma przed sobą imię mówiącego.
- **Stuknięcie krótsze niż klatka.** Gra czytała dotyk w kuchni raz na klatkę, więc stuknięcie, które zaczęło się i skończyło między dwiema klatkami, nie brało naczynia. `DishInputRouter` łapie je teraz osobno.
- **Kara za nic.** Puszczenie czystego talerza nad zlewem (stuknięcie albo palec, który przed chwilą go płukał) liczyło się jako chybione odłożenie na kosz, czyli −2 pkt oceny. Liczy się tylko talerz wyniesiony ze zlewu i upuszczony obok kosza.
- **Wyzerowany zapis nowej gry.** Świeża gra zapisywała portfel, dzień i statystyki dopiero przy pierwszej ich zmianie albo przy wyjściu do tła, a fabuła zapisywała swoje flagi od razu. Aplikacja zamknięta na twardo pierwszego wieczoru zostawiała więc zapis z flagami i zerami: menu pisało „Dzień 1 · ?”, a „Kontynuuj” dawało Kubie 0 EUR i 0 energii (odczytane z pliku na emulatorze przez `adb root`). Hub zapisuje teraz nowe życie od razu po wejściu (`MetaProgressController.EnsureOnDisk`), a zapis bez danych tygodnia wczytuje się jako nowe życie (`LifeLedger.LoadFrom`), więc taki istniejący zapis też się naprawia.
- **Karta zmiany.** Przy łagodnej fali ruchu w „Spokojnym początku” karta pisała „Szczyt - naczynia lecą jedno za drugim”. Teraz używa hasła fali („Kilka stolików naraz.”), jak HUD.

Licznik FPS w lewym górnym rogu i napis „Development Build” w prawym dolnym są tylko w buildzie deweloperskim.

**Co sprawdzić ręcznie na prawdziwym telefonie** (emulator nie zastąpi dotyku, czujnika przechyłu, wibracji ani wydajności prawdziwego GPU): obrót telefonu (gra ma zostać w pionie), minimalizacja w kuchni i powrót (stoi na karcie „Przerwa”, nie rusza zegara), systemowy gest „wstecz” (zamyka mapę, ustawienia, słownik i listę zapisów, w kuchni pauzuje; w menu i hubie bez otwartych kart nic nie robi), zapis po zabiciu aplikacji i wczytanie go po ponownym uruchomieniu. Pierwsze podejście jedną komendą: `Tools\phone_check.ps1` (zrzuty z telefonu i błędy z logu).

### 8.1 Jednorazowa konfiguracja PlayerSettings

**Zmywak → Setup → 9. Configure Player Settings (Mobile)** — ustawia orientację portretową (i blokuje pozostałe; to samo robią już Run All i build, patrz 8.0), scripting backend na IL2CPP (Mono nie obsługuje 64-bit na Androidzie, a Google Play wymaga 64-bit od 2019), architekturę ARM64, minimalny Android API 26 (Android 8.0 Oreo — niższy próg jest już przestarzały w Unity 6), `Build App Bundle` (AAB zamiast APK — wymagane przez Google Play), oraz minimalną wersję iOS 13.0.

**WAŻNE:** nazwę i identyfikator aplikacji (`ZMYWAK`, `com.zielonawyspagames.zmywak`, firma „Zielona Wyspa Games") ustawia już „Run All" (`EnsureProductIdentity` w `VerticalSliceSetup.cs`). Identyfikator to Twoja **permanentna, jednorazowa** tożsamość w sklepie — jeśli chcesz inny, zmień stałą `ApplicationIdentifier` **zanim** cokolwiek opublikujesz; po pierwszym wysłaniu do sklepu nie da się go już zmienić. Sam krok 9 (AAB, wersja) celowo NIE wchodzi w skład „Run All" — uruchamiasz go świadomie, kiedy jesteś gotów na wydanie.

### 8.2 Build Android (AAB)

1. W Unity Hub, przy instalacji/edycji wersji Unity, dodaj moduł **Android Build Support** (razem z Android SDK & NDK Tools i OpenJDK — Unity Hub ogarnia to samo, jeśli zaznaczysz przy instalacji).
2. File → Build Settings → wybierz **Android** → **Switch Platform** (poczeka na reimport).
3. File → Build Settings → **Player Settings** → sprawdź, że sekcja 8.1 się wykonała (Identification, Configuration → Scripting Backend = IL2CPP, Target Architectures = ARM64).
4. **Keystore (podpis aplikacji):** Player Settings → Publishing Settings → Keystore Manager → stwórz nowy keystore (hasło zapisz w bezpiecznym miejscu — bez niego nie zaktualizujesz opublikowanej aplikacji nigdy więcej). Bez keystore Unity użyje debug-keystore, który **nie nadaje się** do publikacji w Google Play.
5. Build Settings → **Build** (albo **Build And Run**, jeśli masz telefon podłączony przez USB z włączonym Developer Mode + USB Debugging) → wskaż folder wyjściowy. Powstanie plik `.aab`.
6. Pierwszy build IL2CPP bywa wolny (10-30+ minut zależnie od maszyny) — kolejne są szybsze (inkrementalne).
7. **Wymagania Google Play (sprawdzone na buildzie z 26.09.2026):** od 31.08.2026 nowe aplikacje i aktualizacje muszą celować w **API 36 (Android 16)**. Player Settings → Target API Level stoi na „Automatic (highest installed)", co przy zainstalowanym SDK platform 36 daje `targetSdkVersion 36` (minSdk 26). **Strony pamięci 16 KB:** wszystkie biblioteki `.so` mają segmenty LOAD wyrównane do 0x4000, a APK przechodzi `zipalign -c -P 16`. Sprawdzenie ręczne (PowerShell):

```powershell
$bt = "C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\build-tools\36.0.0"
& "$bt\zipalign.exe" -c -P 16 4 "Builds\Android\Zmywak-dev.apk"; "exit=$LASTEXITCODE (0 = OK)"
& "$bt\aapt2.exe" dump badging "Builds\Android\Zmywak-dev.apk" | Select-String "targetSdkVersion"
```

### 8.3 Build iOS — wymaga Maca

iOS **fundamentalnie nie da się zbudować na Windows** — to nie jest ograniczenie tego projektu ani moje, tylko wymóg Apple: Xcode (jedyne narzędzie, które potrafi skompilować i podpisać aplikację na iOS/App Store) działa wyłącznie na macOS. Unity na Windows potrafi jedynie **wygenerować projekt Xcode** (File → Build Settings → iOS → Switch Platform → Build), ale sama kompilacja i wysyłka do App Store Connect musi się odbyć na Macu. Opcje:
- Pożycz/kup dostęp do Maca (fizycznego albo w chmurze, np. MacStadium, AWS EC2 Mac) i przenieś tam wygenerowany folder projektu Xcode (albo cały projekt Unity + zainstaluj tam Unity Hub).
- **Unity Cloud Build** albo inny CI (Codemagic, Bitrise) — buduje iOS w chmurze bez posiadania własnego Maca, potrzebujesz tylko konta Apple Developer (99 USD/rok).
- Na start skup się na Androidzie — nic w projekcie nie blokuje dodania iOS później.

### 8.4 Testy wydajności na realnym telefonie

W każdej scenie w lewym górnym rogu ekranu powinna pojawić się nakładka **FPS / ms / Jakość** (`DebugPerformanceOverlay`, Etap 6) — widoczna tylko w Editorze i w **Development Build** (zaznacz „Development Build" w Build Settings przed buildem testowym; w prawdziwym release-buildzie nakładka sama się nie pokaże, więc nie trzeba jej ręcznie usuwać).

Checklista do przetestowania na telefonie (najlepiej średniej półki, kilka lat wsteczny model — brief mówi o „mid-range phones z ostatnich lat"). Oznaczenia: **[auto]** — już sprawdzone automatycznie (testy / soak w Edytorze, bez telefonu); **[skrypt]** — zmierzy `Tools\phone_soak.ps1` na podłączonym telefonie; **[ręcznie]** — tylko Ty (odczucia, głośnik, wibracje, Twój ekran).
- [ ] Gra utrzymuje docelowe FPS z nakładki (Low 30, Medium/High 60 — zależnie od `QualityManager.CurrentTier`) przez pełną zmianę w kuchni z rosnącą Górą. **[auto]** soak 15 min w Edytorze: 60 FPS na High przez 4 zmiany. **[skrypt]** to samo na telefonie (`soak_lines.txt`).
- [ ] Automatyczna jakość (`QualityGovernor`) **[auto]** `QualityGovernorTests` + soak (High utrzymane mimo przycinek ~450 ms); **[skrypt]** Tier w próbkach z telefonu: każde okno 3 s ocenia **mediana** czasu klatki, więc pojedyncza przycinka (ładowanie sceny, GC, powiadomienie) niczego nie zmienia; poziom spada o jeden dopiero po **dwóch** wolnych oknach z rzędu (mediana dłuższa niż klatka limitu FPS + 25%). Poniżej poziomu startowego gra po minucie próbuje wrócić wyżej; jeśli próba okaże się za wolna, wraca i następną robi po dwa razy dłuższej przerwie (najdłużej 8 min). Sprawdź w nakładce: długie przeciążenie (dużo naczyń naraz) obniża Tier, a po uspokojeniu Tier z czasem wraca.
- [ ] Dotyk: swipe/hold/rub/drag z Etapu 1 działają identycznie jak myszką w Edytorze. **[auto]** testy PlayMode i autoplay idą przez Input System tą samą ścieżką co palec (`Pointer.current`). **[ręcznie]** czy gesty łapią pod prawdziwym palcem i czy szorowanie jest przyjemne.
- [ ] Wibracje (Android): lekkie przy zdrapaniu i odłożeniu, średnie przy domyciu, mocne przy stłuczeniu — i że przełącznik „Wibracje" w Ustawieniach realnie je wyłącza. **[ręcznie]** (testy sprawdzają tylko, że zdarzenia padają; drgań nie ma w Edytorze).
- [ ] Dźwięk: woda/szorowanie nie trzeszczą na styku pętli, głośność jest rozsądna na głośniku telefonu, przełącznik „Dźwięk" wycisza kuchnię. **[auto]** `ProceduralSoundsTests` i `RecordedSoundsTests`: bez przesterowania, pętle bez kliknięcia, każdy dźwięk ma nagranie, każde nagranie jest w `CREDITS.md`. **[ręcznie]** brzmienie nagrań i głośność na głośniku telefonu.
- [ ] Cząsteczki nie zjadają FPS na Low (piana przy szybkim szorowaniu to najwięcej naraz). **[skrypt]** jeśli telefon zejdzie na Low, próbki soak to pokażą; **[ręcznie]** ostre szorowanie na Low.
- [ ] Szklanka pod światło: czy przechylanie telefonu wygodnie prowadzi odbicie (czułość `InspectionLight.TiltGain`), czy zacieki są dobrze widoczne w odbiciu, i czy wyłączenie „Przechylania telefonu" przełącza na samoczynne odbicie, które da się przesuwać palcem obok szklanki. **[auto]** test przechylenia (symulowany akcelerometr) i test przeciągnięcia palcem. **[ręcznie]** wygoda i czułość na prawdziwym telefonie.
- [ ] Układ na Twoim ekranie: porównaj z `Logs\Screenshots\20_kitchen_*.png`, `21_hub_*.png` i `22_hub_settings_*.png` (16:9, 19.5:9, 20:9, tablet 4:3) — przyciski w całości na ekranie, nic na siebie nie nachodzi, narzędzia wygodnie pod kciukiem. **[auto]** `LayoutPlayModeTests` w czterech proporcjach + 48 dp na trzech profilach telefonów. **[ręcznie]** Twój konkretny ekran i chwyt.
- [ ] Bezpieczny obszar ekranu (`SafeAreaFitter`) — UI nie chowa się pod notchem/wcięciem na telefonach, które je mają. **[ręcznie]** (Edytor nie ma wcięcia).
- [ ] Rozmiar tekstu w Ustawieniach faktycznie się zmienia (dialogi, hub, kuchnia, przyciski). **[auto]** `LayoutPlayModeTests`: przy 1,5× teksty kuchni i huba rosną i nie wychodzą poza pola w czterech proporcjach ekranu (zrzuty `23_*_largest_text_*.png`). **[ręcznie]** czy na Twoim telefonie 1,5× czyta się wygodnie.
- [ ] Bateria/temperatura przy dłuższej sesji (15-20 min) — czy telefon się przegrzewa, czy `QualityManager` odpowiednio reaguje. **[skrypt]** `thermal.csv`: temperatura baterii i status termiczny co 30 s; w Edytorze niemierzalne.
- [ ] Zabicie aplikacji w trakcie zmiany i powrót. **[auto]** `AppLifecyclePlayModeTests` (stan sprzed zmiany wraca, zapis odporny na przerwany zapis). **[ręcznie]** raz na telefonie: przesuń grę z ostatnich aplikacji w trakcie zmiany i uruchom ponownie.
- [ ] Gest „wstecz" i przerwania (połączenie, przełączenie aplikacji) pauzują zmianę i dźwięk. **[auto]** test pauzy gestem wstecz. **[ręcznie]** prawdziwe połączenie przychodzące.

Wyniki (FPS na każdym Tier, czy coś się zacina, czy dotyk jest responsywny) najlepiej zapisać — to podstawa do dalszego dostrajania progów w `QualityManager` i `DishPileController`.

**Test wytrzymałościowy (soak) — gra gra sama.** Build developerski ma tryb **autoplay** (`DevAutoplay`): gra sama przechodzi menu, wieczór w hubie (scena fabularna, telefon do mamy, pub, przelew) i kolejne zmiany w kuchni — zdrapuje, płucze, szoruje właściwym narzędziem, sprawdza szklanki pod światłem, pakuje stojak, odpala zmywarkę i naprawia jej usterki — przez **prawdziwą ścieżkę wejścia** (wirtualny wskaźnik w Input System, tak jak przychodzą dotknięcia palca). Co 30 s zapisuje próbkę: FPS, najwolniejszą klatkę, pamięć, liczbę tekstur i siatek (wyciek widać jako liczbę rosnącą zmiana po zmianie), baterię i poziom jakości — jako linię `[Soak]` w logcat i wpis `"type":"soak"` w dzienniku rozgrywki. Temperaturę i status termiczny telefonu co 30 s czyta przez adb skrypt na PC. W buildzie release autoplay nie istnieje (`Debug.isDebugBuild`).

Z telefonem podłączonym USB (zainstalowany Development APK — sekcja 9, kroki 5-6), telefon leży i nikt go nie dotyka:
```powershell
powershell -ExecutionPolicy Bypass -File "$proj\Tools\phone_soak.ps1" -Minutes 15
```
Wyniki w `Logs\soak_<data>_<godzina>\`: `thermal.csv` (minuta, temperatura baterii °C, poziom baterii, status termiczny 0-6: 0 brak, 1 lekki, 2 umiarkowany, 3 poważny = dławienie CPU/GPU, 4+ krytyczny), `soak_lines.txt` (próbki gry), `logcat.txt`, `gameplay_log.jsonl`. Autoplay można też włączyć ręcznie: `& $adb shell am start -n com.zielonawyspagames.zmywak/com.unity3d.player.UnityPlayerGameActivity --ez zmywak_autoplay true --ei zmywak_soak_minutes 15`. Na czas autoplay sterowanie odbiciem przechyłem jest wstrzymane (leżący telefon się nie przechyla) — ustawienie gracza się nie zmienia.

**Gdzie ginie czas przy starcie zmiany (`[Startup]`).** W Edytorze i w buildzie developerskim (nie w release) każde ładowanie sceny przez `SceneLoader` otwiera na 8 s okno pomiarowe (`StartupTimeline`). W oknie mierzone są kawałki rozruchu: fazy ładowania sceny (ekran ładowania, ładowanie w tle, aktywacja = `Awake`/`OnEnable`, pierwsza klatka = `Start`), `Awake`/`Start` komponentów kuchni (`KitchenAudio`, `KitchenFx`, `KitchenRadio`, `SceneLighting`, `ShiftHUD`...), `ShiftController.Start` z etapami, tworzenie naczynia (kolizja convex, dirt wygenerowany na wątku głównym albo z prewarmu, tekstura brudu, pełny redraw), synteza dźwięku przy pierwszym użyciu, zapis i odczyt zapisu, ładowanie assetów z `Resources`. Na końcu okna powstaje jeden blok `[Startup]`: liczba klatek i najgorsza, odśmiecenia (GC) i pamięć, każdy kawałek z licznością, sumą i maksimum, a dla **każdej klatki wolniejszej niż 30 fps** to, co się w niej wykonało, i „rest” (to, czego zmierzone kawałki nie tłumaczą: render, fizyka, aktywacja sceny) razem z ilością zaalokowaną w tej klatce. Każdy kawałek jest też próbką `Profiler.BeginSample` o tej samej nazwie, więc build developerski z Autoconnect Profiler pokazuje je na osi czasu. Z telefonu: `& $adb logcat -s Unity | Select-String "Startup"`. Test `StartupTimelinePlayModeTests` pilnuje, że raport z prawdziwego startu kuchni nazywa te kawałki.

### 8.5 Buildy w chmurze (GitHub Actions + GameCI)

Lokalny build Androida blokował na laptopie Smart App Control (niepodpisany `Bee.Toolchain.GNU.dll` z Unity); na komputerze z RTX jest wyłączony (sprawdzone 6.10.2026), więc tam lokalny build jest możliwy (8.0). Zamiast go wyłączać, APK buduje się na serwerach GitHuba: workflow `.github/workflows/android.yml` przy każdym wypchnięciu na `master` (pomija zmiany samych `.md`, `Tools/`, `Logs/`) i na żądanie: **testy EditMode i PlayMode** (PlayMode równolegle, ze zrzutami ekranu jako artefakt `PlayMode-screenshots`; na serwerze bez GPU renderuje programowo, co nie jest jeszcze sprawdzone — dlatego na razie tylko raportuje i nie wstrzymuje APK; gdy okaże się stabilny, wystarczy dopisać go do `needs` joba `android`) **→ development APK** (ten sam skrypt co lokalnie: IL2CPP, ARM64, Development) → **sprawdzenie stron 16 KB** (`zipalign -P 16` i wyrównanie segmentów każdej biblioteki `.so`) oraz **target API ≥ 36** → APK do pobrania jako artefakt (14 dni). Workflow bierze wersję Unity z `ProjectVersion.txt` (teraz `6000.6.3f1`). Czy GameCI ma już obraz tej wersji z modułem Android, trzeba sprawdzić przy pierwszym uruchomieniu workflow.

Jednorazowo (wszystko robisz Ty — ja nie widzę ani nie potrzebuję wartości sekretów):
1. Na github.com: **New repository** → nazwa np. `zmywak` → **Private** → bez README/.gitignore (puste repo).
2. Wypchnij kod (obrazki idą przez Git LFS, jest już skonfigurowany):
```powershell
cd "C:\Users\pawel\OneDrive\PowerWash Simulator"
git lfs install
git remote add origin https://github.com/TWOJ_LOGIN/zmywak.git
git push -u origin master
```
3. Plik licencji Personal: **Unity Hub → Settings (zębatka) → Licenses → Add → Get a free personal license**. Powinien powstać `C:\ProgramData\Unity\Unity_lic.ulf` (samo „Personal” widoczne w Hubie nie znaczy, że plik istnieje — sprawdź).
4. W repo: **Settings → Secrets and variables → Actions → New repository secret** — trzy sekrety:
   - `UNITY_EMAIL` — e-mail konta Unity,
   - `UNITY_PASSWORD` — hasło konta Unity,
   - `UNITY_LICENSE` — **cała zawartość** pliku `Unity_lic.ulf` (otwórz w Notatniku, zaznacz wszystko, wklej).
5. **Actions → Android → Run workflow** (albo po prostu kolejne `git push`). Po ~30-40 min w podsumowaniu przebiegu jest artefakt **Zmywak-dev-apk** (zip z APK) — rozpakuj i zainstaluj: `& $adb install -r Zmywak-dev.apk` (sekcja 9, krok 6).

Uwagi: jeśli Hub nie tworzy pliku `.ulf`, pomiń `UNITY_LICENSE` — nowsze GameCI (zmiana z 5.09.2026) aktywuje licencję Personal z samego e-maila i hasła; nie da się tego sprawdzić bez przebiegu w chmurze, więc jeśli aktywacja się nie uda, przyślij mi linię błędu z logu kroku „unity-builder”. Taka aktywacja „bez okna" **nie przejdzie weryfikacji dwuetapowej** konta Unity, a hasła ze znakami specjalnymi sprawiały GameCI problemy — ścieżka z plikiem `.ulf` jest pewniejsza. Prywatne repo ma limit darmowych minut Actions (2000/mies.); jeden przebieg to ok. 30-40 min, stąd `cancel-in-progress` (nowe wypchnięcie przerywa poprzedni przebieg) i pomijanie zmian samej dokumentacji. Podpisany build do sklepu (AAB z keystore) to osobny krok — będzie wymagał kolejnych sekretów z keystore.

## 9. Linia komend (batch mode) — kompilacja, setup, testy, buildy

Wszystko poniżej da się powtórzyć bez otwierania edytora. **Projekt nie może być w tym czasie otwarty w Unity Editorze** (blokuje go). Licencja musi być aktywna w Unity Hub.

Komendy są dla **PowerShell**. `Unity.exe` to aplikacja okienkowa — wywołana przez `&` nie czeka na zakończenie i nie zwraca kodu wyjścia, dlatego wszędzie jest `Start-Process -Wait -PassThru`. Kod wyjścia `0` = sukces; szczegóły zawsze w pliku `-logFile`.

```powershell
$unity = "C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Unity.exe"
$proj  = "C:\Users\pawel\OneDrive\PowerWash Simulator"
function Invoke-Unity([string[]]$UnityArgs) {
    $p = Start-Process -FilePath $unity -ArgumentList (@('-batchmode', '-projectPath', "`"$proj`"") + $UnityArgs) -Wait -PassThru
    "exit code: $($p.ExitCode)"
}
```

**1. Kompilacja** (import pakietów + kompilacja C#; błędy szukaj w logu po `error CS`):
```powershell
Invoke-Unity @('-nographics', '-quit', '-logFile', "`"$proj\Logs\compile.log`"")
```

**2. Setup (generowanie URP, Input System, contentu i scen)** — bez `-quit`, metoda sama kończy proces kodem 0/1 po weryfikacji wyników. **Bez `-nographics`:** Run All wypieka oświetlenie kuchni i pokoju na karcie graficznej (`BakedLighting`, ok. 10 s na scenę na RTX 3070, pierwsze wypiekanie dłużej). Z `-nographics` sceny się zbudują, ale bez wypieczonego światła (ostrzeżenie w logu: „light was NOT baked”).
```powershell
Invoke-Unity @('-executeMethod', 'Zmywak.EditorTools.VerticalSliceSetup.RunAll', '-logFile', "`"$proj\Logs\runall.log`"")
```

**3. Testy EditMode** (wynik w XML — atrybuty `total/passed/failed` w `test-run`):
```powershell
Invoke-Unity @('-nographics', '-runTests', '-testPlatform', 'EditMode', '-testResults', "`"$proj\Logs\editmode.xml`"", '-logFile', "`"$proj\Logs\editmode.log`"")
```

**4. Testy PlayMode + zrzuty ekranu** — celowo **bez** `-nographics` (bez GPU nie działa `Camera.WorldToScreenPoint` ani render do tekstury). Zrzuty lądują w `Logs\Screenshots\`. Laptop, który w trakcie przebiegu przejdzie w uśpienie (Modern Standby — np. po wygaszeniu ekranu na baterii), zamraża Unity razem z testami; testy liczą swoje limity czasu tylko w czasie, w którym gra faktycznie działała (`PlayDeadline`), więc uśpienie ich nie wywraca, ale przebieg trwa dłużej — przy długich przebiegach najlepiej trzymać laptopa na zasilaczu z niewygaszanym ekranem:
```powershell
Invoke-Unity @('-runTests', '-testPlatform', 'PlayMode', '-testResults', "`"$proj\Logs\playmode.xml`"", '-logFile', "`"$proj\Logs\playmode.log`"")
```

Szybkie podsumowanie wyników testów:
```powershell
[xml]$x = Get-Content "$proj\Logs\playmode.xml"; $x.'test-run' | Select-Object total, passed, failed
```

**4a. Soak w Edytorze** — ten sam autoplay przez 15 minut (zmienna `ZMYWAK_SOAK_MINUTES` zmienia długość), od ekranu Boot: kolejne zmiany i wieczory, bez błędów w logu, bez rosnącej pamięci i mnożących się tekstur/siatek. Test jest oznaczony `[Explicit]`, więc zwykłe `-runTests` go pomija; tabela próbek trafia do wyjścia testu i do `Logs\soak_editor.log`:
```powershell
Invoke-Unity @('-runTests', '-testPlatform', 'PlayMode', '-testFilter', 'Zmywak.Tests.PlayMode.SoakPlayModeTests.Soak_ShiftAfterShift_WithoutErrorsLeaksOrSlowdown', '-testResults', "`"$proj\Logs\soak_editor.xml`"", '-logFile', "`"$proj\Logs\soak_editor.log`"")
Select-String -Path "$proj\Logs\soak_editor.log" -Pattern "\[Soak\]"
```
Wynik na tej wersji (laptop, Intel UHD, 15 min): 4 zmiany po 3 min, 35 naczyń na stojaku, 3 awarie zmywarki — wszystkie naprawione; **60 FPS i poziom High przez cały czas** (przycinki ~450 ms przy przejściu między zmianami nie obniżają już jakości); pamięć stała ~388 MB, tekstury wracają do ~246 po każdej zmianie — bez wycieków. To Edytor na PC; telefon trzeba zmierzyć skryptem z sekcji 8.4.

**5. Android — development APK** (IL2CPP, ARM64) → `Builds\Android\Zmywak-dev.apk`. Pierwszy build trwa kilkanaście–kilkadziesiąt minut, kolejne są inkrementalne. Najprościej jedną komendą, `Tools\build_android.ps1` (sekcja 8.0); to samo ręcznie:
```powershell
Invoke-Unity @('-nographics', '-buildTarget', 'Android', '-executeMethod', 'Zmywak.EditorTools.AndroidBuildScript.BuildAndroid', '-logFile', "`"$proj\Logs\build_android.log`"")
```

**6. Instalacja na telefonie** (USB, włączone Opcje programisty → Debugowanie USB; `adb` z SDK dołączonego do Unity):
```powershell
$adb = "C:\Program Files\Unity\Hub\Editor\6000.6.3f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe"
& $adb devices
& $adb install -r "$proj\Builds\Android\Zmywak-dev.apk"
& $adb shell monkey -p com.zielonawyspagames.zmywak -c android.intent.category.LAUNCHER 1
& $adb logcat -s Unity
```
Nazwa paczki to `com.zielonawyspagames.zmywak`, a pod ikoną na telefonie widać „ZMYWAK" (krótko, bo launcher ucina długie nazwy; pełny tytuł „ZMYWAK: Zielona Wyspa" jest w menu gry). Ustawia to **Run All** (krok `EnsureProductIdentity`) — wcześniej Unity wyprowadzało tożsamość z nazwy folderu (`com.DefaultCompany.PowerWashSimulator`, „PowerWash Simulator" — nazwa cudzej gry). Stary build z tamtą nazwą paczki zostaje na telefonie jako osobna aplikacja; usuń go raz:
```powershell
& $adb uninstall com.DefaultCompany.PowerWashSimulator
```
Identyfikator paczki można jeszcze zmieniać — **po pierwszym wysłaniu do sklepu już nigdy**.

**6-skrót. Pierwsze podejście do telefonu jedną komendą** — instaluje APK, uruchamia grę, po 10, 20 i 40 s robi zrzuty ekranu **z telefonu** i zbiera log Unity z wyłuskanymi błędami/wyjątkami (`Logs\phone_<data>_<godzina>\`: `screen_*.png`, `logcat.txt`, `errors.txt`, `phone.txt` z modelem, rozdzielczością i gęstością ekranu). Domyślnie bierze lokalny `Builds\Android\Zmywak-dev.apk`; APK z GitHub Actions podajesz parametrem:
```powershell
powershell -ExecutionPolicy Bypass -File "$proj\Tools\phone_check.ps1"
powershell -ExecutionPolicy Bypass -File "$proj\Tools\phone_check.ps1" -Apk "$HOME\Downloads\Zmywak-dev.apk"
```
(Zrzuty powstają na telefonie i są ściągane przez `adb pull` — przepuszczenie `adb exec-out screencap -p` przez Windows PowerShell 5.1 psuje plik PNG.) Test wytrzymałościowy (`phone_soak.ps1`, sekcja 8.4) też robi zrzut po pierwszej minucie i potem co 5 minut.

**6a. Dziennik rozgrywki z telefonu.** Gra prowadzi lokalny dziennik `gameplay_log.jsonl` w swoim `persistentDataPath` — jedna linia JSON na zdarzenie, **nic nie jest wysyłane przez sieć**. `"type":"shift"` to zakończona zmiana: plan i faktyczna długość (i czy skończona wcześniej z pauzy), ile naczyń przyszło / umytych / niedokończonych / stłuczonych, zawalenia Góry, upuszczenia obok stojaka, pełne kosze, cykle zmywarki, zamówienia szefa (wydane / spełnione), najdłuższa seria Flow, szklanki pod światłem, pauzy, wypłata rozpisana na stawkę, napiwki i potrącenia, **średni czas na każdym kroku w zlewie** (zdrapywanie, płukanie, szorowanie, sprawdzanie pod światłem, droga na stojak — z naczyń, które trafiły na stojak), liczba awarii zmywarki i każda naprawa (usterka i czas) oraz średnie FPS i udział klatek wyraźnie wolniejszych niż 30 FPS (ponad 36,7 ms — klatka przy limicie 30 FPS trwa 33,4 ms i wolna nie jest). `"type":"life"` to życie między zmianami: wypłata, rachunki tygodnia, telefon do mamy, pub, przelew do domu, skutki scen fabularnych — każde ze stanem portfela i statów po nim. Plik ma limit 1 MB (potem przechodzi w `gameplay_log.old.jsonl`). Ściągnięcie z telefonu:
```powershell
$pkg = "com.zielonawyspagames.zmywak"
& $adb pull "/sdcard/Android/data/$pkg/files/gameplay_log.jsonl" "$proj\Logs\gameplay_log.jsonl"
```
Jeśli telefon nie pozwala czytać `Android/data` (niektóre nakładki Androida 11+), w buildzie developerskim zadziała odczyt jako aplikacja:
```powershell
& $adb exec-out run-as $pkg cat "/sdcard/Android/data/$pkg/files/gameplay_log.jsonl" | Set-Content "$proj\Logs\gameplay_log.jsonl" -Encoding utf8
```
W Edytorze plik leży w `%USERPROFILE%\AppData\LocalLow\Zielona Wyspa Games\ZMYWAK\` (ta sama nazwa firmy/produktu co w paczce; zapisy sprzed zmiany nazwy zostały w `...\LocalLow\DefaultCompany\PowerWash Simulator\` — skopiuj `zmywak_save.json` do nowego folderu, jeśli chcesz grać dalej od tamtego miejsca). Szybki podgląd zmian w PowerShellu:
```powershell
Get-Content "$proj\Logs\gameplay_log.jsonl" | ConvertFrom-Json | Where-Object type -eq 'shift' | Format-Table time, dishesCleaned, dishesBroken, wageEuros, scrapeSeconds, rinseSeconds, scrubSeconds, inspectSeconds, averageFps
```

**7. iOS — tylko projekt Xcode** → `Builds\iOS\Xcode\` (kompilacja i podpis wyłącznie na Macu, patrz 8.3):
```powershell
Invoke-Unity @('-nographics', '-buildTarget', 'iOS', '-executeMethod', 'Zmywak.EditorTools.IosBuildScript.BuildXcodeProject', '-logFile', "`"$proj\Logs\build_ios.log`"")
```
