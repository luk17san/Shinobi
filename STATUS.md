# Shinobi — etap 1

Projekt 2.5D w Unity: geometria 3D, kamera z boku, ruch w osiach X/Y.

## Uruchomienie

Po imporcie skryptow edytor tworzy `Assets/Shinobi/Scenes/TrainingArena.unity`, materialy oraz prefab `Assets/Shinobi/Prefabs/Player.prefab`. Istniejaca arena nie jest ponownie generowana.

1. Poczekaj na kompilacje. W razie potrzeby wybierz Assets > Refresh.
2. Wybierz Shinobi > Etap 1 > Otworz arene treningowa.
3. Uruchom Play i kliknij okno Game.
4. A/D lub strzalki: ruch. Spacja: skok. R: powrot na start.
5. Pad: lewy drazek i dolny przycisk do skoku.

## Inspector

`Player > Shinobi Movement`: Move Speed (6), Jump Height (2), Gravity (-20), Arena Half Width (12), Visual (dziecko Visual). Minimalna predkosc i wysokosc: 0.1; grawitacja: od -60 do -1. Arena Half Width nie powinno przekraczac 12 na tej mapie.

`Main Camera > Camera`: Projection = Orthographic, Size = 8. Postac ma Character Controller; nie dodawaj Rigidbody do tego kontrolera.

## Testy do wykonania w Unity

- Ruch w obu kierunkach i zatrzymanie po puszczeniu przycisku.
- Skok tylko z podloza, ladowanie i ponowny skok; brak dodatkowego skoku w powietrzu.
- Utrzymanie Z = 0 i zatrzymanie na obu koncach areny.
- R przywraca pozycje startowa; znacznik pokazuje kierunek ruchu.
- Sprawdzenie kamery przy docelowej proporcji okna Game (zalecane 16:9).

## Granice etapu

Model zastepczy bez animacji. Walka, unik, blok, techniki i multiplayer sa kolejnymi etapami. Kontroler lokalny wymaga integracji z przyszlym modelem sieciowym; nie stanowi jeszcze synchronizacji online.

Kompilacja Unity, wygenerowanie sceny/prefabu oraz fizyczne testy Play Mode wymagaja osobnego potwierdzenia; dostarczenie skryptow nie potwierdza ich wyniku.

## Sterowanie HFSM

Hierarchia aktywna: Player / Movement / Grounded / Idle lub Run; Player / Movement / Airborne / Jump lub Fall.

- `ShinobiMovement`: komponent sceny, cykl zycia i koordynacja.
- `ShinobiInput`: odczyt klawiatury/pada i dane wejsciowe.
- `ShinobiMotor`: CharacterController, ruch, grawitacja, obrot modelu i reset.
- `HierarchicalState`: aktywny podstan, Enter/Tick/Exit; aktualizacja rodzic-przed-dzieckiem, wyjscie dziecko-przed-rodzicem.
- `PlayerMovementState`: przelaczanie galezi i podstanow; skok tylko na ziemi. Idle/Run oraz Jump/Fall sa liscmi opisujacymi faze ruchu; wspolna logika dziala w rodzicach Grounded/Airborne.

W Play Mode wybierz `Player > Shinobi Movement > Active State (Play Mode)`. Podglad jest tylko do odczytu. Pola ustawien i ich nazwy zachowano, aby zachowac dane sceny/prefabu. Zmiany predkosci, wysokosci skoku i grawitacji w Inspectorze sa odczytywane co klatke. Sterowanie bez zmian.

### Weryfikacja przebudowy

- PASS: kompilacja skryptow runtime i Editor przy uzyciu kompilatora C# i referencji z tego projektu Unity (6000.6.3f1). Ostrzezenie CS0649 dla Visual wynika z przypisywania referencji przez serializacje Unity.
- PASS: testy przejsc Idle/Run, startu skoku, odrzucenia drugiego skoku, przejscia Jump/Fall, ladowania, zejscia z krawedzi, ponownego wejscia po resecie oraz kolejnosci Tick/Exit. Test uzywa zastepczego motoru; nie potwierdza fizyki Unity.
- Import w edytorze, zachowanie referencji sceny/prefabu i fizyczny Play Mode po przebudowie: do sprawdzenia.

