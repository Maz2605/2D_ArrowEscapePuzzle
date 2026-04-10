using System.Collections.Generic;
using ArrowGame.Data.VFX;
using ArrowGame.Gameplay.Visual;
using ArrowGame.Gameplay.VFX; 
using DG.Tweening;
using GameCore.Utils.DesignPattern.ObjectPooling;
using UnityEngine;

namespace ArrowGame.VFX
{
    [RequireComponent(typeof(LineRenderer))]
    public class ChainLightningVFX : MonoBehaviour, IMultiTargetVFX
    {
        [Header("--- Lightning Settings ---")]
        public float zigzagMultiplier = 0.4f; 
        public int segmentsPerUnit = 5;
        public float flickerInterval = 0.04f; 

        [Header("--- EXTREME JUICE (Game Feel) ---")]
        public float minWidth = 0.2f; 
        public float maxWidth = 0.8f; 
        public bool useHitStop = true; 
        public float maxFlashIntensity = 3f; 
        public float flashFadeDuration = 0.2f; 
        
        [Header("--- Color Strobing ---")]
        public Color coreColor = Color.white;
        public Color auraColor = new Color(0f, 0.8f, 1f, 1f); 

        private LineRenderer _lr;
        private Camera _mainCam;
        
        private readonly List<Vector3> _targetPointsCache = new List<Vector3>(20);
        private readonly List<ArrowLineView> _activeArrowsCache = new List<ArrowLineView>(20); 
        private Vector3[] _renderPositionsCache = new Vector3[100]; 
        private readonly Dictionary<ArrowLineView, Vector3> _originalPositions = new Dictionary<ArrowLineView, Vector3>();
        
        private bool _isZapping = false;
        private float _flickerTimer = 0f;
        private float _currentFlash = 0f;

        private void Awake()
        {
            _lr = GetComponent<LineRenderer>();
            _mainCam = Camera.main; 
        }

        public void PlayMultiVisual(List<ArrowLineView> arrows, float totalDuration)
        {
            _lr.useWorldSpace = true;
            _targetPointsCache.Clear();
            _originalPositions.Clear();
            _activeArrowsCache.Clear();
            
            if (useHitStop)
            {
                Time.timeScale = 0.02f;
                DOVirtual.DelayedCall(0.12f, () => Time.timeScale = 1f).SetUpdate(true); 
            }
            
            if (_mainCam != null)
            {
                _mainCam.transform.DOKill();
                _mainCam.transform.DOShakePosition(0.2f, 0.3f, 30, 90, false, true).SetUpdate(true);
            }

            foreach (var arr in arrows)
            {
                if (arr == null) continue;
                
                arr.transform.DOKill();
                DOTween.Kill(arr);

                _targetPointsCache.Add(arr.HeadPosition);
                _originalPositions[arr] = arr.transform.localPosition;
                _activeArrowsCache.Add(arr); 
                
                arr.transform.DOShakePosition(totalDuration * 0.7f, 0.2f, 35, 90, false, true)
                    .SetUpdate(true)
                    .SetLink(arr.gameObject, LinkBehaviour.KillOnDisable);
                arr.transform.DOPunchScale(new Vector3(0.4f, 0.4f, 0f), 0.2f, 10, 1f)
                    .SetUpdate(true)
                    .SetLink(arr.gameObject, LinkBehaviour.KillOnDisable);
            }
            
            if (_targetPointsCache.Count == 1 && arrows.Count == 1)
            {
                var arr = arrows[0];
                Vector3 bodyPos = arr.HeadPosition - arr.EscapeDirection * 2.5f; // Mồi dài hơn để sét vuốt dọc thân nhìn cho bạo lực
                _targetPointsCache.Insert(0, bodyPos);
            }

            _currentFlash = maxFlashIntensity;
            _isZapping = true;
            _flickerTimer = 0f;
            DrawLightning(); 
            
            float shrinkDuration = totalDuration * 0.3f; 
            float zapDuration = totalDuration - shrinkDuration;

            DOVirtual.DelayedCall(zapDuration, () => 
            {
                _isZapping = false; 
                _lr.positionCount = 0; 

                foreach (var arr in arrows)
                {
                    if (arr == null || !arr.gameObject.activeInHierarchy) continue;

                    arr.transform.DOKill(); 
                    
                    Vector3 headTargetPos = arr.HeadPosition; 
                    Vector3 safeLocalPos = _originalPositions.ContainsKey(arr) ? _originalPositions[arr] : Vector3.zero;
                    
                    arr.transform.DOScale(0f, shrinkDuration).SetEase(Ease.InBack).SetUpdate(true);
                    arr.transform.DOMove(headTargetPos, shrinkDuration).SetEase(Ease.InBack).SetUpdate(true)
                        .OnComplete(() => 
                        {
                            if (arr != null)
                            {
                                arr.transform.localScale = Vector3.one;
                                arr.transform.localPosition = safeLocalPos; 
                                arr.transform.localRotation = Quaternion.identity;
                                PoolingManager.Instance.Despawn(arr.gameObject);
                            }
                        }).SetLink(arr.gameObject, LinkBehaviour.KillOnDisable);
                }

                DOVirtual.DelayedCall(shrinkDuration, () => 
                {
                    PoolingManager.Instance.Despawn(gameObject);
                }).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);

            }).SetUpdate(true).SetLink(gameObject, LinkBehaviour.KillOnDisable);
        }

