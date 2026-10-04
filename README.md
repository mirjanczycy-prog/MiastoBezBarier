# MiastoBezBarier — Kraków bez barier

Działający prototyp **A + D + E** w C#: wyszukiwanie miejsc z konkretnymi barierami, zgłoszenia społeczności oraz API i widget dla partnerów. Grupa docelowa: osoby na wózkach, rodzice z wózkami oraz osoby unikające schodów i innych barier.

Nazwa produktu to **MiastoBezBarier**. Techniczne nazwy solution i projektów pozostają `AccessCity`, aby zachować zgodność z działającym projektem.

## Materiały zgłoszeniowe

- [Prezentacja PDF — 10 slajdów](submission/MiastoBezBarier_prezentacja.pdf)
- [Film demonstracyjny — 2 min 30 s, polskie napisy](submission/MiastoBezBarier_demo.mp4)
- [Opis techniczny i biznesowy](submission/MiastoBezBarier_opis.pdf)
- [Prezentacja edytowalna](submission/MiastoBezBarier_prezentacja.pptx)
- [Publikacja bez wtyczki i pozostałe wymagania](docs/SUBMISSION.md)
- [Tekstowa alternatywa filmu](submission/MiastoBezBarier_film_opis.txt)

Film wymaga opublikowania w publicznym repozytorium i sprawdzenia dostępu bez logowania. Test NVDA/VoiceOver pozostaje do wykonania; nie deklarujemy pełnej zgodności AA.

## Uruchomienie w 2 minuty

1. Zainstaluj **.NET 10 SDK** (nie tylko Runtime): https://dotnet.microsoft.com/download/dotnet/10.0.
2. Rozpakuj paczkę i otwórz terminal w katalogu projektu (w paczce zgłoszeniowej `MiastoBezBarier`).
3. Uruchom:

```bash
dotnet run --project src/AccessCity.Api
```

4. Otwórz **http://localhost:5080**.

Pierwszy start pobierze pakiety NuGet, utworzy lokalną bazę SQLite i doda cztery fikcyjne miejsca. Nie potrzebujesz SQL Servera, konta Azure, klucza map ani MAUI. Dane demo i zgłoszenia działają bez internetu po pobraniu zależności. Podkład ulic OSM jest opcjonalny i wymaga połączenia.

Windows: możesz użyć `start.ps1` z PowerShell. IDE: otwórz `AccessCity.sln` w Visual Studio obsługującym .NET 10, ustaw `AccessCity.Api` jako projekt startowy i użyj profilu `AccessCity`. Z terminala rozwiązanie działa również na Linux/macOS.

## Co jest gotowe

| Warstwa | Funkcje |
| --- | --- |
| A — miejsca | Wyszukiwanie po nazwie/adresie, kategorie, preferencje barier i wymiarów, lista tekstowa, mapa współrzędnych i opcjonalny podkład OSM |
| A — informacje | Osobne cechy: wejście, podjazd, szerokość, próg, istnienie i działanie windy, toaleta, nawierzchnia, odpoczynek, oznaczenie wheelchair; źródło, data edycji i pobrania, status i konflikt |
| D — społeczność | Dodawanie obserwacji, osobne dane źródłowe i zgłoszenia, potwierdzenia innych sesji, blokada własnych i powtórnych potwierdzeń, wygasanie, historia zgłoszeń |
| E — platforma | REST API, OpenAPI, działający podgląd JSON, widget iframe, strona integracyjna dla partnerów |
| Infrastruktura | SQLite, transakcje i unikalność potwierdzeń, adapter Overpass, limity zapisów/importu, walidacja, CSRF, chronione cookie sesji |

Routing B oraz osobny „paszport dostępności” / certyfikacja obiektu C nie są częścią projektu. Szczegóły barier przy miejscu należą do podstawowego scenariusza A.

## Demo na hackathon

Pełny scenariusz i gotowa narracja: [docs/DEMO.md](docs/DEMO.md).

