using System;
using DG.Tweening;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    internal sealed class ArrowLineAnimationCoordinator
    {
        private readonly ArrowLineView _owner;
        private Tween _scaleTween;
        private Tween _colorTween;
        private Tween _focusGlowTween;
        private Sequence _actionSequence;

        public ArrowLineAnimationCoordinator(ArrowLineView owner)
        {
            _owner = owner;
        }

        public void PlaySpawnAnimation(float delay, float duration, bool showLine, Action onComplete)
        {
            if (_owner.State.CurrentState == ArrowLineVisualState.Escaping ||
                _owner.State.BodyPoints == null || _owner.State.BodyPoints.Length == 0)
            {
                return;
            }

            _owner.State.CurrentState = ArrowLineVisualState.Spawning;

            if (showLine && _owner.Context.PrimaryDirectionRenderer != null)
            {
                _owner.State.IsDirectionLinePersistent = true;
                _owner.Context.PrimaryDirectionRenderer.enabled = true;
                _owner.State.DirectionLineProgress = 0f;
                _owner.UpdateDirectionLineIfEnabled();

                DOTween.To(() => _owner.State.DirectionLineProgress, x =>
                    {
                        _owner.State.DirectionLineProgress = x;
                        _owner.UpdateDirectionLineIfEnabled();
                    }, 1f, 0.5f)
                    .SetDelay(delay + 0.1f)
                    .SetEase(Ease.OutSine)
                    .SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);
            }

            KillAllActiveTweens();
            _owner.ClearTraceRoute();

            _owner.State.TravelDistance = -_owner.PathPresenter.BodyLength;
            _owner.RefreshBody();

            float revealDuration = duration * _owner.Context.SpawnRevealRatio;
            if (_owner.Context.HeadSpriteRenderer != null)
            {
                Color headColor = _owner.State.BaseColor;
                headColor.a = 0f;
                _owner.Context.HeadSpriteRenderer.color = headColor;
                _owner.Context.HeadSpriteRenderer.DOFade(_owner.State.BaseColor.a, revealDuration)
                    .SetDelay(delay)
                    .SetId(_owner)
                    .SetLink(_owner.Context.HeadSpriteRenderer.gameObject);
            }

            if (_owner.Context.SecondaryEndpointMarker != null && _owner.Context.SecondaryEndpointMarker.enabled)
            {
                Color markerColor = _owner.State.BaseColor;
                markerColor.a = 0f;
                _owner.Context.SecondaryEndpointMarker.color = markerColor;
                _owner.Context.SecondaryEndpointMarker.DOFade(_owner.State.BaseColor.a, revealDuration)
                    .SetDelay(delay)
                    .SetId(_owner)
                    .SetLink(_owner.Context.SecondaryEndpointMarker.gameObject);
            }

            if (_owner.Context.VisualRoot != null)
            {
                _owner.Context.VisualRoot.localScale = Vector3.zero;
                _owner.Context.VisualRoot.DOScale(1f, revealDuration)
                    .SetDelay(delay)
                    .SetEase(_owner.Context.SpawnScaleCurve)
                    .SetId(_owner)
                    .SetLink(_owner.Context.VisualRoot.gameObject);
            }

            _actionSequence = DOTween.Sequence().SetId(_owner).SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);
            _actionSequence.Insert(delay, DOTween.To(() => _owner.State.TravelDistance, x =>
            {
                _owner.State.TravelDistance = x;
                _owner.RefreshBody();
            }, 0f, duration).SetEase(_owner.Context.SpawnMoveCurve));

            _actionSequence.OnComplete(() =>
            {
                _owner.State.CurrentState = ArrowLineVisualState.Idle;
                onComplete?.Invoke();
            });
        }

        public void PlayEscapeAnimation(float startDelay, Action onEscapeStart, Action onEscapeComplete)
        {
            if (_owner.State.CurrentState == ArrowLineVisualState.Escaping || !_owner.PathPresenter.HasMovementPath)
            {
                return;
            }

            _owner.State.CurrentState = ArrowLineVisualState.Escaping;
            KillAllActiveTweens();
            _owner.DisableDirectionRenderers();

            Vector3 routeEndWorld = _owner.transform.TransformPoint(
                _owner.PathPresenter.GetPointAtDistance(_owner.PathPresenter.MovementLength, _owner.State.EscapeDirection));
            Vector3 exitDirection = _owner.State.EscapeDirection.sqrMagnitude > 0f
                ? _owner.State.EscapeDirection.normalized
                : Vector3.up;
            float distanceToEdge = ArrowGame.Utils.CameraUtils.GetDistanceToEdge(_owner.Context.MainCamera, routeEndWorld, exitDirection);

            float targetDistance = _owner.PathPresenter.MovementLength + distanceToEdge +
                                   (_owner.State.CellSize * _owner.Context.EscapeExtraDistanceFactor);
            float moveDuration = Mathf.Max(0.05f, (targetDistance - _owner.State.TravelDistance) / _owner.Context.EscapeSpeed);

            _actionSequence = DOTween.Sequence().SetId(_owner).SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);

            float flashUpTime = 0.02f;
            float flashDownTime = 0.1f;
            _actionSequence.Insert(startDelay, DOTween.To(() => _owner.Appearance.CurrentFlashIntensity,
                    x => _owner.Appearance.SetFlashIntensity(x), _owner.Context.EscapeFlashIntensity, flashUpTime)
                .SetEase(Ease.OutFlash));
            _actionSequence.Insert(startDelay + flashUpTime, DOTween.To(() => _owner.Appearance.CurrentFlashIntensity,
                    x => _owner.Appearance.SetFlashIntensity(x), 0f, flashDownTime)
                .SetEase(Ease.InQuad));

            if (startDelay > 0f)
            {
                _actionSequence.AppendInterval(startDelay);
            }

            _actionSequence.Append(DOTween.To(() => _owner.State.TravelDistance, x =>
            {
                _owner.State.TravelDistance = x;
                _owner.RefreshBody();
            }, _owner.Context.PullbackOffset, _owner.Context.PullbackDuration).SetEase(Ease.OutQuad));

            _actionSequence.AppendCallback(() => _owner.ClearEscapeTrails());
            _actionSequence.Append(DOTween.To(() => _owner.State.TravelDistance, x =>
                {
                    _owner.State.TravelDistance = x;
                    _owner.RefreshBody();
                }, targetDistance, moveDuration)
                .SetEase(_owner.Context.EscapeMoveCurve)
                .OnStart(() =>
                {
                    EventManager<ArrowGame.Data.Events.VisualEventID>.Post(ArrowGame.Data.Events.VisualEventID.ArrowEscaped);
                    onEscapeStart?.Invoke();
                }));

            if (_owner.Context.HeadSpriteRenderer != null)
            {
                _actionSequence.Insert(_owner.Context.PullbackDuration + (moveDuration * _owner.Context.FadeOutRatio),
                    _owner.Context.HeadSpriteRenderer.DOFade(0f, moveDuration * (1f - _owner.Context.FadeOutRatio)));
            }

            _actionSequence.Join(DOTween.To(() => _owner.Appearance.CurrentFlashIntensity,
                    x => _owner.Appearance.SetFlashIntensity(x), _owner.Context.EscapeFlashPeakIntensity,
                    _owner.Context.BumpDuration * 0.15f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));

            _actionSequence.OnComplete(() =>
            {
                _owner.State.CurrentState = ArrowLineVisualState.Idle;
                _owner.ClearBodyRenderers();
                _owner.DisableDirectionRenderers();
                _owner.ClearEscapeTrails();
                onEscapeComplete?.Invoke();
                PoolingManager.Instance.Despawn(_owner.gameObject);
            });
        }

        public void PlayBlockedAnimation(float realBumpDistance, Action onImpact)
        {
            if (_owner.State.CurrentState == ArrowLineVisualState.Escaping ||
                _owner.State.CurrentState == ArrowLineVisualState.Spawning)
            {
                return;
            }

            _owner.State.CurrentState = ArrowLineVisualState.Blocked;
            _owner.State.IsMarkedAsWrong = true;

            KillAllActiveTweens();
            if (_owner.Context.VisualRoot != null)
            {
                _owner.Context.VisualRoot.localPosition = Vector3.zero;
                _owner.Context.VisualRoot.localScale = Vector3.one;
            }

            _owner.DisableDirectionRenderers();
            _actionSequence = DOTween.Sequence().SetId(_owner).SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);

            _actionSequence.Append(DOTween.To(() => 0f, x =>
            {
                float curveValue = _owner.Context.BumpCurve.Evaluate(x);
                _owner.State.TravelDistance = curveValue * realBumpDistance;
                _owner.RefreshBody();
            }, 1f, _owner.Context.BumpDuration).SetEase(Ease.Linear));

            _actionSequence.Join(DOTween.To(() => _owner.Context.HeadSpriteRenderer.color,
                x => _owner.Appearance.ApplyColor(x), _owner.State.BlockedColor, _owner.Context.BumpDuration * 0.3f));

            _actionSequence.InsertCallback(_owner.Context.BumpDuration * 0.3f, () =>
            {
                EventManager<ArrowGame.Data.Events.VisualEventID>.Post(
                    ArrowGame.Data.Events.VisualEventID.ArrowWrongImpact,
                    _owner.HeadPosition);
                onImpact?.Invoke();
            });

            Transform targetShake = _owner.Context.VisualRoot != null ? _owner.Context.VisualRoot : _owner.transform;
            _actionSequence.Insert(_owner.Context.BumpDuration * 0.3f,
                targetShake.DOShakePosition(_owner.Context.ShakeDuration, _owner.Context.ShakeStrength)).SetId(_owner);

            _actionSequence.OnComplete(() =>
            {
                _owner.State.CurrentState = ArrowLineVisualState.Idle;
                if (_owner.State.IsDirectionLinePersistent)
                {
                    if (_owner.Context.PrimaryDirectionRenderer != null)
                    {
                        _owner.Context.PrimaryDirectionRenderer.enabled = true;
                    }

                    if (_owner.Context.SecondaryDirectionRenderer != null && _owner.HasSecondaryEndpointForActivePath())
                    {
                        _owner.Context.SecondaryDirectionRenderer.enabled = true;
                    }

                    _owner.UpdateDirectionLineIfEnabled();
                }
            });
        }

        public void PlayCollisionFlash()
        {
            if (_owner.State.CurrentState == ArrowLineVisualState.Escaping) return;

            _focusGlowTween?.Kill();
            _owner.Appearance.SetFlashIntensity(0f);

            Sequence flashSequence = DOTween.Sequence()
                .SetId(_owner)
                .SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);

            flashSequence.Append(DOTween.To(() => _owner.Appearance.CurrentFlashIntensity,
                x => _owner.Appearance.SetFlashIntensity(x), _owner.Context.CollisionFlashIntensity, 0.1f)
                .SetEase(Ease.OutFlash));
            flashSequence.Append(DOTween.To(() => _owner.Appearance.CurrentFlashIntensity,
                x => _owner.Appearance.SetFlashIntensity(x), 0f, 0.3f)
                .SetEase(Ease.InQuad));
        }

        public void PlayHoldEffect(bool isHolding)
        {
            if (_owner.State.CurrentState != ArrowLineVisualState.Idle) return;

            _scaleTween?.Kill();
            _focusGlowTween?.Kill();
            _owner.Appearance.SetFlashIntensity(0f);

            float targetScale = isHolding ? _owner.Context.HoldScaleTarget : 1f;
            float duration = isHolding ? _owner.Context.HoldScaleDurationIn : _owner.Context.HoldScaleDurationOut;
            Ease ease = isHolding ? Ease.OutBack : Ease.OutQuad;

            if (_owner.Context.VisualRoot != null)
            {
                _scaleTween = _owner.Context.VisualRoot.DOScale(targetScale, duration)
                    .SetId(_owner)
                    .SetEase(ease)
                    .OnUpdate(() =>
                    {
                        if (isHolding)
                        {
                            _owner.UpdateDirectionLineIfEnabled();
                        }
                    })
                    .SetLink(_owner.Context.VisualRoot.gameObject);
            }

            if (_owner.Context.PrimaryDirectionRenderer != null)
            {
                if (isHolding)
                {
                    _owner.Context.PrimaryDirectionRenderer.enabled = true;
                    if (_owner.Context.SecondaryDirectionRenderer != null && _owner.HasSecondaryEndpointForActivePath())
                    {
                        _owner.Context.SecondaryDirectionRenderer.enabled = true;
                    }

                    _owner.UpdateDirectionLineIfEnabled();
                }
                else if (!_owner.State.IsDirectionLinePersistent)
                {
                    _owner.DisableDirectionRenderers();
                }
            }

            Color dynamicHoverColor = new Color(
                Mathf.Clamp01(_owner.State.BaseColor.r * 1.3f),
                Mathf.Clamp01(_owner.State.BaseColor.g * 1.3f),
                Mathf.Clamp01(_owner.State.BaseColor.b * 1.3f),
                _owner.State.BaseColor.a);

            ChangeColorSmooth(isHolding ? dynamicHoverColor :
                (_owner.State.IsMarkedAsWrong ? _owner.State.BlockedColor : _owner.State.BaseColor), duration);
        }

        public void PlayLoseAnimation()
        {
            if (_owner.State.CurrentState == ArrowLineVisualState.Escaping) return;

            _owner.State.CurrentState = ArrowLineVisualState.Idle;
            KillAllActiveTweens();
            ChangeColorSmooth(_owner.State.LoseColor, _owner.Context.LoseDuration);

            if (_owner.Context.VisualRoot != null)
            {
                _owner.Context.VisualRoot.DOScale(_owner.Context.LoseScaleTarget, _owner.Context.LoseDuration)
                    .SetId(_owner)
                    .SetEase(_owner.Context.LoseEase)
                    .SetLink(_owner.Context.VisualRoot.gameObject);
            }
        }

        public void PlayRestoreFromLoseAnimation(float duration)
        {
            if (_owner.State.CurrentState == ArrowLineVisualState.Escaping) return;

            _owner.State.CurrentState = ArrowLineVisualState.Idle;
            _owner.State.IsMarkedAsWrong = false;
            KillAllActiveTweens();

            if (_owner.Context.VisualRoot != null)
            {
                _owner.Context.VisualRoot.DOScale(1f, duration)
                    .SetId(_owner)
                    .SetEase(Ease.OutBack)
                    .SetLink(_owner.Context.VisualRoot.gameObject);
            }

            ChangeColorSmooth(_owner.State.BaseColor, duration);

            if (_owner.State.IsDirectionLinePersistent)
            {
                if (_owner.Context.PrimaryDirectionRenderer != null)
                {
                    _owner.Context.PrimaryDirectionRenderer.enabled = true;
                }

                if (_owner.Context.SecondaryDirectionRenderer != null && _owner.HasSecondaryEndpointForActivePath())
                {
                    _owner.Context.SecondaryDirectionRenderer.enabled = true;
                }

                _owner.UpdateDirectionLineIfEnabled();
            }
        }

        public void PlayFocusHighlight(bool isOn)
        {
            _scaleTween?.Kill();
            _focusGlowTween?.Kill();

            if (_owner.Context.VisualRoot == null) return;

            if (isOn)
            {
                _owner.Context.VisualRoot.DOScale(1.1f, 0.3f).SetEase(Ease.OutBack).SetId(_owner);
                _owner.Appearance.SetFlashIntensity(0.5f);
                _focusGlowTween = DOTween.To(() => _owner.Appearance.CurrentFlashIntensity,
                        x => _owner.Appearance.SetFlashIntensity(x), _owner.Context.FocusGlowIntensity, 0.4f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                _owner.Context.VisualRoot.DOScale(1f, 0.2f).SetEase(Ease.OutQuad).SetId(_owner);
                _owner.Appearance.SetFlashIntensity(0f);
            }
        }

        public void PlayHintEffect()
        {
            if (_owner.State.CurrentState != ArrowLineVisualState.Idle) return;

            KillAllActiveTweens();
            if (_owner.Context.VisualRoot == null) return;

            _owner.Context.VisualRoot.localScale = Vector3.one;
            _scaleTween = _owner.Context.VisualRoot.DOScale(1.15f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetId(_owner)
                .SetLink(_owner.Context.VisualRoot.gameObject);

            Color dynamicGlowColor = new Color(
                Mathf.Min(_owner.State.BaseColor.r * 1.5f, 2f),
                Mathf.Min(_owner.State.BaseColor.g * 1.5f, 2f),
                Mathf.Min(_owner.State.BaseColor.b * 1.5f, 2f),
                _owner.State.BaseColor.a);
            ChangeColorSmooth(dynamicGlowColor, 0.3f);

            _owner.Appearance.SetFlashIntensity(0f);
            _focusGlowTween = DOTween.To(() => _owner.Appearance.CurrentFlashIntensity,
                    x => _owner.Appearance.SetFlashIntensity(x), _owner.Context.HintGlowIntensity, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);
        }

        public void ForceToggleDirectionLine(bool isOn, float delay, bool skipPunchScale = false)
        {
            if (_owner.Context.PrimaryDirectionRenderer == null) return;

            _owner.Context.PrimaryDirectionRenderer.DOKill();
            _owner.Context.SecondaryDirectionRenderer?.DOKill();
            _owner.State.IsDirectionLinePersistent = isOn;

            if (isOn)
            {
                if (_owner.State.CurrentState != ArrowLineVisualState.Spawning &&
                    _owner.Context.VisualRoot != null &&
                    _owner.Context.VisualRoot.localScale.x < 0.1f)
                {
                    _owner.Context.VisualRoot.localScale = Vector3.one;
                }

                _owner.Context.PrimaryDirectionRenderer.enabled = true;
                if (_owner.Context.SecondaryDirectionRenderer != null && _owner.HasSecondaryEndpointForActivePath())
                {
                    _owner.Context.SecondaryDirectionRenderer.enabled = true;
                }

                _owner.State.DirectionLineProgress = 0f;
                _owner.UpdateDirectionLineIfEnabled();

                Sequence toggleSequence = DOTween.Sequence()
                    .SetId(_owner)
                    .SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);

                if (delay > 0f)
                {
                    toggleSequence.AppendInterval(delay);
                }

                if (!skipPunchScale && _owner.Context.VisualRoot != null)
                {
                    toggleSequence.Append(_owner.Context.VisualRoot.DOPunchScale(Vector3.one * 0.12f, 0.25f, 5, 0.5f));
                }

                toggleSequence.Join(DOTween.To(() => _owner.State.DirectionLineProgress, x =>
                    {
                        _owner.State.DirectionLineProgress = x;
                        _owner.UpdateDirectionLineIfEnabled();
                    }, 1f, 0.4f)
                    .SetEase(Ease.OutCubic));

                toggleSequence.AppendCallback(_owner.UpdateDirectionLineIfEnabled);
            }
            else
            {
                Sequence hideSequence = DOTween.Sequence()
                    .SetId(_owner)
                    .SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);

                hideSequence.Append(DOTween.To(() => _owner.State.DirectionLineProgress, x =>
                    {
                        _owner.State.DirectionLineProgress = x;
                        _owner.UpdateDirectionLineIfEnabled();
                    }, 0f, 0.25f)
                    .SetEase(Ease.InSine));
                hideSequence.AppendCallback(() =>
                {
                    _owner.DisableDirectionRenderers();
                    _owner.State.DirectionLineProgress = 1f;
                });
            }
        }

        public void ToggleTargetSelectionState(bool isSelecting)
        {
            if (_owner.State.CurrentState != ArrowLineVisualState.Idle) return;
            if (_owner.Context.VisualRoot == null) return;

            _owner.Context.VisualRoot.DOKill();
            _focusGlowTween?.Kill();

            if (isSelecting)
            {
                _owner.Context.VisualRoot.localScale = Vector3.one;
                _owner.Context.VisualRoot.DOScale(1.03f, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetId(_owner)
                    .SetLink(_owner.Context.VisualRoot.gameObject);

                Color pulseColor = new Color(
                    Mathf.Min(_owner.State.BaseColor.r * 1.5f, 2f),
                    Mathf.Min(_owner.State.BaseColor.g * 1.5f, 2f),
                    Mathf.Min(_owner.State.BaseColor.b * 1.5f, 2f),
                    _owner.State.BaseColor.a);
                ChangeColorSmooth(pulseColor, 0.3f);

                _owner.Appearance.SetFlashIntensity(0f);
                _focusGlowTween = DOTween.To(() => _owner.Appearance.CurrentFlashIntensity,
                        x => _owner.Appearance.SetFlashIntensity(x), _owner.Context.SelectionGlowIntensity, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);
            }
            else
            {
                _owner.Context.VisualRoot.localScale = Vector3.one;
                _owner.Appearance.ApplyColor(_owner.State.BaseColor);
                _owner.Appearance.SetFlashIntensity(0f);
            }
        }

        public void ChangeColorSmooth(Color targetColor, float duration)
        {
            _colorTween?.Kill();
            Color startColor = _owner.Context.HeadSpriteRenderer != null
                ? _owner.Context.HeadSpriteRenderer.color
                : Color.white;
            _colorTween = DOTween.To(() => startColor, x => _owner.Appearance.ApplyColor(x), targetColor, duration)
                .SetId(_owner)
                .SetLink(_owner.gameObject, LinkBehaviour.KillOnDisable);
        }

        public void KillAllActiveTweens()
        {
            _actionSequence?.Kill();
            _scaleTween?.Kill();
            _colorTween?.Kill();
            _focusGlowTween?.Kill();
            DOTween.Kill(_owner);

            if (_owner.Context.VisualRoot != null) _owner.Context.VisualRoot.DOKill();
            if (_owner.Context.HeadSpriteRenderer != null) _owner.Context.HeadSpriteRenderer.DOKill();
            if (_owner.Context.SecondaryEndpointMarker != null) _owner.Context.SecondaryEndpointMarker.DOKill();
            _owner.RendererPool.ForEachRenderer(renderer => renderer.DOKill());
            _owner.TrailPool.ForEachTrail(trail => trail.DOKill());

            _actionSequence = null;
            _scaleTween = null;
            _colorTween = null;
            _focusGlowTween = null;
        }
    }
}
