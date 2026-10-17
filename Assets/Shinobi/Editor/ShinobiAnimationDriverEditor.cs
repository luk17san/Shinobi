using UnityEditor;
using UnityEngine;
namespace Shinobi.Editor
{
    [CustomEditor(typeof(ShinobiAnimationDriver))]
    public sealed class ShinobiAnimationDriverEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var driver = (ShinobiAnimationDriver)target;
            EditorGUILayout.HelpBox("Ruch i reakcje: Motion stanow w Animator Controllerze. Ciosy i jutsu: klipy w definicjach. Zestaw Animations to wartosci poczatkowe i zapasowy Idle.", MessageType.Info);
            if (GUILayout.Button("Edytuj animacje i sprawdz braki")) ShinobiAnimationEditor.Open(driver);
            if (driver.animator != null && driver.animator.runtimeAnimatorController != null && GUILayout.Button("Otworz Animator Controller"))
            {
                var controller = driver.animator.runtimeAnimatorController;
                if (controller is AnimatorOverrideController overrides) controller = overrides.runtimeAnimatorController;
                AssetDatabase.OpenAsset(controller);
            }
            if (Application.isPlaying && driver.animator != null && driver.animator.runtimeAnimatorController != null)
            {
                EditorGUILayout.LabelField("Odtwarzany klip", driver.CurrentClip != null ? driver.CurrentClip.name : "Przejscie / brak klipu");
                EditorGUILayout.IntField("State", driver.animator.GetInteger("State"));
                Repaint();
            }
        }
    }
}
