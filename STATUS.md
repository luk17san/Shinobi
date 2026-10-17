# Shinobi — stan projektu

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


## Model testowy YBot — 16 pazdziernika 2026

TrainingArena oraz Assets/Shinobi/Prefabs/Player.prefab uzywaja modelu z Assets/Shinobi/Animation/YBot/Y Bot@Fight Idle.fbx.

- Hierarchia: Player > Visual > YBot. Visual jest punktem obrotu (Rotation Y = 90 stopni dla prawej strony), Position Y = -0.08 kompensuje Skin Width kontrolera, aby stopy dotykaly ziemi. Skala modelu wynosi okolo 1.2702 i jest dopasowana do wysokosci istniejacego CharacterController (2 jednostki).
- Player > Shinobi Movement > Visual nadal wskazuje ten sam obiekt Visual. Jego kapsula ma wylaczony Mesh Renderer, FacingMarker jest nieaktywny. Zachowano parametry ruchu, walki i AttackPreview oraz TrainingDummy.
- Player > Visual > YBot > Animator: Controller = YBotTraining, Apply Root Motion = false. Avatar Humanoid pochodzi z tego samego FBX.
- Assets/Shinobi/Animation/YBot/YBotTraining.controller: domyslny stan Fight Idle. Klip ma Loop Time i Loop Pose; rotacja oraz pozycja root sa wypieczone do pozy.
- Model odtwarza obecnie tylko Fight Idle, takze podczas ruchu, skoku i atakowania. Brak osobnych klipow tych akcji w folderze YBot; trafienia nadal obsluguje istniejacy kod walki.
- Ponowne przypisanie: Shinobi > Model testowy > Uzyj YBot w TrainingArena. Polecenie zapisuje scene oraz Player.prefab. Istniejacy kontroler animacji nie jest nadpisywany.

Weryfikacja: import i kompilacja Unity 6000.6.3f1 oraz programatyczny Play Mode na kopii aktualnej sceny: poprawny Humanoid/materialy URP, deformacja siatki, petla idle, brak poziomego dryfu, A/D i obrot, skok/ladowanie, reset R, trafienie manekina 100 -> 80 HP. Sprawdzono render areny i zblizenie modelu. Nie wykonano fizycznego testu klawiatura w otwartym edytorze docelowego projektu. Raport i obrazy: Documents/ChatGPT/Shinobi/.verification/YBotUnity/. W izolowanym edytorze wystapil blad jego indeksu Search; automat wznowil Play Mode po automatycznej pauzie konsoli i zakonczyl wszystkie kontrole.

Po odswiezeniu Assets ponownie otworz Assets/Shinobi/Scenes/TrainingArena.unity, aby wczytac zapisana wersje sceny. Sterowanie obecnej wersji projektu: A/D lub strzalki, Spacja, J/LPM, R.

## Etap 4 — walka, jutsu i zderzenia na YBocie

Ta sekcja zastepuje starsze informacje o sterowaniu J/LPM, Spacji oraz oknie combo z konca Recovery. Nowszy kod Stage1 jest teraz zintegrowany z TrainingArena oraz Player.prefab.

### Uruchomienie i sterowanie

Po Assets > Refresh poczekaj na kompilacje, ponownie otworz Assets/Shinobi/Scenes/TrainingArena.unity i uruchom Play. Scena jest juz przygotowana. Nie trzeba generowac jej ponownie.

- A/D lub lewo/prawo: ruch; gora: skok; G: cios. Kolejne osobne G kolejkuja drugi i trzeci cios. Trzymanie G daje tylko jeden cios.
- Przytrzymaj J, nacisnij osobno gora, prawo, dol, a potem pusc J: Fala treningowa. Koszt 20 chakry, obrazenia 30, przygotowanie 0.3 s i powrot 0.3 s. J samo w sobie nie zadaje ciosu. LPM i Spacja nie sa teraz przypisane.
- Pieczecie podczas J nie poruszaja postaci ani nie uruchamiaja skoku. Bledna sekwencja albo za mala chakra nie uruchamia jutsu.
- R przywraca pozycje i chakre oraz przerywa akcje. Nie przywraca HP; treningowe HP odnawiaja sie po 2 sekundach od osiagniecia 0. Brak regeneracji chakry poza R.
- Pad: dotychczasowy ruch, dolny przycisk do skoku i zachodni do ciosu. Wprowadzanie pieczeci wymaga klawiatury.

### Edycja w Inspectorze

- Player > Shinobi Combat > Profile: Assets/Shinobi/Configuration/TrainingCombat.asset. Ten sam profil jest przypisany do Player.prefab.
- Shinobi > Edytor walki: wybierz TrainingCombat w polu Zestaw walki; zakladki Sterowanie, Ataki wrecz, Combo i Jutsu. Definicje trzech ciosow oraz Fali treningowej sa subassetami profilu i maja przyciski Edytuj.
- Ciosy zachowuja obrazenia 20/25/35, zasiegi 1.3/1.4/1.6 i dotychczasowe czasy. Combo Window = 0.5 s (minimum 0.01), liczone od poczatku kazdego ciosu; nacisniecie jest buforowane, a nastepny cios czeka na koniec poprzedniego.
- Trzeci cios ma Push Distance = 1.5. Odepchniecie wymaga ShinobiMovement u celu; obecny nieruchomy TrainingDummy nie jest przesuwany.
- Profil: Max Chakra = 100; Interrupt Chance = 60%, Chance Per Level = 15 punktow procentowych, Maximum Interrupt Chance = 95%, Draw Separation = 2. Level kazdego ciosu i jutsu = 1. Szanse maja zakres 0–100%, odleglosc minimum 0, poziom minimum 1.
- Player > Shinobi Technique Controller > Target Mask oraz Player > Shinobi Combat > Target Mask wybieraja cele. Jutsu: Radius = 2, Forward Offset = 2. To trafienie obszarowe przed postacia, nie lecacy pocisk.
- TrainingHUD > Shinobi Training Hud: Player wskazuje gracza, Position = (12,12), Width = 460 (minimum 300). Szerokosc panelu jest ograniczana do szerokosci okna. Panel pokazuje HP, chakre, pieczecie, stan oraz aktualne przyciski profilu.
- Player > Shinobi Health > Visual wskazuje widoczny SkinnedMeshRenderer YBota, aby blysk obrazen nie trafial w wylaczona kapsule.

Ponowna konfiguracja: Shinobi > Etap 4 > Polacz walke i jutsu z arena. Polecenie zapisuje scene/prefab, dodaje brakujace komponenty i panel, zachowuje istniejacy profil i nie tworzy duplikatow.

### Wynik i ograniczenia

Import i kompilacja Unity 6000.6.3f1 oraz 28 automatycznych kontroli na kopii aktualnej sceny: PASS. Potwierdzono combo 100 -> 20 HP, jutsu, koszty/przerwania, ruch i zderzenia dwoch YBotow (remis, szanse 0/100%). Sprawdzono obraz Game View z panelem. Szczegoly: CombatIntegrationVerification.md.

YBot nadal odtwarza Fight Idle podczas ruchu i atakow. Pola Animation w definicjach przechowuja klipy, ale nie sa jeszcze polaczone z Animatorem. Brak osobnych animacji biegu, skoku i ciosow oraz efektu wizualnego Fali treningowej. Blok, unik, wytrzymalosc, AI przeciwnika i multiplayer pozostaja kolejnymi etapami. Manekin nie atakuje; zderzenia sprawdzono przez automat na tymczasowej drugiej postaci. Testy fizyczna klawiatura i pelna obsluga edytora walki w glownym projekcie pozostaja do recznego sprawdzenia.


## Sterowanie WASD i przykucanie — 17 pazdziernika 2026

Ta sekcja zastepuje poprzednie instrukcje ze strzalkami.

