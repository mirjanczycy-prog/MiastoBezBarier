# Produkt, biznes i utrzymanie

## Problem i odbiorcy

Ogólna etykieta „dostępne” nie odpowiada na pytania: czy zmieszczę się w wejściu, czy jest próg, czy winda dziś działa? MiastoBezBarier pokazuje konkretne informacje, ich źródła i braki. Osoby na wózkach i rodzice z wózkami mogą określić wymagania wobec otoczenia, bez ujawniania diagnozy.

Warstwa użytkownika jest bezpłatna. Społeczność pomaga wychwytywać zmiany, ale potwierdzenia sesji pozostają sygnałem, nie audytem.

## Propozycja komercjalizacji

| Segment | Wartość | Możliwy model |
| --- | --- | --- |
| Hotele i restauracje | Informacje na stronie obiektu, redukcja ręcznego odpowiadania | Abonament za widget, aktualizacje i integrację |
| Organizatorzy wydarzeń | Szczegółowe informacje o obiekcie i tymczasowych problemach | Pakiet wydarzenia i usługa utrzymania danych |
| Portale rezerwacyjne i turystyczne | Wspólne dane o barierach w ich produktach | API według wykorzystania, licencji lub zakresu miast |
| Zarządcy obiektów | Kontrolowane korekty i kontakt ze społecznością | Panel operatora i proces potwierdzania z oględzinami |

Abonamenty i panel operatora są planem, nie zaimplementowanymi funkcjami. Certyfikacja/paszport obiektu C nie należy do tego MVP. Nie proponujemy płatnego „kupowania wiarygodności”: opłata za usługę nie zastępuje weryfikacji danych.

## Odpowiedzialność poza UMK

Operator produktu (zespół, spółka lub partner pilotażu) odpowiada za hosting, aktualizacje, źródła, bezpieczeństwo, moderację, obsługę próśb o usunięcie danych i koszty. Miasto może być partnerem danych/pilotażu, lecz aplikacja nie potrzebuje jego wewnętrznych systemów.

Na pilotaż: jedna instancja aplikacji, trwały dysk, TLS, kopie zapasowe i monitoring; później baza zarządzana i osobny worker importu. Koszt infrastruktury dla małego pilotażu planujemy orientacyjnie na 100–400 PLN/miesiąc; to założenie budżetowe, nie sprawdzona oferta dostawcy. Należy oddzielnie doliczyć moderację, oględziny, wsparcie, usługi mapowe i pracę programistów. Cennik wymaga rozmów z odbiorcami.

## Etapy po hackathonie

1. Walidacja z osobami na wózkach i rodzicami; sprawdzenie katalogu cech, komunikatów i progów ważności.
2. Moderacja i procedura korygowania konfliktów, ochrony przed nadużyciami i usuwania danych.
3. Pilotaż z kilkoma hotelami/wydarzeniami, pełniejszy model wejść i potwierdzanie stanu na miejscu.
4. Harmonogram odświeżania OSM z backoff, pełna synchronizacja, archiwum źródeł i monitoring jakości.
5. Klucze klientów API, limity abonamentów, kontrola dostępu partnerów i rachunkowość wykorzystania.
6. Audyt WCAG 2.2 AA, testy czytnikami ekranu i testy terenowe.
7. Drugie miasto: uzgodnione źródła/licencje, konfiguracja obszaru, kategorie, partner danych i lokalni użytkownicy.

Sukces pilotażu można mierzyć liczbą uzupełnionych braków, czasem wykrycia awarii, odsetkiem konfliktów rozwiązanych po sprawdzeniu i liczbą użytkowników, którzy znaleźli potrzebną informację. Liczba potwierdzeń sesji sama w sobie nie mierzy prawdziwości danych.
