# Źródła i reguły wiarygodności

## Pochodzenie

| Źródło | Co pobieramy | Aktualizacja | Status |
| --- | --- | --- | --- |
| Demo | Fikcyjne miejsca i cechy scenariusza | Przy pierwszym uruchomieniu; daty względne | Dane demonstracyjne; osobne etykiety w aplikacji i API |
| OpenStreetMap / Overpass | Nazwane miejsca z wybranych kategorii i tagi cech | Ręczny import z konfiguracji, przerwa min. 10 min | Dane społecznościowe bez potwierdzonych oględzin |
| Społeczność MiastoBezBarier | Wartość jednej cechy, opis i czas | Natychmiast po zapisie | Niezweryfikowane; po dwóch innych sesjach potwierdzone w sesjach |

OSM: https://www.openstreetmap.org/copyright (ODbL 1.0). Endpoint: https://overpass-api.de/api/interpreter. API zapytania: https://wiki.openstreetmap.org/wiki/Overpass_API/Overpass_QL. Produkcyjny importer powinien używać własnego kontaktowego User-Agent, respektować limity operatora i ograniczać rozmiar obszaru.

Podkład OSM: https://operations.osmfoundation.org/policies/tiles/. Domyślnie jest wyłączony, nie ma pobierania kafelków offline ani masowego prefetchu. Leaflet jest dostarczony lokalnie na BSD-2-Clause. Licencja w `wwwroot/vendor/leaflet/LICENSE`. W produkcji uwzględnij wybranego dostawcę map i jego warunki. Publikowanie lub redystrybucja bazy pochodnej OSM wymaga analizy obowiązków ODbL; nie zmieniamy licencji danych przez MIT kodu.

## Ostrożne mapowanie

| Tag | Cecha API | Reguła |
| --- | --- | --- |
| `wheelchair=yes/no/limited` | `wheelchairAccess` | Wyłącznie ogólne oznaczenie źródłowe; nie wyprowadza pozostałych cech |
| `ramp:wheelchair=yes/no` | `ramp` | Tylko jawne yes/no |
| `elevator=yes/no` | `elevator` | Istnienie; nie stan działania |
| `toilets:wheelchair=yes/no` | `accessibleToilet` | Tylko jawne yes/no |
| `entrance:step_count` | `stepFreeEntrance` | 0 → tak, dodatnia liczba → nie; tylko jeśli tag znajduje się bezpośrednio na importowanym miejscu |
| `entrance:width` | `doorWidthCm` | Jawna wartość w metrach, przeliczenie na cm; obsługiwany zakres >0 do 5 m |
| `entrance:surface` | `surface` | Tylko wspierane wartości katalogu |

Tagi `entrance:*` są wspieranym dodatkowym formatem importu i mogą występować rzadko; importer nie twierdzi, że wszystkie obiekty go używają. Nie analizuje jeszcze osobnych węzłów wejść, relacji budynku ani topologii wnętrza. Ogólne `width` i `surface` obiektu nie oznaczają automatycznie szerokości drzwi i nawierzchni wejścia. Próg, miejsce odpoczynku i działanie windy pochodzą w tym MVP ze zgłoszeń albo demo, bez wymyślania wartości z OSM.

## Czas i konflikt

- `UpdatedAt`: czas edycji elementu OSM lub utworzenia zgłoszenia. Edycja elementu nie dowodzi oględzin danej cechy.
- `RetrievedAt`: czas pobrania danych. Nie zmienia wieku edycji źródła.
- `ExpiresAt`: termin ważności zgłoszenia. Działanie windy: 48 godzin; pozostałe cechy: 30 dni.
- Dane bazowe bez daty albo z edycją starszą niż 180 dni są nieaktualne/bez daty.
- Różne aktywne wartości tej samej cechy dają `conflict`, nawet gdy jedna jest potwierdzona przez społeczność. Wynik `value=null`; komplet dowodów jest dostępny w API i UI.
- Zgodność z preferencjami oznacza wyłącznie wskazanie na podstawie bieżących danych. Brak, konflikt, stara informacja oraz niepotwierdzone dodatnie zgłoszenie dają niewiadomą.
- Znany negatywny sygnał jest wskazaną barierą. Nie jest automatycznie formalnie zweryfikowany.

Progi i okresy ważności są prostymi regułami hackathonowymi; trzeba je ustalić z użytkownikami i właścicielami źródeł podczas pilotażu. Nie ma procentowego „wyniku dostępności”.

## Korekty i synchronizacja

Użytkownik dodaje nową obserwację. Stare zgłoszenie wygasa i pozostaje w historii; nie nadpisujemy go. Nierozwiązany konflikt bazowy należy sprawdzić w źródle albo podczas oględzin; w prototypie nie ma panelu moderatora ani ręcznego statusu „zweryfikowane”.

Udany import zastępuje bieżący zestaw cech OSM dla pobranych miejsc (również usuwa nieobecne już tagi). Zgłoszenia zachowują niezależność. Nie archiwizujemy wszystkich poprzednich snapshotów OSM — provenance bieżącej kopii i historia społeczności to odrębne zakresy. Niekompletna odpowiedź z `remark`, błąd HTTP lub przekroczony czas odrzucają cały import.

Nieobecne w odpowiedzi miejsca pozostają w lokalnej kopii; przestarzałe daty są nadal widoczne. Przed pełnym wdrożeniem potrzebne są kompletne importy obszarów, obsługa usuniętych obiektów, wersjonowanie snapshotów i regularny scheduler z retry/backoff. Import ma limit 300 miejsc i 8 MB odpowiedzi; nie jest synchronizacją całego Krakowa.
