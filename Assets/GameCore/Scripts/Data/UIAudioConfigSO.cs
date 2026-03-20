using System;
using System.Collections.Generic;
using UnityEngine;

// Để dùng Enum.GetValues

namespace GameCore.Data
{
    [Serializable]
    public class UIAudioItem
    {
        public string name; 
        public UISoundType type;
        public AudioClip clip;
    }

    [CreateAssetMenu(fileName = "UIAudioConfig", menuName = "Core/UI Audio Config")]
    public class UIAudioConfigSO : ScriptableObject
    {
        [Header("General")]
        [Range(0f, 1f)] public float uiVolume = 1f;

        [Header("Audio Database")]
        [SerializeField] private List<UIAudioItem> audioList = new List<UIAudioItem>();

        private Dictionary<UISoundType, AudioClip> _audioDict;

#if UNITY_EDITOR
        private void OnValidate()
        {
            var currentTypes = new HashSet<UISoundType>();
            foreach (var item in audioList)
            {
                currentTypes.Add(item.type);
                item.name = item.type.ToString(); 
            }
            
            foreach (UISoundType type in Enum.GetValues(typeof(UISoundType)))
            {
                if (type == UISoundType.None) continue; 

                if (!currentTypes.Contains(type))
                {
                    audioList.Add(new UIAudioItem 
                    { 
                        name = type.ToString(), 
                        type = type, 
                        clip = null 
                    });
                }
            }
        }
#endif

        public AudioClip GetClip(UISoundType type)
        {
            if (_audioDict == null) InitializeDictionary();

            if (_audioDict != null && _audioDict.TryGetValue(type, out AudioClip clip))
                return clip;
            return null;
        }

        private void InitializeDictionary()
        {
            _audioDict = new Dictionary<UISoundType, AudioClip>();
            foreach (var item in audioList)
            {
                if (item.clip != null && !_audioDict.ContainsKey(item.type))
                {
                    _audioDict.Add(item.type, item.clip);
                }
            }
        }
    }
}