Test w Unity: Assets > Refresh, otworz TrainingArena, Play; sprawdz Idle po ladowaniu, Run przy A/D, Jump po Spacji, Fall po szczycie skoku, powrot Grounded po ladowaniu oraz brak dodatkowego skoku w powietrzu. Sprawdz tez reset R w locie i kolizje przy obu granicach areny.


## Etap 2 — pojedynczy atak z bliska

HFSM: `Player / Movement / Grounded / Attack / Windup -> Active -> Recovery`, potem Idle/Run. Atak tylko na ziemi; w trakcie ciosu ruch i skok sa zablokowane. Utrata podloza, reset R i wylaczenie komponentu przerywaja atak. Input korzysta z nacisniecia, wiec przytrzymanie przycisku nie powtarza ciosow.

### Przygotowanie areny

1. Assets > Refresh; poczekaj na kompilacje.
2. Poza Play Mode wybierz `Shinobi > Etap 2 > Przygotuj trening walki`.
3. Polecenie otwiera TrainingArena, dodaje ShinobiCombat i AttackPreview do Player oraz Player.prefab, tworzy TrainingDummy z ShinobiHealth i zapisuje arene. Kolejne wywolanie nie duplikuje manekina ani podgladu. Nie regeneruje geometrii etapu 1.
4. Play, kliknij Game. J / LPM: atak. Na padzie zachodni przycisk (X na Xbox / kwadrat na PlayStation). A/D, skok i R bez zmian.

### Inspector

`Player > Shinobi Movement > Active State (Play Mode)`: podglad fazy ataku.

`Player > Shinobi Combat`: Damage=20 (min. 1), Windup=0.12s (min. 0), Active Duration=0.08s i Recovery=0.25s (min. 0.01). Reach=1.3, Hit Height=1.2, Hit Depth=0.8 (min. 0.1), Hit Center Height=1. Target Mask=Everything; mozna ograniczyc do warstwy przeciwnikow. Hit Preview wskazuje dziecko AttackPreview.

Zaznaczenie Player pokazuje zolty Gizmo obszaru trafienia; w fazie Active widoczny jest zolty blok zastepujacy animacje ciosu. Obrazenia sa sprawdzane raz, przy rozpoczeciu Active. Cel wchodzacy w obszar pozniej nie zostanie trafiony tym ciosem. Wiele colliderow tego samego ShinobiHealth nie zwielokrotnia obrazen.

`TrainingDummy > Shinobi Health`: Max Health=100, Training Auto Reset=true, Reset Delay=2s, Visual wskazuje Renderer dziecka Visual. Nad manekinem widoczna jest liczba HP; trafienie daje krotki bialy blysk. Przy 0 HP po dwoch sekundach manekin odzyskuje zdrowie. To zachowanie manekina, nie docelowy system smierci gracza.

### Weryfikacja

- PASS: niezalezna kompilacja runtime i Editor z referencjami z projektu Unity.
- PASS: testy poprzednich przejsc ruchu oraz Windup/Active/Recovery, pojedynczego trafienia, dlugiej klatki przekraczajacej wszystkie fazy, blokowania ruchu/skoku, odrzucenia ataku w powietrzu i przerwania po utracie podloza/wylaczeniu. Motor i combat w tych testach sa zastepcze.
- Uruchomienie polecenia przygotowania areny, faktyczne Physics.OverlapBox, blysk/HP, pad i fizyczny Play Mode po zmianie wymagaja osobnej weryfikacji.

Test manualny: podejdz z lewej do manekina na X=4 i uderz — HP 100->80; odwroc sie i uderz — brak obrazen; sprawdz cios z prawej, spoza zasiegu oraz w powietrzu. Przytrzymaj J — jeden atak; kliknij ponownie po Recovery — kolejny. Piec trafien sprowadza HP do 0, potem reset treningowy. Sprawdz R w Windup/Recovery i brak pozostawionego podgladu. Przesun sie podczas ciosu — postac stoi. Zmien Damage/Reach w Inspectorze i powtorz probe.