- A/D: ruch. W: skok z ziemi. Przytrzymane S: przykucanie, puszczenie S: wstanie, jesli nad glowa jest miejsce. W podczas przykucania nie uruchamia skoku.
- G: cios/combo. Przytrzymaj J, nacisnij osobno W, D, S i pusc J: Fala treningowa. Mapowanie pieczeci: gora=W, prawo=D, dol=S, lewo=A. Podczas Focus WASD wprowadza tylko pieczecie, bez ruchu, skoku i kucania.
- Strzalki nie steruja ruchem, skokiem ani jutsu. R i sterowanie ruchem/skokiem/ciosem na padzie pozostaja jak w etapie 4.
- Player > Shinobi Combat > Profile > Jump Key = W, Crouch Key = S. Pola sa tez dostepne w Shinobi > Edytor walki > Sterowanie. Zaktualizowano istniejacy TrainingCombat.asset i domyslne ustawienia nowych profili.
- Player > Shinobi Movement > Crouch Height Ratio = 0.6 (zakres 0.4–0.9), Crouch Speed Multiplier = 0.4 (zakres 0–1). Dotyczy rowniez Player.prefab. Przy wysokosci kontrolera 2 m przykucanie zmniejsza ja do 1.2 m, zachowujac dol kapsuly. Minimalna wysokosc to dwie dlugosci promienia kontrolera.
- Podczas kucania Active State pokazuje Crouch. Postac idzie z 40% predkosci. Sprawdzenie miejsca na wstanie uwzglednia fizyczne collidery, pomija triggery, wlasne collidery i ignorowane kolizje. Po usunieciu przeszkody postac moze wstac.
- YBot nie ma osobnego klipu crouch: obnizenie sylwetki jest tymczasowym skalowaniem Visual w osi Y. Mechanika kolizji i predkosci dziala niezaleznie; naturalna animacja kucania wymaga klipu.
- Panel treningowy pokazuje aktualne W/S oraz pieczecie literami W/D/S/A.

Weryfikacja: import/kompilacja Unity i 41 automatycznych kontroli na kopii TrainingArena: PASS. Sprawdzono nowy profil, brak reakcji na strzalki, skok W, kucanie S, obnizony collider, wolniejszy ruch, brak skoku w kucki, niski sufit (takze warstwa Ignore Raycast), powrot do stania, brak kucania podczas J oraz jutsu W/D/S. Przeszly tez regresje combo, chakry, przerwan i zderzen. Test HFSM na atrapach: PASS. Obejrzano Game View i pozycje kucania. Nie przeprowadzono fizycznego testu klawiatura w glownym edytorze. Raporty i podglady: Documents/ChatGPT/Shinobi/.verification/YBotUnity/WasdVerification.txt, Wasd-Game.png, Wasd-Crouch.png. Izolowany edytor ponownie zglosil blad wlasnego indeksu Search; automat wznowil Play Mode po pauzie konsoli.

Uruchomienie: Assets > Refresh, poczekaj na kompilacje, ponownie otworz TrainingArena i wlacz Play.


## Y — kolejne bronie w ekwipunku — 17 pazdziernika 2026

- Y wybiera nastepna poprawnie skonfigurowana bron z Player > Shinobi Weapon Inventory > Weapons. Po ostatniej wraca do pierwszej. Trzymanie Y wykonuje jedna zmiane; kolejna wymaga puszczenia i ponownego nacisniecia.
- Zmiana jest odrzucana podczas calego ciosu/combo, Focus, wykonywania jutsu i przy 0 HP. Nie jest kolejkowana. Y + G w tej samej klatce wybiera bron przed rozpoczeciem ciosu, o ile postac nie jest juz zajeta.
- Profil TrainingCombat ma Next Weapon Key = Y. To samo pole jest w Shinobi > Edytor walki > Sterowanie. Kontrolki WASD/G/J/R, jutsu i chakra pozostaja w profilu postaci.
- Scena i Player.prefab maja ten sam poczatkowy ekwipunek z dwoma wpisami: Bron treningowa 1 (obrazenia 20/25/35, zasiegi 1.3/1.4/1.6) i Bron treningowa 2 (25/30/40, zasiegi 1.6/1.7/1.9). Sa to edytowalne konfiguracje testowe do sprawdzania przelaczania, nie gotowe modele broni. Nazwa wybranej broni jest widoczna w panelu.
- Definicje: Assets/Shinobi/Configuration/Weapons/TrainingWeapon1.asset i TrainingWeapon2.asset. Kazda ma Display Name, Combo (referencje do definicji ciosow), Combo Window (minimum 0.01), opcjonalny Visual Prefab oraz Local Position / Euler Angles / Scale. Kombinacje ciosow sa subassetami kazdej broni; mozna je edytowac po rozwinieciu assetu w Project.
- Nowa bron: Create > Shinobi > Bron; utworz/przypisz definicje przez Create > Shinobi > Atak wrecz i dodaj bron do Weapons. Lista musi zawierac co najmniej jeden poprawny cios na bron. Puste sloty, bronie bez combo i niekompletne definicje sa pomijane. Jeden poprawny wpis pozostaje wybrany. Pusty ekwipunek korzysta z walki wrecz profilu postaci.
- Initial Weapon Index wskazuje pierwszy wyposazany slot (liczenie od 0). Jesli slot jest pusty, wybierany jest nastepny poprawny. Kolejnosc Weapons to kolejnosc pod Y. Ustawienia sceny i prefabu edytuj osobno lub przez Prefab Mode.
- Opcjonalny model wymaga przypisania Weapon Socket w komponencie oraz Visual Prefab w definicji. Transform socketu wybierasz w hierarchii postaci; zoptymalizowany rig YBota wymaga udostepnienia kosci dloni lub wlasnego socketu. Kod ustawia lokalne przesuniecie/obrot/skale modelu, wylacza jego Collidery i ustawia Rigidbody jako kinematic; trafienia obsluguje ShinobiCombat. Przy zmianie usuwa poprzedni model. W obecnych dwoch wpisach nie przypisano modeli ani socketu.
- Ponowne przygotowanie: Shinobi > Etap 5 > Przygotuj ekwipunek broni. Tworzy brakujace przyklady i komponenty; nie nadpisuje istniejacej listy ani definicji.

Weryfikacja: kompilacja/import Unity 6000.6.3f1 i 59 automatycznych kontroli na kopii TrainingArena: PASS. Potwierdzono nacisniecia Y, brak powtarzania przy trzymaniu, powrot na poczatek listy, rzeczywiste 25 obrazen po zmianie broni, nowy zasieg, zachowanie profilu/chakry/jutsu, blokady akcji i 0 HP, nieprawidlowe indeksy, puste/jednoelementowe/niekompletne listy, prefab, regresje WASD/kucania/combo/jutsu/zderzen. Obejrzano Game View z nazwa drugiej broni. Test HFSM na atrapach: PASS. Modeli broni, montazu w dloni, animacji, podnoszenia przedmiotow i zapisu ekwipunku na dysku nie dodawano ani nie testowano. Nie wykonano fizycznego testu klawiatura w glownym edytorze. Izolowany edytor zglosil znany blad indeksu Search; automat wznowil Play Mode po pauzie konsoli.

Raport workspace: .verification/YBotUnity/WeaponVerification.txt. Podglad: Weapon-Selected.png. Po Assets > Refresh ponownie otworz TrainingArena i uruchom Play.



## Etap 6 — automatyczny blok i wytrzymalosc — 17 pazdziernika 2026

Aktualna lista wykonanych i pozostalych etapow jest w ROADMAP.md. Etap 6 zostal zintegrowany z TrainingArena oraz Player.prefab; nastepny proponowany etap to przeciwnik treningowy z AI.

### Zasady

- ShinobiDefense na graczu chroni przed zwyklym ciosem wrecz z przodu, gdy postac stoi/porusza sie/kuca na ziemi i nie atakuje, nie wykonuje Focus/jutsu i nie ma przelamanej gardy. Kierunek bloku pochodzi z kierunku ruchu postaci.
- Blok nie ma przycisku. Poprawnie zablokowany cios nie odbiera HP ani nie odpycha; zuzywa wytrzymalosc rowna obrazeniom razy Block Cost Multiplier. Trafienia od tylu/spoza kata i jutsu przechodza do HP.
- Gdy wytrzymalosci wystarcza dokladnie na koszt, trafienie jest jeszcze w pelni zablokowane (wytrzymalosc 0). Kolejny koszt przekraczajacy zasob przelamuje garde i zadaje pelne obrazenia. Nie ma czesciowej redukcji obrazen.
- Przelamanie trwa domyslnie 1 s i przerywa akcje. W tym czasie ruch, skok, atak, rozpoczynanie jutsu i zmiana broni sa zablokowane. R nadal dziala.
- Wytrzymalosc regeneruje sie po przerwie od trafienia, do limitu. Nie regeneruje sie podczas ataku, Focus, jutsu, przelamania gardy ani przy 0 HP. Same ruch i kucanie nie zuzywaja tego zasobu.
- Obrazenia przyznane przez rozstrzygniete zderzenie atakow omijaja automatyczny blok; obie postacie uczestniczyly wtedy w akcji ofensywnej. Reguly remisu, szansy wygranej i wymiany obrazen zachowano.
- ShinobiHealth.ApplyDamage jest nadal bezposrednim obrazeniem (np. zagrozenia). Zwykle trafienia walki przechodza przez resolver i ShinobiDefense.ReceiveHit; ta metoda otrzymuje kierunek zrodla oraz informacje, czy cios jest blokowalny.

