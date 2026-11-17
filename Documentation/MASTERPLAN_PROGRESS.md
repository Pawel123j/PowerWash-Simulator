# ZMYWAK — postęp masterplanu 1.0

Aktualizacja: 16.10.2026. Praca w małych partiach, zgodnie z planem użytkownika. Pierwsza partia: TASK-001. Szczegóły, zmienione pliki, dowody i ograniczenia: [TASK_001_IMPLEMENTATION.md](TASK_001_IMPLEMENTATION.md).

Postęp listy: **1/44 potwierdzonych** — TASK-001 wdrożony i sprawdzony: 771/771 EditMode i 42/42 celowanych PlayMode na końcowym kodzie. Zaliczenie testów komputerowych nie zastępuje odbioru na telefonach ani testów nowych graczy. Ograniczenie historycznych, niedokończonych zapisów v2 opisuje raport implementacji.

## 1. Błędy krytyczne dla postępu

- [x] Naprawić podwójne naliczanie pieniędzy w dialogach po restarcie — TASK-001, jednokrotność skutków zapisanych w v3; regresja zwrotu 30 EUR od Tomka.
- [ ] Poprawić komunikat i odzyskiwanie danych przy nieudanym zapisie — obsługa dialogów w TASK-001; całość TASK-002 pozostaje.
- [ ] Naprawić ocenianie ostatniego gestu mycia.
- [ ] Poprawić zaliczanie inspekcji i awansów.
- [ ] Naprawić ponawianie tutorialu i zapis pierwszej naprawy.

## 2. Automatyzacja testów i bezpieczeństwo projektu

- [ ] Przenieść brakujące narzędzia QA do repozytorium.
- [ ] Uruchamiać komplet wymaganych testów EditMode i PlayMode — bieżąca regresja jest częścią TASK-001; stały proces jeszcze niegotowy.
- [ ] Objąć odpowiednimi testami przypadki Explicit.
- [ ] Dodać bramkę blokującą dystrybucję APK po nieudanych testach.
- [ ] Przechowywać raporty i identyfikatory testowanego builda.

## 3. Zapis gry i działanie na telefonie

- [ ] Wybrać sposób wznowienia przerwanej zmiany: snapshot albo checkpoint.
- [ ] Obsłużyć zamknięcie aplikacji podczas pracy.
- [ ] Przetestować pauzę, blokadę ekranu i powrót do aplikacji.
- [ ] Sprawdzić gesty na telefonach o różnych rozdzielczościach.
- [ ] Wykluczyć przypadkową utratę ważnego postępu.

## 4. Fabuła i progresja

- [ ] Przetestować wszystkie 6 rozdziałów.
- [ ] Zweryfikować osiągalność wszystkich przewidzianych zakończeń.
- [ ] Poprawić niedziałające konsekwencje wyborów.
- [ ] Domknąć epilog i zachowanie gry po finale.
- [ ] Sprawdzić awanse, zarobki, koszty i ryzyko zadłużenia.
- [ ] Przetestować trzy niezależne sloty zapisu — izolacja transakcji objęta TASK-001; pełne kampanie na slotach pozostają.

## 5. Rozgrywka i interfejs

- [ ] Udoskonalić odczucia podczas mycia i szorowania.
- [ ] Dopracować reakcje wizualne oraz dźwiękowe.
- [ ] Poprawić czytelność instrukcji i HUD.
- [ ] Sprawdzić balans presji czasu i trudności.
- [ ] Przetestować pierwszą zmianę z nowymi graczami.
- [ ] Zweryfikować opcje dostępności i rozmiary tekstu.

## 6. Optymalizacja Androida

- [ ] Zmierzyć rzeczywiste FPS i czasy klatek na telefonach.
- [ ] Sprawdzić RAM, GC i temperaturę urządzenia.
- [ ] Zoptymalizować najdroższe operacje wykazane przez profiler.
- [ ] Sprawdzić ładowanie scen i rozmiar APK.
- [ ] Przetestować grę na urządzeniu z dolnej granicy wymagań.

## 7. Zamknięta alfa i beta

- [ ] Przygotować instalowalny build testowy.
- [ ] Zaprosić testerów, którzy wcześniej nie grali.
- [ ] Zebrać błędy, uwagi i opinie o rozgrywce.
- [ ] Naprawić blokujące i poważne problemy.
- [ ] Przetestować aktualizację istniejącego zapisu.
- [ ] Powtórzyć pełną regresję po poprawkach.

## 8. Wersja 1.0

- [ ] Zamrozić zakres funkcji wersji 1.0.
- [ ] Przygotować finalne menu, ikony i materiały sklepu.
- [ ] Sprawdzić uprawnienia, politykę prywatności i wymagania sklepu.
- [ ] Przygotować podpisany build wydania.
- [ ] Zarchiwizować wyniki testów i konfigurację.
- [ ] Przeprowadzić ostateczny test akceptacyjny.
