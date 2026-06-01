using DG.Tweening;
using UnityEngine;

namespace ArrowGame.Gameplay.Visual
{
    internal sealed class ArrowLineViewContext
    {
        public ArrowLineViewContext(
            Transform visualRoot,
            LineRenderer bodyRenderer,
            Transform headTransform,
            SpriteRenderer headSpriteRenderer,
            LineRenderer primaryDirectionRenderer,
            LineRenderer secondaryDirectionRenderer,
            SpriteRenderer secondaryEndpointMarker,
            TrailRenderer primaryEscapeTrail,
            Camera mainCamera,
            float escapeSpeed,
            float pullbackOffset,
            float pullbackDuration,
            float fadeOutRatio,
            float escapeExtraDistanceFactor,
            float bumpDuration,
            float shakeDuration,
            float shakeStrength,
            float spawnRevealRatio,
            float holdScaleTarget,
            float holdScaleDurationIn,
            float holdScaleDurationOut,
            float loseScaleTarget,
            float loseDuration,
            Ease loseEase,
            AnimationCurve spawnScaleCurve,
            AnimationCurve spawnMoveCurve,
            AnimationCurve bumpCurve,
            AnimationCurve escapeMoveCurve,
            float escapeFlashIntensity,
            float escapeFlashPeakIntensity,
            float collisionFlashIntensity,
            float focusGlowIntensity,
            float hintGlowIntensity,
            float selectionGlowIntensity,
            float teleportHeadCompressRatio,
            float teleportHeadReleaseRatio,
            float teleportFeelDistanceFactor,
            float teleportBoundaryHeadLengthFactor,
            float directionLineAlphaMultiplier)
        {
            VisualRoot = visualRoot;
            BodyRenderer = bodyRenderer;
            HeadTransform = headTransform;
            HeadSpriteRenderer = headSpriteRenderer;
            PrimaryDirectionRenderer = primaryDirectionRenderer;
            SecondaryDirectionRenderer = secondaryDirectionRenderer;
            SecondaryEndpointMarker = secondaryEndpointMarker;
            PrimaryEscapeTrail = primaryEscapeTrail;
            MainCamera = mainCamera;
            EscapeSpeed = escapeSpeed;
            PullbackOffset = pullbackOffset;
            PullbackDuration = pullbackDuration;
            FadeOutRatio = fadeOutRatio;
            EscapeExtraDistanceFactor = escapeExtraDistanceFactor;
            BumpDuration = bumpDuration;
            ShakeDuration = shakeDuration;
            ShakeStrength = shakeStrength;
            SpawnRevealRatio = spawnRevealRatio;
            HoldScaleTarget = holdScaleTarget;
            HoldScaleDurationIn = holdScaleDurationIn;
            HoldScaleDurationOut = holdScaleDurationOut;
            LoseScaleTarget = loseScaleTarget;
            LoseDuration = loseDuration;
            LoseEase = loseEase;
            SpawnScaleCurve = spawnScaleCurve;
            SpawnMoveCurve = spawnMoveCurve;
            BumpCurve = bumpCurve;
            EscapeMoveCurve = escapeMoveCurve;
            EscapeFlashIntensity = escapeFlashIntensity;
            EscapeFlashPeakIntensity = escapeFlashPeakIntensity;
            CollisionFlashIntensity = collisionFlashIntensity;
            FocusGlowIntensity = focusGlowIntensity;
            HintGlowIntensity = hintGlowIntensity;
            SelectionGlowIntensity = selectionGlowIntensity;
            TeleportHeadCompressRatio = teleportHeadCompressRatio;
            TeleportHeadReleaseRatio = teleportHeadReleaseRatio;
            TeleportFeelDistanceFactor = teleportFeelDistanceFactor;
            TeleportBoundaryHeadLengthFactor = teleportBoundaryHeadLengthFactor;
            DirectionLineAlphaMultiplier = directionLineAlphaMultiplier;
        }

        public Transform VisualRoot { get; }
        public LineRenderer BodyRenderer { get; }
        public Transform HeadTransform { get; }
        public SpriteRenderer HeadSpriteRenderer { get; }
        public LineRenderer PrimaryDirectionRenderer { get; }
        public LineRenderer SecondaryDirectionRenderer { get; }
        public SpriteRenderer SecondaryEndpointMarker { get; }
        public TrailRenderer PrimaryEscapeTrail { get; }
        public Camera MainCamera { get; }

        public float EscapeSpeed { get; }
        public float PullbackOffset { get; }
        public float PullbackDuration { get; }
        public float FadeOutRatio { get; }
        public float EscapeExtraDistanceFactor { get; }
        public float BumpDuration { get; }
        public float ShakeDuration { get; }
        public float ShakeStrength { get; }
        public float SpawnRevealRatio { get; }
        public float HoldScaleTarget { get; }
        public float HoldScaleDurationIn { get; }
        public float HoldScaleDurationOut { get; }
        public float LoseScaleTarget { get; }
        public float LoseDuration { get; }
        public Ease LoseEase { get; }
        public AnimationCurve SpawnScaleCurve { get; }
        public AnimationCurve SpawnMoveCurve { get; }
        public AnimationCurve BumpCurve { get; }
        public AnimationCurve EscapeMoveCurve { get; }
        public float EscapeFlashIntensity { get; }
        public float EscapeFlashPeakIntensity { get; }
        public float CollisionFlashIntensity { get; }
        public float FocusGlowIntensity { get; }
        public float HintGlowIntensity { get; }
        public float SelectionGlowIntensity { get; }
        public float TeleportHeadCompressRatio { get; }
        public float TeleportHeadReleaseRatio { get; }
        public float TeleportFeelDistanceFactor { get; }
        public float TeleportBoundaryHeadLengthFactor { get; }
        public float DirectionLineAlphaMultiplier { get; }
    }
}
