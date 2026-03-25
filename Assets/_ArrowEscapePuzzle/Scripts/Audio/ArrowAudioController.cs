using System;
using ArrowGame.Data;
using ArrowGame.Data.Events;
using Codice.Client.Common;
using GameCore.Audio.Manager;
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
    }
}