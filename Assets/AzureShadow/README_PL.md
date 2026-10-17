# Azure Shadow Shinobi — lżejsza siatka i rig

## Zawartość

| Wariant | Trójkąty | Wierzchołki siatki w Blenderze |
| --- | ---: | ---: |
| LOD0 — główny | 45 000 | 22 429 |
| LOD1 — średnia odległość | 18 000 | 8 929 |
| LOD2 — duża odległość | 8 000 | 3 929 |

Oryginał miał 1 115 490 trójkątów. LOD0 ma około 96% mniej trójkątów. Unity może podzielić wierzchołki na granicach UV i normalnych, więc jego licznik wierzchołków może być wyższy.

- `Blender/AzureShadow_Rigged.blend` — edytowalny model, trzy siatki LOD, szkielet FK, spakowane tekstury i akcja kontrolna.
- `Unity/AzureShadow/Models/AzureShadow_LOD0.fbx` — główny model ze szkieletem i wagami.
- `Unity/AzureShadow/Models/AzureShadow_LOD1.fbx` oraz `AzureShadow_LOD2.fbx` — lżejsze warianty na tym samym szkielecie.
- `Unity/AzureShadow/Models/AzureShadow_RigCheck.fbx` — krótka animacja do sprawdzania deformacji. To test, nie gotowy chód lub bieg.
- `Unity/AzureShadow/Textures` — kolor, normal map oraz nieużywana źródłowa tekstura spakowana.
- `Unity/AzureShadow/Editor/AzureShadowSetup.cs` — polecenie tworzące materiał, Avatar Humanoid, LODGroup i dwa prefaby w Unity.
- `Preview` — rendery z Blendera.
- `Validation` — wyniki kontroli siatki, wag, ponownego importu FBX i status weryfikacji Unity.

## Import do Unity

1. Skopiuj **cały folder `Unity/AzureShadow` do folderu `Assets` swojego projektu**. Wynikowa ścieżka musi być `Assets/AzureShadow`.
2. Poczekaj, aż Unity zakończy import i kompilację skryptu edytora.
3. Wybierz **Tools → Azure Shadow → Build or refresh prefab**.
4. Skrypt utworzy materiał pasujący do aktywnego render pipeline, skonfiguruje szkielet jako Humanoid i połączy trzy LOD-y ze wspólnym szkieletem.
5. Przeciągnij wygenerowany **`Assets/AzureShadow/Prefabs/AzureShadow.prefab`** na scenę. Główny prefab ma Animator i Avatar, ale nie ma kontrolera ruchu ani domyślnej animacji.
6. Do krótkiej próby wybierz **`AzureShadow_RigCheck.prefab`** i uruchom Play Mode. Ten prefab odtwarza animację kontrolną. Zastąp jego Animator Controller własnym kontrolerem, gdy będziesz dodawać animacje gry.

Polecenie modyfikuje importery i wygenerowane zasoby wyłącznie wewnątrz `Assets/AzureShadow`. Ponowne użycie odświeża tam materiał, ustawienia modeli, kontroler testowy i prefaby; nie uruchamiaj go po własnych zmianach tych wygenerowanych zasobów, jeżeli chcesz je zachować. Własne warianty prefabów i kontrolery zapisz pod innymi nazwami.

Prefabów nie wygenerowano w tej paczce: lokalny edytor Unity zatrzymał się przed importem na komunikacji z klientem licencji. Powyższe polecenie wykona generowanie w Twoim działającym edytorze. Skrypt został skompilowany względem bibliotek Unity 6000.6.3f1, ale jego wykonanie w Unity pozostaje do sprawdzenia.

## Ręczny import FBX

Jeśli chcesz użyć samego modelu, skopiuj `Models` i `Textures`, zachowując ich wzajemne położenie.

- Zaznacz `AzureShadow_LOD0.fbx` w Project.
- **Model → Scale Factor: 1**. Przy standardowym przeliczeniu jednostek wysokość wynosi około **1,90 m**.
- **Rig → Animation Type: Humanoid**.
- **Avatar Definition: Create From This Model → Apply → Configure**. Sprawdź przypisanie kości; ich nazwy odpowiadają rolom Unity, np. Hips, Spine, LeftUpperArm, LeftLowerLeg.
- Jeśli potrzebujesz własnych kości szala w hierarchii, pozostaw **Optimize Game Objects** wyłączone.
- Używaj tylko jednej siatki LOD naraz albo dodaj LODGroup. Trzy FBX-y przeciągnięte jednocześnie na scenę nie przełączają się automatycznie.

Materiał URP: shader `Universal Render Pipeline/Lit`, Base Map = `AzureShadow_BaseColor.jpg`, Normal Map = `AzureShadow_Normal.jpg`. Materiał Built-in: shader `Standard`, Albedo = tekstura koloru. Teksturę normalnych importuj jako **Normal map**, bez generowania z obrazu w skali szarości. Kolor ma włączone sRGB. Punkt wyjścia: Metallic 0, Smoothness 0.35, siła normal map 0.5.

`Source_Packed_Unverified.jpg` nie jest podłączona: źródłowy FBX nie opisuje kanałów tej tekstury. Nie przypisuj jej automatycznie do Metallic.

## Edycja w Blenderze

Otwórz `Blender/AzureShadow_Rigged.blend`. Tekstury są spakowane wewnątrz pliku. Widoczny jest LOD0; LOD1 i LOD2 są ukryte, aby siatki nie nakładały się na siebie.

- Szkielet: `AzureShadow_Rig`, **56 kości**, w tym kości ciała, po trzy kości każdego palca dłoni, palce stóp i trzy kości szala.
- Bind pose: **T-pose**.
- Wagi: automatyczne bone heat, znormalizowane, maksymalnie **4 kości na wierzchołek**, bez nieprzypisanych wierzchołków.
- W Pose Mode obracaj kości FK. Root steruje całą postacią; Hips miednicą; Scarf01–03 szalem.
- Akcja `Rig_Check_NOT_Walk` znajduje się w danych pliku. Wybierz ją w Action Editor, aby odtworzyć próbę. Jest odłączona od szkieletu przy otwieraniu pliku, więc model startuje w pozie T.

Poprawiono odwrócenie osi V w UV, które powodowało błędne nakładanie oryginalnych JPG. Korekta jest zapisana w siatkach.

## Zakres sprawdzenia i ograniczenia

Potwierdzone w Blenderze 3.6.1: liczby trójkątów, 56 kości, maksymalnie 4 wpływy na każdy wierzchołek, sumy wag równe 1 w granicach dokładności numerycznej, brak nieprzypisanych wierzchołków. Ponowny import wszystkich trzech FBX zachował szkielet, modyfikator skóry, UV, wagi i rozmiar około 1,90 m. Obejrzano rendery pozy T, zgięcia kończyn i palców.

To uproszczona triangulowana siatka źródłowa, nie ręczna retopologia z nowymi pętlami czworokątów. Podstawowe deformacje są przygotowane; skrajne pozy i docelowe animacje mogą wymagać korekt wag. Nie dodano mimiki, blendshape'ów, solvera IK, kolizji ubrania ani symulacji szala. Kości szala można animować ręcznie lub później połączyć z systemem fizyki.

Import, Avatar Humanoid, przełączanie LOD i Play Mode w Unity **nie zostały potwierdzone**, ponieważ edytor nie połączył się z klientem licencji. Nie zmieniano Twojego projektu gry.
