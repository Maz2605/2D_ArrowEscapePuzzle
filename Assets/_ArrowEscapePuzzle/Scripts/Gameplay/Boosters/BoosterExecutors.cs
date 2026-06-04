using System;
using System.Collections.Generic;
using ArrowGame.Data.Booster;
using ArrowGame.Gameplay.Logic;
using ArrowGame.Gameplay.VFX;
using ArrowGame.Gameplay.Visual;
using ArrowGame.UI.Components;
using ArrowGame.Utils;
using ArrowGame.VFX;
using DG.Tweening;
using GameCore.Utils.DesignPattern.ObjectPooling;
using ShareCore.Data;
using UnityEngine;

namespace ArrowGame.Gameplay.Boosters
{
    public abstract class BoosterExecutorBase : IBoosterExecutor
    {
        public abstract BoosterType Type { get; }

        public virtual bool CanUse(BoosterConfigSO config, GridSystem gridLogic)
        {
            return config != null && gridLogic != null && config.CanUse(gridLogic);
        }

        public virtual bool IsValidTarget(BoosterConfigSO config, GridSystem gridLogic, Vector2Int gridPosition)
        {
            if (config == null || gridLogic == null) return false;
            if (!config.RequiresValidArrowTarget) return true;

            return !string.IsNullOrEmpty(gridLogic.GetArrowIdAt(gridPosition.x, gridPosition.y));
        }

        public virtual string GetConfigurationError(BoosterConfigSO config)
        {
            if (config == null) return "Missing booster config.";
            if (config.type != Type) return $"Config type {config.type} does not match executor type {Type}.";
            if (config.boosterIcon == null) return $"{Type} is missing boosterIcon.";
            if (string.IsNullOrWhiteSpace(config.description)) return $"{Type} is missing description.";
            return null;
        }

        public abstract void Execute(BoosterExecutionContext context, Action<bool> onComplete);

        protected static void Complete(Action<bool> onComplete, bool success)
        {
            onComplete?.Invoke(success);
        }

        protected static TConfig CastConfig<TConfig>(BoosterConfigSO config) where TConfig : BoosterConfigSO
        {
            TConfig typedConfig = config as TConfig;
            if (typedConfig == null)
            {
                Debug.LogError($"[BoosterExecutor] Expected {typeof(TConfig).Name}, got {config?.GetType().Name ?? "null"}.");
            }

            return typedConfig;
        }

        protected static void RegisterPooledCleanup(BoosterExecutionContext context, GameObject instance)
        {
            if (context == null || instance == null) return;

            context.RegisterCleanup(() =>
            {
                if (instance != null && instance.activeInHierarchy)
                {
                    PoolingManager.Instance.Despawn(instance);
                }
            });
        }

        protected static void DespawnIfActive(GameObject instance)
        {
            if (instance != null && instance.activeInHierarchy)
            {
                PoolingManager.Instance.Despawn(instance);
            }
        }
    }

    public sealed class HintBoosterExecutor : BoosterExecutorBase
    {
        public override BoosterType Type => BoosterType.Hint;

        public override void Execute(BoosterExecutionContext context, Action<bool> onComplete)
        {
            ArrowData escapableArrow = context.GridLogic.GetOneEscapableArrow();
            if (escapableArrow == null)
            {
                Complete(onComplete, false);
                return;
            }

            GridView gridView = context.GridView;
            gridView?.ShowHint(escapableArrow.ID);

            ArrowLineView arrowView = gridView != null ? gridView.GetArrowViewById(escapableArrow.ID) : null;
            if (arrowView != null)
            {
                float zoomSize = 4f;
                if (context.Config is HintBoosterSO hintConfig)
                {
                    zoomSize = hintConfig.hintZoomSize;
                }

                if (context.CameraController != null)
                {
                    context.CameraController.FocusAndZoomOn(arrowView.HeadPosition, zoomSize, 0.6f);
                }
            }

            GameCore.Utils.DesignPattern.Events.EventManager<ArrowGame.Data.Events.VisualEventID>.Post(ArrowGame.Data.Events.VisualEventID.HintBoosterUsed);

            Complete(onComplete, true);
        }
    }

