# Etap 4 — integracja z TrainingArena i YBotem

Przygotowano integracje nowszego kodu Stage1 z aktualnymi plikami projektu `Documents/GitHub/Shinobi`. Zachowano YBota, Animator Fight Idle, transformy modelu, CharacterController, kamere, geometrie areny i TrainingDummy.

W Unity 6000.6.3f1 na kopii tej sceny wykonano 28 kontroli: wszystkie PASS. Obejmowaly zapisane komponenty sceny/prefabu, zachowanie obrazen 20/25/35, ruch/obrot/skok/ladowanie, trzy osobne nacisniecia G (HP 100 -> 20), brak powtarzania przy trzymaniu G, poprawne jutsu (30 obrazen / 20 chakry), blokade ruchu i skoku podczas pieczeci, bledna sekwencje, brak chakry, przerwanie Focus oraz rzucania obrazeniami, brak zwrotu chakry, reset R i ciaglosc idle po walce.

Zderzenia sprawdzono na graczu ze sceny i drugiej instancji faktycznego prefabu Player z YBotem: remis bez obrazen i rozdzielenie postaci, przewaga poziomu przy szansie 100% oraz wymiana obrazen przy szansie 0%. Druga postac jest tworzona tylko przez test, nie zostala zapisana w arenie. Testy zderzen korzystaja z publicznych metod walki i rzeczywistego LateUpdate, nie z AI.

Sprawdzono obraz Game View z panelem treningowym przy waskim oknie. Panel zawiera HP, chakre, sterowanie, przykladowe jutsu, pieczecie oraz stan akcji. To tymczasowy interfejs treningowy IMGUI, nie docelowy HUD gry. Brak manualnego testu fizyczna klawiatura/mysza w otwartym glownym projekcie. Nie sprawdzono tutaj zderzen jutsu/jutsu ani jutsu/wrecz, multiplayera, Undo i pelnej obslugi okna edytora walki.

Izolowany edytor zglosil znany blad wlasnego indeksu Search przy starcie. Automat wznowil Play Mode po pauzie konsoli; kompilacja i wszystkie kontrole integracji przeszly. Raport nie oznacza, ze konsola byla calkowicie pozbawiona komunikatow Unity.

Artefakty workspace: `.verification/YBotUnity/CombatIntegrationVerification.txt`, `CombatIntegration-Game.png`, `Combat-Setup.log`, `Combat-Verify.log`. Skrypt uruchomienia: `.verification/RunCombatIntegration.ps1`. Zrodlo testu: `.verification/YBotUnity/Assets/Shinobi/Editor/CombatIntegrationVerification.cs`; nie jest instalowane w glownym projekcie.
