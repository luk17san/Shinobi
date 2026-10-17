# Combo i jutsu

Stan dokumentu: 17 października 2026. Opis dotyczy prototypu i konfiguracji Unity.

## Sterowanie i dostępne sekwencje

Domyślne klawisze; po zmianie profilu obowiązują klawisze z eksportu poniżej. Każdy cios i rzut wymaga nowego naciśnięcia. Przytrzymanie G nie wykonuje automatycznej serii.

| Akcja / combo | Wejście | Warunki i działanie |
| --- | --- | --- |
| Podstawowa seria | G → G → G | Na ziemi; kolejne naciśnięcie w oknie kontynuacji (domyślnie 0,5 s od początku ciosu). Parametry zależą od broni. |
| Niski cios | S + G | Kucanie; postać zachowuje niski collider do końca ciosu. Można ponowić po recovery. |
| Cios powietrzny | W, następnie G | Zwykły skok lub spadanie; kolejne ciosy po recovery. Lądowanie przerywa cios. |
| Dash → wślizg | A,A lub D,D, następnie nowe S podczas dasha | Wślizg 2 m / 0,35 s, bez obrażeń i nietykalności. Pod sufitem postać pozostaje przykucnięta. |
| Dash → daleki skok | A,A lub D,D, następnie W podczas dasha | Około 6 m na równym podłożu, stały kierunek; kolizje ograniczają dystans. |
| Dash → daleki skok → silny cios | A,A lub D,D → W → G | Silny cios i odepchnięcie 1,5 m; zachowany tor lotu. Kolejny cios możliwy po recovery, jeśli postać nadal leci. |
| Rzut stojąc | H | Wymaga broni rzutnej i zakończonego cooldownu. |
| Rzut w kucki | S + H | Obniżona pozycja wypuszczenia; kucanie zachowane podczas rzutu na ziemi. |
| Rzut powietrzny | W → H lub dash → W → H | Zachowany ruch i grawitacja. |
| Fala treningowa | Przytrzymaj J, osobno W → D → S, puść J | 20 chakry, 30 obrażeń, obszar przed postacią; nie jest pociskiem. |

Wślizg i daleki skok nie dopłacają wytrzymałości do kosztu dasha (20). Wślizg blokuje cios, rzut i kolejny skok. Jednoczesne W/S podczas dasha wybiera wślizg; G/H wybiera cios.

Rzut celuje w aktualny cel HUD w momencie wypuszczenia broni: rywala podczas pojedynku/sparingu, poza nim manekina. Kierunek jest wyznaczony do środka collidera celu w zasięgu; bez żywego celu broń leci poziomo zgodnie ze zwrotem postaci. Pocisk nie śledzi dalszego ruchu celu.

## Edycja w Unity

1. Otwórz **Shinobi > Edytor walki** i wybierz profil (np. TrainingCombat).
2. W **Combo** wybierz profil lub broń oraz kontekst: Standing (stanie), Crouching (kucanie), Airborne (lot), LongJump (daleki skok).
3. Kliknij **Utwórz combo**, nadaj nazwę i dodaj ciosy. Edytuj fazy, obrażenia, obszar, odepchnięcie, animację i efekt. Zmieniaj kolejność strzałkami; istniejące ciosy można przypisać w tablicy Attacks.
4. Kliknij **Przypisz wybrane**. Puste warianty broni dziedziczą konfigurację profilu. Duplikat combo ma osobne kopie ciosów; odłączenie nie kasuje współdzielonych assetów.
5. W **Jutsu** kliknij **Utwórz jutsu**, ustaw nazwę, pieczęcie, koszt i działanie obszarowe, a następnie **Dodaj do profilu**. Powtórzone sekwencje w jednym profilu są błędem.
6. Zapisz konfigurację. Animacje można przypisać także przez **Shinobi > Edytor animacji**. Edycja definicji jest zablokowana w Play Mode.
7. **Eksportuj listę do Markdown** odświeża tylko fragment pomiędzy znacznikami GENERATED, zachowując instrukcje i plany poza nimi.

Przy przeniesieniu samych skryptów do istniejącego projektu uruchom raz **Shinobi > Combo i jutsu > Przygotuj warianty i migruj combo**. Polecenie zachowuje stare tablice, istniejące definicje i przypisania oraz dodaje brakujące warianty.

