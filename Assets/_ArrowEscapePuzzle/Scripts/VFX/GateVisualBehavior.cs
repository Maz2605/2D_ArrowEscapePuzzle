using ArrowGame.Gameplay.Visual;
using DG.Tweening;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;

namespace ArrowGame.Gameplay.VFX
{
    public class GateVisualBehavior : MonoBehaviour
    {
        private Transform _slurpAnchor;
        private Transform _originalParent;
        private ArrowLineView _activeArrowView;

        public void PlayVisual(ArrowLineView arrowView, Vector3 spawnPos, string arrowId, float totalDuration)
        {
            arrowView.transform.DOKill();
            _activeArrowView = arrowView;
            _originalParent = arrowView.transform.parent;

            _slurpAnchor = new GameObject("SlurpAnchor_" + arrowId).transform;
            _slurpAnchor.position = spawnPos;
            arrowView.transform.SetParent(_slurpAnchor, true);

            Vector3 escapeDir = arrowView.EscapeDirection;
            bool isPinned = false;

            arrowView.PlayEscapeAnimation();

            DOVirtual.Float(0f, 1f, totalDuration, t =>
            {
                if (arrowView == null || _slurpAnchor == null) return;

                Vector3 diff = arrowView.HeadPosition - spawnPos;
                float overshoot = Vector3.Dot(diff, escapeDir);

                if (overshoot <= 0f) return;

                _slurpAnchor.position -= escapeDir * overshoot;

                if (isPinned) return;

                isPinned = true;
                float timeLeft = totalDuration - (t * totalDuration);
                _slurpAnchor.DOScale(0f, Mathf.Max(0.1f, timeLeft)).SetEase(Ease.OutCubic);
            }).OnComplete(() =>
            {
                if (arrowView != null)
                {
                    RestoreArrowTransform();
                }

                if (_slurpAnchor != null) Destroy(_slurpAnchor.gameObject);
                _activeArrowView = null;
                _originalParent = null;
                PoolingManager.Instance.Despawn(gameObject);
            }).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void OnDisable()
        {
            RestoreArrowTransform();
            if (_slurpAnchor != null) Destroy(_slurpAnchor.gameObject);
            _activeArrowView = null;
            _originalParent = null;
        }

        private void RestoreArrowTransform()
        {
            if (_activeArrowView == null) return;

            if (_originalParent != null)
            {
                _activeArrowView.transform.SetParent(_originalParent, true);
            }

            _activeArrowView.transform.localScale = Vector3.one;
            _activeArrowView.transform.localRotation = Quaternion.identity;
        }
    }
}
