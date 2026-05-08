using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.Utils
{
    [RequireComponent(typeof(Image))]
    public class UIFrameByFrame : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image targetImage;
        [SerializeField] private Sprite[] frames;
    
        [Header("Settings")]
        [SerializeField] private int fps = 24;
        [SerializeField] private bool playOnEnable = true;

        private Coroutine _animRoutine;

        private void Awake()
        {
            if (targetImage == null) targetImage = GetComponent<Image>();
        }

        private void OnEnable()
        {
            if (playOnEnable) Play();
        }

        private void OnDisable()
        {
            Stop(); 
        }

        public void Play()
        {
            if (frames == null || frames.Length == 0) return;
            Stop();
            _animRoutine = StartCoroutine(PlayAnimRoutine());
        }

        public void Stop()
        {
            if (_animRoutine != null)
            {
                StopCoroutine(_animRoutine);
                _animRoutine = null;
            }
        }

        private IEnumerator PlayAnimRoutine()
        {
            int currentIndex = 0;
            float frameDuration = 1f / fps;
            float timer = 0f;

            targetImage.sprite = frames[0];

            while (true)
            {
                timer += Time.unscaledDeltaTime;

                if (timer >= frameDuration)
                {
                    timer -= frameDuration;
                    currentIndex = (currentIndex + 1) % frames.Length;
                    targetImage.sprite = frames[currentIndex];
                }
                yield return null; 
            }
        }
    }
}