### Inspector i proba reczna

- Player > Shinobi Combat > Profile > Auto Block = true; Max Stamina = 100 (minimum 1).
- Block Cost Multiplier = 1 (minimum 0), Stamina Regen Rate = 20/s (minimum 0), Stamina Regen Delay = 1 s (minimum 0).
- Guard Break Duration = 1 s (minimum 0.01), Block Half Angle = 60 stopni (zakres 0–90; calkowity kat 120 stopni).
- Te same ustawienia sa w Shinobi > Edytor walki > Obrona. Sa startowymi ustawieniami do balansu, nie gotowym balansem gry.
- TrainingHUD ma panel HP/chakry/wytrzymalosci/broni. Show Controls / „Sterowanie i stan” rozwija instrukcje oraz podglad stanu, domyslnie zwinięte.
- Przyciski Cios z przodu / Cios z tylu / Jutsu symuluja pojedyncze 25-punktowe trafienie. Cztery kolejne ciosy frontalne zuzyja 100 wytrzymalosci bez utraty HP; piaty przed regeneracja przelamie garde i zada 25 obrazen. Przycisk nie odgrywa animacji przeciwnika i nie zastepuje AI.
- R przywraca pozycje, chakre, wytrzymalosc i stan gardy. Nie przywraca HP. Blok/przelamanie nie maja jeszcze osobnych klipow; YBot pozostaje przy Fight Idle.
- Ponowne przypisanie: Shinobi > Etap 6 > Przygotuj blok i wytrzymalosc. Zachowuje istniejace komponenty i ustawienia profilu. TrainingDummy nie otrzymal obrony ani AI.

### Weryfikacja

Unity 6000.6.3f1: import/kompilacja oraz 83 kontrole na kopii aktualnej sceny — wszystkie PASS. Potwierdzono faktyczny frontalny atak przez resolver i jednokrotny koszt bloku, brak obrony od tylu/w powietrzu/podczas ataku lub Focus, sferyczne jutsu, odnowe i limit wytrzymalosci, koniec zasobu i przelamanie, blokady akcji i Y, reset R, parametry profilu oraz wszystkie dotychczasowe regresje. HFSM na atrapach: PASS. Obejrzano Game View z wytrzymaloscia 75/100 i komunikatem Blok oraz zwarty panel.

Artefakty workspace: .verification/YBotUnity/DefenseVerification.txt, Defense-Block.png, Defense-Game.png, Defense-Setup.log, Defense-Verify.log; uruchomienie .verification/RunDefense.ps1. Izolowany edytor zglosil znany blad wlasnego indeksu Search, po ktorym automat wznowil Play Mode. Nie wykonano fizycznego testu klawiatura/mysza w glownym edytorze ani pelnego testu zakladki Obrona i animacji bloku. Testy nie potwierdzaja balansu ani synchronizacji sieciowej.


## Etap 7 — szybki dash i zmiana planu — 17 pazdziernika 2026

ROADMAP.md: etap 7 to szybki dash, etap 8 to przeciwnik treningowy z AI, 9 to animacje/modele, a punkt 10 to lepsza arena treningowa. Pelny ekwipunek, podnoszenie i zapis przesunieto na pozniejszy etap 13.

### Sterowanie i zachowanie

- Lewy Shift uruchamia krotki dash. Kierunek jest wybierany z A/D w chwili nacisniecia; bez A/D uzywany jest ostatni zwrot. Kierunek nie zmienia sie w trakcie dasha. Trzymanie Shiftu nie powtarza ruchu.
- Start z ziemi, poza kucaniem, atakiem, Focus/jutsu, przelamaniem gardy i 0 HP. Podczas dasha nie mozna skoczyc, kucnac, rozpoczac ciosu/jutsu ani zmienic broni. Dash ma pierwszenstwo przed rownoczesnym skokiem, ciosem lub Y, jezeli moze sie rozpoczac.
- Ruch przechodzi przez CharacterController.Move i respektuje collidery, granice X oraz plaszczyzne Z. Grawitacja nadal dziala po opuszczeniu podloza. Boczna kolizja konczy dash.
- Dash nie daje nietykalnosci ani automatycznej gardy. Obrazenia lub inne przerwanie akcji natychmiast go koncza. Koszt nie jest zwracany po trafieniu lub kolizji.
- R przerywa dash, przywraca pozycje/energie i usuwa cooldown. Wylaczenie ShinobiMovement rowniez konczy dash.

### Inspector

- Player > Shinobi Combat > Profile > Dash Key = Left Shift. Pole jest tez w Shinobi > Edytor walki > Sterowanie.
- Player > Shinobi Movement > Dash Distance = 3 (minimum 0.1 m), Dash Duration = 0.15 s (zakres 0.05–0.5), Dash Cooldown = 0.7 s (minimum 0), Dash Stamina Cost = 20 (minimum 0).
- Cooldown liczy sie od udanego rozpoczecia dasha. Odrzucona proba nie pobiera wytrzymalosci i nie przedluza cooldownu. Wytrzymalosc nie regeneruje sie podczas dasha, a jej odnowa czeka na ustawione w profilu opoznienie od wydania zasobu.
- Te ustawienia sa zapisane w scenie oraz Player.prefab. Predkosc dasha jest wyliczana z dystansu i czasu (domyslnie 20 m/s). Przy przeszkodzie przebyty dystans jest krotszy.
- Panel pokazuje przycisk, aktywny dash i pozostaly czas oczekiwania. Active State podczas ruchu wskazuje Player/Movement/Dash. YBot nadal korzysta z Fight Idle; osobna animacja/efekt dasha nalezy do dalszej oprawy.
- Ponowne przygotowanie: Shinobi > Etap 7 > Przygotuj szybki dash. Nie przebudowuje geometrii areny. Lepsza arena ma osobny, pozniejszy punkt 10.

### Weryfikacja

Unity 6000.6.3f1: import/kompilacja i 108 automatycznych kontroli na kopii TrainingArena — wszystkie PASS. Sprawdzono dystans/czas, kierunek, jednorazowe nacisniecie, koszt/cooldown, brak kosztu przy odrzuceniu, blokady akcji, brak startu w kucki/w powietrzu, sciane, granice areny, Z, trafienie/przerwanie/reset/wylaczenie oraz regresje obrony, ekwipunku, WASD, jutsu i combo. Test HFSM na atrapach: PASS. Obejrzano Game View z aktywnym dashem i wytrzymaloscia 80/100.

Raport workspace: .verification/YBotUnity/DashVerification.txt; podglad Dash-Active.png; uruchomienie .verification/RunDash.ps1. Izolowany edytor ponownie zglosil blad indeksu Search, po ktorym automat wznowil Play Mode. Nie wykonano fizycznego testu klawiatura w glownym edytorze, oceny balansu ani animacji dasha. Start w powietrzu i klatki nietykalnosci nie sa czescia tej wersji.

Po Assets > Refresh poczekaj na kompilacje, ponownie otworz TrainingArena i wlacz Play.


## 2026-10-17 — dash przez dwukrotne A/D
- A -> A uruchamia dash w lewo, D -> D w prawo. Lewy Shift nie aktywuje dasha.
- Domyslne okno: 0.25 s. Edycja: profil TrainingCombat, zakladka Sterowanie, dashDoubleTapWindow (0.1–0.5 s).
- Przytrzymanie, przeciwne kierunki, jednoczesne A+D i pieczecie przy J nie skladaja sie na dash. Bufor jest czyszczony przy zablokowanej akcji, trafieniu, resecie i wylaczeniu ruchu.
- Dystans 3 m, czas 0.15 s, koszt 20 i cooldown 0.7 s pozostaja bez zmian.
- Weryfikacja: import/kompilacja oraz 102 kontroli automatycznych Play Mode na kopii TrainingArena: PASS, w tym dwuklik i regresje walki/jutsu/obrony/ekwipunku. Test nie jest recznym testem klawiatury w glownym edytorze. Znany wyjatek indeksu Unity Search przy starcie nadal wystepuje niezaleznie od kodu gry.


