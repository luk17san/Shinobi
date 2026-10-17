using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Shinobi.Editor
{
    public sealed class ShinobiCombatEditor : EditorWindow
    {
        private ShinobiCombatProfile profile;
        private GameObject character;
        private UnityEditor.Editor detailEditor;
        private Object detail;
        private int tab;
        private Vector2 scroll;
        private readonly ShinobiMoveCatalogGUI catalog = new ShinobiMoveCatalogGUI();
        [MenuItem("Shinobi/Edytor walki")]
        public static void Open() { GetWindow<ShinobiCombatEditor>("Edytor walki"); }
        private void OnDisable() { if (detailEditor != null) DestroyImmediate(detailEditor); catalog.Dispose(); AssetDatabase.SaveAssets(); }
        private void OnGUI() {
            scroll = EditorGUILayout.BeginScrollView(scroll);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode)) {
                profile = (ShinobiCombatProfile)EditorGUILayout.ObjectField("Zestaw walki", profile, typeof(ShinobiCombatProfile), false);
                character = (GameObject)EditorGUILayout.ObjectField("Postac", character, typeof(GameObject), true);
                if (GUILayout.Button("Utworz przykladowy zestaw")) CreateExample();
                if (profile != null) {
                    tab = GUILayout.Toolbar(tab, new[] { "Sterowanie", "Ataki wrecz", "Combo", "Jutsu", "Obrona" });
                    var data = new SerializedObject(profile); data.Update();
                    if (tab == 0) {
                        Field(data, "meleeKey"); Field(data, "focusKey"); Field(data, "jumpKey"); Field(data, "crouchKey"); Field(data, "nextWeaponKey"); Field(data, "throwKey"); Field(data, "dashDoubleTapWindow");
                        EditorGUILayout.HelpBox("Dash: dwa szybkie nacisniecia A albo D. Okno dwukliku jest w sekundach. Podczas Focus nacisniecia nie sa zapamietywane jako dash.", MessageType.Info);
                        EditorGUILayout.HelpBox("Focus: W/D/S/A wprowadzaja pieczecie gora/prawo/dol/lewo. Puszczenie Focus wykonuje pasujace jutsu. Kazda pieczec wymaga nowego nacisniecia.", MessageType.Info);
                    }
                    if (tab == 1 || tab == 2) {
                        if (profile.contextCombos == null || profile.contextCombos.standing == null) { Field(data, "comboWindow"); Field(data, "combo"); }
                        else EditorGUILayout.HelpBox("Profil używa definicji combo. Edytuj serie i ciosy w zakładce Combo. Stare tablice zachowano dla zgodności.", MessageType.Info);
                        EditorGUILayout.HelpBox("Okno kontynuacji liczone od poczatku uderzenia. Nacisniecie jest buforowane do jego zakonczenia. Brak nacisniecia resetuje combo.", MessageType.Info);
                    }
                    if (tab == 2 || tab == 3) {
                        EditorGUILayout.LabelField("Zderzenia atakow", EditorStyles.boldLabel);
                        Field(data, "interruptChance"); Field(data, "chancePerLevel");
                        Field(data, "maximumInterruptChance"); Field(data, "drawSeparation");
                        EditorGUILayout.HelpBox("Szansa pochodzi z profilu silniejszego ataku. Remis anuluje obie akcje bez obrazen. Dystans remisu to wieksza wartosc obu profili.", MessageType.Info);
                    }
                    if (tab == 3) { Field(data, "maxChakra"); Field(data, "jutsu"); }
                    if (tab == 4) {
                        Field(data, "autoBlock"); Field(data, "maxStamina"); Field(data, "blockCostMultiplier");
                        Field(data, "staminaRegenRate"); Field(data, "staminaRegenDelay"); Field(data, "guardBreakDuration"); Field(data, "blockHalfAngle");
                        EditorGUILayout.HelpBox("Automatyczny blok chroni z przodu na ziemi poza atakiem i jutsu. Jutsu i obrazenia rozstrzygnietego zderzenia omijaja garde. Za mala wytrzymalosc przelamuje blok.", MessageType.Info);
                    }
                    data.ApplyModifiedProperties();
                    if (tab == 2 || tab == 3) catalog.Draw(profile, tab == 3);
                    if (tab == 1 && profile.combo != null) foreach (var attack in profile.combo) SelectDetail(attack);
                    if (tab == 3 && profile.jutsu != null) foreach (var technique in profile.jutsu) SelectDetail(technique);
                    detail = EditorGUILayout.ObjectField("Edytowana definicja", detail, typeof(ScriptableObject), false);
                    if (detail is ShinobiAttackDefinition || detail is ShinobiJutsuDefinition) {
                        UnityEditor.Editor.CreateCachedEditor(detail, null, ref detailEditor);
                        detailEditor.OnInspectorGUI();
                    }
                    if (GUILayout.Button("Utworz atak")) detail = CreateAsset<ShinobiAttackDefinition>("NowyAtak");
                    if (GUILayout.Button("Utworz jutsu")) detail = CreateAsset<ShinobiJutsuDefinition>("NoweJutsu");
                    ValidateProfile();
                    EditorGUILayout.HelpBox("Jutsu wykonuje trafienie obszarowe. Poziomy rozstrzygaja zderzenia aktywnych obszarow atakow. Klipy animacji i regeneracja chakry wymagaja kolejnego etapu.", MessageType.Info);
                    if (character != null) {
                        if (character.GetComponent<ShinobiMovement>() == null) EditorGUILayout.HelpBox("Brak ShinobiMovement: przypisanie nie utworzy kontrolera ruchu.", MessageType.Warning);
                        if (character.GetComponent<ShinobiCombat>() == null || character.GetComponent<ShinobiTechniqueController>() == null || character.GetComponent<ShinobiHealth>() == null)
                            EditorGUILayout.HelpBox("Brakujace komponenty walki zostana dodane przy przypisaniu.", MessageType.Warning);
                        if (EditorUtility.IsPersistent(character)) EditorGUILayout.HelpBox("Otworz prefab w Prefab Mode lub wybierz postac na scenie.", MessageType.Warning);
                        else if (GUILayout.Button("Przypisz zestaw i dodaj komponenty walki")) Attach();
                    }
                }
            }
            EditorGUILayout.EndScrollView();
        }
        private static void Field(SerializedObject data, string name) { EditorGUILayout.PropertyField(data.FindProperty(name), true); }
        private void SelectDetail(Object value) { if (value != null && GUILayout.Button("Edytuj: " + value.name)) detail = value; }
        private static T CreateAsset<T>(string name) where T : ScriptableObject {
            string path = EditorUtility.SaveFilePanelInProject("Zapisz definicje", name, "asset", "Wybierz folder konfiguracji.");
            if (string.IsNullOrEmpty(path)) return null;
            var asset = CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }
        private void CreateExample() {
            var created = CreateAsset<ShinobiCombatProfile>("ZestawWalki");
            if (created == null) return;
            profile = created;
            // Sub-assets keep the sample self-contained without overwriting existing definitions.
            for (int i = 0; i < 3; i++) {
                var attack = CreateInstance<ShinobiAttackDefinition>(); attack.name = "Uderzenie " + (i + 1);
                attack.attack.damage = 20f + i * 5f; attack.attack.pushDistance = i == 2 ? 1.5f : 0f;
                AssetDatabase.AddObjectToAsset(attack, profile); profile.combo[i] = attack;
            }
            var jutsu = CreateInstance<ShinobiJutsuDefinition>(); jutsu.name = "Fala treningowa";
            AssetDatabase.AddObjectToAsset(jutsu, profile); profile.jutsu = new[] { jutsu };
            EditorUtility.SetDirty(profile); AssetDatabase.SaveAssets();
        }
        private void Attach() {
            Undo.SetCurrentGroupName("Przypisz zestaw walki");
            var combat = character.GetComponent<ShinobiCombat>() ?? Undo.AddComponent<ShinobiCombat>(character);
            if (character.GetComponent<ShinobiHealth>() == null) Undo.AddComponent<ShinobiHealth>(character);
            if (character.GetComponent<ShinobiTechniqueController>() == null) Undo.AddComponent<ShinobiTechniqueController>(character);
            Undo.RecordObject(combat, "Przypisz zestaw walki"); combat.profile = profile;
            PrefabUtility.RecordPrefabInstancePropertyModifications(combat); EditorUtility.SetDirty(combat);
        }
        private void ValidateProfile() {
            if (profile.maximumInterruptChance < profile.interruptChance)
                EditorGUILayout.HelpBox("Limit szansy jest mniejszy od wartosci poczatkowej: szansa zostanie ograniczona do limitu.", MessageType.Warning);
            var keys = new HashSet<Key>();
            foreach (var key in new[] { profile.meleeKey, profile.focusKey, profile.jumpKey, profile.crouchKey, profile.nextWeaponKey, profile.throwKey })
                if (key == Key.None || !keys.Add(key)) EditorGUILayout.HelpBox("Pusty lub powtorzony przycisk sterowania.", MessageType.Error);
            foreach (var key in new[] { profile.meleeKey, profile.focusKey, profile.nextWeaponKey, profile.throwKey })
                if (key == Key.W || key == Key.S || key == Key.R || key == Key.A || key == Key.D)
                    EditorGUILayout.HelpBox("Przycisk ataku/Focus koliduje z ruchem, pieczeciami lub resetem.", MessageType.Error);
            if (profile.combo == null || profile.combo.Length == 0) EditorGUILayout.HelpBox("Brak combo: kontroler uzyje konfiguracji zapasowej.", MessageType.Warning);
            else foreach (var attack in profile.combo) if (attack == null || attack.attack == null) EditorGUILayout.HelpBox("Niekompletna definicja ataku w combo.", MessageType.Error);
            var sequences = new HashSet<string>();
            if (profile.jutsu != null) foreach (var technique in profile.jutsu) {
                if (technique == null || technique.sequence == null || technique.sequence.Length == 0) { EditorGUILayout.HelpBox("Brak jutsu lub sekwencji pieczeci.", MessageType.Error); continue; }
                if (!sequences.Add(string.Join(",", technique.sequence))) EditorGUILayout.HelpBox("Powtorzona sekwencja jutsu: wykonanie zostanie odrzucone.", MessageType.Error);
                if (technique.sequence.Length > 32 || technique.chakraCost > profile.maxChakra) EditorGUILayout.HelpBox("Jutsu przekracza limit 32 pieczeci lub dostepna chakre.", MessageType.Error);
            }
        }
    }
}
