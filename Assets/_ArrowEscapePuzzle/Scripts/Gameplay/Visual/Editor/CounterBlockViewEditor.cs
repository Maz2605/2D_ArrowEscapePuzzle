#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual.Editor
{
    [CustomEditor(typeof(CounterBlockView))]
    public class CounterBlockViewEditor : UnityEditor.Editor
    {
        private static readonly string[] HiddenProperties =
        {
            "m_Script",
            "colorMode",
            "customColor"
        };

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, HiddenProperties);

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("CounterBlockView always uses theme color. Color override settings are hidden.", MessageType.None);

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
