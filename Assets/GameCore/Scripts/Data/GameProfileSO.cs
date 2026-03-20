using UnityEngine;

namespace GameCore.Data
{
    [CreateAssetMenu(fileName = "New Game Profile", menuName = "Game Profile")]
    public class GameProfileSO : ScriptableObject
    {
        public string id;
        public string displayName;
        public Sprite uiSprite;
        public string sceneName;
        
        [Header("Visual Config")]
        public Color textColor = Color.white;
    }
}