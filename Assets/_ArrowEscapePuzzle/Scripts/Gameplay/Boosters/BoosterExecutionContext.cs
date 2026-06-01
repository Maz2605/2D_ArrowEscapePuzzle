using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Controllers;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.Managers;
using ArrowGame.Gameplay.Visual;
using DG.Tweening;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    public sealed class BoosterExecutionContext
    {
        private readonly List<Action> _cleanupActions = new List<Action>();

        public BoosterExecutionContext(
            BoosterConfigSO config,
            GridSystem gridLogic,
            Vector2Int targetGridPosition,
            Sequence sequence,
            bool desiredToggleState)
        {
            Config = config;
            GridLogic = gridLogic;
            TargetGridPosition = targetGridPosition;
            Sequence = sequence;
            DesiredToggleState = desiredToggleState;
        }

        public BoosterConfigSO Config { get; }
        public GridSystem GridLogic { get; }
        public Vector2Int TargetGridPosition { get; }
        public Sequence Sequence { get; }
        public bool DesiredToggleState { get; }

        public bool HasTarget => TargetGridPosition.x >= 0 && TargetGridPosition.y >= 0;
        public GridView GridView => GameManager.Instance != null ? GameManager.Instance.CurrentGridView : null;
        public CameraController CameraController => GameManager.Instance != null ? GameManager.Instance.CurrentCameraController : null;

        public void RegisterCleanup(Action cleanup)
        {
            if (cleanup != null) _cleanupActions.Add(cleanup);
        }

        public void Cleanup()
        {
            for (int i = _cleanupActions.Count - 1; i >= 0; i--)
            {
                try
                {
                    _cleanupActions[i]?.Invoke();
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }
            }

            _cleanupActions.Clear();
        }
    }
}
