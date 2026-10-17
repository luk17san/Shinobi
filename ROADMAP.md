# Shinobi — wykonane etapy i dalsze prace

Stan: 17 pazdziernika 2026. Projekt jest grywalnym prototypem 2.5D. Status „wykonany” oznacza mechanike prototypu, a nie gotowa oprawe, balans lub gre do wydania.

## Wykonane

| Etap | Zakres | Stan i granice |
| --- | --- | --- |
| 1 | TrainingArena, kamera z boku, ruch i HFSM | Ruch A/D, skok W, ladowanie, granice areny i reset. |
| 2 | Walka wrecz, obszary trafien i HP | Przygotowanie/aktywnosc/powrot ciosu; manekin z obrazeniami i treningowym odnowieniem HP. |
| 3 | Combo | Trzy osobne nacisniecia G, bufor kolejnego ciosu, timeout i przerwania. |
| 4 | Edytor walki, jutsu, chakra i zderzenia | J + WASD, koszt/przerwanie jutsu; poziomy, remis i szanse rozstrzygniecia zderzenia. Integracja z YBotem w scenie i prefabie. |
| 5 | Przykucanie i ekwipunek broni | S obniza collider i zwalnia ruch; ochrona przed wstaniem pod sufitem. Y przewija liste broni z osobnymi combo. Dwa wpisy treningowe; od etapu 9 maja proste modele testowe. |
| 6 | Automatyczny blok i wytrzymalosc | Blok frontalny na ziemi podczas gotowosci, zuzycie/odnowa wytrzymalosci, przelamanie gardy, blokady akcji, panel i przyciski testowe. |
| 7 | Szybki dash | Dwukrotne A/A lub D/D w oknie 0.25 s; 3 m w 0.15 s, koszt 20 wytrzymalosci i przerwa 0.7 s. Start z ziemi, kolizje, granice areny i przerwanie trafieniem. Parametry edytowalne. |
| 8 | Przeciwnik treningowy z AI | Podejscie, zwrot, ostrzezenie i pojedynczy cios. Wspolne trafienia, HP, blok i wytrzymalosc. Pauza/start/reset sparingu; osobny profil walki. |
| 10 | Ilustrowana arena treningowa | Cztery warstwy PNG uzytkownika, kamera 16:9, srodek do sparingu/dasha, interaktywny manekin drewniany, blok do skoku i belka do kucania. Warstwy i przeszkody edytowalne w scenie. |
| 11 | Reakcje wizualne i efekty walki | Trafienie i liczba obrazen, blok, przelamanie, luk ciosu, obszar jutsu, impuls dasha i efekt zderzenia. Edytowalny profil; brakujace klipy postaci nadal odlozone. |
| 12.1 | Menu i petla pojedynku | UI Toolkit: trening/pojedynek, pauza Esc, restart, powrot do menu, wygrana/przegrana/remis i rewanz. Trening zachowuje odnowienie HP; pojedynek konczy sie wynikiem. |
| 12.2 | Kompaktowy HUD | HP/chakra/stamina, aktualny cel, bron i Y, pieczecie/koszt/stany jutsu, koszt/gotowosc dasha. UI Toolkit, edytowalne elementy; zwijane cwiczenia zamiast stalego panelu testowego. |

Ostatnia weryfikacja: import i kompilacja Unity 6000.6.3f1 oraz 203 automatycznych kontroli na kopii aktualnej TrainingArena — wszystkie PASS. Potwierdzono animacje, mocowanie i zmiane modeli broni, AI oraz regresje walki i sterowania. Kontrole nie zastapuja manualnego testu sterowania w glownym edytorze, oceny tempa walki ani balansu.

## Proponowana kolejnosc nastepnych etapow

