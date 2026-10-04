# Zgłoszenie MiastoBezBarier — publikacja bez wtyczki

## Co jest przygotowane

- Działający prototyp A + D + E. B (trasy) i C (paszport) poza zakresem.
- Prezentacja PDF — dokładnie 10 slajdów; edytowalny PPTX.
- Film MP4 z rzeczywistym działaniem aplikacji, polskimi napisami, bez dźwięku — 2:30,12.
- Opis problemu, odbiorców, źródeł, reguł wiarygodności, architektury, modelu biznesowego, utrzymania, prywatności i rozwoju.
- Dowody walidacji oraz tekstowa alternatywa nagrania.

## Dwa wymagania do zamknięcia przed wysłaniem

1. Wykonać główny scenariusz z faktycznym czytnikiem ekranu NVDA albo VoiceOver. Protokół jest na stronie 6 pliku `submission/MiastoBezBarier_opis.pdf`. Uzupełnić wynik w `docs/VALIDATION.md`; przy stwierdzonych problemach opisać poprawkę i retest. Pełny audyt AA nie jest zaliczony.
2. Opublikować film w publicznym, dostępnym repozytorium i sprawdzić link bez logowania. Paczka i lokalny plik nie są opublikowanym repozytorium.

## GitHub przez stronę WWW

1. Rozpakuj ZIP. Otwórz katalog `MiastoBezBarier` — to jego zawartość będzie głównym katalogiem repozytorium. Nie dodawaj ZIP jako jedynego pliku.
2. Zaloguj się na https://github.com i otwórz https://github.com/new . Nazwa: `MiastoBezBarier`; opis: `Prototyp C# na wyzwanie Kraków bez barier: miejsca, społeczność, API i widget.`
3. Wybierz **Public**. Utwórz repozytorium. W paczce jest już README i LICENSE, więc dodatkowych plików startowych nie trzeba generować.
4. Na ekranie pustego repozytorium wybierz link **uploading an existing file**. W istniejącym repozytorium: **Add file → Upload files**.
5. Przeciągnij pliki i podkatalogi z rozpakowanego katalogu `MiastoBezBarier`. Główny README ma znaleźć się w katalogu głównym repozytorium, a film w `submission/MiastoBezBarier_demo.mp4`. Wpisz opis commita `Dodaj prototyp i materiały zgłoszeniowe`, zapisz zmiany.
6. Otwórz README i link filmu. Sprawdź możliwość pobrania lub odtworzenia filmu, PDF oraz opisu.
7. W oknie prywatnym, bez logowania, sprawdź repozytorium i film. Zapisz rzeczywisty publiczny adres — nie adres lokalny `localhost`, nie link edycji i nie link do pliku na dysku.
8. W formularzu organizatora dodaj prezentację PDF i publiczny adres filmu / repozytorium, zgodnie z polami formularza. Zgłoszenie nie zostało wysłane automatycznie.

Film ma około 3,7 MiB, więc mieści się w limicie 25 MiB na pojedynczy plik wgrywany przez przeglądarkę. Paczka ma mniej niż 100 plików. Instrukcja dostawcy sprawdzona 4.10.2026: https://docs.github.com/en/repositories/working-with-files/managing-files/adding-a-file-to-a-repository . Jeśli repozytorium ma blokady gałęzi lub ograniczenia organizacji, użyj dozwolonego konta/repozytorium i ścieżki publikacji.

Nie wgrywaj swojej lokalnej bazy, kluczy, danych zgłoszeń ani plików bin/obj po uruchomieniu projektu. Przygotowany ZIP zawiera wyłącznie źródła i materiały demonstracyjne. Stały hosting aplikacji po hackathonie nie jest wymagany przez brief. Publiczny film jest wymagany.

## Po teście czytnika i publikacji

Uzupełnij rzeczywisty wynik i datę, link repozytorium oraz osobę odpowiedzialną za zgłoszenie. W edytowalnej prezentacji zaktualizuj slajd 9 dopiero po wykonaniu testu; zachowaj maksymalnie 10 slajdów przy eksporcie PDF. Wynik pozytywny scenariusza NVDA nie uprawnia do deklaracji pełnej zgodności WCAG AA.
