using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ArrowGame.UI.Components
{
    public class UIButtonFeedbackInjector : MonoBehaviour
    {
        private static UIButtonFeedbackInjector _instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            if (_instance != null) return;
            
            var go = new GameObject("UIButtonFeedbackInjector");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<UIButtonFeedbackInjector>();
        }

        private void Start()
        {
            StartCoroutine(InjectRoutine());
        }

        private IEnumerator InjectRoutine()
        {
            var delay = new WaitForSecondsRealtime(0.5f);
            while (true)
            {
                InjectToAllButtons();
                yield return delay;
            }
        }

        private void InjectToAllButtons()
        {
            // Find all Button components in the loaded scenes (active and inactive)
            var buttons = Resources.FindObjectsOfTypeAll<Button>();
            foreach (var button in buttons)
            {
                if (button == null) continue;
                
                // Only inject in valid scene objects (ignore assets/prefabs in project)
                if (!button.gameObject.scene.IsValid()) continue;

                if (!button.TryGetComponent<UIButtonFeedback>(out _))
                {
                    button.gameObject.AddComponent<UIButtonFeedback>();
                }
            }
        }
    }
}