| Etap | Do utworzenia | Kryterium zakonczenia |
| --- | --- | --- |
| 9 — odlozone klipy | Animacje i wyglad broni | Edytowalny Animator Controller z 15 stanami i okno Shinobi > Edytor animacji z lista brakow; dziala Humanoid idle/chodzenie/bieg/skok/blok, przypisywanie klipow ciosow/jutsu i modele testowe broni w dloni. Do dokonczenia: docelowe klipy kucania, dasha, ciosow, trafienia, przelamania i jutsu oraz dopasowanie chwytu do animacji broni. |
| 12 — dalsza czesc | Cele i poziomy | Cele treningowe/poziomy i zasady progresji; menu, kompaktowy HUD i pojedynczy pojedynek lokalny sa juz gotowe jako prototyp. |
| 13 — pozniej | Pelny ekwipunek i zapis | Podnoszenie/przenoszenie przedmiotow, trwale identyfikatory broni, UI ekwipunku i zapis/odczyt postaci. Przesuniete na pozniejsze etapy zgodnie z decyzja uzytkownika. Obecna lista Weapons pozostaje konfiguracja treningowa. |
| 14 | Multiplayer | Ustalenie modelu sieciowego, autorytetu walki, synchronizacji ruchu/animacji/ekwipunku i testy opoznien. Obecna walka jest lokalna. |
| 15 | Balans i dopracowanie | Manualne testy, poprawki czytelnosci i tempa, optymalizacja oraz przygotowanie docelowej wersji. |

Kolejnosc zaktualizowano 17 pazdziernika 2026: dash dodano jako etap 7, lepsza arena zastepuje ekwipunek w punkcie 10, a pelny ekwipunek i zapis sa odlozone na pozniej. Etap wykonawczy zakonczony: 8. Etap 9 czesciowo odlozony do dostarczenia klipow; etap 10 wykonany jako prototyp. Etap 11 zakonczony jako prototyp efektow. Etap 12.1 zakonczony: menu i petla pojedynku. Etap 12.2 zakonczony: kompaktowy HUD. Dalej: cele i poziomy.

## Biezace sterowanie

- A/D: ruch; W: skok; przytrzymane S: kucanie.
- G: cios/combo; Y: nastepna bron z listy.
- Szybkie A -> A: dash w lewo; D -> D: dash w prawo (okno 0.25 s). Koszt 20 wytrzymalosci; dash nie daje nietykalnosci.
- Przytrzymaj J, nacisnij W, D, S i pusc J: Fala treningowa. Pieczecie: W/D/S/A odpowiadaja gora/prawo/dol/lewo.
- R: pozycja, chakra, wytrzymalosc, usuniecie przelamania gardy i reset dasha/cooldownu; HP odnawiaja sie treningowo dopiero po osiagnieciu 0.
- Blok dziala automatycznie z przodu, na ziemi, poza atakiem/jutsu/dashem/przelamaniem. Nie wymaga dodatkowego przycisku.

## Test aktualnego etapu w Unity

Assets > Refresh, poczekaj na kompilacje, ponownie otworz Assets/Shinobi/Scenes/TrainingArena.unity i wlacz Play.

Panel pokazuje HP/chakre/wytrzymalosc/bron. Opcja „Sterowanie i stan” rozwija szczegoly. Przyciski testowe „Cios z przodu”, „Cios z tylu”, „Jutsu” podaja po 25 obrazen do systemu obrony. Sa symulacja testowa; TrainingDummy pozostaje manekinem. TrainingOpponent to osobny rywal AI: przycisk Rozpocznij sparing / Pauza sparingu oraz Reset sparingu. Rywal startuje w pauzie.

Ustawienia obrony: Player > Shinobi Combat > Profile albo Shinobi > Edytor walki > Obrona. Szczegoly i aktualne wartosci znajduja sie w najnowszej sekcji STATUS.md.













## 2026-10-17 — nowe akcje i edytor
Ukończono prototyp ataków w kucki/powietrzu, wślizgu, dalekiego skoku z silnym ciosem oraz rzutów w kucki/locie. Edytor Unity: Shinobi > Edytor walki > Combo/Jutsu. Aktualna lista i plany: COMBO_I_JUTSU.md. Kolejne prace: docelowe animacje, ręczny balans i cele treningowe. Nowe typy jutsu pozostają propozycjami.

