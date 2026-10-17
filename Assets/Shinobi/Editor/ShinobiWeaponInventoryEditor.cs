using UnityEditor;
using UnityEngine;

namespace Shinobi.Editor
{
    [CustomEditor(typeof(ShinobiWeaponInventory))]
    public sealed class ShinobiWeaponInventoryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            if (!Application.isPlaying) return;
            var inventory = (ShinobiWeaponInventory)target;
            EditorGUILayout.LabelField("Wybrana bron", inventory.CurrentWeaponName);
            if (GUILayout.Button("Nastepna bron")) inventory.TryEquipNext();
            Repaint();
        }
    }
}
