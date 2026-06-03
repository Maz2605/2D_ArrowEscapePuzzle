using System;
using GameCore.Audio.Manager;
using UnityEngine;

namespace GameCore.Utils
{
    public class CoreLoader : MonoBehaviour
    {
        [SerializeField] private GameObject corePrefab;

        [Obsolete("Obsolete")]
        private void Awake()
        {
            if (FindObjectOfType<AudioManager>() == null)
                Instantiate(corePrefab);
            
            Destroy(gameObject);
        }
    }
}
