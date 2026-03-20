using GameCore.Audio.UI_Sound;
using GameCore.Data;
using UnityEditor;
using UnityEngine;

// <--- Check kỹ dòng này xem đúng namespace của file chính chưa

namespace Utils.Editor
{
    // Dòng này cực quan trọng: Nó báo cho Unity biết file này dùng để vẽ cho UIEventSound
    [CustomEditor(typeof(UIEventSound))] 
    [CanEditMultipleObjects]
    public class UIEventSoundEditor : UnityEditor.Editor
    {
        private SerializedProperty _openTypeProp;
        private SerializedProperty _closeTypeProp;
        private SerializedProperty _customOpenClipProp;
        private SerializedProperty _customCloseClipProp;

        private void OnEnable()
        {
            // Tìm các biến theo đúng tên string trong file UIEventSound.cs
            _openTypeProp = serializedObject.FindProperty("openSound");
            _closeTypeProp = serializedObject.FindProperty("closeSound");
            _customOpenClipProp = serializedObject.FindProperty("customOpenClip");
            _customCloseClipProp = serializedObject.FindProperty("customCloseClip");
        }

        public override void OnInspectorGUI()
        {
            // Update dữ liệu mới nhất
            serializedObject.Update();

            // --- VẼ PHẦN OPEN ---
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Open Event", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_openTypeProp, new GUIContent("Open Sound Type"));

            // Logic ẩn hiện: Chỉ hiện clip khi chọn Custom
            UISoundType openType = (UISoundType)_openTypeProp.enumValueIndex;
            if (openType == UISoundType.Custom)
            {
                DrawCustomBox(_customOpenClipProp, "Audio Clip MỞ (Open)");
            }

            // --- VẼ PHẦN CLOSE ---
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Close Event", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_closeTypeProp, new GUIContent("Close Sound Type"));

            // Logic ẩn hiện: Chỉ hiện clip khi chọn Custom
            UISoundType closeType = (UISoundType)_closeTypeProp.enumValueIndex;
            if (closeType == UISoundType.Custom)
            {
                DrawCustomBox(_customCloseClipProp, "Audio Clip ĐÓNG (Close)");
            }

            // Lưu thay đổi
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawCustomBox(SerializedProperty clipProp, string msg)
        {
            // Vẽ cái khung màu xanh/xám bao quanh cho đẹp
            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.LabelField(msg, EditorStyles.miniLabel);
            EditorGUILayout.PropertyField(clipProp, GUIContent.none); // GUIContent.none để không hiện tên biến lặp lại
            EditorGUILayout.EndVertical();
        }
    }
}