    public sealed class GateBoosterExecutor : BoosterExecutorBase
    {
        public override BoosterType Type => BoosterType.Gate;

        public override string GetConfigurationError(BoosterConfigSO config)
        {
            string baseError = base.GetConfigurationError(config);
            if (!string.IsNullOrEmpty(baseError)) return baseError;
            return config.vfxConfig.prefab == null ? $"{Type} is missing vfxConfig.prefab." : null;
        }

        public override void Execute(BoosterExecutionContext context, Action<bool> onComplete)
        {
            string targetId = context.GridLogic.GetArrowIdAt(context.TargetGridPosition.x, context.TargetGridPosition.y);
            if (string.IsNullOrEmpty(targetId))
            {
                Complete(onComplete, false);
                return;
            }

            BoosterConfigSO config = context.Config;
            float duration = Mathf.Max(0f, config.vfxConfig.duration);

            context.Sequence.AppendCallback(() => PlayGateVisual(context, targetId, config));
            context.Sequence.AppendInterval(duration);
            context.Sequence.AppendCallback(() => context.GridLogic.ForceRemoveArrow(targetId));
            context.Sequence.OnComplete(() => Complete(onComplete, true));
        }

        internal static void PlayGateVisual(BoosterExecutionContext context, string targetId, BoosterConfigSO config)
        {
            if (config.vfxConfig.prefab == null) return;

            ArrowLineView arrowView = context.GridView != null ? context.GridView.GetArrowViewById(targetId) : null;
            if (arrowView == null) return;

            Vector3 direction = arrowView.EscapeDirection;
            Vector3 spawnPosition = arrowView.HeadPosition + (direction * config.vfxConfig.offsetDistance);
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            Quaternion spawnRotation = Quaternion.Euler(0f, 0f, angle);

            GameObject instance = PoolingManager.Instance.Spawn(config.vfxConfig.prefab, spawnPosition, spawnRotation);
            RegisterPooledCleanup(context, instance);

            GateVisualBehavior behavior = instance.GetComponent<GateVisualBehavior>();
            if (behavior != null)
            {
                behavior.PlayVisual(arrowView, spawnPosition, targetId, config.vfxConfig.duration);
            }
            else
            {
                Debug.LogError($"[GateBoosterExecutor] Prefab {config.vfxConfig.prefab.name} is missing GateVisualBehavior.");
                DOVirtual.DelayedCall(config.vfxConfig.duration, () => DespawnIfActive(instance))
                    .SetLink(instance, LinkBehaviour.KillOnDisable);
            }
        }
    }

    public sealed class LightningBoosterExecutor : BoosterExecutorBase
    {
        public override BoosterType Type => BoosterType.Lightning;

        public override string GetConfigurationError(BoosterConfigSO config)
        {
            string baseError = base.GetConfigurationError(config);
            if (!string.IsNullOrEmpty(baseError)) return baseError;
            return config.vfxConfig.prefab == null ? $"{Type} is missing vfxConfig.prefab." : null;
        }