1. Wybierz **Hotel Przystań — DEMO**.
2. Zaznacz wejście bez schodów, windę i minimalną szerokość 90 cm; zastosuj preferencje.
3. Rozwiń źródło konkretnej cechy. Zobacz etykietę DEMO, datę edycji oraz pobrania.
4. Dodaj obserwację **Działająca winda → Nie**.
5. Zobacz konflikt: dane demo mówią „Tak”, nowa obserwacja „Nie”. Istnienie windy pozostaje osobną cechą.
6. W dwóch innych, odrębnych profilach przeglądarki potwierdź obserwację. Odśwież szczegóły. Potwierdzenia liczą sesje, nie zweryfikowane osoby.
7. Otwórz **Dla partnerów**. Zobacz ten sam konflikt w JSON i w widżecie.
8. Galeria Otwarta demonstruje braki danych; Biblioteka Sąsiedzka — nieaktualność.

Kolejne okna prywatne tej samej przeglądarki mogą współdzielić cookies. Użyj np. zwykłego Chrome, Firefox i Edge albo trzech odrębnych profili. Autor nie może potwierdzić własnej obserwacji.

## Rzeczywiste dane OpenStreetMap

Kliknij **Pobierz miejsca z OSM**. Import obejmuje mały obszar centralnego Krakowa, maksymalnie 300 nazwanych miejsc. Potem wybierz filtr **Rzeczywiste miejsca OSM**.

Nie dodajemy udawanych danych OSM. Brak tagów oznacza brak informacji. `wheelchair=yes` nie jest automatycznie przeliczane na brak schodów, pomiary drzwi lub sprawną windę. Wiele miejsc ma niepełne dane; właśnie dlatego istnieje warstwa społeczności.

Konfiguracja obszaru i endpointu: `src/AccessCity.Api/appsettings.json`. Aby dodać miasto, zmień `City:Name` i współrzędne. Identyfikatory OSM są globalnie stabilne. Prototyp przechowuje wiele miast w jednym modelu; interfejs mapy początkowo jest ustawiony na Kraków i trzeba go dopasować przy zmianie miasta.

Import jest ręczny, z przerwą minimum 10 minut. Błąd dostawcy lub niepełna odpowiedź nie usuwa zapisanej kopii. Po udanym imporcie zaktualizowane są cechy importowanych miejsc; społeczność pozostaje niezależna. Miejsca nieobecne w ograniczonym wyniku nie są automatycznie usuwane. To ograniczenie trzeba rozwiązać przed pełną synchronizacją miasta.

Źródła, licencje i mapowanie: [docs/DATA.md](docs/DATA.md).

## Technologie i struktura

- **Backend i logika:** C#, .NET 10, ASP.NET Core Minimal API.
- **Trwałość:** Microsoft.Data.Sqlite, lokalny plik SQLite.
- **Interfejs webowy:** HTML, CSS, niewielkie moduły JavaScript oraz Leaflet 1.9.4 zapisany w projekcie. Cała ocena barier i dopasowania jest w C#; interfejs odczytuje API. Frontend nie jest Blazorem ani aplikacją MAUI.
- **Testy:** samodzielny runner w C#, bez dodatkowego frameworka testowego.

| Projekt | Odpowiedzialność |
| --- | --- |
| `AccessCity.Core` | Modele, katalog cech, `AccessibilityEngine`, reguły dopasowania, interfejsy repozytorium i dostawcy |
| `AccessCity.Infrastructure` | SQLite, transakcje, dane demo, historia społeczności |
| `AccessCity.DataProviders` | Import i ostrożne mapowanie tagów OpenStreetMap |
| `AccessCity.Api` | Endpointy, bezpieczeństwo, konfiguracja, hosting plików webowych |
| `AccessCity.Tests` | Testy reguł, danych, bazy i opcjonalne testy HTTP |

Architektura i rozbudowa: [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md). Model biznesowy i utrzymanie: [docs/PRODUCT.md](docs/PRODUCT.md).

## API