## 2026-10-17 — etap 8: przeciwnik treningowy AI
- TrainingArena ma TrainingOpponent (YBot) przy x=-6. Start w pauzie; panel: Rozpocznij sparing / Pauza sparingu oraz Reset sparingu. Zwykly TrainingDummy pozostaje.
- AI podchodzi i zwraca sie do gracza, pokazuje UWAGA: CIOS przez 0.65 s, potem wykonuje pojedynczy fizyczny atak. Dystans 1.15 m; predkosc podejscia 45% ruchu; odstep miedzy probami 1.5 s.
- Obie postacie uzywaja tego samego CharacterController, stanow ruchu/ataku, resolvera trafien, HP, frontalnego bloku, staminy i przelamania gardy. AI nie kopiuje klawiatury, pieczeci ani dasha gracza. Trafienie przerywa ostrzezenie. Przy pokonanej postaci AI czeka; treningowe odnowienie HP pozostaje wlaczone.
- Inspector: TrainingOpponent > Shinobi Training Opponent: Target, Approach Speed, Attack Distance, Telegraph Duration, Attack Interval. Shinobi Combat > Profile wskazuje osobny Configuration/TrainingOpponentCombat.asset; combo[0] wskazuje TrainingOpponentStrike.asset (damage, level, reach, czasy). Edycja nie zmienia profilu ani ciosow gracza.
- Reset sparingu przywraca rywalowi pozycje startowa, HP/stamine/stan akcji i pauze; graczowi odnawia HP/stamine. R pozostaje resetem ruchu/chakry/obrony gracza. Do ustawienia sceny: Shinobi > Etap 8 > Przygotuj przeciwnika treningowego. Ponowne uruchomienie nie dodaje kolejnego rywala. Narzedzia etapow 4–7 wybieraja gracza niezaleznie od obecnosci AI.
- Weryfikacja na kopii aktualnej TrainingArena: kompilacja/import Unity oraz 113 kontroli Play Mode PASS, w tym 11 kontroli AI i 102 regresje. Sprawdzono podejscie, ostrzezenie, pauze/reset, trafienie od tylu, frontalny blok gracza i rywala oraz brak kopiowania inputu. To nie jest manualna ocena sterowania/balansu w glownym edytorze. Znany blad Unity Search przy starcie pozostaje poza kodem gry.
- Ograniczenia etapu: prosty sparing 2.5D; AI nie omija przeszkod, nie skacze, nie robi combo/jutsu/dasha. Nadal tylko Fight Idle — animacje i modele broni to etap 9.


## 2026-10-17 — etap 9: animacje i bronie, czesc 1
- Za zgoda uzytkownika uzyto lokalnych klipow z Downloads: Stare/Ch45_nonPBR@Walking.fbx, @Running.fbx, @Jumping.fbx, Body Block.fbx, Outward Block.fbx. Kopie sa w Assets/Shinobi/Animation/Imported; import Humanoid, ruch root zablokowany. Oryginaly w Pobranych pozostaja bez zmian.
- Gracz, Player.prefab i rywal maja ShinobiAnimationDriver oraz wspolny YBotAnimations.asset. Sterownik odtwarza klipy Humanoid z plynna zmiana w Playables: idle, chodzenie przy wolnym podejsciu AI, bieg gracza, skok i blok. Opadanie na razie korzysta z tego samego klipu skoku. Animator nadal ma avatar/controller YBota; applyRootMotion=false, ruch pozostaje w CharacterController.
- Wylaczono optimizeGameObjects importera YBot, by udostepnic kosci dloni. Tam, gdzie stara zwinieta instancja nie miala kosci, odbudowano model z tego samego FBX z zachowaniem transformacji, materialow i kontrolera. Referencja health.visual zostala przepieta do nowego renderera. WeaponSocket znajduje sie pod prawa dlonia Humanoid.
- Dwa istniejace wpisy ekwipunku maja edytowalne prefaby modelu: drewniany miecz i kij treningowy z prostych bryl. Sa to modele testowe, bez colliderow; Y zmienia widoczny model, obrazenia nadal liczy resolver walki. Docelowy chwyt i klipy walki bronia wymagaja dalszego dopasowania.
- Klipy ciosow sa odczytywane z ShinobiAttackDefinition.animation aktualnie wyposazonej broni; czas klipu dopasowuje sie do czasu uderzenia. Klip jutsu pochodzi z ShinobiJutsuDefinition.animation. Brak klipu bezpiecznie uzywa idle. To nie oznacza ukonczonych animacji walki.
- Inspector: Player / TrainingOpponent > Shinobi Animation Driver > Animations -> YBotAnimations.asset: idle/walk/run/jump/fall/crouch/crouchWalk/dash/hit/block/guardBreak/focus i crossFade. Ataki: definicje combo w wybranej broni lub profilu; jutsu: definicja w profilu. Bron: Player > Shinobi Weapon Inventory > Weapons > visualPrefab/localPosition/localEulerAngles/localScale. Weapon Socket wskazuje kosc prawej dloni / WeaponSocket.
- Przygotowanie: Shinobi > Etap 9 > Przygotuj animacje i modele treningowe. Narzedzie wymaga juz skopiowanych FBX w Animation/Imported, nie czyta folderu Pobrane. Nie nadpisuje przypisanych klipow ani modeli broni.
- Weryfikacja: import/kompilacja Unity i 129 automatycznych kontroli Play Mode na kopii TrainingArena PASS. Sprawdzono retargetowanie i faktyczny ruch kosci dloni w idle, dobor klipow biegu/skoku/bloku i chodzenia AI, odtwarzanie przypisanego klipu ciosu, socket, zmiane modelu po Y, brak colliderow broni, odtworzenie grafu po wylaczeniu oraz regresje walki/obrony/jutsu/dasha/AI. Obejrzano zrzut Game View biegu z modelem broni. Testy nie zastapia recznej oceny tempa, chwytu i przejsc. Znany wyjatek indeksu Unity Search przy starcie nadal wystepuje poza kodem gry.
- Etap 9 jest w trakcie: brak w dostepnych plikach docelowych klipow kucania, kucanego chodu, dasha, ciosow/combo, trafienia, przelamania i jutsu. Na razie te stany korzystaja z idle; kucanie zachowuje prototypowe zmniejszanie Visual. OutwardBlock jest zaimportowany jako alternatywny klip, domyslnie aktywny jest BodyBlock.


## 2026-10-17 — klasyczny Animator Controller
- Na prosbe uzytkownika zastapiono odtwarzanie przez Playables klasycznym Animator Controllerem: Assets/Shinobi/Animation/YBot/YBotGameplay.controller. Gracz, rywal i Player.prefab korzystaja z tego samego zapisanego grafu. ShinobiAnimationDriver wysyla parametry; nie tworzy juz grafu Playables ani nie uruchamia przejsc przez CrossFade/Play w kodzie.
- Graf zawiera 15 stanow: Idle, Walk, Run, Jump, Fall, Crouch, Crouch Walk, Dash, Hit, Block, Guard Break, Focus, Attack A, Attack B, Jutsu. Warunki Any State -> stan uzywaja parametru int State: kolejno 0–14. Przejscia bez Exit Time, domyslnie 0.1 s, bez przejsc do siebie i bez przerywania trwajacego przejscia. Parametr float AttackSpeed dopasowuje klip ciosu do czasu mechaniki.
- Normalne klipy zmienia sie teraz w oknie Animator: wybierz YBotGameplay.controller, kliknij stan i podmien Motion. Kliknij strzalke, by zmienic Transition Duration/warunki. YBotAnimations.asset sluzy do utworzenia domyslnego grafu i jako zapasowy idle dla nieprzypisanych klipow akcji; nie steruje juz na zywo Motion stanow ruchu. Przykladowe wartosci State widac podczas Play po zaznaczeniu Player/Visual/YBot i otwarciu Window > Animation > Animator.
- Attack A/B i Jutsu maja unikatowe klipy zastepcze AttackSlotA/AttackSlotB/JutsuSlot. Osobny runtime AnimatorOverrideController kazdej postaci podmienia je na klip z definicji aktualnego ciosu lub jutsu. Nie nalezy zastepowac tych slotow w grafie, jesli maja dzialac klipy z ekwipunku; odpowiedni klip przypisuje sie w ShinobiAttackDefinition.animation / ShinobiJutsuDefinition.animation. Dwa stany Attack naprzemiennie rozpoczynaja kolejne ciosy od poczatku.
- Menu Shinobi > Etap 9 > Przygotuj Animator Controller przypina graf do sceny i prefabu. Tworzy graf tylko przy jego braku, zachowujac pozniejsze reczne zmiany stanow/przejsc. Dotychczasowe menu animacji i modeli rowniez przypina nowy graf. Root motion pozostaje wylaczony, bronie/sockety i reguly walki zachowane.
- Weryfikacja: kompilacja/import i 132 kontrole Play Mode na kopii TrainingArena PASS. Sprawdzono zapisany graf/warunki/instancje override, rzeczywisty stan Run Animatora i ruch kosci idle, bieg/skok/blok, klip z definicji ciosu, przejscie do idle, wylaczenie/wlaczenie oraz regresje AI/walki/obrony/ekwipunku/dasha/jutsu. Naprawiono restartowanie przejsc powodowane przerwaniem Any State. Przy bloku kontrola czeka na zakonczenie 0.1 s przejscia. To nie jest reczny test okna Animator w glownym edytorze. Znany wyjatek indeksu Unity Search pozostaje poza kodem gry.
- Etap 9 nadal wymaga brakujacych klipow: kucanie, dash, ciosy, trafienie, przelamanie, jutsu. Odpowiednie stany sa widoczne w grafie, tymczasowo z Motion=idle.


