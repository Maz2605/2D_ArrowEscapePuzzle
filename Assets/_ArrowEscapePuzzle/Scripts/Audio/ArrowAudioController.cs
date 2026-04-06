using ArrowGame.Data;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.Audio
{
    public class ArrowAudioController :  MonoBehaviour
    {
        [SerializeField] private ArrowAudioConfig arrowAudioConfig;
        private AudioManager _audioManager;

        private void Awake()
        {
            _audioManager = AudioManager.Instance;
            _audioManager.PlayMusic(arrowAudioConfig.backgroundMusic);
        }

        private void OnEnable()
        {
            EventManager<VisualEventID>.AddListener(VisualEventID.ArrowEscaped, HandleArrowEscape);
            EventManager<VisualEventID>.AddListener(VisualEventID.ArrowWrongImpact, HandleArrowImpact);
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChange);
        }

        

        private void OnDisable()
        {
            EventManager<VisualEventID>.RemoveListener(VisualEventID.ArrowEscaped, HandleArrowEscape);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.ArrowWrongImpact, HandleArrowImpact);
        }

        private void HandleArrowEscape()
        {
            _audioManager.PlaySfx(arrowAudioConfig.arrowDash);
        }

        private void HandleArrowImpact()
        {
            _audioManager.PlaySfx(arrowAudioConfig.arrowWrong);
        }
        
        private void HandleInGameStateChange(InGameState obj)
        {
            switch (obj)
            {
                case InGameState.Win: 
                    _audioManager.PlaySfx(arrowAudioConfig.win);
                    break;
                case InGameState.Lose:
                    _audioManager.PlaySfx(arrowAudioConfig.lose);
                    break;
            }
        }
    }
}