```http
GET /api/places?q=hotel&demo=true
GET /api/search?stepFree=true&minDoorWidthCm=90
GET /api/places/demo-hotel/accessibility?elevator=true
GET /api/places/demo-hotel/reports
POST /api/places/demo-hotel/reports
POST /api/reports/{id}/confirm
GET /api/providers/status
```

Specyfikacja: **http://localhost:5080/openapi.json**. Podgląd integracji: **http://localhost:5080/developers.html**.

Zapisy wymagają cookies i tokenu CSRF: pobierz `GET /api/session`, zachowaj cookies i wyślij `csrfToken` w nagłówku `X-CSRF-TOKEN`. Treść nowej obserwacji:

```json
{
  "feature": "elevatorOperational",
  "value": "false",
  "comment": "Winda nie uruchamia się; informacja od obsługi."
}
```

To prototyp z publicznym API odczytu. Nie ma jeszcze abonamentów, kluczy klientów B2B, panelu moderatora ani weryfikacji właścicieli obiektów. Nie są udawane w interfejsie.

## Testy

```bash
dotnet build AccessCity.sln -c Release
dotnet run --project tests/AccessCity.Tests -c Release
```

Runner zakończy się kodem różnym od zera, jeżeli którykolwiek test nie przejdzie. Testy tworzą i usuwają własną tymczasową bazę. To program testowy: użyj `dotnet run`, nie `dotnet test`.

Aby sprawdzić rzeczywiste endpointy HTTP, uruchom aplikację w osobnym terminalu, następnie:

```bash
dotnet run --project tests/AccessCity.Tests -c Release -- http://localhost:5080
```

Testy HTTP dodają obserwację demonstracyjną do uruchomionej instancji. Wyniki weryfikacji i zakres kontroli UI: [docs/VALIDATION.md](docs/VALIDATION.md).

## Dane lokalne i reset demo

Domyślnie baza znajduje się w `src/AccessCity.Api/App_Data/accesscity.db`; cookies są chronione kluczami w `App_Data/keys`. Ścieżkę zmienisz przez `Storage:Directory` lub zmienną `Storage__Directory`.

**Reset usuwa zgłoszenia i importy:** zatrzymaj aplikację, usuń jej katalog `App_Data`, następnie uruchom ponownie. Nie dołączamy bazy z testów do paczki — Twój pierwszy start jest czysty.

## Docker

```bash
docker compose up --build
```

Otwórz http://localhost:5080. Dane przetrwają restart kontenera w wolumenie. Compose uruchamia **Development**, przeznaczony do lokalnego demo. Test uruchomienia Docker wymaga Dockera; standardowy start przez `dotnet run` jest wystarczający.

## Przejście do wdrożenia

Publikacja kodu:

```bash
dotnet publish src/AccessCity.Api -c Release -o publish
```

Przed wdrożeniem publicznym: HTTPS, trwały katalog danych z ograniczonymi uprawnieniami i kopią zapasową, szyfrowanie kluczy Data Protection, moderacja/obsługa usunięć, ochrona przed tworzeniem wielu anonimowych sesji, audyt dostępności i testy z odbiorcami. Ustaw `Demo__Seed=false` przy pierwszym starcie produkcyjnym. Nie usuwa to wcześniej zapisanych danych demo.

Import administracyjny poza Development wymaga `Admin__ApiKey` i nagłówka `X-Admin-Key`, a także standardowego CSRF. Nie wpisuj klucza do repozytorium. Przy reverse proxy skonfiguruj zaufane proxy/forwarded headers i zakończenie TLS, żeby poprawnie obsługiwać HTTPS, cookies oraz adresy klientów. Bez tej konfiguracji nie traktuj plików Docker jako gotowego wdrożenia produkcyjnego.

Projekt odpowiada za prototyp aplikacji. Prezentacja PDF i film wymagane przez brief organizatora nie są wygenerowane w tej paczce; w `docs` jest opis produktu oraz scenariusz nagrania.