        private void Update()
        {
            if (!_isZapping) return;

            _lr.widthMultiplier = Random.Range(minWidth, maxWidth);
            _lr.startColor = Random.value > 0.5f ? coreColor : auraColor;
            _lr.endColor = _lr.startColor;
            
            if (_currentFlash > 0)
            {
                _currentFlash -= (maxFlashIntensity / flashFadeDuration) * Time.unscaledDeltaTime;
                if (_currentFlash < 0) _currentFlash = 0f;

                for (int i = 0; i < _activeArrowsCache.Count; i++)
                {
                    if (_activeArrowsCache[i] != null && _activeArrowsCache[i].gameObject.activeInHierarchy)
                    {
                        _activeArrowsCache[i].SetFlashIntensity(_currentFlash);
                    }
                }
            }

            _flickerTimer += Time.unscaledDeltaTime;
            if (_flickerTimer >= flickerInterval)
            {
                _flickerTimer = 0f;
                DrawLightning();
            }
        }

        private void DrawLightning()
        {
            if (_targetPointsCache == null || _targetPointsCache.Count < 2) return;

            int pointIndex = 0;
            EnsureCacheCapacity(pointIndex);
            _renderPositionsCache[pointIndex++] = _targetPointsCache[0];

            for (int i = 0; i < _targetPointsCache.Count - 1; i++)
            {
                Vector3 start = _targetPointsCache[i];
                Vector3 end = _targetPointsCache[i + 1];
                float distance = Vector3.Distance(start, end);
                
                int segments = Mathf.Max(1, Mathf.RoundToInt(distance * segmentsPerUnit));
                Vector3 direction = (end - start).normalized;
                Vector3 perp = new Vector3(-direction.y, direction.x, 0);

                for (int j = 1; j < segments; j++)
                {
                    Vector3 basePos = Vector3.Lerp(start, end, (float)j / segments);
                    float offset = Random.Range(-zigzagMultiplier, zigzagMultiplier);
                    
                    EnsureCacheCapacity(pointIndex);
                    _renderPositionsCache[pointIndex++] = basePos + perp * offset;
                }
                
                EnsureCacheCapacity(pointIndex);
                _renderPositionsCache[pointIndex++] = end;
            }

            _lr.positionCount = pointIndex;
            
            for(int i = 0; i < pointIndex; i++)
            {
                _lr.SetPosition(i, _renderPositionsCache[i]);
            }
        }

        private void EnsureCacheCapacity(int requiredIndex)
        {
            if (requiredIndex >= _renderPositionsCache.Length)
            {
                System.Array.Resize(ref _renderPositionsCache, _renderPositionsCache.Length * 2);
            }
        }

        private void OnDisable()
        {
            _isZapping = false;
            if (_lr != null) _lr.positionCount = 0;
            Time.timeScale = 1f; 
            transform.DOKill(); 
        }
    }
}