        public override void Execute(BoosterExecutionContext context, Action<bool> onComplete)
        {
            string clickedId = context.GridLogic.GetArrowIdAt(context.TargetGridPosition.x, context.TargetGridPosition.y);
            if (string.IsNullOrEmpty(clickedId))
            {
                Complete(onComplete, false);
                return;
            }

            Direction4? primaryDirection = context.GridLogic.GetPrimaryExitDirection(clickedId);
            if (!primaryDirection.HasValue)
            {
                Complete(onComplete, false);
                return;
            }

            List<string> sameDirectionIds = context.GridLogic.GetArrowIdsByPrimaryDirection(primaryDirection.Value, clickedId);
            sameDirectionIds.Shuffle();
            if (sameDirectionIds.Count > 4) sameDirectionIds = sameDirectionIds.GetRange(0, 4);

            List<string> finalTargets = new List<string> { clickedId };
            finalTargets.AddRange(sameDirectionIds);

            float duration = Mathf.Max(0f, context.Config.vfxConfig.duration);
            context.Sequence.AppendCallback(() => PlayLightningVisual(context, finalTargets));
            context.Sequence.AppendInterval(duration);
            context.Sequence.AppendCallback(() =>
            {
                foreach (string id in finalTargets)
                {
                    context.GridLogic.ForceRemoveArrow(id);
                }
            });
            context.Sequence.OnComplete(() => Complete(onComplete, true));
        }

        private static void PlayLightningVisual(BoosterExecutionContext context, List<string> targetIds)
        {
            if (context.Config.vfxConfig.prefab == null || targetIds == null || targetIds.Count == 0) return;

            List<ArrowLineView> targetViews = new List<ArrowLineView>();
            foreach (string id in targetIds)
            {
                ArrowLineView view = context.GridView != null ? context.GridView.GetArrowViewById(id) : null;
                if (view != null) targetViews.Add(view);
            }

            if (targetViews.Count == 0) return;

            GameObject instance = PoolingManager.Instance.Spawn(context.Config.vfxConfig.prefab, Vector3.zero, Quaternion.identity);
            RegisterPooledCleanup(context, instance);

            ChainLightningVFX behavior = instance.GetComponent<ChainLightningVFX>();
            if (behavior != null)
            {
                behavior.PlayMultiVisual(targetViews, context.Config.vfxConfig.duration);
            }
            else
            {
                Debug.LogError($"[LightningBoosterExecutor] Prefab {context.Config.vfxConfig.prefab.name} is missing ChainLightningVFX.");
                DOVirtual.DelayedCall(context.Config.vfxConfig.duration, () => DespawnIfActive(instance))
                    .SetLink(instance, LinkBehaviour.KillOnDisable);
            }
        }
    }

    public sealed class ArrowDashBoosterExecutor : BoosterExecutorBase
    {
        public override BoosterType Type => BoosterType.ArrowDash;

        public override void Execute(BoosterExecutionContext context, Action<bool> onComplete)
        {
            ArrowDashBoosterSO config = CastConfig<ArrowDashBoosterSO>(context.Config);
            if (config == null)
            {
                Complete(onComplete, false);
                return;
            }

            string clickedId = context.GridLogic.GetArrowIdAt(context.TargetGridPosition.x, context.TargetGridPosition.y);
            if (string.IsNullOrEmpty(clickedId))
            {
                Complete(onComplete, false);
                return;
            }

            Direction4? primaryDirection = context.GridLogic.GetPrimaryExitDirection(clickedId);
            if (!primaryDirection.HasValue)
            {
                Complete(onComplete, false);
                return;
            }

            List<string> allSameDirectionIds = context.GridLogic.GetArrowIdsByPrimaryDirection(primaryDirection.Value);
            List<string> otherSameDirectionIds = new List<string>(allSameDirectionIds);
            otherSameDirectionIds.Remove(clickedId);
            otherSameDirectionIds.Shuffle();
            
            int additionalCount = Mathf.Min(config.targetCount - 1, otherSameDirectionIds.Count);
            List<string> finalTargets = new List<string> { clickedId };
            finalTargets.AddRange(otherSameDirectionIds.GetRange(0, additionalCount));

            context.Sequence.AppendCallback(() =>
            {
                context.GridView?.SetBoardDarken(true);
                foreach (string id in allSameDirectionIds) context.GridView?.ShowFocus(id);
            });

            context.Sequence.AppendInterval(Mathf.Max(0f, config.highlightDuration));

            foreach (string id in finalTargets)
            {
                string targetId = id;
                context.Sequence.AppendCallback(() =>
                {
                    context.GridView?.PlayDashEscape(targetId);
                    context.GridLogic.ForceRemoveArrow(targetId);
                });
                context.Sequence.AppendInterval(Mathf.Max(0f, config.delayBetweenEscapes));
            }

            context.Sequence.AppendCallback(() =>
            {
                context.GridView?.SetBoardDarken(false);
                foreach (string id in allSameDirectionIds) context.GridView?.HideFocus(id);
            });

            context.RegisterCleanup(() =>
            {
                context.GridView?.SetBoardDarken(false);
                foreach (string id in allSameDirectionIds) context.GridView?.HideFocus(id);
            });

            context.Sequence.OnComplete(() => Complete(onComplete, true));
        }
    }

