using ArrowGame.Data.Events;
using ArrowGame.Utils;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;
using DG.Tweening;

namespace ArrowGame.Gameplay.Visual
{
    public partial class ArrowLineView
    {
        public void PlaySpawnAnimation(float delay, float duration, bool showLine = false)
        {
            if (_currentState == ArrowState.Escaping || _bodyPoints == null || _bodyPoints.Length == 0) return;
            _currentState = ArrowState.Spawning;

            if (showLine && lineDirection != null)
            {
                _isDirectionLinePersistent = true;
                lineDirection.enabled = true;
                _directionLineProgress = 0f;
                UpdateDirectionLine();

                DOTween.To(() => _directionLineProgress, x =>
                    {
                        _directionLineProgress = x;
                        UpdateDirectionLine();
                    }, 1f, 0.5f)
                    .SetDelay(delay + 0.1f)
                    .SetEase(Ease.OutSine)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);
            }

            KillAllActiveTweens();
            ClearTraceRoute();

            _travelDistance = -_bodyLength;
            UpdateSnakeBody();

            float revealDuration = duration * spawnRevealRatio;

            headSpriteRenderer.color = new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f);
            headSpriteRenderer.DOFade(_baseColor.a, revealDuration).SetDelay(delay).SetId(this)
                .SetLink(headSpriteRenderer.gameObject);

            visualRoot.localScale = Vector3.zero;
            visualRoot.DOScale(1f, revealDuration).SetDelay(delay).SetEase(spawnScaleCurve).SetId(this)
                .SetLink(visualRoot.gameObject);

            _actionSequence = DOTween.Sequence().SetId(this).SetLink(gameObject, LinkBehaviour.KillOnDisable);
            _actionSequence.Insert(delay, DOTween.To(() => _travelDistance, x =>
            {
                _travelDistance = x;
                UpdateSnakeBody();
            }, 0f, duration).SetEase(spawnMoveCurve));

