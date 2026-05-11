using ShareCore.Scripts.Data;
using UnityEngine;
using DG.Tweening;
using ArrowGame.Data.Events;
using GameCore.Utils.DesignPattern.Events;

namespace ArrowGame.Gameplay.Visual
{
    public class CounterBlockView : SpecialCellViewBase
    {
        private Vector2Int _gridPos;
        private Color _baseColor;

        [Header("--- HIT ANIMATION SETTINGS ---")]
        [SerializeField] private AnimationCurve impactAxisCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.2f, 0.8f),
            new Keyframe(0.6f, 1.15f),
            new Keyframe(1f, 1f)
        );
        
        [SerializeField] private AnimationCurve perpendicularAxisCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.2f, 1.2f),
            new Keyframe(0.6f, 0.9f),
            new Keyframe(1f, 1f)
        );
        
        [SerializeField] private float hitAnimDuration = 0.32f;

        protected override void Awake()
        {
            base.Awake();
            EventManager<LogicGameEventID>.AddListener<(SpecialCellSaveData, Vector2Int)>(LogicGameEventID.SpecialCellChanged, OnSpecialCellChanged);
            EventManager<LogicGameEventID>.AddListener<Vector2Int>(LogicGameEventID.SpecialCellDestroyed, OnSpecialCellDestroyed);
        }

        private void OnDestroy()
        {
            EventManager<LogicGameEventID>.RemoveListener<(SpecialCellSaveData, Vector2Int)>(LogicGameEventID.SpecialCellChanged, OnSpecialCellChanged);
            EventManager<LogicGameEventID>.RemoveListener<Vector2Int>(LogicGameEventID.SpecialCellDestroyed, OnSpecialCellDestroyed);
        }

        protected override void ApplyVisual(SpecialCellSaveData specialCell, Color color)
        {
            _gridPos = specialCell.Position;
            _baseColor = color;
            UpdateText(specialCell.Counter);
            
            if (BackgroundRenderer != null)
                BackgroundRenderer.color = color;
        }

        private void UpdateText(int counter)
        {
            if (Label != null)
                Label.text = counter.ToString();
        }

        private void OnSpecialCellChanged((SpecialCellSaveData data, Vector2Int dir) payload)
        {
            if (payload.data != null && payload.data.Position == _gridPos)
            {
                UpdateText(payload.data.Counter);
                PlayHitAnimation(payload.dir);
            }
        }

        public void PlayHitAnimation(Vector2Int dir, Color? blockedColor = null)
        {
            transform.DOKill();
            transform.localScale = TargetScale; 

            // 1. Chớp màu (Flash)
            if (BackgroundRenderer != null)
            {
                BackgroundRenderer.DOKill();
                if (blockedColor.HasValue)
                {
                    Sequence flashSeq = DOTween.Sequence().SetLink(gameObject);
                    flashSeq.Append(BackgroundRenderer.DOColor(blockedColor.Value, 0.1f).SetEase(Ease.OutQuad));
                    flashSeq.Append(BackgroundRenderer.DOColor(_baseColor, 0.4f).SetEase(Ease.InQuad));
                }
                else
                {
                    PlayHighlight();
                }
            }

            // 2. Chạy Animation theo Curve
            DOTween.To(() => 0f, t => {
                float impactScale = impactAxisCurve.Evaluate(t);
                float perpScale = perpendicularAxisCurve.Evaluate(t);
                
                if (dir.x != 0) // Hướng ngang
                {
                    transform.localScale = new Vector3(TargetScale.x * impactScale, TargetScale.y * perpScale, 1f);
                }
                else // Hướng dọc hoặc mặc định
                {
                    transform.localScale = new Vector3(TargetScale.x * perpScale, TargetScale.y * impactScale, 1f);
                }
            }, 1f, hitAnimDuration).SetEase(Ease.Linear).SetLink(gameObject);
        }

        private void OnSpecialCellDestroyed(Vector2Int pos)
        {
            if (pos == _gridPos)
            {
                transform.DOKill();
                // Animation biến mất: Co lại về 0 và xoay nhẹ
                transform.DOScale(Vector3.zero, 0.25f).SetEase(Ease.InBack);
                transform.DORotate(new Vector3(0, 0, 90f), 0.25f).SetEase(Ease.InBack).OnComplete(() =>
                {
                    // Chỗ này nếu có Particle Burst màu Neon thì gọi ở đây
                    Destroy(gameObject);
                });
            }
        }
    }
}