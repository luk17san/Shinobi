using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using UnityEditor;
using UnityEngine;

namespace Shinobi.Editor
{
    // Asset operations are shared by the window, setup command and verification.
    public static class ShinobiMoveCatalog
    {
        public const string Root = "Assets/Shinobi/Configuration/";
        public const string Begin = "<!-- GENERATED:BEGIN -->", End = "<!-- GENERATED:END -->";
        public static T[] All<T>() where T : UnityEngine.Object => AssetDatabase.FindAssets("t:" + typeof(T).Name)
            .SelectMany(g => AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(g)).OfType<T>()).Distinct().OrderBy(a => a.name).ToArray();
        public static T Create<T>(string folder, string name) where T : ScriptableObject
        {
            Directory.CreateDirectory(Root + folder); AssetDatabase.Refresh();
            var asset = ScriptableObject.CreateInstance<T>(); asset.name = name;
            AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(Root + folder + "/" + name + ".asset"));
            Undo.RegisterCreatedObjectUndo(asset, "Utworz " + name); return asset;
        }
        public static ShinobiComboDefinition Duplicate(ShinobiComboDefinition source)
        {
            var copy = Create<ShinobiComboDefinition>("Combo", source.name + " kopia");
            EditorUtility.CopySerialized(source, copy); copy.name = source.name + " kopia"; copy.displayName += " kopia";
            copy.attacks = source.attacks.Select(a => {
                if (a == null) return null;
                var clone = UnityEngine.Object.Instantiate(a); clone.name = a.name;
                AssetDatabase.AddObjectToAsset(clone, copy); Undo.RegisterCreatedObjectUndo(clone, "Duplikuj cios"); return clone;
            }).ToArray();
            EditorUtility.SetDirty(copy); AssetDatabase.SaveAssets(); return copy;
        }
        public static ShinobiComboDefinition FromLegacy(string name, ShinobiAttackDefinition[] attacks, float window)
        {
            var result = Create<ShinobiComboDefinition>("Combo", name);
            result.displayName = name; result.comboWindow = window; result.attacks = attacks ?? new ShinobiAttackDefinition[0];
            EditorUtility.SetDirty(result); return result;
        }
        [MenuItem("Shinobi/Combo i jutsu/Przygotuj warianty i migruj combo")]
        public static void Setup()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Zatrzymaj Play Mode przed migracją combo.");
            foreach (var profile in All<ShinobiCombatProfile>()) {
                Undo.RecordObject(profile, "Przygotuj combo");
                if (profile.contextCombos == null) profile.contextCombos = new ShinobiContextCombos();
                var slots = profile.contextCombos;
                if (slots.standing == null && profile.combo != null && profile.combo.Length > 0)
                    slots.standing = FromLegacy(profile.name + " stojac", profile.combo, profile.comboWindow);
                if (slots.crouching == null) slots.crouching = Variant(profile, "Niski cios", false, true);
                if (slots.airborne == null) slots.airborne = Variant(profile, "Cios powietrzny", false, false);
                if (slots.longJump == null) slots.longJump = Variant(profile, "Silny cios w dalekim skoku", true, false);
                EditorUtility.SetDirty(profile);
            }
            foreach (var weapon in All<ShinobiWeaponDefinition>()) {
                Undo.RecordObject(weapon, "Migruj combo broni");
                if (weapon.contextCombos == null) weapon.contextCombos = new ShinobiContextCombos();
                if (weapon.contextCombos.standing == null && weapon.combo != null && weapon.combo.Length > 0)
                    weapon.contextCombos.standing = FromLegacy(weapon.name + " stojac", weapon.combo, weapon.comboWindow);
                EditorUtility.SetDirty(weapon);
            }
            AssetDatabase.SaveAssets();
        }
        private static ShinobiComboDefinition Variant(ShinobiCombatProfile profile, string name, bool heavy, bool low)
        {
            var combo = FromLegacy(profile.name + " " + name, null, profile.comboWindow);
            var definition = ScriptableObject.CreateInstance<ShinobiAttackDefinition>(); definition.name = name;
            var source = profile.combo != null && profile.combo.Length > 0 ? profile.combo[heavy ? profile.combo.Length - 1 : 0] : null;
            definition.attack = source != null ? source.attack.Snapshot() : new ShinobiAttack { damage = heavy ? 35 : 20 };
            // Missing contextual clips remain visibly unassigned instead of reusing an unsuitable standing clip.
            if (low) { definition.attack.hitCenterHeight = .6f; definition.attack.hitHeight = .6f; }
            if (heavy) definition.attack.pushDistance = 1.5f;
            AssetDatabase.AddObjectToAsset(definition, combo); combo.attacks = new[] { definition };
            EditorUtility.SetDirty(combo); return combo;
        }
        private static string N(float value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        private static string Safe(string text) => (text ?? "").Replace("|", "/").Replace("\r", " ").Replace("\n", " ");
        private static string Keys(ShinobiSeal[] seals) => seals == null ? "brak" : string.Join(" → ", seals.Select(s => s == ShinobiSeal.Up ? "W" : s == ShinobiSeal.Down ? "S" : s == ShinobiSeal.Left ? "A" : "D"));
        public static string Generate()
        {
            var text = new StringBuilder("## Dostępne konfiguracje\n\nEksport: " + DateTime.Now.ToString("yyyy-MM-dd") + ". Status: **do weryfikacji w głównym edytorze**; lista opisuje zapisane assety, nie wynik testów.\n\n");
            foreach (var profile in All<ShinobiCombatProfile>()) {
                text.AppendLine("### Profil: " + Safe(profile.name));
                text.AppendLine($"\nSterowanie: cios **{profile.meleeKey}**, skok **{profile.jumpKey}**, kucanie **{profile.crouchKey}**, rzut **{profile.throwKey}**, Focus **{profile.focusKey}**, broń **{profile.nextWeaponKey}**.\n");
                AppendCombos(text, profile.contextCombos, profile.combo, profile.comboWindow, profile.meleeKey.ToString());
                text.AppendLine("\n| Jutsu | Pieczęcie podczas Focus | Chakra | Obrażenia | Przygotowanie / powrót | Promień / przesunięcie |\n| --- | --- | --- | --- | --- | --- |");
                if (profile.jutsu != null) foreach (var j in profile.jutsu.Where(j => j != null))
                    text.AppendLine($"| {Safe(string.IsNullOrEmpty(j.displayName) ? j.name : j.displayName)} | {Keys(j.sequence)} | {N(j.chakraCost)} | {N(j.damage)} | {N(j.windup)} / {N(j.recovery)} s | {N(j.radius)} / {N(j.forwardOffset)} m |");
                text.AppendLine();
            }
            foreach (var weapon in All<ShinobiWeaponDefinition>()) {
                text.AppendLine("### Broń: " + Safe(weapon.displayName) + "\n");
                AppendCombos(text, weapon.contextCombos, weapon.combo, weapon.comboWindow, "przycisk ciosu profilu");
                text.AppendLine(weapon.throwable ? "\nRzut stojąc / w kucki / w powietrzu: " + N(weapon.throwing.damage) + " obrażeń, cooldown " + N(weapon.throwing.cooldown) + " s, zasięg " + N(weapon.throwing.range) + " m. Celowanie w chwili wypuszczenia; lot prosty.\n" : "\nRzut: niedostępny.\n");
            }
            return text.ToString();
        }
        private static void AppendCombos(StringBuilder text, ShinobiContextCombos slots, ShinobiAttackDefinition[] legacy, float window, string key)
        {
            text.AppendLine("| Kontekst | Combo / wejście | Kolejność obrażeń | Fazy ciosów: przygotowanie / aktywność / powrót | Okno | Braki animacji |\n| --- | --- | --- | --- | --- | --- |");
            foreach (ShinobiAttackContext context in Enum.GetValues(typeof(ShinobiAttackContext))) {
                var combo = slots?.Get(context);
                var attacks = combo != null ? combo.attacks : context == ShinobiAttackContext.Standing ? legacy : null;
                if (attacks == null || attacks.Length == 0) {
                    text.AppendLine($"| {context} | dziedziczenie z profilu / wariant zapasowy | — | — | — | do sprawdzenia |"); continue;
                }
                text.AppendLine($"| {context} | {Safe(combo != null ? combo.displayName : "Starsza konfiguracja")} / {Safe(string.Join(" → ", attacks.Select(a => key)))} | {string.Join(" → ", attacks.Select(a => a != null ? N(a.attack.damage) : "BRAK"))} | {string.Join("; ", attacks.Select(a => a != null ? N(a.attack.windup) + " / " + N(a.attack.activeDuration) + " / " + N(a.attack.recovery) : "BRAK"))} | {N(combo != null ? combo.comboWindow : window)} s | {attacks.Count(a => a == null || a.animation == null)} |");
            }
        }
        public static void Export(string path)
        {
            string old = File.Exists(path) ? File.ReadAllText(path) : "# Combo i jutsu\n\n" + Begin + "\n" + End + "\n\n## Planowane\n\n- Kula ognia — pocisk.\n- Podmuch wiatru — obszarowe odepchnięcie.\n- Kamienna osłona — czasowa ochrona.\n";
            int begin = old.IndexOf(Begin, StringComparison.Ordinal), end = old.IndexOf(End, StringComparison.Ordinal);
            if (begin < 0 || end < begin) throw new InvalidOperationException("Dokument nie ma poprawnych znacznikow GENERATED. Nie nadpisano pliku.");
            File.WriteAllText(path, old.Substring(0, begin + Begin.Length) + "\n" + Generate() + old.Substring(end), new UTF8Encoding(false));
        }
        [MenuItem("Shinobi/Combo i jutsu/Eksportuj liste do Markdown")]
        public static void ExportDialog()
        {
            string path = EditorUtility.SaveFilePanel("Lista combo i jutsu", Path.GetFullPath(".."), "COMBO_I_JUTSU", "md");
            if (string.IsNullOrEmpty(path)) return;
            try { Export(path); Debug.Log("Zapisano katalog: " + path); }
            catch (Exception error) { Debug.LogError(error.Message); }
        }
    }
}