<!-- GENERATED:BEGIN -->
## Dostępne konfiguracje

Eksport: 2026-10-17. Status: **do weryfikacji w głównym edytorze**; lista opisuje zapisane assety, nie wynik testów.

### Profil: TrainingCombat

Sterowanie: cios **G**, skok **W**, kucanie **S**, rzut **H**, Focus **J**, broń **Y**.

| Kontekst | Combo / wejście | Kolejność obrażeń | Fazy ciosów: przygotowanie / aktywność / powrót | Okno | Braki animacji |
| --- | --- | --- | --- | --- | --- |
| Standing | TrainingCombat stojac / G → G → G | 20 → 25 → 35 | 0.12 / 0.08 / 0.25; 0.1 / 0.08 / 0.22; 0.18 / 0.08 / 0.4 | 0.5 s | 3 |
| Crouching | TrainingCombat Niski cios / G | 20 | 0.12 / 0.08 / 0.25 | 0.5 s | 1 |
| Airborne | TrainingCombat Cios powietrzny / G | 20 | 0.12 / 0.08 / 0.25 | 0.5 s | 1 |
| LongJump | TrainingCombat Silny cios w dalekim skoku / G | 35 | 0.18 / 0.08 / 0.4 | 0.5 s | 1 |

| Jutsu | Pieczęcie podczas Focus | Chakra | Obrażenia | Przygotowanie / powrót | Promień / przesunięcie |
| --- | --- | --- | --- | --- | --- |
| Fala treningowa | W → D → S | 20 | 30 | 0.3 / 0.3 s | 2 / 2 m |

### Profil: TrainingOpponentCombat

Sterowanie: cios **G**, skok **W**, kucanie **S**, rzut **H**, Focus **J**, broń **Y**.

| Kontekst | Combo / wejście | Kolejność obrażeń | Fazy ciosów: przygotowanie / aktywność / powrót | Okno | Braki animacji |
| --- | --- | --- | --- | --- | --- |
| Standing | TrainingOpponentCombat stojac / G | 20 | 0.12 / 0.08 / 0.25 | 0.5 s | 1 |
| Crouching | TrainingOpponentCombat Niski cios / G | 20 | 0.12 / 0.08 / 0.25 | 0.5 s | 1 |
| Airborne | TrainingOpponentCombat Cios powietrzny / G | 20 | 0.12 / 0.08 / 0.25 | 0.5 s | 1 |
| LongJump | TrainingOpponentCombat Silny cios w dalekim skoku / G | 20 | 0.12 / 0.08 / 0.25 | 0.5 s | 1 |

| Jutsu | Pieczęcie podczas Focus | Chakra | Obrażenia | Przygotowanie / powrót | Promień / przesunięcie |
| --- | --- | --- | --- | --- | --- |
| Fala treningowa | W → D → S | 20 | 30 | 0.3 / 0.3 s | 2 / 2 m |

### Broń: Kunai

| Kontekst | Combo / wejście | Kolejność obrażeń | Fazy ciosów: przygotowanie / aktywność / powrót | Okno | Braki animacji |
| --- | --- | --- | --- | --- | --- |
| Standing | Kunai stojac / przycisk ciosu profilu → przycisk ciosu profilu → przycisk ciosu profilu | 20 → 25 → 35 | 0.12 / 0.08 / 0.25; 0.1 / 0.08 / 0.22; 0.18 / 0.08 / 0.4 | 0.5 s | 3 |
| Crouching | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| Airborne | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| LongJump | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |

Rzut stojąc / w kucki / w powietrzu: 25 obrażeń, cooldown 0.7 s, zasięg 15 m. Celowanie w chwili wypuszczenia; lot prosty.

### Broń: Kunai z wybuchową notką

| Kontekst | Combo / wejście | Kolejność obrażeń | Fazy ciosów: przygotowanie / aktywność / powrót | Okno | Braki animacji |
| --- | --- | --- | --- | --- | --- |
| Standing | Kunai_Seal stojac / przycisk ciosu profilu → przycisk ciosu profilu → przycisk ciosu profilu | 20 → 25 → 35 | 0.12 / 0.08 / 0.25; 0.1 / 0.08 / 0.22; 0.18 / 0.08 / 0.4 | 0.5 s | 3 |
| Crouching | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| Airborne | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| LongJump | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |

Rzut stojąc / w kucki / w powietrzu: 25 obrażeń, cooldown 1.1 s, zasięg 15 m. Celowanie w chwili wypuszczenia; lot prosty.

### Broń: Shuriken

| Kontekst | Combo / wejście | Kolejność obrażeń | Fazy ciosów: przygotowanie / aktywność / powrót | Okno | Braki animacji |
| --- | --- | --- | --- | --- | --- |
| Standing | Shuriken stojac / przycisk ciosu profilu → przycisk ciosu profilu → przycisk ciosu profilu | 20 → 25 → 35 | 0.12 / 0.08 / 0.25; 0.1 / 0.08 / 0.22; 0.18 / 0.08 / 0.4 | 0.5 s | 3 |
| Crouching | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| Airborne | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| LongJump | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |

Rzut stojąc / w kucki / w powietrzu: 20 obrażeń, cooldown 0.7 s, zasięg 15 m. Celowanie w chwili wypuszczenia; lot prosty.

### Broń: Miecz treningowy

| Kontekst | Combo / wejście | Kolejność obrażeń | Fazy ciosów: przygotowanie / aktywność / powrót | Okno | Braki animacji |
| --- | --- | --- | --- | --- | --- |
| Standing | TrainingWeapon1 stojac / przycisk ciosu profilu → przycisk ciosu profilu → przycisk ciosu profilu | 20 → 25 → 35 | 0.12 / 0.08 / 0.25; 0.1 / 0.08 / 0.22; 0.18 / 0.08 / 0.4 | 0.5 s | 3 |
| Crouching | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| Airborne | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| LongJump | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |

Rzut: niedostępny.

### Broń: Kij treningowy

| Kontekst | Combo / wejście | Kolejność obrażeń | Fazy ciosów: przygotowanie / aktywność / powrót | Okno | Braki animacji |
| --- | --- | --- | --- | --- | --- |
| Standing | TrainingWeapon2 stojac / przycisk ciosu profilu → przycisk ciosu profilu → przycisk ciosu profilu | 25 → 30 → 40 | 0.12 / 0.08 / 0.25; 0.1 / 0.08 / 0.22; 0.18 / 0.08 / 0.4 | 0.5 s | 3 |
| Crouching | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| Airborne | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |
| LongJump | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |

Rzut: niedostępny.

<!-- GENERATED:END -->

## Planowane

Poniższe propozycje **nie są wykonywalnymi jutsu** i nie mają przypisanych pieczęci ani kosztów.

| Jutsu do stworzenia | Zamierzone działanie | Potrzebna praca |
| --- | --- | --- |
| Kula ognia | Pocisk lecący przed postacią | Nowy rodzaj wykonania jutsu, kolizje, efekty i balans. |
| Podmuch wiatru | Obszarowe odepchnięcie przeciwnika | Obsługa odepchnięcia w technikach, efekt i balans. |
| Kamienna osłona | Czasowa ochrona postaci | Stan ochrony, czas trwania, zasady pochłaniania obrażeń i efekt. |

Combo do stworzenia po przygotowaniu klipów i ocenie obecnych mechanik:

- Osobne serie niskich i powietrznych ciosów dla każdej broni; obecnie nowe konteksty broni dziedziczą profil.
- Wielociosowa seria podczas dalekiego skoku; obecnie powtarzany jest pojedynczy mocny cios.
- Dalsze przejścia i anulowania między ciosami a ruchem — wymagają osobnego ustalenia zasad i balansu.

## Oprawa i weryfikacja do ukończenia

- Klipy niskiego i powietrznego ataku, silnego ciosu w locie oraz wślizgu i dalekiego skoku. Brakujące klipy są oznaczane w edytorze; mechanika działa z animacjami zastępczymi.
- Osobne klipy rzutu w kucki i w powietrzu; obecnie używany jest skonfigurowany klip rzutu broni.
- Dopracowanie chwytów broni, efektów, dźwięków i tempa walki.
- Ręczny test w głównym edytorze i ocena balansu. Raport automatycznych kontroli: `.verification/ContextActionsEvidence` w workspace.