    public sealed class UfoBoosterExecutor : BoosterExecutorBase
    {
        public override BoosterType Type => BoosterType.Ufo;

        public override string GetConfigurationError(BoosterConfigSO config)
        {
            string baseError = base.GetConfigurationError(config);
            if (!string.IsNullOrEmpty(baseError)) return baseError;

            UFOBoosterSO typedConfig = config as UFOBoosterSO;
            if (typedConfig == null) return $"{Type} config must be UFOBoosterSO.";
            if (typedConfig.ufoShipPrefab == null) return $"{Type} is missing ufoShipPrefab.";
            if (typedConfig.vfxConfig.prefab == null) return $"{Type} is missing vfxConfig.prefab.";
            return null;
        }

        public override void Execute(BoosterExecutionContext context, Action<bool> onComplete)
        {
            UFOBoosterSO config = CastConfig<UFOBoosterSO>(context.Config);
            if (config == null)
            {
                Complete(onComplete, false);
                return;
            }

            List<ArrowData> targets = context.GridLogic.GetMultipleEscapableArrows(3);
            if (targets == null || targets.Count == 0)
            {
                Complete(onComplete, false);
                return;
            }

            GameObject shipInstance = null;
            context.Sequence.AppendCallback(() =>
            {
                if (config.ufoShipPrefab == null) return;

                shipInstance = PoolingManager.Instance.Spawn(config.ufoShipPrefab, config.ufoSpawnPosition, Quaternion.identity);
                RegisterPooledCleanup(context, shipInstance);
            });

            context.Sequence.AppendInterval(Mathf.Max(0f, config.ufoChargeUpTime));
            context.Sequence.AppendCallback(() =>
            {
                foreach (ArrowData target in targets)
                {
                    if (target != null) GateBoosterExecutor.PlayGateVisual(context, target.ID, config);
                }
            });

            float holeDuration = Mathf.Max(0f, config.vfxConfig.duration);
            context.Sequence.AppendInterval(holeDuration);
            context.Sequence.AppendCallback(() =>
            {
                foreach (ArrowData target in targets)
                {
                    if (target != null) context.GridLogic.ForceRemoveArrow(target.ID);
                }
            });

            float remainTime = Mathf.Max(0f, config.ufoShipTotalDuration - config.ufoChargeUpTime - holeDuration);
            if (remainTime > 0f) context.Sequence.AppendInterval(remainTime);

            context.Sequence.AppendCallback(() => DespawnIfActive(shipInstance));
            context.Sequence.OnComplete(() => Complete(onComplete, true));
        }
    }

    public sealed class LineGuideBoosterExecutor : BoosterExecutorBase
    {
        public override BoosterType Type => BoosterType.LineGuide;

        public override bool CanUse(BoosterConfigSO config, GridSystem gridLogic)
        {
            return gridLogic != null && !gridLogic.IsBoardEmpty();
        }

        public override void Execute(BoosterExecutionContext context, Action<bool> onComplete)
        {
            context.GridView?.ToggleDirectionLines(context.DesiredToggleState);
            Complete(onComplete, true);
        }
    }
}
