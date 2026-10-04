# Źródła danych i komponenty zewnętrzne

Nazwa produktu: MiastoBezBarier. Stan wykazu: 4 października 2026.

| Element | Użyta wersja / rola | Licencja / odnośnik |
| --- | --- | --- |
| Kod projektu | A + D + E | MIT — `LICENSE`; nie obejmuje danych OSM |
| .NET / ASP.NET Core | 10; runtime testowy 10.0.12 | MIT oraz informacje komponentów platformy: https://github.com/dotnet/runtime/blob/main/LICENSE.TXT i https://github.com/dotnet/aspnetcore/blob/main/LICENSE.txt |
| Microsoft.Data.Sqlite / Core | 10.0.12 | MIT, repozytorium producenta: https://github.com/dotnet/efcore/blob/main/LICENSE.txt |
| SQLitePCLRaw.bundle_e_sqlite3, core, lib.e_sqlite3, provider.e_sqlite3 | 2.1.12, zależności NuGet | SQLitePCLRaw: Apache-2.0; SQLite: public domain. https://github.com/ericsink/SQLitePCL.raw i https://sqlite.org/copyright.html |
| Leaflet | 1.9.4, dostarczony lokalnie | BSD-2-Clause; pełny tekst `src/AccessCity.Api/wwwroot/vendor/leaflet/LICENSE` |
| OpenStreetMap | Dane odczytane z publicznego Overpass | ODbL 1.0 — © OpenStreetMap contributors; https://www.openstreetmap.org/copyright |
| Overpass | Publiczny endpoint importu | https://overpass-api.de/api/interpreter ; limity operatora, małe obszary, manualny import i minimalna przerwa 10 min |
| Kafelki OSM | Podkład włączany przez użytkownika | Warunki usługi: https://operations.osmfoundation.org/policies/tiles/ ; brak masowego pobierania i trybu offline kafelków |

W źródłowej paczce nie redystrybuujemy bazy OSM, plików NuGet ani runtime .NET. Pakiety są pobierane przy restore i zachowują licencje i notices dostawców. Przy dystrybucji gotowych binariów należy zachować wymagane teksty licencyjne i informacje stron trzecich z konkretnego builda, w tym zależności natywne. Lista wersji wynika z użytego restore; aktualizacje mogą ją zmienić.

Dane demonstracyjne są fikcyjne i oznaczone. Obserwacje demonstracyjne nie potwierdzają rzeczywistych barier. Obecnie nie wykorzystujemy miejskich zbiorów Krakowa ani wewnętrznych systemów UMK/MJO. Nowy zbiór wymaga wskazania źródła, licencji i warunków pobierania przed integracją.

Licencja kodu MIT nie zastępuje obowiązków dotyczących danych ani usług mapowych. Przy publikowaniu bazy pochodnej OSM trzeba ocenić warunki ODbL. Informacje o licencjach sprawdzono w dokumentach właścicieli projektów; nie stanowią audytu całego przyszłego wdrożenia.

## Materiały zgłoszeniowe

Zrzuty i film przedstawiają lokalny prototyp na fikcyjnych danych DEMO. Bez zewnętrznych fotografii, nagrań, muzyki i prawdziwych danych użytkowników. Prezentacja i dokumentacja używają fontu DejaVu Sans (https://dejavu-fonts.github.io/License.html). Fonty zachowują własną licencję.
