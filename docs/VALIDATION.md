# Weryfikacja prototypu

Wykonano 4 października 2026 na Linux, .NET SDK 10.0.401, runtime 10.0.12. Testowane rozwiązanie obejmuje A + D + E.

## Wyniki

| Obszar | Wynik |
| --- | --- |
| Kompilacja Release całego rozwiązania | Sukces; 0 ostrzeżeń, 0 błędów |
| Runner reguł i SQLite | 24 sprawdzenia zakończone sukcesem |
| Testy HTTP rzeczywistych endpointów | 14 sprawdzeń zakończonych sukcesem |
| Interfejs w headless Chromium | Poprawny start, 4 miejsca, 10 cech, mapa/lista zgodne, preferencje, okno zgłoszenia, Escape, zapis i konflikt; brak błędów JavaScript |
| Widok partnera | JSON z działającego API i ten sam konflikt w widżecie iframe |
| Widok telefonu 390 × 844 | Brak poziomego przepełnienia; wszystkie funkcje w układzie pionowym |
| Rzeczywisty import publicznego Overpass | Sukces; 300 miejsc, 61 obserwacji; wynik konkretnego wycinka w chwili testu |
| Niedostępne źródło OSM | HTTP 503; zapisane miejsca zachowane |

Runner w C# sprawdza między innymi: brak danych, stare daty, konflikt pomimo potwierdzeń, wygaśnięcie zgłoszenia, rozdzielenie istnienia i działania windy, poprawność wartości, ostrożne mapowanie OSM, idempotencję demo, konkurencyjne potwierdzenia, trwałość bazy i zachowanie społeczności przy imporcie.

HTTP sprawdza między innymi: wymagany CSRF, walidację, utworzenie zgłoszenia, blokadę własnego i powtórnego potwierdzenia, potwierdzenie w dwóch innych sesjach, brak ujawnienia identyfikatorów autora, konflikt w API i awarię źródła.

## Odtworzenie

```bash
dotnet build AccessCity.sln -c Release
dotnet run --project tests/AccessCity.Tests -c Release
```

Opcjonalnie po uruchomieniu aplikacji w drugim terminalu:

```bash
dotnet run --project tests/AccessCity.Tests -c Release -- http://localhost:5080
```

Drugi parametr runnera może wskazywać instancję z celowo niedostępnym źródłem, np. `http://localhost:5081`. Uruchom ją z konfiguracją `OpenStreetMap__Endpoint=http://127.0.0.1:9`, osobną ścieżką `Storage__Directory` i portem 5081. To test nieudanej synchronizacji; nie zmieniaj tak konfiguracji normalnej instancji demonstracyjnej.

Przykład PowerShell w trzecim terminalu:

```powershell
$env:ASPNETCORE_ENVIRONMENT='Development'
$env:OpenStreetMap__Endpoint='http://127.0.0.1:9'
$env:Storage__Directory="$env:TEMP/accesscity-offline-check"
dotnet run --project src/AccessCity.Api --no-launch-profile --urls http://localhost:5081
```

Następnie w osobnym terminalu bez tych zmiennych:

```bash
dotnet run --project tests/AccessCity.Tests -c Release -- http://localhost:5080 http://localhost:5081
```

## Kontrola dostępności i ograniczenia

W kodzie i w teście przeglądarkowym sprawdzono: etykiety pól, tekstowe statusy i źródła, link omijania nawigacji, fokus klawiatury, natywne dialog/fieldset/select, zamykanie Escape oraz tekstową alternatywę mapy. Zrzuty desktop/mobile obejrzano po renderowaniu.

Dodatkowa kontrola 4.10.2026: axe-core 4.10.3, główny początkowy widok 1440 × 900, tagi WCAG 2 A/AA, 2.1 AA i 2.2 AA: 0 naruszeń, 28 zaliczonych reguł, 2 reguły z elementami do ręcznej oceny (aria-prohibited-attr i color-contrast). Poprawiono kontrast stopki i cele interakcji źródeł. Raport `accessibility/axe-main-view.json` ma zakres ograniczony do tego widoku. Nagranie pełnego scenariusza zgłoszeniowego: brak błędów JavaScript. Dodatkowo wykonano główny scenariusz wyłącznie zdarzeniami klawiatury: link pomijania, checkboxy, szerokość, zastosowanie preferencji, wyszukanie hotelu i otwarcie dialogu; Escape zamyka, a fokus wraca do przycisku. Wynik `accessibility/keyboard-main-flow.json`. To kontrola przeglądarkowa bez czytnika ekranu.

Nie przeprowadzono pełnego audytu WCAG 2.2 AA, testów NVDA/VoiceOver ani badań z użytkownikami. Nie deklarujemy zgodności AA na podstawie tej kontroli. Oczekujące prace: test czytnikami ekranu, powiększenie 200–400%, pełna analiza kontrastu/rozmiaru celów, kolejność fokusu i ocena z użytkownikami. Mapa ma alternatywę w liście.

Docker Compose nie był uruchamiany w środowisku testowym. Hosting publiczny, HTTPS za proxy, abonamenty B2B, moderacja i weryfikacja tożsamości nie były wdrażane.

## Zrzuty

- [Desktop — pierwsze uruchomienie](screenshots/desktop.png)
- [Konflikt po nowym zgłoszeniu](screenshots/conflict.png)
- [API i widget](screenshots/partners.png)
- [Widok telefonu](screenshots/mobile.png)
