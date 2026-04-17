using System.Collections.Generic;
using ArrowGame.Data.Booster;
using UnityEngine;
using ArrowGame.Data.Events;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Visual;
using ArrowGame.VFX;
using GameCore.Utils.DesignPattern.Events;
using GameCore.Utils.DesignPattern.ObjectPooling;

namespace ArrowGame.Gameplay.Controllers
{
    public class GameplayVFXController : MonoBehaviour
    {
        [SerializeField] private GridView _gridView;
        [Header("Tap VFX Config")]
        [SerializeField] private GameObject _tapAuraPrefab;
        [SerializeField] private float _tapAuraDuration = 0.5f;

        private void OnEnable()
        {
            EventManager<VisualEventID>.AddListener<VFXRequestPayload>(VisualEventID.PlayBoosterVFX, HandleVFXRequest);
            EventManager<VisualEventID>.AddListener<VFXChainRequestPayload>(VisualEventID.PlayChainBoosterVFX,
                HandleChainVFXRequest);
            EventManager<VisualEventID>.AddListener<TapVFXPayload>(VisualEventID.PlayTapAuraVFX, HandleTapAuraVFX);
        }

        

        private void OnDisable()
        {
            EventManager<VisualEventID>.RemoveListener<VFXRequestPayload>(VisualEventID.PlayBoosterVFX,
                HandleVFXRequest);
            EventManager<VisualEventID>.RemoveListener<VFXChainRequestPayload>(VisualEventID.PlayChainBoosterVFX,
                HandleChainVFXRequest);
            EventManager<VisualEventID>.RemoveListener<TapVFXPayload>(VisualEventID.PlayTapAuraVFX, HandleTapAuraVFX);
        }

        private void HandleVFXRequest(VFXRequestPayload payload)
        {
            if (payload.Config.prefab == null) return;

            var arrowView = _gridView.GetArrowViewById(payload.TargetArrowId);
            if (arrowView == null) return;

            Vector3 dir = arrowView.EscapeDirection;
            Vector3 spawnPos = arrowView.HeadPosition + (dir * payload.Config.offsetDistance);

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            Quaternion spawnRot = Quaternion.Euler(0, 0, angle);

            GameObject vfxInstance = PoolingManager.Instance.Spawn(payload.Config.prefab, spawnPos, spawnRot);
            ISingleTargetVFX visualBehavior = vfxInstance.GetComponent<ISingleTargetVFX>();

            if (visualBehavior != null)
            {
                visualBehavior.PlayVisual(arrowView, spawnPos, payload.TargetArrowId, payload.Config.duration);
            }
            else
            {
                Debug.LogError(
                    $"[VFX Controller] Prefab {payload.Config.prefab.name} thiếu script IBoosterVisualBehavior!");
            }
        }
        private void HandleTapAuraVFX(TapVFXPayload obj)
        {
            if (_tapAuraPrefab == null) return;

            // Gọi GlobalVFXManager để mượn Object Pooling. Rất clean và tối ưu.
            Managers.GlobalVFXManager.Instance.PlayVFX(
                _tapAuraPrefab, 
                obj.WorldPosition, 
                Quaternion.identity, 
                _tapAuraDuration
            );
        }

        private void HandleChainVFXRequest(VFXChainRequestPayload payload)
        {
            if (payload.Config.prefab == null || payload.TargetArrowIds.Count == 0) return;

            // Lấy nguyên danh sách Object hình ảnh của mũi tên
            List<ArrowLineView> targetViews = new List<ArrowLineView>();

            foreach (var id in payload.TargetArrowIds)
            {
                var arrowView = _gridView.GetArrowViewById(id);
                if (arrowView != null)
                {
                    targetViews.Add(arrowView);
                }
            }

            if (targetViews.Count < 1) return;

            GameObject vfxInstance =
                PoolingManager.Instance.Spawn(payload.Config.prefab, Vector3.zero, Quaternion.identity);
            var multiBehavior = vfxInstance.GetComponent<IMultiTargetVFX>();

            if (multiBehavior != null)
            {
                multiBehavior.PlayMultiVisual(targetViews, payload.Config.duration);
            }
            else
            {
                Debug.LogError($"[VFX Controller] Prefab {payload.Config.prefab.name} thiếu script IMultiTargetVFX!");
            }
        }
    }
}