using GameCore.Audio.UI_Sound;
using GameCore.Data;
using UnityEditor;

namespace Utils.Editor
{
    [CustomEditor(typeof(UIButtonSound))]
    [CanEditMultipleObjects]
    public class UIButtonSoundEditor : UnityEditor.Editor
    {
        private SerializedProperty _soundTypeProp;
        private SerializedProperty _customClipProp;
        private SerializedProperty _volumeProp;

        private void OnEnable()
        {
            _soundTypeProp = serializedObject.FindProperty("soundType");
            _customClipProp = serializedObject.FindProperty("customClip");
            _volumeProp = serializedObject.FindProperty("volumeScale");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_soundTypeProp);

            UISoundType type = (UISoundType)_soundTypeProp.enumValueIndex;

            // Chỉ hiện Custom Clip khi chọn chế độ Custom
            if (type == UISoundType.Custom)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.HelpBox("Custom Mode: Kéo Audio riêng biệt vào đây.", MessageType.Info);
                EditorGUILayout.PropertyField(_customClipProp);
                EditorGUILayout.PropertyField(_volumeProp);
                EditorGUILayout.EndVertical();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}