## 2026-10-17 — etap 9: edytor przypisywania animacji
- Dodano Shinobi > Edytor animacji i przycisk Edytuj animacje i sprawdz braki w Inspectorze Shinobi Animation Driver. Okno uzywa kontrolera przypisanego do wybranej postaci, z mozliwoscia wyboru innego assetu.
- Ruch i reakcje: polskie etykiety stanow, przypisanie AnimationClip bezposrednio do Motion w zapisanym Animator Controllerze. Zmiana obsluguje Undo, nie odtwarza grafu ani nie nadpisuje przejsc. Istniejace Blend Tree otwiera sie do edycji w Animatorze.
- Brak przypisania lub uzycie zapasowego Idle w innym stanie jest oznaczone jako do uzupelnienia. Ostrzezenia pomagaja wykryc klip bez Humanoid oraz brak petli dla ciaglego ruchu. Mozna celowo korzystac z tego samego klipu w kilku stanach.
- Po wybraniu broni edytuje sie animacje jej poszczegolnych ciosow. Po wybraniu postaci widoczne sa jej bronie i definicje jutsu; u rywala bez ekwipunku — combo profilu. Klipy zapisywane sa w definicjach, bez zastepowania runtime slotow Attack A/B/Jutsu.
- Inspector pokazuje podczas Play aktualnie odtwarzany klip i wartosc State. Edycja przypisan w oknie jest zablokowana podczas Play Mode; aktywne stany nadal mozna obserwowac w Animatorze.
- Weryfikacja: kompilacja Unity i kontrola zapisu Motion na kopii kontrolera po ponownym imporcie PASS; liczba stanow/przejsc i warunki zachowane, nieistniejacy stan bezpiecznie odrzucony. Zmiana testowa przywrocona. Ta kontrola dotyczy edytora i zapisu assetu; poprzednie 132 kontrole Play Mode pozostaja ostatnim testem mechanik. Bez recznego testu interakcji okna w glownym edytorze.
- Nadal brak dodatkowych plikow animacji ciosow, kucania, dasha, trafienia, przelamania i jutsu w dostepnym zbiorze Pobrane. Etap 9 pozostaje otwarty; nie zastapiono tych brakow nieodpowiednimi klipami.


## 2026-10-17 — etap 10: ilustrowana arena na warstwach uzytkownika
- Uzyto czterech PNG dostarczonych w rozmowie (1672x941, kanal alfa): Foreground, Courtyard, Village, Mountains; JPG Reference zachowano jako wzor. Oryginaly pozostaja bez zmian. Kopie i materialy: Assets/Shinobi/Art/TrainingArena.
- TrainingArena/ArenaPresentation zawiera oddzielne SpriteRenderer: Mountains z=8, Village z=6, Courtyard z=3, Foreground z=-4. Warstwy sa nieruchome, oddzielone w glebi; nie dodano dynamicznej paralaksy. Materialy URP/Unlit z przezroczystoscia i bez zapisu glebi zachowuja kolory ilustracji i odslaniaja postacie 3D.
- Skala: 28 jednostek szerokosci; PPU=1672/28. Wspolny srodek warstw y=3.84. Kamera ortograficzna na (0,3.84,-25), rozmiar ~7.879, projektowany kadr 16:9. W Game View ustaw 16:9 / 1920x1080, aby widziec cala kompozycje. Inny aspekt przycina boki lub pokazuje wiecej szerokosci.
- Stary Ground oraz LeftBoundary/RightBoundary maja ukryte Renderery, zachowane collidery. Srodkowy odcinek pozostaje plaski do walki i cwiczenia dasha. Player, TrainingOpponent i TrainingDummy zachowuja pozycje oraz komponenty mechaniki.
- ArenaPresentation/TrainingObstacles: JumpBlock przy x=10 (wysokosc 0.9 m); CrouchBeam przy x=-10 (dol na 1.35 m), z podporami poza osia ruchu. Sa oddzielnymi obiektami z edytowalnymi Transform i BoxCollider. Przeszkody nie leza na podstawowym odcinku podejscia AI (-6 do 0). AI nadal nie omija przeszkod — przeniesienie sparingu w tor przeszkod moze go zatrzymac.
- TrainingDummy ma drewniany material, ozdobna poprzeczke i tarcze bez dodatkowych colliderow. Jego HP i trafienia nadal dzialaja. Manekin namalowany w ilustracji po prawej to dekoracja, nie cel z HP; interaktywny ma etykiete HP.
- Menu Shinobi > Etap 10 > Przygotuj ilustrowana arene przygotowuje import i tworzy brakujaca oprawe. Ponowne wykonanie zachowuje przesuniete elementy istniejacego ArenaPresentation, nie dodaje duplikatow. Otwieranie istniejacej sceny nie wymaga uruchamiania menu.
- Weryfikacja na kopii TrainingArena: kompilacja/import oraz 140 kontroli Play Mode PASS: 8 kontroli nowej areny i 132 regresje. Potwierdzono cztery warstwy bez colliderow, fizyczne podloze przy ukrytej starej geometrii, dash zatrzymany blokiem, skok nad blokiem, zatrzymanie stojacej postaci pod belka oraz przejscie w kucki bez wstawania w sufit. Obejrzano render Unity 1280x720 z kompozycja i postacia. Kontrole nie zastapuja manualnego testu klawiatury i oceny wygody; znany wyjatek Unity Search przy starcie pozostaje poza kodem gry.
- Podglad: output/arena/TrainingArena-preview.png w folderze roboczym. Etap 9 (brakujace klipy) odlozony zgodnie z decyzja uzytkownika. Etap 10 wykonany w zakresie prototypu; kolejny etap: reakcje i efekty walki.


