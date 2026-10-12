using UnityEditor;
using UnityEngine;

namespace Shinobi.Editor
{
    [CustomEditor(typeof(ShinobiMovement))]
    public sealed class ShinobiMovementEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Active State (Play Mode)");
            EditorGUILayout.HelpBox(((ShinobiMovement)target).ActiveState, MessageType.Info);
            if (Application.isPlaying) Repaint();
        }
    }
}
