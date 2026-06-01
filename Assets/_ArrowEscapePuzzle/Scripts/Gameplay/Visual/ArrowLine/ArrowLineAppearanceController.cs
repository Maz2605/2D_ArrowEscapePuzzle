using System;
using ArrowGame.Gameplay.Logic;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    internal sealed class ArrowLineAppearanceController
    {
        private static readonly int FlashIntensityId = Shader.PropertyToID("_FlashIntensity");

        private readonly ArrowLineViewContext _context;
        private readonly ArrowLineRendererPool _rendererPool;
        private readonly ArrowTrailPool _trailPool;
        private readonly MaterialPropertyBlock _propertyBlock = new MaterialPropertyBlock();
        private readonly Vector3 _defaultHeadLocalScale;
        private float _currentFlashIntensity;

        public ArrowLineAppearanceController(ArrowLineViewContext context, ArrowLineRendererPool rendererPool, ArrowTrailPool trailPool)
        {
            _context = context;
            _rendererPool = rendererPool;
            _trailPool = trailPool;
            _defaultHeadLocalScale = context.HeadTransform != null ? context.HeadTransform.localScale : Vector3.one;
        }

        public float CurrentFlashIntensity => _currentFlashIntensity;
        public Vector3 DefaultHeadLocalScale => _defaultHeadLocalScale;

        public void ResetVisualState(Color baseColor)
        {
            ApplyColor(baseColor);
            SetFlashIntensity(0f);
            ResetHeadScale();
        }

        public void ApplyColor(Color color)
        {
            if (_context.BodyRenderer != null)
            {
                _context.BodyRenderer.startColor = color;
                _context.BodyRenderer.endColor = color;
            }

            if (_context.HeadSpriteRenderer != null)
            {
                _context.HeadSpriteRenderer.color = color;
            }

            if (_context.SecondaryEndpointMarker != null && _context.SecondaryEndpointMarker.enabled)
            {
                _context.SecondaryEndpointMarker.color = color;
            }

            _rendererPool.ForEachBodyRenderer(renderer =>
            {
                renderer.startColor = color;
                renderer.endColor = color;
            });

            Color directionColor = color;
            directionColor.a = color.a * _context.DirectionLineAlphaMultiplier;
            _rendererPool.ForEachDirectionRenderer(renderer =>
            {
                renderer.startColor = directionColor;
                renderer.endColor = directionColor;
            });

            if (_context.PrimaryEscapeTrail != null)
            {
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new[]
                    {
                        new GradientColorKey(color, 0f),
                        new GradientColorKey(color, 1f)
                    },
                    new[]
                    {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0f, 1f)
                    });

                _trailPool.ForEachTrail(trail => trail.colorGradient = gradient);
            }
        }

        public void SetFlashIntensity(float intensity)
        {
            _currentFlashIntensity = intensity;

            _rendererPool.ForEachRenderer(renderer =>
            {
                renderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(FlashIntensityId, _currentFlashIntensity);
                renderer.SetPropertyBlock(_propertyBlock);
            });

            if (_context.HeadSpriteRenderer != null)
            {
                _context.HeadSpriteRenderer.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(FlashIntensityId, _currentFlashIntensity);
                _context.HeadSpriteRenderer.SetPropertyBlock(_propertyBlock);
            }

            if (_context.SecondaryEndpointMarker != null)
            {
                _context.SecondaryEndpointMarker.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(FlashIntensityId, _currentFlashIntensity);
                _context.SecondaryEndpointMarker.SetPropertyBlock(_propertyBlock);
            }

            _trailPool.ForEachTrail(trail =>
            {
                trail.GetPropertyBlock(_propertyBlock);
                _propertyBlock.SetFloat(FlashIntensityId, _currentFlashIntensity);
                trail.SetPropertyBlock(_propertyBlock);
            });
        }

        public void ResetHeadScale()
        {
            if (_context.HeadTransform != null)
            {
                _context.HeadTransform.localScale = _defaultHeadLocalScale;
            }
        }

        public void UpdateTeleportHeadFeel(EscapeTraceResult activeTraceResult, ArrowLinePathPresenter presenter,
            Vector3[] bodyPoints, float cellSize, float headDist)
        {
            if (_context.HeadTransform == null)
            {
                return;
            }

            Vector3 targetScale = _defaultHeadLocalScale;
            if (activeTraceResult == null || activeTraceResult.PortalJumps == null ||
                activeTraceResult.PortalJumps.Count == 0 || bodyPoints == null || cellSize <= Mathf.Epsilon)
            {
                _context.HeadTransform.localScale = targetScale;
                return;
            }

            float feelDistance = cellSize * _context.TeleportFeelDistanceFactor;
            if (feelDistance <= Mathf.Epsilon)
            {
                _context.HeadTransform.localScale = targetScale;
                return;
            }

            for (int i = 0; i < activeTraceResult.PortalJumps.Count; i++)
            {
                EscapeTracePortalJump jump = activeTraceResult.PortalJumps[i];
                int entryPointIndex = bodyPoints.Length + jump.EntryWaypointIndex;
                int exitPointIndex = bodyPoints.Length + jump.ExitWaypointIndex;

                if (entryPointIndex < 0 || exitPointIndex < 0 ||
                    entryPointIndex >= presenter.MovementDistanceCount ||
                    exitPointIndex >= presenter.MovementDistanceCount)
                {
                    continue;
                }

                float entryDistance = presenter.GetMovementDistance(entryPointIndex);
                float exitDistance = presenter.GetMovementDistance(exitPointIndex);

                if (headDist <= entryDistance)
                {
                    float entryOffset = entryDistance - headDist;
                    if (entryOffset <= feelDistance)
                    {
                        float t = 1f - Mathf.Clamp01(entryOffset / feelDistance);
                        _context.HeadTransform.localScale = GetTeleportHeadScale(_context.TeleportHeadCompressRatio * t);
                        return;
                    }
                }
                else if (headDist > exitDistance)
                {
                    float exitOffset = headDist - exitDistance;
                    if (exitOffset <= feelDistance)
                    {
                        float t = 1f - Mathf.Clamp01(exitOffset / feelDistance);
                        _context.HeadTransform.localScale = GetTeleportHeadScale(-_context.TeleportHeadReleaseRatio * t);
                        return;
                    }
                }
            }

            _context.HeadTransform.localScale = targetScale;
        }

        private Vector3 GetTeleportHeadScale(float squash)
        {
            float stretch = Mathf.Max(0.01f, 1f - squash);
            float widen = 1f + (squash * 0.75f);
            return new Vector3(
                _defaultHeadLocalScale.x * widen,
                _defaultHeadLocalScale.y * stretch,
                _defaultHeadLocalScale.z);
        }
    }
}
