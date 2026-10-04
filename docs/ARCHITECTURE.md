# Architektura

## Przepływ

1. `OpenStreetMapProvider` pobiera wybrany obszar i normalizuje jawne tagi do modelu `Observation`.
2. `SqliteRepository` zapisuje snapshot dostawcy w transakcji; zgłoszenia są niezależne.
3. `AccessibilityEngine` w C# rozwiązuje cechy: niewiadoma, stara informacja, zgodne wskazania, konflikt. Porównuje wynik z preferencjami.
4. Minimal API udostępnia ten sam wynik aplikacji webowej, integratorom i widżetowi.
5. Przeglądarka prezentuje wynik; nie posiada kopii reguł dostępności.

Repozytorium i dostawca są oddzielone od prezentacji. Jedna aplikacja ASP.NET hostuje UI i API, co upraszcza uruchomienie hackathonowe. Nie potrzebujemy CORS do widżetu iframe; zewnętrzne aplikacje backendowe mogą używać HTTP GET bez CORS. Przeglądarkowa integracja przez bezpośredni fetch z obcego originu wymaga przyszłej konfiguracji CORS.

## Model

| Model | Znaczenie |
| --- | --- |
| `Place` | Miejsce, miasto, współrzędne, źródłowy adres i jawne `IsDemo` |
| `Observation` | Jedna cecha, wartość, źródło, URL, czas edycji/pobrania, surowy tag |
| `CommunityReport` | Niezależna obserwacja użytkownika, losowy identyfikator sesji, komentarz i okres ważności |
| `Confirmation` w SQLite | Unikalna para zgłoszenie/sesja; nie można zagłosować dwa razy |
| `FeatureResult` | Uzgodniona wartość albo null, komplet dowodów i status |
| `Preferences` | Wymagania wobec konkretnych barier, bez informacji o diagnozie |

Wartości są kanonicznymi stringami z walidacją katalogu: true/false, liczby w cm albo dozwolony słownik. Ułatwia to prosty adapter źródeł; w większym systemie można przejść do typowanych wartości i jednostek bez zmiany filozofii danych.

## Skalowanie

- Nowe źródło: implementacja `IAccessibilityDataProvider`, mapowanie źródła i licencji, osobne źródłowe identyfikatory, zapis `ProviderBatch`.
- Nowe miasto: mały `CityArea`, kategorie oraz dostawca. Pełne miasto wymaga podziału obszaru na okna, usuwania duplikatów i sprawdzania kompletności.
- Nowa cecha: definicja w `Features`, walidacja i normalizacja, adapter, reguła preferencji, UI input i testy.
- Więcej ruchu: przeniesienie `IAccessibilityRepository` do PostgreSQL, cache wyników, asynchroniczne zadania importowe, obserwowalność i ograniczenia klientów.
- Nie łączymy miejsc między źródłami wyłącznie po nazwie. Deduplikacja przestrzenna i identyfikacja wejść są przyszłymi pracami.

SQLite jest wystarczające do jednego demo/małego pilotażu, nie projektujemy tej konfiguracji jako wieloinstancyjnej platformy. `Search` wykonuje proste odczyty na maksymalnie 300 miejscach; przy większej skali potrzebne będą zapytania zbiorcze i indeksy przestrzenne.

## Bezpieczeństwo i prywatność

- Losowy identyfikator sesji chroniony przez ASP.NET Data Protection; cookie HttpOnly, SameSite Lax, Secure dla HTTPS.
- Token CSRF na wszystkie POST; brak zapisu przez GET.
- Limit: 20 zapisów/minutę na adres IP, 2 wywołania importu/10 minut i globalny semafor importu.
- Parametry SQL, limit rozmiaru żądania, zakres wartości i długość komentarza.
- Teksty użytkowników kodowane podczas renderowania, bez wykonywania HTML komentarzy.
- Publiczne API nie zwraca wewnętrznego identyfikatora autora/sesji.
- Administrator importu: klucz z konfiguracji poza Development; lokalny profil demo pozwala na import bez klucza.
- Źródłowy endpoint jest konfiguracją administratora, nie parametrem żądania użytkownika.
- Preferencje zapisane lokalnie; brak pytań o niepełnosprawność, loginu, imienia i GPS.

Brakuje jeszcze: moderacji, weryfikacji osób/właścicieli, kontroli Sybil, polityki retencji i usuwania danych, szyfrowania kluczy na dysku, audytu uprawnień oraz produkcyjnych alertów. Użytkownik może utworzyć nową sesję w innej przeglądarce — dlatego liczniki są opisane jako potwierdzenia sesji.

Wdrożenie za proxy wymaga świadomej konfiguracji zaufanych forwarded headers, TLS oraz identyfikacji adresu klienta. Nie można bez niej zakładać poprawnego limitowania i flagi Secure. Klucze lokalne i baza wymagają ograniczenia uprawnień katalogu, kopii zapasowych i mechanizmu rotacji.
