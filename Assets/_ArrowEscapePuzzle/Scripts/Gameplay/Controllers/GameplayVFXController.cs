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
        
        
        

        private void OnEnable()
        {
            EventManager<VisualEventID>.AddListener<VFXRequestPayload>(VisualEventID.PlayBoosterVFX, HandleVFXRequest);
            EventManager<VisualEventID>.AddListener<VFXChainRequestPayload>(VisualEventID.PlayChainBoosterVFX,
                HandleChainVFXRequest);
            
        }

        

        private void OnDisable()
        {
            EventManager<VisualEventID>.RemoveListener<VFXRequestPayload>(VisualEventID.PlayBoosterVFX,
                HandleVFXRequest);
            EventManager<VisualEventID>.RemoveListener<VFXChainRequestPayload>(VisualEventID.PlayChainBoosterVFX,
                HandleChainVFXRequest);
            
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