Combo, animacje postaci, blok, unik, energia, techniki i multiplayer pozostaja kolejnymi etapami. Obecny system trafien jest lokalny i wymaga przyszlej integracji z autorytetem sieciowym.


## Etap 3 — combo w HFSM

HFSM: `Grounded / Attack / Hit1`, `Hit2`, `Hit3`; kazdy cios ma Windup/Active/Recovery. Domyslnie sa trzy ciosy, a nie automatyczna seria: pierwszy naciska J/LPM, drugi i trzeci wymagaja nowych nacisniec w koncowym oknie Recovery. Przytrzymanie przycisku nie powtarza ciosow.

### Ustawienia

`Player > Shinobi Combat`: dotychczasowe Damage/Windup/Active Duration/Recovery/Reach okreslaja pierwszy cios. Nowe `Follow Up Attacks > Element 0` to drugi cios: 25 obrazen, 0.10s przygotowania, 0.08s Active, 0.22s Recovery, zasieg 1.4. `Element 1` to trzeci: 35 obrazen, 0.18s przygotowania, 0.08s Active, 0.40s Recovery, zasieg 1.6. Elementy listy sa edytowalne; Size=0 przywraca pojedynczy cios.

`Combo Buffer=0.18` (0.01–0.5s) okresla koncowa czesc Recovery, w ktorej przyjmowane jest nacisniecie nastepnego ciosu. Jesli bufor jest dluzszy niz Recovery, okno obejmuje cale Recovery. Nacisniecie w Windup/Active albo po zakonczeniu ciosu nie kontynuuje combo. W oknie zapamietywane jest jedno kolejne uderzenie. Po koncu serii nastepny atak zaczyna ponownie od Hit1.

Ruch/skok sa zablokowane podczas calej serii. Reset R, utrata podloza i wylaczenie sterowania przerywaja combo i czyszcza oczekujace nacisniecie. Kazdy cios od nowa sprawdza cele i moze zadac temu samemu manekinowi obrazenia. Parametry ciosu sa kopiowane na jego poczatku, wiec edycja w trakcie nie zmienia juz wykonywanego uderzenia.

### Uruchomienie i test

Assets > Refresh, potem otworz TrainingArena. Jezeli scena nie ma TrainingDummy/ShinobiCombat, poza Play Mode uzyj `Shinobi > Etap 2 > Przygotuj trening walki`. Nie trzeba regenerowac istniejacej sceny do samego combo.

Play: podejdz do manekina, uderz J i ponow nacisniecie w koncowej czesci Recovery. Przy domyslnym pierwszym ciosie drugie nacisniecie jest przyjmowane okolo 0.27–0.45s po rozpoczeciu. Przy drugim ciosie trzecie nacisniecie: okolo 0.22–0.40s po rozpoczeciu Hit2. W `Player > Shinobi Movement > Active State` obserwuj Hit1/Hit2/Hit3. Trzy trafienia domyslnie: HP 100->80->55->20.

Sprawdz pojedynczy cios bez kontynuacji, przytrzymanie J, nacisniecie za wczesnie, reset R po zapamietaniu kontynuacji, atak w odwrotnym kierunku, serie spoza zasiegu, oraz Size=0 w Follow Up Attacks. Kontrola fizycznych trafien i tempa combo wymaga Play Mode.

### Weryfikacja

- PASS: kompilacja runtime i Editor z referencjami projektu Unity.
- PASS: poprzednie testy HFSM i ataku oraz seria trzech trafien, okno kontynuacji, odrzucenie wczesnego nacisniecia, reset serii/przerwanie i brak kontynuacji od pierwszego nacisniecia przy dlugiej klatce. Testy wykorzystuja zastepczy motor/combat.
- Zapisana arena zawierala TrainingDummy i AttackPreview przed dodaniem combo. To potwierdza przygotowanie poprzedniego etapu, nie wynik testow Play Mode.
- Import w Unity, aktualizacja nowych pol na istniejacych komponentach, obrazenia 20/25/35 i sterowanie w Play Mode po tej zmianie pozostaja do sprawdzenia.

Animacje, blok, unik, techniki i siec pozostaja nastepnymi etapami.

