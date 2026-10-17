using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Shinobi.Editor
{
    public sealed class ShinobiMoveCatalogGUI
    {
        private ShinobiComboDefinition combo;
        private ShinobiJutsuDefinition jutsu;
        private ShinobiWeaponDefinition weapon;
        private UnityEditor.Editor editor;
        private ShinobiAttackContext context;
        public void Dispose() { if (editor != null) UnityEngine.Object.DestroyImmediate(editor); }
        public void Draw(ShinobiCombatProfile profile, bool techniques)
        {
            if (GUILayout.Button("Eksportuj listę do Markdown")) ShinobiMoveCatalog.ExportDialog();
            if (techniques) { DrawJutsu(profile); return; }
            weapon = (ShinobiWeaponDefinition)EditorGUILayout.ObjectField("Broń (puste = profil)", weapon, typeof(ShinobiWeaponDefinition), false);
            context = (ShinobiAttackContext)EditorGUILayout.EnumPopup("Kontekst", context);
            UnityEngine.Object owner = weapon != null ? (UnityEngine.Object)weapon : profile;
            var data = new SerializedObject(owner); data.Update();
            var slot = data.FindProperty("contextCombos").FindPropertyRelative(new[] { "standing", "crouching", "airborne", "longJump" }[(int)context]);
            EditorGUILayout.PropertyField(slot, new GUIContent("Przypisane combo")); data.ApplyModifiedProperties();
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Edytuj przypisane")) combo = slot.objectReferenceValue as ShinobiComboDefinition;
                if (GUILayout.Button("Odłącz")) { slot.objectReferenceValue = null; data.ApplyModifiedProperties(); }
            }
            EditorGUILayout.LabelField("Biblioteka combo", EditorStyles.boldLabel);
            foreach (var entry in ShinobiMoveCatalog.All<ShinobiComboDefinition>())
                if (GUILayout.Button(entry.displayName + " (" + entry.name + ")")) combo = entry;
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Utwórz combo")) combo = ShinobiMoveCatalog.Create<ShinobiComboDefinition>("Combo", "NoweCombo");
                using (new EditorGUI.DisabledScope(combo == null)) {
                    if (GUILayout.Button("Duplikuj")) combo = ShinobiMoveCatalog.Duplicate(combo);
                    if (GUILayout.Button("Przypisz wybrane")) { slot.objectReferenceValue = combo; data.ApplyModifiedProperties(); }
                }
            }
            if (GUILayout.Button("Migruj starsze combo i dodaj brakujące warianty")) ShinobiMoveCatalog.Setup();
            if (combo == null) return;
            Inspect(combo);
            if (!combo.IsUsable) EditorGUILayout.HelpBox("Combo musi mieć co najmniej jeden poprawny cios.", MessageType.Error);
            if (GUILayout.Button("Dodaj nowy cios do combo")) {
                Undo.RecordObject(combo, "Dodaj cios");
                var attack = ScriptableObject.CreateInstance<ShinobiAttackDefinition>(); attack.name = "Cios " + (combo.attacks.Length + 1);
                AssetDatabase.AddObjectToAsset(attack, combo); Undo.RegisterCreatedObjectUndo(attack, "Utwórz cios");
                combo.attacks = combo.attacks.Concat(new[] { attack }).ToArray(); EditorUtility.SetDirty(combo); AssetDatabase.SaveAssets();
            }
            EditorGUILayout.HelpBox("Lista Attacks obsługuje kolejność i przypisanie istniejących ciosów. Usunięcie wpisu nie kasuje assetu.", MessageType.Info);
            for (int i = 0; i < combo.attacks.Length; i++) {
                using (new EditorGUILayout.HorizontalScope()) {
                    EditorGUILayout.LabelField("Cios " + (i + 1), GUILayout.Width(65));
                    using (new EditorGUI.DisabledScope(i == 0)) if (GUILayout.Button("↑", GUILayout.Width(35))) Move(i, i - 1);
                    using (new EditorGUI.DisabledScope(i == combo.attacks.Length - 1)) if (GUILayout.Button("↓", GUILayout.Width(35))) Move(i, i + 1);
                    if (GUILayout.Button("Odłącz cios")) {
                        Undo.RecordObject(combo, "Odłącz cios"); combo.attacks = combo.attacks.Where((a, index) => index != i).ToArray(); EditorUtility.SetDirty(combo); break;
                    }
                }
            }
            foreach (var attack in combo.attacks ?? new ShinobiAttackDefinition[0]) {
                if (attack == null) continue;
                EditorGUILayout.LabelField(attack.name, EditorStyles.boldLabel);
                var definition = new SerializedObject(attack); definition.Update();
                var iterator = definition.GetIterator(); bool children = true;
                while (iterator.NextVisible(children)) { children = false; if (iterator.name != "m_Script") EditorGUILayout.PropertyField(iterator, true); }
                definition.ApplyModifiedProperties();
                if (attack.animation == null) EditorGUILayout.HelpBox("Brak klipu — używana animacja zastępcza.", MessageType.Warning);
            }
            if (GUILayout.Button("Usuń przypisanie tego combo")) { slot.objectReferenceValue = null; data.ApplyModifiedProperties(); }
        }
        private void DrawJutsu(ShinobiCombatProfile profile)
        {
            EditorGUILayout.LabelField("Biblioteka jutsu", EditorStyles.boldLabel);
            foreach (var entry in ShinobiMoveCatalog.All<ShinobiJutsuDefinition>())
                if (GUILayout.Button(string.IsNullOrEmpty(entry.displayName) ? entry.name : entry.displayName)) jutsu = entry;
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Utwórz jutsu")) jutsu = ShinobiMoveCatalog.Create<ShinobiJutsuDefinition>("Jutsu", "NoweJutsu");
                using (new EditorGUI.DisabledScope(jutsu == null)) {
                    if (GUILayout.Button("Duplikuj jutsu")) {
                        var copy = ShinobiMoveCatalog.Create<ShinobiJutsuDefinition>("Jutsu", jutsu.name + " kopia");
                        EditorUtility.CopySerialized(jutsu, copy); copy.name = jutsu.name + " kopia"; jutsu = copy; EditorUtility.SetDirty(copy);
                    }
                }
            }
            if (jutsu == null) return;
            Inspect(jutsu);
            using (new EditorGUILayout.HorizontalScope()) {
                if (GUILayout.Button("Dodaj do profilu")) {
                    Undo.RecordObject(profile, "Przypisz jutsu");
                    profile.jutsu = (profile.jutsu ?? new ShinobiJutsuDefinition[0]).Concat(new[] { jutsu }).Distinct().ToArray(); EditorUtility.SetDirty(profile);
                }
                if (GUILayout.Button("Usuń z profilu")) {
                    Undo.RecordObject(profile, "Odłącz jutsu"); profile.jutsu = profile.jutsu.Where(j => j != jutsu).ToArray(); EditorUtility.SetDirty(profile);
                }
            }
            if (jutsu.animation == null) EditorGUILayout.HelpBox("Brak animacji jutsu.", MessageType.Warning);
            EditorGUILayout.HelpBox("Działanie: trafienie obszarowe przed postacią. Inne rodzaje technik wymagają implementacji mechaniki.", MessageType.Info);
        }
        private void Inspect(ScriptableObject asset)
        {
            UnityEditor.Editor.CreateCachedEditor(asset, null, ref editor); editor.OnInspectorGUI();
            if (GUILayout.Button("Zapisz konfigurację")) AssetDatabase.SaveAssets();
        }
        private void Move(int from, int to) {
            Undo.RecordObject(combo, "Kolejność ciosów");
            var value = combo.attacks[from]; combo.attacks[from] = combo.attacks[to]; combo.attacks[to] = value; EditorUtility.SetDirty(combo);
        }
    }
}