## 2026-10-17 — etap 11: reakcje wizualne i efekty walki
- Dodano ShinobiCombatFeedback do gracza, rywala, manekina i Player.prefab. Konfiguracja: Assets/Shinobi/Configuration/TrainingFeedback.asset. Inspector aktora > Shinobi Combat Feedback > Profile: osobne kolory trafienia/bloku/przelamania/ciosu/jutsu/dasha, czas efektu, grubosc linii, czas etykiety, Show Effects i Show Labels. Profil mozna zduplikowac i przypisac osobno do aktora.
- Rzeczywista utrata HP wyzwala blysk/rozbrysk i liczbe faktycznie odjetych obrazen (bez zawyzania przy overkill). Frontalny blok pokazuje niebieski okrag i BLOK bez liczby obrazen. Przelamanie gardy pokazuje osobny zloty efekt i PRZELAMANIE; gdy przechodzi tez obrazenie, pojawia sie jego osobne oznaczenie. Przelamanie usuwa poprzedni efekt bloku tej postaci, by nie zaslaniac nowego wyniku.
- Aktywna faza ciosu pokazuje luk, rowniez przy pudle; nie tworzy wtedy efektu trafienia. Rzeczywiste uwolnienie jutsu pokazuje okrag o promieniu obszaru. Udany dash wyzwala krotki plaski impuls za postacia. Zderzenie zaakceptowanej pary atakow pokazuje pojedynczy efekt ZDERZENIE. Nie dodano hit-stop, wstrzasu kamery ani dodatkowego stuna.
- Efekty korzystaja z LineRenderer i unoszacych sie etykiet TextMesh, sa osobnymi obiektami w scenie aktora i usuwaja sie po swoim czasie wraz z materialami runtime. Nie maja colliderow ani wlasnej logiki obrazen. Usuniecie/wylaczenie komponentu feedback nie zmienia mechanik. Przy przypisanym wlasnym effect w definicji ciosu/jutsu zachowano go zamiast domyslnego luku/okregu; feedback trafienia nadal dziala.
- Dodano sygnaly Health.DamageApplied, Defense.Blocked/GuardBreakStarted, Movement.DashStarted, Combat.SwingActivated/Clashed i TechniqueController.TechniqueReleased. Resolver zachowuje deduplikacje trafien i par zderzen; efekty nie rozstrzygaja walki. Dotychczasowe eventy Damaged, flash, przerwania, HP/stamina/chakra, blok frontalny i poziomy pozostaja.
- Menu: Shinobi > Etap 11 > Przygotuj efekty walki. Nie trzeba go uruchamiac dla zaktualizowanej sceny. Ponowne przygotowanie nie duplikuje komponentow i zachowuje przypisany profil. Brakujace animacje z etapu 9 nadal odlozone; te efekty nie zastepuja klipow postaci. Nie dodano audio — brak dedykowanych dzwiekow w tym etapie.
- Weryfikacja: kompilacja/import Unity i 157 kontroli Play Mode na kopii TrainingArena PASS (17 kontroli etapu 11 i 140 regresji). Sprawdzono zgodnosc efektow z prawdziwymi obrazeniami/blokiem/przelamaniem, brak powtorzen przy kolejnych overlapach, pudlo, dash, chakra i jutsu, pojedyncze zderzenie, czyszczenie obiektow, brak colliderow, wypisanie/zapisanie eventow po disable/enable i mozliwosc wyciszenia profilu bez zmiany obrazen. Obejrzano render efektu przelamania i poprawiono rozklad etykiet. Kontrole nie sa manualnym testem klawiatury ani ocena balansu; znany wyjatek indeksu Unity Search przy starcie pozostaje poza kodem gry.
- Podglad: output/feedback/GuardBreak-preview.png w folderze roboczym. Etap 11 zakonczony w zakresie prototypu efektow; kolejny etap 12: menu i petla rozgrywki. Klipy reakcji pozostaja na liscie odlozonych animacji.


## 2026-10-17 — etap 12.1: menu i petla pojedynku
- TrainingArena startuje teraz od menu na tle istniejacej areny: Swobodny trening, Pojedynek z AI, Zakoncz gre. Nie dodano osobnej sceny ani nie zmieniono Build Settings; otwarta TrainingArena zawiera caly przeplyw tego prototypu.
- Swobodny trening przywraca graczowi/AI HP, stamine, chakre, pozycje i akcje oraz poczatkowa bron. Manekin jest aktywny, rywal startuje w pauzie; dotychczasowe przyciski testowe i automatyczne odnowienie HP pozostaja. R zachowuje reset ruchu/chakry/staminy gracza, bez odnawiania HP.
- Pojedynek ustawia postacie naprzeciw siebie, automatycznie uruchamia AI, wylacza automatyczne odnowienie HP obu walczacych i dezaktywuje manekina. Przyciski testowych obrazen/sparingu oraz reset zasobow przez R sa niedostepne. Wynik po utracie HP: ZWYCIESTWO, PORAZKA lub REMIS przy pokonaniu obu w tej samej klatce. Stan jest rozstrzygany po resolverze trafien, wiec prawidlowy trade moze dac remis.
- Esc lub przycisk Pauza zatrzymuje czas, ruch, akcje, AI, regeneracje i timery. Wznowienie przywraca poprzednie tempo. Czesciowo wpisane pieczecie i bufor dwukliku sa czyszczone przy pauzie, zeby wznowienie nie tworzylo przypadkowej akcji. Nie odtwarza sie trafien zgloszonych podczas pauzy. Wykonywana akcja poza Focus zachowuje zamrozony czas.
- Z pauzy lub wyniku mozna zrestartowac trening / wybrac rewanz albo wrocic do menu. Restart zeruje akcje, zasoby, cooldown dasha, pozycje, aktualna bron i oczekujace trafienia oraz usuwa stare efekty. Reset HP przywraca tez material po blysku trafienia. Po wyjsciu lub wylaczeniu sesji przywracany jest pierwotny timeScale i ustawienia autoReset; poprawiono kolejnosc zamykania UIDocument.
- UI Toolkit: GameSession > UIDocument, Assets/Shinobi/UI/GameMenu.uxml (uklad i przyciski), GameMenu.uss (kolory/rozmiary), GameTheme.tss (bazowy motyw). PanelSettings: Configuration/GamePanel.asset, skala do referencji 1280x720. UI mozna otworzyc w UI Builderze. Tytul/opis/wynik zmienia ShinobiGameSession. Pauza jest w prawym dolnym rogu, by nie nakladac sie na prototypowy HUD w waskim Game View.
- Inspector GameSession > Shinobi Game Session: Player, Opponent, Training Dummy. Menu przygotowania: Shinobi > Etap 12 > Przygotuj menu i pojedynek. Ponowne przygotowanie nie tworzy duplikatu sesji ani nie nadpisuje istniejacego PanelSettings. Dla gotowej sceny wystarczy ponowne otwarcie TrainingArena i Play. Zakoncz gre zamyka aplikacje w buildzie, a w edytorze konczy Play Mode.
- Weryfikacja na kopii TrainingArena: kompilacja/import i 178 kontroli Play Mode PASS (21 dla sesji + 157 regresji). Przyciski UI wywolano zdarzeniami nawigacji UI Toolkit: trening, pojedynek, pauza/wznowienie, rewanz, menu. Sprawdzono blokade inputu, zasobow i trafien w pauzie, wyniki przegrana/wygrana/remis, przywracanie zasobow, autoReset w treningu, blokade R w pojedynku, reset Focus, wylaczenie/wlaczenie sesji i przywracanie timeScale. Obejrzano zrzuty menu i zwyciestwa; nie sa to reczne klikniecia myszka ani test spakowanego builda. Znany wyjatek indeksu Unity Search przy starcie nadal wystepuje; brak nowego NullReference przy zamykaniu sesji w koncowej probie.
- Podglady: output/session/Menu-preview.png, Victory-preview.png w folderze roboczym.
- Zakres: zakonczona lokalna petla menu–trening/pojedynek–pauza–wynik–rewanz. Docelowy HUD, cele/poziomy i progresja nadal wymagaja dalszej czesci etapu 12. Pelny ekwipunek/zapis oraz brakujace animacje pozostaja odlozone; nic nie jest jeszcze zapisywane miedzy uruchomieniami gry.


