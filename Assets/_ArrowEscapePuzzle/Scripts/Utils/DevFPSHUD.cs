using TMPro;
using UnityEngine;

namespace ArrowGame.Utils
{
    public class DevHUDService : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private GameObject hudPanel; // Container chứa text

        [Header("Settings")]
        [SerializeField] private float updateInterval = 0.5f; // Nửa giây cập nhật 1 lần để dễ nhìn và giảm GC
        
        private float accum = 0f;
        private int frames = 0;
        private float timeLeft;
        private bool isShowHUD = true;

        private readonly Color colorGood = Color.green;
        private readonly Color colorWarn = Color.yellow;
        private readonly Color colorBad = Color.red;

        private void Awake()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            Destroy(gameObject);
            return;
#endif
            timeLeft = updateInterval;
        }

        private void Update()
        {
            if (!isShowHUD) return;

            float deltaTime = Time.unscaledDeltaTime;
            timeLeft -= deltaTime;
            accum += deltaTime;
            frames++;

            if (timeLeft <= 0f)
            {
                UpdateFPSDisplay();
                
                timeLeft = updateInterval;
                accum = 0f;
                frames = 0;
            }
        }

        private void UpdateFPSDisplay()
        {
            if (fpsText == null) return;

            float fps = frames / accum;
            if (fps >= 50f)
                fpsText.color = colorGood;
            else if (fps >= 30f)
                fpsText.color = colorWarn;
            else
                fpsText.color = colorBad;

            fpsText.text = $"FPS: {Mathf.RoundToInt(fps)}";
        }

        public void ToggleHUD()
        {
            isShowHUD = !isShowHUD;
            if (hudPanel != null)
            {
                hudPanel.SetActive(isShowHUD);
            }
        }
    }
}