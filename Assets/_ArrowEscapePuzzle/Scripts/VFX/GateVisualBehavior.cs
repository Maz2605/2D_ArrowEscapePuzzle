using ArrowGame.Data.Booster;
using ArrowGame.Data.VFX;
using UnityEngine;
using ArrowGame.Gameplay.Visual;
using DG.Tweening;
using GameCore.Utils.DesignPattern.ObjectPooling;

namespace ArrowGame.Gameplay.VFX
{
    public class GateVisualBehavior : MonoBehaviour, ISingleTargetVFX
    {
        private Transform _slurpAnchor;

        public void PlayVisual(ArrowLineView arrowView, Vector3 spawnPos, string arrowId, float totalDuration)
        {
            arrowView.transform.DOKill(); 
            Transform originalParent = arrowView.transform.parent;

            _slurpAnchor = new GameObject("SlurpAnchor_" + arrowId).transform;
            _slurpAnchor.position = spawnPos;
            arrowView.transform.SetParent(_slurpAnchor, true);

            Vector3 escapeDir = arrowView.EscapeDirection;
            bool isPinned = false;

            arrowView.PlayEscapeAnimation(); 

            DOVirtual.Float(0, 1, totalDuration, (t) => {
                if (arrowView == null || _slurpAnchor == null) return;

                Vector3 diff = arrowView.HeadPosition - spawnPos;
                float overshoot = Vector3.Dot(diff, escapeDir);

                if (overshoot > 0)
                {
                    _slurpAnchor.position -= escapeDir * overshoot;

                    if (!isPinned) {
                        isPinned = true;
                        float timeLeft = totalDuration - (t * totalDuration); 
                        _slurpAnchor.DOScale(0f, Mathf.Max(0.1f, timeLeft)).SetEase(Ease.OutCubic);
                    }
                }
            }).OnComplete(() => {
                if (arrowView != null) 
                {
                    arrowView.transform.SetParent(originalParent, true); 
                    arrowView.transform.localScale = Vector3.one; 
                    arrowView.transform.localRotation = Quaternion.identity;
                }
                
                if (_slurpAnchor != null) Destroy(_slurpAnchor.gameObject); 
                PoolingManager.Instance.Despawn(this.gameObject);
            }).SetLink(this.gameObject, LinkBehaviour.KillOnDisable); 
        }

        private void OnDisable()
        {
            if (_slurpAnchor != null) Destroy(_slurpAnchor.gameObject);
        }
    }
}