## 2026-10-17 — etap 12.2: kompaktowy HUD
- Wdrozono mniejszy HUD w UI Toolkit, zgodny z zatwierdzonym ukladem: HP/chakra/wytrzymalosc po lewej, cel po prawej, tryb i Esc posrodku u gory; bron, jutsu i dash na dole z duzymi przerwami. Srodek areny pozostaje odsloniety. Wartosci pochodza z komponentow aktorow, nie z obrazu koncepcyjnego.
- GameSession > Shinobi Combat Hud > Hud Scale: 1 odpowiada kompaktowemu ukladowi dla 1280x720 (zakres Inspector 0.7–1.2). Mniejsze proporcje Game View dodatkowo zmniejszaja karty, projektowany kadr pozostaje 16:9. Normalny gracz ma panel 340x98, cel 290x88, bron 246x80, jutsu 328x108 i dash 234x80 w referencji 1280x720.
- GameMenu.uxml zawiera oddzielne elementy paskow, etykiet, ikon i przyciskow. CombatHud.uss definiuje rozmiary, kolory i pozycje. Ramki z drobnymi zlotymi/bordowymi detalami i symbole serca/chakry/wytrzymalosci/pieczeci sa rysowane jako oddzielne elementy Painter2D przez ShinobiHudDrawing. Nie uzyto calej grafiki koncepcyjnej jako interfejsu.
- Portrety i ikony wyrenderowano z rzeczywistych modeli: Assets/Shinobi/Art/Hud (przezroczyste PNG). Player Portrait / Dummy Portrait sa edytowalne na komponencie HUD. W ShinobiWeaponDefinition dodano hudIcon; kazda bron moze miec wlasna grafike. Dwa domyslne wpisy otrzymaly ikony i nazwy Miecz treningowy / Kij treningowy. Nie zmieniono obrazen, combo ani modeli trzymanych w dloni.
- Paski odczytuja aktualne i maksymalne HP, chakre i stamine. Y zmienia nazwe oraz ikone broni. HUD pokazuje pierwszy wpis jutsu z profilu, jego rzeczywisty koszt i sekwencje. Poprawnie wpisana pieczec jest podswietlana; niepasujaca zaznaczana na czerwono. Przy dlugich sekwencjach widoczne jest do 6 pierwszych klawiszy i wielokropek. Focus, wykonywanie jutsu i brak chakry maja odpowiedni tekst stanu.
- Dash pokazuje rzeczywisty koszt ze ShinobiMovement, gotowosc, czas pozostaly do odnowienia albo brak wytrzymalosci/niedostepnosc. Nie dodano nowego klawisza: nadal A A / D D. Dodano odczyty DashStaminaCost/DashReady i EnteredSeals, bez zmiany logiki tych akcji.
- W swobodnym treningu celem jest manekin, a po uruchomieniu sparingu rywal. Pojedynek zawsze pokazuje rywala. HP celu oraz ostrzezenie AI sa odczytywane na zywo. Stare etykiety HP i stanu AI nad postaciami sa ukryte, gdy dziala nowy HUD; liczby obrazen/efekty trafien pozostaja.
- Cwiczenia (lewy dol, nad bronia) rozwijaja panel z przyciskami sparingu, resetu i testow obrony. Panel jest domyslnie zamkniety i niedostepny w pojedynku. W menu, pauzie i na wyniku HUD jest ukryty. Stary ShinobiTrainingHud jest wylaczony; manualne wylaczenie nowego komponentu przywraca fallback, ponowne wlaczenie usuwa duplikat starego panelu.
- Menu przygotowania: Shinobi > Etap 12 > Przygotuj kompaktowy HUD. Dla zaktualizowanej sceny wystarczy ponowne otwarcie TrainingArena i Play, potem wybor treningu/pojedynku. Edycja ukladu w UI Builderze: Assets/Shinobi/UI/GameMenu.uxml; po zmianach UXML uruchom Play ponownie, aby odswiezyc powiazania przyciskow.
- Weryfikacja na kopii TrainingArena: import/kompilacja oraz 203 kontrole Play Mode PASS, lacznie z aktualnymi przypisaniami Animatora. Sprawdzono prawdziwe wartosci i paski zasobow, koszt/cast/podswietlenie pieczeci, zmiane ikony/nazwy po Y, HP i wybor celu, przyciski sparingu/resetu/testow obrony, brak staminy i cooldown dasha, ukrywanie w menu/pauzie/pojedynku, fallback i brak nakladania kart w kadrze. Obejrzano rzeczywisty zrzut Game View 1280x720. Przyciski wywolano zdarzeniami UI Toolkit; nie jest to manualny test myszy ani spakowanego builda. Znany wyjatek Unity Search przy starcie nadal wystepuje poza kodem gry.
- Podglad wdrozenia: output/hud/implemented/HUD-Unity-preview.png w folderze roboczym. Wizualizacje v1/v2 pozostaja osobnymi propozycjami wygladu. Dalsze prace etapu 12: cele treningowe i poziomy; brakujace klipy, pelny ekwipunek i zapis pozostaja odlozone.


- Zachowano nowsze przypisania ruchu w Animator Controllerze zastane podczas wdrazania. Do kopii testowej dolaczono aktualne pliki Move; dodatkowa kontrola potwierdza rozwiazanie wszystkich Motion grafu. Installer HUD-u nie zapisuje kontrolera ani klipow animacji. Pozostale animacje sa poza zakresem tego wdrozenia.


## 2026-10-17 — naprawa podmiany modelu Azure Shadow
Naprawiono prefab Player i TrainingArena: jeden Animator Humanoid na prawidlowym szkielecie AzureShadow_LOD0, wyzerowane przesuniecie modelu, przypisania driver/visual/health/WeaponSocket. Przeciwnik uzywa tej samej nowej postaci, poniewaz poprzednia siatka YBot zostala usunieta. Zachowano istniejace przypisania klipow; tylko brakujace stany oraz puste fallbacki zestawu animacji otrzymaly nowy Idle. Kopia sprzed naprawy: workspace .verification/ModelRepairBaseline/Assets. Unity: kontrola Avatar/siatek/retargetingu i test Play Mode treningu, odtwarzania klipow oraz przesuniecia dash PASS. Fizyczne sterowanie i wyglad do sprawdzenia w otwartym edytorze.

## Modele shurikena i kunai — 17 października 2026

- Dodano modele FBX shurikena, kunai i kunai z wybuchową notką do Assets/Shinobi/Art/Weapons/Models. Źródło Blender i skrypt generujący są w workspace ChatGPT/Shinobi/output/weapons.
- Prefaby z punktem uchwytu, materiały URP/Lit, tekstura notki i ikony HUD są w podfolderach Prefabs, Materials, Icons oraz pliku Seal_BaseColor.png. Wszystkie nowe modele mają UV. Liczba trójkątów: 320 / 3492 / 3860.
- Ekwipunek Player.prefab rozszerzono o trzy definicje: Assets/Shinobi/Configuration/Weapons/Shuriken.asset, Kunai.asset, Kunai_Seal.asset. TrainingArena dziedziczy listę z prefabu; plik sceny nie wymagał zmiany. Kolejność Y: Miecz treningowy → Kij treningowy → Shuriken → Kunai → Kunai z wybuchową notką → miecz. Początkowa broń pozostaje bez zmian.
- Zachowano dotychczasowe dwie definicje broni. Nowe definicje mają niezależne kopie obecnego podstawowego combo; parametry i animacje można edytować bez wpływu na miecz. Nowe modele korzystają z istniejącego WeaponSocket w prawej dłoni. Przesunięcie i obrót są zapisane w definicjach, dostępne w Inspectorze: Local Position, Local Euler Angles, Local Scale. Pivot prefabów kunai jest w środku rękojeści, shurikena w środku otworu.
- Ponowne przygotowanie: Shinobi > Modele > Dodaj shuriken i kunai. Narzędzie dodaje brakujące wpisy bez duplikatów i zachowuje istniejące niezerowe ustawienia montażu. Nie wymaga zmian skryptów runtime.
- Weryfikacja na kopii aktualnego projektu w Unity 6000.6.3f1: import i kompilacja, trzy kompletne definicje/prefaby/ikony, shadery URP i tekstura notki, poprawna skala w metrach, aktywny model w prawej dłoni, przełączanie przez API ekwipunku, usuwanie poprzedniego modelu, powrót do miecza oraz podążanie modelu za animowanym socketem: PASS. Obejrzano rendery wszystkich modeli przy postaci oraz ikony. Raport: workspace .verification/WeaponModelsEvidence/Checks.txt, rendery Held-2/3/4.png i Detail-2/3/4.png.
- Izolowany edytor zgłosił znany ArgumentOutOfRangeException w UnityEditor.Search.SearchDatabase podczas startu; test Play Mode wznowił działanie i przeszedł. Nie jest to błąd skryptów modeli.
- Zakres: widoczne modele wyposażenia w obecnym systemie walki wręcz. Nie dodano rzucania, eksplozji, animacji/fizyki notki ani dopasowywania palców do rękojeści. Nie przeprowadzono fizycznego testu klawiaturą ani pełnego przeglądu montażu we wszystkich animacjach walki.

Po instalacji: Assets > Refresh, ponownie otworzyć TrainingArena, uruchomić trening; nacisnąć Y dwa razy, aby wybrać shuriken, kolejne naciśnięcia wybierają kunai i kunai z notką.


## H — rzut wybraną bronią rzucaną — 17 października 2026