            _actionSequence.OnComplete(() => _currentState = ArrowState.Idle);
        }

        public void PlayEscapeAnimation()
        {
            if (_currentState == ArrowState.Escaping || _movementPoints == null || _movementPoints.Length == 0) return;
            _currentState = ArrowState.Escaping;

            KillAllActiveTweens();
            if (lineDirection != null) lineDirection.enabled = false;

            Vector3 routeEndWorld = transform.TransformPoint(_movementPoints[_movementPoints.Length - 1]);
            Vector3 exitDirection = _escapeDirection.sqrMagnitude > 0f ? _escapeDirection.normalized : Vector3.up;
            float distanceToEdge = CameraUtils.GetDistanceToEdge(_mainCam, routeEndWorld, exitDirection);

            // _travelDistance điều khiển vị trí của đuôi (tailDist) trong trạng thái Escaping.
            // Để đuôi mũi tên ra khỏi màn hình, nó cần đi hết chiều dài đường đi (_movementLength) 
            // cộng thêm khoảng cách từ điểm kết thúc tới mép màn hình (distanceToEdge).
            float targetDistance = _movementLength + distanceToEdge + (_cellSize * escapeExtraDistanceFactor);
            float moveDuration = Mathf.Max(0.05f, (targetDistance - _travelDistance) / escapeSpeed);

            _actionSequence = DOTween.Sequence().SetId(this).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            float flashUpTime = 0.02f;
            float flashDownTime = 0.1f;
            float maxFlashIntensity = 2f;

            _actionSequence.Insert(0f, DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x),
                maxFlashIntensity, flashUpTime).SetEase(Ease.OutFlash));
            _actionSequence.Insert(flashUpTime, DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x),
                0f, flashDownTime).SetEase(Ease.InQuad));

            _actionSequence.Append(DOTween.To(() => _travelDistance, x =>
            {
                _travelDistance = x;
                UpdateSnakeBody();
            }, pullbackOffset, pullbackDuration).SetEase(Ease.OutQuad));

            _actionSequence.AppendCallback(() =>
            {
                if (escapeTrail != null)
                {
                    escapeTrail.Clear();
                    escapeTrail.emitting = true;
                }
            });

            _actionSequence.Append(DOTween.To(() => _travelDistance, x =>
                {
                    _travelDistance = x;
                    UpdateSnakeBody();
                }, targetDistance, moveDuration)
                .SetEase(escapeMoveCurve)
                .OnStart(() => EventManager<VisualEventID>.Post(VisualEventID.ArrowEscaped)));

            _actionSequence.Insert(pullbackDuration + (moveDuration * fadeOutRatio),
                headSpriteRenderer.DOFade(0f, moveDuration * (1f - fadeOutRatio)));

            _actionSequence.OnComplete(() =>
            {
                _currentState = ArrowState.Idle;
                if (escapeTrail != null) escapeTrail.emitting = false;
                PoolingManager.Instance.Despawn(gameObject);
            });
        }

        public void PlayBlockedAnimation(float realBumpDistance)
        {
            if (_currentState == ArrowState.Escaping || _currentState == ArrowState.Spawning) return;
            _currentState = ArrowState.Blocked;
            _isMarkedAsWrong = true;

            KillAllActiveTweens();

            if (visualRoot != null)
            {
                visualRoot.localPosition = Vector3.zero;
                visualRoot.localScale = Vector3.one;
            }

            if (lineDirection != null) lineDirection.enabled = false;

            float bumpTime = baseBumpTime + (realBumpDistance * bumpDistMultiplier);
            _actionSequence = DOTween.Sequence().SetId(this).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            _actionSequence.Append(DOTween.To(() => _travelDistance, x =>
            {
                _travelDistance = x;
                UpdateSnakeBody();
            }, realBumpDistance, bumpTime).SetEase(bumpImpactCurve));

            _actionSequence.Join(DOTween.To(() => headSpriteRenderer.color, x => SetColor(x), _blockedColor, bumpTime));
            _actionSequence.AppendCallback(() => EventManager<VisualEventID>.Post(VisualEventID.ArrowWrongImpact));

            Transform targetShake = visualRoot != null ? visualRoot : transform;

            _actionSequence.Append(targetShake.DOShakePosition(shakeDuration, shakeStrength)).SetId(this);
            _actionSequence.Join(DOTween.To(() => _travelDistance, x =>
            {
                _travelDistance = x;
                UpdateSnakeBody();
            }, 0f, reboundDuration).SetEase(bumpReboundCurve));

            _actionSequence.OnComplete(() =>
            {
                _currentState = ArrowState.Idle;
                if (_isDirectionLinePersistent)
                {
                    if (lineDirection != null) lineDirection.enabled = true;
                    UpdateDirectionLine();
                }
            });
        }

        public void PlayCollisionFlash()
        {
            if (_currentState == ArrowState.Escaping) return;

            _colorTween?.Kill();
            
            // Nháy sang màu blocked nhanh và mờ dần về màu gốc
            Sequence flashSeq = DOTween.Sequence()
                .SetId(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            flashSeq.Append(DOTween.To(() => headSpriteRenderer.color, x => SetColor(x), _blockedColor, 0.1f).SetEase(Ease.OutQuad));
            flashSeq.Append(DOTween.To(() => headSpriteRenderer.color, x => SetColor(x), _baseColor, 0.4f).SetEase(Ease.InQuad));
        }

        public void PlayHoldEffect(bool isHolding)
        {
            if (_currentState != ArrowState.Idle) return;

            _scaleTween?.Kill();
            _focusGlowTween?.Kill();
            SetFlashIntensity(0f);

            float targetScale = isHolding ? holdScaleTarget : 1.0f;
            float duration = isHolding ? holdScaleDurationIn : holdScaleDurationOut;
            Ease ease = isHolding ? Ease.OutBack : Ease.OutQuad;

            _scaleTween = visualRoot.DOScale(targetScale, duration)
                .SetId(this)
                .SetEase(ease)
                .OnUpdate(() =>
                {
                    if (isHolding) UpdateDirectionLine();
                })
                .SetLink(visualRoot.gameObject);

            if (lineDirection != null)
            {
                if (isHolding)
                {
                    lineDirection.enabled = true;
                    UpdateDirectionLine();
                }
                else if (!_isDirectionLinePersistent)
                {
                    lineDirection.enabled = false;
                }
            }

            Color dynamicHoverColor = new Color(
                Mathf.Clamp01(_baseColor.r * 1.3f),
                Mathf.Clamp01(_baseColor.g * 1.3f),
                Mathf.Clamp01(_baseColor.b * 1.3f),
                _baseColor.a);

            ChangeColorSmooth(isHolding ? dynamicHoverColor : (_isMarkedAsWrong ? _blockedColor : _baseColor), duration);
        }

        public void PlayLoseAnimation()
        {
            if (_currentState == ArrowState.Escaping) return;
            _currentState = ArrowState.Idle;

            KillAllActiveTweens();
            ChangeColorSmooth(_loseColor, loseDuration);

            visualRoot.DOScale(loseScaleTarget, loseDuration)
                .SetId(this)
                .SetEase(loseEase)
                .SetLink(visualRoot.gameObject);
        }

        public void PlayFocusHighlight(bool isOn)
        {
            _scaleTween?.Kill();
            _focusGlowTween?.Kill();

            if (isOn)
            {
                visualRoot.DOScale(1.1f, 0.3f).SetEase(Ease.OutBack).SetId(this);
                SetFlashIntensity(0.5f);
                _focusGlowTween = DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x), 0.8f, 0.4f)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);
            }
            else
            {
                visualRoot.DOScale(1f, 0.2f).SetEase(Ease.OutQuad).SetId(this);
                SetFlashIntensity(0f);
            }
        }

        public void PlayHintEffect()
        {
            if (_currentState != ArrowState.Idle) return;
            KillAllActiveTweens();
            visualRoot.localScale = Vector3.one;

            _scaleTween = visualRoot.DOScale(1.15f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetId(this)
                .SetLink(visualRoot.gameObject);

            Color dynamicGlowColor = new Color(
                Mathf.Min(_baseColor.r * 1.5f, 2f),
                Mathf.Min(_baseColor.g * 1.5f, 2f),
                Mathf.Min(_baseColor.b * 1.5f, 2f),
                _baseColor.a);
            ChangeColorSmooth(dynamicGlowColor, 0.3f);

            SetFlashIntensity(0f);
            _focusGlowTween = DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x), 0.65f, 0.6f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        public void ForceToggleDirectionLine(bool isOn, float delay = 0f)
        {
            if (lineDirection == null) return;

            lineDirection.DOKill();
            // KHÔNG gọi visualRoot.DOKill() ở đây để tránh làm gián đoạn các hiệu ứng Scale khác (như Intro)
            
            _isDirectionLinePersistent = isOn;

            if (isOn)
            {
                if (_currentState != ArrowState.Spawning && visualRoot.localScale.x < 0.1f)
                {
                    visualRoot.localScale = Vector3.one;
                }
                lineDirection.enabled = true;
                _directionLineProgress = 0f;
                UpdateDirectionLine();

                Sequence toggleSeq = DOTween.Sequence()
                    .SetId(this)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);

                if (delay > 0f) toggleSeq.AppendInterval(delay);

                toggleSeq.Append(visualRoot.DOPunchScale(Vector3.one * 0.12f, 0.25f, 5, 0.5f));
                
                toggleSeq.Join(DOTween.To(() => _directionLineProgress, x =>
                    {
                        _directionLineProgress = x;
                        UpdateDirectionLine();
                    }, 1f, 0.4f)
                    .SetEase(Ease.OutCubic));

                toggleSeq.AppendCallback(UpdateDirectionLine);
            }
            else
            {
                Sequence hideSeq = DOTween.Sequence()
                    .SetId(this)
                    .SetLink(gameObject, LinkBehaviour.KillOnDisable);

                hideSeq.Append(DOTween.To(() => _directionLineProgress, x =>
                    {
                        _directionLineProgress = x;
                        UpdateDirectionLine();
                    }, 0f, 0.25f)
                    .SetEase(Ease.InSine));
                hideSeq.AppendCallback(() => 
                {
                    lineDirection.enabled = false;
                    _directionLineProgress = 1f;
                });
            }
        }

        public void ToggleTargetSelectionState(bool isSelecting)
        {
            if (_currentState != ArrowState.Idle) return;
            visualRoot.DOKill();
            _focusGlowTween?.Kill();

            if (isSelecting)
            {
                visualRoot.localScale = Vector3.one;
                visualRoot.DOScale(1.03f, 0.6f).SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine)
                    .SetId(this).SetLink(visualRoot.gameObject);

                Color pulseColor = new Color(Mathf.Min(_baseColor.r * 1.5f, 2f),
                    Mathf.Min(_baseColor.g * 1.5f, 2f), Mathf.Min(_baseColor.b * 1.5f, 2f), _baseColor.a);
                ChangeColorSmooth(pulseColor, 0.3f);

                SetFlashIntensity(0f);
                _focusGlowTween = DOTween.To(() => _currentFlashIntensity, x => SetFlashIntensity(x), 0.65f, 0.6f)
                    .SetLoops(-1, LoopType.Yoyo).SetEase(Ease.InOutSine).SetLink(gameObject,
                        LinkBehaviour.KillOnDisable);
            }
            else
            {
                visualRoot.localScale = Vector3.one;
                ResetColor();
                SetFlashIntensity(0f);
            }
        }

        public void SetFlashIntensity(float intensity)
        {
            _currentFlashIntensity = intensity;
            if (lineRenderer != null)
            {
                lineRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FlashIntensityId, _currentFlashIntensity);
                lineRenderer.SetPropertyBlock(_mpb);
            }

            if (headSpriteRenderer != null)
            {
                headSpriteRenderer.GetPropertyBlock(_mpb);
                _mpb.SetFloat(FlashIntensityId, _currentFlashIntensity);
                headSpriteRenderer.SetPropertyBlock(_mpb);
            }
        }

        private void KillAllActiveTweens()
        {
            _actionSequence?.Kill();
            _scaleTween?.Kill();
            _colorTween?.Kill();
            _focusGlowTween?.Kill();
            DOTween.Kill(this);

            if (visualRoot != null) visualRoot.DOKill();
            if (headSpriteRenderer != null) headSpriteRenderer.DOKill();
            if (lineRenderer != null) lineRenderer.DOKill();

            _actionSequence = null;
            _scaleTween = null;
            _colorTween = null;
        }

        private void SetColor(Color color)
        {
            if (lineRenderer != null) lineRenderer.startColor = lineRenderer.endColor = color;
            if (headSpriteRenderer != null) headSpriteRenderer.color = color;

            if (escapeTrail != null)
            {
                Gradient trailGradient = new Gradient();
                GradientColorKey[] colorKeys =
                {
                    new GradientColorKey(color, 0.0f),
                    new GradientColorKey(color, 1.0f)
                };
                GradientAlphaKey[] alphaKeys =
                {
                    new GradientAlphaKey(color.a * 0.8f, 0.0f),
                    new GradientAlphaKey(0.0f, 1.0f)
                };

                trailGradient.SetKeys(colorKeys, alphaKeys);
                escapeTrail.colorGradient = trailGradient;
            }
        }

        private void ResetColor() => SetColor(_baseColor);

        private void ChangeColorSmooth(Color targetColor, float duration)
        {
            _colorTween?.Kill();
            Color startColor = headSpriteRenderer != null ? headSpriteRenderer.color : Color.white;
            _colorTween = DOTween.To(() => startColor, x => SetColor(x), targetColor, duration)
                .SetId(this)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }
    }
}
