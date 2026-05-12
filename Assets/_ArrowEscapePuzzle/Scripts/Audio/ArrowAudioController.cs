using ArrowGame.Data;
using ArrowGame.Data.Events;
using ArrowGame.Data.States;
using ArrowGame.Data.VFX;
using GameCore.Utils.DesignPattern.Events;
using UnityEngine;

namespace ArrowGame.Audio
{
    public class ArrowAudioController :  MonoBehaviour
    {
        [SerializeField] private ArrowAudioConfig arrowAudioConfig;
        private AudioManager _audioManager;
        
        
        //Coin
        private float _lastCoinSoundTime = 0f;
        private readonly float _coinSoundCooldown = 0.05f;

        private void Awake()
        {
            _audioManager = AudioManager.Instance;
            Debug.Log($"[ArrowAudioController] Awake: Playing music, AudioManager.IsMusicEnabled={_audioManager.IsMusicEnabled}");
            _audioManager.PlayMusic(arrowAudioConfig.backgroundMusic);
        }

        private void OnEnable()
        {
            EventManager<VisualEventID>.AddListener(VisualEventID.ArrowEscaped, HandleArrowEscape);
            EventManager<VisualEventID>.AddListener<Vector3>(VisualEventID.ArrowWrongImpact, HandleArrowImpact);
            EventManager<VisualEventID>.AddListener(VisualEventID.CoinCountTick, HandleCoinTick);
            EventManager<VisualEventID>.AddListener<bool>(VisualEventID.CoinCountComplete, OnCoinComplete);
            EventManager<VisualEventID>.AddListener<TapVFXPayload>(VisualEventID.PlayTapAuraVFX, HandleArrowTap);
            EventManager<LogicGameEventID>.AddListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChange);
        }

        private void HandleArrowTap(TapVFXPayload payload)
        {
            _audioManager.PlaySfx(arrowAudioConfig.fingerTap);
        }


        private void OnDisable()
        {
            EventManager<VisualEventID>.RemoveListener(VisualEventID.ArrowEscaped, HandleArrowEscape);
            EventManager<VisualEventID>.RemoveListener<Vector3>(VisualEventID.ArrowWrongImpact, HandleArrowImpact);
            EventManager<VisualEventID>.RemoveListener(VisualEventID.CoinCountTick, HandleCoinTick);
            EventManager<VisualEventID>.RemoveListener<bool>(VisualEventID.CoinCountComplete, OnCoinComplete);
            EventManager<LogicGameEventID>.RemoveListener<InGameState>(LogicGameEventID.InGameStateChanged, HandleInGameStateChange);
            EventManager<VisualEventID>.RemoveListener<TapVFXPayload>(VisualEventID.PlayTapAuraVFX, HandleArrowTap);
            
        }

        private void HandleArrowEscape()
        {
            _audioManager.PlaySfx(arrowAudioConfig.arrowDash);
        }

        private void HandleArrowImpact(Vector3 impactPosition)
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
        
        private void HandleCoinTick()
        {
            if (Time.unscaledTime - _lastCoinSoundTime >= _coinSoundCooldown)
            {
                _lastCoinSoundTime = Time.unscaledTime;
                _audioManager.PlaySfx(arrowAudioConfig.addCoin); 
            }
        }

        private void OnCoinComplete(bool isAdding)
        {
            // // Phát tiếng "Ting!" chốt sổ
            // if (isAdding)
            // {
            //     // Khi nhận thưởng
            //     AudioManager.Instance.PlaySfx(arrowAudioConfig.addCoinFinal); 
            // }
            // else
            // {
            //     AudioManager.Instance.PlaySfx(arrowAudioConfig.addCoin); 
            // }
        }
    }
}