- H rzuca aktualnie wybranym shurikenem, kunai lub kunai z notką, w kierunku zwrócenia postaci. Miecz i kij nie są rzucane. Y nadal wybiera broń. Jedno naciśnięcie H oznacza jeden rzut; trzymanie nie powtarza rzutu. Przy równoczesnym Y i H najpierw zmienia się broń. Udany rzut ma pierwszeństwo przed równoczesnym G.
- Player.prefab zawiera ShinobiThrowController. TrainingArena dziedziczy komponent z prefabu; plik sceny nie został zmieniony. Rzut ma windup i recovery, wspólny czas odnowienia zapobiega obchodzeniu cooldownu przez Y. Podczas animacji rzutu blokowane są zmiana broni, cios, Focus i dash. Rzut nie rozpoczyna się podczas ciosu, Focus/jutsu, dashu, przełamania gardy, po śmierci, w menu ani podczas pauzy. Otrzymanie obrażeń przerywa przygotowanie; już wypuszczony pocisk pozostaje aktywny.
- Użyto istniejącej animacji Y Bot@Throw Object.fbx, zaimportowanej jako Humanoid. ShinobiAnimationDriver obsługuje klip przez istniejące sloty akcji; nie dodano przesunięcia postaci przez root motion. Przy wypuszczeniu znika model trzymany w dłoni, a po recovery ponownie się pojawia. W treningu liczba rzutów jest nieograniczona.
- Inspector: Assets/Shinobi/Configuration/Weapons > Shuriken / Kunai / Kunai_Seal > Throwable oraz Throwing. Parametry: Speed, Range, Damage, Cooldown, Windup, Recovery, Collision Radius, Spin, Explosive, Explosion Radius, Explosion Damage, Animation, Explosion Effect. Ustawienia są kopiowane do konkretnego rzutu, więc zmiana wybranej broni nie zmienia już lecącego pocisku.
- Domyślne parametry: 18 m/s, 15 m zasięgu, promień kolizji 0,07 m, windup 0,24 s i recovery 0,26 s. Shuriken obraca się i zadaje 20 obrażeń; kunai 25. Odnowienie obu wynosi 0,7 s. Kunai z notką wybucha przy trafieniu i zadaje po 40 obrażeń w promieniu 1,5 m, z odnowieniem 1,1 s. Wariant wybuchowy nie dodaje drugiego bezpośredniego trafienia. Zwykłe pociski mogą być blokowane istniejącą gardą, a eksplozja ją omija. Właściciel nie otrzymuje obrażeń od własnego pocisku ani eksplozji.
- Pociski używają sprawdzania całego odcinka lotu (SphereCast), reagują na przeszkody i znikają po trafieniu lub osiągnięciu zasięgu. Eksplozja liczy każdą postać raz, również przy wielu colliderach; ściany zasłaniają cele. Pauza zamraża lot i cooldown. R/reset treningu, restart rundy i powrót do menu usuwają pociski. Dodano miękki efekt cząsteczkowy i teksturę SealSpark.png w Assets/Shinobi/Art/Weapons.
- Przycisk można zmienić w TrainingCombat > Throw Key lub Shinobi > Edytor walki > Sterowanie > Throw Key. HUD wybranej broni rzucanej pokazuje H oraz odnowienie; podpowiedź jest też w sterowaniu menu. Ponowna konfiguracja: Shinobi > Modele > Przygotuj rzut bronia H.

Weryfikacja: import i kompilacja Unity 6000.6.3f1, a następnie 46 automatycznych kontroli Play Mode na kopii aktualnego projektu: PASS. Sprawdzono rzeczywiste zdarzenia klawiatury H przez Input System, brak powtarzania przy trzymaniu, animację Humanoid, HUD, obrażenia 20/25, kierunek w lewo, brak przebijania cienkiej ściany przy 300 m/s, limit zasięgu, eksplozję 40 na wiele celów, brak wielokrotnych obrażeń z wielu colliderów, osłonę ścianą, odporność właściciela, blokady akcji/HP/menu/pauzy, przerwanie przygotowania, zamrożenie i wznowienie lotu, restart i sprzątanie pocisków. Obejrzano render lotu oraz eksplozji. Test wywołuje klawisze programowo; nie wykonano fizycznego testu klawiaturą w głównym edytorze ani pełnego przeglądu wszystkich póz i animacji.

Raport workspace: .verification/ThrowEvidence/Checks.txt. Log: .verification/WeaponModelsUnity/Throw-PlayFinal.log. Znany błąd startowego indeksowania UnityEditor.Search pojawił się w izolowanym edytorze; test wznowił Play Mode i zakończył się kodem 0. Po instalacji: Assets > Refresh, ponownie otworzyć TrainingArena, rozpocząć trening, wybrać broń przez Y i nacisnąć H.

## 2026-10-17 — ataki kontekstowe, rozwinięcia dasha i edytor combo/jutsu

- Dodano niski cios S+G, powtarzalny cios powietrzny G oraz silny cios G podczas dalekiego skoku. Ciosy powietrzne zachowują ruch i grawitację; lądowanie je przerywa. Niski cios utrzymuje kucanie do końca akcji, również po puszczeniu S.
- Podczas dasha nowe S uruchamia bezpieczny kolizyjnie wślizg 2 m / 0,35 s, bez obrażeń i nietykalności. W uruchamia daleki skok około 6 m. Brak dodatkowego kosztu ponad dash. Niski sufit blokuje wstanie po wślizgu. Wszystkie parametry są edytowalne w ShinobiMovement.
- Rzut H działa stojąc, w kucki i w locie. Przy wypuszczeniu celuje w środek collidera aktualnego żywego celu HUD w zasięgu; bez celu poziomo. Pocisk nie śledzi celu. Rzut powietrzny zachowuje ruch; rzut w kucki ma obniżony punkt wypuszczenia i utrzymuje kucanie na ziemi.
- Nowy ShinobiComboDefinition i cztery konteksty w profilach/broniach. Wariant broni ma pierwszeństwo; brak wariantu dziedziczy profil. Starsze tablice są zachowane. Migracja stojącego combo zachowuje dotychczasowe ciosy, czasy i okna; nowe warianty mają osobne definicje.
- Shinobi > Edytor walki > Combo/Jutsu: biblioteka, tworzenie i duplikowanie, kolejność ciosów, edycja parametrów/klipów/efektów, przypisanie do profilu lub broni, odłączanie oraz zapis. Duplikat combo otrzymuje własne kopie ciosów. Undo i blokada edycji podczas Play Mode. Edytor animacji pokazuje nowe konteksty oraz klipy wślizgu/dalekiego skoku.
- Nie dodano przycisku ani ekranu katalogu w menu gry. Lista i instrukcja: COMBO_I_JUTSU.md. Eksport z edytora odświeża tylko znaczniki GENERATED i zachowuje ręczne plany. Kula ognia, Podmuch wiatru i Kamienna osłona to propozycje, bez wykonywalnych definicji.
- Naprawiono istniejące odwołania instalatorów modeli/rzutów do brakującego ShinobiYBotSetup, wykorzystując obecne stałe ShinobiCombatIntegration.
- Weryfikacja Unity 6000.6.3f1 na kopii WeaponModelsUnity: 12 kontroli edytora, 42 kontrole Play Mode nowych akcji i 46 regresji rzutów — PASS. Potwierdzono rzeczywiste obrażenia niskiego ciosu, wykonanie nowego jutsu z własną sekwencją/kosztem/obrażeniami, wielokrotne ciosy w locie, kolizje wślizgu pod sufitem, koszt, dystans skoku, pauzę, przerwania, aim i brak homingu. Osobna kompilacja runtime/Editor oraz testy HFSM na atrapach PASS.
- Źródło testów: Verification~/ContextActionsCheck.cs; raporty workspace .verification/ContextActionsEvidence i ContextThrowRegression.log. Znany wyjątek indeksu Unity Search przy starcie kopii nie zatrzymał testów. Nie wykonano ręcznego sterowania w głównym edytorze ani oceny balansu. Nowe klipy animacji pozostają do dostarczenia; mechaniki korzystają z przypisanych klipów lub animacji zastępczych. Nie rozbudowano zachowania AI ani mapowania nowych gestów na padzie.

Uruchomienie po imporcie: Assets > Refresh, ponownie otwórz TrainingArena i Play. Przy instalacji samych skryptów: Shinobi > Combo i jutsu > Przygotuj warianty i migruj combo. Dostarczone assety konfiguracji mają już wykonaną migrację.




## 2026-10-17 — poprawka animacji ataku i rzutu
- Sterownik aktualizuje animacje w LateUpdate, po mechanice akcji. Każdy nowy cios/rzut odtwarza stan od początku; wspólny licznik slotów obsługuje przejścia między rodzajami akcji. Podmieniany jest tylko klip nowej akcji.
- Uzupełniono puste klipy ciosów dostępnymi Punching L/R; usunięto Throw Object z ciosów Shurikena. Klip rzutu pozostaje osobny. Brakujące klipy kontekstów/broni dziedziczą ciosy profilu.
- Dedykowane animacje niskiego/powietrznego ciosu i silnego ciosu w skoku nadal do dostarczenia; obecne uderzenia są zastępcze.
- Weryfikacja: kompilacja runtime/Editor PASS; 23 kontrole Animatora w Unity 6000.6.3f1 Play Mode PASS na kopii WeaponModelsUnity (wybór klipów, restart, tempo, przejścia cios/rzut i zachowanie poprzedniego slotu). Test wywołuje sterownik programowo, nie potwierdza ręcznej gry ani wyglądu modelu. Raport: .verification/ActionAnimationResults.txt, log ActionAnimationRetry.log w workspace ChatGPT/Shinobi.

