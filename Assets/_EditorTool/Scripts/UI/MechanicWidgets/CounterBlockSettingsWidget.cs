using System;
using TMPro;
using UnityEngine;

namespace EditorTool.Scripts.UI.MechanicWidgets
{
    public class CounterBlockSettingsWidget : MonoBehaviour
    {
        [Header("Counter Block Settings")]
        [SerializeField] private TMP_InputField _counterInput;

        public Action<int> OnCounterChanged;

        public void Initialize()
        {
            BindEvents();
        }

        private void BindEvents()
        {
            if (_counterInput != null)
            {
                _counterInput.onEndEdit.RemoveAllListeners();
                _counterInput.onEndEdit.AddListener(val =>
                {
                    if (int.TryParse(val, out int result))
                    {
                        OnCounterChanged?.Invoke(result);
                    }
                    else
                    {
                        OnCounterChanged?.Invoke(0);
                    }
                });
            }
        }

        public void Refresh(int counter)
        {
            if (_counterInput != null)
            {
                _counterInput.SetTextWithoutNotify(counter.ToString());
            }
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
